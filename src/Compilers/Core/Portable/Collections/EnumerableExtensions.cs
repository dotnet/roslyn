// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal static class EnumerableExtensions
    {
        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>> items)
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>();
            }

            return ImmutableDictionary.CreateRange(items);
        }

        public static ImmutableDictionary<K, V> ToImmutableDictionaryOrEmpty<K, V>(this IEnumerable<KeyValuePair<K, V>> items, IEqualityComparer<K> keyComparer)
        {
            if (items == null)
            {
                return ImmutableDictionary.Create<K, V>(keyComparer);
            }

            return ImmutableDictionary.CreateRange(keyComparer, items);
        }

        internal static IList<IList<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> data)
        {
#if DEBUG
            var count = data.First().Count();
            Debug.Assert(data.All(d => d.Count() == count));
#endif
            return TransposeInternal(data).ToArray();
        }

        private static IEnumerable<IList<T>> TransposeInternal<T>(this IEnumerable<IEnumerable<T>> data)
        {
            List<IEnumerator<T>> enumerators = new List<IEnumerator<T>>();

            var width = 0;
            foreach (var e in data)
            {
                enumerators.Add(e.GetEnumerator());
                width += 1;
            }

            try
            {
                while (true)
                {
                    T[] line = null;
                    for (int i = 0; i < width; i++)
                    {
                        var e = enumerators[i];
                        if (!e.MoveNext())
                        {
                            yield break;
                        }

                        if (line == null)
                        {
                            line = new T[width];
                        }

                        line[i] = e.Current;
                    }

                    yield return line;
                }
            }
            finally
            {
                foreach (var enumerator in enumerators)
                {
                    enumerator.Dispose();
                }
            }
        }

        internal static void AddAllValues<K, T>(this IDictionary<K, ImmutableArray<T>> data, ArrayBuilder<T> builder)
        {
            foreach (var values in data.Values)
            {
                builder.AddRange(values);
            }
        }

        internal static Dictionary<K, ImmutableArray<T>> ToDictionary<K, T>(this IEnumerable<T> data, Func<T, K> keySelector, IEqualityComparer<K> comparer = null)
        {
            var dictionary = new Dictionary<K, ImmutableArray<T>>(comparer);
            var groups = data.GroupBy(keySelector, comparer);
            foreach (var grouping in groups)
            {
                var items = grouping.AsImmutable();
                dictionary.Add(grouping.Key, items);
            }

            return dictionary;
        }

        /// <summary>
        /// Returns the only element of specified sequence if it has exactly one, and default(TSource) otherwise.
        /// Unlike <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})"/> doesn't throw if there is more than one element in the sequence.
        /// </summary>
        internal static TSource AsSingleton<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                return default(TSource);
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                return (list.Count == 1) ? list[0] : default(TSource);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return default(TSource);
                }

                TSource result = e.Current;
                if (e.MoveNext())
                {
                    return default(TSource);
                }

                return result;
            }
        }
    }
}
