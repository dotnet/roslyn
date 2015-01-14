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
                Return Me._count
            End Get
        End Property

        Public Sub New(size As Integer)
            Me._nodes = New SyntaxTrivia(size - 1) {}
        End Sub

        Default Public ReadOnly Property Item(index As Integer) As SyntaxTrivia
            Get
                Debug.Assert(index >= 0)
                Debug.Assert(index < Me._count)

                Return Me._nodes(index)
            End Get
        End Property

        Public Sub Add(list As SyntaxTriviaList)
            Me.Add(list, 0, list.Count)
        End Sub

        Public Sub Add(items As SyntaxTrivia())
            Me.Add(items, 0, items.Length)
        End Sub

        Public Function Add(item As SyntaxTrivia) As SyntaxTriviaListBuilder
            If ((Me._nodes Is Nothing) OrElse (Me._count >= Me._nodes.Length)) Then
                Me.Grow(If((Me._count = 0), 8, (Me._nodes.Length * 2)))
            End If
            Me._nodes(Me._count) = item
            Me._count += 1
            Return Me
        End Function

        Public Sub Add(items As SyntaxTrivia(), sourceOffset As Integer, length As Integer)
            If ((Me._nodes Is Nothing) OrElse ((Me._count + length) > Me._nodes.Length)) Then
                Me.Grow((Me._count + length))
            End If
            Array.Copy(items, sourceOffset, Me._nodes, Me._count, length)
            Me._count = (Me._count + length)
        End Sub

        Public Sub Add(list As SyntaxTriviaList, sourceOffset As Integer, length As Integer)
            If ((Me._nodes Is Nothing) OrElse ((Me._count + length) > Me._nodes.Length)) Then
                Me.Grow((Me._count + length))
            End If
            list.CopyTo(sourceOffset, Me._nodes, Me._count, length)
            Me._count = (Me._count + length)
        End Sub

        Public Sub Clear()
            Me._count = 0
        End Sub

        Public Shared Function Create() As SyntaxTriviaListBuilder
            Return New SyntaxTriviaListBuilder(4)
        End Function

        Private Sub Grow(size As Integer)
            Dim tmp As SyntaxTrivia() = New SyntaxTrivia(size - 1) {}
            Array.Copy(Me._nodes, tmp, Me._nodes.Length)
            Me._nodes = tmp
        End Sub

        Public Shared Widening Operator CType(builder As SyntaxTriviaListBuilder) As SyntaxTriviaList
            Return builder.ToList
        End Operator

        Public Function ToList() As SyntaxTriviaList
            If (Me._count <= 0) Then
                Return New SyntaxTriviaList
            End If
            Select Case Me._count
                Case 1
                    Return New SyntaxTriviaList(Nothing, Me._nodes(0).UnderlyingNode, 0, 0)
                Case 2
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(DirectCast(Me._nodes(0).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me._nodes(1).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
                Case 3
                    Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(DirectCast(Me._nodes(0).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me._nodes(1).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode), DirectCast(Me._nodes(2).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)), 0, 0)
            End Select
            Dim tmp = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(Me._count - 1) {}
            Dim i As Integer
            For i = 0 To Me._count - 1
                tmp(i).Value = DirectCast(Me._nodes(i).UnderlyingNode, InternalSyntax.VisualBasicSyntaxNode)
            Next i
            Return New SyntaxTriviaList(Nothing, InternalSyntax.SyntaxList.List(tmp), 0, 0)
        End Function

    End Class

End Namespace
