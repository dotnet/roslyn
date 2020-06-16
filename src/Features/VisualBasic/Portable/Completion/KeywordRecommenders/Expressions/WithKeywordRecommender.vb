' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "With" keyword when used in a New syntax (such as New goo With)
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
                                                                                         VBFeaturesResources.Specifies_the_declaration_of_property_initializations_in_an_object_initializer_New_typeName_With_bracket_property_expression_bracket_bracket_bracket))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
