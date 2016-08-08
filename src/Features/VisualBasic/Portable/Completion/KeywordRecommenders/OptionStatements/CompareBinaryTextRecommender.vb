' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the "Binary" and "Text" options that come after "Option Compare"
    ''' </summary>
    Friend Class CompareBinaryTextRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.TargetToken.IsKind(SyntaxKind.CompareKeyword) Then
                Return {New RecommendedKeyword("Binary", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_strict_binary_sort_order),
                        New RecommendedKeyword("Text", VBFeaturesResources.Sets_the_string_comparison_method_specified_in_Option_Compare_to_a_text_sort_order_that_is_not_case_sensitive)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
