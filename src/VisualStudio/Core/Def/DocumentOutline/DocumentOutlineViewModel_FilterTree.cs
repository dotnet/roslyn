// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using System.Linq;
using Roslyn.Utilities;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        private readonly AsyncBatchingWorkQueue<string> _filterQueue;

        internal void EnqueueFilter(string newText)
        {
            _filterQueue.AddWork(newText, cancelExistingWork: true);
        }

        private async ValueTask FilterTreeAsync(ImmutableSegmentedList<string> searchStrings, CancellationToken cancellationToken)
        {
            var searchText = searchStrings.Last();
            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
            {
                return;
            }

            if (searchText == string.Empty)
            {
                var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                using (await _guard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    DocumentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>(documentSymbolViewModelItems);
                }
            }
            else
            {
                var documentSymbolData = DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, searchText, cancellationToken);
                var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(documentSymbolData);

                using (await _guard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    DocumentOutlineHelper.UnselectAll(DocumentSymbolViewModelItems);
                    DocumentSymbolViewModelItems = new ObservableCollection<DocumentSymbolItemViewModel>(documentSymbolViewModelItems);
                }
            }
        }
    }
}
