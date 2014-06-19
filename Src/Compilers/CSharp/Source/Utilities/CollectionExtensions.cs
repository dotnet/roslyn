using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Roslyn.Compilers.Collections;

namespace Roslyn.Compilers.CSharp
{
    internal static class CollectionExtensions
    {
        internal static bool IsEmpty<T>(this IEnumerable<T> sequence)
        {
            return !sequence.Any();
        }

        internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> sequence)
        {
            return sequence.SelectMany(v => v);
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
            foreach(var e in data)
            {
                enumerators.Add(e.GetEnumerator());
                width += 1;
            }

            try
            {
                for (; ; )
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
                for (int i = 0; i < width; i++)
                {
                    enumerators[i].Dispose();
                }
            }
        }


        internal static void AddAllValues<K, T>(this IDictionary<K, ReadOnlyArray<T>> data, ArrayBuilder<T> builder)
        {
            foreach (var values in data.Values)
            {
                builder.AddRange(values);
            }
        }

        [Obsolete("This method is currently unused. Should you need to use it, remove attributes and add a test.", true)]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        internal static Dictionary<K, ReadOnlyArray<T>> ToDictionary<K, T>(this IEnumerable<T> data, Func<T, K> keySelector, IEqualityComparer<K> comparer)
        {
            var dictionary = new Dictionary<K, ReadOnlyArray<T>>(comparer);
            var groups = data.GroupBy(keySelector, comparer);
            foreach (var grouping in groups)
            {
                var items = grouping.AsReadOnly();
                dictionary.Add(grouping.Key, items);
            }

            return dictionary;
        }

        internal static Dictionary<K, ReadOnlyArray<T>> ToDictionary<K, T>(this IEnumerable<T> data, Func<T, K> keySelector)
        {
            var dictionary = new Dictionary<K, ReadOnlyArray<T>>();
            var groups = data.GroupBy(keySelector);
            foreach (var grouping in groups)
            {
                var items = grouping.AsReadOnly();
                dictionary.Add(grouping.Key, items);
            }

            return dictionary;
        }
    }
}
