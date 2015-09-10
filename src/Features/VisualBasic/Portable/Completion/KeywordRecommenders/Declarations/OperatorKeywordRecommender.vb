' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends the "Operator" keyword in member declaration contexts
    ''' </summary>
    Friend Class OperatorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim modifiers = context.ModifierCollectionFacts

            If context.SyntaxTree.IsDeclarationContextWithinTypeBlocks(context.Position, context.TargetToken, True, cancellationToken, SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) AndAlso
               modifiers.CouldApplyToOneOf(PossibleDeclarationTypes.Operator) Then
                If modifiers.NarrowingOrWideningKeyword.Kind <> SyntaxKind.None Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Operator CType", VBFeaturesResources.OperatorCTypeKeywordToolTip))
                Else
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Operator", VBFeaturesResources.OperatorKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
