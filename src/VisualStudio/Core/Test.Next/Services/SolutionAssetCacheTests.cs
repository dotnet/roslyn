// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class SolutionAssetCacheTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestGetAssets()
        {
            var storage = new SolutionAssetCache();

            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            Assert.True(storage.TryGetAsset(checksum, out object _));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCleanup()
        {
            var storage = new SolutionAssetCache(cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2), gcAfter: TimeSpan.FromMilliseconds(5));

            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.True(storage.TryAddAsset(checksum, data));

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                if (!storage.TryGetAsset(checksum, out object _))
                {
                    // asset is deleted
                    return;
                }
            }

            // it should not reach here
            Assert.True(false, "asset not cleaned up");
        }
    }
}
