// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the initial request for code actions. Leaves the Edit and Command properties
    /// of the returned VSCodeActions blank, as these properties should be populated by the
    /// CodeActionsResolveHandler only when the user requests them.
    /// </summary>
    [ExportLspMethod(LSP.Methods.TextDocumentCodeActionName), Shared]
    internal class CodeActionsHandler : AbstractRequestHandler<LSP.CodeActionParams, LSP.VSCodeAction[]>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        internal const string RunCodeActionCommandName = "Roslyn.RunCodeAction";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
        }

        public override async Task<LSP.VSCodeAction[]> HandleRequestAsync(
            LSP.CodeActionParams request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, clientName);
            if (document == null)
            {
                return Array.Empty<VSCodeAction>();
            }

            var (codeFixCollections, codeRefactorings) = await CodeActionHelpers.GetCodeFixesAndRefactoringsAsync(
                document, _codeFixService, _codeRefactoringService,
                request.Range, cancellationToken).ConfigureAwait(false);

            var codeFixes = codeFixCollections.SelectMany(c => c.Fixes);
            using var _ = ArrayBuilder<VSCodeAction>.GetInstance(out var results);

            // Go through code fixes and code refactorings separately so that we can properly set the CodeActionKind.
            foreach (var codeFix in codeFixes)
            {
                // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
                if (codeFix.Action is CodeActionWithOptions)
                {
                    continue;
                }

                // Temporarily filter out suppress and configure code actions, as we'll later combine them under a top-level
                // code action.
                if (codeFix.Action is AbstractConfigurationActionWithNestedActions)
                {
                    continue;
                }

                results.Add(GenerateVSCodeAction(request, codeFix.Action, CodeActionKind.QuickFix));
            }

            foreach (var (action, _) in codeRefactorings.SelectMany(codeRefactoring => codeRefactoring.CodeActions))
            {
                // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
                if (action is CodeActionWithOptions)
                {
                    continue;
                }

                results.Add(GenerateVSCodeAction(request, action, CodeActionKind.Refactor));
            }

            // Special case (also dealt with specially in local Roslyn): 
            // If we have configure/suppress code actions, combine them under one top-level code action.
            var configureSuppressActions = codeFixes.Where(a => a.Action is AbstractConfigurationActionWithNestedActions);
            if (configureSuppressActions.Any())
            {
                results.Add(GenerateVSCodeAction(request, new CodeActionWithNestedActions(
                    CodeFixesResources.Suppress_or_Configure_issues,
                    configureSuppressActions.Select(a => a.Action).ToImmutableArray(), true), CodeActionKind.QuickFix));
            }

            return results.ToArray();

            // Local functions
            static VSCodeAction GenerateVSCodeAction(
                CodeActionParams request,
                CodeAction codeAction,
                CodeActionKind codeActionKind,
                string parentTitle = "")
            {
                using var _ = ArrayBuilder<VSCodeAction>.GetInstance(out var nestedActions);

                // Adding a delimiter for nested code actions, e.g. 'Suppress or Configure issues.Suppress IDEXXXX.in Source'
                if (parentTitle != "")
                {
                    parentTitle += '.';
                }

                // Nested code actions' unique identifiers consist of: parent code action unique identifier + '.' + title of code action
                foreach (var action in codeAction.NestedCodeActions)
                {
                    nestedActions.Add(GenerateVSCodeAction(request, action, codeActionKind, codeAction.Title));
                }

                return new VSCodeAction
                {
                    Title = codeAction.Title,
                    Kind = codeActionKind,
                    Diagnostics = request.Context.Diagnostics,
                    Children = nestedActions.ToArray(),
                    Data = new CodeActionResolveData(parentTitle + codeAction.Title, request.Range, request.TextDocument)
                };
            }
        }
    }
}
