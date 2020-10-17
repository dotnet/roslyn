// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Implements a readonly collection over a set of existing collections. This can be used to
    /// prevent having to copy items from one collection over to another (thus bloating space).
    /// 
    /// Note: this is a *collection*, not a *set*.  There is no removal of duplicated elements. This
    /// allows us to be able to efficiently do operations like CopyTo, Count, etc. in O(c) time
    /// instead of O(n) (where 'c' is the number of collections and 'n' is the number of elements).
    /// If you have a few collections with many elements in them, then this is an appropriate
    /// collection for you.
    /// </summary>
    internal class UnionCollection<T> : ICollection<T>
    {
        private readonly ImmutableArray<ICollection<T>> _collections;
        private int _count = -1;

        public static ICollection<T> Create(ICollection<T> coll1, ICollection<T> coll2)
        {
            Debug.Assert(coll1.IsReadOnly && coll2.IsReadOnly);

            // Often, one of the collections is empty. Avoid allocations in those cases.
            if (coll1.Count == 0)
            {
                return coll2;
            }

            if (coll2.Count == 0)
            {
                return coll1;
            }

            return new UnionCollection<T>(ImmutableArray.Create(coll1, coll2));
        }

        public static ICollection<T> Create<TOrig>(ImmutableArray<TOrig> collections, Func<TOrig, ICollection<T>> selector)
        {
            Debug.Assert(collections.All(c => selector(c).IsReadOnly));

            switch (collections.Length)
            {
                case 0:
                    return SpecializedCollections.EmptyCollection<T>();

                case 1:
                    return selector(collections[0]);

                default:
                    return new UnionCollection<T>(ImmutableArray.CreateRange(collections, selector));
            }
        }

        private UnionCollection(ImmutableArray<ICollection<T>> collections)
        {
            Debug.Assert(!collections.IsDefault);
            _collections = collections;
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            // PERF: Expansion of "return collections.Any(c => c.Contains(item));"
            // to avoid allocating a lambda.
            foreach (var c in _collections)
            {
                if (c.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            var index = arrayIndex;
            foreach (var collection in _collections)
            {
                collection.CopyTo(array, index);
                index += collection.Count;
            }
        }

        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    _count = _collections.Sum(c => c.Count);
                }

                return _count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collections.SelectMany(c => c).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
