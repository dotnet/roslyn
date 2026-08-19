// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    /// <summary>
    /// State transitions:
    /// <see cref="ProjectLoadState.Primordial"/> without an active operation -> <see cref="ProjectLoadState.Primordial"/> with an active operation
    /// <see cref="ProjectLoadState.Primordial"/> with an active operation -> <see cref="ProjectLoadState.LoadedTargets"/> or <see cref="ProjectLoadState.Failed"/>
    /// <see cref="ProjectLoadState.Loading"/> -> <see cref="ProjectLoadState.LoadedTargets"/> or <see cref="ProjectLoadState.Failed"/>
    /// <see cref="ProjectLoadState.Failed"/> -> <see cref="ProjectLoadState.Primordial"/> or <see cref="ProjectLoadState.Loading"/> when retrying a failed load
    /// <see cref="ProjectLoadState.LoadedTargets"/> -> <see cref="ProjectLoadState.LoadedTargets"/> after a subsequent design-time build
    /// Any state -> unloaded (which is denoted by removing the <see cref="_loadedProjects"/> entry for the project)
    /// </summary>
    protected abstract record ProjectLoadState
    {
        /// <summary>
        /// Represents a project which has not yet had a design-time build performed for it,
        /// and which has an associated "primordial project" in the workspace.
        /// </summary>
        /// <param name="PrimordialProjectFactory">
        /// The project factory for the workspace that the primordial project lives within. This
        /// factory was not used to create the project, but still needs to be used during removal to avoid locking issues.
        /// </param>
        /// <param name="PrimordialProjectId">
        /// ID of the project which LSP uses to fulfill requests until the first design-time build is complete.
        /// The project with this ID is removed from the workspace when unloading or when transitioning to <see cref="LoadedTargets"/> state.
        /// </param>
        public sealed record Primordial(ProjectSystemProjectFactory PrimordialProjectFactory, ProjectId PrimordialProjectId, ProjectLoadOperation? LoadOperation) : ProjectLoadState;

        public sealed record Loading(ProjectLoadOperation LoadOperation) : ProjectLoadState;

        /// <summary>
        /// Represents a project for which we have loaded zero or more targets.
        /// A project with zero loaded targets completed evaluation successfully but produced no target frameworks.
        /// Incrementally updated upon subsequent design-time builds.
        /// The <see cref="LoadedProjectTargets"/> are disposed when unloading.
        /// </summary>
        /// <param name="LoadedProjectTargets">List of target frameworks which have been loaded for this project so far.</param>
        public sealed record LoadedTargets(ImmutableArray<LoadedProject> LoadedProjectTargets) : ProjectLoadState;

        public sealed record Failed(
            LanguageServerProjectLoadResult Result,
            ProjectSystemProjectFactory? PrimordialProjectFactory = null,
            ProjectId? PrimordialProjectId = null) : ProjectLoadState;

        private ProjectLoadState() { }
    }
}
