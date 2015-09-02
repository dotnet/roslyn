' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Sub", "Function", "Async" and "Iterator" keywords in expression contexts that would start a lambda.
    ''' </summary>
    Friend Class LambdaKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsDelegateCreationContext() Then
                Return {New RecommendedKeyword("Async", VBFeaturesResources.AsyncLambdaKeywordToolTip),
                        New RecommendedKeyword("Function", VBFeaturesResources.FunctionLambdaKeywordToolTip),
                        New RecommendedKeyword("Iterator", VBFeaturesResources.IteratorLambdaKeywordToolTip),
                        New RecommendedKeyword("Sub", VBFeaturesResources.SubLambdaKeywordToolTip)}
            End If

            Dim targetToken = context.TargetToken
            If context.SyntaxTree.IsExpressionContext(targetToken.SpanStart, cancellationToken, context.SemanticModel) Then
                If targetToken.IsKindOrHasMatchingText(SyntaxKind.IteratorKeyword) Then
                    Return {New RecommendedKeyword("Function", VBFeaturesResources.FunctionLambdaKeywordToolTip)}
                ElseIf targetToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) Then
                    Return {New RecommendedKeyword("Function", VBFeaturesResources.FunctionLambdaKeywordToolTip),
                            New RecommendedKeyword("Sub", VBFeaturesResources.SubLambdaKeywordToolTip)}
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
