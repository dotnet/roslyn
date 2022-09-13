﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Features.TaskList;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.TaskList
{
    [Export(typeof(IVsTypeScriptTodoCommentService))]
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioTaskListService :
        ITaskListProvider,
        IVsTypeScriptTodoCommentService,
        IEventListener<object>,
        IDisposable
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly EventListenerTracker<ITaskListProvider> _eventListenerTracker;
        private readonly TaskListListener _listener;

        public event EventHandler<TaskListUpdatedArgs>? TaskListUpdated;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTaskListService(
            IThreadingContext threadingContext,
            VisualStudioWorkspaceImpl workspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _eventListenerTracker = new EventListenerTracker<ITaskListProvider>(eventListeners, WellKnownEventListeners.TaskListProvider);

            _listener = new TaskListListener(
                globalOptions,
                workspace.Services.SolutionServices,
                asynchronousOperationListenerProvider,
                onTaskListItemsUpdated: (documentId, oldComments, newComments) =>
                {
                    if (TaskListUpdated != null && !oldComments.SequenceEqual(newComments))
                        TaskListUpdated?.Invoke(this, new TaskListUpdatedArgs(documentId, _workspace.CurrentSolution, documentId, newComments));
                },
                threadingContext.DisposalToken);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        void IEventListener<object>.StartListening(Workspace workspace, object _)
        {
            if (workspace is VisualStudioWorkspace)
                _ = StartAsync(workspace);
        }

        private async Task StartAsync(Workspace workspace)
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                // Don't bother doing anything until the workspace has actually loaded.  We don't want to add to any
                // startup costs by doing work too early.
                var workspaceStatus = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                await workspaceStatus.WaitUntilFullyLoadedAsync(_threadingContext.DisposalToken).ConfigureAwait(false);

                // Now that we've started, let the VS todo list know to start listening to us
                _eventListenerTracker.EnsureEventListener(_workspace, this);

                await _listener.StartAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        /// <inheritdoc cref="IVsTypeScriptTodoCommentService.ReportTodoCommentsAsync(Document, ImmutableArray{TodoComment}, CancellationToken)"/>
        async Task IVsTypeScriptTodoCommentService.ReportTodoCommentsAsync(
            Document document, ImmutableArray<TodoComment> todoComments, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TaskListItem>.GetInstance(out var converted);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            await TodoComment.ConvertAsync(document, todoComments, converted, cancellationToken).ConfigureAwait(false);

            await _listener.ReportTaskListItemsAsync(
                document.Id, converted.ToImmutable(), cancellationToken).ConfigureAwait(false);
        }

        public ImmutableArray<TaskListItem> GetTaskListItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            => _listener.GetTodoItems(documentId);
    }
}
