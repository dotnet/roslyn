' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class MethodCalls
        Inherits BasicTestBase

        <Fact>
        Public Sub NamedArguments()
            Dim compilationDef =
<compilation name="VBNamedArguments1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        TestOrder2(b:=TestOrder1(1), a:=TestOrder1(2))
        TestOrder2(a:=TestOrder1(3), b:=TestOrder1(4))
        TestOrder2(b:=TestOrder3("5"), a:=TestOrder1(6))
        TestOrder2(b:=TestOrder1(7), a:=TestOrder3("8"))
        TestOrder2(b:=TestOrder3("9"), a:=TestOrder3("10"))
        TestOrder2(a:=TestOrder3("11"), b:=TestOrder3("12"))
    End Sub


    Sub TestOrder2(a As Integer, b As Integer)
        System.Console.WriteLine("TestOrder2: {0}, {1}", a, b)
    End Sub

    Function TestOrder1(a As Integer) As Integer
        System.Console.WriteLine("TestOrder1: {0}", a)
        Return a
    End Function

    Function TestOrder3(a As String) As String
        System.Console.WriteLine("TestOrder3: {0}", a)
        Return a
    End Function
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
TestOrder1: 2
TestOrder1: 1
TestOrder2: 2, 1
TestOrder1: 3
TestOrder1: 4
TestOrder2: 3, 4
TestOrder1: 6
TestOrder3: 5
TestOrder2: 6, 5
TestOrder3: 8
TestOrder1: 7
TestOrder2: 8, 7
TestOrder3: 10
TestOrder3: 9
TestOrder2: 10, 9
TestOrder3: 11
TestOrder3: 12
TestOrder2: 11, 12
]]>)
        End Sub

        <Fact>
        Public Sub TrueByRefArguments1()
            Dim compilationDef =
<compilation name="VBTrueByRefArguments1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim x As Integer = 1
        Dim z As Integer() = New Integer() {0, 3}

        System.Console.WriteLine("---")
        TestByRef(x)
        System.Console.WriteLine("Test1: {0}", x)

        System.Console.WriteLine("---")
        TestByRef(z(Return1()))
        System.Console.WriteLine("Test3: {0}", z(1))

        System.Console.WriteLine("---")
        TestByRef(ReturnArray(z)(Return1()))
        System.Console.WriteLine("Test4: {0}", z(1))
    End Sub


    Sub TestByRef(ByRef a As Integer)
        a = a + 1
    End Sub

    Function ReturnArray(z As Integer()) As Integer()
        System.Console.WriteLine("ReturnArray")
        Return z
    End Function

    Function Return1() As Integer
        System.Console.WriteLine("Return1")
        Return 1
    End Function

End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
---
Test1: 2
---
Return1
Test3: 4
---
ReturnArray
Return1
Test4: 5
]]>)
        End Sub

        <Fact>
        Public Sub ParamArray1()
            Dim compilationDef =
<compilation name="VBParamArray1">
    <file name="a.vb">
Imports System

Module Module1

    Sub Main()
        ParamArray1()
        ParamArray1("a"c)
        ParamArray1("a"c, "b"c)
        ParamArray2(1)
        ParamArray2(2, "a"c)
        ParamArray2(3, "a"c, "b"c)
        ParamArray2(a:=4)
    End Sub

    Sub ParamArray1(ParamArray a As Char())
        System.Console.WriteLine("ParamArray1: [{0}]", CStr(a))
    End Sub

    Sub ParamArray2(a As Integer, ParamArray b As Char())
        System.Console.WriteLine("ParamArray1: {0}, [{1}]", a, CStr(b))
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
ParamArray1: []
ParamArray1: [a]
ParamArray1: [ab]
ParamArray1: 1, []
ParamArray1: 2, [a]
ParamArray1: 3, [ab]
ParamArray1: 4, []
]]>)
        End Sub

        <Fact>
        Public Sub ByRefArguments2()
            Dim compilationDef =
<compilation name="VBByRefArguments2">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Dim staticFld As Byte
    ReadOnly staticReadonlyFld As Integer

    Sub Main()

        Dim z As Byte() = New Byte() {0, 81}

        System.Console.WriteLine("---")
        Test1()

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test2())
        System.Console.WriteLine("---")
        Test2_1()

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3())
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3_1())
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3_2())

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test4(51))
        Dim By As Byte = 53
        System.Console.WriteLine("---")
        Test4_1(By)
        System.Console.WriteLine(By)

        System.Console.WriteLine("---")
        staticFld = 61
        Test5()
        System.Console.WriteLine(staticFld)

        Dim tc As TestClass = New TestClass()
        System.Console.WriteLine("---")
        tc.instanceFld = 63
        Test5_1(tc)
        System.Console.WriteLine(tc.instanceFld)

        System.Console.WriteLine("---")
        Test5_2()
        System.Console.WriteLine("---")
        Test5_3(tc)

        Dim ts As TestStruct = New TestStruct()
        ts.instanceFld = 65
        System.Console.WriteLine("---")
        Test5_4(ts)
        System.Console.WriteLine(ts.instanceFld)
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test5_5(ts))
        System.Console.WriteLine("---")
        Test5_6(ts)
        System.Console.WriteLine("---")
        Test5_7()
        System.Console.WriteLine("---")
        Test5_8()
        System.Console.WriteLine("---")
        Test5_9(tc)
        System.Console.WriteLine(tc.instanceFld)
        System.Console.WriteLine("---")
        Test5_10()


        System.Console.WriteLine("---")
        Test6(z)
        System.Console.WriteLine(z(1))
        System.Console.WriteLine("---")
        Test6_1(z)
        System.Console.WriteLine(z(1))

        ts.instanceFld = 91
        Dim z1 As TestStruct() = New TestStruct() {Nothing, ts}

        System.Console.WriteLine("---")
        Test6_2(z1)
        System.Console.WriteLine(z1(1).instanceFld)
        System.Console.WriteLine("---")
        Test6_3(z1)
        System.Console.WriteLine(z1(1).instanceFld)

    End Sub


    Sub Test6(z As Byte())
        TestByRef(z(1))
    End Sub

    Sub Test6_1(z As Byte())
        TestByRef(ReturnArray(z)(1))
    End Sub

    Sub Test6_2(z As TestStruct())
        TestByRef(z(Return1()).instanceFld)
    End Sub

    Sub Test6_3(z As TestStruct())
        TestByRef(z(1).instanceFld)
    End Sub

    Sub Test1()
        TestByRef(20)
    End Sub

    Function Test2() As Long
        Dim x As Byte = 30
        Return TestByRef2(CInt(x))
    End Function

    Sub Test2_1()
        Dim x As Byte = 32
        TestByRef2(CInt(x))
    End Sub

    Function Test3() As Byte
        Dim x As Byte = 40
        TestByRef(x)
        Return x
    End Function

    Function Test3_1() As Long
        Dim x As Byte = 42
        Return TestByRef2(x) + x
    End Function

    Function Test3_2() As Byte
        Dim x As Byte = 44
        TestByRef2(x)
        Return x
    End Function

    Function Test4(x As Byte) As Byte
        TestByRef(x)
        Return x
    End Function

    Sub Test4_1(ByRef x As Byte)
        TestByRef(x)
    End Sub

    Sub Test5()
        TestByRef(staticFld)
    End Sub

    Sub Test5_1(x As TestClass)
        TestByRef(x.instanceFld)
    End Sub

    Sub Test5_2()
        TestByRef(staticReadonlyFld)
    End Sub

    Sub Test5_3(x As TestClass)
        TestByRef(x.instanceReadonlyFld)
    End Sub

    Sub Test5_4(ByRef x As TestStruct)
        TestByRef(x.instanceFld)
    End Sub

    Function Test5_5(x As TestStruct) As Byte
        TestByRef(x.instanceFld)
        Return x.instanceFld
    End Function

    Sub Test5_6(x As TestStruct)
        TestByRef(x.instanceReadonlyFld)
    End Sub

    Sub Test5_7()
        TestByRef(ReturnInteger())
    End Sub

    Sub Test5_8()
        TestByRef(ReturnTestStruct().instanceFld2)
    End Sub

    Sub Test5_9(x As TestClass)
        TestByRef(ReturnTestClass(x).instanceFld)
    End Sub

    Sub Test5_10()
        TestByRef(1)
    End Sub

    Function ReturnInteger() As Integer
        Return 71
    End Function

    Function ReturnTestStruct() As TestStruct
        Dim x As TestStruct = New TestStruct()
        x.instanceFld2 = 73
        Return x
    End Function

    Function ReturnTestClass(x As TestClass) As TestClass
        Return x
    End Function

    Sub TestByRef(ByRef a As Integer)
        System.Console.WriteLine("TestByRef: {0}", a)
        a = a + 1
    End Sub

    Function TestByRef2(ByRef a As Integer) As Long
        System.Console.WriteLine("TestByRef2: {0}", a)
        a = a + 1
        Return a
    End Function

    Function ReturnArray(z As Byte()) As Byte()
        System.Console.WriteLine("ReturnArray")
        Return z
    End Function

    Function Return1() As Integer
        System.Console.WriteLine("Return1")
        Return 1
    End Function

End Module


Class TestClass
    Public instanceFld As Byte
    Public ReadOnly instanceReadonlyFld As Integer

End Class


Structure TestStruct
    Public instanceFld As Byte
    Public ReadOnly instanceReadonlyFld As Integer
    Public instanceFld2 As Integer

    Sub Test1()
        TestByRef(Me.instanceFld)
    End Sub

    Sub Test2()
        TestByRef(instanceFld)
    End Sub

    Sub Test3()
        TestByRef(Me.instanceReadonlyFld)
    End Sub

