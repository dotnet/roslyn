// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="Stack{T}"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class StackPool<T> : CustomObjectPool<Stack<T>>
{
    public static readonly StackPool<T> Default = Create();

    private StackPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static StackPool<T> Create(
        Optional<int> maximumObjectSize = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(maximumObjectSize), poolSize);

    public static StackPool<T> Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<Stack<T>> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<Stack<T>> GetPooledObject(out Stack<T> stack)
        => Default.GetPooledObject(out stack);
}
