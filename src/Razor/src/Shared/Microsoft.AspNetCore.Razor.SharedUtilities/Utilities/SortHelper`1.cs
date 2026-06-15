// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

/// <summary>
///  Helper that avoids creating an <see cref="IComparer{T}"/> until its needed.
/// </summary>
internal readonly ref struct SortHelper<T>
{
    public static readonly Func<T, T> IdentityFunc = x => x;

    private readonly IComparer<T> _comparer;
    private readonly Comparison<T>? _comparison;
    private readonly bool _descending;

    public SortHelper(IComparer<T>? comparer, bool descending)
    {
        _comparer = comparer ?? Comparer<T>.Default;
        _comparison = null;
        _descending = descending;
    }

    public SortHelper(Comparison<T> comparison, bool descending)
    {
        _comparer = null!; // This value will never be used when _comparison is non-null.
        _comparison = comparison;
        _descending = descending;
    }

    public IComparer<SortKey<T>> GetOrCreateComparer()
        => _comparison is null
            ? SortComparer<T>.GetOrCreate(_comparer, _descending)
            : SortComparer<T>.Create(_comparison, _descending);

    /// <summary>
    ///  Determines whether <paramref name="value"/> is greater than <paramref name="previousValue"/>
    ///  using the provided <see cref="IComparer{T}"/> or <see cref="Comparison{T}"/>.
    /// </summary>
    /// <remarks>
    ///  We assume that value and previousValue are in sorted order if value is > previousValue.
    ///  We don't consider value == previousValue to be sorted because the actual sort might
    ///  not be stable, depending on T.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AreInSortedOrder(T? value, T? previousValue)
        => (_comparison, _descending) switch
        {
            (null, true) => _comparer.Compare(previousValue!, value!) > 0,
            (null, false) => _comparer.Compare(value!, previousValue!) > 0,
            (not null, true) => _comparison(previousValue!, value!) > 0,
            (not null, false) => _comparison(value!, previousValue!) > 0,
        };

    /// <summary>
    ///  Walk through <paramref name="items"/> and convert each element to a key using <paramref name="keySelector"/>.
    ///  While walking, each computed key is compared with the previous one using the provided <see cref="SortHelper{T}"/>
    ///  to determine whether they are already ordered.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if the keys are in order; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  When the keys are already ordered, there's no need to perform a sort.
    /// </remarks>
    public bool ComputeKeys<TElement>(ReadOnlySpan<TElement> items, Func<TElement, T> keySelector, Span<SortKey<T>> keys)
    {
        // Is this our identity func? If so, we can call a faster path that casts T -> TKey.
        if (ReferenceEquals(IdentityFunc, keySelector))
        {
            return ComputeIdentityKeys(items, keys);
        }

        var isOutOfOrder = false;

        var previousKey = keySelector(items[0]);
        keys[0] = new(Index: 0, previousKey);

        for (var i = 1; i < items.Length; i++)
        {
            var currentKey = keySelector(items[i]);
            keys[i] = new(Index: i, currentKey);

            if (!isOutOfOrder)
            {
                if (!AreInSortedOrder(currentKey, previousKey))
                {
                    // Continue processing to finish converting elements to keys. However, we can stop comparing keys.
                    isOutOfOrder = true;
                }

                previousKey = currentKey;
            }
        }

        return !isOutOfOrder;
    }

    private bool ComputeIdentityKeys<TElement>(ReadOnlySpan<TElement> items, Span<SortKey<T>> keys)
    {
        Debug.Assert(typeof(TElement) == typeof(T));

        var isOutOfOrder = false;

        var previousKey = (T)(object)items[0]!;
        keys[0] = new(Index: 0, previousKey);

        for (var i = 1; i < items.Length; i++)
        {
            var currentKey = (T)(object)items[i]!;
            keys[i] = new(Index: i, currentKey);

            if (!isOutOfOrder)
            {
                if (!AreInSortedOrder(currentKey, previousKey))
                {
                    // Continue processing to finish converting elements to keys. However, we can stop comparing keys.
                    isOutOfOrder = true;
                }

                previousKey = currentKey;
            }
        }

        return !isOutOfOrder;
    }

    /// <summary>
    ///  Walk through <paramref name="items"/> and convert each element to a key using <paramref name="keySelector"/>.
    ///  While walking, each computed key is compared with the previous one using the provided <see cref="SortHelper{T}"/>
    ///  to determine whether they are already ordered.
    /// </summary>
    /// <returns>
    ///  <see langword="true"/> if the keys are in order; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  When the keys are already ordered, there's no need to perform a sort.
    /// </remarks>
    public bool ComputeKeys<TElement>(IReadOnlyList<TElement> items, Func<TElement, T> keySelector, Span<SortKey<T>> keys)
    {
        // Is this our identity func? If so, we can call a faster path that casts T -> TKey.
        if (ReferenceEquals(IdentityFunc, keySelector))
        {
            return ComputeIdentityKeys(items, keys);
        }

        var isOutOfOrder = false;
        var count = items.Count;

        var previousKey = keySelector(items[0]);
        keys[0] = new(Index: 0, previousKey);

        for (var i = 1; i < count; i++)
        {
            var currentKey = keySelector(items[i]);
            keys[i] = new(Index: i, currentKey);

            if (!isOutOfOrder)
            {
                if (!AreInSortedOrder(currentKey, previousKey))
                {
                    // Continue processing to finish converting elements to keys. However, we can stop comparing keys.
                    isOutOfOrder = true;
                }

                previousKey = currentKey;
            }
        }

        return !isOutOfOrder;
    }

    private bool ComputeIdentityKeys<TElement>(IReadOnlyList<TElement> items, Span<SortKey<T>> keys)
    {
        Debug.Assert(typeof(TElement) == typeof(T));

        var isOutOfOrder = false;
        var count = items.Count;

        var previousKey = (T)(object)items[0]!;
        keys[0] = new(Index: 0, previousKey);

        for (var i = 1; i < count; i++)
        {
            var currentKey = (T)(object)items[i]!;
            keys[i] = new(Index: i, currentKey);

            if (!isOutOfOrder)
            {
                if (!AreInSortedOrder(currentKey, previousKey))
                {
                    // Continue processing to finish converting elements to keys. However, we can stop comparing keys.
                    isOutOfOrder = true;
                }

                previousKey = currentKey;
            }
        }

        return !isOutOfOrder;
    }
}
