' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "Order By" query clause.
    ''' </summary>
    Friend Class OrderByKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsQueryOperatorContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Order By", VBFeaturesResources.Specifies_the_sort_order_for_columns_in_a_query_Can_be_followed_by_either_the_Ascending_or_the_Descending_keyword_If_neither_is_specified_Ascending_is_used))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
