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
    ''' Recommends the "Ascending" and "Descending" contextual keywords in a Order By clause.
    ''' </summary>
    Friend Class AscendingDescendingKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("Ascending", VBFeaturesResources.Specifies_the_sort_order_for_an_Order_By_clause_in_a_query_The_smallest_element_will_appear_first),
            New RecommendedKeyword("Descending", VBFeaturesResources.Specifies_the_sort_order_for_an_Order_By_clause_in_a_query_The_largest_element_will_appear_first))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.SyntaxTree.IsFollowingCompleteExpression(Of OrderingSyntax)(context.Position, context.TargetToken, Function(orderingSyntax) orderingSyntax.Expression, cancellationToken),
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