End Structure
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
---
TestByRef: 20
---
TestByRef2: 30
31
---
TestByRef2: 32
---
TestByRef: 40
41
---
TestByRef2: 42
86
---
TestByRef2: 44
45
---
TestByRef: 51
52
---
TestByRef: 53
54
---
TestByRef: 61
62
---
TestByRef: 63
64
---
TestByRef: 0
---
TestByRef: 0
---
TestByRef: 65
66
---
TestByRef: 66
67
---
TestByRef: 0
---
TestByRef: 71
---
TestByRef: 73
---
TestByRef: 64
65
---
TestByRef: 1
---
TestByRef: 81
82
---
ReturnArray
TestByRef: 82
83
---
Return1
TestByRef: 91
92
---
TestByRef: 92
93
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   20
  IL_0002:  stloc.0   
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000a:  ret       
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   30
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function Module1.TestByRef2(ByRef Integer) As Long"
  IL_000a:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2_1",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   32
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function Module1.TestByRef2(ByRef Integer) As Long"
  IL_000a:  pop
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   40
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000a:  ldloc.0
  IL_000b:  conv.ovf.u1
  IL_000c:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_1",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (Byte V_0, //x
  Integer V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_1
  IL_0007:  call       "Function Module1.TestByRef2(ByRef Integer) As Long"
  IL_000c:  ldloc.1
  IL_000d:  conv.ovf.u1
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  conv.u8
  IL_0011:  add.ovf
  IL_0012:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test3_2",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.s   44
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Function Module1.TestByRef2(ByRef Integer) As Long"
  IL_000a:  pop
  IL_000b:  ldloc.0
  IL_000c:  conv.ovf.u1
  IL_000d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0009:  ldloc.0
  IL_000a:  conv.ovf.u1
  IL_000b:  starg.s    V_0
  IL_000d:  ldarg.0
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test4_1",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldind.u1  
  IL_0002:  stloc.0   
  IL_0003:  ldloca.s   V_0
  IL_0005:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000a:  ldarg.0   
  IL_000b:  ldloc.0   
  IL_000c:  conv.ovf.u1
  IL_000d:  stind.i1  
  IL_000e:  ret       
}
]]>)

            verifier.VerifyIL("Module1.Test5",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldsfld     "Module1.staticFld As Byte"
  IL_0005:  stloc.0   
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ldloc.0   
  IL_000e:  conv.ovf.u1
  IL_000f:  stsfld     "Module1.staticFld As Byte"
  IL_0014:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test5_1",
<![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (TestClass V_0,
           Integer V_1)
  IL_0000:  ldarg.0   
  IL_0001:  dup       
  IL_0002:  stloc.0   
  IL_0003:  ldfld      "TestClass.instanceFld As Byte"
  IL_0008:  stloc.1   
  IL_0009:  ldloca.s   V_1
  IL_000b:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0010:  ldloc.0   
  IL_0011:  ldloc.1   
  IL_0012:  conv.ovf.u1
  IL_0013:  stfld      "TestClass.instanceFld As Byte"
  IL_0018:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test5_1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TestClass.instanceFld As Byte"
  IL_0006:  dup
  IL_0007:  ldind.u1
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0010:  ldloc.0
  IL_0011:  conv.ovf.u1
  IL_0012:  stind.i1
  IL_0013:  ret
}
]]>)
#End If

            verifier.VerifyIL("Module1.Test5_2",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldsfld     "Module1.staticReadonlyFld As Integer"
  IL_0005:  stloc.0   
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ret       
}
]]>)

            verifier.VerifyIL("Module1.Test5_3",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "TestClass.instanceReadonlyFld As Integer"
  IL_0006:  stloc.0   
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test5_4",
<![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "TestStruct.instanceFld As Byte"
  IL_0006:  stloc.0   
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ldarg.0   
  IL_000f:  ldloc.0   
  IL_0010:  conv.ovf.u1
  IL_0011:  stfld      "TestStruct.instanceFld As Byte"
  IL_0016:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test5_4",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TestStruct.instanceFld As Byte"
  IL_0006:  dup
  IL_0007:  ldind.u1
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0010:  ldloc.0
  IL_0011:  conv.ovf.u1
  IL_0012:  stind.i1
  IL_0013:  ret
}
]]>)
#End If

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test5_5",
<![CDATA[
{
// Code size       34 (0x22)
.maxstack  2
.locals init (Byte V_0, //Test5_5
Integer V_1)
IL_0000:  ldarga.s   V_0
IL_0002:  ldfld      "TestStruct.instanceFld As Byte"
IL_0007:  stloc.1
IL_0008:  ldloca.s   V_1
IL_000a:  call       "Sub Module1.TestByRef(ByRef Integer)"
IL_000f:  ldarga.s   V_0
IL_0011:  ldloc.1
IL_0012:  conv.ovf.u1
IL_0013:  stfld      "TestStruct.instanceFld As Byte"
IL_0018:  ldarga.s   V_0
IL_001a:  ldfld      "TestStruct.instanceFld As Byte"
IL_001f:  stloc.0
IL_0020:  ldloc.0
IL_0021:  ret
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test5_5",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldflda     "TestStruct.instanceFld As Byte"
  IL_0007:  dup
  IL_0008:  ldind.u1
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0011:  ldloc.0
  IL_0012:  conv.ovf.u1
  IL_0013:  stind.i1
  IL_0014:  ldarg.0
  IL_0015:  ldfld      "TestStruct.instanceFld As Byte"
  IL_001a:  ret
}
]]>)
#End If

            verifier.VerifyIL("Module1.Test5_6",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "TestStruct.instanceReadonlyFld As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test5_7",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  call       "Function Module1.ReturnInteger() As Integer"
  IL_0005:  stloc.0   
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ret       
}
]]>)

            verifier.VerifyIL("Module1.Test5_8",
            <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  call       "Function Module1.ReturnTestStruct() As TestStruct"
  IL_0005:  ldfld      "TestStruct.instanceFld2 As Integer"
  IL_000a:  stloc.0   
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0012:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test5_9",
<![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (TestClass V_0,
           Integer V_1)
  IL_0000:  ldarg.0   
  IL_0001:  call       "Function Module1.ReturnTestClass(TestClass) As TestClass"
  IL_0006:  dup       
  IL_0007:  stloc.0   
  IL_0008:  ldfld      "TestClass.instanceFld As Byte"
  IL_000d:  stloc.1   
  IL_000e:  ldloca.s   V_1
  IL_0010:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0015:  ldloc.0   
  IL_0016:  ldloc.1   
  IL_0017:  conv.ovf.u1
  IL_0018:  stfld      "TestClass.instanceFld As Byte"
  IL_001d:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test5_9",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.ReturnTestClass(TestClass) As TestClass"
  IL_0006:  ldflda     "TestClass.instanceFld As Byte"
  IL_000b:  dup
  IL_000c:  ldind.u1
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0015:  ldloc.0
  IL_0016:  conv.ovf.u1
  IL_0017:  stind.i1
  IL_0018:  ret
}
]]>)
#End If

            verifier.VerifyIL("Module1.Test5_10",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.1  
  IL_0001:  stloc.0   
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0009:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("TestStruct.Test1",
<![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "TestStruct.instanceFld As Byte"
  IL_0006:  stloc.0   
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ldarg.0   
  IL_000f:  ldloc.0   
  IL_0010:  conv.ovf.u1
  IL_0011:  stfld      "TestStruct.instanceFld As Byte"
  IL_0016:  ret       
}
]]>)
#Else
            verifier.VerifyIL("TestStruct.Test1",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TestStruct.instanceFld As Byte"
  IL_0006:  dup
  IL_0007:  ldind.u1
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0010:  ldloc.0
  IL_0011:  conv.ovf.u1
  IL_0012:  stind.i1
  IL_0013:  ret
}
]]>)
#End If

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("TestStruct.Test2",
<![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "TestStruct.instanceFld As Byte"
  IL_0006:  stloc.0   
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ldarg.0   
  IL_000f:  ldloc.0   
  IL_0010:  conv.ovf.u1
  IL_0011:  stfld      "TestStruct.instanceFld As Byte"
  IL_0016:  ret       
}
]]>)
#Else
            verifier.VerifyIL("TestStruct.Test2",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "TestStruct.instanceFld As Byte"
  IL_0006:  dup
  IL_0007:  ldind.u1
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0010:  ldloc.0
  IL_0011:  conv.ovf.u1
  IL_0012:  stind.i1
  IL_0013:  ret
}
]]>)
#End If

            verifier.VerifyIL("TestStruct.Test3",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "TestStruct.instanceReadonlyFld As Integer"
  IL_0006:  stloc.0   
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000e:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test6",
<![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (Byte() V_0,
  Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  dup       
  IL_0002:  stloc.0   
  IL_0003:  ldc.i4.1  
  IL_0004:  ldelem.u1 
  IL_0005:  stloc.1   
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ldloc.0   
  IL_000e:  ldc.i4.1  
  IL_000f:  ldloc.1   
  IL_0010:  conv.ovf.u1
  IL_0011:  stelem.i1 
  IL_0012:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test6",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldelema    "Byte"
  IL_0007:  dup
  IL_0008:  ldind.u1
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0011:  ldloc.0
  IL_0012:  conv.ovf.u1
  IL_0013:  stind.i1
  IL_0014:  ret
}
]]>)
#End If

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test6_1",
<![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (Byte() V_0,
           Integer V_1)
  IL_0000:  ldarg.0   
  IL_0001:  call       "Function Module1.ReturnArray(Byte()) As Byte()"
  IL_0006:  dup       
  IL_0007:  stloc.0   
  IL_0008:  ldc.i4.1  
  IL_0009:  ldelem.u1 
  IL_000a:  stloc.1   
  IL_000b:  ldloca.s   V_1
  IL_000d:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0012:  ldloc.0   
  IL_0013:  ldc.i4.1  
  IL_0014:  ldloc.1   
  IL_0015:  conv.ovf.u1
  IL_0016:  stelem.i1 
  IL_0017:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test6_1",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.ReturnArray(Byte()) As Byte()"
  IL_0006:  ldc.i4.1
  IL_0007:  ldelema    "Byte"
  IL_000c:  dup
  IL_000d:  ldind.u1
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0016:  ldloc.0
  IL_0017:  conv.ovf.u1
  IL_0018:  stind.i1
  IL_0019:  ret
}
]]>)
#End If

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test6_2",
<![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (TestStruct() V_0,
           Integer V_1,
           Integer V_2)
  IL_0000:  ldarg.0   
  IL_0001:  dup       
  IL_0002:  stloc.0   
  IL_0003:  call       "Function Module1.Return1() As Integer"
  IL_0008:  dup       
  IL_0009:  stloc.1   
  IL_000a:  ldelema    "TestStruct"
  IL_000f:  ldfld      "TestStruct.instanceFld As Byte"
  IL_0014:  stloc.2   
  IL_0015:  ldloca.s   V_2
  IL_0017:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_001c:  ldloc.0   
  IL_001d:  ldloc.1   
  IL_001e:  ldelema    "TestStruct"
  IL_0023:  ldloc.2   
  IL_0024:  conv.ovf.u1
  IL_0025:  stfld      "TestStruct.instanceFld As Byte"
  IL_002a:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test6_2",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.Return1() As Integer"
  IL_0006:  ldelema    "TestStruct"
  IL_000b:  ldflda     "TestStruct.instanceFld As Byte"
  IL_0010:  dup
  IL_0011:  ldind.u1
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_001a:  ldloc.0
  IL_001b:  conv.ovf.u1
  IL_001c:  stind.i1
  IL_001d:  ret
}
]]>)
#End If

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test6_3",
<![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (TestStruct() V_0,
           Integer V_1)
  IL_0000:  ldarg.0   
  IL_0001:  dup       
  IL_0002:  stloc.0   
  IL_0003:  ldc.i4.1  
  IL_0004:  ldelema    "TestStruct"
  IL_0009:  ldfld      "TestStruct.instanceFld As Byte"
  IL_000e:  stloc.1   
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0016:  ldloc.0   
  IL_0017:  ldc.i4.1  
  IL_0018:  ldelema    "TestStruct"
  IL_001d:  ldloc.1   
  IL_001e:  conv.ovf.u1
  IL_001f:  stfld      "TestStruct.instanceFld As Byte"
  IL_0024:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test6_3",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldelema    "TestStruct"
  IL_0007:  ldflda     "TestStruct.instanceFld As Byte"
  IL_000c:  dup
  IL_000d:  ldind.u1
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0016:  ldloc.0
  IL_0017:  conv.ovf.u1
  IL_0018:  stind.i1
  IL_0019:  ret
}
]]>)
#End If
        End Sub

        ' Same as ByRefArguments2 but with properties not fields.
        <Fact>
        Public Sub ByRefArguments2A()
            Dim compilationDef =
<compilation name="VBByRefArguments2A">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Property staticFld As Byte
    ReadOnly _staticReadonlyFld As Integer
    ReadOnly Property staticReadonlyFld As Integer
        Get
            Return _staticReadonlyFld
        End Get
    End Property

    Sub Main()

        Dim z As Byte() = New Byte() {0, 81}

        System.Console.WriteLine("---")
        Test1()

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test2())
        System.Console.WriteLine("---")
        Test2_1()

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3())
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3_1())
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test3_2())

        System.Console.WriteLine("---")
        System.Console.WriteLine(Test4(51))

        System.Console.WriteLine("---")
        staticFld = 61
        Test5()
        System.Console.WriteLine(staticFld)

        Dim tc As TestClass = New TestClass()
        System.Console.WriteLine("---")
        tc.instanceFld = 63
        Test5_1(tc)
        System.Console.WriteLine(tc.instanceFld)

        System.Console.WriteLine("---")
        Test5_2()
        System.Console.WriteLine("---")
        Test5_3(tc)

        Dim ts As TestStruct = New TestStruct()
        ts.instanceFld = 65
        System.Console.WriteLine("---")
        Test5_4(ts)
        System.Console.WriteLine(ts.instanceFld)
        System.Console.WriteLine("---")
        System.Console.WriteLine(Test5_5(ts))
        System.Console.WriteLine("---")
        Test5_6(ts)
        System.Console.WriteLine("---")
        Test5_7()
        System.Console.WriteLine("---")
        Test5_8()
        System.Console.WriteLine("---")
        Test5_9(tc)
        System.Console.WriteLine(tc.instanceFld)
        System.Console.WriteLine("---")
        Test5_10()


        System.Console.WriteLine("---")
        Test6(z)
        System.Console.WriteLine(z(1))
        System.Console.WriteLine("---")
        Test6_1(z)
        System.Console.WriteLine(z(1))

        ts.instanceFld = 91
        Dim z1 As TestStruct() = New TestStruct() {Nothing, ts}

        System.Console.WriteLine("---")
        Test6_2(z1)
        System.Console.WriteLine(z1(1).instanceFld)
        System.Console.WriteLine("---")
        Test6_3(z1)
        System.Console.WriteLine(z1(1).instanceFld)

    End Sub


    Sub Test6(z As Byte())
        TestByRef(z(1))
    End Sub

    Sub Test6_1(z As Byte())
        TestByRef(ReturnArray(z)(1))
    End Sub

    Sub Test6_2(z As TestStruct())
        TestByRef(z(Return1()).instanceFld)
    End Sub

    Sub Test6_3(z As TestStruct())
        TestByRef(z(1).instanceFld)
    End Sub

    Sub Test1()
        TestByRef(20)
    End Sub

    Function Test2() As Long
        Dim x As Byte = 30
        Return TestByRef2(CInt(x))
    End Function

    Sub Test2_1()
        Dim x As Byte = 32
        TestByRef2(CInt(x))
    End Sub

    Function Test3() As Byte
        Dim x As Byte = 40
        TestByRef(x)
        Return x
    End Function

    Function Test3_1() As Long
        Dim x As Byte = 42
        Return TestByRef2(x) + x
    End Function

    Function Test3_2() As Byte
        Dim x As Byte = 44
        TestByRef2(x)
        Return x
    End Function

    Function Test4(x As Byte) As Byte
        TestByRef(x)
        Return x
    End Function

    Sub Test5()
        TestByRef(staticFld)
    End Sub

    Sub Test5_1(x As TestClass)
        TestByRef(x.instanceFld)
    End Sub

    Sub Test5_2()
        TestByRef(staticReadonlyFld)
    End Sub

    Sub Test5_3(x As TestClass)
        TestByRef(x.instanceReadonlyFld)
    End Sub

    Sub Test5_4(ByRef x As TestStruct)
        TestByRef(x.instanceFld)
    End Sub

    Function Test5_5(x As TestStruct) As Byte
        TestByRef(x.instanceFld)
        Return x.instanceFld
    End Function

    Sub Test5_6(x As TestStruct)
        TestByRef(x.instanceReadonlyFld)
    End Sub

    Sub Test5_7()
        TestByRef(ReturnInteger())
    End Sub

    Sub Test5_8()
        TestByRef(ReturnTestStruct().instanceFld2)
    End Sub

    Sub Test5_9(x As TestClass)
        TestByRef(ReturnTestClass(x).instanceFld)
    End Sub

    Sub Test5_10()
        TestByRef(1)
    End Sub

    Function ReturnInteger() As Integer
        Return 71
    End Function

    Function ReturnTestStruct() As TestStruct
        Dim x As TestStruct = New TestStruct()
        x.instanceFld2 = 73
        Return x
    End Function

    Function ReturnTestClass(x As TestClass) As TestClass
        Return x
    End Function

    Sub TestByRef(ByRef a As Integer)
        System.Console.WriteLine("TestByRef: {0}", a)
        a = a + 1
    End Sub

    Function TestByRef2(ByRef a As Integer) As Long
        System.Console.WriteLine("TestByRef2: {0}", a)
        a = a + 1
        Return a
    End Function

    Function ReturnArray(z As Byte()) As Byte()
        System.Console.WriteLine("ReturnArray")
        Return z
    End Function

    Function Return1() As Integer
        System.Console.WriteLine("Return1")
        Return 1
    End Function

End Module

Class TestClass
    Public Property instanceFld As Byte
    Private ReadOnly _instanceReadonlyFld As Integer
    Public ReadOnly Property instanceReadonlyFld As Integer
        Get
            Return _instanceReadonlyFld
        End Get
    End Property
End Class

Structure TestStruct
    Public Property instanceFld As Byte
    Private ReadOnly _instanceReadonlyFld As Integer
    Public ReadOnly Property instanceReadonlyFld As Integer
        Get
            Return _instanceReadonlyFld
        End Get
    End Property
    Public Property instanceFld2 As Integer

    Sub Test1()
        TestByRef(Me.instanceFld)
    End Sub

    Sub Test2()
        TestByRef(instanceFld)
    End Sub

    Sub Test3()
        TestByRef(Me.instanceReadonlyFld)
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
---
TestByRef: 20
---
TestByRef2: 30
31
---
TestByRef2: 32
---
TestByRef: 40
41
---
TestByRef2: 42
86
---
TestByRef2: 44
45
---
TestByRef: 51
52
---
TestByRef: 61
62
---
TestByRef: 63
64
---
TestByRef: 0
---
TestByRef: 0
---
TestByRef: 65
66
---
TestByRef: 66
67
---
TestByRef: 0
---
TestByRef: 71
---
TestByRef: 73
---
TestByRef: 64
65
---
TestByRef: 1
---
TestByRef: 81
82
---
ReturnArray
TestByRef: 82
83
---
Return1
TestByRef: 91
92
---
TestByRef: 92
93
]]>)
        End Sub

        <Fact>
        Public Sub ByRefArguments3()
            Dim compilationDef =
<compilation name="VBByRefArguments3">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim By As Byte = 53
        Dim Ob As Object

        Ob = New TestClass(By)
        System.Console.WriteLine(By)
        Ob = New TestStructure(By)
        System.Console.WriteLine(By)
    End Sub

End Module

Class TestClass
    Public Sub New(ByRef x As Integer)
        x = x + 1
    End Sub
End Class

Structure TestStructure
    Public Sub New(ByRef x As Integer)
        x = x + 1
    End Sub
End Structure
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
54
55
]]>)

        End Sub

        <Fact>
        Public Sub ByRefArguments4()
            Dim compilationDef =
<compilation name="VBByRefArguments4">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        System.Console.WriteLine("---")
        Test1()

        System.Console.WriteLine("---")
        System.Console.WriteLine(TestClass.baseStaticReadonlyFld)
        System.Console.WriteLine(TestClass.staticReadonlyFld)
        System.Console.WriteLine(TestClass.output)

        System.Console.WriteLine("---")
        Dim tc1 As TestClass = New TestClass()
        System.Console.WriteLine(tc1.instanceReadonlyFld)

        System.Console.WriteLine("---")
        Dim tc2 As TestClass = New TestClass(tc1)
        System.Console.WriteLine(tc1.instanceReadonlyFld)
        System.Console.WriteLine(tc2.instanceReadonlyFld)

        System.Console.WriteLine("---")
        Dim tc3 As TestClass = New TestClass(tc1, tc2)
        System.Console.WriteLine(TestClass.staticReadonlyFld)

        System.Console.WriteLine("---")
        Dim tc4 As TestClass = New TestClass(tc1, tc2, tc3)
        System.Console.WriteLine(tc4.baseInstanceReadonlyFld)

        System.Console.WriteLine("---")
        Test2(tc1)
        System.Console.WriteLine(TestClass.staticField)

        System.Console.WriteLine("---")
        Dim z As Byte() = New Byte() {0, 3}
        'Dim z As Byte() = New Byte() {0, 1, 2, 3, 4}
        Test3(z)
        System.Console.WriteLine(z((1 + 1 * 2)/3))
    End Sub


    Sub Test1()
        TestByRef(System.Int32.MinValue)
    End Sub

    Sub Test2(tc As TestClass)
        TestByRef(tc.staticField)
    End Sub

    Sub Test3(z As Byte())
        TestByRef(z((1 + 1 * 2)/3))
    End Sub

    Sub TestByRef(ByRef a As Integer)
        System.Console.WriteLine("TestByRef: {0}", a)
        a = a + 1
    End Sub


