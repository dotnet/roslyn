' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Do" keyword at the start of a statement
    ''' </summary>
    Friend Class DoKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                Return {New RecommendedKeyword("Do", VBFeaturesResources.DoKeywordToolTip),
                        New RecommendedKeyword("Do Until", VBFeaturesResources.DoUntilKeywordToolTip),
                        New RecommendedKeyword("Do While", VBFeaturesResources.DoWhileKeywordToolTip)}
            End If

            ' Are we after Exit or Continue?
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            If targetToken.IsKind(SyntaxKind.ExitKeyword, SyntaxKind.ContinueKeyword) AndAlso
               context.IsInStatementBlockOfKind(SyntaxKind.SimpleDoLoopBlock,
                                                SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
                                                SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock) AndAlso
               Not context.IsInStatementBlockOfKind(SyntaxKind.FinallyBlock) Then

                If targetToken.IsKind(SyntaxKind.ExitKeyword) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Do", VBFeaturesResources.ExitDoKeywordToolTip))
                Else
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Do", VBFeaturesResources.ContinueDoKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
