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

        Private useElasticTrivia As Boolean
        Private _lastTokenEndedInWhitespace As Boolean

        Public Sub New(Optional useElasticTrivia As Boolean = False)
            Me.useElasticTrivia = useElasticTrivia
        End Sub

        Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
            If _lastTokenEndedInWhitespace Then
                token = token.WithLeadingTrivia(Enumerable.Empty(Of SyntaxTrivia)())
            ElseIf token.LeadingTrivia.Count > 0 Then
                If useElasticTrivia Then
                    token = token.WithLeadingTrivia(SyntaxFactory.ElasticSpace)
                Else
                    token = token.WithLeadingTrivia(SyntaxFactory.Space)
                End If
            End If
            If token.TrailingTrivia.Count > 0 Then
                If useElasticTrivia Then
                    token = token.WithTrailingTrivia(SyntaxFactory.ElasticSpace)
                Else
                    token = token.WithTrailingTrivia(SyntaxFactory.Space)
                End If
                _lastTokenEndedInWhitespace = True
            Else
                _lastTokenEndedInWhitespace = False
            End If

            Return token
        End Function
    End Class
End Namespace
