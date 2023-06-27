// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => list[list.Count - 1];

        public static int IndexOf<T>(this IReadOnlyList<T> list, T value, int startIndex = 0)
        {
            for (var index = startIndex; index < list.Count; index++)
            {
                if (EqualityComparer<T>.Default.Equals(list[index], value))
                {
                    return index;
                }
            }

            return -1;
        }

        private class ReadOnlyList<T>(IList<T> list) : IReadOnlyList<T>
        {
            public T this[int index] => list[index];
            public int Count => list.Count;
            public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
        }
    }
}
