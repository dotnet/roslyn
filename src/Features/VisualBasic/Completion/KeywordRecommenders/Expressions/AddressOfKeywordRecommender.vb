' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "AddressOf" keyword.
    ''' </summary>
    Friend Class AddressOfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.IsDelegateCreationContext() Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("AddressOf", VBFeaturesResources.AddressOfKeywordToolTip))
            End If

            If context.IsAnyExpressionContext AndAlso Not context.TargetToken.Parent.IsKind(SyntaxKind.AddressOfExpression) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("AddressOf", VBFeaturesResources.AddressOfKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
