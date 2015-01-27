' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Dim" keyword in all appropriate contexts.
    ''' </summary>
    Friend Class DimKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' It can start a statement
            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(
                            New RecommendedKeyword("Dim", VBFeaturesResources.DimKeywordToolTip))
            End If

            If context.IsTypeMemberDeclarationKeywordContext Then
                Dim modifiers = context.ModifierCollectionFacts

                ' In Dev10, we don't show it after Const (but will after ReadOnly, even though the formatter removes it)
                If modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Field) AndAlso
                   modifiers.MutabilityOrWithEventsKeyword.Kind <> SyntaxKind.ConstKeyword AndAlso
                   modifiers.DimKeyword.Kind = SyntaxKind.None Then
                    Return SpecializedCollections.SingletonEnumerable(
                                New RecommendedKeyword("Dim", VBFeaturesResources.DimKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
