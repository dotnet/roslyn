// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static partial class Extensions
{
    public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool)
        where T : class
        => new(pool);

    public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool, out T obj)
        where T : class
    {
        var pooledObject = pool.GetPooledObject();
        obj = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<ImmutableArray<T>.Builder> GetPooledObject<T>(this ObjectPool<ImmutableArray<T>.Builder> pool)
        => new(pool);

    public static PooledObject<ImmutableArray<T>.Builder> GetPooledObject<T>(
        this ObjectPool<ImmutableArray<T>.Builder> pool,
        out ImmutableArray<T>.Builder builder)
    {
        var pooledObject = pool.GetPooledObject();
        builder = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool)
        where TKey : notnull
        => new(pool);

    public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject<TKey, TValue>(
        this ObjectPool<Dictionary<TKey, TValue>> pool,
        out Dictionary<TKey, TValue> map)
        where TKey : notnull
    {
        var pooledObject = pool.GetPooledObject();
        map = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<HashSet<T>> GetPooledObject<T>(this ObjectPool<HashSet<T>> pool)
        => new(pool);

    public static PooledObject<HashSet<T>> GetPooledObject<T>(
        this ObjectPool<HashSet<T>> pool,
        out HashSet<T> set)
    {
        var pooledObject = pool.GetPooledObject();
        set = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<IncrementalHash> GetPooledObject<T>(this ObjectPool<IncrementalHash> pool)
        => new(pool);

    public static PooledObject<IncrementalHash> GetPooledObject<T>(
        this ObjectPool<IncrementalHash> pool,
        out IncrementalHash hash)
    {
        var pooledObject = pool.GetPooledObject();
        hash = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<List<T>> GetPooledObject<T>(this ObjectPool<List<T>> pool)
        => new(pool);

    public static PooledObject<List<T>> GetPooledObject<T>(
        this ObjectPool<List<T>> pool,
        out List<T> list)
    {
        var pooledObject = pool.GetPooledObject();
        list = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<Stack<T>> GetPooledObject<T>(this ObjectPool<Stack<T>> pool)
        => new(pool);

    public static PooledObject<Stack<T>> GetPooledObject<T>(
        this ObjectPool<Stack<T>> pool,
        out Stack<T> stack)
    {
        var pooledObject = pool.GetPooledObject();
        stack = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<Stopwatch> GetPooledObject(this ObjectPool<Stopwatch> pool)
        => new(pool);

    public static PooledObject<Stopwatch> GetPooledObject(
        this ObjectPool<Stopwatch> pool,
        out Stopwatch watch)
    {
        var pooledObject = pool.GetPooledObject();
        watch = pooledObject.Object;
        return pooledObject;
    }

    public static PooledObject<StringBuilder> GetPooledObject(this ObjectPool<StringBuilder> pool)
        => new(pool);

    public static PooledObject<StringBuilder> GetPooledObject(
        this ObjectPool<StringBuilder> pool,
        out StringBuilder builder)
    {
        var pooledObject = pool.GetPooledObject();
        builder = pooledObject.Object;
        return pooledObject;
    }
}
