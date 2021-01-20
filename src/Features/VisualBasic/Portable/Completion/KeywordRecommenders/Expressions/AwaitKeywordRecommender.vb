' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    Friend Class AwaitKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Await", VBFeaturesResources.Asynchronously_waits_for_the_task_to_finish))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext Then
                For Each node In context.TargetToken.GetAncestors(Of SyntaxNode)()
                    If node.IsKind(SyntaxKind.SingleLineSubLambdaExpression, SyntaxKind.SingleLineFunctionLambdaExpression,
                                        SyntaxKind.MultiLineSubLambdaExpression, SyntaxKind.MultiLineFunctionLambdaExpression) Then

                        Return s_keywords
                    End If

                    If node.IsKind(SyntaxKind.FinallyBlock, SyntaxKind.SyncLockBlock, SyntaxKind.CatchBlock) Then
                        Return ImmutableArray(Of RecommendedKeyword).Empty
                    End If
                Next

                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
