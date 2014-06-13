' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay

    ''' <summary>
    ''' Displays a value in the VisualBasic style.
    ''' </summary>
    ''' <seealso cref="T:Microsoft.CodeAnalysis.CSharp.Symbols.ObjectDisplay"/>
    Friend Module ObjectDisplay

        Const NullChar As Char = ChrW(0)
        Const Back As Char = ChrW(8)
        Const Cr As Char = ChrW(13)
        Const FormFeed As Char = ChrW(12)
        Const Lf As Char = ChrW(10)
        Const Tab As Char = ChrW(9)
        Const VerticalTab As Char = ChrW(11)

        ''' <summary>
        ''' Returns a string representation of an object of primitive type.
        ''' </summary>
        ''' <param name="obj">A value to display as a string.</param>
        ''' <param name="quoteStrings">Whether or not to quote string literals.</param>
        ''' <param name="useHexadecimalNumbers">Whether or not to display integral literals in hexadecimal.</param>
        ''' <returns>A string representation of an object of primitive type (or null if the type is not supported).</returns>
        ''' <remarks>
        ''' Handles <see cref="Boolean"/>, <see cref="String"/>, <see cref="Char"/>, <see cref="SByte"/>
        ''' <see cref="Byte"/>, <see cref="Short"/>, <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>,
        ''' <see cref="Long"/>, <see cref="ULong"/>, <see cref="Double"/>, <see cref="Single"/>, <see cref="Decimal"/>,
        ''' <see cref="Date"/>, and <c>Nothing</c>.
        ''' </remarks>
        Public Function FormatPrimitive(obj As Object, quoteStrings As Boolean, useHexadecimalNumbers As Boolean) As String
            If obj Is Nothing Then
                Return NullLiteral
            End If

            Dim type = obj.GetType()
            If type.GetTypeInfo().IsEnum Then
                type = [Enum].GetUnderlyingType(type)
            End If

            If type Is GetType(Integer) Then
                Return FormatLiteral(DirectCast(obj, Integer), useHexadecimalNumbers)
            End If

            If type Is GetType(String) Then
                Return FormatLiteral(DirectCast(obj, String), quoteStrings)
            End If

            If type Is GetType(Boolean) Then
                Return FormatLiteral(DirectCast(obj, Boolean))
            End If

            If type Is GetType(Char) Then
                Return FormatLiteral(DirectCast(obj, Char), quoteStrings, useHexadecimalNumbers)
            End If

            If type Is GetType(Byte) Then
                Return FormatLiteral(DirectCast(obj, Byte), useHexadecimalNumbers)
            End If

            If type Is GetType(Short) Then
                Return FormatLiteral(DirectCast(obj, Short), useHexadecimalNumbers)
            End If

            If type Is GetType(Long) Then
                Return FormatLiteral(DirectCast(obj, Long), useHexadecimalNumbers)
            End If

            If type Is GetType(Double) Then
                Return FormatLiteral(DirectCast(obj, Double))
            End If

            If type Is GetType(ULong) Then
                Return FormatLiteral(DirectCast(obj, ULong), useHexadecimalNumbers)
            End If

            If type Is GetType(UInteger) Then
                Return FormatLiteral(DirectCast(obj, UInteger), useHexadecimalNumbers)
            End If

            If type Is GetType(UShort) Then
                Return FormatLiteral(DirectCast(obj, UShort), useHexadecimalNumbers)
            End If

            If type Is GetType(SByte) Then
                Return FormatLiteral(DirectCast(obj, SByte), useHexadecimalNumbers)
            End If

            If type Is GetType(Single) Then
                Return FormatLiteral(DirectCast(obj, Single))
            End If

            If type Is GetType(Decimal) Then
                Return FormatLiteral(DirectCast(obj, Decimal))
            End If

            If type Is GetType(Date) Then
                Return FormatLiteral(DirectCast(obj, Date))
            End If

            Return Nothing
        End Function

        Friend ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Friend Function FormatLiteral(value As Boolean) As String
            Return If(value, "True", "False")
        End Function

        ''' <summary>
        ''' Formats string literal.
        ''' </summary>
        ''' <param name="value">Literal value.</param>
        ''' <param name="quote">True to double-quote the value. Also enables pretty-listing of non-printable characters using ChrW function and vb* constants.</param>
        ''' <param name="nonPrintableSubstitute">If specified non-printable characters are replaced by this character.</param>
        ''' <param name="useHexadecimalNumbers">Use hexadecimal numbers as arguments to ChrW functions.</param>
        Friend Function FormatLiteral(value As String, Optional quote As Boolean = True, Optional nonPrintableSubstitute As Char = Nothing, Optional useHexadecimalNumbers As Boolean = True) As String
            If value Is Nothing Then
                Throw New ArgumentNullException()
            End If

            Return FormatString(value, quote, nonPrintableSubstitute, useHexadecimalNumbers)
        End Function

        Friend Function FormatLiteral(c As Char, quote As Boolean, useHexadecimalNumbers As Boolean) As String
            Dim wellKnown = GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                Return wellKnown
            End If

            If Not IsPrintable(c) Then
                Dim codepoint = AscW(c)
                Return If(useHexadecimalNumbers, "ChrW(&H" & codepoint.ToString("X"), "ChrW(" & codepoint.ToString()) & ")"
            End If

            If quote Then
                Return """"c & EscapeQuote(c) & """"c & "c"
            Else
                Return c
            End If
        End Function

        Private Function EscapeQuote(c As Char) As String
            Return If(c = """", """""", c)
        End Function

        Friend Function FormatLiteral(value As SByte, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X2"), (CType(value, Integer)).ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As Byte, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & value.ToString("X2")
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As Short, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & (If(value >= 0, value.ToString("X"), (CType(value, Integer)).ToString("X8")))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As UShort, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As Integer, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As UInteger, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As Long, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X16"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As ULong, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X16"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Friend Function FormatLiteral(value As Double) As String
            Return value.ToString("R", CultureInfo.InvariantCulture)
        End Function

        Friend Function FormatLiteral(value As Single) As String
            Return value.ToString("R", CultureInfo.InvariantCulture)
        End Function

        Friend Function FormatLiteral(value As Decimal) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Friend Function FormatLiteral(value As DateTime) As String
            Return value.ToString("#M/d/yyyy hh:mm:ss tt#", CultureInfo.InvariantCulture)
        End Function

        Friend Function FormatString(str As String, quote As Boolean, nonPrintableSubstitute As Char, useHexadecimalNumbers As Boolean) As String
            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            For Each token As Integer In TokenizeString(str, quote, nonPrintableSubstitute, useHexadecimalNumbers)
                sb.Append(ChrW(token And &HFFFF)) ' lower 16 bits of token contains the Unicode char value
            Next

            Return pooledBuilder.ToStringAndFree()
        End Function

        Private Function Character(c As Char) As Integer
            Return (SymbolDisplayPartKind.StringLiteral << 16) Or AscW(c)
        End Function

        Private Function Identifier(c As Char) As Integer
            Return (SymbolDisplayPartKind.MethodName << 16) Or AscW(c)
        End Function

        Private Function Number(c As Char) As Integer
            Return (SymbolDisplayPartKind.NumericLiteral << 16) Or AscW(c)
        End Function

        Private Function Punctuation(c As Char) As Integer
            Return (SymbolDisplayPartKind.Punctuation << 16) Or AscW(c)
        End Function

        Private Function [Operator](c As Char) As Integer
            Return (SymbolDisplayPartKind.Operator << 16) Or AscW(c)
        End Function

        Private Function Space() As Integer
            Return (SymbolDisplayPartKind.Space << 16) Or AscW(" "c)
        End Function

        Private Function Quotes() As Integer
            Return (SymbolDisplayPartKind.StringLiteral << 16) Or AscW("""")
        End Function

        ' TODO: consider making "token" returned by this function a structure to abstract bit masking operations
        Friend Iterator Function TokenizeString(str As String, quote As Boolean, nonPrintableSubstitute As Char, useHexadecimalNumbers As Boolean) As IEnumerable(Of Integer)
            If str.Length = 0 Then
                If quote Then
                    Yield Quotes()
                    Yield Quotes()
                End If

                Return
            End If

            Dim startNewConcatenand = False
            Dim lastConcatenandWasQuoted = False
            Dim i = 0
            While i < str.Length
                Dim isFirst = (i = 0)
                Dim c = str(i)
                i += 1
                Dim wellKnown As String
                Dim isNonPrintable As Boolean
                Dim isCrLf As Boolean

                ' vbCrLf
                If c = Cr AndAlso i < str.Length AndAlso str(i) = Lf Then
                    wellKnown = "vbCrLf"
                    isNonPrintable = True
                    isCrLf = True
                    i += 1
                Else
                    wellKnown = GetWellKnownCharacterName(c)
                    isNonPrintable = wellKnown IsNot Nothing OrElse Not IsPrintable(c)
                    isCrLf = False
                End If

                If isNonPrintable Then
                    If nonPrintableSubstitute <> NullChar Then
                        Yield Character(nonPrintableSubstitute)

                        If isCrLf Then
                            Yield Character(nonPrintableSubstitute)
                        End If
                    ElseIf quote Then
                        If lastConcatenandWasQuoted Then
                            Yield Quotes()
                            lastConcatenandWasQuoted = False
                        End If

                        If Not isFirst Then
                            Yield Space()
                            Yield [Operator]("&"c)
                            Yield Space()
                        End If

                        If wellKnown IsNot Nothing Then
                            For Each e In wellKnown
                                Yield Identifier(e)
                            Next
                        Else
                            Yield Identifier("C"c)
                            Yield Identifier("h"c)
                            Yield Identifier("r"c)
                            Yield Identifier("W"c)
                            Yield Punctuation("("c)

                            If useHexadecimalNumbers Then
                                Yield Number("&"c)
                                Yield Number("H"c)
                            End If

                            Dim codepoint = AscW(c)
                            For Each digit In If(useHexadecimalNumbers, codepoint.ToString("X"), codepoint.ToString())
                                Yield Number(digit)
                            Next

                            Yield Punctuation(")"c)
                        End If

                        startNewConcatenand = True
                    ElseIf (isCrLf) Then
                        Yield Character(Cr)
                        Yield Character(Lf)
                    Else
                        Yield Character(c)
                    End If
                Else
                    If isFirst AndAlso quote Then
                        Yield Quotes()
                    End If

                    If startNewConcatenand Then
                        Yield Space()
                        Yield [Operator]("&"c)
                        Yield Space()
                        Yield Quotes()

                        startNewConcatenand = False
                    End If

                    lastConcatenandWasQuoted = True
                    If c = """"c AndAlso quote Then
                        Yield Quotes()
                        Yield Quotes()
                    Else
                        Yield Character(c)
                    End If
                End If
            End While

            If quote AndAlso lastConcatenandWasQuoted Then
                Yield Quotes()
            End If
        End Function

        Friend Function IsPrintable(c As Char) As Boolean
            Dim category = CharUnicodeInfo.GetUnicodeCategory(c)
            Return category <> UnicodeCategory.OtherNotAssigned AndAlso category <> UnicodeCategory.ParagraphSeparator AndAlso category <> UnicodeCategory.Control
        End Function

        Friend Function GetWellKnownCharacterName(c As Char) As String
            Select Case c
                Case NullChar
                    Return "vbNullChar"
                Case Back
                    Return "vbBack"
                Case Cr
                    Return "vbCr"
                Case FormFeed
                    Return "vbFormFeed"
                Case Lf
                    Return "vbLf"
                Case Tab
                    Return "vbTab"
                Case VerticalTab
                    Return "vbVerticalTab"
            End Select

            Return Nothing
        End Function

    End Module

End Namespace
