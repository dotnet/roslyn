' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "TypeOf" keyword.
    ''' </summary>
    Friend Class TypeOfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("TypeOf", VBFeaturesResources.Determines_the_run_time_type_of_an_object_reference_variable_and_compares_it_to_a_data_type_Returns_True_or_False_depending_on_whether_the_two_types_are_compatible_result_TypeOf_objectExpression_Is_typeName))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsAnyExpressionContext, s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
