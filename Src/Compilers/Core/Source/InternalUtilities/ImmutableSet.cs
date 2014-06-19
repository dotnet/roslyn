using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace System.Collections.Immutable
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class ImmutableHashSet
    {
        public static ImmutableHashSet<T> Create<T>()
        {
            return ImmutableHashSet<T>.PrivateEmpty_DONOTUSE();
        }

        public static ImmutableHashSet<T> Create<T>(T item)
        {
            return ImmutableHashSet<T>.PrivateEmpty_DONOTUSE().Add(item);
        }

        public static ImmutableHashSet<T> Create<T>(params T[] items)
        {
            return From(items);
        }

        /// <summary>
        /// The normal mechanism for building up an ImmutableSet is to start with Empty and call Add
        /// repeatedly.  However, if you already know that you're going to add all the elements of
        /// an existing enumerable (a common pattern), then do everything in one step.
        /// </summary>
        /// <remarks>
        /// As a bonus, we can skip creation of a bunch of temporary set objects and ensure that Empty
        /// is the only zero-element instance.
        /// </remarks>
        public static ImmutableHashSet<T> From<T>(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            ImmutableDictionary<T, object> map = ImmutableDictionary.Create<T, object>(values.Select(v => new KeyValuePair<T, object>(v, null)));

            return map.Count > 0
                ? ImmutableHashSet<T>.PrivateCtor_DONOTUSE(map)
                : ImmutableHashSet<T>.PrivateEmpty_DONOTUSE();
        }
    }
}