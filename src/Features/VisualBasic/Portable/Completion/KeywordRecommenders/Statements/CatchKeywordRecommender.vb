' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Catch" keyword for the statement context
    ''' </summary>
    Friend Class CatchKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If Not context.IsMultiLineStatementContext Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            ' We'll recommend a catch statement if it's within a Try block or a Catch block, because you could be
            ' trying to start one in either location
            If context.IsInStatementBlockOfKind(SyntaxKind.TryBlock, SyntaxKind.CatchBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return ImmutableArray.Create(New RecommendedKeyword("Catch", VBFeaturesResources.Introduces_a_statement_block_to_be_run_if_the_specified_exception_occurs_inside_a_Try_block))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
