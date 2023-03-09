// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

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
        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly DocumentSymbolDataModel _emptyModel;

        /// <summary>
        /// Queue that uses the language-server-protocol to get document symbol information.
        /// This queue can return null if it is called before and LSP server is registered for our document.
        /// </summary>
        private readonly AsyncBatchingResultQueue<DocumentSymbolDataModel> _documentSymbolQueue;

        /// <summary>
        /// Queue for updating the state of the view model.  The boolean indicates if we should expand/collapse all
        /// items.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<bool?> _updateViewModelStateQueue;

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        // Mutable state.  Should only update on UI thread.

        private SortOption _sortOption_doNotAccessDirectly = SortOption.Location;
        private string _searchText_doNotAccessDirectly = "";
        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems_doNotAccessDirectly = ImmutableArray<DocumentSymbolDataViewModel>.Empty;

        /// <summary>
        /// Mutable state.  only accessed from UpdateViewModelStateAsync though.  Since that executes serially, it does not need locking.
        /// </summary>
        private (DocumentSymbolDataModel model, string searchText, ImmutableArray<DocumentSymbolDataViewModel> viewModelItems) _lastPresentedData_onlyAccessSerially;

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
            _emptyModel = new DocumentSymbolDataModel(ImmutableArray<DocumentSymbolData>.Empty, _textBuffer.CurrentSnapshot);
            _lastPresentedData_onlyAccessSerially = (_emptyModel, this.SearchText, this.DocumentSymbolViewModelItems);

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_threadingContext.DisposalToken);

            // work queue for refreshing LSP data
            _documentSymbolQueue = new AsyncBatchingResultQueue<DocumentSymbolDataModel>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                asyncListener,
                CancellationToken);

            // work queue for updating UI state
            _updateViewModelStateQueue = new AsyncBatchingWorkQueue<bool?>(
                DelayTimeSpan.Short,
                UpdateViewModelStateAsync,
                asyncListener,
                CancellationToken);

            _taggerEventSource.Changed += OnEventSourceChanged;
            _taggerEventSource.Connect();

            // queue initial model update
            _documentSymbolQueue.AddWork();
        }

        public void Dispose()
        {
            _taggerEventSource.Changed -= OnEventSourceChanged;
            _taggerEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
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
                _updateViewModelStateQueue.AddWork(item: null);
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
                _threadingContext.ThrowIfNotOnBackgroundThread();

                // Unselect any currently selected items or WPF will believe it needs to select the root node.
                UnselectAll(_documentSymbolViewModelItems_doNotAccessDirectly);
                SetProperty(ref _documentSymbolViewModelItems_doNotAccessDirectly, value);
            }
        }

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _documentSymbolQueue.AddWork(cancelExistingWork: true);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            NotifyPropertyChanged(propertyName);
        }

        private async ValueTask<DocumentSymbolDataModel> GetDocumentSymbolAsync(CancellationToken cancellationToken)
        {
            // We do not want this work running on a background thread
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            var textBuffer = _textBuffer;
            var currentSnapshot = textBuffer.CurrentSnapshot;
            var filePath = _textBuffer.GetRelatedDocuments().FirstOrDefault(static d => d.FilePath is not null)?.FilePath;
            if (filePath is null)
            {
                // text buffer is not saved to disk. LSP does not support calls without URIs. and Visual Studio does not
                // have a URI concept other than the file path.
                return _emptyModel;
            }

            // Obtain the LSP response and text snapshot used.
            var response = await DocumentSymbolsRequestAsync(
                textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

            // If there is no matching LSP server registered the client will return null here - e.g. wrong content type
            // on the buffer, the server totally failed to start, server doesn't support the right capabilities. For C#
            // we might know it's a bug if we get a null response here, but we don't know that in general for all
            // languages. see
            // "Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient.AlwaysActivateInProcLanguageClient" for the
            // list of content types we register for. At this time the expected list is C#, Visual Basic, and F#
            if (response is null)
                return _emptyModel;

            var responseBody = response.Value.response.ToObject<LspDocumentSymbol[]>();
            // It would be a bug in the LSP server implementation if we get back a null result here.
            Assumes.NotNull(responseBody);

            var model = CreateDocumentSymbolDataModel(responseBody, response.Value.snapshot);

            // Now that we produced a new model, kick off the work to present it to the UI.
            _updateViewModelStateQueue.AddWork(item: null);

            return model;
        }

        public void EnqueueSelectTreeNode()
            => _updateViewModelStateQueue.AddWork(item: null);

        public void EnqueueExpandOrCollapse(bool shouldExpand)
            => _updateViewModelStateQueue.AddWork(shouldExpand);

        private async ValueTask UpdateViewModelStateAsync(ImmutableSegmentedList<bool?> viewModelStateData, CancellationToken cancellationToken)
        {
            // just to UI thread to get the last UI state we presented.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var searchText = this.SearchText;
            var caretPoint = _textView.Caret.Position.BufferPosition;
            var lastPresentedData = _lastPresentedData_onlyAccessSerially;

            // Jump back to the BG to do all our work.
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            // Grab the last computed model.  We can compare it to what we previously presented to see if it's changed.
            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false) ?? _emptyModel;

            var expansion = viewModelStateData.LastOrDefault(b => b != null);

            var modelChanged = model != lastPresentedData.model;
            var searchTextChanged = searchText != lastPresentedData.searchText;
            var lastViewModelItems = lastPresentedData.viewModelItems;

            ImmutableArray<DocumentSymbolDataViewModel> currentViewModelItems;

            // if we got new data or the user changed the search text, recompute our items to correspond to this new state.
            if (modelChanged || searchTextChanged)
            {
                // Apply whatever the current search text is to what the model returned, and produce the new items.
                currentViewModelItems = GetDocumentSymbolItemViewModels(
                    SearchDocumentSymbolData(model.DocumentSymbolData, searchText, cancellationToken));

                // If the search text changed, just show everything in expanded form, so the user can see everything
                // that matched, without anything being hidden.
                //
                // in the case of no search text change, attempt to keep the same open/close expansion state from before.
                if (!searchTextChanged)
                {
                    ApplyExpansionStateToNewItems(
                        oldSnapshot: lastPresentedData.model.OriginalSnapshot,
                        newSnapshot: model.OriginalSnapshot,
                        oldItems: lastViewModelItems,
                        newItems: currentViewModelItems);
                }
            }
            else
            {
                // Model didn't change and search text didn't change.  Keep what we have, and only figure out what to
                // select/expand below.
                currentViewModelItems = lastViewModelItems;
            }

            var symbolToSelect = GetDocumentNodeToSelect(currentViewModelItems, model.OriginalSnapshot, caretPoint);
            if (symbolToSelect is not null)
            {
                ExpandAncestors(currentViewModelItems, symbolToSelect.Data.SelectionRangeSpan);
                symbolToSelect.IsSelected = true;
            }

            // If we aren't filtering to search results do expand/collapse
            if (expansion != null)
                SetExpansionOption(currentViewModelItems, expansion.Value);

            // If we produced new items, then let wpf know so it can update hte UI.
            if (currentViewModelItems != lastViewModelItems)
                this.DocumentSymbolViewModelItems = currentViewModelItems;

            // Now that we've made all our changes, record that we've done so so we can see what has changed when future requests come in.
            // note: we are safe to record this on the BG as we are called serially and are the only place to read/write it.
            _lastPresentedData_onlyAccessSerially = (model, searchText, currentViewModelItems);

            return;

            static void ApplyExpansionStateToNewItems(
                ITextSnapshot oldSnapshot,
                ITextSnapshot newSnapshot,
                ImmutableArray<DocumentSymbolDataViewModel> oldItems,
                ImmutableArray<DocumentSymbolDataViewModel> newItems)
            {
                // If we had any items from before, and they were all collapsed, the collapse all the new items.
                if (oldItems.Length > 0 && oldItems.All(static i => !i.IsExpanded))
                {
                    // new nodes are un-collapsed by default
                    // we want to collapse all new top-level nodes if 
                    // everything else currently is so things aren't "jumpy"
                    foreach (var item in newItems)
                        item.IsExpanded = false;

                    return;
                }

                // Walk through the old items, mapping their spans forward and keeping track if they were expanded or
                // collapsed.  Then walk through the new items and see if they have the same span as a prior item.  If
                // so, preserve the expansion state.
                using var _ = PooledDictionary<Span, bool>.GetInstance(out var expansionState);
                AddPreviousExpansionState(newSnapshot, oldItems, expansionState);
                ApplyExpansionState(expansionState, newItems);
            }

            static void AddPreviousExpansionState(
                ITextSnapshot newSnapshot,
                ImmutableArray<DocumentSymbolDataViewModel> oldItems,
                PooledDictionary<Span, bool> expansionState)
            {
                foreach (var item in oldItems)
                {
                    // EdgeInclusive so that if we type on the end of an existing item it maps forward to the new full span.
                    var mapped = item.Data.SelectionRangeSpan.TranslateTo(newSnapshot, SpanTrackingMode.EdgeInclusive);
                    expansionState[mapped.Span] = item.IsExpanded;

                    AddPreviousExpansionState(newSnapshot, item.Children, expansionState);
                }
            }

            static void ApplyExpansionState(
                PooledDictionary<Span, bool> expansionState,
                ImmutableArray<DocumentSymbolDataViewModel> newItems)
            {
                foreach (var item in newItems)
                {
                    if (expansionState.TryGetValue(item.Data.SelectionRangeSpan.Span, out var isExpanded))
                        item.IsExpanded = isExpanded;

                    ApplyExpansionState(expansionState, item.Children);
                }
            }
        }

        /// <summary>
        /// Updates the IsExpanded property for the Document Symbol ViewModel based on the given Expansion Option. The parameter
        /// <param name="currentDocumentSymbolItems"/> is used to reference the current node expansion in the view.
        /// </summary>
        public static void SetIsExpandedOnNewItems(
            ImmutableArray<DocumentSymbolDataViewModel> newDocumentSymbolItems,
            ImmutableArray<DocumentSymbolDataViewModel> currentDocumentSymbolItems)
        {
            using var _ = PooledHashSet<DocumentSymbolDataViewModel>.GetInstance(out var hashSet);
            hashSet.AddRange(newDocumentSymbolItems);

            foreach (var item in currentDocumentSymbolItems)
            {
                if (!hashSet.TryGetValue(item, out var newItem))
                {
                    continue;
                }

                // Setting a boolean property on this View Model is allowed to happen on any thread.
                newItem.IsExpanded = item.IsExpanded;
                SetIsExpandedOnNewItems(newItem.Children, item.Children);
            }
        }
    }
}
