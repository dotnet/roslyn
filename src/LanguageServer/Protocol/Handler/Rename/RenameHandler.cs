﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(RenameHandler)), Shared]
[Method(LSP.Methods.TextDocumentRenameName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RenameHandler() : ILspServiceDocumentRequestHandler<LSP.RenameParams, WorkspaceEdit?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(RenameParams request) => request.TextDocument;

    public Task<WorkspaceEdit?> HandleRequestAsync(RenameParams request, RequestContext context, CancellationToken cancellationToken)
        => GetRenameEditAsync(context.GetRequiredDocument(), ProtocolConversions.PositionToLinePosition(request.Position), request.NewName, includeSourceGenerated: false, cancellationToken);

    internal static async Task<WorkspaceEdit?> GetRenameEditAsync(Document document, LinePosition linePosition, string newName, bool includeSourceGenerated, CancellationToken cancellationToken)
    {
        var oldSolution = document.Project.Solution;
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        var symbolicRenameInfo = await SymbolicRenameInfo.GetRenameInfoAsync(
            document, position, includeSourceGenerated, cancellationToken).ConfigureAwait(false);
        if (symbolicRenameInfo.IsError)
            return null;

        var options = new SymbolRenameOptions(
            renameOverloads: false,
            renameInStrings: false,
            renameInComments: false,
            renameFile: false,
            renameInSourceGeneratedDocuments: includeSourceGenerated);

        var renameLocationSet = await Renamer.FindRenameLocationsAsync(
            oldSolution,
            symbolicRenameInfo.Symbol,
            options,
            cancellationToken).ConfigureAwait(false);

        var renameReplacementInfo = await renameLocationSet.ResolveConflictsAsync(
            symbolicRenameInfo.Symbol, symbolicRenameInfo.GetFinalSymbolName(newName),
            nonConflictSymbolKeys: default,
            cancellationToken).ConfigureAwait(false);

        if (!renameReplacementInfo.IsSuccessful ||
            !renameReplacementInfo.ReplacementTextValid)
        {
            return null;
        }

        var renamedSolution = renameReplacementInfo.NewSolution;
        var solutionChanges = renamedSolution.GetChanges(oldSolution);

        // Linked files can correspond to multiple roslyn documents each with changes.  Merge the changes in the linked files so that all linked documents have the same text.
        // Then we can just take the text changes from the first document to avoid returning duplicate edits.
        renamedSolution = await renamedSolution.WithMergedLinkedFileChangesAsync(oldSolution, solutionChanges, cancellationToken: cancellationToken).ConfigureAwait(false);
        solutionChanges = renamedSolution.GetChanges(oldSolution);

        Contract.ThrowIfTrue(!includeSourceGenerated && !renamedSolution.CompilationState.FrozenSourceGeneratedDocumentStates.IsEmpty, "Renaming in generated documents is not allowed, but there are changes in source generated documents.");

        var changedDocuments = solutionChanges
            .GetProjectChanges()
            .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
            .GroupBy(docId => renamedSolution.GetRequiredDocument(docId).FilePath, StringComparer.OrdinalIgnoreCase).Select(group => group.First())
            .Concat(solutionChanges.GetExplicitlyChangedSourceGeneratedDocuments());

        var textDiffService = renamedSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();

        var documentEdits = await ProtocolConversions.ChangedDocumentsToTextDocumentEditsAsync(changedDocuments, renamedSolution, oldSolution,
            textDiffService, cancellationToken).ConfigureAwait(false);

        return new WorkspaceEdit { DocumentChanges = documentEdits };
    }
}
