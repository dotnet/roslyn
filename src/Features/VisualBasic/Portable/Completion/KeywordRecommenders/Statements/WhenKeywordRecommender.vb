' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "When" keyword for a Catch filter
    ''' </summary>
    Friend Class WhenKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("When", VBFeaturesResources.Adds_a_conditional_test_to_a_Catch_statement_Exceptions_are_caught_by_that_Catch_statement_only_when_the_conditional_test_that_follows_the_When_keyword_evaluates_to_True))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsFromIdentifierNode(Of CatchStatementSyntax)(Function(catchStatement) catchStatement.IdentifierName) Then
                Return s_keywords
            End If

            If context.SyntaxTree.IsFollowingCompleteExpression(Of SimpleAsClauseSyntax)(context.Position, context.TargetToken,
                childGetter:=Function(asClause) If(TypeOf asClause.Parent Is CatchStatementSyntax, asClause.Type, Nothing), cancellationToken:=cancellationToken) Then

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
