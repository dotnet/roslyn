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
    ''' Recommends the "While" and "Until" keywords as a part of a Do or Loop statements
    ''' </summary>
    Friend Class UntilAndWhileKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If (targetToken.Kind = SyntaxKind.DoKeyword AndAlso TypeOf targetToken.Parent Is DoStatementSyntax) OrElse
             (targetToken.Kind = SyntaxKind.LoopKeyword AndAlso
             TypeOf targetToken.Parent Is LoopStatementSyntax AndAlso
             targetToken.Parent.Parent.IsKind(SyntaxKind.SimpleDoLoopBlock, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock)) Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Until", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                       VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Loop_Until_condition,
                                                       VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Until_condition_Loop)),
                    New RecommendedKeyword("While", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                       VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_Loop_While_condition,
                                                       VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_While_condition_Loop)))
            Else
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If
        End Function
    End Class
End Namespace
