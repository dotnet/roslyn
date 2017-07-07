﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeActions
{
    [Export(typeof(ICodeActionEditHandlerService))]
    internal class CodeActionEditHandlerService : ForegroundThreadAffinitizedObject, ICodeActionEditHandlerService
    {
        private readonly IPreviewFactoryService _previewService;
        private readonly IInlineRenameService _renameService;
        private readonly ITextBufferAssociatedViewService _associatedViewService;

        [ImportingConstructor]
        public CodeActionEditHandlerService(
            IPreviewFactoryService previewService,
            IInlineRenameService renameService,
            ITextBufferAssociatedViewService associatedViewService)
        {
            _previewService = previewService;
            _renameService = renameService;
            _associatedViewService = associatedViewService;
        }

        public ITextBufferAssociatedViewService AssociatedViewService => _associatedViewService;

        public SolutionPreviewResult GetPreviews(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations, CancellationToken cancellationToken)
        {
            if (operations.IsDefaultOrEmpty)
            {
                return null;
            }

            SolutionPreviewResult currentResult = null;

            foreach (var op in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var applyChanges = op as ApplyChangesOperation;
                if (applyChanges != null)
                {
                    var oldSolution = workspace.CurrentSolution;
                    var newSolution = applyChanges.ChangedSolution.WithMergedLinkedFileChangesAsync(oldSolution, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
                    var preview = _previewService.GetSolutionPreviews(
                        oldSolution, newSolution, cancellationToken);

                    if (preview != null && !preview.IsEmpty)
                    {
                        currentResult = SolutionPreviewResult.Merge(currentResult, preview);
                        continue;
                    }
                }

                var previewOp = op as PreviewOperation;
                if (previewOp != null)
                {
                    currentResult = SolutionPreviewResult.Merge(currentResult,
                        new SolutionPreviewResult(new SolutionPreviewItem(
                            projectId: null, documentId: null,
                            lazyPreview: c => previewOp.GetPreviewAsync(c))));
                    continue;
                }

                var title = op.Title;

                if (title != null)
                {
                    currentResult = SolutionPreviewResult.Merge(currentResult,
                        new SolutionPreviewResult(new SolutionPreviewItem(
                            projectId: null, documentId: null, text: title)));
                    continue;
                }
            }

            return currentResult;
        }

        public async Task ApplyAsync(
            Workspace workspace, Document fromDocument,
            ImmutableArray<CodeActionOperation> operations,
            string title, IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            this.AssertIsForeground();

            if (operations.IsDefaultOrEmpty)
            {
                return;
            }

            if (_renameService.ActiveSession != null)
            {
                workspace.Services.GetService<INotificationService>()?.SendNotification(
                    EditorFeaturesResources.Cannot_apply_operation_while_a_rename_session_is_active,
                    severity: NotificationSeverity.Error);
                return;
            }

#if DEBUG
            var documentErrorLookup = new HashSet<DocumentId>();
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    // ConfigureAwait(true) so we come back to the same thread as 
                    // we do all application on the UI thread.                    
                    if (!await document.HasAnyErrorsAsync(cancellationToken).ConfigureAwait(true))
                    {
                        documentErrorLookup.Add(document.Id);
                    }
                }
            }
#endif

            var oldSolution = workspace.CurrentSolution;

            // Determine if we're making a simple text edit to a single file or not.
            // If we're not, then we need to make a linked global undo to wrap the 
            // application of these operations.  This way we should be able to undo 
            // them all with one user action.
            //
            // The reason we don't always create a gobal undo is that a global undo
            // forces all files to save.  And that's rather a heavyweight and 
            // unexpected experience for users (for the common case where a single 
            // file got edited).
            var singleChangedDocument = TryGetSingleChangedText(oldSolution, operations);
            if (singleChangedDocument != null)
            {
                // ConfigureAwait(true) so we come back to the same thread as 
                // we do all application on the UI thread.
                var text = await singleChangedDocument.GetTextAsync(cancellationToken).ConfigureAwait(true);

                using (workspace.Services.GetService<ISourceTextUndoService>().RegisterUndoTransaction(text, title))
                {
                    operations.Single().Apply(workspace, cancellationToken);
                }
            }
            else
            {
                // More than just a single document changed.  Make a global undo to run 
                // all the changes under.
                using (var transaction = workspace.OpenGlobalUndoTransaction(title))
                {
                    // ConfigureAwait(true) so we come back to the same thread as 
                    // we do all application on the UI thread.
                    ProcessOperations(
                        workspace, operations, progressTracker,
                        cancellationToken);

                    // link current file in the global undo transaction
                    if (fromDocument != null)
                    {
                        transaction.AddDocument(fromDocument.Id);
                    }

                    transaction.Commit();
                }
            }

            var updatedSolution = operations.OfType<ApplyChangesOperation>().FirstOrDefault()?.ChangedSolution ?? oldSolution;
            TryStartRenameSession(workspace, oldSolution, updatedSolution, cancellationToken);
        }

        private TextDocument TryGetSingleChangedText(
            Solution oldSolution, ImmutableArray<CodeActionOperation> operationsList)
        {
            Debug.Assert(operationsList.Length > 0);
            if (operationsList.Length > 1)
            {
                return null;
            }

            var applyOperation = operationsList.Single() as ApplyChangesOperation;
            if (applyOperation == null)
            {
                return null;
            }

            var newSolution = applyOperation.ChangedSolution;
            var changes = newSolution.GetChanges(oldSolution);

            if (changes.GetAddedProjects().Any() ||
                changes.GetRemovedProjects().Any())
            {
                return null;
            }

            var projectChanges = changes.GetProjectChanges().ToImmutableArray();
            if (projectChanges.Length != 1)
            {
                return null;
            }

            var projectChange = projectChanges.Single();
            if (projectChange.GetAddedAdditionalDocuments().Any() ||
                projectChange.GetAddedAnalyzerReferences().Any() ||
                projectChange.GetAddedDocuments().Any() ||
                projectChange.GetAddedMetadataReferences().Any() ||
                projectChange.GetAddedProjectReferences().Any() ||
                projectChange.GetRemovedAdditionalDocuments().Any() ||
                projectChange.GetRemovedAnalyzerReferences().Any() ||
                projectChange.GetRemovedDocuments().Any() ||
                projectChange.GetRemovedMetadataReferences().Any() ||
                projectChange.GetRemovedProjectReferences().Any())
            {
                return null;
            }

            var changedAdditionalDocuments = projectChange.GetChangedAdditionalDocuments().ToImmutableArray();
            var changedDocuments = projectChange.GetChangedDocuments().ToImmutableArray();

            if (changedAdditionalDocuments.Length + changedDocuments.Length != 1)
            {
                return null;
            }

            return changedDocuments.Length == 1
                ? oldSolution.GetDocument(changedDocuments[0])
                : oldSolution.GetAdditionalDocument(changedAdditionalDocuments[0]);
        }

        private static void ProcessOperations(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var seenApplyChanges = false;
            foreach (var operation in operations)
            {
                if (operation is ApplyChangesOperation)
                {
                    // there must be only one ApplyChangesOperation, we will ignore all other ones.
                    if (seenApplyChanges)
                    {
                        continue;
                    }

                    seenApplyChanges = true;
                }

                operation.TryApply(workspace, progressTracker, cancellationToken);
            }
        }

        private void TryStartRenameSession(Workspace workspace, Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            var changedDocuments = newSolution.GetChangedDocuments(oldSolution);
            foreach (var documentId in changedDocuments)
            {
                var document = newSolution.GetDocument(documentId);
                if (!document.SupportsSyntaxTree)
                {
                    continue;
                }

                var root = document.GetSyntaxRootSynchronously(cancellationToken);

                var renameTokenOpt = root.GetAnnotatedNodesAndTokens(RenameAnnotation.Kind)
                                         .Where(s => s.IsToken)
                                         .Select(s => s.AsToken())
                                         .FirstOrNullable();

                if (renameTokenOpt.HasValue)
                {
                    // It's possible that the workspace's current solution is not the same as
                    // newSolution. This can happen if the workspace host performs other edits
                    // during ApplyChanges, such as in the Venus scenario where indentation and
                    // formatting can happen. To work around this, we create a SyntaxPath to the
                    // rename token in the newSolution and resolve it to the current solution.

                    var pathToRenameToken = new SyntaxPath(renameTokenOpt.Value);
                    var latestDocument = workspace.CurrentSolution.GetDocument(documentId);
                    var latestRoot = latestDocument.GetSyntaxRootSynchronously(cancellationToken);
                    if (pathToRenameToken.TryResolve(latestRoot, out var resolvedRenameToken) &&
                        resolvedRenameToken.IsToken)
                    {
                        var editorWorkspace = workspace;
                        var navigationService = editorWorkspace.Services.GetService<IDocumentNavigationService>();
                        if (navigationService.TryNavigateToSpan(editorWorkspace, documentId, resolvedRenameToken.Span))
                        {
                            var openDocument = workspace.CurrentSolution.GetDocument(documentId);
                            var openRoot = openDocument.GetSyntaxRootSynchronously(cancellationToken);

                            // NOTE: We need to resolve the syntax path again in case VB line commit kicked in
                            // due to the navigation.

                            // TODO(DustinCa): We still have a potential problem here with VB line commit,
                            // because it can insert tokens and all sorts of other business, which could
                            // wind up with us not being able to resolve the token.
                            if (pathToRenameToken.TryResolve(openRoot, out resolvedRenameToken) &&
                                resolvedRenameToken.IsToken)
                            {
                                var snapshot = openDocument.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).FindCorrespondingEditorTextSnapshot();
                                if (snapshot != null)
                                {
                                    _renameService.StartInlineSession(openDocument, resolvedRenameToken.AsToken().Span, cancellationToken);
                                }
                            }
                        }
                    }

                    return;
                }
            }
        }
    }
}
