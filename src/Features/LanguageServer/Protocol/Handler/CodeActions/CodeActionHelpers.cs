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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnifiedSuggestions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    internal static class CodeActionHelpers
    {
        /// <summary>
        /// Get, order, and filter code actions, and then transform them into VSCodeActions or CodeActions based on <paramref name="hasVsLspCapability"/>.
        /// </summary>
        /// <remarks>
        /// Used by CodeActionsHandler.
        /// </remarks>
        public static async Task<LSP.CodeAction[]> GetVSCodeActionsAsync(
            CodeActionParams request,
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            bool hasVsLspCapability,
            CancellationToken cancellationToken)
        {
            var actionSets = await GetActionSetsAsync(
                document, fallbackOptions, codeFixService, codeRefactoringService, request.Range, cancellationToken).ConfigureAwait(false);
            if (actionSets.IsDefaultOrEmpty)
                return Array.Empty<LSP.CodeAction>();

            using var _ = ArrayBuilder<LSP.CodeAction>.GetInstance(out var codeActions);
            // VS-LSP support nested code action, but standard LSP doesn't.
            if (hasVsLspCapability)
            {
                var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // Each suggested action set should have a unique set number, which is used for grouping code actions together.
                var currentHighestSetNumber = 0;

                foreach (var set in actionSets)
                {
                    var currentSetNumber = ++currentHighestSetNumber;
                    foreach (var suggestedAction in set.Actions)
                    {
                        if (!IsCodeActionNotSupportedByLSP(suggestedAction))
                        {
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
                }
            }
            else
            {
                foreach (var set in actionSets)
                {
                    foreach (var suggestedAction in set.Actions)
                    {
                        if (!IsCodeActionNotSupportedByLSP(suggestedAction))
                        {
                            codeActions.AddRange(GenerateCodeActions(
                                request,
                                suggestedAction,
                                GetCodeActionKindFromSuggestedActionCategoryName(set.CategoryName!)));
                        }
                    }
                }
            }

            return codeActions.ToArray();
        }

        private static bool IsCodeActionNotSupportedByLSP(IUnifiedSuggestedAction suggestedAction)
            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            => suggestedAction.OriginalCodeAction is CodeActionWithOptions
            // Skip code actions that requires non-document changes.  We can't apply them in LSP currently.
            // https://github.com/dotnet/roslyn/issues/48698
            || suggestedAction.OriginalCodeAction.Tags.Contains(CodeAction.RequiresNonDocumentChange);

        /// <summary>
        /// Generate the matching code actions for <paramref name="suggestedAction"/>. If it contains nested code actions, flatten them into an array.
        /// </summary>
        private static LSP.CodeAction[] GenerateCodeActions(
            CodeActionParams request,
            IUnifiedSuggestedAction suggestedAction,
            LSP.CodeActionKind codeActionKind,
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

            using var _ = ArrayBuilder<LSP.CodeAction>.GetInstance(out var builder);
            if (suggestedAction is UnifiedSuggestedActionWithNestedActions unifiedSuggestedActions)
            {
                foreach (var actionSet in unifiedSuggestedActions.NestedActionSets)
                {
                    foreach (var action in actionSet.Actions)
                    {
                        // Filter the configure and suppress fixer if it is not VS LSP, because it would generate many nested code actions.
                        // Tracking issue: https://github.com/microsoft/language-server-protocol/issues/994 
                        if (action.OriginalCodeAction is not AbstractConfigurationActionWithNestedActions)
                        {
                            builder.AddRange(GenerateCodeActions(
                                request,
                                action,
                                codeActionKind,
                                currentTitle));
                        }
                    }
                }
            }
            else
            {
                builder.Add(new LSP.CodeAction
                {
                    // Change this to -> because it is shown to the user
                    Title = currentTitle.Replace("|", " -> "),
                    Kind = codeActionKind,
                    Diagnostics = diagnosticsForFix,
                    Data = new CodeActionResolveData(currentTitle, codeAction.CustomTags, request.Range, request.TextDocument)
                });
            }

            return builder.ToArray();
        }

        private static VSInternalCodeAction GenerateVSCodeAction(
            CodeActionParams request,
            SourceText documentText,
            IUnifiedSuggestedAction suggestedAction,
            LSP.CodeActionKind codeActionKind,
            CodeActionPriority setPriority,
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
        }

        private static LSP.Diagnostic[]? GetApplicableDiagnostics(CodeActionContext context, IUnifiedSuggestedAction action)
        {
            if (action is UnifiedCodeFixSuggestedAction codeFixAction && context.Diagnostics != null)
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

        /// <summary>
        /// Get, order, and filter code actions.
        /// </summary>
        /// <remarks>
        /// Used by CodeActionResolveHandler and RunCodeActionHandler.
        /// </remarks>
        public static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            Document document,
            LSP.Range selection,
            CodeActionOptionsProvider fallbackOptions,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            CancellationToken cancellationToken)
        {
            var actionSets = await GetActionSetsAsync(
                document, fallbackOptions, codeFixService, codeRefactoringService, selection, cancellationToken).ConfigureAwait(false);
            if (actionSets.IsDefaultOrEmpty)
                return ImmutableArray<CodeAction>.Empty;

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

            return CodeAction.Create(
                codeAction.Title, nestedActions.ToImmutable(), codeAction.IsInlinable, codeAction.Priority);
        }

        private static async ValueTask<ImmutableArray<UnifiedSuggestedActionSet>> GetActionSetsAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            LSP.Range selection,
            CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(selection, text);

            var codeFixes = await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeFixesAsync(
                document.Project.Solution.Workspace, codeFixService, document, textSpan,
                new DefaultCodeActionRequestPriorityProvider(),
                fallbackOptions, addOperationScope: _ => null, cancellationToken).ConfigureAwait(false);

            var codeRefactorings = await UnifiedSuggestedActionsSource.GetFilterAndOrderCodeRefactoringsAsync(
                document.Project.Solution.Workspace, codeRefactoringService, document, textSpan, priority: null, fallbackOptions,
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

        private static LSP.VSInternalPriorityLevel? UnifiedSuggestedActionSetPriorityToPriorityLevel(CodeActionPriority priority)
            => priority switch
            {
                CodeActionPriority.Lowest => LSP.VSInternalPriorityLevel.Lowest,
                CodeActionPriority.Low => LSP.VSInternalPriorityLevel.Low,
                CodeActionPriority.Default => LSP.VSInternalPriorityLevel.Normal,
                CodeActionPriority.High => LSP.VSInternalPriorityLevel.High,
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
