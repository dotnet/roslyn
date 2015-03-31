// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

            // This task performs the actual rename on solution. We kick this off during preview to be able to show the diff preview
            // and if it was successfully completed without being cancelled, we reuse its result during the actual commit operation as well.
            private Task<RenameTrackingSolutionSet> _renameSymbolTask;

            // Since the renameSymbolTask is used in both Preview and Commit, we need to be able to cancel it cleanly in both phases.
            // We use this CTS to cancel the task kicked off during preview during the commit phase.
            private readonly CancellationTokenSource _previewTaskCancellationTokenSource = new CancellationTokenSource();

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
            }

            public void Commit(CancellationToken cancellationToken)
            {
                AssertIsForeground();

                bool clearTrackingSession = ApplyChangesToWorkspace(cancellationToken);

                // Clear the state machine so that future updates to the same token work,
                // and any text changes caused by this update are not interpreted as 
                // potential renames
                if (clearTrackingSession)
                {
                    _stateMachine.ClearTrackingSession();
                }
            }

            public Task<RenameTrackingSolutionSet> RenameSymbolAsync(bool isPreview, CancellationToken cancellationToken)
            {
                // start rename task and use our own cancellation token if it is a Preview operation.
                _renameSymbolTask = Task.Factory.SafeStartNewFromAsync(
                        () => RenameSymbolWorkerAsync(isPreview ? _previewTaskCancellationTokenSource.Token : cancellationToken),
                        isPreview ? _previewTaskCancellationTokenSource.Token : cancellationToken,
                        TaskScheduler.Default);

                if (isPreview)
                {
                    // register for a callback on the cancellation token we are handed and perform the actual cancellation upon callback.
                    var cancellationTokenRegistration = cancellationToken.Register(() => _previewTaskCancellationTokenSource.Cancel());

                    // deregister the callback after the task completes.
                    _renameSymbolTask.ContinueWith(_ => cancellationTokenRegistration.Dispose(),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                return _renameSymbolTask;
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

                RenameTrackingSolutionSet renameTrackingSolutionSet;
                try
                {
                    // If the task was kicked off and had successfully completed to show preview, re-use its result.
                    if (_renameSymbolTask != null && _renameSymbolTask.Status == TaskStatus.RanToCompletion)
                    {
                        renameTrackingSolutionSet = _renameSymbolTask.Result;
                    }
                    else
                    {
                        // If the task isn't complete yet, cancel it anyway, because lightbulb will cancel the preview
                        // operation once commit is entered, because the preview has technically been dismissed.
                        // We kick off another rename task using Commit's cancellation here.
                        _previewTaskCancellationTokenSource.Cancel();
                        renameTrackingSolutionSet = RenameSymbolAsync(isPreview: false, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
                    }
                }
                finally
                {
                    _previewTaskCancellationTokenSource.Dispose();
                }

                var document = _snapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
                var newName = _snapshotSpan.GetText();

                var workspace = document.Project.Solution.Workspace;

                // Since the state machine is only watching buffer changes, it will interpret the 
                // text changes caused by undo and redo actions as potential renames, so carefully
                // update the state machine after undo/redo actions. 

                var changedDocuments = renameTrackingSolutionSet.RenamedSolution.GetChangedDocuments(renameTrackingSolutionSet.OriginalSolution);

                // When this action is undone (the user has undone twice), restore the state
                // machine to so that they can continue their original rename tracking session.
                UpdateWorkspaceForResetOfTypedIdentifier(workspace, renameTrackingSolutionSet.OriginalSolution);

                // Now that the solution is back in its original state, notify third parties about
                // the coming rename operation.
                if (!_refactorNotifyServices.TryOnBeforeGlobalSymbolRenamed(workspace, changedDocuments, renameTrackingSolutionSet.Symbol, newName, throwOnFailure: false))
                {
                    var notificationService = workspace.Services.GetService<INotificationService>();
                    notificationService.SendNotification(
                        EditorFeaturesResources.RenameOperationWasCancelled,
                        EditorFeaturesResources.RenameSymbol,
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
                    var root = renameTrackingSolutionSet.RenamedSolution.GetDocument(docId).GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                    finalSolution = finalSolution.WithDocumentSyntaxRoot(docId, root);
                }

                // Undo/redo on this action must always clear the state machine
                UpdateWorkspaceForGlobalIdentifierRename(workspace, finalSolution, workspace.CurrentSolution, _displayText, changedDocuments, renameTrackingSolutionSet.Symbol, newName);

                RenameTrackingDismisser.DismissRenameTracking(workspace, changedDocuments);
                return true;
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

            private async Task<ISymbol> TryGetSymbolAsync(Solution solutionWithOriginalName, DocumentId documentId, CancellationToken cancellationToken)
            {
                var documentWithOriginalName = solutionWithOriginalName.GetDocument(documentId);
                var syntaxTreeWithOriginalName = await documentWithOriginalName.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                var syntaxFacts = documentWithOriginalName.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = documentWithOriginalName.GetLanguageService<ISemanticFactsService>();
                var semanticModel = await documentWithOriginalName.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var token = syntaxTreeWithOriginalName.GetTouchingWord(_snapshotSpan.Start, syntaxFacts, cancellationToken);
                var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, token, cancellationToken);

                return tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
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
