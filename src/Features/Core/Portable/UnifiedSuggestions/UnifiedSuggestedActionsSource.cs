// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeFixGroupKey = System.Tuple<Microsoft.CodeAnalysis.Diagnostics.DiagnosticData, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority?>;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Provides mutual code action logic for both local and LSP scenarios
    /// via intermediate interface <see cref="IUnifiedSuggestedAction"/>.
    /// </summary>
    internal partial class UnifiedSuggestedActionsSource
    {
        /// <summary>
        /// Gets, filters, and orders code fixes.
        /// </summary>
        public static ValueTask<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeFixesAsync(
                Workspace workspace,
                ICodeFixService codeFixService,
                Document document,
                TextSpan selection,
                CodeActionRequestPriority priority,
                CodeActionOptions options,
                Func<string, IDisposable?> addOperationScope,
                CancellationToken cancellationToken)
            => CodeFixesComputer.GetFilterAndOrderCodeFixesAsync(workspace, codeFixService, document, selection, priority, options, addOperationScope, cancellationToken);

        /// <summary>
        /// Gets, filters, and orders code refactorings.
        /// </summary>
        public static Task<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeRefactoringsAsync(
                Workspace workspace,
                ICodeRefactoringService codeRefactoringService,
                Document document,
                TextSpan selection,
                CodeActionRequestPriority priority,
                CodeActionOptions options,
                Func<string, IDisposable?> addOperationScope,
                bool filterOutsideSelection,
                CancellationToken cancellationToken)
            => CodeRefactoringsComputer.GetFilterAndOrderCodeRefactoringsAsync(workspace, codeRefactoringService, document, selection, priority, options, addOperationScope, filterOutsideSelection, cancellationToken);

        private static UnifiedSuggestedActionSetPriority GetUnifiedSuggestedActionSetPriority(CodeActionPriority key)
            => key switch
            {
                CodeActionPriority.Lowest => UnifiedSuggestedActionSetPriority.Lowest,
                CodeActionPriority.Low => UnifiedSuggestedActionSetPriority.Low,
                CodeActionPriority.Medium => UnifiedSuggestedActionSetPriority.Medium,
                CodeActionPriority.High => UnifiedSuggestedActionSetPriority.High,
                _ => throw new InvalidOperationException(),
            };

        /// <summary>
        /// Filters and orders the code fix sets and code refactoring sets amongst each other.
        /// Should be called with the results from <see cref="GetFilterAndOrderCodeFixesAsync"/>
        /// and <see cref="GetFilterAndOrderCodeRefactoringsAsync"/>.
        /// </summary>
        public static ImmutableArray<UnifiedSuggestedActionSet> FilterAndOrderActionSets(
            ImmutableArray<UnifiedSuggestedActionSet> fixes,
            ImmutableArray<UnifiedSuggestedActionSet> refactorings,
            TextSpan? selectionOpt,
            int currentActionCount)
        {
            // Get the initial set of action sets, with refactorings and fixes appropriately
            // ordered against each other.
            var result = GetInitiallyOrderedActionSets(selectionOpt, fixes, refactorings);
            if (result.IsEmpty)
                return ImmutableArray<UnifiedSuggestedActionSet>.Empty;

            // Now that we have the entire set of action sets, inline, sort and filter
            // them appropriately against each other.
            var allActionSets = InlineActionSetsIfDesirable(result, currentActionCount);
            var orderedActionSets = OrderActionSets(allActionSets, selectionOpt);
            var filteredSets = FilterActionSetsByTitle(orderedActionSets);

            return filteredSets;
        }

        private static ImmutableArray<UnifiedSuggestedActionSet> GetInitiallyOrderedActionSets(
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
                var highPriRefactorings = refactorings.WhereAsArray(
                    s => s.Priority == UnifiedSuggestedActionSetPriority.High);
                var nonHighPriRefactorings = refactorings.WhereAsArray(
                    s => s.Priority != UnifiedSuggestedActionSetPriority.High)
                        .SelectAsArray(s => WithPriority(s, UnifiedSuggestedActionSetPriority.Low));

                var highPriFixes = fixes.WhereAsArray(s => s.Priority == UnifiedSuggestedActionSetPriority.High);
                var nonHighPriFixes = fixes.WhereAsArray(s => s.Priority != UnifiedSuggestedActionSetPriority.High);

                return highPriFixes.Concat(highPriRefactorings).Concat(nonHighPriFixes).Concat(nonHighPriRefactorings);
            }
        }

        private static ImmutableArray<UnifiedSuggestedActionSet> OrderActionSets(
            ImmutableArray<UnifiedSuggestedActionSet> actionSets, TextSpan? selectionOpt)
        {
            return actionSets.OrderByDescending(s => s.Priority)
                             .ThenBy(s => s, new UnifiedSuggestedActionSetComparer(selectionOpt))
                             .ToImmutableArray();
        }

        private static UnifiedSuggestedActionSet WithPriority(
            UnifiedSuggestedActionSet set, UnifiedSuggestedActionSetPriority priority)
            => new(set.CategoryName, set.Actions, set.Title, priority, set.ApplicableToSpan);

        private static ImmutableArray<UnifiedSuggestedActionSet> InlineActionSetsIfDesirable(
            ImmutableArray<UnifiedSuggestedActionSet> actionSets,
            int currentActionCount)
        {
            // If we only have a single set of items, and that set only has three max suggestion
            // offered. Then we can consider inlining any nested actions into the top level list.
            // (but we only do this if the parent of the nested actions isn't invokable itself).
            return currentActionCount + actionSets.Sum(a => a.Actions.Count()) > 3
                ? actionSets
                : actionSets.SelectAsArray(InlineActions);
        }

        private static UnifiedSuggestedActionSet InlineActions(UnifiedSuggestedActionSet actionSet)
        {
            using var newActionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var newActions);
            foreach (var action in actionSet.Actions)
            {
                var actionWithNestedActions = action as UnifiedSuggestedActionWithNestedActions;

                // Only inline if the underlying code action allows it.
                if (actionWithNestedActions?.OriginalCodeAction.IsInlinable == true)
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

        private static ImmutableArray<UnifiedSuggestedActionSet> FilterActionSetsByTitle(
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
                if (seenTitles.Add(action.OriginalCodeAction.Title))
                {
                    actions.Add(action);
                }
            }

            return actions.Count == 0
                ? null
                : new UnifiedSuggestedActionSet(set.CategoryName, actions.ToImmutable(), set.Title, set.Priority, set.ApplicableToSpan);
        }
    }
}
