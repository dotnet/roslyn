' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic
#Disable Warning CA1200 ' Avoid using cref tags with a prefix
    ''' <summary>
    ''' Displays a symbol in the VisualBasic style.
    ''' </summary>
    ''' <seealso cref="T:Microsoft.CodeAnalysis.CSharp.SymbolDisplay"/>
#Enable Warning CA1200 ' Avoid using cref tags with a prefix
    Public Module SymbolDisplay
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
                Throw New ArgumentNullException(NameOf(symbol))
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
        ''' <returns>A string representation of an object of primitive type (or <see langword="Nothing"/> if the type is not supported).</returns>
        ''' <remarks>
        ''' Handles <see cref="Boolean"/>, <see cref="String"/>, <see cref="Char"/>, <see cref="SByte"/>
        ''' <see cref="Byte"/>, <see cref="Short"/>, <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>,
        ''' <see cref="Long"/>, <see cref="ULong"/>, <see cref="Double"/>, <see cref="Single"/>, <see cref="Decimal"/>,
        ''' <see cref="Date"/>, and <see langword="Nothing"/>.
        ''' </remarks>
        Public Function FormatPrimitive(obj As Object, quoteStrings As Boolean, useHexadecimalNumbers As Boolean) As String
            Return ObjectDisplay.FormatPrimitive(obj, ToObjectDisplayOptions(quoteStrings, useHexadecimalNumbers))
        End Function

        ''' <summary>
        ''' Returns a textual representation of an object of primitive type as an array of string parts,
        ''' each of which has a kind. Useful for colorizing the display string.
        ''' </summary>
        ''' <param name="obj">A value to display as string parts.</param>
        ''' <param name="format">The formatting options to apply. If <see langword="Nothing"/> is passed, <see cref="SymbolDisplayFormat.VisualBasicErrorMessageFormat"/> will be used.</param>
        ''' <returns>A list of display parts (or <see langword="Nothing"/> if the type is not supported).</returns>
        ''' <remarks>
        ''' Handles <see cref="Boolean"/>, <see cref="String"/>, <see cref="Char"/>, <see cref="SByte"/>
        ''' <see cref="Byte"/>, <see cref="Short"/>, <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>,
        ''' <see cref="Long"/>, <see cref="ULong"/>, <see cref="Double"/>, <see cref="Single"/>, <see cref="Decimal"/>,
        ''' <see cref="Date"/>, and <see langword="Nothing"/>.
        ''' </remarks>
        Public Function PrimitiveToDisplayParts(obj As Object, Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            If Not (obj Is Nothing OrElse obj.GetType().IsPrimitive OrElse obj.GetType().IsEnum OrElse TypeOf obj Is String OrElse TypeOf obj Is Decimal OrElse TypeOf obj Is Date) Then
                Return Nothing
            End If

            Dim builder = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
            AddConstantValue(builder, obj, ToObjectDisplayOptions(If(format, SymbolDisplayFormat.VisualBasicErrorMessageFormat).ConstantValueOptions))
            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Returns a textual representation of an object of primitive type as an array of string parts,
        ''' each of which has a kind. Useful for colorizing the display string.
        ''' </summary>
        ''' <param name="obj">A value to display as string parts.</param>
        ''' <param name="semanticModel">Semantic information about the context in which the symbol is being displayed.</param>
        ''' <param name="position">A position within the <see cref="SyntaxTree"/> Or <paramref name="semanticModel"/>.</param>
        ''' <param name="format">The formatting options to apply. If <see langword="Nothing"/> is passed, <see cref="SymbolDisplayFormat.VisualBasicErrorMessageFormat"/> will be used.</param>
        ''' <returns>A list of display parts (or <see langword="Nothing"/> if the type is not supported).</returns>
        ''' <remarks>
        ''' Handles <see cref="Boolean"/>, <see cref="String"/>, <see cref="Char"/>, <see cref="SByte"/>
        ''' <see cref="Byte"/>, <see cref="Short"/>, <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>,
        ''' <see cref="Long"/>, <see cref="ULong"/>, <see cref="Double"/>, <see cref="Single"/>, <see cref="Decimal"/>,
        ''' <see cref="Date"/>, and <see langword="Nothing"/>.
        ''' </remarks>
        Public Function PrimitiveToMinimalDisplayParts(obj As Object,
                                                       semanticModel As SemanticModel,
                                                       position As Integer,
                                                       Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            If Not (obj Is Nothing OrElse obj.GetType().IsPrimitive OrElse obj.GetType().IsEnum OrElse TypeOf obj Is String OrElse TypeOf obj Is Decimal OrElse TypeOf obj Is Date) Then
                Return Nothing
            End If

            Dim builder = ArrayBuilder(Of SymbolDisplayPart).GetInstance()
            AddConstantValue(builder, obj, ToObjectDisplayOptions(If(format, SymbolDisplayFormat.VisualBasicErrorMessageFormat).ConstantValueOptions))
            Return builder.ToImmutableAndFree()
        End Function

        Friend Sub AddConstantValue(builder As ArrayBuilder(Of SymbolDisplayPart), constantValue As Object, options As SymbolDisplayConstantValueOptions)
            AddConstantValue(builder, constantValue, ToObjectDisplayOptions(options))
        End Sub

        Private Sub AddConstantValue(builder As ArrayBuilder(Of SymbolDisplayPart), constantValue As Object, options As ObjectDisplayOptions)
            If constantValue IsNot Nothing Then
                AddLiteralValue(builder, constantValue, options)
            Else
                builder.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Keyword, Nothing, SyntaxFacts.GetText(SyntaxKind.NothingKeyword)))
            End If
        End Sub

        Private Sub AddLiteralValue(builder As ArrayBuilder(Of SymbolDisplayPart), value As Object, options As ObjectDisplayOptions)
            Debug.Assert(value.GetType().IsPrimitive OrElse value.GetType().IsEnum OrElse TypeOf value Is String OrElse TypeOf value Is Decimal OrElse TypeOf value Is Date)

            Dim type = value.GetType()

            Select Case type
                Case GetType(String)
                    AddSymbolDisplayParts(builder, DirectCast(value, String), options)

                Case GetType(Char)
                    AddSymbolDisplayParts(builder, DirectCast(value, Char), options)

                Case Else
                    Dim valueString = ObjectDisplay.FormatPrimitive(value, options)
                    Debug.Assert(valueString IsNot Nothing)
                    Dim kind = If(type = GetType(Boolean), SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.NumericLiteral)
                    builder.Add(New SymbolDisplayPart(kind, Nothing, valueString))
            End Select
        End Sub

        Private Sub AddSymbolDisplayParts(parts As ArrayBuilder(Of SymbolDisplayPart), str As String, options As ObjectDisplayOptions)
            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            Dim lastKind = -1
            For Each token As Integer In ObjectDisplay.TokenizeString(str, options)
                Dim kind = token >> 16

                ' merge contiguous tokens of the same kind into a single part
                If lastKind >= 0 AndAlso lastKind <> kind Then
                    parts.Add(New SymbolDisplayPart(DirectCast(lastKind, SymbolDisplayPartKind), Nothing, sb.ToString()))
                    sb.Clear()
                End If

                lastKind = kind
                sb.Append(ChrW(token And &HFFFF)) ' lower 16 bits of token contains the Unicode char value
            Next

            If lastKind >= 0 Then
                parts.Add(New SymbolDisplayPart(DirectCast(lastKind, SymbolDisplayPartKind), Nothing, sb.ToString()))
            End If

            pooledBuilder.Free()
        End Sub

        Private Sub AddSymbolDisplayParts(parts As ArrayBuilder(Of SymbolDisplayPart), c As Char, options As ObjectDisplayOptions)
            If ObjectDisplay.IsPrintable(c) OrElse Not options.IncludesOption(ObjectDisplayOptions.EscapeNonPrintableCharacters) Then
                Dim literal = If(options.IncludesOption(ObjectDisplayOptions.UseQuotes), """" & c & """c", c)
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.StringLiteral, Nothing, literal))
                Return
            End If

            Dim wellKnown = ObjectDisplay.GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.ConstantName, Nothing, wellKnown))
                Return
            End If

            ' non-printable, add "ChrW(codepoint)"
            Dim codepoint = AscW(c)
            Dim codepointLiteral =
                If(options.IncludesOption(ObjectDisplayOptions.UseHexadecimalNumbersForCharacters),
                    "&H" & codepoint.ToString("X"),
                    codepoint.ToString())
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, Nothing, "ChrW"))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.NumericLiteral, Nothing, codepointLiteral))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, ")"))
        End Sub

        Private Function ToObjectDisplayOptions(quoteStrings As Boolean, useHexadecimalNumbers As Boolean) As ObjectDisplayOptions
            Dim numberFormat = If(useHexadecimalNumbers, NumericFormat.Hexadecimal, NumericFormat.Decimal)
            Return ToObjectDisplayOptions(New SymbolDisplayConstantValueOptions(numberFormat, numberFormat, Not quoteStrings))
        End Function

        Private Function ToObjectDisplayOptions(constantValueOptions As SymbolDisplayConstantValueOptions) As ObjectDisplayOptions
            Dim options = ObjectDisplayOptions.None

            If constantValueOptions.NumericLiteralFormat = NumericFormat.Hexadecimal Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If

            If constantValueOptions.CharacterValueFormat = NumericFormat.Hexadecimal Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbersForCharacters
            End If

            If Not constantValueOptions.NoQuotes Then
                options = options Or ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.EscapeNonPrintableCharacters
            End If

            Return options
        End Function
    End Module
End Namespace
