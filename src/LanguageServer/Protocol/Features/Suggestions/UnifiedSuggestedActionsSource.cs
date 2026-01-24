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
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.Suggestions;

using CodeFixGroupKey = (DiagnosticData diagnostic, CodeActionPriority firstPriority, CodeActionPriority? secondPriority);

/// <summary>
/// Provides mutual code action logic for both local and LSP scenarios
/// via intermediate interface <see cref="SuggestedAction"/>.
/// </summary>
internal sealed class UnifiedSuggestedActionsSource
{
    /// <summary>
    /// Gets, filters, and orders code fixes.
    /// </summary>
    public static async ValueTask<ImmutableArray<SuggestedActionSet>> GetFilterAndOrderCodeFixesAsync(
        ICodeFixService codeFixService,
        TextDocument document,
        TextSpan selection,
        CodeActionRequestPriority? priority,
        CancellationToken cancellationToken)
    {
        // Intentionally switch to a threadpool thread to compute fixes.  We do not want to accidentally run any of
        // this on the UI thread and potentially allow any code to take a dependency on that.
        await TaskScheduler.Default;
        var fixes = await codeFixService.GetFixesAsync(
            document,
            selection,
            priority,
            cancellationToken).ConfigureAwait(false);

        var filteredFixes = fixes.WhereAsArray(c => c.Fixes.Length > 0);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var organizedFixes = await OrganizeFixesAsync(document.Project, text, filteredFixes, cancellationToken).ConfigureAwait(false);

        return organizedFixes;
    }

    /// <summary>
    /// Arrange fixes into groups based on the issue (diagnostic being fixed) and prioritize these groups.
    /// </summary>
    private static async Task<ImmutableArray<SuggestedActionSet>> OrganizeFixesAsync(
        Project project,
        SourceText text,
        ImmutableArray<CodeFixCollection> fixCollections,
        CancellationToken cancellationToken)
    {
        var map = ImmutableDictionary.CreateBuilder<CodeFixGroupKey, IList<SuggestedAction>>();
        using var _ = ArrayBuilder<CodeFixGroupKey>.GetInstance(out var order);

        // First group fixes by diagnostic and priority.
        await GroupFixesAsync(project, fixCollections, map, order, cancellationToken).ConfigureAwait(false);

        // Then prioritize between the groups.
        var prioritizedFixes = PrioritizeFixGroups(text, map.ToImmutable(), order.ToImmutable());
        return prioritizedFixes;
    }

