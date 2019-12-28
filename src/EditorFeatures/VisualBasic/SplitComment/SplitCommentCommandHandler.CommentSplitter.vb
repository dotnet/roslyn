' Copyright(c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt In the project root For license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.FormattingOptions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment


    Partial Friend Class SplitCommentCommandHandler

        Private Class CommentSplitter
            Protected Shared ReadOnly RightNodeAnnotation As SyntaxAnnotation = New SyntaxAnnotation()

            Protected ReadOnly Document As Document
            Protected ReadOnly CursorPosition As Integer
            Protected ReadOnly SourceText As SourceText
            Protected ReadOnly Root As SyntaxNode
            Protected ReadOnly TabSize As Integer
            Protected ReadOnly UseTabs As Boolean
            Protected ReadOnly CancellationToken As CancellationToken

            Private Const CommentCharacter As Char = "'"c
            Private ReadOnly _trivia As SyntaxTrivia

            Private ReadOnly _indentStyle As IndentStyle

            Public Sub New(document As Document, cursorPosition As Integer,
                           sourceText As SourceText, root As SyntaxNode,
                           tabSize As Integer, useTabs As Boolean,
                           trivia As SyntaxTrivia, indentStyle As IndentStyle,
                           cancellationToken As CancellationToken)
                Me.Document = document
                Me.CursorPosition = cursorPosition
                Me.SourceText = sourceText
                Me.Root = root
                Me.TabSize = tabSize
                Me.UseTabs = useTabs
                Me.CancellationToken = cancellationToken
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

            Protected Function GetNodeToReplace() As SyntaxNode
                If _trivia.Token.Parent.IsKind(SyntaxKind.EndStatement) Then
                    ' Return the Parent of the Parent to get the containing statement instead of the end statement
                    Return _trivia.Token.Parent.Parent
                Else
                    ' We are not at an end statement so just get the parent
                    Return _trivia.Token.Parent
                End If

            End Function

            Protected Function CreateSplitComment(indentString As String) As SyntaxTriviaList
                Dim prefix = SourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, CursorPosition)).ToString()
                Dim suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _trivia.Span.End)).ToString()

                Dim firstTrivia = SyntaxFactory.CommentTrivia(prefix)
                Dim secondTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed
                Dim thirdTrivia = SyntaxFactory.CommentTrivia(indentString + CommentCharacter + suffix)

                Return SyntaxFactory.TriviaList(firstTrivia, secondTrivia, thirdTrivia)
            End Function

            Public Function TrySplit() As Integer?
                Dim nodeToReplace = GetNodeToReplace()

                If CursorPosition <= nodeToReplace.SpanStart Or CursorPosition >= nodeToReplace.Span.End Then
                    Return Nothing
                End If

                Return SplitWorker()
            End Function

            Private Function SplitWorker() As Integer
                Dim documentAndPosition = SplitComment()
                Dim workspace = Document.Project.Solution.Workspace

                workspace.TryApplyChanges(documentAndPosition.document.Project.Solution)

                Return documentAndPosition.caretPostion
            End Function

            Private Function SplitComment() As (document As Document, caretPostion As Integer)
                Dim indentString = GetIndentString(Root)
                Dim nodeToRemove = GetNodeToReplace()

                Dim comment = CreateSplitComment(indentString)
                Dim commentToReplace = nodeToRemove.FindTrivia(CursorPosition)
                Dim newRoot = Root.ReplaceTrivia(commentToReplace, comment)

                Dim newLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber + 1
                Dim newPosition = SourceText.Lines(newLineNumber).GetLastNonWhitespacePosition()
                Dim newDocument = Document.WithSyntaxRoot(newRoot)

                Return (newDocument, newPosition.GetValueOrDefault())
            End Function

            Private Function GetIndentString(newRoot As SyntaxNode) As String
                Dim newDocument = Document.WithSyntaxRoot(newRoot)

                Dim indentationService = newDocument.GetLanguageService(Of Indentation.IIndentationService)()
                Dim originalLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber

                Dim desiredIndentation = indentationService.GetIndentation(
                    newDocument, originalLineNumber, _indentStyle, CancellationToken)

                Dim newSourceText = newDocument.GetSyntaxRootSynchronously(CancellationToken).SyntaxTree.GetText(CancellationToken)
                Dim baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition)
                Dim baseOffsetInLine = desiredIndentation.BasePosition - baseLine.Start

                Dim indent = baseOffsetInLine + desiredIndentation.Offset
                Dim indentString = indent.CreateIndentationString(UseTabs, TabSize)

                Return indentString
            End Function
        End Class
    End Class
End Namespace
