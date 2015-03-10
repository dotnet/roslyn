' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------

Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
  Partial Friend Class Scanner

    Private Function ScanInterpolatedStringPunctuation() As SyntaxToken
      If Not CanGet() Then Return MakeEndOfInterpolatedStringToken()

      Dim kind As SyntaxKind

      Dim leadingTriviaLength = GetWhitespaceLength(0)
      Dim offset = leadingTriviaLength
      Dim length As Integer
      Dim c As Char
      If Not TryPeek(offset,c) Then Return MakeEndOfInterpolatedStringToken()
      ' This should only ever happen for $" or }
      Debug.Assert(leadingTriviaLength = 0 OrElse c = "$"c OrElse c = FULLWIDTH_DOLLAR_SIGN OrElse IsRightCurlyBracket(c))

      ' Another } may follow the close brace of an interpolation if the interpolation lacked a format clause.
      ' This is because the normal escaping rules only apply when parsing the format string.
      Debug.Assert(Not CanGet(1) OrElse Peek(offset + 1) <> c OrElse IsRightCurlyBracket(c), "Escape sequence not detected.")

      Dim scanTrailingTrivia As Boolean
     
 

      Select Case c
        Case "$"c, FULLWIDTH_DOLLAR_SIGN
          Dim c1 As Char
          If TryPeek(offset + 1,c1) AndAlso IsDoubleQuote(c1) Then
            kind = SyntaxKind.DollarSignDoubleQuoteToken
            length = 2
            scanTrailingTrivia = False ' Trailing whitespace should be scanned as interpolated string text.
          Else
            ' We should only reach this point if the parser already detected a full $" token and we're now rescanning.
            Throw ExceptionUtilities.Unreachable
          End If
        Case "{"c, FULLWIDTH_LEFT_CURLY_BRACKET
          kind = SyntaxKind.OpenBraceToken
          length = 1
          scanTrailingTrivia = True
        Case ","c, FULLWIDTH_COMMA
          kind = SyntaxKind.CommaToken
          length = 1
          scanTrailingTrivia = True
        Case ":"c, FULLWIDTH_COLON
          kind = SyntaxKind.ColonToken
          length = 1
          scanTrailingTrivia = False ' Trailing trivia should be scanned as part of the format string text.
        Case "}"c, FULLWIDTH_RIGHT_CURLY_BRACKET
          kind = SyntaxKind.CloseBraceToken
          length = 1
          scanTrailingTrivia = False ' Trailing whitespace should be scanned as interpolated string text.
        Case Else
          If Not IsDoubleQuote(c) Then Return MakeEndOfInterpolatedStringToken()
          Debug.Assert(Not CanGet(offset + 1) OrElse Not IsDoubleQuote(Peek(offset + 1)))
          kind = SyntaxKind.DoubleQuoteToken
          length = 1
          scanTrailingTrivia = True
      End Select
      Dim leadingTrivia = ScanWhitespace(leadingTriviaLength)
      Dim text = GetText(length)
      Dim trailingTrivia As SyntaxList(Of VisualBasicSyntaxNode) = If(scanTrailingTrivia, ScanSingleLineTrivia(), Nothing)
      Return MakePunctuationToken(kind, text, leadingTrivia, trailingTrivia.Node)
    End Function

    Private Function ScanInterpolatedStringContent() As SyntaxToken
      If IsInterpolatedStringPunctuation() Then Return ScanInterpolatedStringPunctuation()
      Return ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia:=False)
    End Function

    Private Function ScanInterpolatedStringFormatString() As SyntaxToken
      If IsInterpolatedStringPunctuation() Then Return ScanInterpolatedStringPunctuation()
      Return ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia:=True)
    End Function

    Private Function IsInterpolatedStringPunctuation(Optional offset As Integer = 0) As Boolean
      Dim c As Char
      If Not TryPeek(offset,c) Then Return False
      Dim c1 As Char
      Select Case True
        Case IsLeftCurlyBracket(c)  : Return Not TryPeek(offset + 1,c1) OrElse Not IsLeftCurlyBracket(c1)
        Case IsRightCurlyBracket(c) : Return Not TryPeek(offset + 1,c1) OrElse Not IsRightCurlyBracket(c1)
        Case IsDoubleQuote(c) 
          'A subtle difference between this case and the one above.
          ' In both interpolated and literal strings the two quote characters used in an escape sequence don't have to match.
          ' It's enough that the next character is *a* quote char. It doesn't have to be the same quote.
          ' If we want to preserve consistency the quotes need to be special cased.
          Return Not TryPeek(offset + 1,c1) OrElse Not IsDoubleQuote(c1)
        Case Else
          Return False
      End Select
    End Function

    Private Function ScanInterpolatedStringText(scanTrailingWhitespaceAsTrivia As Boolean) As SyntaxToken
      If Not CanGet() Then Return MakeEndOfInterpolatedStringToken()

      Dim offset = 0
      Dim pendingWhitespace = 0
      Dim valueBuilder = GetScratch()
      Dim c,c1 As Char
      Do While TryPeek(offset,c)
        ' Any combination of fullwidth and ASCII curly braces of the same direction is an escaping sequence for the corresponding ASCII curly brace.
        ' We insert that curly brace doubled and because this is the escaping sequence understood by String.Format, that will be replaced by a single brace.
        ' This is deliberate design and it aligns with existing rules for double quote escaping in strings.
        Select Case True
          Case IsLeftCurlyBracket(c) 

          If Not( TryPeek(offset + 1,c1) AndAlso IsLeftCurlyBracket(c1)) Then Exit Do
          ' This is an escape sequence.
          valueBuilder.Append("{{")
          offset += 2
          pendingWhitespace = 0
          Continue Do
        Case IsRightCurlyBracket(c)

          If Not( CanGet(offset + 1) AndAlso IsRightCurlyBracket(Peek(offset + 1))) Then Exit Do
          ' This is an escape sequence.
          valueBuilder.Append("}}")
          offset += 2
          pendingWhitespace = 0
          Continue Do
        Case IsDoubleQuote(c)
          If Not( TryPeek(offset + 1,c1) AndAlso IsDoubleQuote(c1)) Then Exit Do
          ' This is a VB double quote escape. Oddly enough this logic allows mixing and matching of
          ' smart and dumb double quotes in any order. Regardless we always emit as a standard double quote.
          ' This is consistent with their handling in string literals.
          valueBuilder.Append(""""c)
          offset += 2
          pendingWhitespace = 0
          Continue Do
        Case IsNewLine(c) AndAlso scanTrailingWhitespaceAsTrivia
          Exit Do
        Case IsWhitespace(c) AndAlso scanTrailingWhitespaceAsTrivia
          valueBuilder.Append(c)
          offset += 1
          pendingWhitespace += 1
          Continue Do
        Case Else
          valueBuilder.Append(c)
          offset += 1
          pendingWhitespace = 0
        End Select
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
