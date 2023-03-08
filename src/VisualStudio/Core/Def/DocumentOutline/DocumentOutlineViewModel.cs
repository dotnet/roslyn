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
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using LspDocumentSymbol = DocumentSymbol;
    using Range = LanguageServer.Protocol.Range;

    /// <summary>
    /// Responsible for updating data related to Document outline. It is expected that all public methods on this type
    /// do not need to be on the UI thread. Two properties: <see cref="SortOption"/> and <see cref="SearchText"/> are
    /// intended to be bound to a WPF view and should only be set from the UI thread.
    /// </summary>
    internal sealed partial class DocumentOutlineViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly ITaggerEventSource _taggerEventSource;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ITextBuffer _textBuffer;
        private readonly IThreadingContext _threadingContext;

        private readonly DocumentSymbolDataModel _emptyModel;

        /// <summary>
        /// Queue that uses the language-server-protocol to get document symbol information.
        /// This queue can return null if it is called before and LSP server is registered for our document.
        /// </summary>
        private readonly AsyncBatchingResultQueue<DocumentSymbolDataModel> _documentSymbolQueue;

        /// <summary>
        /// Queue for updating the state of the view model
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ViewModelStateDataChange> _updateViewModelStateQueue;

        private CancellationToken CancellationToken => _cancellationTokenSource.Token;

        // Mutable state.  Should only update on UI thread.

        private SortOption _sortOption = SortOption.Location;
        private string? _searchText;
        private ImmutableArray<DocumentSymbolDataViewModel> _documentSymbolViewModelItems = ImmutableArray<DocumentSymbolDataViewModel>.Empty;

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
            _emptyModel = new DocumentSymbolDataModel(ImmutableArray<DocumentSymbolData>.Empty, _textBuffer.CurrentSnapshot);

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_threadingContext.DisposalToken);

            // work queue for refreshing LSP data
            _documentSymbolQueue = new AsyncBatchingResultQueue<DocumentSymbolDataModel>(
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

        public void Dispose()
        {
            _taggerEventSource.Changed -= OnEventSourceChanged;
            _taggerEventSource.Disconnect();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public SortOption SortOption
        {
            get => _sortOption;
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                SetProperty(ref _sortOption, value);
            }
        }

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

        public ImmutableArray<DocumentSymbolDataViewModel> DocumentSymbolViewModelItems
        {
            get => _documentSymbolViewModelItems;
            private set
            {
                _threadingContext.ThrowIfNotOnBackgroundThread();

                // Unselect any currently selected items or WPF will believe it needs to select the root node.
                UnselectAll(_documentSymbolViewModelItems);
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

            _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, CaretPositionOfNodeToSelect: null, ShouldExpand: null, DataUpdated: true));

            return model;
        }
        /// <summary>
        /// Makes an LSP document symbol request and returns the response and the text snapshot used at 
        /// the time the LSP client sends the request to the server.
        /// </summary>
        public static async Task<(JToken response, ITextSnapshot snapshot)?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            string textViewFilePath,
            CancellationToken cancellationToken)
        {
            ITextSnapshot? requestSnapshot = null;
            JToken ParameterFunction(ITextSnapshot snapshot)
            {
                requestSnapshot = snapshot;
                return JToken.FromObject(new RoslynDocumentSymbolParams()
                {
                    UseHierarchicalSymbols = true,
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = new Uri(textViewFilePath)
                    }
                });
            }

            var response = (await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: _ => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: ParameterFunction,
                cancellationToken: cancellationToken).ConfigureAwait(false))?.Response;

            // The request snapshot or response can be null if there is no LSP server implementation for
            // the document symbol request for that language.
            return requestSnapshot is null || response is null ? null : (response, requestSnapshot);
        }

        /// <summary>
        /// Given an array of Document Symbols in a document, returns a DocumentSymbolDataModel.
        /// </summary>
        /// 
        /// As of right now, the LSP document symbol response only has at most 2 levels of nesting, 
        /// so we nest the symbols first before converting the LSP DocumentSymbols to DocumentSymbolData.
        /// 
        /// Example file structure:
        /// Class A
        ///     ClassB
        ///         Method1
        ///         Method2
        ///         
        /// LSP document symbol response:
        /// [
        ///     {
        ///         Name: ClassA,
        ///         Children: []
        ///     },
        ///     {
        ///         Name: ClassB,
        ///         Children: 
        ///         [
        ///             {
        ///                 Name: Method1,
        ///                 Children: []
        ///             },
        ///             {
        ///                 Name: Method2,
        ///                 Children: []
        ///             }
        ///         ]
        ///     }
        /// ]
        public static DocumentSymbolDataModel CreateDocumentSymbolDataModel(LspDocumentSymbol[] documentSymbols, ITextSnapshot originalSnapshot)
        {
            // Obtain a flat list of all the document symbols sorted by location in the document.
            var allSymbols = documentSymbols
                .SelectMany(x => x.Children)
                .Concat(documentSymbols)
                .OrderBy(x => x.Range.Start.Line)
                .ThenBy(x => x.Range.Start.Character)
                .ToImmutableArray();

            // Iterate through the document symbols, nest them, and add the top level symbols to finalResult.
            using var _1 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var finalResult);
            var currentStart = 0;
            while (currentStart < allSymbols.Length)
                finalResult.Add(NestDescendantSymbols(allSymbols, currentStart, out currentStart));

            return new DocumentSymbolDataModel(finalResult.ToImmutable(), originalSnapshot);

            // Returns the symbol in the list at index start (the parent symbol) with the following symbols in the list
            // (descendants) appropriately nested into the parent.
            DocumentSymbolData NestDescendantSymbols(ImmutableArray<LspDocumentSymbol> allSymbols, int start, out int newStart)
            {
                var currentParent = allSymbols[start];
                start++;
                newStart = start;

                // Iterates through the following symbols and checks whether the next symbol is in range of the parent and needs
                // to be nested into the current parent symbol (along with following symbols that may be siblings/grandchildren/etc)
                // or if the next symbol is a new parent.
                using var _2 = ArrayBuilder<DocumentSymbolData>.GetInstance(out var currentSymbolChildren);
                while (newStart < allSymbols.Length)
                {
                    var nextSymbol = allSymbols[newStart];

                    // If the next symbol in the list is not in range of the current parent (i.e. is a new parent), break.
                    if (!Contains(currentParent, nextSymbol))
                        break;

                    // Otherwise, nest this child symbol and add it to currentSymbolChildren.
                    currentSymbolChildren.Add(NestDescendantSymbols(allSymbols, start: newStart, out newStart));
                }

                // Return the nested parent symbol.
                return new DocumentSymbolData(
                    currentParent.Name,
                    currentParent.Kind,
                    GetSymbolRangeSpan(currentParent.Range),
                    GetSymbolRangeSpan(currentParent.SelectionRange),
                    currentSymbolChildren.ToImmutable());
            }

            // Returns whether the child symbol is in range of the parent symbol.
            static bool Contains(LspDocumentSymbol parent, LspDocumentSymbol child)
                => child.Range.Start.Line > parent.Range.Start.Line && child.Range.End.Line < parent.Range.End.Line;

            // Converts a Document Symbol Range to a SnapshotSpan within the text snapshot used for the LSP request.
            SnapshotSpan GetSymbolRangeSpan(Range symbolRange)
            {
                var originalStartPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.Start.Line).Start.Position + symbolRange.Start.Character;
                var originalEndPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.End.Line).Start.Position + symbolRange.End.Character;

                return new SnapshotSpan(originalSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
            }
        }

        private void EnqueueFilter(string? newText)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(newText, CaretPositionOfNodeToSelect: null, ShouldExpand: null, DataUpdated: false));

        public void EnqueueSelectTreeNode(CaretPosition caretPoint)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, caretPoint, ShouldExpand: null, DataUpdated: false));

        public void EnqueueExpandOrCollapse(bool shouldExpand)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateDataChange(SearchText, CaretPositionOfNodeToSelect: null, ShouldExpand: shouldExpand, DataUpdated: false));

        private async ValueTask UpdateViewModelStateAsync(ImmutableSegmentedList<ViewModelStateDataChange> viewModelStateData, CancellationToken cancellationToken)
        {
            // We do not want this work running on a background thread
            await TaskScheduler.Default;
            cancellationToken.ThrowIfCancellationRequested();

            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            model ??= _emptyModel;

            var searchText = FindLastPresentValue(viewModelStateData, static x => x.SearchText);
            var position = FindLastPresentValue(viewModelStateData, static x => x.CaretPositionOfNodeToSelect);
            var expansion = FindLastPresentValue(viewModelStateData, static x => x.ShouldExpand);
            var dataUpdated = viewModelStateData.Any(static x => x.DataUpdated);

            // These updates always require a valid model to perform
            if (string.IsNullOrEmpty(searchText) && dataUpdated)
            {
                var documentSymbolViewModelItems = GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                DocumentSymbolViewModelItems = documentSymbolViewModelItems;
            }

            if (searchText is { } currentQuery)
            {
                ImmutableArray<DocumentSymbolDataViewModel> documentSymbolViewModelItems;
                if (currentQuery == string.Empty)
                {
                    // search was cleared, show all data.
                    documentSymbolViewModelItems = GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                    ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                }
                else
                {
                    // We are going to show results so we unset any expand / collapse state
                    // If we are in the middle of searching the developer should always be able to see the results
                    // so we don't want to collapse (and therefore hide) data here.
                    documentSymbolViewModelItems = GetDocumentSymbolItemViewModels(
                         SearchDocumentSymbolData(model.DocumentSymbolData, currentQuery, cancellationToken));
                }

                DocumentSymbolViewModelItems = documentSymbolViewModelItems;
            }

            if (position is { } currentPosition)
            {
                var caretPoint = currentPosition.Point.GetPoint(_textBuffer, PositionAffinity.Predecessor);
                // If the we can't find the caret then the document has changed since we were queued to the point that we can't find this position.
                if (caretPoint.HasValue)
                {
                    var symbolToSelect = GetDocumentNodeToSelect(DocumentSymbolViewModelItems, model.OriginalSnapshot, caretPoint.Value);
                    // If null this means we can't find the symbol to select. The document has changed enough since we are queued that we can't find anything applicable.
                    if (symbolToSelect is not null)
                    {
                        ExpandAncestors(DocumentSymbolViewModelItems, symbolToSelect.Data.SelectionRangeSpan);
                        symbolToSelect.IsSelected = true;
                    }
                }
            }

            // If we aren't filtering to search results do expand/collapse
            if (expansion is { } expansionOption && string.IsNullOrEmpty(searchText))
            {
                SetExpansionOption(DocumentSymbolViewModelItems, expansionOption);
            }
            else if (expansion is { } shouldExpand)
            {
                SetExpansionOption(DocumentSymbolViewModelItems, shouldExpand);
            }

            static void ApplyExpansionStateToNewItems(ImmutableArray<DocumentSymbolDataViewModel> oldItems, ImmutableArray<DocumentSymbolDataViewModel> newItems)
            {
                if (AreAllTopLevelItemsCollapsed(oldItems))
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
                    SetIsExpandedOnNewItems(newItems, oldItems);
                }

                static bool AreAllTopLevelItemsCollapsed(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolViewModelItems)
                {
                    if (!documentSymbolViewModelItems.Any())
                    {
                        // We are operating on an empty array this can happen if the LSP service hasn't populated us with any data yet.
                        return false;
                    }

                    // No need to recurse, if all the items are collapsed then so are their children
                    foreach (var item in documentSymbolViewModelItems)
                    {
                        if (item.IsExpanded)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            static TResult? FindLastPresentValue<TSource, TResult>(ImmutableSegmentedList<TSource> source, Func<TSource, TResult?> selector)
            {
                for (var i = source.Count - 1; i >= 0; i--)
                {
                    var item = selector(source[i]);
                    if (item is not null)
                        return item;
                }

                return default;
            }
        }

        /// <summary>
        /// Converts an immutable array of DocumentSymbolData to an immutable array of <see cref="DocumentSymbolDataViewModel"/>.
        /// </summary>
        public static ImmutableArray<DocumentSymbolDataViewModel> GetDocumentSymbolItemViewModels(ImmutableArray<DocumentSymbolData> documentSymbolData)
        {
            using var _ = ArrayBuilder<DocumentSymbolDataViewModel>.GetInstance(out var documentSymbolItems);
            foreach (var documentSymbol in documentSymbolData)
            {
                var children = GetDocumentSymbolItemViewModels(documentSymbol.Children);
                var documentSymbolItem = new DocumentSymbolDataViewModel(
                    documentSymbol,
                    children,
                    isExpanded: true,
                    isSelected: false);
                documentSymbolItems.Add(documentSymbolItem);
            }

            return documentSymbolItems.ToImmutable();
        }

        public static void UnselectAll(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems)
        {
            foreach (var documentSymbolItem in documentSymbolItems)
            {
                // Setting a Boolean property on this item is allowed to happen on any thread.
                documentSymbolItem.IsSelected = false;
                UnselectAll(documentSymbolItem.Children);
            }
        }

        public static void SetExpansionOption(
            ImmutableArray<DocumentSymbolDataViewModel> currentDocumentSymbolItems,
            bool expand)
        {
            foreach (var item in currentDocumentSymbolItems)
            {
                item.IsExpanded = expand;
                SetExpansionOption(item.Children, expand);
            }
        }

        /// <summary>
        /// Expands all the ancestors of a <see cref="DocumentSymbolDataViewModel"/>.
        /// </summary>
        public static void ExpandAncestors(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems, SnapshotSpan documentSymbolRangeSpan)
        {
            var symbol = GetSymbolInRange(documentSymbolItems, documentSymbolRangeSpan);
            if (symbol is not null)
            {
                // Setting a boolean property on this View Model can happen on any thread.
                symbol.IsExpanded = true;
                ExpandAncestors(symbol.Children, documentSymbolRangeSpan);
            }

            static DocumentSymbolDataViewModel? GetSymbolInRange(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems, SnapshotSpan rangeSpan)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.Data.RangeSpan.Contains(rangeSpan))
                        return symbol;
                }

                return null;
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

        /// <summary>
        /// Returns the Document Symbol node that is currently selected by the caret in the editor if it exists.
        /// </summary>
        public static DocumentSymbolDataViewModel? GetDocumentNodeToSelect(
            ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems,
            ITextSnapshot originalSnapshot,
            SnapshotPoint currentCaretPoint)
        {
            var originalCaretPoint = currentCaretPoint.TranslateTo(originalSnapshot, PointTrackingMode.Negative);
            return GetNodeToSelect(documentSymbolItems, null);

            DocumentSymbolDataViewModel? GetNodeToSelect(ImmutableArray<DocumentSymbolDataViewModel> documentSymbols, DocumentSymbolDataViewModel? parent)
            {
                var selectedSymbol = GetNodeSelectedByCaret(documentSymbols);

                if (selectedSymbol is null)
                    return parent;

                return GetNodeToSelect(selectedSymbol.Children, selectedSymbol);
            }

            // Returns a DocumentSymbolItem if the current caret position is in its range and null otherwise.
            DocumentSymbolDataViewModel? GetNodeSelectedByCaret(ImmutableArray<DocumentSymbolDataViewModel> documentSymbolItems)
            {
                foreach (var symbol in documentSymbolItems)
                {
                    if (symbol.Data.RangeSpan.IntersectsWith(originalCaretPoint))
                        return symbol;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns an immutable array of DocumentSymbolData such that each node matches the given pattern.
        /// </summary>
        public static ImmutableArray<DocumentSymbolData> SearchDocumentSymbolData(
            ImmutableArray<DocumentSymbolData> documentSymbolData,
            string pattern,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = ArrayBuilder<DocumentSymbolData>.GetInstance(out var filteredDocumentSymbols);
            var patternMatcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);

            foreach (var documentSymbol in documentSymbolData)
            {
                var filteredChildren = SearchDocumentSymbolData(documentSymbol.Children, pattern, cancellationToken);
                if (SearchNodeTree(documentSymbol, patternMatcher, cancellationToken))
                    filteredDocumentSymbols.Add(documentSymbol with { Children = filteredChildren });
            }

            return filteredDocumentSymbols.ToImmutable();

            // Returns true if the name of one of the tree nodes results in a pattern match.
            static bool SearchNodeTree(DocumentSymbolData tree, PatternMatcher patternMatcher, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return patternMatcher.Matches(tree.Name) || tree.Children.Any(c => SearchNodeTree(c, patternMatcher, cancellationToken));
            }
        }
    }
}
