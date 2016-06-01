// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(ISolutionSnapshotService)), Shared]
    internal class SolutionSnapshotService : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices);
        }

        private class Service : ISolutionSnapshotService
        {
            private readonly HostWorkspaceServices _workspaceServices;
            private readonly Serializer _serializer;
            private readonly SnapshotStorages _storages;

            public Service(HostWorkspaceServices workspaceServices)
            {
                _workspaceServices = workspaceServices;

                _serializer = new Serializer(workspaceServices);
                _storages = new SnapshotStorages(_serializer);
            }

            public async Task<SolutionSnapshot> CreateSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                var snapshotStorage = _storages.CreateSnapshotStorage(solution);

                var builder = new SnapshotBuilder(_serializer, snapshotStorage);
                return new Snapshot(_storages, snapshotStorage, await builder.BuildAsync(solution, cancellationToken).ConfigureAwait(false));
            }

            public Task<ChecksumObject> GetChecksumObjectAsync(Checksum checksum, CancellationToken cancellationToken)
            {
                return _storages.GetChecksumObjectAsync(checksum, cancellationToken);
            }

            private class Snapshot : SolutionSnapshot
            {
                private readonly SnapshotStorages _storages;
                private readonly SnapshotStorage _storage;

                public Snapshot(SnapshotStorages storages, SnapshotStorage storage, SolutionSnapshotId id) : base(id)
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
