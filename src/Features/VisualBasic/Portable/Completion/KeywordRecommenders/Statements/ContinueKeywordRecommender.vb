' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Continue" keyword at the start of a statement when in any loop.
    ''' </summary>
    Friend Class ContinueKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Continue", VBFeaturesResources.Transfers_execution_immediately_to_the_next_iteration_of_the_loop_Can_be_used_in_a_Do_loop_a_For_loop_or_a_While_loop))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                If context.IsInStatementBlockOfKind(
                    SyntaxKind.SimpleDoLoopBlock,
                    SyntaxKind.DoWhileLoopBlock,
                    SyntaxKind.DoUntilLoopBlock,
                    SyntaxKind.DoLoopWhileBlock,
                    SyntaxKind.DoLoopUntilBlock,
                    SyntaxKind.ForBlock,
                    SyntaxKind.ForEachBlock,
                    SyntaxKind.WhileBlock) Then

                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
