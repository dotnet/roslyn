' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
                                New RecommendedKeyword("Async", VBFeaturesResources.Indicates_an_asynchronous_method_that_can_use_the_Await_operator))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
