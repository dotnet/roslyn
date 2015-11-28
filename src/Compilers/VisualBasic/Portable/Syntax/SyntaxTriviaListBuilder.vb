' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Class SyntaxTriviaListBuilder
        Private _count As Integer
        Private _nodes As SyntaxTrivia()

        Public ReadOnly Property Count As Integer
            Get
                Return _count
            End Get
        End Property

        Public Sub New(size As Integer)
            _nodes = New SyntaxTrivia(size - 1) {}
        End Sub

        Default Public ReadOnly Property Item(index As Integer) As SyntaxTrivia
            Get
                Debug.Assert(index >= 0)
                Debug.Assert(index < _count)

                Return _nodes(index)
            End Get
        End Property

        Public Sub Add(list As SyntaxTriviaList)
            Add(list, 0, list.Count)
        End Sub

        Public Sub Add(items As SyntaxTrivia())
            Add(items, 0, items.Length)
        End Sub

        Public Function Add(item As SyntaxTrivia) As SyntaxTriviaListBuilder
            If ((_nodes Is Nothing) OrElse (_count >= _nodes.Length)) Then
                Grow(If((_count = 0), 8, (_nodes.Length * 2)))
            End If
            _nodes(_count) = item
            _count += 1
            Return Me
        End Function

        Public Sub Add(items As SyntaxTrivia(), sourceOffset As Integer, length As Integer)
            If ((_nodes Is Nothing) OrElse ((_count + length) > _nodes.Length)) Then
                Grow((_count + length))
            End If
            Array.Copy(items, sourceOffset, _nodes, _count, length)
            _count = (_count + length)
        End Sub

        Public Sub Add(list As SyntaxTriviaList, sourceOffset As Integer, length As Integer)
            If ((_nodes Is Nothing) OrElse ((_count + length) > _nodes.Length)) Then
                Grow((_count + length))
            End If
            list.CopyTo(sourceOffset, _nodes, _count, length)
            _count = (_count + length)
        End Sub

        Public Sub Clear()
            _count = 0
        End Sub

        Public Shared Function Create() As SyntaxTriviaListBuilder
            Return New SyntaxTriviaListBuilder(4)
        End Function

        Private Sub Grow(size As Integer)
            Dim tmp As SyntaxTrivia() = New SyntaxTrivia(size - 1) {}
            Array.Copy(_nodes, tmp, _nodes.Length)
            _nodes = tmp
        End Sub

        Public Shared Widening Operator CType(builder As SyntaxTriviaListBuilder) As SyntaxTriviaList
            Return builder.ToList
        End Operator

        Public Function ToList() As SyntaxTriviaList
            If (_count <= 0) Then
                Return New SyntaxTriviaList
            End If
            Select Case _count
                Case 1
                    Return New SyntaxTriviaList(Nothing, _nodes(0).UnderlyingNode, 0, 0)
                Case 2
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(DirectCast(_nodes(0).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(1).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
                Case 3
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(DirectCast(_nodes(0).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(1).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(2).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
            End Select
            Dim tmp = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(_count - 1) {}
            Dim i As Integer
            For i = 0 To _count - 1
                tmp(i).Value = DirectCast(_nodes(i).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)
            Next i
            Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(tmp), 0, 0)
        End Function

    End Class

End Namespace
