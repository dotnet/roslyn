Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Text
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaFormatter
        Private Structure TriviaLineBuilder
            Private ReadOnly context As FormattingContext
            Private ReadOnly triviaOnLine As List(Of SyntaxTrivia)

            Private containsOnlyWhitespace As Boolean
            Private containsSkippedTokensOrText As Boolean

            Public Sub New(context As FormattingContext, buffer As List(Of SyntaxTrivia))
                Contract.ThrowIfNull(context)
                Contract.ThrowIfNull(buffer)

                Me.context = context
                Me.triviaOnLine = buffer
                Me.triviaOnLine.Clear()

                Me.containsOnlyWhitespace = True
                Me.containsSkippedTokensOrText = False
            End Sub

            Public Sub Add(trivia As SyntaxTrivia)
                Contract.ThrowIfTrue(trivia.Kind = SyntaxKind.EndOfLineTrivia)

                If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                    triviaOnLine.Add(trivia)
                    Return
                End If

                Me.containsOnlyWhitespace = False

                If trivia.Kind = SyntaxKind.CommentTrivia OrElse
                   trivia.Kind = SyntaxKind.ColonTrivia OrElse
                   trivia.Kind = SyntaxKind.DisabledTextTrivia OrElse
                   trivia.Kind = SyntaxKind.RegionDirective OrElse
                   trivia.Kind = SyntaxKind.EndRegionDirective OrElse
                   trivia.Kind = SyntaxKind.DocumentationComment OrElse
                   trivia.Kind = SyntaxKind.LineContinuationTrivia OrElse
                   trivia.Kind = SyntaxKind.ImplicitLineContinuationTrivia Then
                    triviaOnLine.Add(trivia)
                ElseIf trivia.Kind = SyntaxKind.SkippedTokens Then
                    Me.containsSkippedTokensOrText = True
                    triviaOnLine.Add(trivia)
                Else
                    Contract.ThrowIfFalse(trivia.Kind.IsPreprocessorDirective())
                    triviaOnLine.Add(trivia)
                End If
            End Sub

            Public Sub Reset()
                Me.triviaOnLine.Clear()

                Me.containsOnlyWhitespace = True
                Me.containsSkippedTokensOrText = False
            End Sub

            Public Sub CommitBetweenTokens(initialColumn As Integer, triviaList As List(Of SyntaxTrivia))
                Contract.ThrowIfTrue(Me.containsSkippedTokensOrText)
                Contract.ThrowIfTrue(Me.containsOnlyWhitespace)

                Dim currentColumn = initialColumn

                ' okay, current line should contain more than just whitespaces trivia
                For i As Integer = 0 To Me.triviaOnLine.Count - 1
                    If TryProcessOneWhitespaceTrivia(i, currentColumn, triviaList) Then
                        Continue For
                    End If

                    ' the trivia is not a whitespace trivia, just add it to collection and add up full width
                    Dim trivia = Me.triviaOnLine(i)

                    ' if comment trivia sit right after a token, add a whitespace
                    If trivia.Kind = SyntaxKind.CommentTrivia AndAlso i = 0 Then
                        triviaList.Add(Syntax.Space)
                        currentColumn += Syntax.Space.FullWidth
                    End If

                    triviaList.Add(trivia)
                    currentColumn += trivia.FullWidth
                Next i

                Return
            End Sub

            Public Function CommitLines(beginningOfNewLine As Boolean, triviaList As List(Of SyntaxTrivia)) As Integer
                Contract.ThrowIfTrue(Me.containsSkippedTokensOrText)

                If Me.containsOnlyWhitespace Then
                    Return 0
                End If

                Dim startIndex = If(beginningOfNewLine, GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex:=0), 0)
                Contract.ThrowIfFalse(startIndex >= 0)

                Dim indentation = Me.context.GetBaseIndentation(triviaOnLine(startIndex).Span.Start)
                Return ProcessQueuedTrivia(beginningOfNewLine, startIndex, indentation, triviaList)
            End Function

            Public Function CommitLeftOver(beginningOfNewLine As Boolean, triviaList As List(Of SyntaxTrivia), ByRef additionalLines As Integer) As Boolean
                Contract.ThrowIfTrue(Me.containsSkippedTokensOrText)

                If Me.containsOnlyWhitespace Then
                    Return False
                End If

                If Me.triviaOnLine.Count = 0 Then
                    Return False
                End If

                additionalLines += CommitLines(beginningOfNewLine, triviaList)
                Return True
            End Function

            Private Function ProcessQueuedTrivia(beginningOfNewLine As Boolean,
                                                 startIndex As Integer,
                                                 indentation As Integer,
                                                 triviaList As List(Of SyntaxTrivia)) As Integer
                Dim lineBreaks = 0
                Dim currentColumn = indentation
                Dim indentationDelta = If(beginningOfNewLine, GetIndentationDelta(indentation), 0)
                Dim touchedNoisyTrivia = Not beginningOfNewLine

                ' okay, current line should contain more than just whitespace trivia
                For i As Integer = startIndex To triviaOnLine.Count - 1
                    Dim trivia = triviaOnLine(i)

                    ' well easy case first.
                    If TrySimpleSingleLineOrDisabledTextCase(indentation, trivia, touchedNoisyTrivia, triviaList, lineBreaks) Then
                        Return lineBreaks
                    End If

                    ' now complex multiline stuff
                    ' indentation only matters to the very first one on current line
                    If trivia.Kind = SyntaxKind.DocumentationComment Then
                        lineBreaks += AppendDocumentationComment(touchedNoisyTrivia, indentation, trivia, triviaList)
                        Return lineBreaks
                    End If

                    If TryProcessOneWhitespaceTrivia(i, currentColumn, triviaList) Then
                        Continue For
                    End If

                    If trivia.Kind = SyntaxKind.ColonTrivia Then
                        AppendIndentationStringIfPossible(touchedNoisyTrivia, indentation, triviaList)

                        triviaList.Add(trivia)
                        touchedNoisyTrivia = True
                    End If

                    currentColumn += trivia.FullWidth
                Next i

                Return lineBreaks
            End Function

            Private Function TryProcessOneWhitespaceTrivia(currentIndex As Integer, ByRef currentColumn As Integer, triviaList As List(Of SyntaxTrivia)) As Boolean
                Dim trivia = Me.triviaOnLine(currentIndex)

                ' if there is whitespace trivia between two trivia, make sure we calculate right spaces between them.
                If trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                    Return False
                End If

                ' if next token is line continuation, we alwasy put " " rather than indentation.
                If currentIndex + 1 < Me.triviaOnLine.Count AndAlso Me.triviaOnLine(currentIndex + 1).Kind = SyntaxKind.LineContinuationTrivia Then
                    triviaList.Add(Syntax.Space)
                    currentColumn += 1
                    Return True
                End If

                ' whitespace between noisy characters. convert tab to space if there is any. 
                ' tab can only appear in indentation
                Dim text = trivia.GetText()
                Dim spaces = text.ConvertStringTextPositionToColumn(Me.context.Options.TabSize, currentColumn, text.Length)

                AppendWhitespaceTrivia(GetSpaces(spaces), triviaList)

                ' add right number of spaces
                currentColumn += spaces
                Return True
            End Function

            Private Function TrySimpleSingleLineOrDisabledTextCase(indentation As Integer,
                                                                   trivia As SyntaxTrivia,
                                                                   touchedNonNoisyTrivia As Boolean,
                                                                   list As List(Of SyntaxTrivia),
                                                                   ByRef lineBreaks As Integer) As Boolean
                ' well easy case first.
                If trivia.Kind = SyntaxKind.CommentTrivia Then
                    AppendIndentationStringIfPossible(touchedNonNoisyTrivia, indentation, list)
                    list.Add(trivia)
                    Return True
                ElseIf trivia.Kind = SyntaxKind.RegionDirective OrElse trivia.Kind = SyntaxKind.EndRegionDirective Then
                    AppendIndentationStringIfPossible(touchedNonNoisyTrivia, indentation, list)

                    lineBreaks += 1
                    list.Add(trivia)
                    Return True
                ElseIf trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                    If Not touchedNonNoisyTrivia Then
                        ' always put a space before "_"
                        list.Add(Syntax.Space)
                    End If
                    lineBreaks += 1
                    list.Add(GetOrCreateLineContinuationTrivia(trivia))
                    Return True
                ElseIf trivia.Kind.IsPreprocessorDirective() Then
                    ' for now, put it at the column 0
                    lineBreaks += 1
                    list.Add(trivia)
                    Return True
                ElseIf trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                    lineBreaks += GetLineBreaks(trivia.GetFullText())
                    list.Add(trivia)
                    Return True
                End If

                Return False
            End Function

            Private Function AppendDocumentationComment(touchedNonNoisyTrivia As Boolean,
                                                        indentation As Integer,
                                                        trivia As SyntaxTrivia,
                                                        list As List(Of SyntaxTrivia)) As Integer
                AppendIndentationStringIfPossible(touchedNonNoisyTrivia, indentation, list)

                AppendReindentedText(indentation, trivia, list)

                Return GetLineBreaks(trivia.GetFullText())
            End Function

            Private Sub AppendReindentedText(indentation As Integer, trivia As SyntaxTrivia, list As List(Of SyntaxTrivia))
                Dim xmlDocComment = trivia.GetFullText().ReindentStartOfXmlDocumentationComment(
                        forceIndentation:=True,
                        indentation:=indentation,
                        indentationDelta:=0,
                        useTab:=Me.context.Options.UseTab,
                        tabSize:=Me.context.Options.TabSize)

                ' currently it is not supported in VB
                Dim parsedTrivia = Syntax.ParseLeadingTrivia(xmlDocComment)
                Contract.ThrowIfFalse(parsedTrivia.Count = 1)

                list.Add(parsedTrivia(0))
            End Sub

            Private Function GetFirstNonWhitespaceIndexInString(text As String) As Integer
                For i As Integer = 0 To text.Length - 1
                    If text.Chars(i) <> " "c AndAlso text.Chars(i) <> vbTab Then
                        Return i
                    End If
                Next i

                Return -1
            End Function

            Private Function GetLineBreaks(triviaText As String) As Integer
                Dim lineBreaks As Integer = 0
                For i As Integer = 0 To triviaText.Length - 1
                    If triviaText.Chars(i) = vbLf Then
                        lineBreaks += 1
                    End If
                Next i

                Return lineBreaks
            End Function

            Private Function GetExistingIndentation() As Integer
                Dim spaces = 0

                For i As Integer = 0 To triviaOnLine.Count - 1
                    Dim trivia = triviaOnLine(i)

                    If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                        Dim text = trivia.GetText()
                        spaces += text.ConvertStringTextPositionToColumn(Me.context.Options.TabSize, spaces, text.Length)

                        Continue For
                    End If

                    Return spaces
                Next i

                Return 0
            End Function

            Private Function GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex As Integer) As Integer
                For i As Integer = startIndex To triviaOnLine.Count - 1
                    Dim trivia = triviaOnLine(i)

                    ' eat up all leading whitespaces (indentation)
                    If trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                        Return i
                    End If
                Next i

                Return -1
            End Function

            Private Function HasOnlyTrailingWhitespace(startIndex As Integer) As Boolean
                Return GetFirstNonWhitespaceTriviaIndexInBuffer(startIndex) < 0
            End Function

            Private Sub AppendIndentationStringIfPossible(touchedNonNoisyTrivia As Boolean,
                                                          baseIndentation As Integer,
                                                          triviaList As List(Of SyntaxTrivia))
                If touchedNonNoisyTrivia Then
                    Return
                End If

                Dim indentatationString = baseIndentation.CreateIndentationString(Me.context.Options.UseTab, Me.context.Options.TabSize)
                AppendWhitespaceTrivia(indentatationString, triviaList)
            End Sub

            Private Function GetIndentationDelta(baseIndentation As Integer) As Integer
                Return baseIndentation - GetExistingIndentation()
            End Function

            Private Function GetOrCreateLineContinuationTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                If trivia.GetFullText() = LineContinuationCache.GetFullText() Then
                    Return trivia
                End If

                Return LineContinuationCache
            End Function
        End Structure
    End Class
End Namespace