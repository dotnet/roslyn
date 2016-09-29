' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenMultiDimensionalArray
        Inherits BasicTestBase

        <Fact()>
        Public Sub MultiDimensionalArrayCreateWithInitializer()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(1, 2) {{1, 2, 3}, {1, 2, 3}}
        System.Console.Write(arr(1, 1))
    End Sub
End Module
    </file>
            </compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"),
            expectedOutput:="2").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24 <PrivateImplementationDetails>.D64E555B758C5B66DFAC42F18587BB1B3C9BCFA8"
  IL_000d:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.1
  IL_0014:  call       "Integer(*,*).Get"
  IL_0019:  call       "Sub System.Console.Write(Integer)"
  IL_001e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreateWithInitializer001()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(1, 2) {{1, 2, 3}, {1, Integer.Parse("42"), 3}}
        System.Console.Write(arr(1, 1))
    End Sub
End Module
    </file>
            </compilation>, options:=TestOptions.ReleaseExe.WithModuleName("MODULE"),
            expectedOutput:="42").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  5
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldtoken    "<PrivateImplementationDetails>.__StaticArrayInitTypeSize=24 <PrivateImplementationDetails>.A4B74E064E285570B3499538C5B205C3D0972FDF"
  IL_000d:  call       "Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)"
  IL_0012:  dup
  IL_0013:  ldc.i4.1
  IL_0014:  ldc.i4.1
  IL_0015:  ldstr      "42"
  IL_001a:  call       "Function Integer.Parse(String) As Integer"
  IL_001f:  call       "Integer(*,*).Set"
  IL_0024:  ldc.i4.1
  IL_0025:  ldc.i4.1
  IL_0026:  call       "Integer(*,*).Get"
  IL_002b:  call       "Sub System.Console.Write(Integer)"
  IL_0030:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreateWithInitializer002()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(1, 2) {{Integer.Parse("1"), Integer.Parse("2"), Integer.Parse("3")}, {Integer.Parse("4"), Integer.Parse("5"), Integer.Parse("6")}}
        System.Console.Write(arr(1, 1))
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="5").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size      128 (0x80)
  .maxstack  5
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldstr      "1"
  IL_000f:  call       "Function Integer.Parse(String) As Integer"
  IL_0014:  call       "Integer(*,*).Set"
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldc.i4.1
  IL_001c:  ldstr      "2"
  IL_0021:  call       "Function Integer.Parse(String) As Integer"
  IL_0026:  call       "Integer(*,*).Set"
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.2
  IL_002e:  ldstr      "3"
  IL_0033:  call       "Function Integer.Parse(String) As Integer"
  IL_0038:  call       "Integer(*,*).Set"
  IL_003d:  dup
  IL_003e:  ldc.i4.1
  IL_003f:  ldc.i4.0
  IL_0040:  ldstr      "4"
  IL_0045:  call       "Function Integer.Parse(String) As Integer"
  IL_004a:  call       "Integer(*,*).Set"
  IL_004f:  dup
  IL_0050:  ldc.i4.1
  IL_0051:  ldc.i4.1
  IL_0052:  ldstr      "5"
  IL_0057:  call       "Function Integer.Parse(String) As Integer"
  IL_005c:  call       "Integer(*,*).Set"
  IL_0061:  dup
  IL_0062:  ldc.i4.1
  IL_0063:  ldc.i4.2
  IL_0064:  ldstr      "6"
  IL_0069:  call       "Function Integer.Parse(String) As Integer"
  IL_006e:  call       "Integer(*,*).Set"
  IL_0073:  ldc.i4.1
  IL_0074:  ldc.i4.1
  IL_0075:  call       "Integer(*,*).Get"
  IL_007a:  call       "Sub System.Console.Write(Integer)"
  IL_007f:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreateWithInitializer003()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(1, -1) {{}, {}}
        System.Console.Write(arr.Length)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="0").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_000c:  call       "Sub System.Console.Write(Integer)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreateWithInitializer004()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
