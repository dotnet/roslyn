// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

#if DEBUG
using System.Linq;
#endif

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
                case 0:
                    return ImmutableArray<TResult>.Empty;

                case 1:
                    return ImmutableArray.Create(map(items[0], 0, arg));

                case 2:
                    return ImmutableArray.Create(map(items[0], 0, arg), map(items[1], 1, arg));

                case 3:
                    return ImmutableArray.Create(map(items[0], 0, arg), map(items[1], 1, arg), map(items[2], 2, arg));

                case 4:
                    return ImmutableArray.Create(map(items[0], 0, arg), map(items[1], 1, arg), map(items[2], 2, arg), map(items[3], 3, arg));

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(items.Length);
                    for (int i = 0; i < items.Length; i++)
                    {
                        builder.Add(map(items[i], i, arg));
                    }

                    return builder.ToImmutableAndFree();
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
            {
                return ImmutableArray<TResult>.Empty;
            }

            var builder = ArrayBuilder<TResult>.GetInstance();
            foreach (var item in array)
            {
                if (predicate(item))
                {
                    builder.Add(selector(item));
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Maps an immutable array through a function that returns ValueTasks, returning the new ImmutableArray.
        /// </summary>
        public static async ValueTask<ImmutableArray<TResult>> SelectAsArrayAsync<TItem, TResult>(this ImmutableArray<TItem> array, Func<TItem, CancellationToken, ValueTask<TResult>> selector, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<TResult>.GetInstance(array.Length);

            foreach (var item in array)
            {
                builder.Add(await selector(item, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
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
                case 0:
                    return ImmutableArray<TResult>.Empty;

                case 1:
                    return ImmutableArray.Create(map(self[0], other[0]));

                case 2:
                    return ImmutableArray.Create(map(self[0], other[0]), map(self[1], other[1]));

                case 3:
                    return ImmutableArray.Create(map(self[0], other[0]), map(self[1], other[1]), map(self[2], other[2]));

                case 4:
                    return ImmutableArray.Create(map(self[0], other[0]), map(self[1], other[1]), map(self[2], other[2]), map(self[3], other[3]));

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(self.Length);
                    for (int i = 0; i < self.Length; i++)
                    {
                        builder.Add(map(self[i], other[i]));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        public static ImmutableArray<TResult> ZipAsArray<T1, T2, TArg, TResult>(this ImmutableArray<T1> self, ImmutableArray<T2> other, TArg arg, Func<T1, T2, int, TArg, TResult> map)
        {
            Debug.Assert(self.Length == other.Length);
            if (self.IsEmpty)
            {
                return ImmutableArray<TResult>.Empty;
            }

            var builder = ArrayBuilder<TResult>.GetInstance(self.Length);
            for (int i = 0; i < self.Length; i++)
            {
                builder.Add(map(self[i], other[i], i, arg));
            }
            return builder.ToImmutableAndFree();
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

        public static bool Any<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if (predicate(a, arg))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool All<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if (!predicate(a, arg))
                {
                    return false;
                }
            }

            return true;
        }

        public static async Task<bool> AnyAsync<T>(this ImmutableArray<T> array, Func<T, Task<bool>> predicateAsync)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if (await predicateAsync(a).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> AnyAsync<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, Task<bool>> predicateAsync, TArg arg)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if (await predicateAsync(a, arg).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        public static async ValueTask<T?> FirstOrDefaultAsync<T>(this ImmutableArray<T> array, Func<T, Task<bool>> predicateAsync)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                var a = array[i];

                if (await predicateAsync(a).ConfigureAwait(false))
                {
                    return a;
                }
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
        /// Returns an array of distinct elements, preserving the order in the original array.
        /// If the array has no duplicates, the original array is returned. The original array must not be null.
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

        internal static bool HasAnyErrors<T>(this ImmutableArray<T> diagnostics) where T : Diagnostic
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

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
                return copy.AsImmutable();
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
            {
                return ImmutableArray<TValue>.Empty;
            }

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

        internal static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
        {
            return first.AddRange(second);
        }

        internal static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third)
        {
            var builder = ArrayBuilder<T>.GetInstance(first.Length + second.Length + third.Length);
            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, ImmutableArray<T> second, ImmutableArray<T> third, ImmutableArray<T> fourth)
        {
            var builder = ArrayBuilder<T>.GetInstance(first.Length + second.Length + third.Length + fourth.Length);
            builder.AddRange(first);
            builder.AddRange(second);
            builder.AddRange(third);
            builder.AddRange(fourth);
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<T> Concat<T>(this ImmutableArray<T> first, T second)
        {
            return first.Add(second);
        }

        internal static bool HasDuplicates<T>(this ImmutableArray<T> array, IEqualityComparer<T> comparer)
        {
            switch (array.Length)
            {
                case 0:
                case 1:
                    return false;

                case 2:
                    return comparer.Equals(array[0], array[1]);

                default:
                    var set = new HashSet<T>(comparer);
                    foreach (var i in array)
                    {
                        if (!set.Add(i))
                        {
                            return true;
                        }
                    }

                    return false;
            }
        }

        public static int Count<T>(this ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Length; ++i)
            {
                if (predicate(items[i]))
                {
                    ++count;
                }
            }

            return count;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ImmutableArrayProxy<T>
        {
            internal T[] MutableArray;
        }

        // TODO(https://github.com/dotnet/corefx/issues/34126): Remove when System.Collections.Immutable
        // provides a Span API
        internal static T[] DangerousGetUnderlyingArray<T>(this ImmutableArray<T> array)
            => Unsafe.As<ImmutableArray<T>, ImmutableArrayProxy<T>>(ref array).MutableArray;

        internal static ReadOnlySpan<T> AsSpan<T>(this ImmutableArray<T> array)
            => array.DangerousGetUnderlyingArray();

        internal static ImmutableArray<T> DangerousCreateFromUnderlyingArray<T>([MaybeNull] ref T[] array)
        {
            var proxy = new ImmutableArrayProxy<T> { MutableArray = array };
            array = null!;
            return Unsafe.As<ImmutableArrayProxy<T>, ImmutableArray<T>>(ref proxy);
        }

        internal static Dictionary<K, ImmutableArray<T>> ToDictionary<K, T>(this ImmutableArray<T> items, Func<T, K> keySelector, IEqualityComparer<K>? comparer = null)
            where K : notnull
        {
            if (items.Length == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = items[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (items.Length == 0)
            {
                return new Dictionary<K, ImmutableArray<T>>(comparer);
            }

            // bucketize
            // prevent reallocation. it may not have 'count' entries, but it won't have more. 
            var accumulator = new Dictionary<K, ArrayBuilder<T>>(items.Length, comparer);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var key = keySelector(item);
                if (!accumulator.TryGetValue(key, out var bucket))
                {
                    bucket = ArrayBuilder<T>.GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new Dictionary<K, ImmutableArray<T>>(accumulator.Count, comparer);

            // freeze
            foreach (var pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }

        internal static Location FirstOrNone(this ImmutableArray<Location> items)
        {
            return items.IsEmpty ? Location.None : items[0];
        }

        internal static bool SequenceEqual<TElement, TArg>(this ImmutableArray<TElement> array1, ImmutableArray<TElement> array2, TArg arg, Func<TElement, TElement, TArg, bool> predicate)
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

        internal static int IndexOf<T>(this ImmutableArray<T> array, T item, IEqualityComparer<T> comparer)
            => array.IndexOf(item, startIndex: 0, comparer);

        internal static bool IsSorted<T>(this ImmutableArray<T> array, IComparer<T> comparer)
        {
            for (var i = 1; i < array.Length; i++)
            {
                if (comparer.Compare(array[i - 1], array[i]) > 0)
                {
                    return false;
                }
            }

            return true;
        }

        // same as Array.BinarySearch but the ability to pass arbitrary value to the comparer without allocation
        internal static int BinarySearch<TElement, TValue>(this ImmutableArray<TElement> array, TValue value, Func<TElement, TValue, int> comparer)
        {
            int low = 0;
            int high = array.Length - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                int comparison = comparer(array[middle], value);

                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison > 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return ~low;
        }
    }
}
