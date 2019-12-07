// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        public Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            if (!(id is LiveDiagnosticUpdateArgsId argsId))
            {
                return Task.FromResult(ImmutableArray<DiagnosticData>.Empty);
            }

            var (documentId, projectId) = (argsId.ProjectOrDocumentId is DocumentId docId) ? (docId, docId.ProjectId) : (null, (ProjectId)argsId.ProjectOrDocumentId);
            return new IdeCachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics).GetSpecificDiagnosticsAsync(argsId.Analyzer, (AnalysisKind)argsId.Kind, cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return new IdeCachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds: null, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return new IdeLatestDiagnosticGetter(this, solution, projectId, documentId, diagnosticIds, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId? projectId, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return new IdeLatestDiagnosticGetter(this, solution, projectId, documentId: null, diagnosticIds: diagnosticIds, includeSuppressedDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);
        }

        private abstract class DiagnosticGetter
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner;

            protected readonly Solution Solution;
            protected readonly ProjectId? ProjectId;
            protected readonly DocumentId? DocumentId;
            protected readonly bool IncludeSuppressedDiagnostics;

            private ImmutableArray<DiagnosticData>.Builder? _lazyDataBuilder;

            public DiagnosticGetter(
                DiagnosticIncrementalAnalyzer owner,
                Solution solution,
                ProjectId? projectId,
                DocumentId? documentId,
                bool includeSuppressedDiagnostics)
            {
                Owner = owner;
                Solution = solution;

                DocumentId = documentId;
                ProjectId = projectId ?? documentId?.ProjectId;

                IncludeSuppressedDiagnostics = includeSuppressedDiagnostics;
            }

            protected StateManager StateManager => Owner._stateManager;

            protected virtual bool ShouldIncludeDiagnostic(DiagnosticData diagnostic) => true;

            protected ImmutableArray<DiagnosticData> GetDiagnosticData()
            {
                return (_lazyDataBuilder != null) ? _lazyDataBuilder.ToImmutableArray() : ImmutableArray<DiagnosticData>.Empty;
            }

            protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken);
            protected abstract Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken);

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
            {
                return IncludeSuppressedDiagnostics || !diagnostic.IsSuppressed;
            }
        }

        private sealed class IdeCachedDiagnosticGetter : DiagnosticGetter
        {
            public IdeCachedDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId? projectId, DocumentId? documentId, bool includeSuppressedDiagnostics)
                : base(owner, solution, projectId, documentId, includeSuppressedDiagnostics)
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
                        AppendDiagnostics(await GetDiagnosticsAsync(stateSet, project, documentId, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AppendDiagnostics(await GetProjectStateDiagnosticsAsync(stateSet, project, documentId: null, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            protected override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken)
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

            private async Task<ImmutableArray<DiagnosticData>> GetProjectStateDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                if (!stateSet.TryGetProjectState(project.Id, out var state))
                {
                    // never analyzed this project yet.
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                if (documentId != null)
                {
                    // file doesn't exist in current solution
                    var document = project.Solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return ImmutableArray<DiagnosticData>.Empty;
                    }

                    var result = await state.GetAnalysisDataAsync(Owner.PersistentStorageService, document, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return result.GetDocumentDiagnostics(documentId, kind);
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var nonLocalResult = await state.GetProjectAnalysisDataAsync(Owner.PersistentStorageService, project, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                return nonLocalResult.GetOtherDiagnostics();
            }
        }

        private sealed class IdeLatestDiagnosticGetter : DiagnosticGetter
        {
            private readonly ImmutableHashSet<string>? _diagnosticIds;

            public IdeLatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, bool includeSuppressedDiagnostics)
                : base(owner, solution, projectId, documentId, includeSuppressedDiagnostics)
            {
                _diagnosticIds = diagnosticIds;
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
                // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                var stateSets = StateManager.GetOrCreateStateSets(project).Where(s => ShouldIncludeStateSet(project, s)).ToImmutableArrayOrEmpty();

                // unlike the suppressed (disabled) analyzer, we will include hidden diagnostic only analyzers here.
                var compilation = await Owner.CreateCompilationWithAnalyzersAsync(project, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var result = await Owner.GetProjectAnalysisDataAsync(compilation, project, stateSets, forceAnalyzerRun: true, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analysisResult = result.GetResult(stateSet.Analyzer);

                    foreach (var documentId in documentIds)
                    {
                        AppendDiagnostics(analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Syntax));
                        AppendDiagnostics(analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.Semantic));
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
                // REVIEW: this can be expensive. any way to do this cheaper?
                var diagnosticService = Owner.AnalyzerService;
                if (diagnosticService.IsAnalyzerSuppressed(stateSet.Analyzer, project))
                {
                    return false;
                }

                if (_diagnosticIds != null && diagnosticService.GetDiagnosticDescriptors(stateSet.Analyzer).All(d => !_diagnosticIds.Contains(d.Id)))
                {
                    return false;
                }

                return true;
            }

            protected override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId? documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stateSets = SpecializedCollections.SingletonCollection(stateSet);

                // Here, we don't care what kind of analyzer (StateSet) is given. 
                var forceAnalyzerRun = true;
                var compilation = await Owner.CreateCompilationWithAnalyzersAsync(project, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                if (documentId != null)
                {
                    var document = project.Solution.GetDocument(documentId);
                    Contract.ThrowIfNull(document);

                    switch (kind)
                    {
                        case AnalysisKind.Syntax:
                        case AnalysisKind.Semantic:
                            return (await Owner.GetDocumentAnalysisDataAsync(compilation, document, stateSet, kind, cancellationToken).ConfigureAwait(false)).Items;

                        case AnalysisKind.NonLocal:
                            var nonLocalDocumentResult = await Owner.GetProjectAnalysisDataAsync(compilation, project, stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);
                            var analysisResult = nonLocalDocumentResult.GetResult(stateSet.Analyzer);
                            return analysisResult.GetDocumentDiagnostics(documentId, AnalysisKind.NonLocal);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(kind);
                    }
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var projectResult = await Owner.GetProjectAnalysisDataAsync(compilation, project, stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);
                return projectResult.GetResult(stateSet.Analyzer).GetOtherDiagnostics();
            }
        }
    }
}
