' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On
Option Infer On

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Module XmlCharacterGlobalHelpers
        Friend Function isNameChar(ch As Char) As Boolean
            ' TODO: which of the following is correct?
            Return XmlCharType.IsNameCharXml4e(ch)
            'Return XmlCharType.IsNameSingleChar(ch)
        End Function

        Friend Function isStartNameChar(ch As Char) As Boolean
            ' TODO: which of the following is correct?
            Return XmlCharType.IsStartNameCharXml4e(ch)
            'Return XmlCharType.IsStartNameSingleChar(ch)
        End Function
        Friend Function isValidUtf16(wh As Char) As Boolean
            Return XmlCharType.InRange(wh, ChrW(&H20S), ChrW(&HFFFDS)) OrElse XmlCharType.IsCharData(wh)
        End Function
        Friend Function HexToUTF16(pwcText As StringBuilder) As Scanner.XmlCharResult
            Debug.Assert(pwcText IsNot Nothing)

            Dim ulCode As UInteger
            If TryHexToUnicode(pwcText, ulCode) Then

                If ValidateXmlChar(ulCode) Then
                    Return UnicodeToUTF16(ulCode)
                End If
            End If
            Return Nothing
        End Function

        Friend Function TryHexToUnicode(pwcText As StringBuilder, ByRef pulCode As UInteger) As Boolean
            Debug.Assert(pwcText IsNot Nothing)

            Dim ulCode As UInteger = 0
            Dim wch As Char

            Dim n = pwcText.Length - 1
            For i = 0 To n

                wch = pwcText(i)

                If XmlCharType.InRange(wch, "0"c, "9"c) Then
                    ulCode = (ulCode * 16UI) + CUInt(AscW(wch)) - CUInt(AscW("0"c))

                ElseIf XmlCharType.InRange(wch, "a"c, "f"c) Then
                    ulCode = (ulCode * 16UI) + 10UI + CUInt(AscW(wch)) - CUInt(AscW("a"c))

                ElseIf XmlCharType.InRange(wch, "A"c, "F"c) Then
                    ulCode = (ulCode * 16UI) + 10UI + CUInt(AscW(wch)) - CUInt(AscW("A"c))
                Else
                    Return False
                End If

                If ulCode > &H10FFFF Then
                    ' // overflow
                    Return False
                End If

            Next

            pulCode = CUInt(ulCode)
            Return True
        End Function

        Friend Function DecToUTF16(pwcText As StringBuilder) As Scanner.XmlCharResult
            Debug.Assert(pwcText IsNot Nothing)
            Dim ulCode As UShort

            If TryDecToUnicode(pwcText, ulCode) Then
                If ValidateXmlChar(ulCode) Then
                    Return UnicodeToUTF16(ulCode)
                End If
            End If
            Return Nothing
        End Function

        Friend Function TryDecToUnicode(
            pwcText As StringBuilder,
            ByRef pulCode As UShort
        ) As Boolean
            Debug.Assert(pwcText IsNot Nothing)

            Dim ulCode As Integer = 0
            Dim wch As Char

            Dim n = pwcText.Length - 1
            For i = 0 To n

                wch = pwcText(i)

                If XmlCharType.InRange(wch, "0"c, "9"c) Then
                    ulCode = (ulCode * 10) + AscW(wch) - AscW("0"c)
                Else
                    Return False
                End If

                If ulCode > &H10FFFF Then
                    ' // overflow

                    Return False
                End If
            Next

            pulCode = CUShort(ulCode)
            Return True
        End Function

        Private Function ValidateXmlChar(ulCode As UInteger) As Boolean
            If (ulCode < &HD800 AndAlso (ulCode > &H1F OrElse XmlCharType.IsWhiteSpace(Convert.ToChar(ulCode)))) _
                OrElse (ulCode < &HFFFE AndAlso ulCode > &HDFFF) _
                OrElse (ulCode < &H110000 AndAlso ulCode > &HFFFF) Then

                Return True
            End If
            Return False
        End Function

        Private Function UnicodeToUTF16(ulCode As UInteger) As Scanner.XmlCharResult
            If ulCode > &HFFFF Then

                Return New Scanner.XmlCharResult( _
                    Convert.ToChar(&HD7C0US + (ulCode >> 10US)), _
                    Convert.ToChar(&HDC00US Or (ulCode And &H3FFUS)) _
                    )
            Else
                Return New Scanner.XmlCharResult(Convert.ToChar(ulCode))
            End If
        End Function

        Friend Function UTF16ToUnicode(ch As Scanner.XmlCharResult) As Integer
            Select Case ch.Length
                Case 1
                    Return Convert.ToInt32(ch.Char1)
                Case 2
                    Debug.Assert(Convert.ToInt32(ch.Char1) >= &HD800 AndAlso Convert.ToInt32(ch.Char1) <= &HDBFF AndAlso
                                 Convert.ToInt32(ch.Char2) >= &HDC00 AndAlso Convert.ToInt32(ch.Char2) <= &HDFFF)
                    Return (Convert.ToInt32(ch.Char1) - &HD800) << 10 + (Convert.ToInt32(ch.Char2) - &HDC00) + &H10000
            End Select
            Return 0
        End Function
    End Module
End Namespace
