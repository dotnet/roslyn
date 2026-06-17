// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed partial class IntermediateNodeCollection : IList<IntermediateNode>, IReadOnlyList<IntermediateNode>
{
    public static readonly IntermediateNodeCollection ReadOnly = new IntermediateNodeCollection();

    private InlineList _inner;

    public IntermediateNodeCollection()
    {
    }

    public IntermediateNode this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _inner[index];
        }
        set
        {
            ThrowIfReadOnly();

            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _inner[index] = value;
        }
    }

    public int Count => _inner.Count;

    public bool IsReadOnly => ReferenceEquals(this, ReadOnly);

    private void ThrowIfReadOnly()
    {
        if (IsReadOnly)
        {
            throw new NotSupportedException("Collection is read-only.");
        }
    }

    public void Add(IntermediateNode item)
    {
        ThrowIfReadOnly();

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _inner.Add(item);
    }

    public void AddRange(IEnumerable<IntermediateNode> items)
    {
        ThrowIfReadOnly();

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _inner.Add(item);
        }
    }

    public void AddRange(IntermediateNodeCollection items)
    {
        ThrowIfReadOnly();

        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var count = items.Count;
        for (var i = 0; i < count; i++)
        {
            _inner.Add(items[i]);
        }
    }

    internal void AddRange(in PooledArrayBuilder<IntermediateNode> items)
    {
        ThrowIfReadOnly();

        foreach (var item in items)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _inner.Add(item);
        }
    }

    public void Clear()
    {
        ThrowIfReadOnly();
        _inner.Clear();
    }

    public bool Contains(IntermediateNode item)
    {
        return item != null && IndexOf(item) >= 0;
    }

    public void CopyTo(IntermediateNode[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }
        else if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        _inner.CopyTo(array, arrayIndex);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<IntermediateNode> IEnumerable<IntermediateNode>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(IntermediateNode item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return _inner.IndexOf(item);
    }

    public void Insert(int index, IntermediateNode item)
    {
        ThrowIfReadOnly();

        if (index < 0 || index > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _inner.Insert(index, item);
    }

    public bool Remove(IntermediateNode item)
    {
        ThrowIfReadOnly();

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        ThrowIfReadOnly();

        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _inner.RemoveAt(index);
    }

    public struct Enumerator : IEnumerator<IntermediateNode>
    {
        private readonly IntermediateNodeCollection _items;
        private int _index;

        public Enumerator(IntermediateNodeCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _items = collection;
            _index = -1;
        }

        public IntermediateNode Current
        {
            get
            {
                if (_index < 0 || _index >= _items.Count)
                {
                    return null;
                }

                return _items[_index];
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _items.Count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}
