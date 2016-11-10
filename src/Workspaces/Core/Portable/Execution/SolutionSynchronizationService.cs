﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionSynchronizationService)), Shared]
    internal class SolutionSynchronizationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly AssetStorages _assetStorages = new AssetStorages();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices, _assetStorages);
        }

        internal class Service : ISolutionSynchronizationService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly AssetStorages _assetStorages;

            public Serializer Serializer_TestOnly => new Serializer(_workspaceServices);

            public Service(HostWorkspaceServices workspaceServices, AssetStorages trees)
            {
                _workspaceServices = workspaceServices;
                _assetStorages = trees;
            }

            public void AddGlobalAsset(object value, CustomAsset asset, CancellationToken cancellationToken)
            {
                _assetStorages.AddGlobalAsset(value, asset, cancellationToken);
            }

            public CustomAsset GetGlobalAsset(object value, CancellationToken cancellationToken)
            {
                return _assetStorages.GetGlobalAsset(value, cancellationToken);
            }

            public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
            {
                _assetStorages.RemoveGlobalAsset(value, cancellationToken);
            }

            public async Task<PinnedRemotableDataScope> CreatePinnedRemotableDataScopeAsync(Solution solution, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionSynchronizationServiceFactory_CreatePinnedRemotableDataScopeAsync, cancellationToken))
                {
                    var storage = _assetStorages.CreateStorage(solution.State);
                    var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                    var snapshot = new PinnedRemotableDataScope(_assetStorages, storage, checksum);
                    return snapshot;
                }
            }

            public RemotableData GetRemotableData(Checksum checksum, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionSynchronizationService_GetRemotableData, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
                {
                    return _assetStorages.GetRemotableData(scope: null, checksum: checksum, cancellationToken: cancellationToken);
                }
            }

            public IReadOnlyDictionary<Checksum, RemotableData> GetRemotableData(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionSynchronizationService_GetRemotableData, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
                {
                    return _assetStorages.GetRemotableData(scope: null, checksums: checksums, cancellationToken: cancellationToken);
                }
            }
        }
    }
}
