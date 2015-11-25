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
            _nodes = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            _count = 0
        End Sub

        Friend Sub Add(item As InternalSyntax.VisualBasicSyntaxNode)
            If ((_nodes Is Nothing) OrElse (_count >= _nodes.Length)) Then
                Grow(If((_count = 0), 8, (_nodes.Length * 2)))
            End If
            _nodes(_count) = item
            _count += 1
        End Sub

        Public Sub Add(item As SyntaxNodeOrToken)
            Add(DirectCast(item.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode))
        End Sub

        Public Sub AddRange(list As SyntaxNodeOrTokenList)
            AddRange(list, 0, list.Count)
        End Sub

        Public Sub AddRange(list As SyntaxNodeOrTokenList, offset As Integer, length As Integer)
            If ((_nodes Is Nothing) OrElse ((_count + length) > _nodes.Length)) Then
                Grow((_count + length))
            End If
            list.CopyTo(offset, _nodes, _count, length)
            _count += length
        End Sub

        Public Sub AddRange(list As IEnumerable(Of SyntaxNodeOrToken))
            For Each n In list
                Add(n)
            Next
        End Sub

        Friend Sub RemoveLast()
            _count -= 1
            _nodes(_count) = Nothing
        End Sub

        Public Sub Clear()
            _count = 0
        End Sub

        Private Sub Grow(size As Integer)
            Dim tmp = New InternalSyntax.VisualBasicSyntaxNode(size - 1) {}
            Array.Copy(_nodes, tmp, _nodes.Length)
            _nodes = tmp
        End Sub

        Public Function ToList() As SyntaxNodeOrTokenList
            If (_count > 0) Then
                Select Case _count
                    Case 1
                        Return New SyntaxNodeOrTokenList(_nodes(0).CreateRed(Nothing, 0), 0)
                    Case 2
                        Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(_nodes(0), _nodes(1)).CreateRed(Nothing, 0), 0)
                    Case 3
                        Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(_nodes(0), _nodes(1), _nodes(2)).CreateRed(Nothing, 0), 0)
                End Select
                Dim tmp = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(_count - 1) {}
                Dim i As Integer
                For i = 0 To _count - 1
                    tmp(i).Value = _nodes(i)
                Next i
                Return New SyntaxNodeOrTokenList(InternalSyntax.SyntaxList.List(tmp).CreateRed(Nothing, 0), 0)
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Count As Integer
            Get
                Return _count
            End Get
        End Property

        Default Public Property Item(index As Integer) As SyntaxNodeOrToken
            Get
                Dim innerNode = _nodes(index)
                Dim tk = TryCast(innerNode, InternalSyntax.SyntaxToken)
                If tk IsNot Nothing Then
                    ' getting internal token so we do not know the position
                    Return New SyntaxNodeOrToken(Nothing, tk, 0, 0)
                Else
                    Return innerNode.CreateRed()
                End If
            End Get
            Set(value As SyntaxNodeOrToken)
                _nodes(index) = DirectCast(value.UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)
            End Set
        End Property

        Private _count As Integer
        Private _nodes As InternalSyntax.VisualBasicSyntaxNode()
    End Class

End Namespace
