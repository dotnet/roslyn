// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal partial class Session
        {
            public void FilterModel(
                CompletionFilterReason filterReason,
                bool dismissIfEmptyAllowed,
                bool recheckCaretPosition,
                ImmutableDictionary<CompletionItemFilter, bool> filterState)
            {
                AssertIsForeground();

                var caretPosition = GetCaretPointInViewBuffer();

                // Use an interlocked increment so that reads by existing filter tasks will see the
                // change.
                Interlocked.Increment(ref _filterId);
                var localId = _filterId;
                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model =>
                    {
                        if (model != null && filterState != null)
                        {
                            // If the UI specified an updated filter state, then incorporate that 
                            // into our model.
                            model = model.WithFilterState(filterState);
                        }

                        return FilterModelInBackground(
                            model, localId, caretPosition, recheckCaretPosition, dismissIfEmptyAllowed, filterReason);
                    });
            }

            public void IdentifyBestMatchAndFilterToAllItems(
                CompletionFilterReason filterReason, bool recheckCaretPosition, bool dismissIfEmptyAllowed)
            {
                AssertIsForeground();

                var caretPosition = GetCaretPointInViewBuffer();

                // Use an interlocked increment so that reads by existing filter tasks will see the
                // change.
                Interlocked.Increment(ref _filterId);
                var localId = _filterId;
                Computation.ChainTaskAndNotifyControllerWhenFinished(model =>
                    {
                        var filteredModel = FilterModelInBackground(
                            model, localId, caretPosition, recheckCaretPosition, dismissIfEmptyAllowed, filterReason);

                        return filteredModel != null
                            ? filteredModel.WithFilteredItems(filteredModel.TotalItems).WithSelectedItem(filteredModel.SelectedItem)
                            : null;
                    });
            }

            private Model FilterModelInBackground(
                Model model,
                int id,
                SnapshotPoint caretPosition,
                bool recheckCaretPosition,
                bool dismissIfEmptyAllowed,
                CompletionFilterReason filterReason)
            {
                using (Logger.LogBlock(FunctionId.Completion_ModelComputation_FilterModelInBackground, CancellationToken.None))
                {
                    return FilterModelInBackgroundWorker(
                        model, id, caretPosition, recheckCaretPosition, dismissIfEmptyAllowed, filterReason);
                }
            }

            private Model FilterModelInBackgroundWorker(
                Model model,
                int id,
                SnapshotPoint caretPosition,
                bool recheckCaretPosition,
                bool dismissIfEmptyAllowed,
                CompletionFilterReason filterReason)
            {
                if (model == null)
                {
                    return null;
                }

                var filterState = model.FilterState;

                // If all the filters are on, or all the filters are off then we don't actually 
                // need to filter.
                if (filterState != null)
                {
                    if (filterState.Values.All(b => b) ||
                        filterState.Values.All(b => !b))
                    {
                        filterState = null;
                    }
                }

                // We want to dismiss the session if the caret ever moved outside our bounds.
                if (recheckCaretPosition && Controller.IsCaretOutsideAllItemBounds(model, caretPosition))
                {
                    return null;
                }

                if (id != _filterId)
                {
                    return model;
                }

                var textSnapshot = caretPosition.Snapshot;
                var textSpanToText = new Dictionary<TextSpan, string>();

                var document = this.Controller.GetDocument();
                var helper = this.Controller.GetCompletionHelper();

                var recentItems = this.Controller.GetRecentItems();

                var filterResults = new List<FilterResult>();

                var filterText = model.GetCurrentTextInSnapshot(model.OriginalList.Span, textSnapshot, textSpanToText);

                // If the user was typing a number, then immediately dismiss completion.
                var filterTextStartsWithANumber = filterText.Length > 0 && char.IsNumber(filterText[0]);
                if (filterTextStartsWithANumber)
                {
                    return null;
                }

                foreach (var currentItem in model.TotalItems)
                {
                    // Check if something new has happened and there's a later on filter operation
                    // in the chain.  If so, there's no need for us to do any more work (as it will
                    // just be superceded by the later work).
                    if (id != _filterId)
                    {
                        return model;
                    }

                    if (ItemIsFilteredOut(currentItem.Item, filterState))
                    {
                        continue;
                    }

                    // Check if the item matches the filter text typed so far.
                    var matchesFilterText = MatchesFilterText(helper, currentItem.Item, filterText, model.Trigger, filterReason, recentItems);

                    if (matchesFilterText)
                    {
                        filterResults.Add(new FilterResult(
                            currentItem, filterText, matchedFilterText: true));
                    }
                    else
                    {
                        if (filterText.Length <= 1)
                        {
                            // Even though the rule provider didn't match this, we'll still include it
                            // since we want to allow a user typing a single character and seeing all
                            // possibly completions.
                            filterResults.Add(new FilterResult(
                                currentItem, filterText, matchedFilterText: false));
                        }
                    }
                }

                model = model.WithFilterText(filterText);

                // If no items matched the filter text then determine what we should do.
                if (filterResults.Count == 0)
                {
                    return HandleAllItemsFilteredOut(model, filterReason, dismissIfEmptyAllowed);
                }

                // If this was deletion, then we control the entire behavior of deletion
                // ourselves.
                if (model.Trigger.Kind == CompletionTriggerKind.Deletion)
                {
                    return HandleDeletionTrigger(model, filterResults);
                }

                return HandleNormalFiltering(
                    model, filterReason, textSnapshot, document,
                    helper, recentItems, filterText, filterResults);
            }

            private Model HandleNormalFiltering(
                Model model, CompletionFilterReason filterReason,
                ITextSnapshot textSnapshot, Document document,
                CompletionHelper helper, ImmutableArray<string> recentItems,
                string filterText,
                List<FilterResult> filterResults)
            {
                // Not deletion.  Defer to the language to decide which item it thinks best
                // matches the text typed so far.

                // Ask the language to determine which of the *matched* items it wants to select.
                var service = this.Controller.GetCompletionService();

                var matchingCompletionItems = filterResults.Where(r => r.MatchedFilterText)
                                                           .Select(t => t.PresentationItem.Item)
                                                           .AsImmutable();
                var chosenItems = service.ChooseBestItems(document, matchingCompletionItems, filterText);

                // Of the items the service returned, pick the one most recently committed
                var bestCompletionItem = GetBestCompletionItemBasedOnMRU(chosenItems, recentItems);

                // If we don't have a best completion item yet, then pick the first item from the list.
                var bestOrFirstCompletionItem = bestCompletionItem ?? filterResults.First().PresentationItem.Item;
                var bestOrFirstPresentationItem = filterResults.Where(
                    r => r.PresentationItem.Item == bestOrFirstCompletionItem).First().PresentationItem;

                var hardSelection = IsHardSelection(
                    model, bestOrFirstPresentationItem, textSnapshot, helper, filterReason);

                // Determine if we should consider this item 'unique' or not.  A unique item
                // will be automatically committed if the user hits the 'invoke completion' 
                // without bringing up the completion list.  An item is unique if it was the
                // only item to match the text typed so far, and there was at least some text
                // typed.  i.e.  if we have "Console.$$" we don't want to commit something
                // like "WriteLine" since no filter text has actually been provided.  HOwever,
                // if "Console.WriteL$$" is typed, then we do want "WriteLine" to be committed.
                var matchingItemCount = matchingCompletionItems.Length;
                var isUnique = bestCompletionItem != null &&
                    matchingItemCount == 1 &&
                    filterText.Length > 0;

                var result = model.WithFilteredItems(filterResults.Select(r => r.PresentationItem).AsImmutable())
                                  .WithSelectedItem(bestOrFirstPresentationItem)
                                  .WithHardSelection(hardSelection)
                                  .WithIsUnique(isUnique);

                return result;
            }

            /// <summary>
            /// Given multiple possible chosen completion items, pick the one that has the
            /// best MRU index.
            /// </summary>
            private CompletionItem GetBestCompletionItemBasedOnMRU(
                ImmutableArray<CompletionItem> chosenItems, ImmutableArray<string> recentItems)
            {
                var bestItem = chosenItems.FirstOrDefault();
                for (int i = 1, n = chosenItems.Length; i < n; i++)
                {
                    var chosenItem = chosenItems[i];
                    var mruIndex1 = GetRecentItemIndex(recentItems, bestItem);
                    var mruIndex2 = GetRecentItemIndex(recentItems, chosenItem);

                    if (mruIndex2 < mruIndex1)
                    {
                        bestItem = chosenItem;
                    }
                }

                return bestItem;
            }

            private Model HandleDeletionTrigger(Model model, List<FilterResult> filterResults)
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

                var filteredItems = filterResults.Select(r => r.PresentationItem).AsImmutable();
                model = model.WithFilteredItems(filteredItems);

                if (bestFilterResult != null)
                {
                    return model.WithSelectedItem(bestFilterResult.Value.PresentationItem)
                                .WithHardSelection(true)
                                .WithIsUnique(matchCount == 1);
                }
                else
                {
                    return model.WithHardSelection(false)
                                .WithIsUnique(false);
                }
            }

            private bool IsBetterDeletionMatch(FilterResult result1, FilterResult result2)
            {
                var item1 = result1.PresentationItem.Item;
                var item2 = result2.PresentationItem.Item;

                var prefixLength1 = item1.FilterText.GetCaseInsensitivePrefixLength(result1.FilterText);
                var prefixLength2 = item2.FilterText.GetCaseInsensitivePrefixLength(result2.FilterText);

                // Prefer the item that matches a longer prefix of the filter text.
                if (prefixLength1 > prefixLength2)
                {
                    return true;
                }

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

                return false;
            }

            private struct FilterResult
            {
                public readonly PresentationItem PresentationItem;
                public readonly bool MatchedFilterText;
                public readonly string FilterText;

                public FilterResult(PresentationItem presentationItem, string filterText, bool matchedFilterText)
                {
                    PresentationItem = presentationItem;
                    MatchedFilterText = matchedFilterText;
                    FilterText = filterText;
                }
            }

            private static Model HandleAllItemsFilteredOut(
                Model model,
                CompletionFilterReason filterReason,
                bool dismissIfEmptyAllowed)
            {
                if (dismissIfEmptyAllowed &&
                    model.DismissIfEmpty &&
                    filterReason == CompletionFilterReason.TypeChar)
                {
                    // If the user was just typing, and the list went to empty *and* this is a 
                    // language that wants to dismiss on empty, then just return a null model
                    // to stop the completion session.
                    return null;
                }

                if (model.FilterState?.Values.Any(b => b) == true)
                {
                    // If the user has turned on some filtering states, and we filtered down to 
                    // nothing, then we do want the UI to show that to them.  That way the user
                    // can turn off filters they don't want and get the right set of items.
                    return model.WithFilteredItems(ImmutableArray<PresentationItem>.Empty)
                                .WithFilterText("")
                                .WithHardSelection(false)
                                .WithIsUnique(false);
                }
                else
                {
                    // If we are going to filter everything out, then just preserve the existing
                    // model (and all the previously filtered items), but switch over to soft 
                    // selection.
                    return model.WithHardSelection(false)
                                .WithIsUnique(false);
                }
            }

            private static bool MatchesFilterText(
                CompletionHelper helper, CompletionItem item,
                string filterText, CompletionTrigger trigger,
                CompletionFilterReason filterReason, ImmutableArray<string> recentItems)
            {
                // For the deletion we bake in the core logic for how matching should work.
                // This way deletion feels the same across all languages that opt into deletion 
                // as a completion trigger.

                // Specifically, to avoid being too aggressive when matching an item during 
                // completion, we require that the current filter text be a prefix of the 
                // item in the list.
                if (filterReason == CompletionFilterReason.BackspaceOrDelete &&
                    trigger.Kind == CompletionTriggerKind.Deletion)
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

                    if (!recentItems.IsDefault && GetRecentItemIndex(recentItems, item) < 0)
                    {
                        return true;
                    }
                }

                if (filterText.Length > 0 && IsAllDigits(filterText))
                {
                    // The user is just typing a number.  We never want this to match against
                    // anything we would put in a completion list.
                    return false;
                }

                return helper.MatchesFilterText(item, filterText, CultureInfo.CurrentCulture);
            }

            private static int GetRecentItemIndex(ImmutableArray<string> recentItems, CompletionItem item)
            {
                var index = recentItems.IndexOf(item.DisplayText);
                return -index;
            }

            private static bool IsAllDigits(string filterText)
            {
                for (int i = 0; i < filterText.Length; i++)
                {
                    if (filterText[i] < '0' || filterText[i] > '9')
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool ItemIsFilteredOut(
                CompletionItem item,
                ImmutableDictionary<CompletionItemFilter, bool> filterState)
            {
                if (filterState == null)
                {
                    // No filtering.  The item is not filtered out.
                    return false;
                }

                foreach (var filter in CompletionItemFilter.AllFilters)
                {
                    // only consider filters that match the item
                    var matches = filter.Matches(item);
                    if (matches)
                    {
                        // if the specific filter is enabled then it is not filtered out
                        bool enabled;
                        if (filterState.TryGetValue(filter, out enabled) && enabled)
                        {
                            return false;
                        }
                    }
                }

                // The item was filtered out.
                return true;
            }

            private bool IsHardSelection(
                Model model,
                PresentationItem bestFilterMatch,
                ITextSnapshot textSnapshot,
                CompletionHelper completionHelper,
                CompletionFilterReason reason)
            {
                if (model.SuggestionModeItem != null)
                {
                    return bestFilterMatch != null && bestFilterMatch.Item.DisplayText == model.SuggestionModeItem.Item.DisplayText;
                }

                if (bestFilterMatch == null || model.UseSuggestionMode)
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
                var viewSpan = model.GetViewBufferSpan(bestFilterMatch.Item.Span);
                var fullFilterText = model.GetCurrentTextInSnapshot(viewSpan, textSnapshot, endPoint: null);

                var trigger = model.Trigger;
                var shouldSoftSelect = ShouldSoftSelectItem(bestFilterMatch.Item, fullFilterText, trigger);
                if (shouldSoftSelect)
                {
                    return false;
                }

                // If the user moved the caret left after they started typing, the 'best' match may not match at all
                // against the full text span that this item would be replacing.
                if (!MatchesFilterText(completionHelper, bestFilterMatch.Item, fullFilterText, trigger, reason, this.Controller.GetRecentItems()))
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
            private static bool ShouldSoftSelectItem(CompletionItem item, string filterText, CompletionTrigger trigger)
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
        }
    }
}