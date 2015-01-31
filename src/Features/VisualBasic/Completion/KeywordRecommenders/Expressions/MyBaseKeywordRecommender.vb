' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "MyBase" keyword.
    ''' </summary>
    Friend Class MyBaseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext OrElse context.IsNameOfContext) AndAlso
                targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyBaseKeyword), VBFeaturesResources.MyBaseKeywordToolTip))
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=True, cancellationToken:=cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyBaseKeyword), VBFeaturesResources.MyBaseKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
