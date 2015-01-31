' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Loop" statement.
    ''' </summary>
    Friend Class LoopKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            Dim targetToken = context.TargetToken

            If context.IsSingleLineStatementContext Then
                Dim doBlock = targetToken.GetAncestor(Of DoLoopBlockSyntax)()

                If doBlock Is Nothing OrElse Not doBlock.LoopStatement.IsMissing Then
                    Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
                End If

                If doBlock.Kind <> SyntaxKind.SimpleDoLoopBlock Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Loop", VBFeaturesResources.LoopKeywordToolTip))
                Else
                    Return {New RecommendedKeyword("Loop", VBFeaturesResources.LoopKeywordToolTip),
                            New RecommendedKeyword("Loop Until", VBFeaturesResources.LoopUntilKeywordToolTip),
                            New RecommendedKeyword("Loop While", VBFeaturesResources.LoopWhileKeywordToolTip)}
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
