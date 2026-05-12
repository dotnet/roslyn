// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static class DefaultPool
{
    public const int DefaultPoolSize = 20;
    public const int DefaultMaximumObjectSize = 512;

    public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy, Optional<int> poolSize = default)
        where T : class
        => new DefaultObjectPool<T>(policy, poolSize.HasValue ? poolSize.Value : DefaultPoolSize);

    public static ObjectPool<T> Create<T>(Optional<int> poolSize = default)
        where T : class, IPoolableObject, new()
        => Create(new PoolableObjectPolicy<T>(static () => new()), poolSize);

    public static ObjectPool<T> Create<T>(Func<T> factory, Optional<int> poolSize = default)
        where T : class, IPoolableObject
        => Create(new PoolableObjectPolicy<T>(factory), poolSize);

    public static ObjectPool<T> Create<T, TArg>(TArg arg, Func<TArg, T> factory, Optional<int> poolSize = default)
        where T : class, IPoolableObject
        => Create(new PoolableObjectPolicy<T, TArg>(arg, factory), poolSize);

    private sealed class PoolableObjectPolicy<T>(Func<T> factory) : IPooledObjectPolicy<T>
        where T : class, IPoolableObject
    {
        public T Create() => factory();

        public bool Return(T obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class PoolableObjectPolicy<T, TArg>(TArg arg, Func<TArg, T> factory) : IPooledObjectPolicy<T>
        where T : class, IPoolableObject
    {
        public T Create() => factory(arg);

        public bool Return(T obj)
        {
            obj.Reset();
            return true;
        }
    }
}
