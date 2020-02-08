' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the Aggregate operator.
    ''' </summary>
    Friend Class AggregateKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsQueryOperatorContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Aggregate", VBFeaturesResources.Applies_an_aggregation_function_such_as_Sum_Average_or_Count_to_a_sequence))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
