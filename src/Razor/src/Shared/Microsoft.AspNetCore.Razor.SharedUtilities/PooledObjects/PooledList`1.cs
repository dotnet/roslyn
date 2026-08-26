// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
///  Wraps a pooled <see cref="List{T}"/> but doesn't allocate it until
///  it's needed. Note: Dispose this to ensure that the pooled list is returned
///  to the pool.
/// </summary>
internal ref partial struct PooledList<T>
{
    private readonly ListPool<T> _pool;
    private List<T>? _list;

    public PooledList()
        : this(ListPool<T>.Default)
    {
    }

    public PooledList(ListPool<T> pool)
    {
        _pool = pool;
    }

    public void Dispose()
    {
        ClearAndFree();
    }

    public int Count
        => _list?.Count ?? 0;

    public T this[int index]
    {
        readonly get
        {
            if (_list is { } list)
            {
                return list[index];
            }

            // Throw moved to separate method to encourage better JIT inlining.
            return ThrowIndexOutOfRangeException();

            [DoesNotReturn]
            static T ThrowIndexOutOfRangeException()
            {
                throw new IndexOutOfRangeException();
            }
        }

        set
        {
            _list ??= _pool.Get();
            _list[index] = value;
        }
    }

    public void Add(T item)
    {
        _list ??= _pool.Get();
        _list.Add(item);
    }

    public void AddRange(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        _list ??= _pool.Get();
        _list.AddRange(items);
    }

    public void AddRange(IEnumerable<T> items)
    {
        _list ??= _pool.Get();
        _list.AddRange(items);
    }

    public readonly Enumerator GetEnumerator()
        => _list is { } list
            ? new Enumerator(list)
            : default;

    public void ClearAndFree()
    {
        if (_list is { } list)
        {
            _pool.Return(list);
            _list = null;
        }
    }

    public readonly T[] ToArray()
        => _list.ToArrayOrEmpty();
}
