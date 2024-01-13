' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "RemoveHandler" keyword.
    ''' </summary>
    Friend Class RemoveHandlerKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
                    CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.RemoveHandlerKeyword,
                                                                 VBFeaturesResources.RemoveHandler_statement,
                                                                 Glyph.Keyword,
                                                                 New RemoveHandlerStatementDocumentation()))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsStatementContext OrElse context.CanDeclareCustomEventAccessor(SyntaxKind.RemoveHandlerAccessorBlock),
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
