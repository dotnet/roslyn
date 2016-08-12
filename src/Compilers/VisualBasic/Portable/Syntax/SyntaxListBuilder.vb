' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    Friend Class SyntaxListBuilder
        Inherits AbstractSyntaxListBuilder

        Friend Sub New(size As Integer)
            MyBase.New(size)
        End Sub

        Friend Shadows Function AddRange(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxListBuilder
            Return Me.AddRange(Of TNode)(list, 0, list.Count)
        End Function

        Friend Shadows Function AddRange(list As SyntaxList(Of SyntaxNode)) As SyntaxListBuilder
            Return Me.AddRange(list, 0, list.Count)
        End Function

        Friend Shadows Function AddRange(list As SyntaxNodeOrTokenList) As SyntaxListBuilder
            Return Me.AddRange(list, 0, list.Count)
        End Function

        Friend Shadows Function AddRange(list As SyntaxList(Of SyntaxNode), offset As Integer, length As Integer) As SyntaxListBuilder
            If (Me.Count + length) > Me.Nodes.Length Then
                Me.Grow(Me.Count + length)
            End If

            Dim dst = Count
            For i = offset To offset + length - 1
                Me.Nodes(dst).Value = list.ItemInternal(i).Green
                dst += 1
            Next i

            Dim start As Integer = Me.Count
            Me.Count = (Me.Count + length)
            Me.Validate(start, Me.Count)
            Return Me
        End Function

        Public Shadows Sub AddRange(items As SyntaxNode(), offset As Integer, length As Integer)
            MyBase.AddRange(items, offset, length)
        End Sub

        Friend Shadows Function AddRange(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode), offset As Integer, length As Integer) As SyntaxListBuilder
            Return Me.AddRange(New SyntaxList(Of SyntaxNode)(list.Node), offset, length)
        End Function

        Friend Shadows Function AddRange(list As SyntaxNodeOrTokenList, offset As Integer, length As Integer) As SyntaxListBuilder
            If (Me.Count + length) > Me.Nodes.Length Then
                Me.Grow(Me.Count + length)
            End If

            Dim dst = Count
            For i = offset To offset + length - 1
                Me.Nodes(dst).Value = list(i).UnderlyingNode
                dst += 1
            Next i

            Dim start As Integer = Me.Count
            Me.Count = start + length
            Me.Validate(start, Me.Count)
            Return Me
        End Function

        Friend Shadows Function AddRange(list As SyntaxTokenList, offset As Integer, length As Integer) As SyntaxListBuilder
            Return Me.AddRange(New SyntaxList(Of SyntaxNode)(list.Node.CreateRed), offset, length)
        End Function

        Friend Shadows Function Any(kind As SyntaxKind) As Boolean
            Return MyBase.Any(kind)
        End Function

        Friend Sub RemoveLast()
            Me.Count -= 1
            Me.Nodes(Count) = Nothing
        End Sub

        Friend Function ToGreenArray() As ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)()
            Dim array = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(Me.Count - 1) {}
            Dim i As Integer
            For i = 0 To array.Length - 1
                array(i).Value = DirectCast(Me.Nodes(i).Value, InternalSyntax.VisualBasicSyntaxNode)
            Next i
            Return array
        End Function

        Friend Function ToListNode() As GreenNode
            Select Case Me.Count
                Case 0
                    Return Nothing
                Case 1
                    Return DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode)
                Case 2
                    Return InternalSyntax.SyntaxList.List(DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode))
                Case 3
                    Return InternalSyntax.SyntaxList.List(DirectCast(Me.Nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me.Nodes(2).Value, InternalSyntax.VisualBasicSyntaxNode))
            End Select
            Return InternalSyntax.SyntaxList.List(Me.ToGreenArray)
        End Function
    End Class
End Namespace