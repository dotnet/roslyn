' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "TypeOf" keyword.
    ''' </summary>
    Friend Class TypeOfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("TypeOf", VBFeaturesResources.Determines_the_run_time_type_of_an_object_reference_variable_and_compares_it_to_a_data_type_Returns_True_or_False_depending_on_whether_the_two_types_are_compatible_result_TypeOf_objectExpression_Is_typeName))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
