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

        internal static bool IsNullOrEmpty(this ReadOnlyMemory<char>? memory) =>
            memory is not { Length: > 0 };

        internal static bool IsNullOrWhiteSpace(this ReadOnlyMemory<char>? memory)
        {
            if (memory is not { } m)
            {
                return true;
            }

            var span = m.Span;
            foreach (var c in span)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
