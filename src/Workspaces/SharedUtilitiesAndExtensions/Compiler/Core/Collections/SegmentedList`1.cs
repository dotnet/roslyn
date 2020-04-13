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
        private readonly int _segmentSize;
        private readonly int _segmentShift;
        private readonly int _offsetMask;

        private int _capacity;
        private int _count;
        private T[][] _items;

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

            _segmentSize = segmentSize;
            _offsetMask = segmentSize - 1;
            _segmentShift = 0;

            while (0 != (segmentSize >>= 1))
            {
                _segmentShift++;
            }

            if (initialCapacity > 0)
            {
                initialCapacity = _segmentSize * ((initialCapacity + _segmentSize - 1) / _segmentSize);
                _items = new T[initialCapacity >> _segmentShift][];
                for (var i = 0; i < _items.Length; i++)
                {
                    _items[i] = new T[_segmentSize];
                }

                _capacity = initialCapacity;
            }
        }

        /// <summary>
        /// Returns the count of elements in the list.
        /// </summary>
        public int Count
        {
            get { return _count; }
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
            if (_count == 0)
            {
                throw new InvalidOperationException("Attempting to remove an element from empty collection.");
            }

            var oldSegmentIndex = --_count >> _segmentShift;
            var result = _items[oldSegmentIndex][_count & _offsetMask];

            var newSegmentIndex = (_count - 1) >> _segmentShift;

            if (newSegmentIndex != oldSegmentIndex)
            {
                _items[oldSegmentIndex] = null;
                _capacity -= _segmentSize;
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
                return _items[index >> _segmentShift][index & _offsetMask];
            }

            set
            {
                _items[index >> _segmentShift][index & _offsetMask] = value;
            }
        }

        /// <summary>
        /// Necessary if the list is being used as an array since it creates the segments lazily.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>true if the segment is allocated and false otherwise</returns>
        public bool IsValidIndex(int index)
        {
            return _items[index >> _segmentShift] != null;
        }

        /// <summary>
        /// Get slot of an element
        /// </summary>
        /// <param name="index"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public T[] GetSlot(int index, out int slot)
        {
            slot = index & _offsetMask;
            return _items[index >> _segmentShift];
        }

        /// <summary>
        /// Adds new element at the end of the list.
        /// </summary>
        /// <param name="item">New element.</param>
        public void Add(T item)
        {
            if (_count == _capacity)
            {
                EnsureCapacity(_count + 1);
            }

            _items[_count >> _segmentShift][_count & _offsetMask] = item;
            _count++;
        }

        /// <summary>
        /// Inserts new element at the given position in the list.
        /// </summary>
        /// <param name="index">Insert position.</param>
        /// <param name="item">New element to insert.</param>
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if (_count == _capacity)
            {
                EnsureCapacity(_count + 1);
            }

            if (index < _count)
            {
                AddRoomForElement(index);
            }

            if (index >= _capacity)
            {
                _count = index;
                EnsureCapacity(_count + 1);
            }

            _count++;

            _items[index >> _segmentShift][index & _offsetMask] = item;
        }

        /// <summary>
        /// Removes element at the given position in the list.
        /// </summary>
        /// <param name="index">Position of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < _count)
            {
                RemoveRoomForElement(index);
            }

            _count--;
            _items[_count >> _segmentShift][_count & _offsetMask] = default;
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
            return BinarySearch(item, 0, _count - 1, comparer);
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

            if (high < 0 || high >= _count)
            {
                throw new ArgumentOutOfRangeException($"High index, with value {high}, must not be negative and cannot be greater than the number of elements contained in the list, which is {_count}.");
            }

            while (low <= high)
            {
                var i = low + ((high - low) >> 1);
                var order = comparer.Compare(_items[i >> _segmentShift][i & _offsetMask], item);

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
            Sort(Comparer<T>.Default);
        }

        /// <summary>
        /// Sorts the list using specified comparer for elements.
        /// </summary>
        /// <param name="comparer">Comparer to use.</param>
        public void Sort(IComparer<T> comparer)
        {
            if (_count <= 1)
            {
                return;
            }

            QuickSort(0, _count - 1, comparer);
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
                var minCapacity = _count + count;

                if (_capacity < minCapacity)
                {
                    EnsureCapacity(minCapacity);
                }

                do
                {
                    var sourceSegment = index / from._segmentSize;
                    var sourceOffset = index % from._segmentSize;
                    var sourceLength = from._segmentSize - sourceOffset;
                    var targetSegment = _count >> _segmentShift;
                    var targetOffset = _count & _offsetMask;
                    var targetLength = _segmentSize - targetOffset;
                    var countToCopy = Math.Min(count, Math.Min(sourceLength, targetLength));

                    Array.Copy(from._items[sourceSegment], sourceOffset, _items[targetSegment], targetOffset, countToCopy);

                    index += countToCopy;
                    count -= countToCopy;
                    _count += countToCopy;
                }
                while (count != 0);
            }
        }

        public void AppendFrom(T[] from, int index, int count)
        {
            if (count > 0)
            {
                var minCapacity = _count + count;

                if (_capacity < minCapacity)
                {
                    EnsureCapacity(minCapacity);
                }

                do
                {
                    var targetSegment = _count >> _segmentShift;
                    var targetOffset = _count & _offsetMask;
                    var targetLength = _segmentSize - targetOffset;
                    var countToCopy = Math.Min(count, targetLength);

                    Array.Copy(from, index, _items[targetSegment], targetOffset, countToCopy);

                    index += countToCopy;
                    count -= countToCopy;
                    _count += countToCopy;
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
            var data = new T[_count];

            CopyTo(data, 0);

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
            var remain = _count;

            for (var i = 0; (remain > 0) && (i < _items.Length); i++)
            {
                var len = Math.Min(remain, _items[i].Length);

                Array.Copy(_items[i], 0, array, arrayIndex, len);

                remain -= len;
                arrayIndex += len;
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
            var remain = Math.Min(_count, endIndex - startIndex + 1);
            var firstSegmentIndex = startIndex / _segmentSize;
            var lastSegmentIndex = Math.Min(endIndex / _segmentSize, _items.Length); // The list might not have the range specified, we limit it if necessary to the actual size
            var segmentStartIndex = startIndex % _segmentSize;

            for (var i = firstSegmentIndex; (remain > 0) && (i <= lastSegmentIndex); i++)
            {
                var len = Math.Min(remain, _items[i].Length - segmentStartIndex);

                Array.Copy(_items[i], segmentStartIndex, array, arrayIndex, len);

                remain -= len;
                arrayIndex += len;
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
            _items = null;
            _count = 0;
            _capacity = 0;
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
            CopyTo(array, arrayIndex);
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
            var firstSegment = index >> _segmentShift;
            var lastSegment = _count >> _segmentShift;
            var firstOffset = index & _offsetMask;
            var lastOffset = _count & _offsetMask;

            if (firstSegment == lastSegment)
            {
                Array.Copy(_items[firstSegment], firstOffset, _items[firstSegment], firstOffset + 1, lastOffset - firstOffset);
            }
            else
            {
                var save = _items[firstSegment][_segmentSize - 1];
                Array.Copy(_items[firstSegment],
                    firstOffset, _items[firstSegment],
                    firstOffset + 1,
                    _segmentSize - firstOffset - 1);

                for (var segment = firstSegment + 1; segment < lastSegment; segment++)
                {
                    var saveT = _items[segment][_segmentSize - 1];
                    Array.Copy(_items[segment], 0, _items[segment], 1, _segmentSize - 1);
                    _items[segment][0] = save;
                    save = saveT;
                }

                Array.Copy(_items[lastSegment], 0, _items[lastSegment], 1, lastOffset);
                _items[lastSegment][0] = save;
            }
        }

        /// <summary>
        /// Shifts the tail of the list to remove the element.
        /// </summary>
        /// <param name="index">Index of the removed element.</param>
        private void RemoveRoomForElement(int index)
        {
            var firstSegment = index >> _segmentShift;
            var lastSegment = (_count - 1) >> _segmentShift;
            var firstOffset = index & _offsetMask;
            var lastOffset = (_count - 1) & _offsetMask;

            if (firstSegment == lastSegment)
            {
                Array.Copy(_items[firstSegment], firstOffset + 1, _items[firstSegment], firstOffset, lastOffset - firstOffset);
            }
            else
            {
                Array.Copy(_items[firstSegment], firstOffset + 1, _items[firstSegment], firstOffset, _segmentSize - firstOffset - 1);

                for (var segment = firstSegment + 1; segment < lastSegment; segment++)
                {
                    _items[segment - 1][_segmentSize - 1] = _items[segment][0];
                    Array.Copy(_items[segment], 1, _items[segment], 0, _segmentSize - 1);
                }

                _items[lastSegment - 1][_segmentSize - 1] = _items[lastSegment][0];
                Array.Copy(_items[lastSegment], 1, _items[lastSegment], 0, lastOffset);
            }
        }

        /// <summary>
        /// Ensures that we have enough capacity for the given number of elements.
        /// </summary>
        /// <param name="minCapacity">Number of elements.</param>
        private void EnsureCapacity(int minCapacity)
        {
            if (_capacity < _segmentSize)
            {
                if (_items == null)
                {
                    _items = new T[(minCapacity + _segmentSize - 1) >> _segmentShift][];
                }

                var newFirstSegmentCapacity = _segmentSize;

                if (minCapacity < _segmentSize)
                {
                    newFirstSegmentCapacity = _capacity == 0 ? 2 : _capacity * 2;

                    while (newFirstSegmentCapacity < minCapacity)
                    {
                        newFirstSegmentCapacity *= 2;
                    }

                    newFirstSegmentCapacity = Math.Min(newFirstSegmentCapacity, _segmentSize);
                }

                var newFirstSegment = new T[newFirstSegmentCapacity];

                if (_count > 0)
                {
                    Array.Copy(_items[0], 0, newFirstSegment, 0, _count);
                }

                _items[0] = newFirstSegment;
                _capacity = newFirstSegment.Length;
            }

            if (_capacity < minCapacity)
            {
                var currentSegments = _capacity >> _segmentShift;
                var neededSegments = minCapacity + _segmentSize - 1 >> _segmentShift;

                if (neededSegments > _items.Length)
                {
                    var newSegmentArrayCapacity = _items.Length * 2;

                    while (newSegmentArrayCapacity < neededSegments)
                    {
                        newSegmentArrayCapacity *= 2;
                    }

                    var newItems = new T[newSegmentArrayCapacity][];
                    Array.Copy(_items, 0, newItems, 0, currentSegments);
                    _items = newItems;
                }

                for (var i = currentSegments; i < neededSegments; i++)
                {
                    _items[i] = new T[_segmentSize];
                    _capacity += _segmentSize;
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
                if (comparer.Compare(_items[a >> _segmentShift][a & _offsetMask], _items[b >> _segmentShift][b & _offsetMask]) > 0)
                {
                    var key = _items[a >> _segmentShift][a & _offsetMask];
                    _items[a >> _segmentShift][a & _offsetMask] = _items[b >> _segmentShift][b & _offsetMask];
                    _items[b >> _segmentShift][b & _offsetMask] = key;
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
                var i = left;
                var j = right;

                // pre-sort the low, middle (pivot), and high values in place.
                // this improves performance in the face of already sorted data, or
                // data that is made up of multiple sorted runs appended together.
                var middle = i + ((j - i) >> 1);

                SwapIfGreaterWithItems(comparer, i, middle); // swap the low with the mid point
                SwapIfGreaterWithItems(comparer, i, j); // swap the low with the high
                SwapIfGreaterWithItems(comparer, middle, j); // swap the middle with the high

                var x = _items[middle >> _segmentShift][middle & _offsetMask];

                do
                {
                    while (comparer.Compare(_items[i >> _segmentShift][i & _offsetMask], x) < 0)
                    {
                        i++;
                    }

                    while (comparer.Compare(x, _items[j >> _segmentShift][j & _offsetMask]) < 0)
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
                        var key = _items[i >> _segmentShift][i & _offsetMask];
                        _items[i >> _segmentShift][i & _offsetMask] = _items[j >> _segmentShift][j & _offsetMask];
                        _items[j >> _segmentShift][j & _offsetMask] = key;
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
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly SegmentedList<T> _list;
            private int _index;

            /// <summary>
            /// Constructws the Enumerator.
            /// </summary>
            /// <param name="list">List to enumerate.</param>
            internal Enumerator(SegmentedList<T> list)
            {
                _list = list;
                _index = -1;
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
                if (_index < _list._count - 1)
                {
                    _index++;
                    return true;
                }

                _index = -1;

                return false;
            }

            /// <summary>
            /// Returns the current element.
            /// </summary>
            public T Current
            {
                get { return _list[_index]; }
            }

            /// <summary>
            /// Returns the current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Resets the enumerator to initial state.
            /// </summary>
            void IEnumerator.Reset()
            {
                _index = -1;
            }
        }
    }
}
