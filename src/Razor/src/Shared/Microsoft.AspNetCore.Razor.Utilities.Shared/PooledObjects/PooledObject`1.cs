// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github/dotnet/roslyn

using System;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal struct PooledObject<T> : IDisposable
    where T : class
{
    private readonly ObjectPool<T> _pool;
    private T? _object;

    // Because of how this API is intended to be used, we don't want the consumption code to have
    // to deal with Object being a nullable reference type. Instead, the guarantee is that this is
    // non-null until this is disposed.
    public readonly T Object => _object!;

    public PooledObject(ObjectPool<T> pool)
        : this()
    {
        _pool = pool;
        _object = pool.Get();
    }

    public void Dispose()
    {
        if (_object is { } obj)
        {
            _pool.Return(obj);
            _object = null;
        }
    }
}
