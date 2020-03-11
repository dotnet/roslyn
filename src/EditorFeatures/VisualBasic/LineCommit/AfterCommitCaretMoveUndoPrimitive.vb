' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend Class AfterCommitCaretMoveUndoPrimitive
        Inherits AbstractCommitCaretMoveUndoPrimitive

        Private ReadOnly _newPosition As Integer
        Private ReadOnly _newVirtualSpaces As Integer

        Public Sub New(textBuffer As ITextBuffer, textBufferAssociatedViewService As ITextBufferAssociatedViewService, position As CaretPosition)
            MyBase.New(textBuffer, textBufferAssociatedViewService)

            ' Grab the old position and virtual spaces. This is cheaper than holding onto
            ' a VirtualSnapshotPoint as it won't hold old snapshots alive
            _newPosition = position.VirtualBufferPosition.Position
            _newVirtualSpaces = position.VirtualBufferPosition.VirtualSpaces
        End Sub

        Public Overrides Sub [Do]()
            Dim view = TryGetView()

            If view IsNot Nothing Then
                view.Caret.MoveTo(New VirtualSnapshotPoint(New SnapshotPoint(view.TextSnapshot, _newPosition), _newVirtualSpaces))
                view.Caret.EnsureVisible()
            End If
        End Sub

        Public Overrides Sub Undo()
            ' When we are going forward, we do nothing here since the BeforeCommitCaretMoveUndoPrimitive
            ' will take care of it
        End Sub
    End Class
End Namespace
