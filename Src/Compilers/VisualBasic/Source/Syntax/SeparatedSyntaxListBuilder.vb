' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Friend Structure SeparatedSyntaxListBuilder(Of TNode As SyntaxNode)
        Private builder As SyntaxListBuilder
        Private expectSeparator As Boolean

        Public Shared Function Create() As SeparatedSyntaxListBuilder(Of TNode)
            Return New SeparatedSyntaxListBuilder(Of TNode)(8)
        End Function

        Public Sub New(size As Integer)
            Me.New(New SyntaxListBuilder(size))
        End Sub

        Friend Sub New(builder As SyntaxListBuilder)
            Me.builder = builder
        End Sub

        Public ReadOnly Property IsNull As Boolean
            Get
                Return (Me.builder Is Nothing)
            End Get
        End Property

        Public ReadOnly Property Count As Integer
            Get
                Return Me.builder.Count
            End Get
        End Property

        Public Sub Clear()
            Me.builder.Clear()
        End Sub

        Public Sub Add(node As TNode)
            If expectSeparator Then
                Throw New InvalidOperationException("separator is expected")
            End If
            expectSeparator = True
            Me.builder.Add(DirectCast(DirectCast(node, SyntaxNode), VisualBasicSyntaxNode))
        End Sub

        Friend Sub AddSeparator(separatorToken As InternalSyntax.SyntaxToken)
            If Not expectSeparator Then
                Throw New InvalidOperationException("element is expected")
            End If
            expectSeparator = False
            Me.builder.AddInternal(separatorToken)
        End Sub

        Public Sub AddSeparator(separatorToken As SyntaxToken)
            AddSeparator(DirectCast(separatorToken.Node, InternalSyntax.SyntaxToken))
        End Sub

        Public Sub AddRange(nodes As SeparatedSyntaxList(Of TNode))
            If expectSeparator Then
                Throw New InvalidOperationException("separator is expected")
            End If
            Dim list = nodes.GetWithSeparators
            Me.builder.AddRange(list)
            expectSeparator = ((Me.builder.Count And 1) <> 0)
        End Sub

        Friend Sub AddRange(nodes As SeparatedSyntaxList(Of TNode), count As Integer)
            If expectSeparator Then
                Throw New InvalidOperationException("separator is expected")
            End If
            Dim list = nodes.GetWithSeparators
            Me.builder.AddRange(list, Me.Count, Math.Min(count * 2, list.Count))
            expectSeparator = ((Me.builder.Count And 1) <> 0)
        End Sub

        Friend Sub RemoveLast()
            Me.builder.RemoveLast()
        End Sub

        Public Function Any(kind As SyntaxKind) As Boolean
            Return Me.builder.Any(kind)
        End Function

        Public Function ToList() As SeparatedSyntaxList(Of TNode)
            If builder Is Nothing Then
                Return New SeparatedSyntaxList(Of TNode)()
            End If
            Return builder.ToSeparatedList(Of TNode)()
        End Function

        Public Function ToList(Of TDerived As TNode)() As SeparatedSyntaxList(Of TDerived)
            If builder Is Nothing Then
                Return New SeparatedSyntaxList(Of TDerived)()
            End If
            Return builder.ToSeparatedList(Of TDerived)()
        End Function

        Public Shared Widening Operator CType(builder As SeparatedSyntaxListBuilder(Of TNode)) As SeparatedSyntaxList(Of TNode)
            Return builder.ToList
        End Operator

    End Structure
End Namespace