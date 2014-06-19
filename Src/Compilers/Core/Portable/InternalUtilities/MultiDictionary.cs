// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    // Note that this is not threadsafe for concurrent reading and writing.
    internal sealed class MultiDictionary<K, V>
    {
        // store either a single V or an ImmutableHashSet<V>
        private readonly Dictionary<K, object> dictionary;

        public MultiDictionary()
        {
            this.dictionary = new Dictionary<K, object>();
        }

        public MultiDictionary(IEqualityComparer<K> comparer)
        {
            this.dictionary = new Dictionary<K, object>(comparer);
        }

        public MultiDictionary(int capacity, IEqualityComparer<K> comparer)
        {
            this.dictionary = new Dictionary<K, object>(capacity, comparer);
        }

        public void Add(K k, V v)
        {
            object item;
            if (this.dictionary.TryGetValue(k, out item))
            {
                var set = item as ImmutableHashSet<V> ?? ImmutableHashSet.Create<V>((V)item);
                this.dictionary[k] = set.Add(v);
            }
            else
            {
                this.dictionary[k] = v;
            }
        }

        public int KeyCount
        {
            get { return this.dictionary.Count; }
        }

        public int GetCountForKey(K k)
        {
            object item;
            if (this.dictionary.TryGetValue(k, out item))
            {
                var set = item as ImmutableHashSet<V>;
                return set == null ? 1 : set.Count;
            }

            return 0;
        }

        public bool TryGetSingleValue(K k, out V v)
        {
            object item;
            if (this.dictionary.TryGetValue(k, out item))
            {
                var set = item as ImmutableHashSet<V>;
                if (set == null)
                {
                    v = (V)item;
                    return true;
                }
                else
                {
                    Debug.Assert(set.Count > 1);
                }
            }

            v = default(V);
            return false;
        }

        public bool TryGetMultipleValues(K k, out IEnumerable<V> items)
        {
            object item;
            if (this.dictionary.TryGetValue(k, out item))
            {
                items = item as ImmutableHashSet<V> ?? SpecializedCollections.SingletonEnumerable<V>((V)item);
                return true;
            }

            items = null;
            return false;
        }

        // Returns an empty set if there is no such key in the dictionary.
        public IEnumerable<V> this[K k]
        {
            get
            {
                IEnumerable<V> items;
                if (this.TryGetMultipleValues(k, out items))
                {
                    return items;
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<V>();
                }
            }
        }

        public IEnumerable<K> Keys
        {
            get { return this.dictionary.Keys; }
        }

        public bool ContainsKey(K k)
        {
            return this.dictionary.ContainsKey(k);
        }

        internal void Clear()
        {
            this.dictionary.Clear();
        }
    }
}