' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Structure CodeShapeAnalyzer

            Private ReadOnly _context As FormattingContext
            Private ReadOnly _options As SyntaxFormattingOptions
            Private ReadOnly _list As TriviaList

            Private _indentation As Integer
            Private _trailingSpaces As Integer
            Private _currentColumn As Integer
            Private _lastLineBreakIndex As Integer
            Private _touchedNoisyCharacterOnCurrentLine As Boolean

            Public Shared Function ShouldFormatMultiLine(context As FormattingContext,
                                                beginningOfNewLine As Boolean,
                                                list As TriviaList) As Boolean
                Dim analyzer = New CodeShapeAnalyzer(context, beginningOfNewLine, list)
                Return analyzer.ShouldFormat()
            End Function

            Public Shared Function ShouldFormatSingleLine(triviaList As TriviaList) As Boolean

                Dim index = -1
                For Each trivia In triviaList

                    index = index + 1

                    Contract.ThrowIfTrue(trivia.Kind = SyntaxKind.EndOfLineTrivia)
                    Contract.ThrowIfTrue(trivia.Kind = SyntaxKind.SkippedTokensTrivia)

                    ' if it contains elastic trivia. always format
                    If trivia.IsElastic() Then
                        Return True
                    End If

                    If trivia.Kind = SyntaxKind.WhitespaceTrivia Then
                        Debug.Assert(trivia.ToString() = trivia.ToFullString())
                        Dim text = trivia.ToString()
                        If text.IndexOf(vbTab, StringComparison.Ordinal) >= 0 Then
                            Return True
                        End If
                    End If

                    If trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                        Return False
                    End If

                    If trivia.Kind = SyntaxKind.RegionDirectiveTrivia OrElse trivia.Kind = SyntaxKind.EndRegionDirectiveTrivia OrElse SyntaxFacts.IsPreprocessorDirective(trivia.Kind) Then
                        Return False
                    End If

                    If trivia.Kind = SyntaxKind.ColonTrivia Then
                        Return True
                    End If
                Next

                Return True
            End Function

            Public Shared Function ContainsSkippedTokensOrText(list As TriviaList) As Boolean
                For Each trivia In list
                    If trivia.RawKind = SyntaxKind.SkippedTokensTrivia Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Private Sub New(context As FormattingContext,
                            beginningOfNewLine As Boolean,
                            list As TriviaList)
                Me._context = context
                Me._options = context.Options
                Me._list = list

                Me._indentation = 0
                Me._trailingSpaces = 0
                Me._currentColumn = 0
                Me._lastLineBreakIndex = If(beginningOfNewLine, 0, -1)
                Me._touchedNoisyCharacterOnCurrentLine = False
            End Sub

            Private ReadOnly Property UseIndentation As Boolean
                Get
                    Return Me._lastLineBreakIndex >= 0 AndAlso Not Me._touchedNoisyCharacterOnCurrentLine
                End Get
            End Property

            Private Shared Function OnElastic(trivia As SyntaxTrivia) As Boolean
                ' if it contains elastic trivia. always format
                Return trivia.IsElastic()
            End Function

            Private Function OnWhitespace(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                    Return False
                End If

                ' right after end of line trivia. calculate indentation for current line
                Debug.Assert(trivia.ToString() = trivia.ToFullString())
                Dim text = trivia.ToString()

                ' if text contains tab, we will give up perf optimization and use more expensive one to see whether we need to format this trivia
                If text.IndexOf(vbTab, StringComparison.Ordinal) >= 0 Then
                    Return True
                End If

                Dim currentSpaces = text.ConvertTabToSpace(_options.TabSize, Me._currentColumn, text.Length)

                If currentIndex + 1 < Me._list.Count AndAlso Me._list(currentIndex + 1).RawKind = SyntaxKind.LineContinuationTrivia Then
                    If currentSpaces <> 1 Then
                        Return True
                    End If
                End If

                ' keep track of current column on this line
                Me._currentColumn += currentSpaces

                ' keep track of trailing space after noisy token
                Me._trailingSpaces += currentSpaces

                ' keep track of indentation after new line
                If Not Me._touchedNoisyCharacterOnCurrentLine Then
                    Me._indentation += currentSpaces
                End If

                Return False
            End Function

            Private Sub ResetStateAfterNewLine(currentIndex As Integer)
                ' reset states for current line
                Me._currentColumn = 0
                Me._trailingSpaces = 0
                Me._indentation = 0
                Me._touchedNoisyCharacterOnCurrentLine = False

                ' remember last line break index
                Me._lastLineBreakIndex = currentIndex
            End Sub

            Private Function OnEndOfLine(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.EndOfLineTrivia Then
                    Return False
                End If

                ' end of line trivia right after whitespace trivia
                If Me._trailingSpaces > 0 Then
                    ' has trailing whitespace
                    Return True
                End If

                If Me._indentation > 0 AndAlso Not Me._touchedNoisyCharacterOnCurrentLine Then
                    ' we have empty line with spaces. remove spaces
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Sub MarkTouchedNoisyCharacter()
                Me._touchedNoisyCharacterOnCurrentLine = True
                Me._trailingSpaces = 0
            End Sub

            Private Function OnLineContinuation(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.LineContinuationTrivia Then
                    Return False
                End If

                If Me.UseIndentation AndAlso Me._indentation <> 1 Then
                    Return True
                End If

                If trivia.ToFullString().Length <> 3 Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Shared Function OnColon(trivia As SyntaxTrivia) As Boolean
                If trivia.Kind <> SyntaxKind.ColonTrivia Then
                    Return False
                End If

                ' colon is rare situation. always format in the present of colon trivia.
                Return True
            End Function

            Private Function OnComment(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.CommentTrivia AndAlso
                   trivia.Kind <> SyntaxKind.DocumentationCommentTrivia Then
                    Return False
                End If

                ' if comment is right after a token
                If currentIndex = 0 Then
                    Return True
                End If

                ' check whether indentation are right
                If Me.UseIndentation AndAlso Me._indentation <> Me._context.GetBaseIndentation(trivia.SpanStart) Then
                    ' comment has wrong indentation
                    Return True
                End If

                If trivia.Kind = SyntaxKind.DocumentationCommentTrivia AndAlso
                   ShouldFormatDocumentationComment(_indentation, _options.TabSize, trivia) Then
                    Return True
                End If

                MarkTouchedNoisyCharacter()
                Return False
            End Function

            Private Shared Function OnSkippedTokensOrText(trivia As SyntaxTrivia) As Boolean
                If trivia.Kind <> SyntaxKind.SkippedTokensTrivia Then
                    Return False
                End If

                throw ExceptionUtilities.UnexpectedValue(trivia.Kind)
            End Function

            Private Function OnRegion(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.RegionDirectiveTrivia AndAlso
                   trivia.Kind <> SyntaxKind.EndRegionDirectiveTrivia Then
                    Return False
                End If

                If Not Me.UseIndentation Then
                    Return True
                End If

                If Me._indentation <> Me._context.GetBaseIndentation(trivia.SpanStart) Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Shared Function OnPreprocessor(trivia As SyntaxTrivia) As Boolean
                If Not SyntaxFacts.IsPreprocessorDirective(trivia.Kind) Then
                    Return False
                End If

                Return True
            End Function

            Private Function ShouldFormat() As Boolean
                Dim index = -1
                For Each trivia In Me._list
                    index = index + 1

                    If OnElastic(trivia) OrElse
                       OnWhitespace(trivia, index) OrElse
                       OnEndOfLine(trivia, index) OrElse
                       OnLineContinuation(trivia, index) OrElse
                       OnColon(trivia) OrElse
                       OnComment(trivia, index) OrElse
                       OnSkippedTokensOrText(trivia) OrElse
                       OnRegion(trivia, index) OrElse
                       OnPreprocessor(trivia) Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Private Shared Function ShouldFormatDocumentationComment(indentation As Integer, tabSize As Integer, trivia As SyntaxTrivia) As Boolean
                Dim xmlComment = CType(trivia.GetStructure(), DocumentationCommentTriviaSyntax)

                Dim sawFirstOne = False
                For Each token In xmlComment.DescendantTokens()
                    For Each xmlTrivia In token.LeadingTrivia
                        If xmlTrivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia Then
                            ' skip first one since its leading whitespace will belong to syntax tree's syntax token
                            ' not xml doc comment's token
                            If Not sawFirstOne Then
                                sawFirstOne = True
                                Exit For
                            End If

                            Dim xmlCommentText = xmlTrivia.ToString()

                            ' "'''" == 3.
                            If xmlCommentText.GetColumnFromLineOffset(xmlCommentText.Length - 3, tabSize) <> indentation Then
                                Return True
                            End If

                            Exit For
                        End If
                    Next xmlTrivia
                Next token

                Return False
            End Function
        End Structure
    End Class
End Namespace
