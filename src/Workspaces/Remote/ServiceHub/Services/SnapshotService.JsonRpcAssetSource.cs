// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Snapshot service in service hub side.
    /// 
    /// this service will be used to move over snapshot data from client to service hub
    /// </summary>
    internal partial class SnapshotService
    {
        private sealed class JsonRpcAssetSource : AssetSource
        {
            private readonly SnapshotService _owner;

            public JsonRpcAssetSource(SnapshotService owner) : base(owner.AssetStorage)
            {
                _owner = owner;
            }

            public override async Task<IList<(Checksum, object)>> RequestAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
            {
                return await _owner.RunServiceAsync(() =>
                {
                    using (RoslynLogger.LogBlock(FunctionId.SnapshotService_RequestAssetAsync, GetRequestLogInfo, scopeId, checksums, cancellationToken))
                    {
                        return _owner.EndPoint.InvokeAsync(
                            WellKnownServiceHubServices.AssetService_RequestAssetAsync,
                            new object[] { scopeId, checksums.ToArray() },
                            (stream, cancellationToken) => Task.FromResult(ReadAssets(stream, scopeId, checksums, serializerService, cancellationToken)),
                            cancellationToken);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }

            public override async Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
            {
                return await _owner.RunServiceAsync(() =>
                {
                    using (RoslynLogger.LogBlock(FunctionId.SnapshotService_IsExperimentEnabledAsync, experimentName, cancellationToken))
                    {
                        return _owner.EndPoint.InvokeAsync<bool>(
                            WellKnownServiceHubServices.AssetService_IsExperimentEnabledAsync,
                            new object[] { experimentName },
                            cancellationToken);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }

            private static IList<(Checksum, object)> ReadAssets(
                Stream stream,
                int scopeId,
                ISet<Checksum> checksums,
                ISerializerService serializerService,
                CancellationToken cancellationToken)
            {
                var results = new List<(Checksum, object)>();

                using var reader = ObjectReader.TryGetReader(stream, leaveOpen: true, cancellationToken);

                // We only get a reader for data transmitted between live processes.
                // This data should always be correct as we're never persisting the data between sessions.
                Contract.ThrowIfNull(reader);

                var responseScopeId = reader.ReadInt32();
                Contract.ThrowIfFalse(scopeId == responseScopeId);

                var count = reader.ReadInt32();
                Contract.ThrowIfFalse(count == checksums.Count);

                for (var i = 0; i < count; i++)
                {
                    var responseChecksum = Checksum.ReadFrom(reader);
                    Contract.ThrowIfFalse(checksums.Contains(responseChecksum));

                    var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                    // in service hub, cancellation means simply closed stream
                    var result = serializerService.Deserialize<object>(kind, reader, cancellationToken);

                    results.Add((responseChecksum, result));
                }

                return results;
            }

            private static string GetRequestLogInfo(int serviceId, IEnumerable<Checksum> checksums)
            {
                return $"{serviceId} - {Checksum.GetChecksumsLogInfo(checksums)}";
            }
        }
    }
}
