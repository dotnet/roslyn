// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentRenameName), Shared]
    internal class RenameHandler : AbstractRequestHandler<LSP.RenameParams, WorkspaceEdit?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<WorkspaceEdit?> HandleRequestAsync(RenameParams request, RequestContext context, CancellationToken cancellationToken)
        {
            WorkspaceEdit? workspaceEdit = null;
            var document = SolutionProvider.GetDocument(request.TextDocument, context.ClientName);
            if (document != null)
            {
                var oldSolution = document.Project.Solution;
                var renameService = document.Project.LanguageServices.GetRequiredService<IEditorInlineRenameService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (!renameInfo.CanRename)
                {
                    return workspaceEdit;
                }

                var renameLocationSet = await renameInfo.FindRenameLocationsAsync(oldSolution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, oldSolution.Workspace.Options, cancellationToken).ConfigureAwait(false);

                var renamedSolution = renameReplacementInfo.NewSolution;
                var solutionChanges = renamedSolution.GetChanges(oldSolution);

                // Linked files can correspond to multiple roslyn documents each with changes.  Merge the changes in the linked files so that all linked documents have the same text.
                // Then we can just take the text changes from the first document to avoid returning duplicate edits.
                renamedSolution = await renamedSolution.WithMergedLinkedFileChangesAsync(oldSolution, solutionChanges, cancellationToken: cancellationToken).ConfigureAwait(false);
                solutionChanges = renamedSolution.GetChanges(oldSolution);
                var changedDocuments = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                    .GroupBy(docId => renamedSolution.GetRequiredDocument(docId).FilePath, StringComparer.OrdinalIgnoreCase).Select(group => group.First());

                using var _ = ArrayBuilder<TextDocumentEdit>.GetInstance(out var documentEdits);
                foreach (var docId in changedDocuments)
                {
                    var oldDoc = oldSolution.GetRequiredDocument(docId);
                    var newDoc = renamedSolution.GetRequiredDocument(docId);

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
