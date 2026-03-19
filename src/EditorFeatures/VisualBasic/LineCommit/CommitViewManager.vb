' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    ''' <summary>
    ''' This class watches for view-based events in relation to a specific subject buffer and passes along commit operations.
    ''' </summary>
    Friend Class CommitViewManager
        Private ReadOnly _view As ITextView
        Private ReadOnly _commitBufferManagerFactory As CommitBufferManagerFactory
        Private ReadOnly _textBufferAssociatedViewService As ITextBufferAssociatedViewService
        Private ReadOnly _textUndoHistoryRegistry As ITextUndoHistoryRegistry
        Private ReadOnly _uiThreadOperationExecutor As IUIThreadOperationExecutor

        Public Sub New(view As ITextView,
                       commitBufferManagerFactory As CommitBufferManagerFactory,
                       textBufferAssociatedViewService As ITextBufferAssociatedViewService,
                       textUndoHistoryRegistry As ITextUndoHistoryRegistry,
                       uiThreadOperationExecutor As IUIThreadOperationExecutor)
            _view = view
            _commitBufferManagerFactory = commitBufferManagerFactory
            _textBufferAssociatedViewService = textBufferAssociatedViewService
            _textUndoHistoryRegistry = textUndoHistoryRegistry
            _uiThreadOperationExecutor = uiThreadOperationExecutor

            AddHandler _view.Caret.PositionChanged, AddressOf OnCaretPositionChanged
            AddHandler _view.LostAggregateFocus, AddressOf OnLostAggregateFocus
            AddHandler _view.Closed, AddressOf OnViewClosed
        End Sub

        Public Sub Disconnect()
            RemoveHandler _view.Caret.PositionChanged, AddressOf OnCaretPositionChanged
            RemoveHandler _view.LostAggregateFocus, AddressOf OnLostAggregateFocus
            RemoveHandler _view.Closed, AddressOf OnViewClosed
        End Sub

        Private Sub OnCaretPositionChanged(sender As Object, e As CaretPositionChangedEventArgs)
            Dim oldSnapshotPoint = MapDownToPoint(e.OldPosition)
            Dim newSnapshotPoint = MapDownToPoint(e.NewPosition)

            If Not oldSnapshotPoint.HasValue Then
                Return
            End If

            Dim oldBuffer = oldSnapshotPoint.Value.Snapshot.TextBuffer
            Dim newBuffer = If(newSnapshotPoint.HasValue, newSnapshotPoint.Value.Snapshot.TextBuffer, Nothing)

            _uiThreadOperationExecutor.Execute(
                VBEditorResources.Line_commit,
                VBEditorResources.Committing_line,
                allowCancellation:=True,
                showProgress:=False,
                action:=
                Sub(context)
                    ' If our buffers changed, then we commit the old one
                    If oldBuffer IsNot newBuffer Then
                        CommitBufferForCaretMovement(oldBuffer, e, context.UserCancellationToken)
                        Return
                    End If

                    ' We're in the same snapshot. Are we on the same line?
                    Dim commitBufferManager = _commitBufferManagerFactory.CreateForBuffer(newBuffer)
                    If CommitBufferManager.IsMovementBetweenStatements(oldSnapshotPoint.Value, newSnapshotPoint.Value, context.UserCancellationToken) Then
                        CommitBufferForCaretMovement(oldBuffer, e, context.UserCancellationToken)
                    End If
                End Sub)
        End Sub

        Private Sub CommitBufferForCaretMovement(buffer As ITextBuffer,
                                                 e As CaretPositionChangedEventArgs,
                                                 cancellationToken As CancellationToken)
            Dim commitBufferManager = _commitBufferManagerFactory.CreateForBuffer(buffer)

            If commitBufferManager.HasDirtyRegion Then
                ' In projection buffer scenarios, the text undo history is associated with the surface buffer, so the
                ' following line's usage of the surface buffer is correct
                Using transaction = _textUndoHistoryRegistry.GetHistory(_view.TextBuffer).CreateTransaction(VBEditorResources.Visual_Basic_Pretty_List)
                    Dim beforeUndoPrimitive As New BeforeCommitCaretMoveUndoPrimitive(buffer, _textBufferAssociatedViewService, e.OldPosition)
                    transaction.AddUndo(beforeUndoPrimitive)

                    Dim beforeCommitVersion = buffer.CurrentSnapshot.Version
                    Dim subjectBufferCaretPosition = MapDownToPoint(e.OldPosition)
                    commitBufferManager.CommitDirty(isExplicitFormat:=False, cancellationToken:=cancellationToken)

                    If buffer.CurrentSnapshot.Version Is beforeCommitVersion Then
                        transaction.Cancel()
                        Return
                    End If

                    beforeUndoPrimitive.MarkAsActive()
                    transaction.AddUndo(New AfterCommitCaretMoveUndoPrimitive(buffer, _textBufferAssociatedViewService, _view.Caret.Position))
                    transaction.Complete()
                End Using
            End If
        End Sub

        Private Function MapDownToPoint(caretPosition As CaretPosition) As SnapshotPoint?
            Return _view.BufferGraph.MapDownToFirstMatch(caretPosition.BufferPosition,
                                                         PointTrackingMode.Positive,
                                                         Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType),
                                                         caretPosition.Affinity)
        End Function

        Private Sub OnViewClosed(sender As Object, e As EventArgs)
            Disconnect()
        End Sub

        Private Sub OnLostAggregateFocus(sender As Object, e As EventArgs)
            _uiThreadOperationExecutor.Execute(
                "Commit",
                VBEditorResources.Committing_line,
                allowCancellation:=True,
                showProgress:=False,
                action:=
                Sub(context)
                    For Each buffer In _view.BufferGraph.GetTextBuffers(Function(b) b.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType))
                        _commitBufferManagerFactory.CreateForBuffer(buffer).CommitDirty(isExplicitFormat:=False, cancellationToken:=context.UserCancellationToken)
                    Next
                End Sub)
        End Sub
    End Class
End Namespace
