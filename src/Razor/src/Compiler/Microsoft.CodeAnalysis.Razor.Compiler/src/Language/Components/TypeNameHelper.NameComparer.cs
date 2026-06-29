// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class TypeNameHelper
{
    private sealed class NameComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly NameComparer Instance = new();

        private NameComparer()
        {
        }

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
            => x.Span.Equals(y.Span, StringComparison.Ordinal);

        public int GetHashCode(ReadOnlyMemory<char> memory)
        {
            // We don't rely on ReadOnlyMemory<char>.GetHashCode() because it includes
            // the index and length, but we just want a hash based on the characters.
            var hashCombiner = HashCodeCombiner.Start();

            foreach (var ch in memory.Span)
            {
                hashCombiner.Add(ch);
            }

            return hashCombiner.CombinedHash;
        }
    }
}
