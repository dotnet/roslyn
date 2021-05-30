﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class AssetProviderTests
    {
        private static Workspace CreateRemoteWorkspace()
            => new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices(), WorkspaceKind.RemoteWorkspace);

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCSharpParseOptionsSynchronization()
        {
            await TestAssetAsync(Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestVisualBasicParseOptionsSynchronization()
        {
            await TestAssetAsync(Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions.Default);
        }

        private async Task TestAssetAsync(object data)
        {
            var sessionId = 0;
            var checksum = Checksum.Create(WellKnownSynchronizationKind.Null, ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));

            using var workspace = TestWorkspace.CreateCSharp(file: @"");

            using var remoteWorkspace = CreateRemoteWorkspace();

            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), new Dictionary<Checksum, object>() { { checksum, data } });

            var provider = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            var stored = await provider.GetAssetAsync<object>(checksum, CancellationToken.None);
            Assert.Equal(data, stored);

            var stored2 = await provider.GetAssetsAsync<object>(new[] { checksum }, CancellationToken.None);
            Assert.Equal(1, stored2.Count);

            Assert.Equal(checksum, stored2[0].Item1);
            Assert.Equal(data, stored2[0].Item2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAssetSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var solution = workspace.CurrentSolution;

            // build checksum
            await solution.State.GetChecksumAsync(CancellationToken.None);

            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = 0;
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeAssetsAsync(new HashSet<Checksum>(map.Keys), CancellationToken.None);

            foreach (var kv in map)
            {
                Assert.True(storage.TryGetAsset<object>(kv.Key, out _));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSolutionSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var solution = workspace.CurrentSolution;

            // build checksum
            await solution.State.GetChecksumAsync(CancellationToken.None);

            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = 0;
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeSolutionAssetsAsync(await solution.State.GetChecksumAsync(CancellationToken.None), CancellationToken.None);

            TestUtils.VerifyAssetStorage(map, storage);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestProjectSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var project = workspace.CurrentSolution.Projects.First();

            // build checksum
            await project.State.GetChecksumAsync(CancellationToken.None);

            var map = await project.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = 0;
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeProjectAssetsAsync(SpecializedCollections.SingletonEnumerable(await project.State.GetChecksumAsync(CancellationToken.None)), CancellationToken.None);

            TestUtils.VerifyAssetStorage(map, storage);
        }
    }
}
