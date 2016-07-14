﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
                var allFilteredItems = new List<PresentationItem>();
                var textSpanToText = new Dictionary<TextSpan, string>();
                var helper = this.Controller.GetCompletionHelper();

                // isUnique tracks if there is a single 
                bool? isUnique = null;
                PresentationItem bestFilterMatch = null;
                bool filterTextIsPotentialIdentifier = false;

                var recentItems = this.Controller.GetRecentItems();

                var itemToFilterText = new Dictionary<CompletionItem, string>();
                model = model.WithCompletionItemToFilterText(itemToFilterText);

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

                    var filterText = model.GetCurrentTextInSnapshot(currentItem.Item.Span, textSnapshot, textSpanToText);
                    var matchesFilterText = MatchesFilterText(helper, currentItem.Item, filterText, model.Trigger, filterReason, recentItems);
                    itemToFilterText[currentItem.Item] = filterText;

                    if (matchesFilterText)
                    {
                        allFilteredItems.Add(currentItem);

                        // If we have no best match, or this match is better than the last match,
                        // then the current item is the best filter match.
                        if (bestFilterMatch == null ||
                            IsBetterFilterMatch(helper, currentItem.Item, bestFilterMatch.Item, filterText, model.Trigger, filterReason, recentItems))
                        {
                            bestFilterMatch = currentItem;
                        }

                        // If isUnique is null, then this is the first time we've seen an item that
                        // matches the filter text.  That item is now considered unique.  However, if
                        // isUnique is non-null, then this is the second (or third, or fourth, etc.)
                        // that a provider said to include. It's no longer unique.
                        //
                        // Note: We only want to do this if any filter text was actually provided.
                        // This is so we can handle the following cases properly:
                        //
                        //    Console.WriteLi$$
                        //
                        // If they try to commit unique item there, we want to commit to
                        // "WriteLine".  However, if they just have:
                        //
                        //    Console.$$
                        //
                        // And they try to commit unique item, we won't commit something just
                        // because it was in the MRU list.
                        if (filterText != string.Empty)
                        {
                            isUnique = isUnique == null || false;
                        }
                    }
                    else
                    {
                        if (filterText.Length <= 1)
                        {
                            // Even though the rule provider didn't match this, we'll still include it
                            // since we want to allow a user typing a single character and seeing all
                            // possibly completions.  However, we don't consider it either unique or a
                            // filter match, so we won't select it.
                            allFilteredItems.Add(currentItem);
                        }

                        // We want to dismiss the list if the user is typing a # and nothing matches
                        filterTextIsPotentialIdentifier = filterTextIsPotentialIdentifier ||
                            filterText.Length == 0 ||
                            (!char.IsDigit(filterText[0]) && filterText[0] != '-' && filterText[0] != '.');
                    }
                }

                if (!filterTextIsPotentialIdentifier && bestFilterMatch == null)
                {
                    // We had no matches, and the user is typing a #, dismiss the list
                    return null;
                }

                if (allFilteredItems.Count == 0)
                {
                    if (dismissIfEmptyAllowed &&
                        model.DismissIfEmpty &&
                        filterReason == CompletionFilterReason.TypeChar)
                    {
                        return null;
                    }

                    if (model.FilterState != null && model.FilterState.Values.Any(b => b))
                    {
                        // If the user has turned on some filtering states, and we filtered down to 
                        // nothing, then we do want the UI to show that to them.
                        return model.WithFilteredItems(allFilteredItems.ToImmutableArray())
                                    .WithHardSelection(false)
                                    .WithIsUnique(false);
                    }
                    else
                    {
                        // If we are going to filter everything out, then just preserve the existing
                        // model, but switch over to soft selection.  Also, nothing is unique at that
                        // point.
                        return model.WithHardSelection(false)
                                    .WithIsUnique(false);
                    }
                }

                // If we have a best item, then select it.  Otherwise just use the first item
                // in the list.
                var selectedItem = bestFilterMatch ?? allFilteredItems.First();

                // If we have a best item, then we want to hard select it.  Otherwise we want
                // soft selection.  However, no hard selection if there's a builder.
                var hardSelection = IsHardSelection(model, bestFilterMatch, textSnapshot, helper, model.Trigger, filterReason);

                var result = model.WithFilteredItems(allFilteredItems.ToImmutableArray())
                                  .WithSelectedItem(selectedItem)
                                  .WithHardSelection(hardSelection)
                                  .WithIsUnique(isUnique.HasValue && isUnique.Value);

                return result;
            }

            private static bool IsBetterFilterMatch(
                CompletionHelper helper, CompletionItem item1, CompletionItem item2,
                string filterText, CompletionTrigger trigger,
                CompletionFilterReason filterReason, ImmutableArray<string> recentItems)
            {
                // For the deletion we bake in the core logic for how betterness should work.
                // This way deletion feels the same across all languages that opt into deletion 
                // as a completion trigger.
                if (filterReason == CompletionFilterReason.BackspaceOrDelete)
                {
                    var prefixLength1 = item1.FilterText.GetCaseInsensitivePrefixLength(filterText);
                    var prefixLength2 = item2.FilterText.GetCaseInsensitivePrefixLength(filterText);

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

                return helper.IsBetterFilterMatch(item1, item2, filterText, trigger, recentItems);
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

                return helper.MatchesFilterText(item, filterText, trigger, recentItems);
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
                CompletionTrigger trigger,
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