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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// Responsible for updating data related to Document outline.
    /// It is expected that all operations this type do not need to be on the UI thread.
    /// </summary>
    internal sealed partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly ITaggerEventSource _taggerEventSource;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITextBuffer _textBuffer;
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Queue that uses the language-server-protocol to get document symbol information.
        /// This queue can return null if it is called before and LSP server is registered for our document.
        /// </summary>
        private readonly AsyncBatchingResultQueue<DocumentSymbolDataModel?> _documentSymbolQueue;

        /// <summary>
        /// Queue for updating the state of the view model
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ViewModelStateDataChange> _updateViewModelStateQueue;

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public DocumentOutlineViewModel(
            ILanguageServiceBroker2 languageServiceBroker,
            IAsynchronousOperationListener asyncListener,
            ITaggerEventSource taggerEventSource,
            ITextBuffer textBuffer,
            IThreadingContext threadingContext)
        {
            _languageServiceBroker = languageServiceBroker;
            _taggerEventSource = taggerEventSource;
            _textBuffer = textBuffer;
            _threadingContext = threadingContext;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_threadingContext.DisposalToken);

            // work queue for refreshing LSP data
            _documentSymbolQueue = new AsyncBatchingResultQueue<DocumentSymbolDataModel?>(
                DelayTimeSpan.Short,
                GetDocumentSymbolAsync,
                asyncListener,
                CancellationToken);

            // work queue for updating UI state
            _updateViewModelStateQueue = new AsyncBatchingWorkQueue<ViewModelStateDataChange>(
                DelayTimeSpan.Short,
                UpdateViewModelStateAsync,
                asyncListener,
                CancellationToken);

            _taggerEventSource.Changed += OnEventSourceChanged;
            _taggerEventSource.Connect();

            // queue initial model update
            _documentSymbolQueue.AddWork(cancelExistingWork: true);
        }

        private SortOption _sortOption = SortOption.Location;
        public SortOption SortOption
        {
            get => _sortOption;
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                SetProperty(ref _sortOption, value);
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                _searchText = value;
                EnqueueFilter(_searchText);
            }
        }

        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems = ImmutableArray<DocumentSymbolDataViewModel>.Empty;
        public ImmutableArray<DocumentSymbolDataViewModel> DocumentSymbolViewModelItems
        {
            get => _documentSymbolViewModelItems;
            set
            {
                _threadingContext.ThrowIfNotOnBackgroundThread();

                // Unselect any currently selected items or WPF will believe it needs to select the root node.
                DocumentOutlineHelper.UnselectAll(_documentSymbolViewModelItems);
                SetProperty(ref _documentSymbolViewModelItems, value);
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
            {
                return;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
        }

        public void Dispose()
        {
            _taggerEventSource.Changed -= OnEventSourceChanged;
            _taggerEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private async ValueTask<DocumentSymbolDataModel?> GetDocumentSymbolAsync(CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            cancellationToken.ThrowIfCancellationRequested();

            var textBuffer = _textBuffer;
            var filePath = _textBuffer.GetRelatedDocuments().FirstOrDefault(static d => d.FilePath is not null)?.FilePath;
            if (filePath is null)
            {
                // text buffer is not saved to disk
                // LSP does not support calls without URIs
                // and Visual Studio does not have a URI concept other than the file path.
                return null;
            }

            // Obtain the LSP response and text snapshot used.
            var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(
                textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

            // If there is no matching LSP server registered the client will return null here - e.g. wrong content type on the buffer, the
            // server totally failed to start, server doesn't support the right capabilities. For C# we might know it's a bug if we get a null
            // response here, but we don't know that in general for all languages.
            // see "Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient.AlwaysActivateInProcLanguageClient" for the list of content types we register for.
            // At this time the expected list is C#, Visual Basic, and F#
            if (response is null)
            {
                return null;
            }

            var responseBody = response.Value.response.ToObject<DocumentSymbol[]>();
            // It would be a bug in the LSP server implementation if we get back a null result here.
            Assumes.NotNull(responseBody);

            var model = DocumentOutlineHelper.CreateDocumentSymbolDataModel(responseBody, response.Value.snapshot);

            _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, CaretPositionOfNodeToSelect: null, ShouldExpand: null, DataUpdated: true));

            return model;
        }

        private void EnqueueFilter(string? newText)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(newText, CaretPositionOfNodeToSelect: null, ShouldExpand: null, DataUpdated: false));

        public void EnqueueSelectTreeNode(CaretPosition caretPoint)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, caretPoint, ShouldExpand: null, DataUpdated: false));

        public void EnqueueExpandOrCollapse(bool shouldExpand)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, CaretPositionOfNodeToSelect: null, ShouldExpand: shouldExpand, DataUpdated: false));

        private async ValueTask UpdateViewModelStateAsync(ImmutableSegmentedList<ViewModelStateDataChange> viewModelStateData, CancellationToken cancellationToken)
        {
            _threadingContext.ThrowIfNotOnBackgroundThread();
            cancellationToken.ThrowIfCancellationRequested();

            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            var searchText = viewModelStateData.SelectLastNonNullOrDefault(static x => x.SearchText);
            var position = viewModelStateData.SelectLastNonNullOrDefault(static x => x.CaretPositionOfNodeToSelect);
            var expansion = viewModelStateData.SelectLastNonNullOrDefault(static x => x.ShouldExpand);
            var dataUpdated = viewModelStateData.Any(static x => x.DataUpdated);

            // These updates always require a valid model to perform
            if (model is not null)
            {
                if (string.IsNullOrEmpty(searchText) && dataUpdated)
                {
                    var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                    ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                    DocumentSymbolViewModelItems = documentSymbolViewModelItems;
                }

                if (searchText is { } currentQuery)
                {
                    ImmutableArray<DocumentSymbolDataViewModel> documentSymbolViewModelItems;
                    if (currentQuery == string.Empty)
                    {
                        // search was cleared, show all data.
                        documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                        ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                    }
                    else
                    {
                        // We are going to show results so we unset any expand / collapse state
                        documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(
                             DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, currentQuery, cancellationToken));
                    }

                    DocumentSymbolViewModelItems = documentSymbolViewModelItems;
                }

                if (position is { } currentPosition)
                {
                    var caretPoint = currentPosition.Point.GetPoint(_textBuffer, PositionAffinity.Predecessor);
                    if (!caretPoint.HasValue)
                    {
                        // document has changed since we were queued to the point that we can't find this position.
                        return;
                    }

                    var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(DocumentSymbolViewModelItems, model.OriginalSnapshot, caretPoint.Value);
                    if (symbolToSelect is null)
                    {
                        return;
                    }

                    DocumentOutlineHelper.ExpandAncestors(DocumentSymbolViewModelItems, symbolToSelect.Data.SelectionRangeSpan);
                    symbolToSelect.IsSelected = true;
                }
            }

            // If we aren't filtering to search results do expand/collapse
            if (expansion is { } expansionOption && string.IsNullOrEmpty(searchText))
            {
                DocumentOutlineHelper.SetExpansionOption(DocumentSymbolViewModelItems, expansionOption);
            }
            else if (expansion is { } shouldExpand)
            {
                DocumentOutlineHelper.SetExpansionOption(DocumentSymbolViewModelItems, shouldExpand);
            }

            static void ApplyExpansionStateToNewItems(ImmutableArray<DocumentSymbolDataViewModel> oldItems, ImmutableArray<DocumentSymbolDataViewModel> newItems)
            {
                if (DocumentOutlineHelper.AreAllTopLevelItemsCollapsed(oldItems))
                {
                    // new nodes are un-collapsed by default
                    // we want to collapse all new top-level nodes if 
                    // everything else currently is so things aren't "jumpy"
                    foreach (var item in newItems)
                    {
                        item.IsExpanded = false;
                    }
                }
                else
                {
                    DocumentOutlineHelper.SetIsExpandedOnNewItems(newItems, oldItems);
                }
            }
        }
    }
}
