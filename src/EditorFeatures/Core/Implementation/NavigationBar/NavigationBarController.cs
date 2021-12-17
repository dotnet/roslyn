// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    /// <summary>
    /// The controller for navigation bars.
    /// </summary>
    /// <remarks>
    /// The threading model for this class is simple: all non-static members are affinitized to the
    /// UI thread.
    /// </remarks>
    internal partial class NavigationBarController : ForegroundThreadAffinitizedObject, INavigationBarController
    {
        private readonly INavigationBarPresenter _presenter;
        private readonly ITextBuffer _subjectBuffer;
        private readonly IWaitIndicator _waitIndicator;
        private readonly IAsynchronousOperationListener _asyncListener;

        private bool _disconnected = false;
        private Workspace? _workspace;

        /// <summary>
        /// Latest model and selected items produced once <see cref="DetermineSelectedItemInfoAsync"/> completes and
        /// presents the single item to the view.  These can then be read in when the dropdown is expanded and we want
        /// to show all items.
        /// </summary>
        private (NavigationBarModel model, NavigationBarSelectedTypeAndMember selectedInfo) _latestModelAndSelectedInfo_OnlyAccessOnUIThread;

        /// <summary>
        /// The last full information we have presented. If we end up wanting to present the same thing again, we can
        /// just skip doing that as the UI will already know about this.
        /// </summary>
        private (ImmutableArray<NavigationBarProjectItem> projectItems, NavigationBarProjectItem? selectedProjectItem, NavigationBarModel model, NavigationBarSelectedTypeAndMember selectedInfo) _lastPresentedInfo;

        public NavigationBarController(
            IThreadingContext threadingContext,
            INavigationBarPresenter presenter,
            ITextBuffer subjectBuffer,
            IWaitIndicator waitIndicator,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext)
        {
            _presenter = presenter;
            _subjectBuffer = subjectBuffer;
            _waitIndicator = waitIndicator;
            _asyncListener = asyncListener;

            presenter.CaretMoved += OnCaretMoved;
            presenter.ViewFocused += OnViewFocused;

            presenter.DropDownFocused += OnDropDownFocused;
            presenter.ItemSelected += OnItemSelected;

            subjectBuffer.PostChanged += OnSubjectBufferPostChanged;

            // Initialize the tasks to be an empty model so we never have to deal with a null case.
            _latestModelAndSelectedInfo_OnlyAccessOnUIThread.model = new(
                ImmutableArray<NavigationBarItem>.Empty,
                semanticVersionStamp: default,
                itemService: null!);
            _latestModelAndSelectedInfo_OnlyAccessOnUIThread.selectedInfo = new(typeItem: null, memberItem: null);

            _modelTask = Task.FromResult(_latestModelAndSelectedInfo_OnlyAccessOnUIThread.model);
        }

        public void SetWorkspace(Workspace? newWorkspace)
        {
            DisconnectFromWorkspace();

            if (newWorkspace != null)
            {
                ConnectToWorkspace(newWorkspace);
            }
        }

        private void ConnectToWorkspace(Workspace workspace)
        {
            // If we disconnected before the workspace ever connected, just disregard
            if (_disconnected)
            {
                return;
            }

            _workspace = workspace;
            _workspace.WorkspaceChanged += this.OnWorkspaceChanged;
            _workspace.DocumentActiveContextChanged += this.OnDocumentActiveContextChanged;

            if (IsForeground())
            {
                ConnectToNewWorkspace();
            }
            else
            {
                var asyncToken = _asyncListener.BeginAsyncOperation(nameof(ConnectToWorkspace));
                Task.Run(async () =>
                {
                    await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    ConnectToNewWorkspace();
                }).CompletesAsyncOperation(asyncToken);
            }

            return;

            void ConnectToNewWorkspace()
            {
                // For the first time you open the file, we'll start immediately
                StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0);
            }
        }

        private void DisconnectFromWorkspace()
        {
            if (_workspace != null)
            {
                _workspace.DocumentActiveContextChanged -= this.OnDocumentActiveContextChanged;
                _workspace.WorkspaceChanged -= this.OnWorkspaceChanged;
                _workspace = null;
            }
        }

        public void Disconnect()
        {
            AssertIsForeground();
            DisconnectFromWorkspace();

            _subjectBuffer.PostChanged -= OnSubjectBufferPostChanged;

            _presenter.CaretMoved -= OnCaretMoved;
            _presenter.ViewFocused -= OnViewFocused;

            _presenter.DropDownFocused -= OnDropDownFocused;
            _presenter.ItemSelected -= OnItemSelected;

            _presenter.Disconnect();

            _disconnected = true;

            // Cancel off any remaining background work
            _modelTaskCancellationSource.Cancel();
            _selectedItemInfoTaskCancellationSource.Cancel();
        }

        private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs args)
        {
            // We're getting an event for a workspace we already disconnected from
            if (args.NewSolution.Workspace != _workspace)
            {
                return;
            }

            // If the displayed project is being renamed, retrigger the update
            if (args.Kind == WorkspaceChangeKind.ProjectChanged && args.ProjectId != null)
            {
                var oldProject = args.OldSolution.GetRequiredProject(args.ProjectId);
                var newProject = args.NewSolution.GetRequiredProject(args.ProjectId);

                if (oldProject.Name != newProject.Name)
                {
                    var currentContextDocumentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());

                    if (currentContextDocumentId != null && currentContextDocumentId.ProjectId == args.ProjectId)
                    {
                        StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0);
                    }
                }
            }

            if (args.Kind == WorkspaceChangeKind.DocumentChanged &&
                args.OldSolution == args.NewSolution)
            {
                var currentContextDocumentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());
                if (currentContextDocumentId != null && currentContextDocumentId == args.DocumentId)
                {
                    // The context has changed, so update everything.
                    StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0);
                }
            }
        }

        private void OnDocumentActiveContextChanged(object? sender, DocumentActiveContextChangedEventArgs args)
        {
            AssertIsForeground();
            if (args.Solution.Workspace != _workspace)
                return;

            var currentContextDocumentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());
            if (args.NewActiveContextDocumentId == currentContextDocumentId ||
                args.OldActiveContextDocumentId == currentContextDocumentId)
            {
                // if the active context changed, recompute the types/member as they may be changed as well.
                StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0);
            }
        }

        private void OnSubjectBufferPostChanged(object? sender, EventArgs e)
        {
            AssertIsForeground();
            StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: TaggerConstants.MediumDelay);
        }

        private void OnCaretMoved(object? sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask(delay: TaggerConstants.NearImmediateDelay);
        }

        private void OnViewFocused(object? sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask(delay: TaggerConstants.ShortDelay);
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
                newRight = new NavigationBarPresentedItem(oldRight.Text, oldRight.Glyph, oldRight.Spans, oldRight.ChildItems, oldRight.Bolded, oldRight.Grayed || selectedItems.ShowMemberItemGrayed)
                {
                    TrackingSpans = oldRight.TrackingSpans
                };
                listOfRight.Add(newRight);
            }

            if (oldLeft != null)
            {
                newLeft = new NavigationBarPresentedItem(oldLeft.Text, oldLeft.Glyph, oldLeft.Spans, listOfRight.ToImmutable(), oldLeft.Bolded, oldLeft.Grayed || selectedItems.ShowTypeItemGrayed)
                {
                    TrackingSpans = oldLeft.TrackingSpans
                };
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
            using var waitContext = _waitIndicator.StartWait(
                EditorFeaturesResources.Navigation_Bars,
                EditorFeaturesResources.Refreshing_navigation_bars,
                allowCancel: true,
                showProgress: false);

            try
            {
                await ProcessItemSelectionAsync(item, waitContext.CancellationToken).ConfigureAwait(false);
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
                    var navBarService = GetNavBarService(document);
                    var snapshot = _subjectBuffer.CurrentSnapshot;
                    item.Spans = item.TrackingSpans.SelectAsArray(ts => ts.GetSpan(snapshot).Span.ToTextSpan());
                    var view = _presenter.TryGetCurrentView();

                    // ConfigureAwait(true) as we have to come back to UI thread in order to kick of the refresh task
                    // below. Note that we only want to refresh if selecting the item had an effect (either navigating
                    // or generating).  If nothing happened to don't want to refresh.  This is important as some items
                    // exist in the type list that are only there to show a set a particular set of items in the member
                    // list.  So selecting such an item should only update the member list, and we do not want a refresh
                    // to wipe that out.
                    if (!await navBarService.TryNavigateToItemAsync(document, item, view, cancellationToken).ConfigureAwait(true))
                        return;
                }
            }

            // Now that the edit has been done, refresh to make sure everything is up-to-date.
            // Have to make sure we come back to the main thread for this.
            AssertIsForeground();
            StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0);
        }

        private static INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess GetNavBarService(Document document)
        {
            // Defer to the legacy service if the language is still using it.  Otherwise use the current ea API.
#pragma warning disable CS0618 // Type or member is obsolete
            var legacyService = document.GetLanguageService<INavigationBarItemService>();
#pragma warning restore CS0618 // Type or member is obsolete
            return legacyService == null
                ? document.GetRequiredLanguageService<INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess>()
                : new NavigationBarItemServiceWrapper(legacyService);
        }
    }
}
