// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
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
            private readonly bool _showPreview;

            public RenameTrackingCommitter(
                StateMachine stateMachine,
                SnapshotSpan snapshotSpan,
                IEnumerable<IRefactorNotifyService> refactorNotifyServices,
                ITextUndoHistoryRegistry undoHistoryRegistry,
                string displayText,
                bool showPreview)
            {
                _stateMachine = stateMachine;
                _snapshotSpan = snapshotSpan;
                _refactorNotifyServices = refactorNotifyServices;
                _undoHistoryRegistry = undoHistoryRegistry;
                _displayText = displayText;
                _showPreview = showPreview;
            }

            public void Commit(CancellationToken cancellationToken)
            {
                AssertIsForeground();

                bool clearTrackingSession = false;
                RenameSymbolAndApplyChanges(cancellationToken, out clearTrackingSession);

                // Clear the state machine so that future updates to the same token work,
                // and any text changes caused by this update are not interpreted as 
                // potential renames
                if (clearTrackingSession)
                {
                    _stateMachine.ClearTrackingSession();
                }
            }

            private void RenameSymbolAndApplyChanges(CancellationToken cancellationToken, out bool clearTrackingSession)
            {
                AssertIsForeground();

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

                Document document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    Contract.Fail("Invoked rename tracking smart tag but cannot find the document for the snapshot span.");
                }

                // Get copy of solution with the original name in the place of the renamed name

                var solutionWithOriginalName = CreateSolutionWithOriginalName(document, cancellationToken);

                // Get the symbol for the identifier we're renaming (which has now been reverted to
                // its original name) and invoke the rename service.

                ISymbol symbol;
                if (!TryGetSymbol(solutionWithOriginalName, document.Id, cancellationToken, out symbol))
                {
                    Contract.Fail("Invoked rename tracking smart tag but cannot find the symbol");
                }

                var newName = _snapshotSpan.GetText();
                var optionSet = document.Project.Solution.Workspace.Options;

                if (_stateMachine.TrackingSession.ForceRenameOverloads)
                {
                    optionSet = optionSet.WithChangedOption(RenameOptions.RenameOverloads, true);
                }

                var renamedSolution = Renamer.RenameSymbolAsync(solutionWithOriginalName, symbol, newName, optionSet, cancellationToken).WaitAndGetResult(cancellationToken);

                // Now that the necessary work has been done to create the intermediate and final
                // solutions, check one more time for cancellation before making all of the
                // workspace changes.

                cancellationToken.ThrowIfCancellationRequested();

                if (_showPreview)
                {
                    var previewService = renamedSolution.Workspace.Services.GetService<IPreviewDialogService>();

                    renamedSolution = previewService.PreviewChanges(
                           string.Format(EditorFeaturesResources.PreviewChangesOf, EditorFeaturesResources.Rename),
                           "vs.csharp.refactoring.rename",
                           string.Format(
                               EditorFeaturesResources.RenameToTitle,
                               _stateMachine.TrackingSession.OriginalName,
                               newName),
                           symbol.ToDisplayString(),
                           symbol.GetGlyph(),
                           renamedSolution,
                           solutionWithOriginalName);

                    if (renamedSolution == null)
                    {
                        // User clicked cancel.
                        clearTrackingSession = false;
                        return;
                    }
                }

                var workspace = document.Project.Solution.Workspace;

                // Since the state machine is only watching buffer changes, it will interpret the 
                // text changes caused by undo and redo actions as potential renames, so carefully
                // update the state machine after undo/redo actions. 

                var changedDocuments = renamedSolution.GetChangedDocuments(solutionWithOriginalName);

                // When this action is undone (the user has undone twice), restore the state
                // machine to so that they can continue their original rename tracking session.
                UpdateWorkspaceForResetOfTypedIdentifier(workspace, solutionWithOriginalName);

                // Now that the solution is back in its original state, notify third parties about
                // the coming rename operation.
                if (!_refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: false))
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(
                        EditorFeaturesResources.RenameOperationWasCancelled,
                        EditorFeaturesResources.RenameSymbol,
                        NotificationSeverity.Error);

                    clearTrackingSession = true;
                    return;
                }

                // move all changes to final solution based on the workspace's current solution, since the current solution
                // got updated when we reset it above.
                var finalSolution = workspace.CurrentSolution;
                foreach (var docId in changedDocuments)
                {
                    // because changes have already been made to the workspace (UpdateWorkspaceForResetOfTypedIdentifier() above),
                    // these calls can't be cancelled and must be allowed to complete.
                    var root = renamedSolution.GetDocument(docId).GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, root);
                }

                // Undo/redo on this action must always clear the state machine
                UpdateWorkspaceForGlobalIdentifierRename(workspace, finalSolution, workspace.CurrentSolution, _displayText, changedDocuments, symbol, newName);

                RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
                clearTrackingSession = true;
            }

            private Solution CreateSolutionWithOriginalName(Document document, CancellationToken cancellationToken)
            {
                var syntaxTree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var fullText = syntaxTree.GetText(cancellationToken);
                var textChange = new TextChange(new TextSpan(_snapshotSpan.Start, _snapshotSpan.Length), _stateMachine.TrackingSession.OriginalName);

                var newFullText = fullText.WithChanges(textChange);
#if DEBUG
                var syntaxTreeWithOriginalName = syntaxTree.WithChangedText(newFullText);
                var documentWithOriginalName = document.WithSyntaxRoot(syntaxTreeWithOriginalName.GetRoot(cancellationToken));

                Contract.Requires(newFullText.ToString() == documentWithOriginalName.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken).ToString());
