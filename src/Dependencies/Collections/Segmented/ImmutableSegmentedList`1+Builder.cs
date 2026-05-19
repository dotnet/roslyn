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
        public sealed class Builder : IList<T>, IReadOnlyList<T>, IList
        {
            /// <summary>
            /// The private builder implementation.
            /// </summary>
            private ValueBuilder _builder;

            internal Builder(ImmutableSegmentedList<T> list)
                => this._builder = new ValueBuilder(list);

            public int Count => this._builder.Count;

            bool ICollection<T>.IsReadOnly => ICollectionCalls<T>.IsReadOnly(ref this._builder);

            bool IList.IsFixedSize => IListCalls.IsFixedSize(ref this._builder);

            bool IList.IsReadOnly => IListCalls.IsReadOnly(ref this._builder);

            bool ICollection.IsSynchronized => ICollectionCalls.IsSynchronized(ref this._builder);

            object ICollection.SyncRoot => this;

            public T this[int index]
            {
                get => this._builder[index];
                set => this._builder[index] = value;
            }

            object? IList.this[int index]
            {
                get => IListCalls.GetItem(ref this._builder, index);
                set => IListCalls.SetItem(ref this._builder, index, value);
            }

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.ItemRef(int)"/>
            public ref readonly T ItemRef(int index)
                => ref this._builder.ItemRef(index);

            public void Add(T item)
                => this._builder.Add(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.AddRange(IEnumerable{T})"/>
            public void AddRange(IEnumerable<T> items)
                => this._builder.AddRange(items);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.BinarySearch(T)"/>
            public int BinarySearch(T item)
                => this._builder.BinarySearch(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.BinarySearch(T, IComparer{T}?)"/>
            public int BinarySearch(T item, IComparer<T>? comparer)
                => this._builder.BinarySearch(item, comparer);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.BinarySearch(int, int, T, IComparer{T}?)"/>
            public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
                => this._builder.BinarySearch(index, count, item, comparer);

            public void Clear()
                => this._builder.Clear();

            public bool Contains(T item)
                => this._builder.Contains(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.ConvertAll{TOutput}(Func{T, TOutput})"/>
            public ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
                => this._builder.ConvertAll(converter);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.CopyTo(T[])"/>
            public void CopyTo(T[] array)
                => this._builder.CopyTo(array);

            public void CopyTo(T[] array, int arrayIndex)
                => this._builder.CopyTo(array, arrayIndex);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.CopyTo(int, T[], int, int)"/>
            public void CopyTo(int index, T[] array, int arrayIndex, int count)
                => this._builder.CopyTo(index, array, arrayIndex, count);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Exists(Predicate{T})"/>
            public bool Exists(Predicate<T> match)
                => this._builder.Exists(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Find(Predicate{T})"/>
            public T? Find(Predicate<T> match)
                => this._builder.Find(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindAll(Predicate{T})"/>
            public ImmutableSegmentedList<T> FindAll(Predicate<T> match)
                => this._builder.FindAll(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindIndex(Predicate{T})"/>
            public int FindIndex(Predicate<T> match)
                => this._builder.FindIndex(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindIndex(int, Predicate{T})"/>
            public int FindIndex(int startIndex, Predicate<T> match)
                => this._builder.FindIndex(startIndex, match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindIndex(int, int, Predicate{T})"/>
            public int FindIndex(int startIndex, int count, Predicate<T> match)
                => this._builder.FindIndex(startIndex, count, match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindLast(Predicate{T})"/>
            public T? FindLast(Predicate<T> match)
                => this._builder.FindLast(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindLastIndex(Predicate{T})"/>
            public int FindLastIndex(Predicate<T> match)
                => this._builder.FindLastIndex(match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindLastIndex(int, Predicate{T})"/>
            public int FindLastIndex(int startIndex, Predicate<T> match)
                => this._builder.FindLastIndex(startIndex, match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.FindLastIndex(int, int, Predicate{T})"/>
            public int FindLastIndex(int startIndex, int count, Predicate<T> match)
                => this._builder.FindLastIndex(startIndex, count, match);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.ForEach(Action{T})"/>
            public void ForEach(Action<T> action)
                => this._builder.ForEach(action);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.GetEnumerator()"/>
            public Enumerator GetEnumerator()
                => this._builder.GetEnumerator();

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.GetRange(int, int)"/>
            public ImmutableSegmentedList<T> GetRange(int index, int count)
                => this._builder.GetRange(index, count);

            public int IndexOf(T item)
                => this._builder.IndexOf(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.IndexOf(T, int)"/>
            public int IndexOf(T item, int index)
                => this._builder.IndexOf(item, index);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.IndexOf(T, int, int)"/>
            public int IndexOf(T item, int index, int count)
                => this._builder.IndexOf(item, index, count);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.IndexOf(T, int, int, IEqualityComparer{T}?)"/>
            public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
                => this._builder.IndexOf(item, index, count, equalityComparer);

            public void Insert(int index, T item)
                => this._builder.Insert(index, item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.InsertRange(int, IEnumerable{T})"/>
            public void InsertRange(int index, IEnumerable<T> items)
                => this._builder.InsertRange(index, items);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.LastIndexOf(T)"/>
            public int LastIndexOf(T item)
                => this._builder.LastIndexOf(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.LastIndexOf(T, int)"/>
            public int LastIndexOf(T item, int startIndex)
                => this._builder.LastIndexOf(item, startIndex);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.LastIndexOf(T, int, int)"/>
            public int LastIndexOf(T item, int startIndex, int count)
                => this._builder.LastIndexOf(item, startIndex, count);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.LastIndexOf(T, int, int, IEqualityComparer{T}?)"/>
            public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
                => this._builder.LastIndexOf(item, startIndex, count, equalityComparer);

            public bool Remove(T item)
                => this._builder.Remove(item);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.RemoveAll(Predicate{T})"/>
            public int RemoveAll(Predicate<T> match)
                => this._builder.RemoveAll(match);

            public void RemoveAt(int index)
                => this._builder.RemoveAt(index);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Reverse()"/>
            public void Reverse()
                => this._builder.Reverse();

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Reverse(int, int)"/>
            public void Reverse(int index, int count)
                => this._builder.Reverse(index, count);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Sort()"/>
            public void Sort()
                => this._builder.Sort();

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Sort(IComparer{T}?)"/>
            public void Sort(IComparer<T>? comparer)
                => this._builder.Sort(comparer);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Sort(Comparison{T})"/>
            public void Sort(Comparison<T> comparison)
                => this._builder.Sort(comparison);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.Sort(int, int, IComparer{T}?)"/>
            public void Sort(int index, int count, IComparer<T>? comparer)
                => this._builder.Sort(index, count, comparer);

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.ToImmutable()"/>
            public ImmutableSegmentedList<T> ToImmutable()
                => this._builder.ToImmutable();

            /// <inheritdoc cref="System.Collections.Immutable.ImmutableList{T}.Builder.TrueForAll(Predicate{T})"/>
            public bool TrueForAll(Predicate<T> match)
                => this._builder.TrueForAll(match);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => IEnumerableCalls<T>.GetEnumerator(ref this._builder);

            IEnumerator IEnumerable.GetEnumerator()
                => IEnumerableCalls.GetEnumerator(ref this._builder);

            int IList.Add(object? value)
                => IListCalls.Add(ref this._builder, value);

            bool IList.Contains(object? value)
                => IListCalls.Contains(ref this._builder, value);

            int IList.IndexOf(object? value)
                => IListCalls.IndexOf(ref this._builder, value);

            void IList.Insert(int index, object? value)
                => IListCalls.Insert(ref this._builder, index, value);

            void IList.Remove(object? value)
                => IListCalls.Remove(ref this._builder, value);

            void ICollection.CopyTo(Array array, int index)
                => ICollectionCalls.CopyTo(ref this._builder, array, index);
        }
    }
}
