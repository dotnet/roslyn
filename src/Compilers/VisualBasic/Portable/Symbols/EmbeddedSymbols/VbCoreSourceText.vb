' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Infer On
Option Explicit On
Option Compare Binary

Namespace Global.Microsoft.VisualBasic
    Namespace CompilerServices
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class EmbeddedOperators
            Private Sub New()
            End Sub
            Public Shared Function CompareString(Left As String, Right As String, TextCompare As Boolean) As Integer
                If Left Is Right Then
                    Return 0
                End If
                If Left Is Nothing Then
                    If Right.Length() = 0 Then
                        Return 0
                    End If
                    Return -1
                End If
                If Right Is Nothing Then
                    If Left.Length() = 0 Then
                        Return 0
                    End If
                    Return 1
                End If
                Dim Result As Integer
                If TextCompare Then
                    Dim OptionCompareTextFlags As Global.System.Globalization.CompareOptions = (Global.System.Globalization.CompareOptions.IgnoreCase Or Global.System.Globalization.CompareOptions.IgnoreWidth Or Global.System.Globalization.CompareOptions.IgnoreKanaType)
                    Result = Conversions.GetCultureInfo().CompareInfo.Compare(Left, Right, OptionCompareTextFlags)
                Else
                    Result = Global.System.String.CompareOrdinal(Left, Right)
                End If
                If Result = 0 Then
                    Return 0
                ElseIf Result > 0 Then
                    Return 1
                Else
                    Return -1
                End If
            End Function
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class Conversions
            Private Sub New()
            End Sub
            Private Shared Function GetEnumValue(Value As Object) As Object
                Dim underlyingType = System.Enum.GetUnderlyingType(Value.GetType())
                If underlyingType.Equals(GetType(SByte)) Then
                    Return DirectCast(Value, SByte)
                ElseIf underlyingType.Equals(GetType(Byte)) Then
                    Return DirectCast(Value, Byte)
                ElseIf underlyingType.Equals(GetType(Global.System.Int16)) Then
                    Return DirectCast(Value, Global.System.Int16)
                ElseIf underlyingType.Equals(GetType(Global.System.UInt16)) Then
                    Return DirectCast(Value, Global.System.UInt16)
                ElseIf underlyingType.Equals(GetType(Global.System.Int32)) Then
                    Return DirectCast(Value, Global.System.Int32)
                ElseIf underlyingType.Equals(GetType(Global.System.UInt32)) Then
                    Return DirectCast(Value, Global.System.UInt32)
                ElseIf underlyingType.Equals(GetType(Global.System.Int64)) Then
                    Return DirectCast(Value, Global.System.Int64)
                ElseIf underlyingType.Equals(GetType(Global.System.UInt64)) Then
                    Return DirectCast(Value, Global.System.UInt64)
                Else
                    Throw New Global.System.InvalidCastException
                End If
            End Function
            Public Shared Function ToBoolean(Value As String) As Boolean
                If Value Is Nothing Then
                    Value = ""
                End If
                Try
                    Dim loc As Global.System.Globalization.CultureInfo = GetCultureInfo()
                    If loc.CompareInfo.Compare(Value, Boolean.FalseString, Global.System.Globalization.CompareOptions.IgnoreCase) = 0 Then
                        Return False
                    ElseIf loc.CompareInfo.Compare(Value, Boolean.TrueString, Global.System.Globalization.CompareOptions.IgnoreCase) = 0 Then
                        Return True
                    End If
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CBool(i64Value)
                    End If
                    Return CBool(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToBoolean(Value As Object) As Boolean
                If Value Is Nothing Then
                    Return False
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CBool(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CBool(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CBool(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CBool(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CBool(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CBool(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CBool(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CBool(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CBool(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CBool(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CBool(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CBool(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CBool(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToByte(Value As String) As Byte
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CByte(i64Value)
                    End If
                    Return CByte(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToByte(Value As Object) As Byte
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CByte(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CByte(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CByte(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CByte(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CByte(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CByte(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CByte(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CByte(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CByte(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CByte(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CByte(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CByte(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CByte(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToSByte(Value As String) As SByte
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CSByte(i64Value)
                    End If
                    Return CSByte(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToSByte(Value As Object) As SByte
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CSByte(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CSByte(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CSByte(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CSByte(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CSByte(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CSByte(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CSByte(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CSByte(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CSByte(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CSByte(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CSByte(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CSByte(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CSByte(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToShort(Value As String) As Short
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CShort(i64Value)
                    End If
                    Return CShort(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToShort(Value As Object) As Short
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CShort(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CShort(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CShort(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CShort(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CShort(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CShort(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CShort(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CShort(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CShort(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CShort(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CShort(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CShort(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CShort(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToUShort(Value As String) As UShort
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CUShort(i64Value)
                    End If
                    Return CUShort(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToUShort(Value As Object) As UShort
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CUShort(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CUShort(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CUShort(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CUShort(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CUShort(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CUShort(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CUShort(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CUShort(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CUShort(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CUShort(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CUShort(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CUShort(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CUShort(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToInteger(Value As String) As Integer
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CInt(i64Value)
                    End If
                    Return CInt(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToInteger(Value As Object) As Integer
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CInt(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CInt(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CInt(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CInt(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CInt(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CInt(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CInt(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CInt(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CInt(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CInt(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CInt(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CInt(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CInt(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToUInteger(Value As String) As UInteger
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CUInt(i64Value)
                    End If
                    Return CUInt(ParseDouble(Value))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToUInteger(Value As Object) As UInteger
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CUInt(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CUInt(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CUInt(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CUInt(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CUInt(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CUInt(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CUInt(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CUInt(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CUInt(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CUInt(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CUInt(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CUInt(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CUInt(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToLong(Value As String) As Long
                If (Value Is Nothing) Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CLng(i64Value)
                    End If
                    Return CLng(ParseDecimal(Value, Nothing))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToLong(Value As Object) As Long
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CLng(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CLng(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CLng(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CLng(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CLng(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CLng(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CLng(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CLng(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CLng(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CLng(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CLng(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CLng(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CLng(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToULong(Value As String) As ULong
                If (Value Is Nothing) Then
                    Return 0
                End If
                Try
                    Dim ui64Value As Global.System.UInt64
                    If IsHexOrOctValue(Value, ui64Value) Then
                        Return CULng(ui64Value)
                    End If
                    Return CULng(ParseDecimal(Value, Nothing))
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Function ToULong(Value As Object) As ULong
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CULng(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CULng(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CULng(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CULng(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CULng(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CULng(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CULng(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CULng(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CULng(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CULng(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CULng(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CULng(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CULng(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToDecimal(Value As Boolean) As Decimal
                If Value Then
                    Return -1D
                Else
                    Return 0D
                End If
            End Function
            Public Shared Function ToDecimal(Value As String) As Decimal
                If Value Is Nothing Then
                    Return 0D
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CDec(i64Value)
                    End If
                    Return ParseDecimal(Value, Nothing)
                Catch e1 As Global.System.OverflowException
                    Throw e1
                Catch e2 As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e2.Message, e2)
                End Try
            End Function
            Public Shared Function ToDecimal(Value As Object) As Decimal
                If Value Is Nothing Then
                    Return 0D
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CDec(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CDec(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CDec(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CDec(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CDec(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CDec(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CDec(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CDec(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CDec(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CDec(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CDec(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CDec(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CDec(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Private Shared Function ParseDecimal(Value As String, NumberFormat As Global.System.Globalization.NumberFormatInfo) As Decimal
                Dim NormalizedNumberFormat As Global.System.Globalization.NumberFormatInfo
                Dim culture As Global.System.Globalization.CultureInfo = GetCultureInfo()
                If NumberFormat Is Nothing Then
                    NumberFormat = culture.NumberFormat
                End If
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)
                Const flags As Global.System.Globalization.NumberStyles =
                Global.System.Globalization.NumberStyles.AllowDecimalPoint Or
                Global.System.Globalization.NumberStyles.AllowExponent Or
                Global.System.Globalization.NumberStyles.AllowLeadingSign Or
                Global.System.Globalization.NumberStyles.AllowLeadingWhite Or
                Global.System.Globalization.NumberStyles.AllowThousands Or
                Global.System.Globalization.NumberStyles.AllowTrailingSign Or
                Global.System.Globalization.NumberStyles.AllowParentheses Or
                Global.System.Globalization.NumberStyles.AllowTrailingWhite Or
                Global.System.Globalization.NumberStyles.AllowCurrencySymbol
                Value = ToHalfwidthNumbers(Value, culture)
                Try
                    Return Global.System.Decimal.Parse(Value, flags, NormalizedNumberFormat)
                Catch FormatEx As Global.System.FormatException When Not (NumberFormat Is NormalizedNumberFormat)
                    Return Global.System.Decimal.Parse(Value, flags, NumberFormat)
                Catch Ex As Global.System.Exception
                    Throw Ex
                End Try
            End Function
            Private Shared Function GetNormalizedNumberFormat(InNumberFormat As Global.System.Globalization.NumberFormatInfo) As Global.System.Globalization.NumberFormatInfo
                Dim OutNumberFormat As Global.System.Globalization.NumberFormatInfo
                With InNumberFormat
                    If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                    (Not .NumberDecimalSeparator Is Nothing) AndAlso
                    (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                    (Not .NumberGroupSeparator Is Nothing) AndAlso
                    (.CurrencyDecimalSeparator.Length = 1) AndAlso
                    (.NumberDecimalSeparator.Length = 1) AndAlso
                    (.CurrencyGroupSeparator.Length = 1) AndAlso
                    (.NumberGroupSeparator.Length = 1) AndAlso
                    (.CurrencyDecimalSeparator.Chars(0) = .NumberDecimalSeparator.Chars(0)) AndAlso
                    (.CurrencyGroupSeparator.Chars(0) = .NumberGroupSeparator.Chars(0)) AndAlso
                    (.CurrencyDecimalDigits = .NumberDecimalDigits) Then
                        Return InNumberFormat
                    End If
                End With
                With InNumberFormat
                    If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                    (Not .NumberDecimalSeparator Is Nothing) AndAlso
                    (.CurrencyDecimalSeparator.Length = .NumberDecimalSeparator.Length) AndAlso
                    (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                    (Not .NumberGroupSeparator Is Nothing) AndAlso
                    (.CurrencyGroupSeparator.Length = .NumberGroupSeparator.Length) Then
                        Dim i As Integer
                        For i = 0 To .CurrencyDecimalSeparator.Length - 1
                            If (.CurrencyDecimalSeparator.Chars(i) <> .NumberDecimalSeparator.Chars(i)) Then GoTo MisMatch
                        Next
                        For i = 0 To .CurrencyGroupSeparator.Length - 1
                            If (.CurrencyGroupSeparator.Chars(i) <> .NumberGroupSeparator.Chars(i)) Then GoTo MisMatch
                        Next
                        Return InNumberFormat
                    End If
                End With
MisMatch:
                OutNumberFormat = DirectCast(InNumberFormat.Clone, Global.System.Globalization.NumberFormatInfo)
                With OutNumberFormat
                    .CurrencyDecimalSeparator = .NumberDecimalSeparator
                    .CurrencyGroupSeparator = .NumberGroupSeparator
                    .CurrencyDecimalDigits = .NumberDecimalDigits
                End With
                Return OutNumberFormat
            End Function
            Public Shared Function ToSingle(Value As String) As Single
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CSng(i64Value)
                    End If
                    Dim Result As Double = ParseDouble(Value)
                    If (Result < Global.System.Single.MinValue OrElse Result > Global.System.Single.MaxValue) AndAlso
                    Not Global.System.Double.IsInfinity(Result) Then
                        Throw New Global.System.OverflowException
                    End If
                    Return CSng(Result)
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToSingle(Value As Object) As Single
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CSng(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CSng(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CSng(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CSng(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CSng(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CSng(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CSng(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CSng(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CSng(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CSng(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CSng(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CSng(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CSng(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToDouble(Value As String) As Double
                If Value Is Nothing Then
                    Return 0
                End If
                Try
                    Dim i64Value As Global.System.Int64
                    If IsHexOrOctValue(Value, i64Value) Then
                        Return CDbl(i64Value)
                    End If
                    Return ParseDouble(Value)
                Catch e As Global.System.FormatException
                    Throw New Global.System.InvalidCastException(e.Message, e)
                End Try
            End Function
            Public Shared Function ToDouble(Value As Object) As Double
                If Value Is Nothing Then
                    Return 0
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CDbl(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CDbl(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CDbl(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CDbl(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CDbl(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CDbl(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CDbl(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CDbl(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CDbl(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CDbl(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CDbl(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CDbl(DirectCast(Value, Double))
                ElseIf TypeOf Value Is String Then
                    Return CDbl(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Private Shared Function ParseDouble(Value As String) As Double
                Dim NormalizedNumberFormat As Global.System.Globalization.NumberFormatInfo
                Dim culture As Global.System.Globalization.CultureInfo = GetCultureInfo()
                Dim NumberFormat As Global.System.Globalization.NumberFormatInfo = culture.NumberFormat
                NormalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)
                Const flags As Global.System.Globalization.NumberStyles =
                Global.System.Globalization.NumberStyles.AllowDecimalPoint Or
                Global.System.Globalization.NumberStyles.AllowExponent Or
                Global.System.Globalization.NumberStyles.AllowLeadingSign Or
                Global.System.Globalization.NumberStyles.AllowLeadingWhite Or
                Global.System.Globalization.NumberStyles.AllowThousands Or
                Global.System.Globalization.NumberStyles.AllowTrailingSign Or
                Global.System.Globalization.NumberStyles.AllowParentheses Or
                Global.System.Globalization.NumberStyles.AllowTrailingWhite Or
                Global.System.Globalization.NumberStyles.AllowCurrencySymbol
                Value = ToHalfwidthNumbers(Value, culture)
                Try
                    Return Global.System.Double.Parse(Value, flags, NormalizedNumberFormat)
                Catch FormatEx As Global.System.FormatException When Not (NumberFormat Is NormalizedNumberFormat)
                    Return Global.System.Double.Parse(Value, flags, NumberFormat)
                Catch Ex As Global.System.Exception
                    Throw Ex
                End Try
            End Function
            Public Shared Function ToDate(Value As String) As Date
                Dim ParsedDate As Global.System.DateTime
                Const ParseStyle As Global.System.Globalization.DateTimeStyles =
                Global.System.Globalization.DateTimeStyles.AllowWhiteSpaces Or
                Global.System.Globalization.DateTimeStyles.NoCurrentDateDefault
                Dim Culture As Global.System.Globalization.CultureInfo = GetCultureInfo()
                Dim result = Global.System.DateTime.TryParse(ToHalfwidthNumbers(Value, Culture), Culture, ParseStyle, ParsedDate)
                If result Then
                    Return ParsedDate
                Else
                    Throw New Global.System.InvalidCastException()
                End If
            End Function
            Public Shared Function ToDate(Value As Object) As Date
                If Value Is Nothing Then
                    Return Nothing
                End If
                If TypeOf Value Is Global.System.DateTime Then
                    Return CDate(DirectCast(Value, Global.System.DateTime))
                ElseIf TypeOf Value Is String Then
                    Return CDate(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToChar(Value As String) As Char
                If (Value Is Nothing) OrElse (Value.Length = 0) Then
                    Return Global.System.Convert.ToChar(0 And &HFFFFI)
                End If
                Return Value.Chars(0)
            End Function
            Public Shared Function ToChar(Value As Object) As Char
                If Value Is Nothing Then
                    Return Global.System.Convert.ToChar(0 And &HFFFFI)
                End If
                If TypeOf Value Is Char Then
                    Return CChar(DirectCast(Value, Char))
                ElseIf TypeOf Value Is String Then
                    Return CChar(DirectCast(Value, String))
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Function ToCharArrayRankOne(Value As String) As Char()
                If Value Is Nothing Then
                    Value = ""
                End If
                Return Value.ToCharArray()
            End Function
            Public Shared Function ToCharArrayRankOne(Value As Object) As Char()
                If Value Is Nothing Then
                    Return "".ToCharArray()
                End If
                Dim ArrayValue As Char() = TryCast(Value, Char())
                If ArrayValue IsNot Nothing AndAlso ArrayValue.Rank = 1 Then
                    Return ArrayValue
                ElseIf TypeOf Value Is String Then
                    Return DirectCast(Value, String).ToCharArray()
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Shadows Function ToString(Value As Short) As String
                Return Value.ToString()
            End Function
            Public Shared Shadows Function ToString(Value As Integer) As String
                Return Value.ToString()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Shadows Function ToString(Value As UInteger) As String
                Return Value.ToString()
            End Function
            Public Shared Shadows Function ToString(Value As Long) As String
                Return Value.ToString()
            End Function
            <Global.System.CLSCompliant(False)>
            Public Shared Shadows Function ToString(Value As ULong) As String
                Return Value.ToString()
            End Function
            Public Shared Shadows Function ToString(Value As Single) As String
                Return Value.ToString()
            End Function
            Public Shared Shadows Function ToString(Value As Double) As String
                Return Value.ToString("G")
            End Function
            Public Shared Shadows Function ToString(Value As Date) As String
                Dim TimeTicks As Long = Value.TimeOfDay.Ticks
                If (TimeTicks = Value.Ticks) OrElse
                (Value.Year = 1899 AndAlso Value.Month = 12 AndAlso Value.Day = 30) Then
                    Return Value.ToString("T")
                ElseIf TimeTicks = 0 Then
                    Return Value.ToString("d")
                Else
                    Return Value.ToString("G")
                End If
            End Function
            Public Shared Shadows Function ToString(Value As Decimal) As String
                Return Value.ToString("G")
            End Function
            Public Shared Shadows Function ToString(Value As Object) As String
                If Value Is Nothing Then
                    Return Nothing
                Else
                    Dim StringValue As String = TryCast(Value, String)
                    If StringValue IsNot Nothing Then
                        Return StringValue
                    End If
                End If
                If TypeOf Value Is Global.System.Enum Then
                    Value = GetEnumValue(Value)
                End If
                If TypeOf Value Is Boolean Then
                    Return CStr(DirectCast(Value, Boolean))
                ElseIf TypeOf Value Is SByte Then
                    Return CStr(DirectCast(Value, SByte))
                ElseIf TypeOf Value Is Byte Then
                    Return CStr(DirectCast(Value, Byte))
                ElseIf TypeOf Value Is Global.System.Int16 Then
                    Return CStr(DirectCast(Value, Global.System.Int16))
                ElseIf TypeOf Value Is Global.System.UInt16 Then
                    Return CStr(DirectCast(Value, Global.System.UInt16))
                ElseIf TypeOf Value Is Global.System.Int32 Then
                    Return CStr(DirectCast(Value, Global.System.Int32))
                ElseIf TypeOf Value Is Global.System.UInt32 Then
                    Return CStr(DirectCast(Value, Global.System.UInt32))
                ElseIf TypeOf Value Is Global.System.Int64 Then
                    Return CStr(DirectCast(Value, Global.System.Int64))
                ElseIf TypeOf Value Is Global.System.UInt64 Then
                    Return CStr(DirectCast(Value, Global.System.UInt64))
                ElseIf TypeOf Value Is Decimal Then
                    Return CStr(DirectCast(Value, Global.System.Decimal))
                ElseIf TypeOf Value Is Single Then
                    Return CStr(DirectCast(Value, Single))
                ElseIf TypeOf Value Is Double Then
                    Return CStr(DirectCast(Value, Double))
                ElseIf TypeOf Value Is Char Then
                    Return CStr(DirectCast(Value, Char))
                ElseIf TypeOf Value Is Date Then
                    Return CStr(DirectCast(Value, Date))
                Else
                    Dim CharArray As Char() = TryCast(Value, Char())
                    If CharArray IsNot Nothing Then
                        Return New String(CharArray)
                    End If
                End If
                Throw New Global.System.InvalidCastException()
            End Function
            Public Shared Shadows Function ToString(Value As Boolean) As String
                If Value Then
                    Return Global.System.Boolean.TrueString
                Else
                    Return Global.System.Boolean.FalseString
                End If
            End Function
            Public Shared Shadows Function ToString(Value As Byte) As String
                Return Value.ToString()
            End Function
            Public Shared Shadows Function ToString(Value As Char) As String
                Return Value.ToString()
            End Function
            Friend Shared Function GetCultureInfo() As Global.System.Globalization.CultureInfo
                Return Global.System.Globalization.CultureInfo.CurrentCulture
            End Function
            Friend Shared Function ToHalfwidthNumbers(s As String, culture As Global.System.Globalization.CultureInfo) As String
                Return s
            End Function
            Friend Shared Function IsHexOrOctValue(Value As String, ByRef i64Value As Global.System.Int64) As Boolean
                Dim ch As Char
                Dim Length As Integer
                Dim FirstNonspace As Integer
                Dim TmpValue As String
                Length = Value.Length
                Do While (FirstNonspace < Length)
                    ch = Value.Chars(FirstNonspace)
                    If ch = "&"c AndAlso FirstNonspace + 2 < Length Then
                        GoTo GetSpecialValue
                    End If
                    If ch <> Strings.ChrW(32) AndAlso ch <> Strings.ChrW(&H3000) Then
                        Return False
                    End If
                    FirstNonspace += 1
                Loop
                Return False
GetSpecialValue:
                ch = Global.System.Char.ToLowerInvariant(Value.Chars(FirstNonspace + 1))
                TmpValue = ToHalfwidthNumbers(Value.Substring(FirstNonspace + 2), GetCultureInfo())
                If ch = "h"c Then
                    i64Value = Global.System.Convert.ToInt64(TmpValue, 16)
                ElseIf ch = "o"c Then
                    i64Value = Global.System.Convert.ToInt64(TmpValue, 8)
                Else
                    Throw New Global.System.FormatException
                End If
                Return True
            End Function
            Friend Shared Function IsHexOrOctValue(Value As String, ByRef ui64Value As Global.System.UInt64) As Boolean
                Dim ch As Char
                Dim Length As Integer
                Dim FirstNonspace As Integer
                Dim TmpValue As String
                Length = Value.Length
                Do While (FirstNonspace < Length)
                    ch = Value.Chars(FirstNonspace)
                    If ch = "&"c AndAlso FirstNonspace + 2 < Length Then
                        GoTo GetSpecialValue
                    End If
                    If ch <> Strings.ChrW(32) AndAlso ch <> Strings.ChrW(&H3000) Then
                        Return False
                    End If
                    FirstNonspace += 1
                Loop
                Return False
GetSpecialValue:
                ch = Global.System.Char.ToLowerInvariant(Value.Chars(FirstNonspace + 1))
                TmpValue = ToHalfwidthNumbers(Value.Substring(FirstNonspace + 2), GetCultureInfo())
                If ch = "h"c Then
                    ui64Value = Global.System.Convert.ToUInt64(TmpValue, 16)
                ElseIf ch = "o"c Then
                    ui64Value = Global.System.Convert.ToUInt64(TmpValue, 8)
                Else
                    Throw New Global.System.FormatException
                End If
                Return True
            End Function
            Public Shared Function ToGenericParameter(Of T)(Value As Object) As T
                If Value Is Nothing Then
                    Return Nothing
                End If
                Dim reflectedType As Global.System.Type = GetType(T)
                If Global.System.Type.Equals(reflectedType, GetType(Global.System.Boolean)) Then
                    Return DirectCast(CObj(CBool(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.SByte)) Then
                    Return DirectCast(CObj(CSByte(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Byte)) Then
                    Return DirectCast(CObj(CByte(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Int16)) Then
                    Return DirectCast(CObj(CShort(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt16)) Then
                    Return DirectCast(CObj(CUShort(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Int32)) Then
                    Return DirectCast(CObj(CInt(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt32)) Then
                    Return DirectCast(CObj(CUInt(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Int64)) Then
                    Return DirectCast(CObj(CLng(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.UInt64)) Then
                    Return DirectCast(CObj(CULng(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Decimal)) Then
                    Return DirectCast(CObj(CDec(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Single)) Then
                    Return DirectCast(CObj(CSng(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Double)) Then
                    Return DirectCast(CObj(CDbl(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.DateTime)) Then
                    Return DirectCast(CObj(CDate(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.Char)) Then
                    Return DirectCast(CObj(CChar(Value)), T)
                ElseIf Global.System.Type.Equals(reflectedType, GetType(Global.System.String)) Then
                    Return DirectCast(CObj(CStr(Value)), T)
                Else
                    Return DirectCast(Value, T)
                End If
            End Function
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class ProjectData
            Private Sub New()
            End Sub
            Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception)
            End Sub
            Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception, lErl As Integer)
            End Sub
            Public Shared Sub ClearProjectError()
            End Sub
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class Utils
            Private Sub New()
            End Sub
            Public Shared Function CopyArray(arySrc As Global.System.Array, aryDest As Global.System.Array) As Global.System.Array
                If arySrc Is Nothing Then
                    Return aryDest
                End If
                Dim lLength As Integer
                lLength = arySrc.Length
                If lLength = 0 Then
                    Return aryDest
                End If
                If aryDest.Rank() <> arySrc.Rank() Then
                    Throw New Global.System.InvalidCastException()
                End If
                Dim iDim As Integer
                For iDim = 0 To aryDest.Rank() - 2
                    If aryDest.GetUpperBound(iDim) <> arySrc.GetUpperBound(iDim) Then
                        Throw New Global.System.ArrayTypeMismatchException()
                    End If
                Next iDim
                If lLength > aryDest.Length Then
                    lLength = aryDest.Length
                End If
                If arySrc.Rank > 1 Then
                    Dim LastRank As Integer = arySrc.Rank
                    Dim lenSrcLastRank As Integer = arySrc.GetLength(LastRank - 1)
                    Dim lenDestLastRank As Integer = aryDest.GetLength(LastRank - 1)
                    If lenDestLastRank = 0 Then
                        Return aryDest
                    End If
                    Dim lenCopy As Integer = If(lenSrcLastRank > lenDestLastRank, lenDestLastRank, lenSrcLastRank)
                    Dim i As Integer
                    For i = 0 To (arySrc.Length \ lenSrcLastRank) - 1
                        Global.System.Array.Copy(arySrc, i * lenSrcLastRank, aryDest, i * lenDestLastRank, lenCopy)
                    Next i
                Else
                    Global.System.Array.Copy(arySrc, aryDest, lLength)
                End If
                Return aryDest
            End Function
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class ObjectFlowControl
            Friend NotInheritable Class ForLoopControl
                Public Shared Function ForNextCheckR4(count As Single, limit As Single, StepValue As Single) As Boolean
                    If StepValue >= 0 Then
                        Return count <= limit
                    Else
                        Return count >= limit
                    End If
                End Function
                Public Shared Function ForNextCheckR8(count As Double, limit As Double, StepValue As Double) As Boolean
                    If StepValue >= 0 Then
                        Return count <= limit
                    Else
                        Return count >= limit
                    End If
                End Function
                Public Shared Function ForNextCheckDec(count As Decimal, limit As Decimal, StepValue As Decimal) As Boolean
                    If StepValue >= 0 Then
                        Return count <= limit
                    Else
                        Return count >= limit
                    End If
                End Function
            End Class
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class StaticLocalInitFlag
            Public State As Short
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.Diagnostics.DebuggerNonUserCode(), Global.System.Runtime.CompilerServices.CompilerGenerated()>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        Friend NotInheritable Class IncompleteInitialization
            Inherits Global.System.Exception
            Public Sub New()
                MyBase.New()
            End Sub
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.AttributeUsage(Global.System.AttributeTargets.Class, Inherited:=False)>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        <Global.System.Runtime.CompilerServices.CompilerGenerated()>
        Friend NotInheritable Class StandardModuleAttribute
            Inherits Global.System.Attribute
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.AttributeUsage(Global.System.AttributeTargets.Class, Inherited:=False)>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        <Global.System.Runtime.CompilerServices.CompilerGenerated()>
        Friend NotInheritable Class DesignerGeneratedAttribute
            Inherits Global.System.Attribute
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.AttributeUsage(Global.System.AttributeTargets.Parameter, Inherited:=False)>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        <Global.System.Runtime.CompilerServices.CompilerGenerated()>
        Friend NotInheritable Class OptionCompareAttribute
            Inherits Global.System.Attribute
        End Class
        <Global.Microsoft.VisualBasic.Embedded()>
        <Global.System.AttributeUsage(Global.System.AttributeTargets.Class, Inherited:=False)>
        <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
        <Global.System.Runtime.CompilerServices.CompilerGenerated()>
        Friend NotInheritable Class OptionTextAttribute
            Inherits Global.System.Attribute
        End Class
    End Namespace
    <Global.Microsoft.VisualBasic.Embedded()>
    <Global.System.AttributeUsage(Global.System.AttributeTargets.Class, Inherited:=False)>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    <Global.System.Runtime.CompilerServices.CompilerGenerated()>
    Friend NotInheritable Class HideModuleNameAttribute
        Inherits Global.System.Attribute
    End Class
    <Global.Microsoft.VisualBasic.Embedded(), Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.Runtime.CompilerServices.CompilerGenerated()>
    <Global.Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute>
    Friend Module Strings
        Public Function ChrW(CharCode As Integer) As Char
            If CharCode < -32768 OrElse CharCode > 65535 Then
                Throw New Global.System.ArgumentException()
            End If
            Return Global.System.Convert.ToChar(CharCode And &HFFFFI)
        End Function
        Public Function AscW([String] As String) As Integer
            If ([String] Is Nothing) OrElse ([String].Length = 0) Then
                Throw New Global.System.ArgumentException()
            End If
            Return AscW([String].Chars(0))
        End Function
        Public Function AscW([String] As Char) As Integer
            Return AscW([String])
        End Function
    End Module
    <Global.Microsoft.VisualBasic.Embedded(), Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.Runtime.CompilerServices.CompilerGenerated()>
    <Global.Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute>
    Friend Module Constants
        Public Const vbCrLf As String = ChrW(13) & ChrW(10)
        Public Const vbNewLine As String = ChrW(13) & ChrW(10)
        Public Const vbCr As String = ChrW(13)
        Public Const vbLf As String = ChrW(10)
        Public Const vbBack As String = ChrW(8)
        Public Const vbFormFeed As String = ChrW(12)
        Public Const vbTab As String = ChrW(9)
        Public Const vbVerticalTab As String = ChrW(11)
        Public Const vbNullChar As String = ChrW(0)
        Public Const vbNullString As String = Nothing
    End Module
End Namespace
