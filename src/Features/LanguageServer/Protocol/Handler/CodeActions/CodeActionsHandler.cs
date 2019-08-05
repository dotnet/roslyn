// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
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

            var clientSupportsWorkspaceEdits = true;
            if (clientCapabilities?.Experimental is JObject clientCapabilitiesExtensions)
            {
                clientSupportsWorkspaceEdits = clientCapabilitiesExtensions.SelectToken("supportsWorkspaceEdits")?.Value<bool>() ?? clientSupportsWorkspaceEdits;
            }

            var result = new ArrayBuilder<object>();
            foreach (var codeAction in codeActions)
            {
                // If we have a codeaction with a single applychangesoperation, we want to send the codeaction with the edits.
                var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(keepThreadContext);
                if (clientSupportsWorkspaceEdits && operations.Length == 1 && operations.First() is ApplyChangesOperation applyChangesOperation)
                {
                    var workspaceEdit = new LSP.WorkspaceEdit { Changes = new Dictionary<string, LSP.TextEdit[]>() };
                    var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                    var changedDocuments = changes.GetProjectChanges().SelectMany(pc => pc.GetChangedDocuments());

                    foreach (var docId in changedDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetDocument(docId);
                        var oldDoc = solution.GetDocument(docId);
                        var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(keepThreadContext);
                        var textChanges = await newDoc.GetTextChangesAsync(oldDoc).ConfigureAwait(keepThreadContext);

                        var edits = textChanges.Select(tc => new LSP.TextEdit
                        {
                            NewText = tc.NewText,
                            Range = ProtocolConversions.TextSpanToRange(tc.Span, oldText)
                        });

                        workspaceEdit.Changes.Add(newDoc.FilePath, edits.ToArray());
                    }

                    result.Add(new LSP.CodeAction { Title = codeAction.Title, Edit = workspaceEdit });
                }
                // Otherwise, send the original request to be executed on the host.
                else
                {
                    // Note that we can pass through the params for this
                    // request (like range, filename) because between getcodeaction and runcodeaction there can be no
                    // changes on the IDE side (it will requery for codeactions if there are changes).
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
            }

            return result.ToArrayAndFree();
        }
    }
}
