' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    Friend Class NameOfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return SpecializedCollections.SingletonEnumerable(CreateRecommendedKeywordForIntrinsicOperator(
                    SyntaxKind.NameOfKeyword,
                    VBFeaturesResources.NameOf_function,
                    Glyph.MethodPublic,
                    New NameOfExpressionDocumentation(),
                    context.SemanticModel,
                    context.Position))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
