' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
    ''' <summary>
    ''' Recommends the "Key" keyword.
    ''' </summary>
    Friend Class KeyKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            ' Key can only come after a { or a , in a initializer
            If targetToken.IsKind(SyntaxKind.OpenBraceToken, SyntaxKind.CommaToken) AndAlso
               targetToken.Parent.IsKind(SyntaxKind.ObjectMemberInitializer) Then
                ' We might be in an expression...
                If targetToken.Parent.GetParentOrNull().IsKind(SyntaxKind.AnonymousObjectCreationExpression) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Key", VBFeaturesResources.KeyKeywordToolTip))
                End If

                ' Or we might be in an AsNew. In this case, we need to check to make sure the type is correct
                If targetToken.Parent.GetParentOrNull().GetParentOrNull().IsKind(SyntaxKind.AsNewClause) Then
                    Dim asNewClause = DirectCast(targetToken.Parent.Parent.Parent, AsNewClauseSyntax)
                    If asNewClause.Type.IsMissing Then
                        Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Key", VBFeaturesResources.KeyKeywordToolTip))
                    End If
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
