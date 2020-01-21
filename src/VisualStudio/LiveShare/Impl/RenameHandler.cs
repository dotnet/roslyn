// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    internal class RenameHandler : ILspRequestHandler<RenameParams, WorkspaceEdit, Solution>
    {
        private readonly IThreadingContext _threadingContext;

        public RenameHandler(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task<WorkspaceEdit> HandleAsync(RenameParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var solution = requestContext.Context;
            WorkspaceEdit workspaceEdit = null;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var renameService = document.Project.LanguageServices.GetService<IEditorInlineRenameService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                // We need to be on the UI thread to call GetRenameInfo which computes the rename locations.
                // This is because Roslyn reads the readonly regions of the buffer to compute the locations in the document.
                // This is typically quick. It's marked configureawait(false) so that the bulk of the rename operation can happen
                // in background threads.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (!renameInfo.CanRename)
                {
                    return workspaceEdit;
                }

                var renameLocationSet = await renameInfo.FindRenameLocationsAsync(solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

                var newSolution = renameReplacementInfo.NewSolution;
                var solutionChanges = newSolution.GetChanges(solution);
                var changedDocuments = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments());

                var documentEdits = new ArrayBuilder<TextDocumentEdit>();
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

                workspaceEdit = new WorkspaceEdit { DocumentChanges = documentEdits.ToArrayAndFree() };
            }

            return workspaceEdit;
        }
    }
}
