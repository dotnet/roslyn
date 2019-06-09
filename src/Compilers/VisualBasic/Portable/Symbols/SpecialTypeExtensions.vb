' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module SpecialTypeExtensions
        <Extension>
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

        <Extension>
        Public Function IsFloatingType(this As SpecialType) As Boolean
            Select Case this
                Case SpecialType.System_Single,
                     SpecialType.System_Double
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        <Extension>
        Public Function IsIntrinsicType(this As SpecialType) As Boolean
            Return this = SpecialType.System_String OrElse this.IsIntrinsicValueType()
        End Function

        <Extension>
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

        <Extension>
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

        <Extension>
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

        <Extension>
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

        <Extension>
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


        <Extension>
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

        <Extension>
        Public Function GetDisplayName(this As SpecialType) As String
            Dim result = TryGetKeywordText(this)
            Debug.Assert(result IsNot Nothing)
            Return result
        End Function

        <Extension>
        Public Function TryGetKeywordText(this As SpecialType) As String
            Select Case this
                Case SpecialType.System_SByte
                    Return "SByte"
                Case SpecialType.System_Int16
                    Return "Short"
                Case SpecialType.System_Int32
                    Return "Integer"
                Case SpecialType.System_Int64
                    Return "Long"
                Case SpecialType.System_Byte
                    Return "Byte"
                Case SpecialType.System_UInt16
                    Return "UShort"
                Case SpecialType.System_UInt32
                    Return "UInteger"
                Case SpecialType.System_UInt64
                    Return "ULong"
                Case SpecialType.System_Single
                    Return "Single"
                Case SpecialType.System_Double
                    Return "Double"
                Case SpecialType.System_Decimal
                    Return "Decimal"
                Case SpecialType.System_Char
                    Return "Char"
                Case SpecialType.System_Boolean
                    Return "Boolean"
                Case SpecialType.System_String
                    Return "String"
                Case SpecialType.System_Object
                    Return "Object"
                Case SpecialType.System_DateTime
                    Return "Date"
                Case SpecialType.System_Void
                    Return "Void"
                Case Else
                    Return Nothing
            End Select
        End Function

        <Extension>
        Friend Function ToConstantValueDiscriminator(this As SpecialType) As ConstantValueTypeDiscriminator
            Select Case this
                Case SpecialType.System_SByte
                    Return ConstantValueTypeDiscriminator.SByte
                Case SpecialType.System_Byte
                    Return ConstantValueTypeDiscriminator.Byte
                Case SpecialType.System_Int16
                    Return ConstantValueTypeDiscriminator.Int16
                Case SpecialType.System_UInt16
                    Return ConstantValueTypeDiscriminator.UInt16
                Case SpecialType.System_Int32
                    Return ConstantValueTypeDiscriminator.Int32
                Case SpecialType.System_UInt32
                    Return ConstantValueTypeDiscriminator.UInt32
                Case SpecialType.System_Int64
                    Return ConstantValueTypeDiscriminator.Int64
                Case SpecialType.System_UInt64
                    Return ConstantValueTypeDiscriminator.UInt64
                Case SpecialType.System_Char
                    Return ConstantValueTypeDiscriminator.Char
                Case SpecialType.System_Boolean
                    Return ConstantValueTypeDiscriminator.Boolean
                Case SpecialType.System_Single
                    Return ConstantValueTypeDiscriminator.Single
                Case SpecialType.System_Double
                    Return ConstantValueTypeDiscriminator.Double
                Case SpecialType.System_Decimal
                    Return ConstantValueTypeDiscriminator.Decimal
                Case SpecialType.System_DateTime
                    Return ConstantValueTypeDiscriminator.DateTime
                Case SpecialType.System_String
                    Return ConstantValueTypeDiscriminator.String
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this)
            End Select
        End Function

        <Extension>
        Friend Function GetShiftSizeMask(this As SpecialType) As Integer
            Select Case this
                Case SpecialType.System_SByte, SpecialType.System_Byte
                    Return &H7

                Case SpecialType.System_Int16, SpecialType.System_UInt16
                    Return &HF

                Case SpecialType.System_Int32, SpecialType.System_UInt32
                    Return &H1F

                Case SpecialType.System_Int64, SpecialType.System_UInt64
                    Return &H3F

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this)
            End Select
        End Function

        <Extension>
        Public Function ToRuntimeType(this As SpecialType) As Type
            Select Case this
                Case SpecialType.System_SByte
                    Return GetType(SByte)
                Case SpecialType.System_Int16
                    Return GetType(Short)
                Case SpecialType.System_Int32
                    Return GetType(Integer)
                Case SpecialType.System_Int64
                    Return GetType(Long)
                Case SpecialType.System_Byte
                    Return GetType(Byte)
                Case SpecialType.System_UInt16
                    Return GetType(UShort)
                Case SpecialType.System_UInt32
                    Return GetType(UInteger)
                Case SpecialType.System_UInt64
                    Return GetType(ULong)
                Case SpecialType.System_Single
                    Return GetType(Single)
                Case SpecialType.System_Double
                    Return GetType(Double)
                Case SpecialType.System_Decimal
                    Return GetType(Decimal)
                Case SpecialType.System_Char
                    Return GetType(Char)
                Case SpecialType.System_Boolean
                    Return GetType(Boolean)
                Case SpecialType.System_String
                    Return GetType(String)
                Case SpecialType.System_Object
                    Return GetType(Object)
                Case SpecialType.System_DateTime
                    Return GetType(Date)
                Case SpecialType.System_Void
                    Return GetType(Void)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this)
            End Select
        End Function
    End Module
End Namespace

