// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    [Export(typeof(RemotePersistentStorageLocationService))]
    internal class RemotePersistentStorageLocationService : IPersistentStorageLocationService
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
                // Store the esent database in a different location for the out of proc server.
                _idToStorageLocation[id] = Path.Combine(storageLocation, "Server");
            }
        }
    }
}