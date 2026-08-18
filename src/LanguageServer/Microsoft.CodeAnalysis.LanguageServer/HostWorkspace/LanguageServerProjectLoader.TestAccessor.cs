// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    internal static class TestAccessor
    {
        public static RemoteProjectLoadResult CreateRemoteProjectLoadResult(ProjectSystemProjectFactory projectFactory, string projectPath)
            => new()
            {
                ProjectFileInfos = [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { CommandLineArgs = ["/target:library"] }],
                DiagnosticLogItems = [],
                ProjectRestorePath = null,
                ProjectFactory = projectFactory,
                IsFileBasedProgram = false,
                IsMiscellaneousFile = false,
                HasFileBasedAppDirectives = false,
                HasAllInformation = true,
                PreferredBuildHostKind = BuildHostProcessKind.NetCore,
                ActualBuildHostKind = BuildHostProcessKind.NetCore,
            };

        public static ImmutableArray<LoadedProject> GetLoadedProjectTargets(LanguageServerProjectLoader loader, string projectPath)
            => loader._loadedProjects.TryGetValue(NormalizeProjectPath(projectPath), out var loadState) && loadState is ProjectLoadState.LoadedTargets(var targets)
                ? targets
                : [];
    }
}
