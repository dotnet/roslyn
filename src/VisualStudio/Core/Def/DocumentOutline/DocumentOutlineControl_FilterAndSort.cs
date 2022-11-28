// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System.Linq;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private readonly AsyncBatchingWorkQueue<FilterAndSortOptions, DocumentSymbolDataModel?> _filterAndSortQueue;

        private void EnqueueFilterAndSortDataModelTask(string? search, SortOption sortOptions, SnapshotPoint? caretPoint)
        {
            _filterAndSortQueue.AddWork(new FilterAndSortOptions(search, sortOptions, caretPoint), cancelExistingWork: true);
        }

        private async ValueTask<DocumentSymbolDataModel?> FilterAndSortDataModelAsync(ImmutableSegmentedList<FilterAndSortOptions> settings, CancellationToken cancellationToken)
        {
            var (searchQuery, sortOption, caretPoint) = settings.LastOrDefault();

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

            EnqueueUpdateUITask(ExpansionOption.NoChange, caretPoint);

            return new DocumentSymbolDataModel(updatedDocumentSymbolData, model.OriginalSnapshot);
        }

        private record struct FilterAndSortOptions(string? SearchQuery, SortOption SortOptions, SnapshotPoint? CaretPoint);
    }
}
