// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This is just internal utility type to reduce allocations and reduntant code
    /// </summary>
    internal static class Creator
    {
        public static PooledObject<HashSet<Checksum>> CreateChecksumSet(IEnumerable<Checksum> checksums)
        {
            var items = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

            items.Object.UnionWith(checksums);

            return items;
        }

        public static PooledObject<List<T>> CreateList<T>()
        {
            return SharedPools.Default<List<T>>().GetPooledObject();
        }
    }
}
