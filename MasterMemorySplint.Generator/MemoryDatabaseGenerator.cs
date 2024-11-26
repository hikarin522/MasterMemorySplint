using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemorySplint.Generator;

internal static class MemoryDatabaseGenerator
{
    public static IncrementalValuesProvider<INamedTypeSymbol?> CreateMemoryDatabaseProvider(this SyntaxValueProvider provider)
    {
        return provider.CreateSyntaxProvider(
            static (node, ct) => {
                return !ct.IsCancellationRequested
                    && node is ClassDeclarationSyntax { BaseList.Types: { Count: > 0 } syntaxList }
                    && syntaxList.Any(static syntax => syntax is SimpleBaseTypeSyntax {
                        Type: SimpleNameSyntax {
                            Identifier.Text: "MemoryDatabaseBase",
                        }
                    });
            },
            static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct)
        ).WithComparer(SymbolEqualityComparer.Default);
    }

    public static void RegisterMemoryDatabaseExtensionsGenerator(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<INamedTypeSymbol?> memoryDatabaseProvider
    )
    {
        context.RegisterSourceOutput(memoryDatabaseProvider, registerSource);
    }

    private static void registerSource(SourceProductionContext ctx, INamedTypeSymbol? dbSymbol)
    {
        if (dbSymbol is not { BaseType.Name: "MemoryDatabaseBase" }) {
            return;
        }

        if (dbSymbol.GetMembers("Validate") is not [IMethodSymbol {
            Parameters.IsDefaultOrEmpty: true,
            ReturnType.Name: "ValidateResult",
            IsGenericMethod: false,
            DeclaringSyntaxReferences: [{ } methodSyntaxRef],
        }]) {
            return;
        }

        if (methodSyntaxRef.GetSyntax(ctx.CancellationToken) is not MethodDeclarationSyntax {
            Body.Statements: { Count: > 0 } statements,
        }) {
            return;
        }

        var tableList = enumerateTableListFromValidateMethodBody(statements).ToArray();

        ctx.AddSource($"{dbSymbol.PrintArity()}Extensions.cs", createMemoryDatabaseExtensionsSource(dbSymbol, tableList));
    }

    private static IEnumerable<(string, string)> enumerateTableListFromValidateMethodBody(SyntaxList<StatementSyntax> statements)
    {
        foreach (var syntax in statements) {
            if (syntax is ExpressionStatementSyntax {
                Expression: InvocationExpressionSyntax {
                    Expression: IdentifierNameSyntax {
                        Identifier.Text: "ValidateTable",
                    },
                    ArgumentList.Arguments: [{
                        Expression: MemberAccessExpressionSyntax {
                            OperatorToken.RawKind: (int)SyntaxKind.DotToken,
                            Name.Identifier.Text: "All",
                            Expression: IdentifierNameSyntax {
                                Identifier.Text: var table
                            },
                        }
                    }, _, {
                        Expression: LiteralExpressionSyntax {
                            Token.ValueText: var pk
                        }
                    }, _, _],
                }
            }) {
                yield return (table, pk);
            }
        }
    }

    private static string createMemoryDatabaseExtensionsSource(INamedTypeSymbol dbSymbol, ReadOnlySpan<(string, string)> tableList)
    {
        var sb = new StringBuilder();

        _ = sb.AppendLine($$"""
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MasterMemory.Validation;

using MasterMemorySplint;

namespace {{dbSymbol.PrintNamespace()}};

public static partial class {{dbSymbol.PrintMin()}}Extensions
{
#if !DISABLE_MASTERMEMORY_VALIDATOR
    public static ValidateResult Validate(this {{dbSymbol.Print()}} @this, int maxDegreeOfParallelism, CancellationToken cancellationToken = default)
        => @this.Validate(global::MasterMemorySplint.MemoryDatabaseHelper.Instance, maxDegreeOfParallelism, cancellationToken);

    public static ValidateResult Validate(this {{dbSymbol.Print()}} @this, IMemoryDatabaseHelper helper, int maxDegreeOfParallelism, CancellationToken cancellationToken = default)
    {
        if (maxDegreeOfParallelism is 1) {
            return @this.Validate();
        }

        var database = new ValidationDatabase(new object[] {
"""
        );

        foreach (var (table, _) in tableList) {
            _ = sb.AppendLine($$"""
            @this.{{table}},
"""
            );
        }

        _ = sb.AppendLine($$"""
        });

        var validators = new Action<ValidateResult>[] {
"""
        );

        foreach (var (table, pk) in tableList) {
            _ = sb.AppendLine($$"""
            result => {
                ((ITableUniqueValidate)@this.{{table}}).ValidateUnique(result);
                helper.ValidateTable(@this.{{table}}, database, "{{pk}}", result);
            },
"""
            );
        }

        _ = sb.AppendLine($$"""
        };

        var result = new ValidateResult();
        Parallel.For<ValidateResult>(0, validators.Length, new() {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        }, () => new ValidateResult(), (i, _, result) => {
            validators[i].Invoke(result);
            return result;
        }, subResult => {
            lock (result) {
                ((List<FaildItem>)result.FailedResults).AddRange(subResult.FailedResults);
            }
        });

        return result;
    }
#endif
}
"""
        );

        return sb.ToString();
    }
}
