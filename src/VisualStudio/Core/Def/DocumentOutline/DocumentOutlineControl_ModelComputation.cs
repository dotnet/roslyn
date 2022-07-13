// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private IWpfTextView? GetLastActiveIWpfTextView()
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(CodeWindow.GetLastActiveView(out var textView)))
                return null;

            return EditorAdaptersFactoryService.GetWpfTextView(textView);
        }

        /// <summary>
        /// Starts a new task to compute the data model.
        /// </summary>
        private void StartComputeDataModelTask()
        {
            // 'true' value is unused.  this just signals to the queue that we have work to do.
            _computeDataModelQueue.AddWork(true);
        }

        /// <summary>
        /// Creates the data model.
        /// </summary>
        private async ValueTask<DocumentSymbolDataModel?> ComputeDataModelAsync(ImmutableSegmentedList<bool> _, CancellationToken cancellationToken)
        {
            // Jump to the UI thread to get the currently active text view.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return null;

            var originalSnapshot = activeTextView.TextSnapshot;
            var textBuffer = activeTextView.TextBuffer;

            var filePath = GetFilePath();
            if (filePath is null)
                return null;

            // Ensure we switch to the threadpool before calling ComputeUIModelAsync. It ensures
            // that fetching and processing the document model is not done on the UI thread.
            await TaskScheduler.Default;

            var model = await ComputeDataModelAsync().ConfigureAwait(false);

            if (model is not null)
                StartUpdateDataModelTask();

            return model;

            async Task<DocumentSymbolDataModel?> ComputeDataModelAsync()
            {
                var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(
                    textBuffer, LanguageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

                if (response is null)
                    return null;

                var responseBody = response.ToObject<DocumentSymbol[]>();
                if (responseBody is null)
                    return null;

                var documentSymbols = DocumentOutlineHelper.GetNestedDocumentSymbols(responseBody);
                var documentSymbolData = DocumentOutlineHelper.GetDocumentSymbolData(documentSymbols, originalSnapshot);
                return new DocumentSymbolDataModel(documentSymbolData, originalSnapshot);
            }

            string? GetFilePath()
            {
                ThreadingContext.ThrowIfNotOnUIThread();
                if (EditorAdaptersFactoryService.GetBufferAdapter(textBuffer) is IPersistFileFormat persistFileFormat &&
                    ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out var _)))
                {
                    return filePath;
                }

                return null;
            }
        }

        /// <summary>
        /// Starts a new task to update the UI model.
        /// </summary>
        private void StartUpdateDataModelTask()
        {
            // 'true' value is unused.  this just signals to the queue that we have work to do.
            _updateDataModelQueue.AddWork(true);
        }

        /// <summary>
        /// Filters and sorts the data model.
        /// </summary>
        private async ValueTask<DocumentSymbolDataModel?> UpdateDataModelAsync(ImmutableSegmentedList<bool> _, CancellationToken cancellationToken)
        {
            var model = await _computeDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return null;

            // Switch to the UI thread to get the current search query and latest active text view.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var searchQuery = searchBox.Text;

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return null;

            // Switch to the threadpool to filter and sort the data model.
            await TaskScheduler.Default;

            var updatedDocumentSymbolData = model.DocumentSymbolData;

            if (!string.IsNullOrWhiteSpace(searchQuery))
                updatedDocumentSymbolData = DocumentOutlineHelper.Search(updatedDocumentSymbolData, searchQuery, cancellationToken);

            updatedDocumentSymbolData = DocumentOutlineHelper.Sort(updatedDocumentSymbolData, SortOption, cancellationToken);

            StartHightlightNodeTask(ExpansionOption.NoChange);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }

        /// <summary>
        /// Starts a new task to highlight the symbol node corresponding to the current caret position in the editor, expand/collapse
        /// nodes (if applicable), and updates the UI.
        /// </summary>
        private void StartHightlightNodeTask(ExpansionOption expansionOption)
        {
            _highlightAndExpandNodesQueue.AddWork(expansionOption);
        }

        /// <summary>
        /// Highlights the symbol node corresponding to the current caret position in the editor, expands/collapses nodes, then updates the UI.
        /// </summary>
        private async ValueTask HightlightNodeAsync(ImmutableSegmentedList<ExpansionOption> expansionOption, CancellationToken cancellationToken)
        {
            var model = await _updateDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return;

            // Switch to the UI thread to get the current caret point and latest active text view.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return;

            var caretPoint = activeTextView.GetCaretPoint(activeTextView.TextBuffer);
            if (!caretPoint.HasValue)
                return;

            // Switch to the threadpool to determine which node is currently selected and which node to select (if they exist).
            await TaskScheduler.Default;

            var documentSymbolUIItems = DocumentOutlineHelper.GetDocumentSymbolUIItems(model.DocumentSymbolData);

            //var currentlySelectedSymbol = DocumentOutlineHelper.GetCurrentlySelectedNode(documentSymbolUIItems);
            var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(documentSymbolUIItems, model.OriginalSnapshot, caretPoint.Value);

            // Switch to the UI thread to update the view.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Unselects the currently selected symbol. This is required in case the current caret position is not in range of any symbols
            // (symbolToSelect will be null) so that no symbols in the tree are highlighted.
            /*if (currentlySelectedSymbol is not null)
                currentlySelectedSymbol.IsSelected = false;*/

            if (symbolToSelect is not null)
                symbolToSelect.IsSelected = true;

            var expansion = expansionOption.First();
            if (expansion is not ExpansionOption.NoChange)
                DocumentOutlineHelper.SetIsExpanded(documentSymbolUIItems, expansion);

            symbolTree.ItemsSource = documentSymbolUIItems;
        }

        /// <summary>
        /// Starts a new task to select code when a symbol node is clicked.
        /// </summary>
        private void StartJumpToContentTask(DocumentSymbolUIItem symbol)
        {
            _jumpToContentQueue.AddWork(symbol);
        }

        /// <summary>
        /// Given a DocumentSymbolItem, moves the caret to its position in the latest active text view.
        /// </summary>
        private async ValueTask JumpToContentAsync(ImmutableSegmentedList<DocumentSymbolUIItem> symbol, CancellationToken cancellationToken)
        {
            var model = await _computeDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return;

            // Switch to the UI thread to update the latest active text view.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return;

            // When the user clicks on a symbol node in the window, we want to move the cursor to that line in the editor. If we don't
            // unsubscribe from Caret_PositionChanged first, we will call StartHightlightNodeTask() once we move the cursor ourselves.
            // This is not ideal because we would be doing extra work to highlight a node that's already highlighted.
            activeTextView.Caret.PositionChanged -= Caret_PositionChanged;

            // Prevents us from being permanently unsubscribed if an exception is thrown while updating the text view selection.
            try
            {
                // Get the original position of the start of the symbol.
                var originalPosition = symbol.First().SelectionRangeSpan.Start.Position;

                // Map this position to a span in the current textview.
                var originalSpan = new SnapshotSpan(model.OriginalSnapshot, Span.FromBounds(originalPosition, originalPosition));

                var currentSpan = originalSpan.TranslateTo(activeTextView.TextSnapshot, SpanTrackingMode.EdgeExclusive);

                // Set the active text view selection to this span.
                activeTextView.SetSelection(currentSpan);
                activeTextView.ViewScroller.EnsureSpanVisible(currentSpan);
            }
            finally
            {
                // Resubscribe to Caret_PositionChanged again so that when the user clicks somewhere else, we can highlight that node.
                activeTextView.Caret.PositionChanged += Caret_PositionChanged;
            }
        }
    }
}
