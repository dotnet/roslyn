// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// this is RAII object to automatically release pooled object when its owning pool
/// </summary>
internal struct PooledObject<T> : IDisposable where T : class
{
    private readonly Action<ObjectPool<T>, T> _releaser;
    private readonly ObjectPool<T> _pool;

    public PooledObject(ObjectPool<T> pool, Func<ObjectPool<T>, T> allocator, Action<ObjectPool<T>, T> releaser) : this()
    {
        _pool = pool;
        Object = allocator(pool);
        _releaser = releaser;
    }

    public T Object { get; private set; }

    public void Dispose()
    {
        if (Object != null)
        {
            _releaser(_pool, Object);
            Object = null!;
        }
    }

    #region factory
    public static PooledObject<StringBuilder> Create(ObjectPool<StringBuilder> pool)
    {
        return new PooledObject<StringBuilder>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<Stack<TItem>> Create<TItem>(ObjectPool<Stack<TItem>> pool)
    {
        return new PooledObject<Stack<TItem>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<Queue<TItem>> Create<TItem>(ObjectPool<Queue<TItem>> pool)
    {
        return new PooledObject<Queue<TItem>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<HashSet<TItem>> Create<TItem>(ObjectPool<HashSet<TItem>> pool)
    {
        return new PooledObject<HashSet<TItem>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<Dictionary<TKey, TValue>> Create<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool)
        where TKey : notnull
    {
        return new PooledObject<Dictionary<TKey, TValue>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<List<TItem>> Create<TItem>(ObjectPool<List<TItem>> pool)
    {
        return new PooledObject<List<TItem>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    public static PooledObject<SegmentedList<TItem>> Create<TItem>(ObjectPool<SegmentedList<TItem>> pool)
    {
        return new PooledObject<SegmentedList<TItem>>(
            pool,
            static p => Allocator(p),
            static (p, v) => Releaser(p, v));
    }

    #endregion

    #region allocators and releasers
    private static StringBuilder Allocator(ObjectPool<StringBuilder> pool)
        => pool.AllocateAndClear();

    private static void Releaser(ObjectPool<StringBuilder> pool, StringBuilder sb)
        => pool.ClearAndFree(sb);

    private static Stack<TItem> Allocator<TItem>(ObjectPool<Stack<TItem>> pool)
        => pool.AllocateAndClear();

    private static void Releaser<TItem>(ObjectPool<Stack<TItem>> pool, Stack<TItem> obj)
        => pool.ClearAndFree(obj);

    private static Queue<TItem> Allocator<TItem>(ObjectPool<Queue<TItem>> pool)
        => pool.AllocateAndClear();

    private static void Releaser<TItem>(ObjectPool<Queue<TItem>> pool, Queue<TItem> obj)
        => pool.ClearAndFree(obj);

    private static HashSet<TItem> Allocator<TItem>(ObjectPool<HashSet<TItem>> pool)
        => pool.AllocateAndClear();

    private static void Releaser<TItem>(ObjectPool<HashSet<TItem>> pool, HashSet<TItem> obj)
        => pool.ClearAndFree(obj);

    private static Dictionary<TKey, TValue> Allocator<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool)
        where TKey : notnull
        => pool.AllocateAndClear();

    private static void Releaser<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool, Dictionary<TKey, TValue> obj)
        where TKey : notnull
        => pool.ClearAndFree(obj);

    private static List<TItem> Allocator<TItem>(ObjectPool<List<TItem>> pool)
        => pool.AllocateAndClear();

    private static void Releaser<TItem>(ObjectPool<List<TItem>> pool, List<TItem> obj)
        => pool.ClearAndFree(obj, pool.TrimOnFree);

    private static SegmentedList<TItem> Allocator<TItem>(ObjectPool<SegmentedList<TItem>> pool)
        => pool.AllocateAndClear();

    private static void Releaser<TItem>(ObjectPool<SegmentedList<TItem>> pool, SegmentedList<TItem> obj)
        => pool.ClearAndFree(obj, pool.TrimOnFree);
    #endregion
}
