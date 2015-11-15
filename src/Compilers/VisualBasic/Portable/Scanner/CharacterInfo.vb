' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------
Option Strict On
Option Explicit On
Option Infer On

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Provides members for determining Syntax facts about characters and Unicode conversions.
    ''' </summary>
    Partial Public Class SyntaxFacts

        '/*****************************************************************************/
        '// MakeFullWidth - Converts a half-width to full-width character
        Friend Shared Function MakeFullWidth(c As Char) As Char
            Debug.Assert(IsHalfWidth(c))
            Return Convert.ToChar(Convert.ToUInt16(c) + s_fullwidth)
        End Function

        Friend Shared Function IsHalfWidth(c As Char) As Boolean
            Return c >= ChrW(&H21S) And c <= ChrW(&H7ES)
        End Function

        '// MakeHalfWidth - Converts a full-width character to half-width
        Friend Shared Function MakeHalfWidth(c As Char) As Char
            Debug.Assert(IsFullWidth(c))

            Return Convert.ToChar(Convert.ToUInt16(c) - s_fullwidth)
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
            Return (SPACE = c) Or (CHARACTER_TABULATION = c) Or (c > ChrW(128) And IsWhitespaceNotAscii(c))
        End Function

        ''' <summary>
        ''' Determines if Unicode character represents a XML whitespace.
        ''' </summary>
        ''' <param name="c">The unicode character</param>
        ''' <returns>A boolean value set to True if character represents XML whitespace.</returns>
        Public Shared Function IsXmlWhitespace(c As Char) As Boolean
            Return (SPACE = c) Or (CHARACTER_TABULATION = c) Or (c > ChrW(128) And XmlCharType.IsWhiteSpace(c))
        End Function

        Friend Shared Function IsWhitespaceNotAscii(ch As Char) As Boolean
            Select Case ch
                Case NO_BREAK_SPACE, IDEOGRAPHIC_SPACE, ChrW(&H2000S) To ChrW(&H200BS)
                    Return True

                Case Else
                    Return CharUnicodeInfo.GetUnicodeCategory(ch) = UnicodeCategory.SpaceSeparator
            End Select
        End Function

        Private Const s_fullwidth = &HFEE0

        Friend Const CHARACTER_TABULATION = ChrW(&H0009)
        Friend Const LINE_FEED = ChrW(&H000A)
        Friend Const CARRIAGE_RETURN = ChrW(&H000D)
        Friend Const SPACE = ChrW(&H0020)
        Friend Const NO_BREAK_SPACE = ChrW(&H00A0)
        Friend Const IDEOGRAPHIC_SPACE = ChrW(&H3000)
        Friend Const LINE_SEPARATOR = ChrW(&H2028)
        Friend Const PARAGRAPH_SEPARATOR = ChrW(&H2029)
        Friend Const NEXT_LINE = ChrW(&H0085)


        Friend Const LEFT_SINGLE_QUOTATION_MARK = ChrW(&H2018)        REM  ‘ / "‘"c / ChrW(&H2018)
        Friend Const RIGHT_SINGLE_QUOTATION_MARK = ChrW(&H2019)       REM  ’ / "’"c / ChrW(&H2019)  

        Friend Const LEFT_DOUBLE_QUOTATION_MARK = ChrW(&H201C)        REM   ““ / "““"c / ChrW(&H201C)
        Friend Const RIGHT_DOUBLE_QUOTATION_MARK = ChrW(&H201D)       REM   ”” / "””"c / ChrW(&H201D) 

        Friend Const FULLWIDTH_APOSTROPHE = ChrW(&HFF07)              REM  ＇ / "'"w
        Friend Const FULLWIDTH_QUOTATION_MARK = ChrW(&HFF02)          REM  ＂ / """"w

        Friend Const FULLWIDTH_DIGIT_ZERO = ChrW(&HFF10)              REM  ０ / "0"w
        Friend Const FULLWIDTH_DIGIT_SEVEN = ChrW(&HFF17)             REM  ７ / "7"w
        Friend Const FULLWIDTH_DIGIT_NINE = ChrW(&HFF19)              REM  ９ / "9"w

        Friend Const FULLWIDTH_LOW_LINE = ChrW(&HFF3F)                REM  ＿ / "_"w
        Friend Const FULLWIDTH_COLON = ChrW(&HFF1A)                   REM  ： / ":"w
        Friend Const FULLWIDTH_SOLIDUS = ChrW(&HFF0F)                 REM  ／ / "/"w
        Friend Const FULLWIDTH_HYPHEN_MINUS = ChrW(&HFF0D)            REM  － / "-"w
        Friend Const FULLWIDTH_PLUS_SIGN = ChrW(&HFF0B)               REM  ＋ / "+"w
        Friend Const FULLWIDTH_NUMBER_SIGN = ChrW(&HFF03)             REM  ＃ / "#"w

        Friend Const FULLWIDTH_EQUALS_SIGN = ChrW(&HFF1D)             REM  ＝ / "="w
        Friend Const FULLWIDTH_LESS_THAN_SIGN = ChrW(&HFF1C)          REM  ＜ / "<"w
        Friend Const FULLWIDTH_GREATER_THAN_SIGN = ChrW(&HFF1E)       REM  ＞ / ">"w
        Friend Const FULLWIDTH_LEFT_PARENTHESIS = ChrW(&HFF08)        REM  （ / "("w
        Friend Const FULLWIDTH_RIGHT_PARENTHESIS = ChrW(&HFF09)       REM  （ / "("w
        Friend Const FULLWIDTH_LEFT_SQUARE_BRACKET = ChrW(&HFF3B)     REM  ［ / "["w
        Friend Const FULLWIDTH_RIGHT_SQUARE_BRACKET = ChrW(&HFF3D)    REM  ］ / "]"w
        Friend Const FULLWIDTH_LEFT_CURLY_BRACKET = ChrW(&HFF5B)      REM  ｛ / "{"w
        Friend Const FULLWIDTH_RIGHT_CURLY_BRACKET = ChrW(&HFF5D)     REM  ｝ / "}"w
        Friend Const FULLWIDTH_AMPERSAND = ChrW(&HFF06)               REM  ＆ / "&"w
        Friend Const FULLWIDTH_DOLLAR_SIGN = ChrW(&HFF04)             REM  ＄ / "$"w
        Friend Const FULLWIDTH_QUESTION_MARK = ChrW(&HFF1F)           REM  ？ / "?"w
        Friend Const FULLWIDTH_FULL_STOP = ChrW(&HFF0E)               REM  ． / "."w
        Friend Const FULLWIDTH_COMMA = ChrW(&HFF0C)                   REM  ， / ","w
        Friend Const FULLWIDTH_PERCENT_SIGN = ChrW(&HFF05)            REM  ％ / "%"w

        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_H = ChrW(&HFF28)  REM  Ｈ / "H"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_O = ChrW(&HFF2F)  REM  Ｏ / "O"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_E = ChrW(&HFF25)  REM  Ｅ / "E"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_A = ChrW(&HFF21)  REM  Ａ / "A"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_F = ChrW(&HFF26)  REM  Ｆ / "F"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_C = ChrW(&HFF23)  REM  Ｃ / "C"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_P = ChrW(&HFF30)  REM  Ｐ / "P"w
        Friend Const FULLWIDTH_LATIN_CAPITAL_LETTER_M = ChrW(&HFF2D)  REM  Ｍ / "M"w

        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_H = ChrW(&HFF48)    REM  ｈ / "h"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_O = ChrW(&HFF4F)    REM  ｏ / "o"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_E = ChrW(&HFF45)    REM  ｅ / "e"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_A = ChrW(&HFF41)    REM  ａ / "a"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_F = ChrW(&HFF46)    REM  ｆ / "f"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_C = ChrW(&HFF43)    REM  ｃ / "c"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_P = ChrW(&HFF50)    REM  ｐ / "p"w
        Friend Const FULLWIDTH_LATIN_SMALL_LETTER_M = ChrW(&HFF4D)    REM  ｍ / "m"w

        Friend Const FULLWIDTH_LEFT_PARENTHESIS_STRING$ = FULLWIDTH_LEFT_PARENTHESIS
        Friend Const FULLWIDTH_RIGHT_PARENTHESIS_STRING$ = FULLWIDTH_RIGHT_PARENTHESIS
        Friend Const FULLWIDTH_LEFT_CURLY_BRACKET_STRING$ = FULLWIDTH_LEFT_CURLY_BRACKET
        Friend Const FULLWIDTH_RIGHT_CURLY_BRACKET_STRING$ = FULLWIDTH_RIGHT_CURLY_BRACKET
        Friend Const FULLWIDTH_FULL_STOP_STRING$ = FULLWIDTH_FULL_STOP
        Friend Const FULLWIDTH_COMMA_STRING$ = FULLWIDTH_COMMA
        Friend Const FULLWIDTH_EQUALS_SIGN_STRING$ = FULLWIDTH_EQUALS_SIGN
        Friend Const FULLWIDTH_PLUS_SIGN_STRING$ = FULLWIDTH_PLUS_SIGN
        Friend Const FULLWIDTH_HYPHEN_MINUS_STRING$ = FULLWIDTH_HYPHEN_MINUS
        Friend Const FULLWIDTH_ASTERISK_STRING$ = ChrW(&HFF0A)               REM  "*"w
        Friend Const FULLWIDTH_SOLIDUS_STRING$ = FULLWIDTH_SOLIDUS
        Friend Const FULLWIDTH_REVERSE_SOLIDUS_STRING$ = ChrW(&HFF3C)        REM  "\"w
        Friend Const FULLWIDTH_COLON_STRING$ = FULLWIDTH_COLON
        Friend Const FULLWIDTH_CIRCUMFLEX_ACCENT_STRING$ = ChrW(&HFF3E)      REM  "^"w
        Friend Const FULLWIDTH_AMPERSAND_STRING$ = FULLWIDTH_AMPERSAND
        Friend Const FULLWIDTH_NUMBER_SIGN_STRING$ = FULLWIDTH_NUMBER_SIGN
        Friend Const FULLWIDTH_EXCLAMATION_MARK_STRING$ = ChrW(&HFF01)       REM  "!"w
        Friend Const FULLWIDTH_QUESTION_MARK_STRING$ = FULLWIDTH_QUESTION_MARK
        Friend Const FULLWIDTH_COMMERCIAL_AT_STRING$ = ChrW(&HFF20)          REM  "@"w
        Friend Const FULLWIDTH_LESS_THAN_SIGN_STRING$ = FULLWIDTH_LESS_THAN_SIGN
        Friend Const FULLWIDTH_GREATER_THAN_SIGN_STRING$ = FULLWIDTH_GREATER_THAN_SIGN

        ''' <summary>
        ''' Determines if the Unicode character is a newline character.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character is a newline character.</returns>
        Public Shared Function IsNewLine(c As Char) As Boolean
            Return CARRIAGE_RETURN = c Or
                LINE_FEED = c Or (c >= NEXT_LINE And
                (NEXT_LINE = c Or LINE_SEPARATOR = c Or PARAGRAPH_SEPARATOR = c))
        End Function

        Friend Shared Function IsSingleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ', we also check for Unicode
            ' // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = "'"c Or
                (c >= LEFT_SINGLE_QUOTATION_MARK And
                (c = FULLWIDTH_APOSTROPHE Or c = LEFT_SINGLE_QUOTATION_MARK Or c = RIGHT_SINGLE_QUOTATION_MARK))
        End Function

        Friend Shared Function IsDoubleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ", we also check for Unicode
            ' // LEFT DOUBLE QUOTATION MARK and RIGHT DOUBLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = """"c Or
                (c >= LEFT_DOUBLE_QUOTATION_MARK AndAlso
                 (c = FULLWIDTH_QUOTATION_MARK Or c = LEFT_DOUBLE_QUOTATION_MARK Or c = RIGHT_DOUBLE_QUOTATION_MARK))
        End Function

        Friend Shared Function IsLeftCurlyBracket(c As Char) As Boolean
            Return c = "{"c Or c = FULLWIDTH_LEFT_CURLY_BRACKET
        End Function

        Friend Shared Function IsRightCurlyBracket(c As Char) As Boolean
            Return c = "}"c Or c = FULLWIDTH_RIGHT_CURLY_BRACKET
        End Function

        ''' <summary>
        ''' Determines if the unicode character is a colon character.
        ''' </summary>
        ''' <param name="c">The unicode character.</param>
        ''' <returns>A boolean value set to True if character is a colon character.</returns>
        Public Shared Function IsColon(c As Char) As Boolean
            Return c = ":"c Or c = FULLWIDTH_COLON
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
            Return c = "#"c Or c = FULLWIDTH_NUMBER_SIGN
        End Function

        ''' <summary>
        ''' Determines if the Unicode character can be the starting character of a Visual Basic identifier.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character can be part of a valid start character in an identifier.</returns>
        Public Shared Function IsIdentifierStartCharacter(c As Char) As Boolean
            'TODO: make easy cases fast (or check if they already are)
            Dim CharacterProperties As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)

            Return IsPropAlpha(CharacterProperties) Or IsPropLetterDigit(CharacterProperties) Or
            IsPropConnectorPunctuation(CharacterProperties)
        End Function

        Private Const ascw_0 As Integer = AscW("0"c)
        Private Const ascw_UA As Integer = 10 - AscW("A"c)
        Private Const ascw_LA As Integer = 10 - AscW("a"c)

        ' TODO: replace CByte with something faster.
        Friend Shared Function IntegralLiteralCharacterValue(Digit As Char) As Byte
            If IsFullWidth(Digit) Then Digit = MakeHalfWidth(Digit)
            Dim u As Integer = AscW(Digit)
            If IsDecimalDigit(Digit) Then
                Return CByte(u - ascw_0)
            ElseIf Digit >= "A"c And Digit <= "F"c Then
                Return CByte(u + ascw_UA)
            Else
                Debug.Assert(Digit >= "a"c And Digit <= "f"c, "Surprising digit.")
                Return CByte(u + ascw_LA)
            End If
        End Function

        Friend Shared Function BeginsBaseLiteral(c As Char) As Boolean
            Return (c = "H"c Or c = "O"c Or c = "h"c Or c = "o"c) OrElse
                    (IsFullWidth(c) AndAlso
                     (c = FULLWIDTH_LATIN_CAPITAL_LETTER_H Or c = FULLWIDTH_LATIN_CAPITAL_LETTER_O Or
                      c = FULLWIDTH_LATIN_SMALL_LETTER_H Or c = FULLWIDTH_LATIN_SMALL_LETTER_O))
        End Function

        Private Shared ReadOnly s_isIDChar As Boolean() =
        {
            False, False, False, False, False, False, False, False, False, False,
            False, False, False, False, False, False, False, False, False, False,
            False, False, False, False, False, False, False, False, False, False,
            False, False, False, False, False, False, False, False, False, False,
            False, False, False, False, False, False, False, False, True, True,
            True, True, True, True, True, True, True, True, False, False,
            False, False, False, False, False, True, True, True, True, True,
            True, True, True, True, True, True, True, True, True, True,
            True, True, True, True, True, True, True, True, True, True,
            True, False, False, False, False, True, False, True, True, True,
            True, True, True, True, True, True, True, True, True, True,
            True, True, True, True, True, True, True, True, True, True,
            True, True, True, False, False, False, False, False
        }

        Friend Shared Function IsNarrowIdentifierCharacter(c As UInt16) As Boolean
            Return s_isIDChar(c)
            'Select Case c
            '    Case 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
            '        21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
            '        41, 42, 43, 44, 45, 46, 47
            '        Return False
            '    Case 48, 49, 50, 51, 52, 53, 54, 55, 56, 57
            '        Return True
            '    Case 58, 59, 60, 61, 62, 63, 64
            '        Return False
            '    Case 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
            '        81, 82, 83, 84, 85, 86, 87, 88, 89, 90
            '        Return True
            '    Case 91, 92, 93, 94
            '        Return False
            '    Case 95
            '        Return True
            '    Case 96
            '        Return False
            '    Case 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
            '        111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122
            '        Return True
            'End Select
            Return False
        End Function

        ''' <summary>
        ''' Determines if the Unicode character can be a part of a Visual Basic identifier.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character can be part of a valid identifier.</returns>
        Public Shared Function IsIdentifierPartCharacter(c As Char) As Boolean
            If c < ChrW(128) Then
                Return IsNarrowIdentifierCharacter(Convert.ToUInt16(c))
            End If

            Return IsWideIdentifierCharacter(c)
        End Function

        ''' <summary>
        ''' Determines if the name is a valid identifier.
        ''' </summary>
        ''' <param name="name">The identifier name.</param>
        ''' <returns>A boolean value set to True if name is valid identifier.</returns>
        Public Shared Function IsValidIdentifier(name As String) As Boolean
            If String.IsNullOrEmpty(name) Then
                Return False
            End If

            If Not IsIdentifierStartCharacter(name(0)) Then
                Return False
            End If

            Dim nameLength As Integer = name.Length
            For i As Integer = 1 To nameLength - 1 ' NB: start at 1
                If Not IsIdentifierPartCharacter(name(i)) Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Creates a half width form Unicode character string. 
        ''' </summary>
        ''' <param name="text">The text representing the original identifier.  This can be in full width or half width Unicode form.  </param>
        ''' <returns>A string representing the text in a half width Unicode form.</returns>
        Public Shared Function MakeHalfWidthIdentifier(text As String) As String
            If text Is Nothing Then
                Return text
            End If

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
            Dim CharacterProperties As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)

            Return IsPropAlphaNumeric(CharacterProperties) OrElse
                IsPropLetterDigit(CharacterProperties) OrElse
                IsPropConnectorPunctuation(CharacterProperties) OrElse
                IsPropCombining(CharacterProperties) OrElse
                IsPropOtherFormat(CharacterProperties)
        End Function

        Friend Shared Function BeginsExponent(c As Char) As Boolean
            Return c = "E"c Or c = "e"c Or c = FULLWIDTH_LATIN_CAPITAL_LETTER_E Or c = FULLWIDTH_LATIN_SMALL_LETTER_E
        End Function

        Friend Shared Function IsOctalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "7"c) Or (c >= FULLWIDTH_DIGIT_ZERO And c <= FULLWIDTH_DIGIT_SEVEN)
        End Function

        Friend Shared Function IsDecimalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "9"c) Or (c >= FULLWIDTH_DIGIT_ZERO And c <= FULLWIDTH_DIGIT_NINE)
        End Function

        Friend Shared Function IsHexDigit(c As Char) As Boolean
            Return IsDecimalDigit(c) Or
                    (c >= "a"c And c <= "f"c) Or (c >= "A"c And c <= "F"c) Or
                    (c >= FULLWIDTH_LATIN_SMALL_LETTER_A And c <= FULLWIDTH_LATIN_SMALL_LETTER_F) OrElse
                    (c >= FULLWIDTH_LATIN_CAPITAL_LETTER_A And c <= FULLWIDTH_LATIN_CAPITAL_LETTER_F)
        End Function

        Friend Shared Function IsDateSeparatorCharacter(c As Char) As Boolean
            Return c = "/"c Or c = "-"c Or c = FULLWIDTH_SOLIDUS Or c = FULLWIDTH_HYPHEN_MINUS
        End Function

        Friend Shared ReadOnly DaysToMonth365() As Integer = New Integer(13 - 1) {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365}
        Friend Shared ReadOnly DaysToMonth366() As Integer = New Integer(13 - 1) {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366}

        Friend Shared Function IsLetterC(ch As Char) As Boolean
            Return _
                ch = "c"c Or ch = "C"c Or ch = FULLWIDTH_LATIN_CAPITAL_LETTER_C Or ch = FULLWIDTH_LATIN_SMALL_LETTER_C
        End Function

        ''' <summary>
        ''' matches one char or another.
        ''' Typical usage is for matching lowercase and uppercase.
        ''' </summary>
        Friend Shared Function MatchOneOrAnother(ch As Char, one As Char, another As Char) As Boolean
            Return ch = one Or ch = another
        End Function

        ''' <summary>
        ''' matches one char or another.
        ''' it will try normal width and then fullwidth variations.
        ''' Typical usage is for matching lowercase and uppercase.
        ''' </summary>
        Friend Shared Function MatchOneOrAnotherOrFullwidth(ch As Char, one As Char, another As Char) As Boolean
            Debug.Assert(IsHalfWidth(one))
            Debug.Assert(IsHalfWidth(another))

            If IsFullWidth(ch) Then
                ch = MakeHalfWidth(ch)
            End If
            Return ch = one Or ch = another
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
            If IsHalfWidth(c) Then
                Return MakeFullWidth(c)
            End If

            Return c
        End Function
    End Class

End Namespace
