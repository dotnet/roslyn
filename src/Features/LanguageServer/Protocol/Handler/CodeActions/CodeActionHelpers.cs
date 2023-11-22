﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using StreamJsonRpc;
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
            LSP.CodeActionKind codeActionKind)
        {
            var codeAction = suggestedAction.OriginalCodeAction;
            var diagnosticsForFix = GetApplicableDiagnostics(request.Context, suggestedAction);

            using var _ = ArrayBuilder<LSP.CodeAction>.GetInstance(out var builder);
            using var _1 = ArrayBuilder<string>.GetInstance(out var codeActionPath);
            var nestedCodeActions = CollectNestedActions(request, codeActionKind, diagnosticsForFix, suggestedAction, codeActionPath, isTopLevelCodeAction: true);

            Command? nestedCodeActionCommand = null;
            var title = codeAction.Title;

            var codeActionPathList = codeActionPath.ToArray();
            if (nestedCodeActions.Any())
            {
                nestedCodeActionCommand = new LSP.Command
                {
                    CommandIdentifier = CodeActionsHandler.RunNestedCodeActionCommandName,
                    Title = title,
                    Arguments = [new CodeActionResolveData(title, codeAction.CustomTags, request.Range, request.TextDocument, null, nestedCodeActions: nestedCodeActions, codeActionPathList)]
                };
            }

            AddLSPCodeActions(builder, codeAction, request, codeActionKind, diagnosticsForFix, nestedCodeActionCommand,
                nestedCodeActions, codeActionPathList, suggestedAction);

            return builder.ToArray();
        }

        private static ImmutableArray<LSP.CodeAction> CollectNestedActions(
            CodeActionParams request,
            LSP.CodeActionKind codeActionKind,
            LSP.Diagnostic[]? diagnosticsForFix,
            IUnifiedSuggestedAction suggestedAction,
            ArrayBuilder<string> codeActionPath,
            bool isTopLevelCodeAction = false)
        {
            var codeAction = suggestedAction.OriginalCodeAction;
            using var _1 = ArrayBuilder<LSP.CodeAction>.GetInstance(out var nestedCodeActions);

            codeActionPath.Add(codeAction.Title);
            if (suggestedAction is UnifiedSuggestedActionWithNestedActions unifiedSuggestedActions)
            {
                foreach (var actionSet in unifiedSuggestedActions.NestedActionSets)
                {
                    foreach (var action in actionSet.Actions)
                    {
                        nestedCodeActions.AddRange(CollectNestedActions(request, codeActionKind, diagnosticsForFix, action, codeActionPath));
                    }
                }
            }
            else
            {
                if (!isTopLevelCodeAction)
                {
                    var codeActionPathList = codeActionPath.ToArray();
                    AddLSPCodeActions(nestedCodeActions, codeAction, request, codeActionKind, diagnosticsForFix,
                        nestedCodeActionCommand: null, nestedCodeActions: null, codeActionPathList, suggestedAction);
                }
            }

            return nestedCodeActions.ToImmutable();
        }

        private static void AddLSPCodeActions(
            ArrayBuilder<LSP.CodeAction> builder,
            CodeAction codeAction,
            CodeActionParams request,
            LSP.CodeActionKind codeActionKind,
            LSP.Diagnostic[]? diagnosticsForFix,
            Command? nestedCodeActionCommand,
            ImmutableArray<LSP.CodeAction>? nestedCodeActions,
            string[] codeActionPathList,
            IUnifiedSuggestedAction suggestedAction)
        {
            var title = codeAction.Title;
            // We add an overarching action to the lightbulb that may contain nested actions.
            // Selecting one of these actions from the list invokes a command on the client side to open
            // a quick pick to select a nested action.
            builder.Add(new LSP.CodeAction
            {
                Title = title,
                Kind = codeActionKind,
                Diagnostics = diagnosticsForFix,
                Command = nestedCodeActionCommand,
                Data = new CodeActionResolveData(title, codeAction.CustomTags, request.Range, request.TextDocument, fixAllFlavors: null, nestedCodeActions, codeActionPathList)
            });

            if (suggestedAction is UnifiedCodeFixSuggestedAction unifiedCodeFixSuggestedAction && unifiedCodeFixSuggestedAction.FixAllFlavors is not null)
            {
                var fixAllFlavors = unifiedCodeFixSuggestedAction.FixAllFlavors.Actions.OfType<UnifiedFixAllCodeFixSuggestedAction>().Select(action => action.FixAllState.Scope.ToString());
                var fixAllTitle = string.Format(FeaturesResources.Fix_All_0, title);
                var command = new LSP.Command
                {
                    CommandIdentifier = CodeActionsHandler.RunFixAllCodeActionCommandName,
                    Title = fixAllTitle,
                    Arguments = [new CodeActionResolveData(fixAllTitle, codeAction.CustomTags, request.Range, request.TextDocument, fixAllFlavors.ToArray(), nestedCodeActions: null, codeActionPathList)]
                };

                builder.Add(new LSP.CodeAction
                {
                    Title = fixAllTitle,
                    Command = command,
                    Kind = codeActionKind,
                    Diagnostics = diagnosticsForFix,
                    Data = new CodeActionResolveData(fixAllTitle, codeAction.CustomTags, request.Range, request.TextDocument, fixAllFlavors.ToArray(), nestedCodeActions: null, codeActionPathList)
                });
            }
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
                Data = new CodeActionResolveData(currentTitle, codeAction.CustomTags, request.Range, request.TextDocument, fixAllFlavors: null, nestedCodeActions: null, codeActionPath: null)
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
            string? fixAllScope,
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

                    codeActions.Add(GetNestedActionsFromActionSet(suggestedAction, fixAllScope));

                    if (fixAllScope != null)
                    {
                        GetFixAllActionsFromActionSet(suggestedAction, codeActions, fixAllScope);
                    }
                }
            }

            return codeActions.ToImmutable();
        }

        /// <summary>
        /// Generates a code action with its nested actions properly set.
        /// </summary>
        private static CodeAction GetNestedActionsFromActionSet(IUnifiedSuggestedAction suggestedAction, string? fixAllScope)
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
                    nestedActions.Add(GetNestedActionsFromActionSet(action, fixAllScope));
                    if (fixAllScope != null)
                    {
                        GetFixAllActionsFromActionSet(action, nestedActions, fixAllScope);
                    }
                }
            }

            return CodeAction.Create(
                codeAction.Title, nestedActions.ToImmutable(), codeAction.IsInlinable, codeAction.Priority);
        }

        private static void GetFixAllActionsFromActionSet(IUnifiedSuggestedAction suggestedAction, ArrayBuilder<CodeAction> codeActions, string? fixAllScope)
        {
            var codeAction = suggestedAction.OriginalCodeAction;
            if (suggestedAction is not UnifiedCodeFixSuggestedAction { FixAllFlavors: not null } unifiedCodeFixSuggestedAction)
            {
                return;
            }

            // Retrieves the fix all code action based on the scope that was selected. 
            // Creates a FixAllCodeAction type so that we can get the correct operations for the selected scope.
            var fixAllFlavor = unifiedCodeFixSuggestedAction.FixAllFlavors.Actions.OfType<UnifiedFixAllCodeFixSuggestedAction>().Where(action => action.FixAllState.Scope.ToString() == fixAllScope).First();
            codeActions.Add(new FixAllCodeAction(codeAction.Title, fixAllFlavor.FixAllState, showPreviewChangesDialog: false));
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

        public static CodeAction GetCodeActionToResolve(string[] codeActionPath, ImmutableArray<CodeAction> codeActions, bool isFixAllAction)
        {
            CodeAction? matchingAction = null;
            var currentActions = codeActions;
            for (var i = 0; i < codeActionPath.Length; i++)
            {
                var title = codeActionPath[i];
                var matchingActions = currentActions.Where(action => action.Title == title);

                // If we only have one matching action then just need to retrieve it from the list.
                if (matchingActions.Count() == 1)
                {
                    matchingAction = matchingActions.First();
                }
                else
                {
                    // Otherwise, we are likely at the end of the path and need to retrieve
                    // the FixAllCodeAction if we are in that state or just the regular CodeAction
                    // since they have the same title path.
                    matchingAction = matchingActions.Single(action => isFixAllAction ? action is FixAllCodeAction : action is CodeAction);
                }

                Contract.ThrowIfNull(matchingAction);

                currentActions = matchingAction.NestedCodeActions;
                if (currentActions.IsEmpty)
                {
                    return matchingAction;
                }
            }

            Contract.ThrowIfNull(matchingAction);
            return matchingAction;
        }
    }
}
