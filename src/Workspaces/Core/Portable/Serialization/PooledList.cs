// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    /// <summary>
    /// This is just internal utility type to reduce allocations and reduntant code
    /// </summary>
    internal static class Creator
    {
        public static PooledObject<HashSet<Checksum>> CreateChecksumSet(IEnumerable<Checksum> checksums = null)
        {
            var items = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

            items.Object.UnionWith(checksums ?? SpecializedCollections.EmptyEnumerable<Checksum>());

            return items;
        }

        public static PooledObject<List<T>> CreateList<T>()
            => SharedPools.Default<List<T>>().GetPooledObject();

        public static PooledObject<Dictionary<Checksum, object>> CreateResultSet()
            => SharedPools.Default<Dictionary<Checksum, object>>().GetPooledObject();
    }
}
