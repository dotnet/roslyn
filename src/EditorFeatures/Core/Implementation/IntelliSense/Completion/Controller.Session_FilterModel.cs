// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                ImmutableDictionary<CompletionItemFilter, bool> filterState)
            {
                AssertIsForeground();

                var caretPosition = GetCaretPointInViewBuffer();
                var document = Controller.GetDocument();

                // Use an interlocked increment so that reads by existing filter tasks will see the
                // change.
                Interlocked.Increment(ref _filterId);
                var localId = _filterId;
                Computation.ChainTaskAndNotifyControllerWhenFinished(
                    model => FilterModelInBackground(
                        document, model, localId, caretPosition, filterReason, filterState));
            }

            private Model FilterModelInBackground(
                Document document,
                Model model,
                int id,
                SnapshotPoint caretPosition,
                CompletionFilterReason filterReason,
                ImmutableDictionary<CompletionItemFilter, bool> filterState)
            {
                using (Logger.LogBlock(FunctionId.Completion_ModelComputation_FilterModelInBackground, CancellationToken.None))
                {
                    return FilterModelInBackgroundWorker(
                        document, model, id, caretPosition, filterReason, filterState);
                }
            }

            private Model FilterModelInBackgroundWorker(
                Document document,
                Model model,
                int id,
                SnapshotPoint caretPosition,
                CompletionFilterReason filterReason,
                ImmutableDictionary<CompletionItemFilter, bool> filterState)
            {
                if (model == null)
                {
                    return null;
                }

                // We want to dismiss the session if the caret ever moved outside our bounds.
                // Do this before we check the _filterId.  We don't want this work to not happen
                // just because the user typed more text and added more filter items.
                if (filterReason == CompletionFilterReason.CaretPositionChanged &&
                    Controller.IsCaretOutsideAllItemBounds(model, caretPosition))
                {
                    return null;
                }

                // If the UI specified an updated filter state, then incorporate that 
                // into our model. Do this before we check the _filterId.  We don't 
                // want this work to not happen just because the user typed more text 
                // and added more filter items.
                if (filterState != null)
                {
                    model = model.WithFilterState(filterState);
                }

                // If there's another request in the queue to filter items, then just
                // bail out immediately.  No point in doing extra work that's just
                // going to be overridden by the next filter task.
                if (id != _filterId)
                {
                    return model;
                }

                var textSnapshot = caretPosition.Snapshot;
                var textSpanToText = new Dictionary<TextSpan, string>();

                var helper = this.Controller.GetCompletionHelper(document);

                var recentItems = this.Controller.GetRecentItems();

                var filterResults = new List<FilterResult>();

                var filterText = model.GetCurrentTextInSnapshot(
                    model.OriginalList.Span, textSnapshot, textSpanToText);

                // Check if the user is typing a number.  If so, only proceed if it's a number
                // directly after a <dot>.  That's because it is actually reasonable for completion
                // to be brought up after a <dot> and for the user to want to filter completion
                // items based on a number that exists in the name of the item.  However, when 
                // we are not after a dot (i.e. we're being brought up after <space> is typed)
                // then we don't want to filter things.  Consider the user writing:
                //
                //      dim i =<space>
                //
                // We'll bring up the completion list here (as VB has completion on <space>). 
                // If the user then types '3', we don't want to match against Int32.
                var filterTextStartsWithANumber = filterText.Length > 0 && char.IsNumber(filterText[0]);
                if (filterTextStartsWithANumber)
                {
                    if (!IsAfterDot(model, model.TriggerSnapshot, textSpanToText))
                    {
                        return null;
                    }
                }

                var effectiveFilterItemState = ComputeEffectiveFilterItemState(model);
                foreach (var currentItem in model.TotalItems)
                {
                    // Check if something new has happened and there's a later on filter operation
                    // in the chain.  If so, there's no need for us to do any more work (as it will
                    // just be superceded by the later work).
                    if (id != _filterId)
                    {
                        return model;
                    }

                    if (CompletionItemFilter.ShouldBeFilteredOutOfCompletionList(
                            currentItem, effectiveFilterItemState))
                    {
                        continue;
                    }

                    // Check if the item matches the filter text typed so far.
                    var matchesFilterText = MatchesFilterText(helper, currentItem, filterText, model.Trigger, filterReason, recentItems);

                    if (matchesFilterText)
                    {
                        filterResults.Add(new FilterResult(
                            currentItem, filterText, matchedFilterText: true));
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

                        var wasTriggeredByDeleteOrSimpleInvoke =
                            model.Trigger.Kind == CompletionTriggerKind.Deletion ||
                            model.Trigger.Kind == CompletionTriggerKind.Invoke;
                        var shouldKeepItem = filterText.Length <= 1 || wasTriggeredByDeleteOrSimpleInvoke;

                        if (shouldKeepItem)
                        {
                            filterResults.Add(new FilterResult(
                                currentItem, filterText, matchedFilterText: false));
                        }
                    }
                }

                model = model.WithFilterText(filterText);

                // If no items matched the filter text then determine what we should do.
                if (filterResults.Count == 0)
                {
                    return HandleAllItemsFilteredOut(model, filterReason);
                }

                // If this was deletion, then we control the entire behavior of deletion
                // ourselves.
                if (model.Trigger.Kind == CompletionTriggerKind.Deletion)
                {
                    return HandleDeletionTrigger(model, filterReason, filterResults);
                }

                return HandleNormalFiltering(
                    model, document, filterReason, caretPosition,
                    helper, recentItems, filterText, filterResults);
            }

            private static ImmutableDictionary<CompletionItemFilter, bool> ComputeEffectiveFilterItemState(Model model)
            {
                var filterState = model.FilterState;

                // If all the filters are on, or all the filters are off then we don't actually 
                // need to filter.
                if (filterState != null)
                {
                    if (filterState.Values.All(b => b) ||
                        filterState.Values.All(b => !b))
                    {
                        return null;
                    }
                }

                return filterState;
            }

            private bool IsAfterDot(Model model, ITextSnapshot textSnapshot, Dictionary<TextSpan, string> textSpanToText)
            {
                var originalSpan = model.OriginalList.Span;

                // Move the span back one character if possible.
                var span = Span.FromBounds(Math.Max(0, originalSpan.Start - 1), originalSpan.End);

                // Because we are adjusting the span, it's not safe to call
                // model.GetCurrentTextInSnapshot. GetCurrentTextInSnapshot starts by
                // mapping the span into the ViewBuffer. In debugger intellisense, if the
                // caret is at the start of the immediate/watch window, moving the span 
                // start backwards results in a span that is outside the view.

                // Since we just need to look at the Document's contents, it should
                // be safe to do this check by inspecting model.TriggerSnapshot
                var text = model.TriggerSnapshot.GetText(span);
                return text.Length > 0 && text[0] == '.';
            }

            private Model HandleNormalFiltering(
                Model model,
                Document document,
                CompletionFilterReason filterReason,
                SnapshotPoint caretPosition,
                CompletionHelper helper,
                ImmutableArray<string> recentItems,
                string filterText,
                List<FilterResult> filterResults)
            {
                // Not deletion.  Defer to the language to decide which item it thinks best
                // matches the text typed so far.

                // Ask the language to determine which of the *matched* items it wants to select.
                var service = this.Controller.GetCompletionService();
                if (service == null)
                {
                    return null;
                }

                var matchingCompletionItems = filterResults.Where(r => r.MatchedFilterText)
                                                           .Select(t => t.CompletionItem)
                                                           .AsImmutable();
                var chosenItems = service.FilterItems(
                    document, matchingCompletionItems, filterText);

                // Of the items the service returned, pick the one most recently committed
                var bestCompletionItem = GetBestCompletionItemBasedOnMRU(chosenItems, recentItems);

                // If we don't have a best completion item yet, then pick the first item from the list.
                var bestOrFirstCompletionItem = bestCompletionItem ?? filterResults.First().CompletionItem;

                var hardSelection = IsHardSelection(
                    model, bestOrFirstCompletionItem, caretPosition, helper, filterReason);

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

                var result = model.WithFilteredItems(filterResults.Select(r => r.CompletionItem).AsImmutable())
                                  .WithSelectedItem(bestOrFirstCompletionItem)
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

            private Model HandleDeletionTrigger(
                Model model, CompletionFilterReason filterReason, List<FilterResult> filterResults)
            {
                if (filterReason == CompletionFilterReason.Insertion &&
                    !filterResults.Any(r => r.MatchedFilterText))
                {
                    // The user has typed something, but nothing in the actual list matched what
                    // they were typing.  In this case, we want to dismiss completion entirely.
                    // The thought process is as follows: we aggressively brough up completion
                    // to help them when they typed delete (in case they wanted to pick another
                    // item).  However, they're typing something that doesn't seem to match at all
                    // The completion list is just distracting at this point.
                    return null;
                }

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
                model = model.WithFilteredItems(filteredItems);

                if (bestFilterResult != null)
                {
                    // Only hard select this result if it's a prefix match
                    // We need to do this so that 
                    // * deleting and retyping a dot in a member access does not change the 
                    //   text that originally appeared before the dot
                    // * deleting through a word from the end keeps that word selected
                    // This also preserves the behavior the VB had through Dev12.
                    var hardSelect = bestFilterResult.Value.CompletionItem.FilterText.StartsWith(model.FilterText, StringComparison.CurrentCultureIgnoreCase);
                    return model.WithSelectedItem(bestFilterResult.Value.CompletionItem)
                                .WithHardSelection(hardSelect)
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
                }
                return false;
            }

            private static Model HandleAllItemsFilteredOut(
                Model model,
                CompletionFilterReason filterReason)
            {
                if (model.DismissIfEmpty &&
                    filterReason == CompletionFilterReason.Insertion)
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
                    return model.WithFilteredItems(ImmutableArray<CompletionItem>.Empty)
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
                if (filterReason == CompletionFilterReason.Deletion &&
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

                    if (!recentItems.IsDefault && GetRecentItemIndex(recentItems, item) <= 0)
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

            private bool IsHardSelection(
                Model model,
                CompletionItem bestFilterMatch,
                SnapshotPoint caretPosition,
                CompletionHelper completionHelper,
                CompletionFilterReason reason)
            {
                if (bestFilterMatch == null || model.UseSuggestionMode)
                {
                    return false;
                }

                var textSnapshot = caretPosition.Snapshot;

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
                var itemViewSpan = model.GetViewBufferSpan(bestFilterMatch.Span);
                var fullFilterText = model.GetCurrentTextInSnapshot(itemViewSpan, textSnapshot, endPoint: null);

                var trigger = model.Trigger;
                var shouldSoftSelect = ShouldSoftSelectItem(bestFilterMatch, fullFilterText, trigger);
                if (shouldSoftSelect)
                {
                    return false;
                }

                // If the user moved the caret left after they started typing, the 'best' match may not match at all
                // against the full text span that this item would be replacing.
                if (!MatchesFilterText(completionHelper, bestFilterMatch, fullFilterText, trigger, reason, this.Controller.GetRecentItems()))
                {
                    return false;
                }

                // Switch to soft selection, if user moved caret to the start of a non-empty filter span.
                // This prevents commiting if user types a commit character at this position later, but 
                // still has the list if user types filter character
                // i.e. blah| -> |blah -> !|blah
                // We want the filter span non-empty because we still want hard selection in the following case:
                //
                //  A a = new |
                if (caretPosition == itemViewSpan.TextSpan.Start && itemViewSpan.TextSpan.Length > 0)
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