Public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(-1, -1) {}
        System.Console.Write(arr.Length)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="0").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_000c:  call       "Sub System.Console.Write(Integer)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreate()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(1,2) {}
        System.Console.Write(arr.Length)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="6").
                        VerifyIL("A.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_000c:  call       "Sub System.Console.Write(Integer)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayCreateGeneric()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        foo(Of String)()
        c1(Of Long).foo()
    End Sub

    Class c1(Of T)
        Public Shared Sub foo()
            Dim arr As T(,) = New T(1, 2) {}
            System.Console.Write(arr.Length)
        End Sub
    End Class

    Public Sub foo(Of T)()
        Dim arr As T(,) = New T(1, 2) {}
        System.Console.Write(arr.Length)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="66").
                        VerifyIL("A.foo(Of T)()",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "T(*,*)..ctor"
  IL_0007:  callvirt   "Function System.Array.get_Length() As Integer"
  IL_000c:  call       "Sub System.Console.Write(Integer)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub MultiDimensionalArrayGetSetAddress()
            CompileAndVerify(
            <compilation>
                <file name="a.vb">
public Module A
    Public Sub Main()
        foo(Of String)("hello")
        c1(Of Long).foo(123)
    End Sub

    Class c1(Of T)
        Public Shared Sub foo(e as T)
            Dim arr As T(,) = New T(2, 3) {}
            arr(1, 2) = e

            System.Console.Write(arr(1, 2).ToString)

            Dim v as T = arr(1, 2)
            System.Console.Write(v.ToString)
        End Sub
    End Class

    Public Sub foo(Of T)(e as T)
        Dim arr As T(,) = New T(2, 3) {}
            arr(1, 2) = e

            System.Console.Write(arr(1, 2).ToString)

            Dim v as T = arr(1, 2)
            System.Console.Write(v.ToString)
    End Sub
End Module
    </file>
            </compilation>,
            expectedOutput:="hellohello123123").
                        VerifyIL("A.foo(Of T)(T)",
            <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  5
  .locals init (T V_0) //v
  IL_0000:  ldc.i4.3
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     "T(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.1
  IL_0009:  ldc.i4.2
  IL_000a:  ldarg.0
  IL_000b:  call       "T(*,*).Set"
  IL_0010:  dup
  IL_0011:  ldc.i4.1
  IL_0012:  ldc.i4.2
  IL_0013:  readonly.
  IL_0015:  call       "T(*,*).Address"
  IL_001a:  constrained. "T"
  IL_0020:  callvirt   "Function Object.ToString() As String"
  IL_0025:  call       "Sub System.Console.Write(String)"
  IL_002a:  ldc.i4.1
  IL_002b:  ldc.i4.2
  IL_002c:  call       "T(*,*).Get"
  IL_0031:  stloc.0
  IL_0032:  ldloca.s   V_0
  IL_0034:  constrained. "T"
  IL_003a:  callvirt   "Function Object.ToString() As String"
  IL_003f:  call       "Sub System.Console.Write(String)"
  IL_0044:  ret
}
]]>)
        End Sub

        <WorkItem(542259, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542259")>
        <Fact>
        Public Sub MixMultiAndJaggedArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim x = New Exception(,,) {}
        Dim y = New Exception(,,) {{{}}}
        Dim z = New Exception(,)() {}
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        ' Declaration multi- dimensional array
        <Fact>
        Public Sub DeclarationmultiDimensionalArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim myArray1 As Integer(,) = New Integer(-1, -1) {}
        Dim myArray2 As Integer(,) = New Integer(3, 1) {}
        Dim myArray3 As Integer(,,) = New Integer(3, 1, 2) {}
        Dim myArray4 As Integer(,) = New Integer(2147483646, 2147483646) {}
        Dim myArray5 As Integer(,) = New Integer(2147483648UI - 1, 2147483648UI - 1) {}
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  pop
  IL_0008:  ldc.i4.4
  IL_0009:  ldc.i4.2
  IL_000a:  newobj     "Integer(*,*)..ctor"
  IL_000f:  pop
  IL_0010:  ldc.i4.4
  IL_0011:  ldc.i4.2
  IL_0012:  ldc.i4.3
  IL_0013:  newobj     "Integer(*,*,*)..ctor"
  IL_0018:  pop
  IL_0019:  ldc.i4     0x7fffffff
  IL_001e:  ldc.i4     0x7fffffff
  IL_0023:  newobj     "Integer(*,*)..ctor"
  IL_0028:  pop
  IL_0029:  ldc.i4     0x80000000
  IL_002e:  ldc.i4     0x80000000
  IL_0033:  newobj     "Integer(*,*)..ctor"
  IL_0038:  pop
  IL_0039:  ret
}
]]>)
        End Sub

        ' Declaration multi- dimensional array
        <Fact>
        Public Sub DeclarationmultiDimensionalArray_1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim myArray6 As Integer(,,,,) = Nothing
        myArray6 = New Integer(4, 5, 8, 3, 6) {}
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  5
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.6
  IL_0002:  ldc.i4.s   9
  IL_0004:  ldc.i4.4
  IL_0005:  ldc.i4.7
  IL_0006:  newobj     "Integer(*,*,*,*,*)..ctor"
  IL_000b:  pop
  IL_000c:  ret
}
]]>)
        End Sub

        ' Declaration multi- dimensional array
        <Fact>
        Public Sub DeclarationmultiDimensionalArray_2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Private Delegate Sub myDelegate(myString As String)
    Sub Main()
        Dim myArray1 As myInterface(,) = New myInterface(3, 1) {}
        Dim myArray2 As myDelegate(,) = New myDelegate(3, 1) {}
        Dim myArray3 = New Integer(Number.One, Number.Two) {} 
    End Sub
