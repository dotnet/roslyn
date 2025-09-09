// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

internal sealed partial class RenameTrackingTaggerProvider
{
    private sealed class RenameTrackingCommitter
    {
        private readonly StateMachine _stateMachine;
        private readonly SnapshotSpan _snapshotSpan;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly string _displayText;
        private readonly AsyncLazy<RenameTrackingSolutionSet> _renameSymbolResultGetter;

        public RenameTrackingCommitter(
            StateMachine stateMachine,
            SnapshotSpan snapshotSpan,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            string displayText)
        {
            _stateMachine = stateMachine;
            _snapshotSpan = snapshotSpan;
            _refactorNotifyServices = refactorNotifyServices;
            _undoHistoryRegistry = undoHistoryRegistry;
            _displayText = displayText;
            _renameSymbolResultGetter = AsyncLazy.Create(
                static (self, c) => self.RenameSymbolWorkerAsync(c),
                arg: this);
        }

        /// <summary>
        /// Returns non-null error message if renaming fails.
        /// </summary>
        public async Task<(NotificationSeverity severity, string message)?> TryCommitAsync(CancellationToken cancellationToken)
        {
            _stateMachine.ThreadingContext.ThrowIfNotOnUIThread();

            try
            {
                return await TryApplyChangesToWorkspaceAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Clear the state machine so that future updates to the same token work, and any text changes
                // caused by this update are not interpreted as potential renames.  Intentionally pass
                // CancellationToken.None.  We must clear this state out.
                await _stateMachine.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                _stateMachine.ClearTrackingSession();
            }
        }

        public async Task<RenameTrackingSolutionSet> RenameSymbolAsync(CancellationToken cancellationToken)
            => await _renameSymbolResultGetter.GetValueAsync(cancellationToken).ConfigureAwait(false);

        private async Task<RenameTrackingSolutionSet> RenameSymbolWorkerAsync(CancellationToken cancellationToken)
        {
            var document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            var newName = _snapshotSpan.GetText();

            Contract.ThrowIfNull(document, "Invoked rename tracking smart tag but cannot find the document for the snapshot span.");

            // Get copy of solution with the original name in the place of the renamed name
            var solutionWithOriginalName = await CreateSolutionWithOriginalNameAsync(
                document, cancellationToken).ConfigureAwait(false);

            var symbol = await TryGetSymbolAsync(solutionWithOriginalName, document.Id, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(symbol, "Invoked rename tracking smart tag but cannot find the symbol.");

            var options = new SymbolRenameOptions(RenameOverloads: _stateMachine.TrackingSession.ForceRenameOverloads);
            var renamedSolution = await Renamer.RenameSymbolAsync(solutionWithOriginalName, symbol, options, newName, cancellationToken).ConfigureAwait(false);
            return new RenameTrackingSolutionSet(symbol, solutionWithOriginalName, renamedSolution);
        }

        /// <summary>
        /// Returns non-null error message if renaming fails.
        /// </summary>
        private async Task<(NotificationSeverity, string)?> TryApplyChangesToWorkspaceAsync(CancellationToken cancellationToken)
        {
            // Now that the necessary work has been done to create the intermediate and final
            // solutions during PreparePreview, check one more time for cancellation before making all of the
            // workspace changes.
            cancellationToken.ThrowIfCancellationRequested();

            // Undo must backtrack to the state with the original identifier before the state
            // with the user-edited identifier. For example,
            // 
            //   1. Original:                           void M() { M(); }
            //   2. User types:                         void Method() { M(); }
            //   3. Invoke rename:                      void Method() { Method(); }
            // 
            // The undo process should be as follows
            //   1. Back to original name everywhere:   void M() { M(); }       // No tracking session
            //   2. Back to state 2 above:              void Method() { M(); }  // Resume tracking session
            //   3. Finally, start undoing typing:      void M() { M(); }
            //
            // As far as the user can see, undo state 1 never actually existed so we must insert
            // a state here to facilitate the undo. Do the work to obtain the intermediate and
            // final solution without updating the workspace, and then finally disallow
            // cancellation and update the workspace twice.

            var renameTrackingSolutionSet = await RenameSymbolAsync(cancellationToken).ConfigureAwait(false);

            var document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            var newName = _snapshotSpan.GetText();

            var workspace = document.Project.Solution.Workspace;

            // Since the state machine is only watching buffer changes, it will interpret the 
            // text changes caused by undo and redo actions as potential renames, so carefully
            // update the state machine after undo/redo actions. 

            var changedDocuments = renameTrackingSolutionSet.RenamedSolution.GetChangedDocuments(renameTrackingSolutionSet.OriginalSolution);
            try
            {
                // When this action is undone (the user has undone twice), restore the state
                // machine to so that they can continue their original rename tracking session.

                await _stateMachine.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var trackingSessionId = _stateMachine.StoreCurrentTrackingSessionAndGenerateId();
                var result = TryUpdateWorkspaceForResetOfTypedIdentifier(workspace, renameTrackingSolutionSet.OriginalSolution, trackingSessionId);
                if (result is not null)
                    return result;

                // Now that the solution is back in its original state, notify third parties about
                // the coming rename operation.
                if (!_refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, renameTrackingSolutionSet.Symbol, newName, throwOnFailure: false))
                    return (NotificationSeverity.Error, EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid);

                // move all changes to final solution based on the workspace's current solution, since the current solution
                // got updated when we reset it above.
                var finalSolution = workspace.CurrentSolution;
                foreach (var docId in changedDocuments)
                {
                    // because changes have already been made to the workspace (UpdateWorkspaceForResetOfTypedIdentifier() above),
                    // these calls can't be cancelled and must be allowed to complete.
                    var root = await renameTrackingSolutionSet.RenamedSolution.GetDocument(docId).GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, root);
                }

                // Undo/redo on this action must always clear the state machine
                await _stateMachine.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                return TryUpdateWorkspaceForGlobalIdentifierRename(
                    workspace,
                    finalSolution,
                    _displayText,
                    changedDocuments,
                    renameTrackingSolutionSet.Symbol,
                    newName,
                    trackingSessionId);
            }
            finally
            {
                // Explicit CancellationToken.None here.  We must clean up our state no matter what.
                await _stateMachine.ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
            }
        }

