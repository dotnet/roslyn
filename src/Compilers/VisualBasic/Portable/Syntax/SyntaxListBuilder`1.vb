' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax
    <Extension()>
    Friend Module SyntaxListBuilderExtensions
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
