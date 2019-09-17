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
                    private readonly AsyncDocumentWorkItemQueue _workItemQueue;

                    private Lazy<ImmutableArray<IIncrementalAnalyzer>> _lazyAnalyzers;

                    // whether this processor is running or not
                    private Task _running;

                    public HighPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken)
                        : base(listener, backOffTimeSpanInMs, shutdownToken)
                    {
                        _processor = processor;
                        _lazyAnalyzers = lazyAnalyzers;

                        _running = Task.CompletedTask;
                        _workItemQueue = new AsyncDocumentWorkItemQueue(processor._registration.ProgressReporter, processor._registration.Workspace);

                        Start();
                    }

                    public ImmutableArray<IIncrementalAnalyzer> Analyzers => _lazyAnalyzers.Value;

                    public Task Running => _running;

                    public int WorkItemCount => _workItemQueue.WorkItemCount;
                    public bool HasAnyWork => _workItemQueue.HasAnyWork;

                    public void AddAnalyzer(IIncrementalAnalyzer analyzer)
                    {
                        var analyzers = this.Analyzers;
                        Interlocked.Exchange(ref _lazyAnalyzers, new Lazy<ImmutableArray<IIncrementalAnalyzer>>(() => analyzers.Add(analyzer)));
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
                            _processor._documentTracker.TryGetActiveDocument() != item.DocumentId)
                        {
                            return;
                        }

                        // we need to clone due to waiter
                        EnqueueActiveFileItem(item.With(Listener.BeginAsyncOperation("ActiveFile")));
                    }

                    private void EnqueueActiveFileItem(WorkItem item)
                    {
                        UpdateLastAccessTime();
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
                        if (CancellationToken.IsCancellationRequested)
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
                            Contract.ThrowIfFalse(GetNextWorkItem(out var workItem, out var documentCancellation));

                            var solution = _processor.CurrentSolution;

                            // okay now we have work to do
                            await ProcessDocumentAsync(solution, Analyzers, workItem, documentCancellation).ConfigureAwait(false);
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

                    private bool GetNextWorkItem(out WorkItem workItem, out CancellationToken cancellationToken)
                    {
                        // GetNextWorkItem since it can't fail. we still return bool to confirm that this never fail.
                        var documentId = _processor._documentTracker.TryGetActiveDocument();
                        if (documentId != null)
                        {
                            if (_workItemQueue.TryTake(documentId, out workItem, out cancellationToken))
                            {
                                return true;
                            }
                        }

                        return _workItemQueue.TryTakeAnyWork(
                            preferableProjectId: null,
                            dependencyGraph: _processor.DependencyGraph,
                            analyzerService: _processor.DiagnosticAnalyzerService,
                            workItem: out workItem,
                            cancellationToken: out cancellationToken);
                    }

                    private async Task ProcessDocumentAsync(Solution solution, ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationToken cancellationToken)
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var processedEverything = false;
                        var documentId = workItem.DocumentId;

                        try
                        {
                            using (Logger.LogBlock(FunctionId.WorkCoordinator_ProcessDocumentAsync, w => w.ToString(), workItem, cancellationToken))
                            {
                                var document = solution.GetDocument(documentId);
                                if (document != null)
                                {
                                    await _processor.ProcessDocumentAnalyzersAsync(document, analyzers, workItem, cancellationToken).ConfigureAwait(false);
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

                            SolutionCrawlerLogger.LogProcessActiveFileDocument(_processor._logAggregator, documentId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.MarkWorkItemDoneFor(workItem.DocumentId);
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
