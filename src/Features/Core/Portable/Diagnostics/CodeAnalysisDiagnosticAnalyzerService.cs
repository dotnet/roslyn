// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

[ExportWorkspaceServiceFactory(typeof(ICodeAnalysisDiagnosticAnalyzerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CodeAnalysisDiagnosticAnalyzerServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        var diagnosticAnalyzerService = workspaceServices.SolutionServices.ExportProvider.GetExports<IDiagnosticAnalyzerService>().Single().Value;
        var diagnosticsRefresher = workspaceServices.SolutionServices.ExportProvider.GetExports<IDiagnosticsRefresher>().Single().Value;
        return new CodeAnalysisDiagnosticAnalyzerService(diagnosticAnalyzerService, diagnosticsRefresher, workspaceServices.Workspace);
    }

    private sealed class CodeAnalysisDiagnosticAnalyzerService : ICodeAnalysisDiagnosticAnalyzerService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly IDiagnosticsRefresher _diagnosticsRefresher;
        private readonly Workspace _workspace;

        /// <summary>
        /// List of projects that we've finished running "run code analysis" on.  Cached results can now be returned for
        /// these through <see cref="GetLastComputedDocumentDiagnosticsAsync"/> and <see
        /// cref="GetLastComputedProjectDiagnosticsAsync"/>.
        /// </summary>
        private readonly ConcurrentSet<ProjectId> _analyzedProjectIds = [];

        /// <summary>
        /// Previously analyzed projects that we no longer want to report results for.  This happens when an explicit
        /// build is kicked off.  At that point, we want the build results to win out for a particular project.  We mark
        /// this project (as opposed to removing from <see cref="_analyzedProjectIds"/>) as we want our LSP handler to
        /// still think it should process it, as that will the cause the diagnostics to be removed when they now
        /// transition to an empty list returned from this type.
        /// </summary>
        private readonly ConcurrentSet<ProjectId> _clearedProjectIds = [];

        public CodeAnalysisDiagnosticAnalyzerService(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            IDiagnosticsRefresher diagnosticsRefresher,
            Workspace workspace)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
            _diagnosticsRefresher = diagnosticsRefresher;
            _workspace = workspace;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:

                    _analyzedProjectIds.Clear();
                    _clearedProjectIds.Clear();

                    // Let LSP know so that it requests up to date info, and will see our cached info disappear.
                    _diagnosticsRefresher.RequestWorkspaceRefresh();
                    break;
            }
        }

        public void Clear()
        {
            // Clear the list of analyzed projects.
            _clearedProjectIds.AddRange(_analyzedProjectIds);

            // Let LSP know so that it requests up to date info, and will see our cached info disappear.
            _diagnosticsRefresher.RequestWorkspaceRefresh();
        }

        public bool HasProjectBeenAnalyzed(ProjectId projectId) => _analyzedProjectIds.Contains(projectId);

        public async Task RunAnalysisAsync(Solution solution, ProjectId? projectId, Action<Project> onAfterProjectAnalyzed, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(solution.Workspace == _workspace);

            if (projectId != null)
            {
                var project = solution.GetProject(projectId);
                if (project != null)
                {
                    await AnalyzeProjectCoreAsync(project, onAfterProjectAnalyzed, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                // We run analysis for all the projects concurrently as this is a user invoked operation.
                using var _ = ArrayBuilder<Task>.GetInstance(solution.ProjectIds.Count, out var tasks);
                foreach (var project in solution.Projects)
                    tasks.Add(Task.Run(() => AnalyzeProjectCoreAsync(project, onAfterProjectAnalyzed, cancellationToken), cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        private async Task AnalyzeProjectCoreAsync(Project project, Action<Project> onAfterProjectAnalyzed, CancellationToken cancellationToken)
        {
            // Execute force analysis for the project.
            await _diagnosticAnalyzerService.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);

            // Add the given project to the analyzed projects list **after** analysis has completed.
            // We need this ordering to ensure that 'HasProjectBeenAnalyzed' call above functions correctly.
            _analyzedProjectIds.Add(project.Id);

            // Remove from the cleared list now that we've run a more recent "run code analysis" on this project.
            _clearedProjectIds.Remove(project.Id);

            // Now raise the callback into our caller to indicate this project has been analyzed.
            onAfterProjectAnalyzed(project);

            // Finally, invoke a workspace refresh request for LSP client to pull onto these diagnostics.
            // TODO: Below call will eventually be replaced with a special workspace refresh request that skips
            //       pulling document diagnostics and also does not add any delay for pulling workspace diagnostics.
            _diagnosticsRefresher.RequestWorkspaceRefresh();
        }

        /// <summary>
        /// Running code analysis on the project force computes and caches the diagnostics on the DiagnosticAnalyzerService.
        /// We return these cached document diagnostics here, including both local and non-local document diagnostics.
        /// </summary>
        public Task<ImmutableArray<DiagnosticData>> GetLastComputedDocumentDiagnosticsAsync(DocumentId documentId, CancellationToken cancellationToken)
            => _clearedProjectIds.Contains(documentId.ProjectId)
                ? SpecializedTasks.EmptyImmutableArray<DiagnosticData>()
                : _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(_workspace, documentId.ProjectId,
                    documentId, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true,
                    includeNonLocalDocumentDiagnostics: true, cancellationToken);

        /// <summary>
        /// Running code analysis on the project force computes and caches the diagnostics on the DiagnosticAnalyzerService.
        /// We return these cached project diagnostics here, i.e. diagnostics with no location, by excluding all local and non-local document diagnostics.
        /// </summary>
        public Task<ImmutableArray<DiagnosticData>> GetLastComputedProjectDiagnosticsAsync(ProjectId projectId, CancellationToken cancellationToken)
            => _clearedProjectIds.Contains(projectId)
                ? SpecializedTasks.EmptyImmutableArray<DiagnosticData>()
                : _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(_workspace, projectId, documentId: null,
                    includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: false,
                    includeNonLocalDocumentDiagnostics: false, cancellationToken);
    }
}
