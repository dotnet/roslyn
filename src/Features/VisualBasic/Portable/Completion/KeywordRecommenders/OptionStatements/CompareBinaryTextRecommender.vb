' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the "Binary" and "Text" options that come after "Option Compare"
    ''' </summary>
    Friend Class CompareBinaryTextRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("Binary", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_strict_binary_sort_order),
            New RecommendedKeyword("Text", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_text_sort_order_that_is_not_case_sensitive))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Return If(context.TargetToken.IsKind(SyntaxKind.CompareKeyword), s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
