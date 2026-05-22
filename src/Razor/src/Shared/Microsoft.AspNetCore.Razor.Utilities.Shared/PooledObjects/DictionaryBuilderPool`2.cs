// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

/// <summary>
/// A pool of <see cref="ImmutableDictionary{TKey, TValue}.Builder"/> instances.
/// </summary>
/// 
/// <remarks>
/// Instances originating from this pool are intended to be short-lived and are suitable
/// for temporary work. Do not return them as the results of methods or store them in fields.
/// </remarks>
internal sealed partial class DictionaryBuilderPool<TKey, TValue> : CustomObjectPool<ImmutableDictionary<TKey, TValue>.Builder>
    where TKey : notnull
{
    public static readonly DictionaryBuilderPool<TKey, TValue> Default = Create();

    private DictionaryBuilderPool(PooledObjectPolicy policy, Optional<int> poolSize)
        : base(policy, poolSize)
    {
    }

    public static DictionaryBuilderPool<TKey, TValue> Create(
        Optional<IEqualityComparer<TKey>?> keyComparer = default,
        Optional<int> poolSize = default)
        => new(Policy.Create(keyComparer), poolSize);

    public static DictionaryBuilderPool<TKey, TValue> Create(PooledObjectPolicy policy, Optional<int> poolSize = default)
        => new(policy, poolSize);

    public static PooledObject<ImmutableDictionary<TKey, TValue>.Builder> GetPooledObject()
        => Default.GetPooledObject();

    public static PooledObject<ImmutableDictionary<TKey, TValue>.Builder> GetPooledObject(
        out ImmutableDictionary<TKey, TValue>.Builder builder)
        => Default.GetPooledObject(out builder);
}
