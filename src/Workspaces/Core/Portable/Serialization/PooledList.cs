// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// This is just internal utility type to reduce allocations and reduntant code
    /// </summary>
    internal static class Creator
    {
        public static PooledObject<ConcurrentSet<Checksum>> CreateChecksumSet(IEnumerable<Checksum> checksums = null)
        {
            var items = SharedPools.Default<ConcurrentSet<Checksum>>().GetPooledObject();

            if (checksums != null)
                items.Object.AddRange(checksums);

            return items;
        }

        public static PooledObject<List<T>> CreateList<T>()
            => SharedPools.Default<List<T>>().GetPooledObject();

        public static PooledObject<ConcurrentDictionary<Checksum, object>> CreateResultSet()
            => SharedPools.Default<ConcurrentDictionary<Checksum, object>>().GetPooledObject();
    }
}
