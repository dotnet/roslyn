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
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Resolves a code action by filling out its Edit and/or Command property.
    /// The handler is triggered only when a user hovers over a code action. This
    /// system allows the basic code action data to be computed quickly, and the
    /// complex data, such as edits and commands, to be computed only when necessary
    /// (i.e. when hovering/previewing a code action).
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
            var data = ((JToken)codeAction.Data).ToObject<CodeActionResolveData>();
            var document = SolutionProvider.GetDocument(data.TextDocument, clientName);
            Contract.ThrowIfNull(document);

            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                document,
                _codeFixService,
                _codeRefactoringService,
                data.Range,
                cancellationToken).ConfigureAwait(false);

            var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(
                data.UniqueIdentifier, codeActions.ToImmutableArray());
            Contract.ThrowIfNull(codeActionToResolve);

            var operations = await codeActionToResolve.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations.IsEmpty)
            {
                return codeAction;
            }

            // If we have all non-ApplyChangesOperations, set up to run as command on the server
            // instead of using WorkspaceEdits.
            if (operations.All(operation => !(operation is ApplyChangesOperation)))
            {
                codeAction.Command = SetCommand(codeAction.Title, data);
                return codeAction;
            }

            // TO-DO: We currently must execute code actions which add new documents on the server as commands,
            // since there is no LSP support for adding documents yet. In the future, we should move these actions
            // to execute on the client.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/

            // Add workspace edits
            var applyChangesOperations = operations.OfType<ApplyChangesOperation>();
            if (applyChangesOperations.Any())
            {
                using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var textDocumentEdits);
                foreach (var applyChangesOperation in applyChangesOperations)
                {
                    var solution = document.Project.Solution;
                    var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                    var projectChanges = changes.GetProjectChanges();

                    // TO-DO: If the change involves adding or removing a document, execute via command instead of WorkspaceEdit
                    // until adding/removing documents is supported in LSP: https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/
                    // After support is added, remove the below if-statement and add code to support adding/removing documents.
                    var addedDocuments = projectChanges.SelectMany(
                        pc => pc.GetAddedDocuments().Concat(pc.GetAddedAdditionalDocuments().Concat(pc.GetAddedAnalyzerConfigDocuments())));
                    var removedDocuments = projectChanges.SelectMany(
                        pc => pc.GetRemovedDocuments().Concat(pc.GetRemovedAdditionalDocuments().Concat(pc.GetRemovedAnalyzerConfigDocuments())));
                    if (addedDocuments.Any() || removedDocuments.Any())
                    {
                        codeAction.Command = SetCommand(codeAction.Title, data);
                        return codeAction;
                    }

                    var changedDocuments = projectChanges.SelectMany(pc => pc.GetChangedDocuments());
                    var changedAnalyzerConfigDocuments = projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments());
                    var changedAdditionalDocuments = projectChanges.SelectMany(pc => pc.GetChangedAdditionalDocuments());

                    // Changed documents
                    await AddTextDocumentEdits(
                        textDocumentEdits, applyChangesOperation, solution, changedDocuments,
                        applyChangesOperation.ChangedSolution.GetDocument, solution.GetDocument,
                        cancellationToken).ConfigureAwait(false);

                    // Changed analyzer config documents
                    await AddTextDocumentEdits(
                        textDocumentEdits, applyChangesOperation, solution, changedAnalyzerConfigDocuments,
                        applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument, solution.GetAnalyzerConfigDocument,
                        cancellationToken).ConfigureAwait(false);

                    // Changed additional documents
                    await AddTextDocumentEdits(
                        textDocumentEdits, applyChangesOperation, solution, changedAdditionalDocuments,
                        applyChangesOperation.ChangedSolution.GetAdditionalDocument, solution.GetAdditionalDocument,
                        cancellationToken).ConfigureAwait(false);
                }

                codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = textDocumentEdits.ToArray() };
            }

            return codeAction;

            // Local functions
            static LSP.Command SetCommand(string title, CodeActionResolveData data) => new LSP.Command
            {
                CommandIdentifier = CodeActionsHandler.RunCodeActionCommandName,
                Title = title,
                Arguments = new object[] { data }
            };

            static async Task AddTextDocumentEdits<T>(
                ArrayBuilder<TextDocumentEdit> textDocumentEdits,
                ApplyChangesOperation applyChangesOperation,
                Solution solution,
                IEnumerable<DocumentId> changedDocuments,
                Func<DocumentId, T?> getNewDocumentFunc,
                Func<DocumentId, T?> getOldDocumentFunc,
                CancellationToken cancellationToken)
                where T : TextDocument
            {
                foreach (var docId in changedDocuments)
                {
                    var newDoc = getNewDocumentFunc(docId);
                    var oldDoc = getOldDocumentFunc(docId);

                    Contract.ThrowIfNull(oldDoc);
                    Contract.ThrowIfNull(newDoc);

                    var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var newText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var textChanges = newText.GetTextChanges(oldText);

                    var edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                    var documentIdentifier = new VersionedTextDocumentIdentifier { Uri = newDoc.GetURI() };
                    textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = edits.ToArray() });
                }
            }
        }
    }
}
