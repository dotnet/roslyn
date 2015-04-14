// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private sealed class AsyncProjectWorkItemQueue : AsyncWorkItemQueue<ProjectId>
            {
                private readonly Dictionary<ProjectId, WorkItem> _projectWorkQueue = new Dictionary<ProjectId, WorkItem>();

                public AsyncProjectWorkItemQueue(SolutionCrawlerProgressReporter progressReporter) :
                    base(progressReporter)
                {
                }

                protected override int WorkItemCount_NoLock
                {
                    get
                    {
                        return _projectWorkQueue.Count;
                    }
                }

                public override Task WaitAsync(CancellationToken cancellationToken)
                {
                    if (!HasAnyWork)
                    {
                        Logger.Log(FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem);
                    }

                    return base.WaitAsync(cancellationToken);
                }

                protected override bool TryTake_NoLock(ProjectId key, out WorkItem workInfo)
                {
                    if (!_projectWorkQueue.TryGetValue(key, out workInfo))
                    {
                        workInfo = default(WorkItem);
                        return false;
                    }

                    return _projectWorkQueue.Remove(key);
                }

                protected override bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, out WorkItem workItem)
                {
                    if (preferableProjectId != null)
                    {
                        if (TryTake_NoLock(preferableProjectId, out workItem))
                        {
                            return true;
                        }

                        foreach (var dependingProjectId in dependencyGraph.GetProjectsThatDirectlyDependOnThisProject(preferableProjectId))
                        {
                            if (TryTake_NoLock(dependingProjectId, out workItem))
                            {
                                return true;
                            }
                        }
                    }

                    // explicitly iterate so that we can use struct enumerator
                    foreach (var kvp in _projectWorkQueue)
                    {
                        workItem = kvp.Value;
                        return _projectWorkQueue.Remove(kvp.Key);
                    }

                    workItem = default(WorkItem);
                    return false;
                }

                protected override bool AddOrReplace_NoLock(WorkItem item)
                {
                    var key = item.ProjectId;
                    Cancel_NoLock(key);

                    // now document work
                    var existingWorkItem = default(WorkItem);

                    // see whether we need to update
                    if (_projectWorkQueue.TryGetValue(key, out existingWorkItem))
                    {
                        // replace it.
                        _projectWorkQueue[key] = existingWorkItem.With(item.InvocationReasons, item.ActiveMember, item.Analyzers, item.IsRetry, item.AsyncToken);
                        return false;
                    }

                    // okay, it is new one
                    // always hold onto the most recent one for the same project
                    _projectWorkQueue.Add(key, item);

                    return true;
                }

                protected override void Dispose_NoLock()
                {
                    foreach (var workItem in _projectWorkQueue.Values)
                    {
                        workItem.AsyncToken.Dispose();
                    }

                    _projectWorkQueue.Clear();
                }
            }
        }
    }
}
