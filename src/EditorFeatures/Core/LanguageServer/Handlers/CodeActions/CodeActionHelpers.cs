// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeActionOptions = Microsoft.CodeAnalysis.CodeActions.CodeActionOptions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    internal static class CodeActionHelpers
    {
        /// <summary>
        /// Get, order, and filter code actions, and then transform them into VSCodeActions.
        /// </summary>
        /// <remarks>
        /// Used by CodeActionsHandler.
        /// </remarks>
        public static async Task<VSInternalCodeAction[]> GetVSCodeActionsAsync(
            CodeActionParams request,
            CodeActionsCache codeActionsCache,
            Document document,
            CodeActionOptions options,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            CancellationToken cancellationToken)
        {
            var actionSets = await GetActionSetsAsync(
                document, options, codeFixService, codeRefactoringService, request.Range, cancellationToken).ConfigureAwait(false);
            if (actionSets.IsDefaultOrEmpty)
                return Array.Empty<VSInternalCodeAction>();

            await codeActionsCache.UpdateActionSetsAsync(document, request.Range, actionSets, cancellationToken).ConfigureAwait(false);
            var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Each suggested action set should have a unique set number, which is used for grouping code actions together.
            var currentHighestSetNumber = 0;

            using var _ = ArrayBuilder<VSInternalCodeAction>.GetInstance(out var codeActions);
            foreach (var set in actionSets)
            {
                var currentSetNumber = ++currentHighestSetNumber;
                foreach (var suggestedAction in set.Actions)
                {
                    // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
                    if (suggestedAction.OriginalCodeAction is CodeActionWithOptions)
                    {
                        continue;
                    }

                    // TO-DO: Re-enable code actions involving package manager once supported by LSP.
                    // https://github.com/dotnet/roslyn/issues/48698
                    if (suggestedAction.OriginalCodeAction.Tags.Equals(WellKnownTagArrays.NuGet))
                    {
                        continue;
                    }

                    codeActions.Add(GenerateVSCodeAction(
                        request, documentText,
                        suggestedAction: suggestedAction,
                        codeActionKind: GetCodeActionKindFromSuggestedActionCategoryName(set.CategoryName!),
                        setPriority: set.Priority,
                        applicableRange: set.ApplicableToSpan.HasValue ? ProtocolConversions.TextSpanToRange(set.ApplicableToSpan.Value, documentText) : null,
                        currentSetNumber: currentSetNumber,
                        currentHighestSetNumber: ref currentHighestSetNumber));
                }
            }

            return codeActions.ToArray();
        }

        private static VSInternalCodeAction GenerateVSCodeAction(
            CodeActionParams request,
            SourceText documentText,
            IUnifiedSuggestedAction suggestedAction,
            LSP.CodeActionKind codeActionKind,
            UnifiedSuggestedActionSetPriority setPriority,
            LSP.Range? applicableRange,
            int currentSetNumber,
            ref int currentHighestSetNumber,
            string currentTitle = "")
        {
            if (!string.IsNullOrEmpty(currentTitle))
            {
                // Adding a delimiter for nested code actions, e.g. 'Suppress or Configure issues|Suppress IDEXXXX|in Source'
                currentTitle += '|';
            }

            var codeAction = suggestedAction.OriginalCodeAction;
            currentTitle += codeAction.Title;

            var diagnosticsForFix = GetApplicableDiagnostics(request.Context, suggestedAction);

            // Nested code actions' unique identifiers consist of: parent code action unique identifier + '|' + title of code action
            var nestedActions = GenerateNestedVSCodeActions(request, documentText, suggestedAction, codeActionKind, ref currentHighestSetNumber, currentTitle);

            return new VSInternalCodeAction
            {
                Title = codeAction.Title,
                Kind = codeActionKind,
                Diagnostics = diagnosticsForFix,
                Children = nestedActions,
                Priority = UnifiedSuggestedActionSetPriorityToPriorityLevel(setPriority),
                Group = $"Roslyn{currentSetNumber}",
                ApplicableRange = applicableRange,
                Data = new CodeActionResolveData(currentTitle, codeAction.CustomTags, request.Range, request.TextDocument)
            };

            static VSInternalCodeAction[] GenerateNestedVSCodeActions(
                CodeActionParams request,
                SourceText documentText,
                IUnifiedSuggestedAction suggestedAction,
                CodeActionKind codeActionKind,
                ref int currentHighestSetNumber,
                string currentTitle)
            {
                if (suggestedAction is not UnifiedSuggestedActionWithNestedActions suggestedActionWithNestedActions)
                {
                    return Array.Empty<VSInternalCodeAction>();
                }

                using var _ = ArrayBuilder<VSInternalCodeAction>.GetInstance(out var nestedActions);
                foreach (var nestedActionSet in suggestedActionWithNestedActions.NestedActionSets)
                {
                    // Nested code action sets should each have a unique set number that is not yet assigned to any set.
                    var nestedSetNumber = ++currentHighestSetNumber;
                    foreach (var nestedSuggestedAction in nestedActionSet.Actions)
                    {
                        nestedActions.Add(GenerateVSCodeAction(
                            request, documentText, nestedSuggestedAction, codeActionKind, nestedActionSet.Priority,
                            applicableRange: nestedActionSet.ApplicableToSpan.HasValue
                                ? ProtocolConversions.TextSpanToRange(nestedActionSet.ApplicableToSpan.Value, documentText) : null,
                            nestedSetNumber, ref currentHighestSetNumber, currentTitle));
                    }
                }

                return nestedActions.ToArray();
            }

            static LSP.Diagnostic[]? GetApplicableDiagnostics(LSP.CodeActionContext context, IUnifiedSuggestedAction action)
            {
                if (action is UnifiedCodeFixSuggestedAction codeFixAction)
                {
                    // Associate the diagnostics from the request that match the diagnostic fixed by the code action by ID.
                    // The request diagnostics are already restricted to the code fix location by the request.
                    var diagnosticCodesFixedByAction = codeFixAction.CodeFix.Diagnostics.Select(d => d.Id);
                    using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var diagnosticsBuilder);
                    foreach (var requestDiagnostic in context.Diagnostics)
                    {
                        var diagnosticCode = requestDiagnostic.Code?.Value?.ToString();
                        if (diagnosticCodesFixedByAction.Contains(diagnosticCode))
                        {
                            diagnosticsBuilder.Add(requestDiagnostic);
                        }
                    }

                    return diagnosticsBuilder.ToArray();
                }

                return null;
            }
        }

        /// <summary>
        /// Get, order, and filter code actions.
        /// </summary>
        /// <remarks>
        /// Used by CodeActionResolveHandler and RunCodeActionHandler.
        /// </remarks>
        public static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            CodeActionsCache codeActionsCache,
            Document document,
            LSP.Range selection,
            CodeActionOptions options,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            CancellationToken cancellationToken)
        {
            var actionSets = await GetActionSetsAsync(
                document, options, codeFixService, codeRefactoringService, selection, cancellationToken).ConfigureAwait(false);
            if (actionSets.IsDefaultOrEmpty)
                return ImmutableArray<CodeAction>.Empty;

            await codeActionsCache.UpdateActionSetsAsync(document, selection, actionSets, cancellationToken).ConfigureAwait(false);

            var _ = ArrayBuilder<CodeAction>.GetInstance(out var codeActions);
            foreach (var set in actionSets)
            {
                foreach (var suggestedAction in set.Actions)
                {
                    // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
                    if (suggestedAction.OriginalCodeAction is CodeActionWithOptions)
                    {
                        continue;
                    }

                    codeActions.Add(GetNestedActionsFromActionSet(suggestedAction));
                }
            }

            return codeActions.ToImmutable();
        }

        /// <summary>
        /// Generates a code action with its nested actions properly set.
        /// </summary>
        private static CodeAction GetNestedActionsFromActionSet(IUnifiedSuggestedAction suggestedAction)
        {
            var codeAction = suggestedAction.OriginalCodeAction;
            if (suggestedAction is not UnifiedSuggestedActionWithNestedActions suggestedActionWithNestedActions)
            {
                return codeAction;
            }

            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var nestedActions);
            foreach (var actionSet in suggestedActionWithNestedActions.NestedActionSets)
            {
                foreach (var action in actionSet.Actions)
                {
                    nestedActions.Add(GetNestedActionsFromActionSet(action));
                }
            }

            return new CodeActionWithNestedActions(
                codeAction.Title, nestedActions.ToImmutable(), codeAction.IsInlinable, codeAction.Priority);
        }

        private static async ValueTask<ImmutableArray<UnifiedSuggestedActionSet>> GetActionSetsAsync(
            Document document,
            CodeActionOptions options,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            LSP.Range selection,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(selection, text);

            var codeFixes = await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                document.Project.Solution.Workspace, codeFixService, document, textSpan,
                CodeActionRequestPriority.None,
                options, addOperationScope: _ => null, cancellationToken).ConfigureAwait(false);

            var codeRefactorings = await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                document.Project.Solution.Workspace, codeRefactoringService, document, textSpan, CodeActionRequestPriority.None, options,
                addOperationScope: _ => null, filterOutsideSelection: false, cancellationToken).ConfigureAwait(false);

            var actionSets = UnifiedSuggestedActionsSource.FilterAndOrderActionSets(
                codeFixes, codeRefactorings, textSpan, currentActionCount: 0);
            return actionSets;
        }

        private static CodeActionKind GetCodeActionKindFromSuggestedActionCategoryName(string categoryName)
            => categoryName switch
            {
                UnifiedPredefinedSuggestedActionCategoryNames.CodeFix => CodeActionKind.QuickFix,
                UnifiedPredefinedSuggestedActionCategoryNames.Refactoring => CodeActionKind.Refactor,
                UnifiedPredefinedSuggestedActionCategoryNames.StyleFix => CodeActionKind.QuickFix,
                UnifiedPredefinedSuggestedActionCategoryNames.ErrorFix => CodeActionKind.QuickFix,
                _ => throw ExceptionUtilities.UnexpectedValue(categoryName)
            };

        private static LSP.VSInternalPriorityLevel? UnifiedSuggestedActionSetPriorityToPriorityLevel(UnifiedSuggestedActionSetPriority priority)
            => priority switch
            {
                UnifiedSuggestedActionSetPriority.Lowest => LSP.VSInternalPriorityLevel.Lowest,
                UnifiedSuggestedActionSetPriority.Low => LSP.VSInternalPriorityLevel.Low,
                UnifiedSuggestedActionSetPriority.Medium => LSP.VSInternalPriorityLevel.Normal,
                UnifiedSuggestedActionSetPriority.High => LSP.VSInternalPriorityLevel.High,
                _ => throw ExceptionUtilities.UnexpectedValue(priority)
            };

        public static CodeAction? GetCodeActionToResolve(string distinctTitle, ImmutableArray<CodeAction> codeActions)
        {
            // Searching for the matching code action. We compare against the unique identifier
            // (e.g. "Suppress or Configure issues|Configure IDExxxx|Warning") instead of the
            // code action's title (e.g. "Warning") since there's a chance that multiple code
            // actions may have the same title (e.g. there could be multiple code actions with
            // the title "Warning" that appear in the code action menu if there are multiple
            // diagnostics on the same line).
            foreach (var c in codeActions)
            {
                var action = CheckForMatchingAction(c, distinctTitle);
                if (action != null)
                {
                    return action;
                }
            }

            return null;
        }

        private static CodeAction? CheckForMatchingAction(CodeAction codeAction, string goalTitle, string currentTitle = "")
        {
            // If the unique identifier of the current code action matches the unique identifier of the code action
            // we're looking for, return the code action. If not, check to see if one of the current code action's
            // nested actions may be a match.

            if (!string.IsNullOrEmpty(currentTitle))
            {
                // Adding a delimiter for nested code actions, e.g. 'Suppress or Configure issues.Suppress IDEXXXX|in Source'
                currentTitle += '|';
            }

            currentTitle += codeAction.Title;
            if (currentTitle == goalTitle)
            {
                return codeAction;
            }

            foreach (var nestedAction in codeAction.NestedCodeActions)
            {
                var match = CheckForMatchingAction(nestedAction, goalTitle, currentTitle);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
