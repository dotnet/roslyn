' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Structure SeparatedSyntaxList(Of TNode As VisualBasicSyntaxNode)
        Private _list As SyntaxList(Of VisualBasicSyntaxNode)

        Friend Sub New(list As SyntaxList(Of VisualBasicSyntaxNode))
            _list = list
        End Sub

        Friend ReadOnly Property Node As VisualBasicSyntaxNode
            Get
                Return _list.Node
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return (_list.Count + 1) >> 1
            End Get
        End Property

        Public ReadOnly Property SeparatorCount As Integer
            Get
                Return (_list.Count) >> 1
            End Get
        End Property

        Default Public ReadOnly Property Item(index As Integer) As TNode
            Get
                Return DirectCast(_list(index << 1), TNode)
            End Get
        End Property

        ''' <summary>
        ''' Gets the separator at the given index in this list.
        ''' </summary>
        ''' <param name="index">The index.</param><returns></returns>
        Public Function GetSeparator(index As Integer) As SyntaxToken
            Return DirectCast(_list((index << 1) + 1), SyntaxToken)
        End Function

        Public Function Any() As Boolean
            Return (Count > 0)
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            For i = 0 To Count - 1
                Dim element = Item(i)
                If (element.Kind = kind) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Friend Function GetWithSeparators() As SyntaxList(Of VisualBasicSyntaxNode)
            Return _list
        End Function

        ' for debugging
        Private ReadOnly Property Nodes As TNode()
            Get
                Dim arr As TNode() = New TNode(Count - 1) {}

                For i = 0 To Count - 1
                    arr(i) = Item(i)
                Next
                Return arr
            End Get
        End Property
    End Structure

End Namespace
