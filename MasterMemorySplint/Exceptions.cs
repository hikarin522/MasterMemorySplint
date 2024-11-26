using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MasterMemorySplint;

#if !NET8_0_OR_GREATER
internal static class ArgumentNullException
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        => _ = argument ?? throw new System.ArgumentNullException(paramName);
}
#endif
