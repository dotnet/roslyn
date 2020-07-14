// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(IRemotableDataService)), Shared]
    internal class RemotableDataServiceFactory : IWorkspaceServiceFactory
    {
        private readonly AssetStorages _assetStorages = new AssetStorages();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public RemotableDataServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new Service(workspaceServices, _assetStorages);

        internal class Service : IRemotableDataService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly AssetStorages _assetStorages;

            public ISerializerService Serializer_TestOnly => _workspaceServices.GetRequiredService<ISerializerService>();

            public Service(HostWorkspaceServices workspaceServices, AssetStorages storages)
            {
                _workspaceServices = workspaceServices;
                _assetStorages = storages;
            }

            public async ValueTask<PinnedRemotableDataScope> CreatePinnedRemotableDataScopeAsync(Solution solution, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionSynchronizationServiceFactory_CreatePinnedRemotableDataScopeAsync, cancellationToken))
                {
                    var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                    return PinnedRemotableDataScope.Create(_assetStorages, solution.State, checksum);
                }
            }

            public AssetStorages.Storage? TryGetStorage(int scopeId)
            {
                return _assetStorages.TryGetStorage(scopeId);
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
}
