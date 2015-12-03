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

        Protected Overrides Function FormatLiteral(value As String, quote As Boolean, Optional useHexadecimalNumbers As Boolean = False) As String
            Dim options = ObjectDisplayOptions.None
            If quote Then
                options = options Or ObjectDisplayOptions.UseQuotes
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatLiteral(value, options)
        End Function

        Protected Overrides Function FormatLiteral(c As Char, quote As Boolean, Optional includeCodePoints As Boolean = False, Optional useHexadecimalNumbers As Boolean = False) As String
            Dim options = ObjectDisplayOptions.None
            If quote Then
                options = options Or ObjectDisplayOptions.UseQuotes
            End If
            If includeCodePoints Then
                options = options Or ObjectDisplayOptions.IncludeCodePoints
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatLiteral(c, options)
        End Function

        Protected Overrides Function FormatLiteral(value As SByte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As Byte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As Short, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As UShort, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As Integer, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As UInteger, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As Long, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Protected Overrides Function FormatLiteral(value As ULong, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
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

