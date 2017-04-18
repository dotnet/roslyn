// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    internal abstract class AbstractPersistentStorageLocationService : IPersistentStorageLocationService
    {
        private static readonly object _gate = new object();
        private static readonly Dictionary<SolutionId, string> _idToStorageLocation = new Dictionary<SolutionId, string>();

        public string GetStorageLocation(Solution solution)
        {
            string result;
            _idToStorageLocation.TryGetValue(solution.Id, out result);
            return result;
        }

        public bool IsSupported(Workspace workspace)
        {
            lock (_gate)
            {
                return _idToStorageLocation.ContainsKey(workspace.CurrentSolution.Id);
            }
        }

        public static void UpdateStorageLocation(SolutionId id, string storageLocation)
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
                    _idToStorageLocation[id] = storageLocation;
                }
            }
        }
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService),  layer: WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteWorkspacePersistentStorageLocationService : AbstractPersistentStorageLocationService
    {
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: TemporaryWorkspace.WorkspaceKind_TemporaryWorkspace), Shared]
    internal class TemporaryWorkspacePersistentStorageLocationService : AbstractPersistentStorageLocationService
    {
    }
}