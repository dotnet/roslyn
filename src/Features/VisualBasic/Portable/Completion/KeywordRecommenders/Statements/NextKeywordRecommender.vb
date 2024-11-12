' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Next" statement.
    ''' </summary>
    Friend Class NextKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
             ImmutableArray.Create(New RecommendedKeyword("Next", VBFeaturesResources.Terminates_a_loop_that_iterates_through_the_values_of_a_loop_variable))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, CancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsStatementContext AndAlso context.IsInStatementBlockOfKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock),
                s_keywords,
                ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
