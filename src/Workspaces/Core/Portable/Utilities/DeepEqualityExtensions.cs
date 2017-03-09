using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class DeepEqualityExtensions
    {
        public static IEqualityComparer<ImmutableArray<T>> DeepEqualityComparer<T>(this ImmutableArray<T> array)
        {
            return ImmutableArrayDeepEqualityComparer<T>.Instance;
        }

        public static int GetDeepHashCode<T>(this ImmutableArray<T> array)
        {
            return array.DeepEqualityComparer().GetHashCode(array);
        }

        public static bool DeepEquals<T>(this ImmutableArray<T> array, ImmutableArray<T> other)
        {
            return array.DeepEqualityComparer().Equals(array, other);
        }

        private class ImmutableArrayDeepEqualityComparer<T> : IEqualityComparer<ImmutableArray<T>>
        {
            public static readonly ImmutableArrayDeepEqualityComparer<T> Instance = new ImmutableArrayDeepEqualityComparer<T>();

            public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
            {
                if (x.IsDefault && y.IsDefault)
                {
                    return true;
                }

                if (x.IsDefault || y.IsDefault)
                {
                    return false;
                }

                if (x.Length != y.Length)
                {
                    return false;
                }

                var comparer = EqualityComparer<T>.Default;

                for (int i = 0; i < x.Length; i++)
                {
                    if (!comparer.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ImmutableArray<T> array)
            {
                int hc = 0;

                var comparer = EqualityComparer<T>.Default;
                for (int i = 0; i < array.Length; i++)
                {
                    unchecked
                    {
                        hc += comparer.GetHashCode(array[i]);
                    }
                }

                return hc;
            }
        }

        public static IEqualityComparer<ImmutableDictionary<K, V>> DeepEqualityComparer<K, V>(this ImmutableDictionary<K, V> dictionary)
        {
            return ImmutableDictionaryDeepEqualityComparer<K, V>.Instance;
        }

        public static int GetDeepHashCode<K, V>(this ImmutableDictionary<K, V> dictionary)
        {
            return dictionary.DeepEqualityComparer().GetHashCode(dictionary);
        }

        public static bool DeepEquals<K, V>(this ImmutableDictionary<K, V> dictionary, ImmutableDictionary<K, V> other)
        {
            return dictionary.DeepEqualityComparer().Equals(dictionary, other);
        }

        private class ImmutableDictionaryDeepEqualityComparer<K, V> : IEqualityComparer<ImmutableDictionary<K, V>>
        {
            public static readonly ImmutableDictionaryDeepEqualityComparer<K, V> Instance = new ImmutableDictionaryDeepEqualityComparer<K, V>();

            public bool Equals(ImmutableDictionary<K, V> x, ImmutableDictionary<K, V> y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x.Count != y.Count)
                {
                    return false;
                }

                var vComparer = x.ValueComparer ?? EqualityComparer<V>.Default;

                foreach (var kvp in x)
                {
                    V yValue;
                    if (!(y.TryGetValue(kvp.Key, out yValue) && vComparer.Equals(yValue, kvp.Value)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(ImmutableDictionary<K, V> obj)
            {
                int hc = 0;

                var kComparer = obj.KeyComparer ?? EqualityComparer<K>.Default;
                var vComparer = obj.ValueComparer ?? EqualityComparer<V>.Default;

                foreach (var kvp in obj)
                {
                    unchecked
                    {
                        hc += kComparer.GetHashCode(kvp.Key) + vComparer.GetHashCode(kvp.Value);
                    }
                }

                return hc;
            }
        }
    }
}
