' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "With" keyword when used in a New syntax (such as New foo With)
    ''' </summary>
    Friend Class WithKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim token = context.TargetToken
            If token.IsChildToken(Of AsNewClauseSyntax)(Function(asNewClause) asNewClause.NewExpression.NewKeyword) OrElse
               token.IsFollowingCompleteAsNewClause() OrElse
               token.IsChildToken(Of ObjectCreationExpressionSyntax)(Function(objectCreation) objectCreation.NewKeyword) OrElse
               token.IsFollowingCompleteObjectCreation() Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("With",
                                                                                         VBFeaturesResources.WithInitializerKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
