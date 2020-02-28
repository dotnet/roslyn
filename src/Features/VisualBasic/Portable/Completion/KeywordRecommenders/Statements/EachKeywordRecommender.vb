﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Each" keyword after the "For" keyword
    ''' </summary>
    Friend Class EachKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsKind(SyntaxKind.ForKeyword) AndAlso targetToken.Parent.IsKind(SyntaxKind.ForStatement) Then
                Dim forStatement = DirectCast(targetToken.Parent, ForStatementSyntax)
                If forStatement.EqualsToken = Nothing OrElse forStatement.EqualsToken.IsMissing Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Each", VBFeaturesResources.Introduces_a_loop_that_is_repeated_for_each_element_in_a_collection))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
