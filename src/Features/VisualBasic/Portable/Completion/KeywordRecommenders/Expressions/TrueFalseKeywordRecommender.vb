' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "True" and "False" keywords
    ''' </summary>
    Friend Class TrueFalseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return {New RecommendedKeyword("True", VBFeaturesResources.TrueKeywordToolTip),
                        New RecommendedKeyword("False", VBFeaturesResources.FalseKeywordToolTip)}
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
