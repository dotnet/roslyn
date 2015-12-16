' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Public Class VisualBasicTypeNameFormatter
        Inherits CommonTypeNameFormatter

        Protected Overrides ReadOnly Property PrimitiveFormatter As CommonPrimitiveFormatter

        Public Sub New(pFormatter As CommonPrimitiveFormatter)
            PrimitiveFormatter = pFormatter
        End Sub

        Protected Overrides Function GetPrimitiveTypeName(type As SpecialType) As String
            Select Case type
                Case SpecialType.System_Boolean
                    Return "Boolean"
                Case SpecialType.System_Byte
                    Return "Byte"
                Case SpecialType.System_Char
                    Return "Char"
                Case SpecialType.System_Decimal
                    Return "Decimal"
                Case SpecialType.System_Double
                    Return "Double"
                Case SpecialType.System_Int16
                    Return "Short"
                Case SpecialType.System_Int32
                    Return "Integer"
                Case SpecialType.System_Int64
                    Return "Long"
                Case SpecialType.System_SByte
                    Return "SByte"
                Case SpecialType.System_Single
                    Return "Single"
                Case SpecialType.System_String
                    Return "String"
                Case SpecialType.System_UInt16
                    Return "UShort"
                Case SpecialType.System_UInt32
                    Return "UInteger"
                Case SpecialType.System_UInt64
                    Return "ULong"
                Case SpecialType.System_DateTime
                    Return "Date"
                Case SpecialType.System_Object
                    Return "Object"
                Case Else
                    Return Nothing
            End Select
        End Function

        Protected Overrides ReadOnly Property GenericParameterOpening As String
            Get
                Return "(Of "
            End Get
        End Property

        Protected Overrides ReadOnly Property GenericParameterClosing As String
            Get
                Return ")"
            End Get
        End Property

        Protected Overrides ReadOnly Property ArrayOpening As String
            Get
                Return "("
            End Get
        End Property

        Protected Overrides ReadOnly Property ArrayClosing As String
            Get
                Return ")"
            End Get
        End Property


        Public Overrides Function FormatTypeName(type As Type, options As CommonTypeNameFormatterOptions) As String
            ' TODO (https://github.com/dotnet/roslyn/issues/3739): handle generated type names (e.g. state machines as in C#)

            Return MyBase.FormatTypeName(type, options)
        End Function
    End Class

End Namespace

