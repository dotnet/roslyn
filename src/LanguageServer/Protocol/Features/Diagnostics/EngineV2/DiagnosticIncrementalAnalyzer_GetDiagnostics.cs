// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new DiagnosticGetter(this, solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new DiagnosticGetter(this, solution, projectId, documentId: null, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics: false, includeNonLocalDocumentDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);

        private sealed class DiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId projectId,
            DocumentId? documentId,
            ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics)
        {
            private readonly DiagnosticIncrementalAnalyzer Owner = owner;

            private readonly Solution Solution = solution;
            private readonly ProjectId ProjectId = projectId;
            private readonly DocumentId? DocumentId = documentId;
            private readonly ImmutableHashSet<string>? _diagnosticIds = diagnosticIds;
            private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer = shouldIncludeAnalyzer;
            private readonly bool IncludeLocalDocumentDiagnostics = includeLocalDocumentDiagnostics;
            private readonly bool IncludeNonLocalDocumentDiagnostics = includeNonLocalDocumentDiagnostics;

            private StateManager StateManager => Owner._stateManager;

            private bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
                => _diagnosticIds == null || _diagnosticIds.Contains(diagnostic.Id);

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project == null)
                    return [];

                // return diagnostics specific to one project or document
                var includeProjectNonLocalResult = DocumentId == null;
                return await ProduceProjectDiagnosticsAsync(
                    project,
                    // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
                    // project if no specific document id was requested.
                    this.DocumentId != null ? [this.DocumentId] : [.. project.DocumentIds, .. project.AdditionalDocumentIds],
                    includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
            }

            private async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds,
                bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);
                await this.ProduceDiagnosticsAsync(
                    project, documentIds, includeProjectNonLocalResult, builder, cancellationToken).ConfigureAwait(false);
                return builder.ToImmutableAndClear();
            }

            private void AddIncludedDiagnostics(ArrayBuilder<DiagnosticData> builder, ImmutableArray<DiagnosticData> diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (ShouldIncludeDiagnostic(diagnostic))
                        builder.Add(diagnostic);
                }
            }

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project is null)
                    return [];

                return await ProduceProjectDiagnosticsAsync(
                    project, documentIds: [], includeProjectNonLocalResult: true, cancellationToken).ConfigureAwait(false);
            }

            private async Task ProduceDiagnosticsAsync(
                Project project,
                IReadOnlyList<DocumentId> documentIds,
                bool includeProjectNonLocalResult,
                ArrayBuilder<DiagnosticData> builder,
                CancellationToken cancellationToken)
            {
                var analyzersForProject = await StateManager.GetOrCreateAnalyzersAsync(project, cancellationToken).ConfigureAwait(false);
                var hostAnalyzerInfo = await StateManager.GetOrCreateHostAnalyzerInfoAsync(project, cancellationToken).ConfigureAwait(false);
                var analyzers = analyzersForProject.WhereAsArray(a => ShouldIncludeAnalyzer(project, a));

                var result = await GetOrComputeDiagnosticAnalysisResultsAsync(analyzers).ConfigureAwait(false);

                foreach (var analyzer in analyzers)
                {
                    if (!result.TryGetValue(analyzer, out var analysisResult))
                        continue;

                    foreach (var documentId in documentIds)
                    {
                        if (IncludeLocalDocumentDiagnostics)
                        {
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
                        }

                        if (IncludeNonLocalDocumentDiagnostics)
                            AddIncludedDiagnostics(builder, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AddIncludedDiagnostics(builder, analysisResult.GetOtherDiagnostics());
                    }
                }

                async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetOrComputeDiagnosticAnalysisResultsAsync(
                    ImmutableArray<DiagnosticAnalyzer> analyzers)
                {
                    // If there was a 'ForceAnalyzeProjectAsync' run for this project, we can piggy back off of the
                    // prior computed/cached results as they will be a superset of the results we want.
                    //
                    // Note: the caller will loop over *its* analzyers, grabbing from the full set of data we've cached
                    // for this project, and filtering down further.  So it's ok to return this potentially larger set.
                    //
                    // Note: While ForceAnalyzeProjectAsync should always run with a larger set of analyzers than us
                    // (since it runs all analyzers), we still run a paranoia check that the analyzers we care about are
                    // a subset of that call so that we don't accidentally reuse results that would not correspond to
                    // what we are computing ourselves.
                    if (this.Owner._projectToForceAnalysisData.TryGetValue(project, out var box) &&
                        analyzers.IsSubsetOf(box.Value.analyzers))
                    {
                        return box.Value.diagnosticAnalysisResults;
                    }

                    // Otherwise, just compute for the analyzers we care about.
                    var compilation = await GetOrCreateCompilationWithAnalyzersAsync(
                        project, analyzers, hostAnalyzerInfo, Owner.AnalyzerService.CrashOnAnalyzerException, cancellationToken).ConfigureAwait(false);

                    var result = await Owner.ComputeDiagnosticAnalysisResultsAsync(compilation, project, analyzers, cancellationToken).ConfigureAwait(false);
                    return result;
                }
            }

            private bool ShouldIncludeAnalyzer(Project project, DiagnosticAnalyzer analyzer)
            {
                if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(analyzer, project, Owner.GlobalOptions))
                {
                    return false;
                }

                if (_shouldIncludeAnalyzer != null && !_shouldIncludeAnalyzer(analyzer))
                {
                    return false;
                }

                if (_diagnosticIds != null && Owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer).All(d => !_diagnosticIds.Contains(d.Id)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
