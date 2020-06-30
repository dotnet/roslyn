// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Resolves a code action by filling out its Edit or Command property.
    /// </summary>
    [ExportLspMethod(MSLSPMethods.TextDocumentCodeActionResolveName), Shared]
    internal class CodeActionResolveHandler : AbstractRequestHandler<LSP.VSCodeAction, LSP.VSCodeAction>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionResolveHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
        }

        public override async Task<LSP.VSCodeAction> HandleRequestAsync(
            LSP.VSCodeAction codeAction,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            CodeActionResolveData data;
            if (codeAction.Data is CodeActionResolveData codeActionResolveData)
            {
                data = codeActionResolveData;
            }
            else
            {
                data = ((JToken)codeAction.Data).ToObject<CodeActionResolveData>();
            }

            var document = SolutionProvider.GetDocument(data.CodeActionParams.TextDocument, clientName);
            var codeActions = await CodeActionsHandler.GetCodeActionsAsync(
                document,
                _codeFixService,
                _codeRefactoringService,
                data.CodeActionParams.Range,
                cancellationToken).ConfigureAwait(false);

            if (codeActions == null || !codeActions.Any())
            {
                return codeAction;
            }

            var codeActionToResolve = GetCodeActionToResolve(data.DistinctTitle, codeActions);

            // We didn't find a matching action, so just return the action without an edit or command.
            if (codeActionToResolve == null)
            {
                return codeAction;
            }

            var operations = await codeActionToResolve.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations.IsEmpty)
            {
                return codeAction;
            }

            // TO-DO:
            // 1) We currently must execute code actions which add new documents on the server as commands,
            // since there is no LSP support for adding documents yet. In the future, we should move these actions
            // to execute on the client.
            // 2) There is also a bug (same tracking item) where code actions that edit documents other than the
            // one where the code action was invoked from do not work. We must temporarily execute these as commands
            // as well.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
            var runAsCommand = false;

            var applyChangesOperations = operations.Where(operation => operation is ApplyChangesOperation);
            if (applyChangesOperations.Any())
            {
                var workspaceEdits = await ComputeWorkspaceEdits(applyChangesOperations, document!, cancellationToken).ConfigureAwait(false);
                if (workspaceEdits.Any())
                {
                    codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = workspaceEdits };
                }
                else
                {
                    // The workspace edit is something we don't currently support, like adding a new document.
                    runAsCommand = true;
                }
            }

            // Set up to run as command on the server instead of using WorkspaceEdits.
            var commandOperations = operations.All(operation => !(operation is ApplyChangesOperation));
            if (commandOperations || runAsCommand)
            {
                codeAction.Command = SetCommand(codeAction, data);
            }

            return codeAction;

            // Local functions
            static async Task<TextDocumentEdit[]> ComputeWorkspaceEdits(
                IEnumerable<CodeActionOperation> applyChangesOperations,
                Document document,
                CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var textDocumentEdits);
                foreach (ApplyChangesOperation applyChangesOperation in applyChangesOperations)
                {
                    var solution = document.Project.Solution;
                    var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                    var projectChanges = changes.GetProjectChanges();

                    // TO-DO: If the change involves adding a document, execute via command instead of WorkspaceEdit
                    // until adding documents is supported in LSP: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                    // After support is added, remove the below if-statement and add code to support adding documents.
                    var addedDocuments = projectChanges.SelectMany(
                        pc => pc.GetAddedDocuments().Concat(pc.GetAddedAdditionalDocuments().Concat(pc.GetAddedAnalyzerConfigDocuments())));
                    if (addedDocuments.Any())
                    {
                        return textDocumentEdits.ToArray();
                    }

                    var changedDocuments = projectChanges.SelectMany(pc => pc.GetChangedDocuments());
                    var changedAnalyzerConfigDocuments = projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments());
                    var changedAdditionalDocuments = projectChanges.SelectMany(pc => pc.GetChangedAdditionalDocuments());

                    // TO-DO: If the change involves modifying any document besides the document where the code action
                    // was invoked, temporarily execute via command instead of WorkspaceEdit until LSP bug is fixed:
                    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                    // After bug is fixed, remove the below if-statement and the existing code should work.
                    if (changedDocuments.Any(d => d != document.Id) ||
                        changedAnalyzerConfigDocuments.Any() ||
                        changedAdditionalDocuments.Any())
                    {
                        return textDocumentEdits.ToArray();
                    }

                    // Changed documents
                    foreach (var docId in changedDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetDocument(docId);
                        var oldDoc = solution.GetDocument(docId);
                        if (oldDoc == null || newDoc == null)
                        {
                            continue;
                        }

                        await GetTextDocumentEdits(textDocumentEdits, newDoc, oldDoc, cancellationToken).ConfigureAwait(false);
                    }

                    // Changed analyzer config documents
                    // This for loop won't currently execute until https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                    // is fixed.
                    foreach (var docId in changedAnalyzerConfigDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument(docId);
                        var oldDoc = solution.GetAnalyzerConfigDocument(docId);
                        if (oldDoc == null || newDoc == null)
                        {
                            continue;
                        }

                        await GetTextDocumentEdits(textDocumentEdits, newDoc, oldDoc, cancellationToken).ConfigureAwait(false);
                    }

                    // Changed additional documents
                    // This for loop won't currently execute until https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                    // is fixed.
                    foreach (var docId in changedAdditionalDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetAdditionalDocument(docId);
                        var oldDoc = solution.GetAdditionalDocument(docId);
                        if (oldDoc == null || newDoc == null)
                        {
                            continue;
                        }

                        await GetTextDocumentEdits(textDocumentEdits, newDoc, oldDoc, cancellationToken).ConfigureAwait(false);
                    }
                }

                return textDocumentEdits.ToArray();
            }

            static async Task GetTextDocumentEdits(
                ArrayBuilder<TextDocumentEdit> textDocumentEdits,
                TextDocument newDoc,
                TextDocument oldDoc,
                CancellationToken cancellationToken)
            {
                var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var textChanges = newText.GetTextChanges(oldText).ToList();

                var edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                var documentIdentifier = new VersionedTextDocumentIdentifier() { Uri = newDoc.GetURI() };
                textDocumentEdits.Add(new TextDocumentEdit() { TextDocument = documentIdentifier, Edits = edits.ToArray() });
            }

            static LSP.Command SetCommand(VSCodeAction codeAction, CodeActionResolveData data) => new LSP.Command
            {
                CommandIdentifier = CodeActionsHandler.RunCodeActionCommandName,
                Title = codeAction.Title,
                Arguments = new object[]
                {
                    new RunCodeActionParams
                    {
                        CodeActionParams = data.CodeActionParams,
                        DistinctTitle = data.DistinctTitle
                    }
                }
            };
        }

        internal static CodeAction? GetCodeActionToResolve(string distinctTitle, IEnumerable<CodeAction> codeActions)
        {
            // Searching for the matching code action. We compare against the distinct title (e.g. "Suppress IDExxxxNone")
            // instead of the regular title (e.g. "None") since there's a chance that multiple code actions may have
            // the same regular title.
            CodeAction? codeActionToResolve = null;
            foreach (var c in codeActions)
            {
                var action = CheckForMatchingAction(c, distinctTitle, currentTitle: "");
                if (action != null)
                {
                    codeActionToResolve = action;
                    break;
                }
            }

            return codeActionToResolve;
        }

        private static CodeAction? CheckForMatchingAction(CodeAction codeAction, string goalTitle, string currentTitle)
        {
            if (currentTitle + codeAction.Title == goalTitle)
            {
                return codeAction;
            }

            foreach (var nestedAction in codeAction.NestedCodeActions)
            {
                var match = CheckForMatchingAction(nestedAction, goalTitle, currentTitle + codeAction.Title);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
