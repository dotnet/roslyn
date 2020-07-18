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
            Return Uncast(cast, cast.OpenParenToken, cast.Expression, commaToken:=Nothing, typeNode:=Nothing, cast.CloseParenToken)
        End Function

        Private Function Uncast(castNode As ExpressionSyntax, openParen As SyntaxToken, innerNode As ExpressionSyntax, commaToken As SyntaxToken, typeNode As TypeSyntax, closeParen As SyntaxToken) As ExpressionSyntax

            Dim leadingTrivia As List(Of SyntaxTrivia) = castNode.GetLeadingTrivia.ToList
            leadingTrivia.AddRange(AddTriviaIfContainsAnyCommentOrLineContinuation(openParen.LeadingTrivia))
            leadingTrivia.AddRange(AddTriviaIfContainsAnyCommentOrLineContinuation(openParen.TrailingTrivia))
            If leadingTrivia.FirstOrDefault.IsKind(SyntaxKind.WhitespaceTrivia) AndAlso Not castNode.GetLeadingTrivia.Any Then
                leadingTrivia.RemoveAt(0)
            End If
            leadingTrivia.AddRange(innerNode.GetLeadingTrivia)

            Dim trailingTrivia As List(Of SyntaxTrivia) = castNode.GetTrailingTrivia.ToList
            trailingTrivia.InsertRange(0, closeParen.TrailingTrivia)
            trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(closeParen.TrailingTrivia))
            trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(openParen.LeadingTrivia))

            ' Nothing for Predefined Cast
            If typeNode IsNot Nothing Then
                trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(typeNode.GetTrailingTrivia))
                trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(typeNode.GetLeadingTrivia))
            End If

            ' Kind None for Predefined Cast
            If commaToken.IsKind(SyntaxKind.CommaToken) Then
                trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(commaToken.TrailingTrivia))
                trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(commaToken.LeadingTrivia))
            End If

            trailingTrivia.InsertRange(0, AddTriviaIfContainsAnyCommentOrLineContinuation(innerNode.GetTrailingTrivia))

            If trailingTrivia.Count > 0 Then
                Dim newTrailingTrivia As New List(Of SyntaxTrivia)
                Dim foundEOL As Boolean = False
                For i As Integer = 0 To trailingTrivia.Count - 1
                    Dim trivia As SyntaxTrivia = trailingTrivia(i)
                    Dim nextTrivia As SyntaxTrivia = GetForwardTriviaOrDefault(trailingTrivia, i)
                    Select Case trivia.Kind
                        Case SyntaxKind.WhitespaceTrivia
                            Select Case nextTrivia.Kind
                                Case SyntaxKind.WhitespaceTrivia
                                    trailingTrivia(i + 1) = AdjustWhitespace(trivia, nextTrivia, GetForwardTriviaOrDefault(trailingTrivia, i, lookaheadCount:=2).IsKind(SyntaxKind.LineContinuationTrivia))
                                Case SyntaxKind.EndOfLineTrivia
                                    ' skip Whitespace before EOL
                                Case SyntaxKind.LineContinuationTrivia
                                    If GetForwardTriviaOrDefault(trailingTrivia, i, lookaheadCount:=2).IsKind(SyntaxKind.WhitespaceTrivia) Then
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

        Private Function AddTriviaIfContainsAnyCommentOrLineContinuation(leadingTrivia As SyntaxTriviaList) As IEnumerable(Of SyntaxTrivia)
            If leadingTrivia.ContainsAnyCommentOrLineContinuation Then
                Return leadingTrivia
            End If
            Return New List(Of SyntaxTrivia)
        End Function

        Private Function GetForwardTriviaOrDefault(trailingTrivia As List(Of SyntaxTrivia), index As Integer, Optional lookaheadCount As Integer = 1) As SyntaxTrivia
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
