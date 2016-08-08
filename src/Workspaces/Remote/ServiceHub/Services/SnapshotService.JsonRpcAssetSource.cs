// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using StreamJsonRpc;
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
            private readonly JsonRpc _rpc;
            private readonly TraceSource _logger;
            private readonly CancellationToken _assetChannelCancellationToken;

            public JsonRpcAssetSource(JsonRpc rpc, TraceSource logger, CancellationToken assetChannelCancellationToken)
            {
                _rpc = rpc;
                _logger = logger;
                _assetChannelCancellationToken = assetChannelCancellationToken;
            }

            public override async Task<object> RequestAssetAsync(int serviceId, Checksum checksum, CancellationToken callerCancellationToken)
            {
                // it should succeed as long as matching VS is alive
                // TODO: add logging mechanism using Logger

                // this can be called in two ways. 
                // 1. Connection to get asset is closed (the asset source we were using is disconnected - _assetChannelCancellationToken)
                //    if this asset source's channel is closed, service will move to next asset source to get the asset as long as callerCancellationToken
                //    is not cancelled
                //
                // 2. Request to required this asset has cancelled. (callerCancellationToken)
                using (var mergedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_assetChannelCancellationToken, callerCancellationToken))
                using (RoslynLogger.LogBlock(FunctionId.SnapshotService_RequestAssetAsync, GetRequestLogInfo, serviceId, checksum, mergedCancellationToken.Token))
                {
                    return await _rpc.InvokeAsync(WellKnownServiceHubServices.AssetService_RequestAssetAsync,
                        new object[] { serviceId, checksum.ToArray() },
                        (s, c) => ReadAssetAsync(s, _logger, serviceId, checksum, c), mergedCancellationToken.Token).ConfigureAwait(false);
                }
            }

            private static Task<object> ReadAssetAsync(
                Stream stream, TraceSource logger, int serviceId, Checksum checksum, CancellationToken cancellationToken)
            {
                using (var reader = new ObjectReader(stream))
                {
                    var responseServiceId = reader.ReadInt32();
                    Contract.ThrowIfFalse(serviceId == responseServiceId);

                    var responseChecksum = new Checksum(reader.ReadArray<byte>());
                    Contract.ThrowIfFalse(checksum == responseChecksum);

                    var kind = reader.ReadString();

                    // in service hub, cancellation means simply closed stream
                    var @object = RoslynServices.AssetService.Deserialize<object>(kind, reader, cancellationToken);

                    return Task.FromResult(@object);
                }
            }

            private static string GetRequestLogInfo(int serviceId, Checksum checksum)
            {
                return $"{serviceId} - {checksum.ToString()}";
            }
        }
    }
}