' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Structure CodeShapeAnalyzer

            Private ReadOnly context As FormattingContext
            Private ReadOnly optionSet As OptionSet
            Private ReadOnly list As TriviaList

            Private indentation As Integer
            Private trailingSpaces As Integer
            Private currentColumn As Integer
            Private lastLineBreakIndex As Integer
            Private touchedNoisyCharacterOnCurrentLine As Boolean

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

                    Contract.ThrowIfTrue(trivia.VBKind = SyntaxKind.EndOfLineTrivia)
                    Contract.ThrowIfTrue(trivia.VBKind = SyntaxKind.SkippedTokensTrivia)

                    ' if it contains elastic trivia. always format
                    If trivia.IsElastic() Then
                        Return True
                    End If

                    If trivia.VBKind = SyntaxKind.WhitespaceTrivia Then
                        Debug.Assert(trivia.ToString() = trivia.ToFullString())
                        Dim text = trivia.ToString()
                        If text.IndexOf(vbTab) >= 0 Then
                            Return True
                        End If
                    End If

                    If trivia.VBKind = SyntaxKind.DocumentationCommentTrivia Then
                        Return False
                    End If

                    If trivia.VBKind = SyntaxKind.RegionDirectiveTrivia OrElse trivia.VBKind = SyntaxKind.EndRegionDirectiveTrivia OrElse SyntaxFacts.IsPreprocessorDirective(trivia.VBKind) Then
                        Return False
                    End If

                    If trivia.VBKind = SyntaxKind.ColonTrivia Then
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
                Me.context = context
                Me.optionSet = context.OptionSet
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
                Return trivia.IsElastic()
            End Function

            Private Function OnWhitespace(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.VBKind <> SyntaxKind.WhitespaceTrivia Then
                    Return False
                End If

                ' right after end of line trivia. calculate indentation for current line
                Debug.Assert(trivia.ToString() = trivia.ToFullString())
                Dim text = trivia.ToString()

                ' if text contains tab, we will give up perf optimization and use more expensive one to see whether we need to format this triva
                If text.IndexOf(vbTab) >= 0 Then
                    Return True
                End If

                Dim currentSpaces = text.ConvertTabToSpace(optionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic), Me.currentColumn, text.Length)

                If currentIndex + 1 < Me.list.Count AndAlso Me.list(currentIndex + 1).RawKind = SyntaxKind.LineContinuationTrivia Then
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
                If trivia.VBKind <> SyntaxKind.EndOfLineTrivia Then
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
                If trivia.VBKind <> SyntaxKind.LineContinuationTrivia Then
                    Return False
                End If

                If Me.UseIndentation AndAlso Me.indentation <> 1 Then
                    Return True
                End If

                If trivia.ToFullString().Length <> 3 Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Function OnColon(trivia As SyntaxTrivia) As Boolean
                If trivia.VBKind <> SyntaxKind.ColonTrivia Then
                    Return False
                End If

                ' colon is rare situation. always format in the present of colon trivia.
                Return True
            End Function

            Private Function OnComment(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.VBKind <> SyntaxKind.CommentTrivia AndAlso
                   trivia.VBKind <> SyntaxKind.DocumentationCommentTrivia Then
                    Return False
                End If

                ' if comment is right after a token
                If currentIndex = 0 Then
                    Return True
                End If

                ' check whether indentation are right
                If Me.UseIndentation AndAlso Me.indentation <> Me.context.GetBaseIndentation(trivia.SpanStart) Then
                    ' comment has wrong indentation
                    Return True
                End If

                If trivia.VBKind = SyntaxKind.DocumentationCommentTrivia AndAlso
                   ShouldFormatDocumentationComment(indentation, optionSet.GetOption(FormattingOptions.TabSize, LanguageNames.VisualBasic), trivia) Then
                    Return True
                End If

                MarkTouchedNoisyCharacter()
                Return False
            End Function

            Private Function OnSkippedTokensOrText(trivia As SyntaxTrivia) As Boolean
                If trivia.VBKind <> SyntaxKind.SkippedTokensTrivia Then
                    Return False
                End If

                Return Contract.FailWithReturn(Of Boolean)("This can't happen")
            End Function

            Private Function OnRegion(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If trivia.VBKind <> SyntaxKind.RegionDirectiveTrivia AndAlso
                   trivia.VBKind <> SyntaxKind.EndRegionDirectiveTrivia Then
                    Return False
                End If

                If Not Me.UseIndentation Then
                    Return True
                End If

                If Me.indentation <> Me.context.GetBaseIndentation(trivia.SpanStart) Then
                    Return True
                End If

                ResetStateAfterNewLine(currentIndex)
                Return False
            End Function

            Private Function OnPreprocessor(trivia As SyntaxTrivia, currentIndex As Integer) As Boolean
                If Not SyntaxFacts.IsPreprocessorDirective(trivia.VBKind) Then
                    Return False
                End If

                Return True
            End Function

            Private Function ShouldFormat() As Boolean
                Dim index = -1
                For Each trivia In Me.list
                    index = index + 1

                    If OnElastic(trivia) OrElse
                       OnWhitespace(trivia, index) OrElse
                       OnEndOfLine(trivia, index) OrElse
                       OnLineContinuation(trivia, index) OrElse
                       OnColon(trivia) OrElse
                       OnComment(trivia, index) OrElse
                       OnSkippedTokensOrText(trivia) OrElse
                       OnRegion(trivia, index) OrElse
                       OnPreprocessor(trivia, index) Then
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
                        If xmlTrivia.VBKind = SyntaxKind.DocumentationCommentExteriorTrivia Then
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