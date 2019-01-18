' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Reflection
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay

#Disable Warning CA1200 ' Avoid using cref tags with a prefix
    ''' <summary>
    ''' Displays a value in the VisualBasic style.
    ''' </summary>
    ''' <seealso cref="T:Microsoft.CodeAnalysis.CSharp.Symbols.ObjectDisplay"/>
#Enable Warning CA1200 ' Avoid using cref tags with a prefix
    Friend Module ObjectDisplay

        Private Const s_nullChar As Char = ChrW(0)
        Private Const s_back As Char = ChrW(8)
        Private Const s_Cr As Char = ChrW(13)
        Private Const s_formFeed As Char = ChrW(12)
        Private Const s_Lf As Char = ChrW(10)
        Private Const s_tab As Char = ChrW(9)
        Private Const s_verticalTab As Char = ChrW(11)

        ''' <summary>
        ''' Returns a string representation of an object of primitive type.
        ''' </summary>
        ''' <param name="obj">A value to display as a string.</param>
        ''' <param name="options">Options used to customize formatting of an Object value.</param>
        ''' <returns>A string representation of an object of primitive type (or null if the type is not supported).</returns>
        ''' <remarks>
        ''' Handles <see cref="Boolean"/>, <see cref="String"/>, <see cref="Char"/>, <see cref="SByte"/>
        ''' <see cref="Byte"/>, <see cref="Short"/>, <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>,
        ''' <see cref="Long"/>, <see cref="ULong"/>, <see cref="Double"/>, <see cref="Single"/>, <see cref="Decimal"/>,
        ''' <see cref="Date"/>, and <c>Nothing</c>.
        ''' </remarks>
        Public Function FormatPrimitive(obj As Object, options As ObjectDisplayOptions) As String
            If obj Is Nothing Then
                Return NullLiteral
            End If

            Dim type = obj.GetType()
            If type.GetTypeInfo().IsEnum Then
                type = [Enum].GetUnderlyingType(type)
            End If

            If type Is GetType(Integer) Then
                Return FormatLiteral(DirectCast(obj, Integer), options)
            End If

            If type Is GetType(String) Then
                Return FormatLiteral(DirectCast(obj, String), options)
            End If

            If type Is GetType(Boolean) Then
                Return FormatLiteral(DirectCast(obj, Boolean))
            End If

            If type Is GetType(Char) Then
                Return FormatLiteral(DirectCast(obj, Char), options)
            End If

            If type Is GetType(Byte) Then
                Return FormatLiteral(DirectCast(obj, Byte), options)
            End If

            If type Is GetType(Short) Then
                Return FormatLiteral(DirectCast(obj, Short), options)
            End If

            If type Is GetType(Long) Then
                Return FormatLiteral(DirectCast(obj, Long), options)
            End If

            If type Is GetType(Double) Then
                Return FormatLiteral(DirectCast(obj, Double), options)
            End If

            If type Is GetType(ULong) Then
                Return FormatLiteral(DirectCast(obj, ULong), options)
            End If

            If type Is GetType(UInteger) Then
                Return FormatLiteral(DirectCast(obj, UInteger), options)
            End If

            If type Is GetType(UShort) Then
                Return FormatLiteral(DirectCast(obj, UShort), options)
            End If

            If type Is GetType(SByte) Then
                Return FormatLiteral(DirectCast(obj, SByte), options)
            End If

            If type Is GetType(Single) Then
                Return FormatLiteral(DirectCast(obj, Single), options)
            End If

            If type Is GetType(Decimal) Then
                Return FormatLiteral(DirectCast(obj, Decimal), options)
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
        ''' <param name="options">Options used to customize formatting of a literal value.</param>
        Friend Function FormatLiteral(value As String, options As ObjectDisplayOptions) As String
            ValidateOptions(options)

            If value Is Nothing Then
                Throw New ArgumentNullException()
            End If

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            For Each token As Integer In TokenizeString(value, options)
                sb.Append(ChrW(token And &HFFFF)) ' lower 16 bits of token contains the Unicode char value
            Next

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(c As Char, options As ObjectDisplayOptions) As String
            ValidateOptions(options)

            If IsPrintable(c) OrElse Not options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableCharacters) Then
                Return If(options.IncludesOption(ObjectDisplayOptions.UseQuotes),
                    """" & EscapeQuote(c) & """c",
                    c.ToString())
            End If

            Dim wellKnown = GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                Return wellKnown
            End If

            Dim codepoint = AscW(c)
            Return If(options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers), "ChrW(&H" & codepoint.ToString("X"), "ChrW(" & codepoint.ToString()) & ")"
        End Function

        Private Function EscapeQuote(c As Char) As String
            Return If(c = """", """""", c)
        End Function

        Friend Function FormatLiteral(value As SByte, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                Return "&H" & If(value >= 0, value.ToString("X2"), CInt(value).ToString("X8"))
            Else
                Return value.ToString(GetFormatCulture(cultureInfo))
            End If
        End Function

        Friend Function FormatLiteral(value As Byte, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                Return "&H" & value.ToString("X2")
            Else
                Return value.ToString(GetFormatCulture(cultureInfo))
            End If
        End Function

        Friend Function FormatLiteral(value As Short, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(If(value >= 0, value.ToString("X4"), CInt(value).ToString("X8")))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("S"c)
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As UShort, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(value.ToString("X4"))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("US")
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As Integer, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(value.ToString("X8"))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("I"c)
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As UInteger, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(value.ToString("X8"))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("UI")
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As Long, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(value.ToString("X16"))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("L"c)
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As ULong, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            If options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers) Then
                sb.Append("&H")
                sb.Append(value.ToString("X16"))
            Else
                sb.Append(value.ToString(GetFormatCulture(cultureInfo)))
            End If

            If options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix) Then
                sb.Append("UL")
            End If

            Return pooledBuilder.ToStringAndFree()
        End Function

        Friend Function FormatLiteral(value As Double, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim result = value.ToString("R", GetFormatCulture(cultureInfo))

            Return If(options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix), result & "R", result)
        End Function

        Friend Function FormatLiteral(value As Single, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim result = value.ToString("R", GetFormatCulture(cultureInfo))

            Return If(options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix), result & "F", result)
        End Function

        Friend Function FormatLiteral(value As Decimal, options As ObjectDisplayOptions, Optional cultureInfo As CultureInfo = Nothing) As String
            ValidateOptions(options)

            Dim result = value.ToString(GetFormatCulture(cultureInfo))

            Return If(options.IncludesOption(ObjectDisplayOptions.IncludeTypeSuffix), result & "D", result)
        End Function

        Friend Function FormatLiteral(value As Date) As String
            Return value.ToString("#M/d/yyyy hh:mm:ss tt#", CultureInfo.InvariantCulture)
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
        Friend Iterator Function TokenizeString(str As String, options As ObjectDisplayOptions) As IEnumerable(Of Integer)
            Dim useQuotes = options.IncludesOption(ObjectDisplayOptions.UseQuotes)
            Dim useHexadecimalNumbers = options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbers)
            Dim escapeNonPrintable = options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableCharacters)

            If str.Length = 0 Then
                If useQuotes Then
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
                Dim shouldEscape As Boolean
                Dim isCrLf As Boolean
                Dim copyPair = False

                If Not escapeNonPrintable Then
                    wellKnown = Nothing
                    shouldEscape = False
                    isCrLf = False
                ElseIf c = s_Cr AndAlso i < str.Length AndAlso str(i) = s_Lf Then
                    wellKnown = "vbCrLf"
                    shouldEscape = True
                    isCrLf = True
                    i += 1
                ElseIf CharUnicodeInfo.GetUnicodeCategory(c) = UnicodeCategory.Surrogate AndAlso IsPrintable(CharUnicodeInfo.GetUnicodeCategory(str, i - 1)) Then
                    ' copy properly paired surrogates directly into the resulting output
                    wellKnown = Nothing
                    shouldEscape = False
                    isCrLf = False
                    copyPair = True
                Else
                    wellKnown = GetWellKnownCharacterName(c)
                    shouldEscape = wellKnown IsNot Nothing OrElse Not IsPrintable(c)
                    isCrLf = False
                End If

                If shouldEscape Then
                    If useQuotes Then
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
                        Yield Character(s_Cr)
                        Yield Character(s_Lf)
                    Else
                        Yield Character(c)
                    End If
                Else
                    If isFirst AndAlso useQuotes Then
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
                    If c = """"c AndAlso useQuotes Then
                        Yield Quotes()
                        Yield Quotes()
                    Else
                        Yield Character(c)
                        If copyPair Then
                            ' copy the second character of a Unicode surrogate pair
                            c = str(i)
                            i += 1
                            Yield Character(c)
                        End If
                    End If
                End If
            End While

            If useQuotes AndAlso lastConcatenandWasQuoted Then
                Yield Quotes()
            End If
        End Function

        Friend Function IsPrintable(c As Char) As Boolean
            Return IsPrintable(CharUnicodeInfo.GetUnicodeCategory(c))
        End Function

        Private Function IsPrintable(category As UnicodeCategory) As Boolean
            Select Case category
                Case UnicodeCategory.OtherNotAssigned,
                     UnicodeCategory.ParagraphSeparator,
                     UnicodeCategory.Control,
                     UnicodeCategory.LineSeparator,
                     UnicodeCategory.Surrogate
                    Return False
                Case Else
                    Return True
            End Select
        End Function

        Friend Function GetWellKnownCharacterName(c As Char) As String
            Select Case c
                Case s_nullChar
                    Return "vbNullChar"
                Case s_back
                    Return "vbBack"
                Case s_Cr
                    Return "vbCr"
                Case s_formFeed
                    Return "vbFormFeed"
                Case s_Lf
                    Return "vbLf"
                Case s_tab
                    Return "vbTab"
                Case s_verticalTab
                    Return "vbVerticalTab"
            End Select

            Return Nothing
        End Function

        Private Function GetFormatCulture(cultureInfo As CultureInfo) As CultureInfo
            Return If(cultureInfo, CultureInfo.InvariantCulture)
        End Function

        <Conditional("DEBUG")>
        Private Sub ValidateOptions(options As ObjectDisplayOptions)
            ' This option is not supported and has no meaning in Visual Basic...should not be passed...
            Debug.Assert(Not options.IncludesOption(ObjectDisplayOptions.IncludeCodePoints))
            Debug.Assert(Not options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableCharacters) Or options.IncludesOption(ObjectDisplayOptions.UseQuotes))
        End Sub

    End Module

End Namespace