End Module

Class Base
    Public ReadOnly baseInstanceReadonlyFld As Integer
    Public Shared ReadOnly baseStaticReadonlyFld As Integer
End Class

Class TestClass
    Inherits Base

    Public ReadOnly instanceReadonlyFld As Integer
    Public Shared ReadOnly staticReadonlyFld As Integer
    Public Shared output As Integer
    Public Shared staticField As Byte

    Sub New()
        TestByRef(instanceReadonlyFld)
    End Sub

    Sub New(tc As TestClass)
        TestByRef(tc.instanceReadonlyFld)
    End Sub

    Sub New(tc1 As TestClass, tc2 As TestClass)
        TestByRef(Me.staticReadonlyFld)
    End Sub

    Sub New(tc1 As TestClass, tc2 As TestClass, tc3 As TestClass)
        TestByRef(Me.baseInstanceReadonlyFld)
    End Sub

    Shared Sub New()
        TestByRef(staticReadonlyFld)
        TestByRef(baseStaticReadonlyFld)

        Dim tc As TestClass = New TestClass()
        TestByRef(tc.instanceReadonlyFld)
        output = tc.instanceReadonlyFld
    End Sub

End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
---
TestByRef: -2147483648
---
0
TestByRef: 0
TestByRef: 0
TestByRef: 0
TestByRef: 1
1
1
---
TestByRef: 0
1
---
TestByRef: 1
1
0
---
TestByRef: 1
1
---
TestByRef: 0
0
---
TestByRef: 0
1
---
TestByRef: 3
4
]]>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldc.i4     0x80000000
  IL_0005:  stloc.0   
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ret       
}
]]>)

            verifier.VerifyIL("TestClass..ctor",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  call       "Sub Base..ctor()"
  IL_0006:  ldarg.0   
  IL_0007:  ldflda     "TestClass.instanceReadonlyFld As Integer"
  IL_000c:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0011:  ret       
}
]]>)

            verifier.VerifyIL("TestClass..ctor(TestClass)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  call       "Sub Base..ctor()"
  IL_0006:  ldarg.1   
  IL_0007:  ldfld      "TestClass.instanceReadonlyFld As Integer"
  IL_000c:  stloc.0   
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0014:  ret       
}
]]>)

            verifier.VerifyIL("TestClass..ctor(TestClass, TestClass)",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  call       "Sub Base..ctor()"
  IL_0006:  ldsfld     "TestClass.staticReadonlyFld As Integer"
  IL_000b:  stloc.0   
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0013:  ret       
}
]]>)

            verifier.VerifyIL("TestClass..ctor(TestClass, TestClass, TestClass)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  call       "Sub Base..ctor()"
  IL_0006:  ldarg.0   
  IL_0007:  ldfld      "Base.baseInstanceReadonlyFld As Integer"
  IL_000c:  stloc.0   
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0014:  ret       
}
]]>)

            verifier.VerifyIL("TestClass..cctor",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldsflda    "TestClass.staticReadonlyFld As Integer"
  IL_0005:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000a:  ldsfld     "Base.baseStaticReadonlyFld As Integer"
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0017:  newobj     "Sub TestClass..ctor()"
  IL_001c:  dup
  IL_001d:  ldfld      "TestClass.instanceReadonlyFld As Integer"
  IL_0022:  stloc.0
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_002a:  ldfld      "TestClass.instanceReadonlyFld As Integer"
  IL_002f:  stsfld     "TestClass.output As Integer"
  IL_0034:  ret
}
]]>)

            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldsfld     "TestClass.staticField As Byte"
  IL_0005:  stloc.0   
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ldloc.0   
  IL_000e:  conv.ovf.u1
  IL_000f:  stsfld     "TestClass.staticField As Byte"
  IL_0014:  ret       
}
]]>)

#If DONT_USE_BYREF_LOCALS_FOR_USE_TWICE Then
            verifier.VerifyIL("Module1.Test3",
<![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  3
  .locals init (Byte() V_0,
           Integer V_1)
  IL_0000:  ldarg.0   
  IL_0001:  dup       
  IL_0002:  stloc.0   
  IL_0003:  ldc.i4.1  
  IL_0004:  ldelem.u1 
  IL_0005:  stloc.1   
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_000d:  ldloc.0   
  IL_000e:  ldc.i4.1  
  IL_000f:  ldloc.1   
  IL_0010:  conv.ovf.u1
  IL_0011:  stelem.i1 
  IL_0012:  ret       
}
]]>)
#Else
            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldelema    "Byte"
  IL_0007:  dup
  IL_0008:  ldind.u1
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Sub Module1.TestByRef(ByRef Integer)"
  IL_0011:  ldloc.0
  IL_0012:  conv.ovf.u1
  IL_0013:  stind.i1
  IL_0014:  ret
}
]]>)
#End If
        End Sub

        <Fact>
        Public Sub ByRefArguments5()
            Dim compilationDef =
<compilation name="VBByRefArguments5">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()

        Dim s2 As TestStruct2 = New TestStruct2()
        s2.fld2.fld1 = 1

        System.Console.WriteLine((s2.fld2).Increment())
        System.Console.WriteLine(s2.fld2.fld1)
        System.Console.WriteLine((s2.fld2).Increment())
        System.Console.WriteLine(s2.fld2.fld1)
        System.Console.WriteLine((s2.fld2).Increment())
        System.Console.WriteLine(s2.fld2.fld1)
 
        System.Console.WriteLine("----")

        System.Console.WriteLine(s2.fld2.fld1)
        Test1(s2)
        System.Console.WriteLine(s2.fld2.fld1)
        Test2(s2)
        System.Console.WriteLine(s2.fld2.fld1)
        Test3(s2)
        System.Console.WriteLine(s2.fld2.fld1)
        Test4(s2)
        System.Console.WriteLine(s2.fld2.fld1)
    End Sub



    Sub Test1(ByRef s2 As TestStruct2)
        Increment(s2.fld2.fld1)
    End Sub

    Sub Test2(ByRef s2 As TestStruct2)
        Increment((s2).fld2.fld1)
    End Sub

    Sub Test3(ByRef s2 As TestStruct2)
        Increment((s2.fld2).fld1)
    End Sub

    Sub Test4(ByRef s2 As TestStruct2)
        Increment((s2.fld2.fld1))
    End Sub

    Sub Increment(ByRef x As Integer)
        x = x + 1
    End Sub

End Module


Structure TestStruct1
    Public fld1 As Integer

    Function Increment() As Integer
        fld1 = fld1 + 1
        Return fld1
    End Function
End Structure

Structure TestStruct2
    Public fld2 As TestStruct1

End Structure
    </file>
</compilation>

            Dim verifier = CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
2
2
3
3
4
4
----
4
5
5
5
5
]]>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  ldflda     "TestStruct2.fld2 As TestStruct1"
  IL_0006:  ldflda     "TestStruct1.fld1 As Integer"
  IL_000b:  call       "Sub Module1.Increment(ByRef Integer)"
  IL_0010:  ret       
}
]]>)
            verifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldflda     "TestStruct2.fld2 As TestStruct1"
  IL_0006:  ldfld      "TestStruct1.fld1 As Integer"
  IL_000b:  stloc.0   
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub Module1.Increment(ByRef Integer)"
  IL_0013:  ret       
}
]]>)
            verifier.VerifyIL("Module1.Test3",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldflda     "TestStruct2.fld2 As TestStruct1"
  IL_0006:  ldfld      "TestStruct1.fld1 As Integer"
  IL_000b:  stloc.0   
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub Module1.Increment(ByRef Integer)"
  IL_0013:  ret       
}
]]>)
            verifier.VerifyIL("Module1.Test4",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0   
  IL_0001:  ldflda     "TestStruct2.fld2 As TestStruct1"
  IL_0006:  ldfld      "TestStruct1.fld1 As Integer"
  IL_000b:  stloc.0   
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Sub Module1.Increment(ByRef Integer)"
  IL_0013:  ret       
}
]]>)
        End Sub

        <Fact()>
        Public Sub ByRefArguments6()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb"><![CDATA[ 
Module Program

    Sub Test1(ByRef x As Object)
        x = -2
    End Sub

    Function Test2(Of T)(a As T(), i As Integer) As System.Action
        Return Sub()
                   Test1(a(i))
               End Sub
    End Function

    Sub Main()
        Dim a = New Integer() {1, 2, 3}
        Dim x As System.Action = Test2(a, 1)
        x()
        System.Console.WriteLine(a(1))
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
-2
]]>)
        End Sub

        <Fact>
        Public Sub PassByRef()
            Dim source =
<compilation>
    <file name="c.vb">
Imports System
Class A
    Public Sub New(i As Short)
        FI = i
        _PI = i
        FB = New B(3)
        _PB = New B(3)
        FC = New C(4)
        _PC = New C(4)
        FT = New T(5)
        _PT = New T(5)
    End Sub
    Public FI As Short
    Private _PI As Short
    Property PI As Short
        Get
            Console.WriteLine("A.get_PI")
            Return _PI
        End Get
        Set(value As Short)
            Console.WriteLine("A.set_PI")
            _PI = value
        End Set
    End Property
    Public FC As C
    Private _PC As C
    Property PC As C
        Get
            Console.WriteLine("A.get_PC")
            Return _PC
        End Get
        Set(value As C)
            Console.WriteLine("A.set_PC")
            _PC = value
        End Set
    End Property
    Public FT As T
    Private _PT As T
    Property PT As T
        Get
            Console.WriteLine("A.get_PT")
            Return _PT
        End Get
        Set(value As T)
            Console.WriteLine("A.set_PT")
            _PT = value
        End Set
    End Property
    Public FB As B
    Private _PB As B
    Property PB As B
        Get
            Console.WriteLine("A.get_PB")
            Return _PB
        End Get
        Set(value As B)
            Console.WriteLine("A.set_PB")
            _PB = value
        End Set
    End Property
End Class
Class B
    Public Sub New(i As Short)
        FI = i
        _PI = i
        FC = New C(5)
        _PC = New C(5)
    End Sub
    Public FI As Short
    Private _PI As Short
    Property PI As Short
        Get
            Console.WriteLine("B.get_PI")
            Return _PI
        End Get
        Set(value As Short)
            Console.WriteLine("B.set_PI")
            _PI = value
        End Set
    End Property
    Public FC As C
    Private _PC As C
    Property PC As C
        Get
            Console.WriteLine("B.get_PC")
            Return _PC
        End Get
        Set(value As C)
            Console.WriteLine("B.set_PC")
            _PC = value
        End Set
    End Property
End Class
Structure S
    Public Sub New(i As Short)
        FI = i
        _PI = i
        FA = New A(2)
        _PA = New A(2)
        FC = New C(3)
        _PC = New C(3)
        FT = New T(4)
        _PT = New T(4)
    End Sub
    Public FI As Short
    Private _PI As Short
    Property PI As Short
        Get
            Console.WriteLine("S.get_PI")
            Return _PI
        End Get
        Set(value As Short)
            Console.WriteLine("S.set_PI")
            _PI = value
        End Set
    End Property
    Public FC As C
    Private _PC As C
    Property PC As C
        Get
            Console.WriteLine("S.get_PC")
            Return _PC
        End Get
        Set(value As C)
            Console.WriteLine("S.set_PC")
            _PC = value
        End Set
    End Property
    Public FT As T
    Private _PT As T
    Property PT As T
        Get
            Console.WriteLine("S.get_PT")
            Return _PT
        End Get
        Set(value As T)
            Console.WriteLine("S.set_PT")
            _PT = value
        End Set
    End Property
    Public FA As A
    Private _PA As A
    Property PA As A
        Get
            Console.WriteLine("S.get_PA")
            Return _PA
        End Get
        Set(value As A)
            Console.WriteLine("S.set_PA")
            _PA = value
        End Set
    End Property
End Structure
Structure T
    Public Sub New(i As Short)
        FI = i
        _PI = i
        FC = New C(6)
        _PC = New C(6)
    End Sub
    Public FI As Short
    Private _PI As Short
    Property PI As Short
        Get
            Console.WriteLine("T.get_PI")
            Return _PI
        End Get
        Set(value As Short)
            Console.WriteLine("T.set_PI")
            _PI = value
        End Set
    End Property
    Public FC As C
    Private _PC As C
    Property PC As C
        Get
            Console.WriteLine("T.get_PC")
            Return _PC
        End Get
        Set(value As C)
            Console.WriteLine("T.set_PC")
            _PC = value
        End Set
    End Property
End Structure
Class C
    Public N As Short
    Public Sub New(i As Short)
        N = i
    End Sub
    Public Overrides Function ToString() As String
        Return N.ToString()
    End Function
