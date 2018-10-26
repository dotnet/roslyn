// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using FilterResult = Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Controller.Session.FilterResult;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;
using Session = Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Controller.Session;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal class ItemManager : ForegroundThreadAffinitizedObject, IAsyncCompletionItemManager
    {
        private readonly CompletionHelper _completionHelper;

        private const int MaxMRUSize = 10;
        private ImmutableArray<string> _recentItems = ImmutableArray<string>.Empty;

        internal ItemManager(IThreadingContext threadingContext) : base(threadingContext)
        {
            _completionHelper = new CompletionHelper(isCaseSensitive: true);
        }

        public Task<ImmutableArray<VSCompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken cancellationToken)
        {
            AssertIsBackground();

            SubscribeEvents(session);
            return Task.FromResult(data.InitialList.OrderBy(i => i.SortText).ToImmutableArray());
        }

        public Task<AsyncCompletionData.FilteredCompletionModel> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
            => Task.FromResult(UpdateCompletionList(session, data, cancellationToken));

        private AsyncCompletionData.FilteredCompletionModel UpdateCompletionList(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            AssertIsBackground();

            SubscribeEvents(session);
            if (!session.Properties.TryGetProperty<bool>(CompletionSource.HasSuggestionItemOptions, out bool hasSuggestedItemOptions))
            {
                // This is the scenario when the session is created out of Roslyn, in some other provider, e.g. in Debugger.
                // For now, the default hasSuggestedItemOptions is false. We can discuss if the opposite is required.
                hasSuggestedItemOptions = false;
            }

            var filterText = session.ApplicableToSpan.GetText(data.Snapshot);
            var reason = data.Trigger.Reason;

            // We do not care about the character in the case. We care about the reason only.
            if (!Helpers.TryGetRoslynTrigger(data.Trigger, data.Trigger.Character, out var roslynTrigger))
            {
                return null;
            }

            if (!session.Properties.TryGetProperty<RoslynTrigger>(CompletionSource.InitialTrigger, out var initialRoslynTrigger))
            {
                return null;
            }

            // Check if the user is typing a number. If so, only proceed if it's a number
            // directly after a <dot>. That's because it is actually reasonable for completion
            // to be brought up after a <dot> and for the user to want to filter completion
            // items based on a number that exists in the name of the item. However, when
            // we are not after a dot (i.e. we're being brought up after <space> is typed)
            // then we don't want to filter things. Consider the user writing:
            //
            //      dim i =<space>
            //
            // We'll bring up the completion list here (as VB has completion on <space>).
            // If the user then types '3', we don't want to match against Int32.
            if (filterText.Length > 0 && char.IsNumber(filterText[0]))
            {
                if (!IsAfterDot(data.Snapshot, session.ApplicableToSpan))
                {
                    session.Dismiss();
                    return null;
                }
            }

            // We need to filter if a non-empty strict subset of filters are selected
            var selectedFilters = data.SelectedFilters.Where(f => f.IsSelected).Select(f => f.Filter).ToImmutableArray();
            var needToFilter = selectedFilters.Length > 0 && selectedFilters.Length < data.SelectedFilters.Length;
            var filterReason = Helpers.GetFilterReason(roslynTrigger);

            var initialListOfItemsToBeIncluded = new List<ExtendedFilterResult>();
            foreach (var item in data.InitialSortedList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (needToFilter && ShouldBeFilteredOutOfCompletionList(item, selectedFilters))
                {
                    continue;
                }

                if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionSource.RoslynItem, out var roslynItem))
                {
                    roslynItem = RoslynCompletionItem.Create(item.DisplayText, item.FilterText, item.SortText);
                }

                if (Session.MatchesFilterText(_completionHelper, roslynItem, filterText, initialRoslynTrigger.Kind, filterReason, _recentItems))
                {
                    initialListOfItemsToBeIncluded.Add(new ExtendedFilterResult(item, new FilterResult(roslynItem, filterText, matchedFilterText: true)));
                }
                else
                {
                    // The item didn't match the filter text.  We'll still keep it in the list
                    // if one of two things is true:
                    //
                    //  1. The user has only typed a single character.  In this case they might
                    //     have just typed the character to get completion.  Filtering out items
                    //     here is not desirable.
                    //
                    //  2. They brough up completion with ctrl-j or through deletion.  In these
                    //     cases we just always keep all the items in the list.
                    if (roslynTrigger.Kind == CompletionTriggerKind.Deletion ||
                        roslynTrigger.Kind == CompletionTriggerKind.Invoke ||
                        filterText.Length <= 1)
                    {
                        initialListOfItemsToBeIncluded.Add(new ExtendedFilterResult(item, new FilterResult(roslynItem, filterText, matchedFilterText: false)));
                    }
                }
            }

            // If the session was created/maintained out of Roslyn, e.g. in debugger; no properties are set and we should use data.Snapshot.
            // However, we prefer using the original snapshot in some projection scenarios.
            var snapshotForDocument = data.InitialSortedList
                                          .FirstOrDefault(i => i.Properties.ContainsProperty(CompletionSource.TriggerSnapshot))?
                                          .Properties.GetProperty<ITextSnapshot>(CompletionSource.TriggerSnapshot) 
                                          ?? data.Snapshot;

            var document = snapshotForDocument.GetOpenDocumentInCurrentContextWithChanges();

            if (document == null)
            {
                return null;
            }

            var completionService = document.GetLanguageService<CompletionService>();
            if (completionService == null)
            {
                return null;
            }

            var completionRules = completionService.GetRules();

            var disconnectedBufferGraph = new DisconnectedBufferGraph(session.ApplicableToSpan.TextBuffer, session.TextView.TextBuffer);
            if (data.Trigger.Reason == AsyncCompletionData.CompletionTriggerReason.Backspace &&
                completionRules.DismissIfLastCharacterDeleted &&
                session.ApplicableToSpan.GetText(data.Snapshot).Length == 0)
            {
                return null;
            }

            if (initialListOfItemsToBeIncluded.Count == 0)
            {
                return HandleAllItemsFilteredOut(reason, data.SelectedFilters, selectedFilters.Any(), completionRules);
            }

            var options = document.Project.Solution.Options;
            var highlightMatchingPortions = options.GetOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, document.Project.Language);
            var showCompletionItemFilters = options.GetOption(CompletionOptions.ShowCompletionItemFilters, document.Project.Language);

            var updatedFilters = showCompletionItemFilters
                ? GetUpdatedFilters(initialListOfItemsToBeIncluded, data.SelectedFilters)
                : ImmutableArray<AsyncCompletionData.CompletionFilterWithState>.Empty;

            var highlightedList = GetHighlightedList(initialListOfItemsToBeIncluded, filterText, highlightMatchingPortions).ToImmutableArray();

            // If this was deletion, then we control the entire behavior of deletion ourselves.
            if (initialRoslynTrigger.Kind == CompletionTriggerKind.Deletion)
            {
                return HandleDeletionTrigger(initialListOfItemsToBeIncluded, filterText, updatedFilters, highlightedList);
            }

            return HandleNormalFiltering(
                document,
                completionService,
                filterText,
                updatedFilters,
                initialRoslynTrigger.Kind,
                filterReason,
                data.Trigger.Character,
                initialListOfItemsToBeIncluded,
                highlightedList,
                hasSuggestedItemOptions);
        }

        private static bool IsAfterDot(ITextSnapshot snapshot, ITrackingSpan applicableToSpan)
        {
            var position = applicableToSpan.GetStartPoint(snapshot).Position;
            return position > 0 && snapshot[position - 1] == '.';
        }

        private AsyncCompletionData.FilteredCompletionModel HandleNormalFiltering(
            Document document,
            CompletionService completionService,
            string filterText,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            CompletionTriggerKind initialRoslynTriggerKind,
            CompletionFilterReason filterReason,
            char typeChar,
            List<ExtendedFilterResult> itemsInList,
            ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight> highlightedList,
            bool hasSuggestedItemOptions)
        {
            // Not deletion.  Defer to the language to decide which item it thinks best
            // matches the text typed so far.

            // Ask the language to determine which of the *matched* items it wants to select.
            var matchingItems = itemsInList.Where(r => r.FilterResult.MatchedFilterText)
                                           .Select(t => t.FilterResult.CompletionItem)
                                           .AsImmutable();

            var chosenItems = completionService.FilterItems(document, matchingItems, filterText);

            var recentItems = _recentItems;

            // Of the items the service returned, pick the one most recently committed
            var bestItem = Session.GetBestCompletionItemBasedOnMRU(chosenItems, recentItems);
            VSCompletionItem uniqueItem = null;
            int selectedItemIndex = 0;

            // Determine if we should consider this item 'unique' or not.  A unique item
            // will be automatically committed if the user hits the 'invoke completion' 
            // without bringing up the completion list.  An item is unique if it was the
            // only item to match the text typed so far, and there was at least some text
            // typed.  i.e.  if we have "Console.$$" we don't want to commit something
            // like "WriteLine" since no filter text has actually been provided.  HOwever,
            // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
            if (bestItem != null)
            {
                selectedItemIndex = itemsInList.IndexOf(i => Equals(i.FilterResult.CompletionItem, bestItem));
                if (selectedItemIndex > -1 && bestItem != null && matchingItems.Length == 1 && filterText.Length > 0)
                {
                    uniqueItem = highlightedList[selectedItemIndex].CompletionItem;
                }
            }

            // If we don't have a best completion item yet, then pick the first item from the list.
            var bestOrFirstCompletionItem = bestItem ?? itemsInList.First().FilterResult.CompletionItem;

            // Check that it is a filter symbol. We can be called for a non-filter symbol.
            if (filterReason == CompletionFilterReason.Insertion && 
                !Controller.IsPotentialFilterCharacter(typeChar) && 
                !string.IsNullOrEmpty(filterText) && 
                !Controller.IsFilterCharacter(bestOrFirstCompletionItem, typeChar, filterText))
            {
                return null;
            }

            var updateSelectionHint = Session.IsHardSelection(
                        filterText, initialRoslynTriggerKind, bestOrFirstCompletionItem,
                        _completionHelper, filterReason, recentItems, hasSuggestedItemOptions)
                        ? AsyncCompletionData.UpdateSelectionHint.Selected
                        : AsyncCompletionData.UpdateSelectionHint.SoftSelected;

            if (selectedItemIndex == -1)
            {
                selectedItemIndex = 0;
            }

            return new AsyncCompletionData.FilteredCompletionModel(
                highlightedList, selectedItemIndex, filters,
                updateSelectionHint, centerSelection: true, uniqueItem);
        }

        private AsyncCompletionData.FilteredCompletionModel HandleDeletionTrigger(
            List<ExtendedFilterResult> filterResults,
            string filterText,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight> highlightedList)
        {
            ExtendedFilterResult? bestFilterResult = null;
            int matchCount = 0;
            foreach (var currentFilterResult in filterResults.Where(r => r.FilterResult.MatchedFilterText))
            {
                if (bestFilterResult == null ||
                    Session.IsBetterDeletionMatch(currentFilterResult.FilterResult, bestFilterResult.Value.FilterResult))
                {
                    // We had no best result yet, so this is now our best result.
                    bestFilterResult = currentFilterResult;
                    matchCount++;
                }
            }

            int index;
            bool hardSelect;

            // If we had a matching item, then pick the best of the matching items and
            // choose that one to be hard selected.  If we had no actual matching items
            // (which can happen if the user deletes down to a single character and we
            // include everything), then we just soft select the first item.
            if (bestFilterResult != null)
            {
                // Only hard select this result if it's a prefix match
                // We need to do this so that
                // * deleting and retyping a dot in a member access does not change the
                //   text that originally appeared before the dot
                // * deleting through a word from the end keeps that word selected
                // This also preserves the behavior the VB had through Dev12.
                hardSelect = bestFilterResult.Value.VSCompletionItem.FilterText.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase);
                index = filterResults.IndexOf(bestFilterResult.Value);
            }
            else
            {
                index = 0;
                hardSelect = false;
            }

            return new AsyncCompletionData.FilteredCompletionModel(
                highlightedList, index, filters,
                hardSelect ? AsyncCompletionData.UpdateSelectionHint.Selected : AsyncCompletionData.UpdateSelectionHint.SoftSelected,
                centerSelection: true, uniqueItem: null);
        }

        private AsyncCompletionData.FilteredCompletionModel HandleAllItemsFilteredOut(
            AsyncCompletionData.CompletionTriggerReason triggerReason,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            bool hasActiveFilters,
            CompletionRules completionRules)
        {
            AsyncCompletionData.UpdateSelectionHint selection;
            if (triggerReason == AsyncCompletionData.CompletionTriggerReason.Insertion)
            {
                // If the user was just typing, and the list went to empty *and* this is a 
                // language that wants to dismiss on empty, then just return a null model
                // to stop the completion session.
                if (completionRules.DismissIfEmpty)
                {
                    return null;
                }

                selection = AsyncCompletionData.UpdateSelectionHint.NoChange;
            }
            else
            {
                // If the user has turned on some filtering states, and we filtered down to
                // nothing, then we do want the UI to show that to them.  That way the user
                // can turn off filters they don't want and get the right set of items.

                // If we are going to filter everything out, then just preserve the existing
                // model (and all the previously filtered items), but switch over to soft
                // selection.
                selection = hasActiveFilters ? AsyncCompletionData.UpdateSelectionHint.NoChange : AsyncCompletionData.UpdateSelectionHint.SoftSelected;
            }

            return new AsyncCompletionData.FilteredCompletionModel(
                ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight>.Empty, selectedItemIndex: 0,
                filters, selection, centerSelection: true, uniqueItem: default);
        }

        private IEnumerable<AsyncCompletionData.CompletionItemWithHighlight> GetHighlightedList(
            IEnumerable<ExtendedFilterResult> filterResults, 
            string filterText,
            bool highlightMatchingPortions)
        {
            var highlightedList = new List<AsyncCompletionData.CompletionItemWithHighlight>();
            foreach (var item in filterResults)
            {
                var highlightedSpans = highlightMatchingPortions
                                        ? _completionHelper.GetHighlightedSpans(item.VSCompletionItem.FilterText, filterText, CultureInfo.CurrentCulture)
                                        : ImmutableArray<TextSpan>.Empty;
                highlightedList.Add(new AsyncCompletionData.CompletionItemWithHighlight(item.VSCompletionItem, highlightedSpans.Select(s => s.ToSpan()).ToImmutableArray()));
            }

            return highlightedList;
        }

        private static ImmutableArray<AsyncCompletionData.CompletionFilterWithState> GetUpdatedFilters(
            List<ExtendedFilterResult> filteredList,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters)
        {
            // See which filters might be enabled based on the typed code
            var textFilteredFilters = filteredList.SelectMany(n => n.VSCompletionItem.Filters).Distinct();

            // When no items are available for a given filter, it becomes unavailable
            return ImmutableArray.CreateRange(filters.Select(n => n.WithAvailability(textFilteredFilters.Contains(n.Filter))));
        }

        private void MakeMostRecentItem(string item)
        {
            var updated = false;

            while (!updated)
            {
                var oldItems = _recentItems;
                var newItems = oldItems.Remove(item);

                if (newItems.Length == MaxMRUSize)
                {
                    // Remove the least recent item.
                    newItems = newItems.RemoveAt(0);
                }

                newItems = newItems.Add(item);
                updated = ImmutableInterlocked.InterlockedCompareExchange(ref _recentItems, newItems, oldItems) == oldItems;
            }
        }

        private static bool ShouldBeFilteredOutOfCompletionList(VSCompletionItem item, ImmutableArray<AsyncCompletionData.CompletionFilter> activeFilters)
        {
            foreach (var itemFilter in item.Filters)
            {
                if (activeFilters.Contains(itemFilter))
                {
                    return false;
                }
            }

            return true;
        }

        private void ItemCommitted(object sender, AsyncCompletionData.CompletionItemEventArgs e)
        {
            MakeMostRecentItem(e.Item.DisplayText);
        }

        private void SessionDismissed(object sender, EventArgs e)
        {
            if (sender is IAsyncCompletionSession session)
            {
                UnsubscribeEvents(session);
            }
        }

        private void SubscribeEvents(IAsyncCompletionSession session)
        {
            UnsubscribeEvents(session);
            session.ItemCommitted += ItemCommitted;

            // We can be called multiple times for Update and for Sort during the lifecycle of the session.
            // Then, the session can be dismissed out of our code. We should be sure, we do not keep references to dismissed sessions.
            session.Dismissed += SessionDismissed;
        }

        private void UnsubscribeEvents(IAsyncCompletionSession session)
        {
            session.ItemCommitted -= ItemCommitted;
            session.Dismissed -= SessionDismissed;
        }

        private readonly struct ExtendedFilterResult
        {
            public readonly VSCompletionItem VSCompletionItem;
            public readonly FilterResult FilterResult;

            public ExtendedFilterResult(VSCompletionItem item, FilterResult filterResult)
            {
                VSCompletionItem = item;
                FilterResult = filterResult;
            }
        }
    }
}
