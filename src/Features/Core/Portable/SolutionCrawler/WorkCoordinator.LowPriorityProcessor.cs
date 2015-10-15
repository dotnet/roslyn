// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
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
                private sealed class LowPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> _lazyAnalyzers;
                    private readonly AsyncProjectWorkItemQueue _workItemQueue;

                    public LowPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, processor, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        _lazyAnalyzers = lazyAnalyzers;
                        _workItemQueue = new AsyncProjectWorkItemQueue(processor._registration.ProgressReporter);

                        Start();
                    }

                    internal ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return _lazyAnalyzers.Value;
                        }
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        return _workItemQueue.WaitAsync(cancellationToken);
                    }

                    protected override async Task ExecuteAsync()
                    {
                        try
                        {
                            // we wait for global operation, higher and normal priority processor to finish its working
                            await WaitForHigherPriorityOperationsAsync().ConfigureAwait(false);

                            // process any available project work, preferring the active project.
                            WorkItem workItem;
                            CancellationTokenSource projectCancellation;
                            if (_workItemQueue.TryTakeAnyWork(this.Processor.GetActiveProject(), this.Processor.DependencyGraph, out workItem, out projectCancellation))
                            {
                                await ProcessProjectAsync(this.Analyzers, workItem, projectCancellation).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    protected override Task HigherQueueOperationTask
                    {
                        get
                        {
                            return Task.WhenAll(this.Processor._highPriorityProcessor.Running, this.Processor._normalPriorityProcessor.Running);
                        }
                    }

                    protected override bool HigherQueueHasWorkItem
                    {
                        get
                        {
                            return this.Processor._highPriorityProcessor.HasAnyWork || this.Processor._normalPriorityProcessor.HasAnyWork;
                        }
                    }

                    protected override void PauseOnGlobalOperation()
                    {
                        _workItemQueue.RequestCancellationOnRunningTasks();
                    }

                    public void Enqueue(WorkItem item)
                    {
                        this.UpdateLastAccessTime();

                        // Project work
                        item = item.With(documentId: null, projectId: item.ProjectId, asyncToken: this.Processor._listener.BeginAsyncOperation("WorkItem"));

                        var added = _workItemQueue.AddOrReplace(item);

                        // lower priority queue gets lowest time slot possible. if there is any activity going on in higher queue, it drop whatever it has
                        // and let higher work item run
                        CancelRunningTaskIfHigherQueueHasWorkItem();

                        Logger.Log(FunctionId.WorkCoordinator_Project_Enqueue, s_enqueueLogger, Environment.TickCount, item.ProjectId, !added);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(this.Processor._logAggregator, item.ProjectId);
                    }

                    private void CancelRunningTaskIfHigherQueueHasWorkItem()
                    {
                        if (!HigherQueueHasWorkItem)
                        {
                            return;
                        }

                        _workItemQueue.RequestCancellationOnRunningTasks();
                    }

                    private async Task ProcessProjectAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationTokenSource source)
                    {
                        if (this.CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        // we do have work item for this project
                        var projectId = workItem.ProjectId;
                        var processedEverything = false;
                        var processingSolution = this.Processor.CurrentSolution;

                        try
                        {
                            using (Logger.LogBlock(FunctionId.WorkCoordinator_ProcessProjectAsync, source.Token))
                            {
                                var cancellationToken = source.Token;

                                var project = processingSolution.GetProject(projectId);
                                if (project != null)
                                {
                                    var semanticsChanged = workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SemanticChanged) ||
                                                           workItem.InvocationReasons.Contains(PredefinedInvocationReasons.SolutionRemoved);

                                    using (Processor.EnableCaching(project.Id))
                                    {
                                        await RunAnalyzersAsync(analyzers, project, (a, p, c) => a.AnalyzeProjectAsync(p, semanticsChanged, c), cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    SolutionCrawlerLogger.LogProcessProjectNotExist(this.Processor._logAggregator);

                                    RemoveProject(projectId);
                                }
                            }

                            processedEverything = true;
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                        finally
                        {
                            // we got cancelled in the middle of processing the project.
                            // let's make sure newly enqueued work item has all the flag needed.
                            if (!processedEverything)
                            {
                                _workItemQueue.AddOrReplace(workItem.Retry(this.Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessProject(this.Processor._logAggregator, projectId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.RemoveCancellationSource(projectId);
                        }
                    }

                    private void RemoveProject(ProjectId projectId)
                    {
                        foreach (var analyzer in this.Analyzers)
                        {
                            analyzer.RemoveProject(projectId);
                        }
                    }

                    public override void Shutdown()
                    {
                        base.Shutdown();
                        _workItemQueue.Dispose();
                    }

                    internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
                    {
                        CancellationTokenSource source = new CancellationTokenSource();

                        var uniqueIds = new HashSet<ProjectId>();
                        foreach (var item in items)
                        {
                            if (uniqueIds.Add(item.ProjectId))
                            {
                                ProcessProjectAsync(analyzers, item, source).Wait();
                            }
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
