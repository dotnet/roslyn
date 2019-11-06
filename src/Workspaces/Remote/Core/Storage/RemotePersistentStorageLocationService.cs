// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionSize;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    // Have to re-export this in remote layer for it to be picked up.
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteWorkspacePersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: WorkspaceKind.RemoteTemporaryWorkspace), Shared]
    internal class RemoteTemporaryWorkspacePersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
    }

    [Export(typeof(ISolutionSizeTracker)), Shared]
    internal class RemoteWorkspaceSolutionSizeTracker : SolutionSizeTracker
    {
    }
}
