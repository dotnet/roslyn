' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Catch" keyword for the statement context
    ''' </summary>
    Friend Class CatchKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If Not context.IsMultiLineStatementContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' We'll recommend a catch statement if it's within a Try block or a Catch block, because you could be
            ' trying to start one in either location
            If context.IsInStatementBlockOfKind(SyntaxKind.TryBlock, SyntaxKind.CatchBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Catch", VBFeaturesResources.Introduces_a_statement_block_to_be_run_if_the_specified_exception_occurs_inside_a_Try_block))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