End Class
Class Prog
    Shared Sub Report(i As Integer)
        Console.WriteLine("=> {0}", i)
    End Sub
    Shared Sub Report(i As C)
        Console.WriteLine("=> {0}", i)
    End Sub
    Shared Sub M(ByRef i As Integer)
        Console.WriteLine("M")
        i = i + 1
    End Sub
    Shared Sub M(ByRef i As C)
        Console.WriteLine("M")
        i = New C(i.N + 1)
    End Sub
    Shared Sub Main()
        Dim x As A = New A(7)
        ' Value type members on class.
        M(x.FI)
        Report(x.FI)
        M(x.PI)
        Report(x.PI)
        ' Reference type members on class.
        M(x.FC)
        Report(x.FC)
        M(x.PC)
        Report(x.PC)
        ' Value type members on nested class.
        M(x.FB.FI)
        Report(x.FB.FI)
        M(x.FB.PI)
        Report(x.FB.PI)
        M(x.PB.FI)
        Report(x.PB.FI)
        M(x.PB.PI)
        Report(x.PB.PI)
        ' Reference type members on nested class.
        M(x.FB.FC)
        Report(x.FB.FC)
        M(x.FB.PC)
        Report(x.FB.PC)
        M(x.PB.FC)
        Report(x.PB.FC)
        M(x.PB.PC)
        Report(x.PB.PC)
        ' Value type members on nested struct.
        M(x.FT.FI)
        Report(x.FT.FI)
        M(x.FT.PI)
        Report(x.FT.PI)
        M(x.PT.FI)
        Report(x.PT.FI)
        M(x.PT.PI)
        Report(x.PT.PI)
        ' Reference type members on nested struct.
        M(x.FT.FC)
        Report(x.FT.FC)
        M(x.FT.PC)
        Report(x.FT.PC)
        M(x.PT.FC)
        Report(x.PT.FC)
        M(x.PT.PC)
        Report(x.PT.PC)
        Dim y As S = New S(9)
        ' Value type members on class.
        M(y.FI)
        Report(y.FI)
        M(y.PI)
        Report(y.PI)
        ' Reference type members on class.
        M(y.FC)
        Report(y.FC)
        M(y.PC)
        Report(y.PC)
        ' Value type members on nested class.
        M(y.FA.FI)
        Report(y.FA.FI)
        M(y.FA.PI)
        Report(y.FA.PI)
        M(y.PA.FI)
        Report(y.PA.FI)
        M(y.PA.PI)
        Report(y.PA.PI)
        ' Reference type members on nested class.
        M(y.FA.FC)
        Report(y.FA.FC)
        M(y.FA.PC)
        Report(y.FA.PC)
        M(y.PA.FC)
        Report(y.PA.FC)
        M(y.PA.PC)
        Report(y.PA.PC)
        ' Value type members on nested struct.
        M(y.FT.FI)
        Report(y.FT.FI)
        M(y.FT.PI)
        Report(y.FT.PI)
        M(y.PT.FI)
        Report(y.PT.FI)
        M(y.PT.PI)
        Report(y.PT.PI)
        ' Reference type members on nested struct.
        M(y.FT.FC)
        Report(y.FT.FC)
        M(y.FT.PC)
        Report(y.FT.PC)
        M(y.PT.FC)
        Report(y.PT.FC)
        M(y.PT.PC)
        Report(y.PT.PC)
    End Sub
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:=
            <![CDATA[M
=> 8
A.get_PI
M
A.set_PI
A.get_PI
=> 8
M
=> 5
A.get_PC
M
A.set_PC
A.get_PC
=> 5
M
=> 4
B.get_PI
M
B.set_PI
B.get_PI
=> 4
A.get_PB
M
A.get_PB
=> 4
A.get_PB
B.get_PI
M
B.set_PI
A.get_PB
B.get_PI
=> 4
M
=> 6
B.get_PC
M
B.set_PC
B.get_PC
=> 6
A.get_PB
M
A.get_PB
=> 6
A.get_PB
B.get_PC
M
B.set_PC
A.get_PB
B.get_PC
=> 6
M
=> 6
T.get_PI
M
T.set_PI
T.get_PI
=> 6
A.get_PT
M
A.get_PT
=> 5
A.get_PT
T.get_PI
M
T.set_PI
A.get_PT
T.get_PI
=> 5
M
=> 7
T.get_PC
M
T.set_PC
T.get_PC
=> 7
A.get_PT
M
A.get_PT
=> 6
A.get_PT
T.get_PC
M
T.set_PC
A.get_PT
T.get_PC
=> 6
M
=> 10
S.get_PI
M
S.set_PI
S.get_PI
=> 10
M
=> 4
S.get_PC
M
S.set_PC
S.get_PC
=> 4
M
=> 3
A.get_PI
M
A.set_PI
A.get_PI
=> 3
S.get_PA
M
S.get_PA
=> 3
S.get_PA
A.get_PI
M
A.set_PI
S.get_PA
A.get_PI
=> 3
M
=> 5
A.get_PC
M
A.set_PC
A.get_PC
=> 5
S.get_PA
M
S.get_PA
=> 5
S.get_PA
A.get_PC
M
A.set_PC
S.get_PA
A.get_PC
=> 5
M
=> 5
T.get_PI
M
T.set_PI
T.get_PI
=> 5
S.get_PT
M
S.get_PT
=> 4
S.get_PT
T.get_PI
M
T.set_PI
S.get_PT
T.get_PI
=> 4
M
=> 7
T.get_PC
M
T.set_PC
T.get_PC
=> 7
S.get_PT
M
S.get_PT
=> 6
S.get_PT
T.get_PC
M
T.set_PC
S.get_PT
T.get_PC
=> 6
]]>)
        End Sub

        ' Instance expressions used to reference
        ' statics should be skipped.
        <Fact>
        Public Sub PassByRefStaticFromInstance()
            Dim source =
<compilation>
    <file name="c.vb">
Imports System
Class A
    Public N As Short
    Public Sub New(i As Short)
        N = i
    End Sub
    Public Overrides Function ToString() As String
        Return N.ToString()
    End Function
End Class
Class B
    Public Shared FI As Short = 1
    Public Shared _PI As Short = 4
    Shared Property PI As Short
        Get
            Console.WriteLine("get_PI")
            Return _PI
        End Get
        Set(value As Short)
            Console.WriteLine("set_PI")
            _PI = value
        End Set
    End Property
    Public Shared FC As A = New A(7)
    Public Shared _PC As A = New A(10)
    Shared Property PC As A
        Get
            Console.WriteLine("get_PC")
            Return _PC
        End Get
        Set(value As A)
            Console.WriteLine("set_PC")
            _PC = value
        End Set
    End Property
    Public Function G() As B
        Console.WriteLine("G")
        Return Me
    End Function
End Class
Class Prog
    Shared Function F() As B
        Console.WriteLine("F")
        Return Nothing
    End Function
    Shared Sub Report(i As Integer)
        Console.WriteLine("=> {0}", i)
    End Sub
    Shared Sub Report(i As A)
        Console.WriteLine("=> {0}", i)
    End Sub
    Shared Sub M(ByRef i As Integer)
        Console.WriteLine("M")
        i = i + 1
    End Sub
    Shared Sub M(ByRef i As A)
        Console.WriteLine("M")
        i = New A(i.N + 1)
    End Sub
    Shared Sub Main()
        Dim x As B = New B()
        M(x.G().FI)
        Report(B.FI)
        M(x.G().PI)
        Report(B._PI)
        M(F().FC)
        Report(B.FC)
        M(F().PC)
        Report(B._PC)
    End Sub
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:=
            <![CDATA[M
=> 2
get_PI
M
set_PI
=> 5
M
=> 8
get_PC
M
set_PC
=> 11
]]>)
        End Sub

        <Fact>
        Public Sub PassByRefArgs()
            Dim source =
<compilation>
    <file name="c.vb">
Imports System
Class Value
    Private Shared _n As Integer = 0
    Shared Function [Next]()
        _n = _n + 1
        Return _n
    End Function
End Class
Class C
    Private Shared _P
    Shared Property P(x As Object)
        Get
            Console.WriteLine("P({0}) (= {1})", x, _P)
            Return _P
        End Get
        Set(value)
            Console.WriteLine("P({0}) = {1}", x, value)
            _P = value
        End Set
    End Property
    Shared Sub Main()
        _P = Value.Next()
        F(C.P(F(C.P(Value.Next()))))
    End Sub
    Shared Function F(ByRef o)
        Console.WriteLine("F({0})", o)
        o = Value.Next()
        Return o
    End Function
End Class
    </file>
</compilation>

            Dim compilationVerifier = CompileAndVerify(source, expectedOutput:=
            <![CDATA[P(2) (= 1)
F(1)
P(2) = 3
P(3) (= 3)
F(3)
P(3) = 4
]]>)
            compilationVerifier.VerifyIL("C.Main",
            <![CDATA[
{
  // Code size      104 (0x68)
  .maxstack  3
  .locals init (Object V_0,
  Object V_1,
  Object V_2)
  IL_0000:  call       "Function Value.Next() As Object"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  stsfld     "C._P As Object"
  IL_000f:  call       "Function Value.Next() As Object"
  IL_0014:  dup
  IL_0015:  stloc.1
  IL_0016:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001b:  call       "Function C.get_P(Object) As Object"
  IL_0020:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0025:  stloc.2
  IL_0026:  ldloca.s   V_2
  IL_0028:  call       "Function C.F(ByRef Object) As Object"
  IL_002d:  ldloc.1
  IL_002e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0033:  ldloc.2
  IL_0034:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0039:  call       "Sub C.set_P(Object, Object)"
  IL_003e:  dup
  IL_003f:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0044:  call       "Function C.get_P(Object) As Object"
  IL_0049:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_004e:  stloc.0
  IL_004f:  ldloca.s   V_0
  IL_0051:  call       "Function C.F(ByRef Object) As Object"
  IL_0056:  pop
  IL_0057:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_005c:  ldloc.0
  IL_005d:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0062:  call       "Sub C.set_P(Object, Object)"
  IL_0067:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub CopyBackDiagnostic1()
            Dim compilationDef =
<compilation name="VBCopyBackDiagnostic1">
    <file name="a.vb">
Module Module1

    Sub Main()

        Dim By As Byte = 53
        TestByRef(By)

    End Sub

    Sub TestByRef(ByRef a As Integer)
    End Sub

End Module
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                compilationDef,
                New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom)).
            VerifyDiagnostics(Diagnostic(ERRID.WRN_ImplicitConversionCopyBack, "By").WithArguments("a", "Integer", "Byte"))
        End Sub

        <Fact>
        Public Sub Bug4275()

            Dim compilationDef =
<compilation name="Bug4275">
    <file name="a.vb">
Module M
    Sub Foo()
    End Sub

    Sub Bar(Of T)()
    End Sub

    Function Foo1() As Integer
        Return 0
    End Function

    Sub Main()
        Foo$()
        Foo$
        M.Foo$()
        M.Foo$
        Bar%(Of Integer)()
        Bar$(Of Integer)

        Dim x As Object

        x=Foo1$
    End Sub
End Module
    </file>
</compilation>

            CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef).
            VerifyDiagnostics(
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "Foo$()").WithArguments("$", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "Foo$").WithArguments("$", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "M.Foo$()").WithArguments("$", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "M.Foo$").WithArguments("$", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "Bar%(Of Integer)()").WithArguments("%", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "Bar$(Of Integer)").WithArguments("$", "Void"),
                Diagnostic(ERRID.ERR_TypecharNoMatch2, "Foo1$").WithArguments("$", "Integer"))
        End Sub

        <Fact>
        Public Sub CallGenericMethod()
            Dim compilationDef =
<compilation name="CallGenericMethod">
    <file name="a.vb">
Module M
    Sub Bar(Of T)(x as T)
        System.Console.WriteLine(x)
    End Sub


    Sub Main()
        Bar(Of Integer)("1234")
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="1234")
        End Sub

        <Fact>
        Public Sub ConstructorCallDiagnostic()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ConstructorCallDiagnostic">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim z1 = New TC1()
        Dim z2 = New TC1(1, 2, 3)
    End Sub
End Module

Class TC1
    Sub New(x As Integer)
    End Sub
    Sub New(x As Integer, y As Double)
    End Sub
End Class
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Dim z1 = New TC1()
                     ~~~
BC30516: Overload resolution failed because no accessible 'New' accepts this number of arguments.
        Dim z2 = New TC1(1, 2, 3)
                     ~~~
</expected>)
        End Sub

        <WorkItem(539691, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539691")>
        <Fact>
        Public Sub DiagnosticsOnInvalidConstructorCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="DiagnosticsOnInvalidConstructorCall">
    <file name="a.vb">
class C
   sub Foo()
      dim x = new C(4,5,6)
   end sub
end class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim objectCreationNode = tree.FindNodeOrTokenByKind(SyntaxKind.ObjectCreationExpression)
            Dim semanticInfo = semanticModel.GetSemanticInfoSummary(CType(objectCreationNode, ExpressionSyntax))

            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
        End Sub

        <Fact>
        Public Sub ByRefParamArraysFromMetadata()
            Dim source =
<compilation name="ByRefParamArraysFromMetadata">
    <file name="a.vb">
Imports System

Module M

    Sub Main()
        Test(Nothing)
    End Sub

    Sub Test(d0 As DelegateByRefParamArray_Base())
        Dim d1 As DelegateByRefParamArray_Base() = Nothing

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1(d0)
        Console.WriteLine(d0 Is Nothing)

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1(d1)
        Console.WriteLine(d1 Is Nothing)

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1(_d2)
        Console.WriteLine(_d2 Is Nothing)

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1(d2)
        Console.WriteLine(d2 Is Nothing)

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1((d2))
        Console.WriteLine(d2 Is Nothing)

        Dim d3()() As DelegateByRefParamArray_Base = New DelegateByRefParamArray_Base()() {Nothing}

        DelegateByRefParamArray.SubWithByRefParamArrayOfReferenceTypes_Identify_1(d3(0))
        Console.WriteLine(d3(0) Is Nothing)
    End Sub

    Dim _d2 As DelegateByRefParamArray_Base() = Nothing

    Property d2 As DelegateByRefParamArray_Base()
        Get
            Return _d2
        End Get
        Set(value As DelegateByRefParamArray_Base())
            System.Console.WriteLine("d2.Set")
            _d2 = value
        End Set
    End Property

End Module
    </file>
</compilation>
            Dim assemblyPath = TestReferences.SymbolsTests.DelegateImplementation.DelegateByRefParamArray

            CompileAndVerify(source,
                        additionalRefs:={assemblyPath},
                         expectedOutput:=<![CDATA[
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
Called SubWithByRefParamArrayOfReferenceTypes_Identify_1.
True
]]>)


        End Sub


        <Fact>
        Public Sub ByRefParametersOnProperties1()
            Dim source =
<compilation name="ByRefParametersOnProperties1">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        DoTest(129)
    End Sub

    Sub DoTest(y6 As Integer)
        Dim x As New PropertiesWithByRef()
        Dim y1 As Integer = 123
        Dim y2 As Byte = 124
        Dim z As Integer

        z = x.P1(y1)
        System.Console.WriteLine(y1)
        x.P1(y1) = z + 1
        System.Console.WriteLine(y1)

        z = x.P1(y2)
        System.Console.WriteLine(y2)
        x.P1(y2) = z + 1
        System.Console.WriteLine(y2)

        y3 = 125
        z = x.P1(y3)
        System.Console.WriteLine(y3)
        x.P1(y3) = z + 1
        System.Console.WriteLine(y3)

        y4 = 126
        z = x.P1(y4)
        System.Console.WriteLine(y4)
        x.P1(y4) = z + 1
        System.Console.WriteLine(y4)

        Dim t As New Test1()
        t.y5 = 127
        z = x.P1(t.y5)
        System.Console.WriteLine(t.y5)
        x.P1(t.y5) = z + 1
        System.Console.WriteLine(t.y5)

        Dim ar As Integer() = New Integer() {128}
        z = x.P1(ar(0))
        System.Console.WriteLine(ar(0))
        x.P1(ar(0)) = z + 1
        System.Console.WriteLine(ar(0))

        z = x.P1(y6)
        System.Console.WriteLine(y6)
        x.P1(y6) = z + 1
        System.Console.WriteLine(y6)
    End Sub

    Private _y3 As Integer
    Property y3 As Integer
        Get
            System.Console.WriteLine("Executing get_y3")
            Return _y3
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Executing set_y3")
            _y3 = value
        End Set
    End Property

    Private y4 As Integer


End Module

Class Test1
    Public y5 As Integer
End Class
    </file>
</compilation>
            CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(123)
123
set_P1(123)
123
get_P1(124)
124
set_P1(124)
124
Executing set_y3
Executing get_y3
get_P1(125)
Executing get_y3
125
Executing get_y3
set_P1(125)
Executing get_y3
125
get_P1(126)
126
set_P1(126)
126
get_P1(127)
127
set_P1(127)
127
get_P1(128)
128
set_P1(128)
128
get_P1(129)
129
set_P1(129)
129
]]>)


        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties2()
            Dim source =
<compilation name="ByRefParametersOnProperties2">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()
        Dim ar As Integer() = New Integer() {1}
        DoTest(x, ar)
        System.Console.WriteLine(ar(0))
    End Sub

    Sub DoTest(x As PropertiesWithByRef, ar As Integer())
        PassByRef(x.P1(ar(0)), ar)
    End Sub

    Sub PassByRef(ByRef x As Integer, ar As Integer())
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, ar(0))
    End Sub

End Module
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(1)
PassByRef: 2, 1.
set_P1(1)
1
]]>)


            compilationVerifier.VerifyIL("Module1.DoTest",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldarg.1
  IL_0003:  ldc.i4.0
  IL_0004:  ldelem.i4
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  stloc.2
  IL_0008:  ldloca.s   V_2
  IL_000a:  callvirt   "Function PropertiesWithByRef.get_P1(ByRef Integer) As Integer"
  IL_000f:  stloc.1
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldarg.1
  IL_0013:  call       "Sub Module1.PassByRef(ByRef Integer, Integer())"
  IL_0018:  ldloc.0
  IL_0019:  stloc.2
  IL_001a:  ldloca.s   V_2
  IL_001c:  ldloc.1
  IL_001d:  callvirt   "Sub PropertiesWithByRef.set_P1(ByRef Integer, Integer)"
  IL_0022:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties3()
            Dim source =
<compilation name="ByRefParametersOnProperties3">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()
        Dim y As Integer = 1

        System.Console.WriteLine(DoTest(x, y))
    End Sub

    Function DoTest(x As PropertiesWithByRef, y As Integer) As Integer
        PassByRef(x.P1(y), y)
        Return y
    End Function

    Sub PassByRef(ByRef x As Integer, y As Integer)
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, y)
    End Sub

