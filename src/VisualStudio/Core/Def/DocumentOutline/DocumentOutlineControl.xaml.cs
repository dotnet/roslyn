// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Interaction logic for DocumentOutlineControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IOleCommandTarget
    {
        /// <summary>
        /// The text view of the editor the user is currently interacting with.
        /// </summary>
        private IWpfTextView TextView { get; }

        /// <summary>
        /// The text buffer of the editor the user is currently interacting with.
        /// </summary>
        private ITextBuffer TextBuffer { get; }

        /// <summary>
        /// The text snapshot from when the document symbol request was made.
        /// </summary>
        private ITextSnapshot Snapshot { get; set; }

        private readonly string? _textViewFilePath;

        private SortOption SortOption { get; set; }

        private IThreadingContext ThreadingContext { get; }

        /// <summary>
        /// Stores the latest document model returned by GetModelAsync to be used by UpdateModelAsync.
        /// </summary>
        private ImmutableArray<DocumentSymbolViewModel> DocumentSymbolViewModels { get; set; }

        /// <summary>
        /// Is true when DocumentSymbolViewModels is not empty.
        /// </summary>
        private bool DocumentSymbolViewModelsIsInitialized { get; set; }

        /// <summary>
        /// Queue to batch up work to do to get the current document model. Used so we can batch up a lot of events 
        /// and only fetch the model once for every batch.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _getModelQueue;

        /// <summary>
        /// Queue to batch up work to do to update the current document model. Used so we can batch up a lot of 
        /// events and update the model and UI once for every batch. 
        /// </summary>
        private readonly AsyncBatchingWorkQueue _updateModelQueue;

        /// <summary>
        /// _followCursorQueue
        /// </summary>
        private readonly AsyncBatchingWorkQueue _highlightNodeQueue;

        public DocumentOutlineControl(
            IWpfTextView textView,
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
        {
            InitializeComponent();

            ThreadingContext = threadingContext;
            TextView = textView;
            TextBuffer = textBuffer;
            Snapshot = textView.TextSnapshot;
            _textViewFilePath = GetFilePath(textView);
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

            // Fetches and processes the current document model. Everything should be done on background threads.
            async ValueTask GetModelAsync(CancellationToken cancellationToken)
            {
                await TaskScheduler.Default;
                var response = await DocumentSymbolsRequestAsync(textBuffer, languageServiceBroker, cancellationToken).ConfigureAwait(false);
                Snapshot = textView.TextSnapshot;
                if (response?.Response is not null)
                {
                    var responseBody = response.Response.ToObject<DocumentSymbol[]>();
                    var documentSymbols = DocumentOutlineHelper.GetNestedDocumentSymbols(responseBody);
                    DocumentSymbolViewModels = DocumentOutlineHelper.GetDocumentSymbolModels(documentSymbols);
                    DocumentSymbolViewModelsIsInitialized = DocumentSymbolViewModels.Length > 0;
                    StartModelUpdateTask();
                }
                else
                {
                    DocumentSymbolViewModelsIsInitialized = false;
                    DocumentSymbolViewModels = ImmutableArray<DocumentSymbolViewModel>.Empty;
                }
            }

            // Processes the fetched document model and updates the UI.
            async ValueTask UpdateModelAsync(CancellationToken cancellationToken)
            {
                var updatedSymbolsTreeItemsSource = DocumentSymbolViewModels;

                // Switch to UI thread to obtain search query and the current text snapshot
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var searchQuery = searchBox.Text;
                var currentSnapshot = textBuffer.CurrentSnapshot;

                // Switch to a background thread to filter and sort the model
                await TaskScheduler.Default;

                if (!string.IsNullOrWhiteSpace(searchQuery))
                    updatedSymbolsTreeItemsSource = DocumentOutlineHelper.Search(updatedSymbolsTreeItemsSource, searchQuery);

                updatedSymbolsTreeItemsSource = DocumentOutlineHelper.Sort(updatedSymbolsTreeItemsSource, SortOption, Snapshot, currentSnapshot);

                // Switch back to the UI thread to update the UI with the processed model data
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                symbolTree.ItemsSource = updatedSymbolsTreeItemsSource;
                StartHightlightNodeTask();
            }

            // Highlights the symbol node corresponding to the current caret position in the editor.
            async ValueTask HightlightNodeAsync(CancellationToken cancellationToken)
            {
                if (TextView is not null && DocumentSymbolViewModelsIsInitialized)
                {
                    // Get the current snapshot, caret point, and document model on the UI thread
                    var currentSnapshot = TextBuffer.CurrentSnapshot;
                    var symbolTreeItemsSource = (IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource;
                    var caretPoint = TextView.GetCaretPoint(TextBuffer);
                    if (caretPoint.HasValue)
                    {
                        var caretPosition = caretPoint.Value.Position;
                        // Switch to a background thread to update UI selection
                        await TaskScheduler.Default;
                        UnselectAll(symbolTreeItemsSource);
                        foreach (var documentSymbolModel in symbolTreeItemsSource)
                            DocumentOutlineHelper.SelectNode(documentSymbolModel, currentSnapshot, Snapshot, caretPosition);
                    }
                }

                static void UnselectAll(IEnumerable<DocumentSymbolViewModel> documentSymbolModels)
                {
                    foreach (var documentSymbolModel in documentSymbolModels)
                    {
                        documentSymbolModel.IsSelected = false;
                        UnselectAll(documentSymbolModel.Children);
                    }
                }
            }

            void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
                => _getModelQueue.AddWork();

            TextView.Caret.PositionChanged += FollowCaret;
            TextView.TextBuffer.Changed += TextBuffer_Changed;

            _getModelQueue.AddWork();
        }

        /// <summary>
        /// Starts a new task to update the current document model.
        /// </summary>
        private void StartModelUpdateTask()
        {
            if (DocumentSymbolViewModelsIsInitialized)
                _updateModelQueue.AddWork();
        }

        /// <summary>
        /// Starts a new task to highlight the symbol node corresponding to the current caret position in the editor.
        /// </summary>
        private void StartHightlightNodeTask()
        {
            if (DocumentSymbolViewModelsIsInitialized)
                _highlightNodeQueue.AddWork();
        }

        private async Task<ManualInvocationResponse?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            CancellationToken cancellationToken)
        {
            var parameterFactory = new DocumentSymbolParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(_textViewFilePath)
                }
            };

            // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
            return await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: (JToken x) => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: _ => JToken.FromObject(parameterFactory),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static string? GetFilePath(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (textView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter) &&
                bufferAdapter is IPersistFileFormat persistFileFormat &&
                ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out _)))
            {
                return filePath;
            }

            return null;
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, false);
        }

        private void SetIsExpanded(IEnumerable<DocumentSymbolViewModel> documentSymbolModels, bool isExpanded)
        {
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                documentSymbolModel.IsExpanded = isExpanded;
                SetIsExpanded(documentSymbolModel.Children, isExpanded);
            }
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
            var currentSnapshot = TextBuffer.CurrentSnapshot;
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
            {
                // Avoids highlighting the node after moving the caret ourselves 
                // (The node is already highlighted on user click)
                TextView.Caret.PositionChanged -= FollowCaret;

                // Get the position of the start of the line the symbol is on
                var position = Snapshot.GetLineFromLineNumber(symbol.StartPosition.Line).Start.Position;

                // Gets a point for this position with respect to the updated snapshot
                var snapshotPoint = new SnapshotPoint(currentSnapshot, position);

                // Sets the selection to this point
                var snapshotSpan = new SnapshotSpan(snapshotPoint, snapshotPoint);
                TextView.SetSelection(snapshotSpan);
                TextView.ViewScroller.EnsureSpanVisible(snapshotSpan);

                // We want to continue highlighting nodes when the user moves the caret
                TextView.Caret.PositionChanged += FollowCaret;
            }
        }

        // On caret position change, highlight the corresponding symbol node
        private void FollowCaret(object sender, CaretPositionChangedEventArgs e)
        {
            StartHightlightNodeTask();
        }

        internal const int OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100);

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // we don't support any commands like rename/undo in this view yet
            return OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return VSConstants.S_OK;
        }
    }
}
