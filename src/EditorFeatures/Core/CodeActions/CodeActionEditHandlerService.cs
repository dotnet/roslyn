// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    [Export(typeof(ICodeActionEditHandlerService))]
    internal class CodeActionEditHandlerService : ForegroundThreadAffinitizedObject, ICodeActionEditHandlerService
    {
        private readonly IPreviewFactoryService _previewService;
        private readonly IInlineRenameService _renameService;
        private readonly ITextBufferAssociatedViewService _associatedViewService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionEditHandlerService(
            IThreadingContext threadingContext,
            IPreviewFactoryService previewService,
            IInlineRenameService renameService,
            ITextBufferAssociatedViewService associatedViewService)
            : base(threadingContext)
        {
            _previewService = previewService;
            _renameService = renameService;
            _associatedViewService = associatedViewService;
        }

        public ITextBufferAssociatedViewService AssociatedViewService => _associatedViewService;

        public async Task<SolutionPreviewResult?> GetPreviewsAsync(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations, CancellationToken cancellationToken)
        {
            if (operations.IsDefaultOrEmpty)
                return null;

            SolutionPreviewResult? currentResult = null;

            foreach (var op in operations)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (op is ApplyChangesOperation applyChanges)
                {
                    var oldSolution = workspace.CurrentSolution;
                    var newSolution = await applyChanges.ChangedSolution.WithMergedLinkedFileChangesAsync(
                        oldSolution, cancellationToken: cancellationToken).ConfigureAwait(false);
                    var preview = _previewService.GetSolutionPreviews(
                        oldSolution, newSolution, cancellationToken);

                    if (preview != null && !preview.IsEmpty)
                    {
                        currentResult = SolutionPreviewResult.Merge(currentResult, preview);
                        continue;
                    }
                }

                if (op is PreviewOperation previewOp)
                {
                    currentResult = SolutionPreviewResult.Merge(currentResult,
                        new SolutionPreviewResult(ThreadingContext, new SolutionPreviewItem(
                            projectId: null, documentId: null,
                            lazyPreview: c => previewOp.GetPreviewAsync(c))));
                    continue;
                }

                var title = op.Title;

                if (title != null)
                {
                    currentResult = SolutionPreviewResult.Merge(currentResult,
                        new SolutionPreviewResult(ThreadingContext, new SolutionPreviewItem(
                            projectId: null, documentId: null, text: title)));
                    continue;
                }
            }

            return currentResult;
        }

        public async Task<bool> ApplyAsync(
            Workspace workspace, Document? fromDocument,
            ImmutableArray<CodeActionOperation> operations,
            string title, IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            // Much of the work we're going to do will be on the UI thread, so switch there preemptively.
            // When we get to the expensive parts we can do in the BG then we'll switch over to relinquish
            // the UI thread.
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (operations.IsDefaultOrEmpty)
            {
                return _renameService.ActiveSession is null;
            }

            if (_renameService.ActiveSession != null)
            {
                workspace.Services.GetService<INotificationService>()?.SendNotification(
                    EditorFeaturesResources.Cannot_apply_operation_while_a_rename_session_is_active,
                    severity: NotificationSeverity.Error);
                return false;
            }

            var oldSolution = workspace.CurrentSolution;

            var applied = false;

            // Determine if we're making a simple text edit to a single file or not.
            // If we're not, then we need to make a linked global undo to wrap the
            // application of these operations.  This way we should be able to undo
            // them all with one user action.
            //
            // The reason we don't always create a global undo is that a global undo
            // forces all files to save.  And that's rather a heavyweight and
            // unexpected experience for users (for the common case where a single
            // file got edited).
            var singleChangedDocument = TryGetSingleChangedText(oldSolution, operations);
            if (singleChangedDocument != null)
            {
                var text = await singleChangedDocument.GetTextAsync(cancellationToken).ConfigureAwait(true);

                using (workspace.Services.GetRequiredService<ISourceTextUndoService>().RegisterUndoTransaction(text, title))
                {
                    try
                    {
                        this.AssertIsForeground();

                        applied = await operations.Single().TryApplyAsync(
                            workspace, progressTracker, cancellationToken).ConfigureAwait(true);
                    }
                    catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            }
            else
            {
                // More than just a single document changed.  Make a global undo to run
                // all the changes under.
                using var transaction = workspace.OpenGlobalUndoTransaction(title);

                // link current file in the global undo transaction
                // Do this before processing operations, since that can change
                // documentIds.
                if (fromDocument != null)
                {
                    transaction.AddDocument(fromDocument.Id);
                }

                try
                {
                    // Come back to the UI thread after processing the operations so we can commit the transaction
                    applied = await ProcessOperationsAsync(
                        workspace, operations, progressTracker,
                        cancellationToken).ConfigureAwait(true);
                }
                catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable;
                }

                transaction.Commit();
            }

            var updatedSolution = operations.OfType<ApplyChangesOperation>().FirstOrDefault()?.ChangedSolution ?? oldSolution;
            await TryNavigateToLocationOrStartRenameSessionAsync(
                workspace, oldSolution, updatedSolution, cancellationToken).ConfigureAwait(false);
            return applied;
        }

        private static TextDocument? TryGetSingleChangedText(
            Solution oldSolution, ImmutableArray<CodeActionOperation> operationsList)
        {
            Debug.Assert(operationsList.Length > 0);
            if (operationsList.Length > 1)
                return null;

            if (operationsList.Single() is not ApplyChangesOperation applyOperation)
                return null;

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
                projectChange.GetAddedAnalyzerConfigDocuments().Any() ||
                projectChange.GetAddedMetadataReferences().Any() ||
                projectChange.GetAddedProjectReferences().Any() ||
                projectChange.GetRemovedAdditionalDocuments().Any() ||
                projectChange.GetRemovedAnalyzerReferences().Any() ||
                projectChange.GetRemovedDocuments().Any() ||
                projectChange.GetRemovedAnalyzerConfigDocuments().Any() ||
                projectChange.GetRemovedMetadataReferences().Any() ||
                projectChange.GetRemovedProjectReferences().Any())
            {
                return null;
            }

            var changedAdditionalDocuments = projectChange.GetChangedAdditionalDocuments().ToImmutableArray();
            var changedDocuments = projectChange.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).ToImmutableArray();
            var changedAnalyzerConfigDocuments = projectChange.GetChangedAnalyzerConfigDocuments().ToImmutableArray();

            if (changedAdditionalDocuments.Length + changedDocuments.Length + changedAnalyzerConfigDocuments.Length != 1)
            {
                return null;
            }

            if (changedDocuments.Any(id => newSolution.GetRequiredDocument(id).HasInfoChanged(oldSolution.GetRequiredDocument(id))) ||
                changedAdditionalDocuments.Any(id => newSolution.GetRequiredAdditionalDocument(id).HasInfoChanged(oldSolution.GetRequiredAdditionalDocument(id))) ||
                changedAnalyzerConfigDocuments.Any(id => newSolution.GetRequiredAnalyzerConfigDocument(id).HasInfoChanged(oldSolution.GetRequiredAnalyzerConfigDocument(id))))
            {
                return null;
            }

            if (changedDocuments.Length == 1)
            {
                return oldSolution.GetDocument(changedDocuments[0]);
            }
            else if (changedAdditionalDocuments.Length == 1)
            {
                return oldSolution.GetAdditionalDocument(changedAdditionalDocuments[0]);
            }
            else
            {
                return oldSolution.GetAnalyzerConfigDocument(changedAnalyzerConfigDocuments[0]);
            }
        }

        /// <returns><see langword="true"/> if all expected <paramref name="operations"/> are applied successfully;
        /// otherwise, <see langword="false"/>.</returns>
        private async Task<bool> ProcessOperationsAsync(
            Workspace workspace, ImmutableArray<CodeActionOperation> operations,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var applied = true;
            var seenApplyChanges = false;
            foreach (var operation in operations)
            {
                if (operation is ApplyChangesOperation)
                {
                    // there must be only one ApplyChangesOperation, we will ignore all other ones.
                    if (seenApplyChanges)
                        continue;

                    seenApplyChanges = true;
                }

                this.AssertIsForeground();
                applied &= await operation.TryApplyAsync(workspace, progressTracker, cancellationToken).ConfigureAwait(true);
            }

            return applied;
        }

        private async Task TryNavigateToLocationOrStartRenameSessionAsync(Workspace workspace, Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            var changedDocuments = newSolution.GetChangedDocuments(oldSolution);
            foreach (var documentId in changedDocuments)
            {
                var document = newSolution.GetRequiredDocument(documentId);
                if (!document.SupportsSyntaxTree)
                {
                    continue;
                }

                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var navigationTokenOpt = root.GetAnnotatedTokens(NavigationAnnotation.Kind)
                                             .FirstOrNull();
                if (navigationTokenOpt.HasValue)
                {
                    var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
                    await navigationService.TryNavigateToPositionAsync(
                        workspace, documentId, navigationTokenOpt.Value.SpanStart, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var renameTokenOpt = root.GetAnnotatedTokens(RenameAnnotation.Kind)
                                         .FirstOrNull();

                if (renameTokenOpt.HasValue)
                {
                    // It's possible that the workspace's current solution is not the same as
                    // newSolution. This can happen if the workspace host performs other edits
                    // during ApplyChanges, such as in the Venus scenario where indentation and
                    // formatting can happen. To work around this, we create a SyntaxPath to the
                    // rename token in the newSolution and resolve it to the current solution.

                    var pathToRenameToken = new SyntaxPath(renameTokenOpt.Value);
                    var latestDocument = workspace.CurrentSolution.GetDocument(documentId);
                    if (latestDocument != null)
                    {
                        var latestRoot = await latestDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        if (pathToRenameToken.TryResolve(latestRoot, out var resolvedRenameToken) &&
                            resolvedRenameToken.IsToken)
                        {
                            var editorWorkspace = workspace;
                            var navigationService = editorWorkspace.Services.GetRequiredService<IDocumentNavigationService>();
                            if (await navigationService.TryNavigateToSpanAsync(
                                    editorWorkspace, documentId, resolvedRenameToken.Span, cancellationToken).ConfigureAwait(false))
                            {
                                var openDocument = workspace.CurrentSolution.GetRequiredDocument(documentId);
                                var openRoot = await openDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                                // NOTE: We need to resolve the syntax path again in case VB line commit kicked in
                                // due to the navigation.

                                // TODO(DustinCa): We still have a potential problem here with VB line commit,
                                // because it can insert tokens and all sorts of other business, which could
                                // wind up with us not being able to resolve the token.
                                if (pathToRenameToken.TryResolve(openRoot, out resolvedRenameToken) &&
                                    resolvedRenameToken.IsToken)
                                {
                                    var text = await openDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                                    var snapshot = text.FindCorrespondingEditorTextSnapshot();
                                    if (snapshot != null)
                                    {
                                        await this.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                                        _renameService.StartInlineSession(openDocument, resolvedRenameToken.AsToken().Span, cancellationToken);
                                    }
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
