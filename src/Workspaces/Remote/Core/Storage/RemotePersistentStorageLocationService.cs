// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Storage
{
    // Have to re-export this in remote layer for it to be picked up.
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: WorkspaceKind.RemoteWorkspace), Shared]
    internal class RemoteWorkspacePersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteWorkspacePersistentStorageLocationService()
        {
        }

        public override bool IsSupported(Workspace workspace) => true;
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), layer: WorkspaceKind.RemoteTemporaryWorkspace), Shared]
    internal class RemoteTemporaryWorkspacePersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteTemporaryWorkspacePersistentStorageLocationService()
        {
        }

        public override bool IsSupported(Workspace workspace) => true;
    }
}
