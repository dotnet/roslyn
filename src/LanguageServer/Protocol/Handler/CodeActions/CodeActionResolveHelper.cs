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
            // Don't apply changes in the presence of any non-document changes for now.  Note though that LSP does
            // support additional functionality (like create/rename/delete file).  Once VS updates their LSP client
            // impl to support this, we should add that support here.
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
        // Reject solution-wide changes, generated-document overrides, and operations the client cannot apply.
        // Unlike individual code actions, an aggregate must not silently omit any part of an accepted fix.
        if (solution.Id != newSolution.Id ||
            solution.FilePath != newSolution.FilePath ||
            !solution.ProjectIds.SequenceEqual(newSolution.ProjectIds) ||
            !solution.AnalyzerReferences.SequenceEqual(newSolution.AnalyzerReferences) ||
            solution.Options != newSolution.Options ||
            solution.FallbackAnalyzerOptions != newSolution.FallbackAnalyzerOptions ||
            newSolution.GetChanges(solution).GetExplicitlyChangedSourceGeneratedDocuments().Any() ||
            !CanApplyWorkspaceEdit(solution, newSolution, resourceOperations))
        {
            return false;
        }

        // Track destinations across projects so separate resource operations cannot claim the same file.
        using var _ = PooledDictionary<DocumentUri, DocumentUri>.GetInstance(out var resourceDestinations);
        foreach (var change in newSolution.GetChanges(solution).GetProjectChanges())
        {
            var oldProject = change.OldProject;
            var newProject = change.NewProject;

            // Workspace edits cannot update project configuration or reorder the surviving source documents.
            if (HasProjectConfigurationChange(oldProject, newProject) ||
                !oldProject.DocumentIds.Where(newProject.ContainsDocument).SequenceEqual(newProject.DocumentIds.Where(oldProject.ContainsDocument)))
            {
                return false;
            }

            // A deletion removes a physical file, not just a project entry. All linked contexts must disappear,
            // and no new document may reuse the deleted file's path.
            foreach (var id in change.GetRemovedDocuments().Concat(change.GetRemovedAdditionalDocuments()).Concat(change.GetRemovedAnalyzerConfigDocuments()))
            {
                var removed = solution.GetRequiredTextDocument(id);
                if (!HasUsableFilePath(removed) ||
                    solution.GetDocumentIdsWithFilePath(removed.FilePath).Any(id => newSolution.GetTextDocument(id) is not null) ||
                    newSolution.GetDocumentIdsWithFilePath(removed.FilePath).Any())
                {
                    return false;
                }
            }

            // A creation needs a usable URI and must not overwrite an existing workspace file.
            foreach (var id in change.GetAddedDocuments().Concat(change.GetAddedAdditionalDocuments()).Concat(change.GetAddedAnalyzerConfigDocuments()))
            {
                var added = newSolution.GetRequiredTextDocument(id);
                if (!HasUsableFilePath(added) &&
                    (added.FilePath is not null || added.Project.FilePath is not { } projectPath || !PathUtilities.IsAbsolute(projectPath)))
                {
                    return false;
                }

                // Match the converter's project-relative path fallback when the fix has not assigned a file path.
                var uri = added.FilePath is null ? added.CreateUriForDocumentWithoutFilePath() : added.GetURI();
                if (solution.GetDocumentIdsWithFilePath(uri.GetDocumentFilePathFromUri()).Any() ||
                    !RegisterDestination(uri, uri))
                {
                    return false;
                }
            }

            // Existing files can change text or be renamed, but their logical folders and compilation-related
            // document metadata cannot be represented by a workspace edit.
            foreach (var id in change.GetChangedDocuments().Concat(change.GetChangedAdditionalDocuments()).Concat(change.GetChangedAnalyzerConfigDocuments()))
            {
                var oldDocument = solution.GetRequiredTextDocument(id);
                var newDocument = newSolution.GetRequiredTextDocument(id);
                var oldAttributes = oldDocument.State.Attributes;
                var newAttributes = newDocument.State.Attributes;
                if (!HasUsableFilePath(oldDocument) || !HasUsableFilePath(newDocument) ||
                    !oldAttributes.Folders.SequenceEqual(newAttributes.Folders) ||
                    oldAttributes.SourceCodeKind != newAttributes.SourceCodeKind ||
                    oldAttributes.IsGenerated != newAttributes.IsGenerated ||
                    oldAttributes.DesignTimeOnly != newAttributes.DesignTimeOnly)
                {
                    return false;
                }

                if (!HasDocumentRename(oldDocument, newDocument))
                    continue;

                // A rename must not collide with another resource operation or an unrelated file in either snapshot.
                var destination = GetRenamedDocumentUri(oldDocument, newDocument);
                var linkedIds = solution.GetDocumentIdsWithFilePath(oldDocument.FilePath);
                if (!RegisterDestination(destination, oldDocument.GetURI()) ||
                    solution.GetDocumentIdsWithFilePath(destination.GetDocumentFilePathFromUri()).Any(id => !linkedIds.Contains(id)) ||
                    newSolution.GetDocumentIdsWithFilePath(destination.GetDocumentFilePathFromUri()).Any(id => !linkedIds.Contains(id)))
                {
                    return false;
                }

                // A physical rename must move every project context to the same destination.
                foreach (var linkedId in linkedIds)
                {
                    var oldLinked = solution.GetRequiredTextDocument(linkedId);
                    var newLinked = newSolution.GetTextDocument(linkedId);
                    if (newLinked is null || !HasUsableFilePath(newLinked) ||
                        (HasDocumentRename(oldLinked, newLinked) ? GetRenamedDocumentUri(oldLinked, newLinked) : newLinked.GetURI()) != destination)
                    {
                        return false;
                    }
                }
            }
        }

        return true;

        bool RegisterDestination(DocumentUri destination, DocumentUri original)
        {
            // Linked contexts may repeat the same operation, but different physical files cannot share a destination.
            if (resourceDestinations.TryGetValue(destination, out var previous) && previous != original)
                return false;

            resourceDestinations[destination] = original;
            return true;
        }
    }

    private static bool HasUsableFilePath(TextDocument document)
        => document.FilePath is { } path && PathUtilities.IsAbsolute(path);

    private static bool HasDocumentRename(TextDocument oldDocument, TextDocument newDocument)
        => oldDocument.Name != newDocument.Name || oldDocument.FilePath != newDocument.FilePath;

    private static DocumentUri GetRenamedDocumentUri(TextDocument oldDocument, TextDocument newDocument)
        => oldDocument.FilePath != newDocument.FilePath ? newDocument.GetURI() : newDocument.GetUriForRenamedDocument();

    private static bool HasProjectConfigurationChange(Project oldProject, Project newProject)
    {
        var oldAttributes = oldProject.State.Attributes;
        var newAttributes = newProject.State.Attributes;
        // Project versions and the syntax-tree options provider also change when analyzer-config text changes.
        return oldAttributes.Name != newAttributes.Name ||
            oldAttributes.AssemblyName != newAttributes.AssemblyName ||
            oldAttributes.Language != newAttributes.Language ||
            oldAttributes.FilePath != newAttributes.FilePath ||
            oldAttributes.OutputFilePath != newAttributes.OutputFilePath ||
            oldAttributes.OutputRefFilePath != newAttributes.OutputRefFilePath ||
            oldAttributes.CompilationOutputInfo != newAttributes.CompilationOutputInfo ||
            oldAttributes.DefaultNamespace != newAttributes.DefaultNamespace ||
            oldAttributes.ChecksumAlgorithm != newAttributes.ChecksumAlgorithm ||
            oldAttributes.IsSubmission != newAttributes.IsSubmission ||
            oldAttributes.HasAllInformation != newAttributes.HasAllInformation ||
            oldAttributes.RunAnalyzers != newAttributes.RunAnalyzers ||
            oldAttributes.TelemetryId != newAttributes.TelemetryId ||
            oldAttributes.HasSdkCodeStyleAnalyzers != newAttributes.HasSdkCodeStyleAnalyzers ||
            oldProject.State.ProjectInfo.HostObjectType != newProject.State.ProjectInfo.HostObjectType ||
            !Equals(oldProject.CompilationOptions?.WithSyntaxTreeOptionsProvider(newProject.CompilationOptions?.SyntaxTreeOptionsProvider), newProject.CompilationOptions) ||
            !Equals(oldProject.ParseOptions, newProject.ParseOptions) ||
            !oldProject.State.ProjectReferences.SequenceEqual(newProject.State.ProjectReferences) ||
            !oldProject.MetadataReferences.SequenceEqual(newProject.MetadataReferences) ||
            !oldProject.AnalyzerReferences.SequenceEqual(newProject.AnalyzerReferences);
    }
}
