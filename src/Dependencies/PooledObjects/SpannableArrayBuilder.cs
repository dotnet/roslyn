// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects;

/// <summary>
/// Pooled array building data structure that allows access to the underlying array as a span before being freed back to the pool.
/// </summary>
[DebuggerDisplay("Count = {Count,nq}")]
internal sealed class SpannableArrayBuilder<T>
{
    public const int PooledArrayLengthLimitExclusive = 128;

    private static readonly ObjectPool<SpannableArrayBuilder<T>> s_pool = new ObjectPool<SpannableArrayBuilder<T>>(static () => new SpannableArrayBuilder<T>(), size: 16);

    private T[] _items;

    internal SpannableArrayBuilder(int capacity = 8)
    {
        _items = new T[capacity];
        Count = 0;
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
        var newCount = Count + 1;
        EnsureCapacity(newCount);
        _items[Count] = item;
        Count = newCount;
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

    public int Count { get; private set; }
    public int Capacity => _items.Length;

    public ref T this[int index] => ref _items[index];

    public void Clear()
    {
        Array.Clear(_items, 0, Count);
        Count = 0;
    }

    public ReadOnlySpan<T> AsSpan()
        => _items.AsSpan(0, Count);

    public ReadOnlySpan<TOther> AsSpan<TOther>()
        => ((TOther[])(object)_items).AsSpan(0, Count);
}
