// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="Stopwatch"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class StopwatchPool : CustomObjectPool<Stopwatch>
{
    public static readonly StopwatchPool Default = Create();

    private StopwatchPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static StopwatchPool Create(Optional<int> poolSize = default)
        => new(Policy.Default, poolSize);

    public static StopwatchPool Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<Stopwatch> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<Stopwatch> GetPooledObject(out Stopwatch watch)
        => Default.GetPooledObject(out watch);
}
