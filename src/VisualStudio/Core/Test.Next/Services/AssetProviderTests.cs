// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RemoteHost)]
public sealed class AssetProviderTests
{
    private static Workspace CreateRemoteWorkspace()
        => new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());

    [Fact]
    public Task TestCSharpParseOptionsSynchronization()
        => TestAssetAsync(Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default);

    [Fact]
    public Task TestVisualBasicParseOptionsSynchronization()
        => TestAssetAsync(Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions.Default);

    private static async Task TestAssetAsync(object data)
    {
        var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
        var checksum = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));

        using var workspace = TestWorkspace.CreateCSharp(file: @"");

        using var remoteWorkspace = CreateRemoteWorkspace();

        var storage = new SolutionAssetCache();
        var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), new Dictionary<Checksum, object>() { { checksum, data } });

        var provider = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);
        var stored = await provider.GetAssetAsync<object>(AssetPath.FullLookupForTesting, checksum, CancellationToken.None);
        Assert.Equal(data, stored);

        var stored2 = new List<(Checksum, object)>();
        await provider.GetAssetsAsync<object, VoidResult>(AssetPath.FullLookupForTesting, [checksum], (checksum, asset, _) => stored2.Add((checksum, asset)), default, CancellationToken.None);
        Assert.Equal(1, stored2.Count);

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

        var map = await solution.GetAssetMapAsync(projectConeId: null, CancellationToken.None);

        using var remoteWorkspace = CreateRemoteWorkspace();

        var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
        var storage = new SolutionAssetCache();
        var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

        var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);
        await service.GetAssetsAsync<object>(AssetPath.FullLookupForTesting, [.. map.Keys], CancellationToken.None);

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

        var map = await solution.GetAssetMapAsync(projectConeId: null, CancellationToken.None);

        using var remoteWorkspace = CreateRemoteWorkspace();

        var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
        var storage = new SolutionAssetCache();
        var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

        var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);
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

        var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);

        using var _ = ArrayBuilder<ProjectStateChecksums>.GetInstance(out var allProjectChecksums);
        allProjectChecksums.Add(await project.State.GetStateChecksumsAsync(CancellationToken.None));

        await service.SynchronizeProjectAssetsAsync(allProjectChecksums, CancellationToken.None);

        TestUtils.VerifyAssetStorage(map, storage);
    }

    [Fact]
    public async Task TestAssetArrayOrdering()
    {
        var code1 = @"class Test1 { void Method() { } }";
        var code2 = @"class Test2 { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp([code1, code2]);
        var project = workspace.CurrentSolution.Projects.First();

        await project.State.GetChecksumAsync(CancellationToken.None);

        var map = await project.GetAssetMapAsync(CancellationToken.None);

        using var remoteWorkspace = CreateRemoteWorkspace();

        var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
        var storage = new SolutionAssetCache();
        var assetSource = new OrderedAssetSource(workspace.Services.GetService<ISerializerService>(), map);

        var service = new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);

        using var _ = ArrayBuilder<ProjectStateChecksums>.GetInstance(out var allProjectChecksums);
        var stateChecksums = await project.State.GetStateChecksumsAsync(CancellationToken.None);

        var textChecksums = stateChecksums.Documents.TextChecksums;
        var textChecksumsReversed = new ChecksumCollection([.. textChecksums.Children.Reverse()]);

        var documents = await service.GetAssetsArrayAsync<SerializableSourceText>(
            AssetPath.FullLookupForTesting, textChecksums, CancellationToken.None);
        Assert.True(documents.Length == 2);

        storage.GetTestAccessor().Clear();
        var documentsReversed = await service.GetAssetsArrayAsync<SerializableSourceText>(
            AssetPath.FullLookupForTesting, textChecksumsReversed, CancellationToken.None);
        Assert.True(documentsReversed.Length == 2);

        Assert.True(documents.Select(d => d.ContentChecksum).SequenceEqual(documentsReversed.Reverse().Select(d => d.ContentChecksum)));
    }

    private sealed class OrderedAssetSource(
        ISerializerService serializerService,
        IReadOnlyDictionary<Checksum, object> map) : IAssetSource
    {
        public async ValueTask GetAssetsAsync<T, TArg>(
            Checksum solutionChecksum,
            AssetPath assetPath,
            ReadOnlyMemory<Checksum> checksums,
            ISerializerService deserializerService,
            Action<Checksum, T, TArg> callback,
            TArg arg,
            CancellationToken cancellationToken)
        {
            foreach (var (checksum, asset) in map)
            {
                if (checksums.Span.IndexOf(checksum) >= 0)
                {
                    using var stream = new MemoryStream();
                    using (var writer = new ObjectWriter(stream, leaveOpen: true))
                    {
                        serializerService.Serialize(asset, writer, cancellationToken);
                    }

                    stream.Position = 0;
                    using var reader = ObjectReader.GetReader(stream, leaveOpen: true);
                    var deserialized = deserializerService.Deserialize(asset.GetWellKnownSynchronizationKind(), reader, cancellationToken);
                    Contract.ThrowIfNull(deserialized);
                    callback(checksum, (T)deserialized, arg);
                }
            }
        }
    }
}
