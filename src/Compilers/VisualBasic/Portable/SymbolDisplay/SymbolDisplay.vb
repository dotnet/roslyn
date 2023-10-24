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
            format = If(format, SymbolDisplayFormat.VisualBasicErrorMessageFormat)
            Return ToDisplayString(symbol, semanticModel:=Nothing, position:=-1, format, minimal:=False)
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
            format = If(format, SymbolDisplayFormat.MinimallyQualifiedFormat)
            Return ToDisplayString(symbol, semanticModel, position, format, minimal:=True)
        End Function

        Private Function ToDisplayString(symbol As ISymbol,
                                         semanticModel As SemanticModel,
                                         position As Integer,
                                         format As SymbolDisplayFormat,
                                         minimal As Boolean) As String
            Dim builder = ArrayBuilder(Of SymbolDisplayPart).GetInstance()

            PopulateDisplayParts(builder, symbol, semanticModel, position, format, minimal)

            Dim result = builder.ToDisplayString()
            builder.Free()

            Return result
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
            Dim builder = ArrayBuilder(Of SymbolDisplayPart).GetInstance()

            PopulateDisplayParts(builder, symbol, semanticModelOpt, positionOpt, format, minimal)

            Return builder.ToImmutableAndFree()
        End Function

        Private Sub PopulateDisplayParts(builder As ArrayBuilder(Of SymbolDisplayPart),
                                         symbol As ISymbol,
                                         semanticModelOpt As SemanticModel,
                                         positionOpt As Integer,
                                         format As SymbolDisplayFormat,
                                         minimal As Boolean)
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

            Dim visitor = New SymbolDisplayVisitor(builder, format, semanticModelOpt, positionOpt)
            symbol.Accept(visitor)
        End Sub

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
            Dim options = ObjectDisplayOptions.None
            If quoteStrings Then
                options = options Or ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.EscapeNonPrintableCharacters
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatPrimitive(obj, options)
        End Function

        Friend Sub AddSymbolDisplayParts(parts As ArrayBuilder(Of SymbolDisplayPart), str As String)
            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim sb = pooledBuilder.Builder

            Dim lastKind = -1
            For Each token As Integer In ObjectDisplay.TokenizeString(str, ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.UseHexadecimalNumbers Or ObjectDisplayOptions.EscapeNonPrintableCharacters)
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

        Friend Sub AddSymbolDisplayParts(parts As ArrayBuilder(Of SymbolDisplayPart), c As Char)
            Dim wellKnown = ObjectDisplay.GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.FieldName, Nothing, wellKnown))
                Return
            End If

            If ObjectDisplay.IsPrintable(c) Then
                parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.StringLiteral, Nothing, """" & c & """c"))
                Return
            End If

            ' non-printable, add "ChrW(codepoint)"
            Dim codepoint = AscW(c)
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.MethodName, Nothing, "ChrW"))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, "("))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.NumericLiteral, Nothing, "&H" & codepoint.ToString("X")))
            parts.Add(New SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, Nothing, ")"))
        End Sub
    End Module
End Namespace
