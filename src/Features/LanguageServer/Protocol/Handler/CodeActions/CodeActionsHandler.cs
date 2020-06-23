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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the get code actions command.
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
            var codeActions = await GetCodeActionsAndKindAsync(document,
                _codeFixService,
                _codeRefactoringService,
                request.Range,
                cancellationToken).ConfigureAwait(false);

            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            codeActions = codeActions.Where(c => !(c.Key is CodeActionWithOptions));

            var results = new List<VSCodeAction>();
            foreach (var codeAction in codeActions)
            {
                results.Add(GenerateVSCodeAction(request, codeAction.Key, codeAction.Value));
            }

            return results.ToArray();

            static VSCodeAction GenerateVSCodeAction(
                CodeActionParams request,
                CodeAction codeAction,
                CodeActionKind codeActionKind)
            {
                var nestedActions = new List<VSCodeAction>();
                foreach (var action in codeAction.NestedCodeActions)
                {
                    nestedActions.Add(GenerateVSCodeAction(request, action, codeActionKind));
                }

                return new VSCodeAction
                {
                    Title = codeAction.Title,
                    Kind = codeActionKind,
                    Diagnostics = request.Context.Diagnostics,
                    Children = nestedActions.ToArray(),
                    Data = new CodeActionResolveData { CodeActionParams = request }
                };
            }
        }

        internal static async Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document? document,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            LSP.Range selection,
            CancellationToken cancellationToken)
        {
            if (document == null)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var (codeFixCollections, codeRefactorings) = await GetCodeFixesAndRefactoringsAsync(
                document, codeFixService,
                codeRefactoringService, selection,
                cancellationToken).ConfigureAwait(false);

            var codeActions = codeFixCollections.SelectMany(c => c.Fixes.Select(f => f.Action)).Concat(
                    codeRefactorings.SelectMany(r => r.CodeActions.Select(ca => ca.action)));

            return codeActions;
        }

        internal static async Task<IEnumerable<KeyValuePair<CodeAction, CodeActionKind>>> GetCodeActionsAndKindAsync(
            Document? document,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            LSP.Range selection,
            CancellationToken cancellationToken)
        {
            var actions = new Dictionary<CodeAction, CodeActionKind>();
            if (document == null)
            {
                return actions;
            }

            var (codeFixCollections, codeRefactorings) = await GetCodeFixesAndRefactoringsAsync(
                document, codeFixService,
                codeRefactoringService, selection,
                cancellationToken).ConfigureAwait(false);

            foreach (var fix in codeFixCollections.SelectMany(codeFix => codeFix.Fixes))
            {
                actions.Add(fix.Action, CodeActionKind.QuickFix);
            }

            foreach (var (action, _) in codeRefactorings.SelectMany(codeRefactoring => codeRefactoring.CodeActions))
            {
                actions.Add(action, CodeActionKind.Refactor);
            }

            return actions;
        }

        internal static async Task<(ImmutableArray<CodeFixCollection>, ImmutableArray<CodeRefactoring>)> GetCodeFixesAndRefactoringsAsync(
            Document document,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            LSP.Range selection,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = ProtocolConversions.RangeToTextSpan(selection, text);
            var codeFixCollections = await codeFixService.GetFixesAsync(document, textSpan, true, cancellationToken).ConfigureAwait(false);
            var codeRefactorings = await codeRefactoringService.GetRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return (codeFixCollections, codeRefactorings);
        }
    }
}
