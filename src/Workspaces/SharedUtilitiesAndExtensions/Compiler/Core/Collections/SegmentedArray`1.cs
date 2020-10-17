﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// Defines a fixed-size collection with the same API surface and behavior as an "SZArray", which is a
    /// single-dimensional zero-based array commonly represented in C# as <c>T[]</c>. The implementation of this
    /// collection uses segmented arrays to avoid placing objects on the Large Object Heap.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the array.</typeparam>
    internal readonly struct SegmentedArray<T> : ICloneable, IList, IStructuralComparable, IStructuralEquatable, IList<T>, IReadOnlyList<T>
    {
        /// <summary>
        /// The number of elements in each page of the segmented array of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// <para>The segment size is calculated according to <see cref="Unsafe.SizeOf{T}"/>, performs the IL operation
        /// defined by <see cref="OpCodes.Sizeof"/>. ECMA-335 defines this operation with the following note:</para>
        ///
        /// <para><c>sizeof</c> returns the total size that would be occupied by each element in an array of this type –
        /// including any padding the implementation chooses to add. Specifically, array elements lie <c>sizeof</c>
        /// bytes apart.</para>
        /// </remarks>
        private static readonly int s_segmentSize = SegmentedArrayHelper.CalculateSegmentSize(Unsafe.SizeOf<T>());

        /// <summary>
        /// The bit shift to apply to an array index to get the page index within <see cref="_items"/>.
        /// </summary>
        private static readonly int s_segmentShift = SegmentedArrayHelper.CalculateSegmentShift(s_segmentSize);

        /// <summary>
        /// The bit mask to apply to an array index to get the index within a page of <see cref="_items"/>.
        /// </summary>
        private static readonly int s_offsetMask = SegmentedArrayHelper.CalculateOffsetMask(s_segmentSize);

        private readonly int _length;
        private readonly T[][] _items;

        public SegmentedArray(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0)
            {
                _items = Array.Empty<T[]>();
                _length = 0;
            }
            else
            {
                _items = new T[(length + s_segmentSize - 1) >> s_segmentShift][];
                for (var i = 0; i < _items.Length - 1; i++)
                {
                    _items[i] = new T[s_segmentSize];
                }

                // Make sure the last page only contains the number of elements required for the desired length. This
                // collection is not resizeable so any additional padding would be a waste of space.
                //
                // Avoid using (length & s_offsetMask) because it doesn't handle a last page size of s_segmentSize.
                var lastPageSize = length - ((_items.Length - 1) << s_segmentShift);

                _items[^1] = new T[lastPageSize];
                _length = length;
            }
        }

        private SegmentedArray(int length, T[][] items)
        {
            _length = length;
            _items = items;
        }

        public bool IsFixedSize => true;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public int Length => _length;

        public object SyncRoot => _items;

        public ref T this[int index]
        {
            get
            {
                return ref _items[index >> s_segmentShift][index & s_offsetMask];
            }
        }

        int ICollection.Count => Length;

        int ICollection<T>.Count => Length;

        int IReadOnlyCollection<T>.Count => Length;

        T IReadOnlyList<T>.this[int index] => this[index];

        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value!;
        }

        public object Clone()
        {
            var items = (T[][])_items.Clone();
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = (T[])items[i].Clone();
            }

            return new SegmentedArray<T>(Length, items);
        }

        public void CopyTo(Array array, int index)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                _items[i].CopyTo(array, index + (i * s_segmentSize));
            }
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                ICollection<T> collection = _items[i];
                collection.CopyTo(array, arrayIndex + (i * s_segmentSize));
            }
        }

        public Enumerator GetEnumerator()
            => new(this);

        int IList.Add(object? value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void ICollection<T>.Add(T value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void IList.Clear()
        {
            // Matches System.Array
            // https://github.com/dotnet/runtime/blob/e0ec035994179e8ebd6ccf081711ee11d4c5491b/src/libraries/System.Private.CoreLib/src/System/Array.cs#L279-L282
            foreach (IList list in _items)
            {
                list.Clear();
            }
        }

        void ICollection<T>.Clear()
        {
            // Matches `((ICollection<T>)new T[1]).Clear()`
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        bool IList.Contains(object? value)
        {
            foreach (IList list in _items)
            {
                if (list.Contains(value))
                    return true;
            }

            return false;
        }

        bool ICollection<T>.Contains(T value)
        {
            foreach (ICollection<T> collection in _items)
            {
                if (collection.Contains(value))
                    return true;
            }

            return false;
        }

        int IList.IndexOf(object? value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                IList list = _items[i];
                var index = list.IndexOf(value);
                if (index >= 0)
                {
                    return index + i * s_segmentSize;
                }
            }

            return -1;
        }

        int IList<T>.IndexOf(T value)
        {
            for (var i = 0; i < _items.Length; i++)
            {
                IList<T> list = _items[i];
                var index = list.IndexOf(value);
                if (index >= 0)
                {
                    return index + i * s_segmentSize;
                }
            }

            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void IList<T>.Insert(int index, T value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void IList.Remove(object? value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        bool ICollection<T>.Remove(T value)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException(CompilerExtensionsResources.NotSupported_FixedSizeCollection);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        int IStructuralComparable.CompareTo(object? other, IComparer comparer)
        {
            if (other is null)
                return 1;

            // Matches System.Array
            // https://github.com/dotnet/runtime/blob/e0ec035994179e8ebd6ccf081711ee11d4c5491b/src/libraries/System.Private.CoreLib/src/System/Array.cs#L320-L323
            if (!(other is SegmentedArray<T> o)
                || Length != o.Length)
            {
                throw new ArgumentException(CompilerExtensionsResources.ArgumentException_OtherNotArrayOfCorrectLength, nameof(other));
            }

            for (var i = 0; i < Length; i++)
            {
                var result = comparer.Compare(this[i], o[i]);
                if (result != 0)
                    return result;
            }

            return 0;
        }

        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
        {
            if (other is null)
                return false;

            if (!(other is SegmentedArray<T> o))
                return false;

            if ((object)_items == o._items)
                return true;

            if (Length != o.Length)
                return false;

            for (var i = 0; i < Length; i++)
            {
                if (!comparer.Equals(this[i], o[i]))
                    return false;
            }

            return true;
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            _ = comparer ?? throw new ArgumentNullException(nameof(comparer));

            // Matches System.Array
            // https://github.com/dotnet/runtime/blob/e0ec035994179e8ebd6ccf081711ee11d4c5491b/src/libraries/System.Private.CoreLib/src/System/Array.cs#L380-L383
            var ret = 0;
            for (var i = Length >= 8 ? Length - 8 : 0; i < Length; i++)
            {
                ret = Hash.Combine(comparer.GetHashCode(this[i]!), ret);
            }

            return ret;
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[][] _items;
            private int _nextItemSegment;
            private int _nextItemIndex;
            private T _current;

            public Enumerator(SegmentedArray<T> array)
            {
                _items = array._items;
                _nextItemSegment = 0;
                _nextItemIndex = 0;
                _current = default!;
            }

            public T Current => _current;
            object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_items.Length == 0)
                    return false;

                if (_nextItemIndex == _items[_nextItemSegment].Length)
                {
                    if (_nextItemSegment == _items.Length - 1)
                    {
                        return false;
                    }

                    _nextItemSegment++;
                    _nextItemIndex = 0;
                }

                _current = _items[_nextItemSegment][_nextItemIndex];
                _nextItemIndex++;
                return true;
            }

            public void Reset()
            {
                _nextItemSegment = 0;
                _nextItemIndex = 0;
                _current = default!;
            }
        }

        internal readonly struct TestAccessor
        {
            private readonly SegmentedArray<T> _array;

            public TestAccessor(SegmentedArray<T> array)
            {
                _array = array;
            }

            public static int SegmentSize => s_segmentSize;

            public T[][] Items => _array._items;
        }
    }
}
