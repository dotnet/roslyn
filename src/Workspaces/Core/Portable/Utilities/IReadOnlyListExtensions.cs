using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal static class IReadOnlyListExtensions
    {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IList<T> list)
        {
            if (list is IReadOnlyList<T> readOnlyList)
            {
                return readOnlyList;
            }

            return new ReadOnlyList<T>(list);
        }

        public static T Last<T>(this IReadOnlyList<T> list)
        {
            return list[list.Count - 1];
        }

        public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static T TrySingleOrDefault<T>(this IReadOnlyList<T> list)
            => list.Count == 1 ? list[0] : default;

        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private readonly IList<T> _list;

            public ReadOnlyList(IList<T> list)
            {
                _list = list;
            }

            public T this[int index] => _list[index];
            public int Count => _list.Count;
            public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        }
    }
}
