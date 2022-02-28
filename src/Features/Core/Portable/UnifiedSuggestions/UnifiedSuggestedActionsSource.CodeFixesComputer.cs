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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeFixGroupKey = System.Tuple<Microsoft.CodeAnalysis.Diagnostics.DiagnosticData, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority, Microsoft.CodeAnalysis.CodeActions.CodeActionPriority?>;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal partial class UnifiedSuggestedActionsSource
    {
        /// <summary>
        /// Gets, filters, and orders code fixes.
        /// </summary>
        private static class CodeFixesComputer
        {
            public static async ValueTask<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeFixesAsync(
                Workspace workspace,
                ICodeFixService codeFixService,
                Document document,
                TextSpan selection,
                CodeActionRequestPriority priority,
                CodeActionOptions options,
                Func<string, IDisposable?> addOperationScope,
                CancellationToken cancellationToken)
            {
                // Intentionally switch to a threadpool thread to compute fixes.  We do not want to accidentally
                // run any of this on the UI thread and potentially allow any code to take a dependency on that.
                var fixes = await Task.Run(() => codeFixService.GetFixesAsync(
                    document,
                    selection,
                    priority,
                    options,
                    addOperationScope,
                    cancellationToken), cancellationToken).ConfigureAwait(false);

                var filteredFixes = fixes.WhereAsArray(c => c.Fixes.Length > 0);
                var organizedFixes = OrganizeFixes(workspace, filteredFixes);

                return organizedFixes;
            }

            /// <summary>
            /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
            /// </summary>
            private static ImmutableArray<UnifiedSuggestedActionSet> OrganizeFixes(
                Workspace workspace,
                ImmutableArray<CodeFixCollection> fixCollections)
            {
                var map = ImmutableDictionary.CreateBuilder<CodeFixGroupKey, IList<IUnifiedSuggestedAction>>();
                using var _ = ArrayBuilder<CodeFixGroupKey>.GetInstance(out var order);

                // First group fixes by diagnostic and priority.
                GroupFixes(workspace, fixCollections, map, order);

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
               IDictionary<CodeFixGroupKey, IList<IUnifiedSuggestedAction>> map,
               ArrayBuilder<CodeFixGroupKey> order)
            {
                foreach (var fixCollection in fixCollections)
                    ProcessFixCollection(workspace, map, order, fixCollection);
            }

            private static void ProcessFixCollection(
                Workspace workspace,
                IDictionary<CodeFixGroupKey, IList<IUnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order,
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
                AddCodeActions(workspace, map, order, fixCollection,
                    GetFixAllSuggestedActionSet, supressionCodeFixes);

                return;

                // Local functions
                UnifiedSuggestedActionSet? GetFixAllSuggestedActionSet(CodeAction codeAction)
                    => GetUnifiedFixAllSuggestedActionSet(
                        codeAction, fixCount, fixCollection.FixAllState,
                        fixCollection.SupportedScopes, fixCollection.FirstDiagnostic,
                        workspace);
            }

            private static void AddCodeActions(
                Workspace workspace, IDictionary<CodeFixGroupKey, IList<IUnifiedSuggestedAction>> map,
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
                IUnifiedSuggestedAction GetUnifiedSuggestedAction(CodeAction action, CodeFix fix)
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
                CodeFix fix, IUnifiedSuggestedAction suggestedAction,
                IDictionary<CodeFixGroupKey, IList<IUnifiedSuggestedAction>> map,
                ArrayBuilder<CodeFixGroupKey> order)
            {
                var groupKey = GetGroupKey(fix);
                if (!map.ContainsKey(groupKey))
                {
                    order.Add(groupKey);
                    map[groupKey] = ImmutableArray.CreateBuilder<IUnifiedSuggestedAction>();
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

                using var fixAllSuggestedActionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var fixAllSuggestedActions);
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
                ImmutableDictionary<CodeFixGroupKey, IList<IUnifiedSuggestedAction>> map,
                ImmutableArray<CodeFixGroupKey> order,
                Workspace workspace)
            {
                using var _1 = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var nonSuppressionSets);
                using var _2 = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(out var suppressionSets);
                using var _3 = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var bulkConfigurationActions);

                foreach (var groupKey in order)
                {
                    var actions = map[groupKey];

                    var nonSuppressionActions = actions.Where(a => !IsTopLevelSuppressionAction(a.OriginalCodeAction)).ToImmutableArray();
                    AddUnifiedSuggestedActionsSet(nonSuppressionActions, groupKey, nonSuppressionSets);

                    var suppressionActions = actions.Where(a => IsTopLevelSuppressionAction(a.OriginalCodeAction) &&
                        !IsBulkConfigurationAction(a.OriginalCodeAction)).ToImmutableArray();
                    AddUnifiedSuggestedActionsSet(suppressionActions, groupKey, suppressionSets);

                    bulkConfigurationActions.AddRange(actions.Where(a => IsBulkConfigurationAction(a.OriginalCodeAction)));
                }

                var sets = nonSuppressionSets.ToImmutable();

                // Append bulk configuration fixes at the end of suppression/configuration fixes.
                if (bulkConfigurationActions.Count > 0)
                {
                    var bulkConfigurationSet = new UnifiedSuggestedActionSet(
                        UnifiedPredefinedSuggestedActionCategoryNames.CodeFix,
                        bulkConfigurationActions.ToImmutable(),
                        title: null,
                        priority: UnifiedSuggestedActionSetPriority.Lowest,
                        applicableToSpan: null);
                    suppressionSets.Add(bulkConfigurationSet);
                }

                if (suppressionSets.Count > 0)
                {
                    // Wrap the suppression/configuration actions within another top level suggested action
                    // to avoid clutter in the light bulb menu.
                    var suppressOrConfigureCodeAction = new NoChangeAction(CodeFixesResources.Suppress_or_Configure_issues, nameof(CodeFixesResources.Suppress_or_Configure_issues));
                    var wrappingSuggestedAction = new UnifiedSuggestedActionWithNestedActions(
                        workspace, codeAction: suppressOrConfigureCodeAction,
                        codeActionPriority: suppressOrConfigureCodeAction.Priority, provider: null,
                        nestedActionSets: suppressionSets.ToImmutable());

                    // Combine the spans and the category of each of the nested suggested actions
                    // to get the span and category for the new top level suggested action.
                    var (span, category) = CombineSpansAndCategory(suppressionSets);
                    var wrappingSet = new UnifiedSuggestedActionSet(
                        category,
                        actions: ImmutableArray.Create<IUnifiedSuggestedAction>(wrappingSuggestedAction),
                        title: CodeFixesResources.Suppress_or_Configure_issues,
                        priority: UnifiedSuggestedActionSetPriority.Lowest,
                        applicableToSpan: span);
                    sets = sets.Add(wrappingSet);
                }

                return sets;

                // Local functions
                static (TextSpan? span, string category) CombineSpansAndCategory(ArrayBuilder<UnifiedSuggestedActionSet> sets)
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

                        Debug.Assert(set.CategoryName is UnifiedPredefinedSuggestedActionCategoryNames.CodeFix or
                                     UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix);

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
                ImmutableArray<IUnifiedSuggestedAction> actions,
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
                        category, group.ToImmutableArray(), title: null, priority, applicableToSpan: groupKey.Item1.GetTextSpan()));
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
        }
    }
}
