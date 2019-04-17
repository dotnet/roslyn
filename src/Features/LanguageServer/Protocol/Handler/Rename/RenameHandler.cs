// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentRenameName)]
    internal class RenameHandler : IRequestHandler<RenameParams, WorkspaceEdit>
    {
        public async Task<WorkspaceEdit> HandleRequestAsync(Solution solution, RenameParams request,
            ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            WorkspaceEdit workspaceEdit = null;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var renameService = document.Project.LanguageServices.GetService<IEditorInlineRenameService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                var renameLocationSet = await renameInfo.FindRenameLocationsAsync(solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

                var newSolution = renameReplacementInfo.NewSolution;
                var solutionChanges = newSolution.GetChanges(solution);
                var changedDocuments = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments());

                var documentEdits = new List<TextDocumentEdit>();
                foreach (var docId in changedDocuments)
                {
                    var oldDoc = solution.GetDocument(docId);
                    var newDoc = newSolution.GetDocument(docId);

                    var textChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                    var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var textDocumentEdit = new TextDocumentEdit
                    {
                        TextDocument = new VersionedTextDocumentIdentifier { Uri = newDoc.GetURI() },
                        Edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray()
                    };
                    documentEdits.Add(textDocumentEdit);
                }

                workspaceEdit = new WorkspaceEdit { DocumentChanges = documentEdits.ToArray() };
            }

            return workspaceEdit;
        }
    }
}
