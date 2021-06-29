﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using IUIThreadOperationExecutor = Microsoft.VisualStudio.Utilities.IUIThreadOperationExecutor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    /// <summary>
    /// The controller for navigation bars.
    /// </summary>
    /// <remarks>
    /// The threading model for this class is simple: all non-static members are affinitized to the
    /// UI thread.
    /// </remarks>
    internal partial class NavigationBarController : ForegroundThreadAffinitizedObject, IDisposable
    {
        private readonly INavigationBarPresenter _presenter;
        private readonly ITextBuffer _subjectBuffer;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;

        private bool _disconnected = false;

        /// <summary>
        /// Latest model and selected items produced once <see cref="SelectItemAsync"/> completes and presents the
        /// single item to the view.  These can then be read in when the dropdown is expanded and we want to show all
        /// items.
        /// </summary>
        private (NavigationBarModel model, NavigationBarSelectedTypeAndMember selectedInfo) _latestModelAndSelectedInfo_OnlyAccessOnUIThread;

        /// <summary>
        /// The last full information we have presented. If we end up wanting to present the same thing again, we can
        /// just skip doing that as the UI will already know about this.
        /// </summary>
        private (ImmutableArray<NavigationBarProjectItem> projectItems, NavigationBarProjectItem? selectedProjectItem, NavigationBarModel model, NavigationBarSelectedTypeAndMember selectedInfo) _lastPresentedInfo;

        /// <summary>
        /// Source of events that should cause us to update the nav bar model with new information.
        /// </summary>
        private readonly ITaggerEventSource _eventSource;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Queue to batch up work to do to compute the current model.  Used so we can batch up a lot of events and only
        /// compute the model once for every batch.  The <c>bool</c> type parameter isn't used, but is provided as this
        /// type is generic.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool, NavigationBarModel> _computeModelQueue;

        /// <summary>
        /// Queue to batch up work to do to determine the selected item.  Used so we can batch up a lot of events and
        /// only compute the selected item once for every batch.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _selectItemQueue;

        public NavigationBarController(
            IThreadingContext threadingContext,
            INavigationBarPresenter presenter,
            ITextBuffer subjectBuffer,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext)
        {
            _presenter = presenter;
            _subjectBuffer = subjectBuffer;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _asyncListener = asyncListener;

            _computeModelQueue = new AsyncBatchingWorkQueue<bool, NavigationBarModel>(
                TimeSpan.FromMilliseconds(TaggerConstants.ShortDelay),
                ComputeModelAndSelectItemAsync,
                EqualityComparer<bool>.Default,
                asyncListener,
                _cancellationTokenSource.Token);

            _selectItemQueue = new AsyncBatchingWorkQueue(
                TimeSpan.FromMilliseconds(TaggerConstants.NearImmediateDelay),
                SelectItemAsync,
                asyncListener,
                _cancellationTokenSource.Token);

            presenter.CaretMoved += OnCaretMoved;
            presenter.ViewFocused += OnViewFocused;

            presenter.DropDownFocused += OnDropDownFocused;
            presenter.ItemSelected += OnItemSelected;

            // Initialize the tasks to be an empty model so we never have to deal with a null case.
            _latestModelAndSelectedInfo_OnlyAccessOnUIThread.model = new(ImmutableArray<NavigationBarItem>.Empty, itemService: null!);
            _latestModelAndSelectedInfo_OnlyAccessOnUIThread.selectedInfo = new(typeItem: null, memberItem: null);

            // Use 'compilation available' as that may produce different results from the initial 'frozen partial'
            // snapshot we use.
            _eventSource = new CompilationAvailableTaggerEventSource(
                subjectBuffer,
                asyncListener,
                // Any time an edit happens, recompute as the nav bar items may have changed.
                TaggerEventSources.OnTextChanged(subjectBuffer),
                // Switching what is the active context may change the nav bar contents.
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                // Many workspace changes may need us to change the items (like options changing, or project renaming).
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, asyncListener),
                // Once we hook this buffer up to the workspace, then we can start computing the nav bar items.
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer));
            _eventSource.Changed += OnEventSourceChanged;
            _eventSource.Connect();

            // Kick off initial work to populate the navbars
            StartModelUpdateAndSelectedItemUpdateTasks();
        }

        private void OnEventSourceChanged(object? sender, TaggerEventArgs e)
        {
            StartModelUpdateAndSelectedItemUpdateTasks();
        }

        void IDisposable.Dispose()
        {
            AssertIsForeground();

            _presenter.CaretMoved -= OnCaretMoved;
            _presenter.ViewFocused -= OnViewFocused;

            _presenter.DropDownFocused -= OnDropDownFocused;
            _presenter.ItemSelected -= OnItemSelected;

            _presenter.Disconnect();

            _eventSource.Changed -= OnEventSourceChanged;
            _eventSource.Disconnect();

            _disconnected = true;

            // Cancel off any remaining background work
            _cancellationTokenSource.Cancel();
        }

        private void StartModelUpdateAndSelectedItemUpdateTasks()
        {
            // If we disconnected already, just disregard
            if (_disconnected)
                return;

            // 'true' value is unused.  this just signals to the queue that we have work to do.
            _computeModelQueue.AddWork(true);
        }

        private void OnCaretMoved(object? sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask();
        }

        private void OnViewFocused(object? sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask();
        }

        private void OnDropDownFocused(object? sender, EventArgs e)
        {
            AssertIsForeground();

            var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return;

            // Grab and present whatever information we have at this point.
            GetProjectItems(out var projectItems, out var selectedProjectItem);
            var (model, selectedInfo) = _latestModelAndSelectedInfo_OnlyAccessOnUIThread;

            if (Equals(model, _lastPresentedInfo.model) &&
                Equals(selectedInfo, _lastPresentedInfo.selectedInfo) &&
                Equals(selectedProjectItem, _lastPresentedInfo.selectedProjectItem) &&
                projectItems.SequenceEqual(_lastPresentedInfo.projectItems))
            {
                // Nothing changed, so we can skip presenting these items.
                return;
            }

            _presenter.PresentItems(
                projectItems,
                selectedProjectItem,
                model.Types,
                selectedInfo.TypeItem,
                selectedInfo.MemberItem);

            _lastPresentedInfo = (projectItems, selectedProjectItem, model, selectedInfo);
        }

        private void GetProjectItems(out ImmutableArray<NavigationBarProjectItem> projectItems, out NavigationBarProjectItem? selectedProjectItem)
        {
            var documents = _subjectBuffer.CurrentSnapshot.GetRelatedDocumentsWithChanges();
            if (!documents.Any())
            {
                projectItems = ImmutableArray<NavigationBarProjectItem>.Empty;
                selectedProjectItem = null;
                return;
            }

            projectItems = documents.Select(d =>
                new NavigationBarProjectItem(
                    d.Project.Name,
                    d.Project.GetGlyph(),
                    workspace: d.Project.Solution.Workspace,
                    documentId: d.Id,
                    language: d.Project.Language)).OrderBy(projectItem => projectItem.Text).ToImmutableArray();

            var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            selectedProjectItem = document != null
                ? projectItems.FirstOrDefault(p => p.Text == document.Project.Name) ?? projectItems.First()
                : projectItems.First();
        }

        private void PushSelectedItemsToPresenter(NavigationBarSelectedTypeAndMember selectedItems)
        {
            AssertIsForeground();

            var oldLeft = selectedItems.TypeItem;
            var oldRight = selectedItems.MemberItem;

            NavigationBarItem? newLeft = null;
            NavigationBarItem? newRight = null;
            using var _1 = ArrayBuilder<NavigationBarItem>.GetInstance(out var listOfLeft);
            using var _2 = ArrayBuilder<NavigationBarItem>.GetInstance(out var listOfRight);

            if (oldRight != null)
            {
                newRight = new NavigationBarPresentedItem(
                    oldRight.Text, oldRight.Glyph, oldRight.TrackingSpans, oldRight.NavigationTrackingSpan, oldRight.ChildItems, oldRight.Bolded, oldRight.Grayed || selectedItems.ShowMemberItemGrayed);
                listOfRight.Add(newRight);
            }

            if (oldLeft != null)
            {
                newLeft = new NavigationBarPresentedItem(
                    oldLeft.Text, oldLeft.Glyph, oldLeft.TrackingSpans, oldLeft.NavigationTrackingSpan, listOfRight.ToImmutable(), oldLeft.Bolded, oldLeft.Grayed || selectedItems.ShowTypeItemGrayed);
                listOfLeft.Add(newLeft);
            }

            GetProjectItems(out var projectItems, out var selectedProjectItem);

            _presenter.PresentItems(
                projectItems,
                selectedProjectItem,
                listOfLeft.ToImmutable(),
                newLeft,
                newRight);
        }

        private void OnItemSelected(object? sender, NavigationBarItemSelectedEventArgs e)
        {
            AssertIsForeground();
            var token = _asyncListener.BeginAsyncOperation(nameof(OnItemSelected));
            var task = OnItemSelectedAsync(e.Item);
            _ = task.CompletesAsyncOperation(token);
        }

        private async Task OnItemSelectedAsync(NavigationBarItem item)
        {
            AssertIsForeground();
            using var waitContext = _uiThreadOperationExecutor.BeginExecute(
                EditorFeaturesResources.Navigation_Bars,
                EditorFeaturesResources.Refreshing_navigation_bars,
                allowCancellation: true,
                showProgress: false);

            try
            {
                await ProcessItemSelectionAsync(item, waitContext.UserCancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
            }
        }

        /// <summary>
        /// Process the selection of an item synchronously inside a wait context.
        /// </summary>
        /// <param name="item">The selected item.</param>
        /// <param name="cancellationToken">A cancellation token from the wait context.</param>
        private async Task ProcessItemSelectionAsync(NavigationBarItem item, CancellationToken cancellationToken)
        {
            AssertIsForeground();
            if (item is NavigationBarPresentedItem)
            {
                // Presented items are not navigable, but they may be selected due to a race
                // documented in Bug #1174848. Protect all INavigationBarItemService implementers
                // from this by ignoring these selections here.
                return;
            }

            if (item is NavigationBarProjectItem projectItem)
            {
                projectItem.SwitchToContext();
            }
            else
            {
                // When navigating, just use the partial semantics workspace.  Navigation doesn't need the fully bound
                // compilations to be created, and it can save us a lot of costly time building skeleton assemblies.
                var document = _subjectBuffer.CurrentSnapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
                if (document != null)
                {
                    var navBarService = document.GetRequiredLanguageService<INavigationBarItemService>();
                    var snapshot = _subjectBuffer.CurrentSnapshot;
                    var view = _presenter.TryGetCurrentView();

                    // ConfigureAwait(true) as we have to come back to UI thread in order to kick of the refresh task
                    // below. Note that we only want to refresh if selecting the item had an effect (either navigating
                    // or generating).  If nothing happened to don't want to refresh.  This is important as some items
                    // exist in the type list that are only there to show a set a particular set of items in the member
                    // list.  So selecting such an item should only update the member list, and we do not want a refresh
                    // to wipe that out.
                    if (!await navBarService.TryNavigateToItemAsync(document, item, view, snapshot, cancellationToken).ConfigureAwait(true))
                        return;
                }
            }

            // Now that the edit has been done, refresh to make sure everything is up-to-date.
            StartModelUpdateAndSelectedItemUpdateTasks();
        }
    }
}
