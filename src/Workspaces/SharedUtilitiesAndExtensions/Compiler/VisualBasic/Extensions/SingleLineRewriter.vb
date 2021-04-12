' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text.RegularExpressions

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Class SingleLineRewriter
        Inherits VisualBasicSyntaxRewriter

        Private Shared ReadOnly s_newlinePattern As Regex = New Regex("[\r\n]+")

        Private ReadOnly _useElasticTrivia As Boolean
        Private _lastTokenEndedInWhitespace As Boolean

        Public Sub New(Optional useElasticTrivia As Boolean = False)
            _useElasticTrivia = useElasticTrivia
        End Sub

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If _lastTokenEndedInWhitespace Then
                token = token.WithLeadingTrivia(Enumerable.Empty(Of SyntaxTrivia)())
            ElseIf token.LeadingTrivia.Count > 0 Then
                If _useElasticTrivia Then
                    token = token.WithLeadingTrivia(SyntaxFactory.ElasticSpace)
                Else
                    token = token.WithLeadingTrivia(SyntaxFactory.Space)
                End If
            End If

            If token.TrailingTrivia.Count > 0 Then
                If _useElasticTrivia Then
                    token = token.WithTrailingTrivia(SyntaxFactory.ElasticSpace)
                Else
                    token = token.WithTrailingTrivia(SyntaxFactory.Space)
                End If

                _lastTokenEndedInWhitespace = True
            Else
                _lastTokenEndedInWhitespace = False
            End If

            If token.Kind() = SyntaxKind.StringLiteralToken OrElse
               token.Kind() = SyntaxKind.InterpolatedStringTextToken Then

                If s_newlinePattern.IsMatch(token.Text) Then
                    Dim newText = s_newlinePattern.Replace(token.Text, " ")

                    If token.Kind() = SyntaxKind.StringLiteralToken Then
                        token = SyntaxFactory.StringLiteralToken(
                            token.LeadingTrivia,
                            newText, newText,
                            token.TrailingTrivia)
                    Else
                        token = SyntaxFactory.InterpolatedStringTextToken(
                            token.LeadingTrivia,
                            newText, newText,
                            token.TrailingTrivia)
                    End If
                End If
            End If

            Return token
        End Function
    End Class
End Namespace
