// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// we no longer reference the <see cref="IInlineRenameService"/>
    /// See https://github.com/dotnet/roslyn/issues/55142
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(RenameHandler)), Shared]
    [Method(LSP.Methods.TextDocumentRenameName)]
    internal class RenameHandler : AbstractStatelessRequestHandler<LSP.RenameParams, WorkspaceEdit?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler()
        {
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(RenameParams request) => request.TextDocument;

        public override async Task<WorkspaceEdit?> HandleRequestAsync(RenameParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var oldSolution = document.Project.Solution;
            var renameService = document.Project.LanguageServices.GetRequiredService<IEditorInlineRenameService>();
            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (!renameInfo.CanRename)
            {
                return null;
            }

            var options = new SymbolRenameOptions(
                RenameOverloads: false,
                RenameInStrings: false,
                RenameInComments: false,
                RenameFile: false);

            var renameLocationSet = await renameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
            var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, options, cancellationToken).ConfigureAwait(false);

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

            var textDiffService = renamedSolution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();

            var documentEdits = await ProtocolConversions.ChangedDocumentsToTextDocumentEditsAsync(changedDocuments, renamedSolution.GetRequiredDocument, oldSolution.GetRequiredDocument,
                textDiffService, cancellationToken).ConfigureAwait(false);

            return new WorkspaceEdit { DocumentChanges = documentEdits };
        }
    }
}
