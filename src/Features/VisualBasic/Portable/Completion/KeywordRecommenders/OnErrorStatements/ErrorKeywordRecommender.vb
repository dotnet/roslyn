' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Error" after "On" in a "On Error" statement.
    ''' </summary>
    Friend Class ErrorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If targetToken.IsKind(SyntaxKind.OnKeyword) AndAlso IsOnErrorStatement(targetToken.Parent) Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Error Resume Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error),
                    New RecommendedKeyword("Error GoTo", VBFeaturesResources.Enables_the_error_handling_routine_that_starts_at_the_line_specified_in_the_line_argument_The_specified_line_must_be_in_the_same_procedure_as_the_On_Error_statement_On_Error_GoTo_bracket_label_0_1_bracket))
            End If

            ' The Error statement (i.e. "Error 11" to raise an error)
            If context.IsMultiLineStatementContext OrElse context.IsStatementContext Then
                Return ImmutableArray.Create(New RecommendedKeyword("Error", VBFeaturesResources.Simulates_the_occurrence_of_an_error))
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
