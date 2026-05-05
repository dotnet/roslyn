// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="Dictionary{TKey, TValue}"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class DictionaryPool<TKey, TValue> : CustomObjectPool<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    public static readonly DictionaryPool<TKey, TValue> Default = Create();

    private DictionaryPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static DictionaryPool<TKey, TValue> Create(
        Optional<IEqualityComparer<TKey>?> comparer = default,
        Optional<int> maximumObjectSize = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(comparer, maximumObjectSize), poolSize);

    public static DictionaryPool<TKey, TValue> Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject(out Dictionary<TKey, TValue> map)
        => Default.GetPooledObject(out map);
}
