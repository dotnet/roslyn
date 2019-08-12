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
                private sealed class LowPriorityProcessor : AbstractPriorityProcessor
                {
                    private readonly AsyncProjectWorkItemQueue _workItemQueue;

                    public LowPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, processor, lazyAnalyzers, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        _workItemQueue = new AsyncProjectWorkItemQueue(processor._registration.ProgressReporter, processor._registration.Workspace);

                        Start();
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
                            if (_workItemQueue.TryTakeAnyWork(
                                Processor.GetActiveProject(), Processor.DependencyGraph, Processor.DiagnosticAnalyzerService,
                                out var workItem, out var projectCancellation))
                            {
                                await ProcessProjectAsync(Analyzers, workItem, projectCancellation).ConfigureAwait(false);
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
                            return Task.WhenAll(Processor._highPriorityProcessor.Running, Processor._normalPriorityProcessor.Running);
                        }
                    }

                    protected override bool HigherQueueHasWorkItem
                    {
                        get
                        {
                            return Processor._highPriorityProcessor.HasAnyWork || Processor._normalPriorityProcessor.HasAnyWork;
                        }
                    }

                    protected override void PauseOnGlobalOperation()
                    {
                        base.PauseOnGlobalOperation();

                        _workItemQueue.RequestCancellationOnRunningTasks();
                    }

                    public void Enqueue(WorkItem item)
                    {
                        UpdateLastAccessTime();

                        // Project work
                        item = item.With(documentId: null, projectId: item.ProjectId, asyncToken: Processor._listener.BeginAsyncOperation("WorkItem"));

                        var added = _workItemQueue.AddOrReplace(item);

                        // lower priority queue gets lowest time slot possible. if there is any activity going on in higher queue, it drop whatever it has
                        // and let higher work item run
                        CancelRunningTaskIfHigherQueueHasWorkItem();

                        Logger.Log(FunctionId.WorkCoordinator_Project_Enqueue, s_enqueueLogger, Environment.TickCount, item.ProjectId, !added);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(Processor._logAggregator, item.ProjectId);
                    }

                    private void CancelRunningTaskIfHigherQueueHasWorkItem()
                    {
                        if (!HigherQueueHasWorkItem)
                        {
                            return;
                        }

                        _workItemQueue.RequestCancellationOnRunningTasks();
                    }

                    private async Task ProcessProjectAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationToken cancellationToken)
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        // we do have work item for this project
                        var projectId = workItem.ProjectId;
                        var processedEverything = false;
                        var processingSolution = Processor.CurrentSolution;

                        try
                        {
                            using (Logger.LogBlock(FunctionId.WorkCoordinator_ProcessProjectAsync, w => w.ToString(), workItem, cancellationToken))
                            {
                                var project = processingSolution.GetProject(projectId);
                                if (project != null)
                                {
                                    var reasons = workItem.InvocationReasons;
                                    var semanticsChanged = reasons.Contains(PredefinedInvocationReasons.SemanticChanged) ||
                                                           reasons.Contains(PredefinedInvocationReasons.SolutionRemoved);

                                    using (Processor.EnableCaching(project.Id))
                                    {
                                        await Processor.RunAnalyzersAsync(analyzers, project, (a, p, c) => a.AnalyzeProjectAsync(p, semanticsChanged, reasons, c), cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    SolutionCrawlerLogger.LogProcessProjectNotExist(Processor._logAggregator);

                                    RemoveProject(projectId);
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
                            // we got cancelled in the middle of processing the project.
                            // let's make sure newly enqueued work item has all the flag needed.
                            // Avoid retry attempts after cancellation is requested, since work will not be processed
                            // after that point.
                            if (!processedEverything && !CancellationToken.IsCancellationRequested)
                            {
                                _workItemQueue.AddOrReplace(workItem.Retry(Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessProject(Processor._logAggregator, projectId.Id, processedEverything);

                            // remove one that is finished running
                            _workItemQueue.MarkWorkItemDoneFor(projectId);
                        }
                    }

                    private void RemoveProject(ProjectId projectId)
                    {
                        foreach (var analyzer in Analyzers)
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
                        var uniqueIds = new HashSet<ProjectId>();
                        foreach (var item in items)
                        {
                            if (uniqueIds.Add(item.ProjectId))
                            {
                                ProcessProjectAsync(analyzers, item, CancellationToken.None).Wait();
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
