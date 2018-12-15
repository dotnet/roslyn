' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Option Infer On

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module CompileTimeCalculations

        Friend Function UncheckedCLng(v As ULong) As Long
            Return CType(v, Long)
        End Function

        Friend Function UncheckedCLng(v As Double) As Long
            Return CType(v, Long)
        End Function

        Friend Function UncheckedCULng(v As Long) As ULong
            Return CType(v, ULong)
        End Function

        Friend Function UncheckedCULng(v As Integer) As ULong
            Return CType(v, ULong)
        End Function

        Friend Function UncheckedCULng(v As Double) As ULong
            Return CType(v, ULong)
        End Function

        Friend Function UncheckedCInt(v As ULong) As Integer
            Return CType(v, Integer)
        End Function

        Friend Function UncheckedCInt(v As Long) As Integer
            Return CType(v, Integer)
        End Function

        Friend Function UncheckedCUInt(v As ULong) As UInteger
            Return CType(v, UInteger)
        End Function

        Friend Function UncheckedCUInt(v As Long) As UInteger
            Return CType(v, UInteger)
        End Function

        Friend Function UncheckedCUInt(v As Integer) As UInteger
            Return CType(v, UInteger)
        End Function

        Friend Function UncheckedCShort(v As ULong) As Short
            Return CType(v, Short)
        End Function

        Friend Function UncheckedCShort(v As Long) As Short
            Return CType(v, Short)
        End Function

        Friend Function UncheckedCShort(v As Integer) As Short
            Return CType(v, Short)
        End Function

        Friend Function UncheckedCShort(v As UShort) As Short
            Return CType(v, Short)
        End Function

        Friend Function UncheckedCInt(v As UInteger) As Integer
            Return CType(v, Integer)
        End Function

        Friend Function UncheckedCShort(v As UInteger) As Short
            Return CType(v, Short)
        End Function

        Friend Function UncheckedCUShort(v As Short) As UShort
            Return CType(v, UShort)
        End Function

        Friend Function UncheckedCUShort(v As Integer) As UShort
            Return CType(v, UShort)
        End Function

        Friend Function UncheckedCUShort(v As Long) As UShort
            Return CType(v, UShort)
        End Function

        Friend Function UncheckedCByte(v As SByte) As Byte
            Return CType(v, Byte)
        End Function

        Friend Function UncheckedCByte(v As Integer) As Byte
            Return CType(v, Byte)
        End Function

        Friend Function UncheckedCByte(v As Long) As Byte
            Return CType(v, Byte)
        End Function

        Friend Function UncheckedCByte(v As UShort) As Byte
            Return CType(v, Byte)
        End Function

        Friend Function UncheckedCSByte(v As Byte) As SByte
            Return CType(v, SByte)
        End Function

        Friend Function UncheckedCSByte(v As Integer) As SByte
            Return CType(v, SByte)
        End Function

        Friend Function UncheckedCSByte(v As Long) As SByte
            Return CType(v, SByte)
        End Function

        Friend Function UncheckedMul(x As Integer, y As Integer) As Integer
            Return x * y
        End Function

        Friend Function UncheckedMul(x As Long, y As Long) As Long
            Return x * y
        End Function

        Friend Function UncheckedIntegralDiv(x As Long, y As Long) As Long
            If y = -1 Then
                Return UncheckedNegate(x)
            End If

            Return x \ y
        End Function

        Private Function UncheckedAdd(x As Integer, y As Integer) As Integer
            Return x + y
        End Function

        Private Function UncheckedAdd(x As Long, y As Long) As Long
            Return x + y
        End Function

        Private Function UncheckedAdd(x As ULong, y As ULong) As ULong
            Return x + y
        End Function

        Private Function UncheckedSub(x As Long, y As Long) As Long
            Return x - y
        End Function

        Private Function UncheckedSub(x As UInteger, y As UInteger) As UInteger
            Return x - y
        End Function

        Private Function UncheckedNegate(x As Long) As Long
            Return -x
        End Function

        Friend Function GetConstantValueAsInt64(ByRef value As ConstantValue) As Long
            Select Case (value.Discriminator)
                Case ConstantValueTypeDiscriminator.SByte : Return value.SByteValue
                Case ConstantValueTypeDiscriminator.Byte : Return value.ByteValue
                Case ConstantValueTypeDiscriminator.Int16 : Return value.Int16Value
                Case ConstantValueTypeDiscriminator.UInt16 : Return value.UInt16Value
                Case ConstantValueTypeDiscriminator.Int32 : Return value.Int32Value
                Case ConstantValueTypeDiscriminator.UInt32 : Return value.UInt32Value
                Case ConstantValueTypeDiscriminator.Int64 : Return value.Int64Value
                Case ConstantValueTypeDiscriminator.UInt64 : Return UncheckedCLng(value.UInt64Value)
                Case ConstantValueTypeDiscriminator.Char : Return AscW(value.CharValue)
                Case ConstantValueTypeDiscriminator.Boolean : Return If(value.BooleanValue, 1, 0)
                Case ConstantValueTypeDiscriminator.DateTime : Return value.DateTimeValue.Ticks
                Case Else : Throw ExceptionUtilities.UnexpectedValue(value.Discriminator)
            End Select
        End Function

        Friend Function GetConstantValue(type As ConstantValueTypeDiscriminator, value As Long) As ConstantValue
            Dim result As ConstantValue

            Select Case (type)
                Case ConstantValueTypeDiscriminator.SByte : result = ConstantValue.Create(UncheckedCSByte(value))
                Case ConstantValueTypeDiscriminator.Byte : result = ConstantValue.Create(UncheckedCByte(value))
                Case ConstantValueTypeDiscriminator.Int16 : result = ConstantValue.Create(UncheckedCShort(value))
                Case ConstantValueTypeDiscriminator.UInt16 : result = ConstantValue.Create(UncheckedCUShort(value))
                Case ConstantValueTypeDiscriminator.Int32 : result = ConstantValue.Create(UncheckedCInt(value))
                Case ConstantValueTypeDiscriminator.UInt32 : result = ConstantValue.Create(UncheckedCUInt(value))
                Case ConstantValueTypeDiscriminator.Int64 : result = ConstantValue.Create(value)
                Case ConstantValueTypeDiscriminator.UInt64 : result = ConstantValue.Create(UncheckedCULng(value))
                Case ConstantValueTypeDiscriminator.Char : result = ConstantValue.Create(ChrW(UncheckedCInt(value)))
                Case ConstantValueTypeDiscriminator.Boolean : result = ConstantValue.Create(If(value = 0, False, True))
                Case ConstantValueTypeDiscriminator.DateTime : result = ConstantValue.Create(New DateTime(value))
                Case Else : Throw ExceptionUtilities.UnexpectedValue(type)
            End Select

            Debug.Assert(result.Discriminator = type)

            Return result
        End Function

        Friend Function NarrowIntegralResult(
            sourceValue As Long,
            sourceType As ConstantValueTypeDiscriminator,
            resultType As ConstantValueTypeDiscriminator,
            ByRef overflow As Boolean
        ) As Long
            Dim resultValue As Long = 0

            Select Case (resultType)

                Case ConstantValueTypeDiscriminator.Boolean
                    resultValue = If(sourceValue = 0, 0, 1)
                    Return resultValue

                Case ConstantValueTypeDiscriminator.SByte
                    resultValue = UncheckedCSByte(sourceValue)

                Case ConstantValueTypeDiscriminator.Byte
                    resultValue = UncheckedCByte(sourceValue)

                Case ConstantValueTypeDiscriminator.Int16
                    resultValue = UncheckedCShort(sourceValue)

                Case ConstantValueTypeDiscriminator.UInt16
                    resultValue = UncheckedCUShort(sourceValue)

                Case ConstantValueTypeDiscriminator.Int32
                    resultValue = UncheckedCInt(sourceValue)

                Case ConstantValueTypeDiscriminator.UInt32
                    resultValue = UncheckedCUInt(sourceValue)

                Case ConstantValueTypeDiscriminator.Int64
                    resultValue = sourceValue

                Case ConstantValueTypeDiscriminator.UInt64
                    resultValue = sourceValue 'UncheckedCLng(UncheckedCULng(SourceValue))

                Case ConstantValueTypeDiscriminator.Char
                    resultValue = UncheckedCUShort(sourceValue)
                    ' // ?? overflow?

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(resultType)
            End Select

            If Not ConstantValue.IsBooleanType(sourceType) AndAlso
                 (ConstantValue.IsUnsignedIntegralType(sourceType) Xor ConstantValue.IsUnsignedIntegralType(resultType)) Then
                ' // If source is a signed type and is a negative value or
                ' // target is a signed type and results in a negative
                ' // value, indicate overflow.
                If Not ConstantValue.IsUnsignedIntegralType(sourceType) Then
                    If sourceValue < 0 Then
                        overflow = True
                    End If
                Else
                    Debug.Assert(Not ConstantValue.IsUnsignedIntegralType(resultType), "Expected signed Target type!!!")

                    If resultValue < 0 Then
                        overflow = True
                    End If
                End If
            End If

            If resultValue <> sourceValue Then
                overflow = True
            End If

            Return resultValue
        End Function

        Friend Function NarrowIntegralResult(
            sourceValue As Long,
            sourceType As SpecialType,
            resultType As SpecialType,
            ByRef overflow As Boolean
        ) As Long
            Return NarrowIntegralResult(sourceValue,
                                        sourceType.ToConstantValueDiscriminator(),
                                        resultType.ToConstantValueDiscriminator(),
                                        overflow)
        End Function

        ''' <summary>
        ''' Narrow a quadword result to a specific integral type, setting Overflow true
        ''' if the result value cannot be represented in the result type.
        ''' </summary>
        Friend Function NarrowIntegralResult(
                        sourceValue As Long,
                        sourceType As TypeSymbol,
                        resultType As TypeSymbol,
                        ByRef overflow As Boolean) As Long

            Debug.Assert(sourceType.IsIntegralType() OrElse sourceType.IsBooleanType() OrElse sourceType.IsCharType(),
                        "Unexpected source type passed in to conversion function!!!")

            Return NarrowIntegralResult(sourceValue,
                                        sourceType.GetConstantValueTypeDiscriminator(),
                                        resultType.GetConstantValueTypeDiscriminator(),
                                        overflow)
        End Function

        Friend Function ConvertIntegralValue(
            sourceValue As Long,
            sourceType As ConstantValueTypeDiscriminator,
            targetType As ConstantValueTypeDiscriminator,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            Debug.Assert(ConstantValue.IsIntegralType(sourceType) OrElse ConstantValue.IsBooleanType(sourceType) OrElse ConstantValue.IsCharType(sourceType),
                "Unexpected source type passed in to conversion function!!!")

            If ConstantValue.IsIntegralType(targetType) OrElse ConstantValue.IsBooleanType(targetType) OrElse ConstantValue.IsCharType(targetType) Then
                Return GetConstantValue(targetType, NarrowIntegralResult(sourceValue, sourceType, targetType, integerOverflow))
            End If

            If ConstantValue.IsStringType(targetType) Then
                'This is correct only if the input type is Char.
                If ConstantValue.IsCharType(sourceType) Then
                    Return ConstantValue.Create(New String(ChrW(UncheckedCInt(sourceValue)), 1))
                End If
            End If

            If ConstantValue.IsFloatingType(targetType) Then
                Return ConvertFloatingValue(
                            If(ConstantValue.IsUnsignedIntegralType(sourceType), CType(UncheckedCULng(sourceValue), Double), CType(sourceValue, Double)),
                            targetType,
                            integerOverflow)
            End If

            If ConstantValue.IsDecimalType(targetType) Then

                Dim resultValue As Decimal

                Dim sign As Boolean

                If Not ConstantValue.IsUnsignedIntegralType(sourceType) AndAlso sourceValue < 0 Then
                    ' // Negative numbers need converted to positive and set the negative sign bit
                    sign = True
                    sourceValue = -sourceValue
                Else
                    sign = False
                End If

                Dim lo32 As Integer = UncheckedCInt(sourceValue And &HFFFFFFFF)
                ' // We include the sign bit here because we negated a negative above, so the
                ' // only number that still has the sign bit set is the maximum negative number
                ' // (which has no positive counterpart)
                Dim mid32 As Integer = UncheckedCInt((sourceValue And &HFFFFFFFF00000000) >> 32)
                Dim hi32 As Integer = 0
                Dim scale As Byte = 0

                resultValue = New Decimal(lo32, mid32, hi32, sign, scale)

                Return ConvertDecimalValue(resultValue, targetType, integerOverflow)
            End If

            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Function ConvertFloatingValue(
            sourceValue As Double,
                        targetType As ConstantValueTypeDiscriminator,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            Dim overflow As Boolean = False

            If (ConstantValue.IsBooleanType(targetType)) Then

                Return ConvertIntegralValue(If(sourceValue = 0.0, 0, 1),
                                            ConstantValueTypeDiscriminator.Int64,
                                            targetType,
                                            integerOverflow)
            End If

            If ConstantValue.IsIntegralType(targetType) OrElse ConstantValue.IsCharType(targetType) Then

                overflow = DetectFloatingToIntegralOverflow(sourceValue, ConstantValue.IsUnsignedIntegralType(targetType))

                If Not overflow Then
                    Dim integralValue As Long
                    Dim temporary As Double
                    Dim floor As Double
                    Dim sourceIntegralType As ConstantValueTypeDiscriminator

                    ' // VB has different rounding behavior than C by default. Ensure we
                    ' // are using the right type of rounding

                    temporary = sourceValue + 0.5

                    ' // We had a number that was equally close to 2 integers.
                    ' // We need to return the even one.
                    floor = Math.Floor(temporary)

                    '[AlekseyT]: Using Math.IEEERemainder as a replacement for fmod.
                    If floor <> temporary OrElse Math.IEEERemainder(temporary, 2.0) = 0 Then
                        integralValue = If(IsUnsignedLongType(targetType), ConvertFloatingToUI64(floor), UncheckedCLng(floor))
                    Else
                        integralValue = If(IsUnsignedLongType(targetType), ConvertFloatingToUI64(floor - 1.0), UncheckedCLng(floor - 1.0))
                    End If

                    If sourceValue < 0 Then
                        sourceIntegralType = ConstantValueTypeDiscriminator.Int64
                    Else
                        sourceIntegralType = ConstantValueTypeDiscriminator.UInt64
                    End If

                    Return ConvertIntegralValue(integralValue,
                                                sourceIntegralType,
                                                targetType,
                                                integerOverflow)
                End If
            End If

            If ConstantValue.IsFloatingType(targetType) Then
                Dim resultValue As Double = NarrowFloatingResult(sourceValue, targetType, overflow)

                ' // We have decided to ignore overflows in compile-time evaluation
                ' // of floating expressions.

                If targetType = ConstantValueTypeDiscriminator.Single Then
                    Return ConstantValue.Create(CType(resultValue, Single))
                End If

                Debug.Assert(targetType = ConstantValueTypeDiscriminator.Double)
                Return ConstantValue.Create(resultValue)
            End If

            If ConstantValue.IsDecimalType(targetType) Then
                Dim resultValue As Decimal

                Try
                    resultValue = Convert.ToDecimal(sourceValue)
                Catch ex As OverflowException
                    overflow = True
                End Try

                If Not overflow Then
                    Return ConvertDecimalValue(resultValue, targetType, integerOverflow)
                End If
            End If

            If overflow Then
                Return ConstantValue.Bad
            End If

            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Function ConvertDecimalValue(
            sourceValue As Decimal,
            targetType As ConstantValueTypeDiscriminator,
            ByRef integerOverflow As Boolean
        ) As ConstantValue

            Dim overflow As Boolean = False

            If ConstantValue.IsIntegralType(targetType) OrElse ConstantValue.IsCharType(targetType) Then
                Dim isNegative As Boolean
                Dim scale As Byte
                Dim low, mid, high As UInteger
                sourceValue.GetBits(isNegative, scale, low, mid, high)

                If scale = 0 Then
                    Dim resultValue As Long

                    ' Easy case: no scale factor.
                    overflow = high <> 0

                    If Not overflow Then
                        resultValue = (CLng(mid) << 32) Or low

                        Dim sourceIntegralType As ConstantValueTypeDiscriminator = Nothing

                        If isNegative Then
                            ' The source value is negative, so we need to negate the result value.
                            ' If the result type is unsigned, or the result value is already
                            ' large enough that it consumes the sign bit, then we have overflowed.
                            If ConstantValue.IsUnsignedIntegralType(targetType) OrElse
                               UncheckedCULng(resultValue) > &H8000000000000000UL Then
                                overflow = True
                            Else
                                resultValue = UncheckedNegate(resultValue)
                                sourceIntegralType = ConstantValueTypeDiscriminator.Int64
                            End If
                        Else
                            sourceIntegralType = ConstantValueTypeDiscriminator.UInt64
                        End If

                        If Not overflow Then
                            Return ConvertIntegralValue(resultValue,
                                                        sourceIntegralType,
                                                        targetType,
                                                        integerOverflow)
                        End If
                    End If

                Else
                    Dim resultValue As Double

                    ' // No overflow possible
                    resultValue = Decimal.ToDouble(sourceValue)

                    Return ConvertFloatingValue(resultValue,
                                                targetType,
                                                integerOverflow)
                End If
            End If

            If ConstantValue.IsFloatingType(targetType) OrElse ConstantValue.IsBooleanType(targetType) Then
                Dim resultValue As Double

                ' // No overflow possible
                resultValue = Decimal.ToDouble(sourceValue)

                Return ConvertFloatingValue(resultValue,
                                            targetType,
                                            integerOverflow)
            End If

            If ConstantValue.IsDecimalType(targetType) Then
                Return ConstantValue.Create(sourceValue)
            End If

            If overflow Then
                Return ConstantValue.Bad
            End If

            Throw ExceptionUtilities.Unreachable()
        End Function

        Friend Function NarrowFloatingResult(
            value As Double,
            resultType As ConstantValueTypeDiscriminator,
            ByRef overflow As Boolean
        ) As Double

            If Double.IsNaN(value) Then
                overflow = True
            End If

            Select Case (resultType)

                Case ConstantValueTypeDiscriminator.Double
                    Return value

                Case ConstantValueTypeDiscriminator.Single

                    If value > Single.MaxValue OrElse value < Single.MinValue Then
                        overflow = True
                    End If

                    Return CType(value, Single)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(resultType)
            End Select

            Return value
        End Function

        Friend Function NarrowFloatingResult(
                value As Double,
                resultType As SpecialType,
                ByRef overflow As Boolean
            ) As Double

            If Double.IsNaN(value) Then
                overflow = True
            End If

            Select Case resultType
                Case SpecialType.System_Double
                    Return value

                Case SpecialType.System_Single
                    If value > Single.MaxValue OrElse value < Single.MinValue Then
                        overflow = True
                    End If

                    Return CType(value, Single)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(resultType)
            End Select

            Return value
        End Function

        Private Function DetectFloatingToIntegralOverflow(
            sourceValue As Double,
            isUnsigned As Boolean
        ) As Boolean

            If isUnsigned Then
                ' // this code is shared by fjitdef.h
                If sourceValue < &HF000000000000000UL Then
                    If sourceValue > -1 Then
                        Return False
                    End If
                Else
                    Dim temporary As Double = (sourceValue - &HF000000000000000UL)

                    If temporary < &H7000000000000000L AndAlso UncheckedCLng(temporary) < &H1000000000000000L Then
                        Return False
                    End If
                End If
            Else
                ' // this code is shared by fjitdef.h
                If sourceValue < -&H7000000000000000L Then
                    Dim temporary As Double = sourceValue - (-&H7000000000000000L)

                    If temporary > -&H7000000000000000L AndAlso UncheckedCLng(temporary) > -&H1000000000000001L Then
                        Return False
                    End If
                Else
                    If sourceValue > &H7000000000000000L Then
                        Dim temporary As Double = (sourceValue - &H7000000000000000L)

                        If temporary < &H7000000000000000L AndAlso UncheckedCLng(temporary) > &H1000000000000000L Then
                            Return False
                        End If
                    Else
                        Return False
                    End If
                End If
            End If

            Return True
        End Function

        Private Function ConvertFloatingToUI64(sourceValue As Double) As Long
            ' // Conversion from double to uint64 is annoyingly implemented by the
            ' // VC++ compiler as (uint64)(int64)(double)val, so we have to do it by hand.

            Dim result As Long

            ' // code below stolen from jit...
            Dim two63 As Double = 2147483648.0 * 4294967296.0

            If sourceValue < two63 Then
                result = UncheckedCLng(sourceValue)
            Else
                result = UncheckedAdd(UncheckedCLng(sourceValue - two63), &H8000000000000000)
            End If

            Return result
        End Function

        Private Function IsUnsignedLongType(type As ConstantValueTypeDiscriminator) As Boolean
            Return type = ConstantValueTypeDiscriminator.UInt64
        End Function

        Friend Function TypeAllowsCompileTimeConversions(type As ConstantValueTypeDiscriminator) As Boolean
            Return TypeAllowsCompileTimeOperations(type)
        End Function

        Friend Function TypeAllowsCompileTimeOperations(type As ConstantValueTypeDiscriminator) As Boolean
            Select Case (type)
                Case ConstantValueTypeDiscriminator.Boolean,
                     ConstantValueTypeDiscriminator.SByte,
                     ConstantValueTypeDiscriminator.Byte,
                     ConstantValueTypeDiscriminator.Int16,
                     ConstantValueTypeDiscriminator.UInt16,
                     ConstantValueTypeDiscriminator.Int32,
                     ConstantValueTypeDiscriminator.UInt32,
                     ConstantValueTypeDiscriminator.Int64,
                     ConstantValueTypeDiscriminator.UInt64,
                     ConstantValueTypeDiscriminator.Char,
                     ConstantValueTypeDiscriminator.Decimal,
                     ConstantValueTypeDiscriminator.Double,
                     ConstantValueTypeDiscriminator.Single,
                     ConstantValueTypeDiscriminator.DateTime,
                     ConstantValueTypeDiscriminator.String

                    Return True

                Case Else
                    Return False
            End Select

        End Function

        Friend Function AdjustConstantValueFromMetadata(value As ConstantValue, targetType As TypeSymbol, isByRefParamValue As Boolean) As ConstantValue
            ' See MetaImport::DecodeValue in Dev10 compiler.

            If targetType Is Nothing OrElse targetType.IsErrorType() Then
                Return value
            End If

            Select Case value.Discriminator

                Case ConstantValueTypeDiscriminator.Int32

                    ' Adding the test for Object here is necessary in order to
                    ' make something like "Optional Byref X As Object = 5" work.
                    If targetType.IsIntrinsicType() OrElse
                       targetType.IsEnumType() OrElse
                       targetType.IsObjectType() OrElse
                       (targetType.IsNullableType() AndAlso targetType.GetNullableUnderlyingType().IsIntrinsicType()) Then
                        'No change
                        Exit Select
                    End If

                    If isByRefParamValue OrElse
                        (Not targetType.IsTypeParameter() AndAlso
                         Not targetType.IsArrayType() AndAlso
                         targetType.IsReferenceType()) Then

                        ' // REVIEW: are there other byref types besides Integer and enums
                        ' //         that might need to be handled here?
                        ' // ByRef Integer, and ByRef Enum (that is stored as I4)
                        ' // are pointer types, but optional values are not pointers

                        ' // COM+ encodes pointer constants as I4's
                        ' // CONSIDER LATER AnthonyL 8/21/00: Does this t_ref workaround move to I8 on a 64-bit machine?

                        ' // MattGe: Back out part of 604868 to address build lab issue #4093
                        ' // Probably just need to #ifdef this differently for 64bit, but
                        ' // will let Cameron decide.
                        ' //Value.Integral = (__int32)*(WIN64_UNALIGNED void **)pvValue;

                        If value.Int32Value = 0 Then
                            value = ConstantValue.Nothing
                        Else
                            value = ConstantValue.Bad
                        End If
                    End If

                Case ConstantValueTypeDiscriminator.Int64

                    If targetType.IsDateTimeType() Then
                        value = ConstantValue.Create(New DateTime(value.Int64Value))
                    End If
            End Select

            Return value
        End Function

        Friend Function Multiply(
            leftValue As Long,
            rightValue As Long,
            sourceType As SpecialType,
            resultType As SpecialType,
            ByRef integerOverflow As Boolean
        ) As Long
            Return Multiply(leftValue, rightValue,
                            sourceType.ToConstantValueDiscriminator(),
                            resultType.ToConstantValueDiscriminator(),
                            integerOverflow)
        End Function

        Friend Function Multiply(
            leftValue As Long,
            rightValue As Long,
            sourceType As ConstantValueTypeDiscriminator,
            resultType As ConstantValueTypeDiscriminator,
            ByRef integerOverflow As Boolean
        ) As Long

            Dim ResultValue = NarrowIntegralResult(
                                    UncheckedMul(leftValue, rightValue),
                                    sourceType,
                                    resultType,
                                    integerOverflow)

            If ConstantValue.IsUnsignedIntegralType(resultType) Then
                If rightValue <> 0 AndAlso
                     UncheckedCULng(ResultValue) / UncheckedCULng(rightValue) <> UncheckedCULng(leftValue) Then

                    integerOverflow = True
                End If
            Else
                If (leftValue > 0 AndAlso rightValue > 0 AndAlso ResultValue <= 0) OrElse
                    (leftValue < 0 AndAlso rightValue < 0 AndAlso ResultValue <= 0) OrElse
                    (leftValue > 0 AndAlso rightValue < 0 AndAlso ResultValue >= 0) OrElse
                    (leftValue < 0 AndAlso rightValue > 0 AndAlso ResultValue >= 0) OrElse
                    (rightValue <> 0 AndAlso ResultValue / rightValue <> leftValue) Then

                    integerOverflow = True
                End If
            End If

            Return ResultValue
        End Function
    End Module

End Namespace


