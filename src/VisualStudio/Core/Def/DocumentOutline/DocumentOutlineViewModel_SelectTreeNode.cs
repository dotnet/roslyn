// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        private readonly AsyncBatchingWorkQueue<CaretPosition> _selectTreeNodeQueue;
        private CaretPosition? _currentCaretPosition;

        public void EnqueueSelectTreeNode(CaretPosition caretPoint)
        {
            _selectTreeNodeQueue.AddWork(caretPoint, cancelExistingWork: true);
        }

        private async ValueTask SelectTreeNodeAsync(ImmutableSegmentedList<CaretPosition> snapshotPoints, CancellationToken token)
        {
            if (_currentSnapshot is null)
            {
                return;
            }

            var caretPosition = snapshotPoints.Last();
            var caretPoint = await _visualStudioCodeWindowInfoService.GetSnapshotPointFromCaretPositionAsync(caretPosition, token).ConfigureAwait(false);
            if (!caretPoint.HasValue)
            {
                return;
            }

            using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
            {
                var documentSymbolUIItems = _documentSymbolUIItems;
                var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(documentSymbolUIItems, _currentSnapshot, caretPoint.Value);

                if (symbolToSelect is null)
                {
                    return;
                }

                DocumentOutlineHelper.ExpandAncestors(documentSymbolUIItems, symbolToSelect.RangeSpan);
                symbolToSelect.IsSelected = true;
                _currentCaretPosition = caretPosition;
            }
        }
    }
}