End Module
Interface myInterface
End Interface
Enum Number
    One
    Two
End Enum
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldc.i4.4
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "myInterface(*,*)..ctor"
  IL_0007:  pop
  IL_0008:  ldc.i4.4
  IL_0009:  ldc.i4.2
  IL_000a:  newobj     "Module1.myDelegate(*,*)..ctor"
  IL_000f:  pop
  IL_0010:  ldc.i4.1
  IL_0011:  ldc.i4.2
  IL_0012:  newobj     "Integer(*,*)..ctor"
  IL_0017:  pop
  IL_0018:  ret
}
]]>)
        End Sub

        ' Declaration multi- dimensional array
        <Fact>
        Public Sub DeclarationmultiDimensionalArray_3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class C1
    Public Shared Sub Main()
        Dim myLength As Integer = 3
        Dim arr As Integer(,) = New Integer(myLength, 1) {}
    End Sub
    Private Class A
        Private x As Integer = 1
        Private arr As Integer(,) = New Integer(x, 4) {}
    End Class
End Class
    </file>
</compilation>).VerifyIL("C1.Main", <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.3
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ldc.i4.2
  IL_0004:  newobj     "Integer(*,*)..ctor"
  IL_0009:  pop
  IL_000a:  ret
}
]]>)
        End Sub

        ' Declaration multi- dimensional array
        <Fact>
        Public Sub DeclarationmultiDimensionalArray_5()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim myArray8 As Integer(,) = New Integer(-1, 4) {}
        Dim arrDouble#(,) = New Double(1, 2) {}
        Dim arrDecimal@(,) = New Decimal(1, 2) {}
        Dim arrString$(,) = New String(1, 2) {}
        Dim arrInteger%(,) = New Integer(1, 2) {}
        Dim arrLong&amp;(,) = New Long(1, 2) {}
        Dim arrSingle!(,) = New Single(1, 2) {}
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.5
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  pop
  IL_0008:  ldc.i4.2
  IL_0009:  ldc.i4.3
  IL_000a:  newobj     "Double(*,*)..ctor"
  IL_000f:  pop
  IL_0010:  ldc.i4.2
  IL_0011:  ldc.i4.3
  IL_0012:  newobj     "Decimal(*,*)..ctor"
  IL_0017:  pop
  IL_0018:  ldc.i4.2
  IL_0019:  ldc.i4.3
  IL_001a:  newobj     "String(*,*)..ctor"
  IL_001f:  pop
  IL_0020:  ldc.i4.2
  IL_0021:  ldc.i4.3
  IL_0022:  newobj     "Integer(*,*)..ctor"
  IL_0027:  pop
  IL_0028:  ldc.i4.2
  IL_0029:  ldc.i4.3
  IL_002a:  newobj     "Long(*,*)..ctor"
  IL_002f:  pop
  IL_0030:  ldc.i4.2
  IL_0031:  ldc.i4.3
  IL_0032:  newobj     "Single(*,*)..ctor"
  IL_0037:  pop
  IL_0038:  ret
}
]]>)
        End Sub

        ' Initialize multi- dimensional array
        <Fact()>
        Public Sub InitializemultiDimensionalArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Imports Microsoft.VisualBasic.Information
