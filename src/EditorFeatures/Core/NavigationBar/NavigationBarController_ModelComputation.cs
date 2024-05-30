// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar;

internal partial class NavigationBarController
{
    /// <summary>
    /// Starts a new task to compute the model based on the current text.
    /// </summary>
    private async ValueTask<NavigationBarModel?> ComputeModelAndSelectItemAsync(ImmutableSegmentedList<VoidResult> _, CancellationToken cancellationToken)
    {
        // Jump back to the UI thread to determine what snapshot the user is processing.
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken).NoThrowAwaitable();

        // Cancellation exceptions are ignored in AsyncBatchingWorkQueue, so return without throwing if cancellation
        // occurred while switching to the main thread.
        if (cancellationToken.IsCancellationRequested)
            return null;

        var textSnapshot = _subjectBuffer.CurrentSnapshot;
        var caretPoint = GetCaretPoint();

        // Ensure we switch to the threadpool before calling GetDocumentWithFrozenPartialSemantics.  It ensures that any
        // IO that performs is not potentially on the UI thread.
        await TaskScheduler.Default;

        var model = await ComputeModelAsync().ConfigureAwait(false);

        // Now, enqueue work to select the right item in this new model. Note: we don't want to cancel existing items in
        // the queue as it may be the case that the user moved between us capturing the initial caret point and now, and
        // we'd want the selection work we enqueued for that to take precedence over us.
        if (model != null && caretPoint != null)
            _selectItemQueue.AddWork(caretPoint.Value, cancelExistingWork: false);

        return model;

        async Task<NavigationBarModel?> ComputeModelAsync()
        {
            // When computing items just get the partial semantics workspace.  This will ensure we can get data for this
            // file, and hopefully have enough loaded to get data for other files in the case of partial types.  In the
            // event the other files aren't available, then partial-type information won't be correct.  That's ok though
            // as this is just something that happens during solution load and will pass once that is over.  By using
            // partial semantics, we can ensure we don't spend an inordinate amount of time computing and using full
            // compilation data (like skeleton assemblies).
            var forceFrozenPartialSemanticsForCrossProcessOperations = true;

            var workspace = textSnapshot.TextBuffer.GetWorkspace();
            if (workspace is null)
                return null;

            var document = textSnapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
            if (document == null)
                return null;

            var itemService = document.GetLanguageService<INavigationBarItemService>();
            if (itemService == null)
                return null;

            // If these are navbars for a file that isn't even visible, then avoid doing any unnecessary computation
            // work until far in the future (or if visibility changes).  This ensures our non-visible docs do settle
            // once enough time has passed, while greatly reducing their impact on the system.
            //
            // Use NoThrow as this is a high source of cancellation exceptions.  This avoids the exception and instead
            // bails gracefully by checking below.
            await _visibilityTracker.DelayWhileNonVisibleAsync(
                _threadingContext, _asyncListener, _subjectBuffer, DelayTimeSpan.NonFocus, cancellationToken).NoThrowAwaitable(false);

            if (cancellationToken.IsCancellationRequested)
                return null;

            using (Logger.LogBlock(FunctionId.NavigationBar_ComputeModelAsync, cancellationToken))
            {
                var items = await itemService.GetItemsAsync(
                    document,
                    workspace.CanApplyChange(ApplyChangesKind.ChangeDocument),
                    forceFrozenPartialSemanticsForCrossProcessOperations,
                    textSnapshot.Version,
                    cancellationToken).ConfigureAwait(false);
                return new NavigationBarModel(itemService, items);
            }
        }
    }

    private async ValueTask SelectItemAsync(ImmutableSegmentedList<int> positions, CancellationToken cancellationToken)
    {
        var lastCaretPosition = positions.Last();

        // Can grab this directly here as only this queue ever reads or writes to it.
        var lastPresentedInfo = _lastPresentedInfo;

        // Make a task that waits indefinitely, or until the cancellation token is signaled.
        var cancellationTriggeredTask = Task.Delay(-1, cancellationToken);

        // Get the task representing the computation of the model.
        var modelTask = _computeModelQueue.WaitUntilCurrentBatchCompletesAsync();

        var completedTask = await Task.WhenAny(cancellationTriggeredTask, modelTask).ConfigureAwait(false);
        if (completedTask == cancellationTriggeredTask)
            return;

        var model = await modelTask.ConfigureAwait(false);
        var currentSelectedItem = ComputeSelectedTypeAndMember(model, lastCaretPosition, cancellationToken);

        var (projectItems, selectedProjectItem) = GetProjectItems();
        if (Equals(model, lastPresentedInfo.model) &&
            Equals(currentSelectedItem, lastPresentedInfo.selectedInfo) &&
            Equals(selectedProjectItem, lastPresentedInfo.selectedProjectItem) &&
            projectItems.SequenceEqual(lastPresentedInfo.projectItems))
        {
            // Nothing changed, so we can skip presenting these items.
            return;
        }

        // Finally, switch back to the UI to update our state and UI.
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        _presenter.PresentItems(
            projectItems,
            selectedProjectItem,
            model?.Types ?? [],
            currentSelectedItem.TypeItem,
            currentSelectedItem.MemberItem);

        _lastPresentedInfo = (projectItems, selectedProjectItem, model, currentSelectedItem);
    }

    internal static NavigationBarSelectedTypeAndMember ComputeSelectedTypeAndMember(
        NavigationBarModel? model, int caretPosition, CancellationToken cancellationToken)
    {
        if (model != null)
        {
            var (item, gray) = GetMatchingItem(model.Types, caretPosition, model.ItemService, cancellationToken);
            if (item != null)
            {
                var rightItem = GetMatchingItem(item.ChildItems, caretPosition, model.ItemService, cancellationToken);
                return new NavigationBarSelectedTypeAndMember(item, gray, rightItem.item, rightItem.gray);
            }
        }

        return NavigationBarSelectedTypeAndMember.Empty;
    }

    /// <summary>
    /// Finds the item that point is in, or if it's not in any items, gets the first item that's
    /// positioned after the cursor.
    /// </summary>
    /// <returns>A tuple of the matching item, and if it should be shown grayed.</returns>
    private static (NavigationBarItem? item, bool gray) GetMatchingItem(
        ImmutableArray<NavigationBarItem> items, int point, INavigationBarItemService itemsService, CancellationToken cancellationToken)
    {
        NavigationBarItem? exactItem = null;
        var exactItemStart = 0;
        NavigationBarItem? nextItem = null;
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
