// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests
{
    internal static class TestUtils
    {
        public static void VerifyAssetStorage<T>(IEnumerable<KeyValuePair<Checksum, T>> items, SolutionAssetCache storage)
        {
            foreach (var kv in items)
            {
                if (kv.Value is ChecksumCollection)
                {
                    // ChecksumCollection itself won't be in asset storage. since
                    // it will be never asked from OOP side to host to sync. 
                    // the collection is already part of 
                    // Solution/Project/DocumentStateCheckum so syncing
                    // state checksum automatically bring in the collection.
                    // it only exist to calculate hierarchical checksum
                    continue;
                }

                Assert.True(storage.TryGetAsset(kv.Key, out object _));
            }
        }
    }
}
