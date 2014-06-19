using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    public static class ImmutableListExtensions
    {
        public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> items)
        {
            if (items == null || !items.Any())
            {
                return ImmutableList<T>.Empty;
            }

            var im = items as ImmutableList<T>;
            if (im != null)
            {
                return im;
            }

            return ImmutableList<T>.Empty.InsertAt(0, items);
        }
    }
}