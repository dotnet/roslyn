' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "For" keyword for the statement context
    ''' </summary>
    Friend Class ForKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("For", VBFeaturesResources.Introduces_a_loop_that_is_iterated_a_specified_number_of_times),
                    New RecommendedKeyword("For Each", VBFeaturesResources.Introduces_a_loop_that_is_repeated_for_each_element_in_a_collection))
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                If targetToken.IsKind(SyntaxKind.ExitKeyword) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("For", VBFeaturesResources.Exits_a_For_loop_and_transfers_execution_immediately_to_the_statement_following_the_Next_statement))
                Else
                    Return ImmutableArray.Create(New RecommendedKeyword("For", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_For_loop))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
