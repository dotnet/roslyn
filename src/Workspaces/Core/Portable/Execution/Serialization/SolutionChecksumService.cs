// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionChecksumService)), Shared]
    internal class SolutionChecksumServiceFactory : IWorkspaceServiceFactory
    {
        private readonly ChecksumTreeNodeCacheCollection _caches = new ChecksumTreeNodeCacheCollection();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices, _caches);
        }

        internal class Service : ISolutionChecksumService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly ChecksumTreeNodeCacheCollection _caches;

            public Service(HostWorkspaceServices workspaceServices, ChecksumTreeNodeCacheCollection caches)
            {
                _workspaceServices = workspaceServices;
                _caches = caches;
            }

            public Serializer Serializer_TestOnly => new Serializer(_workspaceServices);

            public void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken)
            {
                _caches.AddGlobalAsset(value, asset, cancellationToken);
            }

            public Asset GetGlobalAsset(object value, CancellationToken cancellationToken)
            {
                return _caches.GetGlobalAsset(value, cancellationToken);
            }

            public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
            {
                _caches.RemoveGlobalAsset(value, cancellationToken);
            }

            public async Task<ChecksumScope> CreateChecksumAsync(Solution solution, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionChecksumServiceFactory_CreateChecksumAsync, cancellationToken))
                {
                    var cache = _caches.CreateRootTreeNodeCache(solution);

                    var builder = new SnapshotBuilder(cache);
                    var snapshot = new ChecksumScope(_caches, cache, await builder.BuildAsync(solution, cancellationToken).ConfigureAwait(false));

                    return snapshot;
                }
            }

            public ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionChecksumServiceFactory_GetChecksumObject, GetChecksumLogInfo, checksum, cancellationToken))
                {
                    return _caches.GetChecksumObject(checksum, cancellationToken);
                }
            }

            private static string GetChecksumLogInfo(Checksum checksum)
            {
                return checksum.ToString();
            }
        }
    }
}
