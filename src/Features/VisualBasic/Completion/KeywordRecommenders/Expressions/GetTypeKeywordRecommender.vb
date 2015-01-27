' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities.IntrinsicOperators

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "GetType" keyword.
    ''' </summary>
    Friend Class GetTypeKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext OrElse context.IsSingleLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(
                   CreateRecommendedKeywordForIntrinsicOperator(SyntaxKind.GetTypeKeyword,
                                                                VBFeaturesResources.GettypeFunction,
                                                                Glyph.Keyword,
                                                                New GetTypeExpressionDocumentation(),
                                                                context.SemanticModel,
                                                                context.Position))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
