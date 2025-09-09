// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// Responsible for updating data related to Document outline. It is expected that all public methods on this type
/// do not need to be on the UI thread. Two properties: <see cref="SortOption"/> and <see cref="SearchText"/> are
/// intended to be bound to a WPF view and should only be set from the UI thread.
/// </summary>
internal sealed partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IThreadingContext _threadingContext;
    private readonly VsCodeWindowViewTracker _codeWindowViewTracker;
    private readonly ILanguageServiceBroker2 _languageServiceBroker;
    private readonly ITaggerEventSource _taggerEventSource;
    private readonly ITextBuffer _textBuffer;

    /// <summary>
    /// Queue that computes the new model and updates the UI state.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _workQueue;

    /// <summary>
    /// Queue responsible for updating the ui after a change/move happens.
    /// </summary>
    private readonly AsyncBatchingWorkQueue _selectionQueue;

    public event PropertyChangedEventHandler? PropertyChanged;

    private DocumentOutlineViewState _lastPresentedViewState_doNotAccessDirectly;
    private bool _isDisposed;

    public DocumentOutlineViewModel(
        IThreadingContext threadingContext,
        VsCodeWindowViewTracker codeWindowViewTracker,
        ILanguageServiceBroker2 languageServiceBroker,
        IAsynchronousOperationListener asyncListener)
    {
        _threadingContext = threadingContext;
        _codeWindowViewTracker = codeWindowViewTracker;
        _languageServiceBroker = languageServiceBroker;
        _textBuffer = codeWindowViewTracker.GetActiveView().TextBuffer;

        // initialize us to an empty state.
        _lastPresentedViewState_doNotAccessDirectly = CreateEmptyViewState(_textBuffer.CurrentSnapshot);

        _workQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Medium,
            ComputeViewStateAsync,
            asyncListener,
            _threadingContext.DisposalToken);

        _selectionQueue = new AsyncBatchingWorkQueue(
            DelayTimeSpan.Medium,
            UpdateSelectionAsync,
            asyncListener,
            _threadingContext.DisposalToken);

        _taggerEventSource = TaggerEventSources.Compose(
            TaggerEventSources.OnTextChanged(_textBuffer),
            TaggerEventSources.OnParseOptionChanged(_textBuffer),
            TaggerEventSources.OnWorkspaceChanged(_textBuffer, asyncListener),
            TaggerEventSources.OnWorkspaceRegistrationChanged(_textBuffer));

        _taggerEventSource.Changed += OnEventSourceChanged;
        _taggerEventSource.Connect();

        // queue initial model update
        _workQueue.AddWork();
    }

    public void Dispose()
    {
        _isDisposed = true;
        _taggerEventSource.Changed -= OnEventSourceChanged;
        _taggerEventSource.Disconnect();
    }

    private static DocumentOutlineViewState CreateEmptyViewState(ITextSnapshot currentSnapshot)
        => new(
            currentSnapshot,
            searchText: "",
            [],
            []);

    private void OnEventSourceChanged(object sender, TaggerEventArgs e)
        => _workQueue.AddWork(cancelExistingWork: true);

    /// <summary>
    /// Keeps track if we're currently in the middle of navigating or not.  For example, when the user clicks on an
    /// item, we will navigate to it.  That will then kick of a caret move.  This flag helps us realize the caret move
    /// is not user driven, so we don't then start the work to go expand/select something.
    /// </summary>
    /// <remarks>This property is not bound to the UI.</remarks>
    public bool IsNavigating
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            Debug.Assert(field != value);
            field = value;
        }
    }

    /// <summary>
    /// Keeps track of all the inputs/computed-state for the last values we presented on the UI.  Used so we can
    /// track prior state forward (like which nodes are expanded).
    /// </summary>
    /// <remarks>This property is not bound to the UI.</remarks>
    private DocumentOutlineViewState LastPresentedViewState
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return _lastPresentedViewState_doNotAccessDirectly;
        }

        set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            _lastPresentedViewState_doNotAccessDirectly = value;
        }
    }

    /// <remarks>This property is bound to the UI. However, it is only read/written by the UI. We only act as
    /// storage for the value. When this value is true, UI updates are deferred.</remarks>
    public Visibility Visibility
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            field = value;
        }
    } = Visibility.Visible;

    /// <remarks>This property is bound to the UI.  However, it is only read/written by the UI.  We only act as
    /// storage for the value.  When the value changes, the sorting is actually handled by
    /// DocumentSymbolDataViewModelSorter.</remarks>
    public SortOption SortOption
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            field = value;
        }
    } = SortOption.Location;

    /// <remarks>This property is bound to the UI.  However, it is read/written by the UI, and also read by us when
    /// computing the model to know what to filter it down to.</remarks>
    public string SearchText
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        set
        {
            // Called from WPF.  When this changes, kick off the work to actually filter down our models.

            _threadingContext.ThrowIfNotOnUIThread();
            field = value;

            _workQueue.AddWork(cancelExistingWork: true);
        }
    } = "";

    /// <remarks>This property is bound to the UI.  It is only read by the UI, but can be read/written by us.</remarks>
    public ImmutableArray<DocumentSymbolDataViewModel> DocumentSymbolViewModelItems
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }

        // Setting this only happens from within this type once we've computed new items or filtered down the existing set.
        private set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            if (field == value)
                return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DocumentSymbolViewModelItems)));
        }
    } = [];

    public void ExpandOrCollapseAll(bool shouldExpand)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        ExpandOrCollapse(this.DocumentSymbolViewModelItems, shouldExpand);

        static void ExpandOrCollapse(ImmutableArray<DocumentSymbolDataViewModel> models, bool shouldExpand)
        {
            foreach (var model in models)
            {
                model.IsExpanded = shouldExpand;
                ExpandOrCollapse(model.Children, shouldExpand);
            }
        }
    }

    private async ValueTask ComputeViewStateAsync(CancellationToken cancellationToken)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (_isDisposed)
            return;

        if (Visibility != Visibility.Visible)
        {
            // Retry the update after a delay
            _workQueue.AddWork(cancelExistingWork: true);
            return;
        }

        // Do any expensive semantic/computation work in the background.
        await TaskScheduler.Default;
        cancellationToken.ThrowIfCancellationRequested();

        // Do the expensive LSP work of actually computing the items to show.
        var (documentSymbolData, newTextSnapshot) = await ComputeDocumentSymbolDataAsync(cancellationToken).ConfigureAwait(false);

        // Now, go back to the UI and grab the prior view state we set, and the current UI values we want to update the data with.
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (_isDisposed)
            return;

        if (Visibility != Visibility.Visible)
        {
            // Retry the update after a delay
            _workQueue.AddWork(cancelExistingWork: true);
            return;
        }

        var searchText = this.SearchText;
        var sortOption = this.SortOption;
        var lastPresentedViewState = this.LastPresentedViewState;

        // Jump back to the BG to do all our work.
        await TaskScheduler.Default;
        cancellationToken.ThrowIfCancellationRequested();

        var searchTextChanged = searchText != lastPresentedViewState.SearchText;
        var oldViewModelItems = lastPresentedViewState.ViewModelItems;

        // if we got new data or the user changed the search text, recompute our items to correspond to this new state.
        // Apply whatever the current search text is to what the model returned, and produce the new items.
        var newViewModelItems = GetDocumentSymbolItemViewModels(
            sortOption, SearchDocumentSymbolData(documentSymbolData, searchText, cancellationToken));

        // If the search text changed, just show everything in expanded form, so the user can see everything
        // that matched, without anything being hidden.
        //
        // in the case of no search text change, attempt to keep the same open/close expansion state from before.
        if (!searchTextChanged)
        {
            ApplyOldStateToNewItems(
                oldSnapshot: lastPresentedViewState.TextSnapshot,
                newSnapshot: newTextSnapshot,
                oldItems: oldViewModelItems,
                newItems: newViewModelItems);
        }

        // Now create an interval tree out of the view models.  This will allow us to easily find the intersecting view
        // models given any position in the file with any particular text snapshot.
        using var _ = SegmentedListPool.GetPooledList<DocumentSymbolDataViewModel>(out var models);
        AddAllModels(newViewModelItems, models);
        var intervalTree = ImmutableIntervalTree<DocumentSymbolDataViewModel>.CreateFromUnsorted(
            new IntervalIntrospector(), models);

        var newViewState = new DocumentOutlineViewState(
            newTextSnapshot,
            searchText,
            newViewModelItems,
            intervalTree);

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (_isDisposed)
            return;

        if (Visibility != Visibility.Visible)
        {
            // Retry the update after a delay
            _workQueue.AddWork(cancelExistingWork: true);
            return;
        }

        this.LastPresentedViewState = newViewState;
        this.DocumentSymbolViewModelItems = newViewModelItems;

        // Now that we've updated our state, enqueue the work to expand/select the right item.
        ExpandAndSelectItemAtCaretPosition();

        return;

        static void AddAllModels(ImmutableArray<DocumentSymbolDataViewModel> viewModels, SegmentedList<DocumentSymbolDataViewModel> result)
        {
            foreach (var model in viewModels)
            {
                result.Add(model);
                AddAllModels(model.Children, result);
            }
        }

        static void ApplyOldStateToNewItems(
            ITextSnapshot oldSnapshot,
            ITextSnapshot newSnapshot,
            ImmutableArray<DocumentSymbolDataViewModel> oldItems,
            ImmutableArray<DocumentSymbolDataViewModel> newItems)
        {
            // Walk through the old items, mapping their spans forward and keeping track if they were expanded or
            // collapsed.  Then walk through the new items and see if they have the same span as a prior item.  If
            // so, preserve the expansion state.
            using var _ = PooledDictionary<Span, (bool isExpanded, bool isSelected)>.GetInstance(out var oldState);
            AddPreviousState(newSnapshot, oldItems, oldState);

            // If we had any items from before, and they were all collapsed, the collapse all the new items.
            if (oldItems.Length > 0 && oldItems.All(static i => !i.IsExpanded))
            {
                foreach (var item in newItems)
                {
                    item.IsExpanded = false;

                    if (oldState.TryGetValue(item.Data.SelectionRangeSpan.Span, out var oldValues) && oldValues.isSelected)
                        item.IsSelected = true;
                }
            }
            else
            {
                ApplyOldState(oldState, newItems);
            }
        }

        static void AddPreviousState(
            ITextSnapshot newSnapshot,
            ImmutableArray<DocumentSymbolDataViewModel> oldItems,
            PooledDictionary<Span, (bool isExpanded, bool isSelected)> oldState)
        {
            foreach (var item in oldItems)
            {
                // EdgeInclusive so that if we type on the end of an existing item it maps forward to the new full span.
                var mapped = item.Data.SelectionRangeSpan.TranslateTo(newSnapshot, SpanTrackingMode.EdgeInclusive);
                oldState[mapped.Span] = (item.IsExpanded, item.IsSelected);

                AddPreviousState(newSnapshot, item.Children, oldState);
            }
        }

        static void ApplyOldState(
            PooledDictionary<Span, (bool isExpanded, bool isSelected)> oldState,
            ImmutableArray<DocumentSymbolDataViewModel> newItems)
        {
            foreach (var item in newItems)
            {
                if (oldState.TryGetValue(item.Data.SelectionRangeSpan.Span, out var oldValues))
                {
                    item.IsExpanded = oldValues.isExpanded;
                    item.IsSelected = oldValues.isSelected;
                }

                ApplyOldState(oldState, item.Children);
            }
        }
    }

    private async Task<(ImmutableArray<DocumentSymbolData> documentSymbolData, ITextSnapshot newTextSnapshot)> ComputeDocumentSymbolDataAsync(CancellationToken cancellationToken)
    {
        var filePath = _textBuffer.GetRelatedDocuments().FirstOrDefault(static d => d.FilePath is not null)?.FilePath;
        if (filePath != null)
        {
            // Obtain the LSP response and text snapshot used.
            var response = await DocumentSymbolsRequestAsync(
                _textBuffer, _languageServiceBroker.RequestAsync, filePath, cancellationToken).ConfigureAwait(false);
            if (response != null)
            {
                var newTextSnapshot = response.Value.snapshot;
                var documentSymbolData = CreateDocumentSymbolData(response.Value.response, newTextSnapshot);
                return (documentSymbolData, newTextSnapshot);
            }
        }

        return (ImmutableArray<DocumentSymbolData>.Empty, _textBuffer.CurrentSnapshot);
    }

    public void ExpandAndSelectItemAtCaretPosition()
    {
        _threadingContext.ThrowIfNotOnUIThread();
        _selectionQueue.AddWork(cancelExistingWork: true);
    }

    private async ValueTask UpdateSelectionAsync(CancellationToken cancellationToken)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        if (_isDisposed)
            return;

        if (this.IsNavigating)
            return;

        // Map the caret back to the snapshot used to create the last set of items.
        var modelTree = this.LastPresentedViewState.ViewModelItemsTree;
        var textView = _codeWindowViewTracker.GetActiveView();
        var caretPosition = textView.Caret.Position.BufferPosition.TranslateTo(this.LastPresentedViewState.TextSnapshot, PointTrackingMode.Positive);

        this.IsNavigating = true;
        try
        {
            // Treat the caret as if it has length 1.  That way if it is in between two items, it will naturally
            // only intersect right the item on the right of it.
            var overlappingModels = modelTree.Algorithms.GetIntervalsThatOverlapWith(
                caretPosition.Position, 1, new IntervalIntrospector());

            if (overlappingModels.Length == 0)
                return;

            // Order from smallest to largest.  The smallest is the innermost and should be the one we actually select.
            // The others are the parents and we should expand those so the innermost one is visible.
            overlappingModels = overlappingModels.Sort(static (m1, m2) =>
            {
                return m1.Data.RangeSpan.Span.Length - m2.Data.RangeSpan.Span.Length;
            });

            overlappingModels[0].IsSelected = true;
            for (var i = 1; i < overlappingModels.Length; i++)
                overlappingModels[i].IsExpanded = true;
        }
        finally
        {
            this.IsNavigating = false;
        }
    }
}
