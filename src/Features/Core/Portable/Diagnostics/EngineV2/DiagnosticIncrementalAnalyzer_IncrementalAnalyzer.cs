// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return AnalyzeDocumentForKindAsync(document, AnalysisKind.Syntax, cancellationToken);
        }

        public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return AnalyzeDocumentForKindAsync(document, AnalysisKind.Semantic, cancellationToken);
        }

        private async Task AnalyzeDocumentForKindAsync(Document document, AnalysisKind kind, CancellationToken cancellationToken)
        {
            try
            {
                if (!AnalysisEnabled(document))
                {
                    // to reduce allocations, here, we don't clear existing diagnostics since it is dealt by other entry point such as
                    // DocumentReset or DocumentClosed.
                    return;
                }

                var stateSets = _stateManager.GetOrUpdateStateSets(document.Project);
                var analyzerDriverOpt = await _compilationManager.GetAnalyzerDriverAsync(document.Project, stateSets, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analyzer = stateSet.Analyzer;

                    var result = await _executor.GetDocumentAnalysisDataAsync(analyzerDriverOpt, document, stateSet, kind, cancellationToken).ConfigureAwait(false);
                    if (result.FromCache)
                    {
                        RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, result.Items);
                        continue;
                    }

                    // no cancellation after this point.
                    var state = stateSet.GetActiveFileState(document.Id);
                    state.Save(kind, result.ToPersistData());

                    RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, result.OldItems, result.Items);
                }

                var asyncToken = Owner.Listener.BeginAsyncOperation(nameof(AnalyzeDocumentForKindAsync));
                var _ = ReportAnalyzerPerformanceAsync(document, analyzerDriverOpt, cancellationToken).CompletesAsyncOperation(asyncToken);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            // Perf optimization. check whether we want to analyze this project or not.
            if (!Executor.FullAnalysisEnabled(project, forceAnalyzerRun: false))
            {
                return;
            }

            await AnalyzeProjectAsync(project, forceAnalyzerRun: false, cancellationToken).ConfigureAwait(false);
        }

        public Task ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
            => AnalyzeProjectAsync(project, forceAnalyzerRun: true, cancellationToken);

        private async Task AnalyzeProjectAsync(Project project, bool forceAnalyzerRun, CancellationToken cancellationToken)
        {
            try
            {
                var stateSets = GetStateSetsForFullSolutionAnalysis(_stateManager.GetOrUpdateStateSets(project), project).ToList();

                // PERF: get analyzers that are not suppressed and marked as open file only
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                var activeAnalyzers = stateSets
                                        .Select(s => s.Analyzer)
                                        .Where(a => !Owner.IsAnalyzerSuppressed(a, project) &&
                                                    !a.IsOpenFileOnly(project.Solution.Workspace));

                // get driver only with active analyzers.
                var includeSuppressedDiagnostics = true;
                var analyzerDriverOpt = await _compilationManager.CreateAnalyzerDriverAsync(project, activeAnalyzers, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var result = await _executor.GetProjectAnalysisDataAsync(analyzerDriverOpt, project, stateSets, forceAnalyzerRun, cancellationToken).ConfigureAwait(false);
                if (result.FromCache)
                {
                    RaiseProjectDiagnosticsIfNeeded(project, stateSets, result.Result);
                    return;
                }

                var compilation = await GetCompilationAsync(analyzerDriverOpt).ConfigureAwait(false);

                // no cancellation after this point.
                // any analyzer that doesn't have result will be treated as returned empty set
                // which means we will remove those from error list
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetProjectState(project.Id);

                    await state.SaveAsync(project, result.GetResult(stateSet.Analyzer)).ConfigureAwait(false);
                    stateSet.ComputeCompilationEndAnalyzer(project, compilation);
                }

                RaiseProjectDiagnosticsIfNeeded(project, stateSets, result.OldResult, result.Result);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }

            async Task<Compilation> GetCompilationAsync(CompilationWithAnalyzers analyzerDriverOpt)
            {
                // we might not have analyzerDriver even if project supports compilation if we are called with
                // no analyzers. 
                return analyzerDriverOpt?.Compilation ??
                    (project.SupportsCompilation ? await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false) : null);
            }
        }

        public async Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other component knows about this event
                _compilationManager.OnDocumentOpened();
                await _stateManager.OnDocumentOpenedAsync(stateSets, document).ConfigureAwait(false);
            }
        }

        public async Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentClose, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                _compilationManager.OnDocumentClosed();
                await _stateManager.OnDocumentClosedAsync(stateSets, document).ConfigureAwait(false);
            }
        }

        public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                _compilationManager.OnDocumentReset();
                _stateManager.OnDocumentReset(stateSets, document);
            }

            return Task.CompletedTask;
        }

        public void RemoveDocument(DocumentId documentId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveDocument, GetRemoveLogMessage, documentId, CancellationToken.None))
            {
                var stateSets = _stateManager.GetStateSets(documentId.ProjectId);

                // let other components knows about this event
                _compilationManager.OnDocumentRemoved();
                var changed = _stateManager.OnDocumentRemoved(stateSets, documentId);

                // if there was no diagnostic reported for this document, nothing to clean up
                if (!changed)
                {
                    // this is Perf to reduce raising events unnecessarily.
                    return;
                }

                // remove all diagnostics for the document
                Owner.RaiseBulkDiagnosticsUpdated(raiseEvents =>
                {
                    Solution nullSolution = null;
                    foreach (var stateSet in stateSets)
                    {
                        // clear all doucment diagnostics
                        RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.Syntax, raiseEvents);
                        RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.Semantic, raiseEvents);
                        RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.NonLocal, raiseEvents);
                    }
                });
            }
        }

        public void RemoveProject(ProjectId projectId)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_RemoveProject, GetRemoveLogMessage, projectId, CancellationToken.None))
            {
                var stateSets = _stateManager.GetStateSets(projectId);

                // let other components knows about this event
                _compilationManager.OnProjectRemoved();
                var changed = _stateManager.OnProjectRemoved(stateSets, projectId);

                // if there was no diagnostic reported for this project, nothing to clean up
                if (!changed)
                {
                    // this is Perf to reduce raising events unnecessarily.
                    return;
                }

                // remove all diagnostics for the project
                Owner.RaiseBulkDiagnosticsUpdated(raiseEvents =>
                {
                    Solution nullSolution = null;
                    foreach (var stateSet in stateSets)
                    {
                        // clear all project diagnostics
                        RaiseDiagnosticsRemoved(projectId, nullSolution, stateSet, raiseEvents);
                    }
                });
            }
        }

        public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            // let other components knows about this event
            _compilationManager.OnNewSolution();

            return Task.CompletedTask;
        }

        private static bool AnalysisEnabled(Document document)
        {
            // change it to check active file (or visible files), not open files if active file tracking is enabled.
            // otherwise, use open file.
            return document.IsOpen() && document.SupportsDiagnostics();
        }

        private IEnumerable<StateSet> GetStateSetsForFullSolutionAnalysis(IEnumerable<StateSet> stateSets, Project project)
        {
            // Get stateSets that should be run for full analysis

            // if full analysis is off, remove state that is from build.
            // this will make sure diagnostics (converted from build to live) from build will never be cleared
            // until next build.
            if (SolutionCrawlerOptions.GetBackgroundAnalysisScope(project) != BackgroundAnalysisScope.FullSolution)
            {
                stateSets = stateSets.Where(s => !s.FromBuild(project.Id));
            }

            // include all analyzers if option is on
            if (project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.ProcessHiddenDiagnostics))
            {
                return stateSets;
            }

            // Include only one we want to run for full solution analysis.
            // stateSet not included here will never be saved because result is unknown.
            return stateSets.Where(s => IsCandidateForFullSolutionAnalysis(s.Analyzer, project));
        }

        private bool IsCandidateForFullSolutionAnalysis(DiagnosticAnalyzer analyzer, Project project)
        {
            // PERF: Don't query descriptors for compiler analyzer, always execute it.
            if (HostAnalyzerManager.IsCompilerDiagnosticAnalyzer(project.Language, analyzer))
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

            return Owner.HasNonHiddenDescriptor(analyzer, project);
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

            Owner.RaiseBulkDiagnosticsUpdated(raiseEvents =>
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
                        // remove old diagnostics
                        RaiseProjectDiagnosticsRemoved(stateSet, oldAnalysisResult.ProjectId, oldAnalysisResult.DocumentIds, raiseEvents);
                        continue;
                    }

                    if (oldAnalysisResult.IsEmpty && !newAnalysisResult.IsEmpty)
                    {
                        // add new diagnostics
                        RaiseProjectDiagnosticsCreated(project, stateSet, oldAnalysisResult, newAnalysisResult, raiseEvents);
                        continue;
                    }

                    // both old and new has items in them. update existing items

                    // first remove ones no longer needed.
                    var documentsToRemove = oldAnalysisResult.DocumentIds.Except(newAnalysisResult.DocumentIds);
                    RaiseProjectDiagnosticsRemoved(stateSet, oldAnalysisResult.ProjectId, documentsToRemove, raiseEvents);

                    // next update or create new ones
                    RaiseProjectDiagnosticsCreated(project, stateSet, oldAnalysisResult, newAnalysisResult, raiseEvents);
                }
            });
        }

        private void RaiseDocumentDiagnosticsIfNeeded(Document document, StateSet stateSet, AnalysisKind kind, ImmutableArray<DiagnosticData> items)
        {
            RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, ImmutableArray<DiagnosticData>.Empty, items);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            Document document, StateSet stateSet, AnalysisKind kind, ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems)
        {
            RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, oldItems, newItems, Owner.RaiseDiagnosticsUpdated, forceUpdate: false);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            Document document, StateSet stateSet, AnalysisKind kind,
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

            var oldItems = GetResult(oldResult, kind, document.Id);
            var newItems = GetResult(newResult, kind, document.Id);

            RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, oldItems, newItems, raiseEvents, forceUpdate);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            Document document, StateSet stateSet, AnalysisKind kind,
            ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems,
            Action<DiagnosticsUpdatedArgs> raiseEvents,
            bool forceUpdate)
        {
            if (!forceUpdate && oldItems.IsEmpty && newItems.IsEmpty)
            {
                // there is nothing to update
                return;
            }

            RaiseDiagnosticsCreated(document, stateSet, kind, newItems, raiseEvents);
        }

        private void RaiseProjectDiagnosticsCreated(Project project, StateSet stateSet, DiagnosticAnalysisResult oldAnalysisResult, DiagnosticAnalysisResult newAnalysisResult, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            foreach (var documentId in newAnalysisResult.DocumentIds)
            {
                var document = project.GetDocument(documentId);
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

                RaiseDocumentDiagnosticsIfNeeded(document, stateSet, AnalysisKind.NonLocal, oldAnalysisResult, newAnalysisResult, raiseEvents);

                // we don't raise events for active file. it will be taken cared by active file analysis
                if (stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                RaiseDocumentDiagnosticsIfNeeded(document, stateSet, AnalysisKind.Syntax, oldAnalysisResult, newAnalysisResult, raiseEvents);
                RaiseDocumentDiagnosticsIfNeeded(document, stateSet, AnalysisKind.Semantic, oldAnalysisResult, newAnalysisResult, raiseEvents);
            }

            RaiseDiagnosticsCreated(project, stateSet, newAnalysisResult.Others, raiseEvents);
        }

        private void RaiseProjectDiagnosticsRemoved(StateSet stateSet, ProjectId projectId, IEnumerable<DocumentId> documentIds, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            var handleActiveFile = false;
            RaiseProjectDiagnosticsRemoved(stateSet, projectId, documentIds, handleActiveFile, raiseEvents);
        }

        private void RaiseProjectDiagnosticsRemoved(StateSet stateSet, ProjectId projectId, IEnumerable<DocumentId> documentIds, bool handleActiveFile, Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            Solution nullSolution = null;
            foreach (var documentId in documentIds)
            {
                RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.NonLocal, raiseEvents);

                // we don't raise events for active file. it will be taken cared by active file analysis
                if (!handleActiveFile && stateSet.IsActiveFile(documentId))
                {
                    continue;
                }

                RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.Syntax, raiseEvents);
                RaiseDiagnosticsRemoved(documentId, nullSolution, stateSet, AnalysisKind.Semantic, raiseEvents);
            }

            RaiseDiagnosticsRemoved(projectId, nullSolution, stateSet, raiseEvents);
        }

        private async Task ReportAnalyzerPerformanceAsync(Document document, CompilationWithAnalyzers analyzerDriverOpt, CancellationToken cancellationToken)
        {
            try
            {
                if (analyzerDriverOpt == null)
                {
                    return;
                }

                var client = await document.Project.Solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    // no remote support
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                using var pooledObject = SharedPools.Default<Dictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>>().GetPooledObject();

                var containsData = false;
                foreach (var analyzer in analyzerDriverOpt.Analyzers)
                {
                    var telemetryInfo = await analyzerDriverOpt.GetAnalyzerTelemetryInfoAsync(analyzer, cancellationToken).ConfigureAwait(false);
                    if (!containsData && telemetryInfo.ExecutionTime.Ticks > 0)
                    {
                        // this is unfortunate tweak due to how GetAnalyzerTelemetryInfoAsync works when analyzers are asked
                        // one by one rather than in bulk.
                        containsData = true;
                    }

                    pooledObject.Object.Add(analyzer, telemetryInfo);
                }

                if (!containsData)
                {
                    // looks like there is no new data from driver. skip reporting.
                    return;
                }

                await client.TryRunCodeAnalysisRemoteAsync(
                    nameof(IRemoteDiagnosticAnalyzerService.ReportAnalyzerPerformance),
                    new object[] { pooledObject.Object.ToAnalyzerPerformanceInfo(Owner), /* unit count */ 1 },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceled(ex))
            {
                // this is fire and forget method
            }
        }
    }
}
