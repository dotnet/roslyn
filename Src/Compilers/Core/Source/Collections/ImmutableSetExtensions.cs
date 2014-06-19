using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Roslyn.Compilers
{
    internal static class ImmutableSetExtensions
    {
        internal static ImmutableSet<T> ToImmutableSet<T>(this IEnumerable<T> items)
        {
            ImmutableSet<T> set = items as ImmutableSet<T>;
            if (set != null)
            {
                return set;
            }

            return new ImmutableSet<T>(items);
        }
    }
}