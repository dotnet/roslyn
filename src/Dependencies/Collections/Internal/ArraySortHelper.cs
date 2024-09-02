// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v8.0.3/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ArraySortHelper.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if NET
using System.Numerics;
#else
using System.Runtime.InteropServices;
#endif

#pragma warning disable CA1822

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    #region ArraySortHelper for single arrays

    internal static class SegmentedArraySortHelper<T>
    {
        public static void Sort(SegmentedArraySegment<T> keys, IComparer<T>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                comparer ??= Comparer<T>.Default;
                IntrospectiveSort(keys, comparer.Compare);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        public static int BinarySearch(SegmentedArray<T> array, int index, int length, T value, IComparer<T>? comparer)
        {
            try
            {
                comparer ??= Comparer<T>.Default;
                return InternalBinarySearch(array, index, length, value, comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                return 0;
            }
        }

        internal static void Sort(SegmentedArraySegment<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null, "Check the arguments in the caller!");

            // Add a try block here to detect bogus comparisons
            try
            {
                IntrospectiveSort(keys, comparer!);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        internal static int InternalBinarySearch(SegmentedArray<T> array, int index, int length, T value, IComparer<T> comparer)
        {
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(array[i], value);

                if (order == 0)
                    return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        private static void SwapIfGreater(SegmentedArraySegment<T> keys, Comparison<T> comparer, int i, int j)
        {
            Debug.Assert(i != j);

            if (comparer(keys[i], keys[j]) > 0)
            {
                T key = keys[i];
                keys[i] = keys[j];
                keys[j] = key;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(SegmentedArraySegment<T> a, int i, int j)
        {
            Debug.Assert(i != j);

            T t = a[i];
            a[i] = a[j];
            a[j] = t;
        }

        internal static void IntrospectiveSort(SegmentedArraySegment<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null);

            if (keys.Length > 1)
            {
                IntroSort(keys, 2 * (SegmentedArraySortUtils.Log2((uint)keys.Length) + 1), comparer!);
            }
        }

        // IntroSort is recursive; block it from being inlined into itself as
        // this is currenly not profitable.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void IntroSort(SegmentedArraySegment<T> keys, int depthLimit, Comparison<T> comparer)
        {
            Debug.Assert(keys.Length > 0);
            Debug.Assert(depthLimit >= 0);
            Debug.Assert(comparer != null);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= SegmentedArrayHelper.IntrosortSizeThreshold)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreater(keys, comparer!, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreater(keys, comparer!, 0, 1);
                        SwapIfGreater(keys, comparer!, 0, 2);
                        SwapIfGreater(keys, comparer!, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), comparer!);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), comparer!);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), comparer!);

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), depthLimit, comparer!);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(SegmentedArraySegment<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(keys.Length >= SegmentedArrayHelper.IntrosortSizeThreshold);
            Debug.Assert(comparer != null);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreater(keys, comparer!, 0, middle);  // swap the low with the mid point
            SwapIfGreater(keys, comparer!, 0, hi);   // swap the low with the high
            SwapIfGreater(keys, comparer!, middle, hi); // swap the middle with the high

            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer!(keys[++left], pivot) < 0)
                {
                    // Intentionally empty
                }

                while (comparer(pivot, keys[--right]) < 0)
                {
                    // Intentionally empty
                }

                if (left >= right)
                    break;

                Swap(keys, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(SegmentedArraySegment<T> keys, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(keys.Length > 0);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n, comparer!);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, 0, i - 1);
                DownHeap(keys, 1, i - 1, comparer!);
            }
        }

        private static void DownHeap(SegmentedArraySegment<T> keys, int i, int n, Comparison<T> comparer)
        {
            Debug.Assert(comparer != null);

            T d = keys[i - 1];
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && comparer!(keys[child - 1], keys[child]) < 0)
                {
                    child++;
                }

                if (!(comparer!(d, keys[child - 1]) < 0))
                    break;

                keys[i - 1] = keys[child - 1];
                i = child;
            }

            keys[i - 1] = d;
        }

        private static void InsertionSort(SegmentedArraySegment<T> keys, Comparison<T> comparer)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                T t = keys[i + 1];

                int j = i;
                while (j >= 0 && comparer(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t;
            }
        }
    }

    internal static class SegmentedGenericArraySortHelper<T>
        where T : IComparable<T>
    {
        public static void Sort(SegmentedArraySegment<T> keys, IComparer<T>? comparer)
        {
            try
            {
                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    if (keys.Length > 1)
                    {
                        // For floating-point, do a pre-pass to move all NaNs to the beginning
                        // so that we can do an optimized comparison as part of the actual sort
                        // on the remainder of the values.
                        if (typeof(T) == typeof(double)
                            || typeof(T) == typeof(float)
#if NET
                            || typeof(T) == typeof(Half)
#endif
                            )
                        {
                            int nanLeft = SegmentedArraySortUtils.MoveNansToFront(keys, default(Span<byte>));
                            if (nanLeft == keys.Length)
                            {
                                return;
                            }
                            keys = keys.Slice(nanLeft);
                        }

                        IntroSort(keys, 2 * (SegmentedArraySortUtils.Log2((uint)keys.Length) + 1));
                    }
                }
                else
                {
                    SegmentedArraySortHelper<T>.IntrospectiveSort(keys, comparer.Compare);
                }
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        public static int BinarySearch(SegmentedArray<T> array, int index, int length, T value, IComparer<T>? comparer)
        {
            Debug.Assert(index >= 0 && length >= 0 && (array.Length - index >= length), "Check the arguments in the caller!");

            try
            {
                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    return BinarySearch(array, index, length, value);
                }
                else
                {
                    return SegmentedArraySortHelper<T>.InternalBinarySearch(array, index, length, value, comparer);
                }
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                return 0;
            }
        }

        // This function is called when the user doesn't specify any comparer.
        // Since T is constrained here, we can call IComparable<T>.CompareTo here.
        // We can avoid boxing for value type and casting for reference types.
        private static int BinarySearch(SegmentedArray<T> array, int index, int length, T value)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order;
                if (array[i] == null)
                {
                    order = (value == null) ? 0 : -1;
                }
                else
                {
                    order = array[i].CompareTo(value!);
                }

                if (order == 0)
                {
                    return i;
                }

                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        /// <summary>Swaps the values in the two references if the first is greater than the second.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SwapIfGreater(ref T i, ref T j)
        {
            if (i != null && GreaterThan(ref i, ref j))
            {
                Swap(ref i, ref j);
            }
        }

        /// <summary>Swaps the values in the two references, regardless of whether the two references are the same.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(ref T i, ref T j)
        {
            Debug.Assert(!Unsafe.AreSame(ref i, ref j));

            T t = i;
            i = j;
            j = t;
        }

        // IntroSort is recursive; block it from being inlined into itself as
        // this is currenly not profitable.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void IntroSort(SegmentedArraySegment<T> keys, int depthLimit)
        {
            Debug.Assert(keys.Length > 0);
            Debug.Assert(depthLimit >= 0);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= SegmentedArrayHelper.IntrosortSizeThreshold)
                {
                    if (partitionSize == 2)
                    {
                        SwapIfGreater(ref keys[0], ref keys[1]);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        ref T hiRef = ref keys[2];
                        ref T him1Ref = ref keys[1];
                        ref T loRef = ref keys[0];

                        SwapIfGreater(ref loRef, ref him1Ref);
                        SwapIfGreater(ref loRef, ref hiRef);
                        SwapIfGreater(ref him1Ref, ref hiRef);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize));
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize));
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize));

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), depthLimit);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(SegmentedArraySegment<T> keys)
        {
            Debug.Assert(keys.Length >= SegmentedArrayHelper.IntrosortSizeThreshold);

            // Use median-of-three to select a pivot. Grab a reference to the 0th, Length-1th, and Length/2th elements, and sort them.
            int zeroIndex = 0;
            int lastIndex = keys.Length - 1;
            int middleIndex = (keys.Length - 1) >> 1;
            SwapIfGreater(ref keys[zeroIndex], ref keys[middleIndex]);
            SwapIfGreater(ref keys[zeroIndex], ref keys[lastIndex]);
            SwapIfGreater(ref keys[middleIndex], ref keys[lastIndex]);

            // Select the middle value as the pivot, and move it to be just before the last element.
            int nextToLastIndex = keys.Length - 2;
            T pivot = keys[middleIndex];
            Swap(ref keys[middleIndex], ref keys[nextToLastIndex]);

            // Walk the left and right pointers, swapping elements as necessary, until they cross.
            int leftIndex = zeroIndex, rightIndex = nextToLastIndex;
            while (leftIndex < rightIndex)
            {
                if (pivot == null)
                {
                    while (leftIndex < nextToLastIndex && keys[++leftIndex] == null)
                    {
                        // Intentionally empty
                    }

                    while (rightIndex > zeroIndex && keys[--rightIndex] != null)
                    {
                        // Intentionally empty
                    }
                }
                else
                {
                    while (leftIndex < nextToLastIndex && GreaterThan(ref pivot, ref keys[++leftIndex]))
                    {
                        // Intentionally empty
                    }

                    while (rightIndex > zeroIndex && LessThan(ref pivot, ref keys[--rightIndex]))
                    {
                        // Intentionally empty
                    }
                }

                if (leftIndex >= rightIndex)
                {
                    break;
                }

                Swap(ref keys[leftIndex], ref keys[rightIndex]);
            }

            // Put the pivot in the correct location.
            if (leftIndex != nextToLastIndex)
            {
                Swap(ref keys[leftIndex], ref keys[nextToLastIndex]);
            }

            return leftIndex;
        }

        private static void HeapSort(SegmentedArraySegment<T> keys)
        {
            Debug.Assert(keys.Length > 0);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, i, n);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(ref keys[0], ref keys[i - 1]);
                DownHeap(keys, 1, i - 1);
            }
        }

        private static void DownHeap(SegmentedArraySegment<T> keys, int i, int n)
        {
            T d = keys[i - 1];
            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && (keys[child - 1] == null || LessThan(ref keys[child - 1], ref keys[child])))
                {
                    child++;
                }

                if (keys[child - 1] == null || !LessThan(ref d, ref keys[child - 1]))
                    break;

                keys[i - 1] = keys[child - 1];
                i = child;
            }

            keys[i - 1] = d;
        }

        private static void InsertionSort(SegmentedArraySegment<T> keys)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                T t = keys[i + 1];

                int j = i;
                while (j >= 0 && (t == null || LessThan(ref t, ref keys[j])))
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t!;
            }
        }

        // - These methods exist for use in sorting, where the additional operations present in
        //   the CompareTo methods that would otherwise be used on these primitives add non-trivial overhead,
        //   in particular for floating point where the CompareTo methods need to factor in NaNs.
        // - The floating-point comparisons here assume no NaNs, which is valid only because the sorting routines
        //   themselves special-case NaN with a pre-pass that ensures none are present in the values being sorted
        //   by moving them all to the front first and then sorting the rest.
        // - These are duplicated here rather than being on a helper type due to current limitations around generic inlining.

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool LessThan(ref T left, ref T right)
        {
            if (typeof(T) == typeof(byte))
                return (byte)(object)left < (byte)(object)right;
            if (typeof(T) == typeof(sbyte))
                return (sbyte)(object)left < (sbyte)(object)right;
            if (typeof(T) == typeof(ushort))
                return (ushort)(object)left < (ushort)(object)right;
            if (typeof(T) == typeof(short))
                return (short)(object)left < (short)(object)right;
            if (typeof(T) == typeof(uint))
                return (uint)(object)left < (uint)(object)right;
            if (typeof(T) == typeof(int))
                return (int)(object)left < (int)(object)right;
            if (typeof(T) == typeof(ulong))
                return (ulong)(object)left < (ulong)(object)right;
            if (typeof(T) == typeof(long))
                return (long)(object)left < (long)(object)right;
            if (typeof(T) == typeof(UIntPtr))
                return (nuint)(object)left < (nuint)(object)right;
            if (typeof(T) == typeof(IntPtr))
                return (nint)(object)left < (nint)(object)right;
            if (typeof(T) == typeof(float))
                return (float)(object)left < (float)(object)right;
            if (typeof(T) == typeof(double))
                return (double)(object)left < (double)(object)right;
#if NET
            if (typeof(T) == typeof(Half))
                return (Half)(object)left < (Half)(object)right;
#endif
            return left.CompareTo(right) < 0 ? true : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool GreaterThan(ref T left, ref T right)
        {
            if (typeof(T) == typeof(byte))
                return (byte)(object)left > (byte)(object)right;
            if (typeof(T) == typeof(sbyte))
                return (sbyte)(object)left > (sbyte)(object)right;
            if (typeof(T) == typeof(ushort))
                return (ushort)(object)left > (ushort)(object)right;
            if (typeof(T) == typeof(short))
                return (short)(object)left > (short)(object)right;
            if (typeof(T) == typeof(uint))
                return (uint)(object)left > (uint)(object)right;
            if (typeof(T) == typeof(int))
                return (int)(object)left > (int)(object)right;
            if (typeof(T) == typeof(ulong))
                return (ulong)(object)left > (ulong)(object)right;
            if (typeof(T) == typeof(long))
                return (long)(object)left > (long)(object)right;
            if (typeof(T) == typeof(UIntPtr))
                return (nuint)(object)left > (nuint)(object)right;
            if (typeof(T) == typeof(IntPtr))
                return (nint)(object)left > (nint)(object)right;
            if (typeof(T) == typeof(float))
                return (float)(object)left > (float)(object)right;
            if (typeof(T) == typeof(double))
                return (double)(object)left > (double)(object)right;
#if NET
            if (typeof(T) == typeof(Half))
                return (Half)(object)left > (Half)(object)right;
#endif
            return left.CompareTo(right) > 0 ? true : false;
        }
    }

    #endregion

    #region ArraySortHelper for paired key and value arrays

    internal static class SegmentedArraySortHelper<TKey, TValue>
    {
        public static void Sort(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                IntrospectiveSort(keys, values, comparer ?? Comparer<TKey>.Default);
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        private static void SwapIfGreaterWithValues(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey> comparer, int i, int j)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(0 <= i && i < keys.Length && i < values.Length);
            Debug.Assert(0 <= j && j < keys.Length && j < values.Length);
            Debug.Assert(i != j);

            if (comparer!.Compare(keys[i], keys[j]) > 0)
            {
                TKey key = keys[i];
                keys[i] = keys[j];
                keys[j] = key;

                TValue value = values[i];
                values[i] = values[j];
                values[j] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(SegmentedArraySegment<TKey> keys, Span<TValue> values, int i, int j)
        {
            Debug.Assert(i != j);

            TKey k = keys[i];
            keys[i] = keys[j];
            keys[j] = k;

            TValue v = values[i];
            values[i] = values[j];
            values[j] = v;
        }

        internal static void IntrospectiveSort(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(keys.Length == values.Length);

            if (keys.Length > 1)
            {
                IntroSort(keys, values, 2 * (SegmentedArraySortUtils.Log2((uint)keys.Length) + 1), comparer!);
            }
        }

        private static void IntroSort(SegmentedArraySegment<TKey> keys, Span<TValue> values, int depthLimit, IComparer<TKey> comparer)
        {
            Debug.Assert(keys.Length > 0);
            Debug.Assert(values.Length == keys.Length);
            Debug.Assert(depthLimit >= 0);
            Debug.Assert(comparer != null);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= SegmentedArrayHelper.IntrosortSizeThreshold)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 1);
                        SwapIfGreaterWithValues(keys, values, comparer!, 0, 2);
                        SwapIfGreaterWithValues(keys, values, comparer!, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), values.Slice(0, partitionSize), comparer!);

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), values.Slice(p + 1, partitionSize - (p + 1)), depthLimit, comparer!);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(keys.Length >= SegmentedArrayHelper.IntrosortSizeThreshold);
            Debug.Assert(comparer != null);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithValues(keys, values, comparer!, 0, middle);  // swap the low with the mid point
            SwapIfGreaterWithValues(keys, values, comparer!, 0, hi);   // swap the low with the high
            SwapIfGreaterWithValues(keys, values, comparer!, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (comparer!.Compare(keys[++left], pivot) < 0)
                {
                    // Intentionally empty
                }

                while (comparer.Compare(pivot, keys[--right]) < 0)
                {
                    // Intentionally empty
                }

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, values, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);
            Debug.Assert(keys.Length > 0);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, values, i, n, comparer!);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, values, 0, i - 1);
                DownHeap(keys, values, 1, i - 1, comparer!);
            }
        }

        private static void DownHeap(SegmentedArraySegment<TKey> keys, Span<TValue> values, int i, int n, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);

            TKey d = keys[i - 1];
            TValue dValue = values[i - 1];

            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && comparer!.Compare(keys[child - 1], keys[child]) < 0)
                {
                    child++;
                }

                if (!(comparer!.Compare(d, keys[child - 1]) < 0))
                    break;

                keys[i - 1] = keys[child - 1];
                values[i - 1] = values[child - 1];
                i = child;
            }

            keys[i - 1] = d;
            values[i - 1] = dValue;
        }

        private static void InsertionSort(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey> comparer)
        {
            Debug.Assert(comparer != null);

            for (int i = 0; i < keys.Length - 1; i++)
            {
                TKey t = keys[i + 1];
                TValue tValue = values[i + 1];

                int j = i;
                while (j >= 0 && comparer!.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    values[j + 1] = values[j];
                    j--;
                }

                keys[j + 1] = t;
                values[j + 1] = tValue;
            }
        }
    }

    internal static class SegmentedGenericArraySortHelper<TKey, TValue>
        where TKey : IComparable<TKey>
    {
        public static void Sort(SegmentedArraySegment<TKey> keys, Span<TValue> values, IComparer<TKey>? comparer)
        {
            // Add a try block here to detect IComparers (or their
            // underlying IComparables, etc) that are bogus.
            try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
                    if (keys.Length > 1)
                    {
                        // For floating-point, do a pre-pass to move all NaNs to the beginning
                        // so that we can do an optimized comparison as part of the actual sort
                        // on the remainder of the values.
                        if (typeof(TKey) == typeof(double)
                            || typeof(TKey) == typeof(float)
#if NET
                            || typeof(TKey) == typeof(Half)
#endif
                            )
                        {
                            int nanLeft = SegmentedArraySortUtils.MoveNansToFront(keys, values);
                            if (nanLeft == keys.Length)
                            {
                                return;
                            }
                            keys = keys.Slice(nanLeft);
                            values = values.Slice(nanLeft);
                        }

                        IntroSort(keys, values, 2 * (SegmentedArraySortUtils.Log2((uint)keys.Length) + 1));
                    }
                }
                else
                {
                    SegmentedArraySortHelper<TKey, TValue>.IntrospectiveSort(keys, values, comparer);
                }
            }
            catch (IndexOutOfRangeException)
            {
                ThrowHelper.ThrowArgumentException_BadComparer(comparer);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
            }
        }

        private static void SwapIfGreaterWithValues(SegmentedArraySegment<TKey> keys, Span<TValue> values, int i, int j)
        {
            Debug.Assert(i != j);

            ref TKey keyRef = ref keys[i];
            if (keyRef != null && GreaterThan(ref keyRef, ref keys[j]))
            {
                TKey key = keyRef;
                keys[i] = keys[j];
                keys[j] = key;

                TValue value = values[i];
                values[i] = values[j];
                values[j] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(SegmentedArraySegment<TKey> keys, Span<TValue> values, int i, int j)
        {
            Debug.Assert(i != j);

            TKey k = keys[i];
            keys[i] = keys[j];
            keys[j] = k;

            TValue v = values[i];
            values[i] = values[j];
            values[j] = v;
        }

        private static void IntroSort(SegmentedArraySegment<TKey> keys, Span<TValue> values, int depthLimit)
        {
            Debug.Assert(keys.Length > 0);
            Debug.Assert(values.Length == keys.Length);
            Debug.Assert(depthLimit >= 0);

            int partitionSize = keys.Length;
            while (partitionSize > 1)
            {
                if (partitionSize <= SegmentedArrayHelper.IntrosortSizeThreshold)
                {

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithValues(keys, values, 0, 1);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithValues(keys, values, 0, 1);
                        SwapIfGreaterWithValues(keys, values, 0, 2);
                        SwapIfGreaterWithValues(keys, values, 1, 2);
                        return;
                    }

                    InsertionSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize));
                    return;
                }

                if (depthLimit == 0)
                {
                    HeapSort(keys.Slice(0, partitionSize), values.Slice(0, partitionSize));
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(keys.Slice(0, partitionSize), values.Slice(0, partitionSize));

                // Note we've already partitioned around the pivot and do not have to move the pivot again.
                IntroSort(keys.Slice(p + 1, partitionSize - (p + 1)), values.Slice(p + 1, partitionSize - (p + 1)), depthLimit);
                partitionSize = p;
            }
        }

        private static int PickPivotAndPartition(SegmentedArraySegment<TKey> keys, Span<TValue> values)
        {
            Debug.Assert(keys.Length >= SegmentedArrayHelper.IntrosortSizeThreshold);

            int hi = keys.Length - 1;

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int middle = hi >> 1;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreaterWithValues(keys, values, 0, middle);  // swap the low with the mid point
            SwapIfGreaterWithValues(keys, values, 0, hi);   // swap the low with the high
            SwapIfGreaterWithValues(keys, values, middle, hi); // swap the middle with the high

            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = 0, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                if (pivot == null)
                {
                    while (left < (hi - 1) && keys[++left] == null)
                    {
                        // Intentionally empty
                    }

                    while (right > 0 && keys[--right] != null)
                    {
                        // Intentionally empty
                    }
                }
                else
                {
                    while (GreaterThan(ref pivot, ref keys[++left]))
                    {
                        // Intentionally empty
                    }

                    while (LessThan(ref pivot, ref keys[--right]))
                    {
                        // Intentionally empty
                    }
                }

                if (left >= right)
                    break;

                Swap(keys, values, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(keys, values, left, hi - 1);
            }
            return left;
        }

        private static void HeapSort(SegmentedArraySegment<TKey> keys, Span<TValue> values)
        {
            Debug.Assert(keys.Length > 0);

            int n = keys.Length;
            for (int i = n >> 1; i >= 1; i--)
            {
                DownHeap(keys, values, i, n);
            }

            for (int i = n; i > 1; i--)
            {
                Swap(keys, values, 0, i - 1);
                DownHeap(keys, values, 1, i - 1);
            }
        }

        private static void DownHeap(SegmentedArraySegment<TKey> keys, Span<TValue> values, int i, int n)
        {
            TKey d = keys[i - 1];
            TValue dValue = values[i - 1];

            while (i <= n >> 1)
            {
                int child = 2 * i;
                if (child < n && (keys[child - 1] == null || LessThan(ref keys[child - 1], ref keys[child])))
                {
                    child++;
                }

                if (keys[child - 1] == null || !LessThan(ref d, ref keys[child - 1]))
                    break;

                keys[i - 1] = keys[child - 1];
                values[i - 1] = values[child - 1];
                i = child;
            }

            keys[i - 1] = d;
            values[i - 1] = dValue;
        }

        private static void InsertionSort(SegmentedArraySegment<TKey> keys, Span<TValue> values)
        {
            for (int i = 0; i < keys.Length - 1; i++)
            {
                TKey t = keys[i + 1];
                TValue tValue = values[i + 1];

                int j = i;
                while (j >= 0 && (t == null || LessThan(ref t, ref keys[j])))
                {
                    keys[j + 1] = keys[j];
                    values[j + 1] = values[j];
                    j--;
                }

                keys[j + 1] = t!;
                values[j + 1] = tValue;
            }
        }

        // - These methods exist for use in sorting, where the additional operations present in
        //   the CompareTo methods that would otherwise be used on these primitives add non-trivial overhead,
        //   in particular for floating point where the CompareTo methods need to factor in NaNs.
        // - The floating-point comparisons here assume no NaNs, which is valid only because the sorting routines
        //   themselves special-case NaN with a pre-pass that ensures none are present in the values being sorted
        //   by moving them all to the front first and then sorting the rest.
        // - These are duplicated here rather than being on a helper type due to current limitations around generic inlining.

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool LessThan(ref TKey left, ref TKey right)
        {
            if (typeof(TKey) == typeof(byte))
                return (byte)(object)left < (byte)(object)right;
            if (typeof(TKey) == typeof(sbyte))
                return (sbyte)(object)left < (sbyte)(object)right;
            if (typeof(TKey) == typeof(ushort))
                return (ushort)(object)left < (ushort)(object)right;
            if (typeof(TKey) == typeof(short))
                return (short)(object)left < (short)(object)right;
            if (typeof(TKey) == typeof(uint))
                return (uint)(object)left < (uint)(object)right;
            if (typeof(TKey) == typeof(int))
                return (int)(object)left < (int)(object)right;
            if (typeof(TKey) == typeof(ulong))
                return (ulong)(object)left < (ulong)(object)right;
            if (typeof(TKey) == typeof(long))
                return (long)(object)left < (long)(object)right;
            if (typeof(TKey) == typeof(UIntPtr))
                return (nuint)(object)left < (nuint)(object)right;
            if (typeof(TKey) == typeof(IntPtr))
                return (nint)(object)left < (nint)(object)right;
            if (typeof(TKey) == typeof(float))
                return (float)(object)left < (float)(object)right;
            if (typeof(TKey) == typeof(double))
                return (double)(object)left < (double)(object)right;
#if NET
            if (typeof(TKey) == typeof(Half))
                return (Half)(object)left < (Half)(object)right;
#endif
            return left.CompareTo(right) < 0 ? true : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // compiles to a single comparison or method call
        private static bool GreaterThan(ref TKey left, ref TKey right)
        {
            if (typeof(TKey) == typeof(byte))
                return (byte)(object)left > (byte)(object)right;
            if (typeof(TKey) == typeof(sbyte))
                return (sbyte)(object)left > (sbyte)(object)right;
            if (typeof(TKey) == typeof(ushort))
                return (ushort)(object)left > (ushort)(object)right;
            if (typeof(TKey) == typeof(short))
                return (short)(object)left > (short)(object)right;
            if (typeof(TKey) == typeof(uint))
                return (uint)(object)left > (uint)(object)right;
            if (typeof(TKey) == typeof(int))
                return (int)(object)left > (int)(object)right;
            if (typeof(TKey) == typeof(ulong))
                return (ulong)(object)left > (ulong)(object)right;
            if (typeof(TKey) == typeof(long))
                return (long)(object)left > (long)(object)right;
            if (typeof(TKey) == typeof(UIntPtr))
                return (nuint)(object)left > (nuint)(object)right;
            if (typeof(TKey) == typeof(IntPtr))
                return (nint)(object)left > (nint)(object)right;
            if (typeof(TKey) == typeof(float))
                return (float)(object)left > (float)(object)right;
            if (typeof(TKey) == typeof(double))
                return (double)(object)left > (double)(object)right;
#if NET
            if (typeof(TKey) == typeof(Half))
                return (Half)(object)left > (Half)(object)right;
#endif
            return left.CompareTo(right) > 0 ? true : false;
        }
    }

    #endregion

    /// <summary>Helper methods for use in array/span sorting routines.</summary>
    internal static class SegmentedArraySortUtils
    {
#if !NETCOREAPP
        private static ReadOnlySpan<byte> Log2DeBruijn => new byte[32]
        {
            00, 09, 01, 10, 13, 21, 02, 29,
            11, 14, 16, 18, 22, 25, 03, 30,
            08, 12, 20, 28, 15, 17, 24, 07,
            19, 27, 23, 06, 26, 05, 04, 31,
        };
#endif

        public static int MoveNansToFront<TKey, TValue>(SegmentedArraySegment<TKey> keys, Span<TValue> values) where TKey : notnull
        {
            Debug.Assert(typeof(TKey) == typeof(double) || typeof(TKey) == typeof(float));

            int left = 0;

            for (int i = 0; i < keys.Length; i++)
            {
                if ((typeof(TKey) == typeof(double) && double.IsNaN((double)(object)keys[i]))
                    || (typeof(TKey) == typeof(float) && float.IsNaN((float)(object)keys[i]))
#if NET
                    || (typeof(TKey) == typeof(Half) && Half.IsNaN((Half)(object)keys[i]))
#endif
                    )
                {
                    TKey temp = keys[left];
                    keys[left] = keys[i];
                    keys[i] = temp;

                    if ((uint)i < (uint)values.Length) // check to see if we have values
                    {
                        TValue tempValue = values[left];
                        values[left] = values[i];
                        values[i] = tempValue;
                    }

                    left++;
                }
            }

            return left;
        }

        public static int Log2(uint value)
        {
#if NET
            return BitOperations.Log2(value);
#else
            // Fallback contract is 0->0
            return Log2SoftwareFallback(value);
#endif
        }

#if !NETCOREAPP
        /// <summary>
        /// Returns the integer (floor) log of the specified value, base 2.
        /// Note that by convention, input value 0 returns 0 since Log(0) is undefined.
        /// Does not directly use any hardware intrinsics, nor does it incur branching.
        /// </summary>
        /// <param name="value">The value.</param>
        private static int Log2SoftwareFallback(uint value)
        {
            // No AggressiveInlining due to large method size
            // Has conventional contract 0->0 (Log(0) is undefined)

            // Fill trailing zeros with ones, eg 00010010 becomes 00011111
            value |= value >> 01;
            value |= value >> 02;
            value |= value >> 04;
            value |= value >> 08;
            value |= value >> 16;

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
                ref MemoryMarshal.GetReference(Log2DeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
        }
#endif
    }
}
