' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Queries
    ''' <summary>
    ''' Recommends the "Group" query operator.
    ''' </summary>
    Friend Class GroupKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsQueryOperatorContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Group", VBFeaturesResources.GroupByQueryKeywordToolTip))
            End If

            Dim targetToken = context.TargetToken

            ' Group By ... Into |
            ' Group Join ... Into |
            If targetToken.IsChildToken(Of GroupByClauseSyntax)(Function(g) g.IntoKeyword) OrElse
               targetToken.IsChildToken(Of GroupJoinClauseSyntax)(Function(gj) gj.IntoKeyword) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Group", VBFeaturesResources.GroupRefQueryKeywordToolTip))
            End If

            ' Group By ... Into ... = |
            ' Group Join ... Into ... = |
            If targetToken.IsChildToken(Of VariableNameEqualsSyntax)(Function(vne) vne.EqualsToken) Then
                Dim variableNameEquals = targetToken.GetAncestor(Of VariableNameEqualsSyntax)()
                If variableNameEquals.IsParentKind(SyntaxKind.AggregationRangeVariable) AndAlso
                  (variableNameEquals.Parent.IsParentKind(SyntaxKind.GroupByClause) OrElse
                   variableNameEquals.Parent.IsParentKind(SyntaxKind.GroupJoinClause)) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Group",
                                                                                             String.Format(VBFeaturesResources.GroupRefNameQueryKeywordToolTip,
                                                                                                                    variableNameEquals.Identifier.Identifier.ValueText)))
                End If
            End If

            ' Group By ... Into ... , |
            ' Group Join ... Into ... , |
            If targetToken.IsChildSeparatorToken(Of GroupByClauseSyntax, AggregationRangeVariableSyntax)(Function(g) g.AggregationVariables) OrElse
               targetToken.IsChildSeparatorToken(Of GroupByClauseSyntax, AggregationRangeVariableSyntax)(Function(g) g.AggregationVariables) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Group", VBFeaturesResources.GroupByQueryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
