// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class SolutionAssetCacheTests
    {
        [Fact]
        public void TestGetAssets()
        {
            var storage = new SolutionAssetCache();

            var checksum = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.Equal(data, storage.GetOrAdd(checksum, data));

            Assert.True(storage.TryGetAsset(checksum, out object _));
        }

        [Fact]
        public async Task TestCleanup()
        {
            var storage = new SolutionAssetCache(
                remoteWorkspace: null, cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2), gcAfter: TimeSpan.FromMilliseconds(5));

            var checksum = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            Assert.Equal(data, storage.GetOrAdd(checksum, data));

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

        [Fact]
        public async Task TestSolutionKeepsAssetPinned()
        {
            var workspace = new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());
            var solution = workspace.CurrentSolution;
            var checksums = await solution.State.GetStateChecksumsAsync(CancellationToken.None);

            // Ensure the lazy has computed its value.
            var storage = new SolutionAssetCache(
                workspace, cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2), gcAfter: TimeSpan.FromMilliseconds(5));

            var data = new object();

            Assert.Equal(data, storage.GetOrAdd(checksums.Checksum, data));

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                // We should always get this asset, as the solution is keeping it alive.
                Assert.True(storage.TryGetAsset(checksums.Checksum, out object _));
            }
        }
    }
}
