// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class WorkCoordinatorRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class HighPriorityProcessor : IdleProcessor
                {
                    private readonly IncrementalAnalyzerProcessor processor;
                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers;
                    private readonly AsyncDocumentWorkItemQueue workItemQueue;

                    // whether this processor is running or not
                    private Task running;

                    public HighPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.processor = processor;
                        this.lazyAnalyzers = lazyAnalyzers;

                        this.running = SpecializedTasks.EmptyTask;
                        this.workItemQueue = new AsyncDocumentWorkItemQueue();

                        Start();
                    }

                    private ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return this.lazyAnalyzers.Value;
                        }
                    }

                    public Task Running
                    {
                        get
                        {
                            return this.running;
                        }
                    }

                    public void Enqueue(WorkItem item)
                    {
                        Contract.ThrowIfFalse(item.DocumentId != null, "can only enqueue a document work item");

                        // we only put workitem in high priority queue if there is a text change.
                        // this is to prevent things like opening a file, changing in other files keep enquening
                        // expensive high priority work.
                        if (!item.InvocationReasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                        {
                            return;
                        }

                        // check whether given item is for active document, otherwise, nothing to do here
                        if (this.processor.documentTracker == null ||
                            this.processor.documentTracker.GetActiveDocument() != item.DocumentId)
                        {
                            return;
                        }

                        // we need to clone due to waiter
                        EnqueueActiveFileItem(item.With(Listener.BeginAsyncOperation("ActiveFile")));
                    }

                    private void EnqueueActiveFileItem(WorkItem item)
                    {
                        this.UpdateLastAccessTime();
                        var added = this.workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_ActivieFileEnqueue, enqueueLogger, Environment.TickCount, item.DocumentId, !added);
                        SolutionCrawlerLogger.LogActiveFileEnqueue(this.processor.logAggregator);
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        return this.workItemQueue.WaitAsync(cancellationToken);
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

                            // okay, there must be at least one item in the map

                            // see whether we have work item for the document
                            WorkItem workItem;
                            CancellationTokenSource documentCancellation;
                            Contract.ThrowIfFalse(GetNextWorkItem(out workItem, out documentCancellation));

                            var solution = this.processor.CurrentSolution;

                            // okay now we have work to do
                            await ProcessDocumentAsync(solution, this.Analyzers, workItem, documentCancellation).ConfigureAwait(false);
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

                    private bool GetNextWorkItem(out WorkItem workItem, out CancellationTokenSource documentCancellation)
                    {
                        var documentId = this.processor.documentTracker.GetActiveDocument();
                        if (documentId != null)
                        {
                            if (this.workItemQueue.TryTake(documentId, out workItem, out documentCancellation))
                            {
                                return true;
                            }
                        }

                        return this.workItemQueue.TryTakeAnyWork(preferableProjectId: null, workItem: out workItem, source: out documentCancellation);
                    }

                    private async Task ProcessDocumentAsync(Solution solution, ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationTokenSource source)
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
                                var document = solution.GetDocument(documentId);
                                if (document != null)
                                {
                                    await ProcessDocumentAnalyzersAsync(document, analyzers, workItem, cancellationToken).ConfigureAwait(false);
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

                            SolutionCrawlerLogger.LogProcessActiveFileDocument(this.processor.logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            this.workItemQueue.RemoveCancellationSource(workItem.DocumentId);
                        }
                    }

                    public void Shutdown()
                    {
                        this.workItemQueue.Dispose();
                    }
                }
            }
        }
    }
}
