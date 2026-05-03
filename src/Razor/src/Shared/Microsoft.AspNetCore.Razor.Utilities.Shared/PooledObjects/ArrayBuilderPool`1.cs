// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="ImmutableArray{T}.Builder"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class ArrayBuilderPool<T> : CustomObjectPool<ImmutableArray<T>.Builder>
{
    public static readonly ArrayBuilderPool<T> Default = Create();

    private ArrayBuilderPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static ArrayBuilderPool<T> Create(
        Optional<int> initialCapacity = default,
        Optional<int> maximumObjectSize = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(initialCapacity, maximumObjectSize), poolSize);

    public static ArrayBuilderPool<T> Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<ImmutableArray<T>.Builder> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<ImmutableArray<T>.Builder> GetPooledObject(out ImmutableArray<T>.Builder builder)
        => Default.GetPooledObject(out builder);
}
