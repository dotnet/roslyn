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
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentRenameName), Shared]
    internal class RenameHandler : IRequestHandler<LSP.RenameParams, WorkspaceEdit?>
    {
        private static TextEditEqualityComparer s_textEditEqualityComparer = new TextEditEqualityComparer();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler()
        {
        }

        public async Task<WorkspaceEdit?> HandleRequestAsync(Solution solution, RenameParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            WorkspaceEdit? workspaceEdit = null;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var renameService = document.Project.LanguageServices.GetRequiredService<IEditorInlineRenameService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (!renameInfo.CanRename)
                {
                    return workspaceEdit;
                }

                var renameLocationSet = await renameInfo.FindRenameLocationsAsync(solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

                var newSolution = renameReplacementInfo.NewSolution;
                var solutionChanges = newSolution.GetChanges(solution);

                var solutionWithLinkedFileChangesMerged = newSolution.WithMergedLinkedFileChangesAsync(solution, solutionChanges, cancellationToken: cancellationToken).Result;
                var changedDocuments = solutionWithLinkedFileChangesMerged.GetChanges(solution).GetProjectChanges().SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true));

                // Linked files will create multiple documents with duplicate edits for the same actual file.
                // So take only the unique edits per URI to avoid returning duplicate edits.
                var textEdits = new Dictionary<Uri, List<TextEdit>>();
                foreach (var changedDocument in changedDocuments)
                {
                    var documentUri = newSolution.GetRequiredDocument(changedDocument).GetURI();
                    var textChangesForUri = textEdits.GetOrValue(documentUri, new List<TextEdit>());
                    textChangesForUri.AddRange(await GetTextChangesAsync(changedDocument, newSolution, solution, cancellationToken).ConfigureAwait(false));
                    textEdits[documentUri] = textChangesForUri.Distinct(s_textEditEqualityComparer).ToList();
                }

                var documentEdits = textEdits.Select(kvp => new TextDocumentEdit
                {
                    TextDocument = new VersionedTextDocumentIdentifier { Uri = kvp.Key },
                    Edits = kvp.Value.ToArray()
                }).ToArray();

                workspaceEdit = new WorkspaceEdit { DocumentChanges = documentEdits };
            }

            return workspaceEdit;

            static async Task<IEnumerable<TextEdit>> GetTextChangesAsync(DocumentId documentId, Solution newSolution, Solution oldSolution, CancellationToken cancellationToken)
            {
                var oldDoc = oldSolution.GetRequiredDocument(documentId);
                var newDoc = newSolution.GetRequiredDocument(documentId);

                var textChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                return textChanges.Select(textChange => ProtocolConversions.TextChangeToTextEdit(textChange, oldText));
            }
        }
    }
}
