// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
                private readonly AsyncSemaphore _semaphore = new AsyncSemaphore(initialCount: 0);

                // map containing cancellation source for the item given out.
                private readonly Dictionary<object, CancellationTokenSource> _cancellationMap = new Dictionary<object, CancellationTokenSource>();

                private readonly object _gate = new object();

                protected abstract int WorkItemCount_NoLock { get; }

                protected abstract void Dispose_NoLock();

                protected abstract bool AddOrReplace_NoLock(WorkItem item);

                protected abstract bool TryTake_NoLock(TKey key, out WorkItem workInfo);

                protected abstract bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, out WorkItem workItem);

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

                public void Dispose()
                {
                    lock (_gate)
                    {
                        Dispose_NoLock();

                        _cancellationMap.Do(p => p.Value.Cancel());
                        _cancellationMap.Clear();
                    }
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

                public bool TryTakeAnyWork(ProjectId preferableProjectId, out WorkItem workItem, out CancellationTokenSource source)
                {
                    lock (_gate)
                    {
                        // there must be at least one item in the map when this is called unless host is shutting down.
                        if (TryTakeAnyWork_NoLock(preferableProjectId, out workItem))
                        {
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
