// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the get code actions command.
    /// </summary>
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentCodeActionName)]
    internal class CodeActionsHandler : CodeActionsHandlerBase, IRequestHandler<LSP.CodeActionParams, object[]>
    {
        [ImportingConstructor]
        public CodeActionsHandler(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService) : base(codeFixService, codeRefactoringService)
        {
        }

        public async Task<object[]> HandleRequestAsync(Solution solution, LSP.CodeActionParams request,
            LSP.ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext = false)
        {
            var codeActions = await GetCodeActionsAsync(solution,
                                                    request.TextDocument.Uri,
                                                    request.Range,
                                                    keepThreadContext,
                                                    cancellationToken).ConfigureAwait(keepThreadContext);

            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            codeActions = codeActions.Where(c => !(c is CodeActionWithOptions));

            var result = new ArrayBuilder<object>();
            foreach (var codeAction in codeActions)
            {
                // Note that we can pass through the params for this
                // request (like range, filename) because between getcodeaction and runcodeaction there can be no
                // changes on the IDE side (it will requery for codeactions if there are changes).

                // Always return the Command instead of a precalculated set of workspace edits. The edits will be
                // calculated when the code action is either previewed or invoked.

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
    }
}
