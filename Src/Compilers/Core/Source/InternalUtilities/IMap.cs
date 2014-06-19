using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal interface IMap<K, V> : IEnumerable<KeyValuePair<K, V>>, IEquatable<IMap<K, V>>
    {
        int Count { get; }
        IEqualityComparer<K> KeyEqualityComparer { get; }
        IEqualityComparer<V> ValueEqualityComparer { get; }

        IEnumerable<K> Keys { get; }
        IEnumerable<V> Values { get; }

        IMap<K, V> Add(K key, V value);
        IMap<K, V> Add(IEnumerable<KeyValuePair<K, V>> pairs);

        bool ContainsKey(K key);

        V this[K key] { get; }
        bool TryGetValue(K key, out V value);

        IMap<K, V> Remove(K key);
        IMap<K, V> Remove(IEnumerable<K> keys);
        IMap<K, V> Remove(IMap<K, V> map);
    }
}