// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            _threadingContext.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(_codeWindow.GetLastActiveView(out var textView)))
                return null;

            return _editorAdaptersFactoryService.GetWpfTextView(textView);
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
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var activeTextView = GetLastActiveIWpfTextView();
            if (activeTextView is null)
                return null;

            var currentSnapshot = activeTextView.TextSnapshot;
            var textBuffer = activeTextView.TextBuffer;

            var filePath = GetFilePath();
            if (filePath is null)
                return null;

            // Ensure we switch to the threadpool before calling ComputeDataModelAsync. It ensures
            // that fetching and processing the document symbol data model is not done on the UI thread.
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
            // 'true' value is unused.  this just signals to the queue that we have work to do.
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
                updatedDocumentSymbolData = DocumentOutlineHelper.Search(updatedDocumentSymbolData, searchQuery, cancellationToken);

            updatedDocumentSymbolData = DocumentOutlineHelper.Sort(updatedDocumentSymbolData, sortOption, cancellationToken);

            StartDetermineHighlightedItemAndPresentItemsTask(ExpansionOption.NoChange);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }

        /// <summary>
        /// Starts a new task to highlight the symbol node corresponding to the current caret position in the editor, expand/collapse
        /// nodes (if applicable), and updates the UI.
        /// </summary>
        private void StartDetermineHighlightedItemAndPresentItemsTask(ExpansionOption expansionOption)
        {
            _determineHighlightAndPresentItemsQueue.AddWork(expansionOption);
        }

        /// <summary>
        /// Highlights the symbol node corresponding to the current caret position in the editor, expands/collapses nodes, then updates the UI.
        /// </summary>
        private async ValueTask DetermineHighlightedItemAndPresentItemsAsync(ImmutableSegmentedList<ExpansionOption> expansionOption, CancellationToken cancellationToken)
        {
            var model = await _filterAndSortDataModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
                return;

            // Switch to the UI thread to get the current caret point and latest active text view.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Unselects the currently selected symbol. This is required in case the current caret position is not in range of any symbols
            // (symbolToSelect will be null) so that no symbols in the tree are highlighted.
            /*if (currentlySelectedSymbol is not null)
                currentlySelectedSymbol.IsSelected = false;*/

            if (symbolToSelect is not null)
                symbolToSelect.IsSelected = true;

            var expansion = expansionOption.First();
            if (expansion is not ExpansionOption.NoChange)
                DocumentOutlineHelper.SetIsExpanded(documentSymbolUIItems, expansion);

            SymbolTree.ItemsSource = documentSymbolUIItems;
        }
    }
}
