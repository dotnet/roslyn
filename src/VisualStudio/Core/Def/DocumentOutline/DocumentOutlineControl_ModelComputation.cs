// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private IWpfTextView? GetLastActiveIWpfTextView()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // If we return null, the calling queue returns and we stop processing.
            if (ErrorHandler.Failed(_codeWindow.GetLastActiveView(out var textView)))
                return null;

            return _editorAdaptersFactoryService.GetWpfTextView(textView);
        }

        /// <summary>
        /// Starts a new task to compute the data model.
        /// </summary>
        private void StartComputeDataModelTask()
        {
            // 'true' value is unused. This just signals to the queue that we have work to do.
            _computeDataModelQueue.AddWork(true);
        }

        /// <summary>
        /// Makes the LSP document symbol request and creates the data model.
        /// </summary>
        private async ValueTask<DocumentSymbolDataModel?> ComputeDataModelAsync(ImmutableSegmentedList<bool> _, CancellationToken cancellationToken)
        {
            // Jump to the UI thread to get the currently active text view. This cancellation token controls the entire DocumentOutlineControl
            // so if we are closed/cancelled on the UI thread, when this jumps back to the UI thread, it will auto-cancel and won't continue
            // further. We only get to the code below if the control is still in an active state.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return null;

            var currentSnapshot = activeTextView.TextSnapshot;
            var textBuffer = activeTextView.TextBuffer;

            var filePath = GetFilePath();
            if (filePath is null)
                return null;

            // Ensure we switch to the threadpool before calling ComputeModelAsync. It ensures that fetching and processing the document
            // symbol data model is not done on the UI thread.
            await TaskScheduler.Default;

            var model = await ComputeModelAsync().ConfigureAwait(false);

            // The model can be null if the LSP document symbol request returns a null response.
            if (model is not null)
                StartFilterAndSortDataModelTask();

            return model;

            async Task<DocumentSymbolDataModel?> ComputeModelAsync()
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await DocumentOutlineHelper.DocumentSymbolsRequestAsync(
                    textBuffer, _languageServiceBroker, filePath, cancellationToken).ConfigureAwait(false);

                // If there is no matching LSP server registered the client will return null here - e.g. wrong content type on the buffer, the
                // server totally failed to start, server doesn't support the right capabilities. For C# we might know it's a bug if we get a null
                // response here, but we don't know that in general for all languages.
                if (response is null)
                    return null;

                var responseBody = response.ToObject<DocumentSymbol[]>();
                if (responseBody is null)
                    return null;

                return DocumentOutlineHelper.GetDocumentSymbolDataModel(responseBody, currentSnapshot);
            }

            string? GetFilePath()
            {
                _threadingContext.ThrowIfNotOnUIThread();
                if (_editorAdaptersFactoryService.GetBufferAdapter(textBuffer) is IPersistFileFormat persistFileFormat &&
                    ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out var _)))
                {
                    return filePath;
                }

                return null;
            }
        }

        /// <summary>
        /// Starts a new task to filter and sort the data model.
        /// </summary>
        private void StartFilterAndSortDataModelTask()
        {
            // 'true' value is unused. This just signals to the queue that we have work to do.
            _filterAndSortDataModelQueue.AddWork(true);
        }

        /// <summary>
        /// Filters and sorts the data model.
        /// </summary>
        private async ValueTask<DocumentSymbolDataModel?> FilterAndSortDataModelAsync(ImmutableSegmentedList<bool> _, CancellationToken cancellationToken)
        {
            var model = await _computeDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return null;

            // Switch to the UI thread to get the current search query and sort option.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var searchQuery = SearchBox.Text;
            var sortOption = SortOption;

            // Switch to the threadpool to filter and sort the data model.
            await TaskScheduler.Default;

            var updatedDocumentSymbolData = model.DocumentSymbolData;

            if (!string.IsNullOrWhiteSpace(searchQuery))
                updatedDocumentSymbolData = DocumentOutlineHelper.SearchDocumentSymbolData(updatedDocumentSymbolData, searchQuery, cancellationToken);

            updatedDocumentSymbolData = DocumentOutlineHelper.SortDocumentSymbolData(updatedDocumentSymbolData, sortOption, cancellationToken);

            StartHighlightExpandAndPresentItemsTask(ExpansionOption.NoChange);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }

        /// <summary>
        /// Starts a new task to highlight the symbol node corresponding to the current caret position in the editor, expand/collapse
        /// nodes, and update the UI.
        /// </summary>
        private void StartHighlightExpandAndPresentItemsTask(ExpansionOption expansionOption)
        {
            _highlightExpandAndPresentItemsQueue.AddWork(expansionOption);
        }

        /// <summary>
        /// Highlights the symbol node corresponding to the current caret position in the editor, expands/collapses nodes, then updates the UI.
        /// </summary>
        private async ValueTask HighlightExpandAndPresentItemsAsync(ImmutableSegmentedList<ExpansionOption> expansionOption, CancellationToken cancellationToken)
        {
            var model = await _filterAndSortDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return;

            // Switch to the UI thread to get the current caret point and latest active text view then create the UI model.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return;

            var caretPoint = activeTextView.GetCaretPoint(activeTextView.TextBuffer);
            if (!caretPoint.HasValue)
                return;

            var documentSymbolUIItems = DocumentOutlineHelper.GetDocumentSymbolUIItems(model.DocumentSymbolData, _threadingContext);

            // Switch to the threadpool to determine which node to select (if applicable).
            await TaskScheduler.Default;

            var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(documentSymbolUIItems, model.OriginalSnapshot, caretPoint.Value);

            // Switch to the UI thread to update the view.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Expand/collapse nodes based on the given Expansion Option.
            var expansion = expansionOption.First();
            if (expansion is not ExpansionOption.NoChange)
                DocumentOutlineHelper.SetIsExpanded(documentSymbolUIItems, (IEnumerable<DocumentSymbolUIItem>)SymbolTree.ItemsSource, expansion);

            // Hightlight the selected node if it exists, otherwise unselect all nodes (required so that the view does not select a node by default).
            if (symbolToSelect is not null)
            {
                // Expand all ancestors first to ensure the selected node will be visible.
                DocumentOutlineHelper.ExpandAncestors(documentSymbolUIItems, symbolToSelect.RangeSpan);
                symbolToSelect.IsSelected = true;
            }
            else
            {
                // On Document Outline Control initialization, SymbolTree.ItemsSource is null
                if (SymbolTree.ItemsSource is not null)
                    DocumentOutlineHelper.UnselectAll((IEnumerable<DocumentSymbolUIItem>)SymbolTree.ItemsSource);
            }

            SymbolTree.ItemsSource = documentSymbolUIItems;
        }
    }
}
