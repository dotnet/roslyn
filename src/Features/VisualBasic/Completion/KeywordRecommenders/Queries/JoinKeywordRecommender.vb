' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Join", VBFeaturesResources.JoinQueryKeywordToolTip))
            End If

            ' Now this might be Group Join...
            Dim targetToken = context.TargetToken

            ' If it's just "Group" it may have parsed as a Group By
            If targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) OrElse
               targetToken.IsChildToken(Of GroupJoinClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Join", VBFeaturesResources.JoinQueryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
