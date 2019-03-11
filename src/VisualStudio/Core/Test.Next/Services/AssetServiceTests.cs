// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.DebugUtil;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class AssetServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAssets()
        {
            var sessionId = 0;
            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var data = new object();

            var storage = new AssetStorage();
            var source = new TestAssetSource(storage, checksum, data);

            var service = new AssetService(sessionId, storage, new RemoteWorkspace().Services.GetService<ISerializerService>());
            var stored = await service.GetAssetAsync<object>(checksum, CancellationToken.None);
            Assert.Equal(data, stored);

            var stored2 = await service.GetAssetsAsync<object>(new[] { checksum }, CancellationToken.None);
            Assert.Equal(1, stored2.Count);

            Assert.Equal(checksum, stored2[0].Item1);
            Assert.Equal(data, stored2[0].Item2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAssetSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var solution = workspace.CurrentSolution;

                // build checksum
                await solution.State.GetChecksumAsync(CancellationToken.None);

                var map = await solution.GetAssetMapAsync(CancellationToken.None);

                var sessionId = 0;
                var storage = new AssetStorage();
                var source = new TestAssetSource(storage, map);

                var service = new AssetService(sessionId, storage, new RemoteWorkspace().Services.GetService<ISerializerService>());
                await service.SynchronizeAssetsAsync(new HashSet<Checksum>(map.Keys), CancellationToken.None);

                foreach (var kv in map)
                {
                    Assert.True(storage.TryGetAsset(kv.Key, out object data));
                }
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSolutionSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var solution = workspace.CurrentSolution;

                // build checksum
                await solution.State.GetChecksumAsync(CancellationToken.None);

                var map = await solution.GetAssetMapAsync(CancellationToken.None);

                var sessionId = 0;
                var storage = new AssetStorage();
                var source = new TestAssetSource(storage, map);

                var service = new AssetService(sessionId, storage, new RemoteWorkspace().Services.GetService<ISerializerService>());
                await service.SynchronizeSolutionAssetsAsync(await solution.State.GetChecksumAsync(CancellationToken.None), CancellationToken.None);

                TestUtils.VerifyAssetStorage(map, storage);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestProjectSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var project = workspace.CurrentSolution.Projects.First();

                // build checksum
                await project.State.GetChecksumAsync(CancellationToken.None);

                var map = await project.GetAssetMapAsync(CancellationToken.None);

                var sessionId = 0;
                var storage = new AssetStorage();
                var source = new TestAssetSource(storage, map);

                var service = new AssetService(sessionId, storage, new RemoteWorkspace().Services.GetService<ISerializerService>());
                await service.SynchronizeProjectAssetsAsync(SpecializedCollections.SingletonEnumerable(await project.State.GetChecksumAsync(CancellationToken.None)), CancellationToken.None);

                TestUtils.VerifyAssetStorage(map, storage);
            }
        }
    }
}
