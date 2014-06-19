Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Partial Class SyntaxList
        Friend Class SeparatedWithLotsOfChildren
            Inherits SeparatedWithManyChildren

            Private ReadOnly _positions As Integer()

            Friend Sub New(parent As SyntaxNode, green As InternalSyntax.SyntaxList, position As Integer)
                MyBase.New(parent, green, position)

                Dim positions = New Integer(((green.SlotCount + 1) >> 1) - 1) {}
                Dim curPosition = position

                For i = 0 To green.SlotCount - 1
                    If (i And 1) = 0 Then
                        positions(i >> 1) = curPosition
                    End If

                    Dim child = green.GetSlot(i)
                    If child IsNot Nothing Then
                        curPosition += child.FullWidth
                    End If
                Next
                _positions = positions
            End Sub

            Friend Overrides Function GetChildPosition(i As Integer) As Integer
                Dim position = _positions(i >> 1)

                If (i And 1) <> 0 Then
                    'separator
                    position += Me.Green.GetSlot(i - 1).FullWidth
                End If
                Return position
            End Function
        End Class

    End Class

End Namespace