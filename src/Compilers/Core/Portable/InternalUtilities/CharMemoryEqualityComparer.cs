// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Provide structural equality for ReadOnlyMemory{char} instances.
    /// </summary>
    internal sealed class CharMemoryEqualityComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public static readonly CharMemoryEqualityComparer Instance = new CharMemoryEqualityComparer();

        private CharMemoryEqualityComparer() { }

        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) => x.Span.SequenceEqual(y.Span);

        public int GetHashCode(ReadOnlyMemory<char> mem) => Hash.GetFNVHashCode(mem.Span);
    }
}
