' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Strict On
Option Explicit On
Option Infer On

Imports System.Globalization
Imports Roslyn.Exts

Namespace Microsoft.CodeAnalysis.VisualBasic

  ''' <summary>
  ''' Provides members for determining Syntax facts about characters and Unicode conversions.
  ''' </summary>
  Partial Public Class SyntaxFacts

    '/*****************************************************************************/
    '// MakeFullWidth - Converts a half-width to full-width character
    Friend Shared Function MakeFullWidth(c As Char) As Char
      Debug.Assert(IsHalfWidth(c))
      Return Convert.ToChar(Convert.ToUInt16(c) + fullwidth)
    End Function

    Friend Shared Function IsHalfWidth(c As Char) As Boolean
      Return c >= ChrW(&H21S) AndAlso c <= ChrW(&H7ES)
    End Function

    '// MakeHalfWidth - Converts a full-width character to half-width
    Friend Shared Function MakeHalfWidth(c As Char) As Char
      Debug.Assert(IsFullWidth(c))
      Return Convert.ToChar(Convert.ToUInt16(c) - fullwidth)
    End Function

    '// IsFullWidth - Returns if the character is full width
    Friend Shared Function IsFullWidth(c As Char) As Boolean
      ' Do not use "AndAlso" or it will not inline.
      Return c > ChrW(&HFF00US) And c < ChrW(&HFF5FUS)
    End Function

    ''' <summary>
    ''' Determines if Unicode character represents a whitespace.
    ''' </summary>
    ''' <param name="c">The Unicode character.</param>
    ''' <returns>A boolean value set to True if character represents whitespace.</returns>
    Public Shared Function IsWhitespace(c As Char) As Boolean
      Return c.IsAnyOf( SPACE, CHARACTER_TABULATION ) OrElse (c > ChrW(128) AndAlso IsWhitespaceNotAscii(c))
    End Function

    ''' <summary>
    ''' Determines if Unicode character represents a XML whitespace.
    ''' </summary>
    ''' <param name="c">The unicode character</param>
    ''' <returns>A boolean value set to True if character represents XML whitespace.</returns>
    Public Shared Function IsXmlWhitespace(c As Char) As Boolean
      Return c.IsAnyOf( SPACE, CHARACTER_TABULATION) OrElse (c > ChrW(128) AndAlso XmlCharType.IsWhiteSpace(c))
    End Function

    Friend Shared Function IsWhitespaceNotAscii(ch As Char) As Boolean
      Select Case ch
        Case NO_BREAK_SPACE, IDEOGRAPHIC_SPACE, ChrW(&H2000S) To ChrW(&H200BS)
          Return True

        Case Else
          Return CharUnicodeInfo.GetUnicodeCategory(ch) = UnicodeCategory.SpaceSeparator
      End Select
    End Function

    Private Const fullwidth = CInt(&HFF00L - &H0020L)
                                                  
    Friend Const CHARACTER_TABULATION             = ChrW(&H0009)
    Friend Const LINE_FEED                        = ChrW(&H000A)
    Friend Const CARRIAGE_RETURN                  = ChrW(&H000D)
    Friend Const SPACE                            = ChrW(&H0020)
    Friend Const NO_BREAK_SPACE                   = ChrW(&H00A0)
    Friend Const IDEOGRAPHIC_SPACE                = ChrW(&H3000)
    Friend Const LINE_SEPARATOR                   = ChrW(&H2028)
    Friend Const PARAGRAPH_SEPARATOR              = ChrW(&H2029)
    Friend Const NEXT_LINE                        = ChrW(&H0085)
    Friend Const LEFT_SINGLE_QUOTATION_MARK       = ChrW(&H2018)                   REM ‘
    Friend Const RIGHT_SINGLE_QUOTATION_MARK      = ChrW(&H2019)                   REM ’                                                 
    Friend Const LEFT_DOUBLE_QUOTATION_MARK       = ChrW(&H201C)                   REM “
    Friend Const RIGHT_DOUBLE_QUOTATION_MARK      = ChrW(&H201D)                   REM ”                                                 
    Friend Const FULLWIDTH_APOSTROPHE             = ChrW(fullwidth + AscW("'"c))   REM ＇
    Friend Const FULLWIDTH_QUOTATION_MARK         = ChrW(fullwidth + AscW(""""c))  REM ＂
    Friend Const FULLWIDTH_DIGIT_ZERO             = ChrW(fullwidth + AscW("0"c))   REM ０
    Friend Const FULLWIDTH_DIGIT_SEVEN            = ChrW(fullwidth + AscW("7"c))   REM ７
    Friend Const FULLWIDTH_DIGIT_NINE             = ChrW(fullwidth + AscW("9"c))   REM ９                                                                                 
    Friend Const FULLWIDTH_LOW_LINE               = ChrW(fullwidth + AscW("_"c))   REM ＿
    Friend Const FULLWIDTH_COLON                  = ChrW(fullwidth + AscW(":"c))   REM ：
    Friend Const FULLWIDTH_SOLIDUS                = ChrW(fullwidth + AscW("/"c))   REM ／
    Friend Const FULLWIDTH_HYPHEN_MINUS           = ChrW(fullwidth + AscW("-"c))   REM －
    Friend Const FULLWIDTH_PLUS_SIGN              = ChrW(fullwidth + AscW("+"c))   REM ＋
    Friend Const FULLWIDTH_NUMBER_SIGN            = ChrW(fullwidth + AscW("#"c))   REM ＃
    Friend Const FULLWIDTH_EQUALS_SIGN            = ChrW(fullwidth + AscW("="c))   REM ＝
    Friend Const FULLWIDTH_LESS_THAN_SIGN         = ChrW(fullwidth + AscW("<"c))   REM ＜
    Friend Const FULLWIDTH_GREATER_THAN_SIGN      = ChrW(fullwidth + AscW(">"c))   REM ＞
    Friend Const FULLWIDTH_LEFT_PARENTHESIS       = ChrW(fullwidth + AscW("("c))   REM （
    Friend Const FULLWIDTH_LEFT_SQUARE_BRACKET    = ChrW(fullwidth + AscW("["c))   REM ［
    Friend Const FULLWIDTH_RIGHT_SQUARE_BRACKET   = ChrW(fullwidth + AscW("]"c))   REM ］
    Friend Const FULLWIDTH_LEFT_CURLY_BRACKET     = ChrW(fullwidth + AscW("{"c))   REM ｛
    Friend Const FULLWIDTH_RIGHT_CURLY_BRACKET    = ChrW(fullwidth + AscW("}"c))   REM ｝
    Friend Const FULLWIDTH_AMPERSAND              = ChrW(fullwidth + AscW("&"c))   REM ＆
    Friend Const FULLWIDTH_DOLLAR_SIGN            = ChrW(fullwidth + AscW("$"c))   REM ＄
    Friend Const FULLWIDTH_QUESTION_MARK          = ChrW(fullwidth + AscW("?"c))   REM ？
    Friend Const FULLWIDTH_FULL_STOP              = ChrW(fullwidth + AscW("."c))   REM ．
    Friend Const FULLWIDTH_COMMA                  = ChrW(fullwidth + AscW(","c))   REM ，
    Friend Const FULLWIDTH_PERCENT_SIGN           = ChrW(fullwidth + AscW("%"c))   REM ％
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_H = ChrW(fullwidth + AscW("H"c))   REM Ｈ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_O = ChrW(fullwidth + AscW("O"c))   REM Ｏ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_E = ChrW(fullwidth + AscW("E"c))   REM Ｅ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_A = ChrW(fullwidth + AscW("A"c))   REM Ａ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_F = ChrW(fullwidth + AscW("F"c))   REM Ｆ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_C = ChrW(fullwidth + AscW("C"c))   REM Ｃ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_P = ChrW(fullwidth + AscW("P"c))   REM Ｐ
    Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_M = ChrW(fullwidth + AscW("M"c))   REM Ｍ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_H   = ChrW(fullwidth + AscW("h"c))   REM ｈ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_O   = ChrW(fullwidth + AscW("o"c))   REM ｏ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_E   = ChrW(fullwidth + AscW("e"c))   REM ｅ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_A   = ChrW(fullwidth + AscW("a"c))   REM ａ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_F   = ChrW(fullwidth + AscW("f"c))   REM ｆ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_C   = ChrW(fullwidth + AscW("c"c))   REM ｃ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_P   = ChrW(fullwidth + AscW("p"c))   REM ｐ
    Friend Const FULLWIDTH_LATIN_SMALL_LETTER_M   = ChrW(fullwidth + AscW("m"c))   REM ｍ

    Friend Const FULLWIDTH_LEFT_PARENTHESIS_STRING$      = FULLWIDTH_LEFT_PARENTHESIS
    Friend Const FULLWIDTH_RIGHT_PARENTHESIS_STRING$     = ChrW(fullwidth + AscW(")"c))
    Friend Const FULLWIDTH_LEFT_CURLY_BRACKET_STRING$    = FULLWIDTH_LEFT_CURLY_BRACKET
    Friend Const FULLWIDTH_RIGHT_CURLY_BRACKET_STRING$   = FULLWIDTH_RIGHT_CURLY_BRACKET
    Friend Const FULLWIDTH_FULL_STOP_STRING$             = FULLWIDTH_FULL_STOP
    Friend Const FULLWIDTH_COMMA_STRING$                 = FULLWIDTH_COMMA
    Friend Const FULLWIDTH_EQUALS_SIGN_STRING$           = FULLWIDTH_EQUALS_SIGN
    Friend Const FULLWIDTH_PLUS_SIGN_STRING$             = FULLWIDTH_PLUS_SIGN
    Friend Const FULLWIDTH_HYPHEN_MINUS_STRING$          = FULLWIDTH_HYPHEN_MINUS
    Friend Const FULLWIDTH_ASTERISK_STRING$              = ChrW(fullwidth + AscW("*"c))
    Friend Const FULLWIDTH_SOLIDUS_STRING$               = FULLWIDTH_SOLIDUS
    Friend Const FULLWIDTH_REVERSE_SOLIDUS_STRING$       = ChrW(fullwidth + AscW("\"c))
    Friend Const FULLWIDTH_COLON_STRING$                 = FULLWIDTH_COLON
    Friend Const FULLWIDTH_CIRCUMFLEX_ACCENT_STRING$     = ChrW(fullwidth + AscW("^"c))
    Friend Const FULLWIDTH_AMPERSAND_STRING$             = FULLWIDTH_AMPERSAND
    Friend Const FULLWIDTH_NUMBER_SIGN_STRING$           = FULLWIDTH_NUMBER_SIGN
    Friend Const FULLWIDTH_EXCLAMATION_MARK_STRING$      = ChrW(fullwidth + AscW("!"c))
    Friend Const FULLWIDTH_QUESTION_MARK_STRING$         = FULLWIDTH_QUESTION_MARK
    Friend Const FULLWIDTH_COMMERCIAL_AT_STRING$         = ChrW(fullwidth + AscW("@"c))
    Friend Const FULLWIDTH_LESS_THAN_SIGN_STRING$        = FULLWIDTH_LESS_THAN_SIGN
    Friend Const FULLWIDTH_GREATER_THAN_SIGN_STRING$     = FULLWIDTH_GREATER_THAN_SIGN

    ''' <summary>
    ''' Determines if the Unicode character is a newline character.
    ''' </summary>
    ''' <param name="c">The Unicode character.</param>
    ''' <returns>A boolean value set to True if character is a newline character.</returns>
    Public Shared Function IsNewLine(c As Char) As Boolean
      Return c.IsAnyOf(CARRIAGE_RETURN, LINE_FEED) OrElse
        (c >= NEXT_LINE AndAlso c.IsAnyOf(NEXT_LINE, LINE_SEPARATOR, PARAGRAPH_SEPARATOR))
    End Function

    Friend Shared Function IsSingleQuote(c As Char) As Boolean
      ' // Besides the half width and full width ', we also check for Unicode
      ' // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
      ' // IME editors paste them in. This isn't really technically correct
      ' // because we ignore the left-ness or right-ness, but see VS 170991
      Return c = "'"c OrElse
        (c >= LEFT_SINGLE_QUOTATION_MARK AndAlso c.IsAnyOf(FULLWIDTH_APOSTROPHE, LEFT_SINGLE_QUOTATION_MARK, RIGHT_SINGLE_QUOTATION_MARK))
    End Function

    Friend Shared Function IsDoubleQuote(c As Char) As Boolean
      ' // Besides the half width and full width ", we also check for Unicode
      ' // LEFT DOUBLE QUOTATION MARK and RIGHT DOUBLE QUOTATION MARK because
      ' // IME editors paste them in. This isn't really technically correct
      ' // because we ignore the left-ness or right-ness, but see VS 170991
      Return c = """"c OrElse
        (c >= LEFT_DOUBLE_QUOTATION_MARK AndAlso c.IsAnyOf(FULLWIDTH_QUOTATION_MARK, LEFT_DOUBLE_QUOTATION_MARK, RIGHT_DOUBLE_QUOTATION_MARK))
    End Function

    Friend Shared Function IsLeftCurlyBracket(c As Char) As Boolean
      Return c.IsAnyOf("{"c, FULLWIDTH_LEFT_CURLY_BRACKET)
    End Function

    Friend Shared Function IsRightCurlyBracket(c As Char) As Boolean
      Return c.IsAnyOf("}"c, FULLWIDTH_RIGHT_CURLY_BRACKET)
    End Function

    ''' <summary>
    ''' Determines if the unicode character is a colon character.
    ''' </summary>
    ''' <param name="c">The unicode character.</param>
    ''' <returns>A boolean value set to True if character is a colon character.</returns>
    Public Shared Function IsColon(c As Char) As Boolean
      Return c.IsAnyOf(":"c, FULLWIDTH_COLON)
    End Function

    ''' <summary>
    ''' Determines if the unicode character is a underscore character.
    ''' </summary>
    ''' <param name="c">The unicode character.</param>
    ''' <returns>A boolean value set to True if character is an underscore character.</returns>
    Public Shared Function IsUnderscore(c As Char) As Boolean
      ' NOTE: fullwidth _ is not considered any special.
      Return c = "_"c
    End Function

    ''' <summary>
    ''' Determines if the unicode character is a hash character.
    ''' </summary>
    ''' <param name="c">The unicode character.</param>
    ''' <returns>A boolean value set to True if character is a hash character.</returns>
    Public Shared Function IsHash(c As Char) As Boolean
      Return c.IsAnyOf("#"c, FULLWIDTH_NUMBER_SIGN)
    End Function

    ''' <summary>
    ''' Determines if the Unicode character can be the starting character of a Visual Basic identifier.
    ''' </summary>
    ''' <param name="c">The Unicode character.</param>
    ''' <returns>A boolean value set to True if character can be part of a valid start charcater in an identifier.</returns>
    Public Shared Function IsIdentifierStartCharacter ( c As Char ) As Boolean
      'TODO: make easy cases fast (or check if they already are)
      Dim CharacterProperties As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)

      Return           IsPropAlpha(CharacterProperties) OrElse
                 IsPropLetterDigit(CharacterProperties) OrElse
        IsPropConnectorPunctuation(CharacterProperties)
    End Function



    ' TODO: replace CByte with something faster.
    Friend Shared Function IntegralLiteralCharacterValue ( Digit As Char ) As Byte
      If IsFullWidth(Digit) Then Digit = MakeHalfWidth(Digit)
      Dim u As Integer = AscW(Digit)
      Select Case True
        Case IsDecimalDigit(Digit)       : Return CByte(u - AscW("0"c))
        Case Digit.IsBetween("A"c, "F"c) : Return CByte(u + (10 - AscW("A"c)))
      End Select
      Debug.Assert(Digit.IsBetween("a"c,"f"c), "Surprising digit.")
      Return CByte(u + (10 - AscW("a"c)))
    End Function

    Friend Shared Function BeginsBaseLiteral(c As Char) As Boolean
      Return c.IsAnyOf("H"c,"h"c, "O"c,"o"c) OrElse
            (IsFullWidth(c) AndAlso c.IsAnyOf( FULLWIDTH_LATIN_CAPITAL_LETTER_H, FULLWIDTH_LATIN_SMALL_LETTER_H,
                                               FULLWIDTH_LATIN_CAPITAL_LETTER_O, FULLWIDTH_LATIN_SMALL_LETTER_O))
    End Function

    Private Shared _IsIDChar As Boolean() =
    {
      False, False, False, False, False, False, False, False, False, False,
      False, False, False, False, False, False, False, False, False, False,
      False, False, False, False, False, False, False, False, False, False,
      False, False, False, False, False, False, False, False, False, False,
      False, False, False, False, False, False, False, False,  True,  True,
       True,  True,  True,  True,  True,  True,  True,  True, False, False,
      False, False, False, False, False,  True,  True,  True,  True,  True,
       True,  True,  True,  True,  True,  True,  True,  True,  True,  True,
       True,  True,  True,  True,  True,  True,  True,  True,  True,  True,
       True, False, False, False, False,  True, False,  True,  True,  True,
       True,  True,  True,  True,  True,  True,  True,  True,  True,  True,
       True,  True,  True,  True,  True,  True,  True,  True,  True,  True,
       True,  True,  True, False, False, False, False, False
    }

    Friend Shared Function IsNarrowIdentifierCharacter(c As UInt16) As Boolean
      Return _IsIDChar(c)
    End Function

    ''' <summary>
    ''' Determines if the Unicode character can be a part of a Visual Basic identifier.
    ''' </summary>
    ''' <param name="c">The Unicode character.</param>
    ''' <returns>A boolean value set to True if character can be part of a valid identifier.</returns>
    Public Shared Function IsIdentifierPartCharacter(c As Char) As Boolean
      If c < ChrW(128) Then Return IsNarrowIdentifierCharacter(Convert.ToUInt16(c))
      Return IsWideIdentifierCharacter(c)
    End Function

    ''' <summary>
    ''' Determines if the name is a valid identifier.
    ''' </summary>
    ''' <param name="name">The identifier name.</param>
    ''' <returns>A boolean value set to True if name is valid identifier.</returns>
    Public Shared Function IsValidIdentifier(name As String) As Boolean
      If String.IsNullOrEmpty(name) OrElse Not IsIdentifierStartCharacter(name(0)) Then Return False
      Dim nameLength As Integer = name.Length
      For i As Integer = 1 To nameLength - 1 ' NB: start at 1
        If Not IsIdentifierPartCharacter(name(i)) Then Return False
      Next
      Return True
    End Function

    ''' <summary>
    ''' Creates a half width form Unicode character string. 
    ''' </summary>
    ''' <param name="text">The text representing the original identifier.  This can be in full width or half width Unicode form.  </param>
    ''' <returns>A string representing the text in a half width Unicode form.</returns>
    Public Shared Function MakeHalfWidthIdentifier(text As String) As String
      If text Is Nothing Then Return text
      Dim characters As Char() = Nothing
      For i = 0 To text.Length - 1
        Dim c = text(i)

        If IsFullWidth(c) Then
          If characters Is Nothing Then
            characters = New Char(text.Length - 1) {}
            text.CopyTo(0, characters, 0, i)
          End If
          characters(i) = MakeHalfWidth(c)

        ElseIf characters IsNot Nothing Then
          characters(i) = c
        End If
      Next

      Return If(characters Is Nothing, text, New String(characters))
    End Function

    Friend Shared Function IsWideIdentifierCharacter(c As Char) As Boolean
      Dim cProps As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)
      Return    IsPropAlphaNumeric(cProps) OrElse IsPropLetterDigit(cProps) OrElse
        IsPropConnectorPunctuation(cProps) OrElse   IsPropCombining(cProps) OrElse
                 IsPropOtherFormat(cProps)
    End Function

    Friend Shared Function BeginsExponent(c As Char) As Boolean
      Return c.IsAnyOf("E"c,"e"c, FULLWIDTH_LATIN_CAPITAL_LETTER_E, FULLWIDTH_LATIN_SMALL_LETTER_E)
    End Function

    Friend Shared Function IsOctalDigit(c As Char) As Boolean
      Return c.IsBetween("0"c,"7"c) OrElse c.IsBetween( FULLWIDTH_DIGIT_ZERO, FULLWIDTH_DIGIT_SEVEN )
    End Function

    Friend Shared Function IsDecimalDigit(c As Char) As Boolean
      Return c.IsBetween("0"c,"9"c) OrElse c.IsBetween( FULLWIDTH_DIGIT_ZERO, FULLWIDTH_DIGIT_NINE )
    End Function

    Friend Shared Function IsHexDigit(c As Char) As Boolean
      Return IsDecimalDigit(c) OrElse
              c.IsBetween("a"c,"f"c) OrElse
              c.IsBetween("A"c,"F"c) OrElse
              c.IsBetween( FULLWIDTH_LATIN_SMALL_LETTER_A, FULLWIDTH_LATIN_SMALL_LETTER_F) OrElse
              c.IsBetween( FULLWIDTH_LATIN_CAPITAL_LETTER_A, FULLWIDTH_LATIN_CAPITAL_LETTER_F)
    End Function

    Friend Shared Function IsDateSeparatorCharacter(c As Char) As Boolean
      Return c.IsAnyOf("/"c, "-"c, FULLWIDTH_SOLIDUS, FULLWIDTH_HYPHEN_MINUS)
    End Function

    Friend Shared ReadOnly DaysToMonth365() As Integer = {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365}
    Friend Shared ReadOnly DaysToMonth366() As Integer = {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366}

    Friend Shared Function IsLetterC(ch As Char) As Boolean
      Return ch.IsAnyOf("c"c,"C"c, FULLWIDTH_LATIN_CAPITAL_LETTER_C, FULLWIDTH_LATIN_SMALL_LETTER_C)
    End Function

    ''' <summary>
    ''' matches one char or another.
    ''' Typical usage is for matching lowercase and uppercase.
    ''' </summary>
    Friend Shared Function MatchOneOrAnother(ch As Char, one As Char, another As Char) As Boolean
      Return ch = one OrElse ch = another
    End Function

    ''' <summary>
    ''' matches one char or another.
    ''' it will try normal width and then fullwidth variations.
    ''' Typical usage is for matching lowercase and uppercase.
    ''' </summary>
    Friend Shared Function MatchOneOrAnotherOrFullwidth(ch As Char, one As Char, another As Char) As Boolean
      Debug.Assert(IsHalfWidth(one))
      Debug.Assert(IsHalfWidth(another))

      If IsFullWidth(ch) Then ch = MakeHalfWidth(ch)
      Return ch.IsAnyOf( one , another)
    End Function

    Friend Shared Function IsPropAlpha(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties <= UnicodeCategory.OtherLetter
    End Function

    Friend Shared Function IsPropAlphaNumeric(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties <= UnicodeCategory.DecimalDigitNumber
    End Function

    Friend Shared Function IsPropLetterDigit(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties = UnicodeCategory.LetterNumber
    End Function

    Friend Shared Function IsPropConnectorPunctuation(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties = UnicodeCategory.ConnectorPunctuation
    End Function

    Friend Shared Function IsPropCombining(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties >= UnicodeCategory.NonSpacingMark AndAlso
          CharacterProperties <= UnicodeCategory.EnclosingMark
    End Function

    Friend Shared Function IsConnectorPunctuation(c As Char) As Boolean
      Return CharUnicodeInfo.GetUnicodeCategory(c) = UnicodeCategory.ConnectorPunctuation
    End Function

    Friend Shared Function IsSpaceSeparator(c As Char) As Boolean
      Return CharUnicodeInfo.GetUnicodeCategory(c) = UnicodeCategory.SpaceSeparator
    End Function

    Friend Shared Function IsPropOtherFormat(CharacterProperties As UnicodeCategory) As Boolean
      Return CharacterProperties = UnicodeCategory.Format
    End Function

    Friend Shared Function IsSurrogate(c As Char) As Boolean
      Return Char.IsSurrogate(c)
    End Function

    Friend Shared Function IsHighSurrogate(c As Char) As Boolean
      Return Char.IsHighSurrogate(c)
    End Function

    Friend Shared Function IsLowSurrogate(c As Char) As Boolean
      Return Char.IsLowSurrogate(c)
    End Function

    Friend Shared Function ReturnFullWidthOrSelf(c As Char) As Char
      If IsHalfWidth(c) Then Return MakeFullWidth(c)
      Return c
    End Function
  End Class

End Namespace
