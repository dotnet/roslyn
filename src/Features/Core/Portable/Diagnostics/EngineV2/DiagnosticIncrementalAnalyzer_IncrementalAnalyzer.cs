// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    // TODO: make it to use cache
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public override Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            return AnalyzeDocumentForKindAsync(document, AnalysisKind.Syntax, cancellationToken);
        }

        public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
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
                var analyzerDriver = await _compilationManager.GetAnalyzerDriverAsync(document.Project, stateSets, cancellationToken).ConfigureAwait(false);

                foreach (var stateSet in stateSets)
                {
                    var analyzer = stateSet.Analyzer;

                    var result = await _executor.GetDocumentAnalysisDataAsync(analyzerDriver, document, stateSet, kind, cancellationToken).ConfigureAwait(false);
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

        public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            try
            {
                var stateSets = _stateManager.GetOrUpdateStateSets(project);

                // get analyzers that are not suppressed.
                // REVIEW: IsAnalyzerSuppressed call seems can be quite expensive in certain condition. is there any other way to do this?
                var activeAnalyzers = stateSets.Select(s => s.Analyzer).Where(a => !Owner.IsAnalyzerSuppressed(a, project)).ToImmutableArrayOrEmpty();

                // get driver only with active analyzers.
                var includeSuppressedDiagnostics = true;
                var analyzerDriver = await _compilationManager.CreateAnalyzerDriverAsync(project, activeAnalyzers, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);

                var result = await _executor.GetProjectAnalysisDataAsync(analyzerDriver, project, stateSets, cancellationToken).ConfigureAwait(false);
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

        public override Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            // let other component knows about this event
            _compilationManager.OnDocumentOpened();

            // here we dont need to raise any event, it will be taken cared by analyze methods.
            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            var stateSets = _stateManager.GetStateSets(document.Project);

            // let other components knows about this event
            _compilationManager.OnDocumentClosed();
            var changed = _stateManager.OnDocumentClosed(stateSets, document.Id);

            // replace diagnostics from project state over active file state
            RaiseLocalDocumentEventsFromProjectOverActiveFile(stateSets, document, changed);

            return SpecializedTasks.EmptyTask;
        }

        public override Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            var stateSets = _stateManager.GetStateSets(document.Project);

            // let other components knows about this event
            _compilationManager.OnDocumentReset();
            var changed = _stateManager.OnDocumentReset(stateSets, document.Id);

            // replace diagnostics from project state over active file state
            RaiseLocalDocumentEventsFromProjectOverActiveFile(stateSets, document, changed);

            return SpecializedTasks.EmptyTask;
        }

        public override void RemoveDocument(DocumentId documentId)
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

        public override void RemoveProject(ProjectId projectId)
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

        private static ImmutableArray<DiagnosticData> GetResult(AnalysisResult result, AnalysisKind kind, DocumentId id)
        {
            switch (kind)
            {
                case AnalysisKind.Syntax:
                    return result.GetResultOrEmpty(result.SyntaxLocals, id);
                case AnalysisKind.Semantic:
                    return result.GetResultOrEmpty(result.SemanticLocals, id);
                case AnalysisKind.NonLocal:
                    return result.GetResultOrEmpty(result.NonLocals, id);
                default:
                    return Contract.FailWithReturn<ImmutableArray<DiagnosticData>>("shouldn't reach here");
            }
        }

        private void RaiseLocalDocumentEventsFromProjectOverActiveFile(IEnumerable<StateSet> stateSets, Document document, bool activeFileDiagnosticExist)
        {
            // PERF: activeFileDiagnosticExist is perf optimization to reduce raising events unnecessarily.

            //  this removes diagnostic reported by active file and replace those with ones from project.
            Owner.RaiseBulkDiagnosticsUpdated(async raiseEvents =>
            {
                // this basically means always load data
                var avoidLoadingData = false;

                foreach (var stateSet in stateSets)
                {
                    // get project state
                    var state = stateSet.GetProjectState(document.Project.Id);

                    // this is perf optimization to reduce events;
                    if (!activeFileDiagnosticExist && state.IsEmpty(document.Id))
                    {
                        // there is nothing reported before. we don't need to do anything.
                        continue;
                    }

                    // no cancellation since event can't be cancelled.
                    // now get diagnostic information from project
                    var result = await state.GetAnalysisDataAsync(document, avoidLoadingData, CancellationToken.None).ConfigureAwait(false);
                    if (result.IsAggregatedForm)
                    {
                        // something made loading data failed.
                        // clear all existing diagnostics
                        RaiseDiagnosticsRemoved(document.Id, document.Project.Solution, stateSet, AnalysisKind.Syntax, raiseEvents);
                        RaiseDiagnosticsRemoved(document.Id, document.Project.Solution, stateSet, AnalysisKind.Semantic, raiseEvents);
                        continue;
                    }

                    // we have data, do actual event raise that will replace diagnostics from active file
                    var syntaxItems = GetResult(result, AnalysisKind.Syntax, document.Id);
                    RaiseDiagnosticsCreated(document, stateSet, AnalysisKind.Syntax, syntaxItems, raiseEvents);

                    var semanticItems = GetResult(result, AnalysisKind.Semantic, document.Id);
                    RaiseDiagnosticsCreated(document, stateSet, AnalysisKind.Semantic, semanticItems, raiseEvents);
                }
            });
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

                    var oldAnalysisResult = ImmutableDictionary.GetValueOrDefault(oldResult, analyzer);
                    var newAnalysisResult = ImmutableDictionary.GetValueOrDefault(newResult, analyzer);

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
                Contract.ThrowIfNull(document);

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
