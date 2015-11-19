' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis

Friend Module SyntaxKindHelper
    'Helpers that return the language-specific (C# / VB) SyntaxKind of a language-agnostic
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
            kind = CSharp.CSharpExtensions.Kind(node).ToString()
        Else
            kind = VisualBasic.VisualBasicExtensions.Kind(node).ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(token As SyntaxToken) As String
        Dim kind = String.Empty

        If token.Language = LanguageNames.CSharp Then
            kind = CSharp.CSharpExtensions.Kind(token).ToString()
        Else
            kind = VisualBasic.VisualBasicExtensions.Kind(token).ToString()
        End If

        Return kind
    End Function

    <Extension()>
    Friend Function GetKind(trivia As SyntaxTrivia) As String
        Dim kind = String.Empty

        If trivia.Language = LanguageNames.CSharp Then
            kind = CSharp.CSharpExtensions.Kind(trivia).ToString()
        Else
            kind = VisualBasic.VisualBasicExtensions.Kind(trivia).ToString()
        End If

        Return kind
    End Function
End Module
