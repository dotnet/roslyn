' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    ' SyntaxNodeOrTokenListBuilder is a means of creating separated lists
    Friend Class SyntaxNodeOrTokenListBuilder

        Public Shared Function Create() As SyntaxNodeOrTokenListBuilder
            Return New SyntaxNodeOrTokenListBuilder(8)
        End Function

        Friend Sub New(size As Integer)
            Me._nodes = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            Me._count = 0
        End Sub

        Friend Sub Add(item As InternalSyntax.VisualBasicSyntaxNode)
            If ((Me._nodes Is Nothing) OrElse (Me._count >= Me._nodes.Length)) Then
                Me.Grow(If((Me._count = 0), 8, (Me._nodes.Length * 2)))
            End If
            Me._nodes(Me._count) = item
            Me._count += 1
        End Sub

        Public Sub Add(item As SyntaxNodeOrToken)
            Me.Add(DirectCast(item.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode))
        End Sub

        Public Sub AddRange(list As SyntaxNodeOrTokenList)
            Me.AddRange(list, 0, list.Count)
        End Sub

        Public Sub AddRange(list As SyntaxNodeOrTokenList, offset As Integer, length As Integer)
            If ((Me._nodes Is Nothing) OrElse ((Me._count + length) > Me._nodes.Length)) Then
                Me.Grow((Me._count + length))
            End If
            list.CopyTo(offset, Me._nodes, Me._count, length)
            Me._count = (Me._count + length)
        End Sub

        Public Sub AddRange(list As IEnumerable(Of SyntaxNodeOrToken))
            For Each n In list
                Add(n)
            Next
        End Sub

        Friend Sub RemoveLast()
            Me._count -= 1
            Me._nodes(_count) = Nothing
        End Sub

        Public Sub Clear()
            Me._count = 0
        End Sub

        Private Sub Grow(size As Integer)
            Dim tmp = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            Array.Copy(Me._nodes, tmp, Me._nodes.Length)
            Me._nodes = tmp
        End Sub

        Public Function ToList() As SyntaxNodeOrTokenList
            If (Me._count > 0) Then
                Select Case Me._count
                    Case 1
                        Return New SyntaxNodeOrTokenList(Me._nodes(0).CreateRed(Nothing, 0), 0)
                    Case 2
                        Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(Me._nodes(0), Me._nodes(1)).CreateRed(Nothing, 0), 0)
                    Case 3
                        Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(Me._nodes(0), Me._nodes(1), Me._nodes(2)).CreateRed(Nothing, 0), 0)
                End Select
                Dim tmp = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(Me._count - 1) {}
                Dim i As Integer
                For i = 0 To Me._count - 1
                    tmp(i).Value = Me._nodes(i)
                Next i
                Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(tmp).CreateRed(Nothing, 0), 0)
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Count As Integer
            Get
                Return Me._count
            End Get
        End Property

        Default Public Property Item(index As Integer) As SyntaxNodeOrToken
            Get
                Dim innerNode = Me._nodes(index)
                Dim tk = TryCast(innerNode, InternalSyntax.SyntaxToken)
                If tk IsNot Nothing Then
                    ' getting internal token so we do not know the position
                    Return New SyntaxNodeOrToken(Nothing, tk, 0, 0)
                Else
                    Return innerNode.CreateRed()
                End If
            End Get
            Set(value As SyntaxNodeOrToken)
                Me._nodes(index) = DirectCast(value.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)
            End Set
        End Property

        Private _count As Integer
        Private _nodes As InternalSyntax.VisualBasicSyntaxNode()
    End Class

End Namespace
