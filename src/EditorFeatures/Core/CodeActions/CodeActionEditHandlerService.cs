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
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

[Export(typeof(ICodeActionEditHandlerService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CodeActionEditHandlerService(
    IThreadingContext threadingContext,
    IPreviewFactoryService previewService,
    IInlineRenameService renameService,
    ITextBufferAssociatedViewService associatedViewService) : ICodeActionEditHandlerService
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IPreviewFactoryService _previewService = previewService;
    private readonly IInlineRenameService _renameService = renameService;

    public ITextBufferAssociatedViewService AssociatedViewService { get; } = associatedViewService;

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
                    new SolutionPreviewResult(_threadingContext, new SolutionPreviewItem(
                        projectId: null, documentId: null,
                        lazyPreview: c => previewOp.GetPreviewAsync(c))));
                continue;
            }

            var title = op.Title;

            if (title != null)
            {
                currentResult = SolutionPreviewResult.Merge(currentResult,
                    new SolutionPreviewResult(_threadingContext, new SolutionPreviewItem(
                        projectId: null, documentId: null, text: title)));
                continue;
            }
        }

        return currentResult;
    }

    public async Task<bool> ApplyAsync(
        Workspace workspace,
        Solution originalSolution,
        Document? fromDocument,
        ImmutableArray<CodeActionOperation> operations,
        string title,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        // Much of the work we're going to do will be on the UI thread, so switch there preemptively.
        // When we get to the expensive parts we can do in the BG then we'll switch over to relinquish
        // the UI thread.
        await this._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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
            var text = await singleChangedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(true);

            using (workspace.Services.GetRequiredService<ISourceTextUndoService>().RegisterUndoTransaction(text, title))
            {
                try
                {
                    _threadingContext.ThrowIfNotOnUIThread();

                    applied = await operations.Single().TryApplyAsync(
                        workspace, originalSolution, progressTracker, cancellationToken).ConfigureAwait(true);
                }
                catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
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
                    workspace, originalSolution, operations, progressTracker, cancellationToken).ConfigureAwait(true);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }

            transaction.Commit();
        }

        var updatedSolution = operations.OfType<ApplyChangesOperation>().FirstOrDefault()?.ChangedSolution ?? oldSolution;
        await TryNavigateToLocationOrStartRenameSessionAsync(
            workspace, operations, oldSolution, updatedSolution, cancellationToken).ConfigureAwait(false);
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

        if (changedDocuments.Any(static (id, arg) => arg.newSolution.GetRequiredDocument(id).HasInfoChanged(arg.oldSolution.GetRequiredDocument(id)), (oldSolution, newSolution)) ||
            changedAdditionalDocuments.Any(static (id, arg) => arg.newSolution.GetRequiredAdditionalDocument(id).HasInfoChanged(arg.oldSolution.GetRequiredAdditionalDocument(id)), (oldSolution, newSolution)) ||
            changedAnalyzerConfigDocuments.Any(static (id, arg) => arg.newSolution.GetRequiredAnalyzerConfigDocument(id).HasInfoChanged(arg.oldSolution.GetRequiredAnalyzerConfigDocument(id)), (oldSolution, newSolution)))
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
        Workspace workspace,
        Solution originalSolution,
        ImmutableArray<CodeActionOperation> operations,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        await this._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

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

            _threadingContext.ThrowIfNotOnUIThread();
            applied &= await operation.TryApplyAsync(workspace, originalSolution, progressTracker, cancellationToken).ConfigureAwait(true);
        }

        return applied;
    }

    private async Task TryNavigateToLocationOrStartRenameSessionAsync(
        Workspace workspace,
        ImmutableArray<CodeActionOperation> operations,
        Solution oldSolution,
        Solution newSolution,
        CancellationToken cancellationToken)
    {
        var navigationOperation = operations.OfType<DocumentNavigationOperation>().FirstOrDefault();
        if (navigationOperation != null && workspace.CanOpenDocuments)
        {
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
            await navigationService.TryNavigateToPositionAsync(
                this._threadingContext, workspace, navigationOperation.DocumentId, navigationOperation.Position, cancellationToken).ConfigureAwait(false);
            return;
        }

        var renameOperation = operations.OfType<StartInlineRenameSessionOperation>().FirstOrDefault();
        if (renameOperation != null && workspace.CanOpenDocuments)
        {
            var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
            if (await navigationService.TryNavigateToPositionAsync(
                    this._threadingContext, workspace, renameOperation.DocumentId, renameOperation.Position, cancellationToken).ConfigureAwait(true))
            {
                var openDocument = workspace.CurrentSolution.GetRequiredDocument(renameOperation.DocumentId);
                _renameService.StartInlineSession(openDocument, new TextSpan(renameOperation.Position, 0), cancellationToken);
                return;
            }
        }

        var changedDocuments = newSolution.GetChangedDocuments(oldSolution);
        foreach (var documentId in changedDocuments)
        {
            var document = newSolution.GetRequiredDocument(documentId);
            if (!document.SupportsSyntaxTree)
                continue;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var navigationToken = root.GetAnnotatedTokens(NavigationAnnotation.Kind).FirstOrNull();
            if (navigationToken.HasValue)
            {
                var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();
                await navigationService.TryNavigateToPositionAsync(
                    this._threadingContext, workspace, documentId, navigationToken.Value.SpanStart, cancellationToken).ConfigureAwait(false);
                return;
            }

            var renameToken = root.GetAnnotatedTokens(RenameAnnotation.Kind).FirstOrNull();
            if (renameToken.HasValue)
            {
                // It's possible that the workspace's current solution is not the same as
                // newSolution. This can happen if the workspace host performs other edits
                // during ApplyChanges, such as in the Venus scenario where indentation and
                // formatting can happen. To work around this, we create a SyntaxPath to the
                // rename token in the newSolution and resolve it to the current solution.

                var pathToRenameToken = new SyntaxPath(renameToken.Value);
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
                                this._threadingContext, editorWorkspace, documentId, resolvedRenameToken.Span, cancellationToken).ConfigureAwait(false))
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
                                var text = await openDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                                var snapshot = text.FindCorrespondingEditorTextSnapshot();
                                if (snapshot != null)
                                {
                                    await this._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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
