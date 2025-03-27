// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A set that returns the inserted values in insertion order.
    /// The mutation operations are not thread-safe.
    /// </summary>
    internal sealed class SetWithInsertionOrder<T> : IOrderedReadOnlySet<T>
    {
        private HashSet<T>? _set = null;
        private ArrayBuilder<T>? _elements = null;

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

            _elements!.Add(value);
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
                    _elements!.Insert(index, value);
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
            if (_set is null)
            {
                return false;
            }

            if (!_set.Remove(value))
            {
                return false;
            }
            _elements!.RemoveAt(_elements.IndexOf(value));
            return true;
        }

        public int Count => _elements?.Count ?? 0;

        public bool Contains(T value) => _set?.Contains(value) ?? false;

        public IEnumerator<T> GetEnumerator()
            => _elements is null ? SpecializedCollections.EmptyEnumerator<T>() : ((IEnumerable<T>)_elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ImmutableArray<T> AsImmutable() => _elements.ToImmutableArrayOrEmpty();

        public T this[int i] => _elements![i];

        private IReadOnlySet<T> Set
            => (IReadOnlySet<T>?)_set ?? SpecializedCollections.EmptyReadOnlySet<T>();

        public bool IsProperSubsetOf(IEnumerable<T> other)
            => Set.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other)
            => Set.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other)
            => Set.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other)
            => Set.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other)
            => Set.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other)
            => Set.SetEquals(other);
    }
}
