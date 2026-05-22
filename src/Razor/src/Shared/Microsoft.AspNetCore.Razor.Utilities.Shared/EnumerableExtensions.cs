// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace System.Collections.Generic;

internal static class EnumerableExtensions
{
    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="source"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
    {
        // If the source is an ImmutableArray<T> boxed as an IEnumerable<T>, it's better to unbox it here and
        // call the SelectAsArray<T> extension method that takes an ImmutableArray<T>. Otherwise, it'll go
        // through the IReadOnlyList<T> path.
        if (source is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.SelectAsArray(array, selector);
        }

        // If the source is an IReadOnlyList<T>, we should call the SelectAsArray<T> extension method that
        // takes an IReadOnlyList<T>. This ensures that we don't foreach over it and incur the cost of allocating
        // or boxing an enumerator.
        if (source is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.SelectAsArray(list, selector);
        }

        // PERF: If we can get the count of the sequence, we can allocate the array up front.
        if (source.TryGetCount(out var count))
        {
            if (count == 0)
            {
                return [];
            }

            var result = new TResult[count];

            var index = 0;
            foreach (var item in source)
            {
                result[index++] = selector(item);
            }

            Debug.Assert(result.Length == count);

            return ImmutableCollectionsMarshal.AsImmutableArray(result);
        }

        // Fall back to a PooledArrayBuilder if we can't get the count up front.
        // If the enumerable has 4 or fewer items, this will still allocate a single array and fill it.
        // However, if it has more than 4 items, it will acquire an ImmutableArray<TResult>.Builder from the default pool.
        using var results = new PooledArrayBuilder<TResult>();

        foreach (var item in source)
        {
            results.Add(selector(item));
        }

        // If the PooledArrayBuilder acquired an ImmutableArray<TResult>.Builder, using ToImmutableAndClear()
        // avoid's allocating a new array and copying the results into it if the builder's capacity *happens*
        // to be the same as the number of items. This is uncommon, but still useful.
        return results.ToImmutableAndClear();
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form by incorporating the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="source"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="source">A sequence of values to invoke a transform function on.</param>
    /// <param name="selector">
    ///  A transform function to apply to each source element; the second parameter of
    ///  the function represents the index of the source element.
    /// </param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="source"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TResult>(this IEnumerable<T> source, Func<T, int, TResult> selector)
    {
        // If the source is an ImmutableArray<T> boxed as an IEnumerable<T>, it's better to unbox it here and
        // call the SelectAsArray<T> extension method that takes an ImmutableArray<T>. Otherwise, it'll go
        // through the IReadOnlyList<T> path.
        if (source is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.SelectAsArray(array, selector);
        }

        // If the source is an IReadOnlyList<T>, we should call the SelectAsArray<T> extension method that
        // takes an IReadOnlyList<T>. This ensures that we don't foreach over it and incur the cost of allocating
        // or boxing an enumerator.
        if (source is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.SelectAsArray(list, selector);
        }

        var index = 0;

        // PERF: If we can get the count of the sequence, we can allocate the array up front.
        if (source.TryGetCount(out var count))
        {
            if (count == 0)
            {
                return [];
            }

            var result = new TResult[count];

            foreach (var item in source)
            {
                result[index] = selector(item, index);
                index++;
            }

            Debug.Assert(result.Length == count);

            return ImmutableCollectionsMarshal.AsImmutableArray(result);
        }

        // Fall back to a PooledArrayBuilder if we can't get the count up front.
        // If the enumerable has 4 or fewer items, this will still allocate a single array and fill it.
        // However, if it has more than 4 items, it will acquire an ImmutableArray<TResult>.Builder from the default pool.
        using var results = new PooledArrayBuilder<TResult>();

        foreach (var item in source)
        {
            results.Add(selector(item, index++));
        }

        // If the PooledArrayBuilder acquired an ImmutableArray<TResult>.Builder, using ToImmutableAndClear()
        // avoid's allocating a new array and copying the results into it if the builder's capacity *happens*
        // to be the same as the number of items. This is uncommon, but still useful.
        return results.ToImmutableAndClear();
    }

    public static bool TryGetCount<T>(this IEnumerable<T> sequence, out int count)
    {
#if NET6_0_OR_GREATER
        // Note: TryGetNonEnumeratedCount doesn't test for IReadOnlyCollection<T>.
        // So, it returns false for IReadOnlyList<T>.
        if (sequence is IReadOnlyCollection<T> collection)
        {
            count = collection.Count;
            return true;
        }

        return Linq.Enumerable.TryGetNonEnumeratedCount(sequence, out count);
#else
        return TryGetCount<T>((IEnumerable)sequence, out count);
#endif
    }

    public static bool TryGetCount<T>(this IEnumerable sequence, out int count)
    {
        switch (sequence)
        {
            case ICollection collection:
                count = collection.Count;
                return true;

            case ICollection<T> collection:
                count = collection.Count;
                return true;

            case IReadOnlyCollection<T> collection:
                count = collection.Count;
                return true;
        }

        count = 0;
        return false;
    }

    /// <summary>
    ///  Copies the contents of the sequence to a destination <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="sequence">The sequence to copy items from.</param>
    /// <param name="destination">The span to copy items into.</param>
    /// <exception cref="ArgumentException">
    ///  The destination span is shorter than the source sequence.
    /// </exception>
    public static void CopyTo<T>(this IEnumerable<T> sequence, Span<T> destination)
    {
        // First, check a few common cases.
        switch (sequence)
        {
            case ImmutableArray<T> array:
                ArgHelper.ThrowIfDestinationTooShort(destination, array.Length);
                array.CopyTo(destination);
                break;

            // HashSet<T> has special enumerator and doesn't implement IReadOnlyList<T>
            case HashSet<T> set:
                set.CopyTo(destination);
                break;

            case IReadOnlyList<T> list:
                list.CopyTo(destination);
                break;

            default:
                CopySequence(sequence, destination);
                break;
        }

        static void CopySequence(IEnumerable<T> sequence, Span<T> destination)
        {
            if (sequence.TryGetCount(out var count))
            {
                ArgHelper.ThrowIfDestinationTooShort(destination, count);

                var index = 0;

                foreach (var item in sequence)
                {
                    destination[index++] = item;
                }
            }
            else
            {
                var index = 0;

                foreach (var item in sequence)
                {
                    ArgHelper.ThrowIfDestinationTooShort(destination, index + 1);

                    destination[index++] = item;
                }
            }
        }
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this IEnumerable<T> sequence)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderAsArray(array);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderAsArray(list);
        }

