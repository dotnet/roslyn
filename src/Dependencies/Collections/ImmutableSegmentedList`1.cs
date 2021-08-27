// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// Represents a segmented list that is immutable; meaning it cannot be changed once it is created.
    /// </summary>
    /// <remarks>
    /// <para>There are different scenarios best for <see cref="ImmutableSegmentedList{T}"/> and others
    /// best for <see cref="ImmutableList{T}"/>.</para>
    ///
    /// <para>The following table summarizes the performance characteristics of
    /// <see cref="ImmutableSegmentedList{T}"/>:</para>
    /// 
    /// <list type="table">
    ///   <item>
    ///     <description>Operation</description>
    ///     <description><see cref="ImmutableSegmentedList{T}"/> Complexity</description>
    ///     <description><see cref="ImmutableList{T}"/> Complexity</description>
    ///     <description>Comments</description>
    ///   </item>
    ///   <item>
    ///     <description>Item</description>
    ///     <description>O(1)</description>
    ///     <description>O(log n)</description>
    ///     <description>Directly index into the underlying segmented list</description>
    ///   </item>
    ///   <item>
    ///     <description>Add()</description>
    ///     <description>Currently O(n), but could be O(1) with a relatively large constant</description>
    ///     <description>O(log n)</description>
    ///     <description>Currently requires creating a new segmented list, but could be modified to only clone the segments with changes</description>
    ///   </item>
    ///   <item>
    ///     <description>Insert()</description>
    ///     <description>O(n)</description>
    ///     <description>O(log n)</description>
    ///     <description>Requires creating a new segmented list and cloning all impacted segments</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>This type is backed by segmented arrays to avoid using the Large Object Heap without impacting algorithmic
    /// complexity.</para>
    /// </remarks>
    /// <typeparam name="T">The type of the value in the list.</typeparam>
    /// <devremarks>
    /// <para>This type has a documented contract of being exactly one reference-type field in size. Our own
    /// <see cref="RoslynImmutableInterlocked"/> class depends on it, as well as others externally.</para>
    ///
    /// <para><strong>IMPORTANT NOTICE FOR MAINTAINERS AND REVIEWERS:</strong></para>
    ///
    /// <para>This type should be thread-safe. As a struct, it cannot protect its own fields from being changed from one
    /// thread while its members are executing on other threads because structs can change <em>in place</em> simply by
    /// reassigning the field containing this struct. Therefore it is extremely important that <strong>⚠⚠ Every member
    /// should only dereference <c>this</c> ONCE ⚠⚠</strong>. If a member needs to reference the
    /// <see cref="_list"/> field, that counts as a dereference of <c>this</c>. Calling other instance members
    /// (properties or methods) also counts as dereferencing <c>this</c>. Any member that needs to use <c>this</c> more
    /// than once must instead assign <c>this</c> to a local variable and use that for the rest of the code instead.
    /// This effectively copies the one field in the struct to a local variable so that it is insulated from other
    /// threads.</para>
    /// </devremarks>
    internal readonly partial struct ImmutableSegmentedList<T> : IImmutableList<T>, IReadOnlyList<T>, IList<T>, IList, IEquatable<ImmutableSegmentedList<T>>
    {
        /// <inheritdoc cref="ImmutableList{T}.Empty"/>
        public static readonly ImmutableSegmentedList<T> Empty = new(new SegmentedList<T>());

        private readonly SegmentedList<T> _list;

        private ImmutableSegmentedList(SegmentedList<T> list)
            => _list = list;

        public int Count => _list.Count;

        public bool IsDefault => _list == null;

        /// <inheritdoc cref="ImmutableList{T}.IsEmpty"/>
        public bool IsEmpty => _list.Count == 0;

        bool ICollection<T>.IsReadOnly => true;

        bool IList.IsFixedSize => true;

        bool IList.IsReadOnly => true;

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => _list;

        public T this[int index] => _list[index];

        T IList<T>.this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException();
        }

        object? IList.this[int index]
        {
            get => _list[index];
            set => throw new NotSupportedException();
        }

        public static bool operator ==(ImmutableSegmentedList<T> left, ImmutableSegmentedList<T> right)
            => left.Equals(right);

        public static bool operator !=(ImmutableSegmentedList<T> left, ImmutableSegmentedList<T> right)
            => !left.Equals(right);

        public static bool operator ==(ImmutableSegmentedList<T>? left, ImmutableSegmentedList<T>? right)
            => left.GetValueOrDefault().Equals(right.GetValueOrDefault());

        public static bool operator !=(ImmutableSegmentedList<T>? left, ImmutableSegmentedList<T>? right)
            => !left.GetValueOrDefault().Equals(right.GetValueOrDefault());

        /// <inheritdoc cref="ImmutableList{T}.ItemRef(int)"/>
        public ref readonly T ItemRef(int index)
        {
            var self = this;

            // Following trick can reduce the number of range comparison operations by one
            if (unchecked((uint)index) >= (uint)self.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            return ref self._list._items[index];
        }

        /// <inheritdoc cref="ImmutableList{T}.Add(T)"/>
        public ImmutableSegmentedList<T> Add(T value)
        {
            var self = this;

            if (self.IsEmpty)
            {
                var list = new SegmentedList<T> { value };
                return new ImmutableSegmentedList<T>(list);
            }
            else
            {
                // TODO: Optimize this to share all segments except for the last one
                // TODO: Only resize the last page the minimum amount necessary
                var builder = self.ToValueBuilder();
                builder.Add(value);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableList{T}.AddRange(IEnumerable{T})"/>
        public ImmutableSegmentedList<T> AddRange(IEnumerable<T> items)
        {
            var self = this;

            if (items is ICollection<T> { Count: 0 })
            {
                return self;
            }
            else if (self.IsEmpty)
            {
                if (items is ImmutableSegmentedList<T> immutableList)
                    return immutableList;
                else if (items is ImmutableSegmentedList<T>.Builder builder)
                    return builder.ToImmutable();

                var list = new SegmentedList<T>(items);
                return new ImmutableSegmentedList<T>(list);
            }
            else
            {
                // TODO: Optimize this to share all segments except for the last one
                var builder = self.ToValueBuilder();
                builder.AddRange(items);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableList{T}.BinarySearch(T)"/>
        public int BinarySearch(T item)
            => _list.BinarySearch(item);

        /// <inheritdoc cref="ImmutableList{T}.BinarySearch(T, IComparer{T}?)"/>
        public int BinarySearch(T item, IComparer<T>? comparer)
            => _list.BinarySearch(item, comparer);

        /// <inheritdoc cref="ImmutableList{T}.BinarySearch(int, int, T, IComparer{T}?)"/>
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
            => _list.BinarySearch(index, count, item, comparer);

        /// <inheritdoc cref="ImmutableList{T}.Clear()"/>
        public ImmutableSegmentedList<T> Clear()
            => Empty;

        public bool Contains(T value)
            => _list.Contains(value);

        /// <inheritdoc cref="ImmutableList{T}.ConvertAll{TOutput}(Func{T, TOutput})"/>
        public ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
            => new ImmutableSegmentedList<TOutput>(_list.ConvertAll(converter));

        /// <inheritdoc cref="ImmutableList{T}.CopyTo(T[])"/>
        public void CopyTo(T[] array)
            => _list.CopyTo(array);

        public void CopyTo(T[] array, int arrayIndex)
            => _list.CopyTo(array, arrayIndex);

        /// <inheritdoc cref="ImmutableList{T}.CopyTo(int, T[], int, int)"/>
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
            => _list.CopyTo(index, array, arrayIndex, count);

        /// <inheritdoc cref="ImmutableList{T}.Exists(Predicate{T})"/>
        public bool Exists(Predicate<T> match)
            => _list.Exists(match);

        /// <inheritdoc cref="ImmutableList{T}.Find(Predicate{T})"/>
        public T? Find(Predicate<T> match)
            => _list.Find(match);

        /// <inheritdoc cref="ImmutableList{T}.FindAll(Predicate{T})"/>
        public ImmutableSegmentedList<T> FindAll(Predicate<T> match)
            => new ImmutableSegmentedList<T>(_list.FindAll(match));

        /// <inheritdoc cref="ImmutableList{T}.FindIndex(Predicate{T})"/>
        public int FindIndex(Predicate<T> match)
            => _list.FindIndex(match);

        /// <inheritdoc cref="ImmutableList{T}.FindIndex(int, Predicate{T})"/>
        public int FindIndex(int startIndex, Predicate<T> match)
            => _list.FindIndex(startIndex, match);

        /// <inheritdoc cref="ImmutableList{T}.FindIndex(int, int, Predicate{T})"/>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
            => _list.FindIndex(startIndex, count, match);

        /// <inheritdoc cref="ImmutableList{T}.FindLast(Predicate{T})"/>
        public T? FindLast(Predicate<T> match)
            => _list.FindLast(match);

        /// <inheritdoc cref="ImmutableList{T}.FindLastIndex(Predicate{T})"/>
        public int FindLastIndex(Predicate<T> match)
            => _list.FindLastIndex(match);

        /// <inheritdoc cref="ImmutableList{T}.FindLastIndex(int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            var self = this;

            if (startIndex == 0 && self.IsEmpty)
            {
                // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                // Handle it explicitly to avoid an exception.
                return -1;
            }

            return self._list.FindLastIndex(startIndex, match);
        }

        /// <inheritdoc cref="ImmutableList{T}.FindLastIndex(int, int, Predicate{T})"/>
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            var self = this;

            if (count == 0 && startIndex == 0 && self.IsEmpty)
            {
                // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                // Handle it explicitly to avoid an exception.
                return -1;
            }

            return self._list.FindLastIndex(startIndex, count, match);
        }

        /// <inheritdoc cref="ImmutableList{T}.ForEach(Action{T})"/>
        public void ForEach(Action<T> action)
            => _list.ForEach(action);

        /// <inheritdoc cref="ImmutableList{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
            => new(_list);

        /// <inheritdoc cref="ImmutableList{T}.GetRange(int, int)"/>
        public ImmutableSegmentedList<T> GetRange(int index, int count)
        {
            var self = this;

            if (index == 0 && count == self.Count)
                return self;

            return new ImmutableSegmentedList<T>(self._list.GetRange(index, count));
        }

        public int IndexOf(T value)
            => _list.IndexOf(value);

        public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
            => _list.IndexOf(item, index, count, equalityComparer);

        /// <inheritdoc cref="ImmutableList{T}.Insert(int, T)"/>
        public ImmutableSegmentedList<T> Insert(int index, T item)
        {
            var self = this;

            if (index == self.Count)
                return self.Add(item);

            // TODO: Optimize this to share all segments prior to index
            // TODO: Only resize the last page the minimum amount necessary
            var builder = self.ToValueBuilder();
            builder.Insert(index, item);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.InsertRange(int, IEnumerable{T})"/>
        public ImmutableSegmentedList<T> InsertRange(int index, IEnumerable<T> items)
        {
            var self = this;

            if (index == self.Count)
                return self.AddRange(items);

            // TODO: Optimize this to share all segments prior to index
            // TODO: Only resize the last page the minimum amount necessary
            var builder = self.ToValueBuilder();
            builder.InsertRange(index, items);
            return builder.ToImmutable();
        }

        public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
        {
            var self = this;

            if (index < 0)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            if (count < 0 || count > self.Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            if (index - count + 1 < 0)
                throw new ArgumentException();

            if (count == 0 && index == 0 && self.IsEmpty)
            {
                // SegmentedList<T> doesn't allow starting at index 0 for an empty list, but IImmutableList<T> does.
                // Handle it explicitly to avoid an exception.
                return -1;
            }

            return self._list.LastIndexOf(item, index, count, equalityComparer);
        }

        /// <inheritdoc cref="ImmutableList{T}.Remove(T)"/>
        public ImmutableSegmentedList<T> Remove(T value)
        {
            var self = this;

            var index = self.IndexOf(value);
            if (index < 0)
                return self;

            return self.RemoveAt(index);
        }

        /// <inheritdoc cref="ImmutableList{T}.Remove(T, IEqualityComparer{T}?)"/>
        public ImmutableSegmentedList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
        {
            var self = this;

            var index = self.IndexOf(value, 0, Count, equalityComparer);
            if (index < 0)
                return self;

            return self.RemoveAt(index);
        }

        /// <inheritdoc cref="ImmutableList{T}.RemoveAll(Predicate{T})"/>
        public ImmutableSegmentedList<T> RemoveAll(Predicate<T> match)
        {
            // TODO: Optimize this to avoid allocations if no items are removed
            // TODO: Optimize this to share pages prior to the first removed item
            var builder = ToValueBuilder();
            builder.RemoveAll(match);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.RemoveAt(int)"/>
        public ImmutableSegmentedList<T> RemoveAt(int index)
        {
            // TODO: Optimize this to share pages prior to the removed item
            var builder = ToValueBuilder();
            builder.RemoveAt(index);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.RemoveRange(IEnumerable{T})"/>
        public ImmutableSegmentedList<T> RemoveRange(IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var self = this;

            if (self.IsEmpty)
                return self;

            var builder = ToValueBuilder();
            foreach (var item in items)
            {
                var index = builder.IndexOf(item);
                if (index < 0)
                    continue;

                builder.RemoveAt(index);
            }

            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.RemoveRange(IEnumerable{T}, IEqualityComparer{T}?)"/>
        public ImmutableSegmentedList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var self = this;

            if (self.IsEmpty)
                return self;

            var builder = ToValueBuilder();
            foreach (var item in items)
            {
                var index = builder.IndexOf(item, 0, builder.Count, equalityComparer);
                if (index < 0)
                    continue;

                builder.RemoveAt(index);
            }

            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.RemoveRange(int, int)"/>
        public ImmutableSegmentedList<T> RemoveRange(int index, int count)
        {
            var self = this;

            if (count == 0 && index >= 0 && index <= self.Count)
            {
                return self;
            }

            // TODO: Optimize this to share pages prior to the first removed item
            var builder = self.ToValueBuilder();
            builder.RemoveRange(index, count);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Replace(T, T)"/>
        public ImmutableSegmentedList<T> Replace(T oldValue, T newValue)
        {
            var self = this;

            var index = self.IndexOf(oldValue);
            if (index < 0)
            {
                throw new ArgumentException(SR.CannotFindOldValue, nameof(oldValue));
            }

            return self.SetItem(index, newValue);
        }

        /// <inheritdoc cref="ImmutableList{T}.Replace(T, T, IEqualityComparer{T}?)"/>
        public ImmutableSegmentedList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
        {
            var self = this;

            var index = self.IndexOf(oldValue, equalityComparer);
            if (index < 0)
            {
                throw new ArgumentException(SR.CannotFindOldValue, nameof(oldValue));
            }

            return self.SetItem(index, newValue);
        }

        /// <inheritdoc cref="ImmutableList{T}.Reverse()"/>
        public ImmutableSegmentedList<T> Reverse()
        {
            var self = this;
            if (self.Count < 2)
                return self;

            var builder = self.ToValueBuilder();
            builder.Reverse();
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Reverse(int, int)"/>
        public ImmutableSegmentedList<T> Reverse(int index, int count)
        {
            var builder = ToValueBuilder();
            builder.Reverse(index, count);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.SetItem(int, T)"/>
        public ImmutableSegmentedList<T> SetItem(int index, T value)
        {
            // TODO: Optimize this to share all pages except the one with 'index'
            var builder = ToValueBuilder();
            builder[index] = value;
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Sort()"/>
        public ImmutableSegmentedList<T> Sort()
        {
            var self = this;

            if (self.Count < 2)
                return self;

            // TODO: Optimize this to avoid allocations if the list is already sorted
            var builder = self.ToValueBuilder();
            builder.Sort();
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Sort(IComparer{T}?)"/>
        public ImmutableSegmentedList<T> Sort(IComparer<T>? comparer)
        {
            var self = this;

            if (self.Count < 2)
                return self;

            // TODO: Optimize this to avoid allocations if the list is already sorted
            var builder = self.ToValueBuilder();
            builder.Sort(comparer);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Sort(Comparison{T})"/>
        public ImmutableSegmentedList<T> Sort(Comparison<T> comparison)
        {
            if (comparison == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            }

            var self = this;

            if (self.Count < 2)
                return self;

            // TODO: Optimize this to avoid allocations if the list is already sorted
            var builder = self.ToValueBuilder();
            builder.Sort(comparison);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.Sort(int, int, IComparer{T}?)"/>
        public ImmutableSegmentedList<T> Sort(int index, int count, IComparer<T>? comparer)
        {
            // TODO: Optimize this to avoid allocations if the list is already sorted
            var builder = ToValueBuilder();
            builder.Sort(index, count, comparer);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableList{T}.ToBuilder()"/>
        public Builder ToBuilder()
            => new Builder(this);

        private ValueBuilder ToValueBuilder()
            => new ValueBuilder(this);

        public override int GetHashCode()
            => _list?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
        {
            return obj is ImmutableSegmentedList<T> other
                && Equals(other);
        }

        public bool Equals(ImmutableSegmentedList<T> other)
            => _list == other._list;

        /// <inheritdoc cref="ImmutableList{T}.TrueForAll(Predicate{T})"/>
        public bool TrueForAll(Predicate<T> match)
            => _list.TrueForAll(match);

        IImmutableList<T> IImmutableList<T>.Clear()
            => Clear();

        IImmutableList<T> IImmutableList<T>.Add(T value)
            => Add(value);

        IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
            => AddRange(items);

        IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
            => Insert(index, element);

        IImmutableList<T> IImmutableList<T>.InsertRange(int index, IEnumerable<T> items)
            => InsertRange(index, items);

        IImmutableList<T> IImmutableList<T>.Remove(T value, IEqualityComparer<T>? equalityComparer)
            => Remove(value, equalityComparer);

        IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
            => RemoveAll(match);

        IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
            => RemoveRange(items, equalityComparer);

        IImmutableList<T> IImmutableList<T>.RemoveRange(int index, int count)
            => RemoveRange(index, count);

        IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
            => RemoveAt(index);

        IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
            => SetItem(index, value);

        IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
            => Replace(oldValue, newValue, equalityComparer);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => IsEmpty ? Enumerable.Empty<T>().GetEnumerator() : GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<T>)this).GetEnumerator();

        void IList<T>.Insert(int index, T item)
            => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index)
            => throw new NotSupportedException();

        void ICollection<T>.Add(T item)
            => throw new NotSupportedException();

        void ICollection<T>.Clear()
            => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item)
            => throw new NotSupportedException();

        int IList.Add(object? value)
            => throw new NotSupportedException();

        void IList.Clear()
            => throw new NotSupportedException();

        bool IList.Contains(object? value)
        {
            IList backingList = _list;
            return backingList.Contains(value);
        }

        int IList.IndexOf(object? value)
        {
            IList backingList = _list;
            return backingList.IndexOf(value);
        }

        void IList.Insert(int index, object? value)
            => throw new NotSupportedException();

        void IList.Remove(object? value)
            => throw new NotSupportedException();

        void IList.RemoveAt(int index)
            => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index)
        {
            IList backingList = _list;
            backingList.CopyTo(array, index);
        }
    }
}
