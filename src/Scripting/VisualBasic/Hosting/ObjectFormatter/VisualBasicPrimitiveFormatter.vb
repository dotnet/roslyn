' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.ObjectFormatterHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend Class VisualBasicPrimitiveFormatter
        Inherits CommonPrimitiveFormatter

        Protected Overrides ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Protected Overrides Function FormatLiteral(value As Boolean) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Protected Overrides Function FormatLiteral(value As Date, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value) ' TODO (https://github.com/dotnet/roslyn/issues/8174): consume cultureInfo
        End Function

        Protected Overrides Function FormatLiteral(value As String, useQuotes As Boolean, escapeNonPrintable As Boolean, Optional numberRadix As Integer = NumberRadixDecimal) As String
            If escapeNonPrintable AndAlso Not useQuotes Then
                Throw New ArgumentException(VBScriptingResources.ExceptionEscapeWithoutQuote, NameOf(escapeNonPrintable))
            End If

            Dim options As ObjectDisplayOptions = GetObjectDisplayOptions(useQuotes:=useQuotes, escapeNonPrintable:=escapeNonPrintable, numberRadix:=numberRadix)
            Return ObjectDisplay.FormatLiteral(value, options)
        End Function

        Protected Overrides Function FormatLiteral(c As Char, useQuotes As Boolean, escapeNonPrintable As Boolean, Optional includeCodePoints As Boolean = False, Optional numberRadix As Integer = NumberRadixDecimal) As String
            If escapeNonPrintable AndAlso Not useQuotes Then
                Throw New ArgumentException(VBScriptingResources.ExceptionEscapeWithoutQuote, NameOf(escapeNonPrintable))
            End If

            Dim options As ObjectDisplayOptions = GetObjectDisplayOptions(useQuotes:=useQuotes, escapeNonPrintable:=escapeNonPrintable, includeCodePoints:=includeCodePoints, numberRadix:=numberRadix)
            Return ObjectDisplay.FormatLiteral(c, options)
        End Function

        Protected Overrides Function FormatLiteral(value As SByte, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Byte, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Short, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As UShort, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Integer, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As UInteger, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Long, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As ULong, Optional numberRadix As Integer = NumberRadixDecimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix), cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Double, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Single, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo)
        End Function

        Protected Overrides Function FormatLiteral(value As Decimal, Optional cultureInfo As CultureInfo = Nothing) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None, cultureInfo)
        End Function
    End Class

End Namespace

