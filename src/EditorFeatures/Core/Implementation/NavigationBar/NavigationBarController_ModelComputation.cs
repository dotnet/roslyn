// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
{
    internal partial class NavigationBarController
    {
        /// <summary>
        /// The computation of the last model.
        /// </summary>
        private Task<NavigationBarModel> _modelTask;
        private NavigationBarModel _lastCompletedModel;
        private CancellationTokenSource _modelTaskCancellationSource = new CancellationTokenSource();

        /// <summary>
        /// Starts a new task to compute the model based on the current text.
        /// </summary>
        private void StartModelUpdateAndSelectedItemUpdateTasks(int modelUpdateDelay, int selectedItemUpdateDelay, bool updateUIWhenDone)
        {
            AssertIsForeground();

            var textSnapshot = _subjectBuffer.CurrentSnapshot;
            var document = textSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            // Cancel off any existing work
            _modelTaskCancellationSource.Cancel();

            _modelTaskCancellationSource = new CancellationTokenSource();
            var cancellationToken = _modelTaskCancellationSource.Token;

            // Enqueue a new computation for the model
            var asyncToken = _asyncListener.BeginAsyncOperation(GetType().Name + ".StartModelUpdateTask");
            _modelTask =
                Task.Delay(modelUpdateDelay, cancellationToken)
                    .SafeContinueWithFromAsync(
                        _ => ComputeModelAsync(document, textSnapshot, cancellationToken),
                        cancellationToken,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Default);
            _modelTask.CompletesAsyncOperation(asyncToken);

            StartSelectedItemUpdateTask(selectedItemUpdateDelay, updateUIWhenDone);
        }

        /// <summary>
        /// Computes a model for the given snapshot.
        /// </summary>
        private async Task<NavigationBarModel> ComputeModelAsync(Document document, ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            // TODO: remove .FirstOrDefault()
            var languageService = document.Project.LanguageServices.GetService<INavigationBarItemService>();
            if (languageService != null)
            {
                // check whether we can re-use lastCompletedModel. otherwise, update lastCompletedModel here.
                // the model should be only updated here
                if (_lastCompletedModel != null)
                {
                    var semanticVersion = await document.Project.GetDependentSemanticVersionAsync(CancellationToken.None).ConfigureAwait(false);
                    if (_lastCompletedModel.SemanticVersionStamp == semanticVersion && SpanStillValid(_lastCompletedModel, snapshot, cancellationToken))
                    {
                        // it looks like we can re-use previous model
                        return _lastCompletedModel;
                    }
                }

                using (Logger.LogBlock(FunctionId.NavigationBar_ComputeModelAsync, cancellationToken))
                {
                    var items = await languageService.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
                    if (items != null)
                    {
                        items.Do(i => i.InitializeTrackingSpans(snapshot));
                        var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                        _lastCompletedModel = new NavigationBarModel(items, version, languageService);
                        return _lastCompletedModel;
                    }
                }
            }

            _lastCompletedModel = _lastCompletedModel ??
                    new NavigationBarModel(SpecializedCollections.EmptyList<NavigationBarItem>(), new VersionStamp(), null);
            return _lastCompletedModel;
        }

        private Task<NavigationBarSelectedTypeAndMember> _selectedItemInfoTask;
        private CancellationTokenSource _selectedItemInfoTaskCancellationSource = new CancellationTokenSource();

        /// <summary>
        /// Starts a new task to compute what item should be selected.
        /// </summary>
        private void StartSelectedItemUpdateTask(int delay, bool updateUIWhenDone)
        {
            AssertIsForeground();

            var currentView = _presenter.TryGetCurrentView();
            if (currentView == null)
            {
                return;
            }

            // Cancel off any existing work
            _selectedItemInfoTaskCancellationSource.Cancel();
            _selectedItemInfoTaskCancellationSource = new CancellationTokenSource();

            var cancellationToken = _selectedItemInfoTaskCancellationSource.Token;
            var subjectBufferCaretPosition = currentView.GetCaretPoint(_subjectBuffer);

            if (!subjectBufferCaretPosition.HasValue)
            {
                return;
            }

            var asyncToken = _asyncListener.BeginAsyncOperation(GetType().Name + ".StartSelectedItemUpdateTask");

            // Enqueue a new computation for the selected item
            _selectedItemInfoTask = _modelTask.ContinueWithAfterDelay(
                t => t.IsCanceled ? new NavigationBarSelectedTypeAndMember(null, null)
                                  : ComputeSelectedTypeAndMember(t.Result, subjectBufferCaretPosition.Value, cancellationToken),
                cancellationToken,
                delay,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            _selectedItemInfoTask.CompletesAsyncOperation(asyncToken);

            if (updateUIWhenDone)
            {
                asyncToken = _asyncListener.BeginAsyncOperation(GetType().Name + ".StartSelectedItemUpdateTask.UpdateUI");
                _selectedItemInfoTask.SafeContinueWith(
                    t => PushSelectedItemsToPresenter(t.Result),
                    cancellationToken,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                    ForegroundThreadAffinitizedObject.DefaultForegroundThreadData.TaskScheduler).CompletesAsyncOperation(asyncToken);
            }
        }

        internal static NavigationBarSelectedTypeAndMember ComputeSelectedTypeAndMember(NavigationBarModel model, SnapshotPoint caretPosition, CancellationToken cancellationToken)
        {
            var leftItem = GetMatchingItem(model.Types, caretPosition, model.ItemService, cancellationToken);

            if (leftItem.Item1 == null)
            {
                // Nothing to show at all
                return new NavigationBarSelectedTypeAndMember(null, null);
            }

            var rightItem = GetMatchingItem(leftItem.Item1.ChildItems, caretPosition, model.ItemService, cancellationToken);

            return new NavigationBarSelectedTypeAndMember(leftItem.Item1, leftItem.Item2, rightItem.Item1, rightItem.Item2);
        }

        /// <summary>
        /// Finds the item that point is in, or if it's not in any items, gets the first item that's
        /// positioned after the cursor.
        /// </summary>
        /// <returns>A tuple of the matching item, and if it should be shown grayed.</returns>
        private static ValueTuple<T, bool> GetMatchingItem<T>(IEnumerable<T> items, SnapshotPoint point, INavigationBarItemService itemsService, CancellationToken cancellationToken) where T : NavigationBarItem
        {
            T exactItem = null;
            int exactItemStart = 0;
            T nextItem = null;
            int nextItemStart = int.MaxValue;

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
                return ValueTuple.Create(exactItem, false);
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

                return ValueTuple.Create(itemToGray, itemToGray != null);
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
