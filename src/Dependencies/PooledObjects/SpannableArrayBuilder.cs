// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects;

[DebuggerDisplay("Count = {Count,nq}")]
internal sealed class SpannableArrayBuilder<T>
{
    public const int PooledArrayLengthLimitExclusive = 128;

    private static readonly ObjectPool<SpannableArrayBuilder<T>> s_pool = new(() => new(capacity: 4), size: 16);

    private T[] _items;
    private int _count;

    internal SpannableArrayBuilder()
        : this(capacity: 8)
    {
    }

    internal SpannableArrayBuilder(int capacity)
    {
        _items = new T[capacity];
        _count = 0;
    }

    public static SpannableArrayBuilder<T> GetInstance()
        => s_pool.Allocate();

    public void Free()
    {
        if (_items.Length < PooledArrayLengthLimitExclusive)
        {
            if (Count != 0)
                Clear();

            s_pool.Free(this);
        }
        else
        {
            s_pool.ForgetTrackedObject(this);
        }
    }

    public void Add(T item)
    {
        var newCount = _count + 1;
        EnsureCapacity(newCount);
        _items[_count] = item;
        _count = newCount;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        foreach (var item in items)
            Add(item);
    }

    public void EnsureCapacity(int capacity)
    {
        if (_items.Length < capacity)
        {
            var newCapacity = Math.Max(_items.Length * 2, capacity);
            Array.Resize(ref _items, newCapacity);
        }
    }

    public int Count
    {
        get => _count;
    }

    public int Capacity
    {
        get => _items.Length;
    }

    public T this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public void Clear()
    {
        Array.Clear(_items, 0, _count);
        _count = 0;
    }

    public ReadOnlySpan<T> AsSpan()
        => new ReadOnlySpan<T>(_items, 0, _count);

    public ReadOnlySpan<TOther> AsSpan<TOther>()
        => new ReadOnlySpan<TOther>((TOther[])(object)_items, 0, _count);

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    internal struct Enumerator
    {
        private readonly SpannableArrayBuilder<T> _builder;
        private int _index;

        public Enumerator(SpannableArrayBuilder<T> builder)
        {
            _builder = builder;
            _index = -1;
        }

        public readonly T Current
        {
            get => _builder[_index];
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _builder.Count;
        }
    }
}
