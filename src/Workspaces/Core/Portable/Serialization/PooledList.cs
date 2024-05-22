// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// This is just internal utility type to reduce allocations and redundant code
/// </summary>
internal static class Creator
{
    public static PooledObject<HashSet<Checksum>> CreateChecksumSet(ReadOnlyMemory<Checksum> checksums)
    {
        var items = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

        var hashSet = items.Object;
        foreach (var checksum in checksums.Span)
            hashSet.Add(checksum);

        return items;
    }

    public static PooledObject<HashSet<Checksum>> CreateChecksumSet(Checksum checksum)
    {
        var items = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
        items.Object.Add(checksum);
        return items;
    }

    public static PooledObject<List<T>> CreateList<T>()
        => SharedPools.Default<List<T>>().GetPooledObject();

    public static PooledObject<Dictionary<Checksum, object>> CreateResultMap(out Dictionary<Checksum, object> result)
    {
        var pooled = SharedPools.Default<Dictionary<Checksum, object>>().GetPooledObject();
        result = pooled.Object;
        return pooled;
    }
}
