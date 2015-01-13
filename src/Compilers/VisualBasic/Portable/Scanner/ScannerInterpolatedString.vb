' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Partial Friend Class Scanner

        Private Function ScanInterpolatedStringPunctuation() As SyntaxToken
            If Not CanGetChar() Then
                Return MakeEndOfInterpolatedStringToken()
            End If

            Dim kind As SyntaxKind

            Dim leadingTriviaLength = GetWhitespaceLength(0)
            Dim offset = leadingTriviaLength
            Dim length As Integer

            If Not CanGetCharAtOffset(offset) Then
                Return MakeEndOfInterpolatedStringToken()
            End If

            Dim c = PeekAheadChar(offset)

            ' This should only ever happen for $" or }
            Debug.Assert(leadingTriviaLength = 0 OrElse c = "$"c OrElse c = "}"c)

            ' Another } may follow the close brace of an interpolation if the interpolation lacked a format clause.
            ' This is because the normal escaping rules only apply when parsing the format string.
            Debug.Assert(Not CanGetCharAtOffset(1) OrElse PeekAheadChar(offset + 1) <> c OrElse c = "}"c, "Escape sequence not detected.")

            Dim scanTrailingTrivia As Boolean

            Select Case c
                Case "$"c

                    If CanGetCharAtOffset(offset + 1) AndAlso IsDoubleQuote(PeekAheadChar(offset + 1)) Then
                        kind = SyntaxKind.DollarSignDoubleQuoteToken
                        length = 2
                        scanTrailingTrivia = False ' Trailing whitespace should be scanned as interpolated string text.
                    Else
                        ' We should only reach this point if the parser already detected a full $" token and we're now rescanning.
                        Throw ExceptionUtilities.Unreachable
                    End If

                Case "{"c

                    kind = SyntaxKind.OpenBraceToken
                    length = 1
                    scanTrailingTrivia = True

                Case ","c

                    kind = SyntaxKind.CommaToken
                    length = 1
                    scanTrailingTrivia = True

                Case ":"c

                    kind = SyntaxKind.ColonToken
                    length = 1
                    scanTrailingTrivia = False ' Trailing trivia should be scanned as part of the format string text.

                Case "}"c

                    kind = SyntaxKind.CloseBraceToken
                    length = 1
                    scanTrailingTrivia = False ' Trailing whitespace should be scanned as interpolated string text.

                Case Else

                    If IsDoubleQuote(c) Then
                        Debug.Assert(Not CanGetCharAtOffset(offset + 1) OrElse Not IsDoubleQuote(PeekAheadChar(offset + 1)))

                        kind = SyntaxKind.DoubleQuoteToken
                        length = 1
                        scanTrailingTrivia = True
                    Else
                        Return MakeEndOfInterpolatedStringToken()
                    End If

            End Select

            Dim leadingTrivia = ScanWhitespace(leadingTriviaLength)

            Dim text = GetText(length)

            Dim trailingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = If(scanTrailingTrivia, ScanSingleLineTrivia(), Nothing)

            Return MakePunctuationToken(kind, text, leadingTrivia, trailingTrivia.Node)

        End Function

        Private Function ScanInterpolatedStringContent() As SyntaxToken
            If IsInterpolatedStringPunctuation() Then
                Return ScanInterpolatedStringPunctuation()
            Else
                Return ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia:=False)
            End If
        End Function

        Private Function ScanInterpolatedStringFormatString() As SyntaxToken
            If IsInterpolatedStringPunctuation() Then
                Return ScanInterpolatedStringPunctuation()
            Else
                Return ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia:=True)
            End If
        End Function

        Private Function IsInterpolatedStringPunctuation(Optional offset As Integer = 0) As Boolean
            If Not CanGetCharAtOffset(offset) Then Return False

            Dim c = PeekAheadChar(offset)

            If c = "{"c OrElse c = "}"c Then
                ' This is an escape sequence.
                Return Not CanGetCharAtOffset(offset + 1) OrElse PeekAheadChar(offset + 1) <> c

            ElseIf IsDoubleQuote(c)
                'A subtle difference between this case and the one above.
                ' In both interpolated and literal strings the two quote characters used in an escape sequence don't have to match.
                ' It's enough that the next character is *a* quote char. It doesn't have to be the same quote.
                ' If we want to preserve consistency the quotes need to be special cased.
                Return Not CanGetCharAtOffset(offset + 1) OrElse Not IsDoubleQuote(PeekAheadChar(offset + 1))

            Else
                Return False
            End If
        End Function

        Private Function ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia As Boolean) As SyntaxToken
            If Not CanGetChar() Then Return MakeEndOfInterpolatedStringToken()

            Dim offset = 0
            Dim pendingWhitespace = 0
            Dim valueBuilder = GetScratch()

            Do While CanGetCharAtOffset(offset)
                Dim c = PeekAheadChar(offset)

                If c = "{"c OrElse c = "}"c Then

                    If CanGetCharAtOffset(offset + 1) AndAlso PeekAheadChar(offset + 1) = c Then
                        ' This is an escape sequence.

                        valueBuilder.Append(c)
                        valueBuilder.Append(c)
                        offset += 2
                        pendingWhitespace = 0

                        Continue Do
                    End If

                    Exit Do

                ElseIf IsDoubleQuote(c)

                    If CanGetCharAtOffset(offset + 1) AndAlso IsDoubleQuote(PeekAheadChar(offset + 1)) Then
                        ' This is a VB double quote escape. Oddly enough this logic allows mixing and matching of
                        ' smart and dumb double quotes in any order. Regardless we always emit as a standard double quote.
                        ' This is consistent with their handling in string literals.
                        valueBuilder.Append(""""c)
                        offset += 2
                        pendingWhitespace = 0

                        Continue Do
                    End If

                    Exit Do

                ElseIf IsNewLine(c) AndAlso scanTrailingWhitespaceAsTrivia

                    Exit Do

                ElseIf IsWhitespace(c) AndAlso scanTrailingWhitespaceAsTrivia

                    valueBuilder.Append(c)
                    offset += 1
                    pendingWhitespace += 1
                    Continue Do

                Else

                    valueBuilder.Append(c)
                    offset += 1
                    pendingWhitespace = 0

                End If
            Loop

            ' There was trailing whitespace.
            If pendingWhitespace > 0 Then
                offset -= pendingWhitespace
                valueBuilder.Length -= pendingWhitespace
            End If

            Dim text = If(offset > 0, GetTextNotInterned(offset), String.Empty)

            ' PERF: It's common for the text and the 'value' to be identical. If so, try to unify the
            ' two strings.
            Dim value = GetScratchText(valueBuilder, text)

            Return SyntaxFactory.InterpolatedStringTextToken(text, value, Nothing, ScanWhitespace(pendingWhitespace))

        End Function

        Private Function MakeEndOfInterpolatedStringToken() As SyntaxToken
            Return SyntaxFactory.Token(Nothing, SyntaxKind.EndOfInterpolatedStringToken, Nothing, String.Empty)
        End Function

    End Class
End Namespace