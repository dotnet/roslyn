' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    Friend Class AwaitKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext Then
                For Each node In context.TargetToken.GetAncestors(Of SyntaxNode)()
                    If node.IsKind(SyntaxKind.SingleLineSubLambdaExpression, SyntaxKind.SingleLineFunctionLambdaExpression,
                                        SyntaxKind.MultiLineSubLambdaExpression, SyntaxKind.MultiLineFunctionLambdaExpression) Then

                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Await", VBFeaturesResources.Asynchronously_waits_for_the_task_to_finish))
                    End If

                    If node.IsKind(SyntaxKind.FinallyBlock, SyntaxKind.SyncLockBlock, SyntaxKind.CatchBlock) Then
                        Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                    End If
                Next

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Await", VBFeaturesResources.Asynchronously_waits_for_the_task_to_finish))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
