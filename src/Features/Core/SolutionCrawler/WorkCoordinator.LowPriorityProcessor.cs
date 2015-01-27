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
    internal sealed partial class WorkCoordinatorRegistrationService
    {
        private sealed partial class WorkCoordinator
        {
            private sealed partial class IncrementalAnalyzerProcessor
            {
                private sealed class LowPriorityProcessor : GlobalOperationAwareIdleProcessor
                {
                    private readonly Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers;
                    private readonly AsyncProjectWorkItemQueue workItemQueue;

                    public LowPriorityProcessor(
                        IAsynchronousOperationListener listener,
                        IncrementalAnalyzerProcessor processor,
                        Lazy<ImmutableArray<IIncrementalAnalyzer>> lazyAnalyzers,
                        IGlobalOperationNotificationService globalOperationNotificationService,
                        int backOffTimeSpanInMs,
                        CancellationToken shutdownToken) :
                        base(listener, processor, globalOperationNotificationService, backOffTimeSpanInMs, shutdownToken)
                    {
                        this.lazyAnalyzers = lazyAnalyzers;
                        this.workItemQueue = new AsyncProjectWorkItemQueue();

                        Start();
                    }

                    internal ImmutableArray<IIncrementalAnalyzer> Analyzers
                    {
                        get
                        {
                            return this.lazyAnalyzers.Value;
                        }
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        return this.workItemQueue.WaitAsync(cancellationToken);
                    }

                    protected override async Task ExecuteAsync()
                    {
                        try
                        {
                            // we wait for global operation, higher and normal priority processor to finish its working
                            await Task.WhenAll(this.Processor.highPriorityProcessor.Running,
                                this.Processor.normalPriorityProcessor.Running,
                                GlobalOperationWaitAsync()).ConfigureAwait(false);

                            // process any available project work, preferring the active project.                            
                            CancellationTokenSource projectCancellation;
                            WorkItem workItem;
                            if (this.workItemQueue.TryTakeAnyWork(this.Processor.GetActiveProject(), out workItem, out projectCancellation))
                            {
                                await ProcessProjectAsync(this.Analyzers, workItem, projectCancellation).ConfigureAwait(false);
                            }
                        }
                        catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    }

                    public void Enqueue(WorkItem item)
                    {
                        this.UpdateLastAccessTime();

                        // Project work
                        item = item.With(documentId: null, projectId: item.ProjectId, asyncToken: this.Processor.listener.BeginAsyncOperation("WorkItem"));

                        var added = this.workItemQueue.AddOrReplace(item);

                        Logger.Log(FunctionId.WorkCoordinator_Project_Enqueue, enqueueLogger, Environment.TickCount, item.ProjectId, !added);

                        SolutionCrawlerLogger.LogWorkItemEnqueue(this.Processor.logAggregator, item.ProjectId);
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

                                    if (project.Solution.Services.CacheService != null)
                                    {
                                        using (project.Solution.Services.CacheService.EnableCaching(project.Id))
                                        {
                                            await RunAnalyzersAsync(analyzers, project, (a, p, c) => a.AnalyzeProjectAsync(p, semanticsChanged, c), cancellationToken).ConfigureAwait(false);
                                        }
                                    }
                                    else
                                    {
                                        await RunAnalyzersAsync(analyzers, project, (a, p, c) => a.AnalyzeProjectAsync(p, semanticsChanged, c), cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    SolutionCrawlerLogger.LogProcessProjectNotExist(this.Processor.logAggregator);

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
                                this.workItemQueue.AddOrReplace(workItem.Retry(this.Listener.BeginAsyncOperation("ReenqueueWorkItem")));
                            }

                            SolutionCrawlerLogger.LogProcessProject(this.Processor.logAggregator, projectId.Id, processedEverything);

                            // remove one that is finished running
                            this.workItemQueue.RemoveCancellationSource(projectId);
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
                        this.workItemQueue.Dispose();
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
