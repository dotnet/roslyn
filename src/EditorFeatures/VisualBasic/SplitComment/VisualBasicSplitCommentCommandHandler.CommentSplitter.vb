' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.FormattingOptions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    Partial Friend Class VisualBasicSplitCommentCommandHandler
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

            Public Shared Function TryCreate(document As Document, position As Integer,
                                      root As SyntaxNode, sourceText As SourceText,
                                      useTabs As Boolean, tabSize As Integer,
                                      indentStyle As IndentStyle, CancellationToken As CancellationToken) As CommentSplitter
                Dim trivia = root.FindTrivia(position)
                If trivia.IsKind(SyntaxKind.CommentTrivia) Then
                    Return New CommentSplitter(document, position, sourceText,
                                               root, tabSize, useTabs, trivia,
                                               indentStyle, CancellationToken)
                Else
                    Return Nothing
                End If
            End Function

            Protected Overrides Function CreateSplitComment(indentString As String) As SyntaxTriviaList
                Dim prefix = _sourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, _cursorPosition)).ToString().TrimEnd()
                Dim suffix = _sourceText.GetSubText(TextSpan.FromBounds(_cursorPosition, _trivia.Span.End)).ToString()

                Dim triviaList = New List(Of SyntaxTrivia)
                triviaList.Add(SyntaxFactory.CommentTrivia(prefix))
                triviaList.Add(SyntaxFactory.ElasticCarriageReturnLineFeed)

                If hasLineContinuation Then
                    triviaList.Add(SyntaxFactory.ElasticSpace)
                    triviaList.Add(SyntaxFactory.LineContinuationTrivia("_"))
                End If

                triviaList.Add(SyntaxFactory.CommentTrivia(indentString + CommentCharacter + SyntaxFactory.ElasticSpace.ToString() + suffix))

                Return SyntaxFactory.TriviaList(triviaList)
            End Function

            Protected Overrides Function GetIndentString(newRoot As SyntaxNode) As String
                Dim newDocument = _document.WithSyntaxRoot(newRoot)

                Dim indentationService = newDocument.GetLanguageService(Of Indentation.IIndentationService)()
                Dim originalLine = _sourceText.Lines.GetLineFromPosition(_cursorPosition)

                Dim node = newRoot.FindNode(originalLine.Span, False, True)
                hasLineContinuation = node.DescendantTrivia().Any(Function(x)
                                                                      Return x.Kind() = SyntaxKind.LineContinuationTrivia
                                                                  End Function)
                If hasLineContinuation Then
                    Return " "
                End If

                Dim desiredIndentation = indentationService.GetIndentation(
                    newDocument, originalLine.LineNumber, _indentStyle, _cancellationToken)

                Dim newSourceText = newDocument.GetSyntaxRootSynchronously(_cancellationToken).SyntaxTree.GetText(_cancellationToken)
                Dim baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition)
                Dim baseOffsetInLine = desiredIndentation.BasePosition - baseLine.Start

                Dim indent = baseOffsetInLine + desiredIndentation.Offset
                Dim indentString = indent.CreateIndentationString(_useTabs, _tabSize)

                Return indentString
            End Function
        End Class
    End Class
End Namespace
