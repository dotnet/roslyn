' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "ElseIf" keyword for the statement context
    ''' </summary>
    Friend Class ElseIfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("ElseIf", VBFeaturesResources.Introduces_a_condition_in_an_If_statement_that_is_to_be_tested_if_the_previous_conditional_test_fails))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsStatementContext AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.ElseBlock) Then

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
