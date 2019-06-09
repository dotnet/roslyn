// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class OrderedSet<T> : IEnumerable<T>, IReadOnlySet<T>, IReadOnlyList<T>, IOrderedReadOnlySet<T>
    {
        private readonly HashSet<T> _set;
        private readonly ArrayBuilder<T> _list;

        public OrderedSet()
        {
            _set = new HashSet<T>();
            _list = new ArrayBuilder<T>();
        }

        public OrderedSet(IEnumerable<T> items)
            : this()
        {
            AddRange(items);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public bool Add(T item)
        {
            if (_set.Add(item))
            {
                _list.Add(item);
                return true;
            }

            return false;
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void Clear()
        {
            _set.Clear();
            _list.Clear();
        }
    }
}
