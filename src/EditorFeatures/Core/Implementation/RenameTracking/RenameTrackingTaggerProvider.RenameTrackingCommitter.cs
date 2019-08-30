// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        private class RenameTrackingCommitter : ForegroundThreadAffinitizedObject
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
                : base(stateMachine.ThreadingContext)
            {
                _stateMachine = stateMachine;
                _snapshotSpan = snapshotSpan;
                _refactorNotifyServices = refactorNotifyServices;
                _undoHistoryRegistry = undoHistoryRegistry;
                _displayText = displayText;
                _renameSymbolResultGetter = new AsyncLazy<RenameTrackingSolutionSet>(c => RenameSymbolWorkerAsync(c), cacheResult: true);
            }

            public void Commit(CancellationToken cancellationToken)
            {
                AssertIsForeground();

                var clearTrackingSession = ApplyChangesToWorkspace(cancellationToken);

                // Clear the state machine so that future updates to the same token work,
                // and any text changes caused by this update are not interpreted as 
                // potential renames
                if (clearTrackingSession)
                {
                    _stateMachine.ClearTrackingSession();
                }
            }

            public async Task<RenameTrackingSolutionSet> RenameSymbolAsync(CancellationToken cancellationToken)
            {
                return await _renameSymbolResultGetter.GetValueAsync(cancellationToken).ConfigureAwait(false);
            }

            private async Task<RenameTrackingSolutionSet> RenameSymbolWorkerAsync(CancellationToken cancellationToken)
            {
                var document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var newName = _snapshotSpan.GetText();

                if (document == null)
                {
                    Contract.Fail("Invoked rename tracking smart tag but cannot find the document for the snapshot span.");
                }

                // Get copy of solution with the original name in the place of the renamed name
                var solutionWithOriginalName = CreateSolutionWithOriginalName(document, cancellationToken);

                var symbol = await TryGetSymbolAsync(solutionWithOriginalName, document.Id, cancellationToken).ConfigureAwait(false);
                if (symbol == null)
                {
                    Contract.Fail("Invoked rename tracking smart tag but cannot find the symbol.");
                }

                var optionSet = document.Project.Solution.Workspace.Options;

                if (_stateMachine.TrackingSession.ForceRenameOverloads)
                {
                    optionSet = optionSet.WithChangedOption(RenameOptions.RenameOverloads, true);
                }

                var renamedSolution = await Renamer.RenameSymbolAsync(solutionWithOriginalName, symbol, newName, optionSet, cancellationToken).ConfigureAwait(false);
                return new RenameTrackingSolutionSet(symbol, solutionWithOriginalName, renamedSolution);
            }

            private bool ApplyChangesToWorkspace(CancellationToken cancellationToken)
            {
                AssertIsForeground();

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

                var renameTrackingSolutionSet = RenameSymbolAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var newName = _snapshotSpan.GetText();

                var workspace = document.Project.Solution.Workspace;

                // Since the state machine is only watching buffer changes, it will interpret the 
                // text changes caused by undo and redo actions as potential renames, so carefully
                // update the state machine after undo/redo actions. 

                var changedDocuments = renameTrackingSolutionSet.RenamedSolution.GetChangedDocuments(renameTrackingSolutionSet.OriginalSolution);

                // When this action is undone (the user has undone twice), restore the state
                // machine to so that they can continue their original rename tracking session.

                var trackingSessionId = _stateMachine.StoreCurrentTrackingSessionAndGenerateId();
                UpdateWorkspaceForResetOfTypedIdentifier(workspace, renameTrackingSolutionSet.OriginalSolution, trackingSessionId);

                // Now that the solution is back in its original state, notify third parties about
                // the coming rename operation.
                if (!_refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, renameTrackingSolutionSet.Symbol, newName, throwOnFailure: false))
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(
                        EditorFeaturesResources.Rename_operation_was_cancelled_or_is_not_valid,
                        EditorFeaturesResources.Rename_Symbol,
                        NotificationSeverity.Error);

                    return true;
                }

                // move all changes to final solution based on the workspace's current solution, since the current solution
                // got updated when we reset it above.
                var finalSolution = workspace.CurrentSolution;
                foreach (var docId in changedDocuments)
                {
                    // because changes have already been made to the workspace (UpdateWorkspaceForResetOfTypedIdentifier() above),
                    // these calls can't be cancelled and must be allowed to complete.
                    var root = renameTrackingSolutionSet.RenamedSolution.GetDocument(docId).GetSyntaxRootSynchronously(CancellationToken.None);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, root);
                }

                // Undo/redo on this action must always clear the state machine
                UpdateWorkspaceForGlobalIdentifierRename(
                    workspace,
                    finalSolution,
                    workspace.CurrentSolution,
                    _displayText,
                    changedDocuments,
                    renameTrackingSolutionSet.Symbol,
                    newName,
                    trackingSessionId);

                RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
                return true;
            }

            private Solution CreateSolutionWithOriginalName(Document document, CancellationToken cancellationToken)
            {
                var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
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
                foreach (var documentId in document.GetLinkedDocumentIds().Add(document.Id))
                {
                    solution = solution.WithDocumentText(documentId, newFullText);
                }

                return solution;
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

            private void UpdateWorkspaceForResetOfTypedIdentifier(Workspace workspace, Solution newSolution, int trackingSessionId)
            {
                AssertIsForeground();

                // Update document in an ITextUndoTransaction with custom behaviors on undo/redo to
                // deal with the state machine.

                var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);
                using var localUndoTransaction = undoHistory.CreateTransaction(EditorFeaturesResources.Text_Buffer_Change);

                var undoPrimitiveBefore = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: true);
                localUndoTransaction.AddUndo(undoPrimitiveBefore);

                if (!workspace.TryApplyChanges(newSolution))
                {
                    Contract.Fail("Rename Tracking could not update solution.");
                }

                // Never resume tracking session on redo
                var undoPrimitiveAfter = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
                localUndoTransaction.AddUndo(undoPrimitiveAfter);

                localUndoTransaction.Complete();
            }

            private void UpdateWorkspaceForGlobalIdentifierRename(
                Workspace workspace,
                Solution newSolution,
                Solution oldSolution,
                string undoName,
                IEnumerable<DocumentId> changedDocuments,
                ISymbol symbol,
                string newName,
                int trackingSessionId)
            {
                AssertIsForeground();

                // Perform rename in a workspace undo action so that undo will revert all 
                // references. It should also be performed in an ITextUndoTransaction to handle 

                var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);

                using var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(undoName);
                using var localUndoTransaction = undoHistory.CreateTransaction(undoName);

                var undoPrimitiveBefore = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
                localUndoTransaction.AddUndo(undoPrimitiveBefore);

                if (!workspace.TryApplyChanges(newSolution))
                {
                    Contract.Fail("Rename Tracking could not update solution.");
                }

                if (!_refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: false))
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(
                        EditorFeaturesResources.Rename_operation_was_not_properly_completed_Some_file_might_not_have_been_updated,
                        EditorFeaturesResources.Rename_Symbol,
                        NotificationSeverity.Information);
                }

                // Never resume tracking session on redo
                var undoPrimitiveAfter = new UndoPrimitive(_stateMachine.Buffer, trackingSessionId, shouldRestoreStateOnUndo: false);
                localUndoTransaction.AddUndo(undoPrimitiveAfter);

                localUndoTransaction.Complete();
                workspaceUndoTransaction.Commit();
            }
        }
    }
}
