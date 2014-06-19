using System.Collections.Generic;

namespace System.Collections.Immutable
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class ImmutableList
    {
        public static ImmutableList<T> Create<T>()
        {
            return ImmutableList<T>.PrivateEmpty_DONOTUSE();
        }

        public static ImmutableList<T> Create<T>(T item)
        {
            return ImmutableList<T>.PrivateEmpty_DONOTUSE().Add(item);
        }

        public static ImmutableList<T> Create<T>(params T[] items)
        {
            return Create<T>().AddRange(items);
        }

        public static ImmutableList<T> From<T>(IEnumerable<T> items)
        {
            return Create<T>().AddRange(items);
        }

        public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> items)
        {
            var immList = items as ImmutableList<T>;
            if (immList != null)
            {
                return immList;
            }

            return ImmutableList.From<T>(items);
        }
    }
}