' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module SpecialTypeExtensions

        <Extension()>
        Public Function IsNumericType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsIntegralType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsUnsignedIntegralType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Byte,
                     SpecialType.System_UInt16,
                     SpecialType.System_UInt32,
                     SpecialType.System_UInt64
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsSignedIntegralType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_Int32,
                     SpecialType.System_Int64
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsFloatingType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Single,
                     SpecialType.System_Double
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsIntrinsicType(this As SpecialType) As Boolean
            Return this = SpecialType.System_String OrElse this.IsIntrinsicValueType()
        End Function

        <Extension()>
        Public Function IsIntrinsicValueType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal,
                     SpecialType.System_DateTime,
                     SpecialType.System_Char
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension>
        Public Function IsPrimitiveType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Boolean,
                    SpecialType.System_Byte,
                    SpecialType.System_Char,
                    SpecialType.System_Double,
                    SpecialType.System_Int16,
                    SpecialType.System_Int32,
                    SpecialType.System_Int64,
                    SpecialType.System_UInt16,
                    SpecialType.System_UInt32,
                    SpecialType.System_UInt64,
                    SpecialType.System_IntPtr,
                    SpecialType.System_UIntPtr,
                    SpecialType.System_SByte,
                    SpecialType.System_Single
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsStrictSupertypeOfConcreteDelegate(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Object,
                     SpecialType.System_Delegate,
                     SpecialType.System_MulticastDelegate
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsRestrictedType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_TypedReference,
                    SpecialType.System_ArgIterator,
                    SpecialType.System_RuntimeArgumentHandle
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsValidTypeForAttributeArgument(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Char,
                     SpecialType.System_Object,
                     SpecialType.System_String
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsValidTypeForSwitchTable(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Char,
                     SpecialType.System_Boolean,
                     SpecialType.System_String
                    Return True
                Case Else
                    Return False
            End Select
        End Function


        <Extension()>
        Public Function TypeToIndex(type As SpecialType) As Integer?
            Dim result As Integer

            Select Case type
                Case SpecialType.System_Object
                    result = 0
                Case SpecialType.System_String
                    result = 1
                Case SpecialType.System_Boolean
                    result = 2
                Case SpecialType.System_Char
                    result = 3
                Case SpecialType.System_SByte
                    result = 4
                Case SpecialType.System_Int16
                    result = 5
                Case SpecialType.System_Int32
                    result = 6
                Case SpecialType.System_Int64
                    result = 7
                Case SpecialType.System_Byte
                    result = 8
                Case SpecialType.System_UInt16
                    result = 9
                Case SpecialType.System_UInt32
                    result = 10
                Case SpecialType.System_UInt64
                    result = 11
                Case SpecialType.System_Single
                    result = 12
                Case SpecialType.System_Double
                    result = 13
                Case SpecialType.System_Decimal
                    result = 14
                Case SpecialType.System_DateTime
                    result = 15

                Case Else
                    Return Nothing
            End Select

            Return result
        End Function

        <Extension>
        Public Function GetNativeCompilerVType(this As SpecialType) As String
            Select Case this
                Case SpecialType.System_Void
                    Return "t_void"
                Case SpecialType.System_Boolean
                    Return "t_bool"
                Case SpecialType.System_Char
                    Return "t_char"
                Case SpecialType.System_SByte
                    Return "t_i1"
                Case SpecialType.System_Byte
                    Return "t_ui1"
                Case SpecialType.System_Int16
                    Return "t_i2"
                Case SpecialType.System_UInt16
                    Return "t_ui2"
                Case SpecialType.System_Int32
                    Return "t_i4"
                Case SpecialType.System_UInt32
                    Return "t_ui4"
                Case SpecialType.System_Int64
                    Return "t_i8"
                Case SpecialType.System_UInt64
                    Return "t_ui8"
                Case SpecialType.System_Decimal
                    Return "t_decimal"
                Case SpecialType.System_Single
                    Return "t_single"
                Case SpecialType.System_Double
                    Return "t_double"
                Case SpecialType.System_String
                    Return "t_string"
                Case SpecialType.System_IntPtr, SpecialType.System_UIntPtr
                    Return "t_ptr"
                Case SpecialType.System_Array
                    Return "t_array"
                Case SpecialType.System_DateTime
                    Return "t_date"
                Case Else
                    Return Nothing
            End Select
        End Function

    End Module

End Namespace

