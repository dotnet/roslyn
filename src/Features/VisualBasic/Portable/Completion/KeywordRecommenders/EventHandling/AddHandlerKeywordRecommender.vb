' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.EventHandling
    ''' <summary>
    ''' Recommends the "AddHandler" keyword.
    ''' </summary>
    Friend Class AddHandlerKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext OrElse context.CanDeclareCustomEventAccessor(SyntaxKind.AddHandlerAccessorBlock) Then
                Return SpecializedCollections.SingletonEnumerable(
                                CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.AddHandlerKeyword,
                                                                             VBFeaturesResources.AddHandler_statement,
                                                                             Glyph.Keyword,
                                                                             New AddHandlerStatementDocumentation()))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
