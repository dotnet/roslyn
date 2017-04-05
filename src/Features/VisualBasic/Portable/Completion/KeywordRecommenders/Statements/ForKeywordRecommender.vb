' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "For" keyword for the statement context
    ''' </summary>
    Friend Class ForKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return {New RecommendedKeyword("For", VBFeaturesResources.Introduces_a_loop_that_is_iterated_a_specified_number_of_times),
                        New RecommendedKeyword("For Each", VBFeaturesResources.Introduces_a_loop_that_is_repeated_for_each_element_in_a_collection)}
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                If targetToken.IsKind(SyntaxKind.ExitKeyword) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("For", VBFeaturesResources.Exits_a_For_loop_and_transfers_execution_immediately_to_the_statement_following_the_Next_statement))
                Else
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("For", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_For_loop))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
