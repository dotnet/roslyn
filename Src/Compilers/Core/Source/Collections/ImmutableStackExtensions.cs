using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    public static class ImmutableStackExtensions
    {
        public static ImmutableStack<T> ToImmutableStack<T>(this IEnumerable<T> items)
        {
            var stack = items as ImmutableStack<T>;
            if (stack != null)
            {
                return stack;
            }

            return ImmutableStack<T>.Empty.Push(items);
        }
    }
}