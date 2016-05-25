﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A concurrent, simplified HashSet.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    internal sealed class ConcurrentSet<T> : ICollection<T>
    {
        /// <summary>
        /// The default concurrency level is 2. That means the collection can cope with up to two
        /// threads making simultaneous modifications without blocking.
        /// Note ConcurrentDictionary's default concurrency level is dynamic, scaling according to
        /// the number of processors.
        /// </summary>
        private const int DefaultConcurrencyLevel = 2;

        /// <summary>
        /// Taken from ConcurrentDictionary.DEFAULT_CAPACITY
        /// </summary>
        private const int DefaultCapacity = 31;

        /// <summary>
        /// The backing dictionary. The values are never used; just the keys.
        /// </summary>
        private readonly ConcurrentDictionary<T, byte> _dictionary;

        /// <summary>
        /// Construct a concurrent set with the default concurrency level.
        /// </summary>
        public ConcurrentSet()
        {
            _dictionary = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity);
        }

        /// <summary>
        /// Construct a concurrent set using the specified equality comparer.
        /// </summary>
        /// <param name="equalityComparer">The equality comparer for values in the set.</param>
        public ConcurrentSet(IEqualityComparer<T> equalityComparer)
        {
            _dictionary = new ConcurrentDictionary<T, byte>(DefaultConcurrencyLevel, DefaultCapacity, equalityComparer);
        }

        /// <summary>
        /// Obtain the number of elements in the set.
        /// </summary>
        /// <returns>The number of elements in the set.</returns>
        public int Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Determine whether the set is empty.</summary>
        /// <returns>true if the set is empty; otherwise, false.</returns>
        public bool IsEmpty
        {
            get { return _dictionary.IsEmpty; }
        }

        public bool IsReadOnly => false;

        /// <summary>
        /// Determine whether the given value is in the set.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>true if the set contains the specified value; otherwise, false.</returns>
        public bool Contains(T value)
        {
            return _dictionary.ContainsKey(value);
        }

        /// <summary>
        /// Attempts to add a value to the set.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>true if the value was added to the set. If the value already exists, this method returns false.</returns>
        public bool Add(T value)
        {
            return _dictionary.TryAdd(value, 0);
        }

        public void AddRange(IEnumerable<T> values)
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
        /// <param name="value">The value to remove.</param>
        /// <returns>true if the value was removed successfully; otherwise false.</returns>
        public bool Remove(T value)
        {
            byte b;
            return _dictionary.TryRemove(value, out b);
        }

        /// <summary>
        /// Clear the set
        /// </summary>
        public void Clear()
        {
            _dictionary.Clear();
        }

        public struct KeyEnumerator
        {
            private readonly IEnumerator<KeyValuePair<T, byte>> _kvpEnumerator;

            internal KeyEnumerator(IEnumerable<KeyValuePair<T, byte>> data)
            {
                _kvpEnumerator = data.GetEnumerator();
            }

            public T Current
            {
                get { return _kvpEnumerator.Current.Key; }
            }

            public bool MoveNext()
            {
                return _kvpEnumerator.MoveNext();
            }

            public void Reset()
            {
                _kvpEnumerator.Reset();
            }
        }

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

        private IEnumerator<T> GetEnumeratorImpl()
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
            return GetEnumeratorImpl();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}
