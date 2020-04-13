// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using IEnumerable = System.Collections.IEnumerable;
using IEnumerator = System.Collections.IEnumerator;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// Segmented list implementation, copied from Microsoft.Exchange.Collections.
    /// </summary>
    /// <typeparam name="T">The type of the list element.</typeparam>
    /// <remarks>
    /// This class implement a list which is allocated in segments, to avoid large lists to go into LOH.
    /// </remarks>
    internal sealed class SegmentedList<T> : ICollection<T>, IReadOnlyList<T>
    {
        private readonly int segmentSize;
        private readonly int segmentShift;
        private readonly int offsetMask;

        private int capacity;
        private int count;
        private T[][] items;

        /// <summary>
        /// Constructs SegmentedList.
        /// </summary>
        /// <param name="segmentSize">Segment size</param>
        public SegmentedList(int segmentSize)
            : this(segmentSize, 0)
        {
        }

        /// <summary>
        /// Constructs SegmentedList.
        /// </summary>
        /// <param name="segmentSize">Segment size</param>
        /// <param name="initialCapacity">Initial capacity</param>
        public SegmentedList(int segmentSize, int initialCapacity)
        {
            if (segmentSize <= 1 || (segmentSize & (segmentSize - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException("segment size must be power of 2 greater than 1");
            }

            this.segmentSize = segmentSize;
            this.offsetMask = segmentSize - 1;
            this.segmentShift = 0;

            while (0 != (segmentSize >>= 1))
            {
                this.segmentShift++;
            }

            if (initialCapacity > 0)
            {
                initialCapacity = this.segmentSize * ((initialCapacity + this.segmentSize - 1) / this.segmentSize);
                this.items = new T[initialCapacity >> this.segmentShift][];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = new T[this.segmentSize];
                }

                this.capacity = initialCapacity;
            }
        }

        /// <summary>
        /// Returns the count of elements in the list.
        /// </summary>
        public int Count
        {
            get { return (int)this.count; }
            set
            {
                Debug.Assert(value >= 0);
                this.count = value;
            }
        }

        /// <summary>
        /// Copy to Array
        /// </summary>
        /// <returns>Array copy</returns>
        public T[] UnderlyingArray => ToArray();

        /// <summary>
        /// Returns the last element on the list and removes it from it.
        /// </summary>
        /// <returns>The last element that was on the list.</returns>
        public T Pop()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Attempting to remove an element from empty collection.");
            }

            int oldSegmentIndex = --count >> segmentShift;
            T result = items[oldSegmentIndex][count & offsetMask];

            int newSegmentIndex = (count - 1) >> segmentShift;

            if (newSegmentIndex != oldSegmentIndex)
            {
                items[oldSegmentIndex] = null;
                capacity -= segmentSize;
            }

            return result;
        }

        /// <summary>
        /// Returns true if this ICollection is read-only.
        /// </summary>
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the given element in the list.
        /// </summary>
        /// <param name="index">Element index.</param>
        public T this[int index]
        {
            get
            {
                return this.items[index >> this.segmentShift][index & this.offsetMask];
            }

            set
            {
                this.items[index >> this.segmentShift][index & this.offsetMask] = value;
            }
        }

        /// <summary>
        /// Necessary if the list is being used as an array since it creates the segments lazily.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>true if the segment is allocated and false otherwise</returns>
        public bool IsValidIndex(int index)
        {
            return this.items[index >> this.segmentShift] != null;
        }

        /// <summary>
        /// Get slot of an element
        /// </summary>
        /// <param name="index"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public T[] GetSlot(int index, out int slot)
        {
            slot = index & this.offsetMask;
            return this.items[index >> this.segmentShift];
        }

        /// <summary>
        /// Adds new element at the end of the list.
        /// </summary>
        /// <param name="item">New element.</param>
        public void Add(T item)
        {
            if (this.count == this.capacity)
            {
                this.EnsureCapacity(this.count + 1);
            }

            this.items[this.count >> this.segmentShift][this.count & this.offsetMask] = item;
            this.count++;
        }

        /// <summary>
        /// Inserts new element at the given position in the list.
        /// </summary>
        /// <param name="index">Insert position.</param>
        /// <param name="item">New element to insert.</param>
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if (this.count == this.capacity)
            {
                this.EnsureCapacity(this.count + 1);
            }

            if (index < this.count)
            {
                this.AddRoomForElement(index);
            }

            if (index >= this.capacity)
            {
                this.count = index;
                this.EnsureCapacity(this.count + 1);
            }

            this.count++;

            this.items[index >> this.segmentShift][index & this.offsetMask] = item;
        }

        /// <summary>
        /// Removes element at the given position in the list.
        /// </summary>
        /// <param name="index">Position of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < this.count)
            {
                this.RemoveRoomForElement(index);
            }

            this.count--;
            this.items[this.count >> this.segmentShift][this.count & this.offsetMask] = default(T);
        }

        /// <summary>
        /// Performs a binary search in a sorted list.
        /// </summary>
        /// <param name="item">Element to search for.</param>
        /// <param name="comparer">Comparer to use.</param>
        /// <returns>Non-negative position of the element if found, negative binary complement of the position of the next element if not found.</returns>
        /// <remarks>The implementation was copied from CLR BinarySearch implementation.</remarks>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(item, 0, this.count - 1, comparer);
        }

        /// <summary>
        /// Performs a binary search in a sorted list.
        /// </summary>
        /// <param name="item">Element to search for.</param>
        /// <param name="low">The lowest index in which to search.</param>
        /// <param name="high">The highest index in which to search.</param>
        /// <param name="comparer">Comparer to use.</param>
        /// <returns>The index </returns>
        public int BinarySearch(T item, int low, int high, IComparer<T> comparer)
        {
            if (low < 0 || low > high)
            {
                throw new ArgumentOutOfRangeException($"Low index, with value {low}, must not be negative and cannot be greater than the high index, whose value is {high}.");
            }

            if (high < 0 || high >= count)
            {
                throw new ArgumentOutOfRangeException($"High index, with value {high}, must not be negative and cannot be greater than the number of elements contained in the list, which is {count}.");
            }

            while (low <= high)
            {
                int i = low + ((high - low) >> 1);
                int order = comparer.Compare(this.items[i >> this.segmentShift][i & this.offsetMask], item);

                if (order == 0)
                {
                    return i;
                }

                if (order < 0)
                {
                    low = i + 1;
                }
                else
                {
                    high = i - 1;
                }
            }

            return ~low;
        }

        /// <summary>
        /// Sorts the list using default comparer for elements.
        /// </summary>
        public void Sort()
        {
            this.Sort(Comparer<T>.Default);
        }

        /// <summary>
        /// Sorts the list using specified comparer for elements.
        /// </summary>
        /// <param name="comparer">Comparer to use.</param>
        public void Sort(IComparer<T> comparer)
        {
            if (this.count <= 1)
            {
                return;
            }

            this.QuickSort(0, this.count - 1, comparer);
        }

        /// <summary>
        /// Appends a range of elements from anothe list.
        /// </summary>
        /// <param name="from">Source list.</param>
        /// <param name="index">Start index in the source list.</param>
        /// <param name="count">Count of elements from the source list to append.</param>
        public void AppendFrom(SegmentedList<T> from, int index, int count)
        {
            if (count > 0)
            {
                int minCapacity = this.count + count;

                if (this.capacity < minCapacity)
                {
                    this.EnsureCapacity(minCapacity);
                }

                do
                {
                    int sourceSegment = index / from.segmentSize;
                    int sourceOffset = index % from.segmentSize;
                    int sourceLength = from.segmentSize - sourceOffset;
                    int targetSegment = this.count >> this.segmentShift;
                    int targetOffset = this.count & this.offsetMask;
                    int targetLength = this.segmentSize - targetOffset;
                    int countToCopy = Math.Min(count, Math.Min(sourceLength, targetLength));

                    Array.Copy(from.items[sourceSegment], sourceOffset, this.items[targetSegment], targetOffset, countToCopy);

                    index += countToCopy;
                    count -= countToCopy;
                    this.count += countToCopy;
                }
                while (count != 0);
            }
        }

        public void AppendFrom(T[] from, int index, int count)
        {
            if (count > 0)
            {
                int minCapacity = this.count + count;

                if (this.capacity < minCapacity)
                {
                    this.EnsureCapacity(minCapacity);
                }

                do
                {
                    int targetSegment = this.count >> this.segmentShift;
                    int targetOffset = this.count & this.offsetMask;
                    int targetLength = this.segmentSize - targetOffset;
                    int countToCopy = Math.Min(count, targetLength);

                    Array.Copy(from, index, this.items[targetSegment], targetOffset, countToCopy);

                    index += countToCopy;
                    count -= countToCopy;
                    this.count += countToCopy;
                }
                while (count != 0);
            }
        }

        /// <summary>
        /// Returns the enumerator.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Copy to Array
        /// </summary>
        /// <returns>Array copy</returns>
        public T[] ToArray()
        {
            T[] data = new T[this.count];

            this.CopyTo(data, 0);

            return data;
        }

        /// <summary>
        /// CopyTo copies a collection into an Array, starting at a particular
        /// index into the array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Destination array starting index.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            int remain = this.count;

            for (int i = 0; (remain > 0) && (i < this.items.Length); i++)
            {
                int len = Math.Min(remain, this.items[i].Length);

                Array.Copy(this.items[i], 0, array, arrayIndex, len);

                remain -= len;
                arrayIndex += (int)len;
            }
        }

        /// <summary>
        /// Copies the contents of the collection that are within a range into an Array, starting at a particular
        /// index into the array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Destination array starting index.</param>
        /// <param name="startIndex">The collection index from where the copying should start.</param>
        /// <param name="endIndex">The collection index where the copying should end.</param>
        public void CopyRangeTo(T[] array, int arrayIndex, int startIndex, int endIndex)
        {
            int remain = Math.Min(this.count, endIndex - startIndex + 1);
            int firstSegmentIndex = (int)(startIndex / segmentSize);
            int lastSegmentIndex = Math.Min((int)(endIndex / segmentSize), this.items.Length); // The list might not have the range specified, we limit it if necessary to the actual size
            int segmentStartIndex = (int)(startIndex % segmentSize);

            for (int i = firstSegmentIndex; (remain > 0) && (i <= lastSegmentIndex); i++)
            {
                int len = Math.Min(remain, this.items[i].Length - segmentStartIndex);

                Array.Copy(this.items[i], segmentStartIndex, array, arrayIndex, len);

                remain -= len;
                arrayIndex += (int)len;
                segmentStartIndex = 0;
            }
        }

        /// <summary>
        /// Returns the enumerator.
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns the enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Clears the list (removes all elements).
        /// </summary>
        void ICollection<T>.Clear()
        {
            Clear();
        }

        public void Clear()
        {
            items = null;
            count = 0;
            capacity = 0;
        }

        /// <summary>
        /// Check if ICollection contains the given element.
        /// </summary>
        /// <param name="item">Element to check.</param>
        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException("This method of ICollection is not implemented");
        }

        /// <summary>
        /// CopyTo copies a collection into an Array, starting at a particular
        /// index into the array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Destination array starting index.</param>
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            this.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the given element from this ICollection.
        /// </summary>
        /// <param name="item">Element to remove.</param>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException("This method of ICollection is not implemented");
        }

        /// <summary>
        /// Shifts the tail of the list to make room for a new inserted element.
        /// </summary>
        /// <param name="index">Index of a new inserted element.</param>
        private void AddRoomForElement(int index)
        {
            int firstSegment = (int)(index >> this.segmentShift);
            int lastSegment = (int)(this.count >> this.segmentShift);
            int firstOffset = index & this.offsetMask;
            int lastOffset = this.count & this.offsetMask;

            if (firstSegment == lastSegment)
            {
                Array.Copy(this.items[firstSegment], firstOffset, this.items[firstSegment], firstOffset + 1, lastOffset - firstOffset);
            }
            else
            {
                T save = this.items[firstSegment][this.segmentSize - 1];
                Array.Copy(this.items[firstSegment],
                    firstOffset, this.items[firstSegment],
                    firstOffset + 1,
                    this.segmentSize - firstOffset - 1);

                for (int segment = firstSegment + 1; segment < lastSegment; segment++)
                {
                    T saveT = this.items[segment][this.segmentSize - 1];
                    Array.Copy(this.items[segment], 0, this.items[segment], 1, this.segmentSize - 1);
                    this.items[segment][0] = save;
                    save = saveT;
                }

                Array.Copy(this.items[lastSegment], 0, this.items[lastSegment], 1, lastOffset);
                this.items[lastSegment][0] = save;
            }
        }

        /// <summary>
        /// Shifts the tail of the list to remove the element.
        /// </summary>
        /// <param name="index">Index of the removed element.</param>
        private void RemoveRoomForElement(int index)
        {
            int firstSegment = (int)(index >> this.segmentShift);
            int lastSegment = (int)((this.count - 1) >> this.segmentShift);
            int firstOffset = index & this.offsetMask;
            int lastOffset = (this.count - 1) & this.offsetMask;

            if (firstSegment == lastSegment)
            {
                Array.Copy(this.items[firstSegment], firstOffset + 1, this.items[firstSegment], firstOffset, lastOffset - firstOffset);
            }
            else
            {
                Array.Copy(this.items[firstSegment], firstOffset + 1, this.items[firstSegment], firstOffset, this.segmentSize - firstOffset - 1);

                for (int segment = firstSegment + 1; segment < lastSegment; segment++)
                {
                    this.items[segment - 1][this.segmentSize - 1] = this.items[segment][0];
                    Array.Copy(this.items[segment], 1, this.items[segment], 0, this.segmentSize - 1);
                }

                this.items[lastSegment - 1][this.segmentSize - 1] = this.items[lastSegment][0];
                Array.Copy(this.items[lastSegment], 1, this.items[lastSegment], 0, lastOffset);
            }
        }

        /// <summary>
        /// Ensures that we have enough capacity for the given number of elements.
        /// </summary>
        /// <param name="minCapacity">Number of elements.</param>
        private void EnsureCapacity(int minCapacity)
        {
            if (this.capacity < this.segmentSize)
            {
                if (this.items == null)
                {
                    this.items = new T[(minCapacity + this.segmentSize - 1) >> this.segmentShift][];
                }

                int newFirstSegmentCapacity = this.segmentSize;

                if (minCapacity < this.segmentSize)
                {
                    newFirstSegmentCapacity = this.capacity == 0 ? 2 : this.capacity * 2;

                    while (newFirstSegmentCapacity < minCapacity)
                    {
                        newFirstSegmentCapacity *= 2;
                    }

                    newFirstSegmentCapacity = Math.Min(newFirstSegmentCapacity, this.segmentSize);
                }

                T[] newFirstSegment = new T[newFirstSegmentCapacity];

                if (this.count > 0)
                {
                    Array.Copy(this.items[0], 0, newFirstSegment, 0, this.count);
                }

                this.items[0] = newFirstSegment;
                this.capacity = newFirstSegment.Length;
            }

            if (this.capacity < minCapacity)
            {
                int currentSegments = (int)this.capacity >> this.segmentShift;
                int neededSegments = (int)(minCapacity + this.segmentSize - 1) >> this.segmentShift;

                if (neededSegments > this.items.Length)
                {
                    int newSegmentArrayCapacity = this.items.Length * 2;

                    while (newSegmentArrayCapacity < neededSegments)
                    {
                        newSegmentArrayCapacity *= 2;
                    }

                    T[][] newItems = new T[newSegmentArrayCapacity][];
                    Array.Copy(this.items, 0, newItems, 0, currentSegments);
                    this.items = newItems;
                }

                for (int i = currentSegments; i < neededSegments; i++)
                {
                    this.items[i] = new T[this.segmentSize];
                    this.capacity += this.segmentSize;
                }
            }
        }

        /// <summary>
        /// Helper method for QuickSort.
        /// </summary>
        /// <param name="comparer">Comparer to use.</param>
        /// <param name="a">Position of the first element.</param>
        /// <param name="b">Position of the second element.</param>
        private void SwapIfGreaterWithItems(IComparer<T> comparer, int a, int b)
        {
            if (a != b)
            {
                if (comparer.Compare(this.items[a >> this.segmentShift][a & this.offsetMask], this.items[b >> this.segmentShift][b & this.offsetMask]) > 0)
                {
                    T key = this.items[a >> this.segmentShift][a & this.offsetMask];
                    this.items[a >> this.segmentShift][a & this.offsetMask] = this.items[b >> this.segmentShift][b & this.offsetMask];
                    this.items[b >> this.segmentShift][b & this.offsetMask] = key;
                }
            }
        }

        /// <summary>
        /// QuickSort implementation.
        /// </summary>
        /// <param name="left">left boundary.</param>
        /// <param name="right">right boundary.</param>
        /// <param name="comparer">Comparer to use.</param>
        /// <remarks>The implementation was copied from CLR QuickSort implementation.</remarks>
        private void QuickSort(int left, int right, IComparer<T> comparer)
        {
            do
            {
                int i = left;
                int j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or
                // data that is made up of multiple sorted runs appended together.
                int middle = i + ((j - i) >> 1);

                this.SwapIfGreaterWithItems(comparer, i, middle); // swap the low with the mid point
                this.SwapIfGreaterWithItems(comparer, i, j); // swap the low with the high
                this.SwapIfGreaterWithItems(comparer, middle, j); // swap the middle with the high

                T x = this.items[middle >> this.segmentShift][middle & this.offsetMask];

                do
                {
                    while (comparer.Compare(this.items[i >> this.segmentShift][i & this.offsetMask], x) < 0)
                    {
                        i++;
                    }

                    while (comparer.Compare(x, this.items[j >> this.segmentShift][j & this.offsetMask]) < 0)
                    {
                        j--;
                    }

                    Debug.Assert(i >= left && j <= right, "(i>=left && j<=right) Sort failed - Is your IComparer bogus?");

                    if (i > j)
                    {
                        break;
                    }

                    if (i < j)
                    {
                        T key = this.items[i >> this.segmentShift][i & this.offsetMask];
                        this.items[i >> this.segmentShift][i & this.offsetMask] = this.items[j >> this.segmentShift][j & this.offsetMask];
                        this.items[j >> this.segmentShift][j & this.offsetMask] = key;
                    }

                    i++;
                    j--;
                }
                while (i <= j);

                if (j - left <= right - i)
                {
                    if (left < j)
                    {
                        QuickSort(left, j, comparer);
                    }
                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        QuickSort(i, right, comparer);
                    }
                    right = j;
                }
            }
            while (left < right);
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enumerator over the segmented list.
        /// </summary>
        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private readonly SegmentedList<T> list;
            private int index;

            /// <summary>
            /// Constructws the Enumerator.
            /// </summary>
            /// <param name="list">List to enumerate.</param>
            internal Enumerator(SegmentedList<T> list)
            {
                this.list = list;
                this.index = -1;
            }

            /// <summary>
            /// Disposes the Enumerator.
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Moves to the nest element in the list.
            /// </summary>
            /// <returns>True if move successful, false if there are no more elements.</returns>
            public bool MoveNext()
            {
                if (this.index < this.list.count - 1)
                {
                    index++;
                    return true;
                }

                this.index = -1;

                return false;
            }

            /// <summary>
            /// Returns the current element.
            /// </summary>
            public T Current
            {
                get { return this.list[this.index]; }
            }

            /// <summary>
            /// Returns the current element.
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Resets the enumerator to initial state.
            /// </summary>
            void System.Collections.IEnumerator.Reset()
            {
                index = -1;
            }
        }
    }
}
