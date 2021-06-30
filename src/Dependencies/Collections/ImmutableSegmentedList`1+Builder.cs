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
        public sealed class Builder : IList<T>, IReadOnlyList<T>, IList
        {
            /// <summary>
            /// The immutable collection this builder is based on.
            /// </summary>
            private ValueBuilder _builder;

            internal Builder(ImmutableSegmentedList<T> list)
                => _builder = new ValueBuilder(list);

            public int Count => _builder.Count;

            bool ICollection<T>.IsReadOnly => ICollectionCalls<T>.IsReadOnly(ref _builder);

            bool IList.IsFixedSize => IListCalls.IsFixedSize(ref _builder);

            bool IList.IsReadOnly => IListCalls.IsReadOnly(ref _builder);

            bool ICollection.IsSynchronized => ICollectionCalls.IsSynchronized(ref _builder);

            object ICollection.SyncRoot => this;

            public T this[int index]
            {
                get => _builder[index];
                set => _builder[index] = value;
            }

            object? IList.this[int index]
            {
                get => IListCalls.GetItem(ref _builder, index);
                set => IListCalls.SetItem(ref _builder, index, value);
            }

            public ref readonly T ItemRef(int index)
                => ref _builder.ItemRef(index);

            public void Add(T item)
                => _builder.Add(item);

            public void AddRange(IEnumerable<T> items)
                => _builder.AddRange(items);

            public int BinarySearch(T item)
                => _builder.BinarySearch(item);

            public int BinarySearch(T item, IComparer<T>? comparer)
                => _builder.BinarySearch(item, comparer);

            public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
                => _builder.BinarySearch(index, count, item, comparer);

            public void Clear()
                => _builder.Clear();

            public bool Contains(T item)
                => _builder.Contains(item);

            public ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
                => _builder.ConvertAll(converter);

            public void CopyTo(T[] array)
                => _builder.CopyTo(array);

            public void CopyTo(T[] array, int arrayIndex)
                => _builder.CopyTo(array, arrayIndex);

            public void CopyTo(int index, T[] array, int arrayIndex, int count)
                => _builder.CopyTo(index, array, arrayIndex, count);

            public bool Exists(Predicate<T> match)
                => _builder.Exists(match);

            public T? Find(Predicate<T> match)
                => _builder.Find(match);

            public ImmutableSegmentedList<T> FindAll(Predicate<T> match)
                => _builder.FindAll(match);

            public int FindIndex(Predicate<T> match)
                => _builder.FindIndex(match);

            public int FindIndex(int startIndex, Predicate<T> match)
                => _builder.FindIndex(startIndex, match);

            public int FindIndex(int startIndex, int count, Predicate<T> match)
                => _builder.FindIndex(startIndex, count, match);

            public T? FindLast(Predicate<T> match)
                => _builder.FindLast(match);

            public int FindLastIndex(Predicate<T> match)
                => _builder.FindLastIndex(match);

            public int FindLastIndex(int startIndex, Predicate<T> match)
                => _builder.FindLastIndex(startIndex, match);

            public int FindLastIndex(int startIndex, int count, Predicate<T> match)
                => _builder.FindLastIndex(startIndex, count, match);

            public void ForEach(Action<T> action)
                => _builder.ForEach(action);

            public Enumerator GetEnumerator()
                => _builder.GetEnumerator();

            public ImmutableSegmentedList<T> GetRange(int index, int count)
                => _builder.GetRange(index, count);

            public int IndexOf(T item)
                => _builder.IndexOf(item);

            public int IndexOf(T item, int index)
                => _builder.IndexOf(item, index);

            public int IndexOf(T item, int index, int count)
                => _builder.IndexOf(item, index, count);

            public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
                => _builder.IndexOf(item, index, count, equalityComparer);

            public void Insert(int index, T item)
                => _builder.Insert(index, item);

            public void InsertRange(int index, IEnumerable<T> items)
                => _builder.InsertRange(index, items);

            public int LastIndexOf(T item)
                => _builder.LastIndexOf(item);

            public int LastIndexOf(T item, int startIndex)
                => _builder.LastIndexOf(item, startIndex);

            public int LastIndexOf(T item, int startIndex, int count)
                => _builder.LastIndexOf(item, startIndex, count);

            public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer)
                => _builder.LastIndexOf(item, startIndex, count, equalityComparer);

            public bool Remove(T item)
                => _builder.Remove(item);

            public int RemoveAll(Predicate<T> match)
                => _builder.RemoveAll(match);

            public void RemoveAt(int index)
                => _builder.RemoveAt(index);

            public void RemoveRange(int index, int count)
                => _builder.RemoveRange(index, count);

            public void Reverse()
                => _builder.Reverse();

            public void Reverse(int index, int count)
                => _builder.Reverse(index, count);

            public void Sort()
                => _builder.Sort();

            public void Sort(IComparer<T>? comparer)
                => _builder.Sort(comparer);

            public void Sort(Comparison<T> comparison)
                => _builder.Sort(comparison);

            public void Sort(int index, int count, IComparer<T>? comparer)
                => _builder.Sort(index, count, comparer);

            public ImmutableSegmentedList<T> ToImmutable()
                => _builder.ToImmutable();

            public bool TrueForAll(Predicate<T> match)
                => _builder.TrueForAll(match);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => IEnumerableCalls<T>.GetEnumerator(ref _builder);

            IEnumerator IEnumerable.GetEnumerator()
                => IEnumerableCalls.GetEnumerator(ref _builder);

            int IList.Add(object? value)
                => IListCalls.Add(ref _builder, value);

            bool IList.Contains(object? value)
                => IListCalls.Contains(ref _builder, value);

            int IList.IndexOf(object? value)
                => IListCalls.IndexOf(ref _builder, value);

            void IList.Insert(int index, object? value)
                => IListCalls.Insert(ref _builder, index, value);

            void IList.Remove(object? value)
                => IListCalls.Remove(ref _builder, value);

            void ICollection.CopyTo(Array array, int index)
                => ICollectionCalls.CopyTo(ref _builder, array, index);
        }
    }
}
