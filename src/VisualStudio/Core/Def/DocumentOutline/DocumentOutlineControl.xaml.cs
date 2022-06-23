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
        private IWpfTextView TextView { get; }
        private ITextBuffer TextBuffer { get; }
        private readonly string? _filePath;

        private SortOption SortOption { get; set; }

        /// <summary>
        /// Stores the result of GetModelAsync to be used by UpdateModelAsync.
        /// </summary>
        private ImmutableArray<DocumentSymbolViewModel> DocumentSymbolViewModels { get; set; }

        /// <summary>
        /// Is true when DocumentSymbolViewModels is not empty.
        /// </summary>
        private bool DocumentSymbolViewModelsIsInitialized { get; set; }

        /// <summary>
        /// Queue to batch up work to do to get the current document model. Used so we can batch up a lot of events 
        /// and only compute the model once for every batch.
        /// </summary>
        private readonly AsyncBatchingWorkQueue _getModelQueue;

        /// <summary>
        /// Queue to batch up work to do to update the current document model. Used so we can batch up a lot of 
        /// events and only compute the model once for every batch. 
        /// </summary>
        private readonly AsyncBatchingWorkQueue _updateModelQueue;

        public DocumentOutlineControl(
            IWpfTextView textView,
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
        {
            InitializeComponent();

            TextView = textView;
            TextBuffer = textBuffer;
            _filePath = GetFilePath(textView);
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

            async ValueTask GetModelAsync(CancellationToken cancellationToken)
            {
                // Ensure fetching and processing the model is done on a background thread
                await TaskScheduler.Default;
                var response = await DocumentSymbolsRequestAsync(textBuffer, languageServiceBroker, cancellationToken).ConfigureAwait(false);
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

            async ValueTask UpdateModelAsync(CancellationToken cancellationToken)
            {
                var updatedSymbolsTreeItemsSource = DocumentSymbolViewModels;

                // Switch to UI thread to obtain search query
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var searchQuery = searchBox.Text;

                // Switch to a background thread to filter and sort the model
                await TaskScheduler.Default;

                if (!string.IsNullOrWhiteSpace(searchQuery))
                    updatedSymbolsTreeItemsSource = DocumentOutlineHelper.Search(updatedSymbolsTreeItemsSource, searchQuery);

                updatedSymbolsTreeItemsSource = DocumentOutlineHelper.Sort(updatedSymbolsTreeItemsSource, SortOption);

                // Switch back to the UI thread to update the UI with the processed model data
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                symbolTree.ItemsSource = updatedSymbolsTreeItemsSource;
                HighlightSymbolNode();
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

        private async Task<ManualInvocationResponse?> DocumentSymbolsRequestAsync(
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            CancellationToken cancellationToken)
        {
            var parameterFactory = new DocumentSymbolParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(_filePath)
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
            var snapshot = TextBuffer.CurrentSnapshot;
            RoslynDebug.AssertNotNull(snapshot);
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
            {
                if (symbol.StartLine >= 0 && symbol.StartLine < snapshot.LineCount)
                {
                    var position = snapshot.GetLineFromLineNumber(symbol.StartLine).Start.Position;
                    if (position >= 0 && position <= snapshot.Length)
                    {
                        // Avoids highlighting the node after moving the caret ourselves 
                        // (The node is already highlighted on user click)
                        TextView.Caret.PositionChanged -= FollowCaret;
                        var point = new SnapshotPoint(snapshot, position);
                        var snapshotSpan = new SnapshotSpan(point, point);
                        TextView.SetSelection(snapshotSpan);
                        TextView.ViewScroller.EnsureSpanVisible(snapshotSpan);
                        // We want to continue highlighting nodes when the user moves the caret
                        TextView.Caret.PositionChanged += FollowCaret;
                    }
                }
            }
        }

        // On caret position change, highlight the corresponding symbol node
        private void FollowCaret(object sender, EventArgs e)
        {
            HighlightSymbolNode();
        }

        /// <summary>
        /// Highlights the symbol node corresponding to the current caret position.
        /// </summary>
        private void HighlightSymbolNode()
        {
            if (TextView is not null && DocumentSymbolViewModelsIsInitialized)
            {
                var caretPoint = TextView.GetCaretPoint(TextBuffer);
                if (caretPoint.HasValue)
                {
                    caretPoint.Value.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                    UnselectAll((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource);
                    foreach (DocumentSymbolViewModel documentSymbolModel in symbolTree.ItemsSource)
                        DocumentOutlineHelper.SelectNode(documentSymbolModel, lineNumber, characterIndex);
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
