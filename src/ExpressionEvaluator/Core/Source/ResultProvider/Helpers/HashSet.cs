// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

#nullable disable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the hash set.</typeparam>
    internal sealed class HashSet<T> : ICollection<T>, IEnumerable<T>
    {
        // Note that the value of this dictionary is unused. This is just being used as a the
        // underlying HashSet of this wrapper class since CLR 2.0 doesn't have an actual HashSet.
        private readonly Dictionary<T, object> _items;

        /// <inheritdoc/>
        public int Count => _items.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the Comparer Instance used to determine if two elements of the
        /// collection are equal or not.
        /// </summary>
        public IEqualityComparer<T> Comparer => _items.Comparer;

        /// <summary>
        /// Initializes a new instance of the HashSet class that is empty
        /// and uses the default equality comparer for the set type.
        /// </summary>
        public HashSet() : this(null) { }

        /// <summary>
        /// Initializes a new instance of the <code>HashSet{T}</code>
        /// class that is empty, but has reserved space for <code>capacity</code>
        /// items and uses the default equality comparer for the set type.
        /// </summary>
        /// <param name="capacity">The initial size of the <code>HashSet{T}</code>.</param>
        public HashSet(int capacity) : this(capacity, null) { }

        /// <summary>
        /// Initializes a new instance of the HashSet class that is empty
        /// and uses the specified equality comparer for the set type.
        /// </summary>
        /// <param name="customComparer">The <see cref="IEqualityComparer{T}"/> implementation
        /// to use when comparing values in the set, or <code>null</code> to use the default 
        /// <see cref="IEqualityComparer{T}"/> implementation for the set type.</param>
        public HashSet(IEqualityComparer<T> customComparer)
        {
            _items = (customComparer == null) ? new Dictionary<T, object>() :
                                                new Dictionary<T, object>(customComparer);
        }

        /// <summary>
        /// Initializes a new instance of the <code>HashSet{T}</code> class that
        /// uses the specified equality comparer for the set type, and has
        /// sufficient capacity to accommodate <code>capacity</code> elements.
        /// </summary>
        /// <param name="capacity">The initial size of the <code>HashSet{T}</code>.</param>
        /// <param name="customComparer"></param>
        public HashSet(int capacity, IEqualityComparer<T> customComparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = (customComparer == null) ? new Dictionary<T, object>(capacity) :
                                                new Dictionary<T, object>(capacity, customComparer);
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            if (null == item)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _items[item] = null;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _items.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return _items.ContainsKey(item);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex >= array.Length || arrayIndex >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            _items.Keys.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            return _items.Remove(item);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
