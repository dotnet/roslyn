// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static partial class SpecializedPools
{
    /// <summary>
    /// A pool of <see cref="HashSet{T}"/> instances that compares items using reference equality.
    /// </summary>
    /// 
    /// <remarks>
    /// Instances originating from this pool are intended to be short-lived and are suitable
    /// for temporary work. Do not return them as the results of methods or store them in fields.
    /// </remarks>
    internal static class ReferenceEqualityHashSet<T>
        where T : class
    {
        public static readonly HashSetPool<T> Default = HashSetPool<T>.Create(ReferenceEqualityComparer<T>.Instance);

        public static PooledObject<HashSet<T>> GetPooledObject()
            => Default.GetPooledObject();

        public static PooledObject<HashSet<T>> GetPooledObject(out HashSet<T> set)
            => Default.GetPooledObject(out set);
    }
}
