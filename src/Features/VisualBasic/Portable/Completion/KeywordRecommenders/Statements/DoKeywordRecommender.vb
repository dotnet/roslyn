' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Do" keyword at the start of a statement
    ''' </summary>
    Friend Class DoKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Do", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_or_until_the_condition_becomes_true_Do_Loop_While_Until_condition),
                    New RecommendedKeyword("Do Until", VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Until_condition_Loop),
                    New RecommendedKeyword("Do While", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_While_condition_Loop))
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.SimpleDoLoopBlock,
                                                SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
                                                SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                If targetToken.IsKind(SyntaxKind.ExitKeyword) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Do", VBFeaturesResources.Exits_a_Do_loop_and_transfers_execution_immediately_to_the_statement_following_the_Loop_statement))
                Else
                    Return ImmutableArray.Create(New RecommendedKeyword("Do", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_Do_loop))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
