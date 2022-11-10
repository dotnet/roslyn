// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Editor.TaskList;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TaskListItem = Microsoft.CodeAnalysis.TaskList.TaskListItem;

namespace Microsoft.VisualStudio.LanguageServices.TaskList
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioTaskListService :
        ITaskListProvider,
        IEventListener<object>
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IAsyncServiceProvider _asyncServiceProvider;
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
            SVsServiceProvider asyncServiceProvider,
            [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _asyncServiceProvider = (IAsyncServiceProvider)asyncServiceProvider;
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

                // Wait until the task list is actually visible so that we don't perform pointless work analyzing files
                // when the user would not even see the results.  When we actually do register the analyer (in
                // _listener.Start below), solution-crawler will reanalyze everything with this analayzer, so it will
                // still find and present all the relevant items to the user.
                await WaitUntilTaskListActivatedAsync().ConfigureAwait(false);

                // Now that we've started, let the VS todo list know to start listening to us
                _eventListenerTracker.EnsureEventListener(_workspace, this);

                _listener.Start();
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

        private async Task WaitUntilTaskListActivatedAsync()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var taskList = await _asyncServiceProvider.GetServiceAsync<SVsTaskList, ITaskList>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);

            var control = taskList.TableControl.Control;

            // if control is already visible, we can proceed to collect task list items.
            if (control.IsVisible)
                return;

            // otherwise, wait for it to become visible.
            var taskSource = new TaskCompletionSource<bool>();
            control.IsVisibleChanged += Control_IsVisibleChanged;

            await taskSource.Task.ConfigureAwait(false);

            return;

            void Control_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
            {
                if (control.IsVisible)
                {
                    control.IsVisibleChanged -= Control_IsVisibleChanged;
                    taskSource.TrySetResult(true);
                }
            }
        }

        public ImmutableArray<TaskListItem> GetTaskListItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            => _listener.GetTaskListItems(documentId);
    }
}
