// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    internal partial class NavigationBarController
    {
        /// <summary>
        /// The computation of the last model.
        /// </summary>
        private Task<NavigationBarModel> _modelTask;

        private CancellationTokenSource _modelTaskCancellationSource = new();
        private CancellationTokenSource _selectedItemInfoTaskCancellationSource = new();

        /// <summary>
        /// Starts a new task to compute the model based on the current text.
        /// </summary>
        private void StartModelUpdateAndSelectedItemUpdateTasks(int modelUpdateDelay)
        {
            AssertIsForeground();

            var textSnapshot = _subjectBuffer.CurrentSnapshot;

            // Cancel off any existing work
            _modelTaskCancellationSource.Cancel();

            _modelTaskCancellationSource = new CancellationTokenSource();
            var cancellationToken = _modelTaskCancellationSource.Token;

            // Enqueue a new computation for the model
            var asyncToken = _asyncListener.BeginAsyncOperation(GetType().Name + ".StartModelUpdateTask");
            _modelTask = ComputeModelAfterDelayAsync(_modelTask, textSnapshot, modelUpdateDelay, cancellationToken);
            _modelTask.CompletesAsyncOperation(asyncToken);

            StartSelectedItemUpdateTask(delay: 0);
        }

        private static async Task<NavigationBarModel> ComputeModelAfterDelayAsync(
            Task<NavigationBarModel> modelTask, ITextSnapshot textSnapshot, int modelUpdateDelay, CancellationToken cancellationToken)
        {
            var previousModel = await modelTask.ConfigureAwait(false);
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(modelUpdateDelay, cancellationToken).ConfigureAwait(false);
                    return await ComputeModelAsync(previousModel, textSnapshot, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e))
                {
                }
            }

            // If we canceled, then just return along whatever we have computed so far.  Note: this means the
            // _modelTask task will never enter the canceled state.  It always represents the last successfully
            // computed model.
            return previousModel;
        }

        /// <summary>
        /// Computes a model for the given snapshot.
        /// </summary>
        private static async Task<NavigationBarModel> ComputeModelAsync(
            NavigationBarModel lastCompletedModel, ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            // Ensure we switch to the threadpool before calling GetDocumentWithFrozenPartialSemantics.  It ensures
            // that any IO that performs is not potentially on the UI thread.
            await TaskScheduler.Default;

            // When computing items just get the partial semantics workspace.  This will ensure we can get data for this
            // file, and hopefully have enough loaded to get data for other files in the case of partial types.  In the
            // event the other files aren't available, then partial-type information won't be correct.  That's ok though
            // as this is just something that happens during solution load and will pass once that is over.  By using
            // partial semantics, we can ensure we don't spend an inordinate amount of time computing and using full
            // compilation data (like skeleton assemblies).
            var document = snapshot.AsText().GetDocumentWithFrozenPartialSemantics(cancellationToken);
            if (document == null)
                return null;

            // TODO: remove .FirstOrDefault()
            var languageService = GetNavBarService(document);
            if (languageService != null)
            {
                // check whether we can re-use lastCompletedModel. otherwise, update lastCompletedModel here.
                // the model should be only updated here
                if (lastCompletedModel != null)
                {
                    var semanticVersion = await document.Project.GetDependentSemanticVersionAsync(CancellationToken.None).ConfigureAwait(false);
                    if (lastCompletedModel.SemanticVersionStamp == semanticVersion && SpanStillValid(lastCompletedModel, snapshot, cancellationToken))
                    {
                        // it looks like we can re-use previous model
                        return lastCompletedModel;
                    }
                }

                using (Logger.LogBlock(FunctionId.NavigationBar_ComputeModelAsync, cancellationToken))
                {
                    var items = await languageService.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
                    if (items != null)
                    {
                        items.Do(i => i.InitializeTrackingSpans(snapshot));
                        var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                        return new NavigationBarModel(items.ToImmutableArray(), version, languageService);
                    }
                }
            }

            return new NavigationBarModel(ImmutableArray<NavigationBarItem>.Empty, new VersionStamp(), null);
        }

        /// <summary>
        /// Starts a new task to compute what item should be selected.
        /// </summary>
        private void StartSelectedItemUpdateTask(int delay)
        {
            AssertIsForeground();

            var currentView = _presenter.TryGetCurrentView();
            var subjectBufferCaretPosition = currentView?.GetCaretPoint(_subjectBuffer);
            if (!subjectBufferCaretPosition.HasValue)
                return;

            // Cancel off any existing work
            _selectedItemInfoTaskCancellationSource.Cancel();
            _selectedItemInfoTaskCancellationSource = new CancellationTokenSource();
            var cancellationToken = _selectedItemInfoTaskCancellationSource.Token;

            var asyncToken = _asyncListener.BeginAsyncOperation(GetType().Name + ".StartSelectedItemUpdateTask");
            var selectedItemInfoTask = DetermineSelectedItemInfoAsync(_modelTask, delay, subjectBufferCaretPosition.Value, cancellationToken);
            selectedItemInfoTask.CompletesAsyncOperation(asyncToken);
        }

        private async Task DetermineSelectedItemInfoAsync(
            Task<NavigationBarModel> lastModelTask,
            int delay,
            SnapshotPoint caretPosition,
            CancellationToken cancellationToken)
        {
            // First wait the delay before doing any other work.  That way if we get canceled due to other events (like
            // the user moving around), we don't end up doing anything, and the next task can take over.
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            var lastModel = await lastModelTask.ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                return;

            var currentSelectedItem = ComputeSelectedTypeAndMember(lastModel, caretPosition, cancellationToken);

            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            AssertIsForeground();

            // Update the UI to show *just* the type/member that was selected.  We don't need it to know about all items
            // as the user can only see one at a time as they're editing in a document.  However, once we've done this,
            // store the full list of items as well so that if the user expands the dropdown, we can take all those
            // values and shove them in so it appears as if the lists were always fully realized.
            _latestModelAndSelectedInfo_OnlyAccessOnUIThread = (lastModel, currentSelectedItem);
            PushSelectedItemsToPresenter(currentSelectedItem);
        }

        internal static NavigationBarSelectedTypeAndMember ComputeSelectedTypeAndMember(NavigationBarModel model, SnapshotPoint caretPosition, CancellationToken cancellationToken)
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
        private static (T item, bool gray) GetMatchingItem<T>(IEnumerable<T> items, SnapshotPoint point, INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess itemsService, CancellationToken cancellationToken) where T : NavigationBarItem
        {
            T exactItem = null;
            var exactItemStart = 0;
            T nextItem = null;
            var nextItemStart = int.MaxValue;

            foreach (var item in items)
            {
                foreach (var span in item.TrackingSpans.Select(s => s.GetSpan(point.Snapshot)))
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

        private static bool SpanStillValid(NavigationBarModel model, ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            // even if semantic version is same, portion of text could have been copied & pasted with 
            // exact same top level content.
            // go through spans to see whether this happened.
            // 
            // paying cost of moving spans forward shouldn't be matter since we need to pay that 
            // price soon or later to figure out selected item.
            foreach (var type in model.Types)
            {
                if (!SpanStillValid(type.TrackingSpans, snapshot))
                {
                    return false;
                }

                foreach (var member in type.ChildItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!SpanStillValid(member.TrackingSpans, snapshot))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool SpanStillValid(IList<ITrackingSpan> spans, ITextSnapshot snapshot)
        {
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];
                var currentSpan = span.GetSpan(snapshot);
                if (currentSpan.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
