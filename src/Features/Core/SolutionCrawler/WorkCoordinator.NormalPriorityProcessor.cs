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
    internal sealed partial class WorkCoordinatorRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class NormalPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    private readonly AsyncDocumentWorkItemQueue workItemQueue;

                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers;
                    private readonly ConcurrentDictionary<DocumentId, bool> higherPriorityDocumentsNotProcessed;

                    private readonly HashSet<ProjectId> currentSnapshotVersionTrackingSet;
                    
                    private ProjectId currentProjectProcessing;
                    private Solution processingSolution;
                    private IDisposable projectCache;

                    // whether this processor is running or not
                    private Task running;

                    public NormalPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, processor, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.lazyAnalyzers = lazyAnalyzers;

                        this.running = SpecializedTasks.EmptyTask;
                        this.workItemQueue = new AsyncDocumentWorkItemQueue();
                        this.higherPriorityDocumentsNotProcessed = new ConcurrentDictionary<DocumentId, bool>(concurrencyLevel: 2, capacity: 20);

                        this.currentProjectProcessing = default(ProjectId);
                        this.processingSolution = null;

                        this.currentSnapshotVersionTrackingSet = new HashSet<ProjectId>();

                        Start();
                    }

                    internal ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return this.lazyAnalyzers.Value;
                        }
                    }

                    public void Enqueue(WorkItem item)
                    {
                        Contract.ThrowIfFalse(item.DocumentId != null, "can only enqueue a document work item");

                        this.UpdateLastAccessTime();

                        var added = this.workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_DocumentWorker_Enqueue, enqueueLogger, Environment.TickCount, item.DocumentId, !added);

                        CheckHigherPriorityDocument(item);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(
                            this.Processor.logAggregator, item.Language, item.DocumentId, item.InvocationReasons, item.IsLowPriority, item.ActiveMember, added);
                    }

                    private void CheckHigherPriorityDocument(WorkItem item)
                    {
                        if (item.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentOpened) ||
                            item.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentClosed))
                        {
                            AddHigherPriorityDocument(item.DocumentId);
                        }
                    }

                    private void AddHigherPriorityDocument(DocumentId id)
                    {
                        this.higherPriorityDocumentsNotProcessed[id] = true;

                        SolutionCrawlerLogger.LogHigherPriority(this.Processor.logAggregator, id.Id);
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        if (!this.workItemQueue.HasAnyWork)
                        {
                            if (this.projectCache != null)
                            {
                                this.projectCache.Dispose();
                                this.projectCache = null;
                            }
                        }

                        return this.workItemQueue.WaitAsync(cancellationToken);
                    }

                    public Task Running
                    {
                        get
                        {
                            return this.running;
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
                            this.running = source.Task;

                            // we wait for global operation if there is anything going on
                            await GlobalOperationWaitAsync().ConfigureAwait(false);

                            // we wait for higher processor to finish its working
                            await this.Processor.highPriorityProcessor.Running.ConfigureAwait(false);

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
                            if (!this.workItemQueue.TryTakeAnyWork(this.currentProjectProcessing, out workItem, out documentCancellation))
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

                    private void SetProjectProcessing(ProjectId currentProject)
                    {
                        if (currentProject != this.currentProjectProcessing)
                        {
                            if (projectCache != null)
                            {
                                projectCache.Dispose();
                                projectCache = null;
                            }

                            var projectCacheService = processingSolution.Workspace.Services.GetService<IProjectCacheService>();
                            if (projectCacheService != null)
                            {
                                projectCache = projectCacheService.EnableCaching(currentProject);
                            }
                        }

                        this.currentProjectProcessing = currentProject;
                    }

                    private IEnumerable<DocumentId> GetPrioritizedPendingDocuments()
                    {
                        if (this.Processor.documentTracker != null)
                        {
                            // First the active document
                            var activeDocumentId = this.Processor.documentTracker.GetActiveDocument();
                            if (activeDocumentId != null && this.higherPriorityDocumentsNotProcessed.ContainsKey(activeDocumentId))
                            {
                                yield return activeDocumentId;
                            }

                            // Now any visible documents
                            foreach (var visibleDocumentId in this.Processor.documentTracker.GetVisibleDocuments())
                            {
                                if (this.higherPriorityDocumentsNotProcessed.ContainsKey(visibleDocumentId))
                                {
                                    yield return visibleDocumentId;
                                }
                            }
                        }

                        // Any other opened documents
                        foreach (var documentId in this.higherPriorityDocumentsNotProcessed.Keys)
                        {
                            yield return documentId;
                        }
                    }

                    private async Task<bool> TryProcessOneHigherPriorityDocumentAsync()
                    {
                        try
                        {
                            // this is an best effort algorithm with some shortcommings.
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
                                if (!this.workItemQueue.TryTake(documentId, out workItem, out documentCancellation))
                                {
                                    continue;
                                }

                                // okay now we have work to do
                                await ProcessDocumentAsync(this.Analyzers, workItem, documentCancellation).ConfigureAwait(false);

                                // remove opened document processed
                                bool dummy;
                                this.higherPriorityDocumentsNotProcessed.TryRemove(documentId, out dummy);
                                return true;
                            }

                            return false;
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
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
                                var document = this.processingSolution.GetDocument(documentId);
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
                                    SolutionCrawlerLogger.LogProcessDocumentNotExist(this.Processor.logAggregator);

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
                                this.workItemQueue.AddOrReplace(workItem.Retry(this.Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessDocument(this.Processor.logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            this.workItemQueue.RemoveCancellationSource(workItem.DocumentId);
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
                        if (this.currentSnapshotVersionTrackingSet.Contains(document.Project.Id))
                        {
                            return;
                        }

                        await service.RecordSemanticVersionsAsync(document.Project, cancellationToken).ConfigureAwait(false);

                        // mark this project as already processed.
                        this.currentSnapshotVersionTrackingSet.Add(document.Project.Id);
                    }

                    private async Task ProcessOpenDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (!isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentOpened))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessOpenDocument(this.Processor.logAggregator, document.Id.Id);

                        await RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentOpenAsync(d, c), cancellationToken).ConfigureAwait(false);
                    }

                    private async Task ProcessCloseDocumentIfNeeded(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, bool isOpen, CancellationToken cancellationToken)
                    {
                        if (isOpen || !workItem.InvocationReasons.Contains(PredefinedInvocationReasons.DocumentClosed))
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogProcessCloseDocument(this.Processor.logAggregator, document.Id.Id);

                        await RunAnalyzersAsync(analyzers, document, (a, d, c) => a.DocumentResetAsync(d, c), cancellationToken).ConfigureAwait(false);
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
                        if (currentSolution == null || processingSolution == null ||
                            currentSolution.Id == this.processingSolution.Id)
                        {
                            return;
                        }

                        SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(
                            this.Processor.correlationId, this.processingSolution, this.Processor.logAggregator, this.Analyzers);

                        this.Processor.ResetLogAggregator();
                    }

                    private async Task ResetStatesAsync()
                    {
                        try
                        {
                            var currentSolution = this.Processor.CurrentSolution;

                            if (currentSolution != processingSolution)
                            {
                                ResetLogAggregatorIfNeeded(currentSolution);

                                // clear version tracking set we already reported.
                                currentSnapshotVersionTrackingSet.Clear();

                                processingSolution = currentSolution;

                                await RunAnalyzersAsync(this.Analyzers, currentSolution, (a, s, c) => a.NewSolutionSnapshotAsync(s, c), this.CancellationToken).ConfigureAwait(false);

                                foreach (var id in this.Processor.GetOpenDocumentIds())
                                {
                                    AddHigherPriorityDocument(id);
                                }

                                SolutionCrawlerLogger.LogResetStates(this.Processor.logAggregator);
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

                        SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics(this.Processor.correlationId, this.processingSolution, this.Processor.logAggregator, this.Analyzers);

                        this.workItemQueue.Dispose();

                        if (this.projectCache != null)
                        {
                            this.projectCache.Dispose();
                            this.projectCache = null;
                        }
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
                    {
                        CancellationTokenSource source = new CancellationTokenSource();

                        this.processingSolution = this.Processor.CurrentSolution;
                        foreach (var item in items)
                        {
                            ProcessDocumentAsync(analyzers, item, source).Wait();
                        }
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly()
                    {
                        // this shouldn't happen. would like to get some diagnostic
                        while (this.workItemQueue.HasAnyWork)
                        {
                            Environment.FailFast("How?");
                        }
                    }
                }
            }
        }
    }
}
