// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public sealed partial class CodeWriter
{
    internal static class IndentCache
    {
        internal const int MaxTabCount = 64;
        internal const int MaxSpaceCount = 128;

        private static readonly ReadOnlyMemory<char> s_tabs = new string('\t', MaxTabCount).AsMemory();
        private static readonly ReadOnlyMemory<char> s_spaces = new string(' ', MaxSpaceCount).AsMemory();

        public static ReadOnlyMemory<char> GetIndentString(int size, bool useTabs, int tabSize)
        {
            if (!useTabs)
            {
                return SliceOrCreate(size, s_spaces);
            }

            var tabCount = size / tabSize;
            var spaceCount = size % tabSize;

            if (spaceCount == 0)
            {
                return SliceOrCreate(tabCount, s_tabs);
            }

            return string.Create(length: tabCount + spaceCount, state: tabCount, static (destination, tabCount) =>
            {
                destination[..tabCount].Fill('\t');
                destination[tabCount..].Fill(' ');
            }).AsMemory();
        }

        private static ReadOnlyMemory<char> SliceOrCreate(int length, ReadOnlyMemory<char> chars)
        {
            return (length <= chars.Length)
                ? chars[..length]
                : new string(chars.Span[0], length).AsMemory();
        }
    }
}
