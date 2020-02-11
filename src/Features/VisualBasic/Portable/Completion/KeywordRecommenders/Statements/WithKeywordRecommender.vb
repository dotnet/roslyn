' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "With" statement.
    ''' </summary>
    Friend Class WithKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("With", VBFeaturesResources.Runs_a_series_of_statements_that_refer_to_a_single_object_or_structure_With_object_End_With))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
