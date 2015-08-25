' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "While" and "Until" keywords as a part of a Do or Loop statements
    ''' </summary>
    Friend Class UntilAndWhileKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.FollowsEndOfStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If (targetToken.Kind = SyntaxKind.DoKeyword AndAlso TypeOf targetToken.Parent Is DoStatementSyntax) OrElse
             (targetToken.Kind = SyntaxKind.LoopKeyword AndAlso
             TypeOf targetToken.Parent Is LoopStatementSyntax AndAlso
             targetToken.Parent.Parent.IsKind(SyntaxKind.SimpleDoLoopBlock, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock)) Then
                Return {New RecommendedKeyword("Until", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                                        VBFeaturesResources.LoopUntilKeywordToolTip,
                                                                        VBFeaturesResources.DoUntilKeywordToolTip)),
                        New RecommendedKeyword("While", If(targetToken.Kind = SyntaxKind.LoopKeyword,
                                                                        VBFeaturesResources.LoopWhileKeywordToolTip,
                                                                        VBFeaturesResources.DoWhileKeywordToolTip))}
            Else
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If
        End Function
    End Class
End Namespace