    /// <summary>
    /// Groups fixes by the diagnostic being addressed by each fix.
    /// </summary>
    private static async Task GroupFixesAsync(
        Project project,
        ImmutableArray<CodeFixCollection> fixCollections,
        IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
        ArrayBuilder<CodeFixGroupKey> order,
        CancellationToken cancellationToken)
    {
        foreach (var fixCollection in fixCollections)
            await ProcessFixCollectionAsync(project, map, order, fixCollection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task ProcessFixCollectionAsync(
        Project project,
        IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
        ArrayBuilder<CodeFixGroupKey> order,
        CodeFixCollection fixCollection,
        CancellationToken cancellationToken)
    {
        var fixes = fixCollection.Fixes;
        var fixCount = fixes.Length;

        var nonSupressionCodeFixes = fixes.WhereAsArray(f => !IsTopLevelSuppressionAction(f.Action));
        var supressionCodeFixes = fixes.WhereAsArray(f => IsTopLevelSuppressionAction(f.Action));

        await AddCodeActionsAsync(project, map, order, fixCollection, GetFlavorsAsync, nonSupressionCodeFixes).ConfigureAwait(false);

        // Add suppression fixes to the end of a given SuggestedActionSet so that they
        // always show up last in a group.
        await AddCodeActionsAsync(project, map, order, fixCollection, GetFlavorsAsync, supressionCodeFixes).ConfigureAwait(false);

        return;

        // Local functions
        Task<SuggestedActionFlavors?> GetFlavorsAsync(CodeAction codeAction)
            => GetUnifiedSuggestedActionFlavorsAsync(
                codeAction, fixCount, fixCollection.FixAllState, fixCollection.SupportedScopes, fixCollection.Diagnostics, cancellationToken);
    }

    private static async Task AddCodeActionsAsync(
        Project project,
        IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
        ArrayBuilder<CodeFixGroupKey> order,
        CodeFixCollection fixCollection,
        Func<CodeAction, Task<SuggestedActionFlavors?>> getFixAllSuggestedActionSetAsync,
        ImmutableArray<CodeFix> codeFixes)
    {
        foreach (var fix in codeFixes)
        {
            var unifiedSuggestedAction = await GetUnifiedSuggestedActionAsync(project, fix.Action, fix).ConfigureAwait(false);
            AddFix(project, fix, unifiedSuggestedAction, map, order);
        }

        return;

        // Local functions
        async Task<SuggestedAction> GetUnifiedSuggestedActionAsync(Project project, CodeAction action, CodeFix fix)
        {
            if (action.NestedActions.Length > 0)
            {
                var unifiedNestedActions = new FixedSizeArrayBuilder<SuggestedAction>(action.NestedActions.Length);
                foreach (var nestedAction in action.NestedActions)
                {
                    var unifiedNestedAction = await GetUnifiedSuggestedActionAsync(project, nestedAction, fix).ConfigureAwait(false);
                    unifiedNestedActions.Add(unifiedNestedAction);
                }

                var set = new SuggestedActionSet(
                    categoryName: null,
                    actions: unifiedNestedActions.MoveToImmutable(),
                    title: null,
                    priority: action.Priority,
                    applicableToSpan: fix.Diagnostics.First().Location.SourceSpan);

                return SuggestedAction.CreateWithNestedActionSets(
                    action, action.Priority, fixCollection.Provider, codeRefactoringKind: null, diagnostics: [], [set]);
            }
            else
            {
                return SuggestedAction.CreateWithFlavors(
                    action, action.Priority, fixCollection.Provider, codeRefactoringKind: null, fix.Diagnostics,
                    await getFixAllSuggestedActionSetAsync(action).ConfigureAwait(false));
            }
        }
    }

    private static void AddFix(
        Project project,
        CodeFix fix,
        SuggestedAction suggestedAction,
        IDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
        ArrayBuilder<CodeFixGroupKey> order)
    {
        var groupKey = GetGroupKey(fix, project);
        if (!map.TryGetValue(groupKey, out var suggestedActions))
        {
            order.Add(groupKey);
            suggestedActions = ImmutableArray.CreateBuilder<SuggestedAction>();
            map[groupKey] = suggestedActions;
        }

        suggestedActions.Add(suggestedAction);
        return;

        static CodeFixGroupKey GetGroupKey(CodeFix fix, Project project)
        {
            var diagnosticData = DiagnosticData.Create(fix.Diagnostics.First(), project);
            if (fix.Action is AbstractConfigurationActionWithNestedActions configurationAction)
            {
                return new CodeFixGroupKey(
                    diagnosticData, configurationAction.Priority, configurationAction.AdditionalPriority);
            }

            return new CodeFixGroupKey(diagnosticData, fix.Action.Priority, null);
        }
    }

    // If the provided fix all context is non-null and the context's code action Id matches
    // the given code action's Id, returns the set of fix all occurrences actions associated
    // with the code action.
    private static async Task<SuggestedActionFlavors?> GetUnifiedSuggestedActionFlavorsAsync(
        CodeAction action,
        int actionCount,
        IRefactorOrFixAllState? fixAllState,
        ImmutableArray<FixAllScope> supportedScopes,
        ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (fixAllState == null)
            return null;

        if (actionCount > 1 && action.EquivalenceKey == null)
            return null;

        if (diagnostics is not [var firstDiagnostic, ..])
            return null;

        var textDocument = fixAllState.Document;
        using var _ = ArrayBuilder<SuggestedAction>.GetInstance(out var fixAllSuggestedActions);
        foreach (var scope in supportedScopes)
        {
            if (scope is FixAllScope.ContainingMember or FixAllScope.ContainingType)
            {
                if (textDocument is not Document document)
                    continue;

                // Skip showing ContainingMember and ContainingType FixAll scopes if the language
                // does not implement 'IFixAllSpanMappingService' langauge service or
                // we have no mapped FixAll spans to fix.

                var spanMappingService = document.GetLanguageService<IFixAllSpanMappingService>();
                if (spanMappingService is null)
                    continue;

                var documentsAndSpans = await spanMappingService.GetFixAllSpansAsync(
                    document, firstDiagnostic.Location.SourceSpan, scope, cancellationToken).ConfigureAwait(false);
                if (documentsAndSpans.IsEmpty)
                    continue;
            }

            var fixAllStateForScope = fixAllState.With(scope: scope, codeActionEquivalenceKey: action.EquivalenceKey);
            var fixAllSuggestedAction = SuggestedAction.CreateRefactorOrFixAll(
                action, action.Priority, codeRefactoringKind: null, diagnostics, fixAllStateForScope);

            fixAllSuggestedActions.Add(fixAllSuggestedAction);
        }

        return new(CodeFixesResources.Fix_all_occurrences_in, fixAllSuggestedActions.ToImmutableAndClear());
    }

    /// <summary>
    /// Return prioritized set of fix groups such that fix group for suppression always show up at the bottom of the list.
    /// </summary>
    /// <remarks>
    /// Fix groups are returned in priority order determined based on <see cref="ExtensionOrderAttribute"/>.
    /// Priority for all <see cref="SuggestedActionSet"/>s containing fixes is set to <see
    /// cref="CodeActionPriority.Default"/> by default. The only exception is the case where a <see
    /// cref="SuggestedActionSet"/> only contains suppression fixes - the priority of such <see
    /// cref="SuggestedActionSet"/>s is set to <see cref="CodeActionPriority.Lowest"/> so that suppression
    /// fixes always show up last after all other fixes (and refactorings) for the selected line of code.
    /// </remarks>
    private static ImmutableArray<SuggestedActionSet> PrioritizeFixGroups(
        SourceText text,
        ImmutableDictionary<CodeFixGroupKey, IList<SuggestedAction>> map,
        ImmutableArray<CodeFixGroupKey> order)
    {
        using var _1 = ArrayBuilder<SuggestedActionSet>.GetInstance(out var nonSuppressionSets);
        using var _2 = ArrayBuilder<SuggestedActionSet>.GetInstance(out var suppressionSets);
        using var _3 = ArrayBuilder<SuggestedAction>.GetInstance(out var bulkConfigurationActions);

        foreach (var groupKey in order)
        {
            var actions = map[groupKey];

            var nonSuppressionActions = actions.WhereAsArray(a => !IsTopLevelSuppressionAction(a.CodeAction));
            AddUnifiedSuggestedActionsSet(text, nonSuppressionActions, groupKey, nonSuppressionSets);

            var suppressionActions = actions.WhereAsArray(a => IsTopLevelSuppressionAction(a.CodeAction) &&
                !IsBulkConfigurationAction(a.CodeAction));
            AddUnifiedSuggestedActionsSet(text, suppressionActions, groupKey, suppressionSets);

            bulkConfigurationActions.AddRange(actions.Where(a => IsBulkConfigurationAction(a.CodeAction)));
        }

        var sets = nonSuppressionSets.ToImmutable();

        // Append bulk configuration fixes at the end of suppression/configuration fixes.
        if (bulkConfigurationActions.Count > 0)
        {
            var bulkConfigurationSet = new SuggestedActionSet(
                UnifiedPredefinedSuggestedActionCategoryNames.CodeFix,
                bulkConfigurationActions.ToImmutable(),
                title: null,
                priority: CodeActionPriority.Lowest,
                applicableToSpan: null);
            suppressionSets.Add(bulkConfigurationSet);
        }

        if (suppressionSets.Count > 0)
        {
            // Wrap the suppression/configuration actions within another top level suggested action
            // to avoid clutter in the light bulb menu.
            var suppressOrConfigureCodeAction = NoChangeAction.Create(CodeFixesResources.Suppress_or_configure_issues, nameof(CodeFixesResources.Suppress_or_configure_issues));

            // Just pass along the provider belonging to the first action that we're offering to suppress/configure.
            // This doesn't actually get used as this top level action is just a container for the nested actions
            // and is never invoked itself.
            var provider = suppressionSets[0].Actions[0].Provider;
            var wrappingSuggestedAction = SuggestedAction.CreateWithNestedActionSets(
                suppressOrConfigureCodeAction,
                suppressOrConfigureCodeAction.Priority,
                provider,
                codeRefactoringKind: null,
                diagnostics: [],
                suppressionSets.ToImmutable());

            // Combine the spans and the category of each of the nested suggested actions
            // to get the span and category for the new top level suggested action.
            var (span, category) = CombineSpansAndCategory(suppressionSets);
            var wrappingSet = new SuggestedActionSet(
                category,
                actions: [wrappingSuggestedAction],
                title: CodeFixesResources.Suppress_or_configure_issues,
                priority: CodeActionPriority.Lowest,
                applicableToSpan: span);
            sets = sets.Add(wrappingSet);
        }

        return sets;

        // Local functions
        static (TextSpan? span, string category) CombineSpansAndCategory(ArrayBuilder<SuggestedActionSet> sets)
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
        SourceText text,
        ImmutableArray<SuggestedAction> actions,
        CodeFixGroupKey groupKey,
        ArrayBuilder<SuggestedActionSet> sets)
    {
        foreach (var group in actions.GroupBy(a => a.CodeActionPriority))
        {
            var priority = group.Key;

            // diagnostic from things like build shouldn't reach here since we don't support LB for those diagnostics
            var category = GetFixCategory(groupKey.diagnostic.Severity);
            sets.Add(new SuggestedActionSet(
                category,
                [.. group],
                title: null,
                priority,
                applicableToSpan: groupKey.diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text)));
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
                throw ExceptionUtilities.Unreachable();
        }
    }

    private static bool IsTopLevelSuppressionAction(CodeAction action)
        => action is AbstractConfigurationActionWithNestedActions;

    private static bool IsBulkConfigurationAction(CodeAction action)
        => (action as AbstractConfigurationActionWithNestedActions)?.IsBulkConfigurationAction == true;

    /// <summary>
    /// Gets, filters, and orders code refactorings.
    /// </summary>
    public static async Task<ImmutableArray<SuggestedActionSet>> GetFilterAndOrderCodeRefactoringsAsync(
        ICodeRefactoringService codeRefactoringService,
        TextDocument document,
        TextSpan selection,
        CodeActionRequestPriority? priority,
        bool filterOutsideSelection,
        CancellationToken cancellationToken)
    {
        // Intentionally switch to a threadpool thread to compute fixes.  We do not want to accidentally run any of
        // this on the UI thread and potentially allow any code to take a dependency on that.
        await TaskScheduler.Default;
        var refactorings = await codeRefactoringService.GetRefactoringsAsync(
            document, selection, priority,
            cancellationToken).ConfigureAwait(false);

        var filteredRefactorings = FilterOnAnyThread(refactorings, selection, filterOutsideSelection);

        var orderedRefactorings = new FixedSizeArrayBuilder<SuggestedActionSet>(filteredRefactorings.Length);
        foreach (var refactoring in filteredRefactorings)
        {
            var orderedRefactoring = await OrganizeRefactoringsAsync(document, selection, refactoring, cancellationToken).ConfigureAwait(false);
            orderedRefactorings.Add(orderedRefactoring);
        }

        return orderedRefactorings.MoveToImmutable();
    }

    private static ImmutableArray<CodeRefactoring> FilterOnAnyThread(
        ImmutableArray<CodeRefactoring> refactorings,
        TextSpan selection,
        bool filterOutsideSelection)
        => [.. refactorings.Select(r => FilterOnAnyThread(r, selection, filterOutsideSelection)).WhereNotNull()];

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
                : new CodeRefactoring(refactoring.Provider, actions, refactoring.FixAllProviderInfo);

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
    /// Priority for all <see cref="SuggestedActionSet"/>s containing refactorings is set to
    /// <see cref="CodeActionPriority.Low"/> and should show up after fixes but before
    /// suppression fixes in the light bulb menu.
    /// </remarks>
    private static async Task<SuggestedActionSet> OrganizeRefactoringsAsync(
        TextDocument document,
        TextSpan selection,
        CodeRefactoring refactoring,
        CancellationToken cancellationToken)
    {
        var refactoringSuggestedActions = new FixedSizeArrayBuilder<SuggestedAction>(refactoring.CodeActions.Length);

        foreach (var (action, applicableToSpan) in refactoring.CodeActions)
        {
            var unifiedActionSet = await GetUnifiedSuggestedActionSetAsync(action, applicableToSpan, selection, cancellationToken).ConfigureAwait(false);
            refactoringSuggestedActions.Add(unifiedActionSet);
        }

        var actions = refactoringSuggestedActions.MoveToImmutable();

        // An action set:
        // - gets the the same priority as the highest priority action within in.
        // - gets `applicableToSpan` of the first action:
        //   - E.g. the `applicableToSpan` closest to current selection might be a more correct
        //     choice. All actions created by one Refactoring have usually the same `applicableSpan`
        //     and therefore the complexity of determining the closest one isn't worth the benefit
        //     of slightly more correct orderings in certain edge cases.
        return new SuggestedActionSet(
            categoryName: UnifiedPredefinedSuggestedActionCategoryNames.Refactoring,
            actions,
            title: null,
            priority: actions.Max(a => a.CodeActionPriority),
            refactoring.CodeActions.FirstOrDefault().applicableToSpan);

        // Local functions
        async Task<SuggestedAction> GetUnifiedSuggestedActionSetAsync(CodeAction codeAction, TextSpan? applicableToSpan, TextSpan selection, CancellationToken cancellationToken)
        {
            if (codeAction.NestedActions.Length > 0)
            {
                var nestedActions = new FixedSizeArrayBuilder<SuggestedAction>(codeAction.NestedActions.Length);
                foreach (var nestedAction in codeAction.NestedActions)
                {
                    var unifiedAction = await GetUnifiedSuggestedActionSetAsync(nestedAction, applicableToSpan, selection, cancellationToken).ConfigureAwait(false);
                    nestedActions.Add(unifiedAction);
                }

                var set = new SuggestedActionSet(
                    categoryName: null,
                    actions: nestedActions.MoveToImmutable(),
                    title: null,
                    priority: codeAction.Priority,
                    applicableToSpan: applicableToSpan);

                return SuggestedAction.CreateWithNestedActionSets(
                    codeAction, codeAction.Priority, refactoring.Provider, codeRefactoringKind: null, diagnostics: [], [set]);
            }
            else
            {
                var fixAllSuggestedActionSet = await GetUnifiedFixAllSuggestedActionSetAsync(codeAction,
                    refactoring.CodeActions.Length, document as Document, selection, refactoring.Provider,
                    refactoring.FixAllProviderInfo, cancellationToken).ConfigureAwait(false);

                return SuggestedAction.CreateWithFlavors(
                    codeAction, codeAction.Priority, refactoring.Provider, refactoring.Provider.Kind,
                    diagnostics: [], fixAllSuggestedActionSet);
            }
        }
    }

    // If the provided fix all context is non-null and the context's code action Id matches
    // the given code action's Id, returns the set of fix all occurrences actions associated
    // with the code action.
    private static async Task<SuggestedActionFlavors?> GetUnifiedFixAllSuggestedActionSetAsync(
        CodeAction action,
        int actionCount,
        Document? document,
        TextSpan selection,
        CodeRefactoringProvider provider,
        FixAllProviderInfo? fixAllProviderInfo,
        CancellationToken cancellationToken)
    {
        if (fixAllProviderInfo == null || document == null)
            return null;

        // If the provider registered more than one code action, but provided a null equivalence key
        // we have no way to distinguish between which registered actions to apply or ignore for FixAll.
        // So, we just bail out for this case.
        if (actionCount > 1 && action.EquivalenceKey == null)
            return null;

        using var _ = ArrayBuilder<SuggestedAction>.GetInstance(out var fixAllSuggestedActions);
        foreach (var scope in fixAllProviderInfo.SupportedScopes)
        {
            var fixAllState = new RefactorAllState(
                (RefactorAllProvider)fixAllProviderInfo.FixAllProvider,
                document, selection, provider, scope.ToRefactorAllScope(), action);

            if (scope is FixAllScope.ContainingMember or FixAllScope.ContainingType)
            {
                // Skip showing ContainingMember and ContainingType FixAll scopes if the language
                // does not implement 'IFixAllSpanMappingService' langauge service or
                // we have no mapped FixAll spans to fix.
                var documentsAndSpans = await fixAllState.GetRefactorAllSpansAsync(cancellationToken).ConfigureAwait(false);
                if (documentsAndSpans.IsEmpty)
                    continue;
            }

            var fixAllSuggestedAction = SuggestedAction.CreateRefactorOrFixAll(
                action, action.Priority, provider.Kind, diagnostics: [], fixAllState);

            fixAllSuggestedActions.Add(fixAllSuggestedAction);
        }

        return new(CodeFixesResources.Fix_all_occurrences_in, fixAllSuggestedActions.ToImmutableAndClear());
    }

    /// <summary>
    /// Filters and orders the code fix sets and code refactoring sets amongst each other.
    /// Should be called with the results from <see cref="GetFilterAndOrderCodeFixesAsync"/>
    /// and <see cref="GetFilterAndOrderCodeRefactoringsAsync"/>.
    /// </summary>
    public static ImmutableArray<SuggestedActionSet> FilterAndOrderActionSets(
        ImmutableArray<SuggestedActionSet> fixes,
        ImmutableArray<SuggestedActionSet> refactorings,
        TextSpan? selectionOpt,
        int currentActionCount)
    {
        // Get the initial set of action sets, with refactorings and fixes appropriately
        // ordered against each other.
        var result = GetInitiallyOrderedActionSets(selectionOpt, fixes, refactorings);
        if (result.IsEmpty)
            return [];

        // Now that we have the entire set of action sets, inline, sort and filter
        // them appropriately against each other.
        var allActionSets = InlineActionSetsIfDesirable(result, currentActionCount);
        var orderedActionSets = OrderActionSets(allActionSets, selectionOpt);
        var filteredSets = FilterActionSetsByTitle(orderedActionSets);

        return filteredSets;
    }

    private static ImmutableArray<SuggestedActionSet> GetInitiallyOrderedActionSets(
        TextSpan? selectionOpt,
        ImmutableArray<SuggestedActionSet> fixes,
        ImmutableArray<SuggestedActionSet> refactorings)
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
            return [.. refactorings, .. fixes];
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
                s => s.Priority == CodeActionPriority.High);
            var nonHighPriRefactorings = refactorings.WhereAsArray(
                s => s.Priority != CodeActionPriority.High)
                    .SelectAsArray(s => WithPriority(s, CodeActionPriority.Low));

