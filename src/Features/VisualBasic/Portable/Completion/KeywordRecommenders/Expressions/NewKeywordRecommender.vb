' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "New" keyword.
    ''' </summary>
    Friend Class NewKeywordRecommender
        Inherits AbstractKeywordRecommender

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("New", VBFeaturesResources.Creates_a_new_object_instance))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return s_keywords
            End If

            If context.FollowsEndOfStatement Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsChildToken(Of AsClauseSyntax)(Function(asClause) asClause.AsKeyword) Then
                Dim asClause = targetToken.GetAncestor(Of AsClauseSyntax)()
                If asClause.IsParentKind(SyntaxKind.VariableDeclarator) OrElse
                   (asClause.IsParentKind(SyntaxKind.PropertyStatement) AndAlso
                    Not DirectCast(asClause.Parent, PropertyStatementSyntax).Modifiers.Any(
                        Function(m) m.IsKind(SyntaxKind.WriteOnlyKeyword))) Then

                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function
    End Class
End Namespace
