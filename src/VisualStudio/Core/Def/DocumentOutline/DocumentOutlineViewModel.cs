// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SemanticModelReuse;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using VsWebSite;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using LspDocumentSymbol = DocumentSymbol;

    /// <summary>
    /// Responsible for updating data related to Document outline. It is expected that all public methods on this type
    /// do not need to be on the UI thread. Two properties: <see cref="SortOption"/> and <see cref="SearchText"/> are
    /// intended to be bound to a WPF view and should only be set from the UI thread.
    /// </summary>
    internal sealed partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly ITaggerEventSource _taggerEventSource;
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Queue that computes the new model and updates the UI state.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _workQueue;

        /// <summary>
        /// Queue responsible for updating the ui after a change/move happens.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _selectionQueue;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Mutable state.  Should only update on UI thread.

        private SortOption _sortOption_doNotAccessDirectly = SortOption.Location;
        private string _searchText_doNotAccessDirectly = "";
        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems_doNotAccessDirectly = ImmutableArray<DocumentSymbolDataViewModel>.Empty;
        private DocumentOutlineViewState _lastPresentedViewState_doNotAccessDirectly;

        /// <summary>
        /// Use to prevent reeentrancy on navigation/selection.
        /// </summary>
        private bool _isNavigating_doNotAccessDirectly;

        public DocumentOutlineViewModel(
            ILanguageServiceBroker2 languageServiceBroker,
            IAsynchronousOperationListener asyncListener,
            ITaggerEventSource taggerEventSource,
            ITextView textView,
            ITextBuffer textBuffer,
            IThreadingContext threadingContext)
        {
            _languageServiceBroker = languageServiceBroker;
            _taggerEventSource = taggerEventSource;
            _textView = textView;
            _textBuffer = textBuffer;
            _threadingContext = threadingContext;

            // initialize us to an empty state.
            _lastPresentedViewState_doNotAccessDirectly = CreateEmptyViewState(textBuffer.CurrentSnapshot);

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

            _taggerEventSource.Changed += OnEventSourceChanged;
            _taggerEventSource.Connect();

            // queue initial model update
            _workQueue.AddWork(default(VoidResult));
        }

        public void Dispose()
        {
            _taggerEventSource.Changed -= OnEventSourceChanged;
            _taggerEventSource.Disconnect();
        }

        private static DocumentOutlineViewState CreateEmptyViewState(ITextSnapshot currentSnapshot)
            => new(
                currentSnapshot,
                searchText: "",
                ImmutableArray<DocumentSymbolDataViewModel>.Empty,
                IntervalTree<DocumentSymbolDataViewModel>.Empty);

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _workQueue.AddWork(default(VoidResult), cancelExistingWork: false);

        public bool IsNavigating
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _isNavigating_doNotAccessDirectly;
            }

            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                Debug.Assert(_isNavigating_doNotAccessDirectly != value);
                _isNavigating_doNotAccessDirectly = value;
            }
        }

        public SortOption SortOption
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _sortOption_doNotAccessDirectly;
            }

            set
            {
                // Called from WPF.

                _threadingContext.ThrowIfNotOnUIThread();
                SetProperty(ref _sortOption_doNotAccessDirectly, value);

                // We do not need to update our views here.  Sorting is handled entirely by WPF using
                // DocumentSymbolDataViewModelSorter.
            }
        }

        public string SearchText
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _searchText_doNotAccessDirectly;
            }

            set
            {
                // setting this happens from wpf itself.  So once this changes, kick off the work to actually filter down our models.

                _threadingContext.ThrowIfNotOnUIThread();
                _searchText_doNotAccessDirectly = value;

                _workQueue.AddWork(default(VoidResult), cancelExistingWork: false);
            }
        }

        public ImmutableArray<DocumentSymbolDataViewModel> DocumentSymbolViewModelItems
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _documentSymbolViewModelItems_doNotAccessDirectly;
            }

            // Setting this only happens from within this type once we've computed new items or filtered down the existing set.
            private set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                SetProperty(ref _documentSymbolViewModelItems_doNotAccessDirectly, value);
            }
        }

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

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            NotifyPropertyChanged(propertyName);
        }

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
            // We do not want this work running on a background thread
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            // Do the expensive LSP work of actually computing the items to show.
            var (documentSymbolData, newTextSnapshot) = await ComputeDocumentSymbolDataAsync(cancellationToken).ConfigureAwait(false);

            // Now, go back to the UI and grab the prior view state we set, and the current UI values we want to update the data with.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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

            // Now create an interval tree out of the view models.  This will allow us to easily find the intersecting
            // view models given any position in the file with any particular text snapshot.
            var intervalTree = SimpleIntervalTree.Create(new IntervalIntrospector(), Array.Empty<DocumentSymbolDataViewModel>());
            AddToIntervalTree(newViewModelItems);

            var newViewState = new DocumentOutlineViewState(
                newTextSnapshot,
                searchText,
                newViewModelItems,
                intervalTree);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            this.LastPresentedViewState = newViewState;
            this.DocumentSymbolViewModelItems = newViewModelItems;

            // Now that we've updated our state, enqueue the work to expand/select the right item.
            ExpandAndSelectItemAtCaretPosition();

            return;

            void AddToIntervalTree(ImmutableArray<DocumentSymbolDataViewModel> viewModels)
            {
                foreach (var model in viewModels)
                {
                    intervalTree.AddIntervalInPlace(model);
                    AddToIntervalTree(model.Children);
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
                    _textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);
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

            if (this.IsNavigating)
                return;

            // Map the caret back to the snapshot used to create the last set of items.
            var modelTree = this.LastPresentedViewState.ViewModelItemsTree;
            var caretPosition = _textView.Caret.Position.BufferPosition.TranslateTo(this.LastPresentedViewState.TextSnapshot, PointTrackingMode.Positive);

            this.IsNavigating = true;
            try
            {
                // Treat the caret as if it has length 1.  That way if it is in between two items, it will naturally
                // only intersect right the item on the right of it.
                var overlappingModels = modelTree.GetIntervalsThatOverlapWith(
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
}
