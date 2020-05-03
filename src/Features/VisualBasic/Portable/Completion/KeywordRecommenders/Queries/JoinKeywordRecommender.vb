' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "Join" keyword.
    ''' </summary>
    Friend Class JoinKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' First there is the normal and boring "Join"
            If context.IsQueryOperatorContext OrElse context.IsAdditionalJoinOperatorContext(cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Join", VBFeaturesResources.Combines_the_elements_of_two_sequences_The_join_operation_is_based_on_matching_keys))
            End If

            ' Now this might be Group Join...
            Dim targetToken = context.TargetToken

            ' If it's just "Group" it may have parsed as a Group By
            If targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) OrElse
               targetToken.IsChildToken(Of GroupJoinClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Join", VBFeaturesResources.Combines_the_elements_of_two_sequences_The_join_operation_is_based_on_matching_keys))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
