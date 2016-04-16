' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

    Friend Structure SyntaxListBuilder(Of TNode As SyntaxNode)
        Private ReadOnly _builder As SyntaxListBuilder

        Public Shared Function Create() As SyntaxListBuilder(Of TNode)
            Return New SyntaxListBuilder(Of TNode)(8)
        End Function

        Public Sub New(size As Integer)
            Me.New(New SyntaxListBuilder(size))
        End Sub

        Friend Sub New(builder As SyntaxListBuilder)
            Me._builder = builder
        End Sub

        Public ReadOnly Property IsNull As Boolean
            Get
                Return (Me._builder Is Nothing)
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return Me._builder.Count
            End Get
        End Property

        Public Sub Clear()
            Me._builder.Clear()
        End Sub

        Public Function Add(node As TNode) As SyntaxListBuilder(Of TNode)
            Me._builder.Add(node)
            Return Me
        End Function

        Public Function AddRange(items As TNode(), offset As Integer, length As Integer) As SyntaxListBuilder(Of TNode)
            Me._builder.AddRange(DirectCast(items, SyntaxNode()), offset, length)
            Return Me
        End Function

        Public Function AddRange(nodes As SyntaxList(Of TNode)) As SyntaxListBuilder(Of TNode)
            Me._builder.AddRange(Of TNode)(nodes)
            Return Me
        End Function

        Public Function AddRange(nodes As SyntaxList(Of TNode), offset As Integer, length As Integer) As SyntaxListBuilder(Of TNode)
            Me._builder.AddRange(Of TNode)(nodes, offset, length)
            Return Me
        End Function

        Public Function Any(kind As SyntaxKind) As Boolean
            Return Me._builder.Any(kind)
        End Function

        Public Function ToList() As SyntaxList(Of TNode)
            Return Me._builder.ToList(Of TNode)()
        End Function

        Public Shared Widening Operator CType(builder As SyntaxListBuilder(Of TNode)) As SyntaxList(Of TNode)
            If (Not builder._builder Is Nothing) Then
                Return builder.ToList
            End If
            Return New SyntaxList(Of TNode)
        End Operator
    End Structure

    <Extension()>
    Friend Module SyntaxListBuilderExtensions
        <Extension()>
        Friend Function ToList(builder As SyntaxListBuilder) As SyntaxList(Of SyntaxNode)
            If builder Is Nothing OrElse builder.Count = 0 Then
                Return New SyntaxList(Of SyntaxNode)
            End If
            Return New SyntaxList(Of SyntaxNode)(builder.ToListNode.CreateRed)
        End Function

        <Extension()>
        Friend Function ToSeparatedList(Of TNode As SyntaxNode)(builder As SyntaxListBuilder) As SeparatedSyntaxList(Of TNode)
            If builder Is Nothing OrElse builder.Count = 0 Then
                Return New SeparatedSyntaxList(Of TNode)
            End If
            Return New SeparatedSyntaxList(Of TNode)(New SyntaxNodeOrTokenList(builder.ToListNode.CreateRed, 0))
        End Function

        <Extension()>
        Friend Function ToList(Of TNode As SyntaxNode)(builder As SyntaxListBuilder) As SyntaxList(Of TNode)
            If builder Is Nothing OrElse builder.Count = 0 Then
                Return New SyntaxList(Of TNode)
            End If
            Return New SyntaxList(Of TNode)(builder.ToListNode.CreateRed)
        End Function

        <Extension()>
        Friend Function ToTokenList(builder As SyntaxListBuilder) As SyntaxTokenList
            If (builder Is Nothing OrElse builder.Count = 0) Then
                Return New SyntaxTokenList
            End If
            Return New SyntaxTokenList(Nothing, builder.ToListNode, 0, 0)
        End Function

    End Module
End Namespace