        private async Task<Solution> CreateSolutionWithOriginalNameAsync(
            Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var fullText = syntaxTree.GetText(cancellationToken);
            var textChange = new TextChange(new TextSpan(_snapshotSpan.Start, _snapshotSpan.Length), _stateMachine.TrackingSession.OriginalName);

            var newFullText = fullText.WithChanges(textChange);
#if DEBUG
            var syntaxTreeWithOriginalName = syntaxTree.WithChangedText(newFullText);
            var documentWithOriginalName = document.WithSyntaxRoot(syntaxTreeWithOriginalName.GetRoot(cancellationToken));

            Debug.Assert(newFullText.ToString() == documentWithOriginalName.GetTextSynchronously(cancellationToken).ToString());
#endif

            // Apply the original name to all linked documents to construct a consistent solution
            var solution = document.Project.Solution;
            var finalSolution = solution.WithDocumentTexts(
                document.GetLinkedDocumentIds().Add(document.Id).SelectAsArray(id => (id, newFullText)));

            return finalSolution;
        }

        private async Task<ISymbol> TryGetSymbolAsync(Solution solutionWithOriginalName, DocumentId documentId, CancellationToken cancellationToken)
        {
            var documentWithOriginalName = solutionWithOriginalName.GetDocument(documentId);
            var syntaxTreeWithOriginalName = await documentWithOriginalName.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = documentWithOriginalName.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = documentWithOriginalName.GetLanguageService<ISemanticFactsService>();
            var semanticModel = await documentWithOriginalName.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var token = await syntaxTreeWithOriginalName.GetTouchingWordAsync(_snapshotSpan.Start, syntaxFacts, cancellationToken).ConfigureAwait(false);
            var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, token, cancellationToken);

            return tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
        }

        /// <summary>
        /// Returns non-null error message if renaming fails.
        /// </summary>
        private (NotificationSeverity, string)? TryUpdateWorkspaceForResetOfTypedIdentifier(Workspace workspace, Solution newSolution, int trackingSessionId)
        {
            _stateMachine.ThreadingContext.ThrowIfNotOnUIThread();

            // Update document in an ITextUndoTransaction with custom behaviors on undo/redo to
            // deal with the state machine.

            var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);
            using var localUndoTransaction = undoHistory.CreateTransaction(EditorFeaturesResources.Text_Buffer_Change);

            var undoPrimitiveBefore = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: true);
            localUndoTransaction.AddUndo(undoPrimitiveBefore);

            if (!workspace.TryApplyChanges(newSolution))
                return (NotificationSeverity.Error, EditorFeaturesResources.Rename_operation_could_not_complete_due_to_external_change_to_workspace);

            // If we successfully updated the workspace then make sure the undo transaction is committed and is
            // always able to undo anything any other external listener did.

            // Never resume tracking session on redo
            var undoPrimitiveAfter = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
            localUndoTransaction.AddUndo(undoPrimitiveAfter);

            localUndoTransaction.Complete();

            return null;
        }

        /// <summary>
        /// Returns non-null error message if renaming fails.
        /// </summary>
        private (NotificationSeverity, string)? TryUpdateWorkspaceForGlobalIdentifierRename(
            Workspace workspace,
            Solution newSolution,
            string undoName,
            IEnumerable<DocumentId> changedDocuments,
            ISymbol symbol,
            string newName,
            int trackingSessionId)
        {
            _stateMachine.ThreadingContext.ThrowIfNotOnUIThread();

            // Perform rename in a workspace undo action so that undo will revert all 
            // references. It should also be performed in an ITextUndoTransaction to handle 

            var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);

            using var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(undoName);
            using var localUndoTransaction = undoHistory.CreateTransaction(undoName);

            var undoPrimitiveBefore = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
            localUndoTransaction.AddUndo(undoPrimitiveBefore);

            if (!workspace.TryApplyChanges(newSolution))
                return (NotificationSeverity.Error, EditorFeaturesResources.Rename_operation_could_not_complete_due_to_external_change_to_workspace);

            try
            {
                if (!_refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: false))
                    return (NotificationSeverity.Information, EditorFeaturesResources.Rename_operation_was_not_properly_completed_Some_file_might_not_have_been_updated);

                return null;
            }
            finally
            {
                // If we successfully updated the workspace then make sure the undo transaction is committed and is
                // always able to undo anything any other external listener did.

                // Never resume tracking session on redo
                var undoPrimitiveAfter = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
                localUndoTransaction.AddUndo(undoPrimitiveAfter);

                localUndoTransaction.Complete();
                workspaceUndoTransaction.Commit();
            }
        }
    }
}
