// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeActions
{
    [Export(typeof(ICodeActionEditHandlerService))]
    internal class CodeActionEditHandlerService : ICodeActionEditHandlerService
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

        public ITextBufferAssociatedViewService AssociatedViewService
        {
            get { return _associatedViewService; }
        }

        public SolutionPreviewResult GetPreviews(Workspace workspace, IEnumerable<CodeActionOperation> operations, CancellationToken cancellationToken)
        {
            if (operations == null)
            {
                return null;
            }

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
                        return preview;
                    }
                }

                var previewOp = op as PreviewOperation;
                if (previewOp != null)
                {
                    return new SolutionPreviewResult(new List<SolutionPreviewItem>() { new SolutionPreviewItem(projectId: null, documentId: null, lazyPreview: c => previewOp.GetPreviewAsync(c)) });
                }

                var title = op.Title;
                if (title != null)
                {
                    return new SolutionPreviewResult(new List<SolutionPreviewItem>() { new SolutionPreviewItem(projectId: null, documentId: null, lazyPreview: c => Task.FromResult<object>(title)) });
                }
            }

            return null;
        }

        public void Apply(Workspace workspace, Document fromDocument, IEnumerable<CodeActionOperation> operations, string title, CancellationToken cancellationToken)
        {
            if (_renameService.ActiveSession != null)
            {
                workspace.Services.GetService<INotificationService>()?.SendNotification(
                    EditorFeaturesResources.CannotApplyOperationWhileRenameSessionIsActive,
                    severity: NotificationSeverity.Error);
                return;
            }

#if DEBUG
            var documentErrorLookup = new HashSet<DocumentId>();
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (!document.HasAnyErrorsAsync(cancellationToken).WaitAndGetResult(cancellationToken))
                    {
                        documentErrorLookup.Add(document.Id);
                    }
                }
            }
#endif

            var oldSolution = workspace.CurrentSolution;
            Solution updatedSolution = oldSolution;

            foreach (var operation in operations)
            {
                var applyChanges = operation as ApplyChangesOperation;
                if (applyChanges == null)
                {
                    operation.Apply(workspace, cancellationToken);
                    continue;
                }

                // there must be only one ApplyChangesOperation, we will ignore all other ones.
                if (updatedSolution == oldSolution)
                {
                    updatedSolution = applyChanges.ChangedSolution;
                    var projectChanges = updatedSolution.GetChanges(oldSolution).GetProjectChanges();
                    var changedDocuments = projectChanges.SelectMany(pd => pd.GetChangedDocuments());
                    var changedAdditionalDocuments = projectChanges.SelectMany(pd => pd.GetChangedAdditionalDocuments());
                    var changedFiles = changedDocuments.Concat(changedAdditionalDocuments);

                    // 0 file changes
                    if (!changedFiles.Any())
                    {
                        operation.Apply(workspace, cancellationToken);
                        continue;
                    }

                    // 1 file change
                    SourceText text = null;
                    if (!changedFiles.Skip(1).Any())
                    {
                        if (changedDocuments.Any())
                        {
                            text = oldSolution.GetDocument(changedDocuments.Single()).GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                        }
                        else if (changedAdditionalDocuments.Any())
                        {
                            text = oldSolution.GetAdditionalDocument(changedAdditionalDocuments.Single()).GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                        }
                    }

                    if (text != null)
                    {
                        using (workspace.Services.GetService<ISourceTextUndoService>().RegisterUndoTransaction(text, title))
                        {
                            operation.Apply(workspace, cancellationToken);
                            continue;
                        }
                    }

                    // multiple file changes
                    using (var undoTransaction = workspace.OpenGlobalUndoTransaction(title))
                    {
                        operation.Apply(workspace, cancellationToken);

                        // link current file in the global undo transaction
                        if (fromDocument != null)
                        {
                            undoTransaction.AddDocument(fromDocument.Id);
                        }

                        undoTransaction.Commit();
                    }

                    continue;
                }
            }

#if DEBUG
            foreach (var project in workspace.CurrentSolution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    if (documentErrorLookup.Contains(document.Id))
                    {
                        document.VerifyNoErrorsAsync("CodeAction introduced error in error-free code", cancellationToken).Wait(cancellationToken);
                    }
                }
            }
#endif

            TryStartRenameSession(workspace, oldSolution, updatedSolution, cancellationToken);
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

                var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

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
                    var latestRoot = latestDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                    SyntaxNodeOrToken resolvedRenameToken;
                    if (pathToRenameToken.TryResolve(latestRoot, out resolvedRenameToken) &&
                        resolvedRenameToken.IsToken)
                    {
                        var editorWorkspace = workspace;
                        var navigationService = editorWorkspace.Services.GetService<IDocumentNavigationService>();
                        if (navigationService.TryNavigateToSpan(editorWorkspace, documentId, resolvedRenameToken.Span))
                        {
                            var openDocument = workspace.CurrentSolution.GetDocument(documentId);
                            var openRoot = openDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);

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
