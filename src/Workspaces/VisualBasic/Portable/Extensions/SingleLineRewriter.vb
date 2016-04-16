' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax


Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Class SingleLineRewriter
        Inherits VisualBasicSyntaxRewriter

        Private _lastTokenEndedInWhitespace As Boolean
        Private Shared ReadOnly s_space As SyntaxTriviaList = SyntaxTriviaList.Create(SyntaxFactory.WhitespaceTrivia(" "))

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If _lastTokenEndedInWhitespace Then
                token = token.WithLeadingTrivia(Enumerable.Empty(Of SyntaxTrivia)())
            ElseIf token.LeadingTrivia.Count > 0 Then
                token = token.WithLeadingTrivia(s_space)
            End If

#If False Then
            If token.Kind = SyntaxKind.StatementTerminatorToken Then
                token = Syntax.Token(token.LeadingTrivia, SyntaxKind.StatementTerminatorToken, token.TrailingTrivia, ":")
            End If
#End If
            If token.TrailingTrivia.Count > 0 Then
                token = token.WithTrailingTrivia(s_space)
                _lastTokenEndedInWhitespace = True
            Else
                _lastTokenEndedInWhitespace = False
            End If

            Return token
        End Function
    End Class
End Namespace
