﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Friend MustInherit Class SyntaxList
        Inherits VisualBasicSyntaxNode

        Friend Sub New(green As InternalSyntax.VisualBasicSyntaxNode, parent As SyntaxNode, position As Integer)
            MyBase.New(green, parent, position)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSyntaxVisitor(Of TResult)) As TResult
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSyntaxVisitor)
            Throw New NotImplementedException()
        End Sub
    End Class

    Friend Class SyntaxListBuilder
        Private _count As Integer
        Private _nodes As ArrayElement(Of GreenNode)()

        Friend Sub New(size As Integer)
            _nodes = New ArrayElement(Of GreenNode)(size - 1) {}
        End Sub

        Friend Function Add(item As SyntaxNode) As SyntaxListBuilder
            Return AddInternal(item.Green)
        End Function

        Friend Function AddInternal(item As GreenNode) As SyntaxListBuilder
            Debug.Assert(item IsNot Nothing)
            If _count >= _nodes.Length Then
                Grow(Math.Max(8, _nodes.Length * 2))
            End If
            _nodes(_count).Value = item
            _count += 1
            Return Me
        End Function

        Friend Function AddRange(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode)) As SyntaxListBuilder
            Return AddRange(list, 0, list.Count)
        End Function

        Friend Function AddRange(items As SyntaxNode()) As SyntaxListBuilder
            Return AddRange(items, 0, items.Length)
        End Function

        Friend Function AddRange(list As SyntaxList(Of SyntaxNode)) As SyntaxListBuilder
            Return AddRange(list, 0, list.Count)
        End Function

        Friend Function AddRange(list As SyntaxNodeOrTokenList) As SyntaxListBuilder
            Return AddRange(list, 0, list.Count)
        End Function

        Friend Function AddRange(list As SyntaxList(Of SyntaxNode), offset As Integer, length As Integer) As SyntaxListBuilder
            If (_count + length) > _nodes.Length Then
                Grow(_count + length)
            End If

            Dim dst = _count
            For i = offset To offset + length - 1
                _nodes(dst).Value = list.ItemInternal(i).Green
                dst += 1
            Next i

            Dim start As Integer = _count
            _count = (_count + length)
            Validate(start, _count)
            Return Me
        End Function

        Friend Function AddRange(items As SyntaxNode(), offset As Integer, length As Integer) As SyntaxListBuilder
            If (_count + length) > _nodes.Length Then
                Grow(_count + length)
            End If

            Dim dst = _count
            For i = offset To offset + length - 1
                _nodes(dst).Value = items(i).Green
                dst += 1
            Next i

            Dim start As Integer = _count
            _count = start + length
            Validate(start, _count)
            Return Me
        End Function

        Friend Function AddRange(Of TNode As SyntaxNode)(list As SyntaxList(Of TNode), offset As Integer, length As Integer) As SyntaxListBuilder
            Return AddRange(New SyntaxList(Of SyntaxNode)(list.Node), offset, length)
        End Function

        Friend Function AddRange(list As SyntaxNodeOrTokenList, offset As Integer, length As Integer) As SyntaxListBuilder
            If (_count + length) > _nodes.Length Then
                Grow(_count + length)
            End If

            Dim dst = _count
            For i = offset To offset + length - 1
                _nodes(dst).Value = list(i).UnderlyingNode
                dst += 1
            Next i

            Dim start As Integer = _count
            _count = start + length
            Validate(start, _count)
            Return Me
        End Function

        Friend Function AddRange(list As SyntaxTokenList, offset As Integer, length As Integer) As SyntaxListBuilder
            Return AddRange(New SyntaxList(Of SyntaxNode)(list.Node.CreateRed), offset, length)
        End Function

        Friend Function Any(kind As SyntaxKind) As Boolean
            Dim i As Integer
            For i = 0 To _count - 1
                If (_nodes(i).Value.RawKind = kind) Then
                    Return True
                End If
            Next i
            Return False
        End Function

        Friend Sub RemoveLast()
            _count -= 1
            _nodes(_count) = Nothing
        End Sub

        Friend Sub Clear()
            _count = 0
        End Sub

        Private Sub Grow(size As Integer)
            Array.Resize(_nodes, size)
        End Sub

        Friend Function ToGreenArray() As ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)()
            Dim array = New ArrayElement(Of InternalSyntax.VisualBasicSyntaxNode)(_count - 1) {}
            Dim i As Integer
            For i = 0 To array.Length - 1
                array(i).Value = DirectCast(_nodes(i).Value, InternalSyntax.VisualBasicSyntaxNode)
            Next i
            Return array
        End Function

        Friend Function ToListNode() As GreenNode
            Select Case _count
                Case 0
                    Return Nothing
                Case 1
                    Return DirectCast(_nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode)
                Case 2
                    Return InternalSyntax.SyntaxList.List(DirectCast(_nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode))
                Case 3
                    Return InternalSyntax.SyntaxList.List(DirectCast(_nodes(0).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(1).Value, InternalSyntax.VisualBasicSyntaxNode), DirectCast(_nodes(2).Value, InternalSyntax.VisualBasicSyntaxNode))
            End Select
            Return InternalSyntax.SyntaxList.List(ToGreenArray)
        End Function

        <Conditional("DEBUG")>
        Private Sub Validate(start As Integer, [end] As Integer)
            Dim i As Integer
            For i = start To [end] - 1
                Debug.Assert(_nodes(i).Value IsNot Nothing)
            Next i
        End Sub

        Friend ReadOnly Property Count As Integer
            Get
                Return _count
            End Get
        End Property
    End Class
End Namespace


