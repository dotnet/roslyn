// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        private readonly record struct FilterAndSortOptions(string? SearchQuery, SortOption SortOption, SnapshotPoint? CaretPoint);

        /// <summary>
        /// Queue to batch up work to do to filter and sort the data model. returns null if the model returned from <see cref="_documentSymbolQueue"/> is null.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<FilterAndSortOptions, DocumentSymbolDataModel?> _filterAndSortQueue;

        public void EnqueueFilterAndSortTask(SnapshotPoint? caretPoint)
        {
            _filterAndSortQueue.AddWork(new FilterAndSortOptions(SearchText, SortOption, caretPoint), cancelExistingWork: true);
        }

        private async ValueTask<DocumentSymbolDataModel?> FilterAndSortDataModelAsync(ImmutableSegmentedList<FilterAndSortOptions> filterAndSortOptions, CancellationToken cancellationToken)
        {
            var (searchQuery, sortOption, caretPoint) = filterAndSortOptions.Last();

            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);

            if (model is null)
            {
                return null;
            }

            var updatedDocumentSymbolData = model.DocumentSymbolData;
            if (!RoslynString.IsNullOrWhiteSpace(searchQuery))
            {
                updatedDocumentSymbolData = DocumentOutlineHelper.SearchDocumentSymbolData(updatedDocumentSymbolData, searchQuery, cancellationToken);
            }

            updatedDocumentSymbolData = DocumentOutlineHelper.SortDocumentSymbolData(updatedDocumentSymbolData, sortOption, cancellationToken);

            EnqueueModelUpdateTask(ExpansionOption.NoChange, caretPoint);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }
    }
}
