using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal static class ImmutableDictionary
    {
        public static ImmutableDictionary<K, V> Create<K, V>()
        {
            return ImmutableDictionary<K, V>.PrivateEmpty_DONOTUSE();
        }

        public static ImmutableDictionary<K, V> Create<K, V>(IEnumerable<KeyValuePair<K, V>> items)
        {
            return Create<K, V>().AddRange(items);
        }

        public static ImmutableDictionary<K, V> Create<K, V>(IEqualityComparer<K> keyComparer)
        {
            return ImmutableDictionary<K, V>.PrivateConstructor_DONOTUSE(keyComparer);
        }

        public static ImmutableDictionary<K, V> Create<K, V>(IEqualityComparer<K> keyComparer, IEnumerable<KeyValuePair<K, V>> items)
        {
            return Create<K, V>(keyComparer).AddRange(items);
        }
    }
}