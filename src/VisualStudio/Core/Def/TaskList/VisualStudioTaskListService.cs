// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.TaskList;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;
using TaskListItem = Microsoft.CodeAnalysis.TaskList.TaskListItem;

namespace Microsoft.VisualStudio.LanguageServices.TaskList
{
    [Export(typeof(VisualStudioTaskListService)), Shared]
    internal class VisualStudioTaskListService : ITaskListProvider
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IGlobalOptionService _globalOptions;
        private readonly ITableManagerProvider _tableManagerProvider;
        private readonly IAsyncServiceProvider _asyncServiceProvider;

        public event EventHandler<TaskListUpdatedArgs>? TaskListUpdated;

        private readonly ConcurrentDictionary<DocumentId, ImmutableArray<TaskListItem>> _documentToTaskListItems = new();

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)> _workQueue;

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
            _globalOptions = globalOptions;
            _tableManagerProvider = tableManagerProvider;
            _asyncServiceProvider = (IAsyncServiceProvider)asyncServiceProvider;

            _workQueue = new AsyncBatchingWorkQueue<(DocumentId documentId, ImmutableArray<TaskListItem> items)>(
                TimeSpan.FromSeconds(1),
                ProcessTaskListItemsAsync,
                asynchronousOperationListenerProvider.GetListener(FeatureAttribute.TaskList),
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

                // Now that we've hooked everything up, kick off the work to actually start computing and reporting items.
                RegisterIncrementalAnalyzerAndStartComputingTaskListItems();
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
            => _documentToTaskListItems.TryGetValue(documentId, out var values)
                ? values
                : ImmutableArray<TaskListItem>.Empty;

        private void RegisterIncrementalAnalyzerAndStartComputingTaskListItems()
        {
            // If we're in pull-diagnostics mode, then todo-comments will be handled by LSP.
            var diagnosticMode = _globalOptions.GetDiagnosticMode();
            if (diagnosticMode == DiagnosticMode.LspPull)
                return;

            // Do not register if solution crawler is explicitly off.
            if (!_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
                return;

            var services = _workspace.Services.SolutionServices;
            var registrationService = services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            var analyzerProvider = new TaskListIncrementalAnalyzerProvider(_globalOptions, this);

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
                var taskListUpdated = this.TaskListUpdated;
                if (TaskListUpdated != null && !oldComments.SequenceEqual(newItems))
                    TaskListUpdated?.Invoke(this, new TaskListUpdatedArgs(documentId, _workspace.CurrentSolution, documentId, newItems));
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
    }
}
