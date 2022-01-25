// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.InternalUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class ConcurrentLruCacheTests
    {
        /// <summary>
        /// Dictionary for testing ConcurrentLruCache.
        /// Like OrderedDictionary in the BCL, doesn't sort elements,
        /// but rather stores them in the order added.
        /// </summary>
        private class OrderedTestDictionary<K, V>
            : IEnumerable<KeyValuePair<K, V>>
        {
            private readonly KeyValuePair<K, V>[] _store;
            private int _index;
            public OrderedTestDictionary(int capacity)
            {
                _store = new KeyValuePair<K, V>[capacity];
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<K, V>>)_store).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public ConcurrentLruCache<K, V> MakeCache()
            {
                return new ConcurrentLruCache<K, V>(_store);
            }

            public void Add(K key, V value)
            {
                _store[_index++] = new KeyValuePair<K, V>(key, value);
            }
        }

        [Fact]
        public void CacheHoldsCapacity()
        {
            var clc = new OrderedTestDictionary<int, int>(3)
                { {1, 1}, {2, 2}, {3, 3} }.MakeCache();

            var expected = new OrderedTestDictionary<int, int>(3)
                { {3, 3}, {2, 2}, {1, 1}};

            Assert.True(clc.TestingEnumerable.SequenceEqual(expected));
        }

        [Fact]
        public void CacheOverwritesKey()
        {
            var clc = new OrderedTestDictionary<int, int>(3)
                { {1, 1}, {2, 2}, {3, 3} }.MakeCache();
            clc[3] = 0;

            var expected = new OrderedTestDictionary<int, int>(3)
                { {3, 0}, {2, 2}, {1, 1}};

            Assert.True(clc.TestingEnumerable.SequenceEqual(expected));
        }

        [Fact]
        public void CacheEvictsNoRead()
        {
            var clc = new OrderedTestDictionary<int, int>(3)
                { {1, 1}, {2, 2}, {3, 3} }.MakeCache();
            clc[4] = 4;

            var expected = new OrderedTestDictionary<int, int>(3)
                { {4, 4 }, {3, 3}, {2, 2} };

            Assert.Equal(expected, clc.TestingEnumerable);
        }

        [Fact]
        public void CacheEvictsWithRead()
        {
            var clc = new OrderedTestDictionary<int, int>(3)
                { {1, 1}, {2, 2}, {3, 3} }.MakeCache();
            int oneVal = clc[1];
            clc[4] = 4;

            var expected = new OrderedTestDictionary<int, int>(3)
                { {4, 4 }, {1, 1}, {3, 3}, };

            Assert.Equal(expected, clc.TestingEnumerable);
        }
    }
}
