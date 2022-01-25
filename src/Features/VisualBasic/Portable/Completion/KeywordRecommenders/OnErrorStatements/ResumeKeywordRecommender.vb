' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Resume Next" after "On Error", or "Resume" as a standalone statement
    ''' </summary>
    Friend Class ResumeKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            ' On Error statements are never valid in lambdas
            If context.IsInLambda Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If targetToken.Kind = SyntaxKind.ErrorKeyword AndAlso IsOnErrorStatement(targetToken.Parent) Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Resume Next", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error))
            End If

            If context.IsMultiLineStatementContext Then
                Return ImmutableArray.Create(
                    New RecommendedKeyword("Resume", VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error))
                ' TODO: we are inconsistent here in Dev10. We offer "On Error Resume Next" even after typing just "On",
                ' yet curiously we don't show "Resume Next" as it's own statement. This might be something to fix if
                ' we determine we even care.
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
