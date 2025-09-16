// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct SolutionUpdate(
    ModuleUpdates moduleUpdates,
    ImmutableDictionary<ProjectId, Guid> staleProjects,
    ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
    ImmutableArray<ProjectBaseline> projectBaselines,
    ImmutableArray<ProjectDiagnostics> diagnostics,
    Diagnostic? syntaxError,
    ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> projectsToRestart,
    ImmutableArray<ProjectId> projectsToRebuild,
    ImmutableArray<ProjectId> projectsToRedeploy)
{
    public readonly ModuleUpdates ModuleUpdates = moduleUpdates;
    public readonly ImmutableDictionary<ProjectId, Guid> StaleProjects = staleProjects;
    public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions = nonRemappableRegions;
    public readonly ImmutableArray<ProjectBaseline> ProjectBaselines = projectBaselines;

    // Diagnostics for projects, unique entries per project.
    public readonly ImmutableArray<ProjectDiagnostics> Diagnostics = diagnostics;
    public readonly Diagnostic? SyntaxError = syntaxError;
    public readonly ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart = projectsToRestart;
    public readonly ImmutableArray<ProjectId> ProjectsToRebuild = projectsToRebuild;
    public readonly ImmutableArray<ProjectId> ProjectsToRedeploy = projectsToRedeploy;

    public static SolutionUpdate Empty(
        ImmutableArray<ProjectDiagnostics> diagnostics,
        Diagnostic? syntaxError,
        ImmutableDictionary<ProjectId, Guid> staleProjects,
        ModuleUpdateStatus status)
        => new(
            new(status, Updates: []),
            staleProjects: staleProjects,
            nonRemappableRegions: [],
            projectBaselines: [],
            diagnostics,
            syntaxError,
            projectsToRestart: ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
            projectsToRebuild: [],
            projectsToRedeploy: []);

    internal void Log(TraceLog log, UpdateId updateId)
    {
        log.Write($"Solution update {updateId} status: {ModuleUpdates.Status}");

        foreach (var moduleUpdate in ModuleUpdates.Updates)
        {
            log.Write("Module update: " +
                $"capabilities=[{string.Join(",", moduleUpdate.RequiredCapabilities)}], " +
                $"types=[{string.Join(",", moduleUpdate.UpdatedTypes.Select(token => token.ToString("X8")))}], " +
                $"methods=[{string.Join(",", moduleUpdate.UpdatedMethods.Select(token => token.ToString("X8")))}]");
        }

        foreach (var projectDiagnostics in Diagnostics)
        {
            foreach (var diagnostic in projectDiagnostics.Diagnostics)
            {
                log.Write($"[{projectDiagnostics.ProjectId.DebugName}]: {diagnostic}", diagnostic.Severity switch
                {
                    DiagnosticSeverity.Warning => LogMessageSeverity.Warning,
                    DiagnosticSeverity.Error => LogMessageSeverity.Error,
                    _ => LogMessageSeverity.Info
                });
            }
        }
    }
}
