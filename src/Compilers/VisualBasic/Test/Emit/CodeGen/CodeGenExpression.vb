' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenExpression
        Inherits BasicTestBase

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFix_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return CSByte(Fix(number))
    End Function
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Fix(number))
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return CByte(Fix(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Fix(number))
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return CShort(Fix(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Fix(number))
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return CUShort(Fix(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Fix(number))
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return CInt(Fix(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Fix(number))
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return CUInt(Fix(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Fix(number))
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return CLng(Fix(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Fix(number))
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return CULng(Fix(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Fix(number))
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 65F, Integer.MinValue)
        CheckSingle(Integer.MinValue +  0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 64F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 65F, -2147483520)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 65F, 2147483520)
        CheckSingle(Integer.MaxValue - 64F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue -  0F, Integer.MinValue)  ' overflow

        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFix_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return CSByte(Fix(number))
    End Function
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Fix(number))
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return CByte(Fix(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Fix(number))
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return CShort(Fix(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Fix(number))
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return CUShort(Fix(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Fix(number))
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return CInt(Fix(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Fix(number))
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return CUInt(Fix(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Fix(number))
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return CLng(Fix(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Fix(number))
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return CULng(Fix(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Fix(number))
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 65F, Integer.MinValue)
        CheckSingle(Integer.MinValue +  0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 64F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 65F, -2147483520)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 65F, 2147483520)
        CheckSingle(Integer.MaxValue - 64F)  ' overflow
        CheckSingle(Integer.MaxValue -  0F)  ' overflow

        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFix_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return Fix(number)
    End Function
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Fix(number)
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return Fix(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Fix(number)
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return Fix(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Fix(number)
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return Fix(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Fix(number)
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return Fix(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Fix(number)
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return Fix(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Fix(number)
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return Fix(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Fix(number)
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return Fix(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Fix(number)
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 65F, Integer.MinValue)
        CheckSingle(Integer.MinValue +  0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 64F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 65F, -2147483520)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 65F, 2147483520)
        CheckSingle(Integer.MaxValue - 64F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue -  0F, Integer.MinValue)  ' overflow

        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFix_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return Fix(number)
    End Function
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Fix(number)
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return Fix(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Fix(number)
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return Fix(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Fix(number)
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return Fix(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Fix(number)
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return Fix(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Fix(number)
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return Fix(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Fix(number)
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return Fix(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Fix(number)
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return Fix(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Fix(number)
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 65F, Integer.MinValue)
        CheckSingle(Integer.MinValue +  0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 64F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 65F, -2147483520)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 65F, 2147483520)
        CheckSingle(Integer.MaxValue - 64F)  ' overflow
        CheckSingle(Integer.MaxValue -  0F)  ' overflow

        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntTruncate_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Truncate(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntTruncate_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Truncate(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Truncate(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntTruncate_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Truncate(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntTruncate_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Truncate(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Truncate(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u1
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u2
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i8
  IL_0002:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u8
  IL_0002:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntCeiling_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Ceiling(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntCeiling_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Ceiling(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Ceiling(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D)  ' overflow
        CheckDouble(Integer.MaxValue + 0.99D)  ' overflow
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntCeiling_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Ceiling(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntCeiling_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Ceiling(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Ceiling(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D)  ' overflow
        CheckDouble(Integer.MaxValue + 0.99D)  ' overflow
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Ceiling(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFloor_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Floor(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Floor(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Floor(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Floor(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Floor(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Floor(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Floor(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Floor(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFloor_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Floor(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Floor(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Floor(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Floor(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Floor(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Floor(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Floor(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Floor(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D)  ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFloor_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Floor(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow
        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFloor_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Floor(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Floor(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Floor(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntRound_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Round(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Round(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Round(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Round(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Round(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Round(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Round(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Round(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntRound_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Math.Round(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Math.Round(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Math.Round(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Math.Round(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Math.Round(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Math.Round(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Math.Round(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Math.Round(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D)  ' overflow
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntRound_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Round(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

    Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntRound_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Math.Round(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Math.Round(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 2)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D)  ' overflow
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function System.Math.Round(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Int(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Int(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Int(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Int(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Int(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Int(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Int(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Int(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return CSByte(Int(number))
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return CByte(Int(number))
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return CShort(Int(number))
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return CUShort(Int(number))
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return CInt(Int(number))
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return CUInt(Int(number))
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return CLng(Int(number))
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return CULng(Int(number))
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D)  ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Int(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Int(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Int(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Int(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Int(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Int(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Int(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Int(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D, Integer.MinValue) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D, Integer.MinValue)  ' overflow
        CheckDouble(Integer.MaxValue + 2D, Integer.MinValue)  ' overflow
        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function DoubleToSByte(number as Double) As SByte
        Return Int(number)
    End Function
    Public Shared Function DoubleToByte(number as Double) As Byte
        Return Int(number)
    End Function
    Public Shared Function DoubleToShort(number as Double) As Short
        Return Int(number)
    End Function
    Public Shared Function DoubleToUShort(number as Double) As UShort
        Return Int(number)
    End Function
    Public Shared Function DoubleToInteger(number as Double) As Integer
        Return Int(number)
    End Function
    Public Shared Function DoubleToUInteger(number as Double) As UInteger
        Return Int(number)
    End Function
    Public Shared Function DoubleToLong(number as Double) As Long
        Return Int(number)
    End Function
    Public Shared Function DoubleToULong(number as Double) As ULong
        Return Int(number)
    End Function
    Public Shared Sub Main()
        CheckDouble(Integer.MinValue - 1D)  ' overflow
        CheckDouble(Integer.MinValue - 0.01D) ' overflow
        CheckDouble(Integer.MinValue + 0D, Integer.MinValue)
        CheckDouble(Integer.MinValue + 1D, -2147483647)
        CheckDouble(1.99D, 1)
        CheckDouble(Integer.MaxValue - 1D, 2147483646)
        CheckDouble(Integer.MaxValue - 0.01D, 2147483646)
        CheckDouble(Integer.MaxValue + 0D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.01D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 0.99D, Integer.MaxValue)
        CheckDouble(Integer.MaxValue + 1D)  ' overflow
        CheckDouble(Integer.MaxValue + 2D)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.DoubleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.DoubleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Double) As Double"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Single_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return CSByte(Int(number))
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return CByte(Int(number))
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return CShort(Int(number))
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return CUShort(Int(number))
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return CInt(Int(number))
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return CUInt(Int(number))
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return CLng(Int(number))
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return CULng(Int(number))
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 1F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MinValue - 0.01F, Integer.MinValue) ' overflow
        CheckSingle(Integer.MinValue + 0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 1F, Integer.MinValue)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 65F, 2147483520)
        CheckSingle(Integer.MaxValue - 60F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 2F, Integer.MinValue)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected  & " " & result)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Single_Checked_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return CSByte(Int(number))
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return CByte(Int(number))
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return CShort(Int(number))
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return CUShort(Int(number))
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return CInt(Int(number))
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return CUInt(Int(number))
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return CLng(Int(number))
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return CULng(Int(number))
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 1F, Integer.MinValue)
        CheckSingle(Integer.MinValue - 0.01F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 1F, Integer.MinValue)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 1F)  ' overflow
        CheckSingle(Integer.MaxValue - 0.01F)  ' overflow
        CheckSingle(Integer.MaxValue + 0F)  ' overflow
        CheckSingle(Integer.MaxValue + 0.01F)  ' overflow
        CheckSingle(Integer.MaxValue + 0.99F)  ' overflow
        CheckSingle(Integer.MaxValue + 1F)  ' overflow
        CheckSingle(Integer.MaxValue + 2F)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
            Throw New Exception("No exception on " & s & " got " & result)
        Catch ex As OverflowException
            Return
        End Try
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Single_Implicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return Int(number)
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return Int(number)
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return Int(number)
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return Int(number)
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return Int(number)
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return Int(number)
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return Int(number)
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return Int(number)
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 1F, Integer.MinValue)
        CheckSingle(Integer.MinValue - 1F, Integer.MinValue)
        CheckSingle(Integer.MinValue - 0.01F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 1F, Integer.MinValue)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 1F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue - 0.01F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 0F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 0.01F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 0.99F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 1F, Integer.MinValue)  ' overflow
        CheckSingle(Integer.MaxValue + 2F, Integer.MinValue)  ' overflow
        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(False))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.u8
  IL_0007:  ret
}
]]>)
        End Sub

        <WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        <ConditionalFact(GetType(DesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub CIntInt_Single_CheckedImplicit_01()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports Microsoft.VisualBasic

Class C1
    Public Shared Function SingleToSByte(number as Single) As SByte
        Return Int(number)
    End Function
    Public Shared Function SingleToByte(number as Single) As Byte
        Return Int(number)
    End Function
    Public Shared Function SingleToShort(number as Single) As Short
        Return Int(number)
    End Function
    Public Shared Function SingleToUShort(number as Single) As UShort
        Return Int(number)
    End Function
    Public Shared Function SingleToInteger(number as Single) As Integer
        Return Int(number)
    End Function
    Public Shared Function SingleToUInteger(number as Single) As UInteger
        Return Int(number)
    End Function
    Public Shared Function SingleToLong(number as Single) As Long
        Return Int(number)
    End Function
    Public Shared Function SingleToULong(number as Single) As ULong
        Return Int(number)
    End Function
    Public Shared Sub Main()
        CheckSingle(Integer.MinValue - 1F, Integer.MinValue)
        CheckSingle(Integer.MinValue - 0.01F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 0F, Integer.MinValue)
        CheckSingle(Integer.MinValue + 1F, Integer.MinValue)
        CheckSingle(1.99F, 1)
        CheckSingle(Integer.MaxValue - 1F)  ' overflow
        CheckSingle(Integer.MaxValue - 0.01F)  ' overflow
        CheckSingle(Integer.MaxValue + 0F)  ' overflow
        CheckSingle(Integer.MaxValue + 0.01F)  ' overflow
        CheckSingle(Integer.MaxValue + 0.99F)  ' overflow
        CheckSingle(Integer.MaxValue + 1F)  ' overflow
        CheckSingle(Integer.MaxValue + 2F)  ' overflow

        Console.WriteLine("done")
    End Sub

    Public Shared Sub CheckSingle(s As Single, expected As Integer)
        Dim result As Integer = SingleToInteger(s)
        If result <> expected
            Throw New Exception("Error on " & s & " " & expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " & s)
    End Sub
End Class
]]>
    </file>
</compilation>
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(source, options:=TestOptions.ReleaseExe.WithOverflowChecks(True))
            Dim cv = CompileAndVerify(compilation,
                            expectedOutput:=<![CDATA[
done
]]>)
            cv.VerifyIL("C1.SingleToSByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToByte", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u1
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUShort", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u2
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToUInteger", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u4
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToLong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.i8
  IL_0007:  ret
}
]]>)
            cv.VerifyIL("C1.SingleToULong", <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Conversion.Int(Single) As Single"
  IL_0006:  conv.ovf.u8
  IL_0007:  ret
}
]]>)
        End Sub
    End Class
End Namespace
