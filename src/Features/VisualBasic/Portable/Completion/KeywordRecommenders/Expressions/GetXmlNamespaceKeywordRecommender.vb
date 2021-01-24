' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "GetXmlNamespace" keyword.
    ''' </summary>
    Friend Class GetXmlNamespaceKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return ImmutableArray.Create(
                   CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.GetXmlNamespaceKeyword,
                                                                VBFeaturesResources.GetXmlNamespace_function,
                                                                Glyph.MethodPublic,
                                                                New GetXmlNamespaceExpressionDocumentation(),
                                                                context.SemanticModel,
                                                                context.Position))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
