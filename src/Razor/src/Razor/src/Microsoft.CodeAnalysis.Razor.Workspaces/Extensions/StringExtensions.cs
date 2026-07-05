// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor;

internal static class StringExtensions
{
    public static bool IsRazorFilePath(this string filePath)
    {
        var comparison = PathUtilities.OSSpecificPathComparison;

        return FileUtilities.IsAnyRazorFilePath(filePath, comparison);
    }

    public static int? GetFirstNonWhitespaceOffset(this string line)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                return i;
            }
        }

        return null;
    }

    public static int? GetLastNonWhitespaceOffset(this string line)
    {
        for (var i = line.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(line[i]))
            {
                return i;
            }
        }

        return null;
    }

    public static string GetLeadingWhitespace(this string lineText)
    {
        var firstOffset = lineText.GetFirstNonWhitespaceOffset();

        return firstOffset.HasValue
            ? lineText[..firstOffset.Value]
            : lineText;
    }

    public static string GetTrailingWhitespace(this string lineText)
    {
        var lastOffset = lineText.GetLastNonWhitespaceOffset();

        return lastOffset.HasValue
            ? lineText[lastOffset.Value..]
            : lineText;
    }
}