        var sortHelper = new SortHelper<T>(comparer: null, descending: false);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderAsArray(array, comparer);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderAsArray(list, comparer);
        }

        var sortHelper = new SortHelper<T>(comparer, descending: false);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this IEnumerable<T> sequence, Comparison<T> comparison)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderAsArray(array, comparison);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderAsArray(list, comparison);
        }

        var sortHelper = new SortHelper<T>(comparison, descending: false);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this IEnumerable<T> sequence)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderDescendingAsArray(array);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderDescendingAsArray(list);
        }

        var sortHelper = new SortHelper<T>(comparer: null, descending: true);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this IEnumerable<T> sequence, IComparer<T> comparer)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderDescendingAsArray(array, comparer);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderDescendingAsArray(list, comparer);
        }

        var sortHelper = new SortHelper<T>(comparer, descending: true);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this IEnumerable<T> sequence, Comparison<T> comparison)
    {
        if (sequence is ImmutableArray<T> array)
        {
            return ImmutableArrayExtensions.OrderDescendingAsArray(array, comparison);
        }

        if (sequence is IReadOnlyList<T> list)
        {
            return ReadOnlyListExtensions.OrderDescendingAsArray(list, comparison);
        }

        var sortHelper = new SortHelper<T>(comparison, descending: true);
        return sequence.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByAsArray(array, keySelector);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByAsArray(list, keySelector);
        }

        var sortHelper = new SortHelper<TKey>(comparer: null, descending: false);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByAsArray(array, keySelector, comparer);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByAsArray(list, keySelector, comparer);
        }

        var sortHelper = new SortHelper<TKey>(comparer, descending: false);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByAsArray(array, keySelector, comparison);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByAsArray(list, keySelector, comparison);
        }

        var sortHelper = new SortHelper<TKey>(comparison, descending: false);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByDescendingAsArray(array, keySelector);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByDescendingAsArray(list, keySelector);
        }

        var sortHelper = new SortHelper<TKey>(comparer: null, descending: true);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByDescendingAsArray(array, keySelector, comparer);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByDescendingAsArray(list, keySelector, comparer);
        }

        var sortHelper = new SortHelper<TKey>(comparer, descending: true);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="IEnumerable{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> whose elements will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        if (sequence is ImmutableArray<TElement> array)
        {
            return ImmutableArrayExtensions.OrderByDescendingAsArray(array, keySelector, comparison);
        }

        if (sequence is IReadOnlyList<TElement> list)
        {
            return ReadOnlyListExtensions.OrderByDescendingAsArray(list, keySelector, comparison);
        }

        var sortHelper = new SortHelper<TKey>(comparison, descending: true);
        return sequence.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    private static ImmutableArray<T> OrderAsArrayCore<T>(this IEnumerable<T> sequence, ref readonly SortHelper<T> sortHelper)
        => sequence.OrderByAsArrayCore(SortHelper<T>.IdentityFunc, in sortHelper);

    private static ImmutableArray<TElement> OrderByAsArrayCore<TElement, TKey>(
        this IEnumerable<TElement> sequence, Func<TElement, TKey> keySelector, ref readonly SortHelper<TKey> sortHelper)
    {
        var newArray = BuildArray(sequence);

        if (newArray.Length <= 1)
        {
            return ImmutableCollectionsMarshal.AsImmutableArray(newArray);
        }

        var length = newArray.Length;
        using var keys = SortKey<TKey>.GetPooledArray(minimumLength: length);

        if (!sortHelper.ComputeKeys(newArray.AsSpan(), keySelector, keys.Span))
        {
            // The keys are not ordered, so we need to sort the array.
            Array.Sort(keys.Array, newArray, 0, length, sortHelper.GetOrCreateComparer());
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(newArray);
    }

    private static T[] BuildArray<T>(IEnumerable<T> sequence)
    {
        if (!sequence.TryGetCount(out var count))
        {
            return BuildSlow(sequence);
        }

        if (count == 0)
        {
            return [];
        }

        var result = new T[count];
        sequence.CopyTo(result);

        return result;

        static T[] BuildSlow(IEnumerable<T> sequence)
        {
            using var builder = new PooledArrayBuilder<T>();

            foreach (var item in sequence)
            {
                builder.Add(item);
            }

            return builder.ToArray();
        }
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(this IEnumerable<T> sequence, Func<T, TResult> selector)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().Order();

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(
        this IEnumerable<T> sequence, Func<T, TResult> selector, IComparer<TResult> comparer)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().Order(comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(
        this IEnumerable<T> sequence, Func<T, TResult> selector, Comparison<TResult> comparison)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().Order(comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(this IEnumerable<T> sequence, Func<T, TResult> selector)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderDescending();

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(
        this IEnumerable<T> sequence, Func<T, TResult> selector, IComparer<TResult> comparer)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderDescending(comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(
        this IEnumerable<T> sequence, Func<T, TResult> selector, Comparison<TResult> comparison)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderDescending(comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector, comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector, comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in decending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in decending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector, comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="IEnumerable{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="sequence"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="sequence">An <see cref="IEnumerable{T}"/> of elements to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="sequence"/> and sorted in decending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this IEnumerable<TElement> sequence, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = sequence.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector, comparison);

        return result;
    }
}
