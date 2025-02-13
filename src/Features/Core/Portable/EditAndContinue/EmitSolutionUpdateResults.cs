// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct EmitSolutionUpdateResults
{
    [DataContract]
    internal readonly struct Data
    {
        [DataMember]
        public required ModuleUpdates ModuleUpdates { get; init; }

        [DataMember]
        public required ImmutableArray<DiagnosticData> Diagnostics { get; init; }

        [DataMember]
        public required ImmutableArray<DiagnosticData> RudeEdits { get; init; }

        [DataMember]
        public required DiagnosticData? SyntaxError { get; init; }

        [DataMember]
        public required ImmutableArray<ProjectId> ProjectsToRestart { get; init; }

        [DataMember]
        public required ImmutableArray<ProjectId> ProjectsToRebuild { get; init; }

        internal ImmutableArray<ManagedHotReloadDiagnostic> GetAllDiagnostics()
        {
            using var _ = ArrayBuilder<ManagedHotReloadDiagnostic>.GetInstance(out var builder);

            // Add semantic and lowering diagnostics reported during delta emit:

            foreach (var diagnostic in Diagnostics)
            {
                builder.Add(diagnostic.ToHotReloadDiagnostic(ModuleUpdates.Status, isRudeEdit: false));
            }

            // Add syntax error:

            if (SyntaxError != null)
            {
                Debug.Assert(SyntaxError.DataLocation != null);
                Debug.Assert(SyntaxError.Message != null);

                var fileSpan = SyntaxError.DataLocation.MappedFileSpan;

                builder.Add(new ManagedHotReloadDiagnostic(
                    SyntaxError.Id,
                    SyntaxError.Message,
                    ManagedHotReloadDiagnosticSeverity.Error,
                    fileSpan.Path,
                    fileSpan.Span.ToSourceSpan()));
            }

            // Report all rude edits.

            foreach (var data in RudeEdits)
            {
                builder.Add(data.ToHotReloadDiagnostic(ModuleUpdates.Status, isRudeEdit: true));
            }

            return builder.ToImmutableAndClear();
        }
    }

    public static readonly EmitSolutionUpdateResults Empty = new()
    {
        Solution = null,
        ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.None, []),
        Diagnostics = [],
        RudeEdits = [],
        SyntaxError = null,
        ProjectsToRestart = [],
        ProjectsToRebuild = [],
    };

    /// <summary>
    /// Solution snapshot to resolve diagnostics in.
    /// Note that this might be a different snapshot from the one passed to EmitSolutionUpdateAsync,
    /// with source generator files refreshed.
    ///
    /// Null only for empty results.
    /// </summary>
    public required Solution? Solution { get; init; }

    public required ModuleUpdates ModuleUpdates { get; init; }
    public required ImmutableArray<ProjectDiagnostics> Diagnostics { get; init; }
    public required ImmutableArray<ProjectDiagnostics> RudeEdits { get; init; }
    public required Diagnostic? SyntaxError { get; init; }

    public required ImmutableArray<ProjectId> ProjectsToRestart { get; init; }
    public required ImmutableArray<ProjectId> ProjectsToRebuild { get; init; }

    public Data Dehydrate()
        => Solution == null
        ? new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = [],
            RudeEdits = [],
            SyntaxError = null,
            ProjectsToRestart = [],
            ProjectsToRebuild = [],
        }
        : new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = Diagnostics.ToDiagnosticData(Solution),
            RudeEdits = RudeEdits.ToDiagnosticData(Solution),
            SyntaxError = GetSyntaxErrorData(),
            ProjectsToRestart = ProjectsToRestart,
            ProjectsToRebuild = ProjectsToRebuild,
        };

    private DiagnosticData? GetSyntaxErrorData()
    {
        if (SyntaxError == null)
        {
            return null;
        }

        Debug.Assert(Solution != null);
        Debug.Assert(SyntaxError.Location.SourceTree != null);
        return DiagnosticData.Create(SyntaxError, Solution.GetRequiredDocument(SyntaxError.Location.SourceTree));
    }

    /// <summary>
    /// Returns projects that need to be rebuilt and/or restarted due to blocking rude edits in order to apply changes.
    /// </summary>
    /// <param name="runningProjects">Identifies projects that have been launched.</param>
    /// <param name="projectsToRestart">Running projects that have to be restarted.</param>
    /// <param name="projectsToRebuild">Projects whose source have been updated and need to be rebuilt.</param>
    internal static void GetProjectsToRebuildAndRestart(
        Solution solution,
        ModuleUpdates moduleUpdates,
        IEnumerable<ProjectDiagnostics> rudeEdits,
        IImmutableSet<ProjectId> runningProjects,
        out ImmutableArray<ProjectId> projectsToRestart,
        out ImmutableArray<ProjectId> projectsToRebuild)
    {
        var graph = solution.GetProjectDependencyGraph();

        // First, find all running projects that transitively depend on projects with rude edits.
        // These will need to be rebuilt and restarted. In order to rebuilt these projects
        // all their transitive references must either be free of source changes or be rebuilt as well.
        // This may add more running projects to the set of projects we need to restart.
        // We need to repeat this process until we find a fixed point.

        using var _1 = ArrayBuilder<Project>.GetInstance(out var traversalStack);
        using var _2 = PooledHashSet<ProjectId>.GetInstance(out var projectsToRestartBuilder);
        using var _3 = ArrayBuilder<ProjectId>.GetInstance(out var projectsToRebuildBuilder);

        foreach (var projectWithRudeEdit in GetProjectsContainingBlockingRudeEdits(solution))
        {
            if (AddImpactedRunningProjects(projectsToRestartBuilder, projectWithRudeEdit))
            {
                projectsToRebuildBuilder.Add(projectWithRudeEdit.Id);
            }
        }

        // At this point the restart set contains all running projects directly affected by rude edits.
        // Next, find projects that were successfully updated and affect running projects.

        if (moduleUpdates.Updates.IsEmpty || projectsToRestartBuilder.Count == 0)
        {
            projectsToRestart = [.. projectsToRestartBuilder];
            projectsToRebuild = [.. projectsToRebuildBuilder];
            return;
        }

        // The set of updated projects is usually much smaller then the number of all projects in the solution.
        // We iterate over this set updating the reset set until no new project is added to the reset set.
        // Once a project is determined to affect a running process, all running processes that
        // reference this project are added to the reset set. The project is then removed from updated
        // project set as it can't contribute any more running projects to the reset set.
        // If an updated project does not affect reset set in a given iteration, it stays in the set
        // because it may affect reset set later on, after another running project is added to it.

        using var _4 = PooledHashSet<Project>.GetInstance(out var updatedProjects);
        using var _5 = ArrayBuilder<Project>.GetInstance(out var updatedProjectsToRemove);

        foreach (var update in moduleUpdates.Updates)
        {
            updatedProjects.Add(solution.GetRequiredProject(update.ProjectId));
        }

        using var _6 = ArrayBuilder<ProjectId>.GetInstance(out var impactedProjects);

        while (true)
        {
            Debug.Assert(updatedProjectsToRemove.Count == 0);

            foreach (var updatedProject in updatedProjects)
            {
                if (AddImpactedRunningProjects(impactedProjects, updatedProject) &&
                    impactedProjects.Any(projectsToRestartBuilder.Contains))
                {
                    projectsToRestartBuilder.AddRange(impactedProjects);
                    updatedProjectsToRemove.Add(updatedProject);
                    projectsToRebuildBuilder.Add(updatedProject.Id);
                }

                impactedProjects.Clear();
            }

            if (updatedProjectsToRemove is [])
            {
                // none of the remaining updated projects affect restart set:
                break;
            }

            updatedProjects.RemoveAll(updatedProjectsToRemove);
            updatedProjectsToRemove.Clear();
        }

        projectsToRestart = [.. projectsToRestartBuilder];
        projectsToRebuild = [.. projectsToRebuildBuilder];
        return;

        bool AddImpactedRunningProjects(ICollection<ProjectId> impactedProjects, Project initialProject)
        {
            Debug.Assert(traversalStack.Count == 0);
            traversalStack.Push(initialProject);

            var added = false;

            while (traversalStack.Count > 0)
            {
                var project = traversalStack.Pop();
                if (runningProjects.Contains(project.Id))
                {
                    impactedProjects.Add(project.Id);
                    added = true;
                }

                foreach (var referencingProjectId in graph.GetProjectsThatDirectlyDependOnThisProject(project.Id))
                {
                    traversalStack.Push(solution.GetRequiredProject(referencingProjectId));
                }
            }

            return added;
        }

        IEnumerable<Project> GetProjectsContainingBlockingRudeEdits(Solution solution)
            => rudeEdits
                .Where(static e => e.Diagnostics.HasBlockingRudeEdits())
                .Select(static e => e.ProjectId)
                .Distinct()
                .OrderBy(static id => id)
                .Select(solution.GetRequiredProject);
    }

    public ImmutableArray<Diagnostic> GetAllDiagnostics()
    {
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);

        // add semantic and lowering diagnostics reported during delta emit:
        foreach (var (_, projectEmitDiagnostics) in Diagnostics)
        {
            diagnostics.AddRange(projectEmitDiagnostics);
        }

        // add syntax error:
        if (SyntaxError != null)
        {
            diagnostics.Add(SyntaxError);
        }

        // add rude edits:
        foreach (var (_, projectEmitDiagnostics) in RudeEdits)
        {
            diagnostics.AddRange(projectEmitDiagnostics);
        }

        return diagnostics.ToImmutableAndClear();
    }
}
