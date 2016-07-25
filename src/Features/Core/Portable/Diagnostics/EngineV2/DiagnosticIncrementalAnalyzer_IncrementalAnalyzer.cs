// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public override Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return AnalyzeDocumentForKindAsync(document, AnalysisKind.Syntax, cancellationToken);
        }

        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
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
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            try
            {
                var stateSets = GetStateSetsForFullSolutionAnalysis(_stateManager.GetOrUpdateStateSets(project), project);

                // PERF: get analyzers that are not suppressed.
                // this is perf optimization. we cache these result since we know the result. (no diagnostics)
                // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                var activeAnalyzers = stateSets
                                        .Select(s => s.Analyzer)
                                        .Where(a => !Owner.IsAnalyzerSuppressed(a, project))
                                        .ToImmutableArrayOrEmpty();

                // get driver only with active analyzers.
                var includeSuppressedDiagnostics = true;
                var analyzerDriverOpt = await _compilationManager.CreateAnalyzerDriverAsync(project, activeAnalyzers, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var ignoreFullAnalysisOptions = false;
                var result = await _executor.GetProjectAnalysisDataAsync(analyzerDriverOpt, project, stateSets, ignoreFullAnalysisOptions, cancellationToken).ConfigureAwait(false);
                if (result.FromCache)
                {
                    RaiseProjectDiagnosticsIfNeeded(project, stateSets, result.Result);
                    return;
                }

                // no cancellation after this point.
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetProjectState(project.Id);
                    await state.SaveAsync(project, result.GetResult(stateSet.Analyzer)).ConfigureAwait(false);
                }

                RaiseProjectDiagnosticsIfNeeded(project, stateSets, result.OldResult, result.Result);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public override async Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentOpen, GetOpenLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other component knows about this event
                _compilationManager.OnDocumentOpened();
                await _stateManager.OnDocumentOpenedAsync(stateSets, document).ConfigureAwait(false);
            }
        }

        public override async Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentClose, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                _compilationManager.OnDocumentClosed();
                await _stateManager.OnDocumentClosedAsync(stateSets, document).ConfigureAwait(false);
            }
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Diagnostics_DocumentReset, GetResetLogMessage, document, cancellationToken))
            {
                var stateSets = _stateManager.GetStateSets(document.Project);

                // let other components knows about this event
                _compilationManager.OnDocumentReset();
                _stateManager.OnDocumentReset(stateSets, document);
            }

            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
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

        public override void RemoveProject(ProjectId projectId)
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

        public override Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            // let other components knows about this event
            _compilationManager.OnNewSolution();

            return SpecializedTasks.EmptyTask;
        }

        private static bool AnalysisEnabled(Document document)
        {
            // change it to check active file (or visible files), not open files if active file tracking is enabled.
            // otherwise, use open file.
            return document.IsOpen();
        }

        private IEnumerable<StateSet> GetStateSetsForFullSolutionAnalysis(IEnumerable<StateSet> stateSets, Project project)
        {
            // Get stateSets that should be run for full analysis

            // if full analysis is off, remove state that is from build.
            // this will make sure diagnostics (converted from build to live) from build will never be cleared
            // until next build.
            if (!ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(project))
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
            return stateSets.Where(s => ShouldRunForFullProject(s.Analyzer, project));
        }

        private bool ShouldRunForFullProject(DiagnosticAnalyzer analyzer, Project project)
        {
            // PERF: Don't query descriptors for compiler analyzer, always execute it.
            if (HostAnalyzerManager.IsCompilerDiagnosticAnalyzer(project.Language, analyzer))
            {
                return true;
            }

            // most of analyzers, number of descriptor is quite small, so this should be cheap.
            return Owner.GetDiagnosticDescriptors(analyzer).Any(d => GetEffectiveSeverity(d, project.CompilationOptions) != ReportDiagnostic.Hidden);
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> result)
        {
            RaiseProjectDiagnosticsIfNeeded(project, stateSets, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty, result);
        }

        private void RaiseProjectDiagnosticsIfNeeded(
            Project project,
            IEnumerable<StateSet> stateSets,
            ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> oldResult,
            ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> newResult)
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
            RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, oldItems, newItems, Owner.RaiseDiagnosticsUpdated);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            Document document, StateSet stateSet, AnalysisKind kind,
            AnalysisResult oldResult, AnalysisResult newResult,
            Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            var oldItems = GetResult(oldResult, kind, document.Id);
            var newItems = GetResult(newResult, kind, document.Id);

            RaiseDocumentDiagnosticsIfNeeded(document, stateSet, kind, oldItems, newItems, raiseEvents);
        }

        private void RaiseDocumentDiagnosticsIfNeeded(
            Document document, StateSet stateSet, AnalysisKind kind,
            ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems,
            Action<DiagnosticsUpdatedArgs> raiseEvents)
        {
            if (oldItems.IsEmpty && newItems.IsEmpty)
            {
                // there is nothing to update
                return;
            }

            RaiseDiagnosticsCreated(document, stateSet, kind, newItems, raiseEvents);
        }

        private void RaiseProjectDiagnosticsCreated(Project project, StateSet stateSet, AnalysisResult oldAnalysisResult, AnalysisResult newAnalysisResult, Action<DiagnosticsUpdatedArgs> raiseEvents)
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
                    // it can be user explict actions such as unloading project, deleting a file, but also it can be 
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
    }
}
