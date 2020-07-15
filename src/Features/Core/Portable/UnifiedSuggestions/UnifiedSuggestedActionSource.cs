// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal class UnifiedSuggestedActionSource
    {
        // TO-DO: Change accessibility and ordering of methods

        /// <summary>
        /// Arrange refactorings into groups.
        /// </summary>
        /// <remarks>
        /// Refactorings are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
        /// Priority for all <see cref="UnifiedSuggestedActionSet"/>s containing refactorings is set to
        /// <see cref="UnifiedSuggestedActionSetPriority.Low"/> and should show up after fixes but before
        /// suppression fixes in the light bulb menu.
        /// </remarks>
        public static UnifiedSuggestedActionSet OrganizeRefactorings(
            Workspace workspace, CodeRefactoring refactoring)
        {
            using var refactoringSuggestedActionsDisposer = ArrayBuilder<UnifiedSuggestedAction>.GetInstance(
                out var refactoringSuggestedActions);

            foreach (var codeAction in refactoring.CodeActions)
            {
                if (codeAction.action.NestedCodeActions.Length > 0)
                {
                    var nestedActions = codeAction.action.NestedCodeActions.SelectAsArray(
                        na => new UnifiedCodeRefactoringSuggestedAction(
                            workspace, refactoring.Provider, na));

                    var set = new UnifiedSuggestedActionSet(
                        categoryName: null,
                        actions: nestedActions,
                        priority: GetUnifiedSuggestedActionSetPriority(codeAction.action.Priority),
                        applicableToSpan: codeAction.applicableToSpan);

                    refactoringSuggestedActions.Add(
                        new UnifiedSuggestedActionWithNestedActions(workspace, refactoring.Provider, codeAction.action, set));
                }
                else
                {
                    refactoringSuggestedActions.Add(
                        new UnifiedCodeRefactoringSuggestedAction(workspace, refactoring.Provider, codeAction.action));
                }
            }

            var actions = refactoringSuggestedActions.ToImmutable();

            // An action set:
            // - gets the the same priority as the highest priority action within in.
            // - gets `applicableToSpan` of the first action:
            //   - E.g. the `applicableToSpan` closest to current selection might be a more correct 
            //     choice. All actions created by one Refactoring have usually the same `applicableSpan`
            //     and therefore the complexity of determining the closest one isn't worth the benefit
            //     of slightly more correct orderings in certain edge cases.
            return new UnifiedSuggestedActionSet(
                UnifiedPredefinedSuggestedActionCategoryNames.Refactoring,
                actions: actions,
                priority: GetUnifiedSuggestedActionSetPriority(actions.Max(a => a.Priority)),
                applicableToSpan: refactoring.CodeActions.FirstOrDefault().applicableToSpan);
        }

        public static UnifiedSuggestedActionSetPriority GetUnifiedSuggestedActionSetPriority(CodeActionPriority key)
            => key switch
            {
                CodeActionPriority.None => UnifiedSuggestedActionSetPriority.None,
                CodeActionPriority.Low => UnifiedSuggestedActionSetPriority.Low,
                CodeActionPriority.Medium => UnifiedSuggestedActionSetPriority.Medium,
                CodeActionPriority.High => UnifiedSuggestedActionSetPriority.High,
                _ => throw new InvalidOperationException(),
            };

        public static ImmutableArray<UnifiedSuggestedActionSet> GetInitiallyOrderedActionSets(
            TextSpan? selectionOpt,
            ImmutableArray<UnifiedSuggestedActionSet> fixes,
            ImmutableArray<UnifiedSuggestedActionSet> refactorings)
        {
            // First, order refactorings based on the order the providers actually gave for
            // their actions. This way, a low pri refactoring always shows after a medium pri
            // refactoring, no matter what we do below.
            refactorings = OrderActionSets(refactorings, selectionOpt);

            // If there's a selection, it's likely the user is trying to perform some operation
            // directly on that operation (like 'extract method').  Prioritize refactorings over
            // fixes in that case.  Otherwise, it's likely that the user is just on some error
            // and wants to fix it (in which case, prioritize fixes).

            if (selectionOpt?.Length > 0)
            {
                // There was a selection.  Treat refactorings as more important than fixes.
                // Note: we still will sort after this.  So any high pri fixes will come to the
                // front.  Any low-pri refactorings will go to the end.
                return refactorings.Concat(fixes);
            }
            else
            {
                // No selection.  Treat all medium and low pri refactorings as low priority, and
                // place after fixes.  Even a low pri fixes will be above what was *originally*
                // a medium pri refactoring.
                //
                // Note: we do not do this for *high* pri refactorings (like 'rename').  These
                // are still very important and need to stay at the top (though still after high
                // pri fixes).
                var highPriRefactorings = refactorings.WhereAsArray(s => s.Priority == UnifiedSuggestedActionSetPriority.High);
                var nonHighPriRefactorings = refactorings.WhereAsArray(s => s.Priority != UnifiedSuggestedActionSetPriority.High)
                                                         .SelectAsArray(s => WithPriority(s, UnifiedSuggestedActionSetPriority.Low));

                var highPriFixes = fixes.WhereAsArray(s => s.Priority == UnifiedSuggestedActionSetPriority.High);
                var nonHighPriFixes = fixes.WhereAsArray(s => s.Priority != UnifiedSuggestedActionSetPriority.High);

                return highPriFixes.Concat(highPriRefactorings).Concat(nonHighPriFixes).Concat(nonHighPriRefactorings);
            }
        }

        private static UnifiedSuggestedActionSet WithPriority(UnifiedSuggestedActionSet set, UnifiedSuggestedActionSetPriority priority)
            => new UnifiedSuggestedActionSet(set.CategoryName, set.Actions, set.Title, priority, set.ApplicableToSpan);

        public static ImmutableArray<UnifiedSuggestedActionSet> OrderActionSets(
            ImmutableArray<UnifiedSuggestedActionSet> actionSets, TextSpan? selectionOpt)
        {
            return actionSets.OrderByDescending(s => s.Priority)
                             .ThenBy(s => s, new UnifiedSuggestedActionSetComparer(selectionOpt))
                             .ToImmutableArray();
        }

        public static ImmutableArray<UnifiedSuggestedActionSet> FilterActionSetsByTitle(
            ImmutableArray<UnifiedSuggestedActionSet> allActionSets)
        {
            using var resultDisposer = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var result);
            var seenTitles = new HashSet<string>();

            foreach (var set in allActionSets)
            {
                var filteredSet = FilterActionSetByTitle(set, seenTitles);
                if (filteredSet != null)
                {
                    result.Add(filteredSet);
                }
            }

            return result.ToImmutable();
        }

        private static UnifiedSuggestedActionSet? FilterActionSetByTitle(UnifiedSuggestedActionSet set, HashSet<string> seenTitles)
        {
            using var actionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var actions);

            foreach (var action in set.Actions)
            {
                if (seenTitles.Add(action.DisplayText))
                {
                    actions.Add(action);
                }
            }

            return actions.Count == 0
                ? null
                : new UnifiedSuggestedActionSet(set.CategoryName, actions.ToImmutable(), set.Title, set.Priority, set.ApplicableToSpan);
        }

        public static ImmutableArray<UnifiedSuggestedActionSet> InlineActionSetsIfDesirable(
            ImmutableArray<UnifiedSuggestedActionSet> allActionSets)
        {
            // If we only have a single set of items, and that set only has three max suggestion 
            // offered. Then we can consider inlining any nested actions into the top level list.
            // (but we only do this if the parent of the nested actions isn't invokable itself).
            if (allActionSets.Sum(a => a.Actions.Count()) > 3)
            {
                return allActionSets;
            }

            return allActionSets.SelectAsArray(InlineActions);
        }

        public static UnifiedSuggestedActionSet InlineActions(UnifiedSuggestedActionSet actionSet)
        {
            using var newActionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var newActions);
            foreach (var action in actionSet.Actions)
            {
                var actionWithNestedActions = action as UnifiedSuggestedActionWithNestedActions;

                // Only inline if the underlying code action allows it.
                if (actionWithNestedActions?.CodeAction.IsInlinable == true)
                {
                    newActions.AddRange(actionWithNestedActions.NestedActionSets.SelectMany(set => set.Actions));
                }
                else
                {
                    newActions.Add(action);
                }
            }

            return new UnifiedSuggestedActionSet(
                actionSet.CategoryName,
                newActions.ToImmutable(),
                actionSet.Title,
                actionSet.Priority,
                actionSet.ApplicableToSpan);
        }
    }
}
