// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionSnapshotService)), Shared]
    internal class SolutionSnapshotServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices);
        }

        internal class Service : ISolutionSnapshotService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly SnapshotStorages _storages;

            public Service(HostWorkspaceServices workspaceServices)
            {
                _workspaceServices = workspaceServices;

                // TODO: storage should be shared between multiple workpsace
                _storages = new SnapshotStorages(new Serializer(workspaceServices));
            }

            public Serializer Serializer_TestOnly => _storages.Serializer;

            public async Task<SolutionSnapshot> CreateSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                // TODO: remove stop watch and use logger
                var stopWatch = Stopwatch.StartNew();
                var snapshotStorage = _storages.CreateSnapshotStorage(solution);

                var builder = new SnapshotBuilder(_storages.Serializer, snapshotStorage);
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

                public override void Dispose()
                {
                    _storages.UnregisterSnapshot(this);
                }
            }
        }
    }
}
