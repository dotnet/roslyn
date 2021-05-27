// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class EnumerableExtensionsTests
    {
        [Fact]
        public void AsSingleton()
        {
            Assert.Equal(0, new int[] { }.AsSingleton());
            Assert.Equal(1, new int[] { 1 }.AsSingleton());
            Assert.Equal(0, new int[] { 1, 2 }.AsSingleton());

            Assert.Equal(0, Enumerable.Range(1, 0).AsSingleton());
            Assert.Equal(1, Enumerable.Range(1, 1).AsSingleton());
            Assert.Equal(0, Enumerable.Range(1, 2).AsSingleton());
        }

        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private readonly T[] _items;

            public ReadOnlyList(params T[] items)
            {
                _items = items;
            }

            public T this[int index] => _items[index];
            public int Count => _items.Length;
            public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        private class SignlessEqualityComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y) => Math.Abs(x) == Math.Abs(y);
            public int GetHashCode(int obj) => throw new NotImplementedException();
        }

        [Fact]
        public void IndexOf()
        {
            Assert.Equal(-1, Enumerable.Range(1, 5).IndexOf(6));
            Assert.Equal(2, Enumerable.Range(1, 5).IndexOf(3));

            Assert.Equal(-1, ((IEnumerable<int>)SpecializedCollections.SingletonList(5)).IndexOf(6));
            Assert.Equal(0, ((IEnumerable<int>)SpecializedCollections.SingletonList(5)).IndexOf(5));

            Assert.Equal(-1, ((IEnumerable<int>)new ReadOnlyList<int>(5)).IndexOf(6));
            Assert.Equal(0, ((IEnumerable<int>)new ReadOnlyList<int>(5)).IndexOf(5));
        }

        [Fact]
        public void IndexOf_EqualityComparer()
        {
            var comparer = new SignlessEqualityComparer();

            Assert.Equal(-1, Enumerable.Range(1, 5).IndexOf(-6, comparer));
            Assert.Equal(2, Enumerable.Range(1, 5).IndexOf(-3, comparer));

            Assert.Equal(-1, ((IEnumerable<int>)SpecializedCollections.SingletonList(5)).IndexOf(-6, comparer));
            Assert.Equal(0, ((IEnumerable<int>)SpecializedCollections.SingletonList(5)).IndexOf(-5, comparer));

            Assert.Equal(-1, ((IEnumerable<int>)new ReadOnlyList<int>(5)).IndexOf(-6, comparer));
            Assert.Equal(0, ((IEnumerable<int>)new ReadOnlyList<int>(5)).IndexOf(-5, comparer));
        }
    }
}
