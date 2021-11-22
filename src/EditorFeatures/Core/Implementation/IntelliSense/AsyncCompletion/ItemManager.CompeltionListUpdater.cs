// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
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
    internal partial class ItemManager
    {
        private sealed class CompletionListUpdater
        {

            public IAsyncCompletionSession Session { get; }
            public AsyncCompletionSessionDataSnapshot Data { get; }
            public RecentItemsManager RecentItemsManager { get; }
            public IGlobalOptionService GlobalOptions { get; }
            public CancellationToken CancellationToken { get; }
            public CompletionHelper DefaultCompletionHelper { get; }

            // We might need to handle large amount of items with import completion enabled,
            // so use a dedicated pool to minimize/avoid array allocations (especially in LOH)
            // Set the size of pool to 1 because we don't expect UpdateCompletionListAsync to be
            // called concurrently, which essentially makes the pooled list a singleton,
            // but we still use ObjectPool for concurrency handling just to be robust.
            private static readonly ObjectPool<List<MatchResult<VSCompletionItem>>> s_listOfMatchResultPool
                    = new(factory: () => new(), size: 1);

            public const string AggressiveDefaultsMatchingOptionName = "AggressiveDefaultsMatchingOption";

            /// <summary>
            /// For telemetry.
            /// </summary>
            private readonly object _targetTypeCompletionFilterChosenMarker = new();

            public CompletionListUpdater(
                IAsyncCompletionSession session,
                AsyncCompletionSessionDataSnapshot data,
                RecentItemsManager recentItemsManager,
                IGlobalOptionService globalOptions,
                CancellationToken cancellationToken)
            {
                Session = session;
                Data = data;
                RecentItemsManager = recentItemsManager;
                GlobalOptions = globalOptions;
                CancellationToken = cancellationToken;

                // Let us make the completion Helper used for non-Roslyn items case-sensitive.
                // We can change this if get requests from partner teams.
                DefaultCompletionHelper = new CompletionHelper(isCaseSensitive: true);
            }

            public FilteredCompletionModel? UpdateCompletionList()
            {
                if (!Session.Properties.TryGetProperty(CompletionSource.HasSuggestionItemOptions, out bool hasSuggestedItemOptions))
                {
                    // This is the scenario when the session is created out of Roslyn, in some other provider, e.g. in Debugger.
                    // For now, the default hasSuggestedItemOptions is false.
                    hasSuggestedItemOptions = false;
                }

                hasSuggestedItemOptions |= Data.DisplaySuggestionItem;

                var filterText = Session.ApplicableToSpan.GetText(Data.Snapshot);
                var reason = Data.Trigger.Reason;
                var initialRoslynTriggerKind = Helpers.GetRoslynTriggerKind(Data.InitialTrigger);

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
                    if (!IsAfterDot(Data.Snapshot, Session.ApplicableToSpan))
                    {
                        // Dismiss the session.
                        return null;
                    }
                }

                // We need to filter if 
                // 1. a non-empty strict subset of filters are selected
                // 2. a non-empty set of expanders are unselected
                var nonExpanderFilterStates = Data.SelectedFilters.WhereAsArray(f => f.Filter is not CompletionExpander);

                var selectedNonExpanderFilters = nonExpanderFilterStates.SelectAsArray(f => f.IsSelected, f => f.Filter);
                var needToFilter = selectedNonExpanderFilters.Length > 0 && selectedNonExpanderFilters.Length < nonExpanderFilterStates.Length;

                var unselectedExpanders = Data.SelectedFilters.SelectAsArray(f => !f.IsSelected && f.Filter is CompletionExpander, f => f.Filter);
                var needToFilterExpanded = unselectedExpanders.Length > 0;

                if (Session.TextView.Properties.TryGetProperty(CompletionSource.TargetTypeFilterExperimentEnabled, out bool isExperimentEnabled) && isExperimentEnabled)
                {
                    // Telemetry: Want to know % of sessions with the "Target type matches" filter where that filter is actually enabled
                    if (needToFilter &&
                        !Session.Properties.ContainsProperty(_targetTypeCompletionFilterChosenMarker) &&
                        selectedNonExpanderFilters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches))
                    {
                        AsyncCompletionLogger.LogTargetTypeFilterChosenInSession();

                        // Make sure we only record one enabling of the filter per session
                        Session.Properties.AddProperty(_targetTypeCompletionFilterChosenMarker, _targetTypeCompletionFilterChosenMarker);
                    }
                }

                var filterReason = Helpers.GetFilterReason(Data.Trigger);

                // We prefer using the original snapshot, which should always be available from items provided by Roslyn's CompletionSource.
                // Only use data.Snapshot in the theoretically possible but rare case when all items we are handling are from some non-Roslyn CompletionSource.
                var snapshotForDocument = TryGetInitialTriggerLocation(Data, out var intialTriggerLocation)
                    ? intialTriggerLocation.Snapshot
                    : Data.Snapshot;

                var document = snapshotForDocument?.TextBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                var completionService = document?.GetLanguageService<CompletionService>();
                var completionRules = completionService?.GetRules(CompletionOptions.From(document!.Project)) ?? CompletionRules.Default;
                var completionHelper = document != null ? CompletionHelper.GetHelper(document) : DefaultCompletionHelper;

                // DismissIfLastCharacterDeleted should be applied only when started with Insertion, and then Deleted all characters typed.
                // This conforms with the original VS 2010 behavior.
                if (initialRoslynTriggerKind == CompletionTriggerKind.Insertion &&
                    Data.Trigger.Reason == CompletionTriggerReason.Backspace &&
                    completionRules.DismissIfLastCharacterDeleted &&
                    Session.ApplicableToSpan.GetText(Data.Snapshot).Length == 0)
                {
                    // Dismiss the session
                    return null;
                }

                var highlightMatchingPortions = GlobalOptions.GetOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems, document?.Project.Language);
                // Nothing to highlight if user hasn't typed anything yet.
                highlightMatchingPortions = highlightMatchingPortions && filterText.Length > 0;

                // Use a monotonically increasing integer to keep track the original alphabetical order of each item.
                var currentIndex = 0;

                var initialListOfItemsToBeIncluded = s_listOfMatchResultPool.Allocate();
                try
                {
                    // Filter items based on the selected filters and matching.
                    foreach (var item in Data.InitialSortedList)
                    {
                        CancellationToken.ThrowIfCancellationRequested();

                        if (needToFilter && ShouldBeFilteredOutOfCompletionList(item, selectedNonExpanderFilters))
                        {
                            continue;
                        }

                        if (needToFilterExpanded && ShouldBeFilteredOutOfExpandedCompletionList(item, unselectedExpanders))
                        {
                            continue;
                        }

                        if (TryCreateMatchResult(
                            completionHelper,
                            item,
                            filterText,
                            initialRoslynTriggerKind,
                            filterReason,
                            RecentItemsManager.RecentItems,
                            highlightMatchingPortions: highlightMatchingPortions,
                            currentIndex,
                            out var matchResult))
                        {
                            initialListOfItemsToBeIncluded.Add(matchResult);
                            currentIndex++;
                        }
                    }

                    if (initialListOfItemsToBeIncluded.Count == 0)
                    {
                        return HandleAllItemsFilteredOut(reason, Data.SelectedFilters, completionRules);
                    }

                    // Sort the items by pattern matching results.
                    // Note that we want to preserve the original alphabetical order for items with same pattern match score,
                    // but `List<T>.Sort` isn't stable. Therefore we have to add a monotonically increasing integer
                    // to `MatchResult` to achieve this.
                    initialListOfItemsToBeIncluded.Sort(MatchResult<VSCompletionItem>.SortingComparer);

                    var filteringResult = initialRoslynTriggerKind == CompletionTriggerKind.Deletion
                            ? HandleDeletionTrigger(reason, initialListOfItemsToBeIncluded, filterText, hasSuggestedItemOptions)
                            : HandleNormalFiltering(GetFilterMethod(), filterText, filterReason, Data.Trigger.Character, initialListOfItemsToBeIncluded, hasSuggestedItemOptions);

                    if (!filteringResult.HasValue)
                        return null;

                    var (selectedItemIndex, selectionHint, uniqueItem) = filteringResult.Value;

                    // Editor is providing us a list of "default" items to consider for selection.
                    if (!Data.Defaults.IsDefaultOrEmpty)
                    {
                        var tick = Environment.TickCount;

                        var selectedItem = initialListOfItemsToBeIncluded[selectedItemIndex].RoslynCompletionItem;

                        // "Preselect" is only used when we have high confidence with the selection, so don't override it.
                        if (selectedItem.Rules.MatchPriority < MatchPriority.Preselect)
                        {
                            int defaultsMatchingIndex;
                            var useAggressiveDefaultsMatching = Session.TextView.Options.GetOptionValue<bool>(AggressiveDefaultsMatchingOptionName);

                            if (useAggressiveDefaultsMatching)
                            {
                                defaultsMatchingIndex = GetAggressiveDefaultsMatch(initialListOfItemsToBeIncluded, Data.Defaults);
                                if (!hasSuggestedItemOptions && defaultsMatchingIndex >= 0)
                                    selectionHint = UpdateSelectionHint.Selected;
                            }
                            else
                            {
                                defaultsMatchingIndex = GetDefaultsMatch(filterText, initialListOfItemsToBeIncluded, selectedItemIndex, Data.Defaults);
                            }

                            if (defaultsMatchingIndex >= 0)
                            {
                                selectedItemIndex = defaultsMatchingIndex;
                            }
                        }

                        AsyncCompletionLogger.LogGetDefaultsMatchTicksDataPoint(Environment.TickCount - tick);
                    }

                    var showCompletionItemFilters = GlobalOptions.GetOption(CompletionViewOptions.ShowCompletionItemFilters, document?.Project.Language);
                    var updatedFilters = showCompletionItemFilters
                        ? GetUpdatedFilters(initialListOfItemsToBeIncluded, Data.SelectedFilters)
                        : ImmutableArray<CompletionFilterWithState>.Empty;

                    return new FilteredCompletionModel(
                        items: GetHighlightedList(initialListOfItemsToBeIncluded, filterText, highlightMatchingPortions, completionHelper),
                        selectedItemIndex,
                        updatedFilters,
                        selectionHint,
                        centerSelection: true,
                        uniqueItem);
                }
                finally
                {
                    // Don't call ClearAndFree, which resets the capacity to a default value.
                    initialListOfItemsToBeIncluded.Clear();
                    s_listOfMatchResultPool.Free(initialListOfItemsToBeIncluded);
                }

                Func<ImmutableArray<(RoslynCompletionItem, PatternMatch?)>, string, ImmutableArray<RoslynCompletionItem>> GetFilterMethod()
                {
                    if (completionService == null)
                    {
                        return (itemsWithPatternMatches, text) => CompletionService.FilterItems(completionHelper, itemsWithPatternMatches, text);
                    }
                    else
                    {
                        Contract.ThrowIfNull(document);
                        return (itemsWithPatternMatches, text) => completionService.FilterItems(document, itemsWithPatternMatches, text);
                    }
                }

                static bool TryGetInitialTriggerLocation(AsyncCompletionSessionDataSnapshot data, out SnapshotPoint intialTriggerLocation)
                {
                    var firstItem = data.InitialSortedList.FirstOrDefault(static item => item.Properties.ContainsProperty(CompletionSource.TriggerLocation));
                    if (firstItem != null)
                    {
                        return firstItem.Properties.TryGetProperty(CompletionSource.TriggerLocation, out intialTriggerLocation);
                    }

                    intialTriggerLocation = default;
                    return false;
                }

                static bool ShouldBeFilteredOutOfCompletionList(VSCompletionItem item, ImmutableArray<CompletionFilter> activeNonExpanderFilters)
                {
                    if (item.Filters.Any(filter => activeNonExpanderFilters.Contains(filter)))
                    {
                        return false;
                    }

                    return true;
                }

                static bool ShouldBeFilteredOutOfExpandedCompletionList(VSCompletionItem item, ImmutableArray<CompletionFilter> unselectedExpanders)
                {
                    var associatedWithUnselectedExpander = false;
                    foreach (var itemFilter in item.Filters)
                    {
                        if (itemFilter is CompletionExpander)
                        {
                            if (!unselectedExpanders.Contains(itemFilter))
                            {
                                // If any of the associated expander is selected, the item should be included in the expanded list.
                                return false;
                            }

                            associatedWithUnselectedExpander = true;
                        }
                    }

                    // at this point, the item either:
                    // 1. has no expander filter, therefore should be included
                    // 2. or, all associated expanders are unselected, therefore should be excluded
                    return associatedWithUnselectedExpander;
                }
            }

            private static bool IsAfterDot(ITextSnapshot snapshot, ITrackingSpan applicableToSpan)
            {
                var position = applicableToSpan.GetStartPoint(snapshot).Position;
                return position > 0 && snapshot[position - 1] == '.';
            }

            private (int selectedItemIndex, UpdateSelectionHint selectionHint, VSCompletionItem? uniqueItem)? HandleNormalFiltering(
                Func<ImmutableArray<(RoslynCompletionItem, PatternMatch?)>, string, ImmutableArray<RoslynCompletionItem>> filterMethod,
                string filterText,
                CompletionFilterReason filterReason,
                char typeChar,
                List<MatchResult<VSCompletionItem>> itemsInList,
                bool hasSuggestedItemOptions)
            {
                // Not deletion.  Defer to the language to decide which item it thinks best
                // matches the text typed so far.

                // Ask the language to determine which of the *matched* items it wants to select.
                var matchingItems = itemsInList.Where(r => r.MatchedFilterText)
                                               .SelectAsArray(t => (t.RoslynCompletionItem, t.PatternMatch));

                var chosenItems = filterMethod(matchingItems, filterText);

                int selectedItemIndex;
                VSCompletionItem? uniqueItem = null;
                MatchResult<VSCompletionItem> bestOrFirstMatchResult;

                if (chosenItems.Length == 0)
                {
                    // We do not have matches: pick the one with longest common prefix or the first item from the list.
                    selectedItemIndex = 0;
                    bestOrFirstMatchResult = itemsInList[0];

                    var longestCommonPrefixLength = bestOrFirstMatchResult.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(filterText);

                    for (var i = 1; i < itemsInList.Count; ++i)
                    {
                        var item = itemsInList[i];
                        var commonPrefixLength = item.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(filterText);

                        if (commonPrefixLength > longestCommonPrefixLength)
                        {
                            selectedItemIndex = i;
                            bestOrFirstMatchResult = item;
                            longestCommonPrefixLength = commonPrefixLength;
                        }
                    }
                }
                else
                {
                    var recentItems = RecentItemsManager.RecentItems;

                    // Of the items the service returned, pick the one most recently committed
                    var bestItem = GetBestCompletionItemBasedOnMRU(chosenItems, recentItems);

                    // Determine if we should consider this item 'unique' or not.  A unique item
                    // will be automatically committed if the user hits the 'invoke completion' 
                    // without bringing up the completion list.  An item is unique if it was the
                    // only item to match the text typed so far, and there was at least some text
                    // typed.  i.e.  if we have "Console.$$" we don't want to commit something
                    // like "WriteLine" since no filter text has actually been provided.  However,
                    // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
                    selectedItemIndex = itemsInList.IndexOf(i => Equals(i.RoslynCompletionItem, bestItem));
                    bestOrFirstMatchResult = itemsInList[selectedItemIndex];
                    var deduplicatedListCount = matchingItems.Count(r => !r.RoslynCompletionItem.IsPreferredItem());
                    if (deduplicatedListCount == 1 &&
                        filterText.Length > 0)
                    {
                        uniqueItem = itemsInList[selectedItemIndex].EditorCompletionItem;
                    }
                }

                // Check that it is a filter symbol. We can be called for a non-filter symbol.
                // If inserting a non-filter character (neither IsPotentialFilterCharacter, nor Helpers.IsFilterCharacter), we should dismiss completion  
                // except cases where this is the first symbol typed for the completion session (string.IsNullOrEmpty(filterText) or string.Equals(filterText, typeChar.ToString(), StringComparison.OrdinalIgnoreCase)).
                // In the latter case, we should keep the completion because it was confirmed just before in InitializeCompletion.
                if (filterReason == CompletionFilterReason.Insertion &&
                    !string.IsNullOrEmpty(filterText) &&
                    !string.Equals(filterText, typeChar.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    !IsPotentialFilterCharacter(typeChar) &&
                    !Helpers.IsFilterCharacter(bestOrFirstMatchResult.RoslynCompletionItem, typeChar, filterText))
                {
                    return null;
                }

                var isHardSelection = IsHardSelection(
                    filterText, bestOrFirstMatchResult.RoslynCompletionItem, bestOrFirstMatchResult.MatchedFilterText, hasSuggestedItemOptions);

                var updateSelectionHint = isHardSelection ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected;

                return (selectedItemIndex, updateSelectionHint, uniqueItem);
            }

            private static (int selectedItemIndex, UpdateSelectionHint selectionHint, VSCompletionItem? uniqueItem)? HandleDeletionTrigger(
                CompletionTriggerReason filterTriggerKind,
                List<MatchResult<VSCompletionItem>> matchResults,
                string filterText,
                bool hasSuggestedItemOptions)
            {
                var matchingItems = matchResults.Where(r => r.MatchedFilterText);
                if (filterTriggerKind == CompletionTriggerReason.Insertion &&
                    !matchingItems.Any())
                {
                    // The user has typed something, but nothing in the actual list matched what
                    // they were typing.  In this case, we want to dismiss completion entirely.
                    // The thought process is as follows: we aggressively brought up completion
                    // to help them when they typed delete (in case they wanted to pick another
                    // item).  However, they're typing something that doesn't seem to match at all
                    // The completion list is just distracting at this point.
                    return null;
                }

                MatchResult<VSCompletionItem>? bestMatchResult = null;
                var moreThanOneMatchWithSamePriority = false;
                foreach (var currentMatchResult in matchingItems)
                {
                    if (bestMatchResult == null)
                    {
                        // We had no best result yet, so this is now our best result.
                        bestMatchResult = currentMatchResult;
                    }
                    else
                    {
                        var match = currentMatchResult.CompareTo(bestMatchResult.Value, filterText);
                        if (match > 0)
                        {
                            moreThanOneMatchWithSamePriority = false;
                            bestMatchResult = currentMatchResult;
                        }
                        else if (match == 0)
                        {
                            moreThanOneMatchWithSamePriority = true;
                        }
                    }
                }

                int index;
                bool hardSelect;

                // If we had a matching item, then pick the best of the matching items and
                // choose that one to be hard selected.  If we had no actual matching items
                // (which can happen if the user deletes down to a single character and we
                // include everything), then we just soft select the first item.
                if (bestMatchResult != null)
                {
                    // Only hard select this result if it's a prefix match
                    // We need to do this so that
                    // * deleting and retyping a dot in a member access does not change the
                    //   text that originally appeared before the dot
                    // * deleting through a word from the end keeps that word selected
                    // This also preserves the behavior the VB had through Dev12.
                    hardSelect = !hasSuggestedItemOptions && bestMatchResult.Value.EditorCompletionItem.FilterText.StartsWith(filterText, StringComparison.CurrentCultureIgnoreCase);
                    index = matchResults.IndexOf(bestMatchResult.Value);
                }
                else
                {
                    index = 0;
                    hardSelect = false;
                }

                return (index,
                    hardSelect ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected,
                    moreThanOneMatchWithSamePriority ? null : bestMatchResult.GetValueOrDefault().EditorCompletionItem);
            }

            private static ImmutableArray<CompletionItemWithHighlight> GetHighlightedList(
                List<MatchResult<VSCompletionItem>> matchResults,
                string filterText,
                bool highlightMatchingPortions,
                CompletionHelper completionHelper)
            {
                return matchResults.SelectAsArray(matchResult =>
                {
                    var highlightedSpans = GetHighlightedSpans(matchResult, completionHelper, filterText, highlightMatchingPortions);
                    return new CompletionItemWithHighlight(matchResult.EditorCompletionItem, highlightedSpans);
                });

                static ImmutableArray<Span> GetHighlightedSpans(
                    MatchResult<VSCompletionItem> matchResult,
                    CompletionHelper completionHelper,
                    string filterText,
                    bool highlightMatchingPortions)
                {
                    if (highlightMatchingPortions)
                    {
                        if (matchResult.RoslynCompletionItem.HasDifferentFilterText)
                        {
                            // The PatternMatch in MatchResult is calculated based on Roslyn item's FilterText, 
                            // which can be used to calculate highlighted span for VSCompletion item's DisplayText w/o doing the matching again.
                            // However, if the Roslyn item's FilterText is different from its DisplayText,
                            // we need to do the match against the display text of the VS item directly to get the highlighted spans.
                            return completionHelper.GetHighlightedSpans(
                                matchResult.EditorCompletionItem.DisplayText, filterText, CultureInfo.CurrentCulture).SelectAsArray(s => s.ToSpan());
                        }

                        var patternMatch = matchResult.PatternMatch;
                        if (patternMatch.HasValue)
                        {
                            // Since VS item's display text is created as Prefix + DisplayText + Suffix, 
                            // we can calculate the highlighted span by adding an offset that is the length of the Prefix.
                            return patternMatch.Value.MatchedSpans.SelectAsArray(s_highlightSpanGetter, matchResult.RoslynCompletionItem);
                        }
                    }

                    // If there's no match for Roslyn item's filter text which is identical to its display text,
                    // then we can safely assume there'd be no matching to VS item's display text.
                    return ImmutableArray<Span>.Empty;
                }
            }

            private static FilteredCompletionModel? HandleAllItemsFilteredOut(
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
                    filters, selection, centerSelection: true, uniqueItem: null);
            }

            private static ImmutableArray<CompletionFilterWithState> GetUpdatedFilters(
                List<MatchResult<VSCompletionItem>> filteredList,
                ImmutableArray<CompletionFilterWithState> filters)
            {
                // See which filters might be enabled based on the typed code
                using var _ = PooledHashSet<CompletionFilter>.GetInstance(out var textFilteredFilters);
                textFilteredFilters.AddRange(filteredList.SelectMany(n => n.EditorCompletionItem.Filters));

                // When no items are available for a given filter, it becomes unavailable.
                // Expanders always appear available as long as it's presented.
                return filters.SelectAsArray(n => n.WithAvailability(n.Filter is CompletionExpander ? true : textFilteredFilters.Contains(n.Filter)));
            }

            /// <summary>
            /// Given multiple possible chosen completion items, pick the one that has the
            /// best MRU index, or the one with highest MatchPriority if none in MRU.
            /// </summary>
            private static RoslynCompletionItem GetBestCompletionItemBasedOnMRU(
                ImmutableArray<RoslynCompletionItem> chosenItems, ImmutableArray<string> recentItems)
            {
                Debug.Assert(chosenItems.Length > 0);

                // Try to find the chosen item has been most recently used.
                var bestItem = chosenItems[0];
                for (int i = 1, n = chosenItems.Length; i < n; i++)
                {
                    var chosenItem = chosenItems[i];
                    var mruIndex1 = GetRecentItemIndex(recentItems, bestItem);
                    var mruIndex2 = GetRecentItemIndex(recentItems, chosenItem);

                    if ((mruIndex2 < mruIndex1) ||
                        (mruIndex2 == mruIndex1 && !bestItem.IsPreferredItem() && chosenItem.IsPreferredItem()))
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

                    if ((currentItemPriority > bestItemPriority) ||
                        ((currentItemPriority == bestItemPriority) && !bestItem.IsPreferredItem() && chosenItem.IsPreferredItem()))
                    {
                        bestItem = chosenItem;
                    }
                }

                return bestItem;
            }

            private static int GetRecentItemIndex(ImmutableArray<string> recentItems, RoslynCompletionItem item)
            {
                var index = recentItems.IndexOf(item.FilterText);
                return -index;
            }

            private static bool TryCreateMatchResult(
                CompletionHelper completionHelper,
                VSCompletionItem item,
                string filterText,
                CompletionTriggerKind initialTriggerKind,
                CompletionFilterReason filterReason,
                ImmutableArray<string> recentItems,
                bool highlightMatchingPortions,
                int currentIndex,
                out MatchResult<VSCompletionItem> matchResult)
            {
                var roslynItem = GetOrAddRoslynCompletionItem(item);
                return CompletionHelper.TryCreateMatchResult(completionHelper, roslynItem, item, filterText, initialTriggerKind, filterReason, recentItems, highlightMatchingPortions, currentIndex, out matchResult);
            }

            // PERF: Create a singleton to avoid lambda allocation on hot path
            private static readonly Func<TextSpan, RoslynCompletionItem, Span> s_highlightSpanGetter
                = (span, item) => span.MoveTo(item.DisplayTextPrefix?.Length ?? 0).ToSpan();

            private static bool IsHardSelection(
                string filterText,
                RoslynCompletionItem item,
                bool matchedFilterText,
                bool useSuggestionMode)
            {
                if (item == null || useSuggestionMode)
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

                // If all that has been typed is punctuation, then don't hard select anything.
                // It's possible the user is just typing language punctuation and selecting
                // anything in the list will interfere.  We only allow this if the filter text
                // exactly matches something in the list already. 
                if (filterText.Length > 0 && IsAllPunctuation(filterText) && filterText != item.DisplayText)
                {
                    return false;
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
                        return false;
                    }

                    // Item did not ask to be preselected.  So definitely soft select it.
                    if (item.Rules.MatchPriority == MatchPriority.Default)
                    {
                        return false;
                    }
                }

                // The user typed something, or the item asked to be preselected.  In 
                // either case, don't soft select this.
                Debug.Assert(filterText.Length > 0 || item.Rules.MatchPriority != MatchPriority.Default);

                // If the user moved the caret left after they started typing, the 'best' match may not match at all
                // against the full text span that this item would be replacing.
                if (!matchedFilterText)
                {
                    return false;
                }

                // There was either filter text, or this was a preselect match.  In either case, we
                // can hard select this.
                return true;
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
            private static bool IsPotentialFilterCharacter(char c)
            {
                // TODO(cyrusn): Actually use the right Unicode categories here.
                return char.IsLetter(c)
                    || char.IsNumber(c)
                    || c == '_';
            }

            private static int GetAggressiveDefaultsMatch(List<MatchResult<VSCompletionItem>> itemsWithMatch, ImmutableArray<string> defaults)
            {
                Debug.Assert(!defaults.IsDefaultOrEmpty);

                foreach (var defaultText in defaults)
                {
                    for (var i = 0; (i < itemsWithMatch.Count); ++i)
                    {
                        var itemWithMatch = itemsWithMatch[i];
                        if (itemWithMatch.RoslynCompletionItem.FilterText == defaultText)
                        {
                            if (itemWithMatch.PatternMatch == null || itemWithMatch.PatternMatch.Value.Kind <= PatternMatchKind.Prefix)
                                return i;

                            break;
                        }
                    }
                }

                return -1;
            }

            private static int GetDefaultsMatch(
                string filterText,
                List<MatchResult<VSCompletionItem>> itemsWithMatch,
                int selectedIndex,
                ImmutableArray<string> defaults)
            {
                Debug.Assert(!defaults.IsDefaultOrEmpty);

                int inferiorItemIndex;
                if (filterText.Length == 0)
                {
                    // Without filterText, all items are eually good match, so we have to consider all of them.
                    inferiorItemIndex = itemsWithMatch.Count;
                }
                else
                {
                    // Because the items are sorted based on pattern-matching score, the selectedIndex is in the middle of a range of
                    // -- as far as the pattern matcher is concerned -- equivalent items. Find the last items in the range and use that
                    // to limit the items searched for from the defaults list.          
                    var selectedItemMatch = itemsWithMatch[selectedIndex].PatternMatch;

                    if (!selectedItemMatch.HasValue)
                        return -1;

                    inferiorItemIndex = selectedIndex;
                    while (++inferiorItemIndex < itemsWithMatch.Count)
                    {
                        // Ignore the case when trying to match the filter text with defaults.
                        // e.g. a default "Console" would be a match for filter text "c" and therefore to be selected,
                        // even if the CompletionService returns item "char" which is a case-sensitive prefix match.
                        var itemMatch = itemsWithMatch[inferiorItemIndex].PatternMatch;
                        if (!itemMatch.HasValue || itemMatch.Value.Kind != selectedItemMatch.Value.Kind)
                            break;
                    }
                }

                foreach (var defaultText in defaults)
                {
                    for (var i = 0; i < inferiorItemIndex; ++i)
                    {
                        if (itemsWithMatch[i].RoslynCompletionItem.FilterText == defaultText)
                            return i;
                    }
                }

                return -1;
            }

        }
    }
}
