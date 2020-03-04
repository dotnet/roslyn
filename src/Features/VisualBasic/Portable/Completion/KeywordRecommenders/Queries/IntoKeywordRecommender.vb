' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "Into" keyword.
    ''' </summary>
    Friend Class IntoKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' "Into" for Group By is easy
            If context.SyntaxTree.IsFollowingCompleteExpression(Of GroupByClauseSyntax)(
               context.Position, context.TargetToken, Function(g) g.Keys.LastRangeExpression(), cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.Specifies_an_identifier_that_can_serve_as_a_reference_to_the_results_of_a_join_or_grouping_subexpression))
            End If

            ' "Into" for Group Join is also easy
            If context.SyntaxTree.IsFollowingCompleteExpression(Of GroupJoinClauseSyntax)(
               context.Position, context.TargetToken, Function(g) g.JoinConditions.LastJoinKey(), cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.Specifies_an_identifier_that_can_serve_as_a_reference_to_the_results_of_a_join_or_grouping_subexpression))
            End If

            ' "Into" for Aggregate is annoying, since it can be following after any number of arbitrary clauses
            If context.IsQueryOperatorContext Then
                Dim token = context.TargetToken.GetPreviousToken()
                Dim aggregateQuery = token.GetAncestor(Of AggregateClauseSyntax)()

                If aggregateQuery IsNot Nothing AndAlso (aggregateQuery.IntoKeyword.IsMissing OrElse aggregateQuery.IntoKeyword = token) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.Specifies_an_identifier_that_can_serve_as_a_reference_to_the_results_of_a_join_or_grouping_subexpression))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
