using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal static class ValueSourceExtensions
    {
        /// <summary>
        /// Creates a new value source that computes its value from this value source.
        /// </summary>
        internal static IValueSource<TValue2> Compute<TValue1, TValue2>(
            this IValueSource<TValue1> valueSource, Func<TValue1, CancellationToken, TValue2> computation)
        {
            return new ComputedValueSource<TValue1, TValue2>(valueSource, computation);
        }

        /// <summary>
        /// Creates a new value source that computes its value from this value source and an additional input value source.
        /// </summary>
        internal static IValueSource<TValue3> Compute<TValue1, TValue2, TValue3>(
            this IValueSource<TValue1> valueSource1, IValueSource<TValue2> valueSource2,
            Func<TValue1, TValue2, CancellationToken, TValue3> composition)
        {
            return new ComputedValueSource<TValue1, TValue2, TValue3>(valueSource1, valueSource2, composition);
        }

        /// <summary>
        /// Creates a new value source that remembers the value from this value source
        /// so it only gets computed once.
        /// </summary>
        internal static IValueSource<T> RetainStrongly<T>(this IValueSource<T> valueSource) where T : class
        {
            return new RetainedValueSource<T>(valueSource);
        }

        /// <summary>
        /// Creates a new value source that guards access to the value of this value source,
        /// so that only one task is computing the value at a time.
        /// </summary>
        internal static IValueSource<T> Guard<T>(this IValueSource<T> valueSource)
        {
            return new GuardedValueSource<T>(valueSource);
        }

        /// <summary>
        /// Creates a new value source that unwraps a nested value source.
        /// </summary>
        internal static IValueSource<T> Unwrap<T>(this IValueSource<IValueSource<T>> valueSource)
        {
            return new UnwrappedValueSource<T>(valueSource);
        }

        /// <summary>
        /// Construct a weak chain of value-sources. The length of the chain is the number of still referenced value
        /// sources with unevaluated values.
        /// </summary>
        internal static IValueSource<T> Chain<T>(this IValueSource<T> valueSource, IValueSource<T> previousValue)
        {
            chainMap.Add(valueSource, new WeakReference(previousValue));
            return valueSource;
        }

        /// <summary>
        /// Determine the number of still referenced values sources in the chain with unevaluated values.
        /// </summary>
        internal static int GetChainLength<T>(this IValueSource<T> valueSource)
        {
            var vs = valueSource;
            int length = 0;

            while (vs != null)
            {
                WeakReference wref;
                if (chainMap.TryGetValue(vs, out wref))
                {
                    vs = wref.Target as IValueSource<T>;
                    if (vs != null && !vs.HasValue)
                    {
                        length++;
                        continue;
                    }
                }
                else
                {
                    vs = null;
                }
            }

            return length;
        }

        private static readonly ConditionalWeakTable<object, WeakReference> chainMap =
            new ConditionalWeakTable<object, WeakReference>();
    }
}