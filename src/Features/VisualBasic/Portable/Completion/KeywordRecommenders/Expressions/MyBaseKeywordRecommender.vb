﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyBaseKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_base_class_of_the_current_class_instance_You_cannot_use_MyBase_to_call_MustOverride_base_methods))
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=True, cancellationToken:=cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyBaseKeyword), VBFeaturesResources.Provides_a_way_to_refer_to_the_base_class_of_the_current_class_instance_You_cannot_use_MyBase_to_call_MustOverride_base_methods))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
