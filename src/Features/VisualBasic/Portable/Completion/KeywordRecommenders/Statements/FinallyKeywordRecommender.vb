' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Statements
    ''' <summary>
    ''' Recommends the "Finally" keyword for the statement context
    ''' </summary>
    Friend Class FinallyKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If Not context.IsMultiLineStatementContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken
            Dim tryBlock = targetToken.GetAncestor(Of TryBlockSyntax)()

            If tryBlock Is Nothing OrElse tryBlock.FinallyBlock IsNot Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            ' If we're in the Try block, then we simply need to make sure we have no catch blocks, or else a Finally
            ' won't be valid here
            If context.IsInStatementBlockOfKind(SyntaxKind.TryBlock) AndAlso
               Not IsInCatchOfTry(targetToken, tryBlock) Then

                If tryBlock.CatchBlocks.Count = 0 Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Finally", VBFeaturesResources.FinallyKeywordToolTip))
                End If
            ElseIf IsInCatchOfTry(targetToken, tryBlock) Then
                If TextSpan.FromBounds(tryBlock.CatchBlocks.Last().SpanStart, tryBlock.EndTryStatement.SpanStart).Contains(context.Position) Then
                    Return SpecializedCollections.SingletonEnumerable(New RecommendedKeyword("Finally", VBFeaturesResources.FinallyKeywordToolTip))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function IsInCatchOfTry(targetToken As SyntaxToken, tryBlock As TryBlockSyntax) As Boolean
            Dim parent = targetToken.Parent
            While parent IsNot tryBlock
                If parent.IsKind(SyntaxKind.CatchBlock) AndAlso tryBlock.CatchBlocks.Contains(DirectCast(parent, CatchBlockSyntax)) Then
                    Return True
                End If

                parent = parent.Parent
            End While

            Return False
        End Function
    End Class
End Namespace
