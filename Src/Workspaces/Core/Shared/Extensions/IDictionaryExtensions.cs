using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class IDictionaryExtensions
    {
        // Copied from ConcurrentDictionary since IDictionary doesn't have this useful method
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> function)
        {
            V value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = function(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> function)
        {
            return dictionary.GetOrAdd(key, _ => function());
        }
    }
}