#endif

                // Apply the original name to all linked documents to construct a consistent solution
                var solution = document.Project.Solution;
                foreach (var documentId in document.GetLinkedDocumentIds().Add(document.Id))
                {
                    solution = solution.WithDocumentText(documentId, newFullText);
                }

                return solution;
            }

            private bool TryGetSymbol(Solution solutionWithOriginalName, DocumentId documentId, CancellationToken cancellationToken, out ISymbol symbol)
            {
                var documentWithOriginalName = solutionWithOriginalName.GetDocument(documentId);
                var syntaxTreeWithOriginalName = documentWithOriginalName.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var syntaxFacts = documentWithOriginalName.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = documentWithOriginalName.GetLanguageService<ISemanticFactsService>();
                var semanticModel = documentWithOriginalName.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);

                var token = syntaxTreeWithOriginalName.GetTouchingWord(_snapshotSpan.Start, syntaxFacts, cancellationToken);
                var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, token, cancellationToken);

                symbol = tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
                return symbol != null;
            }

            private void UpdateWorkspaceForResetOfTypedIdentifier(Workspace workspace, Solution newSolution)
            {
                AssertIsForeground();

                // Update document in an ITextUndoTransaction with custom behaviors on undo/redo to
                // deal with the state machine.

                var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);
                using (var localUndoTransaction = undoHistory.CreateTransaction(EditorFeaturesResources.TextBufferChange))
                {
                    var undoPrimitiveBefore = new UndoPrimitive(_stateMachine, shouldRestoreStateOnUndo: true);
                    localUndoTransaction.AddUndo(undoPrimitiveBefore);

                    if (!workspace.TryApplyChanges(newSolution))
                    {
                        Contract.Fail("Rename Tracking could not update solution.");
                    }

                    // Never resume tracking session on redo
                    var undoPrimitiveAfter = new UndoPrimitive(_stateMachine, shouldRestoreStateOnUndo: false);
                    localUndoTransaction.AddUndo(undoPrimitiveAfter);

                    localUndoTransaction.Complete();
                }
            }

            private void UpdateWorkspaceForGlobalIdentifierRename(
                Workspace workspace,
                Solution newSolution,
                Solution oldSolution,
                string undoName,
                IEnumerable<DocumentId> changedDocuments,
                ISymbol symbol,
                string newName)
            {
                AssertIsForeground();

                // Perform rename in a workspace undo action so that undo will revert all 
                // references. It should also be performed in an ITextUndoTransaction to handle 

                var undoHistory = _undoHistoryRegistry.RegisterHistory(_stateMachine.Buffer);

                using (var workspaceUndoTransaction = workspace.OpenGlobalUndoTransaction(undoName))
                using (var localUndoTransaction = undoHistory.CreateTransaction(undoName))
                {
                    var undoPrimitiveBefore = new UndoPrimitive(_stateMachine, shouldRestoreStateOnUndo: false);
                    localUndoTransaction.AddUndo(undoPrimitiveBefore);

                    if (!workspace.TryApplyChanges(newSolution))
                    {
                        Contract.Fail("Rename Tracking could not update solution.");
                    }

                    if (!_refactorNotifyServices.TryOnAfterGlobalSymbolRenamed(workspace, changedDocuments, symbol, newName, throwOnFailure: false))
                    {
                        var notificationService = workspace.Services.GetService<INotificationService>();
                        notificationService.SendNotification(
                            EditorFeaturesResources.RenameOperationWasNotProperlyCompleted,
                            EditorFeaturesResources.RenameSymbol,
                            NotificationSeverity.Information);
                    }

                    // Never resume tracking session on redo
                    var undoPrimitiveAfter = new UndoPrimitive(_stateMachine, shouldRestoreStateOnUndo: false);
                    localUndoTransaction.AddUndo(undoPrimitiveAfter);

                    localUndoTransaction.Complete();
                    workspaceUndoTransaction.Commit();
                }
            }
        }
    }
}
