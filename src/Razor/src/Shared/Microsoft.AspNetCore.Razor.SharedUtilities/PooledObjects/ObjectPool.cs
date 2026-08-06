// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal abstract class ObjectPool<T>
    where T : class
{
    public abstract T Get();

    public abstract void Return(T obj);
}

internal interface IPooledObjectPolicy<T>
    where T : class
{
    T Create();

    bool Return(T obj);
}

internal class DefaultObjectPool<T> : ObjectPool<T>
    where T : class
{
    private readonly Microsoft.CodeAnalysis.PooledObjects.ObjectPool<T> _pool;
    private readonly IPooledObjectPolicy<T> _policy;

    public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        ArgHelper.ThrowIfNull(policy);
        ArgHelper.ThrowIfNegativeOrZero(maximumRetained);

        _policy = policy;
        _pool = new(() => _policy.Create(), maximumRetained);
    }

    public override T Get()
        => _pool.Allocate();

    public override void Return(T obj)
    {
        ArgHelper.ThrowIfNull(obj);

        if (_policy.Return(obj))
        {
            _pool.Free(obj);
        }
        else
        {
            _pool.ForgetTrackedObject(obj);
        }
    }
}
