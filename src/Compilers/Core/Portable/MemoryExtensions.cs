// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class MemoryExtensions
    {
        public static int IndexOf(this ReadOnlySpan<char> span, char target, int startIndex)
        {
            for (int i = startIndex; i < span.Length; i++)
            {
                if (span[i] == target)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOfAny(this ReadOnlySpan<char> span, char[] characters)
        {
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                foreach (var target in characters)
                {
                    if (c == target)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

#if !NETCOREAPP
        internal static ReadOnlyMemory<char> TrimStart(this ReadOnlyMemory<char> memory)
        {
            var span = memory.Span;
            var index = 0;
            while (index < span.Length && char.IsWhiteSpace(span[index]))
            {
                index++;
            }

            return memory.Slice(index, span.Length - index);
        }

        internal static ReadOnlyMemory<char> TrimEnd(this ReadOnlyMemory<char> memory)
        {
            var span = memory.Span;
            var length = span.Length;
            while (length - 1 >= 0 && char.IsWhiteSpace(span[length - 1]))
            {
                length--;
            }

            return memory.Slice(0, length);
        }

        internal static ReadOnlyMemory<char> Trim(this ReadOnlyMemory<char> memory) => memory.TrimStart().TrimEnd();
#endif

        internal static bool IsNullOrEmpty(this ReadOnlyMemory<char>? memory) =>
            memory is not { Length: > 0 };

        internal static bool IsNullOrWhiteSpace(this ReadOnlyMemory<char>? memory) =>
            memory is not { } m || IsWhiteSpace(m);

        internal static bool IsWhiteSpace(this ReadOnlyMemory<char> memory)
        {
            var span = memory.Span;
            foreach (var c in span)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool StartsWith(this ReadOnlyMemory<char> memory, char c) => memory.Length > 0 && memory.Span[0] == c;

        internal static ReadOnlyMemory<char> Unquote(this ReadOnlyMemory<char> memory)
        {
            var span = memory.Span;
            if (span.Length > 1 && span[0] == '"' && span[span.Length - 1] == '"')
            {
                return memory.Slice(1, memory.Length - 2);
            }

            return memory;
        }
    }
}
