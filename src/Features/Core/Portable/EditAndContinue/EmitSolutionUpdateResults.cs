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
        public required ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart { get; init; }

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
        ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
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

    /// <summary>
    /// Reported diagnostics, other than rude edits, per project.
    /// May contain multiple entries for the same project.
    /// </summary>
    public required ImmutableArray<ProjectDiagnostics> Diagnostics { get; init; }

    public required ImmutableArray<ProjectDiagnostics> RudeEdits { get; init; }
    public required Diagnostic? SyntaxError { get; init; }

    /// <summary>
    /// Running projects that have to be restarted and a list of projects with rude edits that caused the restart.
    /// </summary>
    public required ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart { get; init; }

    /// <summary>
    /// Projects whose source have been updated and need to be rebuilt. Does not include projects without change that depend on such projects.
    /// It is assumed that the host automatically rebuilds all such projects that need rebuilding because it detects the dependent project outputs have been updated.
    /// Unordered set.
    /// </summary>
    public required ImmutableArray<ProjectId> ProjectsToRebuild { get; init; }

    public Data Dehydrate()
        => Solution == null
        ? new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = [],
            RudeEdits = [],
            SyntaxError = null,
            ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
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
    /// <param name="projectsToRestart">
    /// Running projects that have to be restarted and a list of projects with rude edits that caused the restart.
    /// </param>
    /// <param name="projectsToRebuild">
    /// Projects whose source have been updated and need to be rebuilt. Does not include projects without change that depend on such projects.
    /// It is assumed that the host automatically rebuilds all such projects that need rebuilding because it detects the dependent project outputs have been updated.
    /// Unordered set.
    /// </param>
    internal static void GetProjectsToRebuildAndRestart(
        Solution solution,
        ModuleUpdates moduleUpdates,
        ArrayBuilder<ProjectDiagnostics> rudeEdits,
        ImmutableDictionary<ProjectId, RunningProjectInfo> runningProjects,
        out ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> projectsToRestart,
        out ImmutableArray<ProjectId> projectsToRebuild)
    {
        Debug.Assert(!rudeEdits.HasDuplicates(d => d.ProjectId));
        Debug.Assert(rudeEdits.Select(re => re.ProjectId).IsSorted());

        // Projects with blocking rude edits should not have updates:
        Debug.Assert(rudeEdits
            .Where(r => r.Diagnostics.HasBlockingRudeEdits())
            .Select(r => r.ProjectId)
            .Intersect(moduleUpdates.Updates.Select(u => u.ProjectId))
            .IsEmpty());

        var graph = solution.GetProjectDependencyGraph();

        // First, find all running projects that transitively depend on projects with blocking rude edits
        // or edits that have no effect until restart. Note that the latter only trigger restart 
        // of projects that are configured to restart on no-effect change.
        //
        // These will need to be rebuilt and restarted. In order to rebuilt these projects
        // all their transitive references must either be free of source changes [*] or be rebuilt as well.
        // This may add more running projects to the set of projects we need to restart.
        // We need to repeat this process until we find a fixed point.
        //
        // [*] If a running project depended on a project with changes and was not restarted,
        // the debugger might stop at the changed method body and its current source code
        // wouldn't match the IL being executed.

        using var _1 = ArrayBuilder<ProjectId>.GetInstance(out var traversalStack);

        // Maps project to restart to all projects with rude edits that caused the restart:
        var projectsToRestartBuilder = PooledDictionary<ProjectId, ArrayBuilder<ProjectId>>.GetInstance();

        using var _3 = PooledHashSet<ProjectId>.GetInstance(out var projectsToRebuildBuilder);
        using var _4 = ArrayBuilder<(ProjectId projectWithRudeEdits, ImmutableArray<ProjectId> impactedRunningProjects)>.GetInstance(out var impactedRunningProjectMap);
        using var _5 = ArrayBuilder<ProjectId>.GetInstance(out var impactedRunningProjects);

        for (var i = 0; i < rudeEdits.Count; i++)
        {
            var (projectId, projectDiagnostics) = rudeEdits[i];

            var hasBlocking = projectDiagnostics.HasBlockingRudeEdits();
            var hasNoEffect = projectDiagnostics.HasNoEffectRudeEdits();
            if (!hasBlocking && !hasNoEffect)
            {
                continue;
            }

            AddImpactedRunningProjects(impactedRunningProjects, projectId, hasBlocking);

            foreach (var impactedRunningProject in impactedRunningProjects)
            {
                projectsToRestartBuilder.MultiAdd(impactedRunningProject, projectId);
            }

            if (hasBlocking && impactedRunningProjects is [])
            {
                // Projects with rude edits that do not impact running projects has to be rebuilt,
                // so that the change takes effect if it is loaded in future.
                projectsToRebuildBuilder.Add(projectId);
            }

            impactedRunningProjects.Clear();
        }

        // At this point the restart set contains all running projects transitively affected by rude edits.
        // Next, find projects that were successfully updated and affect running projects.

        // Remove once https://github.com/dotnet/roslyn/issues/78244 is implemented.
        if (!runningProjects.Any(static p => p.Value.AllowPartialUpdate))
        {
            // Partial solution update not supported.
            if (projectsToRestartBuilder.Any())
            {
                foreach (var update in moduleUpdates.Updates)
                {
                    AddImpactedRunningProjects(impactedRunningProjects, update.ProjectId, isBlocking: true);

                    foreach (var impactedRunningProject in impactedRunningProjects)
                    {
                        projectsToRestartBuilder.TryAdd(impactedRunningProject, []);
                    }

                    impactedRunningProjects.Clear();
                }
            }
        }
        else if (!moduleUpdates.Updates.IsEmpty && projectsToRestartBuilder.Count > 0)
        {
            // The set of updated projects is usually much smaller than the number of all projects in the solution.
            // We iterate over this set updating the reset set until no new project is added to the reset set.
            // Once a project is determined to affect a running process, all running processes that
            // reference this project are added to the reset set. The project is then removed from updated
            // project set as it can't contribute any more running projects to the reset set.
            // If an updated project does not affect reset set in a given iteration, it stays in the set
            // because it may affect reset set later on, after another running project is added to it.

            using var _6 = PooledHashSet<ProjectId>.GetInstance(out var updatedProjects);
            using var _7 = ArrayBuilder<ProjectId>.GetInstance(out var updatedProjectsToRemove);
            using var _8 = PooledHashSet<ProjectId>.GetInstance(out var projectsThatCausedRestart);

            updatedProjects.AddRange(moduleUpdates.Updates.Select(static u => u.ProjectId));

            while (true)
            {
                Debug.Assert(updatedProjectsToRemove.IsEmpty);

                foreach (var updatedProjectId in updatedProjects)
                {
                    AddImpactedRunningProjects(impactedRunningProjects, updatedProjectId, isBlocking: true);

                    Debug.Assert(projectsThatCausedRestart.Count == 0);

                    // collect all projects that caused restart of any of the impacted running projects:
                    foreach (var impactedRunningProject in impactedRunningProjects)
                    {
                        if (projectsToRestartBuilder.TryGetValue(impactedRunningProject, out var causes))
                        {
                            projectsThatCausedRestart.AddRange(causes);
                        }
                    }

                    if (projectsThatCausedRestart.Any())
                    {
                        // The projects that caused the impacted running project to be restarted
                        // indirectly cause the running project that depends on the updated project to be restarted.
                        foreach (var impactedRunningProject in impactedRunningProjects)
                        {
                            if (!projectsToRestartBuilder.ContainsKey(impactedRunningProject))
                            {
                                projectsToRestartBuilder.MultiAddRange(impactedRunningProject, projectsThatCausedRestart);
                            }
                        }

                        updatedProjectsToRemove.Add(updatedProjectId);
                    }

                    impactedRunningProjects.Clear();
                    projectsThatCausedRestart.Clear();
                }

                if (updatedProjectsToRemove is [])
                {
                    // none of the remaining updated projects affect restart set:
                    break;
                }

                updatedProjects.RemoveAll(updatedProjectsToRemove);
                updatedProjectsToRemove.Clear();
            }
        }

        foreach (var (_, causes) in projectsToRestartBuilder)
        {
            causes.SortAndRemoveDuplicates();
        }

        projectsToRebuildBuilder.AddRange(projectsToRestartBuilder.Keys);
        projectsToRestart = projectsToRestartBuilder.ToImmutableMultiDictionaryAndFree();
        projectsToRebuild = [.. projectsToRebuildBuilder];
        return;

        void AddImpactedRunningProjects(ArrayBuilder<ProjectId> impactedProjects, ProjectId initialProject, bool isBlocking)
        {
            Debug.Assert(impactedProjects.IsEmpty);

            Debug.Assert(traversalStack.Count == 0);
            traversalStack.Push(initialProject);

            while (traversalStack.Count > 0)
            {
                var projectId = traversalStack.Pop();
                if (runningProjects.TryGetValue(projectId, out var runningProject) &&
                    (isBlocking || runningProject.RestartWhenChangesHaveNoEffect))
                {
                    impactedProjects.Add(projectId);
                }

                foreach (var referencingProjectId in graph.GetProjectsThatDirectlyDependOnThisProject(projectId))
                {
                    traversalStack.Push(referencingProjectId);
                }
            }
        }
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

    public ImmutableArray<Diagnostic> GetAllCompilationDiagnostics()
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

        return diagnostics.ToImmutableAndClear();
    }
}
