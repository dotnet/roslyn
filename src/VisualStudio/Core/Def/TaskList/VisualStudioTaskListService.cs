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
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using TaskListItem = Microsoft.CodeAnalysis.TaskList.TaskListItem;

namespace Microsoft.VisualStudio.LanguageServices.TaskList
{
    [Export(typeof(VisualStudioTaskListService)), Shared]
    internal class VisualStudioTaskListService : ITaskListProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly ITableManagerProvider _tableManagerProvider;
        private readonly IAsyncServiceProvider _asyncServiceProvider;
        private readonly TaskListListener _listener;

        public event EventHandler<TaskListUpdatedArgs>? TaskListUpdated;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTaskListService(
            IThreadingContext threadingContext,
            VisualStudioWorkspaceImpl workspace,
            IGlobalOptionService globalOptions,
            ITableManagerProvider tableManagerProvider,
            SVsServiceProvider asyncServiceProvider,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _tableManagerProvider = tableManagerProvider;
            _asyncServiceProvider = (IAsyncServiceProvider)asyncServiceProvider;

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

        public void Start(VisualStudioWorkspace workspace)
        {
            // Fire and forget.
            _ = StartAsync(workspace);
        }

        private async Task StartAsync(VisualStudioWorkspace workspace)
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
                // when the user would not even see the results.  When we actually do register the analyzer (in
                // _listener.Start below), solution-crawler will reanalyze everything with this analyzer, so it will
                // still find and present all the relevant items to the user.
                await WaitUntilTaskListActivatedAsync().ConfigureAwait(false);

                // Now that we've started, create the actual VS todo list and have them hookup to us.
                _ = new VisualStudioTaskListTable(workspace, _threadingContext, _tableManagerProvider, this);

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
