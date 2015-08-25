' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.IntoQueryKeywordToolTip))
            End If

            ' "Into" for Group Join is also easy
            If context.SyntaxTree.IsFollowingCompleteExpression(Of GroupJoinClauseSyntax)(
               context.Position, context.TargetToken, Function(g) g.JoinConditions.LastJoinKey(), cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.IntoQueryKeywordToolTip))
            End If

            ' "Into" for Aggregate is annoying, since it can be following after any number of arbitrary clauses
            If context.IsQueryOperatorContext Then
                Dim token = context.TargetToken.GetPreviousToken()
                Dim aggregateQuery = token.GetAncestor(Of AggregateClauseSyntax)()

                If aggregateQuery IsNot Nothing AndAlso (aggregateQuery.IntoKeyword.IsMissing OrElse aggregateQuery.IntoKeyword = token) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Into", VBFeaturesResources.IntoQueryKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
