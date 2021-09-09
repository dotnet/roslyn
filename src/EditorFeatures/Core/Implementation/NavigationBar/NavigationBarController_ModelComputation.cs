﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    internal partial class NavigationBarController
    {
        /// <summary>
        /// Starts a new task to compute the model based on the current text.
        /// </summary>
        private async ValueTask<NavigationBarModel> ComputeModelAndSelectItemAsync(ImmutableArray<bool> unused, CancellationToken cancellationToken)
        {
            // Jump back to the UI thread to determine what snapshot the user is processing.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var textSnapshot = _subjectBuffer.CurrentSnapshot;

            // Ensure we switch to the threadpool before calling GetDocumentWithFrozenPartialSemantics.  It ensures
            // that any IO that performs is not potentially on the UI thread.
            await TaskScheduler.Default;

            var model = await ComputeModelAsync(textSnapshot, cancellationToken).ConfigureAwait(false);

            // Now, enqueue work to select the right item in this new model.
            StartSelectedItemUpdateTask();

            return model;

            static async Task<NavigationBarModel> ComputeModelAsync(ITextSnapshot textSnapshot, CancellationToken cancellationToken)
            {
                // When computing items just get the partial semantics workspace.  This will ensure we can get data for this
                // file, and hopefully have enough loaded to get data for other files in the case of partial types.  In the
                // event the other files aren't available, then partial-type information won't be correct.  That's ok though
                // as this is just something that happens during solution load and will pass once that is over.  By using
                // partial semantics, we can ensure we don't spend an inordinate amount of time computing and using full
                // compilation data (like skeleton assemblies).
                var document = textSnapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
                if (document == null)
                    return null;

                var itemService = document.GetLanguageService<INavigationBarItemService>();
                if (itemService != null)
                {
                    using (Logger.LogBlock(FunctionId.NavigationBar_ComputeModelAsync, cancellationToken))
                    {
                        var items = await itemService.GetItemsAsync(document, textSnapshot.Version, cancellationToken).ConfigureAwait(false);
                        return new NavigationBarModel(itemService, items);
                    }
                }

                return new NavigationBarModel(itemService: null, ImmutableArray<NavigationBarItem>.Empty);
            }
        }

        /// <summary>
        /// Starts a new task to compute what item should be selected.
        /// </summary>
        private void StartSelectedItemUpdateTask()
        {
            // 'true' value is unused.  this just signals to the queue that we have work to do.
            _selectItemQueue.AddWork();
        }

        private async ValueTask SelectItemAsync(CancellationToken cancellationToken)
        {
            // Switch to the UI so we can determine where the user is and determine the state the last time we updated
            // the UI.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var currentView = _presenter.TryGetCurrentView();
            var caretPosition = currentView?.GetCaretPoint(_subjectBuffer);
            if (!caretPosition.HasValue)
                return;

            var position = caretPosition.Value.Position;
            var lastPresentedInfo = _lastPresentedInfo;

            // Jump back to the BG to do any expensive work walking the entire model
            await TaskScheduler.Default;

            // Ensure the latest model is computed.
            var model = await _computeModelQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(true);

            var currentSelectedItem = ComputeSelectedTypeAndMember(model, position, cancellationToken);

            GetProjectItems(out var projectItems, out var selectedProjectItem);
            if (Equals(model, lastPresentedInfo.model) &&
                Equals(currentSelectedItem, lastPresentedInfo.selectedInfo) &&
                Equals(selectedProjectItem, lastPresentedInfo.selectedProjectItem) &&
                projectItems.SequenceEqual(lastPresentedInfo.projectItems))
            {
                // Nothing changed, so we can skip presenting these items.
                return;
            }

            // Finally, switch back to the UI to update our state and UI.
            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _presenter.PresentItems(
                projectItems,
                selectedProjectItem,
                model.Types,
                currentSelectedItem.TypeItem,
                currentSelectedItem.MemberItem);

            _lastPresentedInfo = (projectItems, selectedProjectItem, model, currentSelectedItem);
        }

        internal static NavigationBarSelectedTypeAndMember ComputeSelectedTypeAndMember(
            NavigationBarModel model, int caretPosition, CancellationToken cancellationToken)
        {
            var (item, gray) = GetMatchingItem(model.Types, caretPosition, model.ItemService, cancellationToken);
            if (item == null)
            {
                // Nothing to show at all
                return new NavigationBarSelectedTypeAndMember(null, null);
            }

            var rightItem = GetMatchingItem(item.ChildItems, caretPosition, model.ItemService, cancellationToken);
            return new NavigationBarSelectedTypeAndMember(item, gray, rightItem.item, rightItem.gray);
        }

        /// <summary>
        /// Finds the item that point is in, or if it's not in any items, gets the first item that's
        /// positioned after the cursor.
        /// </summary>
        /// <returns>A tuple of the matching item, and if it should be shown grayed.</returns>
        private static (NavigationBarItem item, bool gray) GetMatchingItem(
            ImmutableArray<NavigationBarItem> items, int point, INavigationBarItemService itemsService, CancellationToken cancellationToken)
        {
            NavigationBarItem exactItem = null;
            var exactItemStart = 0;
            NavigationBarItem nextItem = null;
            var nextItemStart = int.MaxValue;

            foreach (var item in items)
            {
                foreach (var span in item.Spans)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (span.Contains(point) || span.End == point)
                    {
                        // This is the item we should show normally. We'll continue looking at other
                        // items as there might be a nested type that we're actually in. If there
                        // are multiple items containing the point, choose whichever containing span
                        // starts later because that will be the most nested item.

                        if (exactItem == null || span.Start >= exactItemStart)
                        {
                            exactItem = item;
                            exactItemStart = span.Start;
                        }
                    }
                    else if (span.Start > point && span.Start <= nextItemStart)
                    {
                        nextItem = item;
                        nextItemStart = span.Start;
                    }
                }
            }

            if (exactItem != null)
            {
                return (exactItem, gray: false);
            }
            else
            {
                // The second parameter is if we should show it grayed. We'll be nice and say false
                // unless we actually have an item
                var itemToGray = nextItem ?? items.LastOrDefault();
                if (itemToGray != null && !itemsService.ShowItemGrayedIfNear(itemToGray))
                {
                    itemToGray = null;
                }

                return (itemToGray, gray: itemToGray != null);
            }
        }
    }
}
