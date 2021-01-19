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
    ''' Recommends the "Case" and possibly "Case Else" keyword inside a Select block
    ''' </summary>
    Friend Class CaseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            ' Are we after "Select" for "Select Case"?
            If targetToken.Kind = SyntaxKind.SelectKeyword AndAlso
               Not targetToken.Parent.IsKind(SyntaxKind.SelectClause) AndAlso
               Not context.FollowsEndOfStatement Then

                Return ImmutableArray.Create(New RecommendedKeyword("Case", VBFeaturesResources.Introduces_a_value_or_set_of_values_against_which_the_value_of_an_expression_in_a_Select_Case_statement_is_to_be_tested_Case_expression_expression1_To_expression2_bracket_Is_bracket_comparisonOperator_expression))
            End If

            ' A "Case" keyword must be in a Select block, and exists either where a regular executable statement can go
            ' or the special case of being immediately after the Select Case
            If Not context.IsInStatementBlockOfKind(SyntaxKind.SelectBlock) OrElse
               Not (context.IsMultiLineStatementContext OrElse context.IsAfterStatementOfKind(SyntaxKind.SelectStatement)) Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim selectStatement = targetToken.GetAncestor(Of SelectBlockSyntax)()
            Dim validKeywords As New List(Of RecommendedKeyword)

            ' We can do "Case" as long as we're not after a "Case Else"
            Dim caseElseBlock = selectStatement.CaseBlocks.FirstOrDefault(Function(caseBlock) caseBlock.CaseStatement.Kind = SyntaxKind.CaseElseStatement)
            If caseElseBlock Is Nothing OrElse targetToken.SpanStart < caseElseBlock.SpanStart Then
                validKeywords.Add(New RecommendedKeyword("Case", VBFeaturesResources.Introduces_a_value_or_set_of_values_against_which_the_value_of_an_expression_in_a_Select_Case_statement_is_to_be_tested_Case_expression_expression1_To_expression2_bracket_Is_bracket_comparisonOperator_expression))
            End If

            ' We can do a "Case Else" as long as we're the last one and we don't already have one.
            ' We exclude any partial case keywords the parser is creating (possibly because of user typing)
            Dim lastBlock = selectStatement.CaseBlocks.LastOrDefault(Function(caseBlock) Not caseBlock.CaseStatement.CaseKeyword.IsMissing)
            If caseElseBlock Is Nothing AndAlso (lastBlock Is Nothing OrElse targetToken.SpanStart > lastBlock.SpanStart) Then
                validKeywords.Add(New RecommendedKeyword("Case Else", VBFeaturesResources.Introduces_the_statements_to_run_if_none_of_the_previous_cases_in_the_Select_Case_statement_returns_True))
            End If

            Return validKeywords.ToImmutableArray()
        End Function
    End Class
End Namespace
