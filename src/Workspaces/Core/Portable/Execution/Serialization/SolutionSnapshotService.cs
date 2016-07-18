// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionSnapshotService)), Shared]
    internal class SolutionSnapshotServiceFactory : IWorkspaceServiceFactory
    {
        private readonly SnapshotStorages _storages = new SnapshotStorages();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices, _storages);
        }

        internal class Service : ISolutionSnapshotService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly SnapshotStorages _storages;

            public Service(HostWorkspaceServices workspaceServices, SnapshotStorages storages)
            {
                _workspaceServices = workspaceServices;
                _storages = storages;
            }

            public Serializer Serializer_TestOnly => new Serializer(_workspaceServices);

            public void AddGlobalAsset(object value, Asset asset, CancellationToken cancellationToken)
            {
                _storages.AddGlobalAsset(value, asset, cancellationToken);
            }

            public Asset GetGlobalAsset(object value, CancellationToken cancellationToken)
            {
                return _storages.GetGlobalAsset(value, cancellationToken);
            }

            public void RemoveGlobalAsset(object value, CancellationToken cancellationToken)
            {
                _storages.RemoveGlobalAsset(value, cancellationToken);
            }

            public async Task<SolutionSnapshot> CreateSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                // TODO: remove stop watch and use logger
                var stopWatch = Stopwatch.StartNew();
                var snapshotStorage = _storages.CreateSnapshotStorage(solution);

                var builder = new SnapshotBuilder(snapshotStorage);
                var snapshot = new Snapshot(_storages, snapshotStorage, await builder.BuildAsync(solution, cancellationToken).ConfigureAwait(false));

                Debug.WriteLine(stopWatch.Elapsed);

                return snapshot;
            }

            public Task<ChecksumObject> GetChecksumObjectAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                return _storages.GetChecksumObjectAsync(checksum, cancellationToken);
            }

            internal void TestOnly_ClearCache()
            {
                _storages.TestOnly_ClearCache();
            }

            private class Snapshot : SolutionSnapshot
            {
                private readonly SnapshotStorages _storages;
                private readonly SnapshotStorage _storage;

                public Snapshot(SnapshotStorages storages, SnapshotStorage storage, SolutionSnapshotId id) : base(storage.Solution.Workspace, id)
                {
                    _storages = storages;
                    _storage = storage;

                    _storages.RegisterSnapshot(this, storage);
                }

                public override void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
                {
                    _storage.AddAdditionalAsset(asset, cancellationToken);
                }

                public override void Dispose()
                {
                    _storages.UnregisterSnapshot(this);
                }
            }
        }
    }
}