Module Module1
    Sub Main()
        Dim myArray1 As Integer(,,) = {{{0, 1}, {2, 3}}, {{4, 5}, {6, 7}}}
        Dim myArray2 As Single(,) = New Single(2, 1) {{CSng(1.0), CSng(2.0)}, {CSng(3.0), CSng(4.0)}, {CSng(5.0), CSng(6.0)}}
        Dim myArray3 As Long(,) = New Long(,) {{1, 2}, {3, 4}, {5, 6}}
        Dim myArray4 As Char(,)
        myArray4 = New Char(2, 1) {{"1"c, "2"c}, {"3"c, "4"c}, {"5"c, "6"c}}
        Dim myArray5 As Decimal(,)
        myArray5 = New Decimal(,) {{CDec(1.0), CDec(2.0)}, {CDec(3.0), CDec(4.0)}, {CDec(5.0), CDec(6.0)}}
        Dim myArray6 As Integer(,) = New Integer(-1, -1) {}
        Dim myArray7 As Integer(,) = New Integer(,) {{}}
        Dim myArray8 As Integer(,,) = New Integer(,,) {{{}}}
        Dim myArray9 As String(,) = New String(2, 1) {{"a"c, "b"c}, {"c"c, "d"c}, {"e"c, "f"c}}
        Console.WriteLine(UBound(myArray1, 1))
        Console.WriteLine(UBound(myArray1, 2))
        Console.WriteLine(UBound(myArray1, 3))
        Console.WriteLine(UBound(myArray2, 1))
        Console.WriteLine(UBound(myArray2, 2))
        Console.WriteLine(UBound(myArray3, 1))
        Console.WriteLine(UBound(myArray3, 2))
        Console.WriteLine(UBound(myArray4, 1))
        Console.WriteLine(UBound(myArray4, 2))
        Console.WriteLine(UBound(myArray5, 1))
        Console.WriteLine(UBound(myArray5, 2))
        Console.WriteLine(UBound(myArray6, 1))
        Console.WriteLine(UBound(myArray6, 2))
        Console.WriteLine(UBound(myArray7, 1))
        Console.WriteLine(UBound(myArray7, 2))
        Console.WriteLine(UBound(myArray8, 1))
        Console.WriteLine(UBound(myArray8, 2))
        Console.WriteLine(UBound(myArray8, 3))
        Console.WriteLine(UBound(myArray9, 1))
        Console.WriteLine(UBound(myArray9, 2))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[1
1
1
2
1
2
1
2
1
2
1
-1
-1
0
-1
0
0
-1
2
1]]>)
        End Sub

        ' Use different kinds of var as index upper bound
        <Fact>
        Public Sub DifferentVarAsBound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On        
Imports Microsoft.VisualBasic.Information
Module Module1
    Property prop As Integer
    Sub Main(args As String())
        Dim arr1(3, prop) As Integer
        Dim arr2(3, fun()) As Integer
        Dim x = fun()
        Dim arr3(x, 1) As Integer
        Dim z() As Integer
        Dim y() As Integer
        Dim replyCounts(,) As Short = New Short(UBound(z, 1), UBound(y, 1)) {}
    End Sub
    Function fun() As Integer
        Return 3
    End Function
    Sub foo(x As Integer)
        Dim arr1(3, x) As Integer
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (Integer() V_0, //z
  Integer() V_1) //y
  IL_0000:  ldc.i4.4
  IL_0001:  call       "Function Module1.get_prop() As Integer"
  IL_0006:  ldc.i4.1
  IL_0007:  add.ovf
  IL_0008:  newobj     "Integer(*,*)..ctor"
  IL_000d:  pop
  IL_000e:  ldc.i4.4
  IL_000f:  call       "Function Module1.fun() As Integer"
  IL_0014:  ldc.i4.1
  IL_0015:  add.ovf
  IL_0016:  newobj     "Integer(*,*)..ctor"
  IL_001b:  pop
  IL_001c:  call       "Function Module1.fun() As Integer"
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  ldc.i4.2
  IL_0024:  newobj     "Integer(*,*)..ctor"
  IL_0029:  pop
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  call       "Function Microsoft.VisualBasic.Information.UBound(System.Array, Integer) As Integer"
  IL_0031:  ldc.i4.1
  IL_0032:  add.ovf
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4.1
  IL_0035:  call       "Function Microsoft.VisualBasic.Information.UBound(System.Array, Integer) As Integer"
  IL_003a:  ldc.i4.1
  IL_003b:  add.ovf
  IL_003c:  newobj     "Short(*,*)..ctor"
  IL_0041:  pop
  IL_0042:  ret
}
]]>)
        End Sub

        ' Specify lower bound and up bound for multi-dimensional array
        <Fact>
        Public Sub SpecifyLowerAndUpBound()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Module1
    Property prop As Integer
    Sub Main(args As String())
        Dim arr1(0 To 0, 0 To -1) As Integer
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  pop
  IL_0008:  ret
}
]]>)
        End Sub

        ' Array creation expression can be part of an anonymous object creation expression
        <Fact>
        Public Sub ArrayCreateAsAnonymous()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Imports Microsoft.VisualBasic.Information
