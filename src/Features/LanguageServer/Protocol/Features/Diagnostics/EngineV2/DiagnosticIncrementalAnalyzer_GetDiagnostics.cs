// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
        {
            if (id is not LiveDiagnosticUpdateArgsId argsId)
            {
                return SpecializedTasks.EmptyImmutableArray<DiagnosticData>();
            }

            var (documentId, projectId) = (argsId.ProjectOrDocumentId is DocumentId docId) ? (docId, docId.ProjectId) : (null, (ProjectId)argsId.ProjectOrDocumentId);
            return new IdeCachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics).GetSpecificDiagnosticsAsync(argsId.Analyzer, argsId.Kind, cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeCachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds: null, shouldIncludeAnalyzer: null, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds, shouldIncludeAnalyzer, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics).GetDiagnosticsAsync(cancellationToken);

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
            => new IdeLatestDiagnosticGetter(this, solution, projectId, documentId: null, diagnosticIds, shouldIncludeAnalyzer, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);

        private abstract class DiagnosticGetter
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner;

            protected readonly Solution Solution;
            protected readonly ProjectId? ProjectId;
            protected readonly DocumentId? DocumentId;
            protected readonly bool IncludeSuppressedDiagnostics;
            protected readonly bool IncludeNonLocalDocumentDiagnostics;

            private ImmutableArray<DiagnosticData>.Builder? _lazyDataBuilder;

            public DiagnosticGetter(
                DiagnosticIncrementalAnalyzer owner,
                Solution solution,
                ProjectId? projectId,
                DocumentId? documentId,
                bool includeSuppressedDiagnostics,
                bool includeNonLocalDocumentDiagnostics)
            {
                Owner = owner;
                Solution = solution;

                DocumentId = documentId;
                ProjectId = projectId ?? documentId?.ProjectId;

                IncludeSuppressedDiagnostics = includeSuppressedDiagnostics;
                IncludeNonLocalDocumentDiagnostics = includeNonLocalDocumentDiagnostics;
            }

            protected StateManager StateManager => Owner._stateManager;

            protected virtual bool ShouldIncludeDiagnostic(DiagnosticData diagnostic) => true;

            protected ImmutableArray<DiagnosticData> GetDiagnosticData()
                => (_lazyDataBuilder != null) ? _lazyDataBuilder.ToImmutableArray() : ImmutableArray<DiagnosticData>.Empty;

            protected abstract Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken);

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (ProjectId != null)
                {
                    var project = Solution.GetProject(ProjectId);
                    if (project == null)
                    {
                        return GetDiagnosticData();
                    }

                    var documentIds = (DocumentId != null) ? SpecializedCollections.SingletonEnumerable(DocumentId) : project.DocumentIds;

                    // return diagnostics specific to one project or document
                    var includeProjectNonLocalResult = DocumentId == null;
                    await AppendDiagnosticsAsync(project, documentIds, includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                await AppendDiagnosticsAsync(Solution, cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            protected async Task AppendDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
            {
                // PERF: run projects in parallel rather than running CompilationWithAnalyzer with concurrency == true.
                // We do this to not get into thread starvation causing hundreds of threads to be spawned.
                var includeProjectNonLocalResult = true;

                var tasks = new Task[solution.ProjectIds.Count];
                var index = 0;
                foreach (var project in solution.Projects)
                {
                    var localProject = project;
                    tasks[index++] = Task.Run(
                        () => AppendDiagnosticsAsync(
                            localProject, localProject.DocumentIds, includeProjectNonLocalResult, cancellationToken), cancellationToken);
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            protected void AppendDiagnostics(ImmutableArray<DiagnosticData> items)
            {
                Debug.Assert(!items.IsDefault);

                if (_lazyDataBuilder == null)
                {
                    Interlocked.CompareExchange(ref _lazyDataBuilder, ImmutableArray.CreateBuilder<DiagnosticData>(), null);
                }

                lock (_lazyDataBuilder)
                {
                    _lazyDataBuilder.AddRange(items.Where(ShouldIncludeSuppressedDiagnostic).Where(ShouldIncludeDiagnostic));
                }
            }

            private bool ShouldIncludeSuppressedDiagnostic(DiagnosticData diagnostic)
                => IncludeSuppressedDiagnostics || !diagnostic.IsSuppressed;
        }

        private sealed class IdeCachedDiagnosticGetter : DiagnosticGetter
        {
            public IdeCachedDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics)
                : base(owner, solution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics)
            {
            }

            protected override async Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                foreach (var stateSet in StateManager.GetStateSets(project.Id))
                {
                    foreach (var documentId in documentIds)
                    {
                        AppendDiagnostics(await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.Syntax, cancellationToken).ConfigureAwait(false));
                        AppendDiagnostics(await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.Semantic, cancellationToken).ConfigureAwait(false));
                        if (IncludeNonLocalDocumentDiagnostics)
                            AppendDiagnostics(await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AppendDiagnostics(await GetProjectStateDiagnosticsAsync(stateSet, project, documentId: null, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            public async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(DiagnosticAnalyzer analyzer, AnalysisKind analysisKind, CancellationToken cancellationToken)
            {
                var project = Solution.GetProject(ProjectId);
                if (project == null)
                {
                    // when we return cached result, make sure we at least return something that exist in current solution
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var stateSet = StateManager.GetOrCreateStateSet(project, analyzer);
                if (stateSet == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
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
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                if (documentId != null)
                {
                    // file doesn't exist in current solution
                    var document = await project.Solution.GetTextDocumentAsync(
                        documentId,
                        cancellationToken).ConfigureAwait(false);

                    if (document == null)
                    {
                        return ImmutableArray<DiagnosticData>.Empty;
                    }

                    var result = await state.GetAnalysisDataAsync(document, avoidLoadingData: false, cancellationToken).ConfigureAwait(false);
                    return result.GetDocumentDiagnostics(documentId, kind);
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var nonLocalResult = await state.GetProjectAnalysisDataAsync(project, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                return nonLocalResult.GetOtherDiagnostics();
            }
        }

        private sealed class IdeLatestDiagnosticGetter : DiagnosticGetter
        {
            private readonly ImmutableHashSet<string>? _diagnosticIds;
            private readonly Func<DiagnosticAnalyzer, bool>? _shouldIncludeAnalyzer;

            public IdeLatestDiagnosticGetter(
                DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId? projectId,
                DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
                bool includeSuppressedDiagnostics, bool includeNonLocalDocumentDiagnostics)
                : base(owner, solution, projectId, documentId, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics)
            {
                _diagnosticIds = diagnosticIds;
                _shouldIncludeAnalyzer = shouldIncludeAnalyzer;
            }

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (ProjectId != null)
                {
                    var project = Solution.GetProject(ProjectId);
                    if (project != null)
                    {
                        await AppendDiagnosticsAsync(project, SpecializedCollections.EmptyEnumerable<DocumentId>(), includeProjectNonLocalResult: true, cancellationToken).ConfigureAwait(false);
                    }

                    return GetDiagnosticData();
                }

                await AppendDiagnosticsAsync(Solution, cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            protected override bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
                => _diagnosticIds == null || _diagnosticIds.Contains(diagnostic.Id);

            protected override async Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                // get analyzers that are not suppressed.
                var stateSets = StateManager.GetOrCreateStateSets(project).Where(s => ShouldIncludeStateSet(project, s)).ToImmutableArrayOrEmpty();

                var ideOptions = Owner.AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

                // unlike the suppressed (disabled) analyzer, we will include hidden diagnostic only analyzers here.
                var compilation = await CreateCompilationWithAnalyzersAsync(project, ideOptions, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var result = await Owner.GetProjectAnalysisDataAsync(compilation, project, ideOptions, stateSets, forceAnalyzerRun: true, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analysisResult = result.GetResult(stateSet.Analyzer);

                    foreach (var documentId in documentIds)
                    {
                        AppendDiagnostics(analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                        AppendDiagnostics(analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
                        if (IncludeNonLocalDocumentDiagnostics)
                            AppendDiagnostics(analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AppendDiagnostics(analysisResult.GetOtherDiagnostics());
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
