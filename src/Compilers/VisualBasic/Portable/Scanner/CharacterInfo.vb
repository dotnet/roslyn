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
            Return Convert.ToChar(Convert.ToUInt16(c) + _fullwidth)
        End Function

        Friend Shared Function IsHalfWidth(c As Char) As Boolean
            Return c >= ChrW(&H21S) AndAlso c <= ChrW(&H7ES)
        End Function

        '// MakeHalfWidth - Converts a full-width character to half-width
        Friend Shared Function MakeHalfWidth(c As Char) As Char
            Debug.Assert(IsFullWidth(c))

            Return Convert.ToChar(Convert.ToUInt16(c) - _fullwidth)
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
            Return (SPACE = c) OrElse (_TAB_ = c) OrElse (c > ChrW(128) AndAlso IsWhitespaceNotAscii(c))
        End Function

        ''' <summary>
        ''' Determines if Unicode character represents a XML whitespace.
        ''' </summary>
        ''' <param name="c">The unicode character</param>
        ''' <returns>A boolean value set to True if character represents XML whitespace.</returns>
        Public Shared Function IsXmlWhitespace(c As Char) As Boolean
            Return (SPACE = c) OrElse (_TAB_ = c) OrElse (c > ChrW(128) AndAlso XmlCharType.IsWhiteSpace(c))
        End Function

        Friend Shared Function IsWhitespaceNotAscii(ch As Char) As Boolean
            Select Case ch
                Case NO_BREAK_SPACE, IDEOGRAPHIC_SPACE, ChrW(&H2000S) To ChrW(&H200BS)
                    Return True

                Case Else
                    Return CharUnicodeInfo.GetUnicodeCategory(ch) = UnicodeCategory.SpaceSeparator
            End Select
        End Function

        Private Const _fullwidth = CInt(&HFF00L - &H0020L)
        ''' <summary>
        ''' Character Tabulation
        ''' </summary>
        Friend Const _TAB_ As Char = ChrW(&H0009)
        ''' <summary>
        ''' Line Feed
        ''' </summary>
        Friend Const _LF_ As Char = ChrW(&H000A)
        ''' <summary>
        ''' Carraiage Return
        ''' </summary>
        Friend Const _CR_ As Char = ChrW(&H000D)
        Friend Const SPACE As Char = ChrW(&H0020)
        Friend Const NO_BREAK_SPACE As Char = ChrW(&H00A0)
        Friend Const IDEOGRAPHIC_SPACE As Char = ChrW(&H3000)
        ''' <summary>
        ''' Line Separator
        ''' </summary>
        Friend Const _LS_ As Char = ChrW(&H2028)
        Friend Const PARAGRAPH_SEPARATOR As Char = ChrW(&H2029)
        Friend Const NEXT_LINE As Char = ChrW(&H0085)

        Friend Class QUOTATION_MARK
            ''' <summary>
            ''' (‘) Left Single Quotation mark.
            ''' </summary>
            Friend Const LS As Char = ChrW(&H2018)
            ''' <summary>
            ''' (’) Right Single Quotation mark.
            ''' </summary>
            Friend Const RS As Char = ChrW(&H2019)
            ''' <summary>
            ''' (“) Left Double Quotation mark.
            ''' </summary>
            Friend Const LD As Char = ChrW(&H201C)
            ''' <summary>
            ''' (”) Right Double Quotation mark.
            ''' </summary>
            Friend Const RD As Char = ChrW(&H201D)

        End Class


        Friend Class FULLWIDTH
            Friend Const APOSTROPHE           = ChrW(_fullwidth + AscW("'"c))         REM ＇
            Friend Const QUOTATION_MARK       = ChrW(_fullwidth + AscW(""""c))        REM ＂

            Friend Const LOW_LINE             = ChrW(_fullwidth + AscW("_"c))         REM ＿
            Friend Const COLON                = ChrW(_fullwidth + AscW(":"c))         REM ：
            Friend Const SOLIDUS              = ChrW(_fullwidth + AscW("/"c))         REM ／
            Friend Const HYPHEN_MINUS         = ChrW(_fullwidth + AscW("-"c))         REM －
            Friend Const PLUS_SIGN            = ChrW(_fullwidth + AscW("+"c))         REM ＋
            Friend Const NUMBER_SIGN          = ChrW(_fullwidth + AscW("#"c))         REM ＃

            Friend Const EQUALS_SIGN          = ChrW(_fullwidth + AscW("="c))         REM ＝
            Friend Const LESS_THAN_SIGN       = ChrW(_fullwidth + AscW("<"c))         REM ＜
            Friend Const GREATER_THAN_SIGN    = ChrW(_fullwidth + AscW(">"c))         REM ＞
            Friend Const LEFT_PARENTHESIS     = ChrW(_fullwidth + AscW("("c))         REM （
            Friend Const LEFT_SQUARE_BRACKET  = ChrW(_fullwidth + AscW("["c))         REM ［
            Friend Const RIGHT_SQUARE_BRACKET = ChrW(_fullwidth + AscW("]"c))         REM ］
            Friend Const LEFT_CURLY_BRACKET   = ChrW(_fullwidth + AscW("{"c))         REM ｛
            Friend Const RIGHT_CURLY_BRACKET  = ChrW(_fullwidth + AscW("}"c))         REM ｝
            Friend Const AMPERSAND            = ChrW(_fullwidth + AscW("&"c))         REM ＆
            Friend Const DOLLAR_SIGN          = ChrW(_fullwidth + AscW("$"c))         REM ＄
            Friend Const QUESTION_MARK        = ChrW(_fullwidth + AscW("?"c))         REM ？
            Friend Const FULL_STOP            = ChrW(_fullwidth + AscW("."c))         REM ．
            Friend Const COMMA                = ChrW(_fullwidth + AscW(","c))         REM ，
            Friend Const PERCENT_SIGN         = ChrW(_fullwidth + AscW("%"c))         REM ％
            Friend Class DIGIT
                Friend Const ZERO = ChrW(_fullwidth + AscW("0"c))            REM ０
                Friend Const SEVEN = ChrW(_fullwidth + AscW("7"c))            REM ７
                Friend Const NINE = ChrW(_fullwidth + AscW("9"c))            REM ９
            End Class

            Friend Class LATIN
                Friend Class CAPITAL
                    Friend Const LETTER_H = ChrW(_fullwidth + AscW("H"c)) REM Ｈ
                    Friend Const LETTER_O = ChrW(_fullwidth + AscW("O"c)) REM Ｏ
                    Friend Const LETTER_E = ChrW(_fullwidth + AscW("E"c)) REM Ｅ
                    Friend Const LETTER_A = ChrW(_fullwidth + AscW("A"c)) REM Ａ
                    Friend Const LETTER_F = ChrW(_fullwidth + AscW("F"c)) REM Ｆ
                    Friend Const LETTER_C = ChrW(_fullwidth + AscW("C"c)) REM Ｃ
                    Friend Const LETTER_P = ChrW(_fullwidth + AscW("P"c)) REM Ｐ
                    Friend Const LETTER_M = ChrW(_fullwidth + AscW("M"c)) REM Ｍ
                End Class
                Friend Class SMALL
                    Friend Const LETTER_H = ChrW(_fullwidth + AscW("h"c))   REM ｈ
                    Friend Const LETTER_O = ChrW(_fullwidth + AscW("o"c))   REM ｏ
                    Friend Const LETTER_E = ChrW(_fullwidth + AscW("e"c))   REM ｅ
                    Friend Const LETTER_A = ChrW(_fullwidth + AscW("a"c))   REM ａ
                    Friend Const LETTER_F = ChrW(_fullwidth + AscW("f"c))   REM ｆ
                    Friend Const LETTER_C = ChrW(_fullwidth + AscW("c"c))   REM ｃ
                    Friend Const LETTER_P = ChrW(_fullwidth + AscW("p"c))   REM ｐ
                    Friend Const LETTER_M = ChrW(_fullwidth + AscW("m"c))   REM ｍ
                End Class
            End Class

            Friend Class [STRING]
                Friend Const LEFT_PARENTHESIS$ = FULLWIDTH.LEFT_PARENTHESIS
                Friend Const RIGHT_PARENTHESIS$   = ChrW(_fullwidth + AscW(")"c))
                Friend Const LEFT_CURLY_BRACKET$  = FULLWIDTH.LEFT_CURLY_BRACKET
                Friend Const RIGHT_CURLY_BRACKET$ = FULLWIDTH.RIGHT_CURLY_BRACKET
                Friend Const FULL_STOP$           = FULLWIDTH.FULL_STOP
                Friend Const COMMA$               = FULLWIDTH.COMMA
                Friend Const EQUALS_SIGN$         = FULLWIDTH.EQUALS_SIGN
                Friend Const PLUS_SIGN$           = FULLWIDTH.PLUS_SIGN
                Friend Const HYPHEN_MINUS$        = FULLWIDTH.HYPHEN_MINUS
                Friend Const ASTERISK$            = ChrW(_fullwidth + AscW("*"c))
                Friend Const SOLIDUS$             = FULLWIDTH.SOLIDUS
                Friend Const REVERSE_SOLIDUS$     = ChrW(_fullwidth + AscW("\"c))
                Friend Const COLON$               = FULLWIDTH.COLON
                Friend Const CIRCUMFLEX_ACCENT$   = ChrW(_fullwidth + AscW("^"c))
                Friend Const AMPERSAND$           = FULLWIDTH.AMPERSAND
                Friend Const NUMBER_SIGN$         = FULLWIDTH.NUMBER_SIGN
                Friend Const EXCLAMATION_MARK$    = ChrW(_fullwidth + AscW("!"c))
                Friend Const QUESTION_MARK$       = FULLWIDTH.QUESTION_MARK
                Friend Const COMMERCIAL_AT$       = ChrW(_fullwidth + AscW("@"c))
                Friend Const LESS_THAN_SIGN$      = FULLWIDTH.LESS_THAN_SIGN
                Friend Const GREATER_THAN_SIGN$   = FULLWIDTH.GREATER_THAN_SIGN
            End Class
        End Class

        ''' <summary>
        ''' Determines if the Unicode character is a newline character.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character is a newline character.</returns>
        Public Shared Function IsNewLine(c As Char) As Boolean
            Return _CR_ = c OrElse _LF_ = c OrElse (c >= NEXT_LINE AndAlso (NEXT_LINE = c OrElse _LS_ = c OrElse PARAGRAPH_SEPARATOR = c))
        End Function

        Friend Shared Function IsSingleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ', we also check for Unicode
            ' // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = "'"c OrElse (c >= QUOTATION_MARK.LS AndAlso (c = FULLWIDTH.APOSTROPHE Or c = QUOTATION_MARK.LS Or c = QUOTATION_MARK.RS))
        End Function

        Friend Shared Function IsDoubleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ", we also check for Unicode
            ' // LEFT DOUBLE QUOTATION MARK and RIGHT DOUBLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = """"c OrElse (c >= QUOTATION_MARK.LD AndAlso (c = FULLWIDTH.QUOTATION_MARK Or c = QUOTATION_MARK.LD Or c = QUOTATION_MARK.RD))
        End Function

        Friend Shared Function IsLeftCurlyBracket(c As Char) As Boolean
            Return c = "{"c OrElse c = FULLWIDTH.LEFT_CURLY_BRACKET
        End Function

        Friend Shared Function IsRightCurlyBracket(c As Char) As Boolean
            Return c = "}"c OrElse c = FULLWIDTH.RIGHT_CURLY_BRACKET
        End Function

        ''' <summary>
        ''' Determines if the unicode character is a colon character.
        ''' </summary>
        ''' <param name="c">The unicode character.</param>
        ''' <returns>A boolean value set to True if character is a colon character.</returns>
        Public Shared Function IsColon(c As Char) As Boolean
            Return c = ":"c OrElse c = FULLWIDTH.COLON
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
            Return c = "#"c OrElse c = FULLWIDTH.NUMBER_SIGN
        End Function

        ''' <summary>
        ''' Determines if the Unicode character can be the starting character of a Visual Basic identifier.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character can be part of a valid start charcater in an identifier.</returns>
        Public Shared Function IsIdentifierStartCharacter( c As Char) As Boolean
            'TODO: make easy cases fast (or check if they already are)
            Dim CharacterProperties As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)

            Return IsPropAlpha(CharacterProperties) OrElse
            IsPropLetterDigit(CharacterProperties) OrElse
            IsPropConnectorPunctuation(CharacterProperties)
        End Function

        ' TODO: replace CByte with something faster.
        Friend Shared Function IntegralLiteralCharacterValue( Digit As Char ) As Byte
            If IsFullWidth(Digit) Then Digit = MakeHalfWidth(Digit)
            Dim u As Integer = AscW(Digit)

            If IsDecimalDigit(Digit) Then Return CByte(u - AscW("0"c))
            If Digit >= "A"c AndAlso Digit <= "F"c Then Return CByte(u + (10 - AscW("A"c)))
            Debug.Assert(Digit >= "a"c AndAlso Digit <= "f"c, "Surprising digit.")
            Return CByte(u + (10 - AscW("a"c)))
        End Function

        Friend Shared Function BeginsBaseLiteral(c As Char) As Boolean
            Return (c = "H"c Or c = "O"c Or c = "h"c Or c = "o"c) OrElse
                    (IsFullWidth(c) AndAlso (c = FULLWIDTH.LATIN.CAPITAL.LETTER_H Or c = FULLWIDTH.LATIN.CAPITAL.LETTER_O Or c = FULLWIDTH.LATIN.SMALL.LETTER_H Or c = FULLWIDTH.LATIN.SMALL.LETTER_O))
        End Function

        Private Shared _IsIDChar As Boolean() =
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
            Return _IsIDChar(c)
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
            'Return False
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
            If String.IsNullOrEmpty(name) Then Return False
            If Not IsIdentifierStartCharacter(name(0)) Then Return False

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
            If text Is Nothing Then  Return text

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
            Return c = "E"c Or c = "e"c Or c = FULLWIDTH.LATIN.CAPITAL.LETTER_E Or c = FULLWIDTH.LATIN.SMALL.LETTER_E
        End Function

        Friend Shared Function IsOctalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "7"c) Or (c >= FULLWIDTH.DIGIT.ZERO And c <= FULLWIDTH.DIGIT.SEVEN)
        End Function

        Friend Shared Function IsDecimalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "9"c) Or (c >= FULLWIDTH.DIGIT.ZERO And c <= FULLWIDTH.DIGIT.NINE)
        End Function

        Friend Shared Function IsHexDigit(c As Char) As Boolean
            Return IsDecimalDigit(c) OrElse
                    (c >= "a"c And c <= "f"c) OrElse
                    (c >= "A"c And c <= "F"c) OrElse
                    (c >= FULLWIDTH.LATIN.SMALL.LETTER_A And c <= FULLWIDTH.LATIN.SMALL.LETTER_F) OrElse
                    (c >= FULLWIDTH.LATIN.CAPITAL.LETTER_A And c <= FULLWIDTH.LATIN.CAPITAL.LETTER_F)
        End Function

        Friend Shared Function IsDateSeparatorCharacter(c As Char) As Boolean
            Return c = "/"c Or c = "-"c Or c = FULLWIDTH.SOLIDUS Or c = FULLWIDTH.HYPHEN_MINUS
        End Function

        Friend Shared ReadOnly DaysToMonth365() As Integer = New Integer(13 - 1) {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365}
        Friend Shared ReadOnly DaysToMonth366() As Integer = New Integer(13 - 1) {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366}

        Friend Shared Function IsLetterC(ch As Char) As Boolean
            Return _
                ch = "c"c Or ch = "C"c Or ch = FULLWIDTH.LATIN.CAPITAL.LETTER_C Or ch = FULLWIDTH.LATIN.SMALL.LETTER_C
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

            If IsFullWidth(ch) Then ch = MakeHalfWidth(ch)
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
            If IsHalfWidth(c) Then Return MakeFullWidth(c)
            Return c
        End Function
    End Class

End Namespace
