// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal readonly record struct SortKey<T>(int Index, T Value)
{
    public static PooledArray<SortKey<T>> GetPooledArray(int minimumLength)
        => ArrayPool<SortKey<T>>.Shared.GetPooledArray(minimumLength);
}
