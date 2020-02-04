' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Not" keyword.
    ''' </summary>
    Friend Class NotKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Not", VBFeaturesResources.Performs_logical_negation_on_a_Boolean_expression_or_bitwise_negation_on_a_numeric_expression_result_Not_expression))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
