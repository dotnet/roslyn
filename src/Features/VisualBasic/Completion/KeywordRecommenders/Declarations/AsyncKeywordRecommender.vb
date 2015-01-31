' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    Friend Class AsyncKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            ' Function/Sub declaration
            If context.IsTypeMemberDeclarationKeywordContext OrElse context.IsInterfaceMemberDeclarationKeywordContext Then
                If context.ModifierCollectionFacts.AsyncKeyword.Kind = SyntaxKind.None AndAlso
                   context.ModifierCollectionFacts.IteratorKeyword.Kind = SyntaxKind.None AndAlso
                   context.ModifierCollectionFacts.OverridableSharedOrPartialKeyword.Kind <> SyntaxKind.PartialKeyword AndAlso
                   context.ModifierCollectionFacts.MutabilityOrWithEventsKeyword.Kind = SyntaxKind.None Then

                    Return SpecializedCollections.SingletonEnumerable(
                                New RecommendedKeyword("Async", VBFeaturesResources.AsyncKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
