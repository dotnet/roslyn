' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Return Convert.ToChar(Convert.ToUInt16(c) + (&HFF00US - &H20US))
        End Function

        Friend Shared Function IsHalfWidth(c As Char) As Boolean
            Return c >= ChrW(&H21S) AndAlso c <= ChrW(&H7ES)
        End Function

        '// MakeHalfWidth - Converts a full-width character to half-width
        Friend Shared Function MakeHalfWidth(c As Char) As Char
            Debug.Assert(IsFullWidth(c))

            Return Convert.ToChar(Convert.ToUInt16(c) - (&HFF00US - &H20US))
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
            Return (UCH_SPACE = c) OrElse (UCH_TAB = c) OrElse (c > ChrW(128) AndAlso IsWhitespaceNotAscii(c))
        End Function

        ''' <summary>
        ''' Determines if Unicode character represents a XML whitespace.
        ''' </summary>
        ''' <param name="c">The unicode character</param>
        ''' <returns>A boolean value set to True if character represents XML whitespace.</returns>
        Public Shared Function IsXmlWhitespace(c As Char) As Boolean
            Return (UCH_SPACE = c) OrElse (UCH_TAB = c) OrElse (c > ChrW(128) AndAlso XmlCharType.IsWhiteSpace(c))
        End Function

        Friend Shared Function IsWhitespaceNotAscii(ch As Char) As Boolean
            Select Case ch
                Case UCH_NBSP, UCH_IDEOSP, ChrW(&H2000S) To ChrW(&H200BS)
                    Return True

                Case Else
                    Return CharUnicodeInfo.GetUnicodeCategory(ch) = UnicodeCategory.SpaceSeparator
            End Select
        End Function

        '//        ---------         ------------  ----------------------------------
        '//           ID                Code point        Unicode character name
        '//        ---------         ------------  ----------------------------------
        Friend Const UCH_NULL As Char = ChrW(&H0S)      '// NULL
        Friend Const UCH_TAB As Char = ChrW(&H9S)       '// HORIZONTAL TABULATION
        Friend Const UCH_LF As Char = ChrW(&HAS)        '// LINE FEED
        Friend Const UCH_CR As Char = ChrW(&HDS)        '// CARRIAGE RETURN
        Friend Const UCH_SPACE As Char = ChrW(&H20S)    '// SPACE
        Friend Const UCH_NBSP As Char = ChrW(&HA0S)     '// NO-BREAK SPACE
        Friend Const UCH_IDEOSP As Char = ChrW(&H3000S) '// IDEOGRAPHIC SPACE
        Friend Const UCH_LS As Char = ChrW(&H2028S)     '// LINE SEPARATOR
        Friend Const UCH_PS As Char = ChrW(&H2029S)     '// PARAGRAPH SEPARATOR
        Friend Const UCH_NEL As Char = ChrW(&H85S)      '// NEXT LINE

        Friend Const DWCH_SQ As Char = ChrW(&HFF07)      '// DW single quote

        Friend Const DWCH_LSMART_Q As Char = ChrW(&H2018S)      '// DW left single smart quote
        Friend Const DWCH_RSMART_Q As Char = ChrW(&H2019S)      '// DW right single smart quote

        Friend Const DWCH_LSMART_DQ As Char = ChrW(&H201CS)      '// DW left single smart quote
        Friend Const DWCH_RSMART_DQ As Char = ChrW(&H201DS)      '// DW right single smart quote

        Friend Const DWCH_DQ As Char = ChrW(AscW(""""c) + (&HFF00US - &H20US))      '// DW double quote 

        Friend Const FULLWIDTH_0 As Char = ChrW(AscW("0"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_7 As Char = ChrW(AscW("7"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_9 As Char = ChrW(AscW("9"c) + (&HFF00US - &H20US))

        Friend Const FULLWIDTH_LC As Char = ChrW(AscW("_"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_COL As Char = ChrW(AscW(":"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_SLASH As Char = ChrW(AscW("/"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_DASH As Char = ChrW(AscW("-"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_HASH As Char = ChrW(AscW("#"c) + (&HFF00US - &H20US))

        Friend Const FULLWIDTH_EQ As Char = ChrW(AscW("="c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LT As Char = ChrW(AscW("<"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_GT As Char = ChrW(AscW(">"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LPAREN As Char = ChrW(AscW("("c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_RPAREN As Char = ChrW(AscW(")"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LBR As Char = ChrW(AscW("["c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_RBR As Char = ChrW(AscW("]"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_AMP As Char = ChrW(AscW("&"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Q As Char = ChrW(AscW("?"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_AT As Char = ChrW(AscW("@"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_DOT As Char = ChrW(AscW("."c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_PERCENT As Char = ChrW(AscW("%"c) + (&HFF00US - &H20US))

        Friend Const FULLWIDTH_Hh As Char = ChrW(AscW("H"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Hl As Char = ChrW(AscW("h"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Oh As Char = ChrW(AscW("O"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Ol As Char = ChrW(AscW("o"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Eh As Char = ChrW(AscW("E"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_El As Char = ChrW(AscW("e"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Ah As Char = ChrW(AscW("A"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Al As Char = ChrW(AscW("a"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Fh As Char = ChrW(AscW("F"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Fl As Char = ChrW(AscW("f"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Ch As Char = ChrW(AscW("C"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_cl As Char = ChrW(AscW("c"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Ph As Char = ChrW(AscW("P"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_pl As Char = ChrW(AscW("p"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Mh As Char = ChrW(AscW("M"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_ml As Char = ChrW(AscW("m"c) + (&HFF00US - &H20US))

        Friend Const FULLWIDTH_LPAREN_STR As String = ChrW(AscW("("c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_RPAREN_STR As String = ChrW(AscW(")"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LBRC_STR As String = ChrW(AscW("{"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_RBRC_STR As String = ChrW(AscW("}"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LBRK_STR As String = ChrW(AscW("["c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_RBRK_STR As String = ChrW(AscW("]"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_DOT_STR As String = ChrW(AscW("."c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_COMMA_STR As String = ChrW(AscW(","c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_EQ_STR As String = ChrW(AscW("="c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_PLUS_STR As String = ChrW(AscW("+"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_MINUS_STR As String = ChrW(AscW("-"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_MUL_STR As String = ChrW(AscW("*"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_DIV_STR As String = ChrW(AscW("/"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_IDIV_STR As String = ChrW(AscW("\"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_COL_STR As String = ChrW(AscW(":"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_PWR_STR As String = ChrW(AscW("^"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_AMP_STR As String = ChrW(AscW("&"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_HASH_STR As String = ChrW(AscW("#"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_EXCL_STR As String = ChrW(AscW("!"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_Q_STR As String = ChrW(AscW("?"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_AT_STR As String = ChrW(AscW("@"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_GT_STR As String = ChrW(AscW(">"c) + (&HFF00US - &H20US))
        Friend Const FULLWIDTH_LT_STR As String = ChrW(AscW("<"c) + (&HFF00US - &H20US))

        ''' <summary>
        ''' Determines if the Unicode character is a newline character.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character is a newline character.</returns>
        Public Shared Function IsNewLine(c As Char) As Boolean
            Return UCH_CR = c OrElse UCH_LF = c OrElse (c >= UCH_NEL AndAlso (UCH_NEL = c OrElse UCH_LS = c OrElse UCH_PS = c))
        End Function

        Friend Shared Function IsSingleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ', we also check for Unicode
            ' // LEFT SINGLE QUOTATION MARK and RIGHT SINGLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = "'"c OrElse (c >= DWCH_LSMART_Q AndAlso (c = DWCH_SQ Or c = DWCH_LSMART_Q Or c = DWCH_RSMART_Q))
        End Function

        Friend Shared Function IsDoubleQuote(c As Char) As Boolean
            ' // Besides the half width and full width ", we also check for Unicode
            ' // LEFT DOUBLE QUOTATION MARK and RIGHT DOUBLE QUOTATION MARK because
            ' // IME editors paste them in. This isn't really technically correct
            ' // because we ignore the left-ness or right-ness, but see VS 170991
            Return c = """"c OrElse (c >= DWCH_LSMART_DQ AndAlso (c = DWCH_DQ Or c = DWCH_LSMART_DQ Or c = DWCH_RSMART_DQ))
        End Function

        ''' <summary>
        ''' Determines if the unicode character is a colon character.
        ''' </summary>
        ''' <param name="c">The unicode character.</param>
        ''' <returns>A boolean value set to True if character is a colon character.</returns>
        Public Shared Function IsColon(c As Char) As Boolean
            Return c = ":"c OrElse c = FULLWIDTH_COL
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
            Return c = "#"c OrElse c = FULLWIDTH_HASH
        End Function

        ''' <summary>
        ''' Determines if the Unicode character can be the starting character of a Visual Basic identifier.
        ''' </summary>
        ''' <param name="c">The Unicode character.</param>
        ''' <returns>A boolean value set to True if character can be part of a valid start charcater in an identifier.</returns>
        Public Shared Function IsIdentifierStartCharacter(
            c As Char
        ) As Boolean
            'TODO: make easy cases fast (or check if they already are)
            Dim CharacterProperties As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c)

            Return IsPropAlpha(CharacterProperties) OrElse
            IsPropLetterDigit(CharacterProperties) OrElse
            IsPropConnectorPunctuation(CharacterProperties)
        End Function

        ' TODO: replace CByte with something faster.
        Friend Shared Function IntegralLiteralCharacterValue(
            Digit As Char
        ) As Byte
            If IsFullWidth(Digit) Then
                Digit = MakeHalfWidth(Digit)
            End If
            Dim u As Integer = AscW(Digit)

            If IsDecimalDigit(Digit) Then
                Return CByte(u - AscW("0"c))
            ElseIf Digit >= "A"c AndAlso Digit <= "F"c Then
                Return CByte(u + (10 - AscW("A"c)))
            Else
                Debug.Assert(Digit >= "a"c AndAlso Digit <= "f"c, "Surprising digit.")
                Return CByte(u + (10 - AscW("a"c)))
            End If
        End Function

        Friend Shared Function BeginsBaseLiteral(c As Char) As Boolean
            Return (c = "H"c Or c = "O"c Or c = "h"c Or c = "o"c) OrElse
                    (IsFullWidth(c) AndAlso (c = FULLWIDTH_Hh Or c = FULLWIDTH_Oh Or c = FULLWIDTH_Hl Or c = FULLWIDTH_Ol))
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
            Return c = "E"c Or c = "e"c Or c = FULLWIDTH_Eh Or c = FULLWIDTH_El
        End Function

        Friend Shared Function IsOctalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "7"c) Or
                   (c >= FULLWIDTH_0 And c <= FULLWIDTH_7)
        End Function

        Friend Shared Function IsDecimalDigit(c As Char) As Boolean
            Return (c >= "0"c And c <= "9"c) Or
                   (c >= FULLWIDTH_0 And c <= FULLWIDTH_9)
        End Function

        Friend Shared Function IsHexDigit(c As Char) As Boolean
            Return IsDecimalDigit(c) OrElse
                    (c >= "a"c And c <= "f"c) OrElse
                    (c >= "A"c And c <= "F"c) OrElse
                    (c >= FULLWIDTH_Al And c <= FULLWIDTH_Fl) OrElse
                    (c >= FULLWIDTH_Ah And c <= FULLWIDTH_Fh)
        End Function

        Friend Shared Function IsDateSeparatorCharacter(c As Char) As Boolean
            Return c = "/"c Or c = "-"c Or c = FULLWIDTH_SLASH Or c = FULLWIDTH_DASH
        End Function

        Friend Shared ReadOnly DaysToMonth365() As Integer = New Integer(13 - 1) {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365}
        Friend Shared ReadOnly DaysToMonth366() As Integer = New Integer(13 - 1) {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366}

        Friend Shared Function IsLetterC(ch As Char) As Boolean
            Return _
                ch = "c"c Or ch = "C"c Or ch = FULLWIDTH_Ch Or ch = FULLWIDTH_cl
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