' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If (targetToken.Kind = SyntaxKind.DoKeyword AndAlso TypeOf targetToken.Parent Is DoStatementSyntax) OrElse
             (targetToken.Kind = SyntaxKind.LoopKeyword AndAlso
             TypeOf targetToken.Parent Is LoopStatementSyntax AndAlso
             targetToken.Parent.Parent.IsKind(SyntaxKind.SimpleDoLoopBlock, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock)) Then
                Return {New RecommendedKeyword("Until", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                                        VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Loop_Until_condition,
                                                                        VBFeaturesResources.Repeats_a_block_of_statements_until_a_Boolean_condition_becomes_true_Do_Until_condition_Loop)),
                        New RecommendedKeyword("While", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                                        VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_Loop_While_condition,
                                                                        VBFeaturesResources.Repeats_a_block_of_statements_while_a_Boolean_condition_is_true_Do_While_condition_Loop))}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
