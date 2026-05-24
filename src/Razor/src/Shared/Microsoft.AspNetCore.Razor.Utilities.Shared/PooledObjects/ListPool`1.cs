// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="List{T}"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class ListPool<T> : CustomObjectPool<List<T>>
{
    public static readonly ListPool<T> Default = Create();

    private ListPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static ListPool<T> Create(
        Optional<int> maximumObjectSize = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(maximumObjectSize), poolSize);

    public static ListPool<T> Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<List<T>> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<List<T>> GetPooledObject(out List<T> list)
        => Default.GetPooledObject(out list);
}
