// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Represents an ordered sequence of weak references.
    /// </summary>
    internal sealed class WeakList<T>
        where T : class
    {
        private WeakReference<T>[] _items;
        private int _size;

        public WeakList()
        {
            _items = Array.Empty<WeakReference<T>>();
        }

        private void Resize()
        {
            Debug.Assert(_size == _items.Length);
            Debug.Assert(_items.Length == 0 || _items.Length >= MinimalNonEmptySize);

            int alive = _items.Length;
            int firstDead = -1;
            for (int i = 0; i < _items.Length; i++)
            {
                if (!_items[i].TryGetTarget(out _))
                {
                    if (firstDead == -1)
                    {
                        firstDead = i;
                    }

                    alive--;
                }
            }

            if (alive < _items.Length / 4)
            {
                // If we have just a few items left we shrink the array.
                // We avoid expanding the array until the number of new items added exceeds half of its capacity.
                Shrink(firstDead, alive);
            }
            else if (alive >= 3 * _items.Length / 4)
            {
                // If we have a lot of items alive we expand the array since just compacting them 
                // wouldn't free up much space (we would end up calling Resize again after adding a few more items).
                var newItems = new WeakReference<T>[GetExpandedSize(_items.Length)];

                if (firstDead >= 0)
                {
                    Compact(firstDead, newItems);
                }
                else
                {
                    Array.Copy(_items, 0, newItems, 0, _items.Length);
                    Debug.Assert(_size == _items.Length);
                }

                _items = newItems;
            }
            else
            {
                // Compact in-place to make space for new items at the end.
                // We will free up to length/4 slots in the array.
                Compact(firstDead, _items);
            }

            Debug.Assert(_items.Length > 0 && _size < 3 * _items.Length / 4, "length: " + _items.Length + " size: " + _size);
        }

        private void Shrink(int firstDead, int alive)
        {
            int newSize = GetExpandedSize(alive);
            var newItems = (newSize == _items.Length) ? _items : new WeakReference<T>[newSize];
            Compact(firstDead, newItems);
            _items = newItems;
        }

        private const int MinimalNonEmptySize = 4;

        private static int GetExpandedSize(int baseSize)
        {
            return Math.Max((baseSize * 2) + 1, MinimalNonEmptySize);
        }

        /// <summary>
        /// Copies all live references from <see cref="_items"/> to <paramref name="result"/>.
        /// Assumes that all references prior <paramref name="firstDead"/> are alive.
        /// </summary>
        private void Compact(int firstDead, WeakReference<T>[] result)
        {
            Debug.Assert(_items[firstDead].IsNull());

            if (!ReferenceEquals(_items, result))
            {
                Array.Copy(_items, 0, result, 0, firstDead);
            }

            int oldSize = _size;
            int j = firstDead;
            for (int i = firstDead + 1; i < oldSize; i++)
            {
                var item = _items[i];

                if (item.TryGetTarget(out _))
                {
                    result[j++] = item;
                }
            }

            _size = j;

            // free WeakReferences
            if (ReferenceEquals(_items, result))
            {
                while (j < oldSize)
                {
                    _items[j++] = null!;
                }
            }
        }

        /// <summary>
        /// Returns the number of weak references in this list. 
        /// Note that some of them might not point to live objects anymore.
        /// </summary>
        public int WeakCount
        {
            get { return _size; }
        }

        public WeakReference<T> GetWeakReference(int index)
        {
            if (index < 0 || index >= _size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[index];
        }

        public void Add(T item)
        {
            if (_size == _items.Length)
            {
                Resize();
            }

            Debug.Assert(_size < _items.Length);
            _items[_size++] = new WeakReference<T>(item);
        }

        public struct Enumerator
        {
            private readonly WeakList<T> _weakList;
            private readonly int _count;
            private int _nextIndex;
            private int _alive;
            private int _firstDead;
            private T? _current;

            public Enumerator(WeakList<T> weakList)
            {
                _weakList = weakList;
                _nextIndex = 0;
                _count = weakList._size;
                _alive = weakList._size;
                _firstDead = -1;
                _current = null;
            }

            public T Current => _current!;

            public bool MoveNext()
            {
                while (_nextIndex < _count)
                {
                    int currentIndex = _nextIndex;
                    _nextIndex += 1;
                    if (_weakList._items[currentIndex].TryGetTarget(out var item))
                    {
                        _current = item;
                        return true;
                    }
                    else
                    {
                        // object has been collected 

                        if (_firstDead < 0)
                        {
                            _firstDead = currentIndex;
                        }

                        _alive--;
                    }
                }

                if (_alive == 0)
                {
                    _weakList._items = Array.Empty<WeakReference<T>>();
                    _weakList._size = 0;
                }
                else if (_alive < _weakList._items.Length / 4)
                {
                    // If we have just a few items left we shrink the array.
                    _weakList.Shrink(_firstDead, _alive);
                }

                return false;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal WeakReference<T>[] TestOnly_UnderlyingArray { get { return _items; } }
    }
}
