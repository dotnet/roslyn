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
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.PooledObjects;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the get code actions command.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCodeActionName)]
    internal class CodeActionsHandler : AbstractRequestHandler<LSP.CodeActionParams, LSP.SumType<LSP.Command, LSP.CodeAction>[]>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        internal const string RunCodeActionCommandName = "Roslyn.RunCodeAction";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService, ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
        }

        public override async Task<LSP.SumType<LSP.Command, LSP.CodeAction>[]> HandleRequestAsync(LSP.CodeActionParams request, LSP.ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
        {
            var document = SolutionProvider.GetDocument(request.TextDocument, clientName);
            var codeActions = await GetCodeActionsAsync(document,
                _codeFixService,
                _codeRefactoringService,
                request.Range,
                cancellationToken).ConfigureAwait(false);

            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            codeActions = codeActions.Where(c => !(c is CodeActionWithOptions));

            var result = new ArrayBuilder<LSP.SumType<LSP.Command, LSP.CodeAction>>();
            foreach (var codeAction in codeActions)
            {
                // Always return the Command instead of a precalculated set of workspace edits. 
                // The edits will be calculated when the code action is either previewed or 
                // invoked.

                // It's safe for the client to pass back the range/filename in the command to run
                // on the server because the client will always re-issue a get code actions request
                // before invoking a preview or running the command on the server.

                result.Add(
                    new LSP.Command
                    {
                        CommandIdentifier = RunCodeActionCommandName,
                        Title = codeAction.Title,
                        Arguments = new object[]
                        {
                                new RunCodeActionParams
                                {
                                    CodeActionParams = request,
                                    Title = codeAction.Title
                                }
                        }
                    });
            }

            return result.ToArrayAndFree();
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

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var textSpan = ProtocolConversions.RangeToTextSpan(selection, text);
            var codeFixCollections = await codeFixService.GetFixesAsync(document, textSpan, true, cancellationToken).ConfigureAwait(false);
            var codeRefactorings = await codeRefactoringService.GetRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            var codeActions = codeFixCollections.SelectMany(c => c.Fixes.Select(f => f.Action)).Concat(
                                codeRefactorings.SelectMany(r => r.CodeActions.Select(ca => ca.action)));

            // Flatten out the nested codeactions.
            var nestedCodeActions = codeActions.Where(c => c is CodeAction.CodeActionWithNestedActions nc && nc.IsInlinable).SelectMany(nc => nc.NestedCodeActions);
            codeActions = codeActions.Where(c => !(c is CodeAction.CodeActionWithNestedActions)).Concat(nestedCodeActions);

            return codeActions;
        }
    }
}
