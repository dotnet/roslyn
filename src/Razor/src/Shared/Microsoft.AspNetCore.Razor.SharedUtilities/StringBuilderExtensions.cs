// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Razor;

internal static class StringBuilderExtensions
{
    public static void SetCapacityIfLarger(this StringBuilder builder, int newCapacity)
    {
        if (builder.Capacity < newCapacity)
        {
            builder.Capacity = newCapacity;
        }
    }

    /// <summary>
    /// Returns the string contents of the <see cref="StringBuilder"/> with leading and trailing
    /// whitespace removed, using a single allocation via <see cref="StringBuilder.ToString(int, int)"/>.
    /// </summary>
    public static string ToStringTrimmed(this StringBuilder builder)
    {
        var start = 0;
        while (start < builder.Length && char.IsWhiteSpace(builder[start]))
        {
            start++;
        }

        var end = builder.Length - 1;
        while (end >= start && char.IsWhiteSpace(builder[end]))
        {
            end--;
        }

        var length = end - start + 1;
        return length > 0 ? builder.ToString(start, length) : string.Empty;
    }
}
