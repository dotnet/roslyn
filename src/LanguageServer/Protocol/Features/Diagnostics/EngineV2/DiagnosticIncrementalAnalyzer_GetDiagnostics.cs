// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeCachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, Func<Project, DocumentId?, IReadOnlyList<DocumentId>>? getDocuments, bool includeSuppressedDiagnostics, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, getDocuments, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId: null, diagnosticIds, shouldIncludeAnalyzer, getDocuments: null, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics: false, includeNonLocalDocumentDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);

        private abstract class DiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId? projectId,
            DocumentId? documentId,
            Func<Project, DocumentId?, IReadOnlyList<DocumentId>>? getDocuments,
            bool includeSuppressedDiagnostics,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics)
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner = owner;

            protected readonly Solution Solution = solution;
            protected readonly ProjectId? ProjectId = projectId ?? documentId?.ProjectId;
            protected readonly DocumentId? DocumentId = documentId;
            protected readonly bool IncludeSuppressedDiagnostics = includeSuppressedDiagnostics;
            protected readonly bool IncludeLocalDocumentDiagnostics = includeLocalDocumentDiagnostics;
            protected readonly bool IncludeNonLocalDocumentDiagnostics = includeNonLocalDocumentDiagnostics;

            private readonly Func<Project, DocumentId?, IReadOnlyList<DocumentId>> _getDocuments = getDocuments ?? (static (project, documentId) => documentId != null ? [documentId] : project.DocumentIds);

            protected StateManager StateManager => Owner._stateManager;

            protected virtual bool ShouldIncludeDiagnostic(DiagnosticData diagnostic) => true;

            protected abstract Task ProduceDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds, bool includeProjectNonLocalResult, Action<DiagnosticData> callback, CancellationToken cancellationToken);

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (ProjectId != null)
                {
                    var project = Solution.GetProject(ProjectId);
                    if (project == null)
                        return [];

                    // return diagnostics specific to one project or document
                    var includeProjectNonLocalResult = DocumentId == null;
                    return await ProduceProjectDiagnosticsAsync(
                        [project], project => _getDocuments(project, DocumentId), includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
                }

                return await ProduceSolutionDiagnosticsAsync(Solution, cancellationToken).ConfigureAwait(false);
            }

            protected Task<ImmutableArray<DiagnosticData>> ProduceSolutionDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
                => ProduceProjectDiagnosticsAsync(solution.Projects, static project => project.DocumentIds, includeProjectNonLocalResult: true, cancellationToken);

            protected async Task<ImmutableArray<DiagnosticData>> ProduceProjectDiagnosticsAsync(
                IEnumerable<Project> projects, Func<Project, IReadOnlyList<DocumentId>> getDocumentIds,
                bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                // PERF: run projects in parallel rather than running CompilationWithAnalyzer with concurrency == true.
                // We do this to not get into thread starvation causing hundreds of threads to be spawned.
                return await ProducerConsumer<DiagnosticData>.RunParallelAsync(
                    source: projects,
                    produceItems: static (project, callback, args, cancellationToken) => args.@this.ProduceDiagnosticsAsync(
                        project, args.getDocumentIds(project), args.includeProjectNonLocalResult, callback, cancellationToken),
                    args: (@this: this, getDocumentIds, includeProjectNonLocalResult),
                    cancellationToken).ConfigureAwait(false);
            }

            protected void InvokeCallback(Action<DiagnosticData> callback, ImmutableArray<DiagnosticData> diagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (ShouldIncludeSuppressedDiagnostic(diagnostic) && ShouldIncludeDiagnostic(diagnostic))
                        callback(diagnostic);
                }
            }

            private bool ShouldIncludeSuppressedDiagnostic(DiagnosticData diagnostic)
                => IncludeSuppressedDiagnostics || !diagnostic.IsSuppressed;
        }

        private sealed class IdeCachedDiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId? projectId,
            DocumentId? documentId,
            bool includeSuppressedDiagnostics,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics) : DiagnosticGetter(
                owner, solution, projectId, documentId, getDocuments: null, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics)
        {
            protected override async Task ProduceDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds, bool includeProjectNonLocalResult,
                Action<DiagnosticData> callback, CancellationToken cancellationToken)
            {
                foreach (var stateSet in StateManager.GetStateSets(project.Id))
                {
                    foreach (var documentId in documentIds)
                    {
                        if (IncludeLocalDocumentDiagnostics)
                        {
                            InvokeCallback(callback, await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.Syntax, cancellationToken).ConfigureAwait(false));
                            InvokeCallback(callback, await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.Semantic, cancellationToken).ConfigureAwait(false));
                        }

                        if (IncludeNonLocalDocumentDiagnostics)
                            InvokeCallback(callback, await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        InvokeCallback(callback, await GetProjectStateDiagnosticsAsync(stateSet, project, documentId: null, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            public async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(DiagnosticAnalyzer analyzer, AnalysisKind analysisKind, CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project == null)
                {
                    // when we return cached result, make sure we at least return something that exist in current solution
                    return [];
                }

                var stateSet = StateManager.GetOrCreateStateSet(project, analyzer);
                if (stateSet == null)
                {
                    return [];
                }

                var diagnostics = await GetDiagnosticsAsync(stateSet, project, DocumentId, analysisKind, cancellationToken).ConfigureAwait(false);

                return IncludeSuppressedDiagnostics ? diagnostics : diagnostics.WhereAsArray(d => !d.IsSuppressed);
            }

            private static async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // active file diagnostics:
                if (documentId != null && kind != AnalysisKind.NonLocal && stateSet.TryGetActiveFileState(documentId, out var state))
                {
                    return state.GetAnalysisData(kind).Items;
                }

                // project diagnostics:
                return await GetProjectStateDiagnosticsAsync(stateSet, project, documentId, kind, cancellationToken).ConfigureAwait(false);
            }

            private static async Task<ImmutableArray<DiagnosticData>> GetProjectStateDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                if (!stateSet.TryGetProjectState(project.Id, out var state))
                {
                    // never analyzed this project yet.
                    return [];
                }

                if (documentId != null)
                {
                    // file doesn't exist in current solution
                    var document = await project.Solution.GetTextDocumentAsync(
                        documentId,
                        cancellationToken).ConfigureAwait(false);

                    if (document == null)
                    {
                        return [];
                    }

                    var result = await state.GetAnalysisDataAsync(document, avoidLoadingData: false, cancellationToken).ConfigureAwait(false);
                    return result.GetDocumentDiagnostics(documentId, kind);
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var nonLocalResult = await state.GetProjectAnalysisDataAsync(project, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                return nonLocalResult.GetOtherDiagnostics();
            }
        }

        private sealed class IdeLatestDiagnosticGetter(
            DiagnosticIncrementalAnalyzer owner,
            Solution solution,
            ProjectId? projectId,
            DocumentId? documentId,
            ImmutableHashSet<string>? diagnosticIds,
            Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
            Func<Project, DocumentId?, IReadOnlyList<DocumentId>>? getDocuments,
            bool includeSuppressedDiagnostics,
            bool includeLocalDocumentDiagnostics,
            bool includeNonLocalDocumentDiagnostics) : DiagnosticGetter(
                owner, solution, projectId, documentId, getDocuments, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics)
        {
            private readonly ImmutableHashSet<string>? _diagnosticIds = diagnosticIds;
            private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer = shouldIncludeAnalyzer;

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (ProjectId != null)
                {
                    var project = Solution.GetProject(ProjectId);
                    if (project is null)
                        return [];

                    return await ProduceProjectDiagnosticsAsync(
                        [project], static _ => [], includeProjectNonLocalResult: true, cancellationToken).ConfigureAwait(false);
                }

                return await ProduceSolutionDiagnosticsAsync(Solution, cancellationToken).ConfigureAwait(false);
            }

            protected override bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
                => _diagnosticIds == null || _diagnosticIds.Contains(diagnostic.Id);

            protected override async Task ProduceDiagnosticsAsync(
                Project project, IReadOnlyList<DocumentId> documentIds, bool includeProjectNonLocalResult,
                Action<DiagnosticData> callback, CancellationToken cancellationToken)
            {
                // get analyzers that are not suppressed.
                var stateSets = StateManager.GetOrCreateStateSets(project).Where(s => ShouldIncludeStateSet(project, s)).ToImmutableArrayOrEmpty();

                var ideOptions = Owner.AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

                // unlike the suppressed (disabled) analyzer, we will include hidden diagnostic only analyzers here.
                var compilation = await CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var result = await Owner.GetProjectAnalysisDataAsync(compilation, project, ideOptions, stateSets, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analysisResult = result.GetResult(stateSet.Analyzer);

                    foreach (var documentId in documentIds)
                    {
                        if (IncludeLocalDocumentDiagnostics)
                        {
                            InvokeCallback(callback, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                            InvokeCallback(callback, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
                        }

                        if (IncludeNonLocalDocumentDiagnostics)
                            InvokeCallback(callback, analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        InvokeCallback(callback, analysisResult.GetOtherDiagnostics());
                    }
                }
            }

            private bool ShouldIncludeStateSet(Project project, StateSet stateSet)
            {
                if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(stateSet.Analyzer, project, Owner.GlobalOptions))
                {
                    return false;
                }

                if (_shouldIncludeAnalyzer != null && !_shouldIncludeAnalyzer(stateSet.Analyzer))
                {
                    return false;
                }

                if (_diagnosticIds != null && Owner.DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(stateSet.Analyzer).All(d => !_diagnosticIds.Contains(d.Id)))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
