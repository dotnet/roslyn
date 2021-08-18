' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Sub", "Function", "Async" and "Iterator" keywords in expression contexts that would start a lambda.
    ''' </summary>
    Friend Class LambdaKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsDelegateCreationContext() Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Async", VBFeaturesResources.Defines_an_asynchronous_lambda_expression_that_can_use_the_Await_operator_Can_be_used_wherever_a_delegate_type_is_expected_Async_Sub_Function_parameterList_expression),
                    New RecommendedKeyword("Function", VBFeaturesResources.Defines_a_lambda_expression_that_calculates_and_returns_a_single_value_Can_be_used_wherever_a_delegate_type_is_expected_Function_parameterList_expression),
                    New RecommendedKeyword("Iterator", VBFeaturesResources.Defines_an_iterator_lambda_expression_that_can_use_the_Yield_statement_Iterator_Function_parameterList_As_IEnumerable_Of_T),
                    New RecommendedKeyword("Sub", VBFeaturesResources.Defines_a_lambda_expression_that_can_execute_statements_and_does_not_return_a_value_Can_be_used_wherever_a_delegate_type_is_expected_Sub_parameterList_statement))
            End If

            Dim targetToken = context.TargetToken
            If context.SyntaxTree.IsExpressionContext(targetToken.SpanStart, cancellationToken, context.SemanticModel) Then
                If targetToken.IsKindOrHasMatchingText(SyntaxKind.IteratorKeyword) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Function", VBFeaturesResources.Defines_a_lambda_expression_that_calculates_and_returns_a_single_value_Can_be_used_wherever_a_delegate_type_is_expected_Function_parameterList_expression))
                ElseIf targetToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) Then
                    Return ImmutableArray.Create(
                        New RecommendedKeyword("Function", VBFeaturesResources.Defines_a_lambda_expression_that_calculates_and_returns_a_single_value_Can_be_used_wherever_a_delegate_type_is_expected_Function_parameterList_expression),
                        New RecommendedKeyword("Sub", VBFeaturesResources.Defines_a_lambda_expression_that_can_execute_statements_and_does_not_return_a_value_Can_be_used_wherever_a_delegate_type_is_expected_Sub_parameterList_statement))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
