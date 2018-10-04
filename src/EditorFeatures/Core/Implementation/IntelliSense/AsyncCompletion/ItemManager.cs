// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal class ItemManager : IAsyncCompletionItemManager
    {
        private readonly IAsyncCompletionBroker _broker;
        private readonly CompletionHelper _completionHelper;

        private const int MaxMRUSize = 10;
        private ImmutableArray<string> _recentItems = ImmutableArray<string>.Empty;

        public ItemManager(IAsyncCompletionBroker broker)
        {
            _broker = broker;
            _completionHelper = new CompletionHelper(isCaseSensitive: true);
        }

        public Task<ImmutableArray<VSCompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken cancellationToken)
        {
            session.ItemCommitted += ItemCommitted;
            session.Dismissed += SessionDismissed;
            return Task.FromResult(data.InitialList.OrderBy(i => i.SortText).ToImmutableArray());
        }

        public Task<AsyncCompletionData.FilteredCompletionModel> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateCompletionList(session, data, cancellationToken));
        }

        private AsyncCompletionData.FilteredCompletionModel UpdateCompletionList(
            IAsyncCompletionSession session,
            AsyncCompletionData.AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            if (!session.Properties.TryGetProperty<bool>(Source.HasSuggestionItemOptions, out bool hasSuggestedItemOptions))
            {
                // This is the scenario when the session is created out of Roslyn, in some other provider, e.g. in Debugger.
                // For now, the default hasSuggestedItemOptions is false. We can discuss if the opposite is required.
                hasSuggestedItemOptions = false;
            }

            var filterText = session.ApplicableToSpan.GetText(data.Snapshot);
            var reason = data.Trigger.Reason;

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
                    return new AsyncCompletionData.FilteredCompletionModel(ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight>.Empty, 0);
                }
            }

            // We need to filter if a non-empty strict subset of filters are selected
            var selectedFilters = data.SelectedFilters.Where(f => f.IsSelected).Select(f => f.Filter).ToImmutableArray();
            var needToFilter = selectedFilters.Length > 0 && selectedFilters.Length < data.SelectedFilters.Length;

            var initialListOfItemsToBeIncluded = new List<FilterResult>();
            foreach (var item in data.InitialSortedList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (needToFilter && ShouldBeFilteredOutOfCompletionList(item, selectedFilters))
                {
                    continue;
                }

                if (MatchesFilterText(item.FilterText, item.DisplayText, GetMatchPriority(item), filterText, reason))
                {
                    initialListOfItemsToBeIncluded.Add(new FilterResult(item, filterText, matchedFilterText: true));
                }
                else
                {
                    if (reason == AsyncCompletionData.CompletionTriggerReason.Deletion ||
                        reason == AsyncCompletionData.CompletionTriggerReason.Backspace ||
                        reason == AsyncCompletionData.CompletionTriggerReason.Invoke ||
                        filterText.Length <= 1)
                    {
                        initialListOfItemsToBeIncluded.Add(new FilterResult(item, filterText, matchedFilterText: false));
                    }
                }
            }

            // If the session was created/maintained out of Roslyn, e.g. in debugger; no properties are set and we should use data.Snapshot.
            var snapshotForDocument = data.InitialSortedList
                .FirstOrDefault(i => i.Properties.ContainsProperty(Source.TriggerSnapshot))?.
                Properties.GetProperty<ITextSnapshot>(Source.TriggerSnapshot) ?? data.Snapshot;

            var document = snapshotForDocument.GetOpenDocumentInCurrentContextWithChanges();

            if (document == null)
            {
                var listWithSelections = GetHighlightedList(initialListOfItemsToBeIncluded, filterText);
                return CreateDefaultFilteredCompletionModel(listWithSelections.ToImmutableArray(), data.SelectedFilters);
            }

            if (initialListOfItemsToBeIncluded.Count == 0)
            {
                var completionService = document.GetLanguageService<CompletionService>();
                if (completionService == null)
                {
                    return CreateDefaultFilteredCompletionModel(ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight>.Empty, data.SelectedFilters);
                }

                var completionRules = completionService.GetRules();
                return HandleAllItemsFilteredOut(reason, data.SelectedFilters, selectedFilters, completionRules);
            }

            // If this was deletion, then we control the entire behavior of deletion ourselves.
            if (reason == AsyncCompletionData.CompletionTriggerReason.Deletion ||
                reason == AsyncCompletionData.CompletionTriggerReason.Backspace)
            {
                return HandleDeletionTrigger(data.InitialSortedList, reason, 
                    data.SelectedFilters, reason, filterText, initialListOfItemsToBeIncluded);
            }

            var caretPoint = session.TextView.GetCaretPoint(data.Snapshot.TextBuffer);
            var caretPosition = caretPoint?.Position;

            return HandleNormalFiltering(
                data.InitialSortedList,
                snapshotForDocument,
                document,
                caretPosition,
                filterText,
                data.SelectedFilters,
                reason,
                initialListOfItemsToBeIncluded,
                hasSuggestedItemOptions);
        }

        private static AsyncCompletionData.FilteredCompletionModel CreateDefaultFilteredCompletionModel(
            ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight> items,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters)
            => new AsyncCompletionData.FilteredCompletionModel(
                    items: items, 
                    selectedItemIndex: 0,
                    filters: filters, 
                    selectionHint: AsyncCompletionData.UpdateSelectionHint.NoChange, 
                    centerSelection: true, 
                    uniqueItem: default);

        private static bool IsAfterDot(ITextSnapshot snapshot, ITrackingSpan applicableToSpan)
        {
            var position = applicableToSpan.GetStartPoint(snapshot).Position;
            return position > 0 && snapshot[position - 1] == '.';
        }

        private AsyncCompletionData.FilteredCompletionModel HandleNormalFiltering(
            ImmutableArray<VSCompletionItem> sortedList,
            ITextSnapshot snapshot,
            Document document,
            int? caretPosition,
            string filterText,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            AsyncCompletionData.CompletionTriggerReason triggerReason,
            List<FilterResult> itemsInList,
            bool hasSuggestedItemOptions)
        {
            var highlightedList = GetHighlightedList(itemsInList, filterText).ToImmutableArray();

            // Not deletion.  Defer to the language to decide which item it thinks best
            // matches the text typed so far.

            // Ask the language to determine which of the *matched* items it wants to select.
            var completionService = document.GetLanguageService<CompletionService>();
            if (completionService == null)
            {
                return CreateDefaultFilteredCompletionModel(highlightedList, filters);
            }

            var matchingItems = itemsInList.Where(r => r.MatchedFilterText)
                                           .Select(t => GetOrCreateRoslynItem(t.CompletionItem))
                                           .AsImmutable();

            var chosenItems = completionService.FilterItems(document, matchingItems, filterText);

            var recentItems = _recentItems;

            // Of the items the service returned, pick the one most recently committed
            var bestItem = GetBestItemBasedOnMRU(chosenItems, recentItems);
            var updatedFilters = GetUpdatedFilters(sortedList, itemsInList, filters, filterText);
            VSCompletionItem uniqueItem = null;
            int selectedItemIndex = 0;

            // TODO: Can we get away with less complexity here by only doing hard select on preselection and not on regular filter text matching / etc...
            // https://github.com/dotnet/roslyn/issues/29108

            // Determine if we should consider this item 'unique' or not.  A unique item
            // will be automatically committed if the user hits the 'invoke completion' 
            // without bringing up the completion list.  An item is unique if it was the
            // only item to match the text typed so far, and there was at least some text
            // typed.  i.e.  if we have "Console.$$" we don't want to commit something
            // like "WriteLine" since no filter text has actually been provided.  HOwever,
            // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
            if (bestItem != null)
            {
                selectedItemIndex = itemsInList.IndexOf(i => Equals(GetOrCreateRoslynItem(i.CompletionItem), bestItem));
                if (selectedItemIndex > -1 && bestItem != null && matchingItems.Length == 1 && filterText.Length > 0)
                {
                    uniqueItem = highlightedList[selectedItemIndex].CompletionItem;
                }
            }

            // If we don't have a best completion item yet, then pick the first item from the list.
            var bestOrFirstCompletionItem = bestItem ?? GetOrCreateRoslynItem(itemsInList.First().CompletionItem);

            var updateSelectionHint = GetUpdateSelectionHint(bestOrFirstCompletionItem, snapshot, caretPosition, triggerReason, filterText, selectedItemIndex, hasSuggestedItemOptions);

            if (selectedItemIndex == -1)
            {
                selectedItemIndex = 0;
            }

            return new AsyncCompletionData.FilteredCompletionModel(
                highlightedList, selectedItemIndex, updatedFilters,
                updateSelectionHint, centerSelection: true, uniqueItem);
        }

        private RoslynCompletionItem GetBestItemBasedOnMRU(ImmutableArray<RoslynCompletionItem> chosenItems, ImmutableArray<string> recentItems)
        {
            if (chosenItems.Length == 0)
            {
                return null;
            }

            // Try to find the chosen item has been most recently used.
            var bestItem = chosenItems.First();

            for (int i = 0, n = chosenItems.Length; i < n; i++)
            {
                var chosenItem = chosenItems[i];
                var mruIndex1 = recentItems.IndexOf(bestItem.DisplayText);
                var mruIndex2 = recentItems.IndexOf(chosenItem.DisplayText);

                if (mruIndex2 > mruIndex1)
                {
                    bestItem = chosenItem;
                }
            }

            // If our best item appeared in the MRU list, use it
            if (recentItems.IndexOf(bestItem.DisplayText) >= 0)
            {
                return bestItem;
            }

            // Otherwise use the chosen item that has the highest matchPriority.
            for (int i = 1, n = chosenItems.Length; i < n; i++)
            {
                var chosenItem = chosenItems[i];

                var bestItemPriority = GetMatchPriority(bestItem);
                var currentItemPriority = GetMatchPriority(chosenItem);

                if (currentItemPriority > bestItemPriority)
                {
                    bestItem = chosenItem;
                }
            }

            return bestItem;
        }

        private AsyncCompletionData.FilteredCompletionModel HandleDeletionTrigger(
            ImmutableArray<VSCompletionItem> sortedList,
            AsyncCompletionData.CompletionTriggerReason triggerReason,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            AsyncCompletionData.CompletionTriggerReason filterReason,
            string filterText,
            List<FilterResult> filterResults)
        {
            FilterResult? bestFilterResult = null;
            int matchCount = 0;
            foreach (var currentFilterResult in filterResults.Where(r => r.MatchedFilterText))
            {
                if (bestFilterResult == null ||
                    IsBetterDeletionMatch(currentFilterResult, bestFilterResult.Value))
                {
                    // We had no best result yet, so this is now our best result.
                    bestFilterResult = currentFilterResult;
                    matchCount++;
                }
            }

            // If we had a matching item, then pick the best of the matching items and
            // choose that one to be hard selected.  If we had no actual matching items
            // (which can happen if the user deletes down to a single character and we
            // include everything), then we just soft select the first item.

            var filteredItems = filterResults.Select(r => r.CompletionItem).AsImmutable();
            var highlightedList = GetHighlightedList(filterResults, filterText).ToImmutableArray();
            var updatedFilters = GetUpdatedFilters(sortedList, filterResults, filters, filterText);

            if (bestFilterResult != null)
            {
                // Only hard select this result if it's a prefix match
                // We need to do this so that
                // * deleting and retyping a dot in a member access does not change the
                //   text that originally appeared before the dot
                // * deleting through a word from the end keeps that word selected
                // This also preserves the behavior the VB had through Dev12.
                var hardSelect = bestFilterResult.Value.CompletionItem.FilterText.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase);

                return new AsyncCompletionData.FilteredCompletionModel(highlightedList, filteredItems.IndexOf(bestFilterResult.Value.CompletionItem), updatedFilters, 
                    hardSelect ? AsyncCompletionData.UpdateSelectionHint.NoChange : AsyncCompletionData.UpdateSelectionHint.SoftSelected, 
                    centerSelection: true, uniqueItem: null);
            }
            else
            {
                return new AsyncCompletionData.FilteredCompletionModel(highlightedList, selectedItemIndex: 0, updatedFilters, 
                    AsyncCompletionData.UpdateSelectionHint.SoftSelected, centerSelection: true, uniqueItem: null);
            }
        }

        private bool IsBetterDeletionMatch(FilterResult result1, FilterResult result2)
        {
            var item1 = result1.CompletionItem;
            var item2 = result2.CompletionItem;

            var prefixLength1 = item1.FilterText.GetCaseInsensitivePrefixLength(result1.FilterText);
            var prefixLength2 = item2.FilterText.GetCaseInsensitivePrefixLength(result2.FilterText);

            // Prefer the item that matches a longer prefix of the filter text.
            if (prefixLength1 > prefixLength2)
            {
                return true;
            }

            if (prefixLength1 == prefixLength2)
            {
                // If the lengths are the same, prefer the one with the higher match priority.
                // But only if it's an item that would have been hard selected.  We don't want
                // to aggressively select an item that was only going to be softly offered.

                var item1Priority = CalculatePriority(item1);
                var item2Priority = CalculatePriority(item2);

                if (item1Priority > item2Priority)
                {
                    return true;
                }
            }

            return false;
        }

        private AsyncCompletionData.FilteredCompletionModel HandleAllItemsFilteredOut(
            AsyncCompletionData.CompletionTriggerReason triggerReason,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            ImmutableArray<AsyncCompletionData.CompletionFilter> activeFilters,
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
                selection = activeFilters.Length == 0 ? AsyncCompletionData.UpdateSelectionHint.SoftSelected : AsyncCompletionData.UpdateSelectionHint.NoChange;
            }

            return new AsyncCompletionData.FilteredCompletionModel(
                ImmutableArray<AsyncCompletionData.CompletionItemWithHighlight>.Empty, selectedItemIndex: 0,
                filters, selection, centerSelection: true, uniqueItem: default);
        }

        private bool MatchesFilterText(
            string itemFilterText,
            string itemDisplayText,
            int matchPriority,
            string filterText,
            AsyncCompletionData.CompletionTriggerReason triggerReason)
        {
            // For the deletion we bake in the core logic for how matching should work.
            // This way deletion feels the same across all languages that opt into deletion
            // as a completion trigger.

            // Specifically, to avoid being too aggressive when matching an item during
            // completion, we require that the current filter text be a prefix of the
            // item in the list.
            if (triggerReason == AsyncCompletionData.CompletionTriggerReason.Deletion || triggerReason == AsyncCompletionData.CompletionTriggerReason.Backspace)
            {
                return itemFilterText.GetCaseInsensitivePrefixLength(filterText) > 0;
            }

            // If the user hasn't typed anything, and this item was preselected, or was in the
            // MRU list, then we definitely want to include it.
            if (filterText.Length == 0)
            {
                // TODO: Need ItemRules.MatchPriority.
                if (matchPriority > MatchPriority.Default)
                {
                    return true;
                }

                if (!_recentItems.IsDefault && _recentItems.IndexOf(itemDisplayText) >= 0)
                {
                    return true;
                }
            }

            // Checks if the given completion item matches the pattern provided so far. 
            // A  completion item is checked against the pattern by see if it's 
            // CompletionItem.FilterText matches the item.  That way, the pattern it checked 
            // against terms like "IList" and not IList<>
            return _completionHelper.MatchesPattern(itemFilterText, filterText, CultureInfo.CurrentCulture);
        }

        private bool ShouldSoftSelectItem(RoslynCompletionItem item, string filterText, AsyncCompletionData.CompletionTriggerReason triggerReason)
        {
            // If all that has been typed is puntuation, then don't hard select anything.
            // It's possible the user is just typing language punctuation and selecting
            // anything in the list will interfere.  We only allow this if the filter text
            // exactly matches something in the list already.
            if (filterText.Length > 0 && IsAllPunctuation(filterText) && filterText != item.DisplayText)
            {
                return true;
            }

            // If the user hasn't actually typed anything, then don't hard select any item.
            // The only exception to this is if the completion provider has requested the
            // item be preselected.
            if (filterText.Length == 0)
            {
                // Item didn't want to be hard selected with no filter text.
                // So definitely soft select it.
                if (item.Rules.SelectionBehavior != CompletionItemSelectionBehavior.HardSelection)
                {
                    return true;
                }

                // Item did not ask to be preselected.  So definitely soft select it.
                if (item.Rules.MatchPriority == MatchPriority.Default)
                {
                    return true;
                }
            }

            // The user typed something, or the item asked to be preselected.  In 
            // either case, don't soft select this.
            Debug.Assert(filterText.Length > 0 || item.Rules.MatchPriority != MatchPriority.Default);
            return false;
        }

        private static bool IsAllPunctuation(string filterText)
        {
            foreach (var ch in filterText)
            {
                if (!char.IsPunctuation(ch))
                {
                    return false;
                }
            }

            return true;
        }

        private int CalculatePriority(VSCompletionItem item)
            => GetOrCreateRoslynItem(item)?.Rules?.SelectionBehavior != CompletionItemSelectionBehavior.HardSelection
                    ? MatchPriority.Default
                    : GetMatchPriority(item);

        private static RoslynCompletionItem GetOrCreateRoslynItem(VSCompletionItem item)
        {
            if (item.Properties.TryGetProperty<RoslynCompletionItem>(Source.RoslynItem, out var roslynItem))
            {
                return roslynItem;
            }

            return RoslynCompletionItem.Create(item.DisplayText, item.FilterText, item.SortText);
        }

        private AsyncCompletionData.UpdateSelectionHint GetUpdateSelectionHint(
            RoslynCompletionItem bestItem, ITextSnapshot snapshot, int? caretPosition, 
            AsyncCompletionData.CompletionTriggerReason triggerReason, string filterText, int selectedItemIndex, bool hasSuggestedItemOptions)
        {
            if (bestItem == null || hasSuggestedItemOptions)
            {
                return AsyncCompletionData.UpdateSelectionHint.SoftSelected;
            }

            // We don't have a builder and we have a best match.  Normally this will be hard
            // selected, except for a few cases.  Specifically, if no filter text has been
            // provided, and this is not a preselect match then we will soft select it.  This
            // happens when the completion list comes up implicitly and there is something in
            // the MRU list.  In this case we do want to select it, but not with a hard
            // selection.  Otherwise you can end up with the following problem:
            //
            //  dim i as integer =<space>
            //
            // Completion will comes up after = with 'integer' selected (Because of MRU).  We do
            // not want 'space' to commit this.
            if (ShouldSoftSelectItem(bestItem, filterText, triggerReason))
            {
                return AsyncCompletionData.UpdateSelectionHint.SoftSelected;
            }

            // If the user moved the caret left after they started typing, the 'best' match may not match at all
            // against the full text span that this item would be replacing.
            if (!MatchesFilterText(bestItem.FilterText, bestItem.DisplayText, bestItem.Rules.MatchPriority, filterText, triggerReason))
            {
                return AsyncCompletionData.UpdateSelectionHint.SoftSelected;
            }

            // Switch to soft selection, if user moved caret to the start of a non-empty filter span.
            // This prevents commiting if user types a commit character at this position later, but 
            // still has the list if user types filter character
            // i.e. blah| -> |blah -> !|blah
            // We want the filter span non-empty because we still want hard selection in the following case:
            //
            //  A a = new |
            if (caretPosition == bestItem.Span.Start && bestItem.Span.Length > 0)
            {
                return AsyncCompletionData.UpdateSelectionHint.SoftSelected;
            }

            // There was either filter text, or this was a preselect match. In either case, we can hard select this.
            return AsyncCompletionData.UpdateSelectionHint.Selected;
        }

        private IEnumerable<AsyncCompletionData.CompletionItemWithHighlight> GetHighlightedList(List<FilterResult> filterResults, string filterText)
        {
            var highlightedList = new List<AsyncCompletionData.CompletionItemWithHighlight>();
            foreach (var item in filterResults)
            {
                var highlightedSpans = _completionHelper.GetHighlightedSpans(item.CompletionItem.FilterText, filterText, CultureInfo.CurrentCulture);
                highlightedList.Add(new AsyncCompletionData.CompletionItemWithHighlight(item.CompletionItem, highlightedSpans.Select(s => s.ToSpan()).ToImmutableArray()));
            }

            return highlightedList;
        }

        private ImmutableArray<AsyncCompletionData.CompletionFilterWithState> GetUpdatedFilters(
            ImmutableArray<VSCompletionItem> originalList,
            List<FilterResult> filteredList,
            ImmutableArray<AsyncCompletionData.CompletionFilterWithState> filters,
            string filterText)
        {
            // See which filters might be enabled based on the typed code
            var textFilteredFilters = filteredList.SelectMany(n => n.CompletionItem.Filters).Distinct();

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

        private bool ShouldBeFilteredOutOfCompletionList(VSCompletionItem item, ImmutableArray<AsyncCompletionData.CompletionFilter> activeFilters)
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

        private int GetMatchPriority(VSCompletionItem item)
            =>  GetOrCreateRoslynItem(item)?.Rules?.MatchPriority ?? MatchPriority.Default;

        private int GetMatchPriority(RoslynCompletionItem bestItem)
            => bestItem.Rules.MatchPriority;

        private void ItemCommitted(object sender, AsyncCompletionData.CompletionItemEventArgs e)
        {
            MakeMostRecentItem(e.Item.DisplayText);
        }

        private void SessionDismissed(object sender, EventArgs e)
        {
            if (sender is IAsyncCompletionSession session)
            {
                session.ItemCommitted -= ItemCommitted;
                session.Dismissed -= SessionDismissed;
            }
        }

        private readonly struct FilterResult
        {
            public readonly VSCompletionItem CompletionItem;
            public readonly string FilterText;
            public readonly bool MatchedFilterText;

            public FilterResult(VSCompletionItem item, string filterText, bool matchedFilterText)
            {
                CompletionItem = item;
                FilterText = filterText;
                MatchedFilterText = matchedFilterText;
            }
        }
    }
}
