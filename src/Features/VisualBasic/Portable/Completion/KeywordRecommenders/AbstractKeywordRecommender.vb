' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders
    Friend MustInherit Class AbstractKeywordRecommender
        Implements IKeywordRecommender(Of VisualBasicSyntaxContext)

        Public Function RecommendKeywords(
            position As Integer,
            context As VisualBasicSyntaxContext,
            cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword) Implements IKeywordRecommender(Of VisualBasicSyntaxContext).RecommendKeywords

            Return RecommendKeywords(context, cancellationToken)
        End Function

        Friend Function RecommendKeywords_Test(context As VisualBasicSyntaxContext) As ImmutableArray(Of RecommendedKeyword)
            Return RecommendKeywords(context, CancellationToken.None)
        End Function

        Protected MustOverride Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
    End Class
End Namespace
