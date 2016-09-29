// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private readonly ChecksumTreeCollection _trees = new ChecksumTreeCollection();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices, _trees);
        }

        internal class Service : ISolutionChecksumService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly ChecksumTreeCollection _treeCollection;

            public Service(HostWorkspaceServices workspaceServices, ChecksumTreeCollection trees)
            {
                _workspaceServices = workspaceServices;
                _treeCollection = trees;
            }

            public Serializer Serializer_TestOnly => new Serializer(_workspaceServices);

            public void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken)
            {
                _treeCollection.AddGlobalAsset(value, asset, cancellationToken);
            }

            public Asset GetGlobalAsset(object value, CancellationToken cancellationToken)
            {
                return _treeCollection.GetGlobalAsset(value, cancellationToken);
            }

            public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
            {
                _treeCollection.RemoveGlobalAsset(value, cancellationToken);
            }

            public async Task<ChecksumScope> CreateChecksumAsync(Solution solution, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionChecksumServiceFactory_CreateChecksumAsync, cancellationToken))
                {
                    var rootTreeNode = _treeCollection.CreateRootTreeNode(solution.State);

                    var builder = new ChecksumTreeBuilder(rootTreeNode);
                    var snapshot = new ChecksumScope(_treeCollection, rootTreeNode, await builder.BuildAsync(solution.State, cancellationToken).ConfigureAwait(false));

                    return snapshot;
                }
            }

            public ChecksumObject GetChecksumObject(Checksum checksum, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionChecksumServiceFactory_GetChecksumObject, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
                {
                    return _treeCollection.GetChecksumObject(checksum, cancellationToken);
                }
            }

            public IReadOnlyDictionary<Checksum, ChecksumObject> GetChecksumObjects(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.SolutionChecksumServiceFactory_GetChecksumObjects, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
                {
                    return _treeCollection.GetChecksumObjects(checksums, cancellationToken);
                }
            }
        }
    }
}
