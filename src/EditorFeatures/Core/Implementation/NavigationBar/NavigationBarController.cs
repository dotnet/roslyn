// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private readonly WorkspaceRegistration _workspaceRegistration;

        /// <summary>
        /// If we have pushed a full list to the presenter in response to a focus event, this
        /// contains the version stamp of the list that was pushed. It is null if the last thing
        /// pushed to the list was due to a caret move or file change.
        /// </summary>
        private VersionStamp? _versionStampOfFullListPushedToPresenter = null;

        private bool _disconnected = false;
        private Workspace _workspace;

        public NavigationBarController(
            INavigationBarPresenter presenter,
            ITextBuffer subjectBuffer,
            IWaitIndicator waitIndicator,
            IAsynchronousOperationListener asyncListener)
        {
            _presenter = presenter;
            _subjectBuffer = subjectBuffer;
            _waitIndicator = waitIndicator;
            _asyncListener = asyncListener;
            _workspaceRegistration = Workspace.GetWorkspaceRegistration(subjectBuffer.AsTextContainer());
            _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;

            presenter.CaretMoved += OnCaretMoved;
            presenter.ViewFocused += OnViewFocused;

            presenter.DropDownFocused += OnDropDownFocused;
            presenter.ItemSelected += OnItemSelected;

            subjectBuffer.PostChanged += OnSubjectBufferPostChanged;

            // Initialize the tasks to be an empty model so we never have to deal with a null case.
            _modelTask = Task.FromResult(
                new NavigationBarModel(
                    SpecializedCollections.EmptyList<NavigationBarItem>(),
                    default(VersionStamp),
                    null));

            _selectedItemInfoTask = Task.FromResult(new NavigationBarSelectedTypeAndMember(null, null));

            if (_workspaceRegistration.Workspace != null)
            {
                ConnectToWorkspace(_workspaceRegistration.Workspace);
            }
        }

        private void OnWorkspaceRegistrationChanged(object sender, EventArgs e)
        {
            DisconnectFromWorkspace();

            var newWorkspace = _workspaceRegistration.Workspace;
            if (newWorkspace != null)
            {
                ConnectToWorkspace(newWorkspace);
            }
        }

        private void ConnectToWorkspace(Workspace workspace)
        {
            AssertIsForeground();

            // If we disconnected before the workspace ever connected, just disregard
            if (_disconnected)
            {
                return;
            }

            _workspace = workspace;
            _workspace.WorkspaceChanged += this.OnWorkspaceChanged;

            // For the first time you open the file, we'll start immediately
            StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0, selectedItemUpdateDelay: 0, updateUIWhenDone: true);
        }

        private void DisconnectFromWorkspace()
        {
            if (_workspace != null)
            {
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

            _workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;

            _disconnected = true;

            // Cancel off any remaining background work
            _modelTaskCancellationSource.Cancel();
            _selectedItemInfoTaskCancellationSource.Cancel();
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            // We're getting an event for a workspace we already disconnected from
            if (args.NewSolution.Workspace != _workspace)
            {
                return;
            }

            // If the displayed project is being renamed, retrigger the update
            if (args.Kind == WorkspaceChangeKind.ProjectChanged && args.ProjectId != null)
            {
                var oldProject = args.OldSolution.GetProject(args.ProjectId);
                var newProject = args.NewSolution.GetProject(args.ProjectId);

                if (oldProject.Name != newProject.Name)
                {
                    var currentContextDocumentId = _workspace.GetDocumentIdInCurrentContext(_subjectBuffer.AsTextContainer());

                    if (currentContextDocumentId != null && currentContextDocumentId.ProjectId == args.ProjectId)
                    {
                        StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0, selectedItemUpdateDelay: 0, updateUIWhenDone: true);
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
                    StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0, selectedItemUpdateDelay: 0, updateUIWhenDone: true);
                }
            }
        }

        private void OnSubjectBufferPostChanged(object sender, EventArgs e)
        {
            AssertIsForeground();

            StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: TaggerConstants.MediumDelay, selectedItemUpdateDelay: 0, updateUIWhenDone: true);
        }

        private void OnCaretMoved(object sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask(delay: TaggerConstants.NearImmediateDelay, updateUIWhenDone: true);
        }

        private void OnViewFocused(object sender, EventArgs e)
        {
            AssertIsForeground();
            StartSelectedItemUpdateTask(delay: TaggerConstants.ShortDelay, updateUIWhenDone: true);
        }

        private void OnDropDownFocused(object sender, EventArgs e)
        {
            AssertIsForeground();

            // Refresh the drop downs to their full information
            _waitIndicator.Wait(
                EditorFeaturesResources.NavigationBars,
                EditorFeaturesResources.RefreshingNavigationBars,
                allowCancel: true,
                action: context => UpdateDropDownsSynchronously(context.CancellationToken));
        }

        private void UpdateDropDownsSynchronously(CancellationToken cancellationToken)
        {
            AssertIsForeground();

            // If the presenter already has the full list and the model is already complete, then we
            // don't have to do any further computation nor push anything to the presenter
            if (PresenterAlreadyHaveUpToDateFullList(cancellationToken))
            {
                return;
            }

            // We need to ensure that all the state computation is up to date, so cancel any
            // previous work and ensure the model is up to date
            StartModelUpdateAndSelectedItemUpdateTasks(modelUpdateDelay: 0, selectedItemUpdateDelay: 0, updateUIWhenDone: false);

            // Wait for the work to be complete. We'll wait with our cancellationToken, so if the
            // user hits cancel we won't block them, but the computation can still continue

            using (Logger.LogBlock(FunctionId.NavigationBar_UpdateDropDownsSynchronously_WaitForModel, cancellationToken))
            {
                _modelTask.Wait(cancellationToken);
            }

            using (Logger.LogBlock(FunctionId.NavigationBar_UpdateDropDownsSynchronously_WaitForSelectedItemInfo, cancellationToken))
            {
                _selectedItemInfoTask.Wait(cancellationToken);
            }

            IList<NavigationBarProjectItem> projectItems;
            NavigationBarProjectItem selectedProjectItem;
            GetProjectItems(out projectItems, out selectedProjectItem);

            _presenter.PresentItems(
                projectItems,
                selectedProjectItem,
                _modelTask.Result.Types,
                _selectedItemInfoTask.Result.TypeItem,
                _selectedItemInfoTask.Result.MemberItem);
            _versionStampOfFullListPushedToPresenter = _modelTask.Result.SemanticVersionStamp;
        }

        private void GetProjectItems(out IList<NavigationBarProjectItem> projectItems, out NavigationBarProjectItem selectedProjectItem)
        {
            var documents = _subjectBuffer.CurrentSnapshot.GetRelatedDocumentsWithChanges();
            if (!documents.Any())
            {
                projectItems = SpecializedCollections.EmptyList<NavigationBarProjectItem>();
                selectedProjectItem = null;
                return;
            }

            projectItems = documents.Select(d =>
                new NavigationBarProjectItem(
                    d.Project.Name,
                    GetProjectGlyph(d.Project),
                    workspace: d.Project.Solution.Workspace,
                    documentId: d.Id,
                    language: d.Project.Language)).OrderBy(projectItem => projectItem.Text).ToList();

            projectItems.Do(i => i.InitializeTrackingSpans(_subjectBuffer.CurrentSnapshot));

            var document = _subjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            selectedProjectItem = document != null
                ? projectItems.FirstOrDefault(p => p.Text == document.Project.Name) ?? projectItems.First()
                : projectItems.First();
        }

        private Glyph GetProjectGlyph(Project project)
        {
            // TODO: Get the glyph from the hierarchy
            return project.Language == LanguageNames.CSharp ? Glyph.CSharpProject : Glyph.BasicProject;
        }

        /// <summary>
        /// Check if the presenter has already been pushed the full model that corresponds to the
        /// current buffer's project version stamp.
        /// </summary>
        private bool PresenterAlreadyHaveUpToDateFullList(CancellationToken cancellationToken)
        {
            AssertIsForeground();

            // If it doesn't have a full list pushed, then of course not
            if (_versionStampOfFullListPushedToPresenter == null)
            {
                return false;
            }

            var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            return document.Project.GetDependentSemanticVersionAsync(cancellationToken).WaitAndGetResult(cancellationToken) == _versionStampOfFullListPushedToPresenter;
        }

        private void PushSelectedItemsToPresenter(NavigationBarSelectedTypeAndMember selectedItems)
        {
            AssertIsForeground();

            var oldLeft = selectedItems.TypeItem;
            var oldRight = selectedItems.MemberItem;

            NavigationBarItem newLeft = null;
            NavigationBarItem newRight = null;
            var listOfLeft = new List<NavigationBarItem>();
            var listOfRight = new List<NavigationBarItem>();

            if (oldRight != null)
            {
                newRight = new NavigationBarPresentedItem(oldRight.Text, oldRight.Glyph, oldRight.Spans, oldRight.ChildItems, oldRight.Bolded, oldRight.Grayed || selectedItems.ShowMemberItemGrayed);
                newRight.TrackingSpans = oldRight.TrackingSpans;
                listOfRight.Add(newRight);
            }

            if (oldLeft != null)
            {
                newLeft = new NavigationBarPresentedItem(oldLeft.Text, oldLeft.Glyph, oldLeft.Spans, listOfRight, oldLeft.Bolded, oldLeft.Grayed || selectedItems.ShowTypeItemGrayed);
                newLeft.TrackingSpans = oldLeft.TrackingSpans;
                listOfLeft.Add(newLeft);
            }

            IList<NavigationBarProjectItem> projectItems;
            NavigationBarProjectItem selectedProjectItem;
            GetProjectItems(out projectItems, out selectedProjectItem);

            _presenter.PresentItems(
                projectItems,
                selectedProjectItem,
                listOfLeft,
                newLeft,
                newRight);
            _versionStampOfFullListPushedToPresenter = null;
        }

        private void OnItemSelected(object sender, NavigationBarItemSelectedEventArgs e)
        {
            AssertIsForeground();

            _waitIndicator.Wait(
                EditorFeaturesResources.NavigationBars,
                EditorFeaturesResources.RefreshingNavigationBars,
                allowCancel: true,
                action: context => ProcessItemSelectionSynchronously(e.Item, context.CancellationToken));
        }

        /// <summary>
        /// Process the selection of an item synchronously inside a wait context.
        /// </summary>
        /// <param name="item">The selected item.</param>
        /// <param name="cancellationToken">A cancellation token from the wait context.</param>
        private void ProcessItemSelectionSynchronously(NavigationBarItem item, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var presentedItem = item as NavigationBarPresentedItem;
            if (presentedItem != null)
            {
                // Presented items are not navigable, but they may be selected due to a race
                // documented in Bug #1174848. Protect all INavigationBarItemService implementers
                // from this by ignoring these selections here.
                return;
            }

            var projectItem = item as NavigationBarProjectItem;
            if (projectItem != null)
            {
                projectItem.SwitchToContext();

                // TODO: navigate to document / focus text view
            }
            else
            {
                var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    var languageService = document.Project.LanguageServices.GetService<INavigationBarItemService>();

                    NavigateToItem(item, document, _subjectBuffer.CurrentSnapshot, languageService, cancellationToken);
                }
            }

            // Now that the edit has been done, refresh to make sure everything is up-to-date. At
            // this point, we now use CancellationToken.None to ensure we're properly refreshed.
            UpdateDropDownsSynchronously(CancellationToken.None);
        }

        private void NavigateToItem(NavigationBarItem item, Document document, ITextSnapshot snapshot, INavigationBarItemService languageService, CancellationToken cancellationToken)
        {
            item.Spans = item.TrackingSpans.Select(ts => ts.GetSpan(snapshot).Span.ToTextSpan()).ToList();
            languageService.NavigateToItem(document, item, _presenter.TryGetCurrentView(), cancellationToken);
        }
    }
}
