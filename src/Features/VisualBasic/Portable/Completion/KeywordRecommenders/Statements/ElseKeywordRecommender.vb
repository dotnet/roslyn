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
    ''' Recommends the "Else" keyword for the statement context.
    ''' </summary>
    Friend Class ElseKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken
            Dim parent = targetToken.GetAncestor(Of SingleLineIfStatementSyntax)()

            If parent IsNot Nothing AndAlso Not parent.Statements.IsEmpty() Then
                If context.IsFollowingCompleteStatement(Of SingleLineIfStatementSyntax)(Function(ifStatement) ifStatement.Statements.Last()) Then
                    Return ImmutableArray.Create(New RecommendedKeyword("Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_If_statement_that_is_executed_if_no_previous_condition_evaluates_to_True))
                End If
            End If

            If context.IsStatementContext AndAlso
               IsDirectlyInIfOrElseIf(context) Then

                Return ImmutableArray.Create(New RecommendedKeyword("Else", VBFeaturesResources.Introduces_a_group_of_statements_in_an_If_statement_that_is_executed_if_no_previous_condition_evaluates_to_True))
            End If

            ' Determine whether we can offer "Else" after "Case" in a Select block.
            If targetToken.Kind = SyntaxKind.CaseKeyword AndAlso targetToken.Parent.IsKind(SyntaxKind.CaseStatement) Then
                ' Next, grab the parenting "Select" block and ensure that it doesn't have any Case Else statements
                Dim selectBlock = targetToken.GetAncestor(Of SelectBlockSyntax)()
                If selectBlock IsNot Nothing AndAlso
                   Not selectBlock.CaseBlocks.Any(Function(cb) cb.CaseStatement.Kind = SyntaxKind.CaseElseStatement) Then

                    ' Finally, ensure this case statement is the last one in the parenting "Select" block.
                    If selectBlock.CaseBlocks.Last().CaseStatement Is targetToken.Parent Then
                        Return ImmutableArray.Create(New RecommendedKeyword("Else", VBFeaturesResources.Introduces_the_statements_to_run_if_none_of_the_previous_cases_in_the_Select_Case_statement_returns_True))
                    End If
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function IsDirectlyInIfOrElseIf(context As VisualBasicSyntaxContext) As Boolean
            ' Maybe we're after the Then keyword
            If context.TargetToken.IsKind(SyntaxKind.ThenKeyword) AndAlso
                context.TargetToken.Parent?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock) Then
                Return True
            End If

            Dim statement = context.TargetToken.Parent.GetAncestor(Of StatementSyntax)
            Return If(statement?.Parent.IsKind(SyntaxKind.MultiLineIfBlock, SyntaxKind.ElseIfBlock), False)
        End Function
    End Class
End Namespace
