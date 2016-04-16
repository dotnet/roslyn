' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Try" keyword for the statement context
    ''' </summary>
    Friend Class TryKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Try", VBFeaturesResources.TryKeywordToolTip))
            End If

            ' Are we after Exit or Continue (but not in a Finally block)?
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.TryBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Try", VBFeaturesResources.ExitTryKeywordToolTip))
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
