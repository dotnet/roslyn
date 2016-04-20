// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class ArrayBuilder<T>
    {
        private static readonly ObjectPool<ArrayBuilder<T>> s_poolInstance = new ObjectPool<ArrayBuilder<T>>(() => new ArrayBuilder<T>(), 16);
        private static readonly ReadOnlyCollection<T> s_empty = new ReadOnlyCollection<T>(new T[0]);

        private readonly List<T> _items;

        public static ArrayBuilder<T> GetInstance(int size = 0)
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Count == 0);
            if (size > 0)
            {
                builder._items.Capacity = size;
            }
            return builder;
        }

        internal ArrayBuilder()
        {
            _items = new List<T>();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void AddRange(T[] items)
        {
            foreach (var item in items)
            {
                _items.Add(item);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            _items.AddRange(items);
        }

        public T Peek()
        {
            return _items[_items.Count - 1];
        }

        public void Push(T item)
        {
            Add(item);
        }

        public T Pop()
        {
            var position = _items.Count - 1;
            var result = _items[position];
            _items.RemoveAt(position);
            return result;
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void Free()
        {
            _items.Clear();
            s_poolInstance.Free(this);
        }

        public T[] ToArray()
        {
            return _items.ToArray();
        }

        public T[] ToArrayAndFree()
        {
            var result = this.ToArray();
            this.Free();
            return result;
        }

        public ReadOnlyCollection<T> ToImmutable()
        {
            return (_items.Count > 0) ? new ReadOnlyCollection<T>(_items.ToArray()) : s_empty;
        }

        public ReadOnlyCollection<T> ToImmutableAndFree()
        {
            var result = this.ToImmutable();
            this.Free();
            return result;
        }

        public T this[int i]
        {
            get { return _items[i]; }
        }

        public void Sort(IComparer<T> comparer)
        {
            _items.Sort(comparer);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
