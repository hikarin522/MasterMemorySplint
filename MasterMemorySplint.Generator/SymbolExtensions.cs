using System.Reflection;

using Microsoft.CodeAnalysis;

namespace MasterMemorySplint.Generator;

internal static class SymbolExtensions
{
    public static string Print(this ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string PrintMin(this ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    public static string PrintNamespace(this ISymbol symbol) => symbol.ContainingNamespace.ToDisplayString(NamespaceFormat);

    private static readonly SymbolDisplayFormat NamespaceFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    public static string PrintArity(this ISymbol symbol) => symbol.ToDisplayString(QualifiedNameArityFormat);

    private static readonly SymbolDisplayFormat QualifiedNameArityFormat = (SymbolDisplayFormat)typeof(SymbolDisplayFormat)
        .GetField(nameof(QualifiedNameArityFormat), BindingFlags.Static | BindingFlags.NonPublic)
        .GetValue(null);
}
