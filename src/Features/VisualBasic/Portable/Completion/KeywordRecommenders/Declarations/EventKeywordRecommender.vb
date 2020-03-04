' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Event" keyword in type declaration contexts
    ''' </summary>
    Friend Class EventKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsTypeMemberDeclarationKeywordContext OrElse context.IsInterfaceMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Event) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Event", VBFeaturesResources.Declares_a_user_defined_event))
                End If
            End If

            ' We also allow "Event" after Custom (which is parsed as an identifier) in a class/structure declaration context
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.Kind = SyntaxKind.IdentifierToken AndAlso SyntaxFacts.GetContextualKeywordKind(targetToken.GetIdentifierText()) = SyntaxKind.CustomKeyword Then
                If targetToken.GetAncestor(Of MethodBlockBaseSyntax)() Is Nothing AndAlso
                    targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.StructureBlock, SyntaxKind.ClassBlock) Then

                    Dim variableDeclarator = targetToken.GetAncestor(Of VariableDeclaratorSyntax)()
                    If variableDeclarator IsNot Nothing Then
                        If variableDeclarator.Names.Count = 1 AndAlso variableDeclarator.Names.First().Identifier = targetToken Then
                            Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Event", VBFeaturesResources.Declares_a_user_defined_event))
                        End If
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
