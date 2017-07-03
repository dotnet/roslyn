// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteFeatureOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Features\Remote\";

        /// <summary>
        /// Global switch that determines of OOP can be used for a language feature. Exposed through
        /// user visible switch to let people opt-in/out of this behavior.
        /// </summary>
        public static readonly Option<bool> OutOfProcessAllowed = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(OutOfProcessAllowed), defaultValue: false,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(OutOfProcessAllowed)));

        // Individual feature switches.  Not exposed to the user.  Supplied as an escape hatch for
        // features if necessary.  If all features use OOP then no indices will need to be built
        // within VS.  However, if any features need to run in VS, then we have to build our indices
        // in VS as well.

        public static readonly Option<bool> AddImportEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(AddImportEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(AddImportEnabled)));

        public static readonly Option<bool> DocumentHighlightingEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(DocumentHighlightingEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DocumentHighlightingEnabled)));

        public static readonly Option<bool> NavigateToEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(NavigateToEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NavigateToEnabled)));

        public static readonly Option<bool> SymbolSearchEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(SymbolSearchEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(SymbolSearchEnabled)));

        public static readonly Option<bool> SymbolFinderEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(SymbolFinderEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(SymbolFinderEnabled)));

        public static readonly Option<bool> DiagnosticsEnabled = new Option<bool>(
            nameof(RemoteFeatureOptions), nameof(DiagnosticsEnabled), defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(DiagnosticsEnabled)));

        private static ImmutableArray<Option<bool>> AllFeatureOptions { get; } =
            ImmutableArray.Create(AddImportEnabled, DocumentHighlightingEnabled, NavigateToEnabled, SymbolSearchEnabled, SymbolFinderEnabled);

        public static bool AnyFeatureRunsInProcess(Workspace workspace)
            => AllFeatureOptions.Any(o => !workspace.IsOutOfProcessEnabled(o));

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
                    // If any features are going to run in-process, then we need to create an index in the
                    // host workspace.
                    return AnyFeatureRunsInProcess(workspace);
            }

            return false;
        }
    }
}
