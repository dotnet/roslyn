// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

internal sealed class CodeActionResolveHelper
{
    public static async Task<LSP.WorkspaceEdit> GetCodeActionResolveEditsAsync(RequestContext context, CodeActionResolveData data, ImmutableArray<CodeActionOperation> operations, CancellationToken cancellationToken)
    {
        var solution = await context.GetRequiredSolutionAsync(cancellationToken).ConfigureAwait(false);

        return await GetCodeActionResolveEditsAsync(
            solution,
            data,
            operations,
            context.GetRequiredClientCapabilities().Workspace?.WorkspaceEdit?.ResourceOperations ?? [],
            context.TraceDebug,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<LSP.WorkspaceEdit> GetCodeActionResolveEditsAsync(Solution solution, CodeActionResolveData data, ImmutableArray<CodeActionOperation> operations, ResourceOperationKind[] resourceOperations, Action<string> logFunction, CancellationToken cancellationToken)
    {
        // TO-DO: We currently must execute code actions which add new documents on the server as commands,
        // since there is no LSP support for adding documents yet. In the future, we should move these actions
        // to execute on the client.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/

        var textDiffService = solution.Services.GetService<IDocumentTextDifferencingService>();

        using var _1 = ArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>.GetInstance(out var textDocumentEdits);
        using var _2 = PooledHashSet<DocumentUri>.GetInstance(out var modifiedDocumentUris);
        using var _3 = PooledHashSet<DocumentUri>.GetInstance(out var createdDocumentUris);
        using var _4 = PooledHashSet<DocumentUri>.GetInstance(out var deletedDocumentUris);

        foreach (var option in operations)
        {
            // We only support making solution-updating operations in LSP.  And only ones that modify documents. 1st
            // class code actions that do more than this are supposed to add the CodeAction.MakesNonDocumentChange
            // in their Tags so we can filter them out before returning them to the client.
            //
            // However, we cannot enforce this as 3rd party fixers can still run.  So we filter their results to 
            // only apply the portions of their work that updates documents, and nothing else.
            if (option is not ApplyChangesOperation applyChangesOperation)
            {
                logFunction($"Skipping code action operation for '{data.UniqueIdentifier}'.  It was a '{option.GetType().FullName}'");
                continue;
            }

            var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
            var newSolution = await applyChangesOperation.ChangedSolution.WithMergedLinkedFileChangesAsync(solution, changes, cancellationToken: cancellationToken).ConfigureAwait(false);
            changes = newSolution.GetChanges(solution);

            var projectChanges = changes.GetProjectChanges();

            if (!CanApplyWorkspaceEdit(solution, newSolution, resourceOperations))
            {
                logFunction($"Skipping code action '{data.UniqueIdentifier}': unsupported references or resource operations.");
                return new LSP.WorkspaceEdit { DocumentChanges = Array.Empty<TextDocumentEdit>() };
            }

#if false

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

#endif

            // Removed documents
            await AddTextDocumentDeletionsAsync(
                projectChanges.SelectMany(pc => pc.GetRemovedDocuments()),
                solution.GetDocument).ConfigureAwait(false);

            // Removed analyzer config documents
            await AddTextDocumentDeletionsAsync(
                projectChanges.SelectMany(pc => pc.GetRemovedAnalyzerConfigDocuments()),
                solution.GetAnalyzerConfigDocument).ConfigureAwait(false);

            // Removed additional documents
            await AddTextDocumentDeletionsAsync(
                projectChanges.SelectMany(pc => pc.GetRemovedAdditionalDocuments()),
                solution.GetAdditionalDocument).ConfigureAwait(false);

            // Added documents
            await AddTextDocumentAdditionsAsync(
                projectChanges.SelectMany(pc => pc.GetAddedDocuments()),
                newSolution.GetDocument).ConfigureAwait(false);

            // Added analyzer config documents
            await AddTextDocumentAdditionsAsync(
                projectChanges.SelectMany(pc => pc.GetAddedAnalyzerConfigDocuments()),
                newSolution.GetAnalyzerConfigDocument).ConfigureAwait(false);

            // Added additional documents
            await AddTextDocumentAdditionsAsync(
                projectChanges.SelectMany(pc => pc.GetAddedAdditionalDocuments()),
                newSolution.GetAdditionalDocument).ConfigureAwait(false);

            // Changed documents
            await AddTextDocumentEditsAsync(
                projectChanges.SelectMany(pc => pc.GetChangedDocuments()),
                newSolution.GetDocument,
                solution.GetDocument).ConfigureAwait(false);

            // Razor calls through our code action handlers with documents that come from the Razor source generator
            // Those changes are not visible in project changes, because they happen in the compilation state, so we
            // make sure to pull changes out from that too. Changed source generated documents are "frozen" because
            // their content no longer comes from the source generator, so thats our cue to know when to handle.
            // Changes to non-frozen documents don't need to be included, because changes to the origin document would
            // cause the generator to re-generate the same changed content.
            await AddTextDocumentEditsAsync(
                changes.GetExplicitlyChangedSourceGeneratedDocuments(),
                newSolution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId,
                solution.GetRequiredSourceGeneratedDocumentForAlreadyGeneratedId).ConfigureAwait(false);

            // Changed analyzer config documents
            await AddTextDocumentEditsAsync(
                projectChanges.SelectMany(pc => pc.GetChangedAnalyzerConfigDocuments()),
                newSolution.GetAnalyzerConfigDocument,
                solution.GetAnalyzerConfigDocument).ConfigureAwait(false);

            // Changed additional documents
            await AddTextDocumentEditsAsync(
                projectChanges.SelectMany(pc => pc.GetChangedAdditionalDocuments()),
                newSolution.GetAdditionalDocument,
                solution.GetAdditionalDocument).ConfigureAwait(false);
        }

        return new LSP.WorkspaceEdit { DocumentChanges = textDocumentEdits.ToArray() };

        async Task AddTextDocumentDeletionsAsync<TTextDocument>(
            IEnumerable<DocumentId> removedDocuments,
            Func<DocumentId, TTextDocument?> getOldDocument)
            where TTextDocument : TextDocument
        {
            foreach (var docId in removedDocuments)
            {
                var oldTextDoc = getOldDocument(docId);
                Contract.ThrowIfNull(oldTextDoc);

                var uri = oldTextDoc.GetURI();
                if (deletedDocumentUris.Add(uri))
                    textDocumentEdits.Add(new DeleteFile { DocumentUri = uri });
            }
        }

        async Task AddTextDocumentAdditionsAsync<TTextDocument>(
            IEnumerable<DocumentId> addedDocuments,
            Func<DocumentId, TTextDocument?> getNewDocument)
            where TTextDocument : TextDocument
        {
            foreach (var docId in addedDocuments)
            {
                var newTextDoc = getNewDocument(docId);
                Contract.ThrowIfNull(newTextDoc);

                DocumentUri? uri = null;
                if (newTextDoc.FilePath != null)
                {
                    uri = newTextDoc.GetURI();
                }
                else if (newTextDoc.Project.FilePath != null)
                {
                    // If there is no file path with the document, try to find its path by using its project file.
                    uri = newTextDoc.CreateUriForDocumentWithoutFilePath();
                }
                else
                {
                    // No document file path, and no project path. We don't know how to add this document. Throw.
                    Contract.Fail($"Can't find uri for document: {newTextDoc.Name}.");
                }

                if (!createdDocumentUris.Add(uri))
                    continue;

                textDocumentEdits.Add(new CreateFile { DocumentUri = uri });

                // And then give it content
                var newText = await newTextDoc.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var emptyDocumentRange = new LSP.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } };
                var edit = new TextEdit { Range = emptyDocumentRange, NewText = newText.ToString() };
                var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { DocumentUri = uri };
                textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = [edit] });
            }
        }

        async Task AddTextDocumentEditsAsync<TTextDocument>(
            IEnumerable<DocumentId> changedDocuments,
            Func<DocumentId, TTextDocument?> getNewDocument,
            Func<DocumentId, TTextDocument?> getOldDocument)
            where TTextDocument : TextDocument
        {
            foreach (var docId in changedDocuments)
            {
                var newTextDoc = getNewDocument(docId);
                var oldTextDoc = getOldDocument(docId);

                Contract.ThrowIfNull(oldTextDoc);
                Contract.ThrowIfNull(newTextDoc);

                // Linked contexts of every document kind share one physical-file edit.
                if (modifiedDocumentUris.Add(oldTextDoc.GetURI()))
                {
                    var oldText = await oldTextDoc.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

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
                        var newText = await newTextDoc.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                        textChanges = newText.GetTextChanges(oldText);
                    }

                    var edits = textChanges.Select(tc => new LSP.SumType<LSP.TextEdit, LSP.AnnotatedTextEdit>(ProtocolConversions.TextChangeToTextEdit(tc, oldText))).ToArray();

                    if (edits.Length > 0)
                    {
                        var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { DocumentUri = oldTextDoc.GetURI() };
                        textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = edits });
                    }

                    // Add Rename edit.
                    // Note:
                    // Client is expected to do the change in the order in which they are provided.
                    // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit
                    // So we would like to first edit the old document, then rename it.
                    if (HasDocumentRename(oldTextDoc, newTextDoc))
                    {
                        textDocumentEdits.Add(new RenameFile { OldDocumentUri = oldTextDoc.GetURI(), NewDocumentUri = GetRenamedDocumentUri(oldTextDoc, newTextDoc) });
                    }
                }
            }
        }
    }

    private static bool CanApplyWorkspaceEdit(Solution solution, Solution newSolution, ResourceOperationKind[] resourceOperations)
    {
        foreach (var change in newSolution.GetChanges(solution).GetProjectChanges())
        {
            // Workspace edits cannot represent changes to project, metadata, or analyzer references.
            // File creation, rename, and deletion are supported when the client advertises those capabilities.
            //
            // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit
            //
            // Tracked with: https://github.com/dotnet/roslyn/issues/65303
            if (change.GetAddedProjectReferences().Any() || change.GetRemovedProjectReferences().Any() ||
                change.GetAddedMetadataReferences().Any() || change.GetRemovedMetadataReferences().Any() ||
                change.GetAddedAnalyzerReferences().Any() || change.GetRemovedAnalyzerReferences().Any())
            {
                return false;
            }

            // If the client does not support delete operations, we cannot allow removed documents.
            if (!resourceOperations.Contains(ResourceOperationKind.Delete) &&
                change.GetRemovedDocuments().Concat(change.GetRemovedAdditionalDocuments()).Concat(change.GetRemovedAnalyzerConfigDocuments()).Any())
            {
                return false;
            }

            // If the client does not support create operations, we cannot allow added documents.
            if (!resourceOperations.Contains(ResourceOperationKind.Create) &&
                change.GetAddedDocuments().Concat(change.GetAddedAdditionalDocuments()).Concat(change.GetAddedAnalyzerConfigDocuments()).Any())
            {
                return false;
            }

            // If the client does not support rename operations, we cannot allow renamed documents.
            if (!resourceOperations.Contains(ResourceOperationKind.Rename) &&
                change.GetChangedDocuments().Concat(change.GetChangedAdditionalDocuments()).Concat(change.GetChangedAnalyzerConfigDocuments())
                    .Any(id => HasDocumentRename(solution.GetRequiredTextDocument(id), newSolution.GetRequiredTextDocument(id))))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool CanApplyFixAllChanges(Solution solution, Solution newSolution, ResourceOperationKind[] resourceOperations)
    {
        // Workspace edits cannot replace the solution or its projects or update solution-wide analyzer references.
        // Per-project references and client resource capabilities are checked by the shared validator.
        if (solution.Id != newSolution.Id ||
            !solution.ProjectIds.SequenceEqual(newSolution.ProjectIds) ||
            !solution.AnalyzerReferences.SequenceEqual(newSolution.AnalyzerReferences) ||
            !CanApplyWorkspaceEdit(solution, newSolution, resourceOperations))
        {
            return false;
        }

        // Track destinations across projects so separate resource operations cannot claim the same file.
        using var _ = PooledDictionary<DocumentUri, DocumentUri>.GetInstance(out var seenFileChanges);
        foreach (var change in newSolution.GetChanges(solution).GetProjectChanges())
        {
            // Removing a linked context would still emit DeleteFile for the shared source file.
            // Preserve that file while any of its original project contexts survives.
            foreach (var id in change.GetRemovedDocuments().Concat(change.GetRemovedAdditionalDocuments()).Concat(change.GetRemovedAnalyzerConfigDocuments()))
            {
                var removed = solution.GetRequiredTextDocument(id);
                if (!HasAbsolutePath(removed) ||
                    solution.GetDocumentIdsWithFilePath(removed.FilePath).Any(id => newSolution.GetTextDocument(id) is not null))
                {
                    return false;
                }
            }

            // A creation needs a usable URI and must not overwrite an existing workspace file.
            foreach (var id in change.GetAddedDocuments().Concat(change.GetAddedAdditionalDocuments()).Concat(change.GetAddedAnalyzerConfigDocuments()))
            {
                var added = newSolution.GetRequiredTextDocument(id);
                if (!HasAbsolutePath(added) &&
                    (added.FilePath is not null || added.Project.FilePath is not { } projectPath || !PathUtilities.IsAbsolute(projectPath)))
                {
                    return false;
                }

                // Match the converter's project-relative path fallback when the fix has not assigned a file path.
                var filePath = added.FilePath ?? added.GetFilePathForDocumentWithoutFilePath();
                var uri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
                if (solution.GetDocumentIdsWithFilePath(filePath).Any() ||
                    HasConflictingFileChange(uri, uri))
                {
                    return false;
                }
            }

            // Text edits can target an existing untitled URI, but renames still require filesystem paths.
            foreach (var id in change.GetChangedDocuments().Concat(change.GetChangedAdditionalDocuments()).Concat(change.GetChangedAnalyzerConfigDocuments()))
            {
                var oldDocument = solution.GetRequiredTextDocument(id);
                var newDocument = newSolution.GetRequiredTextDocument(id);
                if (!IsAbsolutePathOrUntitledUri(oldDocument.FilePath) || !IsAbsolutePathOrUntitledUri(newDocument.FilePath))
                    return false;

                if (!HasDocumentRename(oldDocument, newDocument))
                    continue;

                if (!HasAbsolutePath(oldDocument) || !HasAbsolutePath(newDocument))
                    return false;

                // A rename must not collide with another resource operation or an unrelated file in either snapshot.
                var destinationPath = oldDocument.FilePath != newDocument.FilePath
                    ? newDocument.FilePath
                    : newDocument.GetFilePathForRenamedDocument();
                var destination = GetRenamedDocumentUri(oldDocument, newDocument);
                var linkedIds = solution.GetDocumentIdsWithFilePath(oldDocument.FilePath);
                if (HasConflictingFileChange(destination, oldDocument.GetURI()) ||
                    solution.GetDocumentIdsWithFilePath(destinationPath).Any(id => !linkedIds.Contains(id)) ||
                    newSolution.GetDocumentIdsWithFilePath(destinationPath).Any(id => !linkedIds.Contains(id)))
                {
                    return false;
                }

                // The converter emits only one rename per source URI. Unchanged linked contexts do not need
                // matching metadata, but multiple requested renames must agree on their physical destination.
                foreach (var linkedId in linkedIds)
                {
                    var oldLinked = solution.GetRequiredTextDocument(linkedId);
                    var newLinked = newSolution.GetTextDocument(linkedId);
                    if (newLinked is not null && HasDocumentRename(oldLinked, newLinked) &&
                        (!HasAbsolutePath(newLinked) || GetRenamedDocumentUri(oldLinked, newLinked) != destination))
                    {
                        return false;
                    }
                }
            }
        }

        return true;

        bool HasConflictingFileChange(DocumentUri destination, DocumentUri original)
        {
            // Linked contexts may repeat the same operation, but different physical files cannot share a destination.
            if (seenFileChanges.TryGetValue(destination, out var previous) && previous != original)
                return true;

            seenFileChanges[destination] = original;
            return false;
        }
    }

    // Text edits may use an existing untitled URI; resource operations must still use filesystem paths.
    internal static bool IsAbsolutePathOrUntitledUri(string? filePath)
        => filePath is not null &&
            (PathUtilities.IsAbsolute(filePath) ||
             string.Equals(new DocumentUri(filePath).ParsedDocumentUri?.Scheme, "untitled", StringComparison.OrdinalIgnoreCase));

    private static bool HasAbsolutePath(TextDocument document)
        => PathUtilities.IsAbsolute(document.FilePath);

    private static bool HasDocumentRename(TextDocument oldDocument, TextDocument newDocument)
        => oldDocument.Name != newDocument.Name || oldDocument.FilePath != newDocument.FilePath;

    private static DocumentUri GetRenamedDocumentUri(TextDocument oldDocument, TextDocument newDocument)
        => oldDocument.FilePath != newDocument.FilePath ? newDocument.GetURI() : newDocument.GetUriForRenamedDocument();
}
