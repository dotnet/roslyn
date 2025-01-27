// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class SolutionAssetCacheTests
    {
        private static void ForceGC()
        {
            for (var i = 0; i < 3; i++)
                GC.Collect();
        }

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
                remoteWorkspace: null, cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2));

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

                ForceGC();
            }

            // it should not reach here
            AssertEx.Fail("asset not cleaned up");
        }

        [Fact]
        public async Task TestSolutionKeepsAssetPinned()
        {
            var workspace = new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());
            var solution = workspace.CurrentSolution;
            var checksums = await solution.CompilationState.GetStateChecksumsAsync(CancellationToken.None);

            // Ensure the lazy has computed its value.
            var storage = new SolutionAssetCache(
                workspace, cleanupInterval: TimeSpan.FromMilliseconds(1), purgeAfter: TimeSpan.FromMilliseconds(2));

            var checksum1 = checksums.Checksum;
            var data1 = new object();

            var checksum2 = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data2 = new object();

            Assert.Equal(data1, storage.GetOrAdd(checksum1, data1));
            Assert.Equal(data2, storage.GetOrAdd(checksum2, data2));

            var gotChecksum2 = true;

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                // We should always get this asset, as the solution is keeping it alive.
                Assert.True(storage.TryGetAsset(checksum1, out object current1));
                Assert.Equal(data1, current1);

                gotChecksum2 = storage.TryGetAsset(checksum2, out object _);
                ForceGC();
            }

            // By the end, checksum2/data2 should be gone.
            Assert.False(gotChecksum2);

            // Now, add a project.  At this point, the original pinned object should go away.
            workspace.SetCurrentSolution(solution => solution.AddProject("Project", "Assembly", LanguageNames.CSharp).Solution, WorkspaceChangeKind.ProjectAdded);

            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(10);

                // Eventually, this asset should go away.
                if (!storage.TryGetAsset(checksum1, out object _))
                    return;

                ForceGC();
            }

            // it should not reach here
            AssertEx.Fail("asset not cleaned up");
        }
    }
}
