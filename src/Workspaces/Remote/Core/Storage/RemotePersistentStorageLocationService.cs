// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemotePersistentStorageLocationService : IPersistentStorageLocationService
    {
        private readonly object _gate = new object();
        private readonly Dictionary<SolutionId, string> _idToStorageLocation = new Dictionary<SolutionId, string>();

        [ImportingConstructor]
        public RemotePersistentStorageLocationService()
        {
        }

        public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;

        public string TryGetStorageLocation(SolutionId solutionId)
        {
            _idToStorageLocation.TryGetValue(solutionId, out var result);
            return result;
        }

        public bool IsSupported(Workspace workspace)
        {
            lock (_gate)
            {
                return _idToStorageLocation.ContainsKey(workspace.CurrentSolution.Id);
            }
        }

        public void UpdateStorageLocation(SolutionId id, string storageLocation)
        {
            lock (_gate)
            {
                // We can get null when the solution has no corresponding file location
                // in the host process.  This is not abnormal and can come around for
                // many reasons.  In that case, we simply do not store a storage location
                // for this solution, indicating to all remote consumers that persistent
                // storage is not available for this solution.
                if (storageLocation == null)
                {
                    _idToStorageLocation.Remove(id);
                }
                else
                {
                    // Store the esent database in a different location for the out of proc server.
                    storageLocation = Path.Combine(storageLocation, "Server");
                    _idToStorageLocation[id] = storageLocation;
                }
            }

            StorageLocationChanging?.Invoke(this, new PersistentStorageLocationChangingEventArgs(id, storageLocation, mustUseNewStorageLocationImmediately: false));
        }
    }
}
