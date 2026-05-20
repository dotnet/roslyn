// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="StringBuilder"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class StringBuilderPool : CustomObjectPool<StringBuilder>
{
    public static readonly StringBuilderPool Default = Create();

    private StringBuilderPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static StringBuilderPool Create(
        Optional<int> maximumObjectSize = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(maximumObjectSize), poolSize);

    public static StringBuilderPool Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<StringBuilder> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<StringBuilder> GetPooledObject(out StringBuilder builder)
        => Default.GetPooledObject(out builder);
}
