// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            => AnalyzeDocumentForKindAsync(document, AnalysisKind.Syntax, cancellationToken);

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            => AnalyzeDocumentForKindAsync(document, AnalysisKind.Semantic, cancellationToken);

        public Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
            => AnalyzeDocumentForKindAsync(textDocument, AnalysisKind.Syntax, cancellationToken);

        private async Task AnalyzeDocumentForKindAsync(TextDocument document, AnalysisKind kind, CancellationToken cancellationToken)
        {
            try
            {
                if (!document.SupportsDiagnostics())
                {
                    return;
                }

                var isActiveDocument = _documentTrackingService.TryGetActiveDocument() == document.Id;
                var isOpenDocument = document.IsOpen();
                var isGeneratedRazorDocument = document.IsRazorDocument();

                // Only analyze open/active documents, unless it is a generated Razor document.
                if (!isActiveDocument && !isOpenDocument && !isGeneratedRazorDocument)
                {
                    return;
                }

                var stateSets = _stateManager.GetOrUpdateStateSets(document.Project);
                var compilationWithAnalyzers = await GetOrCreateCompilationWithAnalyzersAsync(document.Project, stateSets, cancellationToken).ConfigureAwait(false);
                var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                var backgroundAnalysisScope = GlobalOptions.GetBackgroundAnalysisScope(document.Project.Language);
                var compilerDiagnosticsScope = GlobalOptions.GetBackgroundCompilerAnalysisScope(document.Project.Language);

                // TODO: Switch to a more reliable service to determine visible documents.
                //       DocumentTrackingService is known be unreliable at times.
                var isVisibleDocument = _documentTrackingService.GetVisibleDocuments().Contains(document.Id);

                // We split the diagnostic computation for document into following steps:
                //  1. Try to get cached diagnostics for each analyzer, while computing the set of analyzers that do not have cached diagnostics.
                //  2. Execute all the non-cached analyzers with a single invocation into CompilationWithAnalyzers.
                //  3. Fetch computed diagnostics per-analyzer from the above invocation, and cache and raise diagnostic reported events.
                // In near future, the diagnostic computation invocation into CompilationWithAnalyzers will be moved to OOP.
                // This should help simplify and/or remove the IDE layer diagnostic caching in devenv process.

                // First attempt to fetch diagnostics from the cache, while computing the analyzers that are not cached.
                using var _ = ArrayBuilder<(DiagnosticAnalyzer analyzer, ActiveFileState state)>.GetInstance(out var nonCachedAnalyzersAndStates);
                foreach (var stateSet in stateSets)
                {
                    var (activeFileState, existingData) = TryGetCachedDocumentAnalysisData(document, stateSet, kind, version,
                        backgroundAnalysisScope, compilerDiagnosticsScope, isActiveDocument, isVisibleDocument,
                        isOpenDocument, isGeneratedRazorDocument, cancellationToken, out var isAnalyzerSuppressed);
                    if (existingData.HasValue)
                    {
                        PersistAndRaiseDiagnosticsIfNeeded(existingData.Value, stateSet.Analyzer, activeFileState);
                    }
                    else if (!isAnalyzerSuppressed)
                    {
                        nonCachedAnalyzersAndStates.Add((stateSet.Analyzer, activeFileState));
                    }
                }

                // Then, compute the diagnostics for non-cached state sets, and cache and raise diagnostic reported events for these diagnostics.
                if (nonCachedAnalyzersAndStates.Count > 0)
                {
                    var analysisScope = new DocumentAnalysisScope(document, span: null, nonCachedAnalyzersAndStates.SelectAsArray(s => s.analyzer), kind);
                    var executor = new DocumentAnalysisExecutor(analysisScope, compilationWithAnalyzers, _diagnosticAnalyzerRunner, isExplicit: false, logPerformanceInfo: false, onAnalysisException: OnAnalysisException);
                    var logTelemetry = GlobalOptions.GetOption(DiagnosticOptionsStorage.LogTelemetryForBackgroundAnalyzerExecution);
                    foreach (var (analyzer, state) in nonCachedAnalyzersAndStates)
                    {
                        var computedData = await ComputeDocumentAnalysisDataAsync(executor, analyzer, state, logTelemetry, cancellationToken).ConfigureAwait(false);
                        PersistAndRaiseDiagnosticsIfNeeded(computedData, analyzer, state);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            void PersistAndRaiseDiagnosticsIfNeeded(DocumentAnalysisData result, DiagnosticAnalyzer analyzer, ActiveFileState state)
            {
                if (result.FromCache == true)
                {
                    RaiseDocumentDiagnosticsIfNeeded(document, analyzer, kind, result.Items);
                    return;
                }

                // no cancellation after this point.
                state.Save(kind, result.ToPersistData());

                RaiseDocumentDiagnosticsIfNeeded(document, analyzer, kind, result.OldItems, result.Items);
            }

            void OnAnalysisException()
            {
                // Do not re-use cached CompilationWithAnalyzers instance in presence of an exception, as the underlying analysis state might be corrupt.
                ClearCompilationsWithAnalyzersCache(document.Project);
            }
        }

        public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            await AnalyzeProjectAsync(project, forceAnalyzerRun: false, cancellationToken).ConfigureAwait(false);
        }

        public Task ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
            => AnalyzeProjectAsync(project, forceAnalyzerRun: true, cancellationToken);

        private async Task AnalyzeProjectAsync(Project project, bool forceAnalyzerRun, CancellationToken cancellationToken)
        {
            try
            {
                var stateSets = GetStateSetsForFullSolutionAnalysis(_stateManager.GetOrUpdateStateSets(project), project);

                // get driver only with active analyzers.
                var ideOptions = AnalyzerService.GlobalOptions.GetIdeAnalyzerOptions(project);

                // PERF: get analyzers that are not suppressed and marked as open file only
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                var activeAnalyzers = stateSets
                                        .Select(s => s.Analyzer)
                                        .Where(a => DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(a, project, GlobalOptions) && !a.IsOpenFileOnly(ideOptions.CleanupOptions?.SimplifierOptions));

                CompilationWithAnalyzers? compilationWithAnalyzers = null;

                if (forceAnalyzerRun || GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language))
                {
                    compilationWithAnalyzers = await DocumentAnalysisExecutor.CreateCompilationWithAnalyzersAsync(project, ideOptions, activeAnalyzers, includeSuppressedDiagnostics: true, cancellationToken).ConfigureAwait(false);
                }

                var result = await GetProjectAnalysisDataAsync(compilationWithAnalyzers, project, ideOptions, stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);

                // no cancellation after this point.
                using var _ = ArrayBuilder<StateSet>.GetInstance(out var analyzedStateSetsBuilder);
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetOrCreateProjectState(project.Id);

                    if (result.TryGetResult(stateSet.Analyzer, out var analyzerResult))
                    {
                        await state.SaveToInMemoryStorageAsync(project, analyzerResult).ConfigureAwait(false);
                        analyzedStateSetsBuilder.Add(stateSet);
                    }
                }

                if (analyzedStateSetsBuilder.Count > 0)
                {
                    var oldResult = result.OldResult ?? ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty;
                    RaiseProjectDiagnosticsIfNeeded(project, analyzedStateSetsBuilder.ToImmutable(), oldResult, result.Result);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            => TextDocumentOpenAsync(document, cancellationToken);

        public Task NonSourceDocumentOpenAsync(TextDocument document, CancellationToken cancellationToken)
            => TextDocumentOpenAsync(document, cancellationToken);

        private async Task TextDocumentOpenAsync(TextDocument document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other component knows about this event
                ClearCompilationsWithAnalyzersCache();

                // can not be canceled
                foreach (var stateSet in stateSets)
                    await stateSet.OnDocumentOpenedAsync(document).ConfigureAwait(false);
            }
        }

        public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            => TextDocumentCloseAsync(document, cancellationToken);

        public Task NonSourceDocumentCloseAsync(TextDocument document, CancellationToken cancellationToken)
            => TextDocumentCloseAsync(document, cancellationToken);

        private async Task TextDocumentCloseAsync(TextDocument document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentClose, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                ClearCompilationsWithAnalyzersCache();

                // can not be canceled
                var documentHadDiagnostics = false;
                foreach (var stateSet in stateSets)
                    documentHadDiagnostics |= await stateSet.OnDocumentClosedAsync(document, GlobalOptions).ConfigureAwait(false);

                RaiseDiagnosticsRemovedIfRequiredForClosedOrResetDocument(document, stateSets, documentHadDiagnostics);
            }
        }

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            => TextDocumentResetAsync(document, cancellationToken);

        public Task NonSourceDocumentResetAsync(TextDocument document, CancellationToken cancellationToken)
            => TextDocumentResetAsync(document, cancellationToken);

        private Task TextDocumentResetAsync(TextDocument document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                ClearCompilationsWithAnalyzersCache();
                // can not be canceled
                var documentHadDiagnostics = false;
                foreach (var stateSet in stateSets)
                    documentHadDiagnostics |= stateSet.OnDocumentReset(document);

                RaiseDiagnosticsRemovedIfRequiredForClosedOrResetDocument(document, stateSets, documentHadDiagnostics);
            }

            return Task.CompletedTask;
        }

        private void RaiseDiagnosticsRemovedIfRequiredForClosedOrResetDocument(TextDocument document, IEnumerable<StateSet> stateSets, bool documentHadDiagnostics)
        {
            // If there was no diagnostic reported for this document, nothing to clean up
            // This is done for Perf to reduce raising events unnecessarily.
            if (!documentHadDiagnostics)
                return;

            // If full solution analysis is enabled for both compiler diagnostics and analyzers,
            // we don't need to clear diagnostics for individual documents on document close/reset.
            // This is done for Perf to reduce raising events unnecessarily.
            var _ = GlobalOptions.IsFullSolutionAnalysisEnabled(document.Project.Language, out var compilerFullAnalysisEnabled, out var analyzersFullAnalysisEnabled);
            if (compilerFullAnalysisEnabled && analyzersFullAnalysisEnabled)
                return;

            var removeDiagnosticsOnDocumentClose = GlobalOptions.GetOption(SolutionCrawlerOptionsStorage.RemoveDocumentDiagnosticsOnDocumentClose, document.Project.Language);

            if (!removeDiagnosticsOnDocumentClose)
            {
                return;
            }

            RaiseDiagnosticsRemovedForDocument(document.Id, stateSets);
        }

        public async Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
        {
            // Retrigger analysis of newly active document to always get up-to-date diagnostics.
            // Note that we do so regardless of the current background analysis scope,
            // as we might have switched the document _while_ the diagnostic refresh was in progress for
            // all open documents, which can lead to cancellation of diagnostic recomputation task
            // for the newly active document.  This can lead to a race condition where we end up with
            // stale diagnostics for the active document.  We avoid that by always recomputing
            // the diagnostics for the newly active document whenever active document is switched.

            // First reset the document states.
            await TextDocumentResetAsync(document, cancellationToken).ConfigureAwait(false);

            // Trigger syntax analysis.
            await AnalyzeDocumentForKindAsync(document, AnalysisKind.Syntax, cancellationToken).ConfigureAwait(false);

            // Trigger semantic analysis for source documents. Non-source documents do not support semantic analysis.
            if (document is Document)
                await AnalyzeDocumentForKindAsync(document, AnalysisKind.Semantic, cancellationToken).ConfigureAwait(false);
        }

        public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveDocument, GetRemoveLogMessage, documentId, CancellationToken.None))
            {
                var stateSets = _stateManager.GetStateSets(documentId.ProjectId);

                // let other components knows about this event
                ClearCompilationsWithAnalyzersCache();

                var changed = false;
                foreach (var stateSet in stateSets)
                    changed |= stateSet.OnDocumentRemoved(documentId);

                // if there was no diagnostic reported for this document, nothing to clean up
                // this is Perf to reduce raising events unnecessarily.
                if (changed)
                    RaiseDiagnosticsRemovedForDocument(documentId, stateSets);
            }

            return Task.CompletedTask;
        }

        private void RaiseDiagnosticsRemovedForDocument(DocumentId documentId, IEnumerable<StateSet> stateSets)
        {
            // remove all diagnostics for the document
            AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
            {
                foreach (var stateSet in stateSets)
                {
                    // clear all doucment diagnostics
                    RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.Syntax, raiseEvents);
                    RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.Semantic, raiseEvents);
                    RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.NonLocal, raiseEvents);
                }
            });
        }

        public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellation)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveProject, GetRemoveLogMessage, projectId, CancellationToken.None))
            {
                var stateSets = _stateManager.GetStateSets(projectId);

                // let other components knows about this event
                ClearCompilationsWithAnalyzersCache();
                var changed = _stateManager.OnProjectRemoved(stateSets, projectId);

                // if there was no diagnostic reported for this project, nothing to clean up
                // this is Perf to reduce raising events unnecessarily.
                if (changed)
                {
                    // remove all diagnostics for the project
                    AnalyzerService.RaiseBulkDiagnosticsUpdated(raiseEvents =>
                    {
                        foreach (var stateSet in stateSets)
                        {
                            // clear all project diagnostics
                            RaiseDiagnosticsRemoved(projectId, solution: null, stateSet.Analyzer, raiseEvents);
                        }
                    });
                }
            }

            return Task.CompletedTask;
        }

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            // let other components knows about this event
            ClearCompilationsWithAnalyzersCache();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Return list of <see cref="StateSet"/> to be used for full solution analysis.
        /// </summary>
        private ImmutableArray<StateSet> GetStateSetsForFullSolutionAnalysis(ImmutableArray<StateSet> stateSets, Project project)
        {
            // If full analysis is off, remove state that is created from build.
            // this will make sure diagnostics from build (converted from build to live) will never be cleared
            // until next build.
            _ = GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language, out var compilerFullSolutionAnalysisEnabled, out var analyzersFullSolutionAnalysisEnabled);
            if (!compilerFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for compiler diagnostics,
                // so we remove the compiler analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either not for compiler analyzer or those which are for compiler
                // analyzer, but not from build.
                stateSets = stateSets.WhereAsArray(s => !s.Analyzer.IsCompilerAnalyzer() || !s.FromBuild(project.Id));
            }

            if (!analyzersFullSolutionAnalysisEnabled)
            {
                // Full solution analysis is not enabled for analyzer diagnostics,
                // so we remove the analyzer state sets that are from build.
                // We do so by retaining only those state sets that are
                // either for the special compiler/workspace analyzers or those which are for
                // other analyzers, but not from build.
                stateSets = stateSets.WhereAsArray(s => s.Analyzer.IsCompilerAnalyzer() || s.Analyzer.IsWorkspaceDiagnosticAnalyzer() || !s.FromBuild(project.Id));
            }

            // Include only analyzers we want to run for full solution analysis.
            // Analyzers not included here will never be saved because result is unknown.
            return stateSets.WhereAsArray(s => IsCandidateForFullSolutionAnalysis(s.Analyzer, project));
        }

        private bool IsCandidateForFullSolutionAnalysis(DiagnosticAnalyzer analyzer, Project project)
        {
            // PERF: Don't query descriptors for compiler analyzer or workspace load analyzer, always execute them.
            if (analyzer == FileContentLoadAnalyzer.Instance ||
                analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance ||
                analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            if (analyzer.IsBuiltInAnalyzer())
            {
                // always return true for builtin analyzer. we can't use
                // descriptor check since many builtin analyzer always return 
                // hidden descriptor regardless what descriptor it actually
                // return on runtime. they do this so that they can control
                // severity through option page rather than rule set editor.
                // this is special behavior only ide analyzer can do. we hope
                // once we support editorconfig fully, third party can use this
                // ability as well and we can remove this kind special treatment on builtin
                // analyzer.
                return true;
            }

            if (analyzer is DiagnosticSuppressor)
            {
                // Always execute diagnostic suppressors.
                return true;
            }

            // For most of analyzers, the number of diagnostic descriptors is small, so this should be cheap.
            var descriptors = DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(analyzer);
            var analyzerConfigOptions = project.GetAnalyzerConfigOptions();

            return descriptors.Any(static (d, arg) => d.GetEffectiveSeverity(arg.project.CompilationOptions!, arg.analyzerConfigOptions?.AnalyzerOptions, arg.analyzerConfigOptions?.TreeOptions) != ReportDiagnostic.Hidden, (project, analyzerConfigOptions));
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            RaiseProjectDiagnosticsIfNeeded(project, stateSets, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, result);
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> oldResult,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> newResult)
        {
            if (oldResult.Count == 0 && newResult.Count == 0)
            {
                // there is nothing to update
                return;
            }

            AnalyzerService.RaiseBulkDiagnosticsUpdated(async raiseEvents =>
            {
                foreach (var stateSet in stateSets)
                {
                    var analyzer = stateSet.Analyzer;

                    var oldAnalysisResult = GetResultOrEmpty(oldResult, analyzer, project.Id, VersionStamp.Default);
                    var newAnalysisResult = GetResultOrEmpty(newResult, analyzer, project.Id, VersionStamp.Default);

                    // Perf - 4 different cases.
                    // upper 3 cases can be removed and it will still work. but this is hot path so if we can bail out
                    // without any allocations, that's better.
                    if (oldAnalysisResult.IsEmpty && newAnalysisResult.IsEmpty)
                    {
                        // nothing to do
                        continue;
                    }

                    if (!oldAnalysisResult.IsEmpty && newAnalysisResult.IsEmpty)
                    {
                        RoslynDebug.Assert(oldAnalysisResult.DocumentIds != null);

                        // remove old diagnostics
                        RaiseProjectDiagnosticsRemoved(stateSet, oldAnalysisResult.ProjectId, oldAnalysisResult.DocumentIds, handleActiveFile: false, raiseEvents);
                        continue;
                    }

                    if (oldAnalysisResult.IsEmpty && !newAnalysisResult.IsEmpty)
                    {
                        // add new diagnostics
                        await RaiseProjectDiagnosticsCreatedAsync(project, stateSet, oldAnalysisResult, newAnalysisResult, raiseEvents, CancellationToken.None).ConfigureAwait(false);
                        continue;
                    }

                    // both old and new has items in them. update existing items
                    RoslynDebug.Assert(oldAnalysisResult.DocumentIds != null);
                    RoslynDebug.Assert(newAnalysisResult.DocumentIds != null);

                    // first remove ones no longer needed.
                    var documentsToRemove = oldAnalysisResult.DocumentIds.Except(newAnalysisResult.DocumentIds);
                    RaiseProjectDiagnosticsRemoved(stateSet, oldAnalysisResult.ProjectId, documentsToRemove, handleActiveFile: false, raiseEvents);

                    // next update or create new ones
                    await RaiseProjectDiagnosticsCreatedAsync(project, stateSet, oldAnalysisResult, newAnalysisResult, raiseEvents, CancellationToken.None).ConfigureAwait(false);
                }
            });
        }

        private void RaiseDocumentDiagnosticsIfNeeded(TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind, ImmutableArray<DiagnosticData> items)
            => RaiseDocumentDiagnosticsIfNeeded(document, analyzer, kind, ImmutableArray<DiagnosticData>.Empty, items);

        private void RaiseDocumentDiagnosticsIfNeeded(
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind, ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems)
        {
            RaiseDocumentDiagnosticsIfNeeded(document, analyzer, kind, oldItems, newItems, AnalyzerService.RaiseDiagnosticsUpdated, forceUpdate: false);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind,
            DiagnosticAnalysisResult oldResult, DiagnosticAnalysisResult newResult,
            Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            // if our old result is from build and we don't have actual data, don't try micro-optimize and always refresh diagnostics.
            // most of time, we don't actually load or hold the old data in memory from persistent storage due to perf reasons.
            //
            // we need this special behavior for errors from build since unlike live errors, we don't know whether errors
            // from build is for syntax, semantic or others. due to that, we blindly mark them as semantic errors (most common type of errors from build)
            //
            // that can sometime cause issues. for example, if the error turns out to be syntax error (live) then we at the end fail to de-dup.
            // but since this optimization saves us a lot of refresh between live errors analysis we want to disable this only in this condition.
            var forceUpdate = oldResult.FromBuild && oldResult.IsAggregatedForm;

            var oldItems = oldResult.GetDocumentDiagnostics(document.Id, kind);
            var newItems = newResult.GetDocumentDiagnostics(document.Id, kind);

            RaiseDocumentDiagnosticsIfNeeded(document, analyzer, kind, oldItems, newItems, raiseEvents, forceUpdate);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            TextDocument document, DiagnosticAnalyzer analyzer, AnalysisKind kind,
            ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems,
            Action<DiagnosticsUpdatedArgs> raiseEvents,
            bool forceUpdate)
        {
            if (!forceUpdate && oldItems.IsEmpty && newItems.IsEmpty)
            {
                // there is nothing to update
                return;
            }

            RaiseDiagnosticsCreated(document, analyzer, kind, newItems, raiseEvents);
        }

        private async Task RaiseProjectDiagnosticsCreatedAsync(Project project, StateSet stateSet, DiagnosticAnalysisResult oldAnalysisResult, DiagnosticAnalysisResult newAnalysisResult, Action<DiagnosticsUpdatedArgs> raiseEvents, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(newAnalysisResult.DocumentIds != null);

            foreach (var documentId in newAnalysisResult.DocumentIds)
            {
                var document = project.GetTextDocument(documentId);

                // If we couldn't find a normal document, and all features are enabled for source generated documents,
                // attempt to locate a matching source generated document in the project.
                if (document is null
                    && project.Solution.Services.GetService<ISolutionCrawlerOptionsService>()?.EnableDiagnosticsInSourceGeneratedFiles == true)
                {
                    document = await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                }

                if (document == null)
                {
                    // it can happen with build synchronization since, in build case, 
                    // we don't have actual snapshot (we have no idea what sources out of proc build has picked up)
                    // so we might be out of sync.
                    // example of such cases will be changing anything about solution while building is going on.
                    // it can be user explicit actions such as unloading project, deleting a file, but also it can be 
                    // something project system or roslyn workspace does such as populating workspace right after
                    // solution is loaded.
                    continue;
                }

                RaiseDocumentDiagnosticsIfNeeded(document, stateSet.Analyzer, AnalysisKind.NonLocal, oldAnalysisResult, newAnalysisResult, raiseEvents);

                // we don't raise events for active file. it will be taken cared by active file analysis
                if (stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                RaiseDocumentDiagnosticsIfNeeded(document, stateSet.Analyzer, AnalysisKind.Syntax, oldAnalysisResult, newAnalysisResult, raiseEvents);
                RaiseDocumentDiagnosticsIfNeeded(document, stateSet.Analyzer, AnalysisKind.Semantic, oldAnalysisResult, newAnalysisResult, raiseEvents);
            }

            RaiseDiagnosticsCreated(project, stateSet.Analyzer, newAnalysisResult.GetOtherDiagnostics(), raiseEvents);
        }

        private void RaiseProjectDiagnosticsRemoved(StateSet stateSet, ProjectId projectId, IEnumerable<DocumentId> documentIds, bool handleActiveFile, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            foreach (var documentId in documentIds)
            {
                RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.NonLocal, raiseEvents);

                // we don't raise events for active file. it will be taken care of by active file analysis
                if (!handleActiveFile && stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.Syntax, raiseEvents);
                RaiseDiagnosticsRemoved(documentId, solution: null, stateSet.Analyzer, AnalysisKind.Semantic, raiseEvents);
            }

            RaiseDiagnosticsRemoved(projectId, solution: null, stateSet.Analyzer, raiseEvents);
        }
    }
}
