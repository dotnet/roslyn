' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    Friend MustInherit Class AbstractCommitCaretMoveUndoPrimitive
        Implements ITextUndoPrimitive

        Private ReadOnly _textBuffer As ITextBuffer
        Private ReadOnly _textBufferAssociatedViewService As ITextBufferAssociatedViewService
        Private _parent As ITextUndoTransaction

        Public Sub New(textBuffer As ITextBuffer, textBufferAssociatedViewService As ITextBufferAssociatedViewService)
            _textBuffer = textBuffer
            _textBufferAssociatedViewService = textBufferAssociatedViewService
        End Sub

        Protected Function TryGetView() As ITextView
            Dim views = _textBufferAssociatedViewService.GetAssociatedTextViews(_textBuffer)

            ' We prefer one with focus if we have one
            Dim view = views.FirstOrDefault(Function(v) v.HasAggregateFocus)

            If view IsNot Nothing Then
                Return view
            End If

            ' No focus, so we can't do any better than just picking one
            Return views.FirstOrDefault
        End Function

        Public Function CanMerge(older As ITextUndoPrimitive) As Boolean Implements ITextUndoPrimitive.CanMerge
            Return False
        End Function

        Public ReadOnly Property CanRedo As Boolean Implements ITextUndoPrimitive.CanRedo
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property CanUndo As Boolean Implements ITextUndoPrimitive.CanUndo
            Get
                Return True
            End Get
        End Property

        Public MustOverride Sub [Do]() Implements ITextUndoPrimitive.Do
        Public MustOverride Sub Undo() Implements ITextUndoPrimitive.Undo

        Public Function Merge(older As ITextUndoPrimitive) As ITextUndoPrimitive Implements ITextUndoPrimitive.Merge
            Throw New NotSupportedException()
        End Function

        Public Property Parent As ITextUndoTransaction Implements ITextUndoPrimitive.Parent
            Get
                Return _parent
            End Get
            Set(value As ITextUndoTransaction)
                _parent = value
            End Set
        End Property
    End Class
End Namespace
