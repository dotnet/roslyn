' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders

    Friend MustInherit Class AbstractKeywordRecommender
        Implements IKeywordRecommender(Of VisualBasicSyntaxContext)

        Public Function RecommendKeywordsAsync(
            position As Integer,
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken) As Task(Of IEnumerable(Of RecommendedKeyword)) Implements IKeywordRecommender(Of VisualBasicSyntaxContext).RecommendKeywordsAsync

            Return Task.FromResult(RecommendKeywords(context, cancellationToken))
        End Function

        Friend Function RecommendKeywords_Test(context As VisualBasicSyntaxContext) As IEnumerable(Of RecommendedKeyword)
            Return RecommendKeywords(context, CancellationToken.None)
        End Function

        Protected MustOverride Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
    End Class
End Namespace
