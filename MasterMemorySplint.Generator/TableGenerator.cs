using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MasterMemorySplint.Generator;

internal static class TableGenerator
{
    public static IncrementalValuesProvider<INamedTypeSymbol?> CreateTableProvider(this SyntaxValueProvider provider)
    {
        return provider.CreateSyntaxProvider(
            static (node, ct) => {
                return !ct.IsCancellationRequested
                    && node is ClassDeclarationSyntax { BaseList.Types: { Count: > 0 } syntaxList }
                    && syntaxList.Any(static syntax => syntax is SimpleBaseTypeSyntax {
                        Type: GenericNameSyntax {
                            Identifier.Text: "TableBase",
                            TypeArgumentList.Arguments.Count: 1,
                        }
                    });
            },
            static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)ctx.Node, ct)
        ).WithComparer(SymbolEqualityComparer.Default);
    }

    public static void RegisterTableGenerator(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<INamedTypeSymbol?> tableProvider
    )
    {
        context.RegisterSourceOutput(tableProvider, registerTableSource);
    }

    private static void registerTableSource(SourceProductionContext ctx, INamedTypeSymbol? tableSymbol)
    {
        if (tableSymbol is not {
            BaseType: {
                Name: "TableBase",
                IsGenericType: true,
                TypeArguments: [INamedTypeSymbol elementSymbol],
            },
        }) {
            return;
        }

        if (elementSymbol.GetAttributes().FirstOrDefault(static attr => attr is {
            AttributeClass.Name: "MemoryTableAttribute",
        }) is not {
            ConstructorArguments: [{ Value: string tableName }],
        }) {
            return;
        }

        if (tableSymbol.GetMembers("PrimaryKeySelector") is not ([IPropertySymbol {
            Type: INamedTypeSymbol {
                Name: "Func",
                IsGenericType: true,
                TypeArguments: [INamedTypeSymbol arg, { } pkTypeSymbol],
            },
        }]) || !SymbolEqualityComparer.Default.Equals(elementSymbol, arg)) {
            return;
        }

        if (tableSymbol.Constructors.FirstOrDefault(e => e is {
            Parameters: [{ Type: IArrayTypeSymbol { ElementType: INamedTypeSymbol arg } }],
        } && SymbolEqualityComparer.Default.Equals(elementSymbol, arg)) is not {
            DeclaringSyntaxReferences: [{ } methodSyntaxRef],
        }) {
            return;
        }

        if (methodSyntaxRef.GetSyntax(ctx.CancellationToken) is not ConstructorDeclarationSyntax {
            Body.Statements: [ExpressionStatementSyntax {
                Expression: AssignmentExpressionSyntax {
                    Left: MemberAccessExpressionSyntax {
                        Expression: ThisExpressionSyntax,
                        Name: IdentifierNameSyntax { Identifier.Text: "primaryIndexSelector" },
                    },
                    Right: SimpleLambdaExpressionSyntax { ExpressionBody: { } } pkLambda,
                },
            }, ..],
        }) {
            return;
        }

        ctx.AddSource($"{tableSymbol.PrintArity()}.cs", createTableSource(
            tableSymbol,
            tableName,
            elementSymbol,
            pkTypeSymbol,
            pkLambda
        ));
    }

    private static string createTableSource(
        INamedTypeSymbol tableSymbol,
        string tableName,
        INamedTypeSymbol elementSymbol,
        ITypeSymbol pkTypeSymbol,
        SimpleLambdaExpressionSyntax pkLambda
    )
    {
        var sb = new StringBuilder();

        _ = sb.AppendLine($$"""
using System;
using System.Collections.Generic;

using MasterMemorySplint;

namespace {{tableSymbol.PrintNamespace()}};

partial class {{tableSymbol.PrintMin()}}: ITable<{{tableSymbol.Print()}}, {{elementSymbol.Print()}}, {{pkTypeSymbol.Print()}}>
{
    public static Type ElementType => typeof({{elementSymbol.Print()}});

    public static Type PrimaryKeyType => typeof({{pkTypeSymbol.Print()}});

    public static string TableName => "{{tableName}}";

    public static {{pkTypeSymbol.Print()}} SelectPrimaryKey({{elementSymbol.Print()}} {{pkLambda.Parameter}}) => {{pkLambda.Body}};

    public static {{tableSymbol.PrintMin()}} CreateFromSortedData({{elementSymbol.Print()}}[] sortedData) => new {{tableSymbol.PrintMin()}}(sortedData);

    public static {{tableSymbol.PrintMin()}} Create({{elementSymbol.Print()}}[] data)
    {
        var keys = new {{pkTypeSymbol.Print()}}[data.Length];
        for (var i = 0; i < data.Length; ++i) {
            keys[i] = SelectPrimaryKey(data[i]);
        }
        Array.Sort(keys, data, 0, data.Length, {{(pkTypeSymbol.SpecialType is SpecialType.System_String ? "StringComparer.Ordinal" : $"Comparer<{pkTypeSymbol.Print()}>.Default")}});
        return CreateFromSortedData(data);
    }

#if !NET8_0_OR_GREATER
    Delegate ITable.PrimaryKeySelector => this.PrimaryKeySelector;

    object[] ITable.GetRawDataUnsafe() => this.GetRawDataUnsafe();
#endif

#if NET8_0_OR_GREATER && DISABLE_MASTERMEMORY_METADATABASE
    static MetaTable ITable.CreateMetaTable() => throw new NotImplementedException("DISABLE_MASTERMEMORY_METADATABASE is defined");
#endif
}
""");

        return sb.ToString();
    }
}
