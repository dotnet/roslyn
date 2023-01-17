// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.Progression;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Queue for updating the state of the view model
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ViewModelStateData> _updateViewModelStateQueue;

        public void EnqueueFilter(string newText)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateData(newText, null, null, false));

        public void EnqueueSelectTreeNode(CaretPosition caretPoint)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateData(null, caretPoint, null, false));

        public void EnqueueExpandOrCollapse(ExpansionOption option)
            => _updateViewModelStateQueue.AddWork(new ViewModelStateData(null, null, option, false));

        /// <summary>
        /// state that is only updated in <see cref="UpdateViewModelStateAsync"/>
        /// </summary>
        private string? _currentSearchText = null;
        private CaretPosition? _currentCaretPosition = null;
        private ExpansionOption? _currentExpansionOption = null;

        private async ValueTask UpdateViewModelStateAsync(ImmutableSegmentedList<ViewModelStateData> viewModelStateData, CancellationToken cancellationToken)
        {
            var model = await _documentSymbolQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
            Assumes.NotNull(model); // This can only be null if no work was ever enqueued, we expect at least one item to always have been queued in the constructor

            var searchText = viewModelStateData.SelectLastOrDefault(static x => x.SearchText);
            var position = viewModelStateData.SelectLastOrDefault(static x => x.CaretPositionOfNodeToSelect);
            var expansion = viewModelStateData.SelectLastOrDefault(static x => x.ExpansionOption);
            var dataUpdated = viewModelStateData.Any(static x => x.DataUpdated);

            if (searchText is not null)
            {
                _currentSearchText = searchText;
            }

            if (position is not null)
            {
                _currentCaretPosition = position;
            }

            // These updates always require a valid model to perform
            if (!model.IsEmpty)
            {
                if (_currentSearchText is null && dataUpdated)
                {
                    var documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                    ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                    DocumentSymbolViewModelItems = documentSymbolViewModelItems;
                }

                if (_currentSearchText is { } currentQuery)
                {
                    ImmutableArray<DocumentSymbolDataViewModel> documentSymbolViewModelItems;
                    if (currentQuery == string.Empty)
                    {
                        // search was cleared, show all data.
                        documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(model.DocumentSymbolData);
                        ApplyExpansionStateToNewItems(documentSymbolViewModelItems, DocumentSymbolViewModelItems);
                    }
                    else
                    {
                        // We are going to show results so we unset any expand / collapse state
                        _currentExpansionOption = null;
                        documentSymbolViewModelItems = DocumentOutlineHelper.GetDocumentSymbolItemViewModels(
                             DocumentOutlineHelper.SearchDocumentSymbolData(model.DocumentSymbolData, currentQuery, cancellationToken));
                    }

                    DocumentSymbolViewModelItems = documentSymbolViewModelItems;
                }

                if (_currentCaretPosition is { } currentPosition)
                {
                    var caretPoint = currentPosition.Point.GetPoint(_textBuffer, PositionAffinity.Predecessor);
                    if (!caretPoint.HasValue)
                    {
                        // document has changed since we were queued to the point that we can't find this position.
                        return;
                    }

                    var symbolToSelect = DocumentOutlineHelper.GetDocumentNodeToSelect(DocumentSymbolViewModelItems, model.OriginalSnapshot, caretPoint.Value);
                    if (symbolToSelect is null)
                    {
                        return;
                    }

                    DocumentOutlineHelper.ExpandAncestors(DocumentSymbolViewModelItems, symbolToSelect.SelectionRangeSpan);
                    symbolToSelect.IsSelected = true;
                    _currentExpansionOption = null;
                }
            }

            // If we aren't filtering to search results do expand/collapse
            if (expansion is { } expansionOption && string.IsNullOrEmpty(searchText))
            {
                if (expansionOption == ExpansionOption.Collapse && DocumentSymbolViewModelItems.Any())
                {
                    _currentExpansionOption = expansionOption;
                }

                DocumentOutlineHelper.SetExpansionOption(DocumentSymbolViewModelItems, expansionOption);
            }
            else if (_currentExpansionOption is ExpansionOption.Collapse)
            {
                DocumentOutlineHelper.SetExpansionOption(DocumentSymbolViewModelItems, ExpansionOption.Collapse);
            }

            static void ApplyExpansionStateToNewItems(ImmutableArray<DocumentSymbolDataViewModel> oldItems, ImmutableArray<DocumentSymbolDataViewModel> newItems)
            {
                if (DocumentOutlineHelper.AreAllTopLevelItemsCollapsed(oldItems))
                {
                    // new nodes are un-collapsed by default
                    // we want to collapse all new top-level nodes if 
                    // everything else currently is so things aren't "jumpy"
                    foreach (var item in newItems)
                    {
                        item.IsExpanded = false;
                    }
                }
                else
                {
                    DocumentOutlineHelper.SetIsExpandedOnNewItems(newItems, oldItems);
                }
            }
        }

        private record ViewModelStateDataChange(string? SearchText, CaretPosition? CaretPositionOfNodeToSelect, ExpansionOption? ExpansionOption, bool DataUpdated);
    }
}
