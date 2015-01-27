// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class WorkCoordinatorRegistrationService
    {
        private partial class WorkCoordinator
        {
            private sealed class AsyncProjectWorkItemQueue : AsyncWorkItemQueue<ProjectId>
            {
                private readonly Dictionary<ProjectId, WorkItem> projectWorkQueue = new Dictionary<ProjectId, WorkItem>();

                protected override int WorkItemCount_NoLock
                {
                    get
                    {
                        return projectWorkQueue.Count;
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
                    if (!this.projectWorkQueue.TryGetValue(key, out workInfo))
                    {
                        workInfo = default(WorkItem);
                        return false;
                    }

                    return this.projectWorkQueue.Remove(key);
                }

                protected override bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, out WorkItem workItem)
                {
                    if (preferableProjectId != null)
                    {
                        if (TryTake_NoLock(preferableProjectId, out workItem))
                        {
                            return true;
                        }
                    }

                    // explicitly iterate so that we can use struct enumerator
                    foreach (var kvp in this.projectWorkQueue)
                    {
                        workItem = kvp.Value;
                        return this.projectWorkQueue.Remove(kvp.Key);
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
                    if (this.projectWorkQueue.TryGetValue(key, out existingWorkItem))
                    {
                        // replace it.
                        projectWorkQueue[key] = existingWorkItem.With(item.InvocationReasons, item.ActiveMember, item.Analyzers, item.IsRetry, item.AsyncToken);
                        return false;
                    }

                    // okay, it is new one
                    // always hold onto the most recent one for the same project
                    projectWorkQueue.Add(key, item);

                    return true;
                }

                protected override void Dispose_NoLock()
                {
                    foreach (var workItem in this.projectWorkQueue.Values)
                    {
                        workItem.AsyncToken.Dispose();
                    }

                    this.projectWorkQueue.Clear();
                }
            }
        }
    }
}
