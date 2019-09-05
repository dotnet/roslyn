// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class NormalPriorityProcessor : AbstractPriorityProcessor
                {
                    private const int MaxHighPriorityQueueCache = 29;

                    private readonly AsyncDocumentWorkItemQueue _workItemQueue;
                    private readonly ConcurrentDictionary<DocumentId, IDisposable> _higherPriorityDocumentsNotProcessed;

                    private ProjectId _currentProjectProcessing;
                    private IDisposable _projectCache;

                    // this is only used in ResetState to find out solution has changed
                    // and reset some states such as logging some telemetry or
                    // priorities active,visible, opened files and etc
                    private Solution _lastSolution = null;

                    // whether this processor is running or not
                    private Task _running;

                    public NormalPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken)
                        : base(listener, processor, lazyAnalyzers, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        _running = Task.CompletedTask;
                        _workItemQueue = new AsyncDocumentWorkItemQueue(processor._registration.ProgressReporter, processor._registration.Workspace);
                        _higherPriorityDocumentsNotProcessed = new ConcurrentDictionary<DocumentId, IDisposable>(concurrencyLevel: 2, capacity: 20);

                        _currentProjectProcessing = default;

                        Start();
                    }

                    public void Enqueue(WorkItem item)
                    {
                        Contract.ThrowIfFalse(item.DocumentId != null, "can only enqueue a document work item");

                        UpdateLastAccessTime();

                        var added = _workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_DocumentWorker_Enqueue, s_enqueueLogger, Environment.TickCount, item.DocumentId, !added);

                        CheckHigherPriorityDocument(item);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(
                            Processor._logAggregator, item.Language, item.DocumentId, item.InvocationReasons, item.IsLowPriority, item.ActiveMember, added);
                    }

                    private void CheckHigherPriorityDocument(WorkItem item)
                    {
                        if (!item.InvocationReasons.Contains(PredefinedInvocationReasons.HighPriority))
                        {
                            return;
                        }

                        AddHigherPriorityDocument(item.DocumentId);
                    }

                    private void AddHigherPriorityDocument(DocumentId id)
                    {
                        var cache = GetHighPriorityQueueProjectCache(id);
                        if (!_higherPriorityDocumentsNotProcessed.TryAdd(id, cache))
                        {
                            // we already have the document in the queue.
                            cache?.Dispose();
                        }

                        SolutionCrawlerLogger.LogHigherPriority(Processor._logAggregator, id.Id);
                    }

                    private IDisposable GetHighPriorityQueueProjectCache(DocumentId id)
                    {
                        // NOTE: we have one potential issue where we can cache a lot of stuff in memory 
                        //       since we will cache all high prioirty work's projects in memory until they are processed. 
                        //
                        //       To mitigate that, we will turn off cache if we have too many items in high priority queue
                        //       this shouldn't affect active file since we always enable active file cache from background compiler.

                        return _higherPriorityDocumentsNotProcessed.Count <= MaxHighPriorityQueueCache ? Processor.EnableCaching(id.ProjectId) : null;
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        if (!_workItemQueue.HasAnyWork)
                        {
                            DisposeProjectCache();
                        }

                        return _workItemQueue.WaitAsync(cancellationToken);
                    }

                    public Task Running => _running;
                    public int WorkItemCount => _workItemQueue.WorkItemCount;
                    public bool HasAnyWork => _workItemQueue.HasAnyWork;

                    protected override async Task ExecuteAsync()
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var source = new TaskCompletionSource<object>();
                        try
                        {
                            // mark it as running
                            _running = source.Task;

                            await WaitForHigherPriorityOperationsAsync().ConfigureAwait(false);

                            // okay, there must be at least one item in the map
                            await ResetStatesAsync().ConfigureAwait(false);

                            if (await TryProcessOneHigherPriorityDocumentAsync().ConfigureAwait(false))
                            {
                                // successfully processed a high priority document.
                                return;
                            }
                            // process one of documents remaining
                            if (!_workItemQueue.TryTakeAnyWork(
                                _currentProjectProcessing, Processor.DependencyGraph, Processor.DiagnosticAnalyzerService,
                                out var workItem, out var documentCancellation))
                            {
                                return;
                            }

                            // check whether we have been shutdown
                            if (CancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            // check whether we have moved to new project
                            SetProjectProcessing(workItem.ProjectId);

                            // process the new document
                            await ProcessDocumentAsync(Analyzers, workItem, documentCancellation).ConfigureAwait(false);
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                        finally
                        {
                            // mark it as done running
                            source.SetResult(null);
                        }
                    }

                    protected override Task HigherQueueOperationTask
                    {
                        get
                        {
                            return Processor._highPriorityProcessor.Running;
                        }
                    }

                    protected override bool HigherQueueHasWorkItem
                    {
                        get
                        {
                            return Processor._highPriorityProcessor.HasAnyWork;
                        }
                    }

                    protected override void PauseOnGlobalOperation()
                    {
                        base.PauseOnGlobalOperation();

                        _workItemQueue.RequestCancellationOnRunningTasks();
                    }

                    private void SetProjectProcessing(ProjectId currentProject)
                    {
                        EnableProjectCacheIfNecessary(currentProject);

                        _currentProjectProcessing = currentProject;
                    }

                    private void EnableProjectCacheIfNecessary(ProjectId currentProject)
                    {
                        if (_projectCache != null && currentProject == _currentProjectProcessing)
                        {
                            return;
                        }

                        DisposeProjectCache();

                        _projectCache = Processor.EnableCaching(currentProject);
                    }

                    private static void DisposeProjectCache(IDisposable projectCache)
                    {
                        projectCache?.Dispose();
                    }

                    private void DisposeProjectCache()
                    {
                        DisposeProjectCache(_projectCache);
                        _projectCache = null;
                    }

                    private IEnumerable<DocumentId> GetPrioritizedPendingDocuments()
                    {
                        if (Processor._documentTracker != null)
                        {
                            // First the active document
                            var activeDocumentId = Processor._documentTracker.TryGetActiveDocument();
                            if (activeDocumentId != null)
                            {
                                yield return activeDocumentId;
                            }

                            // Now any visible documents
                            foreach (var visibleDocumentId in Processor._documentTracker.GetVisibleDocuments())
                            {
                                yield return visibleDocumentId;
                            }
                        }

                        // Any other high priority documents
                        foreach (var documentId in _higherPriorityDocumentsNotProcessed.Keys)
                        {
                            yield return documentId;
                        }
                    }

                    private async Task<bool> TryProcessOneHigherPriorityDocumentAsync()
                    {
                        try
                        {
                            foreach (var documentId in GetPrioritizedPendingDocuments())
                            {
                                if (CancellationToken.IsCancellationRequested)
                                {
                                    return true;
                                }

                                // this is a best effort algorithm with some shortcomings.
                                //
                                // the most obvious issue is if there is a new work item (without a solution change - but very unlikely) 
                                // for a opened document we already processed, the work item will be treated as a regular one rather than higher priority one
                                // (opened document)
                                // see whether we have work item for the document
                                if (!_workItemQueue.TryTake(documentId, out var workItem, out var documentCancellation))
                                {
                                    RemoveHigherPriorityDocument(documentId);
                                    continue;
                                }

                                // okay now we have work to do
                                await ProcessDocumentAsync(Analyzers, workItem, documentCancellation).ConfigureAwait(false);

                                RemoveHigherPriorityDocument(documentId);
                                return true;
                            }

                            return false;
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    private void RemoveHigherPriorityDocument(DocumentId documentId)
                    {
                        // remove opened document processed
                        if (_higherPriorityDocumentsNotProcessed.TryRemove(documentId, out var projectCache))
                        {
                            DisposeProjectCache(projectCache);
                        }
                    }

                    private async Task ProcessDocumentAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationToken cancellationToken)
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var processedEverything = false;
                        var documentId = workItem.DocumentId;

                        // we should always use solution snapshot after workitem is removed from the queue.
                        // otherwise, we can have a race such as below.
                        //
                        // 1.solution crawler picked up a solution
                        // 2.before processing the solution, an workitem got changed
                        // 3.and then the work item got picked up from the queue
                        // 4.and use the work item with the solution that got picked up in step 1
                        // 
                        // step 2 is happening because solution has changed, but step 4 used old solution from step 1
                        // that doesn't have effects of the solution changes.
                        // 
                        // solution crawler must remove the work item from the queue first and then pick up the soluton,
                        // so that the queue gets new work item if there is any solution changes after the work item is removed
                        // from the queue
                        // 
                        // using later version of solution is always fine since, as long as there is new work item in the queue,
                        // solution crawler will eventually call the last workitem with the lastest solution
                        // making everything to catch up
                        var solution = Processor.CurrentSolution;
                        try
                        {
                            using (Logger.LogBlock(FunctionId.WorkCoordinator_ProcessDocumentAsync, w => w.ToString(), workItem, cancellationToken))
                            {
                                var document = solution.GetDocument(documentId);

                                if (document != null)
                                {
                                    // if we are called because a document is opened, we invalidate the document so that
                                    // it can be re-analyzed. otherwise, since newly opened document has same version as before
                                    // analyzer will simply return same data back
                                    if (workItem.MustRefresh && !workItem.IsRetry)
                                    {
                                        var isOpen = document.IsOpen();

                                        await ProcessOpenDocumentIfNeeded(analyzers, workItem, document, isOpen, cancellationToken).ConfigureAwait(false);
                                        await ProcessCloseDocumentIfNeeded(analyzers, workItem, document, isOpen, cancellationToken).ConfigureAwait(false);
                                    }

                                    // check whether we are having special reanalyze request
                                    await ProcessReanalyzeDocumentAsync(workItem, document, cancellationToken).ConfigureAwait(false);

                                    await Processor.ProcessDocumentAnalyzersAsync(document, analyzers, workItem, cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    SolutionCrawlerLogger.LogProcessDocumentNotExist(Processor._logAggregator);

                                    RemoveDocument(documentId);
                                }

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    processedEverything = true;
                                }
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                        finally
                        {
                            // we got cancelled in the middle of processing the document.
                            // let's make sure newly enqueued work item has all the flag needed.
                            // Avoid retry attempts after cancellation is requested, since work will not be processed
                            // after that point.
                            if (!processedEverything && !CancellationToken.IsCancellationRequested)
                            {
                                _workItemQueue.AddOrReplace(workItem.Retry(Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessDocument(Processor._logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.MarkWorkItemDoneFor(workItem.DocumentId);
                        }
                    }

                    private async Task ProcessOpenDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (!isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentOpened))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessOpenDocument(Processor._logAggregator, document.Id.Id);

                        await Processor.RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentOpenAsync(d, c), cancellationToken).ConfigureAwait(false);
                    }

                    private async Task ProcessCloseDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentClosed))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessCloseDocument(Processor._logAggregator, document.Id.Id);

                        await Processor.RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentCloseAsync(d, c), cancellationToken).ConfigureAwait(false);
                    }

                    private async Task ProcessReanalyzeDocumentAsync(WorkItem workItem, Document document, CancellationToken cancellationToken)
                    {
                        try
                        {
#if DEBUG
                            Debug.Assert(!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.Reanalyze) || workItem.Analyzers.Count > 0);
#endif

                            // no-reanalyze request or we already have a request to re-analyze every thing
                            if (workItem.MustRefresh || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.Reanalyze))
                            {
                                return;
                            }

                            // First reset the document state in analyzers.
                            var reanalyzers = workItem.Analyzers.ToImmutableArray();
                            await Processor.RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.DocumentResetAsync(d, c), cancellationToken).ConfigureAwait(false);

                            // no request to re-run syntax change analysis. run it here
                            var reasons = workItem.InvocationReasons;
                            if (!reasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                            {
                                await Processor.RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.AnalyzeSyntaxAsync(d, reasons, c), cancellationToken).ConfigureAwait(false);
                            }

                            // no request to re-run semantic change analysis. run it here
                            if (!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                            {
                                await Processor.RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.AnalyzeDocumentAsync(d, null, reasons, c), cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    private void RemoveDocument(DocumentId documentId)
                    {
                        RemoveDocument(Analyzers, documentId);
                    }

                    private static void RemoveDocument(ImmutableArray<IIncrementalAnalyzer> analyzers, DocumentId documentId)
                    {
                        foreach (var analyzer in analyzers)
                        {
                            analyzer.RemoveDocument(documentId);
                        }
                    }

                    private async Task ResetStatesAsync()
                    {
                        try
                        {
                            if (!IsSolutionChanged())
                            {
                                return;
                            }

                            await Processor.RunAnalyzersAsync(Analyzers, Processor.CurrentSolution, (a, s, c) => a.NewSolutionSnapshotAsync(s, c), CancellationToken).ConfigureAwait(false);

                            foreach (var id in Processor.GetOpenDocumentIds())
                            {
                                AddHigherPriorityDocument(id);
                            }

                            SolutionCrawlerLogger.LogResetStates(Processor._logAggregator);
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }

                        bool IsSolutionChanged()
                        {
                            var currentSolution = Processor.CurrentSolution;
                            var oldSolution = _lastSolution;

                            if (currentSolution == oldSolution)
                            {
                                return false;
                            }

                            _lastSolution = currentSolution;

                            ResetLogAggregatorIfNeeded(currentSolution, oldSolution);

                            return true;
                        }

                        void ResetLogAggregatorIfNeeded(Solution currentSolution, Solution oldSolution)
                        {
                            if (currentSolution == null || oldSolution == null ||
                                currentSolution.Id == oldSolution.Id)
                            {
                                // we log aggregated info when solution is changed such as
                                // new solution is opened or solution is closed
                                return;
                            }

                            // this log things like how many time we analyzed active files, how many times other files are analyzed,
                            // avg time to analyze files, how many solution snapshot got analyzed and etc.
                            // all accumultation is done in VS side and we only send statistics to VS telemetry otherwise, it is too much
                            // data to send
                            SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(
                                Processor._registration.CorrelationId, oldSolution, Processor._logAggregator, Analyzers);

                            Processor.ResetLogAggregator();
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();

                        SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(Processor._registration.CorrelationId, Processor.CurrentSolution, Processor._logAggregator, Analyzers);

                        _workItemQueue.Dispose();

                        if (_projectCache != null)
                        {
                            _projectCache.Dispose();
                            _projectCache = null;
                        }
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
                    {
                        foreach (var item in items)
                        {
                            ProcessDocumentAsync(analyzers, item, CancellationToken.None).Wait();
                        }
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly()
                    {
                        // this shouldn't happen. would like to get some diagnostic
                        while (_workItemQueue.HasAnyWork)
                        {
                            Environment.FailFast("How?");
                        }
                    }
                }
            }
        }
    }
}
