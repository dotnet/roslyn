' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ValueFormatterTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub IntegralPrimitives()
            ' only testing a couple simple cases here...more tests live in ObjectDisplayTests...
            Assert.Equal("1", FormatValue(CUShort(1)))
            Assert.Equal("65535", FormatValue(UShort.MaxValue))
            Assert.Equal("1", FormatValue(CInt(1)))
            Assert.Equal("-1", FormatValue(CInt(-1)))

            Assert.Equal("&H00", FormatValue(CSByte(0), useHexadecimal:=True))
            Assert.Equal("&H01", FormatValue(CSByte(1), useHexadecimal:=True))
            Assert.Equal("&HFFFFFFFF", FormatValue(CSByte(-1), useHexadecimal:=True))
            Assert.Equal("&H0000000000000000", FormatValue(CULng(0), useHexadecimal:=True))
            Assert.Equal("&H0000000000000001", FormatValue(CULng(1), useHexadecimal:=True))
            Assert.Equal("&HFFFFFFFFFFFFFFFF", FormatValue(ULong.MaxValue, useHexadecimal:=True))
        End Sub

        <Fact>
        Public Sub Doubles()
            Assert.Equal("-1.7976931348623157E+308", FormatValue(Double.MinValue))
            Assert.Equal("-1.1", FormatValue(CDbl(-1.1)))
            Assert.Equal("0", FormatValue(CDbl(0)))
            Assert.Equal("1.1", FormatValue(CDbl(1.1)))
            Assert.Equal("1.7976931348623157E+308", FormatValue(Double.MaxValue))

            Assert.Equal("-Infinity", FormatValue(Double.NegativeInfinity))
            Assert.Equal("Infinity", FormatValue(Double.PositiveInfinity))
            Assert.Equal("NaN", FormatValue(Double.NaN))
            Assert.Equal("4.94065645841247E-324", FormatValue(Double.Epsilon))
        End Sub

        <Fact>
        Public Sub Singles()
            Assert.Equal("-3.40282347E+38", FormatValue(Single.MinValue))
            Assert.Equal("-1.1", FormatValue(CSng(-1.1)))
            Assert.Equal("0", FormatValue(CSng(0)))
            Assert.Equal("1.1", FormatValue(CSng(1.1)))
            Assert.Equal("3.40282347E+38", FormatValue(Single.MaxValue))

            Assert.Equal("-Infinity", FormatValue(Single.NegativeInfinity))
            Assert.Equal("Infinity", FormatValue(Single.PositiveInfinity))
            Assert.Equal("NaN", FormatValue(Single.NaN))
            Assert.Equal("1.401298E-45", FormatValue(Single.Epsilon))
        End Sub

        <Fact>
        Public Sub Decimals()
            Assert.Equal("-79228162514264337593543950335", FormatValue(Decimal.MinValue))
            Assert.Equal("-1.1", FormatValue(CDec(-1.1)))
            Assert.Equal("0", FormatValue(CDec(0)))
            Assert.Equal("1.1", FormatValue(CDec(1.1)))
            Assert.Equal("79228162514264337593543950335", FormatValue(Decimal.MaxValue))
        End Sub

        <Fact>
        Public Sub Booleans()
            Assert.Equal("True", FormatValue(True))
            Assert.Equal("False", FormatValue(False))
        End Sub

        <Fact>
        Public Sub Chars()
            ' We'll exhaustively test the first 256 code points (single-byte characters) as well
            ' as a few double-byte characters.  Testing all possible characters takes too long.
            Dim ch As Char
            For i = 0 To &HFF
                ch = ChrW(i)
                Dim expected As String
                Dim expectedHex As String = Nothing
                ' NOTE: The old EE used to just display " " for non-printable characters.
                '       This has been changed in Roslyn to display the equivalent VB constants
                '       or the actual numeric code point value (this seems far more useful).
                Select Case CStr(ch)
                    Case vbNullChar
                        expected = "vbNullChar"
                    Case vbBack
                        expected = "vbBack"
                    Case vbCr
                        expected = "vbCr"
                    Case vbFormFeed
                        expected = "vbFormFeed"
                    Case vbLf
                        expected = "vbLf"
                    Case vbTab
                        expected = "vbTab"
                    Case vbVerticalTab
                        expected = "vbVerticalTab"
                    Case """"c
                        expected = """""""""c"
                    Case Else
                        If ObjectDisplay.IsPrintable(ch) Then
                            expected = """" & ch & """c"
                        Else
                            expected = "ChrW(" & i & ")"
                            expectedHex = "ChrW(&H" & i.ToString("X") & ")"
                        End If
                End Select

                Assert.Equal(expected, FormatValue(ch))
                Assert.Equal(If(expectedHex, expected), FormatValue(ch, useHexadecimal:=True))
            Next

            For Each ch In {ChrW(&HABCD), ChrW(&HFEEF)}
                Assert.Equal("""" & ch & """c", FormatValue(ch))
                Assert.Equal("""" & ch & """c", FormatValue(ch, useHexadecimal:=True))
            Next

            ch = ChrW(&HFFEF)
            Assert.Equal("ChrW(65519)", FormatValue(ch))
            Assert.Equal("ChrW(&HFFEF)", FormatValue(ch, useHexadecimal:=True))

            Assert.Equal("ChrW(65535)", FormatValue(Char.MaxValue))
            Assert.Equal("ChrW(&HFFFF)", FormatValue(Char.MaxValue, useHexadecimal:=True))
        End Sub

        <Fact>
        Public Sub Strings()
            Assert.Equal("Nothing", FormatNull(Of String)())
            Assert.Equal("Nothing", FormatNull(Of String)(useHexadecimal:=True))

            ' We'll exhaustively test the first 256 code points (single-byte characters) as well
            ' as a few multi-byte characters.  Testing all possible characters takes too long.
            Dim ch As Char
            For i = 0 To &HFF
                ch = ChrW(i)
                Dim s As String = "a" & ch
                Dim expected As String
                Dim expectedHex As String = Nothing
                Select Case CStr(ch)
                    Case vbNullChar
                        expected = """a"" & vbNullChar"
                    Case vbTab
                        expected = """a"" & vbTab"
                    Case vbCr
                        expected = """a"" & vbCr"
                    Case vbFormFeed
                        expected = """a"" & vbFormFeed"
                    Case vbLf
                        expected = """a"" & vbLf"
                    Case vbBack
                        expected = """a"" & vbBack"
                    Case vbVerticalTab
                        expected = """a"" & vbVerticalTab"
                    Case """"
                        expected = """a"""""""
                    Case Else
                        If ObjectDisplay.IsPrintable(ch) Then
                            expected = """" & s & """"
                        Else
                            expected = """a"" & ChrW(" & i & ")"
                            expectedHex = """a"" & ChrW(&H" & i.ToString("X") & ")"
                        End If
                End Select

                Assert.Equal(expected, FormatValue(s))
                Assert.Equal(If(expectedHex, expected), FormatValue(s, useHexadecimal:=True))
            Next

            Assert.Equal("""a"" & vbNullChar & ""b""", FormatValue("a" & vbNullChar & "b"))

            For Each ch In {ChrW(&HABCD), ChrW(&HFEEF)}
                Assert.Equal("""" & ch & """", FormatValue(CStr(ch)))
                Assert.Equal("""" & ch & """", FormatValue(CStr(ch), useHexadecimal:=True))
            Next

            ch = ChrW(&HFFEF)
            Assert.Equal("ChrW(65519)", FormatValue(ch))
            Assert.Equal("ChrW(&HFFEF)", FormatValue(ch, useHexadecimal:=True))

            Assert.Equal("ChrW(65535)", FormatValue(CStr(Char.MaxValue)))
            Assert.Equal("ChrW(&HFFFF)", FormatValue(CStr(Char.MaxValue), useHexadecimal:=True))

            Dim multiByte = ChrW(&HD83C) & ChrW(&HDFC8)
            Assert.Equal("""🏈""", FormatValue(multiByte))
            Assert.Equal("""🏈""", FormatValue(multiByte, useHexadecimal:=True))
            Assert.Equal("🏈", multiByte)

            multiByte = ChrW(&HDFC8) & ChrW(&HD83C)
            Assert.Equal("ChrW(57288) & ChrW(55356)", FormatValue(multiByte))
            Assert.Equal("ChrW(&HDFC8) & ChrW(&HD83C)", FormatValue(multiByte, useHexadecimal:=True))
        End Sub

        <Fact>
        Public Sub Void()
            ' Something happens but, in practice, we expect the debugger to recognize
            ' that the value is of type void and turn it into the error string 
            ' "Expression has been evaluated and has no value".
            Assert.Equal("{System.Void}", FormatValue(Nothing, GetType(Void)))
        End Sub

        <Fact>
        Public Sub InvalidValue_1()
            Const errorMessage = "An error has occurred."
            Dim clrValue = CreateDkmClrValue(errorMessage, GetType(String), evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.Error)
            Assert.Equal(errorMessage, (DirectCast(FormatResult("invalidIdentifier", clrValue), DkmFailedEvaluationResult)).ErrorMessage)
        End Sub

        <Fact>
        Public Sub InvalidValue_2()
            Const errorMessage = "An error has occurred."
            Dim clrValue = CreateDkmClrValue(errorMessage, GetType(Integer), evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.Error)
            Assert.Equal(errorMessage, (DirectCast(FormatResult("invalidIdentifier", clrValue), DkmFailedEvaluationResult)).ErrorMessage)
        End Sub

        <Fact>
        Public Sub NonFlagsEnum()
            Dim source = "
Enum E
    A = 1
    B = 2
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("0", FormatValue(0, type))
            Assert.Equal("A {1}", FormatValue(1, type))
            Assert.Equal("B {2}", FormatValue(2, type))
            Assert.Equal("3", FormatValue(3, type))
        End Sub

        <Fact>
        Public Sub NonFlagsEnum_Negative()
            Dim source = "
Enum E
    A = -1
    B = -2
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("0", FormatValue(0, type))
            Assert.Equal("A {-1}", FormatValue(-1, type))
            Assert.Equal("B {-2}", FormatValue(-2, type))
            Assert.Equal("-3", FormatValue(-3, type))
        End Sub

        <Fact>
        Public Sub NonFlagsEnum_Order()
            Dim source = "
Enum E1
    A = 1
    B = 1
End ENum

Enum E2
    B = 1
    A = 1
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim e1 = assembly.GetType("E1")
            Dim e2 = assembly.GetType("E2")

            ' VB always picks field that is lexically "first"
            Assert.Equal("A {1}", FormatValue(1, e1))
            Assert.Equal("A {1}", FormatValue(1, e2))
        End Sub

        <Fact>
        Public Sub FlagsEnum()
            Dim source = "
Imports System

<Flags>
Enum E
    A = 1
    B = 2
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("0", FormatValue(0, type))
            Assert.Equal("A {1}", FormatValue(1, type))
            Assert.Equal("B {2}", FormatValue(2, type))
            Assert.Equal("A Or B {3}", FormatValue(3, type))
            Assert.Equal("4", FormatValue(4, type))
        End Sub

        <Fact>
        Public Sub FlagsEnum_Zero()
            Dim source = "
Imports System

<Flags>
Enum E
    None = 0
    A = 1
    B = 2
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("None {0}", FormatValue(0, type))
            Assert.Equal("A {1}", FormatValue(1, type))
            Assert.Equal("B {2}", FormatValue(2, type))
            Assert.Equal("A Or B {3}", FormatValue(3, type))
            Assert.Equal("4", FormatValue(4, type))
        End Sub

        <Fact>
        Public Sub FlagsEnum_Combination()
            Dim source = "
Imports System

<Flags>
Enum E
    None = 0
    A = 1
    B = 2
    C = A Or B
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("None {0}", FormatValue(0, type))
            Assert.Equal("A {1}", FormatValue(1, type))
            Assert.Equal("B {2}", FormatValue(2, type))
            Assert.Equal("C {3}", FormatValue(3, type))
            Assert.Equal("4", FormatValue(4, type))
        End Sub

        <Fact>
        Public Sub FlagsEnum_Negative()
            Dim source = "
Imports System

<Flags>
Enum E
    None = 0
    A = -1
    B = -2
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim type = assembly.GetType("E")

            Assert.Equal("None {0}", FormatValue(0, type))
            Assert.Equal("A {-1}", FormatValue(-1, type))
            Assert.Equal("B {-2}", FormatValue(-2, type))
            Assert.Equal("-3", FormatValue(-3, type))
            Assert.Equal("-4", FormatValue(-4, type))
        End Sub

        <Fact>
        Public Sub FlagsEnum_Order()
            Dim source = "
Imports System

<Flags>
Enum E1
    A = 1
    B = 1
    C = 2
    D = 2
End Enum

<Flags>
Enum E2
    D = 2
    C = 2
    B = 1
    A = 1
End Enum
"
            Dim assembly = GetAssembly(source)

            Dim e1 = assembly.GetType("E1")
            Dim e2 = assembly.GetType("E2")

            Assert.Equal("0", FormatValue(0, e1))
            Assert.Equal("A {1}", FormatValue(1, e1))
            Assert.Equal("C {2}", FormatValue(2, e1))
            Assert.Equal("A Or C {3}", FormatValue(3, e1))

            Assert.Equal("0", FormatValue(0, e2))
            Assert.Equal("A {1}", FormatValue(1, e2))
            Assert.Equal("C {2}", FormatValue(2, e2))
            Assert.Equal("A Or C {3}", FormatValue(3, e2))
        End Sub

        <Fact>
        Public Sub Arrays()
            Dim source = "
Namespace N
    Public Class A(Of T)
        Public Class B(Of U)
        End Class
    End Class
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("N.A`1")
            Dim typeB = typeA.GetNestedType("B`1")
            Dim constructedType = typeB.MakeGenericType(GetType(Boolean), GetType(Long))

            Dim vectorInstance = Array.CreateInstance(constructedType, 2)
            Dim matrixInstance = Array.CreateInstance(constructedType, 3, 4)
            Dim irregularInstance = Array.CreateInstance(constructedType, {1, 2}, {3, 4})

            Assert.Equal("{Length=2}", FormatValue(vectorInstance))
            Assert.Equal("{Length=12}", FormatValue(matrixInstance))
            Assert.Equal("{Length=2}", FormatValue(irregularInstance))

            Assert.Equal("{Length=2}", FormatValue(vectorInstance, useHexadecimal:=True))
            Assert.Equal("{Length=12}", FormatValue(matrixInstance, useHexadecimal:=True))
            Assert.Equal("{Length=2}", FormatValue(irregularInstance, useHexadecimal:=True))
        End Sub

        <Fact>
        Public Sub Pointers()
            Dim pointerType = GetType(Integer).MakePointerType()
            Dim doublePointerType = pointerType.MakePointerType()

            Assert.Equal("&H00000001", FormatValue(1, pointerType, useHexadecimal:=False)) ' In hex, regardless.
            Assert.Equal("&H00000001", FormatValue(1, pointerType, useHexadecimal:=True))

            Assert.Equal("&HFFFFFFFF", FormatValue(-1, doublePointerType, useHexadecimal:=False)) ' In hex, regardless.
            Assert.Equal("&HFFFFFFFF", FormatValue(-1, doublePointerType, useHexadecimal:=True))
        End Sub

        <Fact>
        Public Sub Nullable()
            Dim source = "
Namespace N
    Public Structure A(Of T)
        Public Structure B(Of U)
            Public Overrides Function ToString() As String
                Return ""ToString() called.""
            End Function
        End Structure
    End Structure
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim typeA = assembly.GetType("N.A`1")
            Dim typeB = typeA.GetNestedType("B`1")
            Dim constructedType = typeB.MakeGenericType(GetType(Boolean), GetType(Long))
            Dim nullableType = GetType(Nullable(Of ))
            Dim nullableConstructedType = nullableType.MakeGenericType(constructedType)
            Dim nullableInt = nullableType.MakeGenericType(GetType(Integer))

            Assert.Equal("Nothing", FormatValue(Nothing, nullableConstructedType))
            Assert.Equal("{ToString() called.}", FormatValue(constructedType.Instantiate(), nullableConstructedType))

            Assert.Equal("Nothing", FormatValue(Nothing, nullableInt))
            Assert.Equal("1", FormatValue(1, nullableInt))
        End Sub

        <Fact>
        Public Sub ToStringOverrides()
            Dim source = "
Public Class A(Of T)
End Class

Public Class B : Inherits A(Of Integer)
    Public Overrides Function ToString() As String
        Return ""B.ToString()""
    End Function
End Class

Public Class C : Inherits B
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeC = assembly.GetType("C")
            Dim typeB = assembly.GetType("B")
            Dim typeA = typeB.BaseType

            Assert.Equal("Nothing", FormatValue(Nothing, typeA))
            Assert.Equal("Nothing", FormatValue(Nothing, typeB))
            Assert.Equal("Nothing", FormatValue(Nothing, typeC))

            Assert.Equal("{A(Of Integer)}", FormatValue(typeA.Instantiate()))
            Assert.Equal("{B.ToString()}", FormatValue(typeB.Instantiate()))
            Assert.Equal("{B.ToString()}", FormatValue(typeC.Instantiate()))
        End Sub

        <Fact>
        Public Sub ValuesWithUnderlyingString()
            Assert.True(HasUnderlyingString("Test"))
            Assert.False(HasUnderlyingString(Nothing, GetType(String)))
            Assert.False(HasUnderlyingString(0))
            Assert.False(HasUnderlyingString(DkmEvaluationFlags.None)) ' Enum
            Assert.False(HasUnderlyingString("a"c))
            Assert.False(HasUnderlyingString({1, 2, 3}))
            Assert.False(HasUnderlyingString(New Object()))
            Assert.False(HasUnderlyingString(0, GetType(Integer).MakePointerType()))
        End Sub

        <Fact>
        Public Sub VisualizeString()
            Assert.Equal(vbCrLf, GetUnderlyingString(vbCrLf))
        End Sub

        <Fact>
        Public Sub VisualizeSqlString()
            Dim source = "
Namespace System.Data.SqlTypes
    Public Structure SqlString
        Private m_value As String

        Public Sub New(data As String)
            m_value = data
        End Sub
    End Structure
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("System.Data.SqlTypes.SqlString")

            Dim sqlString = type.Instantiate({"Test"})

            Assert.Equal("Test", GetUnderlyingString(sqlString))
        End Sub

        <Fact>
        Public Sub VisualizeXNode()
            Dim source = "
Namespace System.Xml.Linq
    Public Class XNode
        Public Overrides Function ToString() As String
            Return ""Test1""
        End Function
    End Class

    Public Class XContainer : Inherits XNode
    End Class

    Public Class XElement : Inherits XContainer
        Public Overrides Function ToString() As String
            Return ""Test2""
        End Function
    End Class
End Namespace
"
            Dim assembly = GetAssembly(source)
            Dim xnType = assembly.GetType("System.Xml.Linq.XNode")
            Dim xeType = assembly.GetType("System.Xml.Linq.XElement")

            Assert.Equal("Test1", GetUnderlyingString(xnType.Instantiate()))
            Assert.Equal("Test2", GetUnderlyingString(xeType.Instantiate()))
        End Sub

        <Fact>
        Public Sub Dates()
            Assert.Equal("#1/1/0001 12:00:00 AM#", FormatValue(New Date(0)))
            Assert.Equal("#1/1/1970 12:00:00 AM#", FormatValue(New Date(1970, 1, 1)))
            Assert.Equal("#1/1/0001 12:00:00 PM#", FormatValue(New Date(1, 1, 1, 12, 0, 0, 0)))
            Assert.Equal("#1/1/0001 12:00:00 PM#", FormatValue(New Date(1, 1, 1, 12, 0, 0, 0), useHexadecimal:=True)) ' Hexadecimal setting shouldn't change output
            ' DateTimeKind is stored in the top two bits of the ULong value that backs the DateTime instance.
            ' We need to make sure we don't throw an Exception when those bits are set to non-zero values.
            Assert.Equal("#1/1/1970 12:00:00 AM#", FormatValue(New Date(&H89F7FF5F7B58000, DateTimeKind.Local)))
            Assert.Equal("#1/1/1970 12:00:00 AM#", FormatValue(New Date(&H89F7FF5F7B58000, DateTimeKind.Utc)))
        End Sub

        <Fact>
        Public Sub HostValueNotFound_Integer()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(Integer), TypeImpl)),
                alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        <Fact>
        Public Sub HostValueNotFound_char()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(Char), TypeImpl)),
                                           alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        <Fact>
        Public Sub HostValueNotFound_IntPtr()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(IntPtr), TypeImpl)),
                                           alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        <Fact>
        Public Sub HostValueNotFound_UIntPtr()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(UIntPtr), TypeImpl)),
                                           alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        <Fact>
        Public Sub HostValueNotFound_Enum()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(TestEnum), TypeImpl)),
                                           alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        ' DateTime is a primitive type in VB but not in C#.
        <Fact>
        Public Sub HostValueNotFound_DateTime()
            Dim clrValue = New DkmClrValue(value:=Nothing, hostObjectValue:=Nothing, New DkmClrType(CType(GetType(DateTime), TypeImpl)),
                                           alias:=Nothing, evalFlags:=DkmEvaluationResultFlags.None, valueFlags:=DkmClrValueFlags.None)

            Assert.Equal(Resources.HostValueNotFound, FormatValue(clrValue))
        End Sub

        Private Enum TestEnum
            One
        End Enum

    End Class

End Namespace
