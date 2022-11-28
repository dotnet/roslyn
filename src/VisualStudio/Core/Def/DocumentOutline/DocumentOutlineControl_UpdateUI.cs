// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Threading;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Text;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineControl
    {
        private readonly AsyncBatchingWorkQueue<UIData> _updateUIQueue;

        private void EnqueueUpdateUITask(ExpansionOption option, SnapshotPoint? caretPoint)
        {
            _updateUIQueue.AddWork(new UIData(option, caretPoint), cancelExistingWork: true);
        }

        private async ValueTask UpdateUIAsync(ImmutableSegmentedList<UIData> options, CancellationToken cancellationToken)
        {
            var (expansion, caretPoint) = options.Last();
            var model = await _filterAndSortQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
            {
                return;
            }

            // Switch to the UI thread to get the current caret point and latest active text view then create the UI model.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var documentSymbolUIItems = DocumentOutlineHelper.GetDocumentSymbolUIItems(model.DocumentSymbolData, _threadingContext);

            DocumentSymbolUIItem? symbolToSelect = null;
            if (caretPoint.HasValue)
            {
                symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(documentSymbolUIItems, model.OriginalSnapshot, caretPoint.Value);
            }

            // Expand/collapse nodes based on the given Expansion Option.
            if (expansion is not ExpansionOption.NoChange && SymbolTree.ItemsSource is not null)
            {
                DocumentOutlineHelper.SetIsExpanded(documentSymbolUIItems, (IEnumerable<DocumentSymbolUIItem>)SymbolTree.ItemsSource, expansion);
            }

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

        private record struct UIData(ExpansionOption ExpansionOption, SnapshotPoint? CaretPoint);
    }
}
