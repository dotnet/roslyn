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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    /// <summary>
    /// Provides mutual code action logic for both local and LSP scenarios
    /// via intermediate interface <see cref="IUnifiedSuggestedAction"/>.
    /// </summary>
    internal partial class UnifiedSuggestedActionsSource
    {
        private static class CodeRefactoringsComputer
        {
            /// <summary>
            /// Gets, filters, and orders code refactorings.
            /// </summary>
            public static async Task<ImmutableArray<UnifiedSuggestedActionSet>> GetFilterAndOrderCodeRefactoringsAsync(
                Workspace workspace,
                ICodeRefactoringService codeRefactoringService,
                Document document,
                TextSpan selection,
                CodeActionRequestPriority priority,
                CodeActionOptions options,
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
                        document, selection, priority, options, addOperationScope,
                        cancellationToken), cancellationToken).ConfigureAwait(false);

                var filteredRefactorings = FilterOnAnyThread(refactorings, selection, filterOutsideSelection);

                _ = ArrayBuilder<UnifiedSuggestedActionSet>.GetInstance(filteredRefactorings.Length, out var orderedRefactorings);
                foreach (var refactoring in filteredRefactorings)
                {
                    var orderedRefactoring = await OrganizeRefactoringsAsync(workspace, document, selection, refactoring, cancellationToken).ConfigureAwait(false);
                    orderedRefactorings.Add(orderedRefactoring);
                }

                return orderedRefactorings.ToImmutable();
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
            /// Priority for all <see cref="UnifiedSuggestedActionSet"/>s containing refactorings is set to
            /// <see cref="UnifiedSuggestedActionSetPriority.Low"/> and should show up after fixes but before
            /// suppression fixes in the light bulb menu.
            /// </remarks>
            private static async Task<UnifiedSuggestedActionSet> OrganizeRefactoringsAsync(
                Workspace workspace,
                Document document,
                TextSpan selection,
                CodeRefactoring refactoring,
                CancellationToken cancellationToken)
            {
                using var refactoringSuggestedActionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var refactoringSuggestedActions);

                foreach (var (action, applicableToSpan) in refactoring.CodeActions)
                {
                    var unifiedActionSet = await GetUnifiedSuggestedActionSetAsync(action, applicableToSpan, selection, cancellationToken).ConfigureAwait(false);
                    refactoringSuggestedActions.Add(unifiedActionSet);
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

                // Local functions
                async Task<IUnifiedSuggestedAction> GetUnifiedSuggestedActionSetAsync(CodeAction codeAction, TextSpan? applicableToSpan, TextSpan selection, CancellationToken cancellationToken)
                {
                    if (codeAction.NestedCodeActions.Length > 0)
                    {
                        _ = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(codeAction.NestedCodeActions.Length, out var nestedActions);
                        foreach (var nestedAction in codeAction.NestedCodeActions)
                        {
                            var unifiedAction = await GetUnifiedSuggestedActionSetAsync(nestedAction, applicableToSpan, selection, cancellationToken).ConfigureAwait(false);
                            nestedActions.Add(unifiedAction);
                        }

                        var set = new UnifiedSuggestedActionSet(
                            categoryName: null,
                            actions: nestedActions.ToImmutable(),
                            title: null,
                            priority: GetUnifiedSuggestedActionSetPriority(codeAction.Priority),
                            applicableToSpan: applicableToSpan);

                        return new UnifiedSuggestedActionWithNestedActions(
                            workspace, codeAction, codeAction.Priority, refactoring.Provider, ImmutableArray.Create(set));
                    }
                    else
                    {
                        var fixAllSuggestedActionSet = await GetUnifiedFixAllSuggestedActionSetAsync(codeAction,
                            refactoring.CodeActions.Length, document, selection, refactoring.Provider,
                            refactoring.FixAllProviderInfo, workspace, cancellationToken).ConfigureAwait(false);

                        return new UnifiedCodeRefactoringSuggestedAction(
                                workspace, codeAction, codeAction.Priority, refactoring.Provider, fixAllSuggestedActionSet);
                    }
                }
            }

            // If the provided fix all context is non-null and the context's code action Id matches
            // the given code action's Id, returns the set of fix all occurrences actions associated
            // with the code action.
            private static async Task<UnifiedSuggestedActionSet?> GetUnifiedFixAllSuggestedActionSetAsync(
                CodeAction action,
                int actionCount,
                Document document,
                TextSpan selection,
                CodeRefactoringProvider provider,
                FixAllProviderInfo? fixAllProviderInfo,
                Workspace workspace,
                CancellationToken cancellationToken)
            {
                if (fixAllProviderInfo == null)
                {
                    return null;
                }

                if (actionCount > 1 && action.EquivalenceKey == null)
                {
                    return null;
                }

                using var fixAllSuggestedActionsDisposer = ArrayBuilder<IUnifiedSuggestedAction>.GetInstance(out var fixAllSuggestedActions);
                foreach (var scope in fixAllProviderInfo.SupportedScopes)
                {
                    if (scope == FixAllScope.Selection && selection.IsEmpty)
                        continue;

                    var fixAllSpan = await GetFixAllSpanForScopeAsync(scope).ConfigureAwait(false);
                    var fixAllState = new FixAllState(fixAllProviderInfo.FixAllProvider, document, provider, scope, fixAllSpan, action);
                    var fixAllSuggestedAction = new UnifiedFixAllCodeRefactoringSuggestedAction(
                        workspace, action, action.Priority, fixAllState);

                    fixAllSuggestedActions.Add(fixAllSuggestedAction);
                }

                return new UnifiedSuggestedActionSet(
                    categoryName: null,
                    actions: fixAllSuggestedActions.ToImmutable(),
                    title: CodeFixesResources.Fix_all_occurrences_in,
                    priority: UnifiedSuggestedActionSetPriority.Lowest,
                    applicableToSpan: null);

                // Local functions
                async Task<TextSpan?> GetFixAllSpanForScopeAsync(FixAllScope fixAllScope)
                {
                    return fixAllScope switch
                    {
                        FixAllScope.Selection => selection,
                        FixAllScope.ContainingMember or FixAllScope.ContainingType
                            => await GetSpanForContainingMemberOrTypeAsync(fixAllScope).ConfigureAwait(false),
                        _ => null,
                    };
                }

                async Task<TextSpan?> GetSpanForContainingMemberOrTypeAsync(FixAllScope fixAllScope)
                {
                    Contract.ThrowIfFalse(fixAllScope is FixAllScope.ContainingMember or FixAllScope.ContainingType);

                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                    var startContainer = fixAllScope == FixAllScope.ContainingMember
                        ? syntaxFacts.GetContainingMemberDeclaration(root, selection.Start)
                        : syntaxFacts.GetContainingTypeDeclaration(root, selection.Start);
                    if (selection.IsEmpty || startContainer == null)
                        return startContainer?.FullSpan;

                    var endContainer = fixAllScope == FixAllScope.ContainingMember
                        ? syntaxFacts.GetContainingMemberDeclaration(root, selection.End)
                        : syntaxFacts.GetContainingTypeDeclaration(root, selection.End);
                    if (startContainer == endContainer)
                        return startContainer.FullSpan;

                    return null;
                }
            }
        }
    }
}
