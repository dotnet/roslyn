// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.RemoteHost
{
    internal class SolutionAssetManager
    {
        private readonly string _fileName;
        private readonly Solution _solution;

        public static async Task<Checksum> SaveAsync(string fileName, Solution solution, CancellationToken cancellationToken)
        {
            var manager = new SolutionAssetManager(fileName, solution);

            return await manager.SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<Solution> LoadAsync(string fileName, Solution baseSolution, CancellationToken cancellationToken)
        {
            var manager = new SolutionAssetManager(fileName, baseSolution);

            return await manager.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        private SolutionAssetManager(string fileName, Solution solution)
        {
            _fileName = fileName;
            _solution = solution;
        }

        private async Task<Solution> LoadAsync(CancellationToken cancellationToken)
        {
            var adhocWorkspace = new AdhocWorkspace();
            var serializer = adhocWorkspace.Services.GetService<ISerializerService>();

            var solutionChecksum = default(Checksum);
            var map = new Dictionary<Checksum, object>();

            using (var stream = new FileStream(_fileName, FileMode.Open))
            using (var compressed = new DeflateStream(stream, CompressionMode.Decompress))
            using (var reader = ObjectReader.TryGetReader(compressed, cancellationToken))
            {
                // save root checksum and number of entries
                solutionChecksum = Checksum.ReadFrom(reader);

                // number of items in the package
                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var itemChecksum = Checksum.ReadFrom(reader);

                    var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                    // in service hub, cancellation means simply closed stream
                    var @object = serializer.Deserialize<object>(kind, reader, cancellationToken);

                    Debug.Assert(itemChecksum == serializer.CreateChecksum(@object, cancellationToken));

                    map.Add(itemChecksum, @object);
                }
            }

            var assetSource = new SimpleAssetSource(AssetStorage.Default, map);
            var assetService = new AssetService(scopeId: 0, AssetStorage.Default, serializer);

            var solutionCreator = new SolutionCreator(assetService, _solution, cancellationToken);

            // check whether solution is update to the given base solution
            if (await solutionCreator.IsIncrementalUpdateAsync(solutionChecksum).ConfigureAwait(false))
            {
                // create updated solution off the baseSolution
                return await solutionCreator.CreateSolutionAsync(solutionChecksum).ConfigureAwait(false);
            }

            // get new solution info
            var solutionInfo = await SolutionInfoCreator.CreateSolutionInfoAsync(assetService, solutionChecksum, cancellationToken).ConfigureAwait(false);

            // otherwise, just return new solution
            return adhocWorkspace.AddSolution(solutionInfo);
        }

        private async Task<Checksum> SaveAsync(CancellationToken cancellationToken)
        {
            var adhocWorkspace = new AdhocWorkspace();

            // do this so that we don't host specific optimizatino such as memory map files and etc
            var solutionWithDefaultServices = _solution.WithNewWorkspace(adhocWorkspace, _solution.WorkspaceVersion + 1);

            var checksum = await solutionWithDefaultServices.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

            var remotableDataService = solutionWithDefaultServices.Workspace.Services.GetService<IRemotableDataService>();
            var serializer = solutionWithDefaultServices.Workspace.Services.GetService<ISerializerService>();

            // currently signing key files or xml doc files are not part of asset. we should add those as well at some point.
            // currently analyzer won't work since we only put filepath rather than whole dll in the asset. and depdendency needs to work out
            // sourcetext checksum doesn't work unless it is desktop since sourcetext checksum uses endcoding and there is no way to serialize encoding
            // unless it is desktop. core layer should use different checksum than SourceText.GetChecksum
            var assetMap = await solutionWithDefaultServices.GetAssetMapAsync(cancellationToken).ConfigureAwait(false);
            using (var scope = await remotableDataService.CreatePinnedRemotableDataScopeAsync(solutionWithDefaultServices, cancellationToken).ConfigureAwait(false))
            {
                using (var stream = new FileStream(_fileName, FileMode.Create))
                using (var compressed = new DeflateStream(stream, CompressionMode.Compress))
                using (var writer = new ObjectWriter(compressed, cancellationToken))
                {
                    // save root checksum and number of entries
                    checksum.WriteTo(writer);

                    writer.WriteInt32(assetMap.Count);

                    foreach (var kv in assetMap)
                    {
                        // right now, solution is created with full desktop service (mmf support), but we want to save without desktop service (as bits) which cause
                        // some issues. this work around that issue.

                        // get new checksum that is based on only bits
                        var itemChecksum = serializer.CreateChecksum(kv.Value, cancellationToken);
                        itemChecksum.WriteTo(writer);

                        var remotableData = scope.GetRemotableData(kv.Key, cancellationToken);
                        Debug.Assert(itemChecksum == remotableData.Checksum);

                        // save kind
                        writer.WriteInt32((int)remotableData.Kind);

                        // save raw bits
                        await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);

                        // verification
                        await VerifyAsync(remotableData, serializer, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return checksum;
        }

        private async Task VerifyAsync(RemotableData remotableData, ISerializerService serializerService, CancellationToken cancellationToken)
        {
            using (var stream = new MemoryStream())
            using (var writer = new ObjectWriter(stream, cancellationToken))
            {
                await remotableData.WriteObjectToAsync(writer, cancellationToken).ConfigureAwait(false);

                stream.Position = 0;

                using (var reader = ObjectReader.TryGetReader(stream, cancellationToken))
                {
                    var value = serializerService.Deserialize<object>(remotableData.Kind, reader, cancellationToken);
                    var checksum = serializerService.CreateChecksum(value, cancellationToken);

                    Debug.Assert(checksum == remotableData.Checksum);
                }
            }
        }
    }
}
