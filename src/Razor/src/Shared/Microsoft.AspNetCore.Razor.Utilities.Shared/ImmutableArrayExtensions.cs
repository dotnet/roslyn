// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;

namespace System.Collections.Immutable;

/// <summary>
/// <see cref="ImmutableArray{T}"/> extension methods
/// </summary>
internal static partial class ImmutableArrayExtensions
{
    /// <summary>
    /// Returns an empty array if the input array is null (default)
    /// </summary>
    public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array)
    {
        return array.IsDefault ? [] : array;
    }

    public static void SetCapacityIfLarger<T>(this ImmutableArray<T>.Builder builder, int newCapacity)
    {
        if (builder.Capacity < newCapacity)
        {
            builder.Capacity = newCapacity;
        }
    }

    public static void InsertRange<T>(this ImmutableArray<T>.Builder builder, int index, ReadOnlySpan<T> items)
    {
        // ImmutableArray<T>.Builder doesn't currently provide an overload of InsertRange(...) that takes
        // a ReadOnlySpan<T>, so we add our own here.

        ArgHelper.ThrowIfNegative(index);
        ArgHelper.ThrowIfGreaterThan(index, builder.Count);

        if (items.Length == 0)
        {
            // No items? Nothing to do.
            return;
        }

        if (index == builder.Count)
        {
            // If we're inserting at the end of the builder, we can call AddRange(...) which *does* provide
            // an overload that takes a ReadOnlySpan<T>.
            builder.AddRange(items);
        }
        else if (items.Length == 1)
        {
            // If our span contains a single item, we can just insert that item.
            builder.Insert(index, items[0]);
        }
        else
        {
            // As a general strategy, we create an ImmutableArray<T> for the ReadOnlySpan<T> and call
            // the InsertRange(...) overload that takes an ImmutableArray<T>. This should be more efficient than
            // calling the overload that takes an IEnumerable<T>.
            var array = ImmutableArray.Create(items);
            builder.InsertRange(index, array);
        }
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TResult>(this ImmutableArray<T> array, Func<T, TResult> selector)
        => ImmutableCollectionsMarshal.AsImmutableArray(SelectAsPlainArray(array, selector));

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form by incorporating the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="selector">
    ///  A transform function to apply to each element; the second parameter of the function represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TResult>(this ImmutableArray<T> array, Func<T, int, TResult> selector)
        => ImmutableCollectionsMarshal.AsImmutableArray(SelectAsPlainArray(array, selector));

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="arg">An argument to pass to <paramref name="selector"/>.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TArg, TResult>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, TResult> selector)
        => ImmutableCollectionsMarshal.AsImmutableArray(SelectAsPlainArray(array, arg, selector));

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form by incorporating the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="arg">An argument to pass to <paramref name="selector"/>.</param>
    /// <param name="selector">
    ///  A transform function to apply to each element; the third parameter of the function represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static ImmutableArray<TResult> SelectAsArray<T, TArg, TResult>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, int, TResult> selector)
        => ImmutableCollectionsMarshal.AsImmutableArray(SelectAsPlainArray(array, arg, selector));

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new array whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static TResult[] SelectAsPlainArray<T, TResult>(this ImmutableArray<T> array, Func<T, TResult> selector)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        var result = new TResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = selector(array[i]);
        }

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form by incorporating the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="selector">
    ///  A transform function to apply to each element; the second parameter of the function represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new array whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static TResult[] SelectAsPlainArray<T, TResult>(this ImmutableArray<T> array, Func<T, int, TResult> selector)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        var result = new TResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = selector(array[i], i);
        }

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="arg">An argument to pass to <paramref name="selector"/>.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new array whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static TResult[] SelectAsPlainArray<T, TArg, TResult>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, TResult> selector)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        var result = new TResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = selector(array[i], arg);
        }

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form by incorporating the element's index.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="selector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on.</param>
    /// <param name="arg">An argument to pass to <paramref name="selector"/>.</param>
    /// <param name="selector">
    ///  A transform function to apply to each element; the third parameter of the function represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new array whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/>.
    /// </returns>
    public static TResult[] SelectAsPlainArray<T, TArg, TResult>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, int, TResult> selector)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        var result = new TResult[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = selector(array[i], arg, i);
        }

        return result;
    }

    public static ImmutableArray<TResult> SelectManyAsArray<TSource, TResult>(this IReadOnlyCollection<TSource>? source, Func<TSource, ImmutableArray<TResult>> selector)
    {
        if (source is null || source.Count == 0)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<TResult>(capacity: source.Count);
        foreach (var item in source)
        {
            builder.AddRange(selector(item));
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    ///  Filters an <see cref="ImmutableArray{T}"/> based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An <see cref="ImmutableArray{T}"/> to filter.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> that contains elements from the input array
    ///  that satisfy the condition.
    /// </returns>
    /// <remarks>
    ///  This method uses an optimized algorithm that avoids allocating a new array if all elements
    ///  match the predicate (returns the original array) or if no elements match (returns an empty array).
    /// </remarks>
    public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, bool> predicate)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<T>();
        var none = true;
        var all = true;

        for (var i = 0; i < length; i++)
        {
            var item = array[i];

            if (predicate(item))
            {
                none = false;

                if (all)
                {
                    continue;
                }

                Debug.Assert(i > 0);

                builder.Add(item);
            }
            else
            {
                if (none)
                {
                    all = false;
                    continue;
                }

                Debug.Assert(i > 0);

                // We found at least one item that doesn't match the predicate.
                // If this is the first one, we need to copy all the previous items
                // that matched the predicate.
                if (all)
                {
                    all = false;
                    builder.AddRange(array.AsSpan()[..i]);
                }
            }
        }

        Debug.Assert(!(none && all));

        if (all)
        {
            return array;
        }

        if (none)
        {
            return [];
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    ///  Filters an <see cref="ImmutableArray{T}"/> based on a predicate. Each element's index is used in
    ///  the logic of the predicate function.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An <see cref="ImmutableArray{T}"/> to filter.</param>
    /// <param name="predicate">
    ///  A function to test each element for a condition; the second parameter of the function
    ///  represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> that contains elements from the input array
    ///  that satisfy the condition.
    /// </returns>
    /// <remarks>
    ///  This method uses an optimized algorithm that avoids allocating a new array if all elements
    ///  match the predicate (returns the original array) or if no elements match (returns an empty array).
    /// </remarks>
    public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, int, bool> predicate)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<T>();
        var none = true;
        var all = true;

        for (var i = 0; i < length; i++)
        {
            var item = array[i];

            if (predicate(item, i))
            {
                none = false;

                if (all)
                {
                    continue;
                }

                Debug.Assert(i > 0);

                builder.Add(item);
            }
            else
            {
                if (none)
                {
                    all = false;
                    continue;
                }

                Debug.Assert(i > 0);

                // We found at least one item that doesn't match the predicate.
                // If this is the first one, we need to copy all the previous items
                // that matched the predicate.
                if (all)
                {
                    all = false;
                    builder.AddRange(array.AsSpan()[..i]);
                }
            }
        }

        Debug.Assert(!(none && all));

        if (all)
        {
            return array;
        }

        if (none)
        {
            return [];
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    ///  Filters an <see cref="ImmutableArray{T}"/> based on a predicate.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="predicate"/>.</typeparam>
    /// <param name="array">An <see cref="ImmutableArray{T}"/> to filter.</param>
    /// <param name="arg">An argument to pass to <paramref name="predicate"/>.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> that contains elements from the input array
    ///  that satisfy the condition.
    /// </returns>
    /// <remarks>
    ///  This method uses an optimized algorithm that avoids allocating a new array if all elements
    ///  match the predicate (returns the original array) or if no elements match (returns an empty array).
    /// </remarks>
    public static ImmutableArray<T> WhereAsArray<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<T>();
        var none = true;
        var all = true;

        for (var i = 0; i < length; i++)
        {
            var item = array[i];

            if (predicate(item, arg))
            {
                none = false;

                if (all)
                {
                    continue;
                }

                Debug.Assert(i > 0);

                builder.Add(item);
            }
            else
            {
                if (none)
                {
                    all = false;
                    continue;
                }

                Debug.Assert(i > 0);

                // We found at least one item that doesn't match the predicate.
                // If this is the first one, we need to copy all the previous items
                // that matched the predicate.
                if (all)
                {
                    all = false;
                    builder.AddRange(array.AsSpan()[..i]);
                }
            }
        }

        Debug.Assert(!(none && all));

        if (all)
        {
            return array;
        }

        if (none)
        {
            return [];
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    ///  Filters an <see cref="ImmutableArray{T}"/> based on a predicate. Each element's index is used in
    ///  the logic of the predicate function.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="predicate"/>.</typeparam>
    /// <param name="array">An <see cref="ImmutableArray{T}"/> to filter.</param>
    /// <param name="arg">An argument to pass to <paramref name="predicate"/>.</param>
    /// <param name="predicate">
    ///  A function to test each element for a condition; the third parameter of the function
    ///  represents the index of the element.
    /// </param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> that contains elements from the input array
    ///  that satisfy the condition.
    /// </returns>
    /// <remarks>
    ///  This method uses an optimized algorithm that avoids allocating a new array if all elements
    ///  match the predicate (returns the original array) or if no elements match (returns an empty array).
    /// </remarks>
    public static ImmutableArray<T> WhereAsArray<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, int, bool> predicate)
    {
        var length = array.Length;

        if (length == 0)
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<T>();
        var none = true;
        var all = true;

        for (var i = 0; i < length; i++)
        {
            var item = array[i];

            if (predicate(item, arg, i))
            {
                none = false;

                if (all)
                {
                    continue;
                }

                Debug.Assert(i > 0);

                builder.Add(item);
            }
            else
            {
                if (none)
                {
                    all = false;
                    continue;
                }

                Debug.Assert(i > 0);

                // We found at least one item that doesn't match the predicate.
                // If this is the first one, we need to copy all the previous items
                // that matched the predicate.
                if (all)
                {
                    all = false;
                    builder.AddRange(array.AsSpan()[..i]);
                }
            }
        }

        Debug.Assert(!(none && all));

        if (all)
        {
            return array;
        }

        if (none)
        {
            return [];
        }

        return builder.ToImmutableAndClear();
    }

    /// <summary>
    ///  Determines whether any element of an array satisfies a condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> whose elements to apply the predicate to.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the array is not empty and at least one of its elements passes
    ///  the test in the specified predicate; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Any<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array)
        {
            if (predicate(item, arg))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Determines whether all elements of an array satisfy a condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> whose elements to apply the predicate to.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if every element of the array passes the test
    ///  in the specified predicate, or if the array is empty; otherwise,
    ///  <see langword="false"/>.</returns>
    public static bool All<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array)
        {
            if (!predicate(item, arg))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///  Returns the first element in an array that satisfies a specified condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return an element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The first element in the array that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static T First<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return ThrowHelper.ThrowInvalidOperationException<T>(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the first element of the array that satisfies a condition or a default value if no such element is found.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return an element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if <paramref name="array"/> is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the first element in <paramref name="array"/>
    ///  that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public static T? FirstOrDefault<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the last element of an array that satisfies a specified condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return an element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  The last element in the array that passes the test in the specified predicate function.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  No element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static T Last<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array.AsSpan().Reversed)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return ThrowHelper.ThrowInvalidOperationException<T>(SR.Contains_no_matching_elements);
    }

    /// <summary>
    ///  Returns the last element of an array that satisfies a condition or a default value if no such element is found.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return an element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <returns>
    ///  <see langword="default"/>(<typeparamref name="T"/>) if <paramref name="array"/> is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in <paramref name="array"/>
    ///  that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public static T? LastOrDefault<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        foreach (var item in array.AsSpan().Reversed)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    ///  Returns the last element of an array that satisfies a condition, or a specified default value if no such element is found.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return an element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test each element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if the array is empty or no element was found.
    /// </param>
    /// <returns>
    ///  <paramref name="defaultValue"/> if <paramref name="array"/> is empty or if no element
    ///  passes the test specified by <paramref name="predicate"/>; otherwise, the last element in <paramref name="array"/>
    ///  that passes the test specified by <paramref name="predicate"/>.
    /// </returns>
    public static T LastOrDefault<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate, T defaultValue)
    {
        foreach (var item in array.AsSpan().Reversed)
        {
            if (predicate(item, arg))
            {
                return item;
            }
        }

        return defaultValue;
    }

    /// <summary>
    ///  Returns the only element of an array that satisfies a specified condition or a default value
    ///  if no such element exists; this method throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return a single element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <returns>
    ///  The single element of the array that satisfies the condition, or
    ///  <see langword="default"/>(<typeparamref name="T"/>) if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static T? SingleOrDefault<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate)
    {
        var firstSeen = false;
        T? result = default;

        foreach (var item in array)
        {
            if (predicate(item, arg))
            {
                if (firstSeen)
                {
                    return ThrowHelper.ThrowInvalidOperationException<T>(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  Returns the only element of an array that satisfies a specified condition, or a specified default value
    ///  if no such element exists; this method throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <param name="array">
    ///  An <see cref="ImmutableArray{T}"/> to return a single element from.
    /// </param>
    /// <param name="arg">
    ///  An argument to pass to <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///  A function to test an element for a condition.
    /// </param>
    /// <param name="defaultValue">
    ///  The default value to return if the array is empty or no element was found.
    /// </param>
    /// <returns>
    ///  The single element of the array that satisfies the condition, or
    ///  <paramref name="defaultValue"/> if no such element is found.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  More than one element satisfies the condition in <paramref name="predicate"/>.
    /// </exception>
    public static T SingleOrDefault<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, bool> predicate, T defaultValue)
    {
        var firstSeen = false;
        var result = defaultValue;

        foreach (var item in array)
        {
            if (predicate(item, arg))
            {
                if (firstSeen)
                {
                    return ThrowHelper.ThrowInvalidOperationException<T>(SR.Contains_more_than_one_matching_element);
                }

                firstSeen = true;
                result = item;
            }
        }

        return result;
    }

    /// <summary>
    ///  Returns an <see cref="ImmutableArray{T}"/> that contains no duplicates from the <paramref name="source"/> array
    ///  and returns the most recent copy of each item.
    /// </summary>
    /// <param name="source">The array to process.</param>
    public static ImmutableArray<T> GetMostRecentUniqueItems<T>(this ImmutableArray<T> source)
    {
        if (source.IsEmpty)
        {
            return [];
        }

        using var _ = HashSetPool<T>.GetPooledObject(out var uniqueItems);

        return source.GetMostRecentUniqueItems(uniqueItems);
    }

    /// <summary>
    ///  Returns an <see cref="ImmutableArray{T}"/> that contains no duplicates from the <paramref name="source"/> array
    ///  and returns the most recent copy of each item.
    /// </summary>
    /// <param name="source">The array to process.</param>
    /// <param name="comparer">
    ///  A comparer to use for uniqueness.
    /// </param>
    public static ImmutableArray<T> GetMostRecentUniqueItems<T>(this ImmutableArray<T> source, IEqualityComparer<T> comparer)
    {
        if (source.IsEmpty)
        {
            return [];
        }

#if !NETSTANDARD2_0
        var uniqueItems = new HashSet<T>(capacity: source.Length, comparer);
#else
        var uniqueItems = new HashSet<T>(comparer);
#endif

        return source.GetMostRecentUniqueItems(uniqueItems);
    }

    /// <summary>
    ///  Returns an <see cref="ImmutableArray{T}"/> that contains no duplicates from the <paramref name="source"/> array
    ///  and returns the most recent copy of each item.
    /// </summary>
    /// <param name="source">The array to process.</param>
    /// <param name="uniqueItems">
    ///  An empty <see cref="HashSet{T}"/> to use for uniqueness.
    ///  Note that this may still contain items after processing.
    /// </param>
    public static ImmutableArray<T> GetMostRecentUniqueItems<T>(this ImmutableArray<T> source, HashSet<T> uniqueItems)
    {
        if (source.IsEmpty)
        {
            return [];
        }

        return GetMostRecentUniqueItemsCore(source, uniqueItems);
    }

    private static ImmutableArray<T> GetMostRecentUniqueItemsCore<T>(ImmutableArray<T> source, HashSet<T> uniqueItems)
    {
        Debug.Assert(uniqueItems.Count == 0, $"{nameof(uniqueItems)} should be empty!");

        using var stack = new PooledArrayBuilder<T>(capacity: source.Length);

        // Walk the next batch in reverse to identify unique items.
        // We push them on a stack so that we can pop them in order later
        for (var i = source.Length - 1; i >= 0; i--)
        {
            var item = source[i];

            if (uniqueItems.Add(item))
            {
                stack.Push(item);
            }
        }

        // Did we actually dedupe anything? If not, just return the original.
        if (stack.Count == source.Length)
        {
            return source;
        }

        using var result = new PooledArrayBuilder<T>(capacity: stack.Count);

        while (stack.Count > 0)
        {
            result.Add(stack.Pop());
        }

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Executes a binary search over an array, but allows the caller to decide what constitutes a match
    /// </summary>
    /// <typeparam name="T">Type of the elements in the array</typeparam>
    /// <typeparam name="TArg">Type of the argument to pass to the comparer</typeparam>
    /// <param name="array">The array to search</param>
    /// <param name="arg">An argument to pass to the comparison function</param>
    /// <param name="comparer">A comparison function that evaluates an item in the array. Return 0 if the item is a match,
    /// or -1 if the item indicates a successful match will be found in the left branch, or 1 if the item indicates a successful
    /// match will be found in the right branch.</param>
    /// <returns>The index of the element found</returns>
    public static int BinarySearchBy<T, TArg>(this ImmutableArray<T> array, TArg arg, Func<T, TArg, int> comparer)
    {
        var min = 0;
        var max = array.Length - 1;

        while (min <= max)
        {
            var mid = (min + max) / 2;
            var comparison = comparer(array[mid], arg);
            if (comparison == 0)
            {
                return mid;
            }

            if (comparison < 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid - 1;
            }
        }

        return ~min;
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this ImmutableArray<T> array)
    {
        var sortHelper = new SortHelper<T>(comparer: null, descending: false);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this ImmutableArray<T> array, IComparer<T> comparer)
    {
        var sortHelper = new SortHelper<T>(comparer, descending: false);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> OrderAsArray<T>(this ImmutableArray<T> array, Comparison<T> comparison)
    {
        var sortHelper = new SortHelper<T>(comparison, descending: false);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this ImmutableArray<T> array)
    {
        var sortHelper = new SortHelper<T>(comparer: null, descending: true);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this ImmutableArray<T> array, IComparer<T> comparer)
    {
        var sortHelper = new SortHelper<T>(comparer, descending: true);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> OrderDescendingAsArray<T>(this ImmutableArray<T> array, Comparison<T> comparison)
    {
        var sortHelper = new SortHelper<T>(comparison, descending: true);
        return array.OrderAsArrayCore(in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector)
    {
        var sortHelper = new SortHelper<TKey>(comparer: null, descending: false);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var sortHelper = new SortHelper<TKey>(comparer, descending: false);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var sortHelper = new SortHelper<TKey>(comparison, descending: false);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector)
    {
        var sortHelper = new SortHelper<TKey>(comparer: null, descending: true);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var sortHelper = new SortHelper<TKey>(comparer, descending: true);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    /// <summary>
    ///  Sorts the elements of an <see cref="ImmutableArray{T}"/> in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="array">An array to be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> OrderByDescendingAsArray<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var sortHelper = new SortHelper<TKey>(comparison, descending: true);
        return array.OrderByAsArrayCore(keySelector, in sortHelper);
    }

    private static ImmutableArray<T> OrderAsArrayCore<T>(this ImmutableArray<T> array, ref readonly SortHelper<T> sortHelper)
        => array.OrderByAsArrayCore(SortHelper<T>.IdentityFunc, in sortHelper);

    private static ImmutableArray<TElement> OrderByAsArrayCore<TElement, TKey>(
        this ImmutableArray<TElement> array, Func<TElement, TKey> keySelector, ref readonly SortHelper<TKey> sortHelper)
    {
        if (array.Length <= 1)
        {
            return array;
        }

        var items = array.AsSpan();
        var length = items.Length;

        using var keys = SortKey<TKey>.GetPooledArray(minimumLength: length);

        if (sortHelper.ComputeKeys(items, keySelector, keys.Span))
        {
            // The keys are already ordered, so we don't need to create a new array and sort it.
            return array;
        }

        var newArray = new TElement[length];
        items.CopyTo(newArray);

        Array.Sort(keys.Array, newArray, 0, length, sortHelper.GetOrCreateComparer());

        return ImmutableCollectionsMarshal.AsImmutableArray(newArray);
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> and clears the collection.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be cleared.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/>.
    /// </returns>
    /// <remarks>
    /// This method is preferred over calling DrainToImmutable as it allows reuse of the
    /// backing array in the common case where the builder isn't fully utilizing it's capacity.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableAndClear<T>(this ImmutableArray<T>.Builder builder)
    {
        ImmutableArray<T> result;
        if (builder.Count != builder.Capacity)
        {
            result = builder.ToImmutable();
            builder.Clear();
        }
        else
        {
            result = builder.DrainToImmutable();
        }

        return result;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrdered<T>(this ImmutableArray<T>.Builder builder)
    {
        var array = builder.ToImmutable();
        array.Unsafe().Order();
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrdered<T>(this ImmutableArray<T>.Builder builder, IComparer<T> comparer)
    {
        var array = builder.ToImmutable();
        array.Unsafe().Order(comparer);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrdered<T>(this ImmutableArray<T>.Builder builder, Comparison<T> comparison)
    {
        var array = builder.ToImmutable();
        array.Unsafe().Order(comparison);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrderedDescending<T>(this ImmutableArray<T>.Builder builder)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderDescending();
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrderedDescending<T>(this ImmutableArray<T>.Builder builder, IComparer<T> comparer)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderDescending(comparer);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order.
    /// </returns>
    public static ImmutableArray<T> ToImmutableOrderedDescending<T>(this ImmutableArray<T>.Builder builder, Comparison<T> comparison)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderDescending(comparison);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedBy<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderBy(keySelector);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedBy<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderBy(keySelector, comparer);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedBy<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderBy(keySelector, comparison);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescending<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderByDescending(keySelector);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescending<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderByDescending(keySelector, comparer);
        return array;
    }

    /// <summary>
    ///  Returns an immutable array that contains the current contents of this
    ///  <see cref="ImmutableArray{T}.Builder"/> sorted in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescending<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var array = builder.ToImmutable();
        array.Unsafe().OrderByDescending(keySelector, comparison);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedAndClear<T>(this ImmutableArray<T>.Builder builder)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().Order();
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedAndClear<T>(this ImmutableArray<T>.Builder builder, IComparer<T> comparer)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().Order(comparer);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedAndClear<T>(this ImmutableArray<T>.Builder builder, Comparison<T> comparison)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().Order(comparison);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedDescendingAndClear<T>(this ImmutableArray<T>.Builder builder)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderDescending();
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedDescendingAndClear<T>(this ImmutableArray<T>.Builder builder, IComparer<T> comparer)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderDescending(comparer);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order and sets the collection to a zero length array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<T> ToImmutableOrderedDescendingAndClear<T>(this ImmutableArray<T>.Builder builder, Comparison<T> comparison)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderDescending(comparison);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderBy(keySelector);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderBy(keySelector, comparer);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in ascending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in ascending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderBy(keySelector, comparison);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescendingAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderByDescending(keySelector);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescendingAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, IComparer<TKey> comparer)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderByDescending(keySelector, comparer);
        return array;
    }

    /// <summary>
    ///  Returns the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  as an immutable array sorted in descending order according to a key and sets
    ///  the collection to a zero length array.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="builder"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> whose contents will be sorted.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparison">An <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  An immutable array that contains the current contents of this <see cref="ImmutableArray{T}.Builder"/>
    ///  sorted in descending order according to a key.
    /// </returns>
    /// <remarks>
    ///  If <see cref="ImmutableArray{T}.Builder.Capacity">Capacity</see> equals
    ///  <see cref="ImmutableArray{T}.Builder.Count">Count</see>, the internal array will be extracted as an
    ///  <see cref="ImmutableArray{T}"/> without copying the contents. Otherwise, the contents will be copied
    ///  into a new array. The collection will then be set to a zero length array.
    /// </remarks>
    public static ImmutableArray<TElement> ToImmutableOrderedByDescendingAndClear<TElement, TKey>(
        this ImmutableArray<TElement>.Builder builder, Func<TElement, TKey> keySelector, Comparison<TKey> comparison)
    {
        var array = builder.ToImmutableAndClear();
        array.Unsafe().OrderByDescending(keySelector, comparison);
        return array;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(this ImmutableArray<T> array, Func<T, TResult> selector)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().Order();

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare projected elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(
        this ImmutableArray<T> array, Func<T, TResult> selector, IComparer<TResult> comparer)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().Order(comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderAsArray<T, TResult>(
        this ImmutableArray<T> array, Func<T, TResult> selector, Comparison<TResult> comparison)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().Order(comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(this ImmutableArray<T> array, Func<T, TResult> selector)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderDescending();

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(
        this ImmutableArray<T> array, Func<T, TResult> selector, IComparer<TResult> comparer)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderDescending(comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare elements.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderDescendingAsArray<T, TResult>(
        this ImmutableArray<T> array, Func<T, TResult> selector, Comparison<TResult> comparison)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderDescending(comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector, comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in ascending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in ascending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderBy(keySelector, comparison);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, IComparer<TKey> comparer)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector, comparer);

        return result;
    }

    /// <summary>
    ///  Projects each element of an <see cref="ImmutableArray{T}"/> into a new form and sorts them in descending order according to a key.
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in <paramref name="array"/>.</typeparam>
    /// <typeparam name="TKey">The type of key returned by <paramref name="keySelector"/>.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector"/>.</typeparam>
    /// <param name="array">An array of values to invoke a transform function on and sort.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <param name="keySelector">A function to extract a key from a projected element.</param>
    /// <param name="comparison">A <see cref="Comparison{T}"/> to compare keys.</param>
    /// <returns>
    ///  Returns a new <see cref="ImmutableArray{T}"/> whose elements are the result of invoking the transform function
    ///  on each element of <paramref name="array"/> and sorted in descending order according to a key.
    /// </returns>
    public static ImmutableArray<TResult> SelectAndOrderByDescendingAsArray<TElement, TKey, TResult>(
        this ImmutableArray<TElement> array, Func<TElement, TResult> selector, Func<TResult, TKey> keySelector, Comparison<TKey> comparison)
    {
        var result = array.SelectAsArray(selector);
        result.Unsafe().OrderByDescending(keySelector, comparison);

        return result;
    }
}