Module Module1
    Sub Main(args As String())
        Dim a0 = New With {
         Key.b4 = New Integer(1, 2) {}, _
         Key.b5 = New Integer(1, 1) {{1, 2}, {2, 3}},
         Key.b6 = New Integer()() {New Integer(1) {}, New Integer(2) {}},
         Key.b7 = New Integer(2)() {},
         Key.b8 = New Integer(1)() {New Integer(0) {}, New Integer(1) {}},
         Key.b9 = New Integer() {1, 2, 3},
         Key.b10 = New Integer(,) {{1, 2}, {2, 3}}
        }
        Console.WriteLine(UBound(a0.b4, 1))
        Console.WriteLine(UBound(a0.b4, 2))
        Console.WriteLine(UBound(a0.b5, 1))
        Console.WriteLine(UBound(a0.b5, 2))
        Console.WriteLine(UBound(a0.b6, 1))
        Console.WriteLine(UBound(a0.b7, 1))
        Console.WriteLine(UBound(a0.b8, 1))
        Console.WriteLine(UBound(a0.b9, 1))
        Console.WriteLine(UBound(a0.b10, 1))
        Console.WriteLine(UBound(a0.b10, 2))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[1
2
1
1
1
2
1
2
1
1]]>)
        End Sub

        ' Accessing an array's 0th element should work fine
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessZero()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 4) {}
        arr(0, 0) = 5
        Console.WriteLine(arr(0, 0))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[5]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  5
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.5
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.5
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.0
  IL_0012:  call       "Integer(*,*).Get"
  IL_0017:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001c:  ret
}
]]>)
        End Sub

        ' Accessing an array's maxlength element should work fine
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessMaxLength()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 3) {}
        arr(4, 3) = 5
        Console.WriteLine(arr(4, 3))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[5]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  5
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.4
  IL_0009:  ldc.i4.3
  IL_000a:  ldc.i4.5
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  ldc.i4.4
  IL_0011:  ldc.i4.3
  IL_0012:  call       "Integer(*,*).Get"
  IL_0017:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001c:  ret
}
]]>)
        End Sub

        ' Accessing an array's -1 element should throw an exception
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessLessThanMin()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 4) {}
        Try
            arr(-1, 1) = 5
            arr(1, -1) = 5
            arr(-1, -1) = 5
        Catch generatedExceptionName As System.IndexOutOfRangeException
        End Try
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  4
  .locals init (Integer(,) V_0, //arr
  System.IndexOutOfRangeException V_1) //generatedExceptionName
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.5
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  stloc.0
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.m1
  IL_000a:  ldc.i4.1
  IL_000b:  ldc.i4.5
  IL_000c:  call       "Integer(*,*).Set"
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.m1
  IL_0014:  ldc.i4.5
  IL_0015:  call       "Integer(*,*).Set"
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.m1
  IL_001c:  ldc.i4.m1
  IL_001d:  ldc.i4.5
  IL_001e:  call       "Integer(*,*).Set"
  IL_0023:  leave.s    IL_0033
}
  catch System.IndexOutOfRangeException
{
  IL_0025:  dup
  IL_0026:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_002b:  stloc.1
  IL_002c:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0031:  leave.s    IL_0033
}
  IL_0033:  ret
}
]]>)
        End Sub

        ' Accessing an array's maxlength+1 element should throw an exception
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessGreaterThanMax()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 3) {}
        Try
            arr(5, 3) = 5
            arr(4, 4) = 5
            arr(5, 4) = 5
        Catch
        End Try
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (Integer(,) V_0) //arr
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.4
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  stloc.0
  .try
{
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.5
  IL_000a:  ldc.i4.3
  IL_000b:  ldc.i4.5
  IL_000c:  call       "Integer(*,*).Set"
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.4
  IL_0013:  ldc.i4.4
  IL_0014:  ldc.i4.5
  IL_0015:  call       "Integer(*,*).Set"
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.5
  IL_001c:  ldc.i4.4
  IL_001d:  ldc.i4.5
  IL_001e:  call       "Integer(*,*).Set"
  IL_0023:  leave.s    IL_0031
}
  catch System.Exception
{
  IL_0025:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_002a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_002f:  leave.s    IL_0031
}
  IL_0031:  ret
}
]]>)
        End Sub

        ' Accessing an array's index with a variable of type int, short, byte should work
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessWithDifferentType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(4, 4) {}
        Dim idx As Integer = 2
        Dim idx1 As Byte = 1
        Dim idx3 As Short = 4
        Dim idx4 = 2.0
        Dim idx5 As Long = 3L
        arr(idx, 3) = 100
        arr(idx1, 3) = 100
        arr(idx3, 3) = 100
        arr(cint(idx4), 3) = 100
        arr(cint(idx5), 3) = 100
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  5
  .locals init (Integer V_0, //idx
  Byte V_1, //idx1
  Short V_2, //idx3
  Double V_3, //idx4
  Long V_4) //idx5
  IL_0000:  ldc.i4.5
  IL_0001:  ldc.i4.5
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  stloc.1
  IL_000b:  ldc.i4.4
  IL_000c:  stloc.2
  IL_000d:  ldc.r8     2
  IL_0016:  stloc.3
  IL_0017:  ldc.i4.3
  IL_0018:  conv.i8
  IL_0019:  stloc.s    V_4
  IL_001b:  dup
  IL_001c:  ldloc.0
  IL_001d:  ldc.i4.3
  IL_001e:  ldc.i4.s   100
  IL_0020:  call       "Integer(*,*).Set"
  IL_0025:  dup
  IL_0026:  ldloc.1
  IL_0027:  ldc.i4.3
  IL_0028:  ldc.i4.s   100
  IL_002a:  call       "Integer(*,*).Set"
  IL_002f:  dup
  IL_0030:  ldloc.2
  IL_0031:  ldc.i4.3
  IL_0032:  ldc.i4.s   100
  IL_0034:  call       "Integer(*,*).Set"
  IL_0039:  dup
  IL_003a:  ldloc.3
  IL_003b:  call       "Function System.Math.Round(Double) As Double"
  IL_0040:  conv.ovf.i4
  IL_0041:  ldc.i4.3
  IL_0042:  ldc.i4.s   100
  IL_0044:  call       "Integer(*,*).Set"
  IL_0049:  ldloc.s    V_4
  IL_004b:  conv.ovf.i4
  IL_004c:  ldc.i4.3
  IL_004d:  ldc.i4.s   100
  IL_004f:  call       "Integer(*,*).Set"
  IL_0054:  ret
}
]]>)
        End Sub

        ' Passing an element to a function as a byVal or byRef parameter should work
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub ArrayAsArgument()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(,) {{1, 2}}
        ElementTaker((arr(0, 1)))
        Console.WriteLine(arr(0, 1))
        ElementTaker(arr(0, 1))
        Console.WriteLine(arr(0, 1))
    End Sub
    Public Function ElementTaker(ByRef val As Integer) As Integer
        val = val + 5
        Return val
    End Function
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[2
7]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       82 (0x52)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  dup
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.2
  IL_0014:  call       "Integer(*,*).Set"
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldc.i4.1
  IL_001c:  call       "Integer(*,*).Get"
  IL_0021:  stloc.0
  IL_0022:  ldloca.s   V_0
  IL_0024:  call       "Function Module1.ElementTaker(ByRef Integer) As Integer"
  IL_0029:  pop
  IL_002a:  dup
  IL_002b:  ldc.i4.0
  IL_002c:  ldc.i4.1
  IL_002d:  call       "Integer(*,*).Get"
  IL_0032:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0037:  dup
  IL_0038:  ldc.i4.0
  IL_0039:  ldc.i4.1
  IL_003a:  call       "Integer(*,*).Address"
  IL_003f:  call       "Function Module1.ElementTaker(ByRef Integer) As Integer"
  IL_0044:  pop
  IL_0045:  ldc.i4.0
  IL_0046:  ldc.i4.1
  IL_0047:  call       "Integer(*,*).Get"
  IL_004c:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0051:  ret
}
]]>)
        End Sub

        ' Passing an element to a function as a byVal or byRef parameter should work
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub ArrayAsArgument_1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main(args As String())
        Dim arr As Integer(,) = New Integer(,) {{1, 2}}
        ElementTaker(arr(0, 1))
        Console.WriteLine(arr(0, 1))
    End Sub
    Public Function ElementTaker(ByVal val As Integer) As Integer
        val = 5
        Return val
    End Function
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[2]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  5
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  dup
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.2
  IL_0014:  call       "Integer(*,*).Set"
  IL_0019:  dup
  IL_001a:  ldc.i4.0
  IL_001b:  ldc.i4.1
  IL_001c:  call       "Integer(*,*).Get"
  IL_0021:  call       "Function Module1.ElementTaker(Integer) As Integer"
  IL_0026:  pop
  IL_0027:  ldc.i4.0
  IL_0028:  ldc.i4.1
  IL_0029:  call       "Integer(*,*).Get"
  IL_002e:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0033:  ret
}
]]>)
        End Sub

        ' Assigning nothing to an array variable
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AssignNothingToArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Public Sub Main()
        Dim arr As Integer(,) = New Integer(0, 1) {{1, 2}}
        Dim arr1 As Integer(,) = Nothing
        arr = Nothing
        arr(0, 1) = 3
        arr1(0, 1) = 3
    End Sub
