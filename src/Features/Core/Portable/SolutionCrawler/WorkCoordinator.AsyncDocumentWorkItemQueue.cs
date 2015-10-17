// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private class AsyncDocumentWorkItemQueue : AsyncWorkItemQueue<DocumentId>
            {
                private readonly Dictionary<ProjectId, Dictionary<DocumentId, WorkItem>> _documentWorkQueue = new Dictionary<ProjectId, Dictionary<DocumentId, WorkItem>>();

                public AsyncDocumentWorkItemQueue(SolutionCrawlerProgressReporter progressReporter) :
                    base(progressReporter)
                {
                }

                protected override int WorkItemCount_NoLock
                {
                    get
                    {
                        return _documentWorkQueue.Count;
                    }
                }

                protected override bool TryTake_NoLock(DocumentId key, out WorkItem workInfo)
                {
                    workInfo = default(WorkItem);

                    var documentMap = default(Dictionary<DocumentId, WorkItem>);
                    if (_documentWorkQueue.TryGetValue(key.ProjectId, out documentMap) &&
                        documentMap.TryGetValue(key, out workInfo))
                    {
                        documentMap.Remove(key);

                        if (documentMap.Count == 0)
                        {
                            _documentWorkQueue.Remove(key.ProjectId);
                            SharedPools.BigDefault<Dictionary<DocumentId, WorkItem>>().ClearAndFree(documentMap);
                        }

                        return true;
                    }

                    return false;
                }

                protected override bool TryTakeAnyWork_NoLock(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph, out WorkItem workItem)
                {
                    // there must be at least one item in the map when this is called unless host is shutting down.
                    if (_documentWorkQueue.Count == 0)
                    {
                        workItem = default(WorkItem);
                        return false;
                    }

                    var documentId = GetBestDocumentId_NoLock(preferableProjectId, dependencyGraph);
                    if (TryTake_NoLock(documentId, out workItem))
                    {
                        return true;
                    }

                    return Contract.FailWithReturn<bool>("how?");
                }

                private DocumentId GetBestDocumentId_NoLock(ProjectId preferableProjectId, ProjectDependencyGraph dependencyGraph)
                {
                    var projectId = GetBestProjectId_NoLock(preferableProjectId, dependencyGraph);

                    var documentMap = _documentWorkQueue[projectId];

                    // explicitly iterate so that we can use struct enumerator.
                    // Return the first normal priority work item we find.  If we don't
                    // find any, then just return the first low prio item we saw.
                    DocumentId lowPriorityDocumentId = null;
                    foreach (var pair in documentMap)
                    {
                        var workItem = pair.Value;
                        if (workItem.IsLowPriority)
                        {
                            lowPriorityDocumentId = pair.Key;
                        }
                        else
                        {
                            return pair.Key;
                        }
                    }

                    Contract.ThrowIfNull(lowPriorityDocumentId);
                    return lowPriorityDocumentId;
                }

                private ProjectId GetBestProjectId_NoLock(ProjectId projectId, ProjectDependencyGraph dependencyGraph)
                {
                    if (projectId != null)
                    {
                        if (_documentWorkQueue.ContainsKey(projectId))
                        {
                            return projectId;
                        }

                        // see if there is any project that depends on this project has work item queued. if there is any, use that project
                        // as next project to process
                        foreach (var dependingProjectId in dependencyGraph.GetProjectsThatDirectlyDependOnThisProject(projectId))
                        {
                            if (_documentWorkQueue.ContainsKey(dependingProjectId))
                            {
                                return dependingProjectId;
                            }
                        }
                    }

                    // explicitly iterate so that we can use struct enumerator
                    foreach (var pair in _documentWorkQueue)
                    {
                        return pair.Key;
                    }

                    return Contract.FailWithReturn<ProjectId>("Shouldn't reach here");
                }

                protected override bool AddOrReplace_NoLock(WorkItem item)
                {
                    // now document work
                    var existingWorkItem = default(WorkItem);
                    Cancel_NoLock(item.DocumentId);

                    // see whether we need to update
                    var key = item.DocumentId;
                    var documentMap = default(Dictionary<DocumentId, WorkItem>);
                    if (_documentWorkQueue.TryGetValue(key.ProjectId, out documentMap) &&
                        documentMap.TryGetValue(key, out existingWorkItem))
                    {
                        // TODO: should I care about language when replace it?
                        Contract.Requires(existingWorkItem.Language == item.Language);

                        // replace it
                        documentMap[key] = existingWorkItem.With(item.InvocationReasons, item.ActiveMember, item.Analyzers, item.IsRetry, item.AsyncToken);
                        return false;
                    }

                    // add document map if it is not already there
                    if (documentMap == null)
                    {
                        documentMap = SharedPools.BigDefault<Dictionary<DocumentId, WorkItem>>().AllocateAndClear();
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

                        SharedPools.BigDefault<Dictionary<DocumentId, WorkItem>>().ClearAndFree(map);
                    }

                    _documentWorkQueue.Clear();
                }
            }
        }
    }
}
