' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Hosting.ObjectFormatterHelpers

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Public Class VisualBasicPrimitiveFormatter
        Inherits CommonPrimitiveFormatter

        Protected Overrides ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Protected Overrides Function FormatLiteral(value As Boolean) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Protected Overrides Function FormatLiteral(value As Date) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Protected Overrides Function FormatLiteral(value As String, useQuotes As Boolean, escapeNonPrintable As Boolean, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Dim options As ObjectDisplayOptions = GetObjectDisplayOptions(useQuotes:=useQuotes, escapeNonPrintable:=escapeNonPrintable, numberRadix:=numberRadix)
            Return ObjectDisplay.FormatLiteral(value, options)
        End Function

        Protected Overrides Function FormatLiteral(c As Char, useQuotes As Boolean, escapeNonPrintable As Boolean, Optional includeCodePoints As Boolean = False, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Dim options As ObjectDisplayOptions = GetObjectDisplayOptions(useQuotes:=useQuotes, escapeNonPrintable:=escapeNonPrintable, includeCodePoints:=includeCodePoints, numberRadix:=numberRadix)
            Return ObjectDisplay.FormatLiteral(c, options)
        End Function

        Protected Overrides Function FormatLiteral(value As SByte, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As Byte, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As Short, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As UShort, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As Integer, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As UInteger, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As Long, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As ULong, Optional numberRadix As Integer = NumberRadixDecimal) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(numberRadix:=numberRadix))
        End Function

        Protected Overrides Function FormatLiteral(value As Double) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Protected Overrides Function FormatLiteral(value As Single) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Protected Overrides Function FormatLiteral(value As Decimal) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function
    End Class

End Namespace

