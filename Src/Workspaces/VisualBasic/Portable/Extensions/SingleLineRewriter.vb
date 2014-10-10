' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Class SingleLineRewriter
        Inherits VisualBasicSyntaxRewriter

        Private lastTokenEndedInWhitespace As Boolean
        Private Shared ReadOnly Space As SyntaxTriviaList = SyntaxTriviaList.Create(SyntaxFactory.WhitespaceTrivia(" "))

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If lastTokenEndedInWhitespace Then
                token = token.WithLeadingTrivia(Enumerable.Empty(Of SyntaxTrivia)())
            ElseIf token.LeadingTrivia.Count > 0 Then
                token = token.WithLeadingTrivia(Space)
            End If

#If False Then
            If token.Kind = SyntaxKind.StatementTerminatorToken Then
                token = Syntax.Token(token.LeadingTrivia, SyntaxKind.StatementTerminatorToken, token.TrailingTrivia, ":")
            End If
#End If
            If token.TrailingTrivia.Count > 0 Then
                token = token.WithTrailingTrivia(Space)
                lastTokenEndedInWhitespace = True
            Else
                lastTokenEndedInWhitespace = False
            End If

            Return token
        End Function
    End Class
End Namespace