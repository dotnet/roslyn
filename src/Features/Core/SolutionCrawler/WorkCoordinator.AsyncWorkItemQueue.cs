// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                private readonly SolutionCrawlerProgressReporter _progressReporter;

                // map containing cancellation source for the item given out.
                private readonly Dictionary<object, CancellationTokenSource> _cancellationMap;

                public AsyncWorkItemQueue(SolutionCrawlerProgressReporter progressReporter)
                {
                    _gate = new object();
                    _semaphore = new SemaphoreSlim(initialCount: 0);
                    _cancellationMap = new Dictionary<object, CancellationTokenSource>();
                    _progressReporter = progressReporter;
                }

                protected abstract int WorkItemCount_NoLock { get; }

                protected abstract void Dispose_NoLock();

                protected abstract bool AddOrReplace_NoLock(WorkItem item);

                protected abstract bool TryTake_NoLock(TKey key, out WorkItem workInfo);

                protected abstract bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, out WorkItem workItem);

                public bool HasAnyWork
                {
                    get
                    {
                        lock (_gate)
                        {
                            return HasAnyWork_NoLock;
                        }
                    }
                }

                public void RemoveCancellationSource(object key)
                {
                    lock (_gate)
                    {
                        // just remove cancellation token from the map.
                        // the cancellation token might be passed out to other service
                        // so don't call cancel on the source only because we are done using it.
                        _cancellationMap.Remove(key);
                    }
                }

                public virtual Task WaitAsync(CancellationToken cancellationToken)
                {
                    return _semaphore.WaitAsync(cancellationToken);
                }

                public bool AddOrReplace(WorkItem item)
                {
                    if (!HasAnyWork)
                    {
                        // first work is added.
                        _progressReporter.Start();
                    }

                    lock (_gate)
                    {
                        if (AddOrReplace_NoLock(item))
                        {
                            // increase count 
                            _semaphore.Release();
                            return true;
                        }

                        return false;
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

                private static void RaiseCancellation_NoLock(List<CancellationTokenSource> cancellations)
                {
                    if (cancellations == null)
                    {
                        return;
                    }

                    // cancel can cause outer code to be run inlined, run it outside of the lock.
                    cancellations.Do(s => s.Cancel());
                }

                private bool HasAnyWork_NoLock
                {
                    get
                    {
                        return WorkItemCount_NoLock > 0;
                    }
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
                    CancellationTokenSource source;
                    if (_cancellationMap.TryGetValue(key, out source))
                    {
                        source.Cancel();
                        _cancellationMap.Remove(key);
                    }
                }

                public bool TryTake(TKey key, out WorkItem workInfo, out CancellationTokenSource source)
                {
                    lock (_gate)
                    {
                        if (TryTake_NoLock(key, out workInfo))
                        {
                            if (!HasAnyWork_NoLock)
                            {
                                // last work is done.
                                _progressReporter.Stop();
                            }

                            source = GetNewCancellationSource_NoLock(key);
                            workInfo.AsyncToken.Dispose();
                            return true;
                        }
                        else
                        {
                            source = null;
                            return false;
                        }
                    }
                }

                public bool TryTakeAnyWork(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, out WorkItem workItem, out CancellationTokenSource source)
                {
                    lock (_gate)
                    {
                        // there must be at least one item in the map when this is called unless host is shutting down.
                        if (TryTakeAnyWork_NoLock(preferableProjectId, dependencyGraph, out workItem))
                        {
                            if (!HasAnyWork_NoLock)
                            {
                                // last work is done.
                                _progressReporter.Stop();
                            }

                            source = GetNewCancellationSource_NoLock(workItem.Key);
                            workItem.AsyncToken.Dispose();
                            return true;
                        }
                        else
                        {
                            source = null;
                            return false;
                        }
                    }
                }

                protected CancellationTokenSource GetNewCancellationSource_NoLock(object key)
                {
                    Contract.Requires(!_cancellationMap.ContainsKey(key));

                    var source = new CancellationTokenSource();
                    _cancellationMap.Add(key, source);

                    return source;
                }
            }
        }
    }
}
