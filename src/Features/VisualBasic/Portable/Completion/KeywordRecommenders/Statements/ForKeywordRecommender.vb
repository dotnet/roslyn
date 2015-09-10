' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "For" keyword for the statement context
    ''' </summary>
    Friend Class ForKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return {New RecommendedKeyword("For", VBFeaturesResources.ForKeywordToolTip),
                        New RecommendedKeyword("For Each", VBFeaturesResources.ForEachKeywordToolTip)}
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                If targetToken.IsKind(SyntaxKind.ExitKeyword) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("For", VBFeaturesResources.ExitForKeywordToolTip))
                Else
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("For", VBFeaturesResources.ContinueForKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
