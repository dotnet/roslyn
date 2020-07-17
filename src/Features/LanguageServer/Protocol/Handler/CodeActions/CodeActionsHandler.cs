// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private readonly IThreadingContext _threadingContext;

        internal const string RunCodeActionCommandName = "Roslyn.RunCodeAction";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider,
            IThreadingContext threadingContext)
            : base(solutionProvider)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _threadingContext = threadingContext;
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

            var codeActionsAndKinds = await CodeActionHelpers.GetCodeActionsAndKindsAsync(
                document, _codeFixService, _codeRefactoringService, _threadingContext,
                request.Range, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<VSCodeAction>.GetInstance(out var results);
            foreach (var action in codeActionsAndKinds)
            {
                results.Add(GenerateVSCodeAction(request, action.CodeAction, action.Kind));
            }

            return results.ToArray();

            // Local functions
            static VSCodeAction GenerateVSCodeAction(
                CodeActionParams request,
                CodeAction codeAction,
                CodeActionKind codeActionKind,
                string currentTitle = "")
            {
                using var _ = ArrayBuilder<VSCodeAction>.GetInstance(out var nestedActions);

                if (!string.IsNullOrEmpty(currentTitle))
                {
                    // Adding a delimiter for nested code actions, e.g. 'Suppress or Configure issues|Suppress IDEXXXX|in Source'
                    currentTitle += '|';
                }

                currentTitle += codeAction.Title;

                // Nested code actions' unique identifiers consist of: parent code action unique identifier + '|' + title of code action
                foreach (var action in codeAction.NestedCodeActions)
                {
                    nestedActions.Add(GenerateVSCodeAction(request, action, codeActionKind, currentTitle));
                }

                return new VSCodeAction
                {
                    Title = codeAction.Title,
                    Kind = codeActionKind,
                    Diagnostics = request.Context.Diagnostics,
                    Children = nestedActions.ToArray(),
                    Data = new CodeActionResolveData(currentTitle, request.Range, request.TextDocument)
                };
            }
        }
    }
}
