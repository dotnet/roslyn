// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class OrderedSet<T> : IEnumerable<T>, IReadOnlySet<T>
    {
        private readonly HashSet<T> set;
        private readonly ArrayBuilder<T> list;

        public OrderedSet()
        {
            set = new HashSet<T>();
            list = new ArrayBuilder<T>();
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
            if (set.Add(item))
            {
                list.Add(item);
                return true;
            }

            return false;
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public void Clear()
        {
            set.Clear();
            list.Clear();
        }
    }
}
