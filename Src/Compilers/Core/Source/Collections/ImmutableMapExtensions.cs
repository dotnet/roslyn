using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    public static class ImmutableMapExtensions
    {
        public static ImmutableMap<K, V> ToImmutableMap<T, K, V>(this IEnumerable<T> items, Func<T, K> keySelector, Func<T, V> valueSelector)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }

            if (valueSelector == null)
            {
                throw new ArgumentNullException("valueSelector");
            }

            return new ImmutableMap<K, V>(items.Select(i => new KeyValuePair<K, V>(keySelector(i), valueSelector(i))));
        }

        public static ImmutableMap<K, V> ToImmutableMap<K, V>(this IEnumerable<V> items, Func<V, K> keySelector)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }

            return new ImmutableMap<K, V>(items.Select(v => new KeyValuePair<K, V>(keySelector(v), v)));
        }
    }
}