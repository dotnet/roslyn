// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial struct ImmutableSegmentedList<T>
    {
        public sealed class Builder : IList<T>, IReadOnlyList<T>, IList
        {
            private Builder() => throw null!;

            public int Count => throw null!;

            bool ICollection<T>.IsReadOnly => throw null!;

            bool IList.IsFixedSize => throw null!;

            bool IList.IsReadOnly => throw null!;

            bool ICollection.IsSynchronized => throw null!;

            object ICollection.SyncRoot => throw null!;

            public T this[int index]
            {
                get => throw null!;
                set => throw null!;
            }

            object? IList.this[int index]
            {
                get => throw null!;
                set => throw null!;
            }

            public void Add(T item) => throw null!;

            public void AddRange(IEnumerable<T> items) => throw null!;

            public int BinarySearch(T item) => throw null!;

            public int BinarySearch(T item, IComparer<T> comparer) => throw null!;

            public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => throw null!;

            public void Clear() => throw null!;

            public bool Contains(T item) => throw null!;

            public ImmutableSegmentedList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter) => throw null!;

            public void CopyTo(T[] array) => throw null!;

            public void CopyTo(T[] array, int arrayIndex) => throw null!;

            public void CopyTo(int index, T[] array, int arrayIndex, int count) => throw null!;

            public bool Exists(Predicate<T> match) => throw null!;

            public T Find(Predicate<T> match) => throw null!;

            public ImmutableSegmentedList<T> FindAll(Predicate<T> match) => throw null!;

            public int FindIndex(Predicate<T> match) => throw null!;

            public int FindIndex(int startIndex, Predicate<T> match) => throw null!;

            public int FindIndex(int startIndex, int count, Predicate<T> match) => throw null!;

            public T FindLast(Predicate<T> match) => throw null!;

            public int FindLastIndex(Predicate<T> match) => throw null!;

            public int FindLastIndex(int startIndex, Predicate<T> match) => throw null!;

            public int FindLastIndex(int startIndex, int count, Predicate<T> match) => throw null!;

            public void ForEach(Action<T> action) => throw null!;

            public Enumerator GetEnumerator() => throw null!;

            public ImmutableSegmentedList<T> GetRange(int index, int count) => throw null!;

            public int IndexOf(T item) => throw null!;

            public int IndexOf(T item, int index) => throw null!;

            public int IndexOf(T item, int index, int count) => throw null!;

            public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => throw null!;

            public void Insert(int index, T item) => throw null!;

            public void InsertRange(int index, IEnumerable<T> items) => throw null!;

            public int LastIndexOf(T item) => throw null!;

            public int LastIndexOf(T item, int startIndex) => throw null!;

            public int LastIndexOf(T item, int startIndex, int count) => throw null!;

            public int LastIndexOf(T item, int startIndex, int count, IEqualityComparer<T> equalityComparer) => throw null!;

            public bool Remove(T item) => throw null!;

            public int RemoveAll(Predicate<T> match) => throw null!;

            public void RemoveAt(int index) => throw null!;

            public void Reverse() => throw null!;

            public void Reverse(int index, int count) => throw null!;

            public void Sort() => throw null!;

            public void Sort(IComparer<T> comparer) => throw null!;

            public void Sort(Comparison<T> comparison) => throw null!;

            public void Sort(int index, int count, IComparer<T> comparer) => throw null!;

            public ImmutableSegmentedList<T> ToImmutable() => throw null!;

            public bool TrueForAll(Predicate<T> match) => throw null!;

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw null!;

            IEnumerator IEnumerable.GetEnumerator() => throw null!;

            int IList.Add(object? value) => throw null!;

            bool IList.Contains(object? value) => throw null!;

            int IList.IndexOf(object? value) => throw null!;

            void IList.Insert(int index, object? value) => throw null!;

            void IList.Remove(object? value) => throw null!;

            void ICollection.CopyTo(Array array, int index) => throw null!;
        }
    }
}
