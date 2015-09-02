' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "When" keyword for a Catch filter
    ''' </summary>
    Friend Class WhenKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsFromIdentifierNode(Of CatchStatementSyntax)(Function(catchStatement) catchStatement.IdentifierName) Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("When", VBFeaturesResources.WhenKeywordToolTip))
            End If

            If context.SyntaxTree.IsFollowingCompleteExpression(Of SimpleAsClauseSyntax)(context.Position, context.TargetToken,
                childGetter:=Function(asClause) If(TypeOf asClause.Parent Is CatchStatementSyntax, asClause.Type, Nothing), cancellationToken:=cancellationToken) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("When", VBFeaturesResources.WhenKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
