// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal partial class UnitTestingWorkCoordinator
    {
        private class UnitTestingAsyncDocumentWorkItemQueue(UnitTestingSolutionCrawlerProgressReporter progressReporter) : UnitTestingAsyncWorkItemQueue<DocumentId>(progressReporter)
        {
            private readonly Dictionary<ProjectId, Dictionary<DocumentId, UnitTestingWorkItem>> _documentWorkQueue = [];

            protected override int WorkItemCount_NoLock => _documentWorkQueue.Count;

            protected override bool TryTake_NoLock(DocumentId key, out UnitTestingWorkItem workInfo)
            {
                workInfo = default;
                if (_documentWorkQueue.TryGetValue(key.ProjectId, out var documentMap) &&
                    documentMap.TryGetValue(key, out workInfo))
                {
                    documentMap.Remove(key);

                    if (documentMap.Count == 0)
                    {
                        _documentWorkQueue.Remove(key.ProjectId);
                        SharedPools.BigDefault<Dictionary<DocumentId, UnitTestingWorkItem>>().ClearAndFree(documentMap);
                    }

                    return true;
                }

                return false;
            }

            protected override bool TryTakeAnyWork_NoLock(
                ProjectId? preferableProjectId,
                out UnitTestingWorkItem workItem)
            {
                // there must be at least one item in the map when this is called unless host is shutting down.
                if (_documentWorkQueue.Count == 0)
                {
                    workItem = default;
                    return false;
                }

                var documentId = GetBestDocumentId_NoLock(preferableProjectId);
                if (TryTake_NoLock(documentId, out workItem))
                {
                    return true;
                }

                throw ExceptionUtilities.Unreachable();
            }

            private DocumentId GetBestDocumentId_NoLock(
                ProjectId? preferableProjectId)
            {
                var projectId = GetBestProjectId_NoLock(
                    _documentWorkQueue,
                    preferableProjectId);

                var documentMap = _documentWorkQueue[projectId];

                // explicitly iterate so that we can use struct enumerator.
                // Return the first normal priority work item we find.  If we don't
                // find any, then just return the first low prio item we saw.
                DocumentId? lowPriorityDocumentId = null;
                foreach (var (documentId, workItem) in documentMap)
                {
                    if (workItem.IsLowPriority)
                    {
                        lowPriorityDocumentId = documentId;
                    }
                    else
                    {
                        return documentId;
                    }
                }

                Contract.ThrowIfNull(lowPriorityDocumentId);
                return lowPriorityDocumentId;
            }

            protected override bool AddOrReplace_NoLock(UnitTestingWorkItem item)
            {
                Contract.ThrowIfNull(item.DocumentId);

                Cancel_NoLock(item.DocumentId);

                // see whether we need to update
                var key = item.DocumentId;

                // now document work
                if (_documentWorkQueue.TryGetValue(key.ProjectId, out var documentMap) &&
                    documentMap.TryGetValue(key, out var existingWorkItem))
                {
                    // TODO: should I care about language when replace it?
                    Debug.Assert(existingWorkItem.Language == item.Language);

                    // replace it
                    documentMap[key] = existingWorkItem.With(item.InvocationReasons, item.ActiveMember, item.SpecificAnalyzers, item.IsRetry, item.AsyncToken);
                    return false;
                }

                // add document map if it is not already there
                if (documentMap == null)
                {
                    documentMap = SharedPools.BigDefault<Dictionary<DocumentId, UnitTestingWorkItem>>().AllocateAndClear();
                    _documentWorkQueue.Add(key.ProjectId, documentMap);

                    if (_documentWorkQueue.Count == 1)
                    {
                        Logger.Log(FunctionId.WorkCoordinator_AsyncWorkItemQueue_FirstItem);
                    }
                }

                // okay, it is new one
                // always hold onto the most recent one for the same document
                documentMap.Add(key, item);

                return true;
            }

            protected override void Dispose_NoLock()
            {
                foreach (var map in _documentWorkQueue.Values)
                {
                    foreach (var workItem in map.Values)
                    {
                        workItem.AsyncToken.Dispose();
                    }

                    SharedPools.BigDefault<Dictionary<DocumentId, UnitTestingWorkItem>>().ClearAndFree(map);
                }

                _documentWorkQueue.Clear();
            }
        }
    }
}
