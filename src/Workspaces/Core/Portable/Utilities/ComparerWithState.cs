// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal delegate int ComparisonWithState<in T, in S>(T x, T y, S state);

    /// <summary>
    /// Extension of <see cref="Comparer"/> to take into account dynamic <typeparamref name="S"/>
    /// </summary>
    /// <typeparam name="T">Comparing type</typeparam>
    /// <typeparam name="S">State</typeparam>
    internal class ComparerWithState<T, S>
    {
        private readonly ComparisonWithState<T, S> _comparison;

        internal ComparerWithState(ComparisonWithState<T, S> comparison)
        {
            _comparison = comparison;
        }

        public int Compare(T x, T y, S state)
            => _comparison(x, y, state);

        public static ComparerWithState<T, S> Create(Func<T, S, IComparable> comparingMethod)
            => new ComparerWithState<T, S>((t1, t2, state) => comparingMethod(t1, state).CompareTo(comparingMethod(t2, state)));
    }

    internal class ComparerWithState<T> : ComparerWithState<T, object>
    {
        internal ComparerWithState(ComparisonWithState<T, object> comparison) : base(comparison)
        {
        }

        public static ComparerWithState<T> Create(Func<T, IComparable> comparingMethod)
            => new ComparerWithState<T>((t1, t2, state) => comparingMethod(t1).CompareTo(comparingMethod(t2)));
    }

    internal class ComparerWithState
    {
        public static int CompareTo<T, S, C>(T first, T second, S state, ImmutableArray<C> comparingComponents)
            where C : ComparerWithState<T, S>
        {
            foreach (var component in comparingComponents)
            {
                var comparison = component.Compare(first, second, state);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        public static int CompareTo<T>(T first, T second, ImmutableArray<ComparerWithState<T>> comparingComponents)
            => CompareTo<T, object, ComparerWithState<T>>(first, second, default /* ignored parameter */, comparingComponents);

        // Should be called from static methods only.
        public static ImmutableArray<ComparerWithState<T, S>> CreateComparers<T, S>(params Func<T, S, IComparable>[] comparingMethods)
            => comparingMethods.SelectAsArray(method => ComparerWithState<T, S>.Create(method));

        // Should be called from static methods only.
        public static ImmutableArray<ComparerWithState<T>> CreateComparers<T>(params Func<T, IComparable>[] comparingMethods)
            => comparingMethods.SelectAsArray(method => ComparerWithState<T>.Create(method));
    }
}
