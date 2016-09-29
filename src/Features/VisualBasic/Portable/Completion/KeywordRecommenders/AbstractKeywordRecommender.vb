' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
