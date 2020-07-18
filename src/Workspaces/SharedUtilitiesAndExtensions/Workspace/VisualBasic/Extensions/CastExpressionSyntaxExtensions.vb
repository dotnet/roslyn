' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module CastExpressionSyntaxExtensions
        <Extension>
        Public Function Uncast(cast As CastExpressionSyntax) As ExpressionSyntax
            Return Uncast(cast, cast.OpenParenToken, cast.Expression, cast.CommaToken, cast.Type, cast.CloseParenToken)
        End Function

        <Extension>
        Public Function Uncast(cast As PredefinedCastExpressionSyntax) As ExpressionSyntax
            Return Uncast(cast, cast.OpenParenToken, cast.Expression, Nothing, Nothing, cast.CloseParenToken)
        End Function

        Private Function Uncast(castNode As ExpressionSyntax, openParen As SyntaxToken, innerNode As ExpressionSyntax, commaToken As SyntaxToken, typeNode As TypeSyntax, closeParen As SyntaxToken) As ExpressionSyntax

            Dim leadingTrivia As List(Of SyntaxTrivia) = castNode.GetLeadingTrivia.ToList
            If openParen.LeadingTrivia.ContainsCommentOrLineContinue Then
                leadingTrivia.AddRange(openParen.LeadingTrivia)
            End If

            If openParen.TrailingTrivia.ContainsCommentOrLineContinue Then
                leadingTrivia.AddRange(openParen.TrailingTrivia)
            End If
            If (Not castNode.GetLeadingTrivia.Any) AndAlso leadingTrivia.FirstOrDefault.IsKind(SyntaxKind.WhitespaceTrivia) Then
                leadingTrivia.RemoveAt(0)
            End If

            If innerNode.GetLeadingTrivia.ContainsCommentOrLineContinue Then
                leadingTrivia.AddRange(innerNode.GetLeadingTrivia)
            End If

            Dim trailingTrivia As List(Of SyntaxTrivia) = castNode.GetTrailingTrivia.ToList
            If closeParen.TrailingTrivia.ContainsCommentOrLineContinue Then
                trailingTrivia.InsertRange(0, closeParen.TrailingTrivia)
            End If

            If closeParen.LeadingTrivia.ContainsCommentOrLineContinue Then
                trailingTrivia.InsertRange(0, openParen.LeadingTrivia)
            End If

            If typeNode IsNot Nothing Then
                If typeNode.GetTrailingTrivia.ContainsCommentOrLineContinue Then
                    trailingTrivia.InsertRange(0, typeNode.GetTrailingTrivia)
                End If

                If typeNode.GetLeadingTrivia.ContainsCommentOrLineContinue Then
                    trailingTrivia.InsertRange(0, typeNode.GetLeadingTrivia)
                End If
            End If

            If commaToken.IsKind(SyntaxKind.CommaToken) Then
                If commaToken.TrailingTrivia.ContainsCommentOrLineContinue Then
                    trailingTrivia.InsertRange(0, commaToken.TrailingTrivia)
                End If

                If commaToken.LeadingTrivia.ContainsCommentOrLineContinue Then
                    trailingTrivia.InsertRange(0, commaToken.LeadingTrivia)
                End If
            End If

            If innerNode.GetTrailingTrivia.ContainsCommentOrLineContinue Then
                trailingTrivia.InsertRange(0, innerNode.GetTrailingTrivia)
            End If

            If trailingTrivia.Count > 0 Then
                Dim newTrailingTrivia As New List(Of SyntaxTrivia)
                Dim foundEOL As Boolean = False
                For i As Integer = 0 To trailingTrivia.Count - 1
                    Dim trivia As SyntaxTrivia = trailingTrivia(i)
                    Dim nextTrivia As SyntaxTrivia = GetNextTrivia(trailingTrivia, i, 1)
                    Select Case trivia.Kind
                        Case SyntaxKind.WhitespaceTrivia
                            Select Case nextTrivia.Kind
                                Case SyntaxKind.WhitespaceTrivia
                                    trailingTrivia(i + 1) = AdjustWhitespace(trivia, nextTrivia, GetNextTrivia(trailingTrivia, i, 2).IsKind(SyntaxKind.LineContinuationTrivia))
                                Case SyntaxKind.EndOfLineTrivia
                                    ' skip Whitespace before EOL
                                Case SyntaxKind.LineContinuationTrivia
                                    If GetNextTrivia(trailingTrivia, i, 2).IsKind(SyntaxKind.WhitespaceTrivia) Then
                                        trailingTrivia(i + 2) = trivia
                                        trivia = SyntaxFactory.Space
                                    End If
                                    newTrailingTrivia.Add(trivia)
                                Case SyntaxKind.CommentTrivia
                                    newTrailingTrivia.Add(trivia)
                                Case Else
                                    newTrailingTrivia.Add(trivia)
                            End Select
                        Case SyntaxKind.EndOfLineTrivia
                            If Not foundEOL Then
                                newTrailingTrivia.Add(trivia)
                                foundEOL = True
                            End If
                        Case SyntaxKind.LineContinuationTrivia
                            newTrailingTrivia.Add(trivia)
                        Case SyntaxKind.CommentTrivia
                            newTrailingTrivia.Add(trivia)
                            foundEOL = False
                        Case Else
                            foundEOL = False
                            newTrailingTrivia.Add(trivia)
                    End Select
                Next
                trailingTrivia = newTrailingTrivia
            End If
            Dim resultNode = innerNode.With(leadingTrivia, trailingTrivia)

            resultNode = SimplificationHelpers.CopyAnnotations(castNode, resultNode)

            Return resultNode

        End Function

        Private Function GetNextTrivia(trailingTrivia As List(Of SyntaxTrivia), index As Integer, lookaheadCount As Integer) As SyntaxTrivia
            Return If(index < trailingTrivia.Count - lookaheadCount, trailingTrivia(index + lookaheadCount), Nothing)
        End Function

        Private Function AdjustWhitespace(trivia As SyntaxTrivia, nextTrivia As SyntaxTrivia, afterLineContinue As Boolean) As SyntaxTrivia
            If trivia.Span.Length = nextTrivia.Span.Length Then
                Return trivia
            End If
            Dim lineContinueOffset As Integer = If(afterLineContinue, 2, 0)
            If trivia.Span.Length > nextTrivia.Span.Length Then
                Return SyntaxFactory.Whitespace(New String(" "c, Math.Max(trivia.FullWidth - lineContinueOffset, 1)))
            End If
            Return SyntaxFactory.Whitespace(New String(" "c, Math.Max(nextTrivia.FullWidth - lineContinueOffset, 1)))
        End Function
    End Module
End Namespace
