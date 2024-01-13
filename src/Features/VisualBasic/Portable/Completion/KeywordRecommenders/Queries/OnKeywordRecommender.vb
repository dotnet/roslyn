' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "On" keyword.
    ''' </summary>
    Friend Class OnKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("On", VBFeaturesResources.Specifies_the_element_keys_used_to_correlate_sequences_for_a_join_operation))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.SyntaxTree.IsFollowingCompleteExpression(Of JoinClauseSyntax)(context.Position, context.TargetToken, Function(joinQuery) joinQuery.JoinedVariables.LastCollectionExpression, cancellationToken) OrElse
               context.SyntaxTree.IsFollowingCompleteExpression(Of JoinConditionSyntax)(context.Position, context.TargetToken, Function(joinCondition) joinCondition.Right, cancellationToken) Then
                Dim token = context.TargetToken.GetPreviousToken()

                ' There must be at least one Join clause in this query which doesn't have an On statement. We also recommend
                ' it if the parser has already placed this On in the tree.
                For Each joinClause In token.GetAncestors(Of JoinClauseSyntax)()
                    If joinClause.OnKeyword.IsMissing OrElse joinClause.OnKeyword = token Then
                        Return s_keywords
                    End If
                Next
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
