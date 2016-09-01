// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;

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
                _idToStorageLocation[id] = storageLocation;
            }
        }
    }
}