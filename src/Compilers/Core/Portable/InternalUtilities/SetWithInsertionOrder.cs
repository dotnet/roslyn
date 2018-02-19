// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A set that returns the inserted values in insertion order.
    /// The mutation operations are not thread-safe.
    /// </summary>
    internal sealed class SetWithInsertionOrder<T> : IEnumerable<T>, IReadOnlySet<T>
    {
        private HashSet<T> _set = null;
        private ArrayBuilder<T> _elements = null;

        public bool Add(T value)
        {
            if (_set == null)
            {
                _set = new HashSet<T>();
                _elements = new ArrayBuilder<T>();
            }

            if (!_set.Add(value))
            {
                return false;
            }

            _elements.Add(value);
            return true;
        }

        public bool Insert(int index, T value)
        {
            if (_set == null)
            {
                if (index > 0)
                {
                    throw new IndexOutOfRangeException();
                }
                Add(value);
            }
            else
            {
                if (!_set.Add(value))
                {
                    return false;
                }

                try
                {
                    _elements.Insert(index, value);
                }
                catch
                {
                    _set.Remove(value);
                    throw;
                }
            }
            return true;
        }

        public bool Remove(T value)
        {
            if (!_set.Remove(value))
            {
                return false;
            }
            _elements.RemoveAt(_elements.IndexOf(value));
            return true;
        }

        public int Count => _elements?.Count ?? 0;

        public bool Contains(T value) => _set?.Contains(value) ?? false;

        public IEnumerator<T> GetEnumerator()
            => _elements?.GetEnumerator() ?? SpecializedCollections.EmptyEnumerator<T>();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ImmutableArray<T> AsImmutable() => _elements.ToImmutableArrayOrEmpty();

        public T this[int i] => _elements[i];
    }
}
