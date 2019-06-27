// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal class ItemManager : IAsyncCompletionItemManager
    {
        /// <summary>
        /// Used for filtering non-Roslyn data only. 
        /// </summary>
        private readonly CompletionHelper _defaultCompletionHelper;

        private readonly RecentItemsManager _recentItemsManager;

        /// <summary>
        /// For telemetry.
        /// </summary>
        private readonly object _targetTypeCompletionFilterChosenMarker = new object();

        internal ItemManager(RecentItemsManager recentItemsManager)
        {
            // Let us make the completion Helper used for non-Roslyn items case-sensitive.
            // We can change this if get requests from partner teams.
            _defaultCompletionHelper = new CompletionHelper(isCaseSensitive: true);
            _recentItemsManager = recentItemsManager;
        }

        public Task<ImmutableArray<VSCompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken cancellationToken)
        {
            if (session.TextView.Properties.TryGetProperty(CompletionSource.TargetTypeFilterExperimentEnabled, out bool isTargetTypeFilterEnabled) && isTargetTypeFilterEnabled)
            {
                AsyncCompletionLogger.LogSessionHasTargetTypeFilterEnabled();

                // This method is called exactly once, so use the opportunity to set a baseline for telemetry.
                if (data.InitialList.Any(i => i.Filters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches)))
                {
                    AsyncCompletionLogger.LogSessionContainsTargetTypeFilter();
                }
            }

            if (session.TextView.Properties.TryGetProperty(CompletionSource.TypeImportCompletionEnabled, out bool isTypeImportCompletionEnabled) && isTypeImportCompletionEnabled)
            {
                AsyncCompletionLogger.LogSessionWithTypeImportCompletionEnabled();
            }


            return Task.FromResult(data.InitialList.OrderBy(i => i.SortText).ToImmutableArray());
        }

        public Task<FilteredCompletionModel> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
            => Task.FromResult(UpdateCompletionList(session, data, cancellationToken));

        private FilteredCompletionModel UpdateCompletionList(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            if (!session.Properties.TryGetProperty(CompletionSource.HasSuggestionItemOptions, out bool hasSuggestedItemOptions))
            {
                // This is the scenario when the session is created out of Roslyn, in some other provider, e.g. in Debugger.
                // For now, the default hasSuggestedItemOptions is false.
                hasSuggestedItemOptions = false;
            }

            hasSuggestedItemOptions |= data.DisplaySuggestionItem;

            var filterText = session.ApplicableToSpan.GetText(data.Snapshot);
            var reason = data.Trigger.Reason;

            if (!session.Properties.TryGetProperty(CompletionSource.InitialTriggerKind, out CompletionTriggerKind initialRoslynTriggerKind))
            {
                initialRoslynTriggerKind = CompletionTriggerKind.Invoke;
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
                    // Dismiss the session.
                    return null;
                }
            }

            // We need to filter if a non-empty strict subset of filters are selected
            var selectedFilters = data.SelectedFilters.Where(f => f.IsSelected).Select(f => f.Filter).ToImmutableArray();
            var needToFilter = selectedFilters.Length > 0 && selectedFilters.Length < data.SelectedFilters.Length;

            if (session.TextView.Properties.TryGetProperty(CompletionSource.TargetTypeFilterExperimentEnabled, out bool isExperimentEnabled) && isExperimentEnabled)
            {
                // Telemetry: Want to know % of sessions with the "Target type matches" filter where that filter is actually enabled
                if (needToFilter &&
                    !session.Properties.ContainsProperty(_targetTypeCompletionFilterChosenMarker) &&
                    selectedFilters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches))
                {
                    AsyncCompletionLogger.LogTargetTypeFilterChosenInSession();

                    // Make sure we only record one enabling of the filter per session
                    session.Properties.AddProperty(_targetTypeCompletionFilterChosenMarker, _targetTypeCompletionFilterChosenMarker);
                }
            }

            var filterReason = Helpers.GetFilterReason(data.Trigger);

            // If the session was created/maintained out of Roslyn, e.g. in debugger; no properties are set and we should use data.Snapshot.
            // However, we prefer using the original snapshot in some projection scenarios.
            if (!session.Properties.TryGetProperty(CompletionSource.TriggerSnapshot, out ITextSnapshot snapshotForDocument))
            {
                snapshotForDocument = data.Snapshot;
            }

            var document = snapshotForDocument.TextBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            var completionService = document?.GetLanguageService<CompletionService>();
            var completionRules = completionService?.GetRules() ?? CompletionRules.Default;
            var completionHelper = document != null ? CompletionHelper.GetHelper(document) : _defaultCompletionHelper;

            var initialListOfItemsToBeIncluded = new List<ExtendedFilterResult>();
            foreach (var item in data.InitialSortedList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (needToFilter && ShouldBeFilteredOutOfCompletionList(item, selectedFilters))
                {
                    continue;
                }

                if (!item.Properties.TryGetProperty(CompletionSource.RoslynItem, out RoslynCompletionItem roslynItem))
                {
                    roslynItem = RoslynCompletionItem.Create(
                        displayText: item.DisplayText,
                        filterText: item.FilterText,
                        sortText: item.SortText,
                        displayTextSuffix: item.Suffix);
                }

                if (MatchesFilterText(completionHelper, roslynItem, filterText, initialRoslynTriggerKind, filterReason, _recentItemsManager.RecentItems))
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
                    if (initialRoslynTriggerKind == CompletionTriggerKind.Deletion ||
                        initialRoslynTriggerKind == CompletionTriggerKind.Invoke ||
                        filterText.Length <= 1)
                    {
                        initialListOfItemsToBeIncluded.Add(new ExtendedFilterResult(item, new FilterResult(roslynItem, filterText, matchedFilterText: false)));
                    }
                }
            }

            // DismissIfLastCharacterDeleted should be applied only when started with Insertion, and then Deleted all characters typed.
            // This confirms with the original VS 2010 behavior.
            if (initialRoslynTriggerKind == CompletionTriggerKind.Insertion &&
                data.Trigger.Reason == CompletionTriggerReason.Backspace &&
                completionRules.DismissIfLastCharacterDeleted &&
                session.ApplicableToSpan.GetText(data.Snapshot).Length == 0)
            {
                // Dismiss the session
                return null;
            }

            if (initialListOfItemsToBeIncluded.Count == 0)
            {
                return HandleAllItemsFilteredOut(reason, data.SelectedFilters, completionRules);
            }

            var options = document?.Project.Solution.Options;
            var highlightMatchingPortions = options?.GetOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, document.Project.Language) ?? true;
            var showCompletionItemFilters = options?.GetOption(CompletionOptions.ShowCompletionItemFilters, document.Project.Language) ?? true;

            var updatedFilters = showCompletionItemFilters
                ? GetUpdatedFilters(initialListOfItemsToBeIncluded, data.SelectedFilters)
                : ImmutableArray<CompletionFilterWithState>.Empty;

            var highlightedList = GetHighlightedList(initialListOfItemsToBeIncluded, filterText, completionHelper, highlightMatchingPortions).ToImmutableArray();

            // If this was deletion, then we control the entire behavior of deletion ourselves.
            if (initialRoslynTriggerKind == CompletionTriggerKind.Deletion)
            {
                return HandleDeletionTrigger(data.Trigger.Reason, initialListOfItemsToBeIncluded, filterText, updatedFilters, highlightedList);
            }

            Func<ImmutableArray<RoslynCompletionItem>, string, ImmutableArray<RoslynCompletionItem>> filterMethod;
            if (completionService == null)
            {
                filterMethod = (items, text) => CompletionService.FilterItems(completionHelper, items, text);
            }
            else
            {
                filterMethod = (items, text) => completionService.FilterItems(document, items, text);
            }

            return HandleNormalFiltering(
                filterMethod,
                filterText,
                updatedFilters,
                initialRoslynTriggerKind,
                filterReason,
                data.Trigger.Character,
                initialListOfItemsToBeIncluded,
                highlightedList,
                completionHelper,
                hasSuggestedItemOptions);
        }

        private static bool IsAfterDot(ITextSnapshot snapshot, ITrackingSpan applicableToSpan)
        {
            var position = applicableToSpan.GetStartPoint(snapshot).Position;
            return position > 0 && snapshot[position - 1] == '.';
        }

        private FilteredCompletionModel HandleNormalFiltering(
            Func<ImmutableArray<RoslynCompletionItem>, string, ImmutableArray<RoslynCompletionItem>> filterMethod,
            string filterText,
            ImmutableArray<CompletionFilterWithState> filters,
            CompletionTriggerKind initialRoslynTriggerKind,
            CompletionFilterReason filterReason,
            char typeChar,
            List<ExtendedFilterResult> itemsInList,
            ImmutableArray<CompletionItemWithHighlight> highlightedList,
            CompletionHelper completionHelper,
            bool hasSuggestedItemOptions)
        {
            // Not deletion.  Defer to the language to decide which item it thinks best
            // matches the text typed so far.

            // Ask the language to determine which of the *matched* items it wants to select.
            var matchingItems = itemsInList.Where(r => r.FilterResult.MatchedFilterText)
                                           .Select(t => t.FilterResult.CompletionItem)
                                           .AsImmutable();

            var chosenItems = filterMethod(matchingItems, filterText);

            var recentItems = _recentItemsManager.RecentItems;

            // Of the items the service returned, pick the one most recently committed
            var bestItem = GetBestCompletionItemBasedOnMRU(chosenItems, recentItems);
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
                var deduplicatedList = matchingItems.Where(r => !r.DisplayText.StartsWith("★"));
                if (selectedItemIndex > -1 &&
                    deduplicatedList.Count() == 1 &&
                    filterText.Length > 0)
                {
                    var uniqueItemIndex = itemsInList.IndexOf(i => Equals(i.FilterResult.CompletionItem, deduplicatedList.First()));
                    uniqueItem = highlightedList[uniqueItemIndex].CompletionItem;
                }
            }

            // If we don't have a best completion item yet, then pick the first item from the list.
            var bestOrFirstCompletionItem = bestItem ?? itemsInList.First().FilterResult.CompletionItem;

            // Check that it is a filter symbol. We can be called for a non-filter symbol.
            // If inserting a non-filter character (neither IsPotentialFilterCharacter, nor Helpers.IsFilterCharacter), we should dismiss completion  
            // except cases where this is the first symbol typed for the completion session (string.IsNullOrEmpty(filterText) or string.Equals(filterText, typeChar.ToString(), StringComparison.OrdinalIgnoreCase)).
            // In the latter case, we should keep the completion because it was confirmed just before in InitializeCompletion.
            if (filterReason == CompletionFilterReason.Insertion &&
                !string.IsNullOrEmpty(filterText) &&
                !string.Equals(filterText, typeChar.ToString(), StringComparison.OrdinalIgnoreCase) &&
                !IsPotentialFilterCharacter(typeChar) &&
                !Helpers.IsFilterCharacter(bestOrFirstCompletionItem, typeChar, filterText))
            {
                return null;
            }

            bool isHardSelection = IsHardSelection(
                filterText, initialRoslynTriggerKind, bestOrFirstCompletionItem,
                completionHelper, filterReason, recentItems, hasSuggestedItemOptions);

            var updateSelectionHint = isHardSelection ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected;

            // If no items found above, select the first item.
            if (selectedItemIndex == -1)
            {
                selectedItemIndex = 0;
            }

            return new FilteredCompletionModel(
                highlightedList, selectedItemIndex, filters,
                updateSelectionHint, centerSelection: true, uniqueItem);
        }

        private FilteredCompletionModel HandleDeletionTrigger(
            CompletionTriggerReason filterTriggerKind,
            List<ExtendedFilterResult> filterResults,
            string filterText,
            ImmutableArray<CompletionFilterWithState> filters,
            ImmutableArray<CompletionItemWithHighlight> highlightedList)
        {
            var matchingItems = filterResults.Where(r => r.FilterResult.MatchedFilterText).AsImmutable();
            if (filterTriggerKind == CompletionTriggerReason.Insertion &&
                !matchingItems.Any())
            {
                // The user has typed something, but nothing in the actual list matched what
                // they were typing.  In this case, we want to dismiss completion entirely.
                // The thought process is as follows: we aggressively brough up completion
                // to help them when they typed delete (in case they wanted to pick another
                // item).  However, they're typing something that doesn't seem to match at all
                // The completion list is just distracting at this point.
                return null;
            }

            ExtendedFilterResult? bestFilterResult = null;
            foreach (var currentFilterResult in matchingItems)
            {
                if (bestFilterResult == null ||
                    IsBetterDeletionMatch(currentFilterResult.FilterResult, bestFilterResult.Value.FilterResult))
                {
                    // We had no best result yet, so this is now our best result.
                    bestFilterResult = currentFilterResult;
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

            var deduplicatedListCount = matchingItems.Where(r => !r.VSCompletionItem.DisplayText.StartsWith("★")).Count();

            return new FilteredCompletionModel(
                highlightedList, index, filters,
                hardSelect ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected,
                centerSelection: true,
                uniqueItem: deduplicatedListCount == 1 ? bestFilterResult.GetValueOrDefault().VSCompletionItem : default);
        }

        private FilteredCompletionModel HandleAllItemsFilteredOut(
            CompletionTriggerReason triggerReason,
            ImmutableArray<CompletionFilterWithState> filters,
            CompletionRules completionRules)
        {
            if (triggerReason == CompletionTriggerReason.Insertion)
            {
                // If the user was just typing, and the list went to empty *and* this is a 
                // language that wants to dismiss on empty, then just return a null model
                // to stop the completion session.
                if (completionRules.DismissIfEmpty)
                {
                    return null;
                }
            }

            // If the user has turned on some filtering states, and we filtered down to
            // nothing, then we do want the UI to show that to them.  That way the user
            // can turn off filters they don't want and get the right set of items.

            // If we are going to filter everything out, then just preserve the existing
            // model (and all the previously filtered items), but switch over to soft
            // selection.
            var selection = UpdateSelectionHint.SoftSelected;

            return new FilteredCompletionModel(
                ImmutableArray<CompletionItemWithHighlight>.Empty, selectedItemIndex: 0,
                filters, selection, centerSelection: true, uniqueItem: default);
        }

        private static IEnumerable<CompletionItemWithHighlight> GetHighlightedList(
            IEnumerable<ExtendedFilterResult> filterResults,
            string filterText,
            CompletionHelper completionHelper,
            bool highlightMatchingPortions)
        {
            var highlightedList = new List<CompletionItemWithHighlight>();
            foreach (var item in filterResults)
            {
                var highlightedSpans = highlightMatchingPortions
                    ? completionHelper.GetHighlightedSpans(item.VSCompletionItem.DisplayText, filterText, CultureInfo.CurrentCulture)
                    : ImmutableArray<TextSpan>.Empty;
                highlightedList.Add(new CompletionItemWithHighlight(item.VSCompletionItem, highlightedSpans.Select(s => s.ToSpan()).ToImmutableArray()));
            }

            return highlightedList;
        }

        private static ImmutableArray<CompletionFilterWithState> GetUpdatedFilters(
            List<ExtendedFilterResult> filteredList,
            ImmutableArray<CompletionFilterWithState> filters)
        {
            // See which filters might be enabled based on the typed code
            var textFilteredFilters = filteredList.SelectMany(n => n.VSCompletionItem.Filters).ToImmutableHashSet();

            // When no items are available for a given filter, it becomes unavailable
            return ImmutableArray.CreateRange(filters.Select(n => n.WithAvailability(textFilteredFilters.Contains(n.Filter))));
        }

        private static bool ShouldBeFilteredOutOfCompletionList(VSCompletionItem item, ImmutableArray<CompletionFilter> activeFilters)
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

        /// <summary>
        /// Given multiple possible chosen completion items, pick the one that has the
        /// best MRU index.
        /// </summary>
        internal static RoslynCompletionItem GetBestCompletionItemBasedOnMRU(
            ImmutableArray<RoslynCompletionItem> chosenItems, ImmutableArray<string> recentItems)
        {
            if (chosenItems.Length == 0)
            {
                return null;
            }

            // Try to find the chosen item has been most
            // recently used.
            var bestItem = chosenItems.FirstOrDefault();
            for (int i = 0, n = chosenItems.Length; i < n; i++)
            {
                var chosenItem = chosenItems[i];
                var mruIndex1 = GetRecentItemIndex(recentItems, bestItem);
                var mruIndex2 = GetRecentItemIndex(recentItems, chosenItem);

                if (mruIndex2 < mruIndex1)
                {
                    bestItem = chosenItem;
                }
            }

            // If our best item appeared in the MRU list, use it
            if (GetRecentItemIndex(recentItems, bestItem) <= 0)
            {
                return bestItem;
            }

            // Otherwise use the chosen item that has the highest
            // matchPriority.
            for (int i = 1, n = chosenItems.Length; i < n; i++)
            {
                var chosenItem = chosenItems[i];
                var bestItemPriority = bestItem.Rules.MatchPriority;
                var currentItemPriority = chosenItem.Rules.MatchPriority;

                if (currentItemPriority > bestItemPriority)
                {
                    bestItem = chosenItem;
                }
            }

            return bestItem;
        }

        internal static int GetRecentItemIndex(ImmutableArray<string> recentItems, RoslynCompletionItem item)
        {
            var index = recentItems.IndexOf(item.DisplayText);
            return -index;
        }

        internal static bool IsBetterDeletionMatch(FilterResult result1, FilterResult result2)
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
                var item1Priority = item1.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? item1.Rules.MatchPriority : MatchPriority.Default;
                var item2Priority = item2.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? item2.Rules.MatchPriority : MatchPriority.Default;

                if (item1Priority > item2Priority)
                {
                    return true;
                }

                prefixLength1 = item1.FilterText.GetCaseSensitivePrefixLength(result1.FilterText);
                prefixLength2 = item2.FilterText.GetCaseSensitivePrefixLength(result2.FilterText);

                // If there are "Abc" vs "abc", we should prefer the case typed by user.
                if (prefixLength1 > prefixLength2)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool MatchesFilterText(
            CompletionHelper helper, RoslynCompletionItem item,
            string filterText, CompletionTriggerKind initialTriggerKind,
            CompletionFilterReason filterReason, ImmutableArray<string> recentItems)
        {
            // For the deletion we bake in the core logic for how matching should work.
            // This way deletion feels the same across all languages that opt into deletion 
            // as a completion trigger.

            // Specifically, to avoid being too aggressive when matching an item during 
            // completion, we require that the current filter text be a prefix of the 
            // item in the list.
            if (filterReason == CompletionFilterReason.Deletion &&
                initialTriggerKind == CompletionTriggerKind.Deletion)
            {
                return item.FilterText.GetCaseInsensitivePrefixLength(filterText) > 0;
            }

            // If the user hasn't typed anything, and this item was preselected, or was in the
            // MRU list, then we definitely want to include it.
            if (filterText.Length == 0)
            {
                if (item.Rules.MatchPriority > MatchPriority.Default)
                {
                    return true;
                }

                if (!recentItems.IsDefault && ItemManager.GetRecentItemIndex(recentItems, item) <= 0)
                {
                    return true;
                }
            }

            // Checks if the given completion item matches the pattern provided so far. 
            // A  completion item is checked against the pattern by see if it's 
            // CompletionItem.FilterText matches the item.  That way, the pattern it checked 
            // against terms like "IList" and not IList<>
            return helper.MatchesPattern(item.FilterText, filterText, CultureInfo.CurrentCulture);
        }


        internal static bool IsHardSelection(
            string fullFilterText,
            CompletionTriggerKind initialTriggerKind,
            RoslynCompletionItem bestFilterMatch,
            CompletionHelper completionHelper,
            CompletionFilterReason filterReason,
            ImmutableArray<string> recentItems,
            bool useSuggestionMode)
        {
            if (bestFilterMatch == null || useSuggestionMode)
            {
                return false;
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

            var shouldSoftSelect = ShouldSoftSelectItem(bestFilterMatch, fullFilterText);
            if (shouldSoftSelect)
            {
                return false;
            }

            // If the user moved the caret left after they started typing, the 'best' match may not match at all
            // against the full text span that this item would be replacing.
            if (!ItemManager.MatchesFilterText(completionHelper, bestFilterMatch, fullFilterText, initialTriggerKind, filterReason, recentItems))
            {
                return false;
            }

            // There was either filter text, or this was a preselect match.  In either case, we
            // can hard select this.
            return true;
        }

        /// <summary>
        /// Returns true if the completion item should be "soft" selected, or false if it should be "hard"
        /// selected.
        /// </summary>
        private static bool ShouldSoftSelectItem(RoslynCompletionItem item, string filterText)
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

        /// <summary>
        /// A potential filter character is something that can filter a completion lists and is
        /// *guaranteed* to not be a commit character.
        /// </summary>
        internal static bool IsPotentialFilterCharacter(char c)
        {
            // TODO(cyrusn): Actually use the right unicode categories here.
            return char.IsLetter(c)
                || char.IsNumber(c)
                || c == '_';
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
