// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private abstract class AsyncWorkItemQueue<TKey> : IDisposable
                where TKey : class
            {
                private readonly object _gate;
                private readonly SemaphoreSlim _semaphore;

                private readonly Workspace _workspace;
                private readonly SolutionCrawlerProgressReporter _progressReporter;

                // map containing cancellation source for the item given out.
                private readonly Dictionary<object, CancellationTokenSource> _cancellationMap;

                public AsyncWorkItemQueue(SolutionCrawlerProgressReporter progressReporter, Workspace workspace)
                {
                    _gate = new object();
                    _semaphore = new SemaphoreSlim(initialCount: 0);
                    _cancellationMap = new Dictionary<object, CancellationTokenSource>();

                    _workspace = workspace;
                    _progressReporter = progressReporter;
                }

                protected abstract int WorkItemCount_NoLock { get; }

                protected abstract void Dispose_NoLock();

                protected abstract bool AddOrReplace_NoLock(WorkItem item);

                protected abstract bool TryTake_NoLock(TKey key, out WorkItem workInfo);

                protected abstract bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, IDiagnosticAnalyzerService service, out WorkItem workItem);

                public int WorkItemCount
                {
                    get
                    {
                        lock (_gate)
                        {
                            return WorkItemCount_NoLock;
                        }
                    }
                }

                public bool HasAnyWork
                {
                    get
                    {
                        lock (_gate)
                        {
                            return WorkItemCount_NoLock > 0;
                        }
                    }
                }

                public virtual Task WaitAsync(CancellationToken cancellationToken)
                {
                    return _semaphore.WaitAsync(cancellationToken);
                }

                public bool AddOrReplace(WorkItem item)
                {
                    lock (_gate)
                    {
                        if (AddOrReplace_NoLock(item))
                        {
                            // the item is new item that got added to the queue.
                            // let solution crawler progress report to know about new item enqueued.
                            // progress reporter will take care of nested/overlapped works by itself
                            // 
                            // order of events is as follow
                            // 1. first item added by AddOrReplace which is the point where progress start.
                            // 2. bunch of other items added or replaced (workitem in the queue > 0)
                            // 3. items start dequeued to be processed by TryTake or TryTakeAnyWork
                            // 4. once item is done processed, it is marked as done by MarkWorkItemDoneFor
                            // 5. all items in the queue are dequeued (workitem in the queue == 0) 
                            //    but there can be still work in progress
                            // 6. all works are considered done when last item is marked done by MarkWorkItemDoneFor
                            //    and at the point, we will set progress to stop.
                            _progressReporter.Start();

                            // increase count 
                            _semaphore.Release();
                            return true;
                        }

                        return false;
                    }
                }

                public void MarkWorkItemDoneFor(object key)
                {
                    lock (_gate)
                    {
                        // just remove cancellation token from the map.
                        // the cancellation token might be passed out to other service
                        // so don't call cancel on the source only because we are done using it.
                        _cancellationMap.Remove(key);

                        // every works enqueued by "AddOrReplace" will be processed
                        // at some point, and when it is processed, this method will be called to mark
                        // work has been done.
                        _progressReporter.Stop();
                    }
                }

                public void RequestCancellationOnRunningTasks()
                {
                    List<CancellationTokenSource> cancellations;
                    lock (_gate)
                    {
                        // request to cancel all running works
                        cancellations = CancelAll_NoLock();
                    }

                    RaiseCancellation_NoLock(cancellations);
                }

                public void Dispose()
                {
                    List<CancellationTokenSource> cancellations;
                    lock (_gate)
                    {
                        // here we don't need to care about progress reporter since
                        // it will be only called when host is shutting down.
                        // we do the below since we want to kill any pending tasks
                        Dispose_NoLock();

                        cancellations = CancelAll_NoLock();
                    }

                    RaiseCancellation_NoLock(cancellations);
                }

                protected Workspace Workspace => _workspace;

                private static void RaiseCancellation_NoLock(List<CancellationTokenSource> cancellations)
                {
                    if (cancellations == null)
                    {
                        return;
                    }

                    // cancel can cause outer code to be run inlined, run it outside of the lock.
                    cancellations.Do(s => s.Cancel());
                }

                private List<CancellationTokenSource> CancelAll_NoLock()
                {
                    // nothing to do
                    if (_cancellationMap.Count == 0)
                    {
                        return null;
                    }

                    // make a copy
                    var cancellations = _cancellationMap.Values.ToList();

                    // clear cancellation map
                    _cancellationMap.Clear();

                    return cancellations;
                }

                protected void Cancel_NoLock(object key)
                {
                    if (_cancellationMap.TryGetValue(key, out var source))
                    {
                        source.Cancel();
                        _cancellationMap.Remove(key);
                    }
                }

                public bool TryTake(TKey key, out WorkItem workInfo, out CancellationToken cancellationToken)
                {
                    lock (_gate)
                    {
                        if (TryTake_NoLock(key, out workInfo))
                        {
                            cancellationToken = GetNewCancellationToken_NoLock(key);
                            workInfo.AsyncToken.Dispose();
                            return true;
                        }
                        else
                        {
                            cancellationToken = CancellationToken.None;
                            return false;
                        }
                    }
                }

                public bool TryTakeAnyWork(
                    ProjectId preferableProjectId,
                    ProjectDependencyGraph dependencyGraph,
                    IDiagnosticAnalyzerService analyzerService,
                    out WorkItem workItem, out CancellationToken cancellationToken)
                {
                    lock (_gate)
                    {
                        // there must be at least one item in the map when this is called unless host is shutting down.
                        if (TryTakeAnyWork_NoLock(preferableProjectId, dependencyGraph, analyzerService, out workItem))
                        {
                            cancellationToken = GetNewCancellationToken_NoLock(workItem.Key);
                            workItem.AsyncToken.Dispose();
                            return true;
                        }
                        else
                        {
                            cancellationToken = CancellationToken.None;
                            return false;
                        }
                    }
                }

                protected CancellationToken GetNewCancellationToken_NoLock(object key)
                {
                    Debug.Assert(!_cancellationMap.ContainsKey(key));

                    var source = new CancellationTokenSource();
                    _cancellationMap.Add(key, source);

                    return source.Token;
                }

                protected ProjectId GetBestProjectId_NoLock<T>(
                    Dictionary<ProjectId, T> workQueue, ProjectId projectId,
                    ProjectDependencyGraph dependencyGraph, IDiagnosticAnalyzerService analyzerService)
                {
                    if (projectId != null)
                    {
                        if (workQueue.ContainsKey(projectId))
                        {
                            return projectId;
                        }

                        // prefer project that directly depends on the given project and has diagnostics as next project to
                        // process
                        foreach (var dependingProjectId in dependencyGraph.GetProjectsThatDirectlyDependOnThisProject(projectId))
                        {
                            if (workQueue.ContainsKey(dependingProjectId) && analyzerService?.ContainsDiagnostics(Workspace, dependingProjectId) == true)
                            {
                                return dependingProjectId;
                            }
                        }
                    }

                    // prefer a project that has diagnostics as next project to process.
                    foreach (var pendingProjectId in workQueue.Keys)
                    {
                        if (analyzerService?.ContainsDiagnostics(Workspace, pendingProjectId) == true)
                        {
                            return pendingProjectId;
                        }
                    }

                    // explicitly iterate so that we can use struct enumerator
                    foreach (var pair in workQueue)
                    {
                        return pair.Key;
                    }

                    return Contract.FailWithReturn<ProjectId>("Shouldn't reach here");
                }
            }
        }
    }
}
