// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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

        public int GetHashCode(ReadOnlyMemory<char> obj)
        {
#if NET
            return string.GetHashCode(obj.Span);
#else
            return Hash.GetFNVHashCode(obj.Span);
#endif
        }
    }

    /// <summary>
    /// Very cheap trivial comparer that never matches the keys,
    /// should only be used in empty dictionaries.
    /// </summary>
    internal sealed class EmptyReadOnlyMemoryOfCharComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly EmptyReadOnlyMemoryOfCharComparer Instance = new EmptyReadOnlyMemoryOfCharComparer();

        private EmptyReadOnlyMemoryOfCharComparer()
        {
        }

        public bool Equals(ReadOnlyMemory<char> a, ReadOnlyMemory<char> b)
            => throw ExceptionUtilities.Unreachable();

        public int GetHashCode(ReadOnlyMemory<char> s)
        {
            // dictionary will call this often
            return 0;
        }
    }
}
