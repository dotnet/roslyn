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
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class HighPriorityProcessor : IdleProcessor
                {
                    private readonly IncrementalAnalyzerProcessor _processor;
                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> _lazyAnalyzers;
                    private readonly AsyncDocumentWorkItemQueue _workItemQueue;

                    // whether this processor is running or not
                    private Task _running;

                    public HighPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, backOffTimeSpanInMs, shutdownToken)
                    {
                        _processor = processor;
                        _lazyAnalyzers = lazyAnalyzers;

                        _running = SpecializedTasks.EmptyTask;
                        _workItemQueue = new AsyncDocumentWorkItemQueue(processor._registration.ProgressReporter, processor._registration.Workspace);

                        Start();
                    }

                    private ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return _lazyAnalyzers.Value;
                        }
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

                    public void Enqueue(WorkItem item)
                    {
                        Contract.ThrowIfFalse(item.DocumentId != null, "can only enqueue a document work item");

                        // we only put workitem in high priority queue if there is a text change.
                        // this is to prevent things like opening a file, changing in other files keep enqueuing
                        // expensive high priority work.
                        if (!item.InvocationReasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                        {
                            return;
                        }

                        // check whether given item is for active document, otherwise, nothing to do here
                        if (_processor._documentTracker == null ||
                            _processor._documentTracker.GetActiveDocument() != item.DocumentId)
                        {
                            return;
                        }

                        // we need to clone due to waiter
                        EnqueueActiveFileItem(item.With(Listener.BeginAsyncOperation("ActiveFile")));
                    }

                    private void EnqueueActiveFileItem(WorkItem item)
                    {
                        this.UpdateLastAccessTime();
                        var added = _workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_ActiveFileEnqueue, s_enqueueLogger, Environment.TickCount, item.DocumentId, !added);
                        SolutionCrawlerLogger.LogActiveFileEnqueue(_processor._logAggregator);
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        return _workItemQueue.WaitAsync(cancellationToken);
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

                            // okay, there must be at least one item in the map

                            // see whether we have work item for the document
                            WorkItem workItem;
                            CancellationTokenSource documentCancellation;
                            Contract.ThrowIfFalse(GetNextWorkItem(out workItem, out documentCancellation));

                            var solution = _processor.CurrentSolution;

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
                        // GetNextWorkItem since it can't fail. we still return bool to confirm that this never fail.
                        var documentId = _processor._documentTracker.GetActiveDocument();
                        if (documentId != null)
                        {
                            if (_workItemQueue.TryTake(documentId, out workItem, out documentCancellation))
                            {
                                return true;
                            }
                        }

                        return _workItemQueue.TryTakeAnyWork(
                            preferableProjectId: null,
                            dependencyGraph: _processor.DependencyGraph,
                            analyzerService: _processor.DiagnosticAnalyzerService,
                            workItem: out workItem,
                            source: out documentCancellation);
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
                                _workItemQueue.AddOrReplace(workItem.Retry(this.Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessActiveFileDocument(_processor._logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.RemoveCancellationSource(workItem.DocumentId);
                        }
                    }

                    public void Shutdown()
                    {
                        _workItemQueue.Dispose();
                    }
                }
            }
        }
    }
}
