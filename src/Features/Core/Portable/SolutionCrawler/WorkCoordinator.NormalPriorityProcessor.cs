// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Versions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class NormalPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    private const int MaxHighPriorityQueueCache = 29;

                    private readonly AsyncDocumentWorkItemQueue _workItemQueue;

                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> _lazyAnalyzers;
                    private readonly ConcurrentDictionary<DocumentId, IDisposable> _higherPriorityDocumentsNotProcessed;

                    private readonly HashSet<ProjectId> _currentSnapshotVersionTrackingSet;

                    private ProjectId _currentProjectProcessing;
                    private Solution _processingSolution;
                    private IDisposable _projectCache;

                    // whether this processor is running or not
                    private Task _running;

                    public NormalPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, processor, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        _lazyAnalyzers = lazyAnalyzers;

                        _running = SpecializedTasks.EmptyTask;
                        _workItemQueue = new AsyncDocumentWorkItemQueue(processor._registration.ProgressReporter);
                        _higherPriorityDocumentsNotProcessed = new ConcurrentDictionary<DocumentId, IDisposable>(concurrencyLevel: 2, capacity: 20);

                        _currentProjectProcessing = default(ProjectId);
                        _processingSolution = null;

                        _currentSnapshotVersionTrackingSet = new HashSet<ProjectId>();

                        Start();
                    }

                    internal ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return _lazyAnalyzers.Value;
                        }
                    }

                    public void Enqueue(WorkItem item)
                    {
                        Contract.ThrowIfFalse(item.DocumentId != null, "can only enqueue a document work item");

                        this.UpdateLastAccessTime();

                        var added = _workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_DocumentWorker_Enqueue, s_enqueueLogger, Environment.TickCount, item.DocumentId, !added);

                        CheckHigherPriorityDocument(item);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(
                            this.Processor._logAggregator, item.Language, item.DocumentId, item.InvocationReasons, item.IsLowPriority, item.ActiveMember, added);
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

                        SolutionCrawlerLogger.LogHigherPriority(this.Processor._logAggregator, id.Id);
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

                    public Task Running
                    {
                        get
                        {
                            return _running;
                        }
                    }

                    public bool HasAnyWork
                    {
                        get
                        {
                            return _workItemQueue.HasAnyWork;
                        }
                    }

                    protected override async Task ExecuteAsync()
                    {
                        if (this.CancellationToken.IsCancellationRequested)
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
                            var documentCancellation = default(CancellationTokenSource);
                            WorkItem workItem;
                            if (!_workItemQueue.TryTakeAnyWork(_currentProjectProcessing, this.Processor.DependencyGraph, out workItem, out documentCancellation))
                            {
                                return;
                            }

                            // check whether we have been shutdown
                            if (this.CancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            // check whether we have moved to new project
                            SetProjectProcessing(workItem.ProjectId);

                            // process the new document
                            await ProcessDocumentAsync(this.Analyzers, workItem, documentCancellation).ConfigureAwait(false);
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
                            return this.Processor._highPriorityProcessor.Running;
                        }
                    }

                    protected override bool HigherQueueHasWorkItem
                    {
                        get
                        {
                            return this.Processor._highPriorityProcessor.HasAnyWork;
                        }
                    }

                    protected override void PauseOnGlobalOperation()
                    {
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
                        if (this.Processor._documentTracker != null)
                        {
                            // First the active document
                            var activeDocumentId = this.Processor._documentTracker.GetActiveDocument();
                            if (activeDocumentId != null && _higherPriorityDocumentsNotProcessed.ContainsKey(activeDocumentId))
                            {
                                yield return activeDocumentId;
                            }

                            // Now any visible documents
                            foreach (var visibleDocumentId in this.Processor._documentTracker.GetVisibleDocuments())
                            {
                                if (_higherPriorityDocumentsNotProcessed.ContainsKey(visibleDocumentId))
                                {
                                    yield return visibleDocumentId;
                                }
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
                            // this is a best effort algorithm with some shortcomings.
                            //
                            // the most obvious issue is if there is a new work item (without a solution change - but very unlikely) 
                            // for a opened document we already processed, the work item will be treated as a regular one rather than higher priority one
                            // (opened document)
                            CancellationTokenSource documentCancellation;
                            foreach (var documentId in this.GetPrioritizedPendingDocuments())
                            {
                                if (this.CancellationToken.IsCancellationRequested)
                                {
                                    return true;
                                }

                                // see whether we have work item for the document
                                WorkItem workItem;
                                if (!_workItemQueue.TryTake(documentId, out workItem, out documentCancellation))
                                {
                                    RemoveHigherPriorityDocument(documentId);
                                    continue;
                                }

                                // okay now we have work to do
                                await ProcessDocumentAsync(this.Analyzers, workItem, documentCancellation).ConfigureAwait(false);

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
                        IDisposable projectCache;
                        if (_higherPriorityDocumentsNotProcessed.TryRemove(documentId, out projectCache))
                        {
                            DisposeProjectCache(projectCache);
                        }
                    }

                    private async Task ProcessDocumentAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationTokenSource source)
                    {
                        if (this.CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var processedEverything = false;
                        var documentId = workItem.DocumentId;

                        try
                        {
                            using (Logger.LogBlock(FunctionId.WorkCoordinator_ProcessDocumentAsync, source.Token))
                            {
                                var cancellationToken = source.Token;
                                var document = _processingSolution.GetDocument(documentId);
                                if (document != null)
                                {
                                    await TrackSemanticVersionsAsync(document, workItem, cancellationToken).ConfigureAwait(false);

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

                                    await ProcessDocumentAnalyzersAsync(document, analyzers, workItem, cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    SolutionCrawlerLogger.LogProcessDocumentNotExist(this.Processor._logAggregator);

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
                            if (!processedEverything)
                            {
                                _workItemQueue.AddOrReplace(workItem.Retry(this.Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessDocument(this.Processor._logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.RemoveCancellationSource(workItem.DocumentId);
                        }
                    }

                    private async Task TrackSemanticVersionsAsync(Document document, WorkItem workItem, CancellationToken cancellationToken)
                    {
                        if (workItem.IsRetry ||
                            workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentAdded) ||
                            !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                        {
                            return;
                        }

                        var service = document.Project.Solution.Workspace.Services.GetService<ISemanticVersionTrackingService>();
                        if (service == null)
                        {
                            return;
                        }

                        // we already reported about this project for same snapshot, don't need to do it again
                        if (_currentSnapshotVersionTrackingSet.Contains(document.Project.Id))
                        {
                            return;
                        }

                        await service.RecordSemanticVersionsAsync(document.Project, cancellationToken).ConfigureAwait(false);

                        // mark this project as already processed.
                        _currentSnapshotVersionTrackingSet.Add(document.Project.Id);
                    }

                    private async Task ProcessOpenDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (!isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentOpened))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessOpenDocument(this.Processor._logAggregator, document.Id.Id);

                        await RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentOpenAsync(d, c), cancellationToken).ConfigureAwait(false);
                    }

                    private async Task ProcessCloseDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentClosed))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessCloseDocument(this.Processor._logAggregator, document.Id.Id);

                        await RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentCloseAsync(d, c), cancellationToken).ConfigureAwait(false);
                    }

                    private async Task ProcessReanalyzeDocumentAsync(WorkItem workItem, Document document, CancellationToken cancellationToken)
                    {
                        try
                        {
#if DEBUG
                            Contract.Requires(!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.Reanalyze) || workItem.Analyzers.Count > 0);
#endif

                            // no-reanalyze request or we already have a request to re-analyze every thing
                            if (workItem.MustRefresh || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.Reanalyze))
                            {
                                return;
                            }

                            // First reset the document state in analyzers.
                            var reanalyzers = workItem.Analyzers.ToImmutableArray();
                            await RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.DocumentResetAsync(d, c), cancellationToken).ConfigureAwait(false);

                            // no request to re-run syntax change analysis. run it here
                            if (!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                            {
                                await RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.AnalyzeSyntaxAsync(d, c), cancellationToken).ConfigureAwait(false);
                            }

                            // no request to re-run semantic change analysis. run it here
                            if (!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                            {
                                await RunAnalyzersAsync(reanalyzers, document, (a, d, c) => a.AnalyzeDocumentAsync(d, null, c), cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    private void RemoveDocument(DocumentId documentId)
                    {
                        RemoveDocument(this.Analyzers, documentId);
                    }

                    private static void RemoveDocument(IEnumerable<IIncrementalAnalyzer> analyzers, DocumentId documentId)
                    {
                        foreach (var analyzer in analyzers)
                        {
                            analyzer.RemoveDocument(documentId);
                        }
                    }

                    private void ResetLogAggregatorIfNeeded(Solution currentSolution)
                    {
                        if (currentSolution == null || _processingSolution == null ||
                            currentSolution.Id == _processingSolution.Id)
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(
                            this.Processor._registration.CorrelationId, _processingSolution, this.Processor._logAggregator, this.Analyzers);

                        this.Processor.ResetLogAggregator();
                    }

                    private async Task ResetStatesAsync()
                    {
                        try
                        {
                            var currentSolution = this.Processor.CurrentSolution;

                            if (currentSolution != _processingSolution)
                            {
                                ResetLogAggregatorIfNeeded(currentSolution);

                                // clear version tracking set we already reported.
                                _currentSnapshotVersionTrackingSet.Clear();

                                _processingSolution = currentSolution;

                                await RunAnalyzersAsync(this.Analyzers, currentSolution, (a, s, c) => a.NewSolutionSnapshotAsync(s, c), this.CancellationToken).ConfigureAwait(false);

                                foreach (var id in this.Processor.GetOpenDocumentIds())
                                {
                                    AddHigherPriorityDocument(id);
                                }

                                SolutionCrawlerLogger.LogResetStates(this.Processor._logAggregator);
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();

                        SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(this.Processor._registration.CorrelationId, _processingSolution, this.Processor._logAggregator, this.Analyzers);

                        _workItemQueue.Dispose();

                        if (_projectCache != null)
                        {
                            _projectCache.Dispose();
                            _projectCache = null;
                        }
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
                    {
                        CancellationTokenSource source = new CancellationTokenSource();

                        _processingSolution = this.Processor.CurrentSolution;
                        foreach (var item in items)
                        {
                            ProcessDocumentAsync(analyzers, item, source).Wait();
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