End Module
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(1)
PassByRef: 2, 1.
set_P1(1)
1
]]>)


            compilationVerifier.VerifyIL("Module1.DoTest",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  4
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldarg.1
  IL_0003:  dup
  IL_0004:  stloc.0
  IL_0005:  stloc.2
  IL_0006:  ldloca.s   V_2
  IL_0008:  callvirt   "Function PropertiesWithByRef.get_P1(ByRef Integer) As Integer"
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldarg.1
  IL_0011:  call       "Sub Module1.PassByRef(ByRef Integer, Integer)"
  IL_0016:  ldloc.0
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  ldloc.1
  IL_001b:  callvirt   "Sub PropertiesWithByRef.set_P1(ByRef Integer, Integer)"
  IL_0020:  ldarg.1
  IL_0021:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties4()
            Dim source =
<compilation name="ByRefParametersOnProperties4">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()

        System.Console.WriteLine(DoTest(x))
    End Sub

    Function DoTest(x As PropertiesWithByRef) As Integer
        Dim y As Integer = 1
        PassByRef(x.P1(y), y)
        Return y
    End Function

    Sub PassByRef(ByRef x As Integer, y As Integer)
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, y)
    End Sub

End Module
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(1)
PassByRef: 2, 1.
set_P1(1)
1
]]>)


            compilationVerifier.VerifyIL("Module1.DoTest",
            <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  4
  .locals init (Integer V_0, //y
  Integer V_1,
  Integer V_2,
  Integer V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldarg.0
  IL_0003:  dup
  IL_0004:  ldloc.0
  IL_0005:  dup
  IL_0006:  stloc.1
  IL_0007:  stloc.3
  IL_0008:  ldloca.s   V_3
  IL_000a:  callvirt   "Function PropertiesWithByRef.get_P1(ByRef Integer) As Integer"
  IL_000f:  stloc.2
  IL_0010:  ldloca.s   V_2
  IL_0012:  ldloc.0
  IL_0013:  call       "Sub Module1.PassByRef(ByRef Integer, Integer)"
  IL_0018:  ldloc.1
  IL_0019:  stloc.3
  IL_001a:  ldloca.s   V_3
  IL_001c:  ldloc.2
  IL_001d:  callvirt   "Sub PropertiesWithByRef.set_P1(ByRef Integer, Integer)"
  IL_0022:  ldloc.0
  IL_0023:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties5()
            Dim source =
<compilation name="ByRefParametersOnProperties5">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()

        DoTest(x)
        System.Console.WriteLine(y)
    End Sub

    Dim y As Integer = 1

    Sub DoTest(x As PropertiesWithByRef)
        PassByRef(x.P1(y), y)
    End Sub

    Sub PassByRef(ByRef x As Integer, y As Integer)
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, y)
    End Sub

End Module
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(1)
PassByRef: 2, 1.
set_P1(1)
1
]]>)


            compilationVerifier.VerifyIL("Module1.DoTest",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  4
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldsfld     "Module1.y As Integer"
  IL_0007:  dup
  IL_0008:  stloc.0
  IL_0009:  stloc.2
  IL_000a:  ldloca.s   V_2
  IL_000c:  callvirt   "Function PropertiesWithByRef.get_P1(ByRef Integer) As Integer"
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_1
  IL_0014:  ldsfld     "Module1.y As Integer"
  IL_0019:  call       "Sub Module1.PassByRef(ByRef Integer, Integer)"
  IL_001e:  ldloc.0
  IL_001f:  stloc.2
  IL_0020:  ldloca.s   V_2
  IL_0022:  ldloc.1
  IL_0023:  callvirt   "Sub PropertiesWithByRef.set_P1(ByRef Integer, Integer)"
  IL_0028:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties6()
            Dim source =
<compilation name="ByRefParametersOnProperties6">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()
        s.y = 1
        DoTest(x)
        System.Console.WriteLine(s.y)
    End Sub

    Dim s As S1

    Sub DoTest(x As PropertiesWithByRef)
        PassByRef(x.P1(s.y), s.y)
    End Sub

    Sub PassByRef(ByRef x As Integer, y As Integer)
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, y)
    End Sub

    Structure S1
        Public y As Integer
    End Structure
End Module
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(1)
PassByRef: 2, 1.
set_P1(1)
1
]]>)


            compilationVerifier.VerifyIL("Module1.DoTest",
            <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2)
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldsflda    "Module1.s As Module1.S1"
  IL_0007:  ldfld      "Module1.S1.y As Integer"
  IL_000c:  dup
  IL_000d:  stloc.0
  IL_000e:  stloc.2
  IL_000f:  ldloca.s   V_2
  IL_0011:  callvirt   "Function PropertiesWithByRef.get_P1(ByRef Integer) As Integer"
  IL_0016:  stloc.1
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldsflda    "Module1.s As Module1.S1"
  IL_001e:  ldfld      "Module1.S1.y As Integer"
  IL_0023:  call       "Sub Module1.PassByRef(ByRef Integer, Integer)"
  IL_0028:  ldloc.0
  IL_0029:  stloc.2
  IL_002a:  ldloca.s   V_2
  IL_002c:  ldloc.1
  IL_002d:  callvirt   "Sub PropertiesWithByRef.set_P1(ByRef Integer, Integer)"
  IL_0032:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ByRefParametersOnProperties7()
            Dim source =
<compilation name="ByRefParametersOnProperties7">
    <file name="a.vb">
Option Strict Off

Module Module1

    Sub Main()
        DoTest(129)
    End Sub

    Sub DoTest(y6 As Integer)
        Dim x As New PropertiesWithByRef()
        Dim y1 As Integer = 123
        Dim y2 As Byte = 124

        PassByRef(x.P1(y1), y1)
        System.Console.WriteLine(y1)

        PassByRef(x.P1(y2), y2)
        System.Console.WriteLine(y2)

        y3 = 125
        PassByRef(x.P1(y3), y3)
        System.Console.WriteLine(y3)

        y4 = 126
        PassByRef(x.P1(y4), y4)
        System.Console.WriteLine(y4)

        Dim t As New Test1()
        t.y5 = 127
        PassByRef(x.P1(t.y5), t.y5)
        System.Console.WriteLine(t.y5)

        Dim ar As Integer() = New Integer() {128}
        PassByRef(x.P1(ar(0)), ar(0))
        System.Console.WriteLine(ar(0))

        PassByRef(x.P1(y6), y6)
        System.Console.WriteLine(y6)
    End Sub

    Sub PassByRef(ByRef x As Integer, ByRef y As Integer)
        System.Console.WriteLine("PassByRef: {0}, {1}.", x, y)
        x = x + 25
        y = y + 50
    End Sub

    Private _y3 As Integer
    Property y3 As Integer
        Get
            System.Console.WriteLine("Executing get_y3")
            Return _y3
        End Get
        Set(value As Integer)
            System.Console.WriteLine("Executing set_y3")
            _y3 = value
        End Set
    End Property

    Private y4 As Integer


End Module

Class Test1
    Public y5 As Integer
End Class
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(123)
PassByRef: 124, 123.
set_P1(123)
173
get_P1(124)
PassByRef: 125, 124.
set_P1(124)
174
Executing set_y3
Executing get_y3
get_P1(125)
Executing get_y3
PassByRef: 126, 125.
Executing set_y3
set_P1(125)
Executing get_y3
175
get_P1(126)
PassByRef: 127, 126.
set_P1(126)
176
get_P1(127)
PassByRef: 128, 127.
set_P1(127)
177
get_P1(128)
PassByRef: 129, 128.
set_P1(128)
178
get_P1(129)
PassByRef: 130, 129.
set_P1(129)
179
]]>)

        End Sub

        <Fact>
        Public Sub MeInByRefContext1()
            Dim source =
<compilation name="MeInByRefContext1">
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1

    Sub Main()
        Dim x As New TestStruct1(100)
        x.Test1()
        x.Test2()

        System.Console.WriteLine("-----")

        Dim y As New TestClass1(50)
        y.Test1()
        y.Test2()
        y.Test3()
    End Sub

    Function PassByRef0(ByRef x As Integer) As Integer
        x = x + 1
        Return x
    End Function

End Module


Structure TestStruct1
    Public fld1 As Integer

    Sub New(x As Integer)
        fld1 = x
        System.Console.WriteLine(PassByRef0(Me.fld1))
        System.Console.WriteLine(Me.fld1)

        System.Console.WriteLine(PassByRef1(Me))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Sub Test1()
        System.Console.WriteLine(PassByRef0(Me.fld1))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Sub Test2()
        System.Console.WriteLine(PassByRef1(Me))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Shared Function PassByRef1(ByRef x As TestStruct1) As Integer
        x.fld1 = x.fld1 + 1
        Return x.fld1
    End Function

End Structure

Class TestClass1
    Public fld1 As Integer

    Sub New(x As Integer)
        fld1 = x
        System.Console.WriteLine(PassByRef0(Me.fld1))
        System.Console.WriteLine(Me.fld1)

        If x = 50 Then
            System.Console.WriteLine(PassByRef2(Me))
            System.Console.WriteLine(Me.fld1)
        End If
    End Sub

    Sub Test1()
        System.Console.WriteLine(PassByRef0(Me.fld1))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Sub Test2()
        System.Console.WriteLine(PassByRef1(Me))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Sub Test3()
        System.Console.WriteLine(PassByRef2(Me))
        System.Console.WriteLine(Me.fld1)
    End Sub

    Shared Function PassByRef1(ByRef x As TestClass1) As Integer
        x.fld1 = x.fld1 + 1
        Return x.fld1
    End Function

    Shared Function PassByRef2(ByRef x As TestClass1) As Integer
        Dim old As TestClass1 = x
        x = New TestClass1(x.fld1 + 15)
        Return old.fld1
    End Function

End Class

Structure S1

    Sub Test1()
        PassByRef1(Me)
    End Sub

    Sub Test2()
        PassByRef1((Me))
    End Sub

    Sub Test3()
        Me.Test1()
    End Sub

    Shared Sub PassByRef1(ByRef x As S1)
    End Sub
End Structure

Class C2

    Sub Test1()
        PassByRef1(Me)
    End Sub

    Sub Test2()
        PassByRef1((Me))
    End Sub

    Sub Test3()
        Me.Test1()
    End Sub

    Shared Sub PassByRef1(ByRef x As C2)
    End Sub
End Class
    </file>
</compilation>
            Dim compilationVerifier = CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
101
101
102
101
102
102
103
102
-----
51
51
67
67
51
51
52
52
53
53
69
69
53
53
]]>)

            compilationVerifier.VerifyIL("TestStruct1..ctor(Integer)",
            <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (TestStruct1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  initobj    "TestStruct1"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      "TestStruct1.fld1 As Integer"
  IL_000e:  ldarg.0
  IL_000f:  ldflda     "TestStruct1.fld1 As Integer"
  IL_0014:  call       "Function Module1.PassByRef0(ByRef Integer) As Integer"
  IL_0019:  call       "Sub System.Console.WriteLine(Integer)"
  IL_001e:  ldarg.0
  IL_001f:  ldfld      "TestStruct1.fld1 As Integer"
  IL_0024:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0029:  ldarg.0
  IL_002a:  ldobj      "TestStruct1"
  IL_002f:  stloc.0
  IL_0030:  ldloca.s   V_0
  IL_0032:  call       "Function TestStruct1.PassByRef1(ByRef TestStruct1) As Integer"
  IL_0037:  call       "Sub System.Console.WriteLine(Integer)"
  IL_003c:  ldarg.0
  IL_003d:  ldfld      "TestStruct1.fld1 As Integer"
  IL_0042:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0047:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestStruct1.Test1",
            <![CDATA[
{
// Code size       28 (0x1c)
.maxstack  1
IL_0000:  ldarg.0
IL_0001:  ldflda     "TestStruct1.fld1 As Integer"
IL_0006:  call       "Function Module1.PassByRef0(ByRef Integer) As Integer"
IL_000b:  call       "Sub System.Console.WriteLine(Integer)"
IL_0010:  ldarg.0
IL_0011:  ldfld      "TestStruct1.fld1 As Integer"
IL_0016:  call       "Sub System.Console.WriteLine(Integer)"
IL_001b:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestStruct1.Test2",
            <![CDATA[
{
// Code size       31 (0x1f)
.maxstack  1
.locals init (TestStruct1 V_0)
IL_0000:  ldarg.0
IL_0001:  ldobj      "TestStruct1"
IL_0006:  stloc.0
IL_0007:  ldloca.s   V_0
IL_0009:  call       "Function TestStruct1.PassByRef1(ByRef TestStruct1) As Integer"
IL_000e:  call       "Sub System.Console.WriteLine(Integer)"
IL_0013:  ldarg.0
IL_0014:  ldfld      "TestStruct1.fld1 As Integer"
IL_0019:  call       "Sub System.Console.WriteLine(Integer)"
IL_001e:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestClass1..ctor",
            <![CDATA[
{
// Code size       71 (0x47)
.maxstack  2
.locals init (TestClass1 V_0)
IL_0000:  ldarg.0
IL_0001:  call       "Sub Object..ctor()"
IL_0006:  ldarg.0
IL_0007:  ldarg.1
IL_0008:  stfld      "TestClass1.fld1 As Integer"
IL_000d:  ldarg.0
IL_000e:  ldflda     "TestClass1.fld1 As Integer"
IL_0013:  call       "Function Module1.PassByRef0(ByRef Integer) As Integer"
IL_0018:  call       "Sub System.Console.WriteLine(Integer)"
IL_001d:  ldarg.0
IL_001e:  ldfld      "TestClass1.fld1 As Integer"
IL_0023:  call       "Sub System.Console.WriteLine(Integer)"
IL_0028:  ldarg.1
IL_0029:  ldc.i4.s   50
IL_002b:  bne.un.s   IL_0046
IL_002d:  ldarg.0
IL_002e:  stloc.0
IL_002f:  ldloca.s   V_0
IL_0031:  call       "Function TestClass1.PassByRef2(ByRef TestClass1) As Integer"
IL_0036:  call       "Sub System.Console.WriteLine(Integer)"
IL_003b:  ldarg.0
IL_003c:  ldfld      "TestClass1.fld1 As Integer"
IL_0041:  call       "Sub System.Console.WriteLine(Integer)"
IL_0046:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestClass1.Test1",
            <![CDATA[
{
// Code size       28 (0x1c)
.maxstack  1
IL_0000:  ldarg.0
IL_0001:  ldflda     "TestClass1.fld1 As Integer"
IL_0006:  call       "Function Module1.PassByRef0(ByRef Integer) As Integer"
IL_000b:  call       "Sub System.Console.WriteLine(Integer)"
IL_0010:  ldarg.0
IL_0011:  ldfld      "TestClass1.fld1 As Integer"
IL_0016:  call       "Sub System.Console.WriteLine(Integer)"
IL_001b:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestClass1.Test2",
            <![CDATA[
{
// Code size       26 (0x1a)
.maxstack  1
.locals init (TestClass1 V_0)
IL_0000:  ldarg.0
IL_0001:  stloc.0
IL_0002:  ldloca.s   V_0
IL_0004:  call       "Function TestClass1.PassByRef1(ByRef TestClass1) As Integer"
IL_0009:  call       "Sub System.Console.WriteLine(Integer)"
IL_000e:  ldarg.0
IL_000f:  ldfld      "TestClass1.fld1 As Integer"
IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
IL_0019:  ret
}
]]>)

            compilationVerifier.VerifyIL("TestClass1.Test3",
            <![CDATA[
{
// Code size       26 (0x1a)
.maxstack  1
.locals init (TestClass1 V_0)
IL_0000:  ldarg.0
IL_0001:  stloc.0
IL_0002:  ldloca.s   V_0
IL_0004:  call       "Function TestClass1.PassByRef2(ByRef TestClass1) As Integer"
IL_0009:  call       "Sub System.Console.WriteLine(Integer)"
IL_000e:  ldarg.0
IL_000f:  ldfld      "TestClass1.fld1 As Integer"
IL_0014:  call       "Sub System.Console.WriteLine(Integer)"
IL_0019:  ret
}
]]>)

            compilationVerifier.VerifyIL("S1.Test1",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S1"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub S1.PassByRef1(ByRef S1)"
  IL_000e:  ret
}
]]>)

            compilationVerifier.VerifyIL("S1.Test2",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (S1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      "S1"
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       "Sub S1.PassByRef1(ByRef S1)"
  IL_000e:  ret
}
]]>)

            compilationVerifier.VerifyIL("S1.Test3",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub S1.Test1()"
  IL_0006:  ret
}
]]>)

            compilationVerifier.VerifyIL("C2.Test1",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (C2 V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Sub C2.PassByRef1(ByRef C2)"
  IL_0009:  ret
}
]]>)

            compilationVerifier.VerifyIL("C2.Test2",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (C2 V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "Sub C2.PassByRef1(ByRef C2)"
  IL_0009:  ret
}
]]>)

            compilationVerifier.VerifyIL("C2.Test3",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub C2.Test1()"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub Bug7195()
            Dim compilationDef =
<compilation name="Bug7195">
    <file name="a.vb">
Option Strict Off
Imports System
Module Module1
    Sub Main()
        Dim s2 As TestStruct2 = New TestStruct2()
        s2.fld2.fld1 = 1
        System.Console.WriteLine((s2).fld2.Increment())
        System.Console.WriteLine(s2.fld2.fld1)
        System.Console.WriteLine((s2.fld2).Increment())
        System.Console.WriteLine(s2.fld2.fld1)
        System.Console.WriteLine(((s2).fld2).Increment())
        System.Console.WriteLine(s2.fld2.fld1)
    End Sub
End Module

Structure TestStruct1
    Public fld1 As Integer
    Function Increment() As Integer
        fld1 = fld1 + 1
        Return fld1
    End Function
End Structure
Structure TestStruct2
    Public fld2 As TestStruct1
End Structure
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
2
2
3
3
4
4
]]>)
        End Sub

        <Fact>
        Public Sub ByRefParametersOnPropertiesInLambda1()
            Dim source =
