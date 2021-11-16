// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable CA1825 // Avoid zero-length array allocations.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    // Based on the ArrayBuilder.cs implementation.
    internal sealed class HashSetBuilder<T, TComparer>
        where TComparer : IEqualityComparer<T>, new()
    {
        private static readonly ObjectPool<HashSetBuilder<T, TComparer>> s_poolInstance =
            new ObjectPool<HashSetBuilder<T, TComparer>>(() => new HashSetBuilder<T, TComparer>(new TComparer()), 16);

        private readonly HashSet<T> _items;

        public static HashSetBuilder<T, TComparer> GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Count == 0);

            return builder;
        }

        internal HashSetBuilder(IEqualityComparer<T> comparer)
        {
            _items = new HashSet<T>(comparer);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
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
            var result = new T[Count];

            int i = 0;
            foreach (var item in _items)
            {
                result[i++] = item;
            }

            return result;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
