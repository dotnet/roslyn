﻿// Licensed to the .NET Foundation under one or more agreements.
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

[ExportWorkspaceServiceFactory(typeof(ICodeAnalysisDiagnosticAnalyzerService), ServiceLayer.Host), Shared]
internal sealed class CodeAnalysisDiagnosticAnalyzerServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeAnalysisDiagnosticAnalyzerServiceFactory()
    {
    }

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
        private readonly ConcurrentSet<ProjectId> _analyzedProjectIds = new();

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
                    break;
            }
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
            => _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(_workspace, documentId.ProjectId,
                documentId, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true,
                includeNonLocalDocumentDiagnostics: true, cancellationToken);

        /// <summary>
        /// Running code analysis on the project force computes and caches the diagnostics on the DiagnosticAnalyzerService.
        /// We return these cached project diagnostics here, i.e. diagnostics with no location, by excluding all local and non-local document diagnostics.
        /// </summary>
        public Task<ImmutableArray<DiagnosticData>> GetLastComputedProjectDiagnosticsAsync(ProjectId projectId, CancellationToken cancellationToken)
            => _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(_workspace, projectId, documentId: null,
                includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: false,
                includeNonLocalDocumentDiagnostics: false, cancellationToken);
    }
}