<compilation name="ByRefParametersOnPropertiesInLambda1">
    <file name="a.vb">
Option Strict On

Module Module1

    Sub Main()
        Dim x As New PropertiesWithByRef()
        Dim y1 As Integer = 123
        Dim z As Integer

        Dim d1 As System.Action = Sub()
                                        z = x.P1(y1)
                                  End Sub

        d1()
        System.Console.WriteLine(y1)

        Dim d2 As System.Action = Sub()
                                        x.P1(y1) = z + 1
                                  End Sub

        d2()
        System.Console.WriteLine(y1)
    End Sub
End Module
    </file>
</compilation>
            CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
get_P1(123)
123
set_P1(123)
123
]]>)


        End Sub

        <Fact>
        Public Sub ByRefInInitializer1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ByRefInInitializer1">
    <file name="a.vb">
Class T1
    Public ReadOnly x1 As Integer
    Public ReadOnly x2 As Integer
    Public Shared ReadOnly x3 As Integer
    Public Shared ReadOnly x4 As Integer
    Public ReadOnly x5 As Integer
    Public ReadOnly x6 As Integer
    Public Shared ReadOnly x7 As Integer
    Public Shared ReadOnly x8 As Integer
    Public ReadOnly x9 As Integer
    Public Shared ReadOnly x10 As Integer

    Public y1 As Integer = TestByRef(x1)
    Public Shared y2 As Integer = TestByRef(x2)

    Public z1 As Integer = TestByRef(x3)
    Public Shared z2 As Integer = TestByRef(x4)

    Public Property y3 As Integer = TestByRef(x5)
    Public Shared Property y4 As Integer = TestByRef(x6)

    Public Property z3 As Integer = TestByRef(x7)
    Public Shared Property z4 As Integer = TestByRef(x8)

    Public Const y5 As Integer = TestByRef(x9).MaxValue
    Public Const y6 As Integer = TestByRef(x10).MaxValue
End Class

Module Module1
    Sub Main()
    End Sub

    Function TestByRef(ByRef x As Integer) As Integer
        x = x + 100
        Return x
    End Function
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Shared y2 As Integer = TestByRef(x2)
                                            ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Shared Property y4 As Integer = TestByRef(x6)
                                                     ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Const y5 As Integer = TestByRef(x9).MaxValue
                                           ~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
    Public Const y6 As Integer = TestByRef(x10).MaxValue
                                 ~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub ByRefInInitializer2()
            Dim source =
<compilation name="ByRefInInitializer2">
    <file name="a.vb">
Imports System

Class T1
    Public ReadOnly x1 As Integer
    Public ReadOnly x2 As Integer
    Public Shared ReadOnly x3 As Integer
    Public Shared ReadOnly x4 As Integer
    Public ReadOnly x5 As Integer
    Public ReadOnly x6 As Integer
    Public Shared ReadOnly x7 As Integer
    Public Shared ReadOnly x8 As Integer
    Public ReadOnly x9 As Integer
    Public Shared ReadOnly x10 As Integer

    Public y1 As Integer = TestByRef(x1)
    'Public Shared y2 As Integer = TestByRef(x2)

    Public z1 As Integer = TestByRef(x3)
    Public Shared z2 As Integer = TestByRef(x4)

    Public Property y3 As Integer = TestByRef(x5)
    'Public Shared Property y4 As Integer = TestByRef(x6)

    Public Property z3 As Integer = TestByRef(x7)
    Public Shared Property z4 As Integer = TestByRef(x8)

    'Public Const y5 As Integer = TestByRef(x9).MaxValue
    Public Const y6 As Integer = TestByRef(x10).MaxValue

End Class

Module Module1

    Sub Main()
        Dim t As New T1()

        Console.WriteLine(t.x1)
        Console.WriteLine(t.x2)
        Console.WriteLine(T1.x3)
        Console.WriteLine(T1.x4)
        Console.WriteLine(t.x5)
        Console.WriteLine(t.x6)
        Console.WriteLine(T1.x7)
        Console.WriteLine(T1.x8)
        Console.WriteLine(t.x9)
        Console.WriteLine(T1.x10)
    End Sub

    Function TestByRef(ByRef x As Integer) As Integer
        x = x + 100
        Return x
    End Function
End Module
    </file>
</compilation>
            CompileAndVerify(source,
                        additionalRefs:={TestReferences.SymbolsTests.PropertiesWithByRef},
                         expectedOutput:=<![CDATA[
100
0
0
100
100
0
0
100
0
0
]]>)


        End Sub

        <Fact>
        Public Sub ByRefInInitializer3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="ByRefInInitializer3">
    <file name="a.vb">
Imports System

Class T1
    Public ReadOnly x1 As Integer
    Public ReadOnly x2 As Integer
    Public Shared ReadOnly x3 As Integer
    Public Shared ReadOnly x4 As Integer
    Public ReadOnly x5 As Integer
    Public ReadOnly x6 As Integer
    Public Shared ReadOnly x7 As Integer
    Public Shared ReadOnly x8 As Integer
    Public ReadOnly x9 As Integer
    Public Shared ReadOnly x10 As Integer

    Public y1 As Action = Sub() TestByRef(x1)
    Public Shared y2 As Action = Sub() TestByRef(x2)

    Public z1 As Action = Sub() TestByRef(x3)
    Public Shared z2 As Action = Sub() TestByRef(x4)

    Public Property y3 As Action = Sub() TestByRef(x5)
    Public Shared Property y4 As Action = Sub() TestByRef(x6)

    Public Property z3 As Action = Sub() TestByRef(x7)
    Public Shared Property z4 As Action = Sub() TestByRef(x8)

    Public Const y5 As Object = CType(Function() TestByRef(x9), Func(Of Integer)).Invoke().MaxValue
    Public Const y6 As Object = CType(Function() TestByRef(x10), Func(Of Integer)).Invoke().MaxValue
End Class

Module Module1
    Sub Main()
    End Sub

    Function TestByRef(ByRef x As Integer) As Integer
        x = x + 100
        Return x
    End Function
End Module
    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Public y1 As Action = Sub() TestByRef(x1)
                                          ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Shared y2 As Action = Sub() TestByRef(x2)
                                                 ~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Public Shared z2 As Action = Sub() TestByRef(x4)
                                                 ~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Public Property y3 As Action = Sub() TestByRef(x5)
                                                   ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Shared Property y4 As Action = Sub() TestByRef(x6)
                                                          ~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Public Shared Property z4 As Action = Sub() TestByRef(x8)
                                                          ~~
BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.
    Public Const y5 As Object = CType(Function() TestByRef(x9), Func(Of Integer)).Invoke().MaxValue
                                                           ~~
BC36602: 'ReadOnly' variable cannot be the target of an assignment in a lambda expression inside a constructor.
    Public Const y6 As Object = CType(Function() TestByRef(x10), Func(Of Integer)).Invoke().MaxValue
                                                           ~~~
</expected>)
        End Sub


        <Fact>
        Public Sub NamedArgumentsAndOverriding()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Class Test1
    Overridable Sub Foo(x As Integer)
    End Sub
End Class

Class Test2
    Inherits Test1

    Overrides Sub Foo(y As Integer)
    End Sub
End Class

Class Test3
    Inherits Test2

    Overrides Sub Foo(z As Integer)
    End Sub
End Class

Module Module1
    Sub Main()
        Dim t3 As New Test3()

        t3.Foo(z:=1)
        t3.Foo(y:=1)
        t3.Foo(x:=1)
    End Sub
End Module

Namespace GenMethod4140
    Friend Module GenMethod4140mod
        Class Base
            Overridable Function fun1(Of T)(ByRef t1 As T) As Object
                Return Nothing
            End Function
        End Class
        Class Derived
            Inherits Base
            Overrides Function fun1(Of T)(ByRef t2 As T) As Object
                Return Nothing
            End Function
        End Class

        Sub GenMethod4140()
            Dim c3 As New Derived

            c3.fun1(t1:=3US)

        End Sub
    End Module
End Namespace

    </file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30455: Argument not specified for parameter 'z' of 'Public Overrides Sub Foo(z As Integer)'.
        t3.Foo(y:=1)
           ~~~
BC30272: 'y' is not a parameter of 'Public Overrides Sub Foo(z As Integer)'.
        t3.Foo(y:=1)
               ~
BC30455: Argument not specified for parameter 'z' of 'Public Overrides Sub Foo(z As Integer)'.
        t3.Foo(x:=1)
           ~~~
BC30272: 'x' is not a parameter of 'Public Overrides Sub Foo(z As Integer)'.
        t3.Foo(x:=1)
               ~
BC30455: Argument not specified for parameter 't2' of 'Public Overrides Function fun1(Of T)(ByRef t2 As T) As Object'.
            c3.fun1(t1:=3US)
               ~~~~
BC30272: 't1' is not a parameter of 'Public Overrides Function fun1(Of T)(ByRef t2 As T) As Object'.
            c3.fun1(t1:=3US)
                    ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub SharedThroughInstance1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New TestC()
        x.Test(1, 2)
    End Sub
End Module

Class TestC
    Public Shared Sub Test(x As Integer)
    End Sub

    Public Shared Sub Test(x As Integer, y As Integer)
        System.Console.WriteLine("Success")
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        x.Test(1, 2)
        ~~~~~~
</expected>)

            CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Success
]]>)

        End Sub

        <Fact()>
        Public Sub InaccessibleOverloads1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New TestC()
        x.Test(1)
        x.Test(2, 1)
        x.Test()

        Dim d1 As System.Action(Of Integer) = AddressOf x.Test
        Dim d2 As System.Action(Of Integer, Integer) = AddressOf x.Test
        Dim d3 As System.Action = AddressOf x.Test
    End Sub
End Module

Class TestC
    Protected Sub Test(x As Integer)
    End Sub

    Protected Sub Test(x As Integer, y As Integer)
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'TestC.Protected Sub Test(x As Integer)' is not accessible in this context because it is 'Protected'.
        x.Test(1)
        ~~~~~~
BC30390: 'TestC.Protected Sub Test(x As Integer, y As Integer)' is not accessible in this context because it is 'Protected'.
        x.Test(2, 1)
        ~~~~~~
BC30517: Overload resolution failed because no 'Test' is accessible.
        x.Test()
        ~~~~~~
BC30390: 'TestC.Protected Sub Test(x As Integer)' is not accessible in this context because it is 'Protected'.
        Dim d1 As System.Action(Of Integer) = AddressOf x.Test
                                                        ~~~~~~
BC30390: 'TestC.Protected Sub Test(x As Integer, y As Integer)' is not accessible in this context because it is 'Protected'.
        Dim d2 As System.Action(Of Integer, Integer) = AddressOf x.Test
                                                                 ~~~~~~
BC30517: Overload resolution failed because no 'Test' is accessible.
        Dim d3 As System.Action = AddressOf x.Test
                                            ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InaccessibleOverloads2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New TestC()
        x.Test(1)
        x.Test(2, 1)
        x.Test()

        Dim d1 As System.Action(Of Integer) = AddressOf x.Test
        Dim d2 As System.Action(Of Integer, Integer) = AddressOf x.Test
        Dim d3 As System.Action = AddressOf x.Test
    End Sub
End Module

Class TestC
    Protected Shared Sub Test(x As Integer)
    End Sub

    Protected Shared Sub Test(x As Integer, y As Integer)
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'TestC.Protected Shared Sub Test(x As Integer)' is not accessible in this context because it is 'Protected'.
        x.Test(1)
        ~~~~~~
BC30390: 'TestC.Protected Shared Sub Test(x As Integer, y As Integer)' is not accessible in this context because it is 'Protected'.
        x.Test(2, 1)
        ~~~~~~
BC30517: Overload resolution failed because no 'Test' is accessible.
        x.Test()
        ~~~~~~
BC30390: 'TestC.Protected Shared Sub Test(x As Integer)' is not accessible in this context because it is 'Protected'.
        Dim d1 As System.Action(Of Integer) = AddressOf x.Test
                                                        ~~~~~~
BC30390: 'TestC.Protected Shared Sub Test(x As Integer, y As Integer)' is not accessible in this context because it is 'Protected'.
        Dim d2 As System.Action(Of Integer, Integer) = AddressOf x.Test
                                                                 ~~~~~~
