// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial struct ImmutableSegmentedList<T>
    {
        private struct ValueBuilder : IList<T>, IReadOnlyList<T>, IList
        {
            /// <summary>
            /// The immutable collection this builder is based on.
            /// </summary>
            private ImmutableSegmentedList<T> _list;

            /// <summary>
            /// The current mutable collection this builder is operating on. This field is initialized to a copy of
            /// <see cref="_list"/> the first time a change is made.
            /// </summary>
            private SegmentedList<T>? _mutableList;

            internal ValueBuilder(ImmutableSegmentedList<T> list)
            {
                _list = list;
                _mutableList = null;
            }

            public readonly int Count => ReadOnlyList.Count;

            private readonly SegmentedList<T> ReadOnlyList => _mutableList ?? _list._list;

            readonly bool ICollection<T>.IsReadOnly => false;

            readonly bool IList.IsFixedSize => false;

            readonly bool IList.IsReadOnly => false;

            readonly bool ICollection.IsSynchronized => false;

            readonly object ICollection.SyncRoot => throw new NotSupportedException();

            public T this[int index]
            {
                readonly get => ReadOnlyList[index];
                set => GetOrCreateMutableList()[index] = value;
            }

            object? IList.this[int index]
            {
                readonly get => ((IList)ReadOnlyList)[index];
                set => ((IList)GetOrCreateMutableList())[index] = value;
            }

            public readonly ref readonly T ItemRef(int index)
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)ReadOnlyList.Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                }

                return ref ReadOnlyList._items[index];
            }

            private SegmentedList<T> GetOrCreateMutableList()
            {
                if (_mutableList is null)
                {
                    var originalList = RoslynImmutableInterlocked.InterlockedExchange(ref _list, default);
                    if (originalList.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {GetType()}");

                    _mutableList = new SegmentedList<T>(originalList._list);
                }

                return _mutableList;
            }

            public void Add(T item)
                => GetOrCreateMutableList().Add(item);

            public void AddRange(IEnumerable<T> items)
            {
                if (items is null)
                    throw new ArgumentNullException(nameof(items));

                GetOrCreateMutableList().AddRange(items);
            }

            public readonly int BinarySearch(T item)
                => ReadOnlyList.BinarySearch(item);

            public readonly int BinarySearch(T item, IComparer<T>? comparer)
                => ReadOnlyList.BinarySearch(item, comparer);

            public readonly int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
                => ReadOnlyList.BinarySearch(index, count, item, comparer);

            public void Clear()
            {
                if (ReadOnlyList.Count != 0)
                {
                    if (_mutableList is null)
                    {
                        _mutableList = new SegmentedList<T>();
                        _list = default;
                    }
                    else
                    {
                        _mutableList.Clear();
                    }
                }
            }

            public readonly bool Contains(T item)
                => ReadOnlyList.Contains(item);

            public readonly ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
                => new ImmutableSegmentedList<TOutput>(ReadOnlyList.ConvertAll(converter));

            public readonly void CopyTo(T[] array)
                => ReadOnlyList.CopyTo(array);

            public readonly void CopyTo(T[] array, int arrayIndex)
                => ReadOnlyList.CopyTo(array, arrayIndex);

            public readonly void CopyTo(int index, T[] array, int arrayIndex, int count)
                => ReadOnlyList.CopyTo(index, array, arrayIndex, count);

            public readonly bool Exists(Predicate<T> match)
                => ReadOnlyList.Exists(match);

            public readonly T? Find(Predicate<T> match)
                => ReadOnlyList.Find(match);

            public readonly ImmutableSegmentedList<T> FindAll(Predicate<T> match)
                => new ImmutableSegmentedList<T>(ReadOnlyList.FindAll(match));

            public readonly int FindIndex(Predicate<T> match)
                => ReadOnlyList.FindIndex(match);

            public readonly int FindIndex(int startIndex, Predicate<T> match)
                => ReadOnlyList.FindIndex(startIndex, match);

            public readonly int FindIndex(int startIndex, int count, Predicate<T> match)
                => ReadOnlyList.FindIndex(startIndex, count, match);

            public readonly T? FindLast(Predicate<T> match)
                => ReadOnlyList.FindLast(match);

            public readonly int FindLastIndex(Predicate<T> match)
                => ReadOnlyList.FindLastIndex(match);

            public readonly int FindLastIndex(int startIndex, Predicate<T> match)
            {
                if (startIndex == 0 && Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return ReadOnlyList.FindLastIndex(startIndex, match);
            }

            public readonly int FindLastIndex(int startIndex, int count, Predicate<T> match)
            {
                if (count == 0 && startIndex == 0 && Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return ReadOnlyList.FindLastIndex(startIndex, count, match);
            }

            public readonly void ForEach(Action<T> action)
                => ReadOnlyList.ForEach(action);

            public Enumerator GetEnumerator()
                => new Enumerator(GetOrCreateMutableList());

            public ImmutableSegmentedList<T> GetRange(int index, int count)
            {
                if (index == 0 && count == Count)
                    return ToImmutable();

                return new ImmutableSegmentedList<T>(ReadOnlyList.GetRange(index, count));
            }

            public readonly int IndexOf(T item)
                => ReadOnlyList.IndexOf(item);

            public readonly int IndexOf(T item, int index)
                => ReadOnlyList.IndexOf(item, index);

            public readonly int IndexOf(T item, int index, int count)
                => ReadOnlyList.IndexOf(item, index, count);

            public readonly int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
                => ReadOnlyList.IndexOf(item, index, count, equalityComparer);

            public void Insert(int index, T item)
                => GetOrCreateMutableList().Insert(index, item);

            public void InsertRange(int index, IEnumerable<T> items)
                => GetOrCreateMutableList().InsertRange(index, items);

            public readonly int LastIndexOf(T item)
                => ReadOnlyList.LastIndexOf(item);

            public readonly int LastIndexOf(T item, int startIndex)
            {
                if (startIndex == 0 && Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return ReadOnlyList.LastIndexOf(item, startIndex);
            }

            public readonly int LastIndexOf(T item, int startIndex, int count)
            {
                if (count == 0 && startIndex == 0 && Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return ReadOnlyList.LastIndexOf(item, startIndex, count);
            }

            public readonly int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
            {
                if (startIndex < 0)
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                if (count < 0 || count > Count)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
                if (startIndex - count + 1 < 0)
                    throw new ArgumentException();

                return ReadOnlyList.LastIndexOf(item, startIndex, count, equalityComparer);
            }

            public bool Remove(T item)
            {
                if (_mutableList is null)
                {
                    var index = IndexOf(item);
                    if (index < 0)
                        return false;

                    RemoveAt(index);
                    return true;
                }
                else
                {
                    return _mutableList.Remove(item);
                }
            }

            public int RemoveAll(Predicate<T> match)
                => GetOrCreateMutableList().RemoveAll(match);

            public void RemoveAt(int index)
                => GetOrCreateMutableList().RemoveAt(index);

            public void RemoveRange(int index, int count)
                => GetOrCreateMutableList().RemoveRange(index, count);

            public void Reverse()
            {
                if (Count < 2)
                    return;

                GetOrCreateMutableList().Reverse();
            }

            public void Reverse(int index, int count)
                => GetOrCreateMutableList().Reverse(index, count);

            public void Sort()
            {
                if (Count < 2)
                    return;

                GetOrCreateMutableList().Sort();
            }

            public void Sort(IComparer<T>? comparer)
            {
                if (Count < 2)
                    return;

                GetOrCreateMutableList().Sort(comparer);
            }

            public void Sort(Comparison<T> comparison)
            {
                if (comparison == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
                }

                if (Count < 2)
                    return;

                GetOrCreateMutableList().Sort(comparison);
            }

            public void Sort(int index, int count, IComparer<T>? comparer)
                => GetOrCreateMutableList().Sort(index, count, comparer);

            public ImmutableSegmentedList<T> ToImmutable()
            {
                _list = new ImmutableSegmentedList<T>(ReadOnlyList);
                _mutableList = null;
                return _list;
            }

            public readonly bool TrueForAll(Predicate<T> match)
                => ReadOnlyList.TrueForAll(match);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            int IList.Add(object? value)
                => ((IList)GetOrCreateMutableList()).Add(value);

            readonly bool IList.Contains(object? value)
                => ((IList)ReadOnlyList).Contains(value);

            readonly int IList.IndexOf(object? value)
                => ((IList)ReadOnlyList).IndexOf(value);

            void IList.Insert(int index, object? value)
                => ((IList)GetOrCreateMutableList()).Insert(index, value);

            void IList.Remove(object? value)
                => ((IList)GetOrCreateMutableList()).Remove(value);

            readonly void ICollection.CopyTo(Array array, int index)
                => ((ICollection)ReadOnlyList).CopyTo(array, index);
        }
    }
}
