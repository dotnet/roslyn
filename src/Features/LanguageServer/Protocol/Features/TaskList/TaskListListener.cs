// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.TaskList
{
    internal sealed class TaskListListener : ITaskListListener
    {
        private readonly CancellationToken _disposalToken;
        private readonly IGlobalOptionService _globalOptions;
        private readonly SolutionServices _services;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly Action<DocumentId, ImmutableArray<TaskListItem>, ImmutableArray<TaskListItem>> _onTaskListItemsUpdated;
        private readonly ConcurrentDictionary<DocumentId, ImmutableArray<TaskListItem>> _documentToTaskListItems = new();

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)> _workQueue;

        public TaskListListener(
            IGlobalOptionService globalOptions,
            SolutionServices services,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            Action<DocumentId, ImmutableArray<TaskListItem>, ImmutableArray<TaskListItem>> onTaskListItemsUpdated,
            CancellationToken disposalToken)
        {
            _globalOptions = globalOptions;
            _services = services;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.TaskList);
            _onTaskListItemsUpdated = onTaskListItemsUpdated;
            _disposalToken = disposalToken;

            _workQueue = new AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)>(
                TimeSpan.FromSeconds(1),
                ProcessTaskListItemsAsync,
                _asyncListener,
                _disposalToken);
        }

        public void Start()
        {
            // If we're in pull-diagnostics mode, then todo-comments will be handled by LSP.
            var diagnosticMode = _globalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode);
            if (diagnosticMode == DiagnosticMode.Pull)
                return;

            var registrationService = _services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            var analyzerProvider = new TaskListIncrementalAnalyzerProvider(this);

            registrationService.AddAnalyzerProvider(
                analyzerProvider,
                new IncrementalAnalyzerProviderMetadata(
                    nameof(TaskListIncrementalAnalyzerProvider),
                    highPriorityForActiveFile: false,
                    workspaceKinds: WorkspaceKind.Host));
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public ValueTask ReportTaskListItemsAsync(DocumentId documentId, ImmutableArray<TaskListItem> items, CancellationToken cancellationToken)
        {
            try
            {
                _workQueue.AddWork((documentId, items));
                return ValueTaskFactory.CompletedTask;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                // report NFW before returning back to the remote process
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public ValueTask<TaskListOptions> GetOptionsAsync(CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetTaskListOptions());

        private ValueTask ProcessTaskListItemsAsync(
            ImmutableSegmentedList<(DocumentId documentId, ImmutableArray<TaskListItem> items)> docAndCommentsArray, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<(DocumentId documentId, ImmutableArray<TaskListItem> items)>.GetInstance(out var filteredArray);
            AddFilteredItems(docAndCommentsArray, filteredArray);

            foreach (var (documentId, newItems) in filteredArray)
            {
                var oldComments = _documentToTaskListItems.TryGetValue(documentId, out var oldBoxedInfos)
                    ? oldBoxedInfos
                    : ImmutableArray<TaskListItem>.Empty;

                // only one thread can be executing ProcessTodoCommentInfosAsync at a time,
                // so it's safe to remove/add here.
                if (newItems.IsEmpty)
                {
                    _documentToTaskListItems.TryRemove(documentId, out _);
                }
                else
                {
                    _documentToTaskListItems[documentId] = newItems;
                }

                // If we have someone listening for updates, and our new items are different from
                // our old ones, then notify them of the change.
                _onTaskListItemsUpdated(documentId, oldComments, newItems);
            }

            return ValueTaskFactory.CompletedTask;
        }

        private static void AddFilteredItems(
            ImmutableSegmentedList<(DocumentId documentId, ImmutableArray<TaskListItem> items)> array,
            ArrayBuilder<(DocumentId documentId, ImmutableArray<TaskListItem> items)> filteredArray)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenDocumentIds);

            // Walk the list of todo comments in reverse, and skip any items for a document once
            // we've already seen it once.  That way, we're only reporting the most up to date
            // information for a document, and we're skipping the stale information.
            for (var i = array.Count - 1; i >= 0; i--)
            {
                var info = array[i];
                if (seenDocumentIds.Add(info.documentId))
                    filteredArray.Add(info);
            }
        }

        public ImmutableArray<TaskListItem> GetTaskListItems(DocumentId documentId)
        {
            return _documentToTaskListItems.TryGetValue(documentId, out var values)
                ? values
                : ImmutableArray<TaskListItem>.Empty;
        }
    }
}
