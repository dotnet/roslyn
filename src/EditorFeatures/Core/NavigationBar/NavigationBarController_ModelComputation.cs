// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar;

internal partial class NavigationBarController
{
    /// <summary>
    /// Starts a new task to compute the model based on the current text.
    /// </summary>
    private async ValueTask<NavigationBarModel?> ComputeModelAndSelectItemAsync(
        ImmutableSegmentedList<NavigationBarQueueItem> queueItems, CancellationToken cancellationToken)
    {
        // If any of the requests are for frozen partial, then we do compute with frozen partial semantics.  We
        // always want these "fast but inaccurate" passes to happen first.  That pass will then enqueue the work
        // to do the slow-but-accurate pass.
        var frozenPartialSemantics = queueItems.Any(t => t.FrozenPartialSemantics);

        if (!frozenPartialSemantics)
        {
            // We're asking for the expensive nav-bar-pass, Kick off the work to do that, but attach ourselves to the
            // requested cancellation token so this expensive work can be canceled if new requests for frozen partial
            // work come in.

            // Since we're not frozen-partial, all requests must have an associated cancellation token.  And all but
            // the last *must* be already canceled (since each is canceled as new work is added).
            Contract.ThrowIfFalse(queueItems.All(t => !t.FrozenPartialSemantics));
            Contract.ThrowIfFalse(queueItems.All(t => t.NonFrozenComputationToken != null));
            Contract.ThrowIfFalse(queueItems.Take(queueItems.Count - 1).All(t => t.NonFrozenComputationToken!.Value.IsCancellationRequested));

            var lastNonFrozenComputationToken = queueItems[^1].NonFrozenComputationToken!.Value;

            // Need a dedicated try/catch here since we're operating on a different token than the queue's token.
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(lastNonFrozenComputationToken, cancellationToken);
            try
            {
                return await ComputeModelAndSelectItemAsync(frozenPartialSemantics: false, linkedTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (ExceptionUtilities.IsCurrentOperationBeingCancelled(ex, linkedTokenSource.Token))
            {
                return null;
            }
        }
        else
        {
            // Normal request to either compute nav-bar items using frozen partial semantics.
            var model = await ComputeModelAndSelectItemAsync(frozenPartialSemantics: true, cancellationToken).ConfigureAwait(false);

            // After that completes, enqueue work to compute *without* frozen partial snapshots so we move to accurate
            // results shortly. Create and pass along a new cancellation token for this expensive work so that it can be
            // canceled by future lightweight work.
            _computeModelQueue.AddWork(new NavigationBarQueueItem(FrozenPartialSemantics: false, _nonFrozenComputationCancellationSeries.CreateNext(default)));

            return model;
        }
    }

    /// <summary>
    /// Starts a new task to compute the model based on the current text.
    /// </summary>
    private async ValueTask<NavigationBarModel?> ComputeModelAndSelectItemAsync(
        bool frozenPartialSemantics, CancellationToken cancellationToken)
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
            var workspace = textSnapshot.TextBuffer.GetWorkspace();
            if (workspace is null)
                return null;

            var document = frozenPartialSemantics
                ? textSnapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken)
                : textSnapshot.AsText().GetOpenDocumentInCurrentContextWithChanges();
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
                    frozenPartialSemantics,
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

        // If we didn't get a new model (which can happen if the work to do it was canceled), use the last one we
        // computed. This ensures that we still update the project info if needed, and we don't unintentionally 
        // clear our the type/member info from the last time we computed it.
        model ??= lastPresentedInfo.model;
        if (model is null)
            return;

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
        NavigationBarModel model, int caretPosition, CancellationToken cancellationToken)
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
