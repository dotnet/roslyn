// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed partial class IntermediateNodeCollection
{
    /// <summary>
    /// A list-like struct that stores a single item inline (no array allocation)
    /// and uses <see cref="ArrayPool{T}"/> for backing storage when more than one
    /// item is needed. On resize, the old array is returned to the pool.
    /// </summary>
    /// <remarks>
    /// This struct is private to <see cref="IntermediateNodeCollection"/> and its methods
    /// make assumptions about their inputs (e.g., valid indices, non-null items) that are
    /// guaranteed by the collection wrapper. They do not perform defensive validation.
    /// </remarks>
    private struct InlineList
    {
        private IntermediateNode _single;
        private IntermediateNode[] _items;
        private int _count;

        public readonly int Count => _count;

        public IntermediateNode this[int index]
        {
            readonly get
            {
                if (_items != null)
                {
                    return _items[index];
                }

                if (index == 0 && _count == 1)
                {
                    return _single;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }

            set
            {
                if (_items != null)
                {
                    _items[index] = value;
                    return;
                }

                if (index == 0 && _count == 1)
                {
                    _single = value;
                    return;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void Add(IntermediateNode item)
        {
            if (_count == 0)
            {
                _single = item;
                _count = 1;
                return;
            }

            if (_items == null)
            {
                // Transition from inline to array
                _items = ArrayPool<IntermediateNode>.Shared.Rent(4);
                _items[0] = _single;
                _items[1] = item;
                _single = null;
                _count = 2;
                return;
            }

            if (_count == _items.Length)
            {
                Grow(_count + 1);
            }

            _items[_count++] = item;
        }

        public void Insert(int index, IntermediateNode item)
        {
            if (_count == 0 && index == 0)
            {
                _single = item;
                _count = 1;
                return;
            }

            if (_items == null)
            {
                // Currently have 1 item inline, need to transition to array
                _items = ArrayPool<IntermediateNode>.Shared.Rent(4);
                if (index == 0)
                {
                    _items[0] = item;
                    _items[1] = _single;
                }
                else
                {
                    _items[0] = _single;
                    _items[1] = item;
                }

                _single = null;
                _count = 2;
                return;
            }

            if (_count == _items.Length)
            {
                Grow(_count + 1);
            }

            if (index < _count)
            {
                Array.Copy(_items, index, _items, index + 1, _count - index);
            }

            _items[index] = item;
            _count++;
        }

        public void RemoveAt(int index)
        {
            if (_count == 1)
            {
                // Removing the single item
                _single = null;
                _count = 0;
            }
            else if (_count == 2)
            {
                // Transition back to single item mode when possible.
                _single = index == 0 ? _items[1] : _items[0];
                ArrayPool<IntermediateNode>.Shared.Return(_items, clearArray: true);
                _count = 1;
                _items = null;
            }
            else
            {
                _count--;
                if (index < _count)
                {
                    Array.Copy(_items, index + 1, _items, index, _count - index);
                }

                _items[_count] = null;
            }
        }

        public void Clear()
        {
            if (_items != null)
            {
                Array.Clear(_items, 0, _count);
                ArrayPool<IntermediateNode>.Shared.Return(_items, clearArray: false);
                _items = null;
            }
            else
            {
                _single = null;
            }

            _count = 0;
        }

        public readonly int IndexOf(IntermediateNode item)
        {
            if (_items != null)
            {
                return Array.IndexOf(_items, item, 0, _count);
            }

            if (_count == 1 && EqualityComparer<IntermediateNode>.Default.Equals(_single, item))
            {
                return 0;
            }

            return -1;
        }

        public readonly void CopyTo(IntermediateNode[] array, int arrayIndex)
        {
            if (_items != null)
            {
                Array.Copy(_items, 0, array, arrayIndex, _count);
            }
            else if (_count == 1)
            {
                array[arrayIndex] = _single;
            }
        }

        private void Grow(int minimumRequired)
        {
            var newCapacity = _items.Length * 2;
            if (newCapacity < minimumRequired)
            {
                newCapacity = minimumRequired;
            }

            var oldArray = _items;
            var newArray = ArrayPool<IntermediateNode>.Shared.Rent(newCapacity);
            Array.Copy(oldArray, newArray, _count);
            _items = newArray;
            ArrayPool<IntermediateNode>.Shared.Return(oldArray, clearArray: true);
        }
    }
}
