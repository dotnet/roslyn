// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal abstract class CustomObjectPool<T> : DefaultObjectPool<T>
    where T : class
{
    protected const int DefaultPoolSize = DefaultPool.DefaultPoolSize;
    protected const int DefaultMaximumObjectSize = DefaultPool.DefaultMaximumObjectSize;

    protected CustomObjectPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize.HasValue ? poolSize.Value : DefaultPoolSize)
    {
    }

    public abstract class PooledObjectPolicy : IPooledObjectPolicy<T>
    {
        public abstract T Create();

        public abstract bool Return(T obj);
    }
}
