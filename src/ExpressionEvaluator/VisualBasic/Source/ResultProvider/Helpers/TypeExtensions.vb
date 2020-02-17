' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Type = Microsoft.VisualStudio.Debugger.Metadata.Type
Imports TypeCode = Microsoft.VisualStudio.Debugger.Metadata.TypeCode

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Module TypeExtensions

        <Extension>
        Public Function IsPredefinedType(type As Type) As Boolean
            Return type.GetPredefinedTypeName() IsNot Nothing
        End Function

        <Extension>
        Public Function GetPredefinedTypeName(type As Type) As String
            If type.IsEnum Then
                Return Nothing
            End If

            Select Case Type.GetTypeCode(type)
                Case TypeCode.Object
                    Return If(type.IsObject(), "Object", Nothing)
                Case TypeCode.Boolean
                    Return "Boolean"
                Case TypeCode.Char
                    Return "Char"
                Case TypeCode.SByte
                    Return "SByte"
                Case TypeCode.Byte
                    Return "Byte"
                Case TypeCode.Int16
                    Return "Short"
                Case TypeCode.UInt16
                    Return "UShort"
                Case TypeCode.Int32
                    Return "Integer"
                Case TypeCode.UInt32
                    Return "UInteger"
                Case TypeCode.Int64
                    Return "Long"
                Case TypeCode.UInt64
                    Return "ULong"
                Case TypeCode.Single
                    Return "Single"
                Case TypeCode.Double
                    Return "Double"
                Case TypeCode.Decimal
                    Return "Decimal"
                Case TypeCode.String
                    Return "String"
                Case TypeCode.DateTime
                    Return "Date"
            End Select

            Return Nothing
        End Function

    End Module

End Namespace
