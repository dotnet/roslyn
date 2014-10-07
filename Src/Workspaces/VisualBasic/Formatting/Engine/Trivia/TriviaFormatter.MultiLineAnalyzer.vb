Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Formatting

Namespace Roslyn.Services.VisualBasic.Formatting
    Partial Friend Class TriviaFormatter
        Private Structure MultiLineAnalyzer

            Private ReadOnly options As FormattingOptions
            Private ReadOnly desiredIndentation As Integer
            Private ReadOnly list As TriviaList

            Private indentation As Integer
            Private trailingSpaces As Integer
            Private currentColumn As Integer
            Private lastLineBreakIndex As Integer
            Private touchedNoisyCharacterOnCurrentLine As Boolean

            Public Shared Function ShouldFormat(options As FormattingOptions,
                                                beginningOfNewLine As Boolean,
                                                desiredIndentation As Integer,
                                                list As TriviaList) As Boolean
                Dim analyzer = New MultiLineAnalyzer(options, beginningOfNewLine, desiredIndentation, list)
                Return analyzer.ShouldFormat()
            End Function

            Private Sub New(options As FormattingOptions,
                            beginningOfNewLine As Boolean,
                            desiredIndentation As Integer,
                            list As TriviaList)
                Me.options = options
                Me.desiredIndentation = desiredIndentation
                Me.list = list

                Me.indentation = 0
                Me.trailingSpaces = 0
                Me.currentColumn = 0
                Me.lastLineBreakIndex = If(beginningOfNewLine, 0, -1)
                Me.touchedNoisyCharacterOnCurrentLine = False
            End Sub

            Private ReadOnly Property UseIndentation As Boolean
                Get
                    Return Me.lastLineBreakIndex >= 0 AndAlso Not Me.touchedNoisyCharacterOnCurrentLine
                End Get
            End Property

            Private Function OnElastic(trivia As SyntaxTrivia) As Boolean
                ' if it contains elastic trivia. always format
                Return trivia.IsElastic
            End Function

            Private Function OnWhitespace(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.WhitespaceTrivia Then
                    Return False
                End If

                ' right after end of line trivia. calculate indentation for current line
                Debug.Assert(trivia.GetText() = trivia.GetFullText())
                Dim text = trivia.GetText()

                ' if text contains tab, we will give up perf optimization and use more expensive one to see whether we need to format this triva
                If text.IndexOf(vbTab) >= 0 Then
                    Return True
                End If

                Dim currentSpaces = text.ConvertStringTextPositionToColumn(options.TabSize, Me.currentColumn, text.Length)

                If currentIndex + 1 < Me.list.Count AndAlso Me.list(currentIndex + 1).Kind = SyntaxKind.LineContinuationTrivia Then
                    If currentSpaces <> 1 Then
                        Return True
                    End If
                End If

                ' keep track of current column on this line
                Me.currentColumn += currentSpaces

                ' keep track of trailing space after noisy token
                Me.trailingSpaces += currentSpaces

                ' keep track of indentation after new line
                If Not Me.touchedNoisyCharacterOnCurrentLine Then
                    Me.indentation += currentSpaces
                End If

                Return False
            End Function

            Private Sub ResetStateAfterNewLine(currentIndex As Integer)
                ' reset states for current line
                Me.currentColumn = 0
                Me.trailingSpaces = 0
                Me.indentation = 0
                Me.touchedNoisyCharacterOnCurrentLine = False

                ' remember last line break index
                Me.lastLineBreakIndex = currentIndex
            End Sub

            Private Function OnEndOfLine(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.EndOfLineTrivia Then
                    Return False
                End If

                ' end of line trivia right after whitespace trivia
                If Me.trailingSpaces > 0 Then
                    ' has trailing whitespace
                    Return True
                End If

                If Me.indentation > 0 AndAlso Not Me.touchedNoisyCharacterOnCurrentLine Then
                    ' we have empty line with spaces. remove spaces
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Sub MarkTouchedNoisyCharacter()
                Me.touchedNoisyCharacterOnCurrentLine = True
                Me.trailingSpaces = 0
            End Sub

            Private Function OnLineContinuation(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.LineContinuationTrivia Then
                    Return False
                End If

                If Me.UseIndentation AndAlso Me.indentation <> 1 Then
                    Return True
                End If

                If trivia.GetFullText().Length <> 3 Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Function OnColon(trivia As SyntaxTrivia) As Boolean
                If trivia.Kind <> SyntaxKind.ColonTrivia Then
                    Return False
                End If

                ' check whether indentation are right
                If Me.UseIndentation AndAlso Me.indentation <> desiredIndentation Then
                    ' comment has wrong indentation
                    Return True
                End If

                Me.currentColumn += trivia.FullWidth

                MarkTouchedNoisyCharacter()
                Return False
            End Function

            Private Function OnComment(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.CommentTrivia AndAlso
                   trivia.Kind <> SyntaxKind.DocumentationComment Then
                    Return False
                End If

                ' check whether indentation are right
                If Me.UseIndentation AndAlso Me.indentation <> desiredIndentation Then
                    ' comment has wrong indentation
                    Return True
                End If

                If trivia.Kind = SyntaxKind.DocumentationComment AndAlso
                   ShouldFormatDocumentationComment(indentation, options.TabSize, trivia) Then
                    Return True
                End If

                MarkTouchedNoisyCharacter()
                Return False
            End Function

            Private Function OnSkippedTokensOrText(trivia As SyntaxTrivia) As Boolean
                If trivia.Kind <> SyntaxKind.SkippedTokens Then
                    Return False
                End If

                Return Contract.FailWithReturn(Of Boolean)("This can't happen")
            End Function

            Private Function OnRegion(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.Kind <> SyntaxKind.RegionDirective AndAlso
                   trivia.Kind <> SyntaxKind.EndRegionDirective Then
                    Return False
                End If

                If Not Me.UseIndentation Then
                    Return True
                End If

                If Me.indentation <> Me.desiredIndentation Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Function OnPreprocessor(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If Not trivia.Kind.IsPreprocessorDirective() Then
                    Return False
                End If

                If Not Me.UseIndentation Then
                    Return True
                End If

                ' preprocessor must be at from column 0
                If Me.indentation <> 0 Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Function ShouldFormat() As Boolean
                For i As Integer = 0 To list.Count - 1
                    Dim trivia = list(i)

                    If OnElastic(trivia) OrElse
                       OnWhitespace(trivia, i) OrElse
                       OnEndOfLine(trivia, i) OrElse
                       OnLineContinuation(trivia, i) OrElse
                       OnColon(trivia) OrElse
                       OnComment(trivia, i) OrElse
                       OnSkippedTokensOrText(trivia) OrElse
                       OnRegion(trivia, i) OrElse
                       OnPreprocessor(trivia, i) Then
                        Return True
                    End If
                Next i

                Return False
            End Function

            Private Shared Function ShouldFormatDocumentationComment(indentation As Integer, tabSize As Integer, trivia As SyntaxTrivia) As Boolean
                Dim xmlComment = CType(trivia.GetStructure(), DocumentationCommentSyntax)

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

                            Dim xmlCommentText = xmlTrivia.GetText()

                            ' "'''" == 3.
                            If xmlCommentText.ConvertStringTextPositionToColumn(tabSize, xmlCommentText.Length - 3) <> indentation Then
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