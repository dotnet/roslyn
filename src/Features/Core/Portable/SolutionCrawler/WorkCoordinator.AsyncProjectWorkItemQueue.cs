// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private sealed class AsyncProjectWorkItemQueue : AsyncWorkItemQueue<ProjectId>
            {
                private readonly Dictionary<ProjectId, WorkItem> _projectWorkQueue = new Dictionary<ProjectId, WorkItem>();

                public AsyncProjectWorkItemQueue(SolutionCrawlerProgressReporter progressReporter, Workspace workspace)
                    : base(progressReporter, workspace)
                {
                }

                protected override int WorkItemCount_NoLock => _projectWorkQueue.Count;

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
                        workInfo = default;
                        return false;
                    }

                    return _projectWorkQueue.Remove(key);
                }

                protected override bool TryTakeAnyWork_NoLock(
                    ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, IDiagnosticAnalyzerService analyzerService,
                    out WorkItem workItem)
                {
                    // there must be at least one item in the map when this is called unless host is shutting down.
                    if (_projectWorkQueue.Count == 0)
                    {
                        workItem = default;
                        return false;
                    }

                    var projectId = GetBestProjectId_NoLock(_projectWorkQueue, preferableProjectId, dependencyGraph, analyzerService);
                    if (TryTake_NoLock(projectId, out workItem))
                    {
                        return true;
                    }

                    return Contract.FailWithReturn<bool>("how?");
                }

                protected override bool AddOrReplace_NoLock(WorkItem item)
                {
                    var key = item.ProjectId;
                    Cancel_NoLock(key);
                    // now document work

                    // see whether we need to update
                    if (_projectWorkQueue.TryGetValue(key, out var existingWorkItem))
                    {
                        // replace it.
                        _projectWorkQueue[key] = existingWorkItem.With(item.InvocationReasons, item.ActiveMember, item.SpecificAnalyzers, item.IsRetry, item.AsyncToken);
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
