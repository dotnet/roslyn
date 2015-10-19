' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Await", VBFeaturesResources.AwaitKeywordToolTip))
                    End If

                    If node.IsKind(SyntaxKind.FinallyBlock, SyntaxKind.SyncLockBlock, SyntaxKind.CatchBlock) Then
                        Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                    End If
                Next

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Await", VBFeaturesResources.AwaitKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
