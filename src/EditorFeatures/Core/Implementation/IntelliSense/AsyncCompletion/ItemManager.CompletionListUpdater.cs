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
        /// <summary>
        /// Handles the filtering, sorting and selection of the completion items based on user inputs 
        /// (e.g. typed characters, selected filters, etc.)
        /// </summary>
        private sealed class CompletionListUpdater
        {
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

            private readonly Func<ImmutableArray<(RoslynCompletionItem, PatternMatch?)>, string, ImmutableArray<RoslynCompletionItem>> _filterMethod;

            private CompletionTriggerReason InitialTriggerReason => _snapshotData.InitialTrigger.Reason;
            private CompletionTriggerReason UpdateTriggerReason => _snapshotData.Trigger.Reason;

            // We might need to handle large amount of items with import completion enabled,
            // so use a dedicated pool to minimize/avoid array allocations (especially in LOH)
            // Set the size of pool to 1 because we don't expect UpdateCompletionListAsync to be
            // called concurrently, which essentially makes the pooled list a singleton,
            // but we still use ObjectPool for concurrency handling just to be robust.
            private static readonly ObjectPool<List<MatchResult<VSCompletionItem>>> s_listOfMatchResultPool = new(factory: () => new(), size: 1);

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
                        ? ((itemsWithPatternMatches, text) => CompletionService.FilterItems(_completionHelper, itemsWithPatternMatches, text))
                        : ((itemsWithPatternMatches, text) => _completionService.FilterItems(_document, itemsWithPatternMatches, text));

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
                    _filterMethod = (itemsWithPatternMatches, text) => CompletionService.FilterItems(_completionHelper, itemsWithPatternMatches, text);

                    _highlightMatchingPortions = false;
                    _showCompletionItemFilters = true;
                }
            }

            public FilteredCompletionModel? UpdateCompletionList(CancellationToken cancellationToken)
            {
                if (ShouldDismissCompletionListImmediately())
                    return null;

                // Use a dedicated pool to minimize potentially repeated large allocations,
                // since the completion list could be long with import completion enabled.
                var itemsToBeIncluded = s_listOfMatchResultPool.Allocate();
                try
                {
                    // Determine the list of items to be included in the completion list.
                    // This is computed based on the filter text as well as the current
                    // selection of filters and expander.
                    AddCompletionItems(itemsToBeIncluded, cancellationToken);

                    // Decide if we want to dismiss an empty completion list based on CompletionRules and filter usage.
                    if (itemsToBeIncluded.Count == 0)
                        return HandleAllItemsFilteredOut();

                    // Decide the item to be selected for this completion session.
                    // The selection is mostly based on how well the item matches with the filter text, but we also need to
                    // take into consideration for things like CompletionTrigger, MatchPriority, MRU, etc. 
                    var initialSelection = InitialTriggerReason == CompletionTriggerReason.Backspace || InitialTriggerReason == CompletionTriggerReason.Deletion
                        ? HandleDeletionTrigger(itemsToBeIncluded)
                        : HandleNormalFiltering(itemsToBeIncluded);

                    if (!initialSelection.HasValue)
                        return null;

                    // Editor might provide a list of items to us as a suggestion to what to select for this session
                    // (via IAsyncCompletionDefaultsSource), where the "default" means the "default selection".
                    // The main scenario for this is to keep the selected item in completion list in sync with the
                    // suggestion of "Whole-Line Completion" feature, where the default is usually set to the first token
                    // of the WLC suggestion.
                    var finalSelection = UpdateSelectionBasedOnSuggestedDefaults(itemsToBeIncluded, initialSelection.Value, cancellationToken);

                    return new FilteredCompletionModel(
                        items: GetHighlightedList(itemsToBeIncluded, cancellationToken),
                        finalSelection.SelectedItemIndex,
                        filters: GetUpdatedFilters(itemsToBeIncluded, cancellationToken),
                        finalSelection.SelectionHint,
                        centerSelection: true,
                        finalSelection.UniqueItem);
                }
                finally
                {
                    // Don't call ClearAndFree, which resets the capacity to a default value.
                    itemsToBeIncluded.Clear();
                    s_listOfMatchResultPool.Free(itemsToBeIncluded);
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

            private void AddCompletionItems(List<MatchResult<VSCompletionItem>> list, CancellationToken cancellationToken)
            {
                // FilterStateHelper is used to decide whether a given item should be included in the list based on the state of filter/expander buttons.
                var filterHelper = new FilterStateHelper(_snapshotData.SelectedFilters);
                filterHelper.LogTargetTypeFilterTelemetry(_sessionData);

                // We want to sort the items by pattern matching results while preserving the original alphabetical order for items with
                // same pattern match score, but `List<T>.Sort` isn't stable. Therefore we have to add a monotonically increasing integer
                // to `MatchResult` to keep track the original alphabetical order of each item.
                var currentIndex = 0;

                // Convert initial and update trigger reasons to corresponding Roslyn type so 
                // we can interact with Roslyn's completion system
                var roslynInitialTriggerKind = Helpers.GetRoslynTriggerKind(InitialTriggerReason);
                var roslynFilterReason = Helpers.GetFilterReason(UpdateTriggerReason);

                // Filter items based on the selected filters and matching.
                foreach (var item in _snapshotData.InitialSortedList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (filterHelper.ShouldBeFilteredOut(item))
                        continue;

                    if (CompletionItemData.TryGetData(item, out var itemData))
                    {
                        if (CompletionHelper.TryCreateMatchResult(_completionHelper, itemData.RoslynItem, item, _filterText,
                            roslynInitialTriggerKind, roslynFilterReason, _recentItemsManager.RecentItems, _highlightMatchingPortions, currentIndex,
                            out var matchResult))
                        {
                            list.Add(matchResult);
                            currentIndex++;
                        }
                    }
                    else
                    {
                        // All items passed in should contain a CompletionItemData object in the property bag,
                        // which is guaranteed in `ItemManager.SortCompletionListAsync`.
                        throw ExceptionUtilities.Unreachable;
                    }
                }

                list.Sort(MatchResult<VSCompletionItem>.SortingComparer);
            }

            private ItemSelection? HandleNormalFiltering(IReadOnlyList<MatchResult<VSCompletionItem>> items)
            {
                // Not deletion.  Defer to the language to decide which item it thinks best
                // matches the text typed so far.

                // Ask the language to determine which of the *matched* items it wants to select.
                var matchingItems = items.Where(r => r.MatchedFilterText).SelectAsArray(t => (t.RoslynCompletionItem, t.PatternMatch));

                var chosenItems = _filterMethod(matchingItems, _filterText);

                int selectedItemIndex;
                VSCompletionItem? uniqueItem = null;
                MatchResult<VSCompletionItem> bestOrFirstMatchResult;

                if (chosenItems.Length == 0)
                {
                    // We do not have matches: pick the one with longest common prefix.
                    // If we can't find such an item, just return the first item from the list.
                    selectedItemIndex = 0;
                    bestOrFirstMatchResult = items[0];

                    var longestCommonPrefixLength = bestOrFirstMatchResult.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(_filterText);

                    for (var i = 1; i < items.Count; ++i)
                    {
                        var item = items[i];
                        var commonPrefixLength = item.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(_filterText);

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
                    // Of the items the service returned, pick the one most recently committed
                    var bestItem = GetBestCompletionItemBasedOnMRUFirstOtherwiseOnPriority(chosenItems);

                    // Determine if we should consider this item 'unique' or not.  A unique item
                    // will be automatically committed if the user hits the 'invoke completion' 
                    // without bringing up the completion list.  An item is unique if it was the
                    // only item to match the text typed so far, and there was at least some text
                    // typed.  i.e.  if we have "Console.$$" we don't want to commit something
                    // like "WriteLine" since no filter text has actually been provided.  However,
                    // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
                    for (selectedItemIndex = 0; selectedItemIndex < items.Count; ++selectedItemIndex)
                    {
                        if (Equals(items[selectedItemIndex].RoslynCompletionItem, bestItem))
                            break;
                    }

                    Debug.Assert(selectedItemIndex < items.Count);

                    bestOrFirstMatchResult = items[selectedItemIndex];

                    if (_filterText.Length > 0)
                    {
                        // PreferredItems from IntelliCode are duplicate of normal items, so we ignore them
                        // when deciding if we have an unique item.
                        if (matchingItems.Count(r => !r.RoslynCompletionItem.IsPreferredItem()) == 1)
                            uniqueItem = items[selectedItemIndex].EditorCompletionItem;
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
                    !Helpers.IsFilterCharacter(bestOrFirstMatchResult.RoslynCompletionItem, typedChar, _filterText))
                {
                    return null;
                }

                var isHardSelection = IsHardSelection(bestOrFirstMatchResult.RoslynCompletionItem, bestOrFirstMatchResult.MatchedFilterText);
                var updateSelectionHint = isHardSelection ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected;

                return new(selectedItemIndex, updateSelectionHint, uniqueItem);
            }

            private ItemSelection? HandleDeletionTrigger(IReadOnlyList<MatchResult<VSCompletionItem>> items)
            {
                // Go through the entire item list to find the best match(es).
                // If we had matching items, then pick the best of the matching items and
                // choose that one to be hard selected.  If we had no actual matching items
                // (which can happen if the user deletes down to a single character and we
                // include everything), then we just soft select the first item.
                var indexToSelect = 0;
                var hardSelect = false;
                MatchResult<VSCompletionItem>? bestMatchResult = null;
                var moreThanOneMatch = false;

                for (var i = 0; i < items.Count; ++i)
                {
                    var currentMatchResult = items[i];

                    if (!currentMatchResult.MatchedFilterText)
                        continue;

                    if (bestMatchResult == null)
                    {
                        // We had no best result yet, so this is now our best result.
                        bestMatchResult = currentMatchResult;
                        indexToSelect = i;
                    }
                    else
                    {
                        var match = currentMatchResult.CompareTo(bestMatchResult.Value, _filterText);
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

                if (UpdateTriggerReason == CompletionTriggerReason.Insertion && bestMatchResult is null)
                {
                    // The user has typed something, but nothing in the actual list matched what
                    // they were typing.  In this case, we want to dismiss completion entirely.
                    // The thought process is as follows: we aggressively brought up completion
                    // to help them when they typed delete (in case they wanted to pick another
                    // item).  However, they're typing something that doesn't seem to match at all
                    // The completion list is just distracting at this point.
                    return null;
                }

                if (bestMatchResult is not null)
                {
                    // Only hard select this result if it's a prefix match
                    // We need to do this so that
                    // * deleting and retyping a dot in a member access does not change the
                    //   text that originally appeared before the dot
                    // * deleting through a word from the end keeps that word selected
                    // This also preserves the behavior the VB had through Dev12.
                    hardSelect = !_hasSuggestedItemOptions && bestMatchResult.Value.EditorCompletionItem.FilterText.StartsWith(_filterText, StringComparison.CurrentCultureIgnoreCase);
                }

                // The best match we have selected is unique if `moreThanOneMatch` is false.
                return new(SelectedItemIndex: indexToSelect,
                    SelectionHint: hardSelect ? UpdateSelectionHint.Selected : UpdateSelectionHint.SoftSelected,
                    UniqueItem: moreThanOneMatch ? null : bestMatchResult.GetValueOrDefault().EditorCompletionItem);
            }

            private ImmutableArray<CompletionItemWithHighlight> GetHighlightedList(IReadOnlyList<MatchResult<VSCompletionItem>> items, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<CompletionItemWithHighlight>.GetInstance(items.Count, out var builder);
                builder.AddRange(items.Select(matchResult =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var highlightedSpans = _highlightMatchingPortions
                        ? GetHighlightedSpans(matchResult, _completionHelper, _filterText)
                        : ImmutableArray<Span>.Empty;

                    return new CompletionItemWithHighlight(matchResult.EditorCompletionItem, highlightedSpans);
                }));

                return builder.ToImmutable();

                static ImmutableArray<Span> GetHighlightedSpans(
                    MatchResult<VSCompletionItem> matchResult,
                    CompletionHelper completionHelper,
                    string filterText)
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
                        return patternMatch.Value.MatchedSpans.SelectAsArray(GetOffsetSpan, matchResult.RoslynCompletionItem);
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

                // If the user has turned on some filtering states, and we filtered down to
                // nothing, then we do want the UI to show that to them.  That way the user
                // can turn off filters they don't want and get the right set of items.

                // If we are going to filter everything out, then just preserve the existing
                // model (and all the previously filtered items), but switch over to soft
                // selection.
                return new FilteredCompletionModel(
                    items: ImmutableArray<CompletionItemWithHighlight>.Empty, selectedItemIndex: 0,
                    filters: _snapshotData.SelectedFilters, selectionHint: UpdateSelectionHint.SoftSelected, centerSelection: true, uniqueItem: null);
            }

            private ImmutableArray<CompletionFilterWithState> GetUpdatedFilters(IReadOnlyList<MatchResult<VSCompletionItem>> items, CancellationToken cancellationToken)
            {
                if (!_showCompletionItemFilters)
                    return ImmutableArray<CompletionFilterWithState>.Empty;

                // See which filters might be enabled based on the typed code
                using var _ = PooledHashSet<CompletionFilter>.GetInstance(out var filters);
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    filters.AddRange(item.EditorCompletionItem.Filters);
                }

                // When no items are available for a given filter, it becomes unavailable.
                // Expanders always appear available as long as it's presented.
                return _snapshotData.SelectedFilters.SelectAsArray(n => n.WithAvailability(n.Filter is CompletionExpander || filters.Contains(n.Filter)));
            }

            /// <summary>
            /// Given multiple possible chosen completion items, pick the one that has the
            /// best MRU index, or the one with highest MatchPriority if none in MRU.
            /// </summary>
            private RoslynCompletionItem GetBestCompletionItemBasedOnMRUFirstOtherwiseOnPriority(ImmutableArray<RoslynCompletionItem> chosenItems)
            {
                Debug.Assert(chosenItems.Length > 0);

                var recentItems = _recentItemsManager.RecentItems;

                // Try to find the chosen item has been most recently used.
                var bestItem = chosenItems[0];
                var mruIndex1 = GetRecentItemIndex(recentItems, bestItem);
                for (int i = 1, n = chosenItems.Length; i < n; i++)
                {
                    var chosenItem = chosenItems[i];
                    var mruIndex2 = GetRecentItemIndex(recentItems, chosenItem);

                    if ((mruIndex2 < mruIndex1) ||
                        (mruIndex2 == mruIndex1 && !bestItem.IsPreferredItem() && chosenItem.IsPreferredItem()))
                    {
                        bestItem = chosenItem;
                        mruIndex1 = GetRecentItemIndex(recentItems, bestItem);
                    }
                }

                // If our best item appeared in the MRU list, use it
                if (GetRecentItemIndex(recentItems, bestItem) <= 0)
                {
                    return bestItem;
                }

                // Otherwise use the chosen item that has the highest matchPriority.
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

                static int GetRecentItemIndex(ImmutableArray<string> recentItems, RoslynCompletionItem item)
                {
                    var index = recentItems.IndexOf(item.FilterText);
                    return -index;
                }
            }

            private static bool TryGetInitialTriggerLocation(AsyncCompletionSessionDataSnapshot data, out SnapshotPoint intialTriggerLocation)
            {
                foreach (var item in data.InitialSortedList)
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

            private ItemSelection UpdateSelectionBasedOnSuggestedDefaults(IReadOnlyList<MatchResult<VSCompletionItem>> items, ItemSelection itemSelection, CancellationToken cancellationToken)
            {
                // Editor doesn't provide us a list of "default" items.
                if (_snapshotData.Defaults.IsDefaultOrEmpty)
                    return itemSelection;

                // "Preselect" is only used when we have high confidence with the selection, so don't override it.
                var selectedItem = items[itemSelection.SelectedItemIndex].RoslynCompletionItem;
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
            private ItemSelection GetDefaultsMatch(IReadOnlyList<MatchResult<VSCompletionItem>> items, ItemSelection intialSelection, CancellationToken cancellationToken)
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
                        if (items[i].RoslynCompletionItem.DisplayText == defaultText)
                            return intialSelection with { SelectedItemIndex = i };
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
                    => _needToFilter && !item.Filters.Any(filter => _selectedNonExpanderFilters.Contains(filter));

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

                public void LogTargetTypeFilterTelemetry(CompletionSessionData sessionData)
                {
                    if (sessionData.TargetTypeFilterExperimentEnabled)
                    {
                        // Telemetry: Want to know % of sessions with the "Target type matches" filter where that filter is actually enabled
                        if (_needToFilter &&
                            !sessionData.TargetTypeFilterSelected &&
                            _selectedNonExpanderFilters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches))
                        {
                            AsyncCompletionLogger.LogTargetTypeFilterChosenInSession();

                            // Make sure we only record one enabling of the filter per session
                            sessionData.TargetTypeFilterSelected = true;
                        }
                    }
                }
            }

            private readonly record struct ItemSelection(int SelectedItemIndex, UpdateSelectionHint SelectionHint, VSCompletionItem? UniqueItem);
        }
    }
}
