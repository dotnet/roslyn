' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.LanguageServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTokenExtensions
        <Extension()>
        Public Function IsKind(token As SyntaxToken, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return token.Kind = kind1 OrElse
                   token.Kind = kind2
        End Function

        <Extension()>
        Public Function IsKind(token As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
            Return kinds.Contains(token.Kind)
        End Function

        <Extension()>
        Public Function HasMatchingText(token As SyntaxToken, kind As SyntaxKind) As Boolean
            Return String.Equals(token.ToString(), SyntaxFacts.GetText(kind), StringComparison.OrdinalIgnoreCase)
        End Function

        <Extension()>
        Public Function GetPreviousTokenIfTouchingWord(token As SyntaxToken, position As Integer) As SyntaxToken
            Return If(token.IntersectsWith(position) AndAlso IsWord(token),
                      token.GetPreviousToken(includeSkipped:=True),
                      token)
        End Function

        <Extension>
        Public Function IsWord(token As SyntaxToken) As Boolean
            Return VisualBasicSyntaxFactsService.Instance.IsWord(token)
        End Function
    End Module
End Namespace
