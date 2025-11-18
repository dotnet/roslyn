// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
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
        public required DiagnosticData? SyntaxError { get; init; }

        [DataMember]
        public required ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart { get; init; }

        [DataMember]
        public required ImmutableArray<ProjectId> ProjectsToRebuild { get; init; }

        [DataMember]
        public required ImmutableArray<ProjectId> ProjectsToRedeploy { get; init; }

        internal ImmutableArray<ManagedHotReloadDiagnostic> GetAllDiagnostics()
        {
            using var _ = ArrayBuilder<ManagedHotReloadDiagnostic>.GetInstance(out var builder);

            foreach (var diagnostic in Diagnostics)
            {
                var severity = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => EditAndContinueDiagnosticDescriptors.IsEncDiagnostic(diagnostic.Id) ? ManagedHotReloadDiagnosticSeverity.RestartRequired : ManagedHotReloadDiagnosticSeverity.Error,
                    DiagnosticSeverity.Warning => ManagedHotReloadDiagnosticSeverity.Warning,
                    _ => default
                };

                if (severity != default)
                {
                    builder.Add(diagnostic.ToHotReloadDiagnostic(severity));
                }
            }

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

            return builder.ToImmutableAndClear();
        }

        public static Data CreateFromInternalError(Solution solution, string errorMessage, ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects)
        {
            ImmutableArray<DiagnosticData> diagnostics = [];
            var firstProject = solution.GetProject(runningProjects.FirstOrDefault().Key) ?? solution.Projects.First();
            var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.CannotApplyChangesUnexpectedError);

            var diagnostic = Diagnostic.Create(
                descriptor,
                Location.None,
                string.Format(descriptor.MessageFormat.ToString(), "", errorMessage));

            return new()
            {
                ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.Ready, []),
                Diagnostics = [DiagnosticData.Create(diagnostic, firstProject)],
                SyntaxError = null,
                ProjectsToRebuild = [.. runningProjects.Keys],
                ProjectsToRedeploy = [],
                ProjectsToRestart = runningProjects.Keys.ToImmutableDictionary(keySelector: static p => p, elementSelector: static p => ImmutableArray.Create(p)),
            };
        }
    }

    public static readonly EmitSolutionUpdateResults Empty = new()
    {
        Solution = null,
        ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.None, []),
        Diagnostics = [],
        SyntaxError = null,
        ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
        ProjectsToRebuild = [],
        ProjectsToRedeploy = [],
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
    /// Reported diagnostics per project.
    /// At most one set of diagnostics per project.
    /// </summary>
    public required ImmutableArray<ProjectDiagnostics> Diagnostics { get; init; }

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

    /// <summary>
    /// Projects whose dependencies need to be deployed to their output directory, if not already present.
    /// Unordered set.
    /// </summary>
    public required ImmutableArray<ProjectId> ProjectsToRedeploy { get; init; }

    public Data Dehydrate()
        => Solution == null
        ? new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = [],
            SyntaxError = null,
            ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
            ProjectsToRebuild = [],
            ProjectsToRedeploy = [],
        }
        : new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = Diagnostics.ToDiagnosticData(Solution),
            SyntaxError = GetSyntaxErrorData(),
            ProjectsToRestart = ProjectsToRestart,
            ProjectsToRebuild = ProjectsToRebuild,
            ProjectsToRedeploy = ProjectsToRedeploy,
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
    /// <param name="addedUnbuiltProjects">Projects that were added to the solution and not built yet.</param>
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
        ImmutableArray<ManagedHotReloadUpdate> moduleUpdates,
        ImmutableArray<ProjectDiagnostics> diagnostics,
        IReadOnlyCollection<ProjectId> addedUnbuiltProjects,
        ImmutableDictionary<ProjectId, RunningProjectOptions> runningProjects,
        out ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> projectsToRestart,
        out ImmutableArray<ProjectId> projectsToRebuild)
    {
        Debug.Assert(!diagnostics.HasDuplicates(d => d.ProjectId));
        Debug.Assert(diagnostics.Select(re => re.ProjectId).IsSorted());

        // Projects with errors (including blocking rude edits) should not have updates:
        Debug.Assert(diagnostics
            .Where(r => r.Diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error))
            .Select(r => r.ProjectId)
            .Intersect(moduleUpdates.Select(u => u.ProjectId))
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
        using var _2 = PooledHashSet<ProjectId>.GetInstance(out var projectsToRestartBuilder);
        var projectsToRebuildBuilder = PooledDictionary<ProjectId, ArrayBuilder<ProjectId>>.GetInstance();

        using var _3 = ArrayBuilder<(ProjectId projectWithRudeEdits, ImmutableArray<ProjectId> impactedRunningProjects)>.GetInstance(out var impactedRunningProjectMap);

        foreach (var (projectId, projectDiagnostics) in diagnostics)
        {
            ClassifyRudeEdits(projectDiagnostics, out var hasBlocking, out var hasNoEffect);
            if (!hasBlocking && !hasNoEffect)
            {
                continue;
            }

            var hasImpactedRunningProjects = false;
            foreach (var ancestor in GetAncestorsAndSelf(projectId))
            {
                if (runningProjects.TryGetValue(ancestor, out var runningProject) &&
                    (hasBlocking || runningProject.RestartWhenChangesHaveNoEffect))
                {
                    projectsToRebuildBuilder.MultiAdd(ancestor, projectId);
                    projectsToRestartBuilder.Add(ancestor);

                    hasImpactedRunningProjects = true;
                }
            }

            if (hasBlocking && !hasImpactedRunningProjects)
            {
                // Projects with rude edits that do not impact running projects has to be rebuilt,
                // so that the change takes effect if it is loaded in future.
                projectsToRebuildBuilder.MultiAdd(projectId, projectId);
            }
        }

        // Rebuild unbuilt projects that have been added and impact a running project
        // (a project reference was added).
        foreach (var projectId in addedUnbuiltProjects)
        {
            if (GetAncestorsAndSelf(projectId).Where(runningProjects.ContainsKey).Any())
            {
                projectsToRebuildBuilder.MultiAdd(projectId, projectId);
            }
        }

        // At this point the restart set contains all running projects transitively affected by rude edits.
        // Next, find projects that were successfully updated and affect running projects.

        if (!moduleUpdates.IsEmpty && projectsToRebuildBuilder.Count > 0)
        {
            // The set of updated projects is usually much smaller than the number of all projects in the solution.
            // We iterate over this set updating the restart set until no new project is added to the restart set.
            // Once a project is determined to affect a running process, all running processes that
            // reference this project are added to the restart set. The project is then removed from updated
            // project set as it can't contribute any more running projects to the restart set.
            // If an updated project does not affect restart set in a given iteration, it stays in the set
            // because it may affect restart set later on, after another running project is added to it.

            using var _6 = PooledHashSet<ProjectId>.GetInstance(out var updatedProjects);
            using var _7 = ArrayBuilder<ProjectId>.GetInstance(out var updatedProjectsToRemove);
            using var _8 = PooledHashSet<ProjectId>.GetInstance(out var projectsThatCausedRebuild);

            updatedProjects.AddRange(moduleUpdates.Select(static u => u.ProjectId));

            while (true)
            {
                updatedProjectsToRemove.Clear();

                foreach (var updatedProjectId in updatedProjects)
                {
                    projectsThatCausedRebuild.Clear();

                    // A project being updated that is a transitive dependency of a running project and
                    // also transitive dependency of a project that needs to be rebuilt
                    // causes the running project to be restarted.

                    foreach (var ancestor in GetAncestorsAndSelf(updatedProjectId))
                    {
                        if (projectsToRebuildBuilder.TryGetValue(ancestor, out var causes))
                        {
                            projectsThatCausedRebuild.AddRange(causes);
                        }
                    }

                    if (!projectsThatCausedRebuild.Any())
                    {
                        continue;
                    }

                    var hasImpactOnRestartSet = false;
                    foreach (var ancestor in GetAncestorsAndSelf(updatedProjectId))
                    {
                        if (!runningProjects.ContainsKey(ancestor))
                        {
                            continue;
                        }

                        if (!projectsToRebuildBuilder.ContainsKey(ancestor))
                        {
                            projectsToRebuildBuilder.MultiAddRange(ancestor, projectsThatCausedRebuild);
                            projectsToRestartBuilder.Add(ancestor);

                            hasImpactOnRestartSet = true;
                        }
                    }

                    if (hasImpactOnRestartSet)
                    {
                        updatedProjectsToRemove.Add(updatedProjectId);
                    }
                }

                if (updatedProjectsToRemove is [])
                {
                    // none of the remaining updated projects affect restart set:
                    break;
                }

                updatedProjects.RemoveAll(updatedProjectsToRemove);
            }
        }

        foreach (var (_, causes) in projectsToRebuildBuilder)
        {
            causes.SortAndRemoveDuplicates();
        }

        projectsToRebuild = [.. projectsToRebuildBuilder.Keys];

        projectsToRestart = projectsToRebuildBuilder.ToImmutableMultiDictionaryAndFree(
            where: static (id, projectsToRestartBuilder) => projectsToRestartBuilder.Contains(id),
            projectsToRestartBuilder);

        return;

        IEnumerable<ProjectId> GetAncestorsAndSelf(ProjectId initialProject)
        {
            traversalStack.Clear();
            traversalStack.Push(initialProject);

            while (traversalStack.Count > 0)
            {
                var projectId = traversalStack.Pop();
                yield return projectId;

                foreach (var referencingProjectId in graph.GetProjectsThatDirectlyDependOnThisProject(projectId))
                {
                    traversalStack.Push(referencingProjectId);
                }
            }
        }
    }

    private static void ClassifyRudeEdits(ImmutableArray<Diagnostic> diagnostics, out bool blocking, out bool noEffect)
    {
        noEffect = false;
        blocking = false;

        foreach (var diagnostic in diagnostics)
        {
            noEffect |= diagnostic.IsNoEffectDiagnostic();
            blocking |= diagnostic.IsEncDiagnostic() && diagnostic.Severity == DiagnosticSeverity.Error;

            if (noEffect && blocking)
            {
                return;
            }
        }
    }

    public ImmutableArray<Diagnostic> GetAllDiagnostics()
    {
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);

        foreach (var (_, projectEmitDiagnostics) in Diagnostics)
        {
            result.AddRange(projectEmitDiagnostics);
        }

        if (SyntaxError != null)
        {
            result.Add(SyntaxError);
        }

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Returns all diagnostics that can be addressed by rebuilding/restarting the project.
    /// </summary>
    public ImmutableArray<(ProjectId projectId, ImmutableArray<Diagnostic> diagnostics)> GetTransientDiagnostics()
    {
        using var _ = ArrayBuilder<(ProjectId projectId, ImmutableArray<Diagnostic> diagnostics)>.GetInstance(out var result);

        foreach (var (projectId, diagnostics) in Diagnostics)
        {
            var transientDiagnostics = diagnostics.WhereAsArray(static d => d.IsEncDiagnostic());
            if (transientDiagnostics.Length > 0)
            {
                result.Add((projectId, transientDiagnostics));
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Returns all diagnostics that can't be addressed by rebuilding/restarting the project.
    /// </summary>
    public ImmutableArray<Diagnostic> GetPersistentDiagnostics()
    {
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);

        foreach (var (_, diagnostics) in Diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.IsEncDiagnostic())
                {
                    result.Add(diagnostic);
                }
            }
        }

        if (SyntaxError != null)
        {
            result.Add(SyntaxError);
        }

        return result.ToImmutableAndClear();
    }
}
