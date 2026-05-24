// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class ReadOnlyMemoryOfCharComparer : IEqualityComparer<ReadOnlyMemory<char>>
{
    public static readonly ReadOnlyMemoryOfCharComparer Instance = new ReadOnlyMemoryOfCharComparer();

    private ReadOnlyMemoryOfCharComparer()
    {
    }

    public static bool Equals(ReadOnlySpan<char> x, ReadOnlyMemory<char> y)
        => x.SequenceEqual(y.Span);

    public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y)
        => x.Span.SequenceEqual(y.Span);

    public int GetHashCode(ReadOnlyMemory<char> memory)
    {
#if NET
        return string.GetHashCode(memory.Span);
#else
        // We don't rely on ReadOnlyMemory<char>.GetHashCode() because it includes
        // the index and length, but we just want a hash based on the characters.
        var hashCombiner = HashCodeCombiner.Start();

        foreach (var ch in memory.Span)
        {
            hashCombiner.Add(ch);
        }

        return hashCombiner.CombinedHash;
#endif
    }
}
