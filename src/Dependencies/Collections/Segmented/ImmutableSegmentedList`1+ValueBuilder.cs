// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
                this._list = list;
                this._mutableList = null;
            }

            public readonly int Count => this.ReadOnlyList.Count;

            private readonly SegmentedList<T> ReadOnlyList
            {
                get
                {
                    var self = this;

                    return self._mutableList ?? self._list._list;
                }
            }
            readonly bool ICollection<T>.IsReadOnly => false;

            readonly bool IList.IsFixedSize => false;

            readonly bool IList.IsReadOnly => false;

            readonly bool ICollection.IsSynchronized => false;

            readonly object ICollection.SyncRoot => throw new NotSupportedException();

            public T this[int index]
            {
                readonly get => this.ReadOnlyList[index];
                set => this.GetOrCreateMutableList()[index] = value;
            }

            object? IList.this[int index]
            {
                readonly get => ((IList)this.ReadOnlyList)[index];
                set => ((IList)this.GetOrCreateMutableList())[index] = value;
            }

            public readonly ref readonly T ItemRef(int index)
            {
                var self = this;
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)self.ReadOnlyList.Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                }

                return ref self.ReadOnlyList._items[index];
            }

            private SegmentedList<T> GetOrCreateMutableList()
            {
                var self = this;
                if (self._mutableList is null)
                {
                    var originalList = RoslynImmutableInterlocked.InterlockedExchange(ref self._list, default);
                    if (originalList.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {self.GetType()}");

                    self._mutableList = new SegmentedList<T>(originalList._list);
                }
                this = self;

                return self._mutableList;
            }

            public void Add(T item)
                => this.GetOrCreateMutableList().Add(item);

            public void AddRange(IEnumerable<T> items)
            {
                if (items is null)
                    throw new ArgumentNullException(nameof(items));

                this.GetOrCreateMutableList().AddRange(items);
            }

            public readonly int BinarySearch(T item)
                => this.ReadOnlyList.BinarySearch(item);

            public readonly int BinarySearch(T item, IComparer<T>? comparer)
                => this.ReadOnlyList.BinarySearch(item, comparer);

            public readonly int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
                => this.ReadOnlyList.BinarySearch(index, count, item, comparer);

            public void Clear()
            {
                var self = this;
                if (self.ReadOnlyList.Count != 0)
                {
                    if (self._mutableList is null)
                    {
                        self._mutableList = new SegmentedList<T>();
                        self._list = default;
                    }
                    else
                    {
                        self._mutableList.Clear();
                    }
                }

                this = self;
            }

            public readonly bool Contains(T item)
                => this.ReadOnlyList.Contains(item);

            public readonly ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
                => new ImmutableSegmentedList<TOutput>(this.ReadOnlyList.ConvertAll(converter));

            public readonly void CopyTo(T[] array)
                => this.ReadOnlyList.CopyTo(array);

            public readonly void CopyTo(T[] array, int arrayIndex)
                => this.ReadOnlyList.CopyTo(array, arrayIndex);

            public readonly void CopyTo(int index, T[] array, int arrayIndex, int count)
                => this.ReadOnlyList.CopyTo(index, array, arrayIndex, count);

            public readonly bool Exists(Predicate<T> match)
                => this.ReadOnlyList.Exists(match);

            public readonly T? Find(Predicate<T> match)
                => this.ReadOnlyList.Find(match);

            public readonly ImmutableSegmentedList<T> FindAll(Predicate<T> match)
                => new ImmutableSegmentedList<T>(this.ReadOnlyList.FindAll(match));

            public readonly int FindIndex(Predicate<T> match)
                => this.ReadOnlyList.FindIndex(match);

            public readonly int FindIndex(int startIndex, Predicate<T> match)
                => this.ReadOnlyList.FindIndex(startIndex, match);

            public readonly int FindIndex(int startIndex, int count, Predicate<T> match)
                => this.ReadOnlyList.FindIndex(startIndex, count, match);

            public readonly T? FindLast(Predicate<T> match)
                => this.ReadOnlyList.FindLast(match);

            public readonly int FindLastIndex(Predicate<T> match)
                => this.ReadOnlyList.FindLastIndex(match);

            public readonly int FindLastIndex(int startIndex, Predicate<T> match)
            {
                var self = this;
                if (startIndex == 0 && self.Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return self.ReadOnlyList.FindLastIndex(startIndex, match);
            }

            public readonly int FindLastIndex(int startIndex, int count, Predicate<T> match)
            {
                var self = this;
                if (count == 0 && startIndex == 0 && self.Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return self.ReadOnlyList.FindLastIndex(startIndex, count, match);
            }

            public readonly void ForEach(Action<T> action)
                => this.ReadOnlyList.ForEach(action);

            public Enumerator GetEnumerator()
                => new Enumerator(this.GetOrCreateMutableList());

            public ImmutableSegmentedList<T> GetRange(int index, int count)
            {
                var self = this;
                if (index == 0 && count == self.Count)
                    return self.ToImmutable();

                return new ImmutableSegmentedList<T>(self.ReadOnlyList.GetRange(index, count));
            }

            public readonly int IndexOf(T item)
                => this.ReadOnlyList.IndexOf(item);

            public readonly int IndexOf(T item, int index)
                => this.ReadOnlyList.IndexOf(item, index);

            public readonly int IndexOf(T item, int index, int count)
                => this.ReadOnlyList.IndexOf(item, index, count);

            public readonly int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
                => this.ReadOnlyList.IndexOf(item, index, count, equalityComparer);

            public void Insert(int index, T item)
                => this.GetOrCreateMutableList().Insert(index, item);

            public void InsertRange(int index, IEnumerable<T> items)
                => this.GetOrCreateMutableList().InsertRange(index, items);

            public readonly int LastIndexOf(T item)
                => this.ReadOnlyList.LastIndexOf(item);

            public readonly int LastIndexOf(T item, int startIndex)
            {
                var self = this;
                if (startIndex == 0 && self.Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return self.ReadOnlyList.LastIndexOf(item, startIndex);
            }

            public readonly int LastIndexOf(T item, int startIndex, int count)
            {
                var self = this;
                if (count == 0 && startIndex == 0 && self.Count == 0)
                {
                    // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                    // Handle it explicitly to avoid an exception.
                    return -1;
                }

                return self.ReadOnlyList.LastIndexOf(item, startIndex, count);
            }

            public readonly int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
            {
                var self = this;
                if (startIndex < 0)
                    ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
                if (count < 0 || count > self.Count)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
                if (startIndex - count + 1 < 0)
                    throw new ArgumentException();

                return self.ReadOnlyList.LastIndexOf(item, startIndex, count, equalityComparer);
            }

            public bool Remove(T item)
            {
                var self = this;
                if (self._mutableList is null)
                {
                    var index = self.IndexOf(item);
                    if (index < 0)
                        return false;

                    self.RemoveAt(index);
                    return true;
                }
                else
                {
                    return self._mutableList.Remove(item);
                }
            }

            public int RemoveAll(Predicate<T> match)
                => this.GetOrCreateMutableList().RemoveAll(match);

            public void RemoveAt(int index)
                => this.GetOrCreateMutableList().RemoveAt(index);

            public void RemoveRange(int index, int count)
                => this.GetOrCreateMutableList().RemoveRange(index, count);

            public void Reverse()
            {
                var self = this;
                if (self.Count < 2)
                    return;

                self.GetOrCreateMutableList().Reverse();
                this = self;
            }

            public void Reverse(int index, int count)
                => this.GetOrCreateMutableList().Reverse(index, count);

            public void Sort()
            {
                var self = this;
                if (self.Count < 2)
                    return;

                self.GetOrCreateMutableList().Sort();
                this = self;
            }

            public void Sort(IComparer<T>? comparer)
            {
                var self = this;
                if (self.Count < 2)
                    return;

                self.GetOrCreateMutableList().Sort(comparer);
                this = self;
            }

            public void Sort(Comparison<T> comparison)
            {
                var self = this;
                if (comparison == null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
                }

                if (self.Count < 2)
                    return;

                self.GetOrCreateMutableList().Sort(comparison);

                this = self;
            }

            public void Sort(int index, int count, IComparer<T>? comparer)
                => this.GetOrCreateMutableList().Sort(index, count, comparer);

            public ImmutableSegmentedList<T> ToImmutable()
            {
                var self = this;
                self._list = new ImmutableSegmentedList<T>(self.ReadOnlyList);
                self._mutableList = null;
                this = self;
                return self._list;
            }

            public readonly bool TrueForAll(Predicate<T> match)
                => this.ReadOnlyList.TrueForAll(match);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => this.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => this.GetEnumerator();

            int IList.Add(object? value)
                => ((IList)this.GetOrCreateMutableList()).Add(value);

            readonly bool IList.Contains(object? value)
                => ((IList)this.ReadOnlyList).Contains(value);

            readonly int IList.IndexOf(object? value)
                => ((IList)this.ReadOnlyList).IndexOf(value);

            void IList.Insert(int index, object? value)
                => ((IList)this.GetOrCreateMutableList()).Insert(index, value);

            void IList.Remove(object? value)
                => ((IList)this.GetOrCreateMutableList()).Remove(value);

            readonly void ICollection.CopyTo(Array array, int index)
                => ((ICollection)this.ReadOnlyList).CopyTo(array, index);
        }
    }
}
