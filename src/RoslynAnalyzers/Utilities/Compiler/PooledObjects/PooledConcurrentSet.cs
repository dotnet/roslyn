// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    /// <summary>
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> that can be recycled via an object pool.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
    internal sealed class PooledConcurrentSet<T> : ICollection<T>, IDisposable
        where T : notnull
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        private readonly PooledConcurrentDictionary<T, byte> _dictionary;

        private PooledConcurrentSet(PooledConcurrentDictionary<T, byte> dictionary)
        {
            _dictionary = dictionary;
        }

        public void Dispose() => Free(CancellationToken.None);
        public void Free(CancellationToken cancellationToken) => _dictionary.Free(cancellationToken);

        public static PooledConcurrentSet<T> GetInstance(IEqualityComparer<T>? comparer = null)
        {
            var dictionary = PooledConcurrentDictionary<T, byte>.GetInstance(comparer);
            return new PooledConcurrentSet<T>(dictionary);
        }

        public static PooledConcurrentSet<T> GetInstance(IEnumerable<T> initializer, IEqualityComparer<T>? comparer = null)
        {
            var instance = GetInstance(comparer);
            foreach (var item in initializer)
            {
                instance.Add(item);
            }

            return instance;
        }

        /// <summary>
        /// Obtain the number of elements in the set.
        /// </summary>
        /// <returns>The number of elements in the set.</returns>
        public int Count => _dictionary.Count;

        /// <summary>
        /// Determine whether the set is empty.</summary>
        /// <returns>true if the set is empty; otherwise, false.</returns>
        public bool IsEmpty => _dictionary.IsEmpty;

        public bool IsReadOnly => false;

        /// <summary>
        /// Attempts to add a <paramref name="value"/> to the set.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>true if the value was added to the set. If the value already exists, this method returns false.</returns>
        public bool Add(T value) => _dictionary.TryAdd(value, 0);

        /// <summary>
        /// Adds the given <paramref name="values"/> to the set.
        /// </summary>
        public void AddRange(IEnumerable<T>? values)
        {
            if (values != null)
            {
                foreach (var v in values)
                {
                    Add(v);
                }
            }
        }

        /// <summary>
        /// Attempts to remove a value from the set.
        /// </summary>
        /// <param name="item">The value to remove.</param>
        /// <returns>true if the value was removed successfully; otherwise false.</returns>
        public bool Remove(T item) => _dictionary.TryRemove(item, out _);

        /// <summary>
        /// Clears all the elements from the set.
        /// </summary>
        public void Clear() => _dictionary.Clear();

        /// <summary>
        /// Returns true if the given <paramref name="item"/> is present in the set.
        /// </summary>
        public bool Contains(T item) => _dictionary.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        /// <summary>
        /// Obtain an enumerator that iterates through the elements in the set.
        /// </summary>
        /// <returns>An enumerator for the set.</returns>
        public KeyEnumerator GetEnumerator()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.
            return new KeyEnumerator(_dictionary);
        }

        private IEnumerator<T> GetEnumeratorCore()
        {
            // PERF: Do not use dictionary.Keys here because that creates a snapshot
            // of the collection resulting in a List<T> allocation. Instead, use the
            // KeyValuePair enumerator and pick off the Key part.
            foreach (var kvp in _dictionary)
            {
                yield return kvp.Key;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumeratorCore();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorCore();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        public readonly struct KeyEnumerator
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            private readonly IEnumerator<KeyValuePair<T, byte>> _kvpEnumerator;

            internal KeyEnumerator(IEnumerable<KeyValuePair<T, byte>> data)
            {
                _kvpEnumerator = data.GetEnumerator();
            }

            public T Current => _kvpEnumerator.Current.Key;

            public bool MoveNext()
            {
                return _kvpEnumerator.MoveNext();
            }

            public void Reset()
            {
                _kvpEnumerator.Reset();
            }
        }
    }
}
