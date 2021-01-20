// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable.Tests
{
    /// <summary>
    /// Produces the same hash for every value.
    /// </summary>
    /// <typeparam name="T">The type to hash</typeparam>
    internal class BadHasher<T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<T> _equalityComparer;

        internal BadHasher(IEqualityComparer<T> equalityComparer = null)
        {
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(T x, T y)
        {
            return _equalityComparer.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return 1;
        }
    }
}
