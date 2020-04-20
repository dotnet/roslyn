// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// Implements a variable-size list that uses a <see cref="SegmentedArray{T}"/> of objects to store the elements. A
    /// <see cref="SegmentedList{T}"/> has a capacity, which is the allocated length of the internal segmented array. As
    /// elements are added to a segmented list, the capacity of the list is automatically increased as required by
    /// reallocating the internal array.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the list.</typeparam>
    internal class SegmentedList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        private const int DefaultCapacity = 4;

        internal SegmentedArray<T> _items;
        internal int _size;
        private int _version;

        private static readonly SegmentedArray<T> s_emptyArray = new SegmentedArray<T>(0);

        /// <summary>
        /// Constructs a segmented list. The list is initially empty and has a capacity of zero. Upon adding the first
        /// element to the list the capacity is increased to <see cref="DefaultCapacity"/>, and then increased in
        /// multiples of two as required.
        /// </summary>
        public SegmentedList()
        {
            _items = s_emptyArray;
        }

        /// <summary>
        /// Constructs a segmented list with a given initial capacity. The list is initially empty, but will have room
        /// for the given number of elements before any reallocations are required.
        /// </summary>
        /// <param name="capacity">The initial capacity of the list.</param>
        public SegmentedList(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            // SegmentedArray<T> does not allocate when capacity is 0, so no need to special-case it here.
            _items = new SegmentedArray<T>(capacity);
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        //
        public SegmentedList(IEnumerable<T> collection)
        {
            _ = collection ?? throw new ArgumentNullException(nameof(collection));

            if (collection is ICollection<T> c)
            {
                var count = c.Count;
                if (count == 0)
                {
                    _items = s_emptyArray;
                }
                else
                {
                    _items = new SegmentedArray<T>(count);
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = s_emptyArray;

                foreach (var value in collection)
                {
                    Add(value);
                }
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal
        // array of the list is reallocated to the given capacity.
        //
        public int Capacity
        {
            get
            {
                return _items.Length;
            }

            set
            {
                if (value < _size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), CompilerExtensionsResources.ArgumentOutOfRange_SmallCapacity);
                }

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        var newItems = new SegmentedArray<T>(value);
                        if (_size > 0)
                        {
                            SegmentedArray.Copy(_items, newItems, _size);
                        }

                        _items = newItems;
                    }
                    else
                    {
                        _items = s_emptyArray;
                    }
                }
            }
        }

        public int Count => _size;

        bool IList.IsFixedSize => false;

        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_Index);
                }

                return _items[index];
            }

            set
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_Index);
                }

                _items[index] = value;
                _version++;
            }
        }

        private static bool IsCompatibleObject(object? value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (value == null && default(T) == null);
        }

        private static bool IsReferenceOrContainsReferences()
        {
#if NETCOREAPP
            return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
            return !typeof(T).IsPrimitive;
#endif
        }

        object? IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (!(default(T) is null) && value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                try
                {
                    this[index] = (T)value!;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(string.Format(CompilerExtensionsResources.Arg_WrongType, value, typeof(T)), nameof(value));
                }
            }
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end of this list.
        /// </summary>
        /// <remarks>
        /// The size of the list is increased by one. If required, the capacity of the list is increased before adding
        /// the new element.
        /// </remarks>
        /// <param name="item">The item to add to the collection.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            _version++;
            var array = _items;
            var size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        // Non-inline from SegmentedList.Add to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            var size = _size;
            EnsureCapacity(size + 1);
            _size = size + 1;
            _items[size] = item;
        }

        int IList.Add(object? item)
        {
            if (!(default(T) is null) && item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                Add((T)item!);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(string.Format(CompilerExtensionsResources.Arg_WrongType, item, typeof(T)), nameof(item));
            }

            return Count - 1;
        }

        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
            => InsertRange(_size, collection);

        public ReadOnlyCollection<T> AsReadOnly()
            => new ReadOnlyCollection<T>(this);

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        //
        // The method uses the SegmentedArray.BinarySearch method to perform the
        // search.
        //
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            return SegmentedArray.BinarySearch(_items, index, count, item, comparer);
        }

        /// <inheritdoc cref="List{T}.BinarySearch(T)"/>
        public int BinarySearch(T item)
            => BinarySearch(0, Count, item, null);

        /// <inheritdoc cref="List{T}.BinarySearch(T, IComparer{T}?)"/>
        public int BinarySearch(T item, IComparer<T>? comparer)
            => BinarySearch(0, Count, item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _version++;
            if (IsReferenceOrContainsReferences())
            {
                var size = _size;
                _size = 0;
                if (size > 0)
                {
                    // Clear the elements so that the gc can reclaim the references.
                    SegmentedArray.Clear(_items, 0, size);
                }
            }
            else
            {
                _size = 0;
            }
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // EqualityComparer<T>.Default.Equals().
        //
        public bool Contains(T item)
        {
            // PERF: IndexOf calls Array.IndexOf via SegmentedArray.IndexOf,
            // which internally calls EqualityComparer<T>.Default.IndexOf,
            // which is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.IndexOf
            // for each page of the segmented array.

            return _size != 0 && IndexOf(item) != -1;
        }

        bool IList.Contains(object? item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T)item!);
            }

            return false;
        }

        /// <inheritdoc cref="List{T}.ConvertAll{TOutput}(Converter{T, TOutput})"/>
        public SegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            _ = converter ?? throw new ArgumentNullException(nameof(converter));

            var list = new SegmentedList<TOutput>(_size);
            for (var i = 0; i < _size; i++)
            {
                list._items[i] = converter(_items[i]);
            }

            list._size = _size;
            return list;
        }

        /// <inheritdoc cref="List{T}.CopyTo(T[])"/>
        public void CopyTo(T[] array)
            => CopyTo(array, 0);

        // Copies this SegmentedList into array, which must be of a
        // compatible array type.
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                throw new ArgumentException(CompilerExtensionsResources.Arg_RankMultiDimNotSupported);
            }

            try
            {
                // SegmentedArray.Copy will check for null.
                SegmentedArray.Copy(_items, 0, array!, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidArrayType);
            }
        }

        /// <inheritdoc cref="List{T}.CopyTo(int, T[], int, int)"/>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            // Delegate rest of error checking to SegmentedArray.Copy.
            SegmentedArray.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to SegmentedArray.Copy.
            SegmentedArray.Copy(_items, 0, array, arrayIndex, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the current capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        //
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                var newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;

                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > int.MaxValue)
                {
                    newCapacity = int.MaxValue;
                }

                if (newCapacity < min)
                {
                    newCapacity = min;
                }

                Capacity = newCapacity;
            }
        }

        /// <inheritdoc cref="List{T}.Exists(Predicate{T})"/>
        public bool Exists(Predicate<T> match)
            => FindIndex(match) != -1;

        /// <inheritdoc cref="List{T}.Find(Predicate{T})"/>
        [return: MaybeNull]
        public T Find(Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            for (var i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }

            return default!;
        }

        /// <inheritdoc cref="List{T}.FindAll(Predicate{T})"/>
        public SegmentedList<T> FindAll(Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            var list = new SegmentedList<T>();
            for (var i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    list.Add(_items[i]);
                }
            }

            return list;
        }

        /// <inheritdoc cref="List{T}.FindIndex(Predicate{T})"/>
        public int FindIndex(Predicate<T> match)
            => FindIndex(0, _size, match);

        /// <inheritdoc cref="List{T}.FindIndex(int, Predicate{T})"/>
        public int FindIndex(int startIndex, Predicate<T> match)
            => FindIndex(startIndex, _size - startIndex, match);

        /// <inheritdoc cref="List{T}.FindIndex(int, int, Predicate{T})"/>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), CompilerExtensionsResources.ArgumentOutOfRange_Index);
            }

            if (count < 0 || startIndex > _size - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_Count);
            }

            _ = match ?? throw new ArgumentNullException(nameof(match));

            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(_items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc cref="List{T}.FindLast(Predicate{T})"/>
        [return: MaybeNull]
        public T FindLast(Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            for (var i = _size - 1; i >= 0; i--)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }

            return default!;
        }

        /// <inheritdoc cref="List{T}.FindLastIndex(Predicate{T})"/>
        public int FindLastIndex(Predicate<T> match)
            => FindLastIndex(_size - 1, _size, match);

        /// <inheritdoc cref="List{T}.FindLastIndex(int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, Predicate<T> match)
            => FindLastIndex(startIndex, startIndex + 1, match);

        /// <inheritdoc cref="List{T}.FindLastIndex(int, int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            if (_size == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex), CompilerExtensionsResources.ArgumentOutOfRange_Index);
                }
            }
            else
            {
                // Make sure we're not out of range
                if ((uint)startIndex >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex), CompilerExtensionsResources.ArgumentOutOfRange_Index);
                }
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_Count);
            }

            var endIndex = startIndex - count;
            for (var i = startIndex; i > endIndex; i--)
            {
                if (match(_items[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc cref="List{T}.ForEach(Action{T})"/>
        public void ForEach(Action<T> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));

            var version = _version;

            for (var i = 0; i < _size; i++)
            {
                if (version != _version)
                {
                    break;
                }

                action(_items[i]);
            }

            if (version != _version)
            {
                throw new InvalidOperationException(CompilerExtensionsResources.InvalidOperation_EnumFailedVersion);
            }
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list
        // while an enumeration is in progress, the MoveNext and
        // GetObject methods of the enumerator will throw an exception.
        //
        /// <inheritdoc cref="List{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
            => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this);

        /// <inheritdoc cref="List{T}.GetRange(int, int)"/>
        public SegmentedList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            var list = new SegmentedList<T>(count);
            SegmentedArray.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        public int IndexOf(T item)
            => SegmentedArray.IndexOf(_items, item, 0, _size);

        int IList.IndexOf(object? item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item!);
            }

            return -1;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        /// <inheritdoc cref="List{T}.IndexOf(T, int)"/>
        public int IndexOf(T item, int index)
        {
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_Index);
            }

            return SegmentedArray.IndexOf(_items, item, index, _size - index);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.IndexOf method to perform the
        // search.
        //
        /// <inheritdoc cref="List{T}.IndexOf(T, int, int)"/>
        public int IndexOf(T item, int index, int count)
        {
            if (index > _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_Index);
            }

            if (count < 0 || index > _size - count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_Count);
            }

            return SegmentedArray.IndexOf(_items, item, index, count);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        //
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_ListInsert);
            }

            if (_size == _items.Length)
            {
                EnsureCapacity(_size + 1);
            }

            if (index < _size)
            {
                SegmentedArray.Copy(_items, index, _items, index + 1, _size - index);
            }

            _items[index] = item;
            _size++;
            _version++;
        }

        void IList.Insert(int index, object? item)
        {
            if (!(default(T) is null) && item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                Insert(index, (T)item!);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(string.Format(CompilerExtensionsResources.Arg_WrongType, item, typeof(T)), nameof(item));
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        /// <inheritdoc cref="List{T}.InsertRange(int, IEnumerable{T})"/>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            _ = collection ?? throw new ArgumentNullException(nameof(collection));

            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (collection is ICollection<T> c)
            {
                var count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        SegmentedArray.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        SegmentedArray.Copy(_items, 0, _items, index, index);

                        // Copy last part of _items back to inserted location
                        SegmentedArray.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        c.CopyTo(_items, index);
                    }

                    _size += count;
                }
            }
            else
            {
                foreach (var item in collection)
                {
                    Insert(index++, item);
                }
            }

            _version++;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end
        // and ending at the first element in the list. The elements of the list
        // are compared to the given value using the Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        /// <inheritdoc cref="List{T}.LastIndexOf(T)"/>
        public int LastIndexOf(T item)
        {
            if (_size == 0)
            {
                // Special case for empty list
                return -1;
            }
            else
            {
                return LastIndexOf(item, _size - 1, _size);
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int)"/>
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        //
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        //
        /// <inheritdoc cref="List{T}.LastIndexOf(T, int, int)"/>
        public int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((Count != 0) && (count < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size == 0)
            {
                // Special case for empty list
                return -1;
            }

            if (index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_BiggerThanCollection);
            }

            if (count > index + 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_BiggerThanCollection);
            }

            return SegmentedArray.LastIndexOf(_items, item, index, count);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void IList.Remove(object? item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item!);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).
        public int RemoveAll(Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            var freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex]))
            {
                freeIndex++;
            }

            if (freeIndex >= _size)
            {
                return 0;
            }

            var current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current]))
                {
                    current++;
                }

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }

            if (IsReferenceOrContainsReferences())
            {
                SegmentedArray.Clear(_items, freeIndex, _size - freeIndex); // Clear the elements so that the gc can reclaim the references.
            }

            var result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_Index);
            }

            _size--;
            if (index < _size)
            {
                SegmentedArray.Copy(_items, index + 1, _items, index, _size - index);
            }
            if (IsReferenceOrContainsReferences())
            {
                _items[_size] = default!;
            }

            _version++;
        }

        // Removes a range of elements from this list.
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    SegmentedArray.Copy(_items, index + count, _items, index, _size - index);
                }

                _version++;
                if (IsReferenceOrContainsReferences())
                {
                    SegmentedArray.Clear(_items, _size, count);
                }
            }
        }

        // Reverses the elements in this list.
        public void Reverse()
            => Reverse(0, Count);

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        //
        public void Reverse(int index, int count)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            if (count > 1)
            {
                SegmentedArray.Reverse(_items, index, count);
            }

            _version++;
        }

        // Sorts the elements in this list.  Uses the default comparer and
        // Array.Sort.
        public void Sort()
            => Sort(0, Count, null);

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T>? comparer)
            => Sort(0, Count, comparer);

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        //
        // This method uses the Array.Sort method to sort the elements.
        //
        public void Sort(int index, int count, IComparer<T>? comparer)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), CompilerExtensionsResources.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                throw new ArgumentException(CompilerExtensionsResources.Argument_InvalidOffLen);
            }

            if (count > 1)
            {
                SegmentedArray.Sort(_items, index, count, comparer);
            }

            _version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            _ = comparison ?? throw new ArgumentNullException(nameof(comparison));

            if (_size > 1)
            {
                SegmentedArray.Sort(_items, comparison);
            }

            _version++;
        }

        // ToArray returns an array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray()
        {
            if (_size == 0)
            {
                return Array.Empty<T>();
            }

            var array = new T[_size];
            SegmentedArray.Copy(_items, array, _size);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        //
        // list.Clear();
        // list.TrimExcess();
        //
        public void TrimExcess()
        {
            var threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        public bool TrueForAll(Predicate<T> match)
        {
            _ = match ?? throw new ArgumentNullException(nameof(match));

            for (var i = 0; i < _size; i++)
            {
                if (!match(_items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly SegmentedList<T> _list;
            private int _index;
            private readonly int _version;
            [AllowNull, MaybeNull] private T _current;

            internal Enumerator(SegmentedList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var localList = _list;

                if (_version == localList._version && ((uint)_index < (uint)localList._size))
                {
                    _current = localList._items[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException(CompilerExtensionsResources.InvalidOperation_EnumFailedVersion);
                }

                _index = _list._size + 1;
                _current = default;
                return false;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                    {
                        throw new InvalidOperationException(CompilerExtensionsResources.InvalidOperation_EnumOpCantHappen);
                    }

                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException(CompilerExtensionsResources.InvalidOperation_EnumFailedVersion);
                }

                _index = 0;
                _current = default;
            }
        }
    }
}