End Module
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       45 (0x2d)
  .maxstack  5
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  dup
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.1
  IL_0013:  ldc.i4.2
  IL_0014:  call       "Integer(*,*).Set"
  IL_0019:  pop
  IL_001a:  ldnull
  IL_001b:  ldnull
  IL_001c:  ldc.i4.0
  IL_001d:  ldc.i4.1
  IL_001e:  ldc.i4.3
  IL_001f:  call       "Integer(*,*).Set"
  IL_0024:  ldc.i4.0
  IL_0025:  ldc.i4.1
  IL_0026:  ldc.i4.3
  IL_0027:  call       "Integer(*,*).Set"
  IL_002c:  ret
}
]]>)
        End Sub

        ' Assigning a smaller array to a bigger array or vice versa should work
        <Fact>
        Public Sub AssignArrayToArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic.Information
Module Program
    Public Sub Main()
        Dim arr1 As Integer(,) = New Integer(4, 1) {{1, 2}, {3, 4}, {5, 6}, {8, 9}, {100, 1210}}
        Dim arr2 As Integer(,) = New Integer(2, 1) {{6, 7}, {8, 1}, {2, 12}}
        arr1 = arr2
        Dim arr3 As Integer(,) = New Integer(1, 1) {{6, 7}, {9, 8}}
        Dim arr4 As Integer(,) = New Integer(2, 2) {{1, 2, 3}, {4, 5, 6}, {8, 0, 2}}
        arr3 = arr4
        Console.WriteLine(UBound(arr1, 1))
        Console.WriteLine(UBound(arr1, 2))
        Console.WriteLine(UBound(arr2, 1))
        Console.WriteLine(UBound(arr2, 2))
        Console.WriteLine(UBound(arr3, 1))
        Console.WriteLine(UBound(arr3, 2))
        Console.WriteLine(UBound(arr4, 1))
        Console.WriteLine(UBound(arr4, 2))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[2
1
2
1
2
2
2
2]]>)
        End Sub

        ' Access index by enum
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AccessIndexByEnum()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Public Sub Main()
        Dim cube As Integer(,) = New Integer(1, 2) {}
        cube(Number.One, Number.Two) = 1
        Console.WriteLine(cube(Number.One, Number.Two))
    End Sub
