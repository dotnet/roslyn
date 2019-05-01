// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteFeatureOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\Remote\";

        public static readonly Option<bool> DiagnosticsEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(DiagnosticsEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DiagnosticsEnabled)));

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
            }

            // Otherwise, don't compute the index for any other workspaces.
            return false;
        }
    }
}
