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

namespace Microsoft.CodeAnalysis.LanguageServer.Features.TaskList
{
    internal sealed class TaskListListener : ITaskListListener, IDisposable
    {
        private readonly CancellationToken _disposalToken;
        private readonly IGlobalOptionService _globalOptions;
        private readonly SolutionServices _services;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly Action<DocumentId, ImmutableArray<TaskListItem>, ImmutableArray<TaskListItem>> _onTaskListItemsUpdated;
        private readonly ConcurrentDictionary<DocumentId, ImmutableArray<TaskListItem>> _documentToTaskListItems = new();

        /// <summary>
        /// Remote service connection. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private RemoteServiceConnection<IRemoteTaskListService>? _lazyConnection;

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)> _workQueue;

        public TaskListListener(
            IGlobalOptionService globalOptions,
            SolutionServices services,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            Action<DocumentId, ImmutableArray<TaskListItem>, ImmutableArray<TaskListItem>> onTodoCommentsUpdated,
            CancellationToken disposalToken)
        {
            _globalOptions = globalOptions;
            _services = services;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.TaskList);
            _onTaskListItemsUpdated = onTodoCommentsUpdated;
            _disposalToken = disposalToken;

            _workQueue = new AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)>(
                TimeSpan.FromSeconds(1),
                ProcessTodoCommentInfosAsync,
                _asyncListener,
                _disposalToken);
        }

        public void Dispose()
        {
            _globalOptions.OptionChanged -= GlobalOptionChanged;

            var connection = _lazyConnection;
            _lazyConnection = null;

            connection?.Dispose();
        }

        public async ValueTask StartAsync()
        {
            // Should only be started once.
            Contract.ThrowIfTrue(_lazyConnection != null);

            // If we're in pull-diagnostics mode, then todo-comments will be handled by LSP.
            var diagnosticMode = _globalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode);
            if (diagnosticMode == DiagnosticMode.Pull)
                return;

            var cancellationToken = _disposalToken;

            var client = await RemoteHostClient.TryGetClientAsync(_services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                ComputeTodoCommentsInCurrentProcess(cancellationToken);
                return;
            }

            _globalOptions.OptionChanged += GlobalOptionChanged;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // todo comments it will call back into us to notify VS about it.
            _lazyConnection = client.CreateConnection<IRemoteTaskListService>(callbackTarget: this);

            // Now kick off scanning in the OOP process.
            // If the call fails an error has already been reported and there is nothing more to do.
            _ = await _lazyConnection.TryInvokeAsync(
                (service, callbackId, cancellationToken) => service.ComputeTaskListItemsAsync(callbackId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        private void ComputeTodoCommentsInCurrentProcess(CancellationToken cancellationToken)
        {
            var registrationService = _services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            var analyzerProvider = new InProcTaskListIncrementalAnalyzerProvider(this);

            registrationService.AddAnalyzerProvider(
                analyzerProvider,
                new IncrementalAnalyzerProviderMetadata(
                    nameof(InProcTaskListIncrementalAnalyzerProvider),
                    highPriorityForActiveFile: false,
                    workspaceKinds: WorkspaceKind.Host));
        }

        private void GlobalOptionChanged(object? sender, OptionChangedEventArgs e)
        {
            // Notify remote service that TokenList changed and the solution needs to be re-analyzed:
            if (e.Option == TaskListOptionsStorage.Descriptors && _lazyConnection != null)
            {
                // only perform the call if connection has not been disposed:
                _ = Task.Run(() => _lazyConnection?.TryInvokeAsync((service, cancellationToken) => service.ReanalyzeAsync(cancellationToken), _disposalToken))
                    .ReportNonFatalErrorUnlessCancelledAsync(_disposalToken);
            }
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public ValueTask ReportTaskListItemsAsync(DocumentId documentId, ImmutableArray<TaskListItem> infos, CancellationToken cancellationToken)
        {
            try
            {
                _workQueue.AddWork((documentId, infos));
                return ValueTaskFactory.CompletedTask;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                // report NFW before returning back to the remote process
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public ValueTask<TaskListOptions> GetOptionsAsync(CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_globalOptions.GetTaskListOptions());

        private ValueTask ProcessTodoCommentInfosAsync(
            ImmutableSegmentedList<(DocumentId documentId, ImmutableArray<TaskListItem> items)> docAndCommentsArray, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<(DocumentId documentId, ImmutableArray<TaskListItem> items)>.GetInstance(out var filteredArray);
            AddFilteredInfos(docAndCommentsArray, filteredArray);

            foreach (var (documentId, newComments) in filteredArray)
            {
                var oldComments = _documentToTaskListItems.TryGetValue(documentId, out var oldBoxedInfos)
                    ? oldBoxedInfos
                    : ImmutableArray<TaskListItem>.Empty;

                // only one thread can be executing ProcessTodoCommentInfosAsync at a time,
                // so it's safe to remove/add here.
                if (newComments.IsEmpty)
                {
                    _documentToTaskListItems.TryRemove(documentId, out _);
                }
                else
                {
                    _documentToTaskListItems[documentId] = newComments;
                }

                // If we have someone listening for updates, and our new items are different from
                // our old ones, then notify them of the change.
                _onTaskListItemsUpdated(documentId, oldComments, newComments);
            }

            return ValueTaskFactory.CompletedTask;
        }

        private static void AddFilteredInfos(
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

        public ImmutableArray<TaskListItem> GetTodoItems(DocumentId documentId)
        {
            return _documentToTaskListItems.TryGetValue(documentId, out var values)
                ? values
                : ImmutableArray<TaskListItem>.Empty;
        }
    }
}
