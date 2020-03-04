' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "If" keyword for the statement context
    ''' </summary>
    Friend Class IfKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsSingleLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("If", VBFeaturesResources.Conditionally_executes_a_group_of_statements_depending_on_the_value_of_an_expression))
            End If

            ' We might be typing "Else If" as two keywords. At this point, the parser is parsing this statement as a
            ' ElseStatementSyntax.
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsChildToken(Of ElseStatementSyntax)(Function(ifStatement) ifStatement.ElseKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("If", VBFeaturesResources.Introduces_a_condition_in_an_If_statement_that_is_to_be_tested_if_the_previous_conditional_test_fails))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
