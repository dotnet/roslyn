' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
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

        Private Shared ReadOnly s_keywords As ImmutableArray(Of RecommendedKeyword) =
            ImmutableArray.Create(New RecommendedKeyword("Finally", VBFeaturesResources.Introduces_a_statement_block_to_be_run_before_exiting_a_Try_structure))

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As ImmutableArray(Of RecommendedKeyword)
            If Not context.IsMultiLineStatementContext Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            Dim targetToken = context.TargetToken
            Dim tryBlock = targetToken.GetAncestor(Of TryBlockSyntax)()

            If tryBlock Is Nothing OrElse tryBlock.FinallyBlock IsNot Nothing Then
                Return ImmutableArray(Of RecommendedKeyword).Empty
            End If

            ' If we're in the Try block, then we simply need to make sure we have no catch blocks, or else a Finally
            ' won't be valid here
            If context.IsInStatementBlockOfKind(SyntaxKind.TryBlock) AndAlso
               Not IsInCatchOfTry(targetToken, tryBlock) Then

                If tryBlock.CatchBlocks.Count = 0 Then
                    Return s_keywords
                End If
            ElseIf IsInCatchOfTry(targetToken, tryBlock) Then
                If TextSpan.FromBounds(tryBlock.CatchBlocks.Last().SpanStart, tryBlock.EndTryStatement.SpanStart).Contains(context.Position) Then
                    Return s_keywords
                End If
            End If

            Return ImmutableArray(Of RecommendedKeyword).Empty
        End Function

        Private Shared Function IsInCatchOfTry(targetToken As SyntaxToken, tryBlock As TryBlockSyntax) As Boolean
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
