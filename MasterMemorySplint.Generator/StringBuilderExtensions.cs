using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMemorySplint.Generator;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendLines(this StringBuilder sb, IEnumerable<string> values)
    {
        foreach (var value in values) {
            _ = sb.AppendLine(value);
        }
        return sb;
    }

    public static StringBuilder AppendLines<T>(this StringBuilder sb, IEnumerable<T> values, Func<T, string> selector)
    {
        foreach (var value in values) {
            _ = sb.AppendLine(selector(value));
        }
        return sb;
    }

    public static StringBuilder AppendLines(this StringBuilder sb, ReadOnlySpan<string> values)
    {
        foreach (var value in values) {
            _ = sb.AppendLine(value);
        }
        return sb;
    }

    public static StringBuilder AppendLines<T>(this StringBuilder sb, ReadOnlySpan<T> values, Func<T, string> selector)
    {
        foreach (var value in values) {
            _ = sb.AppendLine(selector(value));
        }
        return sb;
    }
}