End Module
Enum Number
    One
    Two
End Enum
    </file>
</compilation>, expectedOutput:=<![CDATA[1]]>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  5
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     "Integer(*,*)..ctor"
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Integer(*,*).Set"
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.1
  IL_0012:  call       "Integer(*,*).Get"
  IL_0017:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001c:  ret
}
]]>)
        End Sub

        ' Assigning a struct variable to an element should work
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub AssigningStructToElement()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Public Sub Main()
        Dim arr As myStruct(,) = New myStruct(2, 1) {}
        Dim ms As myStruct
        ms.x = 4
        ms.y = 5
        arr(2, 0) = ms
    End Sub
End Module
Structure myStruct
    Public x As Integer
    Public y As Integer
End Structure
    </file>
</compilation>).VerifyIL("Module1.Main", <![CDATA[
{
  // Code size       32 (0x20)
  .maxstack  4
  .locals init (myStruct V_0) //ms
  IL_0000:  ldc.i4.3
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     "myStruct(*,*)..ctor"
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.4
  IL_000a:  stfld      "myStruct.x As Integer"
  IL_000f:  ldloca.s   V_0
  IL_0011:  ldc.i4.5
  IL_0012:  stfld      "myStruct.y As Integer"
  IL_0017:  ldc.i4.2
  IL_0018:  ldc.i4.0
  IL_0019:  ldloc.0
  IL_001a:  call       "myStruct(*,*).Set"
  IL_001f:  ret
}
]]>)
        End Sub

        ' Using foreach on a multi-dimensional array
        <WorkItem(528752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528752")>
        <Fact>
        Public Sub ForEachMultiDimensionalArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Public Sub Main()
        Dim arr As Integer(,,) = New Integer(,,) {{{1, 2}, {4, 5}}}
        For Each i As Integer In arr
            Console.WriteLine(i)
        Next
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[1
2
4
5]]>)
        End Sub

        ' Overload method by different dimension of array
        <Fact>
        Public Sub OverloadByDiffDimensionArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module Program
    Sub Main(args As String())
    End Sub
    Sub foo(ByRef arg(,,) As Integer)
    End Sub
    Sub foo(ByRef arg(,) As Integer)
    End Sub
    Sub foo(ByRef arg() As Integer)
    End Sub
