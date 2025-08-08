// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal sealed partial class UnitTestingWorkCoordinator
    {
        private sealed class UnitTestingAsyncProjectWorkItemQueue(UnitTestingSolutionCrawlerProgressReporter progressReporter) : UnitTestingAsyncWorkItemQueue<ProjectId>(progressReporter)
        {
            private readonly Dictionary<ProjectId, UnitTestingWorkItem> _projectWorkQueue = [];

            protected override int WorkItemCount_NoLock => _projectWorkQueue.Count;

            public override Task WaitAsync(CancellationToken cancellationToken)
            {
                if (!HasAnyWork)
                {
                    Logger.Log(FunctionId.WorkCoordinator_AsyncWorkItemQueue_LastItem);
                }

                return base.WaitAsync(cancellationToken);
            }

            protected override bool TryTake_NoLock(ProjectId key, out UnitTestingWorkItem workInfo)
            {
                if (!_projectWorkQueue.TryGetValue(key, out workInfo))
                {
                    workInfo = default;
                    return false;
                }

                return _projectWorkQueue.Remove(key);
            }

            protected override bool TryTakeAnyWork_NoLock(
                ProjectId? preferableProjectId,
                out UnitTestingWorkItem workItem)
            {
                // there must be at least one item in the map when this is called unless host is shutting down.
                if (_projectWorkQueue.Count == 0)
                {
                    workItem = default;
                    return false;
                }

                var projectId = GetBestProjectId_NoLock(
                    _projectWorkQueue, preferableProjectId);
                if (TryTake_NoLock(projectId, out workItem))
                {
                    return true;
                }

                throw ExceptionUtilities.Unreachable();
            }

            protected override bool AddOrReplace_NoLock(UnitTestingWorkItem item)
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
