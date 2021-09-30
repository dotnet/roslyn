﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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
    internal class CodeActionResolveHandler : IRequestHandler<LSP.VSCodeAction, LSP.VSCodeAction>
    {
        private readonly CodeActionsCache _codeActionsCache;
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        public CodeActionResolveHandler(
            CodeActionsCache codeActionsCache,
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService)
        {
            _codeActionsCache = codeActionsCache;
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
        }

        public string Method => MSLSPMethods.TextDocumentCodeActionResolveName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier? GetTextDocumentIdentifier(VSCodeAction request)
            => ((JToken)request.Data!).ToObject<CodeActionResolveData>().TextDocument;

        public async Task<LSP.VSCodeAction> HandleRequestAsync(LSP.VSCodeAction codeAction, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var data = ((JToken)codeAction.Data!).ToObject<CodeActionResolveData>();
            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                _codeActionsCache,
                document,
                data.Range,
                _codeFixService,
                _codeRefactoringService,
                cancellationToken).ConfigureAwait(false);

            var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(
                data.UniqueIdentifier, codeActions);
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
                var solution = document.Project.Solution;
                var textDiffService = solution.Workspace.Services.GetService<IDocumentTextDifferencingService>();

                using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var textDocumentEdits);
                foreach (var applyChangesOperation in applyChangesOperations)
                {
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

                    // TO-DO: If the change involves adding or removing a project reference, execute via command instead of
                    // WorkspaceEdit until adding/removing project references is supported in LSP:
                    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1166040
                    var projectReferences = projectChanges.SelectMany(
                        pc => pc.GetAddedProjectReferences().Concat(pc.GetRemovedProjectReferences()));
                    if (projectReferences.Any())
                    {
                        codeAction.Command = SetCommand(codeAction.Title, data);
                        return codeAction;
                    }

                    var changedDocuments = projectChanges.SelectMany(pc => pc.GetChangedDocuments());
                    var changedAnalyzerConfigDocuments = projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments());
                    var changedAdditionalDocuments = projectChanges.SelectMany(pc => pc.GetChangedAdditionalDocuments());

                    // Changed documents
                    await AddTextDocumentEditsAsync(
                        textDocumentEdits, changedDocuments,
                        applyChangesOperation.ChangedSolution.GetDocument, solution.GetDocument, textDiffService,
                        cancellationToken).ConfigureAwait(false);

                    // Changed analyzer config documents
                    await AddTextDocumentEditsAsync(
                        textDocumentEdits, changedAnalyzerConfigDocuments,
                        applyChangesOperation.ChangedSolution.GetAnalyzerConfigDocument, solution.GetAnalyzerConfigDocument,
                        textDiffService: null, cancellationToken).ConfigureAwait(false);

                    // Changed additional documents
                    await AddTextDocumentEditsAsync(
                        textDocumentEdits, changedAdditionalDocuments,
                        applyChangesOperation.ChangedSolution.GetAdditionalDocument, solution.GetAdditionalDocument,
                        textDiffService: null, cancellationToken).ConfigureAwait(false);
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

            static async Task AddTextDocumentEditsAsync<T>(
                ArrayBuilder<TextDocumentEdit> textDocumentEdits,
                IEnumerable<DocumentId> changedDocuments,
                Func<DocumentId, T?> getNewDocumentFunc,
                Func<DocumentId, T?> getOldDocumentFunc,
                IDocumentTextDifferencingService? textDiffService,
                CancellationToken cancellationToken)
                where T : TextDocument
            {
                foreach (var docId in changedDocuments)
                {
                    var newTextDoc = getNewDocumentFunc(docId);
                    var oldTextDoc = getOldDocumentFunc(docId);

                    Contract.ThrowIfNull(oldTextDoc);
                    Contract.ThrowIfNull(newTextDoc);

                    var oldText = await oldTextDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    IEnumerable<TextChange> textChanges;

                    // Normal documents have a unique service for calculating minimal text edits. If we used the standard 'GetTextChanges'
                    // method instead, we would get a change that spans the entire document, which we ideally want to avoid.
                    if (newTextDoc is Document newDoc && oldTextDoc is Document oldDoc)
                    {
                        Contract.ThrowIfNull(textDiffService);
                        textChanges = await textDiffService.GetTextChangesAsync(oldDoc, newDoc, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var newText = await newTextDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        textChanges = newText.GetTextChanges(oldText);
                    }

                    var edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();
                    var documentIdentifier = new VersionedTextDocumentIdentifier { Uri = newTextDoc.GetURI() };
                    textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = edits.ToArray() });
                }
            }
        }
    }
}
