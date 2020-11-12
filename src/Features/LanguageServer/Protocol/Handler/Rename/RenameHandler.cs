// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentRenameName, mutatesSolutionState: false), Shared]
    internal class RenameHandler : IRequestHandler<LSP.RenameParams, WorkspaceEdit?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler()
        {
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(RenameParams request) => request.TextDocument;

        public async Task<WorkspaceEdit?> HandleRequestAsync(RenameParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            var oldSolution = document.Project.Solution;
            var renameService = document.Project.LanguageServices.GetRequiredService<IEditorInlineRenameService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (!renameInfo.CanRename)
            {
                return null;
            }

            var renameLocationSet = await renameInfo.FindRenameLocationsAsync(oldSolution.Workspace.Options, cancellationToken).ConfigureAwait(false);
            var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, oldSolution.Workspace.Options, cancellationToken).ConfigureAwait(false);

            var renamedSolution = renameReplacementInfo.NewSolution;
            if (renamedSolution == oldSolution)
            {
                return null;
            }

            var solutionChanges = renamedSolution.GetChanges(oldSolution);

            // Linked files can correspond to multiple roslyn documents each with changes.  Merge the changes in the linked files so that all linked documents have the same text.
            // Then we can just take the text changes from the first document to avoid returning duplicate edits.
            renamedSolution = await renamedSolution.WithMergedLinkedFileChangesAsync(oldSolution, solutionChanges, cancellationToken: cancellationToken).ConfigureAwait(false);
            solutionChanges = renamedSolution.GetChanges(oldSolution);
            var changedDocuments = solutionChanges
                .GetProjectChanges()
                .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                .GroupBy(docId => renamedSolution.GetRequiredDocument(docId).FilePath, StringComparer.OrdinalIgnoreCase).Select(group => group.First());

            var textDiffService = renamedSolution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();
            var documentEdits = await ProtocolConversions.ChangedDocumentsToTextDocumentEditsAsync(changedDocuments, renamedSolution.GetRequiredDocument, oldSolution.GetRequiredDocument,
                textDiffService, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>.GetInstance(out var edits);

            // Add the document edits
            foreach (var documentEdit in documentEdits)
            {
                edits.AddRange(documentEdit);
            }

            // Add an edit for renaming the file (if applicable)
            if (renameInfo is IInlineRenameInfoWithFileRename renameInfoWithFileRename &&
                renameInfoWithFileRename.GetFileRenameInfo() == InlineRenameFileRenameInfo.Allowed)
            {
                AddFileNameEdit(request.NewName, document, edits);
            }

            return new WorkspaceEdit { DocumentChanges = edits.ToArray() };
        }

        private static void AddFileNameEdit(
            string newName,
            Document document,
            ArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>> edits)
        {
            var fileInfo = new FileInfo(document.FilePath);

            // If a file with the same name already exists within the project, generate a unique name.
            var uniqueName = GenerateUniqueFileNameWithinProject(document.Project, newName, fileInfo.Extension);
            var newPath = Path.Combine(fileInfo.DirectoryName, uniqueName);

            edits.Add(new LSP.RenameFile
            {
                OldUri = document.GetURI(),
                NewUri = ProtocolConversions.GetUriFromFilePath(newPath),
                Options = new LSP.RenameFileOptions { IgnoreIfExists = true }
            });

            // Local functions
            static string GenerateUniqueFileNameWithinProject(Project project, string newName, string extension)
            {
                var existingFileNames = project.Documents.Select(d => d.Name);
                var fileNameWithExtension = newName + extension;
                if (!existingFileNames.Contains(fileNameWithExtension))
                {
                    return fileNameWithExtension;
                }

                return NameGenerator.GenerateUniqueName(newName, extension, n => !existingFileNames.Contains(n));
            }
        }
    }
}
