' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Select" keyword at the start of a statement
    ''' </summary>
    Friend Class SelectKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return ImmutableArray.Create(New RecommendedKeyword("Select", VBFeaturesResources.Runs_one_of_several_groups_of_statements_depending_on_the_value_of_an_expression))
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.SelectBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return ImmutableArray.Create(New RecommendedKeyword("Select", VBFeaturesResources.Exits_a_Select_block_and_transfers_execution_immediately_to_the_statement_following_the_End_Select_statement))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
