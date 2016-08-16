' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class SyntaxListPool
        Private ReadOnly _freeList As New Stack(Of CommonSyntaxListBuilder)

        Friend Function Allocate() As CommonSyntaxListBuilder
            If _freeList.Count > 0 Then
                Return _freeList.Pop
            End If
            Return CommonSyntaxListBuilder.Create()
        End Function

        Friend Function Allocate(Of TNode As VisualBasicSyntaxNode)() As CommonSyntaxListBuilder(Of TNode)
            Return New CommonSyntaxListBuilder(Of TNode)(Me.Allocate)
        End Function

        Friend Function AllocateSeparated(Of TNode As VisualBasicSyntaxNode)() As CommonSeparatedSyntaxListBuilder(Of TNode)
            Return New CommonSeparatedSyntaxListBuilder(Of TNode)(Me.Allocate)
        End Function

        Friend Sub Free(item As CommonSyntaxListBuilder)
            If item IsNot Nothing Then
                item.Clear()
                _freeList.Push(item)
            End If
        End Sub

        Friend Function ToListAndFree(Of TNode As VisualBasicSyntaxNode)(item As CommonSyntaxListBuilder(Of TNode)) As CommonSyntaxList(Of TNode)
            Dim list = item.ToList()
            Free(item)
            Return list
        End Function
    End Class
End Namespace