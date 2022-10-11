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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
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
        /// <summary>
        /// Handles the filtering, sorting and selection of the completion items based on user inputs 
        /// (e.g. typed characters, selected filters, etc.)
        /// </summary>
        private sealed class CompletionListUpdater
        {
            // Index used for selecting suggestion item when in suggestion mode.
            private const int SuggestionItemIndex = -1;

            private readonly CompletionSessionData _sessionData;
            private readonly AsyncCompletionSessionDataSnapshot _snapshotData;
            private readonly RecentItemsManager _recentItemsManager;

            private readonly ITrackingSpan _applicableToSpan;
            private readonly bool _hasSuggestedItemOptions;
            private readonly string _filterText;
            private readonly Document? _document;
            private readonly CompletionService? _completionService;
            private readonly CompletionRules _completionRules;
            private readonly CompletionHelper _completionHelper;
            private readonly bool _highlightMatchingPortions;
            private readonly bool _showCompletionItemFilters;

            private readonly Action<IReadOnlyList<MatchResult>, string, IList<MatchResult>> _filterMethod;

            private bool ShouldSelectSuggestionItemWhenNoItemMatchesFilterText
                => _snapshotData.DisplaySuggestionItem && _filterText.Length > 0;

            private CompletionTriggerReason InitialTriggerReason => _snapshotData.InitialTrigger.Reason;
            private CompletionTriggerReason UpdateTriggerReason => _snapshotData.Trigger.Reason;

            // We might need to handle large amount of items with import completion enabled, so use a dedicated pool to minimize/avoid array allocations
            // (especially in LOH). In practice, the size of pool should be 1 because we don't expect UpdateCompletionListAsync to be called concurrently,
            // which essentially makes the pooled list a singleton, but we still use ObjectPool for concurrency handling just to be robust.
            private static readonly ObjectPool<List<MatchResult>> s_listOfMatchResultPool = new(factory: () => new());

            public CompletionListUpdater(
                ITrackingSpan applicableToSpan,
                CompletionSessionData sessionData,
                AsyncCompletionSessionDataSnapshot snapshotData,
                RecentItemsManager recentItemsManager,
                IGlobalOptionService globalOptions)
            {
                _sessionData = sessionData;
                _snapshotData = snapshotData;
                _recentItemsManager = recentItemsManager;

                _applicableToSpan = applicableToSpan;
                _filterText = applicableToSpan.GetText(_snapshotData.Snapshot);

                _hasSuggestedItemOptions = _sessionData.HasSuggestionItemOptions || _snapshotData.DisplaySuggestionItem;

                // We prefer using the original snapshot, which should always be available from items provided by Roslyn's CompletionSource.
                // Only use data.Snapshot in the theoretically possible but rare case when all items we are handling are from some non-Roslyn CompletionSource.
                var snapshotForDocument = TryGetInitialTriggerLocation(_snapshotData, out var intialTriggerLocation)
                    ? intialTriggerLocation.Snapshot
                    : _snapshotData.Snapshot;

                _document = snapshotForDocument?.TextBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
                if (_document != null)
                {
                    _completionService = _document.GetLanguageService<CompletionService>();
                    _completionRules = _completionService?.GetRules(globalOptions.GetCompletionOptions(_document.Project.Language)) ?? CompletionRules.Default;

                    // Let us make the completion Helper used for non-Roslyn items case-sensitive.
                    // We can change this if get requests from partner teams.
                    _completionHelper = CompletionHelper.GetHelper(_document);
                    _filterMethod = _completionService == null
                        ? ((matchResults, text, filtereditemsBuilder) => CompletionService.FilterItems(_completionHelper, matchResults, text, filtereditemsBuilder))
                        : ((matchResults, text, filtereditemsBuilder) => _completionService.FilterItems(_document, matchResults, text, filtereditemsBuilder));

                    // Nothing to highlight if user hasn't typed anything yet.
                    _highlightMatchingPortions = _filterText.Length > 0
                        && globalOptions.GetOption(CompletionViewOptions.HighlightMatchingPortionsOfCompletionListItems, _document.Project.Language);

                    _showCompletionItemFilters = globalOptions.GetOption(CompletionViewOptions.ShowCompletionItemFilters, _document.Project.Language);
                }
                else
                {
                    _completionService = null;
                    _completionRules = CompletionRules.Default;

                    // Let us make the completion Helper used for non-Roslyn items case-sensitive.
                    // We can change this if get requests from partner teams.
                    _completionHelper = new CompletionHelper(isCaseSensitive: true);
                    _filterMethod = (matchResults, text, filteredMatchResultsBuilder) => CompletionService.FilterItems(_completionHelper, matchResults, text, filteredMatchResultsBuilder);

                    _highlightMatchingPortions = false;
                    _showCompletionItemFilters = true;
                }
            }

            public async Task<FilteredCompletionModel?> UpdateCompletionListAsync(IAsyncCompletionSession session, CancellationToken cancellationToken)
            {
                if (ShouldDismissCompletionListImmediately())
                    return null;

                // Use a dedicated pool to minimize potentially repeated large allocations,
                // since the completion list could be long with import completion enabled.
                var itemsToBeIncluded = s_listOfMatchResultPool.Allocate();
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    // Determine the list of items to be included in the completion list.
                    // This is computed based on the filter text as well as the current
                    // selection of filters and expander.
                    AddCompletionItems(itemsToBeIncluded, cancellationToken);

                    // Decide if we want to dismiss an empty completion list based on CompletionRules and filter usage.
                    if (itemsToBeIncluded.Count == 0)
                        return HandleAllItemsFilteredOut();

                    var highlightAndFilterTask = Task.Run(
                        () => GetHighlightedListAndUpdatedFilters(session, itemsToBeIncluded, cancellationTokenSource.Token),
                        cancellationTokenSource.Token);

                    // Decide the item to be selected for this completion session.
                    // The selection is mostly based on how well the item matches with the filter text, but we also need to
                    // take into consideration for things like CompletionTrigger, MatchPriority, MRU, etc. 
                    var initialSelection = InitialTriggerReason == CompletionTriggerReason.Backspace || InitialTriggerReason == CompletionTriggerReason.Deletion
                        ? HandleDeletionTrigger(itemsToBeIncluded, cancellationToken)
                        : HandleNormalFiltering(itemsToBeIncluded, cancellationToken);

                    if (!initialSelection.HasValue)
                        return null;

                    // Editor might provide a list of items to us as a suggestion to what to select for this session
                    // (via IAsyncCompletionDefaultsSource), where the "default" means the "default selection".
                    // The main scenario for this is to keep the selected item in completion list in sync with the
                    // suggestion of "Whole-Line Completion" feature, where the default is usually set to the first token
                    // of the WLC suggestion.
                    var finalSelection = UpdateSelectionBasedOnSuggestedDefaults(itemsToBeIncluded, initialSelection.Value, cancellationToken);
                    var (highlightedList, updatedFilters) = await highlightAndFilterTask.ConfigureAwait(false);

                    return new FilteredCompletionModel(
                        items: highlightedList,
                        finalSelection.SelectedItemIndex,
                        filters: updatedFilters,
                        finalSelection.SelectionHint,
                        centerSelection: true,
                        finalSelection.UniqueItem);
                }
                finally
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();

                    // Don't call ClearAndFree, which resets the capacity to a default value.
                    itemsToBeIncluded.Clear();
                    s_listOfMatchResultPool.Free(itemsToBeIncluded);
                }

                (CompletionList<CompletionItemWithHighlight>, ImmutableArray<CompletionFilterWithState>) GetHighlightedListAndUpdatedFilters(
                    IAsyncCompletionSession session, IReadOnlyList<MatchResult> itemsToBeIncluded, CancellationToken cancellationToken)
                {
                    var highLightedList = GetHighlightedList(session, itemsToBeIncluded, cancellationToken);
                    var updatedFilters = GetUpdatedFilters(itemsToBeIncluded, cancellationToken);
                    return (highLightedList, updatedFilters);
                }
            }

            private bool ShouldDismissCompletionListImmediately()
            {
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
                if (_filterText.Length > 0 && char.IsNumber(_filterText[0]) && !IsAfterDot(_snapshotData.Snapshot, _applicableToSpan))
                {
                    // Dismiss the session.
                    return true;
                }

                // DismissIfLastCharacterDeleted should be applied only when started with Insertion, and then Deleted all characters typed.
                // This conforms with the original VS 2010 behavior.
                if (InitialTriggerReason == CompletionTriggerReason.Insertion &&
                    UpdateTriggerReason == CompletionTriggerReason.Backspace &&
                    _completionRules.DismissIfLastCharacterDeleted &&
                    _filterText.Length == 0)
                {
                    // Dismiss the session
                    return true;
                }

                return false;

                static bool IsAfterDot(ITextSnapshot snapshot, ITrackingSpan applicableToSpan)
                {
                    var position = applicableToSpan.GetStartPoint(snapshot).Position;
                    return position > 0 && snapshot[position - 1] == '.';
                }
            }

            private void AddCompletionItems(List<MatchResult> list, CancellationToken cancellationToken)
            {
                // Convert initial and update trigger reasons to corresponding Roslyn type so 
                // we can interact with Roslyn's completion system
                var roslynInitialTriggerKind = Helpers.GetRoslynTriggerKind(InitialTriggerReason);
                var roslynFilterReason = Helpers.GetFilterReason(UpdateTriggerReason);

                // FilterStateHelper is used to decide whether a given item should be included in the list based on the state of filter/expander buttons.
                var filterHelper = new FilterStateHelper(_snapshotData.SelectedFilters);

                // Filter items based on the selected filters and matching.
                var totalCount = _snapshotData.InitialSortedItemList.Count;
                for (var currentIndex = 0; currentIndex < totalCount; currentIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var item = _snapshotData.InitialSortedItemList[currentIndex];

                    if (filterHelper.ShouldBeFilteredOut(item))
                        continue;

                    if (CompletionItemData.TryGetData(item, out var itemData))
                    {
                        // currentIndex is used to track the index of the VS CompletionItem in the intial sorted list to maintain a map from Roslyn itemt o VS item.
                        // It's also used to sort the items by pattern matching results while preserving the original alphabetical order for items with
                        // same pattern match score since `List<T>.Sort` isn't stable.
                        if (CompletionHelper.TryCreateMatchResult(_completionHelper, itemData.RoslynItem, _filterText,
                            roslynInitialTriggerKind, roslynFilterReason, _recentItemsManager.GetRecentItemIndex(itemData.RoslynItem), _highlightMatchingPortions, currentIndex,
                            out var matchResult))
                        {
                            list.Add(matchResult);
                        }
                    }
                    else
                    {
                        // All items passed in should contain a CompletionItemData object in the property bag,
                        // which is guaranteed in `ItemManager.SortCompletionListAsync`.
                        throw ExceptionUtilities.Unreachable();
                    }
                }

                list.Sort(MatchResult.SortingComparer);
            }

            private ItemSelection? HandleNormalFiltering(IReadOnlyList<MatchResult> matchResults, CancellationToken cancellationToken)
            {
                Debug.Assert(matchResults.Count > 0);
                var filteredMatchResultsBuilder = s_listOfMatchResultPool.Allocate();

                try
                {
                    // Not deletion.  Defer to the language to decide which item it thinks best
                    // matches the text typed so far.
                    _filterMethod(matchResults, _filterText, filteredMatchResultsBuilder);

                    // Ask the language to determine which of the *matched* items it wants to select.
                    int selectedItemIndex;
                    VSCompletionItem? uniqueItem = null;
                    MatchResult bestOrFirstMatchResult;
                    if (filteredMatchResultsBuilder.Count == 0)
                    {
                        // When we are in suggestion mode and there's nothing in the list matches what user has typed in any ways,
                        // we should select the SuggestionItem instead.
                        if (ShouldSelectSuggestionItemWhenNoItemMatchesFilterText)
                            return new ItemSelection(SelectedItemIndex: SuggestionItemIndex, SelectionHint: UpdateSelectionHint.SoftSelected, UniqueItem: null);

                        // We do not have matches: pick the one with longest common prefix.
                        // If we can't find such an item, just return the first item from the list.
                        selectedItemIndex = 0;
                        bestOrFirstMatchResult = matchResults[0];

                        var longestCommonPrefixLength = bestOrFirstMatchResult.FilterTextUsed.GetCaseInsensitivePrefixLength(_filterText);

                        for (var i = 1; i < matchResults.Count; ++i)
                        {
                            var matchResult = matchResults[i];
                            var commonPrefixLength = matchResult.FilterTextUsed.GetCaseInsensitivePrefixLength(_filterText);

                            if (commonPrefixLength > longestCommonPrefixLength)
                            {
                                selectedItemIndex = i;
                                bestOrFirstMatchResult = matchResult;
                                longestCommonPrefixLength = commonPrefixLength;
                            }
                        }
                    }
                    else
                    {
                        // Of the items the service returned, pick the one most recently committed
                        var bestResult = GetBestCompletionItemSelectionFromFilteredResults(filteredMatchResultsBuilder);

                        // Determine if we should consider this item 'unique' or not.  A unique item
                        // will be automatically committed if the user hits the 'invoke completion' 
                        // without bringing up the completion list.  An item is unique if it was the
                        // only item to match the text typed so far, and there was at least some text
                        // typed.  i.e.  if we have "Console.$$" we don't want to commit something
                        // like "WriteLine" since no filter text has actually been provided.  However,
                        // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
                        for (selectedItemIndex = 0; selectedItemIndex < matchResults.Count; ++selectedItemIndex)
                        {
                            if (Equals(matchResults[selectedItemIndex].CompletionItem, bestResult.CompletionItem))
                                break;
                        }

                        Debug.Assert(selectedItemIndex < matchResults.Count);

                        bestOrFirstMatchResult = matchResults[selectedItemIndex];

                        if (_filterText.Length > 0)
                        {
                            // PreferredItems from IntelliCode are duplicate of normal items, so we ignore them
                            // when deciding if we have an unique item.
                            if (matchResults.Count(matchResult => matchResult.ShouldBeConsideredMatchingFilterText && !matchResult.CompletionItem.IsPreferredItem()) == 1)
                                uniqueItem = GetCorrespondingVsCompletionItem(matchResults[selectedItemIndex], cancellationToken);
                        }
                    }

                    var typedChar = _snapshotData.Trigger.Character;

                    // Check that it is a filter symbol. We can be called for a non-filter symbol.
                    // If inserting a non-filter character (neither IsPotentialFilterCharacter, nor Helpers.IsFilterCharacter),
                    // we should dismiss completion except cases where this is the first symbol typed for the completion session
                    // (string.IsNullOrEmpty(filterText) or string.Equals(filterText, typeChar.ToString(), StringComparison.OrdinalIgnoreCase)).
                    // In the latter case, we should keep the completion because it was confirmed just before in InitializeCompletion.
                    if (UpdateTriggerReason == CompletionTriggerReason.Insertion &&
                        !string.IsNullOrEmpty(_filterText) &&
                        !string.Equals(_filterText, typedChar.ToString(), StringComparison.OrdinalIgnoreCase) &&
                        !IsPotentialFilterCharacter(typedChar) &&
                        !Helpers.IsFilterCharacter(bestOrFirstMatchResult.CompletionItem, typedChar, _filterText))
                    {
                        return null;
                    }

                    var isHardSelection = IsHardSelection(bestOrFirstMatchResult.CompletionItem, bestOrFirstMatchResult.ShouldBeConsideredMatchingFilterText);
                    var updateSelectionHint = isHardSelection ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected;

                    return new(selectedItemIndex, updateSelectionHint, uniqueItem);
                }
                finally
                {
                    // Don't call ClearAndFree, which resets the capacity to a default value.
                    filteredMatchResultsBuilder.Clear();
                    s_listOfMatchResultPool.Free(filteredMatchResultsBuilder);
                }
            }

            private VSCompletionItem GetCorrespondingVsCompletionItem(MatchResult matchResult, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _snapshotData.InitialSortedItemList[matchResult.IndexInOriginalSortedOrder];
            }

            private ItemSelection? HandleDeletionTrigger(IReadOnlyList<MatchResult> items, CancellationToken cancellationToken)
            {
                // Go through the entire item list to find the best match(es).
                // If we had matching items, then pick the best of the matching items and
                // choose that one to be hard selected.  If we had no actual matching items
                // (which can happen if the user deletes down to a single character and we
                // include everything), then we just soft select the first item.
                var indexToSelect = 0;
                var hardSelect = false;
                MatchResult? bestMatchResult = null;
                var moreThanOneMatch = false;

                for (var i = 0; i < items.Count; ++i)
                {
                    var currentMatchResult = items[i];

                    if (!currentMatchResult.ShouldBeConsideredMatchingFilterText)
                        continue;

                    if (bestMatchResult == null)
                    {
                        // We had no best result yet, so this is now our best result.
                        bestMatchResult = currentMatchResult;
                        indexToSelect = i;
                    }
                    else
                    {
                        var match = CompareForDeletion(currentMatchResult, bestMatchResult.Value, _filterText);
                        if (match > 0)
                        {
                            moreThanOneMatch = false;
                            bestMatchResult = currentMatchResult;
                            indexToSelect = i;
                        }
                        else if (match == 0)
                        {
                            moreThanOneMatch = true;
                        }
                    }
                }

                if (bestMatchResult is null)
                {
                    // The user has typed something, but nothing in the actual list matched what
                    // they were typing.  In this case, we want to dismiss completion entirely.
                    // The thought process is as follows: we aggressively brought up completion
                    // to help them when they typed delete (in case they wanted to pick another
                    // item).  However, they're typing something that doesn't seem to match at all
                    // The completion list is just distracting at this point.
                    if (UpdateTriggerReason == CompletionTriggerReason.Insertion)
                        return null;

                    // If we are in suggestion mode and nothing matches filter text, we should soft select SuggestionItem.
                    if (ShouldSelectSuggestionItemWhenNoItemMatchesFilterText)
                        indexToSelect = SuggestionItemIndex;
                }
                else
                {
                    // Only hard select this result if it's a prefix match
                    // We need to do this so that
                    // * deleting and retyping a dot in a member access does not change the
                    //   text that originally appeared before the dot
                    // * deleting through a word from the end keeps that word selected
                    // This also preserves the behavior the VB had through Dev12.
                    hardSelect = !_hasSuggestedItemOptions && bestMatchResult.Value.FilterTextUsed.StartsWith(_filterText, StringComparison.CurrentCultureIgnoreCase);
                }

                // The best match we have selected is unique if `moreThanOneMatch` is false.
                return new(SelectedItemIndex: indexToSelect,
                    SelectionHint: hardSelect ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected,
                    UniqueItem: moreThanOneMatch || !bestMatchResult.HasValue ? null : GetCorrespondingVsCompletionItem(bestMatchResult.Value, cancellationToken));

                static int CompareForDeletion(MatchResult x, MatchResult y, string pattern)
                {
                    // Prefer the item that matches a longer prefix of the filter text.
                    var comparison = x.FilterTextUsed.GetCaseInsensitivePrefixLength(pattern).CompareTo(y.FilterTextUsed.GetCaseInsensitivePrefixLength(pattern));
                    if (comparison != 0)
                        return comparison;

                    // If there are "Abc" vs "abc", we should prefer the case typed by user.
                    comparison = x.FilterTextUsed.GetCaseSensitivePrefixLength(pattern).CompareTo(y.FilterTextUsed.GetCaseSensitivePrefixLength(pattern));
                    if (comparison != 0)
                        return comparison;

                    var xItem = x.CompletionItem;
                    var yItem = y.CompletionItem;

                    // If the lengths are the same, prefer the one with the higher match priority.
                    // But only if it's an item that would have been hard selected.  We don't want
                    // to aggressively select an item that was only going to be softly offered.
                    comparison = GetPriority(xItem).CompareTo(GetPriority(yItem));
                    if (comparison != 0)
                        return comparison;

                    // Prefer Intellicode items.
                    return xItem.IsPreferredItem().CompareTo(yItem.IsPreferredItem());

                    static int GetPriority(RoslynCompletionItem item)
                        => item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection ? item.Rules.MatchPriority : MatchPriority.Default;
                }
            }

            private CompletionList<CompletionItemWithHighlight> GetHighlightedList(
                IAsyncCompletionSession session,
                IReadOnlyList<MatchResult> matchResults,
                CancellationToken cancellationToken)
            {
                return session.CreateCompletionList(matchResults.Select(matchResult =>
                {
                    var vsItem = GetCorrespondingVsCompletionItem(matchResult, cancellationToken);
                    var highlightedSpans = _highlightMatchingPortions
                        ? GetHighlightedSpans(matchResult, _completionHelper, _filterText)
                        : ImmutableArray<Span>.Empty;

                    return new CompletionItemWithHighlight(vsItem, highlightedSpans);
                }));

                static ImmutableArray<Span> GetHighlightedSpans(
                    MatchResult matchResult,
                    CompletionHelper completionHelper,
                    string filterText)
                {
                    if (matchResult.CompletionItem.HasDifferentFilterText || matchResult.CompletionItem.HasAdditionalFilterTexts)
                    {
                        // The PatternMatch in MatchResult is calculated based on Roslyn item's FilterText, which can be used to calculate
                        // highlighted span for VSCompletion item's DisplayText w/o doing the matching again.
                        // However, if the Roslyn item's FilterText is different from its DisplayText, we need to do the match against the
                        // display text of the VS item directly to get the highlighted spans. This is done in a best effort fashion and there
                        // is no guarantee a proper match would be found for highlighting.
                        return completionHelper.GetHighlightedSpans(
                            matchResult.CompletionItem, filterText, CultureInfo.CurrentCulture).SelectAsArray(s => s.ToSpan());
                    }

                    var patternMatch = matchResult.PatternMatch;
                    if (patternMatch.HasValue)
                    {
                        // Since VS item's display text is created as Prefix + DisplayText + Suffix, 
                        // we can calculate the highlighted span by adding an offset that is the length of the Prefix.
                        return patternMatch.Value.MatchedSpans.SelectAsArray(GetOffsetSpan, matchResult.CompletionItem);
                    }

                    // If there's no match for Roslyn item's filter text which is identical to its display text,
                    // then we can safely assume there'd be no matching to VS item's display text.
                    return ImmutableArray<Span>.Empty;
                }

                // PERF: static local function to avoid lambda allocation on hot path
                static Span GetOffsetSpan(TextSpan span, RoslynCompletionItem item)
                    => span.MoveTo(item.DisplayTextPrefix?.Length ?? 0).ToSpan();
            }

            private FilteredCompletionModel? HandleAllItemsFilteredOut()
            {
                if (UpdateTriggerReason == CompletionTriggerReason.Insertion)
                {
                    // If the user was just typing, and the list went to empty *and* this is a 
                    // language that wants to dismiss on empty, then just return a null model
                    // to stop the completion session.
                    if (_completionRules.DismissIfEmpty)
                    {
                        return null;
                    }
                }

                // If we are in suggestion mode then we should select the SuggestionItem instead.
                var selectedItemIndex = ShouldSelectSuggestionItemWhenNoItemMatchesFilterText ? SuggestionItemIndex : 0;

                // If the user has turned on some filtering states, and we filtered down to
                // nothing, then we do want the UI to show that to them.  That way the user
                // can turn off filters they don't want and get the right set of items.

                // If we are going to filter everything out, then just preserve the existing
                // model (and all the previously filtered items), but switch over to soft
                // selection.
                return new FilteredCompletionModel(
                    items: ImmutableArray<CompletionItemWithHighlight>.Empty, selectedItemIndex,
                    filters: _snapshotData.SelectedFilters, selectionHint: UpdateSelectionHint.SoftSelected, centerSelection: true, uniqueItem: null);
            }

            private ImmutableArray<CompletionFilterWithState> GetUpdatedFilters(IReadOnlyList<MatchResult> matchResults, CancellationToken cancellationToken)
            {
                if (!_showCompletionItemFilters)
                    return ImmutableArray<CompletionFilterWithState>.Empty;

                // See which filters might be enabled based on the typed code
                using var _ = PooledHashSet<CompletionFilter>.GetInstance(out var filters);
                foreach (var item in matchResults)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    filters.AddRange(GetCorrespondingVsCompletionItem(item, cancellationToken).Filters);
                }

                // When no items are available for a given filter, it becomes unavailable.
                // Expanders always appear available as long as it's presented.
                return _snapshotData.SelectedFilters.SelectAsArray(n => n.WithAvailability(n.Filter is CompletionExpander || filters.Contains(n.Filter)));
            }

            /// <summary>
            /// Given multiple possible chosen completion items, pick the one using the following perferences (in order):
            ///     1. Most recently used item is our top preference
            ///     2. IntelliCode item over non-IntelliCode item
            ///     3. Higher MatchPriority
            ///     4. Match to FilterText over AdditionalFilterTexts
            /// </summary>
            private static MatchResult GetBestCompletionItemSelectionFromFilteredResults(IReadOnlyList<MatchResult> filteredMatchResults)
            {
                Debug.Assert(filteredMatchResults.Count > 0);

                var bestResult = filteredMatchResults[0];
                var bestResultMruIndex = bestResult.RecentItemIndex;

                for (int i = 1, n = filteredMatchResults.Count; i < n; i++)
                {
                    var currentResult = filteredMatchResults[i];
                    var currentResultMruIndex = currentResult.RecentItemIndex;

                    // Most recently used item is our top preference.
                    if (currentResultMruIndex != bestResultMruIndex)
                    {
                        if (currentResultMruIndex > bestResultMruIndex)
                        {
                            bestResult = currentResult;
                            bestResultMruIndex = currentResultMruIndex;
                        }

                        continue;
                    }

                    // 2nd preference is IntelliCode item
                    var currentIsPreferred = currentResult.CompletionItem.IsPreferredItem();
                    var bestIsPreferred = bestResult.CompletionItem.IsPreferredItem();

                    if (currentIsPreferred != bestIsPreferred)
                    {
                        if (currentIsPreferred && !bestIsPreferred)
                        {
                            bestResult = currentResult;
                        }

                        continue;
                    }

                    // 3rd preference is higher MatchPriority
                    var currentMatchPriority = currentResult.CompletionItem.Rules.MatchPriority;
                    var bestMatchPriority = bestResult.CompletionItem.Rules.MatchPriority;

                    if (currentMatchPriority != bestMatchPriority)
                    {
                        if (currentMatchPriority > bestMatchPriority)
                        {
                            bestResult = currentResult;
                        }

                        continue;
                    }

                    // final preference is match to FilterText over AdditionalFilterTexts
                    if (bestResult.MatchedWithAdditionalFilterTexts && !currentResult.MatchedWithAdditionalFilterTexts)
                    {
                        bestResult = currentResult;
                    }
                }

                return bestResult;
            }

            private static bool TryGetInitialTriggerLocation(AsyncCompletionSessionDataSnapshot data, out SnapshotPoint intialTriggerLocation)
            {
                foreach (var item in data.InitialSortedItemList)
                {
                    if (CompletionItemData.TryGetData(item, out var itemData) && itemData.TriggerLocation.HasValue)
                    {
                        intialTriggerLocation = itemData.TriggerLocation.Value;
                        return true;
                    }
                }

                intialTriggerLocation = default;
                return false;
            }

            private bool IsHardSelection(
                RoslynCompletionItem item,
                bool matchedFilterText)
            {
                if (_hasSuggestedItemOptions)
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
                if (_filterText.Length > 0 && IsAllPunctuation(_filterText) && _filterText != item.DisplayText)
                {
                    return false;
                }

                // If the user hasn't actually typed anything, then don't hard select any item.
                // The only exception to this is if the completion provider has requested the
                // item be preselected.
                if (_filterText.Length == 0)
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
                Debug.Assert(_filterText.Length > 0 || item.Rules.MatchPriority != MatchPriority.Default);

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

            private ItemSelection UpdateSelectionBasedOnSuggestedDefaults(IReadOnlyList<MatchResult> items, ItemSelection itemSelection, CancellationToken cancellationToken)
            {
                // Editor doesn't provide us a list of "default" items, or we select SuggestionItem (because we are in suggestion mode and have no match in the list)
                if (_snapshotData.Defaults.IsDefaultOrEmpty || itemSelection.SelectedItemIndex == SuggestionItemIndex)
                    return itemSelection;

                // "Preselect" is only used when we have high confidence with the selection, so don't override it.
                var selectedItem = items[itemSelection.SelectedItemIndex].CompletionItem;
                if (selectedItem.Rules.MatchPriority >= MatchPriority.Preselect)
                    return itemSelection;

                var tick = Environment.TickCount;

                var finalSelection = GetDefaultsMatch(items, itemSelection, cancellationToken);

                AsyncCompletionLogger.LogGetDefaultsMatchTicksDataPoint(Environment.TickCount - tick);
                return finalSelection;
            }

            /// <summary>
            /// Compare the pattern matching result of the current selection with the pattern matching result of the suggested defaults (both w.r.t. the filter text.)
            /// If the suggested default is no worse than current selected item (in a case-sensitive manner,) use the suggested default. Otherwise use the original selection.
            /// For example, if user typed "C", roslyn might select "CancellationToken", but with suggested default "Console" we will end up selecting "Console" instead.
            /// </summary>
            private ItemSelection GetDefaultsMatch(IReadOnlyList<MatchResult> items, ItemSelection intialSelection, CancellationToken cancellationToken)
            {
                // Because the items are already sorted based on pattern-matching score, try to limit the range for the items we compare default with
                // by searching for the first "inferior" item, so we can avoid always going through the entire list.
                int inferiorItemIndex;
                if (_filterText.Length == 0)
                {
                    // Without filterText, all items are equally good match (w.r.t to the empty filterText), so we have to consider all of them.
                    inferiorItemIndex = items.Count;
                }
                else
                {
                    var selectedItemMatch = items[intialSelection.SelectedItemIndex].PatternMatch;

                    // It's possible that an item doesn't match filter text but still ended up being selected, this is because we just always keep all the
                    // items in the list in some cases. For example, user brought up completion with ctrl-j or through deletion.
                    // Don't bother changing the selection in such cases (since there's no match to the filter text in the list)
                    if (!selectedItemMatch.HasValue)
                        return intialSelection;

                    // Because the items are sorted based on pattern-matching score, the selectedIndex is in the middle of a range of
                    // -- as far as the pattern matcher is concerned -- equivalent items (items with identical PatternMatch.Kind and IsCaseSensitive).
                    // Find the last items in the range and use that to limit the items searched for from the defaults list.     
                    inferiorItemIndex = intialSelection.SelectedItemIndex;
                    while (++inferiorItemIndex < items.Count)
                    {
                        var itemMatch = items[inferiorItemIndex].PatternMatch;
                        if (!itemMatch.HasValue
                            || itemMatch.Value.Kind != selectedItemMatch.Value.Kind
                            || itemMatch.Value.IsCaseSensitive != selectedItemMatch.Value.IsCaseSensitive)
                        {
                            break;
                        }
                    }
                }

                foreach (var defaultText in _snapshotData.Defaults)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // The range includes all items that are as good of a match as what we initially selected (and in descending order of matching score)
                    // so we just need to search for the first item that matches the suggested default.
                    for (var i = 0; i < inferiorItemIndex; ++i)
                    {
                        if (items[i].CompletionItem.DisplayText == defaultText)
                        {
                            // If user hasn't typed anything, we'd like to hard select the default item.
                            // This way, they can easily commit the default item which matches what WLC shows.
                            var selectionHint = _filterText.Length == 0 ? UpdateSelectionHint.Selected : intialSelection.SelectionHint;
                            return intialSelection with { SelectedItemIndex = i, SelectionHint = selectionHint };
                        }
                    }
                }

                // Don't change the original selection since there's no match to the defaults provided.
                return intialSelection;
            }

            private sealed class FilterStateHelper
            {
                private readonly ImmutableArray<CompletionFilterWithState> _nonExpanderFilterStates;
                private readonly ImmutableArray<CompletionFilter> _selectedNonExpanderFilters;
                private readonly ImmutableArray<CompletionFilter> _unselectedExpanders;
                private readonly bool _needToFilter;
                private readonly bool _needToFilterExpanded;

                public FilterStateHelper(ImmutableArray<CompletionFilterWithState> filtersWithState)
                {
                    // The filter state list contains two kinds of "filters": regular filter and expander.
                    // The difference between them is they have different semantics.
                    // - When all filters or no filter is selected, everything should be included.
                    //   But when a strict subset of filters is selected, only items corresponding to the selected filters should be included.
                    // - When expander is selected, all expanded items should be included, otherwise, expanded items should be excluded.
                    //   expander state has no affect on non-expanded items.
                    //   For example, right now we only have one expander for items from unimported namespaces, selecting/unselecting expander would
                    //   include/exclude those items from completion list, but in-scope items would be shown regardless.
                    // 
                    // Therefore, we need to filter if 
                    // 1. a non-empty strict subset of filters are selected
                    // 2. a non-empty set of expanders are unselected
                    _nonExpanderFilterStates = filtersWithState.WhereAsArray(f => f.Filter is not CompletionExpander);

                    _selectedNonExpanderFilters = _nonExpanderFilterStates.SelectAsArray(f => f.IsSelected, f => f.Filter);
                    _needToFilter = _selectedNonExpanderFilters.Length > 0 && _selectedNonExpanderFilters.Length < _nonExpanderFilterStates.Length;

                    _unselectedExpanders = filtersWithState.SelectAsArray(f => !f.IsSelected && f.Filter is CompletionExpander, f => f.Filter);
                    _needToFilterExpanded = _unselectedExpanders.Length > 0;
                }

                public bool ShouldBeFilteredOut(VSCompletionItem item)
                    => ShouldBeFilteredOutOfCompletionList(item) || ShouldBeFilteredOutOfExpandedCompletionList(item);

                private bool ShouldBeFilteredOutOfCompletionList(VSCompletionItem item)
                    => _needToFilter && !item.Filters.Any(static (filter, self) => self._selectedNonExpanderFilters.Contains(filter), this);

                private bool ShouldBeFilteredOutOfExpandedCompletionList(VSCompletionItem item)
                {
                    if (!_needToFilterExpanded)
                        return false;

                    var associatedWithUnselectedExpander = false;
                    foreach (var itemFilter in item.Filters)
                    {
                        if (itemFilter is CompletionExpander)
                        {
                            if (!_unselectedExpanders.Contains(itemFilter))
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

            private readonly record struct ItemSelection(int SelectedItemIndex, UpdateSelectionHint SelectionHint, VSCompletionItem? UniqueItem);
        }
    }
}
