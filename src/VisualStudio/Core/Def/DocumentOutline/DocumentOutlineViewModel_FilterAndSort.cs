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
        private readonly AsyncBatchingWorkQueue<FilterAndSortOptions, DocumentSymbolDataModel?> _filterAndSortQueue;

        public void EnqueueFilterAndSortTask(SnapshotPoint? caretPoint)
        {
            _filterAndSortQueue.AddWork(new FilterAndSortOptions(SearchText, SortOption, caretPoint), cancelExistingWork: true);
        }

        private async ValueTask<DocumentSymbolDataModel?> FilterAndSortDataModelAsync(ImmutableSegmentedList<FilterAndSortOptions> settings, CancellationToken cancellationToken)
        {
            var (searchQuery, sortOption, caretPoint) = settings.Last();

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

            EnqueueUIUpdateTask(ExpansionOption.NoChange, caretPoint);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }

        private record struct FilterAndSortOptions(string? SearchQuery, SortOption SortOptions, SnapshotPoint? CaretPoint);
    }
}
