' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "On Error Resume Next" and "On Error Goto" in a statement context.
    ''' </summary>
    Friend Class OnErrorKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) = ImmutableArray.Create(
            New RecommendedKeyword("On Error Resume Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error),
            New RecommendedKeyword("On Error GoTo", VBFeaturesResources.Enables_the_error_handling_routine_that_starts_at_the_line_specified_in_the_line_argument_The_specified_line_must_be_in_the_same_procedure_as_the_On_Error_statement_On_Error_GoTo_bracket_label_0_1_bracket))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Return If(context.IsMultiLineStatementContext AndAlso Not context.IsInLambda, s_keywords, ImmutableArray(Of RecommendedKeyword).Empty)
        End Function
    End Class
End Namespace
