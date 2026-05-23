// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
#if NET
using System.Runtime.CompilerServices;
#endif
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Utilities;

/// <summary>
/// A simple double-ended queue backed by a circular buffer. Supports O(1) push/pop at both ends
/// and O(1) indexed access. Pooled via <see cref="GetInstance"/> to avoid allocation churn in
/// hot loops.
/// </summary>
internal sealed class Deque<T> : IPooled
{
    private static readonly ObjectPool<Deque<T>> s_pool = new(static () => new Deque<T>());

    private T[] _array;
    private int _head;
    private int _count;

    private Deque()
    {
        _array = [];
    }

    public int Count => _count;

    public T this[int index]
    {
        get
        {
            Debug.Assert((uint)index < (uint)_count);
            return _array[(_head + index) % _array.Length];
        }
    }

    public T First
    {
        get
        {
            Debug.Assert(_count > 0);
            return _array[_head];
        }
    }

    public T Last
    {
        get
        {
            Debug.Assert(_count > 0);
            return _array[(_head + _count - 1) % _array.Length];
        }
    }

    public void AddLast(T item)
    {
        EnsureCapacity(_count + 1);
        _array[(_head + _count) % _array.Length] = item;
        _count++;
    }

    public T RemoveFirst()
    {
        Debug.Assert(_count > 0);
        var item = _array[_head];
        if (MustClearReferences())
            _array[_head] = default!;
        _head = (_head + 1) % _array.Length;
        _count--;
        return item;
    }

    public T RemoveLast()
    {
        Debug.Assert(_count > 0);
        _count--;
        var index = (_head + _count) % _array.Length;
        var item = _array[index];
        if (MustClearReferences())
            _array[index] = default!;
        return item;
    }

    private void EnsureCapacity(int min)
    {
        if (_array.Length >= min)
            return;

        var newCapacity = Math.Max(4, _array.Length * 2);
        while (newCapacity < min)
            newCapacity *= 2;

        var newArray = ArrayPool<T>.Shared.Rent(newCapacity);
        if (_count > 0)
        {
            if (_head + _count <= _array.Length)
            {
                Array.Copy(_array, _head, newArray, 0, _count);
            }
            else
            {
                var firstPart = _array.Length - _head;
                Array.Copy(_array, _head, newArray, 0, firstPart);
                Array.Copy(_array, 0, newArray, firstPart, _count - firstPart);
            }
        }

        ReturnArray();
        _array = newArray;
        _head = 0;
    }

    private void ReturnArray()
    {
        if (_array.Length > 0)
            ArrayPool<T>.Shared.Return(_array, clearArray: MustClearReferences());
    }

    private static bool MustClearReferences()
    {
#if NET
        return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        return true;
#endif
    }

    void IPooled.Free(bool discardLargeInstances)
    {
        ReturnArray();
        _array = [];
        _head = 0;
        _count = 0;
        s_pool.Free(this);
    }

    public static PooledDisposer<Deque<T>> GetInstance(out Deque<T> instance)
    {
        instance = s_pool.Allocate();
        Debug.Assert(instance._count == 0);
        return new PooledDisposer<Deque<T>>(instance);
    }
}
