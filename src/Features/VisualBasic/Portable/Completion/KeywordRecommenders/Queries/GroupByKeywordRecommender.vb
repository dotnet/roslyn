' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("By", VBFeaturesResources.Specifies_the_element_keys_used_for_grouping_in_Group_By_or_sort_order_in_Order_By))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            If context.TargetToken.IsChildToken(Of GroupByClauseSyntax)(Function(groupBy) groupBy.GroupKeyword) OrElse
               context.SyntaxTree.IsFollowingCompleteExpression(Of GroupByClauseSyntax)(
                   context.Position, context.TargetToken, Function(groupBy) groupBy.Items.LastRangeExpression(), cancellationToken) Then
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
