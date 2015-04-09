// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal abstract partial class AbstractVisualStudioErrorTaskList : AbstractVisualStudioTaskList
    {
        // use predefined tagger delay constant for error list reporting threshold
        private const int ReportingThresholdInMS = TaggerConstants.ShortDelay;

        // this is an arbitary number I chose. we should update this number as we dogfood and 
        // decide what would be appropriate number of errors to show users.
        private const int ErrorReportThreshold = 416;

        private static readonly VisualStudioTaskItem[] s_noItems = Array.Empty<VisualStudioTaskItem>();

        private readonly SimpleTaskQueue _taskQueue;
        private readonly VisualStudioWorkspace _workspace;

        private readonly object _gate;
        private readonly IDocumentTrackingService _documentTracker;

        private readonly Dictionary<object, VisualStudioTaskItem[]> _reportedItemsMap;
        private readonly Dictionary<ProjectId, Dictionary<object, VisualStudioTaskItem[]>> _notReportedProjectItemsMap;
        private readonly Dictionary<DocumentId, Dictionary<object, VisualStudioTaskItem[]>> _notReportedDocumentItemMap;

        // opened files set
        private readonly HashSet<DocumentId> _openedFiles;

        // cached temporary set that will be used for various temporary operation
        private readonly HashSet<object> _inProcessSet;

        // time stamp on last time new item is added or removed
        private int _lastNewItemAddedOrRemoved;
        private int _lastReported;

        // indicate whether we are still reporting errors to error list
        private bool _reportRequestRunning;

        // number of items reported to vs error list
        private int _reportedCount;

        protected AbstractVisualStudioErrorTaskList(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IForegroundNotificationService notificationService,
            IDiagnosticService diagnosticService,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
            base(serviceProvider, notificationService, FeatureAttribute.ErrorList, asyncListeners)
        {
            _gate = new object();

            _workspace = workspace;

            // we should have document tracking service in visual studio host
            _documentTracker = _workspace.Services.GetService<IDocumentTrackingService>();
            Contract.ThrowIfNull(_documentTracker);

            _taskQueue = new SimpleTaskQueue(TaskScheduler.Default);

            _reportedItemsMap = new Dictionary<object, VisualStudioTaskItem[]>();
            _notReportedProjectItemsMap = new Dictionary<ProjectId, Dictionary<object, VisualStudioTaskItem[]>>();
            _notReportedDocumentItemMap = new Dictionary<DocumentId, Dictionary<object, VisualStudioTaskItem[]>>();

            _openedFiles = new HashSet<DocumentId>();
            _inProcessSet = new HashSet<object>();

            _lastNewItemAddedOrRemoved = Environment.TickCount;
            _lastReported = Environment.TickCount;

            _reportRequestRunning = false;
            _reportedCount = 0;

            if (ErrorListInstalled)
            {
                return;
            }

            // this should be called after all fields are initialized
            InitializeTaskList();
            diagnosticService.DiagnosticsUpdated += this.OnDiagnosticUpdated;
        }

        public override int EnumTaskItems(out IVsEnumTaskItems enumTaskItems)
        {
            // this will be called at the end of build.
            // return real enumerator, rather than calling async refresh
            var items = GetAllReportedTaskItems();

            enumTaskItems = new TaskItemsEnum<VisualStudioTaskItem>(items);
            return VSConstants.S_OK;
        }

        private VisualStudioTaskItem[] GetAllReportedTaskItems()
        {
            lock (_gate)
            {
                // return sorted list
                var list = _reportedItemsMap.Values.SelectMany(i => i).ToArray();
                Array.Sort(list);

                return list;
            }
        }

        private void OnDiagnosticUpdated(object sender, DiagnosticsUpdatedArgs args)
        {
            if (args.Workspace != _workspace)
            {
                return;
            }

            var asyncToken = this.Listener.BeginAsyncOperation("OnTaskListUpdated");
            _taskQueue.ScheduleTask(
                () => this.Enqueue(
                    args.Id, args.ProjectId, args.DocumentId,
                        args.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden).Select(d => new DiagnosticTaskItem(d)))).CompletesAsyncOperation(asyncToken);
        }

        private bool HasPendingTaskItemsToReport
        {
            get
            {
                return _notReportedDocumentItemMap.Count > 0 || _notReportedProjectItemsMap.Count > 0;
            }
        }

        private bool IsUnderThreshold
        {
            get
            {
                return _reportedCount < ErrorReportThreshold;
            }
        }

        private bool HasPendingOpenDocumentTaskItemsToReport
        {
            get
            {
                return _openedFiles.Count > 0;
            }
        }

        public void Enqueue(object key, ProjectId projectId, DocumentId documentId, IEnumerable<ITaskItem> items)
        {
            lock (_gate)
            {
                var newItems = CreateVisualStudioTaskItems(items);
                var existingItems = GetExistingVisualStudioTaskItems(key);

                var hasNewItems = newItems != null;
                var hasExistingItems = existingItems != null || HasPendingVisualStudioTaskItems(key, projectId, documentId);

                // track items from opened files
                if (documentId != null && _workspace.IsDocumentOpen(documentId))
                {
                    _openedFiles.Add(documentId);
                }

                // nothing to do
                if (!hasNewItems && !hasExistingItems)
                {
                    return;
                }

                // handle 3 operations.
                // 1. delete
                if (!hasNewItems && hasExistingItems)
                {
                    RemoveExistingTaskItems_NoLock(key, projectId, documentId, existingItems);
                    ReportPendingTaskItems_NoLock();
                    return;
                }

                // 2. insert
                if (hasNewItems && !hasExistingItems)
                {
                    EnqueuePendingTaskItems_NoLock(key, projectId, documentId, newItems);
                    ReportPendingTaskItems_NoLock();
                    return;
                }

                // 3. update
                Contract.Requires(hasNewItems && hasExistingItems);
                EnqueueUpdate_NoLock(key, projectId, documentId, newItems, existingItems);
            }
        }

        private bool HasPendingVisualStudioTaskItems(object key, ProjectId projectId, DocumentId documentId)
        {
            Dictionary<object, VisualStudioTaskItem[]> taskMap;
            if (documentId != null)
            {
                return _notReportedDocumentItemMap.TryGetValue(documentId, out taskMap) && taskMap.ContainsKey(key);
            }

            if (projectId != null)
            {
                return _notReportedProjectItemsMap.TryGetValue(projectId, out taskMap) && taskMap.ContainsKey(key);
            }

            return false;
        }

        private void EnqueueUpdate_NoLock(object key, ProjectId projectId, DocumentId documentId, VisualStudioTaskItem[] newItems, VisualStudioTaskItem[] existingItems)
        {
            existingItems = existingItems ?? s_noItems;

            _inProcessSet.Clear();
            _inProcessSet.UnionWith(existingItems);
            _inProcessSet.IntersectWith(newItems);

            if (_inProcessSet.Count == 0)
            {
                // completely replaced
                RemoveExistingTaskItems_NoLock(key, projectId, documentId, existingItems);
                EnqueuePendingTaskItems_NoLock(key, projectId, documentId, newItems);

                ReportPendingTaskItems_NoLock();
                return;
            }

            if (_inProcessSet.Count == newItems.Length && existingItems.Length == newItems.Length)
            {
                // nothing has changed.
                RemovePendingTaskItems_NoLock(key, projectId, documentId);

                _inProcessSet.Clear();
                return;
            }

            if (_inProcessSet.Count == existingItems.Length)
            {
                // all existing items survived. only added items
                var itemsToInsert = GetItemsNotInSet_NoLock(newItems, _inProcessSet.Count);

                EnqueuePendingTaskItems_NoLock(key, projectId, documentId, itemsToInsert);
                ReportPendingTaskItems_NoLock();

                _inProcessSet.Clear();
                return;
            }

            if (_inProcessSet.Count == newItems.Length)
            {
                // all new items survived. only deleted items
                var itemsToDelete = GetItemsNotInSet_NoLock(existingItems, _inProcessSet.Count);

                UpdateExistingTaskItems_NoLock(key, projectId, documentId, _inProcessSet.OfType<VisualStudioTaskItem>().ToArray(), itemsToDelete);
                ReportPendingTaskItems_NoLock();

                _inProcessSet.Clear();
                return;
            }

            // part of existing items are changed
            {
                var itemsToDelete = GetItemsNotInSet_NoLock(existingItems, _inProcessSet.Count);
                var itemsToInsert = GetItemsNotInSet_NoLock(newItems, _inProcessSet.Count);

                // update only actually changed items
                UpdateExistingTaskItems_NoLock(key, projectId, documentId, _inProcessSet.OfType<VisualStudioTaskItem>().ToArray(), itemsToDelete);
                EnqueuePendingTaskItems_NoLock(key, projectId, documentId, itemsToInsert);
                ReportPendingTaskItems_NoLock();

                _inProcessSet.Clear();
            }
        }

        private VisualStudioTaskItem[] GetItemsNotInSet_NoLock(VisualStudioTaskItem[] items, int sharedCount)
        {
            var taskItems = new VisualStudioTaskItem[items.Length - sharedCount];

            var index = 0;
            for (var i = 0; i < items.Length; i++)
            {
                if (_inProcessSet.Contains(items[i]))
                {
                    continue;
                }

                taskItems[index++] = items[i];
            }

            Contract.Requires(items.Where(i => !_inProcessSet.Contains(i)).SetEquals(taskItems));
            return taskItems;
        }

        private VisualStudioTaskItem[] GetExistingVisualStudioTaskItems(object key)
        {
            VisualStudioTaskItem[] reported;
            if (!_reportedItemsMap.TryGetValue(key, out reported))
            {
                return null;
            }

            // everything should be in sorted form
            ValidateSorted(reported);
            return reported;
        }

        private void ReportPendingTaskItems_NoLock()
        {
            _lastNewItemAddedOrRemoved = Environment.TickCount;

            if (!this.HasPendingTaskItemsToReport || _reportRequestRunning)
            {
                return;
            }

            // no task items for opened files and we are over threshold
            if (!this.HasPendingOpenDocumentTaskItemsToReport && !this.IsUnderThreshold)
            {
                return;
            }

            RegisterNotificationForAddedItems_NoLock();
        }

        private void RegisterNotificationForAddedItems_NoLock()
        {
            this.NotificationService.RegisterNotification(() =>
            {
                lock (_gate)
                {
                    _reportRequestRunning = true;

                    // when there is high activity on task items, delay reporting it
                    // but, do not hold it too long
                    var current = Environment.TickCount;
                    if (current - _lastNewItemAddedOrRemoved < ReportingThresholdInMS &&
                        current - _lastReported < ReportingThresholdInMS * 4)
                    {
                        RegisterNotificationForAddedItems_NoLock();
                        return false;
                    }

                    _lastReported = current;

                    ValueTuple<object, VisualStudioTaskItem[]> bestItemToReport;
                    if (!TryGetNextBestItemToReport_NoLock(out bestItemToReport))
                    {
                        return _reportRequestRunning = false;
                    }

                    var existingItems = GetExistingVisualStudioTaskItems(bestItemToReport.Item1);
                    ValidateSorted(bestItemToReport.Item2);
                    ValidateSorted(existingItems);

                    VisualStudioTaskItem[] itemsToReport;
                    if (existingItems == null)
                    {
                        itemsToReport = bestItemToReport.Item2;
                        _reportedItemsMap[bestItemToReport.Item1] = bestItemToReport.Item2;
                    }
                    else
                    {
                        itemsToReport = existingItems.Concat(bestItemToReport.Item2).ToArray();
                        Array.Sort(itemsToReport);

                        _reportedItemsMap[bestItemToReport.Item1] = itemsToReport;
                    }

                    _reportedCount += bestItemToReport.Item2.Length;

                    Contract.Requires(_reportedCount >= 0);
                    Contract.Requires(_reportedCount == _reportedItemsMap.Values.Sum(a => a.Length));

                    this.RefreshOrAddTasks(itemsToReport);

                    return _reportRequestRunning = (this.IsUnderThreshold && this.HasPendingTaskItemsToReport) || this.HasPendingOpenDocumentTaskItemsToReport;
                }
            }, ReportingThresholdInMS, this.Listener.BeginAsyncOperation("TaskItemQueue_ReportItem"));
        }

        private bool TryGetNextBestItemToReport_NoLock(out ValueTuple<object, VisualStudioTaskItem[]> bestItemToReport)
        {
            bestItemToReport = default(ValueTuple<object, VisualStudioTaskItem[]>);

            // no item to report
            if (!this.HasPendingTaskItemsToReport)
            {
                return false;
            }

            // order of process is like this
            // 1. active document
            var activeDocumentId = _documentTracker.GetActiveDocument();
            if (activeDocumentId != null &&
                TryGetNextBestItemToReport_NoLock(_notReportedDocumentItemMap, activeDocumentId, out bestItemToReport))
            {
                return true;
            }

            // 2. visible documents
            foreach (var visibleDocumentId in _documentTracker.GetVisibleDocuments())
            {
                if (TryGetNextBestItemToReport_NoLock(_notReportedDocumentItemMap, visibleDocumentId, out bestItemToReport))
                {
                    return true;
                }
            }

            // 3. opened documents
            if (TryGetNextBestItemToReportFromOpenedFiles_NoLock(out bestItemToReport))
            {
                return true;
            }

            if (!this.IsUnderThreshold)
            {
                return false;
            }

            // 4. documents in the project where active document is in
            if (activeDocumentId != null)
            {
                if (TryGetNextBestItemToReport_NoLock(
                        _notReportedDocumentItemMap, keys => keys.FirstOrDefault(k => k.ProjectId == activeDocumentId.ProjectId),
                        (s, k) => s.ContainsDocument(k), out bestItemToReport))
                {
                    return true;
                }
            }

            // just take random one from the document map
            if (_notReportedDocumentItemMap.Count > 0)
            {
                if (TryGetNextBestItemToReport_NoLock(_notReportedDocumentItemMap, keys => keys.FirstOrDefault(), (s, k) => s.ContainsDocument(k), out bestItemToReport))
                {
                    return true;
                }
            }

            if (_notReportedProjectItemsMap.Count > 0)
            {
                if (TryGetNextBestItemToReport_NoLock(_notReportedProjectItemsMap, keys => keys.FirstOrDefault(), (s, k) => s.ContainsProject(k), out bestItemToReport))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetNextBestItemToReportFromOpenedFiles_NoLock(out ValueTuple<object, VisualStudioTaskItem[]> bestItemToReport)
        {
            bestItemToReport = default(ValueTuple<object, VisualStudioTaskItem[]>);

            if (!this.HasPendingOpenDocumentTaskItemsToReport)
            {
                return false;
            }

            _inProcessSet.Clear();

            var result = false;
            foreach (var openedDocumentId in _openedFiles)
            {
                if (!_workspace.CurrentSolution.ContainsDocument(openedDocumentId))
                {
                    _notReportedDocumentItemMap.Remove(openedDocumentId);
                }

                if (TryGetNextBestItemToReport_NoLock(_notReportedDocumentItemMap, openedDocumentId, out bestItemToReport))
                {
                    result = true;
                    break;
                }

                _inProcessSet.Add(openedDocumentId);
            }

            if (_inProcessSet.Count > 0)
            {
                _openedFiles.RemoveAll(_inProcessSet.OfType<DocumentId>());
                _inProcessSet.Clear();
            }

            if (!this.HasPendingTaskItemsToReport)
            {
                _openedFiles.Clear();
            }

            return result;
        }

        private bool TryGetNextBestItemToReport_NoLock<T>(
            Dictionary<T, Dictionary<object, VisualStudioTaskItem[]>> map,
            Func<IEnumerable<T>, T> key,
            Func<Solution, T, bool> predicate,
            out ValueTuple<object, VisualStudioTaskItem[]> bestItemToReport) where T : class
        {
            bestItemToReport = default(ValueTuple<object, VisualStudioTaskItem[]>);

            while (true)
            {
                var id = key(map.Keys);
                if (id == null)
                {
                    break;
                }

                if (!predicate(_workspace.CurrentSolution, id))
                {
                    map.Remove(id);
                    continue;
                }

                if (TryGetNextBestItemToReport_NoLock(map, id, out bestItemToReport))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetNextBestItemToReport_NoLock<T>(
            Dictionary<T, Dictionary<object, VisualStudioTaskItem[]>> map, T key,
            out ValueTuple<object, VisualStudioTaskItem[]> bestItemToReport)
        {
            bestItemToReport = default(ValueTuple<object, VisualStudioTaskItem[]>);

            Dictionary<object, VisualStudioTaskItem[]> taskMap;
            if (!map.TryGetValue(key, out taskMap))
            {
                return false;
            }

            Contract.Requires(taskMap.Count > 0);
            foreach (var first in taskMap)
            {
                bestItemToReport = ValueTuple.Create(first.Key, first.Value);
                ValidateSorted(bestItemToReport.Item2);
                break;
            }

            taskMap.Remove(bestItemToReport.Item1);
            if (taskMap.Count == 0)
            {
                map.Remove(key);
            }

            return true;
        }

        private void EnqueuePendingTaskItems_NoLock(object key, ProjectId projectId, DocumentId documentId, VisualStudioTaskItem[] taskItems)
        {
            if (taskItems.Length == 0)
            {
                return;
            }

            var first = taskItems[0];
            Contract.Requires(projectId == first.Info.ProjectId);
            Contract.Requires(documentId == first.Info.DocumentId);

            if (documentId != null)
            {
                EnqueuePendingTaskItemsToMap_NoLock(_notReportedDocumentItemMap, documentId, key, taskItems);
                return;
            }

            if (projectId != null)
            {
                EnqueuePendingTaskItemsToMap_NoLock(_notReportedProjectItemsMap, projectId, key, taskItems);
                return;
            }

            // assert and ignore items that have neither document id or project id
            Contract.Requires(false, "how this can happen?");
        }

        private void EnqueuePendingTaskItemsToMap_NoLock<T>(
            Dictionary<T, Dictionary<object, VisualStudioTaskItem[]>> map, T mapKey, object taskKey, VisualStudioTaskItem[] taskItems)
        {
            var taskMap = map.GetOrAdd(mapKey, _ => new Dictionary<object, VisualStudioTaskItem[]>());

            Array.Sort(taskItems);
            taskMap[taskKey] = taskItems;
        }

        private void RemovePendingTaskItems_NoLock(object key, ProjectId projectId, DocumentId documentId)
        {
            if (documentId != null)
            {
                // check not reported item in document
                RemovePendingTaskItemsFromMap_NoLock(_notReportedDocumentItemMap, documentId, key);
                return;
            }

            if (projectId != null)
            {
                // check not reported item in project
                RemovePendingTaskItemsFromMap_NoLock(_notReportedProjectItemsMap, projectId, key);
                return;
            }

            Contract.Requires(false, "should have at least one of them");
        }

        private void RemovePendingTaskItemsFromMap_NoLock<T>(Dictionary<T, Dictionary<object, VisualStudioTaskItem[]>> map, T mapKey, object taskKey)
        {
            // remove task item if we found one
            Dictionary<object, VisualStudioTaskItem[]> taskMap;
            if (!map.TryGetValue(mapKey, out taskMap))
            {
                return;
            }

            if (!taskMap.Remove(taskKey))
            {
                return;
            }

            if (taskMap.Count > 0)
            {
                return;
            }

            map.Remove(mapKey);
        }

        private void RemoveReportedTaskItems_NoLock(object key, VisualStudioTaskItem[] existingItems)
        {
            if (existingItems == null)
            {
                return;
            }

            _reportedCount -= existingItems.Length;
            _reportedItemsMap.Remove(key);

            Contract.Requires(_reportedCount >= 0);
            Contract.Requires(_reportedCount == _reportedItemsMap.Values.Sum(a => a.Length));

            RegisterNotificationForDeleteItems_NoLock(existingItems);
        }

        private void UpdateReportedTaskItems_NoLock(object key, VisualStudioTaskItem[] survivedItems, VisualStudioTaskItem[] itemsToDelete)
        {
            _reportedCount -= itemsToDelete.Length;

            Array.Sort(survivedItems);
            _reportedItemsMap[key] = survivedItems;

            Contract.Requires(_reportedCount >= 0);
            Contract.Requires(_reportedCount == _reportedItemsMap.Values.Sum(a => a.Length));

            RegisterNotificationForDeleteItems_NoLock(itemsToDelete);
        }

        private void RegisterNotificationForDeleteItems_NoLock(VisualStudioTaskItem[] itemsToDelete)
        {
            // if there is actually no items to delete, bail out
            if (itemsToDelete.Length == 0)
            {
                return;
            }

            this.NotificationService.RegisterNotification(() =>
            {
                lock (_gate)
                {
                    this.RemoveTasks(itemsToDelete);
                }
            }, this.Listener.BeginAsyncOperation("TaskItemQueue_RemoveItem"));
        }

        private void UpdateExistingTaskItems_NoLock(object key, ProjectId projectId, DocumentId documentId, VisualStudioTaskItem[] survivedItems, VisualStudioTaskItem[] itemsToDelete)
        {
            UpdateReportedTaskItems_NoLock(key, survivedItems, itemsToDelete);
            RemovePendingTaskItems_NoLock(key, projectId, documentId);
        }

        private void RemoveExistingTaskItems_NoLock(object key, ProjectId projectId, DocumentId documentId, VisualStudioTaskItem[] existingItems)
        {
            RemoveReportedTaskItems_NoLock(key, existingItems);
            RemovePendingTaskItems_NoLock(key, projectId, documentId);
        }

        private static readonly Func<IErrorTaskItem, VisualStudioTaskItem> s_createTaskItem = d => new VisualStudioTaskItem(d);

        private VisualStudioTaskItem[] CreateVisualStudioTaskItems(IEnumerable<ITaskItem> items)
        {
            if (!items.Any())
            {
                return null;
            }

            _inProcessSet.Clear();
            _inProcessSet.UnionWith(items.OfType<IErrorTaskItem>().Where(d => d.Workspace == _workspace).Select(s_createTaskItem));

            // make sure we only return unique items.
            // sometimes there can be error items with exact same content. for example, if same tokens are missing at the end of a file,
            // compiler will report same errors for all missing tokens. now in those cases, we will unify those errors to one.
            var newItems = _inProcessSet.OfType<VisualStudioTaskItem>().ToArray();
            if (newItems.Length == 0)
            {
                return null;
            }

            _inProcessSet.Clear();
            return newItems;
        }

        [Conditional("DEBUG")]
        private static void ValidateSorted(VisualStudioTaskItem[] reported)
        {
            if (reported == null)
            {
                return;
            }

            var copied = reported.ToArray();
            Array.Sort(copied);
            Contract.Requires(copied.SequenceEqual(reported));
        }
    }
}
