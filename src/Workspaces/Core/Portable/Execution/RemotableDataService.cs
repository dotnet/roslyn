// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Execution
{
    internal sealed class RemotableDataService : IRemotableDataService
    {
        [ExportWorkspaceServiceFactory(typeof(IRemotableDataService)), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly AssetStorages _assetStorages = new AssetStorages();

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new RemotableDataService(_assetStorages);
        }

        private readonly AssetStorages _assetStorages;

        private RemotableDataService(AssetStorages storages)
        {
            _assetStorages = storages;
        }

        public async ValueTask<PinnedRemotableDataScope> CreatePinnedRemotableDataScopeAsync(Solution solution, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SolutionSynchronizationServiceFactory_CreatePinnedRemotableDataScopeAsync, cancellationToken))
            {
                var storage = AssetStorages.CreateStorage(solution.State);
                var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                return PinnedRemotableDataScope.Create(_assetStorages, storage, checksum);
            }
        }

        public async ValueTask<RemotableData?> GetRemotableDataAsync(int scopeId, Checksum checksum, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SolutionSynchronizationService_GetRemotableData, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                return await _assetStorages.GetRemotableDataAsync(scopeId, checksum, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask<IReadOnlyDictionary<Checksum, RemotableData>> GetRemotableDataAsync(int scopeId, IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SolutionSynchronizationService_GetRemotableData, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                return await _assetStorages.GetRemotableDataAsync(scopeId, checksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask<RemotableData?> TestOnly_GetRemotableDataAsync(Checksum checksum, CancellationToken cancellationToken)
            => await _assetStorages.TestOnly_GetRemotableDataAsync(checksum, cancellationToken).ConfigureAwait(false);
    }
}
