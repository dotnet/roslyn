// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private class JsonRpcAssetSource : AssetSource
        {
            private readonly SnapshotService _owner;

            public JsonRpcAssetSource(SnapshotService owner, int sessionId) :
                base(owner.AssetStorage, sessionId)
            {
                _owner = owner;
            }

            public override async Task<IList<ValueTuple<Checksum, object>>> RequestAssetsAsync(int sessionId, ISet<Checksum> checksums, CancellationToken callerCancellationToken)
            {
                // it should succeed as long as matching VS is alive
                // TODO: add logging mechanism using Logger

                // this can be called in two ways. 
                // 1. Connection to get asset is closed (the asset source we were using is disconnected - _assetChannelCancellationToken)
                //    if this asset source's channel is closed, service will move to next asset source to get the asset as long as callerCancellationToken
                //    is not cancelled
                //
                // 2. Request to required this asset has cancelled. (callerCancellationToken)
                using (var mergedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_owner.CancellationToken, callerCancellationToken))
                using (RoslynLogger.LogBlock(FunctionId.SnapshotService_RequestAssetAsync, GetRequestLogInfo, sessionId, checksums, mergedCancellationToken.Token))
                {
                    return await _owner.Rpc.InvokeAsync(WellKnownServiceHubServices.AssetService_RequestAssetAsync,
                        new object[] { sessionId, checksums.ToArray() },
                        (s, c) => ReadAssets(s, sessionId, checksums, c), mergedCancellationToken.Token).ConfigureAwait(false);
                }
            }

            private IList<ValueTuple<Checksum, object>> ReadAssets(
                Stream stream, int sessionId, ISet<Checksum> checksums, CancellationToken cancellationToken)
            {
                var results = new List<ValueTuple<Checksum, object>>();

                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    Debug.Assert(reader != null,
@"We only ge a reader for data transmitted between live processes.
This data should always be correct as we're never persisting the data between sessions.");

                    var responseSessionId = reader.ReadInt32();
                    Contract.ThrowIfFalse(sessionId == responseSessionId);

                    var count = reader.ReadInt32();
                    Contract.ThrowIfFalse(count == checksums.Count);

                    for (var i = 0; i < count; i++)
                    {
                        var responseChecksum = Checksum.ReadFrom(reader);
                        Contract.ThrowIfFalse(checksums.Contains(responseChecksum));

                        var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                        // in service hub, cancellation means simply closed stream
                        var @object = _owner.RoslynServices.AssetService.Deserialize<object>(kind, reader, cancellationToken);

                        results.Add(ValueTuple.Create(responseChecksum, @object));
                    }

                    return results;
                }
            }

            private static string GetRequestLogInfo(int serviceId, IEnumerable<Checksum> checksums)
            {
                return $"{serviceId} - {Checksum.GetChecksumsLogInfo(checksums)}";
            }
        }
    }
}