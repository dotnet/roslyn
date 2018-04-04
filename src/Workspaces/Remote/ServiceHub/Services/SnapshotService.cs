// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Snapshot service in service hub side.
    /// 
    /// this service will be used to move over remotable data from client to service hub
    /// </summary>
    internal partial class SnapshotService : ServiceHubServiceBase, ISnapshotService
    {
        private readonly AssetSource _source;

        public SnapshotService(Stream stream, IServiceProvider serviceProvider) :
            base(serviceProvider, stream)
        {
            _source = new JsonRpcAssetSource(this);

            Rpc.StartListening();
        }

        public Task SynchronizePrimaryWorkspaceAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizePrimaryWorkspaceAsync, Checksum.GetChecksumLogInfo, checksum, token))
                {
                    var solutionController = (ISolutionController)RoslynServices.SolutionService;
                    await solutionController.UpdatePrimaryWorkspaceAsync(checksum, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task SynchronizeGlobalAssetsAsync(Checksum[] checksums, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (RoslynLogger.LogBlock(FunctionId.RemoteHostService_SynchronizeGlobalAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, token))
                {
                    var assets = await RoslynServices.AssetService.GetAssetsAsync<object>(checksums, token).ConfigureAwait(false);

                    foreach (var asset in assets)
                    {
                        AssetStorage.TryAddGlobalAsset(asset.Item1, asset.Item2);
                    }
                }
            }, cancellationToken);
        }
    }
}
