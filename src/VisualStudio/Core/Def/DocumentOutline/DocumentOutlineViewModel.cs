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
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Queue that uses the language-server-protocol to get document symbol information.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _workQueue;

        public event PropertyChangedEventHandler? PropertyChanged;

        ///// <summary>
        ///// Queue for updating the state of the view model.  The boolean indicates if we should expand/collapse all
        ///// items.
        ///// </summary>
        //private readonly AsyncBatchingWorkQueue<bool?> _updateViewModelStateQueue;

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        // Mutable state.  Should only update on UI thread.

        private SortOption _sortOption_doNotAccessDirectly = SortOption.Location;
        private string _searchText_doNotAccessDirectly = "";
        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems_doNotAccessDirectly = ImmutableArray<DocumentSymbolDataViewModel>.Empty;

        private DocumentOutlineViewState _lastViewState_onlyAccessFromUIThread;

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

            var currentSnapshot = textBuffer.CurrentSnapshot;
            _lastViewState_onlyAccessFromUIThread = new DocumentOutlineViewState(
                currentSnapshot,
                this.SearchText,
                this.DocumentSymbolViewModelItems,
                IntervalTree<DocumentSymbolDataViewModel>.Empty);

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_threadingContext.DisposalToken);

            _workQueue = new AsyncBatchingWorkQueue(
                DelayTimeSpan.Short,
                ComputeViewStateAsync,
                asyncListener,
                CancellationToken);

            _taggerEventSource.Changed += OnEventSourceChanged;
            _taggerEventSource.Connect();

            // queue initial model update
            _workQueue.AddWork();
        }

        public void Dispose()
        {
            _taggerEventSource.Changed -= OnEventSourceChanged;
            _taggerEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

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

                _workQueue.AddWork();
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

        private void OnEventSourceChanged(object sender, TaggerEventArgs e)
            => _workQueue.AddWork();

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

            var filePath = _textBuffer.GetRelatedDocuments().FirstOrDefault(static d => d.FilePath is not null)?.FilePath;
            if (filePath is null)
            {
                // text buffer is not saved to disk. LSP does not support calls without URIs. and Visual Studio does not
                // have a URI concept other than the file path.
                return;
            }

            // Obtain the LSP response and text snapshot used.
            var response = await DocumentSymbolsRequestAsync(
                _textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);
            if (response is null)
                return;

            var newTextSnapshot = response.Value.snapshot;
            var rawData = CreateDocumentSymbolData(response.Value.response, newTextSnapshot);

            // Now, go back to the UI and grab the prior view state we set, and the current UI values we want to update the data with.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var searchText = this.SearchText;
            var lastViewState = _lastViewState_onlyAccessFromUIThread;

            // Jump back to the BG to do all our work.
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            var searchTextChanged = searchText != lastViewState.SearchText;
            var oldViewModelItems = lastViewState.ViewModelItems;

            // if we got new data or the user changed the search text, recompute our items to correspond to this new state.
            // Apply whatever the current search text is to what the model returned, and produce the new items.
            var newViewModelItems = GetDocumentSymbolItemViewModels(
                SearchDocumentSymbolData(rawData, searchText, cancellationToken));

            // If the search text changed, just show everything in expanded form, so the user can see everything
            // that matched, without anything being hidden.
            //
            // in the case of no search text change, attempt to keep the same open/close expansion state from before.
            if (!searchTextChanged)
            {
                ApplyExpansionStateToNewItems(
                    oldSnapshot: lastViewState.TextSnapshot,
                    newSnapshot: newTextSnapshot,
                    oldItems: oldViewModelItems,
                    newItems: newViewModelItems);
            }

            // Now create an interval tree out of the view models.  This will allow us to easily find the intersecting
            // view models given any position in the file with any particular text snapshot.
            var intervalTree = SimpleIntervalTree.Create(new IntervalIntrospector(newTextSnapshot), Array.Empty<DocumentSymbolDataViewModel>());
            AddToIntervalTree(newViewModelItems);

            var newViewState = new DocumentOutlineViewState(
                newTextSnapshot,
                searchText,
                newViewModelItems,
                intervalTree);

            // Now, go back to the UI and set this as our current state.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Actually update our view items (wpf will take care of rerendering efficiently).
            this.DocumentSymbolViewModelItems = newViewModelItems;
            _lastViewState_onlyAccessFromUIThread = newViewState;

            // Finally, select the appropriate item based on the users current position.
            // ExpandAndSelectItemAtCaretPosition(_textView.Caret.Position);

            return;

            void AddToIntervalTree(ImmutableArray<DocumentSymbolDataViewModel> viewModels)
            {
                foreach (var model in viewModels)
                {
                    intervalTree.AddIntervalInPlace(model);
                    AddToIntervalTree(model.Children);
                }
            }

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

        public void ExpandAndSelectItemAtCaretPosition(CaretPosition position)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (this.IsNavigating)
                return;

            this.IsNavigating = true;
            try
            {
                var models = _lastViewState_onlyAccessFromUIThread.ViewModelItemsTree.GetIntervalsThatIntersectWith(
                    position.BufferPosition.Position, 0, new IntervalIntrospector(_textBuffer.CurrentSnapshot));

                if (models.Length == 0)
                    return;

                // Order from smallest to largest.  The smallest is the innermost and should be the one we actually select.
                // The others are the parents and we should expand those so the innermost one is visible.
                models = models.Sort(static (m1, m2) =>
                {
                    return m1.Data.RangeSpan.Span.Length - m2.Data.RangeSpan.Span.Length;
                });

                models[0].IsSelected = true;
                for (var i = 1; i < models.Length; i++)
                    models[i].IsExpanded = true;
            }
            finally
            {
                this.IsNavigating = false;
            }
        }

        //private DocumentOutlineViewState ComputeNewViewModelState(
        //    DocumentOutlineViewState lastViewState,
        //    ITextSnapshot textSnapshot,
        //    ImmutableArray<DocumentSymbolData> data,
        //    string searchText,
        //    CancellationToken cancellationToken)
        //{

        //    // Now that we've made all our changes, record that we've done so so we can see what has changed when future requests come in.
        //    // note: we are safe to record this on the BG as we are called serially and are the only place to read/write it.

        //    // Jump back to UI thread to set the current data.
        //    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        //    _lastPresentedData_onlyAccessSerially = (model, searchText, currentViewModelItems);

        //    return;
        //}

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
