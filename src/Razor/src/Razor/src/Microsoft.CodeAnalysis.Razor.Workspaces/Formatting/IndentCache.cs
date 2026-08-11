// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET
using System;
#endif
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal static class IndentCache
{
    // Copied from the compilers IndentCache
    internal const int MaxTabCount = 64;
    internal const int MaxSpaceCount = 128;

    internal const int MaxSpaceCountInMixedString = 8;

    private static readonly string?[] s_tabStrings = new string[MaxTabCount + 1];
    private static readonly string?[] s_spaceStrings = new string[MaxSpaceCount + 1];
    // Mixed tab+space indentation is sparser than tab-only or space-only lookups, so we keep
    // the first dimension by tab count and lazily allocate each per-tab space row on first use.
    private static readonly string?[][] s_mixedStrings = new string[MaxTabCount + 1][];

    public static string GetIndentString(int size, bool insertSpaces, int tabSize)
    {
        ArgHelper.ThrowIfNegative(size);
        ArgHelper.ThrowIfNegativeOrZero(tabSize);

        if (size == 0)
        {
            return string.Empty;
        }

        if (insertSpaces)
        {
            return GetSingleCharacterString(size, ' ', s_spaceStrings);
        }

        var tabCount = size / tabSize;
        var spaceCount = size % tabSize;

        if (spaceCount == 0)
        {
            return GetSingleCharacterString(tabCount, '\t', s_tabStrings);
        }

        if (tabCount == 0)
        {
            return GetSingleCharacterString(spaceCount, ' ', s_spaceStrings);
        }

        return GetMixedString(tabCount, spaceCount);
    }

    private static string GetSingleCharacterString(int length, char character, string?[] cache)
    {
        if (length >= cache.Length)
        {
            return new string(character, length);
        }

        var indentString = cache[length];
        if (indentString is not null)
        {
            return indentString;
        }

        indentString = new string(character, length);
        cache[length] = indentString;
        return indentString;
    }

    private static string GetMixedString(int tabCount, int spaceCount)
    {
        if (tabCount > MaxTabCount || spaceCount > MaxSpaceCountInMixedString)
        {
            return CreateMixedString(tabCount, spaceCount);
        }

        var tabStrings = s_mixedStrings[tabCount];
        if (tabStrings is null)
        {
            tabStrings = new string?[MaxSpaceCountInMixedString + 1];
            s_mixedStrings[tabCount] = tabStrings;
        }

        var indentString = tabStrings[spaceCount];
        if (indentString is not null)
        {
            return indentString;
        }

        indentString = CreateMixedString(tabCount, spaceCount);
        tabStrings[spaceCount] = indentString;
        return indentString;
    }

    private static string CreateMixedString(int tabCount, int spaceCount)
    {
        return string.Create(length: tabCount + spaceCount, state: tabCount, static (destination, tabCount) =>
        {
            destination[..tabCount].Fill('\t');
            destination[tabCount..].Fill(' ');
        });
    }
}
