' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Continue" keyword at the start of a statement when in any loop.
    ''' </summary>
    Friend Class ContinueKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsMultiLineStatementContext Then
                If context.IsInStatementBlockOfKind(
                    SyntaxKind.SimpleDoLoopBlock,
                    SyntaxKind.DoWhileLoopBlock,
                    SyntaxKind.DoUntilLoopBlock,
                    SyntaxKind.DoLoopWhileBlock,
                    SyntaxKind.DoLoopUntilBlock,
                    SyntaxKind.ForBlock,
                    SyntaxKind.ForEachBlock,
                    SyntaxKind.WhileBlock) Then

                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Continue", VBFeaturesResources.ContinueKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function
    End Class
End Namespace
