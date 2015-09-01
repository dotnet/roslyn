' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "MyClass" keyword.
    ''' </summary>
    Friend Class MyClassKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext OrElse context.IsNameOfContext) AndAlso
               targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                ' This isn't valid in shared methods
                Dim methodBlock = targetToken.GetAncestor(Of MethodBlockBaseSyntax)()

                If methodBlock IsNot Nothing AndAlso methodBlock.BlockStatement.Modifiers.Any(Function(modifier) modifier.Kind = SyntaxKind.SharedKeyword) Then
                    Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                End If

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyClassKeyword), VBFeaturesResources.MyClassKeywordToolTip))
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=False, cancellationToken:=cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MyClassKeyword), VBFeaturesResources.MyClassKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
