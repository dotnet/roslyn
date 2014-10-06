' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis

Friend Module SyntaxKindHelper
    'Helpers that return the language-sepcific (C# / VB) SyntaxKind of a language-agnostic
    'SyntaxNode / SyntaxToken / SyntaxTrivia.

    <Extension()>
    Friend Function GetKind(nodeOrToken As SyntaxNodeOrToken) As String
        Dim kind = String.Empty

        If nodeOrToken.IsNode Then
            kind = nodeOrToken.AsNode().GetKind()
        Else
            kind = nodeOrToken.AsToken().GetKind()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(node As SyntaxNode) As String
        Dim kind = String.Empty

        If node.Language = LanguageNames.CSharp Then
            kind = node.CSharpKind().ToString()
        Else
            kind = node.VBKind().ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(token As SyntaxToken) As String
        Dim kind = String.Empty

        If token.Language = LanguageNames.CSharp Then
            kind = token.CSharpKind().ToString()
        Else
            kind = token.VBKind().ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(trivia As SyntaxTrivia) As String
        Dim kind = String.Empty

        If trivia.Language = LanguageNames.CSharp Then
            kind = trivia.CSharpKind().ToString()
        Else
            kind = trivia.VBKind().ToString()
        End If

        Return kind
    End Function
End Module