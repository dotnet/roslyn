﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.OnErrorStatements
    ''' <summary>
    ''' Recommends "Next" after "On Error Resume" or after the "Resume" statement
    ''' </summary>
    Friend Class NextKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ResumeKeyword) AndAlso Not context.IsInLambda AndAlso
               targetToken.Parent.IsKind(SyntaxKind.OnErrorResumeNextStatement, SyntaxKind.ResumeStatement, SyntaxKind.ResumeNextStatement) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Next",
                                                                                         VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error))
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
