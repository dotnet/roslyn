// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteFeatureOptions
    {
        public static bool ShouldComputeIndex(Workspace workspace)
        {
            switch (workspace.Kind)
            {
                case WorkspaceKind.Test:
                    // Always compute indices in tests.
                    return true;

                case WorkspaceKind.RemoteWorkspace:
                    // Always compute indices in the remote workspace.
                    return true;

                case WorkspaceKind.Host:
                case WorkspaceKind.MSBuild:
                    // Compute indices in the host workspace when OOP is disabled.
                    var remoteHostClientService = workspace.Services.GetService<IRemoteHostClientService>();
                    if (remoteHostClientService is null)
                    {
                        return true;
                    }

                    return !remoteHostClientService.IsEnabled();
            }

            // Otherwise, don't compute the index for any other workspaces.
            return false;
        }
    }
}
