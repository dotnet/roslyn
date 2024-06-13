// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal static class ArrayExtensions
    {
        internal static T[] Copy<T>(this T[] array, int start, int length)
        {
            // It's ok for 'start' to equal 'array.Length'.  In that case you'll
            // just get an empty array back.
            Debug.Assert(start >= 0);
            Debug.Assert(start <= array.Length);

            if (start + length > array.Length)
            {
                length = array.Length - start;
            }

            T[] newArray = new T[length];
            Array.Copy(array, start, newArray, 0, length);
            return newArray;
        }

        public static int IndexOf<T>(this T[] array, T value)
            => Array.IndexOf(array, value);

        public static bool Contains<T>(this T[] array, T value)
            => Array.IndexOf(array, value) >= 0;

        internal static T[] InsertAt<T>(this T[] array, int position, T item)
        {
            T[] newArray = new T[array.Length + 1];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < array.Length)
            {
                Array.Copy(array, position, newArray, position + 1, array.Length - position);
            }

            newArray[position] = item;
            return newArray;
        }

        internal static T[] Append<T>(this T[] array, T item)
        {
            return InsertAt(array, array.Length, item);
        }

        internal static T[] InsertAt<T>(this T[] array, int position, T[] items)
        {
            T[] newArray = new T[array.Length + items.Length];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < array.Length)
            {
                Array.Copy(array, position, newArray, position + items.Length, array.Length - position);
            }

            items.CopyTo(newArray, position);
            return newArray;
        }

        internal static T[] Append<T>(this T[] array, T[] items)
        {
            return InsertAt(array, array.Length, items);
        }

        internal static T[] RemoveAt<T>(this T[] array, int position)
        {
            return RemoveAt(array, position, 1);
        }

        internal static T[] RemoveAt<T>(this T[] array, int position, int length)
        {
            if (position + length > array.Length)
            {
                length = array.Length - position;
            }

            T[] newArray = new T[array.Length - length];
            if (position > 0)
            {
                Array.Copy(array, newArray, position);
            }

            if (position < newArray.Length)
            {
                Array.Copy(array, position + length, newArray, position, newArray.Length - position);
            }

            return newArray;
        }

        internal static T[] ReplaceAt<T>(this T[] array, int position, T item)
        {
            T[] newArray = new T[array.Length];
            Array.Copy(array, newArray, array.Length);
            newArray[position] = item;
            return newArray;
        }

        internal static T[] ReplaceAt<T>(this T[] array, int position, int length, T[] items)
        {
            return InsertAt(RemoveAt(array, position, length), position, items);
        }

        internal static void ReverseContents<T>(this T[] array)
        {
            ReverseContents(array, 0, array.Length);
        }

        internal static void ReverseContents<T>(this T[] array, int start, int count)
        {
            int end = start + count - 1;
            for (int i = start, j = end; i < j; i++, j--)
            {
                T tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }

        // same as Array.BinarySearch, but without using IComparer to compare ints
        internal static int BinarySearch(this int[] array, int value)
        {
            var low = 0;
            var high = array.Length - 1;

            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var midValue = array[middle];

                if (midValue == value)
                {
                    return middle;
                }
                else if (midValue > value)
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

        public static bool SequenceEqual<T>(this T[]? first, T[]? second, Func<T, T, bool> comparer)
        {
            RoslynDebug.Assert(comparer != null);

            if (first == second)
            {
                return true;
            }

            if (first == null || second == null || first.Length != second.Length)
            {
                return false;
            }

            for (var i = 0; i < first.Length; i++)
            {
                if (!comparer(first[i], second[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Search a sorted integer array for the target value in O(log N) time.
        /// </summary>
        /// <param name="array">The array of integers which must be sorted in ascending order.</param>
        /// <param name="value">The target value.</param>
        /// <returns>An index in the array pointing to the position where <paramref name="value"/> should be
        /// inserted in order to maintain the sorted order. All values to the right of this position will be
        /// strictly greater than <paramref name="value"/>. Note that this may return a position off the end
        /// of the array if all elements are less than or equal to <paramref name="value"/>.</returns>
        internal static int BinarySearchUpperBound(this int[] array, int value)
        {
            int low = 0;
            int high = array.Length - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                if (array[middle] > value)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }
    }
}
