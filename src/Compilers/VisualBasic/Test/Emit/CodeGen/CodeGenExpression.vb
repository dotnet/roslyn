' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenExpression
        Inherits BasicTestBase

        <Fact, WorkItem(25692, "https://github.com/dotnet/roslyn/issues/25692")>
        Public Sub CIntFix_01()
            Dim source =
<compilation>
    <file name="a.vb">
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
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
End Class
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
    <file name="a.vb">
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
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " &amp; s)
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " &amp; s)
    End Sub
End Class
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
    <file name="a.vb">
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
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
End Class
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
    <file name="a.vb">
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
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckSingle(s As Single)
        Try
            Dim result As Integer = SingleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " &amp; s)
    End Sub
    Public Shared Sub CheckDouble(s As Double, expected As Integer)
        Dim result As Integer = DoubleToInteger(s)
        If result &lt;> expected
            Throw New Exception("Error on " &amp; s &amp; " " &amp; expected)
        End If
    End Sub
    Public Shared Sub CheckDouble(s As Double)
        Try
            Dim result As Integer = DoubleToInteger(s)
        Catch ex As OverflowException
            Return
        End Try
        Throw New Exception("Error on " &amp; s)
    End Sub
End Class
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
    End Class
End Namespace
