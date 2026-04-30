// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static class PooledArrayBuilder
{
    public static PooledArrayBuilder<T> Create<T>(ReadOnlySpan<T> source)
    {
        var pooledArray = new PooledArrayBuilder<T>(source.Length);
        pooledArray.AddRange(source);
        return pooledArray;
    }
}
