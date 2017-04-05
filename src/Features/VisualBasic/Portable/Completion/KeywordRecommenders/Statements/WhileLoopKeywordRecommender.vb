' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "While" keyword at the start of a statement. "While" as a part of a Do statement is handled in
    ''' the UntilAndWhileKeywordRecommender.
    ''' </summary>
    Friend Class WhileLoopKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("While", VBFeaturesResources.Runs_a_series_of_statements_as_long_as_a_given_condition_is_true))
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.WhileBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return SpecializedCollections.SingletonEnumerable(
                            New RecommendedKeyword("While",
                                                   If(targetToken.IsKind(SyntaxKind.ExitKeyword),
                                                                    VBFeaturesResources.Exits_a_While_loop_and_transfers_execution_immediately_to_the_statement_following_the_End_While_statement,
                                                                    VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_While_loop)))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
