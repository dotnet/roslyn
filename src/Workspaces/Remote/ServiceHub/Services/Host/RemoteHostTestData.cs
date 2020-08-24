// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Test hook used to pass test data to remote services.
    /// </summary>
    internal sealed class RemoteHostTestData
    {
        public readonly AssetStorage AssetStorage;
        public readonly RemoteWorkspaceManager WorkspaceManager;
        public readonly bool IsInProc;

        public RemoteHostTestData(AssetStorage assetStorage, RemoteWorkspaceManager workspaceManager, bool isInProc)
        {
            AssetStorage = assetStorage;
            WorkspaceManager = workspaceManager;
            IsInProc = isInProc;
        }
    }
}
