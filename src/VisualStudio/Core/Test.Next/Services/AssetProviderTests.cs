// Licensed to the .NET Foundation under one or more agreements.
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
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class AssetProviderTests
    {
        private static Workspace CreateRemoteWorkspace()
            => new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());

        [Fact]
        public async Task TestCSharpParseOptionsSynchronization()
        {
            await TestAssetAsync(Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default);
        }

        [Fact]
        public async Task TestVisualBasicParseOptionsSynchronization()
        {
            await TestAssetAsync(Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions.Default);
        }

        private static async Task TestAssetAsync(object data)
        {
            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var checksum = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));

            using var workspace = TestWorkspace.CreateCSharp(file: @"");

            using var remoteWorkspace = CreateRemoteWorkspace();

            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), new Dictionary<Checksum, object>() { { checksum, data } });

            var provider = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            var stored = await provider.GetAssetAsync<object>(assetHint: AssetHint.None, checksum, CancellationToken.None);
            Assert.Equal(data, stored);

            var stored2 = await provider.GetAssetsAsync<object>(assetHint: AssetHint.None, new HashSet<Checksum> { checksum }, CancellationToken.None);
            Assert.Equal(1, stored2.Length);

            Assert.Equal(checksum, stored2[0].Item1);
            Assert.Equal(data, stored2[0].Item2);
        }

        [Fact]
        public async Task TestAssetSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var solution = workspace.CurrentSolution;

            // build checksum
            await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeAssetsAsync(assetHint: AssetHint.None, new HashSet<Checksum>(map.Keys), CancellationToken.None);

            foreach (var kv in map)
            {
                Assert.True(storage.TryGetAsset<object>(kv.Key, out _));
            }
        }

        [Fact]
        public async Task TestSolutionSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var solution = workspace.CurrentSolution;

            // build checksum
            await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeSolutionAssetsAsync(await solution.CompilationState.GetChecksumAsync(CancellationToken.None), CancellationToken.None);

            TestUtils.VerifyAssetStorage(map, storage);
        }

        [Fact]
        public async Task TestProjectSynchronization()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            var project = workspace.CurrentSolution.Projects.First();

            // build checksum
            await project.State.GetChecksumAsync(CancellationToken.None);

            var map = await project.GetAssetMapAsync(CancellationToken.None);

            using var remoteWorkspace = CreateRemoteWorkspace();

            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
            await service.SynchronizeProjectAssetsAsync(await project.State.GetStateChecksumsAsync(CancellationToken.None), CancellationToken.None);

            TestUtils.VerifyAssetStorage(map, storage);
        }
    }
}
