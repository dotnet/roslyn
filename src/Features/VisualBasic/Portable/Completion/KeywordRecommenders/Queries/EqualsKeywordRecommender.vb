' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the Equals keyword when in a join syntax.
    ''' </summary>
    Friend Class EqualsKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.SyntaxTree.IsFollowingCompleteExpression(Of JoinConditionSyntax)(
               context.Position, context.TargetToken, Function(joinCondition) joinCondition.Left, cancellationToken) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Equals", VBFeaturesResources.Specifies_the_relationship_between_element_keys_to_use_as_the_basis_of_a_join_operation))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
