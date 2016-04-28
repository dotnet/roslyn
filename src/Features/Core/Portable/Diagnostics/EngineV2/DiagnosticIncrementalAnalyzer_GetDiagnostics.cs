﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDECachedDiagnosticGetter(this, solution, id, includeSuppressedDiagnostics).GetSpecificDiagnosticsAsync(cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDECachedDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDELatestDiagnosticGetter(this, solution, id, includeSuppressedDiagnostics).GetSpecificDiagnosticsAsync(cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDELatestDiagnosticGetter(this, solution, projectId, documentId, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, DocumentId documentId, ImmutableHashSet<string> diagnosticIds, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDELatestDiagnosticGetter(this, diagnosticIds, solution, projectId, documentId, includeSuppressedDiagnostics).GetDiagnosticsAsync(cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId, ImmutableHashSet<string> diagnosticIds, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new IDELatestDiagnosticGetter(this, diagnosticIds, solution, projectId, includeSuppressedDiagnostics).GetProjectDiagnosticsAsync(cancellationToken);
        }

        private abstract class DiagnosticGetter
        {
            protected readonly DiagnosticIncrementalAnalyzer Owner;

            protected readonly Solution CurrentSolution;
            protected readonly ProjectId CurrentProjectId;
            protected readonly DocumentId CurrentDocumentId;
            protected readonly object Id;
            protected readonly bool IncludeSuppressedDiagnostics;

            private ImmutableArray<DiagnosticData>.Builder _builder;

            public DiagnosticGetter(
                DiagnosticIncrementalAnalyzer owner,
                Solution solution,
                ProjectId projectId,
                DocumentId documentId,
                object id,
                bool includeSuppressedDiagnostics)
            {
                Owner = owner;
                CurrentSolution = solution;

                CurrentDocumentId = documentId;
                CurrentProjectId = projectId ?? documentId?.ProjectId;

                Id = id;
                IncludeSuppressedDiagnostics = includeSuppressedDiagnostics;

                // try to retrieve projectId/documentId from id if possible.
                var argsId = id as LiveDiagnosticUpdateArgsId;
                if (argsId != null)
                {
                    CurrentDocumentId = CurrentDocumentId ?? argsId.Key as DocumentId;
                    CurrentProjectId = CurrentProjectId ?? (argsId.Key as ProjectId) ?? CurrentDocumentId.ProjectId;
                }

                _builder = null;
            }

            protected StateManager StateManager => this.Owner._stateManager;

            protected Project CurrentProject => CurrentSolution.GetProject(CurrentProjectId);
            protected Document CurrentDocument => CurrentSolution.GetDocument(CurrentDocumentId);

            protected virtual bool ShouldIncludeDiagnostic(DiagnosticData diagnostic) => true;

            protected ImmutableArray<DiagnosticData> GetDiagnosticData()
            {
                return _builder != null ? _builder.ToImmutableArray() : ImmutableArray<DiagnosticData>.Empty;
            }

            protected abstract Task<ImmutableArray<DiagnosticData>?> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId documentId, AnalysisKind kind, CancellationToken cancellationToken);
            protected abstract Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken);

            public async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (CurrentSolution == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var argsId = Id as LiveDiagnosticUpdateArgsId;
                if (argsId == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                if (CurrentProject == null)
                {
                    // when we return cached result, make sure we at least return something that exist in current solution
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var stateSet = this.StateManager.GetOrCreateStateSet(CurrentProject, argsId.Analyzer);
                if (stateSet == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var diagnostics = await GetDiagnosticsAsync(stateSet, CurrentProject, CurrentDocumentId, (AnalysisKind)argsId.Kind, cancellationToken).ConfigureAwait(false);
                if (diagnostics == null)
                {
                    // Document or project might have been removed from the solution.
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                return FilterSuppressedDiagnostics(diagnostics.Value);
            }

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (CurrentSolution == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                if (CurrentProjectId != null)
                {
                    if (CurrentProject == null)
                    {
                        return GetDiagnosticData();
                    }

                    var documentIds = CurrentDocumentId != null ? SpecializedCollections.SingletonEnumerable(CurrentDocumentId) : CurrentProject.DocumentIds;

                    // return diagnostics specific to one project or document
                    var includeProjectNonLocalResult = CurrentDocumentId == null;
                    await AppendDiagnosticsAsync(CurrentProject, documentIds, includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                await AppendDiagnosticsAsync(CurrentSolution, cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            protected async Task AppendDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
            {
                // PERF; run projects parallely rather than running CompilationWithAnalyzer with concurrency == true.
                //       we doing this to be safe (not get into thread starvation causing hundreds of threads to be spawn up).
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

            protected void AppendDiagnostics(IEnumerable<DiagnosticData> items)
            {
                if (items == null)
                {
                    return;
                }

                if (_builder == null)
                {
                    Interlocked.CompareExchange(ref _builder, ImmutableArray.CreateBuilder<DiagnosticData>(), null);
                }

                lock (_builder)
                {
                    _builder.AddRange(items.Where(ShouldIncludeSuppressedDiagnostic).Where(ShouldIncludeDiagnostic));
                }
            }

            private bool ShouldIncludeSuppressedDiagnostic(DiagnosticData diagnostic)
            {
                return IncludeSuppressedDiagnostics || !diagnostic.IsSuppressed;
            }

            private ImmutableArray<DiagnosticData> FilterSuppressedDiagnostics(ImmutableArray<DiagnosticData> diagnostics)
            {
                if (IncludeSuppressedDiagnostics || diagnostics.IsDefaultOrEmpty)
                {
                    return diagnostics;
                }

                // create builder only if there is suppressed diagnostics
                ImmutableArray<DiagnosticData>.Builder builder = null;
                for (int i = 0; i < diagnostics.Length; i++)
                {
                    var diagnostic = diagnostics[i];
                    if (diagnostic.IsSuppressed)
                    {
                        if (builder == null)
                        {
                            builder = ImmutableArray.CreateBuilder<DiagnosticData>();
                            for (int j = 0; j < i; j++)
                            {
                                builder.Add(diagnostics[j]);
                            }
                        }
                    }
                    else if (builder != null)
                    {
                        builder.Add(diagnostic);
                    }
                }

                return builder != null ? builder.ToImmutable() : diagnostics;
            }
        }

        private class IDECachedDiagnosticGetter : DiagnosticGetter
        {
            public IDECachedDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, object id, bool includeSuppressedDiagnostics) :
                base(owner, solution, projectId: null, documentId: null, id: id, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
            }

            public IDECachedDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics) :
                base(owner, solution, projectId, documentId, id: null, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
            }

            protected override async Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                // when we return cached result, make sure we at least return something that exist in current solution
                if (project == null)
                {
                    return;
                }

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
                        DocumentId targetDocumentId = null;
                        AppendDiagnostics(await GetProjectStateDiagnosticsAsync(stateSet, project, targetDocumentId, AnalysisKind.NonLocal, cancellationToken).ConfigureAwait(false));
                    }
                }
            }

            protected override async Task<ImmutableArray<DiagnosticData>?> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var activeFileDiagnostics = GetActiveFileDiagnostics(stateSet, documentId, kind);
                if (activeFileDiagnostics.HasValue)
                {
                    return activeFileDiagnostics.Value;
                }

                var projectDiagnostics = await GetProjectStateDiagnosticsAsync(stateSet, project, documentId, kind, cancellationToken).ConfigureAwait(false);
                if (projectDiagnostics.HasValue)
                {
                    return projectDiagnostics.Value;
                }

                return null;
            }

            private ImmutableArray<DiagnosticData>? GetActiveFileDiagnostics(StateSet stateSet, DocumentId documentId, AnalysisKind kind)
            {
                if (documentId == null || kind == AnalysisKind.NonLocal)
                {
                    return null;
                }

                ActiveFileState state;
                if (!stateSet.TryGetActiveFileState(documentId, out state))
                {
                    return null;
                }

                return state.GetAnalysisData(kind).Items;
            }

            private async Task<ImmutableArray<DiagnosticData>?> GetProjectStateDiagnosticsAsync(
                StateSet stateSet, Project project, DocumentId documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                ProjectState state;
                if (!stateSet.TryGetProjectState(project.Id, out state))
                {
                    // never analyzed this project yet.
                    return null;
                }

                if (documentId != null)
                {
                    // file doesn't exist in current solution
                    var document = project.Solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var result = await state.GetAnalysisDataAsync(document, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return GetResult(result, kind, documentId);
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var nonLocalResult = await state.GetProjectAnalysisDataAsync(project, avoidLoadingData: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                return nonLocalResult.Others;
            }
        }

        private class IDELatestDiagnosticGetter : DiagnosticGetter
        {
            private readonly ImmutableHashSet<string> _diagnosticIds;

            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, object id, bool includeSuppressedDiagnostics) :
                base(owner, solution, projectId: null, documentId: null, id: id, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
                _diagnosticIds = null;
            }

            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, Solution solution, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics) :
                base(owner, solution, projectId, documentId, id: null, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
                _diagnosticIds = null;
            }

            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, ImmutableHashSet<string> diagnosticIds, Solution solution, ProjectId projectId, bool includeSuppressedDiagnostics) :
                this(owner, diagnosticIds, solution, projectId, documentId: null, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
            }

            public IDELatestDiagnosticGetter(DiagnosticIncrementalAnalyzer owner, ImmutableHashSet<string> diagnosticIds, Solution solution, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics) :
                base(owner, solution, projectId, documentId, id: null, includeSuppressedDiagnostics: includeSuppressedDiagnostics)
            {
                _diagnosticIds = diagnosticIds;
            }

            public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(CancellationToken cancellationToken)
            {
                if (CurrentSolution == null)
                {
                    return GetDiagnosticData();
                }

                if (CurrentProjectId != null)
                {
                    var includeProjectNonLocalResult = true;
                    await AppendDiagnosticsAsync(CurrentProject, SpecializedCollections.EmptyEnumerable<DocumentId>(), includeProjectNonLocalResult, cancellationToken).ConfigureAwait(false);
                    return GetDiagnosticData();
                }

                await AppendDiagnosticsAsync(CurrentSolution, cancellationToken).ConfigureAwait(false);
                return GetDiagnosticData();
            }

            protected override bool ShouldIncludeDiagnostic(DiagnosticData diagnostic)
            {
                return _diagnosticIds == null || _diagnosticIds.Contains(diagnostic.Id);
            }

            protected override async Task AppendDiagnosticsAsync(Project project, IEnumerable<DocumentId> documentIds, bool includeProjectNonLocalResult, CancellationToken cancellationToken)
            {
                // when we return cached result, make sure we at least return something that exist in current solution
                if (project == null)
                {
                    return;
                }

                // get analyzers that are not suppressed.
                // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                var stateSets = StateManager.GetOrCreateStateSets(project).Where(s => ShouldIncludeStateSet(project, s)).ToImmutableArrayOrEmpty();

                // unlike the suppressed (disabled) analyzer, we will include hidden diagnostic only analyzers here.
                var analyzerDriverOpt = await Owner._compilationManager.CreateAnalyzerDriverAsync(project, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var ignoreFullAnalysisOptions = true;
                var result = await Owner._executor.GetProjectAnalysisDataAsync(analyzerDriverOpt, project, stateSets, ignoreFullAnalysisOptions, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analysisResult = result.GetResult(stateSet.Analyzer);

                    foreach (var documentId in documentIds)
                    {
                        AppendDiagnostics(GetResult(analysisResult, AnalysisKind.Syntax, documentId));
                        AppendDiagnostics(GetResult(analysisResult, AnalysisKind.Semantic, documentId));
                        AppendDiagnostics(GetResult(analysisResult, AnalysisKind.NonLocal, documentId));
                    }

                    if (includeProjectNonLocalResult)
                    {
                        // include project diagnostics if there is no target document
                        AppendDiagnostics(analysisResult.Others);
                    }
                }
            }

            private bool ShouldIncludeStateSet(Project project, StateSet stateSet)
            {
                // REVIEW: this can be expensive. any way to do this cheaper?
                var diagnosticService = Owner.Owner;
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

            protected override async Task<ImmutableArray<DiagnosticData>?> GetDiagnosticsAsync(StateSet stateSet, Project project, DocumentId documentId, AnalysisKind kind, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stateSets = SpecializedCollections.SingletonCollection(stateSet);

                // Here, we don't care what kind of analyzer (StateSet) is given. 
                // We just create and use AnalyzerDriver with the given analyzer (StateSet). 
                var ignoreFullAnalysisOptions = true;
                var analyzerDriverOpt = await Owner._compilationManager.CreateAnalyzerDriverAsync(project, stateSets, IncludeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                if (documentId != null)
                {
                    var document = project.Solution.GetDocument(documentId);
                    Contract.ThrowIfNull(document);

                    switch (kind)
                    {
                        case AnalysisKind.Syntax:
                        case AnalysisKind.Semantic:
                            {
                                var result = await Owner._executor.GetDocumentAnalysisDataAsync(analyzerDriverOpt, document, stateSet, kind, cancellationToken).ConfigureAwait(false);
                                return result.Items;
                            }
                        case AnalysisKind.NonLocal:
                            {
                                var nonLocalDocumentResult = await Owner._executor.GetProjectAnalysisDataAsync(analyzerDriverOpt, project, stateSets, ignoreFullAnalysisOptions, cancellationToken).ConfigureAwait(false);
                                var analysisResult = nonLocalDocumentResult.GetResult(stateSet.Analyzer);
                                return GetResult(analysisResult, AnalysisKind.NonLocal, documentId);
                            }
                        default:
                            return Contract.FailWithReturn<ImmutableArray<DiagnosticData>?>("shouldn't reach here");
                    }
                }

                Contract.ThrowIfFalse(kind == AnalysisKind.NonLocal);
                var projectResult = await Owner._executor.GetProjectAnalysisDataAsync(analyzerDriverOpt, project, stateSets, ignoreFullAnalysisOptions, cancellationToken).ConfigureAwait(false);
                return projectResult.GetResult(stateSet.Analyzer).Others;
            }
        }
    }
}