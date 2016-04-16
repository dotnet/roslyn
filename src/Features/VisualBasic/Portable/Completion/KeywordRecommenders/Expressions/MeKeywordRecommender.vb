' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Me" keyword.
    ''' </summary>
    Friend Class MeKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If (context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext OrElse context.IsNameOfContext) AndAlso
                targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then

                If targetToken.GetContainingMemberBlockBegin().TypeSwitch(
                    Function(methodBase As MethodBaseSyntax) Not methodBase.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(propertyStatement As PropertyStatementSyntax) Not propertyStatement.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(eventStatement As EventStatementSyntax) Not eventStatement.Modifiers.Any(SyntaxKind.SharedKeyword)) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.MeKeywordToolTip))
                End If

                If targetToken.GetContainingMember().TypeSwitch(Function(fieldInitializer As FieldDeclarationSyntax) Not fieldInitializer.Modifiers.Any(SyntaxKind.SharedKeyword)) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.MeKeywordToolTip))
                End If
            End If

            If context.IsAccessibleEventContext(startAtEnclosingBaseType:=False, cancellationToken:=cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword(SyntaxFacts.GetText(SyntaxKind.MeKeyword), VBFeaturesResources.MeKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
