// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageServiceBrokerShim;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Interaction logic for DocumentOutlineControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IVsCodeWindowEvents
    {
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; }
        public IVsCodeWindow CodeWindow { get; }

        private IThreadingContext ThreadingContext { get; }
        private ILanguageServiceBrokerShim LanguageServiceBroker { get; }

        /// <summary>
        /// The type of sorting applied to the document model.
        /// </summary>
        private SortOption SortOption { get; set; }

        /// <summary>
        /// Queue to batch up work to do to get the current document model. Used so we can batch up a lot of events 
        /// and only fetch the model once for every batch.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _getModelQueue;

        /// <summary>
        /// Queue to batch up work to do to update the current document model and UI.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _updateModelQueue;

        /// <summary>
        /// Queue to batch up work to do to highlight the currently selected symbol node.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _highlightNodeQueue;

        /// <summary>
        /// The text snapshot from when the document symbol request was made.
        /// </summary>
        private ITextSnapshot? LspSnapshot { get; set; }

        /// <summary>
        /// Keeps track of the current primary and secondary text views.
        /// </summary>
        private readonly Dictionary<IVsTextView, ITextView> _trackedTextViews = new();

        /// <summary>
        /// Stores the latest document model returned by GetModelAsync to be used by UpdateModelAsync.
        /// </summary>
        private ImmutableArray<DocumentSymbolViewModel> LatestFetchedDocumentModel { get; set; }

        /// <summary>
        /// Is true when LatestFetchedDocumentModel is not empty.
        /// </summary>
        [MemberNotNullWhen(true, nameof(LspSnapshot))]
        private bool LatestFetchedDocumentModelIsNotEmpty { get; set; }

        public DocumentOutlineControl(
            ILanguageServiceBrokerShim languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IVsCodeWindow codeWindow)
        {
            InitializeComponent();

            ThreadingContext = threadingContext;
            LanguageServiceBroker = languageServiceBroker;
            EditorAdaptersFactoryService = editorAdaptersFactoryService;
            CodeWindow = codeWindow;
            ComEventSink.Advise<IVsCodeWindowEvents>(codeWindow, this);
            SortOption = SortOption.Order;

            _getModelQueue = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.Short,
                    GetModelAsync,
                    asyncListener,
                    threadingContext.DisposalToken);

            _updateModelQueue = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.NearImmediate,
                    UpdateModelAsync,
                    asyncListener,
                    threadingContext.DisposalToken);

            _highlightNodeQueue = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.NearImmediate,
                    HightlightNodeAsync,
                    asyncListener,
                    threadingContext.DisposalToken);

            codeWindow.GetPrimaryView(out var pTextViewPrimary);
            StartTrackingView(pTextViewPrimary, out var textViewPrimary);

            // Primary text view should always exist on window initialization unless an error is thrown.
            if (textViewPrimary is null)
                return;

            codeWindow.GetSecondaryView(out var pTextViewSecondary);
            if (pTextViewSecondary is not null)
                StartTrackingView(pTextViewSecondary, out var _);

            StartGetModelTask();
        }

        int IVsCodeWindowEvents.OnNewView(IVsTextView pView)
        {
            StartTrackingView(pView, out var _);
            return VSConstants.S_OK;
        }

        private void StartTrackingView(IVsTextView pTextView, out IWpfTextView? wpfTextView)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            wpfTextView = null;
            if (pTextView != null)
            {
                wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(pTextView);
                if (wpfTextView != null)
                {
                    _trackedTextViews.Add(pTextView, wpfTextView);
                    wpfTextView.Caret.PositionChanged += Caret_PositionChanged;
                    wpfTextView.TextBuffer.Changed += TextBuffer_Changed;
                }
            }
        }

        int IVsCodeWindowEvents.OnCloseView(IVsTextView pView)
        {
            if (_trackedTextViews.TryGetValue(pView, out var view))
            {
                view.Caret.PositionChanged -= Caret_PositionChanged;
                view.TextBuffer.Changed -= TextBuffer_Changed;

                _trackedTextViews.Remove(pView);
            }

            return VSConstants.S_OK;
        }

        private void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            => StartGetModelTask();

        // On caret position change, highlight the corresponding symbol node
        private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (!e.NewPosition.Equals(e.OldPosition))
                StartHightlightNodeTask();
        }

        /// <summary>
        /// Fetches and processes the current document model.
        /// </summary>
        private async ValueTask GetModelAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            CodeWindow.GetLastActiveView(out var textView);
            var activeTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
            if (activeTextView is not null)
            {
                // Need to be on UI thread to get the file path.
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var filePath = GetFilePath(activeTextView);

                // Ensure we switch to the threadpool before calling DocumentSymbolsRequestAsync.  It ensures
                // that fetching and processing the document model is not done on the UI thread.
                await TaskScheduler.Default;
                var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(
                    activeTextView.TextBuffer, LanguageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

                if (response is not null)
                {
                    var responseBody = response.ToObject<DocumentSymbol[]>();
                    var documentSymbols = DocumentOutlineHelper.GetNestedDocumentSymbols(responseBody);
                    LatestFetchedDocumentModel = DocumentOutlineHelper.GetDocumentSymbolModels(documentSymbols);

                    LspSnapshot = activeTextView.TextSnapshot;
                    LatestFetchedDocumentModelIsNotEmpty = LatestFetchedDocumentModel.Length > 0;

                    StartModelUpdateTask();
                }
                else
                {
                    LatestFetchedDocumentModelIsNotEmpty = false;
                    LatestFetchedDocumentModel = ImmutableArray<DocumentSymbolViewModel>.Empty;
                }
            }
            else
            {
                LatestFetchedDocumentModelIsNotEmpty = false;
                LatestFetchedDocumentModel = ImmutableArray<DocumentSymbolViewModel>.Empty;
            }

            string? GetFilePath(IWpfTextView textView)
            {
                ThreadingContext.ThrowIfNotOnUIThread();
                if (textView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter) &&
                    bufferAdapter is IPersistFileFormat persistFileFormat &&
                    ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out _)))
                {
                    return filePath;
                }

                return null;
            }
        }

        /// <summary>
        /// Processes the fetched document model and updates the UI.
        /// </summary>
        private async ValueTask UpdateModelAsync(CancellationToken cancellationToken)
        {
            if (LatestFetchedDocumentModelIsNotEmpty)
            {
                var updatedSymbolTreeSource = LatestFetchedDocumentModel;

                // Switch to UI thread to obtain the search query.
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var searchQuery = searchBox.Text;

                // Switch to the threadpool to get the lastest text view and filter and sort the model.
                await TaskScheduler.Default;
                CodeWindow.GetLastActiveView(out var textView);
                var activeTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
                if (activeTextView is not null)
                {
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                        updatedSymbolTreeSource = DocumentOutlineHelper.Search(updatedSymbolTreeSource, searchQuery);

                    updatedSymbolTreeSource = DocumentOutlineHelper.Sort(updatedSymbolTreeSource, SortOption, LspSnapshot, activeTextView.TextSnapshot);

                    await UpdateSymbolTreeSourceAsync(updatedSymbolTreeSource, cancellationToken).ConfigureAwait(false);
                    StartHightlightNodeTask();
                }
            }
        }

        /// <summary>
        /// Switch to the UI thread to update the document symbol tree with the model data
        /// </summary>
        private async Task UpdateSymbolTreeSourceAsync(
            ImmutableArray<DocumentSymbolViewModel> updatedSymbolsTreeItemsSource,
            CancellationToken cancellationToken)
        {
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            symbolTree.ItemsSource = updatedSymbolsTreeItemsSource;
        }

        /// <summary>
        /// Highlights the symbol node corresponding to the current caret position in the editor.
        /// </summary>
        private async ValueTask HightlightNodeAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            if (LatestFetchedDocumentModelIsNotEmpty)
            {
                CodeWindow.GetLastActiveView(out var textView);
                var activeTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
                if (activeTextView is not null)
                {
                    var documentSymbolModelArray = ((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource).ToImmutableArray();
                    var caretPoint = activeTextView.GetCaretPoint(activeTextView.TextBuffer);
                    if (caretPoint.HasValue)
                    {
                        var caretPosition = caretPoint.Value.Position;
                        DocumentOutlineHelper.SelectDocumentNode(documentSymbolModelArray, activeTextView.TextSnapshot, LspSnapshot, caretPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a new task to get the current document model.
        /// </summary>
        private void StartGetModelTask()
        {
            _getModelQueue.AddWork();
        }

        /// <summary>
        /// Starts a new task to update the current document model.
        /// </summary>
        private void StartModelUpdateTask()
        {
            if (LatestFetchedDocumentModelIsNotEmpty)
                _updateModelQueue.AddWork();
        }

        /// <summary>
        /// Starts a new task to highlight the symbol node corresponding to the current caret position in the editor.
        /// </summary>
        private void StartHightlightNodeTask()
        {
            if (LatestFetchedDocumentModelIsNotEmpty)
                _highlightNodeQueue.AddWork();
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            DocumentOutlineHelper.SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            DocumentOutlineHelper.SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, false);
        }

        private void Search(object sender, EventArgs e)
        {
            StartModelUpdateTask();
        }

        private void SortByName(object sender, EventArgs e)
        {
            SortOption = SortOption.Name;
            StartModelUpdateTask();
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            SortOption = SortOption.Order;
            StartModelUpdateTask();
        }

        private void SortByType(object sender, EventArgs e)
        {
            SortOption = SortOption.Type;
            StartModelUpdateTask();
        }

        // When symbol node clicked, select the corresponding code
        private void JumpToContent(object sender, EventArgs e)
        {
            if (LatestFetchedDocumentModelIsNotEmpty)
            {
                CodeWindow.GetLastActiveView(out var textView);
                var activeTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
                if (activeTextView is not null && sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
                {
                    // Avoids highlighting the node after moving the caret ourselves 
                    // (The node is already highlighted on user click)
                    activeTextView.Caret.PositionChanged -= Caret_PositionChanged;

                    // Get the position of the start of the line the symbol is on
                    var position = LspSnapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position;

                    // Gets a point for this position with respect to the updated snapshot
                    var currentSnapshot = activeTextView.TextSnapshot;
                    var snapshotPoint = new SnapshotPoint(currentSnapshot, position);

                    // Sets the selection to this point
                    var snapshotSpan = new SnapshotSpan(snapshotPoint, snapshotPoint);
                    activeTextView.SetSelection(snapshotSpan);
                    activeTextView.ViewScroller.EnsureSpanVisible(snapshotSpan);

                    // We want to continue highlighting nodes when the user moves the caret
                    activeTextView.Caret.PositionChanged += Caret_PositionChanged;
                }
            }
        }
    }
}
