' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsAnyExpressionContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("New", VBFeaturesResources.Creates_a_new_object_instance))
            End If

            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsChildToken(Of AsClauseSyntax)(Function(asClause) asClause.AsKeyword) Then
                Dim asClause = targetToken.GetAncestor(Of AsClauseSyntax)()
                If asClause.IsParentKind(SyntaxKind.VariableDeclarator) OrElse
                   (asClause.IsParentKind(SyntaxKind.PropertyStatement) AndAlso
                    Not DirectCast(asClause.Parent, PropertyStatementSyntax).Modifiers.Any(
                        Function(m) m.IsKind(SyntaxKind.WriteOnlyKeyword))) Then

                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("New", VBFeaturesResources.Creates_a_new_object_instance))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
