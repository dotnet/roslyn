// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

[ExportWorkspaceServiceFactory(typeof(ICodeAnalysisDiagnosticAnalyzerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CodeAnalysisDiagnosticAnalyzerServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new CodeAnalysisDiagnosticAnalyzerService(workspaceServices.Workspace);

    private sealed class CodeAnalysisDiagnosticAnalyzerService : ICodeAnalysisDiagnosticAnalyzerService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly Workspace _workspace;

        /// <summary>
        /// Mapping of projects to the diagnostics for the projects that we've finished running "run code analysis" on.
        /// Cached results can now be returned for these through <see cref="GetLastComputedDocumentDiagnostics"/>
        /// and <see cref="GetLastComputedProjectDiagnostics"/>.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, ImmutableArray<DiagnosticData>> _analyzedProjectToDiagnostics = [];

        /// <summary>
        /// Previously analyzed projects that we no longer want to report results for.  This happens when an explicit
        /// build is kicked off.  At that point, we want the build results to win out for a particular project.  We mark
        /// this project (as opposed to removing from <see cref="_analyzedProjectToDiagnostics"/>) as we want our LSP
        /// handler to still think it should process it, as that will the cause the diagnostics to be removed when they
        /// now transition to an empty list returned from this type.
        /// </summary>
        private readonly ConcurrentSet<ProjectId> _clearedProjectIds = [];

        public CodeAnalysisDiagnosticAnalyzerService(Workspace workspace)
        {
            _workspace = workspace;
            _diagnosticAnalyzerService = _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();

            _ = workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);
        }

        private void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.SolutionRemoved:

                    _analyzedProjectToDiagnostics.Clear();
                    _clearedProjectIds.Clear();

                    // Let LSP know so that it requests up to date info, and will see our cached info disappear.
                    _diagnosticAnalyzerService.RequestDiagnosticRefresh();
                    break;
            }
        }

        public void Clear()
        {
            // Clear the list of analyzed projects.
            _clearedProjectIds.AddRange(_analyzedProjectToDiagnostics.Keys);

            // Let LSP know so that it requests up to date info, and will see our cached info disappear.
            _diagnosticAnalyzerService.RequestDiagnosticRefresh();
        }

        public bool HasProjectBeenAnalyzed(ProjectId projectId)
            => _analyzedProjectToDiagnostics.ContainsKey(projectId);

        public async ValueTask RunAnalysisAsync(Project project, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Contract.ThrowIfFalse(project.Solution.Workspace == _workspace);

            var diagnostics = await _diagnosticAnalyzerService.ForceRunCodeAnalysisDiagnosticsAsync(
                project, cancellationToken).ConfigureAwait(false);

            // Add the given project to the analyzed projects list **after** analysis has completed.
            // We need this ordering to ensure that 'HasProjectBeenAnalyzed' call above functions correctly.
            _analyzedProjectToDiagnostics[project.Id] = diagnostics;

            // Remove from the cleared list now that we've run a more recent "run code analysis" on this project.
            _clearedProjectIds.Remove(project.Id);

            // Finally, invoke a workspace refresh request for LSP client to pull onto these diagnostics.
            //
            // TODO: Below call will eventually be replaced with a special workspace refresh request that skips pulling
            // document diagnostics and also does not add any delay for pulling workspace diagnostics.
            _diagnosticAnalyzerService.RequestDiagnosticRefresh();
        }

        /// <summary>
        /// Running code analysis on the project force computes and caches the diagnostics in <see
        /// cref="_analyzedProjectToDiagnostics"/>. We return these cached document diagnostics here, including both
        /// local and non-local document diagnostics.
        /// </summary>
        /// <remarks>
        /// Only returns non-suppressed diagnostics.
        /// </remarks>
        public ImmutableArray<DiagnosticData> GetLastComputedDocumentDiagnostics(DocumentId documentId)
        {
            if (_clearedProjectIds.Contains(documentId.ProjectId))
                return [];

            if (!_analyzedProjectToDiagnostics.TryGetValue(documentId.ProjectId, out var diagnostics))
                return [];

            return diagnostics.WhereAsArray(static (d, documentId) =>
                !d.IsSuppressed && d.DataLocation.DocumentId == documentId, documentId);
        }

        /// <summary>
        /// Running code analysis on the project force computes and caches the diagnostics in <see
        /// cref="_analyzedProjectToDiagnostics"/>. We return these cached project diagnostics here, i.e. diagnostics
        /// with no location, by excluding all local and non-local document diagnostics.
        /// </summary>
        /// <remarks>
        /// Only returns non-suppressed diagnostics.
        /// </remarks>
        public ImmutableArray<DiagnosticData> GetLastComputedProjectDiagnostics(ProjectId projectId)
        {
            if (_clearedProjectIds.Contains(projectId))
                return [];

            if (!_analyzedProjectToDiagnostics.TryGetValue(projectId, out var diagnostics))
                return [];

            return diagnostics.WhereAsArray(static (d, projectId) =>
                !d.IsSuppressed && d.ProjectId == projectId && d.DataLocation.DocumentId == null, projectId);
        }
    }
}
