// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the get code actions command.
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

            var codeActionToResolve = codeActions.FirstOrDefault(a => a.Title == data.DistinctTitle);
            if (codeActionToResolve == null)
            {
                // Check any potential nested actions for a match.
                foreach (var c in codeActions)
                {
                    foreach (var n in c.NestedCodeActions)
                    {
                        if (c.Title + n.Title == data.DistinctTitle)
                        {
                            codeActionToResolve = n;
                            break;
                        }
                    }
                }
            }

            if (codeActionToResolve == null)
            {
                return codeAction;
            }

            var operations = await codeActionToResolve.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations.IsEmpty)
            {
                return codeAction;
            }

            var applyChangesOperations = operations.Where(operation => operation is ApplyChangesOperation);
            if (applyChangesOperations.Any())
            {
                using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var textDocumentEdits);
                foreach (ApplyChangesOperation applyChangesOperation in applyChangesOperations)
                {
                    var solution = document!.Project.Solution;
                    var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                    var projectChanges = changes.GetProjectChanges();

                    var changedDocuments = projectChanges.SelectMany(pc => pc.GetChangedDocuments());
                    foreach (var docId in changedDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetDocument(docId);
                        var oldDoc = solution.GetDocument(docId);
                        if (oldDoc == null || newDoc == null)
                        {
                            continue;
                        }

                        var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var newText = await newDoc.GetTextChangesAsync(oldDoc).ConfigureAwait(false);

                        var edits = newText.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                        var documentIdentifier = new VersionedTextDocumentIdentifier() { Uri = newDoc.GetURI() };
                        textDocumentEdits.Add(new TextDocumentEdit() { TextDocument = documentIdentifier, Edits = edits.ToArray() });
                    }

                    var changedAnalyzerConfigDocuments = projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments());
                    foreach (var docId in changedDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument(docId);
                        var oldDoc = solution.GetAnalyzerConfigDocument(docId);
                        if (oldDoc == null || newDoc == null)
                        {
                            continue;
                        }

                        // TO-DO: Fix this
                        //var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        //var newText = await newDoc.GetTextChangesAsync(oldDoc).ConfigureAwait(false);

                        //var edits = newText.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                        //var documentIdentifier = new VersionedTextDocumentIdentifier() { Uri = newDoc.GetURI() };
                        //textDocumentEdits.Add(new TextDocumentEdit() { TextDocument = documentIdentifier, Edits = edits.ToArray() });
                    }

                    var addedDocuments = projectChanges.SelectMany(pc => pc.GetAddedDocuments());
                    foreach (var docId in addedDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetDocument(docId);
                        if (newDoc == null)
                        {
                            continue;
                        }

                        var newText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var edit = new LSP.TextEdit() { NewText = newText.ToString(), Range = new LSP.Range() { Start = new Position(), End = new Position() } };
                        var documentIdentifier =
                            new VersionedTextDocumentIdentifier()
                            {
                                Uri = new Uri(Path.GetDirectoryName(document.Project.FilePath) + "/" + newDoc.Name, UriKind.Absolute)
                            };
                        textDocumentEdits.Add(new TextDocumentEdit() { TextDocument = documentIdentifier, Edits = new TextEdit[] { edit } });
                    }

                    /* TO-DO: Fix:
                    var addedAnalyzerConfigDocuments = projectChanges.SelectMany(pc => pc.GetAddedAnalyzerConfigDocuments());
                    foreach (var docId in addedAnalyzerConfigDocuments)
                    {
                        var newDoc = applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument(docId);
                        if (newDoc == null)
                        {
                            continue;
                        }

                        var newText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        var edit = new LSP.TextEdit() { NewText = newText.ToString(), Range = new LSP.Range() { Start = new Position(), End = new Position() } };
                        var documentIdentifier = new VersionedTextDocumentIdentifier() { Uri = new Uri(Path.ChangeExtension(document.Project.FilePath, null) + "//" + newDoc.Name, UriKind.Absolute) };
                        textDocumentEdits.Add(new TextDocumentEdit() { TextDocument = documentIdentifier, Edits = new TextEdit[] { edit } });
                    } */
                }

                codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = textDocumentEdits.ToArray() };
            }

            var commandOperations = operations.Where(operation => !(operation is ApplyChangesOperation));
            if (commandOperations.Any())
            {
                codeAction.Command = new LSP.Command
                {
                    CommandIdentifier = CodeActionsHandler.RunCodeActionCommandName,
                    Title = codeAction.Title,
                    Arguments = new object[]
                    {
                        new RunCodeActionParams
                        {
                            CodeActionParams = data.CodeActionParams,
                            Title = codeAction.Title
                        }
                    }
                };
            }

            return codeAction;
        }
    }
}
