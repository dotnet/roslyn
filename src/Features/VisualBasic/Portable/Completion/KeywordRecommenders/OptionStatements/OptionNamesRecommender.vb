' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the names of options that can appear after an Option keyword, such as Compare or Infer.
    ''' </summary>
    Friend Class OptionNamesRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("Compare", VBFeaturesResources.Sets_the_default_comparison_method_to_use_when_comparing_string_data_When_set_to_Text_uses_a_text_sort_order_that_is_not_case_sensitive_When_set_to_Binary_uses_a_strict_binary_sort_order_Option_Compare_Binary_Text),
            New RecommendedKeyword("Explicit", VBFeaturesResources.When_set_to_On_requires_explicit_declaration_of_all_variables_using_a_Dim_Private_Public_or_ReDim_statement_Option_Explicit_On_Off),
            New RecommendedKeyword("Infer", VBFeaturesResources.When_set_to_On_allows_the_use_of_local_type_inference_in_declaring_variables_Option_Infer_On_Off),
            New RecommendedKeyword("Strict", VBFeaturesResources.When_set_to_On_restricts_implicit_data_type_conversions_to_only_widening_conversions_Option_Strict_On_Off))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Return If(context.TargetToken.IsKind(SyntaxKind.OptionKeyword), s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
