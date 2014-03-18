' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic.ObjectDisplay
    ''' <summary>
    ''' Displays a value in the VisualBasic style.
    ''' </summary>
    ''' <seealso cref="T:Microsoft.CodeAnalysis.CSharp.Symbols.ObjectDisplay"/>
    Friend Module ObjectDisplay
        ''' <summary>
        ''' Displays a symbol in the Visual Basic style, based on a <see cref="SymbolDisplayFormat"/>.
        ''' </summary>
        ''' <param name="symbol">The symbol to be displayed.</param>
        ''' <param name="format">The formatting options to apply.  If Nothing is passed, <see cref="SymbolDisplayFormat.VisualBasicErrorMessageFormat"/> will be used.</param>
        ''' <returns>A formatted string that can be displayed to the user.</returns>
        ''' <remarks>
        ''' The return value is not expected to be syntactically valid Visual Basic.
        ''' </remarks>
        Public Function ToDisplayString(symbol As ISymbol, Optional format As SymbolDisplayFormat = Nothing) As String
            Return ToDisplayParts(symbol, format:=format).ToDisplayString()
        End Function

        ''' <summary>
        ''' Displays a symbol in the Visual Basic style, based on a <see cref="SymbolDisplayFormat"/>.
        ''' Based on the context, qualify type And member names as little as possible without
        ''' introducing ambiguities.
        ''' </summary>
        ''' <param name="symbol">The symbol to be displayed.</param>
        ''' <param name="semanticModel">Semantic information about the context in which the symbol is being displayed.</param>
        ''' <param name="position">A position within the <see cref="SyntaxTree"/> Or <paramref name="semanticModel"/>.</param>
        ''' <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        ''' <returns>A formatted string that can be displayed to the user.</returns>
        ''' <remarks>
        ''' The return value is not expected to be syntactically valid Visual Basic.
        ''' </remarks>
        Public Function ToMinimalDisplayString(symbol As ISymbol,
                                               semanticModel As SemanticModel,
                                               position As Integer,
                                               Optional format As SymbolDisplayFormat = Nothing) As String
            Return ToMinimalDisplayParts(symbol, semanticModel, position, format).ToDisplayString()
        End Function

        ''' <summary>
        ''' Convert a symbol to an array of string parts, each of which has a kind. Useful for
        ''' colorizing the display string.
        ''' </summary>
        ''' <param name="symbol">The symbol to be displayed.</param>
        ''' <param name="format">The formatting options to apply.  If Nothing Is passed, <see cref="SymbolDisplayFormat.VisualBasicErrorMessageFormat"/> will be used.</param>
        ''' <returns>A list of display parts.</returns>
        ''' <remarks>
        ''' Parts are not localized until they are converted to strings.
        ''' </remarks>
        Public Function ToDisplayParts(symbol As ISymbol,
                                       Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            format = If(format, SymbolDisplayFormat.VisualBasicErrorMessageFormat)
            Return ToDisplayParts(symbol, semanticModelOpt:=Nothing, positionOpt:=-1, format:=format, minimal:=False)
        End Function

        ''' <summary>
        ''' Convert a symbol to an array of string parts, each of which has a kind. Useful for
        ''' colorizing the display string.
        ''' </summary>
        ''' <param name="symbol">The symbol to be displayed.</param>
        ''' <param name="semanticModel">Semantic information about the context in which the symbol is being displayed.</param>
        ''' <param name="position">A position within the <see cref="SyntaxTree"/> or <paramref name="semanticModel"/>.</param>
        ''' <param name="format">The formatting options to apply.  If null is passed, <see cref="SymbolDisplayFormat.CSharpErrorMessageFormat"/> will be used.</param>
        ''' <returns>A list of display parts.</returns>
        ''' <remarks>
        ''' Parts are not localized until they are converted to strings.
        ''' </remarks>
        Public Function ToMinimalDisplayParts(symbol As ISymbol,
                                              semanticModel As SemanticModel,
                                              position As Integer,
                                              Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            format = If(format, SymbolDisplayFormat.MinimallyQualifiedFormat)
            Return ToDisplayParts(symbol, semanticModel, position, format, minimal:=True)
        End Function

        Private Function ToDisplayParts(symbol As ISymbol,
                                       semanticModelOpt As SemanticModel,
                                       positionOpt As Integer,
                                       format As SymbolDisplayFormat,
                                       minimal As Boolean) As ImmutableArray(Of SymbolDisplayPart)
            If symbol Is Nothing Then
                Throw New ArgumentNullException("symbol")
            End If

            If minimal Then
                If semanticModelOpt Is Nothing Then
                    Throw New ArgumentException(VBResources.SemanticModelMustBeProvided)
                ElseIf positionOpt < 0 OrElse positionOpt > semanticModelOpt.SyntaxTree.Length Then 'Note: not >= since EOF is allowed.
                    Throw New ArgumentOutOfRangeException(VBResources.PositionNotWithinTree)
                End If
            Else
                Debug.Assert(semanticModelOpt Is Nothing)
                Debug.Assert(positionOpt < 0)
            End If

            Dim builder = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
            Dim visitor = New SymbolDisplayVisitor(builder, format, semanticModelOpt, positionOpt)
            symbol.Accept(visitor)
            Return builder.ToImmutableAndFree()
        End Function

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

            Select Case System.Type.GetTypeCode(obj.GetType())
                Case TypeCode.Boolean
                    Return FormatLiteral(DirectCast(obj, Boolean))
                Case TypeCode.String
                    Return FormatLiteral(DirectCast(obj, String), quoteStrings)
                Case TypeCode.Char
                    Return FormatLiteral(DirectCast(obj, Char), quoteStrings, useHexadecimalNumbers)
                Case TypeCode.SByte
                    Return FormatLiteral(DirectCast(obj, SByte), useHexadecimalNumbers)
                Case TypeCode.Byte
                    Return FormatLiteral(DirectCast(obj, Byte), useHexadecimalNumbers)
                Case TypeCode.Int16
                    Return FormatLiteral(DirectCast(obj, Short), useHexadecimalNumbers)
                Case TypeCode.UInt16
                    Return FormatLiteral(DirectCast(obj, UShort), useHexadecimalNumbers)
                Case TypeCode.Int32
                    Return FormatLiteral(DirectCast(obj, Integer), useHexadecimalNumbers)
                Case TypeCode.UInt32
                    Return FormatLiteral(DirectCast(obj, UInteger), useHexadecimalNumbers)
                Case TypeCode.Int64
                    Return FormatLiteral(DirectCast(obj, Long), useHexadecimalNumbers)
                Case TypeCode.UInt64
                    Return FormatLiteral(DirectCast(obj, ULong), useHexadecimalNumbers)
                Case TypeCode.Double
                    Return FormatLiteral(DirectCast(obj, Double))
                Case TypeCode.Single
                    Return FormatLiteral(DirectCast(obj, Single))
                Case TypeCode.Decimal
                    Return FormatLiteral(DirectCast(obj, Decimal))
                Case TypeCode.DateTime
                    Return FormatLiteral(DirectCast(obj, DateTime))
                Case Else
                    Return Nothing
            End Select
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

            Return VbStringDisplay.FormatString(value, quote, nonPrintableSubstitute, useHexadecimalNumbers)
        End Function

        Friend Function FormatLiteral(c As Char, quote As Boolean, useHexadecimalNumbers As Boolean) As String
            Dim wellKnown = VbStringDisplay.GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                Return wellKnown
            End If

            If Not VbStringDisplay.IsPrintable(c) Then
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
            Return value.ToString("M/d/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture)
        End Function

    End Module
End Namespace
