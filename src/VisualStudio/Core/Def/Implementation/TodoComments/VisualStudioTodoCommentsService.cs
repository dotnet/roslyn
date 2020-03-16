// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComments
{
    internal class VisualStudioTodoCommentsService
        : ForegroundThreadAffinitizedObject, ITodoCommentsService, ITodoCommentsServiceCallback, ITodoListProvider
    {
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly EventListenerTracker<ITodoListProvider> _eventListenerTracker;

        private readonly ConditionalWeakTable<DocumentId, StrongBox<ImmutableArray<TodoCommentInfo>>> _documentToInfos
            = new ConditionalWeakTable<DocumentId, StrongBox<ImmutableArray<TodoCommentInfo>>>();

        /// <summary>
        /// Our connections to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private KeepAliveSession? _keepAliveSession;

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private AsyncBatchingWorkQueue<TodoCommentInfo> _workQueue = null!;

        public event EventHandler<TodoItemsUpdatedArgs>? TodoListUpdated;

        public VisualStudioTodoCommentsService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext,
            EventListenerTracker<ITodoListProvider> eventListenerTracker)
            : base(threadingContext)
        {
            _workspace = workspace;
            _eventListenerTracker = eventListenerTracker;
        }

        void ITodoCommentsService.Start(CancellationToken cancellationToken)
            => _ = StartAsync(cancellationToken);

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                await StartWorkerAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task StartWorkerAsync(CancellationToken cancellationToken)
        {
            _workQueue = new AsyncBatchingWorkQueue<TodoCommentInfo>(
                TimeSpan.FromSeconds(1),
                ProcessTodoCommentInfosAsync,
                cancellationToken);

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _keepAliveSession = await client.TryCreateKeepAliveSessionAsync(
                WellKnownServiceHubServices.RemoteTodoCommentsService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);
            if (_keepAliveSession == null)
                return;

            // Now that we've started, let the VS todo list know to start listening to us
            _eventListenerTracker.EnsureEventListener(_workspace, this);

            // Now kick off scanning in the OOP process.
            var success = await _keepAliveSession.TryInvokeAsync(
                nameof(IRemoteTodoCommentsService.ComputeTodoCommentsAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public Task ReportTodoCommentsAsync(List<TodoCommentInfo> infos, CancellationToken cancellationToken)
        {
            _workQueue.AddWork(infos);
            return Task.CompletedTask;
        }

        private Task ProcessTodoCommentInfosAsync(
            ImmutableArray<TodoCommentInfo> infos, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<TodoCommentInfo>.GetInstance(out var filteredInfos);
            AddFilteredInfos(infos, filteredInfos);

            foreach (var group in filteredInfos.GroupBy(i => i.DocumentId))
            {
                var documentId = group.Key;
                var documentInfos = group.ToImmutableArray();

                // only one thread can be executing ProcessTodoCommentInfosAsync at a time,
                // so it's safe to remove/add here.
                _documentToInfos.Remove(documentId);
                _documentToInfos.Add(documentId, new StrongBox<ImmutableArray<TodoCommentInfo>>(documentInfos));

                this.TodoListUpdated?.Invoke(
                    this, new TodoItemsUpdatedArgs(
                        documentId, _workspace, _workspace.CurrentSolution,
                        documentId.ProjectId, documentId, documentInfos));
            }

            return Task.CompletedTask;
        }

        private void AddFilteredInfos(ImmutableArray<TodoCommentInfo> infos, ArrayBuilder<TodoCommentInfo> filteredInfos)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenProjectIds);

            // Walk the list of telemetry items in reverse, and skip any items for a document once
            // we've already seen it once.  That way, we're only reporting the most up to date
            // information for a document, and we're skipping the stale information.
            for (var i = infos.Length - 1; i >= 0; i--)
            {
                var info = infos[i];
                if (seenProjectIds.Add(info.DocumentId))
                    filteredInfos.Add(info);
            }
        }

        public ImmutableArray<TodoCommentInfo> GetTodoItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            return _documentToInfos.TryGetValue(documentId, out var values)
                ? values.Value
                : ImmutableArray<TodoCommentInfo>.Empty;
        }

        public IEnumerable<UpdatedEventArgs> GetTodoItemsUpdatedEventArgs(
            Workspace workspace, CancellationToken cancellationToken)
        {
            // Don't need to implement this.  OOP pushes all items over to VS.  So there's no need
            return SpecializedCollections.EmptyEnumerable<UpdatedEventArgs>();
        }
    }
}
