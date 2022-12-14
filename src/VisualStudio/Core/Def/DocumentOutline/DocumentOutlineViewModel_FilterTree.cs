// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue to filter items based on a users search terms.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<string> _filterQueue;

        internal void EnqueueFilter(string newText)
            => _filterQueue.AddWork(newText, cancelExistingWork: true);

        private async ValueTask FilterTreeAsync(ImmutableSegmentedList<string> searchStrings, CancellationToken cancellationToken)
        {
            var searchText = searchStrings.Last();
            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
            {
                // we haven't gotten an LSP response yet
                return;
            }

            // search has been cleared, re-create all items from the model
            if (searchText == string.Empty)
            {
                var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                using (await _guard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Unselect any currently selected items or WPF will believe it needs to select the root node.
                    DocumentOutlineHelper.UnselectAll(DocumentSymbolViewModelItems);
                    DocumentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>(documentSymbolViewModelItems);
                }
            }
            else
            {
                var documentSymbolData = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, searchText, cancellationToken);
                var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(documentSymbolData);
                using (await _guard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Unselect any currently selected items or WPF will believe it needs to select the root node.
                    DocumentOutlineHelper.UnselectAll(DocumentSymbolViewModelItems);
                    DocumentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>(documentSymbolViewModelItems);
                }
            }
        }
    }
}
