// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue of caret positions that we want to select to corresponding node for.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<CaretPosition> _selectTreeNodeQueue;

        /// <summary>
        /// Last used caret position, read by our model update task so we can keep nodes selected across snap-shot updates.
        /// </summary>
        private CaretPosition? _currentlySelectedSymbolCaretPosition;

        public void EnqueueSelectTreeNode(CaretPosition caretPoint)
            => _selectTreeNodeQueue.AddWork(caretPoint, cancelExistingWork: true);

        private async ValueTask SelectTreeNodeAsync(ImmutableSegmentedList<CaretPosition> caretPositions, CancellationToken token)
        {
            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            if (model is null)
            {
                // we haven't gotten an LSP response yet
                return;
            }

            var caretPosition = caretPositions.Last();
            var currentTextSnapshot = model.OriginalSnapshot;
            var caretPoint = await _visualStudioCodeWindowInfoService.GetSnapshotPointFromCaretPositionAsync(caretPosition, token).ConfigureAwait(false);
            if (!caretPoint.HasValue)
            {
                // document has changed since we were queue'd to the point that we can't find this position.
                return;
            }

            // guard as we update the state of the view models
            using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
            {
                var documentSymbolViewModelItems = _documentSymbolViewModelItems;
                var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(documentSymbolViewModelItems, currentTextSnapshot, caretPoint.Value);

                if (symbolToSelect is null)
                {
                    return;
                }

                DocumentOutlineHelper.ExpandAncestors(documentSymbolViewModelItems, symbolToSelect.RangeSpan);
                symbolToSelect.IsSelected = true;
                _currentlySelectedSymbolCaretPosition = caretPosition;
            }
        }
    }
}
