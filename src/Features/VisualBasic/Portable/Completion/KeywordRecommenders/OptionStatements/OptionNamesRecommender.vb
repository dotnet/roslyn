' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OptionStatements
    ''' <summary>
    ''' Recommends the names of options that can appear after an Option keyword, such as Compare or Infer.
    ''' </summary>
    Friend Class OptionNamesRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            If context.TargetToken.IsKind(SyntaxKind.OptionKeyword) Then
                Return {New RecommendedKeyword("Compare", VBFeaturesResources.CompareKeywordToolTip),
                        New RecommendedKeyword("Explicit", VBFeaturesResources.ExplicitKeywordToolTip),
                        New RecommendedKeyword("Infer", VBFeaturesResources.InferKeywordToolTip),
                        New RecommendedKeyword("Strict", VBFeaturesResources.StrictKeywordToolTip)}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
