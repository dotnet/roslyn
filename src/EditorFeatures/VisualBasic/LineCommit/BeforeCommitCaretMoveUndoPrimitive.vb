' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend Class BeforeCommitCaretMoveUndoPrimitive
        Inherits AbstractCommitCaretMoveUndoPrimitive

        Private ReadOnly _oldPosition As Integer
        Private ReadOnly _oldVirtualSpaces As Integer
        Private _active As Boolean

        Public Sub New(textBuffer As ITextBuffer, textBufferAssociatedViewService As ITextBufferAssociatedViewService, oldLocation As CaretPosition)
            MyBase.New(textBuffer, textBufferAssociatedViewService)

            ' Grab the old position and virtual spaces. This is cheaper than holding onto
            ' a VirtualSnapshotPoint as it won't hold old snapshots alive
            _oldPosition = oldLocation.VirtualBufferPosition.Position
            _oldVirtualSpaces = oldLocation.VirtualBufferPosition.VirtualSpaces
        End Sub

        Public Sub MarkAsActive()
            ' We must create this undo primitive and add it to the transaction before we know if our
            ' commit is actually going to do something. If we cancel the transaction, we still get
            ' called on Undo, but we don't want to actually do anything there. Thus we have flag to
            ' know if we're actually a live undo primitive
            _active = True
        End Sub

        Public Overrides Sub [Do]()
            ' When we are going forward, we do nothing here since the AfterCommitCaretMoveUndoPrimitive
            ' will take care of it
        End Sub

        Public Overrides Sub Undo()
            ' Sometimes we cancel the transaction, in this case don't do anything.
            If Not _active Then
                Return
            End If

            Dim view = TryGetView()

            If view IsNot Nothing Then
                view.Caret.MoveTo(New VirtualSnapshotPoint(New SnapshotPoint(view.TextSnapshot, _oldPosition), _oldVirtualSpaces))
                view.Caret.EnsureVisible()
            End If
        End Sub
    End Class
End Namespace
