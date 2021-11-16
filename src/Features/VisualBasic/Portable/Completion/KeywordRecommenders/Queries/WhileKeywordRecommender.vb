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
    ''' Recommends the While keyword after a Skip/Take query
    ''' </summary>
    Friend Class WhileKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("While", VBFeaturesResources.Specifies_a_condition_for_Skip_and_Take_operations_Elements_will_be_bypassed_or_included_as_long_as_the_condition_is_true))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            ' We may get two different types, depending on whether the user has already typed the While or not.
            If targetToken.IsChildToken(Of PartitionClauseSyntax)(Function(partitionQuery) partitionQuery.SkipOrTakeKeyword) OrElse
               targetToken.IsChildToken(Of PartitionWhileClauseSyntax)(Function(partitionWhileQuery) partitionWhileQuery.SkipOrTakeKeyword) Then
                Return s_keywords
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