End Module
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        ' Multi-dimensional array args  could not as entry point 
        <Fact()>
        Public Sub MultidimensionalArgsAsEntryPoint()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Class B
    Public Shared Sub Main(args As String(,))
    End Sub
    Public Shared Sub Main()
    End Sub
End Class
Class M1
    Public Shared Sub Main(args As String(,))
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()
        End Sub

        ' Declare multi-dimensional and Jagged array
        <Fact>
        Public Sub MixedArray()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports Microsoft.VisualBasic.Information
Class program
    Public Shared Sub Main()
        Dim x = New Integer(,)() {}
        Dim y(,)() As Byte = New Byte(,)() {{New Byte() {2, 1, 3}}, {New Byte() {3, 0}}}
        System.Console.WriteLine(UBound(y, 1))
        System.Console.WriteLine(UBound(y, 2))
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[1
0]]>)
        End Sub

        ' Parse an Attribute instance that takes a generic type with an generic argument that is a multi-dimensional array
        <Fact>
        Public Sub Generic()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
&lt;TypeAttribute(GetType(Program(Of [String])(,)))&gt; _
Public Class Foo
End Class
Class Program(Of T)
End Class
Class TypeAttribute
    Inherits Attribute
    Public Sub New(value As Type)
    End Sub
End Class
    </file>
</compilation>).VerifyDiagnostics()
        End Sub


        <Fact()>
        Public Sub MDArrayTypeRef()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[
public class A
{
    public static readonly string[,] dummy = new string[2, 2] { { "", "M" }, { "S", "SM" } };
}]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()

            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[
Module Module1
    Sub Main()
        Dim x = A.dummy
        System.Console.Writeline(x(1,1))
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[
SM
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub


    End Class

End Namespace
