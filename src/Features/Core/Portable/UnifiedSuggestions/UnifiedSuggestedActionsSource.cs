﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal class UnifiedSuggestedActionsSource
    {
        /// <summary>
        /// Gets, filters, and orders code fixes.
        /// </summary>
        public static async Task<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeFixesAsync(
            Workspace workspace,
            ICodeFixService codeFixService,
            Document document,
            TextSpan selection,
            bool includeSuppressionFixes,
            bool isBlocking,
            Func<string, IDisposable?> addOperationScope,
            CancellationToken cancellationToken)
        {
            // It may seem strange that we kick off a task, but then immediately 'Wait' on 
            // it. However, it's deliberate.  We want to make sure that the code runs on 
            // the background so that no one takes an accidentally dependency on running on 
            // the UI thread.
            var fixes = await Task.Run(
                () => codeFixService.GetFixesAsync(
                document, selection, includeSuppressionFixes, isBlocking,
                addOperationScope, cancellationToken), cancellationToken).ConfigureAwait(false);

            var filteredFixes = fixes.WhereAsArray(c => c.Fixes.Length > 0);
            var organizedFixes = OrganizeFixes(workspace, filteredFixes, includeSuppressionFixes);

            return organizedFixes;
        }

        /// <summary>
        /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
        /// </summary>
        private static ImmutableArray<UnifiedSuggestedActionSet> OrganizeFixes(
            Workspace workspace,
            ImmutableArray<CodeFixCollection> fixCollections,
            bool includeSuppressionFixes)
        {
            var map = ImmutableDictionary.CreateBuilder<CodeFixGroupKey, IList<UnifiedSuggestedAction>>();
            using var _ = ArrayBuilder<CodeFixGroupKey>.GetInstance(out var order);

            // First group fixes by diagnostic and priority.
            GroupFixes(workspace, fixCollections, map, order, includeSuppressionFixes);

            // Then prioritize between the groups.
            var prioritizedFixes = PrioritizeFixGroups(map.ToImmutable(), order.ToImmutable(), workspace);
            return prioritizedFixes;
        }

        /// <summary>
        /// Groups fixes by the diagnostic being addressed by each fix.
        /// </summary>
        private static void GroupFixes(
           Workspace workspace,
           ImmutableArray<CodeFixCollection> fixCollections,
           IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
           ArrayBuilder<CodeFixGroupKey> order,
           bool includeSuppressionFixes)
        {
            foreach (var fixCollection in fixCollections)
            {
                ProcessFixCollection(
                    workspace, map, order, includeSuppressionFixes, fixCollection);
            }
        }

        private static void ProcessFixCollection(
            Workspace workspace,
            IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
            ArrayBuilder<CodeFixGroupKey> order,
            bool includeSuppressionFixes,
            CodeFixCollection fixCollection)
        {
            var fixes = fixCollection.Fixes;
            var fixCount = fixes.Length;

            var nonSupressionCodeFixes = fixes.WhereAsArray(f => !IsTopLevelSuppressionAction(f.Action));
            var supressionCodeFixes = fixes.WhereAsArray(f => IsTopLevelSuppressionAction(f.Action));

            AddCodeActions(workspace, map, order, fixCollection,
                GetFixAllSuggestedActionSet, nonSupressionCodeFixes);

            // Add suppression fixes to the end of a given SuggestedActionSet so that they
            // always show up last in a group.
            if (includeSuppressionFixes)
            {
                AddCodeActions(workspace, map, order, fixCollection,
                    GetFixAllSuggestedActionSet, supressionCodeFixes);
            }

            return;

            // Local functions
            UnifiedSuggestedActionSet? GetFixAllSuggestedActionSet(CodeAction codeAction)
                => GetUnifiedFixAllSuggestedActionSet(
                    codeAction, fixCount, fixCollection.FixAllState,
                    fixCollection.SupportedScopes, fixCollection.FirstDiagnostic,
                    workspace);
        }

        private static void AddCodeActions(
            Workspace workspace, IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
            ArrayBuilder<CodeFixGroupKey> order, CodeFixCollection fixCollection,
            Func<CodeAction, UnifiedSuggestedActionSet?> getFixAllSuggestedActionSet,
            ImmutableArray<CodeFix> codeFixes)
        {
            foreach (var fix in codeFixes)
            {
                var unifiedSuggestedAction = GetUnifiedSuggestedAction(fix.Action, fix);
                AddFix(fix, unifiedSuggestedAction, map, order);
            }

            return;

            // Local functions
            UnifiedSuggestedAction GetUnifiedSuggestedAction(CodeAction action, CodeFix fix)
            {
                if (action.NestedCodeActions.Length > 0)
                {
                    var nestedActions = action.NestedCodeActions.SelectAsArray(
                        nestedAction => GetUnifiedSuggestedAction(nestedAction, fix));

                    var set = new UnifiedSuggestedActionSet(
                        categoryName: null,
                        actions: nestedActions,
                        title: null,
                        priority: GetUnifiedSuggestedActionSetPriority(action.Priority),
                        applicableToSpan: fix.PrimaryDiagnostic.Location.SourceSpan);

                    return new UnifiedSuggestedActionWithNestedActions(
                        workspace, action, action.Priority, fixCollection.Provider, ImmutableArray.Create(set));
                }
                else
                {
                    return new UnifiedCodeFixSuggestedAction(
                        workspace, action, action.Priority, fix, fixCollection.Provider,
                        getFixAllSuggestedActionSet(action));
                }
            }
        }

        private static void AddFix(
            CodeFix fix, UnifiedSuggestedAction suggestedAction,
            IDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
            ArrayBuilder<CodeFixGroupKey> order)
        {
            var groupKey = GetGroupKey(fix);
            if (!map.ContainsKey(groupKey))
            {
                order.Add(groupKey);
                map[groupKey] = ImmutableArray.CreateBuilder<UnifiedSuggestedAction>();
            }

            map[groupKey].Add(suggestedAction);
            return;

            static CodeFixGroupKey GetGroupKey(CodeFix fix)
            {
                var diag = fix.GetPrimaryDiagnosticData();
                if (fix.Action is AbstractConfigurationActionWithNestedActions configurationAction)
                {
                    return new CodeFixGroupKey(
                        diag, configurationAction.Priority, configurationAction.AdditionalPriority);
                }

                return new CodeFixGroupKey(diag, fix.Action.Priority, null);
            }
        }

        // If the provided fix all context is non-null and the context's code action Id matches
        // the given code action's Id, returns the set of fix all occurrences actions associated
        // with the code action.
        private static UnifiedSuggestedActionSet? GetUnifiedFixAllSuggestedActionSet(
            CodeAction action,
            int actionCount,
            FixAllState fixAllState,
            ImmutableArray<FixAllScope> supportedScopes,
            Diagnostic firstDiagnostic,
            Workspace workspace)
        {

            if (fixAllState == null)
            {
                return null;
            }

            if (actionCount > 1 && action.EquivalenceKey == null)
            {
                return null;
            }

            using var fixAllSuggestedActionsDisposer = ArrayBuilder<UnifiedFixAllSuggestedAction>.GetInstance(
                out var fixAllSuggestedActions);
            foreach (var scope in supportedScopes)
            {
                var fixAllStateForScope = fixAllState.With(scope: scope, codeActionEquivalenceKey: action.EquivalenceKey);
                var fixAllSuggestedAction = new UnifiedFixAllSuggestedAction(
                    workspace, action, action.Priority, fixAllStateForScope, firstDiagnostic);

                fixAllSuggestedActions.Add(fixAllSuggestedAction);
            }

            return new UnifiedSuggestedActionSet(
                categoryName: null,
                actions: fixAllSuggestedActions.ToImmutable(),
                title: CodeFixesResources.Fix_all_occurrences_in,
                priority: UnifiedSuggestedActionSetPriority.Lowest,
                applicableToSpan: null);
        }

        /// <summary>
        /// Return prioritized set of fix groups such that fix group for suppression always show up at the bottom of the list.
        /// </summary>
        /// <remarks>
        /// Fix groups are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
        /// Priority for all <see cref="UnifiedSuggestedActionSet"/>s containing fixes is set to
        /// <see cref="UnifiedSuggestedActionSetPriority.Medium"/> by default.
        /// The only exception is the case where a <see cref="UnifiedSuggestedActionSet"/> only contains suppression fixes -
        /// the priority of such <see cref="UnifiedSuggestedActionSet"/>s is set to
        /// <see cref="UnifiedSuggestedActionSetPriority.Lowest"/> so that suppression fixes
        /// always show up last after all other fixes (and refactorings) for the selected line of code.
        /// </remarks>
        private static ImmutableArray<UnifiedSuggestedActionSet> PrioritizeFixGroups(
            ImmutableDictionary<CodeFixGroupKey, IList<UnifiedSuggestedAction>> map,
            ImmutableArray<CodeFixGroupKey> order,
            Workspace workspace)
        {
            using var _1 = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var nonSuppressionSets);
            using var _2 = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var suppressionSets);
            using var _3 = ArrayBuilder<UnifiedSuggestedAction>.GetInstance(out var bulkConfigurationActions);

            foreach (var groupKey in order)
            {
                var actions = map[groupKey];

                var nonSuppressionActions = actions.Where(a => !IsTopLevelSuppressionAction(a.OriginalCodeAction));
                AddUnifiedSuggestedActionsSet(nonSuppressionActions, groupKey, nonSuppressionSets);

                var suppressionActions = actions.Where(a => IsTopLevelSuppressionAction(a.OriginalCodeAction) &&
                    !IsBulkConfigurationAction(a.OriginalCodeAction));
                AddUnifiedSuggestedActionsSet(suppressionActions, groupKey, suppressionSets);

                bulkConfigurationActions.AddRange(actions.Where(a => IsBulkConfigurationAction(a.OriginalCodeAction)));
            }

            var sets = nonSuppressionSets.ToImmutable();

            // Append bulk configuration fixes at the end of suppression/configuration fixes.
            if (bulkConfigurationActions.Count > 0)
            {
                var bulkConfigurationSet = new UnifiedSuggestedActionSet(
                    UnifiedPredefinedSuggestedActionCategoryNames.CodeFix, bulkConfigurationActions.ToArray(),
                    title: null, priority: UnifiedSuggestedActionSetPriority.Lowest, applicableToSpan: null);
                suppressionSets.Add(bulkConfigurationSet);
            }

            if (suppressionSets.Count > 0)
            {
                // Wrap the suppression/configuration actions within another top level suggested action
                // to avoid clutter in the light bulb menu.
                var suppressOrConfigureCodeAction = new NoChangeAction(CodeFixesResources.Suppress_or_Configure_issues);
                var wrappingSuggestedAction = new UnifiedSuggestedActionWithNestedActions(
                    workspace, codeAction: suppressOrConfigureCodeAction,
                    codeActionPriority: suppressOrConfigureCodeAction.Priority, provider: null,
                    nestedActionSets: suppressionSets.ToImmutable());

                // Combine the spans and the category of each of the nested suggested actions
                // to get the span and category for the new top level suggested action.
                var (span, category) = CombineSpansAndCategory(suppressionSets);
                var wrappingSet = new UnifiedSuggestedActionSet(
                    category,
                    actions: SpecializedCollections.SingletonEnumerable(wrappingSuggestedAction),
                    title: CodeFixesResources.Suppress_or_Configure_issues,
                    priority: UnifiedSuggestedActionSetPriority.Lowest,
                    applicableToSpan: span);
                sets = sets.Add(wrappingSet);
            }

            return sets;

            // Local functions
            static (TextSpan? span, string category) CombineSpansAndCategory(IEnumerable<UnifiedSuggestedActionSet> sets)
            {
                // We are combining the spans and categories of the given set of suggested action sets
                // to generate a result span containing the spans of individual suggested action sets and
                // a result category which is the maximum severity category amongst the set
                var minStart = -1;
                var maxEnd = -1;
                var category = UnifiedPredefinedSuggestedActionCategoryNames.CodeFix;

                foreach (var set in sets)
                {
                    if (set.ApplicableToSpan.HasValue)
                    {
                        var currentStart = set.ApplicableToSpan.Value.Start;
                        var currentEnd = set.ApplicableToSpan.Value.End;

                        if (minStart == -1 || currentStart < minStart)
                        {
                            minStart = currentStart;
                        }

                        if (maxEnd == -1 || currentEnd > maxEnd)
                        {
                            maxEnd = currentEnd;
                        }
                    }

                    Debug.Assert(set.CategoryName == UnifiedPredefinedSuggestedActionCategoryNames.CodeFix ||
                                 set.CategoryName == UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix);

                    // If this set contains an error fix, then change the result category to ErrorFix
                    if (set.CategoryName == UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix)
                    {
                        category = UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix;
                    }
                }

                var combinedSpan = minStart >= 0 ? TextSpan.FromBounds(minStart, maxEnd) : (TextSpan?)null;
                return (combinedSpan, category);
            }
        }

        private static void AddUnifiedSuggestedActionsSet(
            IEnumerable<UnifiedSuggestedAction> actions,
            CodeFixGroupKey groupKey,
            ArrayBuilder<UnifiedSuggestedActionSet> sets)
        {
            foreach (var group in actions.GroupBy(a => a.CodeActionPriority))
            {
                var priority = GetUnifiedSuggestedActionSetPriority(group.Key);

                // diagnostic from things like build shouldn't reach here since we don't support LB for those diagnostics
                Debug.Assert(groupKey.Item1.HasTextSpan);
                var category = GetFixCategory(groupKey.Item1.Severity);
                sets.Add(new UnifiedSuggestedActionSet(
                    category, group, title: null, priority: priority, applicableToSpan: groupKey.Item1.GetTextSpan()));
            }
        }

        private static string GetFixCategory(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                case DiagnosticSeverity.Info:
                case DiagnosticSeverity.Warning:
                    return UnifiedPredefinedSuggestedActionCategoryNames.CodeFix;
                case DiagnosticSeverity.Error:
                    return UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool IsTopLevelSuppressionAction(CodeAction action)
            => action is AbstractConfigurationActionWithNestedActions;

        private static bool IsBulkConfigurationAction(CodeAction action)
            => (action as AbstractConfigurationActionWithNestedActions)?.IsBulkConfigurationAction == true;

        /// <summary>
        /// Gets, filters, and orders code refactorings.
        /// </summary>
        public static async Task<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeRefactoringsAsync(
            Workspace workspace,
            ICodeRefactoringService codeRefactoringService,
            Document document,
            TextSpan selection,
            bool isBlocking,
            Func<string, IDisposable?> addOperationScope,
            bool filterOutsideSelection,
            CancellationToken cancellationToken)
        {
            // It may seem strange that we kick off a task, but then immediately 'Wait' on 
            // it. However, it's deliberate.  We want to make sure that the code runs on 
            // the background so that no one takes an accidentally dependency on running on 
            // the UI thread.
            var refactorings = await Task.Run(
                () => codeRefactoringService.GetRefactoringsAsync(
                    document, selection, isBlocking, addOperationScope,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

            var filteredRefactorings = FilterOnAnyThread(refactorings, selection, filterOutsideSelection);

            return filteredRefactorings.SelectAsArray(
                r => OrganizeRefactorings(workspace, r));
        }

        private static ImmutableArray<CodeRefactoring> FilterOnAnyThread(
            ImmutableArray<CodeRefactoring> refactorings,
            TextSpan selection,
            bool filterOutsideSelection)
            => refactorings.Select(r => FilterOnAnyThread(r, selection, filterOutsideSelection)).WhereNotNull().ToImmutableArray();

        private static CodeRefactoring? FilterOnAnyThread(
            CodeRefactoring refactoring,
            TextSpan selection,
            bool filterOutsideSelection)
        {
            var actions = refactoring.CodeActions.WhereAsArray(IsActionAndSpanApplicable);
            return actions.Length == 0
                ? null
                : actions.Length == refactoring.CodeActions.Length
                    ? refactoring
                    : new CodeRefactoring(refactoring.Provider, actions);

            bool IsActionAndSpanApplicable((CodeAction action, TextSpan? applicableSpan) actionAndSpan)
            {
                if (filterOutsideSelection)
                {
                    // Filter out refactorings with applicable span outside the selection span.
                    if (!actionAndSpan.applicableSpan.HasValue ||
                        !selection.IntersectsWith(actionAndSpan.applicableSpan.Value))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Arrange refactorings into groups.
        /// </summary>
        /// <remarks>
        /// Refactorings are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
        /// Priority for all <see cref="UnifiedSuggestedActionSet"/>s containing refactorings is set to
        /// <see cref="UnifiedSuggestedActionSetPriority.Low"/> and should show up after fixes but before
        /// suppression fixes in the light bulb menu.
        /// </remarks>
        private static UnifiedSuggestedActionSet OrganizeRefactorings(Workspace workspace, CodeRefactoring refactoring)
        {
            using var refactoringSuggestedActionsDisposer = ArrayBuilder<UnifiedSuggestedAction>.GetInstance(
                out var refactoringSuggestedActions);

            foreach (var codeAction in refactoring.CodeActions)
            {
                if (codeAction.action.NestedCodeActions.Length > 0)
                {
                    var nestedActions = codeAction.action.NestedCodeActions.SelectAsArray(
                        na => new UnifiedCodeRefactoringSuggestedAction(
                            workspace, na, na.Priority, refactoring.Provider));

                    var set = new UnifiedSuggestedActionSet(
                        categoryName: null,
                        actions: nestedActions,
                        title: null,
                        priority: GetUnifiedSuggestedActionSetPriority(codeAction.action.Priority),
                        applicableToSpan: codeAction.applicableToSpan);

                    refactoringSuggestedActions.Add(
                        new UnifiedSuggestedActionWithNestedActions(
                            workspace, codeAction.action, codeAction.action.Priority, refactoring.Provider, ImmutableArray.Create(set)));
                }
                else
                {
                    refactoringSuggestedActions.Add(
                        new UnifiedCodeRefactoringSuggestedAction(
                            workspace, codeAction.action, codeAction.action.Priority, refactoring.Provider));
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
                title: null,
                priority: GetUnifiedSuggestedActionSetPriority(actions.Max(a => a.CodeActionPriority)),
                applicableToSpan: refactoring.CodeActions.FirstOrDefault().applicableToSpan);
        }

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
        public static ImmutableArray<UnifiedSuggestedActionSet>? FilterAndOrderActionSets(
            ImmutableArray<UnifiedSuggestedActionSet> fixes,
            ImmutableArray<UnifiedSuggestedActionSet> refactorings,
            TextSpan? selectionOpt)
        {
            // Get the initial set of action sets, with refactorings and fixes appropriately
            // ordered against each other.
            var result = GetInitiallyOrderedActionSets(selectionOpt, fixes, refactorings);
            if (result.IsEmpty)
            {
                return null;
            }

            // Now that we have the entire set of action sets, inline, sort and filter
            // them appropriately against each other.
            var allActionSets = InlineActionSetsIfDesirable(result);
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