BC30517: Overload resolution failed because no 'Test' is accessible.
        Dim d3 As System.Action = AddressOf x.Test
                                            ~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub InaccessibleOverloads3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="NamedArgumentsAndOverriding">
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As New TestC()
        x.Test1(1)
        x.Test2(1)

        x.Test1(Of Integer)(1)
        x.Test2(Of Integer)(1)

        x.Test1(1,2)

        x.Test3(1)
        x.Test4(1)

        x.Test1(DoesntExist)
        x.Test2(DoesntExist)

        Dim y as Long = 0 
        x.Test5(y)

        Dim d1 As System.Action(Of Integer) = AddressOf x.Test1
        Dim d2 As System.Action(Of Integer) = AddressOf x.Test2

        Dim d3 As System.Action(Of Integer) = AddressOf x.Test1(Of Integer)
        Dim d4 As System.Action(Of Integer) = AddressOf x.Test2(Of Integer)

        Dim d5 As System.Action(Of Integer, Integer) = AddressOf x.Test1

        Dim d6 As System.Action(Of Integer) = AddressOf x.Test3
        Dim d7 As System.Action(Of Integer) = AddressOf x.Test4

        Dim d8 As System.Action(Of Long) = AddressOf x.Test5

        System.Console.WriteLine(1 + AddressOf x.Test1)
        System.Console.WriteLine(1 + AddressOf x.Test2)
    End Sub
End Module

Class TestC
    Protected Sub Test1(x As System.Guid)
    End Sub

    Protected Sub Test2(x As System.Guid)
    End Sub

    Protected Sub Test2(x As System.Type)
    End Sub

    Protected Sub Test3(Of T)(x As Integer)
    End Sub

    Protected Sub Test4(Of T)(x As Integer)
    End Sub

    Protected Sub Test4(Of T)(x As String)
    End Sub

    Protected Sub Test5(x As Integer)
    End Sub

    Protected Sub Test5(x As String)
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        x.Test1(1)
        ~~~~~~~
BC30311: Value of type 'Integer' cannot be converted to 'Guid'.
        x.Test1(1)
                ~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        x.Test2(1)
        ~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        x.Test1(Of Integer)(1)
        ~~~~~~~~~~~~~~~~~~~
BC32045: 'Protected Sub Test1(x As Guid)' has no type parameters and so cannot have type arguments.
        x.Test1(Of Integer)(1)
               ~~~~~~~~~~~~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        x.Test2(Of Integer)(1)
        ~~~~~~~~~~~~~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        x.Test1(1,2)
        ~~~~~~~
BC30311: Value of type 'Integer' cannot be converted to 'Guid'.
        x.Test1(1,2)
                ~
BC30057: Too many arguments to 'Protected Sub Test1(x As Guid)'.
        x.Test1(1,2)
                  ~
BC30390: 'TestC.Protected Sub Test3(Of T)(x As Integer)' is not accessible in this context because it is 'Protected'.
        x.Test3(1)
        ~~~~~~~
BC32050: Type parameter 'T' for 'Protected Sub Test3(Of T)(x As Integer)' cannot be inferred.
        x.Test3(1)
          ~~~~~
BC30517: Overload resolution failed because no 'Test4' is accessible.
        x.Test4(1)
        ~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        x.Test1(DoesntExist)
        ~~~~~~~
BC30451: 'DoesntExist' is not declared. It may be inaccessible due to its protection level.
        x.Test1(DoesntExist)
                ~~~~~~~~~~~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        x.Test2(DoesntExist)
        ~~~~~~~
BC30451: 'DoesntExist' is not declared. It may be inaccessible due to its protection level.
        x.Test2(DoesntExist)
                ~~~~~~~~~~~
BC30517: Overload resolution failed because no 'Test5' is accessible.
        x.Test5(y)
        ~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        Dim d1 As System.Action(Of Integer) = AddressOf x.Test1
                                                        ~~~~~~~
BC31143: Method 'Protected Sub Test1(x As Guid)' does not have a signature compatible with delegate 'Delegate Sub Action(Of Integer)(obj As Integer)'.
        Dim d1 As System.Action(Of Integer) = AddressOf x.Test1
                                                        ~~~~~~~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        Dim d2 As System.Action(Of Integer) = AddressOf x.Test2
                                                        ~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        Dim d3 As System.Action(Of Integer) = AddressOf x.Test1(Of Integer)
                                                        ~~~~~~~~~~~~~~~~~~~
BC32045: 'Protected Sub Test1(x As Guid)' has no type parameters and so cannot have type arguments.
        Dim d3 As System.Action(Of Integer) = AddressOf x.Test1(Of Integer)
                                                               ~~~~~~~~~~~~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        Dim d4 As System.Action(Of Integer) = AddressOf x.Test2(Of Integer)
                                                        ~~~~~~~~~~~~~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        Dim d5 As System.Action(Of Integer, Integer) = AddressOf x.Test1
                                                                 ~~~~~~~
BC31143: Method 'Protected Sub Test1(x As Guid)' does not have a signature compatible with delegate 'Delegate Sub Action(Of Integer, Integer)(arg1 As Integer, arg2 As Integer)'.
        Dim d5 As System.Action(Of Integer, Integer) = AddressOf x.Test1
                                                                 ~~~~~~~
BC30390: 'TestC.Protected Sub Test3(Of T)(x As Integer)' is not accessible in this context because it is 'Protected'.
        Dim d6 As System.Action(Of Integer) = AddressOf x.Test3
                                                        ~~~~~~~
BC36564: Type arguments could not be inferred from the delegate.
        Dim d6 As System.Action(Of Integer) = AddressOf x.Test3
                                                        ~~~~~~~
BC30517: Overload resolution failed because no 'Test4' is accessible.
        Dim d7 As System.Action(Of Integer) = AddressOf x.Test4
                                                        ~~~~~~~
BC30517: Overload resolution failed because no 'Test5' is accessible.
        Dim d8 As System.Action(Of Long) = AddressOf x.Test5
                                                     ~~~~~~~
BC30491: Expression does not produce a value.
        System.Console.WriteLine(1 + AddressOf x.Test1)
                                     ~~~~~~~~~~~~~~~~~
BC30390: 'TestC.Protected Sub Test1(x As Guid)' is not accessible in this context because it is 'Protected'.
        System.Console.WriteLine(1 + AddressOf x.Test1)
                                               ~~~~~~~
BC30491: Expression does not produce a value.
        System.Console.WriteLine(1 + AddressOf x.Test2)
                                     ~~~~~~~~~~~~~~~~~
BC30517: Overload resolution failed because no 'Test2' is accessible.
        System.Console.WriteLine(1 + AddressOf x.Test2)
                                               ~~~~~~~
</expected>)
        End Sub

        <WorkItem(543719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543719")>
        <Fact()>
        Public Sub CallByRefWithTwoArgs()
            Dim compilationDef =
<compilation name="Test.vb">
    <file name="a.vb">
Imports System

Module Program
    Sub SUB8(ByRef X1 As Integer, ByRef X2 As Integer)
        Console.WriteLine(X1)
        Console.WriteLine(X2)
    End Sub

    Sub Main(args As String())
        SUB8(10, 40)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef,
                             expectedOutput:=
            <![CDATA[
10
40
]]>)
        End Sub

        <Fact, WorkItem(544511, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544511")>
        Public Sub Bug12877_1()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
Imports Microsoft.VisualBasic.Strings

Module Module1

    Sub Main()
        System.Console.WriteLine(Test1("a"c))
        System.Console.WriteLine(Test2("b"))
    End Sub

    Function Test1(x As Char) As Integer
        Return AscW(x)
    End Function

    Function Test2(x As String) As Integer
        Return AscW(x)
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
97
98
]]>)


            compilationVerifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>)

            compilationVerifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Strings.AscW(String) As Integer"
  IL_0006:  ret
}
]]>)

            compilation = CreateCompilationWithMscorlibAndReferences(source, {SystemRef}, TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True))

            compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
97
98
]]>)


            compilationVerifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
]]>)

            compilationVerifier.VerifyIL("Module1.Test2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Microsoft.VisualBasic.Strings.AscW(String) As Integer"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(545521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545521")>
        Public Sub Bug14020()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
Option Strict On
 
Class A
    Shared Sub Foo(Of T)(x As T)
        System.Console.WriteLine("Foo(Of T)(x As T)")
    End Sub
End Class
 
Class B
    Inherits A
    Overloads Shared Sub Foo(Of T)(y As Integer)
    End Sub
    Shared Sub Main()
        Foo(x:=1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
Foo(Of T)(x As T)
]]>)
        End Sub

        <Fact, WorkItem(545522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545522")>
        Public Sub Bug14021()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
        <![CDATA[
Option Strict On

Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim s As String = Nothing
        s.Foo(y:=1)
    End Sub

    <Extension>
    Sub Foo(x As Object, y As Integer)
        System.Console.WriteLine("Foo(x As Object, y As Integer)")
    End Sub

    <Extension>
    Sub Foo(Of T)(x As T, z As Integer)
        System.Console.WriteLine("Foo(Of T)(x As T, z As Integer)")
    End Sub
End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
Foo(x As Object, y As Integer)
]]>)
        End Sub

        <Fact, WorkItem(545522, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545522")>
        Public Sub Bug14021_2()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
        <![CDATA[
Option Strict On

Imports System.Runtime.CompilerServices

Module M
    Sub Main()
        Dim s As String = Nothing
        s.Foo(y:=1)
    End Sub

    <Extension>
    Sub Foo(x As String, z As Integer)
        System.Console.WriteLine("Foo(x As String, z As Integer)")
    End Sub

    <Extension>
    Sub Foo(x As Object, y As Integer)
        System.Console.WriteLine("Foo(x As Object, y As Integer)")
    End Sub

End Module
]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
Foo(x As Object, y As Integer)
]]>)
        End Sub

        <Fact, WorkItem(545524, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545524")>
        Public Sub Bug14024()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
        <![CDATA[
Option Strict On

Class B
    Overloads Function Foo(Of T)() As Integer
        System.Console.WriteLine("Function Foo(Of T)() As Integer")
        Return 4321
    End Function
End Class

Class C
    Inherits B
    Overloads Shared Function Foo() As Integer()
        System.Console.WriteLine("Function Foo() As Integer()")
        Return {1234}
    End Function
    Shared Sub Main()
        Foo(0).ToString()
    End Sub
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
Function Foo() As Integer()
]]>)
        End Sub

        <Fact, WorkItem(545524, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545524")>
        Public Sub Bug14024_2()
            Dim source =
<compilation name="AscW">
    <file name="a.vb">
        <![CDATA[
Option Strict On

Class B
    Overloads Function Foo(Of T)() As Integer
        System.Console.WriteLine("Function Foo(Of T)() As Integer")
        Return 4321
    End Function
End Class

Class C
    Inherits B
    Shared Sub Main()
        Foo(0).ToString()
    End Sub
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30057: Too many arguments to 'Public Overloads Function Foo(Of T)() As Integer'.
        Foo(0).ToString()
            ~
</expected>)

        End Sub

        <Fact, WorkItem(546006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546006")>
        Public Sub Bug14936()
            Dim source =
<compilation name="Bug14936">
    <file name="a.vb">
        <![CDATA[
Module Program
    Sub Main()
        Dim o As New cls1
        Dim x1 As Object = 1
        o.foo(x1)
    End Sub
End Module
Class cls1
    Sub foo(ByVal x As cls1)
        System.Console.WriteLine("foo(ByVal x As cls1)")
    End Sub
    Sub foo(ByVal x As Integer)
        System.Console.WriteLine("foo(ByVal x As Integer)")
    End Sub
End Class
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
foo(ByVal x As Integer)
]]>)

            AssertTheseDiagnostics(compilation,
<expected>
BC42017: Late bound resolution; runtime errors could occur.
        o.foo(x1)
          ~~~
</expected>)
        End Sub

        <Fact, WorkItem(547132, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547132")>
        Public Sub Bug18047()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Module Program
    Property S1 As String
        Get
            Return ""
        End Get
        Set(value As String)
            Console.WriteLine("S1")
        End Set
    End Property
    Property S2 As String
        Get
            Return ""
        End Get
        Set(value As String)
            Console.WriteLine("S2")
        End Set
    End Property
    Property S3 As String
        Get
            Return ""
        End Get
        Set(value As String)
            Console.WriteLine("S3")
        End Set
    End Property
    Sub Verify1(ByRef x As String, ByRef y As String, ByRef z As String)
        x = "a"
        y = "b"
        z = "c"
    End Sub
    Sub Main(args As String())
        Verify1(S1, S2, S3)
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
S3
S2
S1
]]>)
        End Sub

        <Fact, WorkItem(531448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531448")>
        Public Sub Bug18133_1()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Compare Text

Imports System
Imports Microsoft.VisualBasic

Module module1
    Sub Main(args As String())
        Console.WriteLine(InStr(1, "SSHORTDATE", "Date"))
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
7
]]>)
        End Sub

        <Fact, WorkItem(531448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531448")>
        Public Sub Bug18133_2()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Option Compare Text

Imports System
Imports Microsoft.VisualBasic

Module module1
    Sub Main(args As String())
        Console.WriteLine(MyInStr(1, "SSHORTDATE", "Date"))
    End Sub

    Function MyInStr(Start As Integer, String1 As String, String2 As String, <CompilerServices.OptionCompareAttribute()> Optional Compare As CompareMethod = CompareMethod.Binary) As Integer
        Return InStr(Start, String1, String2, Compare)
    End Function

End Module


    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
0
]]>)
        End Sub

        <Fact, WorkItem(531413, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531413")>
        Public Sub Bug18089()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Friend Class Class1
    Sub New(ByVal x As Short, Optional ByVal y As Integer = 0)
        System.Console.WriteLine("1")
    End Sub

    Sub New(ByVal x As Integer, Optional ByVal y As Short = 0)
        System.Console.WriteLine("2")
    End Sub

    Shared Sub Foo(ByVal x As Short, Optional ByVal y As Integer = 0)
        System.Console.WriteLine("3")
    End Sub

    Shared Sub Foo(ByVal x As Integer, Optional ByVal y As Short = 0)
        System.Console.WriteLine("4")
    End Sub

End Class

Module Module1
    Sub Main(args As String())
        Dim x As New Class1(CShort(0), )
        Dim y As New Class1(CShort(0))

        Class1.Foo(CShort(0), )
        Class1.Foo(CShort(0))
    End Sub
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(source, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
1
1
3
3
]]>)
        End Sub

        <Fact, WorkItem(570936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570936")>
        Public Sub Bug570936()
            Dim source =
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Module Module1
Sub Main
    Dim x As New C3
    Dim sResult As String = x.Foo3(1)
    Console.writeline("C3 Pass")
    Console.writeline( sResult)
End Sub

Class C3
    Function Foo3(<[ParamArray]()> ByVal x As Integer) As String
        Return "C3 Fail"
    End Function
End Class
End Module
Module NonArrayMarkedAsParamArray1
    <Extension()> Function Foo3(ByVal x As C3, ByVal y As Integer) As String
        Return "C3 Pass"
    End Function
End Module
    ]]>
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim compilationVerifier = CompileAndVerify(compilation,
                         expectedOutput:=
            <![CDATA[
C3 Pass
C3 Pass
]]>)
        End Sub

        <Fact()>
        Public Sub ParameterSyntax_ModifiersAndAttributes()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
            <![CDATA[Imports System
Imports Microsoft.VisualBasic
Imports System.Collections
Imports System.Collections.Generic

Module Module1

    Sub main()
        Foo("test", "test2")
        Bar("Test")
        Bar("Test", 2)
        FooAttributes({"Test", "Test2"})
        FooAttributes2({"Test2", "Test"})
        FooAttributes3({"Test", "Test2"}, 1)
    End Sub

    Sub Foo(ByVal ParamArray x() As String)
        Console.WriteLine("Foo")
    End Sub

    Sub Bar(ByRef x As String, Optional ByVal y As Integer = 1)
        Console.WriteLine("Bar")
    End Sub

    Sub FooAttributes(<Test> <Test2> x() As String)
        Console.WriteLine("FooAttributes")
    End Sub

    Sub FooAttributes2(<Test, Test2> x() As String)
        Console.WriteLine("FooAttributes2")
    End Sub

    Sub FooAttributes3(<Test, Test2> x() As String, <Test> z As Integer)
        Console.WriteLine("FooAttributes3")
    End Sub
End Module

<AttributeUsageAttribute(AttributeTargets.Parameter, Inherited:=True)>
Public NotInheritable Class TestAttribute
    Inherits Attribute
End Class

<AttributeUsageAttribute(AttributeTargets.Parameter, Inherited:=True)>
Public NotInheritable Class Test2Attribute
    Inherits Attribute
End Class
]]>
        </file>
    </compilation>,
        expectedOutput:=<![CDATA[Foo
Bar
Bar
FooAttributes
FooAttributes2
FooAttributes3
]]>)

        End Sub

        <Fact()>
        Public Sub AutoImplementedPropertiesWithGenericTypeParameters()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic
