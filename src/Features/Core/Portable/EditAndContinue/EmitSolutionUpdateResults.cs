// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
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
        public required ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits { get; init; }

        [DataMember]
        public required DiagnosticData? SyntaxError { get; init; }
    }

    public static readonly EmitSolutionUpdateResults Empty = new()
    {
        ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.None, []),
        Diagnostics = [],
        RudeEdits = [],
        SyntaxError = null
    };

    public required ModuleUpdates ModuleUpdates { get; init; }
    public required ImmutableArray<ProjectDiagnostics> Diagnostics { get; init; }
    public required ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits { get; init; }
    public required Diagnostic? SyntaxError { get; init; }

    public Data Dehydrate(Solution solution)
        => new()
        {
            ModuleUpdates = ModuleUpdates,
            Diagnostics = Diagnostics.ToDiagnosticData(solution),
            RudeEdits = RudeEdits,
            SyntaxError = GetSyntaxErrorData(solution)
        };

    public DiagnosticData? GetSyntaxErrorData(Solution solution)
    {
        if (SyntaxError == null)
        {
            return null;
        }

        Debug.Assert(SyntaxError.Location.SourceTree != null);
        return DiagnosticData.Create(SyntaxError, solution.GetRequiredDocument(SyntaxError.Location.SourceTree));
    }

    private IEnumerable<Project> GetProjectsContainingBlockingRudeEdits(Solution solution)
        => RudeEdits
            .Where(static e => e.Diagnostics.Any(static d => d.Kind.IsBlocking()))
            .Select(static e => e.DocumentId.ProjectId)
            .Distinct()
            .OrderBy(static id => id)
            .Select(solution.GetRequiredProject);

    /// <summary>
    /// Returns projects that need to be rebuilt and/or restarted due to blocking rude edits in order to apply changes.
    /// </summary>
    /// <param name="isRunningProject">Identifies projects that have been launched.</param>
    /// <param name="projectsToRestart">Running projects that have to be restarted.</param>
    /// <param name="projectsToRebuild">Projects whose source have been updated and need to be rebuilt.</param>
    public void GetProjectsToRebuildAndRestart(
        Solution solution,
        Func<Project, bool> isRunningProject,
        ISet<Project> projectsToRestart,
        ISet<Project> projectsToRebuild)
    {
        var graph = solution.GetProjectDependencyGraph();

        // First, find all running projects that transitively depend on projects with rude edits.
        // These will need to be rebuilt and restarted. In order to rebuilt these projects
        // all their transitive references must either be free of source changes or be rebuilt as well.
        // This may add more running projects to the set of projects we need to restart.
        // We need to repeat this process until we find a fixed point.

        using var _1 = ArrayBuilder<Project>.GetInstance(out var traversalStack);

        projectsToRestart.Clear();
        projectsToRebuild.Clear();

        foreach (var projectWithRudeEdit in GetProjectsContainingBlockingRudeEdits(solution))
        {
            if (AddImpactedRunningProjects(projectsToRestart, projectWithRudeEdit))
            {
                projectsToRebuild.Add(projectWithRudeEdit);
            }
        }

        // At this point the restart set contains all running projects directly affected by rude edits.
        // Next, find projects that were successfully updated and affect running projects.

        if (ModuleUpdates.Updates.IsEmpty || projectsToRestart.IsEmpty())
        {
            return;
        }

        // The set of updated projects is usually much smaller then the number of all projects in the solution.
        // We iterate over this set updating the reset set until no new project is added to the reset set.
        // Once a project is determined to affect a running process, all running processes that
        // reference this project are added to the reset set. The project is then removed from updated
        // project set as it can't contribute any more running projects to the reset set. 
        // If an updated project does not affect reset set in a given iteration, it stays in the set
        // because it may affect reset set later on, after another running project is added to it.

        using var _2 = PooledHashSet<Project>.GetInstance(out var updatedProjects);
        using var _3 = ArrayBuilder<Project>.GetInstance(out var updatedProjectsToRemove);
        foreach (var update in ModuleUpdates.Updates)
        {
            updatedProjects.Add(solution.GetRequiredProject(update.ProjectId));
        }

        using var _4 = ArrayBuilder<Project>.GetInstance(out var impactedProjects);

        while (true)
        {
            Debug.Assert(updatedProjectsToRemove.Count == 0);

            foreach (var updatedProject in updatedProjects)
            {
                if (AddImpactedRunningProjects(impactedProjects, updatedProject) &&
                    impactedProjects.Any(projectsToRestart.Contains))
                {
                    projectsToRestart.AddRange(impactedProjects);
                    updatedProjectsToRemove.Add(updatedProject);
                    projectsToRebuild.Add(updatedProject);
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

        return;

        bool AddImpactedRunningProjects(ICollection<Project> impactedProjects, Project initialProject)
        {
            Debug.Assert(traversalStack.Count == 0);
            traversalStack.Push(initialProject);

            var added = false;

            while (traversalStack.Count > 0)
            {
                var project = traversalStack.Pop();
                if (isRunningProject(project))
                {
                    impactedProjects.Add(project);
                    added = true;
                }

                foreach (var referencingProjectId in graph.GetProjectsThatDirectlyDependOnThisProject(project.Id))
                {
                    traversalStack.Push(solution.GetRequiredProject(referencingProjectId));
                }
            }

            return added;
        }
    }

    public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
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
        foreach (var (documentId, documentRudeEdits) in RudeEdits)
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            foreach (var documentRudeEdit in documentRudeEdits)
            {
                diagnostics.Add(documentRudeEdit.ToDiagnostic(tree));
            }
        }

        return diagnostics.ToImmutableAndClear();
    }

    internal static async ValueTask<ImmutableArray<ManagedHotReloadDiagnostic>> GetAllDiagnosticsAsync(
        Solution solution,
        ImmutableArray<DiagnosticData> diagnosticData,
        ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits,
        DiagnosticData? syntaxError,
        ModuleUpdateStatus updateStatus,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ManagedHotReloadDiagnostic>.GetInstance(out var builder);

        // Add semantic and lowering diagnostics reported during delta emit:

        foreach (var data in diagnosticData)
        {
            builder.Add(data.ToHotReloadDiagnostic(updateStatus));
        }

        // Add syntax error:

        if (syntaxError != null)
        {
            Debug.Assert(syntaxError.DataLocation != null);
            Debug.Assert(syntaxError.Message != null);

            var fileSpan = syntaxError.DataLocation.MappedFileSpan;

            builder.Add(new ManagedHotReloadDiagnostic(
                syntaxError.Id,
                syntaxError.Message,
                ManagedHotReloadDiagnosticSeverity.Error,
                fileSpan.Path,
                fileSpan.Span.ToSourceSpan()));
        }

        // Report all rude edits.

        foreach (var (documentId, diagnostics) in rudeEdits)
        {
            var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(diagnostic.Kind);

                var severity = descriptor.DefaultSeverity switch
                {
                    DiagnosticSeverity.Error => ManagedHotReloadDiagnosticSeverity.RestartRequired,
                    DiagnosticSeverity.Warning => ManagedHotReloadDiagnosticSeverity.Warning,
                    _ => throw ExceptionUtilities.UnexpectedValue(descriptor.DefaultSeverity)
                };

                var fileSpan = tree.GetMappedLineSpan(diagnostic.Span, cancellationToken);

                builder.Add(new ManagedHotReloadDiagnostic(
                    descriptor.Id,
                    string.Format(descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture), diagnostic.Arguments),
                    severity,
                    fileSpan.Path ?? "",
                    fileSpan.Span.ToSourceSpan()));
            }
        }

        return builder.ToImmutableAndClear();
    }
}
