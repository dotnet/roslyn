' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.FormattingOptions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    Partial Friend Class SplitCommentCommandHandler
        Private Class CommentSplitter
            Inherits AbstractCommentSplitter

            Friend Const CommentCharacter As Char = "'"c
            Private hasLineContinuation As Boolean = False

            Private Sub New(document As Document, cursorPosition As Integer,
                           sourceText As SourceText, root As SyntaxNode,
                           tabSize As Integer, useTabs As Boolean,
                           trivia As SyntaxTrivia, indentStyle As IndentStyle,
                           cancellationToken As CancellationToken)
                _document = document
                _cursorPosition = cursorPosition
                _sourceText = sourceText
                _root = root
                _tabSize = tabSize
                _useTabs = useTabs
                _cancellationToken = cancellationToken
                _trivia = trivia
                _indentStyle = indentStyle
            End Sub

            Public Shared Function Create(document As Document, position As Integer,
                                      root As SyntaxNode, sourceText As SourceText,
                                      useTabs As Boolean, tabSize As Integer,
                                      indentStyle As IndentStyle,
                                      cancellationToken As CancellationToken) As CommentSplitter
                Dim trivia = root.FindTrivia(position)
                If trivia.IsKind(SyntaxKind.CommentTrivia) Then
                    Return New CommentSplitter(document, position, sourceText,
                                               root, tabSize, useTabs, trivia,
                                               indentStyle, cancellationToken)
                Else
                    Return Nothing
                End If
            End Function

            Protected Overrides Function CreateSplitComment(indentString As String) As SyntaxTriviaList
                Dim prefix = _sourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, _cursorPosition)).ToString()
                Dim suffix = _sourceText.GetSubText(TextSpan.FromBounds(_cursorPosition, _trivia.Span.End)).ToString()

                Dim triviaList = New List(Of SyntaxTrivia)
                triviaList.Add(SyntaxFactory.CommentTrivia(prefix))
                triviaList.Add(SyntaxFactory.ElasticCarriageReturnLineFeed)

                If hasLineContinuation Then
                    triviaList.Add(SyntaxFactory.ElasticSpace)
                    triviaList.Add(SyntaxFactory.LineContinuationTrivia("_"))
                End If

                triviaList.Add(SyntaxFactory.CommentTrivia(indentString + CommentCharacter + suffix))

                Return SyntaxFactory.TriviaList(triviaList)
            End Function
        End Class
    End Class
End Namespace