            var highPriFixes = fixes.WhereAsArray(s => s.Priority == CodeActionPriority.High);
            var nonHighPriFixes = fixes.WhereAsArray(s => s.Priority != CodeActionPriority.High);

            return [.. highPriFixes, .. highPriRefactorings, .. nonHighPriFixes, .. nonHighPriRefactorings];
        }
    }

    private static ImmutableArray<SuggestedActionSet> OrderActionSets(
        ImmutableArray<SuggestedActionSet> actionSets, TextSpan? selectionOpt)
    {
        return [.. actionSets.OrderByDescending(s => s.Priority).ThenBy(s => s, new UnifiedSuggestedActionSetComparer(selectionOpt))];
    }

    private static SuggestedActionSet WithPriority(
        SuggestedActionSet set, CodeActionPriority priority)
        => new(set.CategoryName, set.Actions, set.Title, priority, set.ApplicableToSpan);

    private static ImmutableArray<SuggestedActionSet> InlineActionSetsIfDesirable(
        ImmutableArray<SuggestedActionSet> actionSets,
        int currentActionCount)
    {
        // If we only have a single set of items, and that set only has three max suggestion
        // offered. Then we can consider inlining any nested actions into the top level list.
        // (but we only do this if the parent of the nested actions isn't invokable itself).
        return currentActionCount + actionSets.Sum(a => a.Actions.Length) > 3
            ? actionSets
            : actionSets.SelectAsArray(InlineActions);
    }

    private static SuggestedActionSet InlineActions(SuggestedActionSet actionSet)
    {
        using var newActionsDisposer = ArrayBuilder<SuggestedAction>.GetInstance(out var newActions);
        foreach (var action in actionSet.Actions)
        {
            // Only inline if the underlying code action allows it.
            if (action is { CodeAction.IsInlinable: true, NestedActionSets.Length: > 0 })
            {
                newActions.AddRange(action.NestedActionSets.SelectMany(set => set.Actions));
            }
            else
            {
                newActions.Add(action);
            }
        }

        return new SuggestedActionSet(
            actionSet.CategoryName,
            newActions.ToImmutable(),
            actionSet.Title,
            actionSet.Priority,
            actionSet.ApplicableToSpan);
    }

    private static ImmutableArray<SuggestedActionSet> FilterActionSetsByTitle(
        ImmutableArray<SuggestedActionSet> allActionSets)
    {
        using var resultDisposer = ArrayBuilder<SuggestedActionSet>.GetInstance(out var result);
        var seenTitles = new HashSet<string>();

        foreach (var set in allActionSets)
        {
            var filteredSet = FilterActionSetByTitle(set, seenTitles);
            if (filteredSet != null)
            {
                result.Add(filteredSet);
            }
        }

        return result.ToImmutableAndClear();
    }

    private static SuggestedActionSet? FilterActionSetByTitle(SuggestedActionSet set, HashSet<string> seenTitles)
    {
        using var _ = ArrayBuilder<SuggestedAction>.GetInstance(out var actions);

        foreach (var action in set.Actions)
        {
            if (seenTitles.Add(action.CodeAction.Title))
            {
                actions.Add(action);
            }
        }

        return actions.Count == 0
            ? null
            : new SuggestedActionSet(set.CategoryName, actions.ToImmutable(), set.Title, set.Priority, set.ApplicableToSpan);
    }
}
