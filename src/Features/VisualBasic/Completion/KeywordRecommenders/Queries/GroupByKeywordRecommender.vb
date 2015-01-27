' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "By" keyword for the "Group By" query clause.
    ''' </summary>
    Friend Class GroupByKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.TargetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) OrElse
               context.SyntaxTree.IsFollowingCompleteExpression(Of GroupByClauseSyntax)(
                   context.Position, context.TargetToken, Function(groupBy) groupBy.Items.LastRangeExpression(), cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("By", VBFeaturesResources.ByQueryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
