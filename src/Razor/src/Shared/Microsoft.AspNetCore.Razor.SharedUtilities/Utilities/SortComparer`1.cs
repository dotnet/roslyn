// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal abstract class SortComparer<T> : IComparer<SortKey<T>>
{
    public static IComparer<SortKey<T>> GetOrCreate(IComparer<T> comparer, bool descending)
    {
        if (ReferenceEquals(comparer, Comparer<T>.Default))
        {
            return descending
                ? DescendingComparer.Default
                : AscendingComparer.Default;
        }

        return descending
            ? new DescendingComparer(comparer)
            : new AscendingComparer(comparer);
    }

    public static IComparer<SortKey<T>> Create(Comparison<T> comparison, bool descending)
        => descending
            ? new DescendingComparison(comparison)
            : new AscendingComparison(comparison);

    private SortComparer()
    {
    }

    protected abstract int CompareValues(T x, T y);

    public int Compare(SortKey<T> x, SortKey<T> y)
        => CompareValues(x.Value, y.Value) switch
        {
            // If the values are equal, use their indices to ensure stability.
            0 => x.Index - y.Index,
            var result => result
        };

    private sealed class AscendingComparer(IComparer<T> comparer) : SortComparer<T>
    {
        private static SortComparer<T>? s_default;

        public static SortComparer<T> Default
            => s_default ?? InterlockedOperations.Initialize(ref s_default, new AscendingComparer(Comparer<T>.Default));

        protected override int CompareValues(T x, T y)
            => comparer.Compare(x, y);
    }

    private sealed class DescendingComparer(IComparer<T> comparer) : SortComparer<T>
    {
        private static SortComparer<T>? s_default;

        public static SortComparer<T> Default
            => s_default ?? InterlockedOperations.Initialize(ref s_default, new DescendingComparer(Comparer<T>.Default));

        protected override int CompareValues(T x, T y)
            => comparer.Compare(y, x);
    }

    private sealed class AscendingComparison(Comparison<T> comparison) : SortComparer<T>
    {
        protected override int CompareValues(T x, T y)
            => comparison(x, y);
    }

    private sealed class DescendingComparison(Comparison<T> comparison) : SortComparer<T>
    {
        protected override int CompareValues(T x, T y)
            => comparison(y, x);
    }
}
