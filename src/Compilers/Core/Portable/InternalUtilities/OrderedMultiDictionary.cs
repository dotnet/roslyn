// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    // Note that this is not threadsafe for concurrent reading and writing.
    internal sealed class OrderedMultiDictionary<K, V> : IEnumerable<KeyValuePair<K, SetWithInsertionOrder<V>>>
        where K : notnull
    {
        private readonly Dictionary<K, SetWithInsertionOrder<V>> _dictionary;
        private readonly List<K> _keys;

        public int Count => _dictionary.Count;

        public IEnumerable<K> Keys => _keys;

        // Returns an empty set if there is no such key in the dictionary.
        public SetWithInsertionOrder<V> this[K k]
        {
            get
            {
                SetWithInsertionOrder<V>? set;
                return _dictionary.TryGetValue(k, out set)
                    ? set : new SetWithInsertionOrder<V>();
            }
        }

        public OrderedMultiDictionary()
        {
            _dictionary = new Dictionary<K, SetWithInsertionOrder<V>>();
            _keys = new List<K>();
        }

        public void Add(K k, V v)
        {
            SetWithInsertionOrder<V>? set;
            if (!_dictionary.TryGetValue(k, out set))
            {
                _keys.Add(k);
                set = new SetWithInsertionOrder<V>();
            }
            set.Add(v);
            _dictionary[k] = set;
        }

        public void AddRange(K k, IEnumerable<V> values)
        {
            foreach (var v in values)
            {
                Add(k, v);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<K, SetWithInsertionOrder<V>>> GetEnumerator()
        {
            foreach (var key in _keys)
            {
                yield return new KeyValuePair<K, SetWithInsertionOrder<V>>(
                    key, _dictionary[key]);
            }
        }
    }
}
