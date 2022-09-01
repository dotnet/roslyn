' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Loop" statement.
    ''' </summary>
    Friend Class LoopKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If context.IsStatementContext Then
                Dim doBlock = targetToken.GetAncestor(Of DoLoopBlockSyntax)()

                If doBlock Is Nothing OrElse Not doBlock.LoopStatement.IsMissing Then
                    Return ImmutableArray(Of RecommendedKeyword).Empty
                End If

                If doBlock.Kind <> SyntaxKind.SimpleDoLoopBlock Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Loop", VBFeaturesResources.Terminates_a_loop_that_is_introduced_with_a_Do_statement))
                Else
                    Return ImmutableArray.Create(
                        New RecommendedKeyword("Loop", VBFeaturesResources.Terminates_a_loop_that_is_introduced_with_a_Do_statement),
                        New RecommendedKeyword("Loop Until", VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Loop_Until_condition),
                        New RecommendedKeyword("Loop While", VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_Loop_While_condition))
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