Imports System.Collections
Imports System.Collections.Generic

Module Module1
    Sub main()
        Dim x As New Foo
        x.Items = New List(Of String) From {"A", "B", "C"}
        Console.WriteLine(x.Items.Count)

        Dim y As New FooWithInterface
        Dim i As IPropTest = y
        i.Items = New List(Of String) From {"A", "B", "C"}
    End Sub
End Module

Class Foo
    Public Property Items As New List(Of String)
End Class

Class FooWithInterface
    Implements IPropTest

    Public Property Items As New List(Of String) Implements IPropTest.Items
End Class

Interface IPropTest
    Property Items As List(Of String)
End Interface
    </file>
</compilation>, expectedOutput:=<![CDATA[3
]]>)
        End Sub

        <Fact(), WorkItem(758861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/758861")>
        Public Sub Bug758861()

            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Module Program
 
    Const c1 As String = "DefaultValue1"
    Const c2 As String = "DefaultValue2"
    Declare Ansi Sub subAnsiByValStr1 Lib "DeclDll" Alias "subAnsiByValStr1" (Optional ByVal str As String = c1) 'BIND1:"c1"
    Declare Ansi Function subAnsiByValStr2 Lib "DeclDll" Alias "subAnsiByValStr2" (Optional ByVal str As String = c2) 'BIND2:"c2"

    Sub Main(args As String())
        subAnsiByValStr1()
        subAnsiByValStr2()
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseDll)

            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldstr      "DefaultValue1"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Declare Ansi Sub Program.subAnsiByValStr1 Lib "DeclDll" Alias "subAnsiByValStr1" (String)"
  IL_000d:  ldstr      "DefaultValue2"
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Declare Ansi Function Program.subAnsiByValStr2 Lib "DeclDll" Alias "subAnsiByValStr2" (String) As Object"
  IL_001a:  pop
  IL_001b:  ret
}
]]>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim node2 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 2)

            Dim cnt = model.GetConstantValue(node1)
            Assert.Equal("DefaultValue1", CStr(cnt.Value))

            cnt = model.GetConstantValue(node2)
            Assert.Equal("DefaultValue2", CStr(cnt.Value))
        End Sub

        <Fact(), WorkItem(762717, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762717")>
        Public Sub Bug762717()

            Dim library = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Test1
    Public Shared Sub Test1(a As Integer, <System.Runtime.InteropServices.OptionalAttribute> x As Integer, <System.ParamArrayAttribute> y As Integer())
    End Sub
End Class

Public Class Test2
    Public Shared Sub Test2(a As Integer, <System.Runtime.InteropServices.OptionalAttribute> x As Integer, <System.ParamArrayAttribute> y As Integer())
    End Sub
    Public Shared Sub Test2(a As String, <System.Runtime.InteropServices.OptionalAttribute> x As Integer, <System.ParamArrayAttribute> y As Integer())
    End Sub
End Class
    ]]></file>
</compilation>, TestOptions.ReleaseDll)

            CompileAndVerify(library)

            Dim compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class Module1
    Sub Main1()
    	Test1.Test1(1)
    End Sub
    Sub Main2()
    	Test2.Test2(2)
    End Sub
End Class
    ]]></file>
</compilation>, {library.EmitToImageReference()}, TestOptions.ReleaseDll)



            Dim verifier = CompileAndVerify(compilation)

            verifier.VerifyIL("Module1.Main1",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.0
  IL_0003:  newarr     "Integer"
  IL_0008:  call       "Sub Test1.Test1(Integer, Integer, ParamArray Integer())"
  IL_000d:  ret
}
]]>)

            verifier.VerifyIL("Module1.Main2",
            <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.2
  IL_0001:  ldc.i4.0
  IL_0002:  ldc.i4.0
  IL_0003:  newarr     "Integer"
  IL_0008:  call       "Sub Test2.Test2(Integer, Integer, ParamArray Integer())"
  IL_000d:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(1040093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040093"), WorkItem(1026678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1026678")>
        Public Sub ParenthesizedVariableAsAReceiver_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks


Module Module1
    Sub Main()
        System.Console.WriteLine("Non-Async")
        System.Console.WriteLine()

        TestLocal()
        TestParameter()
        TestSharedField()
        TestInstanceField()
        TestArrayElement()

        System.Console.WriteLine()
        System.Console.WriteLine("Async")
        System.Console.WriteLine()

        Task.WaitAll(TestLocalAsync())
        Task.WaitAll(TestParameterAsync())
        Task.WaitAll(TestSharedFieldAsync())
        Task.WaitAll(TestInstanceFieldAsync())
        Task.WaitAll(TestArrayElementAsync())
    End Sub

    Sub TestLocal()
        Dim l = TestStruct1.Create()
        Call (l).Change()
        System.Console.WriteLine("Local         : {0}", l.State())
    End Sub

    Sub TestParameter(Optional p As TestStruct1 = Nothing)
        p = TestStruct1.Create()
        Call (p).Change()
        System.Console.WriteLine("Parameter     : {0}", p.State())
    End Sub

    Private f As TestStruct1
    Sub TestSharedField()
        f = TestStruct1.Create()
        Call (f).Change()
        System.Console.WriteLine("Shared Field  : {0}", f.State())
    End Sub

    Sub TestInstanceField()
        Dim i = New TestClass()
        Call (i.fld2).Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld2.State())
    End Sub

    Sub TestArrayElement()
        Dim a = {TestStruct1.Create()}
        Call (a(0)).Change()
        System.Console.WriteLine("Array element : {0}", a(0).State())
    End Sub

    Async Function DummyAsync() As Task(Of Object)
        Return Nothing
    End Function

    Async Function TestLocalAsync() As Task
        Dim l = TestStruct1.Create()
        Call (l).Change(Await DummyAsync())
        System.Console.WriteLine("Local         : {0}", l.State())
    End Function

    Async Function TestParameterAsync(Optional p As TestStruct1 = Nothing) As Task
        p = TestStruct1.Create()
        Call (p).Change(Await DummyAsync())
        System.Console.WriteLine("Parameter     : {0}", p.State())
    End Function

    Async Function TestSharedFieldAsync() As Task
        f = TestStruct1.Create()
        Call (f).Change(Await DummyAsync())
        System.Console.WriteLine("Shared Field  : {0}", f.State())
    End Function

    Async Function TestInstanceFieldAsync() As Task
        Dim i = New TestClass()
        Call (i.fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld2.State())
    End Function

    Async Function TestArrayElementAsync() As Task
        Dim a = {TestStruct1.Create()}
        Call (a(0)).Change(Await DummyAsync())
        System.Console.WriteLine("Array element : {0}", a(0).State())
    End Function

End Module

Structure TestStruct1
    Private fld1 As String

    Shared Function Create() As TestStruct1
        Return New TestStruct1() With {.fld1 = "Unchanged"}
    End Function

    Sub Change(Optional x As Object = Nothing)
        fld1 = "Changed"
    End Sub

    Function State() As String
        Return fld1
    End Function
End Structure

Class TestClass
    Public fld2 As TestStruct1 = TestStruct1.Create()
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Non-Async

Local         : Changed
Parameter     : Changed
Shared Field  : Changed
Instance Field: Changed
Array element : Changed

Async

Local         : Changed
Parameter     : Changed
Shared Field  : Changed
Instance Field: Changed
Array element : Changed
]]>)
        End Sub

        <Fact(), WorkItem(1040093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040093"), WorkItem(1026678, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1026678")>
        Public Sub ParenthesizedVariableAsAReceiver_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks


Module Module1
    Sub Main()
        System.Console.WriteLine("Non-Async")
        System.Console.WriteLine()

        TestLocal()
        TestParameter()
        TestSharedField()
        TestInstanceField()
        TestArrayElement()

        System.Console.WriteLine()
        System.Console.WriteLine("Async")
        System.Console.WriteLine()

        Task.WaitAll(TestLocalAsync())
        Task.WaitAll(TestParameterAsync())
        Task.WaitAll(TestSharedFieldAsync())
        Task.WaitAll(TestInstanceFieldAsync())
        Task.WaitAll(TestArrayElementAsync())
    End Sub

    Sub TestLocal()
        Dim l As TestStruct2
        l = TestStruct2.Create()
        Call (l).fld2.Change()
        System.Console.WriteLine("Local         : {0}", l.fld2.State())

        l = TestStruct2.Create()
        Call ((l).fld2).Change()
        System.Console.WriteLine("Local         : {0}", l.fld2.State())

        l = TestStruct2.Create()
        Call (l.fld2).Change()
        System.Console.WriteLine("Local         : {0}", l.fld2.State())
    End Sub

    Sub TestParameter(Optional p As TestStruct2 = Nothing)
        p = TestStruct2.Create()
        Call (p).fld2.Change()
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())

        p = TestStruct2.Create()
        Call ((p).fld2).Change()
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())

        p = TestStruct2.Create()
        Call (p.fld2).Change()
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())
    End Sub

    Private f As TestStruct2
    Sub TestSharedField()
        f = TestStruct2.Create()
        Call (f).fld2.Change()
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())

        f = TestStruct2.Create()
        Call ((f).fld2).Change()
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())

        f = TestStruct2.Create()
        Call (f.fld2).Change()
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())
    End Sub

    Sub TestInstanceField()
        Dim i As TestClass
        i = New TestClass()
        Call (i).fld3.fld2.Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call ((i).fld3).fld2.Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (((i).fld3).fld2).Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (i.fld3).fld2.Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call ((i.fld3).fld2).Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (i.fld3.fld2).Change()
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())
    End Sub

    Sub TestArrayElement()
        Dim a = {TestStruct2.Create()}
        Call (a(0)).fld2.Change()
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())

        a = {TestStruct2.Create()}
        Call ((a(0)).fld2).Change()
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())

        a = {TestStruct2.Create()}
        Call (a(0).fld2).Change()
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())
    End Sub

    Async Function DummyAsync() As Task(Of Object)
        Return Nothing
    End Function

    Async Function TestLocalAsync() As Task
        Dim l As TestStruct2
        l = TestStruct2.Create()
        Call (l).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Local         : {0}", l.fld2.State())

        l = TestStruct2.Create()
        Call ((l).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Local         : {0}", l.fld2.State())

        l = TestStruct2.Create()
        Call (l.fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Local         : {0}", l.fld2.State())
    End Function

    Async Function TestParameterAsync(Optional p As TestStruct2 = Nothing) As Task
        p = TestStruct2.Create()
        Call (p).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())

        p = TestStruct2.Create()
        Call ((p).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())

        p = TestStruct2.Create()
        Call (p.fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Parameter     : {0}", p.fld2.State())
    End Function

    Async Function TestSharedFieldAsync() As Task
        f = TestStruct2.Create()
        Call (f).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())

        f = TestStruct2.Create()
        Call ((f).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())

        f = TestStruct2.Create()
        Call (f.fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Shared Field  : {0}", f.fld2.State())
    End Function

    Async Function TestInstanceFieldAsync() As Task
        Dim i As TestClass
        i = New TestClass()
        Call (i).fld3.fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call ((i).fld3).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (((i).fld3).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (i.fld3).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call ((i.fld3).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())

        i = New TestClass()
        Call (i.fld3.fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Instance Field: {0}", i.fld3.fld2.State())
    End Function

    Async Function TestArrayElementAsync() As Task
        Dim a = {TestStruct2.Create()}
        Call (a(0)).fld2.Change(Await DummyAsync())
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())

        a = {TestStruct2.Create()}
        Call ((a(0)).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())

        a = {TestStruct2.Create()}
        Call (a(0).fld2).Change(Await DummyAsync())
        System.Console.WriteLine("Array element : {0}", a(0).fld2.State())
    End Function

End Module

Structure TestStruct1
    Private fld1 As String

    Shared Function Create() As TestStruct1
        Return New TestStruct1() With {.fld1 = "Unchanged"}
    End Function

    Sub Change(Optional x As Object = Nothing)
        fld1 = "Changed"
    End Sub

    Function State() As String
        Return fld1
    End Function
End Structure

Structure TestStruct2
    Public fld2 As TestStruct1

    Shared Function Create() As TestStruct2
        Return New TestStruct2() With {.fld2 = TestStruct1.Create()}
    End Function

End Structure

Class TestClass
    Public fld3 As TestStruct2 = TestStruct2.Create()
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(compilationDef, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, TestOptions.ReleaseExe, parseOptions:=TestOptions.ReleaseExe.ParseOptions)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
            <![CDATA[
Non-Async

Local         : Changed
Local         : Changed
Local         : Changed
Parameter     : Changed
Parameter     : Changed
Parameter     : Changed
Shared Field  : Changed
Shared Field  : Changed
Shared Field  : Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Array element : Changed
Array element : Changed
Array element : Changed

Async

Local         : Changed
Local         : Changed
Local         : Changed
Parameter     : Changed
Parameter     : Changed
Parameter     : Changed
Shared Field  : Changed
Shared Field  : Changed
Shared Field  : Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Instance Field: Changed
Array element : Changed
Array element : Changed
Array element : Changed
]]>)
        End Sub

        <Fact(), WorkItem(2903, "https://github.com/dotnet/roslyn/issues/2903")>
        Public Sub DelegateWithParamArray()

            Dim source1 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Public Delegate Function MessageFormatter(ByVal format As String, <[ParamArray]> args As Object()) As String
    ]]></file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlib(source1, options:=TestOptions.DebugDll)

            Dim source2 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program

    Sub Main()
        Log(Function(f) f("Test {0}", 1))
        Log(Function(f) f("Test"))
        Log(Function(f) "test")
    End Sub

    Sub Log(messageFunc As Func(Of MessageFormatter, String))
        Console.WriteLine(messageFunc(New MessageFormatter(Function(format, args) String.Format(format, args))))
    End Sub

End Module
    ]]></file>
</compilation>

            Dim expectedOutput = <![CDATA[
Test 1
Test
test
]]>

            Dim compilation2 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {compilation1.EmitToImageReference()}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation2, expectedOutput:=expectedOutput)

            Dim compilation3 = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source2, {New VisualBasicCompilationReference(compilation1)}, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation3, expectedOutput:=expectedOutput)

            Dim source4 =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Delegate Function MessageFormatter(ByVal format As String, <[ParamArray]> args As Object()) As String

Module Program

    Sub Main()
        Log(Function(f) f("Test {0}", 1))
        Log(Function(f) f("Test"))
        Log(Function(f) "test")
    End Sub

    Sub Log(messageFunc As Func(Of MessageFormatter, String))
        Console.WriteLine(messageFunc(New MessageFormatter(Function(format, args) String.Format(format, args))))
    End Sub

End Module
    ]]></file>
</compilation>

            Dim compilation4 = CreateCompilationWithMscorlibAndVBRuntime(source4, options:=TestOptions.DebugExe)

            CompileAndVerify(compilation4, expectedOutput:=expectedOutput)

        End Sub

    End Class

End Namespace

