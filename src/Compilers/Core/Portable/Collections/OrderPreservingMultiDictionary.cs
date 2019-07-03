// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A MultiDictionary that allows only adding, and preserves the order of values added to the 
    /// dictionary. Thread-safe for reading, but not for adding.
    /// </summary>
    /// <remarks>
    /// Always uses the default comparer.
    /// </remarks>
    internal sealed class OrderPreservingMultiDictionary<K, V> :
        IEnumerable<KeyValuePair<K, OrderPreservingMultiDictionary<K, V>.ValueSet>>
    {
        #region Pooling

        private readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> _pool;

        private OrderPreservingMultiDictionary(ObjectPool<OrderPreservingMultiDictionary<K, V>> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            if (_dictionary != null)
            {
                // Allow our ValueSets to return their underlying ArrayBuilders to the pool.
                foreach (var kvp in _dictionary)
                {
                    kvp.Value.Free();
                }

                _dictionary.Free();
                _dictionary = null;
            }

            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<OrderPreservingMultiDictionary<K, V>> s_poolInstance = CreatePool();

        // if someone needs to create a pool;
        public static ObjectPool<OrderPreservingMultiDictionary<K, V>> CreatePool()
        {
            ObjectPool<OrderPreservingMultiDictionary<K, V>> pool = null;
            pool = new ObjectPool<OrderPreservingMultiDictionary<K, V>>(() => new OrderPreservingMultiDictionary<K, V>(pool), 16); // Size is a guess.
            return pool;
        }

        public static OrderPreservingMultiDictionary<K, V> GetInstance()
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance.IsEmpty);
            return instance;
        }

        #endregion Pooling

        // An empty dictionary we keep around to simplify certain operations (like "Keys")
        // when we don't have an underlying dictionary of our own.
        private static readonly Dictionary<K, ValueSet> s_emptyDictionary = new Dictionary<K, ValueSet>();

        // The underlying dictionary we store our data in.  null if we are empty.
        private PooledDictionary<K, ValueSet> _dictionary;

        public OrderPreservingMultiDictionary()
        {
        }

        private void EnsureDictionary()
        {
            _dictionary ??= PooledDictionary<K, ValueSet>.GetInstance();
        }

        public bool IsEmpty => _dictionary == null;

        /// <summary>
        /// Add a value to the dictionary.
        /// </summary>
        public void Add(K k, V v)
        {
            if (!this.IsEmpty && _dictionary.TryGetValue(k, out var valueSet))
            {
                Debug.Assert(valueSet.Count >= 1);
                // Have to re-store the ValueSet in case we upgraded the existing ValueSet from 
                // holding a single item to holding multiple items.
                _dictionary[k] = valueSet.WithAddedItem(v);
            }
            else
            {
                this.EnsureDictionary();
                _dictionary[k] = new ValueSet(v);
            }
        }

        public Dictionary<K, ValueSet>.Enumerator GetEnumerator()
        {
            return IsEmpty ? s_emptyDictionary.GetEnumerator() : _dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<K, ValueSet>> IEnumerable<KeyValuePair<K, ValueSet>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get all values associated with K, in the order they were added.
        /// Returns empty read-only array if no values were present.
        /// </summary>
        public ImmutableArray<V> this[K k]
        {
            get
            {
                if (!this.IsEmpty && _dictionary.TryGetValue(k, out var valueSet))
                {
                    Debug.Assert(valueSet.Count >= 1);
                    return valueSet.Items;
                }

                return ImmutableArray<V>.Empty;
            }
        }

        public bool Contains(K key, V value)
        {
            return !this.IsEmpty &&
                _dictionary.TryGetValue(key, out var valueSet) &&
                valueSet.Contains(value);
        }

        /// <summary>
        /// Get a collection of all the keys.
        /// </summary>
        public Dictionary<K, ValueSet>.KeyCollection Keys
        {
            get { return this.IsEmpty ? s_emptyDictionary.Keys : _dictionary.Keys; }
        }

        public struct ValueSet : IEnumerable<V>
        {
            /// <summary>
            /// Each value is either a single V or an <see cref="ArrayBuilder{V}"/>.
            /// Never null.
            /// </summary>
            private readonly object _value;

            internal ValueSet(V value)
            {
                _value = value;
            }

            internal ValueSet(ArrayBuilder<V> values)
            {
                _value = values;
            }

            internal void Free()
            {
                var arrayBuilder = _value as ArrayBuilder<V>;
                arrayBuilder?.Free();
            }

            internal V this[int index]
            {
                get
                {
                    Debug.Assert(this.Count >= 1);

                    var arrayBuilder = _value as ArrayBuilder<V>;
                    if (arrayBuilder == null)
                    {
                        if (index == 0)
                        {
                            return (V)_value;
                        }
                        else
                        {
                            throw new IndexOutOfRangeException();
                        }
                    }
                    else
                    {
                        return arrayBuilder[index];
                    }
                }
            }

            internal bool Contains(V item)
            {
                Debug.Assert(this.Count >= 1);
                var arrayBuilder = _value as ArrayBuilder<V>;
                return arrayBuilder == null
                    ? EqualityComparer<V>.Default.Equals(item, (V)_value)
                    : arrayBuilder.Contains(item);
            }

            internal ImmutableArray<V> Items
            {
                get
                {
                    Debug.Assert(this.Count >= 1);

                    var arrayBuilder = _value as ArrayBuilder<V>;
                    if (arrayBuilder == null)
                    {
                        // promote singleton to set
                        Debug.Assert(_value is V, "Item must be a a V");
                        return ImmutableArray.Create<V>((V)_value);
                    }
                    else
                    {
                        return arrayBuilder.ToImmutable();
                    }
                }
            }

            internal int Count => (_value as ArrayBuilder<V>)?.Count ?? 1;

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            internal ValueSet WithAddedItem(V item)
            {
                Debug.Assert(this.Count >= 1);

                var arrayBuilder = _value as ArrayBuilder<V>;
                if (arrayBuilder == null)
                {
                    // Promote from singleton V to ArrayBuilder<V>.
                    Debug.Assert(_value is V, "_value must be a V");

                    // By default we allocate array builders with a size of two.  That's to store
                    // the single item already in _value, and to store the item we're adding.  
                    // In general, we presume that the amount of values per key will be low, so this
                    // means we have very little overhead when there are multiple keys per value.
                    arrayBuilder = ArrayBuilder<V>.GetInstance(capacity: 2);
                    arrayBuilder.Add((V)_value);
                    arrayBuilder.Add(item);
                }
                else
                {
                    arrayBuilder.Add(item);
                }

                return new ValueSet(arrayBuilder);
            }

            public struct Enumerator : IEnumerator<V>
            {
                private readonly ValueSet _valueSet;
                private readonly int _count;
                private int _index;

                public Enumerator(ValueSet valueSet)
                {
                    _valueSet = valueSet;
                    _count = _valueSet.Count;
                    Debug.Assert(_count >= 1);
                    _index = -1;
                }

                public V Current => _valueSet[_index];

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    return _index < _count;
                }

                public void Reset()
                {
                    _index = -1;
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
