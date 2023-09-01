// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Resolves a code action by filling out its Edit property. The handler is triggered only when a user hovers over a
    /// code action. This system allows the basic code action data to be computed quickly, and the complex data, to be
    /// computed only when necessary (i.e. when hovering/previewing a code action).
    /// <para>
    /// This system only supports text edits to documents.  In the future, supporting complex edits (including changes to
    /// project files) would be desirable.
    /// </para>
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(CodeActionResolveHandler)), Shared]
    [Method(LSP.Methods.CodeActionResolveName)]
    internal class CodeActionResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeAction, LSP.CodeAction>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionResolveHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeAction request)
            => ((JToken)request.Data!).ToObject<CodeActionResolveData>()!.TextDocument;

        public async Task<LSP.CodeAction> HandleRequestAsync(LSP.CodeAction codeAction, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var solution = document.Project.Solution;

            var data = ((JToken)codeAction.Data!).ToObject<CodeActionResolveData>();
            Assumes.Present(data);

            var options = _globalOptions.GetCodeActionOptionsProvider();

            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                document,
                data.Range,
                options,
                _codeFixService,
                _codeRefactoringService,
                cancellationToken).ConfigureAwait(false);

            var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(data.UniqueIdentifier, codeActions);
            Contract.ThrowIfNull(codeActionToResolve);

            var operations = await codeActionToResolve.GetOperationsAsync(
                solution, new ProgressTracker(), cancellationToken).ConfigureAwait(false);

            // TO-DO: We currently must execute code actions which add new documents on the server as commands,
            // since there is no LSP support for adding documents yet. In the future, we should move these actions
            // to execute on the client.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1147293/

            var textDiffService = solution.Services.GetService<IDocumentTextDifferencingService>();

            using var _1 = ArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>.GetInstance(out var textDocumentEdits);
            using var _2 = PooledHashSet<DocumentId>.GetInstance(out var modifiedDocumentIds);

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
                    context.TraceInformation($"Skipping code action operation for '{data.UniqueIdentifier}'.  It was a '{option.GetType().FullName}'");
                    continue;
                }

                var changes = applyChangesOperation.ChangedSolution.GetChanges(solution);
                var newSolution = await applyChangesOperation.ChangedSolution.WithMergedLinkedFileChangesAsync(solution, changes, cancellationToken: cancellationToken).ConfigureAwait(false);
                changes = newSolution.GetChanges(solution);

                var projectChanges = changes.GetProjectChanges();

                // Don't apply changes in the presence of any non-document changes for now.  Note though that LSP does
                // support additional functionality (like create/rename/delete file).  Once VS updates their LSP client
                // impl to support this, we should add that support here.
                //
                // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit
                //
                // Tracked with: https://github.com/dotnet/roslyn/issues/65303
                foreach (var projectChange in projectChanges)
                {
                    if (projectChange.GetAddedProjectReferences().Any()
                        || projectChange.GetRemovedProjectReferences().Any()
                        || projectChange.GetAddedMetadataReferences().Any()
                        || projectChange.GetRemovedMetadataReferences().Any()
                        || projectChange.GetAddedAnalyzerReferences().Any()
                        || projectChange.GetRemovedAnalyzerReferences().Any())
                    {
                        // Changes to references are not currently supported
                        codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = Array.Empty<TextDocumentEdit>() };
                        return codeAction;
                    }

                    if (projectChange.GetRemovedDocuments().Any()
                        || projectChange.GetRemovedAdditionalDocuments().Any()
                        || projectChange.GetRemovedAnalyzerConfigDocuments().Any())
                    {
                        if (context.GetRequiredClientCapabilities() is not { Workspace.WorkspaceEdit.ResourceOperations: { } resourceOperations }
                            || !resourceOperations.Contains(ResourceOperationKind.Delete))
                        {
                            // Removing documents is not supported by this workspace
                            codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = Array.Empty<TextDocumentEdit>() };
                            return codeAction;
                        }
                    }

                    if (projectChange.GetAddedDocuments().Any()
                        || projectChange.GetAddedAdditionalDocuments().Any()
                        || projectChange.GetAddedAnalyzerConfigDocuments().Any())
                    {
                        if (context.GetRequiredClientCapabilities() is not { Workspace.WorkspaceEdit.ResourceOperations: { } resourceOperations }
                            || !resourceOperations.Contains(ResourceOperationKind.Create))
                        {
                            // Adding documents is not supported by this workspace
                            codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = Array.Empty<TextDocumentEdit>() };
                            return codeAction;
                        }
                    }

                    if (projectChange.GetChangedDocuments().Any(docId => HasDocumentNameChange(docId, newSolution, solution))
                        || projectChange.GetChangedAdditionalDocuments().Any(docId => HasDocumentNameChange(docId, newSolution, solution)
                        || projectChange.GetChangedAnalyzerConfigDocuments().Any(docId => HasDocumentNameChange(docId, newSolution, solution))))
                    {
                        if (context.GetRequiredClientCapabilities() is not { Workspace.WorkspaceEdit.ResourceOperations: { } resourceOperations }
                            || !resourceOperations.Contains(ResourceOperationKind.Rename))
                        {
                            // Rename documents is not supported by this workspace
                            codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = Array.Empty<TextDocumentEdit>() };
                            return codeAction;
                        }
                    }
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

            codeAction.Edit = new LSP.WorkspaceEdit { DocumentChanges = textDocumentEdits.ToArray() };

            return codeAction;

            Task AddTextDocumentDeletionsAsync<TTextDocument>(
                IEnumerable<DocumentId> removedDocuments,
                Func<DocumentId, TTextDocument?> getOldDocument)
                where TTextDocument : TextDocument
            {
                foreach (var docId in removedDocuments)
                {
                    var oldTextDoc = getOldDocument(docId);
                    Contract.ThrowIfNull(oldTextDoc);

                    textDocumentEdits.Add(new DeleteFile { Uri = oldTextDoc.GetURI() });
                }

                return Task.CompletedTask;
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

                    Uri? uri = null;
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

                    textDocumentEdits.Add(new CreateFile { Uri = uri });

                    // And then give it content
                    var newText = await newTextDoc.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                    var emptyDocumentRange = new LSP.Range { Start = new Position { Line = 0, Character = 0 }, End = new Position { Line = 0, Character = 0 } };
                    var edit = new TextEdit { Range = emptyDocumentRange, NewText = newText.ToString() };
                    var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { Uri = uri };
                    textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = new[] { edit } });
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

                    // For linked documents, only generated the document edit once.
                    if (modifiedDocumentIds.Add(docId))
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

                        var edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray();

                        if (edits.Length > 0)
                        {
                            var documentIdentifier = new OptionalVersionedTextDocumentIdentifier { Uri = newTextDoc.GetURI() };
                            textDocumentEdits.Add(new TextDocumentEdit { TextDocument = documentIdentifier, Edits = edits });
                        }

                        // Add Rename edit.
                        // Note:
                        // Client is expected to do the change in the order in which they are provided.
                        // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceEdit
                        // So we would like to first edit the old document, then rename it.
                        if (oldTextDoc.Name != newTextDoc.Name)
                        {
                            textDocumentEdits.Add(new RenameFile() { OldUri = oldTextDoc.GetURI(), NewUri = newTextDoc.GetUriForRenamedDocument() });
                        }

                        var linkedDocuments = solution.GetRelatedDocumentIds(docId);
                        modifiedDocumentIds.AddRange(linkedDocuments);
                    }
                }
            }
        }

        private static bool HasDocumentNameChange(DocumentId documentId, Solution newSolution, Solution oldSolution)
        {
            var newDocument = newSolution.GetRequiredTextDocument(documentId);
            var oldDocument = oldSolution.GetRequiredTextDocument(documentId);
            return newDocument.Name != oldDocument.Name;
        }
    }
}
