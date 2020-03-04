' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "To" keyword in an For.
    ''' </summary>
    Friend Class ToKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' Case statements. We must check for what the parser would parse both with and without
            ' the To statement.
            If context.SyntaxTree.IsFollowingCompleteExpression(Of SimpleCaseClauseSyntax)(context.Position, context.TargetToken, Function(c) c.Value, cancellationToken) OrElse
               context.SyntaxTree.IsFollowingCompleteExpression(Of RangeCaseClauseSyntax)(context.Position, context.TargetToken, Function(c) c.LowerBound, cancellationToken) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("To", VBFeaturesResources.Separates_the_beginning_and_ending_values_of_a_loop_counter_or_array_bounds_or_that_of_a_value_match_range))
            End If

            If context.SyntaxTree.IsFollowingCompleteExpression(Of ForStatementSyntax)(context.Position, context.TargetToken, Function(forStatement) forStatement.FromValue, cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("To", VBFeaturesResources.Separates_the_beginning_and_ending_values_of_a_loop_counter_or_array_bounds_or_that_of_a_value_match_range))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
