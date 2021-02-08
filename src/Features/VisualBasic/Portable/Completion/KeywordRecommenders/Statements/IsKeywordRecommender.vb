' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Is" keyword at the beginning of any clause in a "Case" statement
    ''' </summary>
    Friend Class IsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Is", VBFeaturesResources.Followed_by_a_comparison_operator_and_then_an_expression_Case_Is_introduces_the_statements_to_run_if_the_Select_Case_expression_combined_with_the_Case_Is_expression_evaluates_to_True))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            ' Determine whether we can offer "Is" at the beginning of a CaseClauseSyntax. Make sure
            ' that the token is not after a CaseElseStatement.
            Dim selectBlock = targetToken.GetAncestor(Of SelectBlockSyntax)()
            If selectBlock IsNot Nothing Then
                Dim caseElseBlock = selectBlock.CaseBlocks.FirstOrDefault(Function(caseBlock) caseBlock.CaseStatement.Kind = SyntaxKind.CaseElseStatement)
                If caseElseBlock Is Nothing OrElse targetToken.SpanStart < caseElseBlock.SpanStart Then
                    ' Handle cases where the token is at the beginning of the first clause
                    ' (following the Case keyword) and where the token is at the beginning of
                    ' subsequent clauses (following the list separator token).
                    If targetToken.IsChildToken(Of CaseStatementSyntax)(Function(caseStatement) caseStatement.CaseKeyword) OrElse
                       targetToken.IsChildSeparatorToken(Of CaseStatementSyntax, CaseClauseSyntax)(Function(caseStatement) caseStatement.Cases) Then
                        Return s_keywords
                    End If
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

    End Class
End Namespace
