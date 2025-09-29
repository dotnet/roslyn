// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The collection of extension methods for the <see cref="ImmutableArray{T}"/> type
    /// </summary>
    internal static class ImmutableArrayExtensions
    {
        /// <summary>
        /// Converts a sequence to an immutable array.
        /// </summary>
        /// <typeparam name="T">Elemental type of the sequence.</typeparam>
        /// <param name="items">The sequence to convert.</param>
        /// <returns>An immutable copy of the contents of the sequence.</returns>
        /// <exception cref="ArgumentNullException">If items is null (default)</exception>
        /// <remarks>If the sequence is null, this will throw <see cref="ArgumentNullException"/></remarks>
        public static ImmutableArray<T> AsImmutable<T>(this IEnumerable<T> items)
        {
            return ImmutableArray.CreateRange<T>(items);
        }

        /// <summary>
        /// Converts a sequence to an immutable array.
        /// </summary>
        /// <typeparam name="T">Elemental type of the sequence.</typeparam>
        /// <param name="items">The sequence to convert.</param>
        /// <returns>An immutable copy of the contents of the sequence.</returns>
        /// <remarks>If the sequence is null, this will return an empty array.</remarks>
        public static ImmutableArray<T> AsImmutableOrEmpty<T>(this IEnumerable<T>? items)
        {
            if (items == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        /// <summary>
        /// Converts a sequence to an immutable array.
        /// </summary>
        /// <typeparam name="T">Elemental type of the sequence.</typeparam>
        /// <param name="items">The sequence to convert.</param>
        /// <returns>An immutable copy of the contents of the sequence.</returns>
        /// <remarks>If the sequence is null, this will return the default (null) array.</remarks>
        public static ImmutableArray<T> AsImmutableOrNull<T>(this IEnumerable<T>? items)
        {
            if (items == null)
            {
                return default;
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        /// <summary>
        /// Converts an array to an immutable array. The array must not be null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The sequence to convert</param>
        /// <returns></returns>
        public static ImmutableArray<T> AsImmutable<T>(this T[] items)
        {
            Debug.Assert(items != null);
            return ImmutableArray.Create<T>(items);
        }

        /// <summary>
        /// Converts a array to an immutable array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The sequence to convert</param>
        /// <returns></returns>
        /// <remarks>If the sequence is null, this will return the default (null) array.</remarks>
        public static ImmutableArray<T> AsImmutableOrNull<T>(this T[]? items)
        {
            if (items == null)
            {
                return default;
            }

            return ImmutableArray.Create<T>(items);
        }

        /// <summary>
        /// Converts an array to an immutable array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The sequence to convert</param>
        /// <returns>If the array is null, this will return an empty immutable array.</returns>
        public static ImmutableArray<T> AsImmutableOrEmpty<T>(this T[]? items)
        {
            if (items == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return ImmutableArray.Create<T>(items);
        }

        /// <summary>
        /// Reads bytes from specified <see cref="MemoryStream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Read-only content of the stream.</returns>
        public static ImmutableArray<byte> ToImmutable(this MemoryStream stream)
        {
            return ImmutableArray.Create<byte>(stream.ToArray());
        }

        /// <summary>
        /// Maps an immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items">The array to map</param>
        /// <param name="map">The mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ImmutableArray<TItem> items, Func<TItem, TResult> map)
        {
            return ImmutableArray.CreateRange(items, map);
        }

        /// <summary>
        /// Maps an immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items">The sequence to map</param>
        /// <param name="map">The mapping delegate</param>
        /// <param name="arg">The extra input used by mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items, Func<TItem, TArg, TResult> map, TArg arg)
        {
            return ImmutableArray.CreateRange(items, map, arg);
        }

        /// <summary>
        ///  Maps an immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="items">The sequence to map</param>
        /// <param name="map">The mapping delegate</param>
        /// <param name="arg">The extra input used by mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> items, Func<TItem, int, TArg, TResult> map, TArg arg)
        {
            switch (items.Length)
            {
                case 0: return [];
                case 1: return [map(items[0], 0, arg)];
                case 2: return [map(items[0], 0, arg), map(items[1], 1, arg)];
                case 3: return [map(items[0], 0, arg), map(items[1], 1, arg), map(items[2], 2, arg)];
                case 4: return [map(items[0], 0, arg), map(items[1], 1, arg), map(items[2], 2, arg), map(items[3], 3, arg)];

                default:
                    var builder = new FixedSizeArrayBuilder<TResult>(items.Length);
                    for (int i = 0; i < items.Length; i++)
                    {
                        builder.Add(map(items[i], i, arg));
                    }

                    return builder.MoveToImmutable();
            }
        }

        /// <summary>
        /// Maps a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, bool> predicate, Func<TItem, TResult> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item))
                    builder.Add(selector(item));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <typeparam name="TArg">Type of the extra argument</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <param name="arg">The extra input used by <paramref name="predicate"/> and <paramref name="selector"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> array, Func<TItem, TArg, bool> predicate, Func<TItem, TArg, TResult> selector, TArg arg)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item, arg))
                    builder.Add(selector(item, arg));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>If the array's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, IEnumerable<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
                builder.AddRange(selector(item));

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>If the array's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, ImmutableArray<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
                builder.AddRange(selector(item));

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, OneOrMany<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
                selector(item).AddRangeTo(builder);

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, bool> predicate, Func<TItem, IEnumerable<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item))
                    builder.AddRange(selector(item));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, bool> predicate, Func<TItem, ImmutableArray<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item))
                    builder.AddRange(selector(item));
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, bool> predicate, Func<TItem, OneOrMany<TResult>> selector)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item))
                    selector(item).AddRangeTo(builder);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps and flattens a subset of immutable array to another immutable array.
        /// </summary>
        /// <typeparam name="TItem">Type of the source array items</typeparam>
        /// <typeparam name="TArg">Type of the argument to pass to the predicate and selector</typeparam>
        /// <typeparam name="TResult">Type of the transformed array items</typeparam>
        /// <param name="array">The array to transform</param>
        /// <param name="predicate">The condition to use for filtering the array content.</param>
        /// <param name="selector">A transform function to apply to each element that is not filtered out by <paramref name="predicate"/>.</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> SelectManyAsArray<TItem, TArg, TResult>(this ImmutableArray<TItem> array, Func<TItem, TArg, bool> predicate, Func<TItem, TArg, OneOrMany<TResult>> selector, TArg arg)
        {
            if (array.Length == 0)
                return [];

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item, arg))
                    selector(item, arg).AddRangeTo(builder);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTasks, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, CancellationToken, ValueTask<TResult>> selector, CancellationToken cancellationToken)
        {
            if (array.IsEmpty)
                return [];

            var builder = new FixedSizeArrayBuilder<TResult>(array.Length);
            foreach (var item in array)
                builder.Add(await selector(item, cancellationToken).ConfigureAwait(false));

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTasks, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TArg, TResult>(this ImmutableArray<TItem> array, Func<TItem, TArg, CancellationToken, ValueTask<TResult>> selector, TArg arg, CancellationToken cancellationToken)
        {
            if (array.IsEmpty)
                return [];

            var builder = new FixedSizeArrayBuilder<TResult>(array.Length);
            foreach (var item in array)
                builder.Add(await selector(item, arg, cancellationToken).ConfigureAwait(false));

            return builder.MoveToImmutable();
        }

        public static ValueTask<ImmutableArray<TResult>> SelectManyAsArrayAsync<TItem, TArg, TResult>(this ImmutableArray<TItem> source, Func<TItem, TArg, CancellationToken, ValueTask<ImmutableArray<TResult>>> selector, TArg arg, CancellationToken cancellationToken)
        {
            if (source.Length == 0)
                return new ValueTask<ImmutableArray<TResult>>([]);

            if (source.Length == 1)
                return selector(source[0], arg, cancellationToken);

            return CreateTaskAsync();

            async ValueTask<ImmutableArray<TResult>> CreateTaskAsync()
            {
                var builder = ArrayBuilder<TResult>.GetInstance();

                foreach (var item in source)
                {
                    builder.AddRange(await selector(item, arg, cancellationToken).ConfigureAwait(false));
                }

                return builder.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Zips two immutable arrays together through a mapping function, producing another immutable array.
        /// </summary>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public static ImmutableArray<TResult> ZipAsArray<T1, T2, TResult>(this ImmutableArray<T1> self, ImmutableArray<T2> other, Func<T1, T2, TResult> map)
        {
            Debug.Assert(self.Length == other.Length);
            switch (self.Length)
            {
                case 0: return [];
                case 1: return [map(self[0], other[0])];
                case 2: return [map(self[0], other[0]), map(self[1], other[1])];
                case 3: return [map(self[0], other[0]), map(self[1], other[1]), map(self[2], other[2])];
                case 4: return [map(self[0], other[0]), map(self[1], other[1]), map(self[2], other[2]), map(self[3], other[3])];

                default:
                    var builder = new TResult[self.Length];
                    for (var i = 0; i < self.Length; i++)
                        builder[i] = map(self[i], other[i]);

                    return ImmutableCollectionsMarshal.AsImmutableArray(builder);
            }
        }

        public static ImmutableArray<TResult> ZipAsArray<T1, T2, TArg, TResult>(this ImmutableArray<T1> self, ImmutableArray<T2> other, TArg arg, Func<T1, T2, int, TArg, TResult> map)
        {
            Debug.Assert(self.Length == other.Length);
            if (self.IsEmpty)
                return [];

            var builder = new FixedSizeArrayBuilder<TResult>(self.Length);
            for (int i = 0; i < self.Length; i++)
            {
                builder.Add(map(self[i], other[i], i, arg));
            }

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Creates a new immutable array based on filtered elements by the predicate. The array must not be null.
        /// </summary>
        /// <param name="array">The array to process</param>
        /// <param name="predicate">The delegate that defines the conditions of the element to search for.</param>
        public static ImmutableArray<T> WhereAsArray<T>(this ImmutableArray<T> array, Func<T, bool> predicate)
            => WhereAsArrayImpl<T, object?>(array, predicate, predicateWithArg: null, arg: null);

        /// <summary>
        /// Creates a new immutable array based on filtered elements by the predicate. The array must not be null.
        /// </summary>
        /// <param name="array">The array to process</param>
        /// <param name="predicate">The delegate that defines the conditions of the element to search for.</param>
        public static ImmutableArray<T> WhereAsArray<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
            => WhereAsArrayImpl(array, predicateWithoutArg: null, predicate, arg);

        private static ImmutableArray<T> WhereAsArrayImpl<T, TArg>(ImmutableArray<T> array, Func<T, bool>? predicateWithoutArg, Func<T, TArg, bool>? predicateWithArg, TArg arg)
        {
            Debug.Assert(!array.IsDefault);
            Debug.Assert(predicateWithArg != null ^ predicateWithoutArg != null);

            ArrayBuilder<T>? builder = null;
            bool none = true;
            bool all = true;

            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if ((predicateWithoutArg != null) ? predicateWithoutArg(a) : predicateWithArg!(a, arg))
                {
                    none = false;
                    if (all)
                    {
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (builder == null)
                    {
                        builder = ArrayBuilder<T>.GetInstance();
                    }

                    builder.Add(a);
                }
                else
                {
                    if (none)
                    {
                        all = false;
                        continue;
                    }

                    Debug.Assert(i > 0);
                    if (all)
                    {
                        Debug.Assert(builder == null);
                        all = false;
                        builder = ArrayBuilder<T>.GetInstance();
                        for (int j = 0; j < i; j++)
                        {
                            builder.Add(array[j]);
                        }
                    }
                }
            }

            if (builder != null)
            {
                Debug.Assert(!all);
                Debug.Assert(!none);
                return builder.ToImmutableAndFree();
            }
            else if (all)
            {
                return array;
            }
            else
            {
                Debug.Assert(none);
                return ImmutableArray<T>.Empty;
            }
        }

        public static async Task<bool> AnyAsync<T>(this ImmutableArray<T> array, Func<T, Task<bool>> predicateAsync)
        {
            foreach (var item in array)
            {
                if (await predicateAsync(item).ConfigureAwait(false))
                    return true;
            }

            return false;
        }

        public static async Task<bool> AnyAsync<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, Task<bool>> predicateAsync, TArg arg)
        {
            foreach (var item in array)
            {
                if (await predicateAsync(item, arg).ConfigureAwait(false))
                    return true;
            }

            return false;
        }

        public static async ValueTask<T?> FirstOrDefaultAsync<T>(this ImmutableArray<T> array, Func<T, Task<bool>> predicateAsync)
        {
            foreach (var item in array)
            {
                if (await predicateAsync(item).ConfigureAwait(false))
                    return item;
            }

            return default;
        }

        /// <summary>
        /// Casts the immutable array of a Type to an immutable array of its base type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ImmutableArray<TBase> Cast<TDerived, TBase>(this ImmutableArray<TDerived> items)
            where TDerived : class, TBase
        {
            return ImmutableArray<TBase>.CastUp(items);
        }

        /// <summary>
        /// Determines whether this instance and another immutable array are equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <param name="comparer">The comparer to determine if the two arrays are equal.</param>
        /// <returns>True if the two arrays are equal</returns>
        public static bool SetEquals<T>(this ImmutableArray<T> array1, ImmutableArray<T> array2, IEqualityComparer<T> comparer)
        {
            if (array1.IsDefault)
            {
                return array2.IsDefault;
            }
            else if (array2.IsDefault)
            {
                return false;
            }

            var count1 = array1.Length;
            var count2 = array2.Length;

            // avoid constructing HashSets in these common cases
            if (count1 == 0)
            {
                return count2 == 0;
            }
            else if (count2 == 0)
            {
                return false;
            }
            else if (count1 == 1 && count2 == 1)
            {
                var item1 = array1[0];
                var item2 = array2[0];

                return comparer.Equals(item1, item2);
            }

            var set1 = new HashSet<T>(array1, comparer);
            var set2 = new HashSet<T>(array2, comparer);

            // internally recognizes that set2 is a HashSet with the same comparer (http://msdn.microsoft.com/en-us/library/bb346516.aspx)
            return set1.SetEquals(set2);
        }

        /// <summary>
        /// Returns an empty array if the input array is null (default)
        /// </summary>
        public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array)
        {
            return array.IsDefault ? ImmutableArray<T>.Empty : array;
        }

        /// <summary>
        /// Returns an empty array if the input nullable value type is null or the underlying array is null (default)
        /// </summary>
        public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T>? array)
            => array switch
            {
                null or { IsDefault: true } => ImmutableArray<T>.Empty,
                { } underlying => underlying
            };

        // In DEBUG, swap the first and last elements of a read-only array, yielding a new read only array.
        // This helps to avoid depending on accidentally sorted arrays.
        internal static ImmutableArray<T> ConditionallyDeOrder<T>(this ImmutableArray<T> array)
        {
#if DEBUG
            if (!array.IsDefault && array.Length >= 2)
            {
                T[] copy = array.ToArray();
                int last = copy.Length - 1;
                var temp = copy[0];
                copy[0] = copy[last];
                copy[last] = temp;

                return ImmutableCollectionsMarshal.AsImmutableArray(copy);
            }
#endif
            return array;
        }

        internal static ImmutableArray<TValue> Flatten<TKey, TValue>(
            this Dictionary<TKey, ImmutableArray<TValue>> dictionary,
            IComparer<TValue>? comparer = null)
            where TKey : notnull
        {
            if (dictionary.Count == 0)
                return [];

            var builder = ArrayBuilder<TValue>.GetInstance();

            foreach (var kvp in dictionary)
            {
                builder.AddRange(kvp.Value);
            }

            if (comparer != null && builder.Count > 1)
            {
                // PERF: Beware ImmutableArray<T>.Builder.Sort allocates a Comparer wrapper object
                builder.Sort(comparer);
            }

            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<T> AddRange<T>(this ImmutableArray<T> self, in TemporaryArray<T> items)
        {
            if (items.Count == 0)
            {
                return self;
            }

            if (items.Count == 1)
            {
                return self.Add(items[0]);
            }

            var builder = new T[self.Length + items.Count];
            var index = 0;

            foreach (var item in self)
            {
                builder[index++] = item;
            }

            foreach (var item in items)
            {
                builder[index++] = item;
            }

            return ImmutableCollectionsMarshal.AsImmutableArray(builder);
        }

        internal static bool HasDuplicates<T>(this ImmutableArray<T> array)
            => array.HasDuplicates(EqualityComparer<T>.Default);

        internal static bool HasDuplicates<T>(this ImmutableArray<T> array, IEqualityComparer<T> comparer)
            => array.HasDuplicates(static x => x, comparer);

        public static bool HasDuplicates<TItem, TValue>(this ImmutableArray<TItem> array, Func<TItem, TValue> selector)
            => array.HasDuplicates(selector, EqualityComparer<TValue>.Default);

        /// <summary>
        /// Determines whether duplicates exist using given equality comparer.
        /// </summary>
        /// <param name="array">Array to search for duplicates</param>
        /// <returns>Whether duplicates were found</returns>
        /// <remarks>
        /// API proposal: https://github.com/dotnet/runtime/issues/30582.
        /// <seealso cref="Roslyn.Utilities.EnumerableExtensions.HasDuplicates{TItem, TValue}(IEnumerable{TItem}, Func{TItem, TValue}, IEqualityComparer{TValue})"/>
        /// </remarks>
        internal static bool HasDuplicates<TItem, TValue>(this ImmutableArray<TItem> array, Func<TItem, TValue> selector, IEqualityComparer<TValue> comparer)
        {
            switch (array.Length)
            {
                case 0:
                case 1:
                    return false;

                case 2:
                    return comparer.Equals(selector(array[0]), selector(array[1]));

                default:
                    var set = comparer == EqualityComparer<TValue>.Default ? PooledHashSet<TValue>.GetInstance() : new HashSet<TValue>(comparer);
                    var result = false;

                    foreach (var element in array)
                    {
                        if (!set.Add(selector(element)))
                        {
                            result = true;
                            break;
                        }
                    }

                    (set as PooledHashSet<TValue>)?.Free();
                    return result;
            }
        }

        internal static void AddToMultiValueDictionaryBuilder<K, T>(Dictionary<K, object> accumulator, K key, T item)
            where K : notnull
            where T : notnull
        {
            if (accumulator.TryGetValue(key, out var existingValueOrArray))
            {
                if (existingValueOrArray is ArrayBuilder<T> arrayBuilder)
                {
                    // Already a builder in the accumulator, just add to that.
                }
                else
                {
                    // Just a single value in the accumulator so far.  Convert to using a builder.
                    arrayBuilder = ArrayBuilder<T>.GetInstance(capacity: 2);
                    arrayBuilder.Add((T)existingValueOrArray);
                    accumulator[key] = arrayBuilder;
                }

                arrayBuilder.Add(item);
            }
            else
            {
                // Nothing in the dictionary so far.  Add the item directly.
                accumulator.Add(key, item);
            }
        }

        internal static void CreateNameToMembersMap<TKey, TNamespaceOrTypeSymbol, TNamedTypeSymbol, TNamespaceSymbol>
            (Dictionary<TKey, object> dictionary, Dictionary<TKey, ImmutableArray<TNamespaceOrTypeSymbol>> result)
            where TKey : notnull
            where TNamespaceOrTypeSymbol : class
            where TNamedTypeSymbol : class, TNamespaceOrTypeSymbol
            where TNamespaceSymbol : class, TNamespaceOrTypeSymbol
        {
            foreach (var entry in dictionary)
                result.Add(entry.Key, createMembers(entry.Value));

            return;

            static ImmutableArray<TNamespaceOrTypeSymbol> createMembers(object value)
            {
                if (value is ArrayBuilder<TNamespaceOrTypeSymbol> builder)
                {
                    Debug.Assert(builder.Count > 1);
                    foreach (var item in builder)
                    {
                        if (item is TNamespaceSymbol)
                            return builder.ToImmutableAndFree();
                    }

                    return ImmutableArray<TNamespaceOrTypeSymbol>.CastUp(builder.ToDowncastedImmutableAndFree<TNamedTypeSymbol>());
                }
                else
                {
                    TNamespaceOrTypeSymbol symbol = (TNamespaceOrTypeSymbol)value;
                    return symbol is TNamespaceSymbol
                        ? ImmutableArray.Create(symbol)
                        : ImmutableArray<TNamespaceOrTypeSymbol>.CastUp(ImmutableArray.Create((TNamedTypeSymbol)symbol));
                }
            }
        }

        internal static Dictionary<TKey, ImmutableArray<TNamedTypeSymbol>> GetTypesFromMemberMap<TKey, TNamespaceOrTypeSymbol, TNamedTypeSymbol>
            (Dictionary<TKey, ImmutableArray<TNamespaceOrTypeSymbol>> map, IEqualityComparer<TKey> comparer)
            where TKey : notnull
            where TNamespaceOrTypeSymbol : class
            where TNamedTypeSymbol : class, TNamespaceOrTypeSymbol
        {
            // Initialize dictionary capacity to avoid resize allocations during Add calls.
            // Most iterations through the loop add an entry. If map is smaller than the
            // smallest capacity dictionary will use, we'll let it grow organically as
            // it's possible we might not add anything to the dictionary.
            var capacity = map.Count > 3 ? map.Count : 0;

            var dictionary = new Dictionary<TKey, ImmutableArray<TNamedTypeSymbol>>(capacity, comparer);

            foreach (var entry in map)
            {
                var namedTypes = getOrCreateNamedTypes(entry.Value);
                if (namedTypes.Length > 0)
                    dictionary.Add(entry.Key, namedTypes);
            }

            return dictionary;

            static ImmutableArray<TNamedTypeSymbol> getOrCreateNamedTypes(ImmutableArray<TNamespaceOrTypeSymbol> members)
            {
                Debug.Assert(members.Length > 0);

                // See if creator 'map' put a downcasted ImmutableArray<TNamedTypeSymbol> in it.  If so, we can just directly
                // downcast to that and trivially reuse it.  If not, that means the array must have contained at least one
                // TNamespaceSymbol and we'll need to filter that out.
                var membersAsNamedTypes = members.As<TNamedTypeSymbol>();

                if (!membersAsNamedTypes.IsDefault)
                    return membersAsNamedTypes;

                // Preallocate the right amount so we can avoid garbage reallocs.
                var count = members.Count(static s => s is TNamedTypeSymbol);

                // Must have less items than in the original array.  Otherwise, the .As<TNamedTypeSymbol>() cast would
                // have succeeded.
                Debug.Assert(count < members.Length);

                if (count == 0)
                    return ImmutableArray<TNamedTypeSymbol>.Empty;

                var builder = ArrayBuilder<TNamedTypeSymbol>.GetInstance(count);
                foreach (var member in members)
                {
                    if (member is TNamedTypeSymbol namedType)
                        builder.Add(namedType);
                }

                Debug.Assert(builder.Count == count);
                return builder.ToImmutableAndFree();
            }
        }

        internal static int IndexOf<T>(this ImmutableArray<T> array, T item, IEqualityComparer<T> comparer)
            => array.IndexOf(item, startIndex: 0, comparer);

        internal static bool IsSorted<T>(this ImmutableArray<T> array, Comparison<T> comparison)
            => IsSorted(array, Comparer<T>.Create(comparison));

        internal static bool IsSorted<T>(this ImmutableArray<T> array, IComparer<T>? comparer = null)
        {
            comparer ??= Comparer<T>.Default;

            for (var i = 1; i < array.Length; i++)
            {
                if (comparer.Compare(array[i - 1], array[i]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSubsetOf<TElement>(this ImmutableArray<TElement> array, ImmutableArray<TElement> other)
        {
            if (other.Length == 0)
            {
                return array.Length == 0;
            }

            switch (array.Length)
            {
                case 0:
                    return true;
                case 1:
                    return other.Contains(array[0]);
                case 2:
                    return other.Contains(array[0]) && other.Contains(array[1]);
                case 3:
                    return other.Contains(array[0]) && other.Contains(array[1]) && other.Contains(array[2]);
            }

            var set = PooledHashSet<TElement>.GetInstance();
            foreach (var item in other)
            {
                set.Add(item);
            }

            foreach (var item in array)
            {
                if (!set.Contains(item))
                {
                    set.Free();
                    return false;
                }
            }

            set.Free();
            return true;
        }
    }
}

namespace System.Linq
{
    /// <remarks>
    /// Defines polyfill methods and overloads or alternative names of existing methods defined in System.Collections.Immutable.
    ///
    /// Extension methods that are available on both <see cref="IEnumerable{T}"/> and <see cref="ImmutableArray{T}"/> in System.Linq namespace
    /// are defined in System.Linq namespace to avoid accidental boxing of <see cref="ImmutableArray{T}"/>.
    /// The boxing would occur if the file calling these methods didn't have <c>using System.Linq</c>.
    /// </remarks>
    internal static class RoslynImmutableArrayExtensions
    {
        /// <summary>
        /// Variant of <see cref="System.Linq.ImmutableArrayExtensions.FirstOrDefault{T}(ImmutableArray{T}, Func{T, bool})"/>
        /// </summary>
        public static TValue? FirstOrDefault<TValue, TArg>(this ImmutableArray<TValue> array, Func<TValue, TArg, bool> predicate, TArg arg)
        {
            foreach (var val in array)
            {
                if (predicate(val, arg))
                    return val;
            }

            return default;
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.ImmutableArrayExtensions.Single{T}(ImmutableArray{T}, Func{T, bool})"/>
        /// </summary>
        public static TValue Single<TValue, TArg>(this ImmutableArray<TValue> array, Func<TValue, TArg, bool> predicate, TArg arg)
        {
            var hasValue = false;
            TValue? value = default;
            foreach (var item in array)
            {
                if (predicate(item, arg))
                {
                    if (hasValue)
                    {
                        throw ExceptionUtilities.Unreachable();
                    }

                    value = item;
                    hasValue = true;
                }
            }

            if (!hasValue)
            {
                throw ExceptionUtilities.Unreachable();
            }

            return value!;
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.ImmutableArrayExtensions.SequenceEqual{TDerived, TBase}(ImmutableArray{TBase}, ImmutableArray{TDerived}, Func{TBase, TBase, bool})"/>.
        /// </summary>
        public static bool SequenceEqual<TElement, TArg>(this ImmutableArray<TElement> array1, ImmutableArray<TElement> array2, TArg arg, Func<TElement, TElement, TArg, bool> predicate)
        {
            // The framework implementation of SequenceEqual forces a NullRef for default array1 and 2, so we
            // maintain the same behavior in this extension
            if (array1.IsDefault)
            {
                throw new NullReferenceException();
            }

            if (array2.IsDefault)
            {
                throw new NullReferenceException();
            }

            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (!predicate(array1[i], array2[i], arg))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Specialization of <see cref="System.Linq.Enumerable.Count{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/> for <see cref="ImmutableArray{T}"/>.
        /// </summary>
        public static int Count<T>(this ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
                return 0;

            var count = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                    ++count;
            }

            return count;
        }

        /// <summary>
        /// Specialization of <see cref="System.Linq.Enumerable.Sum(IEnumerable{int})"/> for <see cref="ImmutableArray{T}"/>.
        /// </summary>
        public static int Sum<T>(this ImmutableArray<T> items, Func<T, int> selector)
        {
            var sum = 0;
            foreach (var item in items)
                sum += selector(item);

            return sum;
        }

        /// <summary>
        /// Variation of <see cref="System.Linq.Enumerable.Sum(IEnumerable{int})"/> for <see cref="ImmutableArray{T}"/>.
        /// </summary>
        public static int Sum<T>(this ImmutableArray<T> items, Func<T, int, int> selector)
        {
            var sum = 0;
            for (var i = 0; i < items.Length; i++)
                sum += selector(items[i], i);

            return sum;
        }

        /// <summary>
        /// Specialization of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/> for <see cref="ImmutableArray{T}"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
            => first.AddRange(second);

        /// <summary>
        /// Variant of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, T second)
            => first.Add(second);

        /// <summary>
        /// Variant of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third)
        {
            var builder = new FixedSizeArrayBuilder<T>(first.Length + second.Length + third.Length);

            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third, ImmutableArray<T> fourth)
        {
            var builder = new FixedSizeArrayBuilder<T>(first.Length + second.Length + third.Length + fourth.Length);

            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);
            builder.AddRange(fourth);

            return builder.MoveToImmutable();

        }

        /// <summary>
        /// Variant of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third, ImmutableArray<T> fourth, ImmutableArray<T> fifth)
        {
            var builder = new FixedSizeArrayBuilder<T>(first.Length + second.Length + third.Length + fourth.Length + fifth.Length);

            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);
            builder.AddRange(fourth);
            builder.AddRange(fifth);

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.Enumerable.Concat{TSource}(IEnumerable{TSource}, IEnumerable{TSource})"/>.
        /// </summary>
        public static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third, ImmutableArray<T> fourth, ImmutableArray<T> fifth, ImmutableArray<T> sixth)
        {
            var builder = new FixedSizeArrayBuilder<T>(first.Length + second.Length + third.Length + fourth.Length + fifth.Length + sixth.Length);

            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);
            builder.AddRange(fourth);
            builder.AddRange(fifth);
            builder.AddRange(sixth);

            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Returns an array of distinct elements, preserving the order in the original array.
        /// If the array has no duplicates, the original array is returned. The original array must not be null.
        /// 
        /// Specialization of <see cref="System.Linq.Enumerable.Distinct{TSource}(IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>.
        /// </summary>
        public static ImmutableArray<T> Distinct<T>(this ImmutableArray<T> array, IEqualityComparer<T>? comparer = null)
        {
            Debug.Assert(!array.IsDefault);

            if (array.Length < 2)
            {
                return array;
            }

            var set = new HashSet<T>(comparer);
            var builder = ArrayBuilder<T>.GetInstance();
            foreach (var a in array)
            {
                if (set.Add(a))
                {
                    builder.Add(a);
                }
            }

            var result = (builder.Count == array.Length) ? array : builder.ToImmutable();
            builder.Free();
            return result;
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.ImmutableArrayExtensions.Any{T}(ImmutableArray{T}, Func{T, bool})"/>.
        /// </summary>
        public static bool Any<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (var item in array)
            {
                if (predicate(item, arg))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Variant of <see cref="System.Linq.ImmutableArrayExtensions.All{T}(ImmutableArray{T}, Func{T, bool})"/>.
        /// </summary>
        public static bool All<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (var item in array)
            {
                if (!predicate(item, arg))
                    return false;
            }

            return true;
        }

    }
}

namespace System.Collections.Immutable
{
    /// <remarks>
    /// Defines polyfill methods and overloads or alternative names of existing methods defined in System.Collections.Immutable.
    ///
    /// Methods that are available on both <see cref="IEnumerable{T}"/> and <see cref="ImmutableArray{T}"/> in System.Collections.Immutable namespace
    /// are defined in System.Collections.Immutable namespace to avoid accidental boxing of <see cref="ImmutableArray{T}"/>.
    /// The boxing would occur if the file calling these methods didn't have <c>using System.Collections.Immutable</c>.
    /// </remarks>
    internal static class RoslynImmutableArrayExtensions
    {
        /// <summary>
        /// Variant of <see cref="System.Collections.Immutable.ImmutableArray{T}.Contains(T, IEqualityComparer{T})"/>
        /// </summary>
        public static bool Contains<T>(this ImmutableArray<T> array, Func<T, bool> predicate)
            => array.Any(predicate);

        /// <summary>
        /// Variant of <see cref="System.Collections.Immutable.ImmutableArray.BinarySearch{T}(ImmutableArray{T}, T, IComparer{T}?)"/> 
        /// with the ability to pass arbitrary value to the comparer without allocation.
        /// </summary>
        public static int BinarySearch<TElement, TValue>(this ImmutableArray<TElement> array, TValue value, Func<TElement, TValue, int> comparer)
            => array.AsSpan().BinarySearch(value, comparer);
    }
}
