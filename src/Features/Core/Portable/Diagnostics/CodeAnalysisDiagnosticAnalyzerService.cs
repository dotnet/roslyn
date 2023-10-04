// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Threading.Tasks;
using System.Composition;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportWorkspaceServiceFactory(typeof(ICodeAnalysisDiagnosticAnalyzerService), ServiceLayer.Default), Shared]
    internal sealed class CodeAnalysisDiagnosticAnalyzerServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly IDiagnosticsRefresher _diagnosticsRefresher;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeAnalysisDiagnosticAnalyzerServiceFactory(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            IDiagnosticsRefresher diagnosticsRefresher)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
            _diagnosticsRefresher = diagnosticsRefresher;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new CodeAnalysisDiagnosticAnalyzerService(_diagnosticAnalyzerService, _diagnosticsRefresher, workspaceServices.Workspace);

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

            public async Task RunAnalysisAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId, CancellationToken cancellationToken)
            {
                if (projectId != null)
                {
                    var project = solution.GetProject(projectId);
                    if (project != null)
                    {
                        await AnalyzeProjectCoreAsync(project, onProjectAnalyzed, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var tasks = new Task[solution.ProjectIds.Count];
                    var index = 0;
                    foreach (var project in solution.Projects)
                    {
                        tasks[index++] = Task.Run(() => AnalyzeProjectCoreAsync(project, onProjectAnalyzed, cancellationToken), cancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }

            private async Task AnalyzeProjectCoreAsync(Project project, Action<Project> onProjectAnalyzed, CancellationToken cancellationToken)
            {
                await _diagnosticAnalyzerService.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
                _analyzedProjectIds.Add(project.Id);
                onProjectAnalyzed(project);
                _diagnosticsRefresher.RequestWorkspaceRefresh();
            }

            // Running code analysis on the project force computes and caches the diagnostics on the DiagnosticAnalyzerService.
            // We return these cached document diagnostics here, including both local and non-local document diagnostics.
            public Task<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(DocumentId documentId, Workspace workspace, CancellationToken cancellationToken)
                => _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(workspace, documentId.ProjectId,
                    documentId, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true,
                    includeNonLocalDocumentDiagnostics: true, cancellationToken);

            // Running code analysis on the project force computes and caches the diagnostics on the DiagnosticAnalyzerService.
            // We return these cached project diagnostics here, i.e. diagnostics with no location, by excluding all local and non-local document diagnostics.
            public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(ProjectId projectId, Workspace workspace, CancellationToken cancellationToken)
                => _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(workspace, projectId, documentId: null,
                    includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: false,
                    includeNonLocalDocumentDiagnostics: false, cancellationToken);
        }
    }
}
