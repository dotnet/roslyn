' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenNullable
        Inherits BasicTestBase

        <Fact(), WorkItem(544947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544947")>
        Public Sub LiftedIntrinsicNegationLocal()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As New Integer?(1)
        Dim y = -x
        Console.Write("y1={0} ", y)
        x = Nothing
        y = -x
        Console.Write("y2={0} ", y)
        y = -New Long?()
        Console.Write("y3={0} ", y)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="y1=-1 y2= y3=").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      127 (0x7f)
  .maxstack  2
  .locals init (Integer? V_0, //x
  Integer? V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0022
  IL_0014:  ldc.i4.0
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_001c:  sub.ovf
  IL_001d:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0022:  stloc.1
  IL_0023:  ldstr      "y1={0} "
  IL_0028:  ldloc.1
  IL_0029:  box        "Integer?"
  IL_002e:  call       "Sub System.Console.Write(String, Object)"
  IL_0033:  ldloca.s   V_0
  IL_0035:  initobj    "Integer?"
  IL_003b:  ldloca.s   V_0
  IL_003d:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0042:  brtrue.s   IL_0047
  IL_0044:  ldloc.0
  IL_0045:  br.s       IL_0055
  IL_0047:  ldc.i4.0
  IL_0048:  ldloca.s   V_0
  IL_004a:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_004f:  sub.ovf
  IL_0050:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0055:  stloc.1
  IL_0056:  ldstr      "y2={0} "
  IL_005b:  ldloc.1
  IL_005c:  box        "Integer?"
  IL_0061:  call       "Sub System.Console.Write(String, Object)"
  IL_0066:  ldloca.s   V_1
  IL_0068:  initobj    "Integer?"
  IL_006e:  ldstr      "y3={0} "
  IL_0073:  ldloc.1
  IL_0074:  box        "Integer?"
  IL_0079:  call       "Sub System.Console.Write(String, Object)"
  IL_007e:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIntrinsicNegationField()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Dim x As New Integer?(1)

    Sub Main(args As String())
        Dim y = -x
        Console.WriteLine(y)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="-1").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (Integer? V_0)
  IL_0000:  ldsfld     "MyClass1.x As Integer?"
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000d:  brtrue.s   IL_0012
  IL_000f:  ldloc.0
  IL_0010:  br.s       IL_0020
  IL_0012:  ldc.i4.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_001a:  sub.ovf
  IL_001b:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0020:  box        "Integer?"
  IL_0025:  call       "Sub System.Console.WriteLine(Object)"
  IL_002a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIntrinsicNegationNull()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim y = ---CType(Nothing, Int32?)
        Console.WriteLine(y.HasValue)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="False").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer? V_0) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Integer?"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_0014:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIntrinsicNegationNotNull()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim y = ---CType(42, Int32?)
        Console.WriteLine(y.HasValue)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="True").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  5
  .locals init (Integer? V_0) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.s   42
  IL_0007:  sub.ovf
  IL_0008:  sub.ovf
  IL_0009:  sub.ovf
  IL_000a:  call       "Sub Integer?..ctor(Integer)"
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0016:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001b:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIsTrueLiteral()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        If Not Not Not (CType(False, Boolean?)) Then
            Console.Write("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "hi"
  IL_0005:  call       "Sub System.Console.Write(String)"
  IL_000a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIsTrueLocal()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x as boolean? = false
        If Not Not Not x Then
            Console.Write("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      112 (0x70)
  .maxstack  2
  .locals init (Boolean? V_0, //x
  Boolean? V_1,
  Boolean? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_0014
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_0023
  IL_0014:  ldloca.s   V_0
  IL_0016:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001b:  ldc.i4.0
  IL_001c:  ceq
  IL_001e:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0023:  stloc.2
  IL_0024:  ldloca.s   V_2
  IL_0026:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_002b:  brtrue.s   IL_0030
  IL_002d:  ldloc.2
  IL_002e:  br.s       IL_003f
  IL_0030:  ldloca.s   V_2
  IL_0032:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0037:  ldc.i4.0
  IL_0038:  ceq
  IL_003a:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_003f:  stloc.1
  IL_0040:  ldloca.s   V_1
  IL_0042:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0047:  brtrue.s   IL_004c
  IL_0049:  ldloc.1
  IL_004a:  br.s       IL_005b
  IL_004c:  ldloca.s   V_1
  IL_004e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0053:  ldc.i4.0
  IL_0054:  ceq
  IL_0056:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_005b:  stloc.1
  IL_005c:  ldloca.s   V_1
  IL_005e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0063:  brfalse.s  IL_006f
  IL_0065:  ldstr      "hi"
  IL_006a:  call       "Sub System.Console.Write(String)"
  IL_006f:  ret
}
                ]]>)

        End Sub

        <Fact()>
        Public Sub LiftedBinaryPlus()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer? = 2
        Dim y As Integer? = 4
        If x + x = y Then
            Console.Write("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      137 (0x89)
  .maxstack  2
  .locals init (Integer? V_0, //x
  Integer? V_1, //y
  Integer? V_2,
  Integer? V_3,
  Boolean? V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.4
  IL_000b:  call       "Sub Integer?..ctor(Integer)"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_001e:  and
  IL_001f:  brtrue.s   IL_002c
  IL_0021:  ldloca.s   V_3
  IL_0023:  initobj    "Integer?"
  IL_0029:  ldloc.3
  IL_002a:  br.s       IL_0040
  IL_002c:  ldloca.s   V_0
  IL_002e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0033:  ldloca.s   V_0
  IL_0035:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_003a:  add.ovf
  IL_003b:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0040:  stloc.2
  IL_0041:  ldloca.s   V_2
  IL_0043:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0048:  ldloca.s   V_1
  IL_004a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_004f:  and
  IL_0050:  brtrue.s   IL_005e
  IL_0052:  ldloca.s   V_4
  IL_0054:  initobj    "Boolean?"
  IL_005a:  ldloc.s    V_4
  IL_005c:  br.s       IL_0073
  IL_005e:  ldloca.s   V_2
  IL_0060:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0065:  ldloca.s   V_1
  IL_0067:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_006c:  ceq
  IL_006e:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0073:  stloc.s    V_4
  IL_0075:  ldloca.s   V_4
  IL_0077:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_007c:  brfalse.s  IL_0088
  IL_007e:  ldstr      "hi"
  IL_0083:  call       "Sub System.Console.Write(String)"
  IL_0088:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryPlus1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer? = 0

        Console.WriteLine(x + goo(x))
    End Sub

    Function goo(ByRef v As Integer?) As Integer
        v = 0

        Return 42
    End Function
End Module

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (Integer? V_0, //x
  Integer? V_1,
  Integer V_2,
  Integer? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Function MyClass1.goo(ByRef Integer?) As Integer"
  IL_0011:  stloc.2
  IL_0012:  ldloca.s   V_1
  IL_0014:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0019:  brtrue.s   IL_0026
  IL_001b:  ldloca.s   V_3
  IL_001d:  initobj    "Integer?"
  IL_0023:  ldloc.3
  IL_0024:  br.s       IL_0034
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002d:  ldloc.2
  IL_002e:  add.ovf
  IL_002f:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0034:  box        "Integer?"
  IL_0039:  call       "Sub System.Console.WriteLine(Object)"
  IL_003e:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryPlusHasValue1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer? = 2
        If x + x = 4 Then
            Console.Write("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      113 (0x71)
  .maxstack  2
  .locals init (Integer? V_0, //x
  Integer? V_1,
  Integer? V_2,
  Boolean? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0016:  and
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_2
  IL_001b:  initobj    "Integer?"
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0038
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0032:  add.ovf
  IL_0033:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0040:  brtrue.s   IL_004d
  IL_0042:  ldloca.s   V_3
  IL_0044:  initobj    "Boolean?"
  IL_004a:  ldloc.3
  IL_004b:  br.s       IL_005c
  IL_004d:  ldloca.s   V_1
  IL_004f:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0054:  ldc.i4.4
  IL_0055:  ceq
  IL_0057:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_005c:  stloc.3
  IL_005d:  ldloca.s   V_3
  IL_005f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0064:  brfalse.s  IL_0070
  IL_0066:  ldstr      "hi"
  IL_006b:  call       "Sub System.Console.Write(String)"
  IL_0070:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryPlusHasValue2()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer? = 2
        If 4 = x + x Then
            Console.Write("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      113 (0x71)
  .maxstack  2
  .locals init (Integer? V_0, //x
  Integer? V_1,
  Integer? V_2,
  Boolean? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.2
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0016:  and
  IL_0017:  brtrue.s   IL_0024
  IL_0019:  ldloca.s   V_2
  IL_001b:  initobj    "Integer?"
  IL_0021:  ldloc.2
  IL_0022:  br.s       IL_0038
  IL_0024:  ldloca.s   V_0
  IL_0026:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0032:  add.ovf
  IL_0033:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0038:  stloc.1
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0040:  brtrue.s   IL_004d
  IL_0042:  ldloca.s   V_3
  IL_0044:  initobj    "Boolean?"
  IL_004a:  ldloc.3
  IL_004b:  br.s       IL_005c
  IL_004d:  ldc.i4.4
  IL_004e:  ldloca.s   V_1
  IL_0050:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0055:  ceq
  IL_0057:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_005c:  stloc.3
  IL_005d:  ldloca.s   V_3
  IL_005f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0064:  brfalse.s  IL_0070
  IL_0066:  ldstr      "hi"
  IL_006b:  call       "Sub System.Console.Write(String)"
  IL_0070:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanXor()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = True
        If (x Xor Nothing) Then
        Else
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0006:  pop
  IL_0007:  ldstr      "hi"
  IL_000c:  call       "Sub System.Console.WriteLine(String)"
  IL_0011:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanXor1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = True
        Dim y As Boolean? = False
        If (x Xor y) Then
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (Boolean? V_0, //x
  Boolean? V_1, //y
  Boolean? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.0
  IL_000b:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0017:  ldloca.s   V_1
  IL_0019:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_001e:  and
  IL_001f:  brtrue.s   IL_002c
  IL_0021:  ldloca.s   V_2
  IL_0023:  initobj    "Boolean?"
  IL_0029:  ldloc.2
  IL_002a:  br.s       IL_0040
  IL_002c:  ldloca.s   V_0
  IL_002e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_003a:  xor
  IL_003b:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0040:  stloc.2
  IL_0041:  ldloca.s   V_2
  IL_0043:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0048:  brfalse.s  IL_0054
  IL_004a:  ldstr      "hi"
  IL_004f:  call       "Sub System.Console.WriteLine(String)"
  IL_0054:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanOrNothing()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = True
        If (x or Nothing) Then
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (Boolean? V_0, //x
  Boolean? V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    "Boolean?"
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_001d
  IL_001c:  ldloc.0
  IL_001d:  stloc.1
  IL_001e:  ldloca.s   V_1
  IL_0020:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0025:  brfalse.s  IL_0031
  IL_0027:  ldstr      "hi"
  IL_002c:  call       "Sub System.Console.WriteLine(String)"
  IL_0031:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanAndNothing()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = True
        If (Nothing And x) Then
        Else
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  3
  .locals init (Boolean? V_0, //x
  Boolean? V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0016:  ldc.i4.0
  IL_0017:  ceq
  IL_0019:  and
  IL_001a:  brtrue.s   IL_0027
  IL_001c:  ldloca.s   V_1
  IL_001e:  initobj    "Boolean?"
  IL_0024:  ldloc.1
  IL_0025:  br.s       IL_0028
  IL_0027:  ldloc.0
  IL_0028:  stloc.1
  IL_0029:  ldloca.s   V_1
  IL_002b:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0030:  brtrue.s   IL_003c
  IL_0032:  ldstr      "hi"
  IL_0037:  call       "Sub System.Console.WriteLine(String)"
  IL_003c:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanAnd()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        If (T() And T()) Then
            Console.Write("hi")
        End If
    End Sub

    Function T() As Boolean?
        Return True
    End Function
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       97 (0x61)
  .maxstack  1
  .locals init (Boolean? V_0,
  Boolean? V_1,
  Boolean? V_2)
  IL_0000:  call       "Function MyClass1.T() As Boolean?"
  IL_0005:  stloc.0
  IL_0006:  call       "Function MyClass1.T() As Boolean?"
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0013:  brfalse.s  IL_001e
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brfalse.s  IL_0046
  IL_001e:  ldloca.s   V_1
  IL_0020:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0025:  brtrue.s   IL_0032
  IL_0027:  ldloca.s   V_2
  IL_0029:  initobj    "Boolean?"
  IL_002f:  ldloc.2
  IL_0030:  br.s       IL_004c
  IL_0032:  ldloca.s   V_1
  IL_0034:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0039:  brtrue.s   IL_0043
  IL_003b:  ldc.i4.0
  IL_003c:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0041:  br.s       IL_004c
  IL_0043:  ldloc.0
  IL_0044:  br.s       IL_004c
  IL_0046:  ldc.i4.0
  IL_0047:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_004c:  stloc.1
  IL_004d:  ldloca.s   V_1
  IL_004f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0054:  brfalse.s  IL_0060
  IL_0056:  ldstr      "hi"
  IL_005b:  call       "Sub System.Console.Write(String)"
  IL_0060:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryBooleanOr()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = True
        Dim y As Boolean? = True

        If (x Or y) Then
            Console.Write("hi")
        End If

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      101 (0x65)
  .maxstack  2
  .locals init (Boolean? V_0, //x
  Boolean? V_1, //y
  Boolean? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0017:  brfalse.s  IL_0022
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0020:  brtrue.s   IL_004a
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0029:  brtrue.s   IL_0036
  IL_002b:  ldloca.s   V_2
  IL_002d:  initobj    "Boolean?"
  IL_0033:  ldloc.2
  IL_0034:  br.s       IL_0050
  IL_0036:  ldloca.s   V_1
  IL_0038:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_003d:  brtrue.s   IL_0042
  IL_003f:  ldloc.0
  IL_0040:  br.s       IL_0050
  IL_0042:  ldc.i4.1
  IL_0043:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0048:  br.s       IL_0050
  IL_004a:  ldc.i4.1
  IL_004b:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0050:  stloc.2
  IL_0051:  ldloca.s   V_2
  IL_0053:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0058:  brfalse.s  IL_0064
  IL_005a:  ldstr      "hi"
  IL_005f:  call       "Sub System.Console.Write(String)"
  IL_0064:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub BinaryBool()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Option Strict On

Imports System

Module MyClass1
    Sub Main(args As String())
        Print("== And ==")
        Print(T() And T())
        Print(T() And F())
        Print(T() And N())

        Print(F() And T())
        Print(F() And F())
        Print(F() And N())

        Print(N() And T())
        Print(N() And F())
        Print(N() And N())

        Print("== Or ==")
        Print(T() Or T())
        Print(T() Or F())
        Print(T() Or N())

        Print(F() Or T())
        Print(F() Or F())
        Print(F() Or N())

        Print(N() Or T())
        Print(N() Or F())
        Print(N() Or N())

        Print("== AndAlso ==")
        Print(T() AndAlso T())
        Print(T() AndAlso F())
        Print(T() AndAlso N())

        Print(F() AndAlso T())
        Print(F() AndAlso F())
        Print(F() AndAlso N())

        Print(N() AndAlso T())
        Print(N() AndAlso F())
        Print(N() AndAlso N())

        Print("== OrElse ==")
        Print(T() OrElse T())
        Print(T() OrElse F())
        Print(T() OrElse N())

        Print(F() OrElse T())
        Print(F() OrElse F())
        Print(F() OrElse N())

        Print(N() OrElse T())
        Print(N() OrElse F())
        Print(N() OrElse N())
    End Sub

    Private Sub Print(s As String)
        Console.WriteLine(s)
    End Sub

    Private Sub Print(r As Boolean?)
        Console.Write(": HasValue = ")
        Console.Write(r.HasValue)
        Console.Write(": Value =")
        If r.HasValue Then
            Console.Write(" ")
            Console.Write(r)
        End If
        Console.WriteLine()
    End Sub

    Private Function T() As Boolean?
        Console.Write("T")
        Return True
    End Function

    Private Function F() As Boolean?
        Console.Write("F")
        Return False
    End Function

    Private Function N() As Boolean?
        Console.Write("N")
        Return Nothing
    End Function

End Module

                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[
== And ==
TT: HasValue = True: Value = True
TF: HasValue = True: Value = False
TN: HasValue = False: Value =
FT: HasValue = True: Value = False
FF: HasValue = True: Value = False
FN: HasValue = True: Value = False
NT: HasValue = False: Value =
NF: HasValue = True: Value = False
NN: HasValue = False: Value =
== Or ==
TT: HasValue = True: Value = True
TF: HasValue = True: Value = True
TN: HasValue = True: Value = True
FT: HasValue = True: Value = True
FF: HasValue = True: Value = False
FN: HasValue = False: Value =
NT: HasValue = True: Value = True
NF: HasValue = False: Value =
NN: HasValue = False: Value =
== AndAlso ==
TT: HasValue = True: Value = True
TF: HasValue = True: Value = False
TN: HasValue = False: Value =
F: HasValue = True: Value = False
F: HasValue = True: Value = False
F: HasValue = True: Value = False
NT: HasValue = False: Value =
NF: HasValue = True: Value = False
NN: HasValue = False: Value =
== OrElse ==
T: HasValue = True: Value = True
T: HasValue = True: Value = True
T: HasValue = True: Value = True
FT: HasValue = True: Value = True
FF: HasValue = True: Value = False
FN: HasValue = False: Value =
NT: HasValue = True: Value = True
NF: HasValue = False: Value =
NN: HasValue = False: Value =
]]>
            )
        End Sub

        <Fact()>
        Public Sub BinaryBoolConstLeft()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Option Strict On

Imports System

Module MyClass1
    Sub Main(args As String())
        Print("== And ==")
        Print(True And T())
        Print(True And F())
        Print(True And N())

        Print(False And T())
        Print(False And F())
        Print(False And N())

        Print(Nothing And T())
        Print(Nothing And F())
        Print(Nothing And N())

        Print("== Or ==")
        Print(True Or T())
        Print(True Or F())
        Print(True Or N())

        Print(False Or T())
        Print(False Or F())
        Print(False Or N())

        Print(Nothing Or T())
        Print(Nothing Or F())
        Print(Nothing Or N())

        Print("== AndAlso ==")
        Print(True AndAlso T())
        Print(True AndAlso F())
        Print(True AndAlso N())

        Print(False AndAlso T())
        Print(False AndAlso F())
        Print(False AndAlso N())

        Print(Nothing AndAlso T())
        Print(Nothing AndAlso F())
        Print(Nothing AndAlso N())

        Print("== OrElse ==")
        Print(True OrElse T())
        Print(True OrElse F())
        Print(True OrElse N())

        Print(False OrElse T())
        Print(False OrElse F())
        Print(False OrElse N())

        Print(Nothing OrElse T())
        Print(Nothing OrElse F())
        Print(Nothing OrElse N())
    End Sub

    Private Sub Print(s As String)
        Console.WriteLine(s)
    End Sub

    Private Sub Print(r As Boolean?)
        Console.Write(": HasValue = ")
        Console.Write(r.HasValue)
        Console.Write(": Value =")
        If r.HasValue Then
            Console.Write(" ")
            Console.Write(r)
        End If
        Console.WriteLine()
    End Sub

    Private Function T() As Boolean?
        Console.Write("T")
        Return True
    End Function

    Private Function F() As Boolean?
        Console.Write("F")
        Return False
    End Function

    Private Function N() As Boolean?
        Console.Write("N")
        Return Nothing
    End Function

End Module


                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[
== And ==
T: HasValue = True: Value = True
F: HasValue = True: Value = False
N: HasValue = False: Value =
T: HasValue = True: Value = False
F: HasValue = True: Value = False
N: HasValue = True: Value = False
T: HasValue = False: Value =
F: HasValue = True: Value = False
N: HasValue = False: Value =
== Or ==
T: HasValue = True: Value = True
F: HasValue = True: Value = True
N: HasValue = True: Value = True
T: HasValue = True: Value = True
F: HasValue = True: Value = False
N: HasValue = False: Value =
T: HasValue = True: Value = True
F: HasValue = False: Value =
N: HasValue = False: Value =
== AndAlso ==
T: HasValue = True: Value = True
F: HasValue = True: Value = False
N: HasValue = False: Value =
: HasValue = True: Value = False
: HasValue = True: Value = False
: HasValue = True: Value = False
T: HasValue = False: Value =
F: HasValue = True: Value = False
N: HasValue = False: Value =
== OrElse ==
: HasValue = True: Value = True
: HasValue = True: Value = True
: HasValue = True: Value = True
T: HasValue = True: Value = True
F: HasValue = True: Value = False
N: HasValue = False: Value =
T: HasValue = True: Value = True
F: HasValue = False: Value =
N: HasValue = False: Value =
]]>
            )
        End Sub

        <Fact()>
        Public Sub BinaryBoolConstRight()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Option Strict On

Imports System

Module MyClass1
    Sub Main(args As String())
        Print("== And ==")
        Print(T() And True)
        Print(T() And False)
        Print(T() And Nothing)

        Print(F() And True)
        Print(F() And False)
        Print(F() And Nothing)

        Print(N() And True)
        Print(N() And False)
        Print(N() And Nothing)

        Print("== Or ==")
        Print(T() Or True)
        Print(T() Or False)
        Print(T() Or Nothing)

        Print(F() Or True)
        Print(F() Or False)
        Print(F() Or Nothing)

        Print(N() Or True)
        Print(N() Or False)
        Print(N() Or Nothing)

        Print("== AndAlso ==")
        Print(T() AndAlso True)
        Print(T() AndAlso False)
        Print(T() AndAlso Nothing)

        Print(F() AndAlso True)
        Print(F() AndAlso False)
        Print(F() AndAlso Nothing)

        Print(N() AndAlso True)
        Print(N() AndAlso False)
        Print(N() AndAlso Nothing)

        Print("== OrElse ==")
        Print(T() OrElse True)
        Print(T() OrElse False)
        Print(T() OrElse Nothing)

        Print(F() OrElse True)
        Print(F() OrElse False)
        Print(F() OrElse Nothing)

        Print(N() OrElse True)
        Print(N() OrElse False)
        Print(N() OrElse Nothing)
    End Sub

    Private Sub Print(s As String)
        Console.WriteLine(s)
    End Sub

    Private Sub Print(r As Boolean?)
        Console.Write(": HasValue = ")
        Console.Write(r.HasValue)
        Console.Write(": Value =")
        If r.HasValue Then
            Console.Write(" ")
            Console.Write(r)
        End If
        Console.WriteLine()
    End Sub

    Private Function T() As Boolean?
        Console.Write("T")
        Return True
    End Function

    Private Function F() As Boolean?
        Console.Write("F")
        Return False
    End Function

    Private Function N() As Boolean?
        Console.Write("N")
        Return Nothing
    End Function

End Module

                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[
== And ==
T: HasValue = True: Value = True
T: HasValue = True: Value = False
T: HasValue = False: Value =
F: HasValue = True: Value = False
F: HasValue = True: Value = False
F: HasValue = True: Value = False
N: HasValue = False: Value =
N: HasValue = True: Value = False
N: HasValue = False: Value =
== Or ==
T: HasValue = True: Value = True
T: HasValue = True: Value = True
T: HasValue = True: Value = True
F: HasValue = True: Value = True
F: HasValue = True: Value = False
F: HasValue = False: Value =
N: HasValue = True: Value = True
N: HasValue = False: Value =
N: HasValue = False: Value =
== AndAlso ==
T: HasValue = True: Value = True
T: HasValue = True: Value = False
T: HasValue = False: Value =
F: HasValue = True: Value = False
F: HasValue = True: Value = False
F: HasValue = True: Value = False
N: HasValue = False: Value =
N: HasValue = True: Value = False
N: HasValue = False: Value =
== OrElse ==
T: HasValue = True: Value = True
T: HasValue = True: Value = True
T: HasValue = True: Value = True
F: HasValue = True: Value = True
F: HasValue = True: Value = False
F: HasValue = False: Value =
N: HasValue = True: Value = True
N: HasValue = False: Value =
N: HasValue = False: Value =
]]>
            )
        End Sub

        <Fact()>
        Public Sub NewBooleanInLogicalExpression()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module M
    Sub Main()

        Dim bRet As Boolean?
        bRet = New Boolean?(True) And Nothing
        Console.WriteLine("Ret1={0}", bRet)
        Select Case bret
            Case Nothing
                Console.WriteLine("Nothing")
            Case Else
                Console.Write("Else: ")
                If (Nothing Or New Boolean?(False)) Is Nothing Then
                    Console.WriteLine("Ret2={0}", New Boolean?(True) OrElse New Boolean?() AndAlso New Boolean?(False))
                End If
        End Select
    End Sub

End Module
                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[
Ret1=
Else: Ret2=True
]]>
            ).VerifyDiagnostics(Diagnostic(ERRID.WRN_EqualToLiteralNothing, "Nothing"))

        End Sub

        <Fact(), WorkItem(544948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544948")>
        Public Sub NothingOrZeroInBinaryExpression()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Enum E
    Zero
    One
End Enum
Module M
    Friend bN As Boolean? = Nothing
    Public x As ULong? = 11
    Sub Main()

        Dim nZ As Integer? = 0
        Dim r = nZ + 0 - nZ * 0
        Console.Write("r1={0}", r)

        Dim eN As E? = Nothing
        Dim y As UShort? = Nothing
        r = eN - bN * nZ + Nothing ^ y Mod x
        Console.Write(" r2={0}", r)

    End Sub

End Module
                    </file>
                </compilation>, expectedOutput:="r1=0 r2=")

        End Sub

        <Fact()>
        Public Sub NullableIs()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Boolean? = Nothing
        If (x Is Nothing) Then
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (Boolean? V_0) //x
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Boolean?"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_001b
  IL_0011:  ldstr      "hi"
  IL_0016:  call       "Sub System.Console.WriteLine(String)"
  IL_001b:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub NullableIsNot()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        If Nothing IsNot CType(3, Int32?) Then
            Console.WriteLine("hi")
        End If
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="hi").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "hi"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub NullableIsAndIsNot()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Sub Main()
          While New Boolean?(False) Is Nothing OrElse Nothing IsNot New Boolean?(True)
            If Nothing Is New Ulong?() Then
                Console.Write("True")
                Exit While
            End If
        End While
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="True").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      "True"
  IL_0005:  call       "Sub System.Console.Write(String)"
  IL_000a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedIntrinsicConversion()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer = 123
        Dim y As Long? = x
        Console.WriteLine(y)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="123").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.s   123
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  conv.i8
  IL_0005:  newobj     "Sub Long?..ctor(Long)"
  IL_000a:  box        "Long?"
  IL_000f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0014:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedInterfaceConversion()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As IComparable(Of Integer) = 123
        Dim y As Integer? = x
        Console.WriteLine(y)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="123").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  box        "Integer"
  IL_0007:  castclass  "System.IComparable(Of Integer)"
  IL_000c:  unbox.any  "Integer?"
  IL_0011:  box        "Integer?"
  IL_0016:  call       "Sub System.Console.WriteLine(Object)"
  IL_001b:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedReferenceConversionNarrowingFail()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Short? = 42
        Dim z As ValueType = x
        Try
            Dim y As UInteger? = z
            Console.WriteLine(y)

        Catch ex As InvalidCastException
            Console.WriteLine("pass")
        End Try
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="pass").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (System.ValueType V_0, //z
                System.InvalidCastException V_1) //ex
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     "Sub Short?..ctor(Short)"
  IL_0007:  box        "Short?"
  IL_000c:  stloc.0
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  unbox.any  "UInteger?"
    IL_0013:  box        "UInteger?"
    IL_0018:  call       "Sub System.Console.WriteLine(Object)"
    IL_001d:  leave.s    IL_0037
  }
  catch System.InvalidCastException
  {
    IL_001f:  dup
    IL_0020:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0025:  stloc.1
    IL_0026:  ldstr      "pass"
    IL_002b:  call       "Sub System.Console.WriteLine(String)"
    IL_0030:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0035:  leave.s    IL_0037
  }
  IL_0037:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedInterfaceConversion1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim x As Integer? = 123
        Dim y As IComparable(Of Integer) = x
        Console.WriteLine(y)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="123").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0007:  box        "Integer?"
  IL_000c:  call       "Sub System.Console.WriteLine(Object)"
  IL_0011:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedInterfaceConversionGeneric()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
                        
Imports System

Module MyClass1
    Sub Main(args As String())
        goo(42)
    End Sub

    Sub goo(Of T As {Structure, IComparable(Of T)})(x As T?)
        Dim y As IComparable(Of T) = x
        Console.Write(y.CompareTo(x.Value))
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="0").
                            VerifyIL("MyClass1.goo",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T?"
  IL_0006:  ldarga.s   V_0
  IL_0008:  call       "Function T?.get_Value() As T"
  IL_000d:  callvirt   "Function System.IComparable(Of T).CompareTo(T) As Integer"
  IL_0012:  call       "Sub System.Console.Write(Integer)"
  IL_0017:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedInterfaceConversionGeneric1()
            Dim compilationDef =
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Sub Main(args As String())
        goo(42)
    End Sub

    Sub goo(Of T As {Structure, IComparable(Of T)})(x As T?)
        Dim y As IComparable(Of Integer) = x
        Console.Write(y.CompareTo(43))
    End Sub
End Module

                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'T?' to 'IComparable(Of Integer)'.
        Dim y As IComparable(Of Integer) = x
                                           ~
</expected>)

            CompileAndVerify(compilation,
                             expectedOutput:="-1").
            VerifyIL("MyClass1.goo",
            <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        "T?"
  IL_0006:  castclass  "System.IComparable(Of Integer)"
  IL_000b:  ldc.i4.s   43
  IL_000d:  callvirt   "Function System.IComparable(Of Integer).CompareTo(Integer) As Integer"
  IL_0012:  call       "Sub System.Console.Write(Integer)"
  IL_0017:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedCompoundOp()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Dim y As Integer? = 42
        y += 1

        Console.WriteLine(y)

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="43").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (Integer? V_0,
  Integer? V_1)
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0007:  stloc.0
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_1
  IL_0013:  initobj    "Integer?"
  IL_0019:  ldloc.1
  IL_001a:  br.s       IL_002a
  IL_001c:  ldloca.s   V_0
  IL_001e:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0023:  ldc.i4.1
  IL_0024:  add.ovf
  IL_0025:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002a:  box        "Integer?"
  IL_002f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedCompoundOp1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Dim y As Integer? = 42
        y += Nothing

        Console.WriteLine(y.HasValue)

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="False").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (Integer? V_0) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  call       "Sub Integer?..ctor(Integer)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  initobj    "Integer?"
  IL_0011:  ldloca.s   V_0
  IL_0013:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0018:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001d:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryIf()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Dim y As Integer? = 123

        Console.Write(If(If(New Long?(), New Short?(42)), y))
        Console.Write(If(If(New Short?(42), New Long?()), y))

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="4242").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  IL_0000:  ldc.i4.s   123
  IL_0002:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0007:  pop
  IL_0008:  ldc.i4.s   42
  IL_000a:  conv.i8
  IL_000b:  box        "Long"
  IL_0010:  call       "Sub System.Console.Write(Object)"
  IL_0015:  ldc.i4.s   42
  IL_0017:  conv.i8
  IL_0018:  box        "Long"
  IL_001d:  call       "Sub System.Console.Write(Object)"
  IL_0022:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryIf1()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Dim x As Long? = Nothing
        Dim y As Integer? = 42

        Console.WriteLine(If(y, x))

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (Long? V_0, //x
  Integer? V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Long?"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.s   42
  IL_000c:  call       "Sub Integer?..ctor(Integer)"
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0018:  brtrue.s   IL_001d
  IL_001a:  ldloc.0
  IL_001b:  br.s       IL_002a
  IL_001d:  ldloca.s   V_1
  IL_001f:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0024:  conv.i8
  IL_0025:  newobj     "Sub Long?..ctor(Long)"
  IL_002a:  box        "Long?"
  IL_002f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(544930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544930")>
        Public Sub LiftedBinaryIf1a()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Console.Write(If(y, x))
        Console.Write(If(x, y))
    End Sub

    Function x() As Long?
        Console.Write("x")
        Return Nothing
    End Function

    Function y() As Integer?
        Console.Write("y")
        Return 42
    End Function

End Module

                    </file>
                </compilation>, expectedOutput:="y42xy42")
        End Sub

        <Fact()>
        Public Sub LiftedBinaryIf2()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System      

Module MyClass1

    Sub Main()
        Dim x As Short? = Nothing
        Dim y As Ushort? = 42

        Console.WriteLine(If(y, x))

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Short? V_0, //x
                UShort? V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Short?"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.s   42
  IL_000c:  call       "Sub UShort?..ctor(UShort)"
  IL_0011:  ldloca.s   V_1
  IL_0013:  call       "Function UShort?.get_HasValue() As Boolean"
  IL_0018:  brtrue.s   IL_0022
  IL_001a:  ldloc.0
  IL_001b:  box        "Short?"
  IL_0020:  br.s       IL_002e
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       "Function UShort?.GetValueOrDefault() As UShort"
  IL_0029:  box        "UShort"
  IL_002e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0033:  call       "Sub System.Console.WriteLine(Object)"
  IL_0038:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryIf3()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1

    Sub Main()
        Dim x As Short? = Nothing
        Dim y As Long = 42S

        Dim z = If(x, y)

        Console.WriteLine(z)

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (Short? V_0, //x
  Long V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Short?"
  IL_0008:  ldc.i4.s   42
  IL_000a:  conv.i8
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       "Function Short?.get_HasValue() As Boolean"
  IL_0013:  brtrue.s   IL_0018
  IL_0015:  ldloc.1
  IL_0016:  br.s       IL_0020
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Function Short?.GetValueOrDefault() As Short"
  IL_001f:  conv.i8
  IL_0020:  call       "Sub System.Console.WriteLine(Long)"
  IL_0025:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryIf4()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1

    Sub Main()
        Dim x As Short? = Nothing
        Dim y As IComparable(Of Short) = 42S

        Dim z = If(x, y)

        Console.WriteLine(z)

    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  1
  .locals init (Short? V_0, //x
  System.IComparable(Of Short) V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Short?"
  IL_0008:  ldc.i4.s   42
  IL_000a:  box        "Short"
  IL_000f:  castclass  "System.IComparable(Of Short)"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function Short?.get_HasValue() As Boolean"
  IL_001c:  brtrue.s   IL_0021
  IL_001e:  ldloc.1
  IL_001f:  br.s       IL_0032
  IL_0021:  ldloca.s   V_0
  IL_0023:  call       "Function Short?.GetValueOrDefault() As Short"
  IL_0028:  box        "Short"
  IL_002d:  castclass  "System.IComparable(Of Short)"
  IL_0032:  call       "Sub System.Console.WriteLine(Object)"
  IL_0037:  ret
}
                ]]>)
        End Sub

        <Fact, WorkItem(545064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545064")>
        Public Sub LiftedBinaryIf5_Nested()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Infer On
Imports System

Module Program
    Sub Main()
        Dim s4_a As Integer? = Nothing
        If If(If(True, s4_a, 0), If(True, s4_a, 0)) Then
            Console.Write("Fail")
        Else
            Console.Write("Pass")
        End If
    End Sub
End Module
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="Pass")

        End Sub

        <Fact(), WorkItem(544945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544945")>
        Public Sub LiftedBinaryRelationalWithNothingLiteral()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim x As SByte? = 127
        Dim y As SByte? = Nothing

        Dim r1 = x >= Nothing  ' = Nothing
        Console.Write("r1={0} ", r1)
        Dim r2 As Boolean? = Nothing < x ' = Nothing
        Console.Write("r2={0} ", r2)
        r1 = x > y
        Console.Write("r3={0} ", r1)
        r2 = y <= x
        Console.Write("r4={0} ", r2)
    End Sub
End Class
]]>
                    </file>
                </compilation>
            'emitOptions:=EmitOptions.RefEmitBug, 
            CompileAndVerify(source, expectedOutput:="r1= r2= r3= r4=")

        End Sub

        <WorkItem(544946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544946")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28044")>
        Public Sub LiftedBinaryConcatLikeWithNothingLiteral()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim x As SByte? = 127

        Dim r1 = x & Nothing '  = 127
        Console.Write("r1={0} ", r1)
        Dim r2 = Nothing Like x ' = False
        Console.Write("r2={0}", r2)
    End Sub
End Class
]]>
                    </file>
                </compilation>
            'emitOptions:=EmitOptions.RefEmitBug, 
            CompileAndVerify(source, expectedOutput:="r1=127 r2=False")

        End Sub

        <Fact(), WorkItem(544947, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544947")>
        Public Sub LiftedBinaryDivisionWithNothingLiteral()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim x As SByte? = 127
        Dim y As SByte? = Nothing

        Dim r1 = y / Nothing  ' = Nothing
        Console.Write("r1={0} ", r1)
        Dim r2 = Nothing / x ' = Nothing
        Console.Write("r2={0} ", r2)
        r1 = x \ Nothing
        Console.Write("r3={0} ", r1)
        r2 = Nothing \ y
        Console.Write("r4={0} ", r2)
        r1 = x \ y
        Console.Write("r5={0} ", r1)
        r2 = y / x
        Console.Write("r6={0} ", r2)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            CompileAndVerify(source, expectedOutput:="r1= r2= r3= r4= r5= r6=")

        End Sub

        <Fact()>
        Public Sub LiftedConversionHasValue()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Structure S1
        Public x As Integer
        Public Shared Widening Operator CType(ByVal a As S1) As S2
            Console.Write("W")
            Dim result As S2
            result.x = a.x + 1
            Return result
        End Operator

        Sub New(x As Integer)
            Me.x = x
        End Sub
    End Structure

    Structure S2
        Public x As Integer
    End Structure

    Sub Main()
        Dim y As S2 = New S1?(New S1(42))
        Console.WriteLine(y.x)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="W43").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldc.i4.s   42
  IL_0002:  newobj     "Sub MyClass1.S1..ctor(Integer)"
  IL_0007:  call       "Function MyClass1.S1.op_Implicit(MyClass1.S1) As MyClass1.S2"
  IL_000c:  ldfld      "MyClass1.S2.x As Integer"
  IL_0011:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0016:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedConversionHasNoValue()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Structure S1
        Public x As Integer
        Public Shared Widening Operator CType(ByVal a As S1) As S2
            Console.Write("W")
            Dim result As S2
            result.x = a.x + 1
            Return result
        End Operator

        Sub New(x As Integer)
            Me.x = x
        End Sub
    End Structure

    Structure S2
        Public x As Integer
    End Structure

    Sub Main()
        Try
            Dim y As S2 = New S1?()
            Console.WriteLine(y.x)
        Catch ex As InvalidOperationException
            Console.WriteLine("pass")
        End Try
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="pass").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (MyClass1.S1? V_0,
  System.InvalidOperationException V_1) //ex
  .try
{
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "MyClass1.S1?"
  IL_0008:  ldloc.0
  IL_0009:  stloc.0
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       "Function MyClass1.S1?.get_Value() As MyClass1.S1"
  IL_0011:  call       "Function MyClass1.S1.op_Implicit(MyClass1.S1) As MyClass1.S2"
  IL_0016:  ldfld      "MyClass1.S2.x As Integer"
  IL_001b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0020:  leave.s    IL_003a
}
  catch System.InvalidOperationException
{
  IL_0022:  dup
  IL_0023:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
  IL_0028:  stloc.1
  IL_0029:  ldstr      "pass"
  IL_002e:  call       "Sub System.Console.WriteLine(String)"
  IL_0033:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
  IL_0038:  leave.s    IL_003a
}
  IL_003a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedConversion()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module MyClass1
    Structure S1
        Public x As Integer
        Public Shared Widening Operator CType(ByVal a As S1) As S2
            Console.Write("W")
            Dim result As S2
            result.x = a.x + 1
            Return result
        End Operator

        Sub New(x As Integer)
            Me.x = x
        End Sub
    End Structure

    Structure S2
        Public x As Integer
    End Structure

    Sub Main()
        Dim x As S1? = New S1(42)
        Dim y As S2? = x

        Console.WriteLine(y.Value.x)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:="W43").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (MyClass1.S1? V_0, //x
  MyClass1.S2? V_1, //y
  MyClass1.S2? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  newobj     "Sub MyClass1.S1..ctor(Integer)"
  IL_0009:  call       "Sub MyClass1.S1?..ctor(MyClass1.S1)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       "Function MyClass1.S1?.get_HasValue() As Boolean"
  IL_0015:  brtrue.s   IL_0022
  IL_0017:  ldloca.s   V_2
  IL_0019:  initobj    "MyClass1.S2?"
  IL_001f:  ldloc.2
  IL_0020:  br.s       IL_0033
  IL_0022:  ldloca.s   V_0
  IL_0024:  call       "Function MyClass1.S1?.GetValueOrDefault() As MyClass1.S1"
  IL_0029:  call       "Function MyClass1.S1.op_Implicit(MyClass1.S1) As MyClass1.S2"
  IL_002e:  newobj     "Sub MyClass1.S2?..ctor(MyClass1.S2)"
  IL_0033:  stloc.1
  IL_0034:  ldloca.s   V_1
  IL_0036:  call       "Function MyClass1.S2?.get_Value() As MyClass1.S2"
  IL_003b:  ldfld      "MyClass1.S2.x As Integer"
  IL_0040:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0045:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedConversionDev10()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System
imports Microsoft.VisualBasic

Module Module1

    Sub Main()
        Dim av As New S1

        Dim c As S1?
        Dim d As T1?
        d = c
        Console.WriteLine("Lifted UD conversion: null check skips conversion call. d.HasValue= {0}" &amp; Environment.NewLine, d.HasValue) 'expect 7

        c = 1
        Console.WriteLine("widening to nullable UD conversion: c=1;  c.value= {0}" &amp; Environment.NewLine, c.Value.i) 'expect 7

        c = Nothing
        Console.WriteLine("widening to nullable UD conversion: c=Nothing;  c.HasValue= {0}" &amp; Environment.NewLine, c.HasValue) 'expect 7

        av.i = 7
        Dim a2 As New S1?(av)
        Dim b2 As T1
        b2 = a2
        Console.WriteLine("regular UD conversion+PDconversion:  S1?->S1 -->T1, value passed:{0}" &amp; Environment.NewLine, b2.i) 'expect 7

        Dim a21 As New S1
        a21.i = 8
        Dim b21 As T1?
        b21 = a21
        Console.WriteLine("regular UD conversion+PD conversion: S1-->T1->T1?, value passed:{0}" &amp; Environment.NewLine, b21.Value.i) 'expect 8

        Dim val As New S1
        val.i = 3
        c = New S1?(val)
        d = c
        Console.WriteLine("lifted UD conversion, value passed:{0}" &amp; Environment.NewLine, d.Value.i) 'expect 3

        Dim k As New S2
        k.i = 2
        Dim c2 As New S2?(k)
        Dim d2 As T2?
        d2 = c2 'UD conversion on nullable preferred over lifting
        Console.WriteLine(" UD nullable conversion, preferred over lifted value passed: {0}" &amp; Environment.NewLine, d2.Value.i) 'expect 2


        av.i = 5
        Dim a As New S1?(av)
        'a.i = 2
        Dim b As T1?
        b = a
        Console.WriteLine("lifted UD conversion, value passed:{0}" &amp; Environment.NewLine, b.Value.i) 'expect 5

        Dim a1 As S1
        a1.i = 6
        Dim b1 As T1
        b1 = a1
        Console.WriteLine("regular UD conversion, value passed:{0}" &amp; Environment.NewLine, b1.i) 'expect 6

        Dim a3 As S1
        a3.i = 8
        Dim b3 As T1?
        b3 = a3
        Console.WriteLine("regular UD conversion+PD conversion, value passed:{0}" &amp; Environment.NewLine, b3.Value.i) 'expect 8

        Dim atv = New st(Of Integer)
        atv.i = 9
        Dim at As New st(Of Integer)?(atv)
        Dim bt As Integer?
        bt = at
        Console.WriteLine("generic UD, value passed bt.value = :{0}" &amp; Environment.NewLine, bt.Value) 'expect 8
    End Sub

    Structure S1
        Dim i As Integer

        'Public Shared Widening Operator CType(ByVal a As S1?) As T1?

        '    Dim t As New T1
        '    t.i = a.Value.i
        '    Return t
        'End Operator

        Public Shared Narrowing Operator CType(ByVal a As S1) As T1
            Dim t As New T1
            t.i = a.i
            Console.WriteLine("UD regular conversion S1->T1 (possible by lifting) invoked")
            Return t
        End Operator

        Public Shared Widening Operator CType(ByVal a As Integer) As S1
            Dim t As New S1
            t.i = a
            Console.WriteLine("UD regular conversion int->S1 (possible by lifting) invoked")
            Return t
        End Operator
    End Structure

    Structure T1
        Dim i As Integer
    End Structure

    Structure S2
        Dim i As Integer

        Public Shared Widening Operator CType(ByVal a As S2?) As T2?
            Console.WriteLine("UD S2?->T2? conversion on nullable invoked")

            If a.HasValue Then
                Dim t As New T2
                t.i = a.Value.i
                Return t
            Else

                Return Nothing
            End If

        End Operator

        Public Shared Narrowing Operator CType(ByVal a As S2) As T2
            Dim t As New T2
            t.i = a.i
            Console.WriteLine("UD regular conversion S2->T2 (possible by lifting) invoked")
            Return t

        End Operator
    End Structure

    Structure T2
        Dim i As Integer
        'Public Shared Narrowing Operator CType(ByVal a As T2) As T2
        '    Dim t As New T2
        '    t.i = a.i
        '    Return t
        'End Operator
    End Structure

    Structure st(Of T As Structure)
        Dim i As T

        Public Shared Narrowing Operator CType(ByVal a As st(Of T)) As T
            Dim t As New T
            t = a.i
            Console.WriteLine("UD generic regular conversion st(of T)->T (possible by lifting) invoked")
            Return t
        End Operator
    End Structure
End Module


                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[Lifted UD conversion: null check skips conversion call. d.HasValue= False

UD regular conversion int->S1 (possible by lifting) invoked
widening to nullable UD conversion: c=1;  c.value= 1

widening to nullable UD conversion: c=Nothing;  c.HasValue= False

UD regular conversion S1->T1 (possible by lifting) invoked
regular UD conversion+PDconversion:  S1?->S1 -->T1, value passed:7

UD regular conversion S1->T1 (possible by lifting) invoked
regular UD conversion+PD conversion: S1-->T1->T1?, value passed:8

UD regular conversion S1->T1 (possible by lifting) invoked
lifted UD conversion, value passed:3

UD S2?->T2? conversion on nullable invoked
 UD nullable conversion, preferred over lifted value passed: 2

UD regular conversion S1->T1 (possible by lifting) invoked
lifted UD conversion, value passed:5

UD regular conversion S1->T1 (possible by lifting) invoked
regular UD conversion, value passed:6

UD regular conversion S1->T1 (possible by lifting) invoked
regular UD conversion+PD conversion, value passed:8

UD generic regular conversion st(of T)->T (possible by lifting) invoked
generic UD, value passed bt.value = :9

]]>)
        End Sub

        <Fact()>
        Public Sub LiftedWideningAndNarrowingConversions()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Structure S(Of T As Structure)
    Shared Narrowing Operator CType(x As S(Of T)) As T
        System.Console.Write("Narrowing ")
        Return Nothing
    End Operator

    Shared Widening Operator CType(x As S(Of T)) As T?
        System.Console.Write("Widening ")
        Return Nothing
    End Operator

End Structure

Module Program
    Sub Main()
        Dim x As S(Of Integer)? = New S(Of Integer)()
        Dim y As Integer? = 123
        Dim ret = If(x, y)
        Console.Write("Ret={0}", ret)
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:="Widening Ret=").
                            VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  2
  .locals init (S(Of Integer)? V_0, //x
  Integer? V_1, //y
  Integer? V_2, //ret
  S(Of Integer) V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldloca.s   V_3
  IL_0004:  initobj    "S(Of Integer)"
  IL_000a:  ldloc.3
  IL_000b:  call       "Sub S(Of Integer)?..ctor(S(Of Integer))"
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldc.i4.s   123
  IL_0014:  call       "Sub Integer?..ctor(Integer)"
  IL_0019:  ldloca.s   V_0
  IL_001b:  call       "Function S(Of Integer)?.get_HasValue() As Boolean"
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldloc.1
  IL_0023:  br.s       IL_0031
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       "Function S(Of Integer)?.GetValueOrDefault() As S(Of Integer)"
  IL_002c:  call       "Function S(Of Integer).op_Implicit(S(Of Integer)) As Integer?"
  IL_0031:  stloc.2
  IL_0032:  ldstr      "Ret={0}"
  IL_0037:  ldloc.2
  IL_0038:  box        "Integer?"
  IL_003d:  call       "Sub System.Console.Write(String, Object)"
  IL_0042:  ret
}]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryUserDefinedMinusDev10()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
imports System
imports Microsoft.VisualBasic

Structure S1
    Public x As Integer

    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub

    Public Shared Operator -(ByVal x As S1, ByVal y As S1?) As S1
        Return New S1(x.x - y.Value.x)
    End Operator
End Structure

Module M1
    Sub OutputEntry(ByVal expr As String, ByVal value As S1)
        Console.WriteLine("{0} = {1}", expr, value.x)
    End Sub

    Sub Main()
        Dim a As S1? = Nothing
        Dim b As S1? = New S1(1)
        Dim c As New S1(2)

        Dim tmp As S1

        Try
            tmp = a - c
        Catch ex As InvalidOperationException
            Console.WriteLine("a - c threw an InvalidOperationException as expected!")
        End Try

        Try
            tmp = c - a
        Catch ex As InvalidOperationException
            Console.WriteLine("c - a threw an InvalidOperationException as expected!")
        End Try

        Try
            tmp = a - b
        Catch ex As InvalidOperationException
            Console.WriteLine("a - b threw an InvalidOperationException as expected!")
        End Try

        Try
            tmp = b - a
        Catch ex As InvalidOperationException
            Console.WriteLine("a - b threw an InvalidOperationException as expected!")
        End Try

        OutputEntry("b - c", b - c)
        OutputEntry("c - b", c - b)
    End Sub
End Module

                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[a - c threw an InvalidOperationException as expected!
c - a threw an InvalidOperationException as expected!
a - b threw an InvalidOperationException as expected!
a - b threw an InvalidOperationException as expected!
b - c = -1
c - b = 1
]]>)
        End Sub

        <Fact()>
        Public Sub LiftedUnaryUserDefinedMinusDev10()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Imports System
Imports Microsoft.VisualBasic

Structure S1
    Dim x As Integer

    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub

    Public Shared Operator -(ByVal x As S1) As S1
        Return New S1(-x.x)
    End Operator
End Structure

Module M1
    Sub Main()
        Dim arg As S1? = New S1(1)
        Dim x = -arg

        Dim y = -New S1?
        Dim z = -New S1?(New S1(4))

        Console.WriteLine("x.Value = {0}", x.Value.x)
        Console.WriteLine("y.HasValue = {0}", y.HasValue)
        Console.WriteLine("z.Value = {0}", z.Value.x)
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:=
            <![CDATA[x.Value = -1
y.HasValue = False
z.Value = -4
]]>).
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size      155 (0x9b)
  .maxstack  2
  .locals init (S1? V_0, //arg
  S1? V_1, //x
  S1? V_2, //y
  S1? V_3, //z
  S1? V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  newobj     "Sub S1..ctor(Integer)"
  IL_0008:  call       "Sub S1?..ctor(S1)"
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       "Function S1?.get_HasValue() As Boolean"
  IL_0014:  brtrue.s   IL_0022
  IL_0016:  ldloca.s   V_4
  IL_0018:  initobj    "S1?"
  IL_001e:  ldloc.s    V_4
  IL_0020:  br.s       IL_0033
  IL_0022:  ldloca.s   V_0
  IL_0024:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0029:  call       "Function S1.op_UnaryNegation(S1) As S1"
  IL_002e:  newobj     "Sub S1?..ctor(S1)"
  IL_0033:  stloc.1
  IL_0034:  ldloca.s   V_2
  IL_0036:  initobj    "S1?"
  IL_003c:  ldloca.s   V_3
  IL_003e:  ldc.i4.4
  IL_003f:  newobj     "Sub S1..ctor(Integer)"
  IL_0044:  call       "Function S1.op_UnaryNegation(S1) As S1"
  IL_0049:  call       "Sub S1?..ctor(S1)"
  IL_004e:  ldstr      "x.Value = {0}"
  IL_0053:  ldloca.s   V_1
  IL_0055:  call       "Function S1?.get_Value() As S1"
  IL_005a:  ldfld      "S1.x As Integer"
  IL_005f:  box        "Integer"
  IL_0064:  call       "Sub System.Console.WriteLine(String, Object)"
  IL_0069:  ldstr      "y.HasValue = {0}"
  IL_006e:  ldloca.s   V_2
  IL_0070:  call       "Function S1?.get_HasValue() As Boolean"
  IL_0075:  box        "Boolean"
  IL_007a:  call       "Sub System.Console.WriteLine(String, Object)"
  IL_007f:  ldstr      "z.Value = {0}"
  IL_0084:  ldloca.s   V_3
  IL_0086:  call       "Function S1?.get_Value() As S1"
  IL_008b:  ldfld      "S1.x As Integer"
  IL_0090:  box        "Integer"
  IL_0095:  call       "Sub System.Console.WriteLine(String, Object)"
  IL_009a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryUserDefinedOneArgNotNullable()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Imports System
Imports Microsoft.VisualBasic

Structure S1
    Dim x As Integer

    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub

    Public Shared Operator -(ByVal x As S1, ByVal y As S1) As S1
        Return New S1(x.x - y.x)
    End Operator
End Structure

Module M1
    Sub Main()
        Dim x As New S1?(New S1(42))
        Dim y = x - New S1(2)

        Console.WriteLine(y.Value.x)
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:="40").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (S1? V_0, //x
  S1? V_1, //y
  S1 V_2,
  S1? V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  newobj     "Sub S1..ctor(Integer)"
  IL_0009:  call       "Sub S1?..ctor(S1)"
  IL_000e:  ldloca.s   V_2
  IL_0010:  ldc.i4.2
  IL_0011:  call       "Sub S1..ctor(Integer)"
  IL_0016:  ldloca.s   V_0
  IL_0018:  call       "Function S1?.get_HasValue() As Boolean"
  IL_001d:  brtrue.s   IL_002a
  IL_001f:  ldloca.s   V_3
  IL_0021:  initobj    "S1?"
  IL_0027:  ldloc.3
  IL_0028:  br.s       IL_003c
  IL_002a:  ldloca.s   V_0
  IL_002c:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0031:  ldloc.2
  IL_0032:  call       "Function S1.op_Subtraction(S1, S1) As S1"
  IL_0037:  newobj     "Sub S1?..ctor(S1)"
  IL_003c:  stloc.1
  IL_003d:  ldloca.s   V_1
  IL_003f:  call       "Function S1?.get_Value() As S1"
  IL_0044:  ldfld      "S1.x As Integer"
  IL_0049:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004e:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedBinaryUserDefinedReturnsNullable()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Imports System
Imports Microsoft.VisualBasic

Structure S1
    Dim x As Integer

    Public Sub New(ByVal x As Integer)
        Me.x = x
    End Sub

    Public Shared Operator +(ByVal x As S1, ByVal y As S1) As S1?
        Return New S1(x.x + y.x)
    End Operator
End Structure

Module M1
    Sub Main()
        Dim x As New S1?(New S1(42))
        Dim y = x + x

        Console.WriteLine(y.Value.x)
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:="84").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (S1? V_0, //x
  S1? V_1, //y
  S1? V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  newobj     "Sub S1..ctor(Integer)"
  IL_0009:  call       "Sub S1?..ctor(S1)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       "Function S1?.get_HasValue() As Boolean"
  IL_0015:  ldloca.s   V_0
  IL_0017:  call       "Function S1?.get_HasValue() As Boolean"
  IL_001c:  and
  IL_001d:  brtrue.s   IL_002a
  IL_001f:  ldloca.s   V_2
  IL_0021:  initobj    "S1?"
  IL_0027:  ldloc.2
  IL_0028:  br.s       IL_003d
  IL_002a:  ldloca.s   V_0
  IL_002c:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0031:  ldloca.s   V_0
  IL_0033:  call       "Function S1?.GetValueOrDefault() As S1"
  IL_0038:  call       "Function S1.op_Addition(S1, S1) As S1?"
  IL_003d:  stloc.1
  IL_003e:  ldloca.s   V_1
  IL_0040:  call       "Function S1?.get_Value() As S1"
  IL_0045:  ldfld      "S1.x As Integer"
  IL_004a:  call       "Sub System.Console.WriteLine(Integer)"
  IL_004f:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedUserConversionShortToInt()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System                        
Module M1

    Structure S1
        Shared Widening Operator CType(x As Byte) As S1
            Return New S1()
        End Operator
    End Structure

    Sub Main()
        Dim y As S1? = new Long?(42) ' === UDC
        Console.WriteLine(y.HasValue)
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:="True").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (M1.S1? V_0) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  call       "Function M1.S1.op_Implicit(Byte) As M1.S1"
  IL_0009:  call       "Sub M1.S1?..ctor(M1.S1)"
  IL_000e:  ldloca.s   V_0
  IL_0010:  call       "Function M1.S1?.get_HasValue() As Boolean"
  IL_0015:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_001a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedUserConversionShortToIntA()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System                        
Module M1

    Structure S1
        Shared Widening Operator CType(x As Byte) As S1
            Return New S1()
        End Operator
    End Structure

    Sub Main()
        Dim y As S1? = x ' === UDC
        Console.WriteLine(y.HasValue)
    End Sub

    Function x() As Long?
        Console.Write("x")
        Return New Long?(42)
    End Function
End Module
                    </file>
                </compilation>, expectedOutput:="xTrue").
            VerifyIL("M1.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (M1.S1? V_0, //y
  Long? V_1,
  Long? V_2,
  M1.S1? V_3)
  IL_0000:  call       "Function M1.x() As Long?"
  IL_0005:  dup
  IL_0006:  stloc.1
  IL_0007:  stloc.2
  IL_0008:  ldloca.s   V_2
  IL_000a:  call       "Function Long?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_001c
  IL_0011:  ldloca.s   V_3
  IL_0013:  initobj    "M1.S1?"
  IL_0019:  ldloc.3
  IL_001a:  br.s       IL_002e
  IL_001c:  ldloca.s   V_1
  IL_001e:  call       "Function Long?.GetValueOrDefault() As Long"
  IL_0023:  conv.ovf.u1
  IL_0024:  call       "Function M1.S1.op_Implicit(Byte) As M1.S1"
  IL_0029:  newobj     "Sub M1.S1?..ctor(M1.S1)"
  IL_002e:  stloc.0
  IL_002f:  ldloca.s   V_0
  IL_0031:  call       "Function M1.S1?.get_HasValue() As Boolean"
  IL_0036:  call       "Sub System.Console.WriteLine(Boolean)"
  IL_003b:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(544589, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544589")>
        Public Sub LiftedShortCircuitOperations()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Module M

    Sub Main()
        Dim bF As Boolean? = False
        Dim bT As Boolean? = True
        Dim bN As Boolean? = Nothing

        If bF OrElse bT AndAlso bN Then
            Console.Write("True")
        Else
            Console.Write("False")
        End If

    End Sub

End Module
                    </file>
                </compilation>, expectedOutput:="False").
            VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       91 (0x5b)
  .maxstack  2
  .locals init (Boolean? V_0, //bF
                Boolean? V_1, //bT
                Boolean? V_2) //bN
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.1
  IL_000b:  call       "Sub Boolean?..ctor(Boolean)"
  IL_0010:  ldloca.s   V_2
  IL_0012:  initobj    "Boolean?"
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001f:  brtrue.s   IL_0045
  IL_0021:  ldloca.s   V_1
  IL_0023:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0028:  brfalse.s  IL_0033
  IL_002a:  ldloca.s   V_1
  IL_002c:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0031:  brfalse.s  IL_0050
  IL_0033:  ldloca.s   V_2
  IL_0035:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_003a:  brfalse.s  IL_0050
  IL_003c:  ldloca.s   V_1
  IL_003e:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0043:  brfalse.s  IL_0050
  IL_0045:  ldstr      "True"
  IL_004a:  call       "Sub System.Console.Write(String)"
  IL_004f:  ret
  IL_0050:  ldstr      "False"
  IL_0055:  call       "Sub System.Console.Write(String)"
  IL_005a:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(545124, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545124")>
        Public Sub LogicalOperationsWithNullableEnum()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Enum EnumItems
    Item1 = 1
    Item2 = 2
    Item3 = 4
    Item4 = 8
End Enum

Module M

    Sub Main()
        Dim value1a As EnumItems? = 10
        Dim value1b As EnumItems = EnumItems.Item3
        Console.Write(Not value1a And value1b Or value1a Xor value1b)
    End Sub

End Module
                    </file>
                </compilation>, expectedOutput:="10").
            VerifyIL("M.Main",
            <![CDATA[
{
  // Code size      169 (0xa9)
  .maxstack  2
  .locals init (EnumItems? V_0, //value1a
  EnumItems V_1, //value1b
  EnumItems? V_2,
  EnumItems? V_3,
  EnumItems? V_4,
  EnumItems? V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   10
  IL_0004:  call       "Sub EnumItems?..ctor(EnumItems)"
  IL_0009:  ldc.i4.4
  IL_000a:  stloc.1
  IL_000b:  ldloca.s   V_0
  IL_000d:  call       "Function EnumItems?.get_HasValue() As Boolean"
  IL_0012:  brtrue.s   IL_0017
  IL_0014:  ldloc.0
  IL_0015:  br.s       IL_0024
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       "Function EnumItems?.GetValueOrDefault() As EnumItems"
  IL_001e:  not
  IL_001f:  newobj     "Sub EnumItems?..ctor(EnumItems)"
  IL_0024:  stloc.s    V_4
  IL_0026:  ldloca.s   V_4
  IL_0028:  call       "Function EnumItems?.get_HasValue() As Boolean"
  IL_002d:  brtrue.s   IL_003b
  IL_002f:  ldloca.s   V_5
  IL_0031:  initobj    "EnumItems?"
  IL_0037:  ldloc.s    V_5
  IL_0039:  br.s       IL_0049
  IL_003b:  ldloca.s   V_4
  IL_003d:  call       "Function EnumItems?.GetValueOrDefault() As EnumItems"
  IL_0042:  ldloc.1
  IL_0043:  and
  IL_0044:  newobj     "Sub EnumItems?..ctor(EnumItems)"
  IL_0049:  stloc.3
  IL_004a:  ldloca.s   V_3
  IL_004c:  call       "Function EnumItems?.get_HasValue() As Boolean"
  IL_0051:  ldloca.s   V_0
  IL_0053:  call       "Function EnumItems?.get_HasValue() As Boolean"
  IL_0058:  and
  IL_0059:  brtrue.s   IL_0067
  IL_005b:  ldloca.s   V_4
  IL_005d:  initobj    "EnumItems?"
  IL_0063:  ldloc.s    V_4
  IL_0065:  br.s       IL_007b
  IL_0067:  ldloca.s   V_3
  IL_0069:  call       "Function EnumItems?.GetValueOrDefault() As EnumItems"
  IL_006e:  ldloca.s   V_0
  IL_0070:  call       "Function EnumItems?.GetValueOrDefault() As EnumItems"
  IL_0075:  or
  IL_0076:  newobj     "Sub EnumItems?..ctor(EnumItems)"
  IL_007b:  stloc.2
  IL_007c:  ldloca.s   V_2
  IL_007e:  call       "Function EnumItems?.get_HasValue() As Boolean"
  IL_0083:  brtrue.s   IL_0090
  IL_0085:  ldloca.s   V_3
  IL_0087:  initobj    "EnumItems?"
  IL_008d:  ldloc.3
  IL_008e:  br.s       IL_009e
  IL_0090:  ldloca.s   V_2
  IL_0092:  call       "Function EnumItems?.GetValueOrDefault() As EnumItems"
  IL_0097:  ldloc.1
  IL_0098:  xor
  IL_0099:  newobj     "Sub EnumItems?..ctor(EnumItems)"
  IL_009e:  box        "EnumItems?"
  IL_00a3:  call       "Sub System.Console.Write(Object)"
  IL_00a8:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(545125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545125")>
        Public Sub ArithmeticOperationsWithNullableType()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Friend Module M
    Public Function goo_exception() As Integer
        Console.Write("1st Called ")
        Throw New ArgumentNullException()
    End Function

    Public Function goo_eval_check() As Integer?
        Console.Write("2nd Called ")
        goo_eval_check = 2
    End Function

    Sub Main()
        Try
            Dim r1 = goo_exception() \ goo_eval_check()
        Catch ex As ArgumentNullException

        End Try
    End Sub
End Module
                    </file>
                </compilation>, expectedOutput:="1st Called").
            VerifyIL("M.Main",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (Integer V_0,
                Integer? V_1,
                System.ArgumentNullException V_2) //ex
  .try
  {
    IL_0000:  call       "Function M.goo_exception() As Integer"
    IL_0005:  stloc.0
    IL_0006:  call       "Function M.goo_eval_check() As Integer?"
    IL_000b:  stloc.1
    IL_000c:  ldloca.s   V_1
    IL_000e:  call       "Function Integer?.get_HasValue() As Boolean"
    IL_0013:  brfalse.s  IL_0024
    IL_0015:  ldloc.0
    IL_0016:  ldloca.s   V_1
    IL_0018:  call       "Function Integer?.GetValueOrDefault() As Integer"
    IL_001d:  div
    IL_001e:  newobj     "Sub Integer?..ctor(Integer)"
    IL_0023:  pop
    IL_0024:  leave.s    IL_0034
  }
  catch System.ArgumentNullException
  {
    IL_0026:  dup
    IL_0027:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_002c:  stloc.2
    IL_002d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0032:  leave.s    IL_0034
  }
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(545437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545437")>
        Public Sub Regress13840()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Module Module1
    Sub Main()
        Dim y As MyStruct? = Nothing
        If (CType(CType(Nothing, MyStruct?), MyStruct?) = y) Then 'expect warning
            System.Console.WriteLine("Equals")
        End If
    End Sub
    Structure MyStruct
        Shared Operator =(ByVal left As MyStruct, ByVal right As MyStruct) As Boolean
            Return True
        End Operator
        Shared Operator &lt;&gt;(ByVal left As MyStruct, ByVal right As MyStruct) As Boolean
            Return False
        End Operator
    End Structure
End Module

                    </file>
                </compilation>, expectedOutput:="")
        End Sub

#Region "Diagnostics"

        <Fact(), WorkItem(544942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544942"), WorkItem(599013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/599013")>
        Public Sub BC30424ERR_ConstAsNonConstant_Nullable()
            Dim source =
                <compilation>
                    <file name="a.vb">
Structure S : End Structure
Enum E : Zero : End Enum

Class C
    Sub M()
        Const c1 As Boolean? = Nothing
        Const c2 As ULong? = 12345
        Const c3 As E? = E.Zero
        Dim d = Sub()
                    Const c4 As S? = Nothing
                End Sub
    End Sub
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source)

            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_ConstAsNonConstant, "Boolean?"),
                                        Diagnostic(ERRID.ERR_ConstAsNonConstant, "ULong?"),
                                        Diagnostic(ERRID.ERR_ConstAsNonConstant, "E?"),
                                        Diagnostic(ERRID.ERR_ConstAsNonConstant, "S?"))
        End Sub

        <Fact()>
        Public Sub BC30456ERR_NameNotMember2_Nullable()
            Dim source =
                <compilation>
                    <file name="a.vb">
Option Strict Off
Option Explicit Off
Imports System

Structure S
    Public field As C
End Structure

Class C
    Public field As S
    Shared Widening Operator CType(p As C) As S
        Console.Write("N")
        Return p.field
    End Operator

    Shared Narrowing Operator CType(p As S) As C
        Console.Write("W")
        Return p.field
    End Operator

End Class

Module Program
    Sub Main(args As String())
        Dim x As New S()
        x.field = New C()
        x.field.field = x
        Dim y As C = x.field
        Dim ns As S? = y
        Console.Write(ns.field.field Is y) ' BC30456
    End Sub

End Module
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source)
            comp.VerifyDiagnostics(Diagnostic(ERRID.ERR_NameNotMember2, "ns.field").WithArguments("field", "S?"))
        End Sub

        <Fact(), WorkItem(544945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544945")>
        Public Sub BC42037And42038WRN_EqualToLiteralNothing_Nullable()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Class C
    Shared Sub Main()
        Dim x As SByte? = Nothing
        Dim y As SByte? = 123

        Dim r1 = x = Nothing ' Assert here
        If Not (r1 AndAlso Nothing <> y) Then
            Console.WriteLine("FAIL")
        Else
            Console.WriteLine("PASS")
        End If
    End Sub
End Class
]]>
                    </file>
                </compilation>

            CompileAndVerify(source, expectedOutput:="PASS").VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_EqualToLiteralNothing, "x = Nothing"),
                    Diagnostic(ERRID.WRN_NotEqualToLiteralNothing, "Nothing <> y")
                                                                          )
        End Sub

        <Fact(), WorkItem(545050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545050")>
        Public Sub DoNotGiveBC32126ERR_AddressOfNullableMethod()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main()
        Dim x As Integer? = Nothing
        Dim f1 As Func(Of String) = AddressOf x.ToString
        f1()
        Dim f2 As Func(Of Integer) = AddressOf x.GetHashCode
        f2()
        Dim f3 As Func(Of Object, Boolean) = AddressOf x.Equals
        f3(Nothing)
        Dim f4 As Func(Of Type) = AddressOf x.GetType
        f4()
        Dim f5 As Func(Of Integer, Integer?) = AddressOf Integer?.op_Implicit
        f5(1)
    End Sub
End Module
]]>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerify(source, verify:=Verification.FailsILVerify.WithILVerifyMessage("
[Main]: Unrecognized arguments for delegate .ctor. { Offset = 0x16 }
[Main]: Unrecognized arguments for delegate .ctor. { Offset = 0x2e }
[Main]: Unrecognized arguments for delegate .ctor. { Offset = 0x46 }
[Main]: Unrecognized arguments for delegate .ctor. { Offset = 0x5d }"))

            verifier.VerifyDiagnostics().VerifyIL("Program.Main", <![CDATA[
{
  // Code size      124 (0x7c)
  .maxstack  3
  .locals init (Integer? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Integer?"
  IL_0008:  ldloc.0
  IL_0009:  dup
  IL_000a:  box        "Integer?"
  IL_000f:  dup
  IL_0010:  ldvirtftn  "Function System.ValueType.ToString() As String"
  IL_0016:  newobj     "Sub System.Func(Of String)..ctor(Object, System.IntPtr)"
  IL_001b:  callvirt   "Function System.Func(Of String).Invoke() As String"
  IL_0020:  pop
  IL_0021:  dup
  IL_0022:  box        "Integer?"
  IL_0027:  dup
  IL_0028:  ldvirtftn  "Function System.ValueType.GetHashCode() As Integer"
  IL_002e:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0033:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0038:  pop
  IL_0039:  dup
  IL_003a:  box        "Integer?"
  IL_003f:  dup
  IL_0040:  ldvirtftn  "Function System.ValueType.Equals(Object) As Boolean"
  IL_0046:  newobj     "Sub System.Func(Of Object, Boolean)..ctor(Object, System.IntPtr)"
  IL_004b:  ldnull
  IL_004c:  callvirt   "Function System.Func(Of Object, Boolean).Invoke(Object) As Boolean"
  IL_0051:  pop
  IL_0052:  box        "Integer?"
  IL_0057:  ldftn      "Function Object.GetType() As System.Type"
  IL_005d:  newobj     "Sub System.Func(Of System.Type)..ctor(Object, System.IntPtr)"
  IL_0062:  callvirt   "Function System.Func(Of System.Type).Invoke() As System.Type"
  IL_0067:  pop
  IL_0068:  ldnull
  IL_0069:  ldftn      "Function Integer?.op_Implicit(Integer) As Integer?"
  IL_006f:  newobj     "Sub System.Func(Of Integer, Integer?)..ctor(Object, System.IntPtr)"
  IL_0074:  ldc.i4.1
  IL_0075:  callvirt   "Function System.Func(Of Integer, Integer?).Invoke(Integer) As Integer?"
  IL_007a:  pop
  IL_007b:  ret
}
                ]]>)
        End Sub

        <Fact(), WorkItem(545126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545126")>
        Public Sub BC36629ERR_NullableTypeInferenceNotSupported()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System

Class C
    Public X? = 10
    Public X1? = {10}

    Public Property Z? = 10
    Public Property Z1? = {10}

    Sub Test()
        Dim Y? = 10
        Dim Y1? = {10}
    End Sub

    Sub Test1()
        ' Option Strict Off, Option Infer Off - Expected No errors
        ' Option Strict Off, Option Infer On - Expected BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        ' Option Strict On, Option Infer On - Expected BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U? = 10

        ' Option Strict Off, Option Infer Off - Expected No errors
        ' Option Strict Off, Option Infer On - Expected BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        ' Option Strict On, Option Infer On - Expected BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U1? = {10}
        Static V?() = Nothing
        Static V1?(1) = Nothing
    End Sub

    Sub Test2()
        Const U? = 10
        Dim x As Object = U
    End Sub

End Class
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off).WithOptionInfer(False))

            AssertTheseDiagnostics(compilation,
<expected>
BC36629: Nullable type inference is not supported in this context.
    Public X? = 10
           ~~
BC36629: Nullable type inference is not supported in this context.
    Public X1? = {10}
           ~~~
BC30205: End of statement expected.
    Public Property Z? = 10
                     ~
BC30205: End of statement expected.
    Public Property Z1? = {10}
                      ~
BC36629: Nullable type inference is not supported in this context.
        Dim Y? = 10
            ~~
BC36629: Nullable type inference is not supported in this context.
        Dim Y1? = {10}
            ~~~
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        Static V1?(1) = Nothing
               ~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off).WithOptionInfer(True))

            AssertTheseDiagnostics(compilation,
<expected>
BC36629: Nullable type inference is not supported in this context.
    Public X? = 10
           ~~
BC36629: Nullable type inference is not supported in this context.
    Public X1? = {10}
           ~~~
BC30205: End of statement expected.
    Public Property Z? = 10
                     ~
BC30205: End of statement expected.
    Public Property Z1? = {10}
                      ~
BC36628: A nullable type cannot be inferred for variable 'Y1'.
        Dim Y1? = {10}
            ~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U? = 10
               ~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U1? = {10}
               ~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static V?() = Nothing
               ~~~~
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        Static V1?(1) = Nothing
               ~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static V1?(1) = Nothing
               ~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On).WithOptionInfer(False))

            AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Public X? = 10
           ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Public X1? = {10}
           ~~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Public Property Z? = 10
                    ~
BC30205: End of statement expected.
    Public Property Z? = 10
                     ~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Public Property Z1? = {10}
                    ~~
BC30205: End of statement expected.
    Public Property Z1? = {10}
                      ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Dim Y? = 10
            ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Dim Y1? = {10}
            ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static U? = 10
               ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static U1? = {10}
               ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static V?() = Nothing
               ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static V1?(1) = Nothing
               ~~
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        Static V1?(1) = Nothing
               ~~~~~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Const U? = 10
              ~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On).WithOptionInfer(True))

            AssertTheseDiagnostics(compilation,
<expected>
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Public X? = 10
           ~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
    Public X1? = {10}
           ~~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Public Property Z? = 10
                    ~
BC30205: End of statement expected.
    Public Property Z? = 10
                     ~
BC30210: Option Strict On requires all Function, Property, and Operator declarations to have an 'As' clause.
    Public Property Z1? = {10}
                    ~~
BC30205: End of statement expected.
    Public Property Z1? = {10}
                      ~
BC36628: A nullable type cannot be inferred for variable 'Y1'.
        Dim Y1? = {10}
            ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static U? = 10
               ~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U? = 10
               ~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static U1? = {10}
               ~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static U1? = {10}
               ~~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static V?() = Nothing
               ~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static V?() = Nothing
               ~~~~
BC30209: Option Strict On requires all variable declarations to have an 'As' clause.
        Static V1?(1) = Nothing
               ~~
BC30672: Explicit initialization is not permitted for arrays declared with explicit bounds.
        Static V1?(1) = Nothing
               ~~~~~~
BC33112: Nullable modifier cannot be used with a variable whose implicit type is 'Object'.
        Static V1?(1) = Nothing
               ~~~~~~
</expected>)
        End Sub

        <Fact(), WorkItem(545126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545126")>
        Public Sub BC36629ERR_NullableTypeInferenceNotSupported_2()
            Dim source =
                <compilation>
                    <file name="a.vb">
Imports System

Class C
    Public X%? = 10
    Public X1%? = {10}

    Public Property Z%? = 10
    Public Property Z1%? = {10}

    Sub Test()
        Dim Y%? = 10
        Dim Y1%? = {10}
    End Sub

    Sub Test1()
        Static U%? = 10

        Static U1%? = {10}
    End Sub

    Sub Test2()
        Const V%? = 10
        Dim x As Object = V
    End Sub
End Class
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(source, options:=TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off).WithOptionInfer(False))

            AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
    Public X1%? = {10}
                  ~~~~
BC30205: End of statement expected.
    Public Property Z%? = 10
                      ~
BC30205: End of statement expected.
    Public Property Z1%? = {10}
                       ~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Dim Y1%? = {10}
                   ~~~~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Static U1%? = {10}
                      ~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        Const V%? = 10
              ~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Off).WithOptionInfer(True))

            AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
    Public X1%? = {10}
                  ~~~~
BC30205: End of statement expected.
    Public Property Z%? = 10
                      ~
BC30205: End of statement expected.
    Public Property Z1%? = {10}
                       ~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Dim Y1%? = {10}
                   ~~~~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Static U1%? = {10}
                      ~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        Const V%? = 10
              ~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On).WithOptionInfer(False))

            AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
    Public X1%? = {10}
                  ~~~~
BC30205: End of statement expected.
    Public Property Z%? = 10
                      ~
BC30205: End of statement expected.
    Public Property Z1%? = {10}
                       ~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Dim Y1%? = {10}
                   ~~~~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Static U1%? = {10}
                      ~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        Const V%? = 10
              ~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On).WithOptionInfer(True))

            AssertTheseDiagnostics(compilation,
<expected>
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
    Public X1%? = {10}
                  ~~~~
BC30205: End of statement expected.
    Public Property Z%? = 10
                      ~
BC30205: End of statement expected.
    Public Property Z1%? = {10}
                       ~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Dim Y1%? = {10}
                   ~~~~
BC30311: Value of type 'Integer()' cannot be converted to 'Integer?'.
        Static U1%? = {10}
                      ~~~~
BC30424: Constants must be of an intrinsic or enumerated type, not a class, structure, type parameter, or array type.
        Const V%? = 10
              ~~~
</expected>)
        End Sub

#End Region

#Region "For Loop"

        <Fact()>
        Public Sub LiftedForTo()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Option Strict On
Imports System

Public Class MyClass1
    Public Shared Sub Main()
        Dim l As UInteger? = 1
        For x As UInteger? = 1 To 10 Step l
            Console.Write(x)
            If x >= 5 Then
                x = Nothing
            End If
        Next
    End Sub
End Class

                    </file>
                </compilation>, expectedOutput:="12345").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      282 (0x11a)
  .maxstack  3
  .locals init (UInteger? V_0, //l
                UInteger? V_1,
                UInteger? V_2,
                Boolean V_3,
                UInteger? V_4, //x
                Long? V_5,
                Long? V_6,
                Boolean? V_7,
                UInteger? V_8)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub UInteger?..ctor(UInteger)"
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     "Sub UInteger?..ctor(UInteger)"
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.i4.s   10
  IL_0012:  call       "Sub UInteger?..ctor(UInteger)"
  IL_0017:  ldloc.0
  IL_0018:  stloc.2
  IL_0019:  ldloca.s   V_2
  IL_001b:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_0020:  brfalse.s  IL_0031
  IL_0022:  ldloca.s   V_2
  IL_0024:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_0029:  ldc.i4.0
  IL_002a:  clt.un
  IL_002c:  ldc.i4.0
  IL_002d:  ceq
  IL_002f:  br.s       IL_0032
  IL_0031:  ldc.i4.0
  IL_0032:  stloc.3
  IL_0033:  stloc.s    V_4
  IL_0035:  br         IL_00d8
  IL_003a:  ldloc.s    V_4
  IL_003c:  box        "UInteger?"
  IL_0041:  call       "Sub System.Console.Write(Object)"
  IL_0046:  ldloca.s   V_4
  IL_0048:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_004d:  brtrue.s   IL_005b
  IL_004f:  ldloca.s   V_6
  IL_0051:  initobj    "Long?"
  IL_0057:  ldloc.s    V_6
  IL_0059:  br.s       IL_0068
  IL_005b:  ldloca.s   V_4
  IL_005d:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_0062:  conv.u8
  IL_0063:  newobj     "Sub Long?..ctor(Long)"
  IL_0068:  stloc.s    V_5
  IL_006a:  ldloca.s   V_5
  IL_006c:  call       "Function Long?.get_HasValue() As Boolean"
  IL_0071:  brtrue.s   IL_007f
  IL_0073:  ldloca.s   V_7
  IL_0075:  initobj    "Boolean?"
  IL_007b:  ldloc.s    V_7
  IL_007d:  br.s       IL_0092
  IL_007f:  ldloca.s   V_5
  IL_0081:  call       "Function Long?.GetValueOrDefault() As Long"
  IL_0086:  ldc.i4.5
  IL_0087:  conv.i8
  IL_0088:  clt
  IL_008a:  ldc.i4.0
  IL_008b:  ceq
  IL_008d:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0092:  stloc.s    V_7
  IL_0094:  ldloca.s   V_7
  IL_0096:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_009b:  brfalse.s  IL_00a5
  IL_009d:  ldloca.s   V_4
  IL_009f:  initobj    "UInteger?"
  IL_00a5:  ldloca.s   V_2
  IL_00a7:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_00ac:  ldloca.s   V_4
  IL_00ae:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_00b3:  and
  IL_00b4:  brtrue.s   IL_00c2
  IL_00b6:  ldloca.s   V_8
  IL_00b8:  initobj    "UInteger?"
  IL_00be:  ldloc.s    V_8
  IL_00c0:  br.s       IL_00d6
  IL_00c2:  ldloca.s   V_4
  IL_00c4:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_00c9:  ldloca.s   V_2
  IL_00cb:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_00d0:  add.ovf.un
  IL_00d1:  newobj     "Sub UInteger?..ctor(UInteger)"
  IL_00d6:  stloc.s    V_4
  IL_00d8:  ldloca.s   V_1
  IL_00da:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_00df:  ldloca.s   V_4
  IL_00e1:  call       "Function UInteger?.get_HasValue() As Boolean"
  IL_00e6:  and
  IL_00e7:  brfalse.s  IL_0119
  IL_00e9:  ldloc.3
  IL_00ea:  brtrue.s   IL_0101
  IL_00ec:  ldloca.s   V_4
  IL_00ee:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_00f3:  ldloca.s   V_1
  IL_00f5:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_00fa:  clt.un
  IL_00fc:  ldc.i4.0
  IL_00fd:  ceq
  IL_00ff:  br.s       IL_0114
  IL_0101:  ldloca.s   V_4
  IL_0103:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_0108:  ldloca.s   V_1
  IL_010a:  call       "Function UInteger?.GetValueOrDefault() As UInteger"
  IL_010f:  cgt.un
  IL_0111:  ldc.i4.0
  IL_0112:  ceq
  IL_0114:  brtrue     IL_003a
  IL_0119:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedForToDefaultStep()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Option Strict On
Imports System

Public Class MyClass1
    Public Shared Sub Main()
        Dim x As Integer? = 1
        For x = 1 To 10 
            Console.Write(x)
            If x >= 5 Then
                x = Nothing
            End If
        Next
    End Sub
End Class

                    </file>
                </compilation>, expectedOutput:="12345").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      248 (0xf8)
  .maxstack  3
  .locals init (Integer? V_0, //x
  Integer? V_1,
  Integer? V_2,
  Boolean V_3,
  Integer? V_4,
  Boolean? V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldc.i4.1
  IL_0009:  newobj     "Sub Integer?..ctor(Integer)"
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.i4.s   10
  IL_0012:  call       "Sub Integer?..ctor(Integer)"
  IL_0017:  ldloca.s   V_2
  IL_0019:  ldc.i4.1
  IL_001a:  call       "Sub Integer?..ctor(Integer)"
  IL_001f:  ldloca.s   V_2
  IL_0021:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0026:  brfalse.s  IL_0037
  IL_0028:  ldloca.s   V_2
  IL_002a:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_002f:  ldc.i4.0
  IL_0030:  clt
  IL_0032:  ldc.i4.0
  IL_0033:  ceq
  IL_0035:  br.s       IL_0038
  IL_0037:  ldc.i4.0
  IL_0038:  stloc.3
  IL_0039:  stloc.0
  IL_003a:  br.s       IL_00b6
  IL_003c:  ldloc.0
  IL_003d:  box        "Integer?"
  IL_0042:  call       "Sub System.Console.Write(Object)"
  IL_0047:  ldloc.0
  IL_0048:  stloc.s    V_4
  IL_004a:  ldloca.s   V_4
  IL_004c:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0051:  brtrue.s   IL_005f
  IL_0053:  ldloca.s   V_5
  IL_0055:  initobj    "Boolean?"
  IL_005b:  ldloc.s    V_5
  IL_005d:  br.s       IL_0071
  IL_005f:  ldloca.s   V_4
  IL_0061:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0066:  ldc.i4.5
  IL_0067:  clt
  IL_0069:  ldc.i4.0
  IL_006a:  ceq
  IL_006c:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0071:  stloc.s    V_5
  IL_0073:  ldloca.s   V_5
  IL_0075:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_007a:  brfalse.s  IL_0084
  IL_007c:  ldloca.s   V_0
  IL_007e:  initobj    "Integer?"
  IL_0084:  ldloca.s   V_2
  IL_0086:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_008b:  ldloca.s   V_0
  IL_008d:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0092:  and
  IL_0093:  brtrue.s   IL_00a1
  IL_0095:  ldloca.s   V_4
  IL_0097:  initobj    "Integer?"
  IL_009d:  ldloc.s    V_4
  IL_009f:  br.s       IL_00b5
  IL_00a1:  ldloca.s   V_0
  IL_00a3:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00a8:  ldloca.s   V_2
  IL_00aa:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00af:  add.ovf
  IL_00b0:  newobj     "Sub Integer?..ctor(Integer)"
  IL_00b5:  stloc.0
  IL_00b6:  ldloca.s   V_1
  IL_00b8:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_00bd:  ldloca.s   V_0
  IL_00bf:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_00c4:  and
  IL_00c5:  brfalse.s  IL_00f7
  IL_00c7:  ldloc.3
  IL_00c8:  brtrue.s   IL_00df
  IL_00ca:  ldloca.s   V_0
  IL_00cc:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00d1:  ldloca.s   V_1
  IL_00d3:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00d8:  clt
  IL_00da:  ldc.i4.0
  IL_00db:  ceq
  IL_00dd:  br.s       IL_00f2
  IL_00df:  ldloca.s   V_0
  IL_00e1:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00e6:  ldloca.s   V_1
  IL_00e8:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_00ed:  cgt
  IL_00ef:  ldc.i4.0
  IL_00f0:  ceq
  IL_00f2:  brtrue     IL_003c
  IL_00f7:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedForToDecimalSideeffectsNullStep()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Option Strict On
Imports System

Public Class MyClass1
    Public Shared Sub Main()
        For x As Decimal? = Init() To Limit() Step [Step]()
            Console.Write(x)
        Next
        End Sub

        Private Shared Function Init() As Integer
            Console.WriteLine("init")
            Return 9
        End Function

        Private Shared Function Limit() As Integer
            Console.WriteLine("limit")
            Return 1
        End Function

        Private Shared Function [Step]() As Decimal?
            Console.WriteLine("step")
            Return nothing
        End Function

    End Class

                    </file>
                </compilation>, expectedOutput:=<![CDATA[init
limit
step
9]]>).
            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size      220 (0xdc)
  .maxstack  3
  .locals init (Decimal? V_0,
  Decimal? V_1,
  Boolean V_2,
  Decimal? V_3, //x
  Decimal? V_4)
  IL_0000:  call       "Function MyClass1.Init() As Integer"
  IL_0005:  newobj     "Sub Decimal..ctor(Integer)"
  IL_000a:  newobj     "Sub Decimal?..ctor(Decimal)"
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Function MyClass1.Limit() As Integer"
  IL_0016:  newobj     "Sub Decimal..ctor(Integer)"
  IL_001b:  call       "Sub Decimal?..ctor(Decimal)"
  IL_0020:  call       "Function MyClass1.Step() As Decimal?"
  IL_0025:  stloc.1
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_002d:  brfalse.s  IL_0048
  IL_002f:  ldloca.s   V_1
  IL_0031:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_0036:  ldsfld     "Decimal.Zero As Decimal"
  IL_003b:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_0040:  ldc.i4.0
  IL_0041:  clt
  IL_0043:  ldc.i4.0
  IL_0044:  ceq
  IL_0046:  br.s       IL_0049
  IL_0048:  ldc.i4.0
  IL_0049:  stloc.2
  IL_004a:  stloc.3
  IL_004b:  br.s       IL_008e
  IL_004d:  ldloc.3
  IL_004e:  box        "Decimal?"
  IL_0053:  call       "Sub System.Console.Write(Object)"
  IL_0058:  ldloca.s   V_1
  IL_005a:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_005f:  ldloca.s   V_3
  IL_0061:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_0066:  and
  IL_0067:  brtrue.s   IL_0075
  IL_0069:  ldloca.s   V_4
  IL_006b:  initobj    "Decimal?"
  IL_0071:  ldloc.s    V_4
  IL_0073:  br.s       IL_008d
  IL_0075:  ldloca.s   V_3
  IL_0077:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_007c:  ldloca.s   V_1
  IL_007e:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_0083:  call       "Function Decimal.Add(Decimal, Decimal) As Decimal"
  IL_0088:  newobj     "Sub Decimal?..ctor(Decimal)"
  IL_008d:  stloc.3
  IL_008e:  ldloca.s   V_0
  IL_0090:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_0095:  ldloca.s   V_3
  IL_0097:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_009c:  and
  IL_009d:  brfalse.s  IL_00db
  IL_009f:  ldloc.2
  IL_00a0:  brtrue.s   IL_00bd
  IL_00a2:  ldloca.s   V_3
  IL_00a4:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_00a9:  ldloca.s   V_0
  IL_00ab:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_00b0:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_00b5:  ldc.i4.0
  IL_00b6:  clt
  IL_00b8:  ldc.i4.0
  IL_00b9:  ceq
  IL_00bb:  br.s       IL_00d6
  IL_00bd:  ldloca.s   V_3
  IL_00bf:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_00c4:  ldloca.s   V_0
  IL_00c6:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_00cb:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_00d0:  ldc.i4.0
  IL_00d1:  cgt
  IL_00d3:  ldc.i4.0
  IL_00d4:  ceq
  IL_00d6:  brtrue     IL_004d
  IL_00db:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub LiftedForCombinationsOneToThree()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Option Strict On
Imports System

Public Class MyClass1
    Public Shared Sub Main()
        ' Step 1
        For x As Integer? = One() To Three() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = One() To Null() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To Three() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        ' Step -1

        For x As Integer? = One() To Three() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = One() To Null() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To Three() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        ' Step Null

        For x As Integer? = One() To Three() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = One() To Null() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To Three() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

    End Sub

    Private Shared Function One() As Integer?
        Console.Write("one ")
        Return 1
    End Function

    Private Shared Function MOne() As Integer?
        Console.Write("-one ")
        Return -1
    End Function

    Private Shared Function Three() As Integer?
        Console.Write("three ")
        Return 3
    End Function

    Private Shared Function Null() As Integer?
        Console.Write("null ")
        Return Nothing
    End Function

End Class


                    </file>
                </compilation>, expectedOutput:=<![CDATA[one three one 123
one null one 
null three one 
one three -one 
one null -one 
null three -one 
one three null 
one null null 
null three null 
]]>)
        End Sub

        <Fact()>
        Public Sub LiftedForCombinationsTreeToOne()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">

Option Strict On
Imports System

Public Class MyClass1
    Public Shared Sub Main()
        ' Step 1
        For x As Integer? = Three() To One() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Three() To Null() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To One() Step One()
            Console.Write(x)
        Next
        Console.WriteLine()

        ' Step -1

        For x As Integer? = Three() To One() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Three() To Null() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To One() Step MOne()
            Console.Write(x)
        Next
        Console.WriteLine()

        ' Step Null

        For x As Integer? = Three() To One() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Three() To Null() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

        For x As Integer? = Null() To One() Step Null()
            Console.Write(x)
        Next
        Console.WriteLine()

    End Sub

    Private Shared Function One() As Integer?
        Console.Write("one ")
        Return 1
    End Function

    Private Shared Function MOne() As Integer?
        Console.Write("-one ")
        Return -1
    End Function

    Private Shared Function Three() As Integer?
        Console.Write("three ")
        Return 3
    End Function

    Private Shared Function Null() As Integer?
        Console.Write("null ")
        Return Nothing
    End Function

End Class



                    </file>
                </compilation>, expectedOutput:=<![CDATA[three one one 
three null one 
null one one 
three one -one 321
three null -one 
null one -one 
three one null 3
three null null 
null one null 
]]>)
        End Sub

        <Fact()>
        Public Sub LiftedStringConversion()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb">
Imports System

Class MyClass1
    Shared Sub Main()
        Dim x As SByte? = 42
        Dim y As String = x
        x = y

        Console.Write(x)
    End Sub
End Class

                    </file>
                </compilation>, expectedOutput:="42").
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (SByte? V_0, //x
                String V_1) //y
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.s   42
  IL_0004:  call       "Sub SByte?..ctor(SByte)"
  IL_0009:  ldloca.s   V_0
  IL_000b:  call       "Function SByte?.get_Value() As SByte"
  IL_0010:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String"
  IL_0015:  stloc.1
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldloc.1
  IL_0019:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToSByte(String) As SByte"
  IL_001e:  call       "Sub SByte?..ctor(SByte)"
  IL_0023:  ldloc.0
  IL_0024:  box        "SByte?"
  IL_0029:  call       "Sub System.Console.Write(Object)"
  IL_002e:  ret
}
                ]]>)
        End Sub

#End Region

        <Fact()>
        Public Sub Regress14397()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module Module1
    Const a As Integer = If(True, 1, 2)
    Sub Main()
        Dim s As String
        Dim inull As Integer? = Nothing
        s = " :catenation right to integer?(null): " &amp; inull
        Console.WriteLine(s)
        s = inull &amp; " :catenation left to integer?(null): "
        Console.WriteLine(s)
        s = inull &amp; inull
        Console.WriteLine(" :catenation Integer?(null) &amp; Integer?(null):  " &amp; s)
    End Sub
End Module


    </file>
</compilation>,
expectedOutput:=<![CDATA[:catenation right to integer?(null): 
 :catenation left to integer?(null): 
 :catenation Integer?(null) & Integer?(null): ]]>)
        End Sub

        ''' <summary>
        ''' same as Dev11
        '''    implicit: int  --> int?
        '''    explicit: int? --> int
        ''' </summary>
        ''' <remarks></remarks>
        <Fact, WorkItem(545166, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545166")>
        Public Sub Op_ExplicitImplicitOnNullable()
            CompileAndVerify(
                <compilation>
                    <file name="nullableOp.vb">
Imports System

Module MyClass1
    Sub Main()
        Dim x = (Integer?).op_Implicit(1)
            x = (Integer?).op_Explicit(2)
        Dim y = (Nullable(Of Integer)).op_Implicit(1)
            y = (Nullable(Of Integer)).op_Explicit(2)
    End Sub
End Module
                    </file>
                </compilation>).
                            VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Function Integer?.op_Implicit(Integer) As Integer?"
  IL_0006:  pop
  IL_0007:  ldc.i4.2
  IL_0008:  newobj     "Sub Integer?..ctor(Integer)"
  IL_000d:  call       "Function Integer?.op_Explicit(Integer?) As Integer"
  IL_0012:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0017:  pop
  IL_0018:  ldc.i4.1
  IL_0019:  call       "Function Integer?.op_Implicit(Integer) As Integer?"
  IL_001e:  pop
  IL_001f:  ldc.i4.2
  IL_0020:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0025:  call       "Function Integer?.op_Explicit(Integer?) As Integer"
  IL_002a:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002f:  pop
  IL_0030:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub DecimalConst()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Public Class NullableTest
    Shared NULL As Decimal? = Nothing

    Public Shared Sub EqualEqual()
        Test.Eval(CType(1D, Decimal?) is Nothing, False)
        Test.Eval(CType(1D, Decimal?) = NULL, False)
        Test.Eval(CType(0, Decimal?) = NULL, False)
    End Sub
End Class

Public Class Test
    Public Shared Sub Eval(obj1 As Object, obj2 As Object)
    End Sub
End Class

    </file>
</compilation>).
VerifyIL("NullableTest.EqualEqual",
            <![CDATA[
{
  // Code size      152 (0x98)
  .maxstack  2
  .locals init (Decimal? V_0,
                Boolean? V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  box        "Boolean"
  IL_0006:  ldc.i4.0
  IL_0007:  box        "Boolean"
  IL_000c:  call       "Sub Test.Eval(Object, Object)"
  IL_0011:  ldsfld     "NullableTest.NULL As Decimal?"
  IL_0016:  stloc.0
  IL_0017:  ldloca.s   V_0
  IL_0019:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_001e:  brtrue.s   IL_002b
  IL_0020:  ldloca.s   V_1
  IL_0022:  initobj    "Boolean?"
  IL_0028:  ldloc.1
  IL_0029:  br.s       IL_0044
  IL_002b:  ldsfld     "Decimal.One As Decimal"
  IL_0030:  ldloca.s   V_0
  IL_0032:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_0037:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_003c:  ldc.i4.0
  IL_003d:  ceq
  IL_003f:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0044:  box        "Boolean?"
  IL_0049:  ldc.i4.0
  IL_004a:  box        "Boolean"
  IL_004f:  call       "Sub Test.Eval(Object, Object)"
  IL_0054:  ldsfld     "NullableTest.NULL As Decimal?"
  IL_0059:  stloc.0
  IL_005a:  ldloca.s   V_0
  IL_005c:  call       "Function Decimal?.get_HasValue() As Boolean"
  IL_0061:  brtrue.s   IL_006e
  IL_0063:  ldloca.s   V_1
  IL_0065:  initobj    "Boolean?"
  IL_006b:  ldloc.1
  IL_006c:  br.s       IL_0087
  IL_006e:  ldsfld     "Decimal.Zero As Decimal"
  IL_0073:  ldloca.s   V_0
  IL_0075:  call       "Function Decimal?.GetValueOrDefault() As Decimal"
  IL_007a:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_007f:  ldc.i4.0
  IL_0080:  ceq
  IL_0082:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0087:  box        "Boolean?"
  IL_008c:  ldc.i4.0
  IL_008d:  box        "Boolean"
  IL_0092:  call       "Sub Test.Eval(Object, Object)"
  IL_0097:  ret
}
                ]]>)

        End Sub

        <Fact>
        Public Sub LiftedToIntPtrConversion()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module M
    Sub Main()
        Console.WriteLine(CType(M(Nothing), IntPtr?))
        Console.WriteLine(CType(M(42), IntPtr?))
    End Sub

    Function M(p as Integer?) As Integer?
        Return p
    End Function
End Module

    </file>
</compilation>

            Dim expectedOutput = "" + vbCrLf + "42"
            CompileAndVerify(source, expectedOutput)
        End Sub

        <Fact()>
        Public Sub SubtractFromZero()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Module Module1
  Sub Main()
    Dim a As Integer? = 1
    Dim b As Integer? = 0 - a
    Console.WriteLine(String.Format("a: {0}, b: {1}", a, b))

    Dim a1 As Integer? = 1
    Dim b1 As Integer? = a - 0
    Console.WriteLine(String.Format("a1: {0}, b1: {1}", a1, b1))

 End Sub
End Module

    </file>
</compilation>, expectedOutput:="a: 1, b: -1
a1: 1, b1: 1").
VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size      144 (0x90)
  .maxstack  3
  .locals init (Integer? V_0, //a
                Integer? V_1, //b
                Integer? V_2, //a1
                Integer? V_3, //b1
                Integer? V_4,
                Integer? V_5)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       "Sub Integer?..ctor(Integer)"
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_000f:  brtrue.s   IL_001d
  IL_0011:  ldloca.s   V_4
  IL_0013:  initobj    "Integer?"
  IL_0019:  ldloc.s    V_4
  IL_001b:  br.s       IL_002b
  IL_001d:  ldc.i4.0
  IL_001e:  ldloca.s   V_0
  IL_0020:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_0025:  sub.ovf
  IL_0026:  newobj     "Sub Integer?..ctor(Integer)"
  IL_002b:  stloc.1
  IL_002c:  ldstr      "a: {0}, b: {1}"
  IL_0031:  ldloc.0
  IL_0032:  box        "Integer?"
  IL_0037:  ldloc.1
  IL_0038:  box        "Integer?"
  IL_003d:  call       "Function String.Format(String, Object, Object) As String"
  IL_0042:  call       "Sub System.Console.WriteLine(String)"
  IL_0047:  ldloca.s   V_2
  IL_0049:  ldc.i4.1
  IL_004a:  call       "Sub Integer?..ctor(Integer)"
  IL_004f:  ldloc.0
  IL_0050:  stloc.s    V_4
  IL_0052:  ldloca.s   V_4
  IL_0054:  call       "Function Integer?.get_HasValue() As Boolean"
  IL_0059:  brtrue.s   IL_0067
  IL_005b:  ldloca.s   V_5
  IL_005d:  initobj    "Integer?"
  IL_0063:  ldloc.s    V_5
  IL_0065:  br.s       IL_0073
  IL_0067:  ldloca.s   V_4
  IL_0069:  call       "Function Integer?.GetValueOrDefault() As Integer"
  IL_006e:  newobj     "Sub Integer?..ctor(Integer)"
  IL_0073:  stloc.3
  IL_0074:  ldstr      "a1: {0}, b1: {1}"
  IL_0079:  ldloc.2
  IL_007a:  box        "Integer?"
  IL_007f:  ldloc.3
  IL_0080:  box        "Integer?"
  IL_0085:  call       "Function String.Format(String, Object, Object) As String"
  IL_008a:  call       "Sub System.Console.WriteLine(String)"
  IL_008f:  ret
}
                ]]>)

        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_01()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Class C

    Public shared Sub Main()
	    Test1(Nothing, new C())
	    Test2(Nothing, new C())
        Test3(Nothing, new C())
	    Test4(Nothing, new C())
	    Test5(Nothing, new C())
    End Sub

    Public Shared Sub Test1(x as C, y As C)
        System.Console.WriteLine("->Test1")
	    If GetBool3(x) = True AndAlso y.GetBool2()
                System.Console.WriteLine("In If")
	    End If
        System.Console.WriteLine("<-Test1")
    End Sub

    Public Shared Sub Test2(x as C, y As C)
        System.Console.WriteLine("->Test2")
	    If x?.GetBool1() = True AndAlso y.GetBool2()
                System.Console.WriteLine("In If")
	    End If
        System.Console.WriteLine("<-Test2")
    End Sub

    Public Shared Sub Test3(x as C, y As C)
        System.Console.WriteLine("->Test3")
	    Dim z = GetBool3(x) = True AndAlso y.GetBool2()
	    If z
                System.Console.WriteLine("In If")
	    End If
        System.Console.WriteLine("<-Test3")
    End Sub

    Public Shared Sub Test4(x as C, y As C)
        System.Console.WriteLine("->Test4")
	    Dim z = x?.GetBool1() = True AndAlso y.GetBool2()
	    If z
                System.Console.WriteLine("In If")
	    End If
        System.Console.WriteLine("<-Test4")
    End Sub

    Public Shared Sub Test5(x as C, y As C)
        System.Console.WriteLine("->Test5")
	    If GetBool3(x) AndAlso y.GetBool2()
                System.Console.WriteLine("In If")
	    End If
        System.Console.WriteLine("<-Test5")
    End Sub

    Function GetBool1() As Boolean
        Return True
    End Function
    Function GetBool2() As Boolean
        System.Console.WriteLine("GetBool2")
        Return True
    End Function
    Shared Function GetBool3(x as C) As Boolean?
	if x Is Nothing
             Return Nothing
	End If

        Return True
    End Function
End Class
                    ]]></file>
                </compilation>, expectedOutput:=
            <![CDATA[
->Test1
GetBool2
<-Test1
->Test2
GetBool2
<-Test2
->Test3
GetBool2
<-Test3
->Test4
GetBool2
<-Test4
->Test5
GetBool2
<-Test5
]]>
            )
        End Sub

        <ConditionalFact(GetType(DesktopClrOnly))>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_02()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Module Module1

    Sub Main()
        Dim x = From y In New C()
                Where y.GetBool3() = True AndAlso y.GetBool2()
                Where y.GetBool3() = True OrElse y.GetBool2()

    End Sub

End Module

Class C
    Function [Select](selector As System.Func(Of C, C)) As C
        Return Me
    End Function

    Function Where(filter As System.Linq.Expressions.Expression(Of System.Func(Of C, Boolean))) As C
        System.Console.WriteLine(filter.ToString())
        Return Me
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function
End Class
                    ]]></file>
                </compilation>, expectedOutput:=
            <![CDATA[
y => (((y.GetBool3() == Convert(True)) AndAlso Convert(y.GetBool2())) ?? False)
y => (((y.GetBool3() == Convert(True)) OrElse Convert(y.GetBool2())) ?? False)
]]>
            )
        End Sub

        <ConditionalFact(GetType(DesktopClrOnly))>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_03()
            CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
        Dim x As System.Linq.Expressions.Expression(Of System.Func(Of Boolean))
        x = Function() If(GetBool3() = True AndAlso GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() = True OrElse GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() AndAlso New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() OrElse New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() AndAlso GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() OrElse GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() AndAlso GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(New Boolean?() OrElse GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool2() AndAlso New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool2() OrElse New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() AndAlso New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() OrElse New Boolean?(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() AndAlso GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() OrElse GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() OrElse GetBool3() OrElse GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If((GetBool3() OrElse GetBool3()) OrElse GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() OrElse (GetBool3() OrElse GetBool3()), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If((GetBool3() OrElse GetBool3()) OrElse (GetBool3() OrElse GetBool3()), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If((GetBool3() OrElse GetBool3()) OrElse GetBool3() OrElse GetBool3(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If(GetBool3() AndAlso GetBool2() AndAlso GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
        x = Function() If((GetBool3() AndAlso GetBool2()) AndAlso GetBool2(), True, False)
        System.Console.WriteLine(x.ToString())
    End Sub

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>, expectedOutput:=
            <![CDATA[
() => IIF((((GetBool3() == Convert(True)) AndAlso Convert(GetBool2())) ?? False), True, False)
() => IIF((((GetBool3() == Convert(True)) ?? False) OrElse GetBool2()), True, False)
() => IIF(((new Nullable`1() AndAlso new Nullable`1()) ?? False), True, False)
() => IIF(((new Nullable`1() ?? False) OrElse (new Nullable`1() ?? False)), True, False)
() => IIF(((new Nullable`1() AndAlso Convert(GetBool2())) ?? False), True, False)
() => IIF(((new Nullable`1() ?? False) OrElse GetBool2()), True, False)
() => IIF(((new Nullable`1() AndAlso GetBool3()) ?? False), True, False)
() => IIF(((new Nullable`1() ?? False) OrElse (GetBool3() ?? False)), True, False)
() => IIF((GetBool2() AndAlso (new Nullable`1() ?? False)), True, False)
() => IIF((GetBool2() OrElse (new Nullable`1() ?? False)), True, False)
() => IIF(((GetBool3() AndAlso new Nullable`1()) ?? False), True, False)
() => IIF(((GetBool3() ?? False) OrElse (new Nullable`1() ?? False)), True, False)
() => IIF(((GetBool3() AndAlso GetBool3()) ?? False), True, False)
() => IIF(((GetBool3() ?? False) OrElse (GetBool3() ?? False)), True, False)
() => IIF((((GetBool3() ?? False) OrElse (GetBool3() ?? False)) OrElse (GetBool3() ?? False)), True, False)
() => IIF((((GetBool3() ?? False) OrElse (GetBool3() ?? False)) OrElse (GetBool3() ?? False)), True, False)
() => IIF(((GetBool3() ?? False) OrElse ((GetBool3() ?? False) OrElse (GetBool3() ?? False))), True, False)
() => IIF((((GetBool3() ?? False) OrElse (GetBool3() ?? False)) OrElse ((GetBool3() ?? False) OrElse (GetBool3() ?? False))), True, False)
() => IIF(((((GetBool3() ?? False) OrElse (GetBool3() ?? False)) OrElse (GetBool3() ?? False)) OrElse (GetBool3() ?? False)), True, False)
() => IIF((((GetBool3() AndAlso Convert(GetBool2())) AndAlso Convert(GetBool2())) ?? False), True, False)
() => IIF((((GetBool3() AndAlso Convert(GetBool2())) AndAlso Convert(GetBool2())) ?? False), True, False)
]]>
            )
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_04()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() AndAlso GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000d:  brfalse.s  IL_0018
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0016:  brfalse.s  IL_002c
  IL_0018:  call       "Function Module1.GetBool2() As Boolean"
  IL_001d:  brfalse.s  IL_002c
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0026:  brfalse.s  IL_002c
  IL_0028:  ldc.i4.2
  IL_0029:  stloc.0
  IL_002a:  br.s       IL_002e
  IL_002c:  ldc.i4.3
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_05()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() OrElse GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_0016
  IL_000f:  call       "Function Module1.GetBool2() As Boolean"
  IL_0014:  brfalse.s  IL_001a
  IL_0016:  ldc.i4.2
  IL_0017:  stloc.0
  IL_0018:  br.s       IL_001c
  IL_001a:  ldc.i4.3
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_06()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool2() AndAlso GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
   // Code size       30 (0x1e)
   .maxstack  1
   .locals init (Integer V_0, //Test1
                 Boolean? V_1)
   IL_0000:  call       "Function Module1.GetBool2() As Boolean"
   IL_0005:  brfalse.s  IL_001a
   IL_0007:  call       "Function Module1.GetBool3() As Boolean?"
   IL_000c:  stloc.1
   IL_000d:  ldloca.s   V_1
   IL_000f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
   IL_0014:  brfalse.s  IL_001a
   IL_0016:  ldc.i4.2
   IL_0017:  stloc.0
   IL_0018:  br.s       IL_001c
   IL_001a:  ldc.i4.3
   IL_001b:  stloc.0
   IL_001c:  ldloc.0
   IL_001d:  ret
 }
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_07()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool2() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  brtrue.s   IL_0016
  IL_0007:  call       "Function Module1.GetBool3() As Boolean?"
  IL_000c:  stloc.1
  IL_000d:  ldloca.s   V_1
  IL_000f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0014:  brfalse.s  IL_001a
  IL_0016:  ldc.i4.2
  IL_0017:  stloc.0
  IL_0018:  br.s       IL_001c
  IL_001a:  ldc.i4.3
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_08()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() AndAlso GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  pop
  IL_0006:  ldc.i4.3
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_09()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() OrElse GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  brfalse.s  IL_000b
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  br.s       IL_000d
  IL_000b:  ldc.i4.3
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_10()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() AndAlso GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  pop
  IL_0006:  ldc.i4.3
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_11()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brfalse.s  IL_0013
  IL_000f:  ldc.i4.2
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_0015
  IL_0013:  ldc.i4.3
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_12()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() AndAlso New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  pop
  IL_0006:  ldc.i4.3
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_13()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() OrElse New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brfalse.s  IL_0013
  IL_000f:  ldc.i4.2
  IL_0010:  stloc.0
  IL_0011:  br.s       IL_0015
  IL_0013:  ldc.i4.3
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_14()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool2() AndAlso New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  pop
  IL_0006:  ldc.i4.3
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_15()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool2() OrElse New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  brfalse.s  IL_000b
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  br.s       IL_000d
  IL_000b:  ldc.i4.3
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_16()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() AndAlso New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_17()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If New Boolean?() OrElse New Boolean?() Then
            Return 2
        Else
            Return 3
        End If
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (Integer V_0) //Test1
  IL_0000:  ldc.i4.3
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_18()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() AndAlso GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1,
                Boolean? V_2)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000d:  brfalse.s  IL_0018
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0016:  brfalse.s  IL_0034
  IL_0018:  call       "Function Module1.GetBool3() As Boolean?"
  IL_001d:  stloc.2
  IL_001e:  ldloca.s   V_2
  IL_0020:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0025:  brfalse.s  IL_0034
  IL_0027:  ldloca.s   V_1
  IL_0029:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_002e:  brfalse.s  IL_0034
  IL_0030:  ldc.i4.2
  IL_0031:  stloc.0
  IL_0032:  br.s       IL_0036
  IL_0034:  ldc.i4.3
  IL_0035:  stloc.0
  IL_0036:  ldloc.0
  IL_0037:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_19()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)
            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_001e
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brfalse.s  IL_0022
  IL_001e:  ldc.i4.2
  IL_001f:  stloc.0
  IL_0020:  br.s       IL_0024
  IL_0022:  ldc.i4.3
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_20()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool2() AndAlso (GetBool2() AndAlso GetBool3()) Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool2() As Boolean"
  IL_0005:  brfalse.s  IL_0021
  IL_0007:  call       "Function Module1.GetBool2() As Boolean"
  IL_000c:  brfalse.s  IL_0021
  IL_000e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0013:  stloc.1
  IL_0014:  ldloca.s   V_1
  IL_0016:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001b:  brfalse.s  IL_0021
  IL_001d:  ldc.i4.2
  IL_001e:  stloc.0
  IL_001f:  br.s       IL_0023
  IL_0021:  ldc.i4.3
  IL_0022:  stloc.0
  IL_0023:  ldloc.0
  IL_0024:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_21()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() OrElse (GetBool3() OrElse GetBool3()) Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_002d
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brtrue.s   IL_002d
  IL_001e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002b:  brfalse.s  IL_0031
  IL_002d:  ldc.i4.2
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0033
  IL_0031:  ldc.i4.3
  IL_0032:  stloc.0
  IL_0033:  ldloc.0
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_22()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() OrElse GetBool3()) AndAlso GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size      134 (0x86)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1,
                Boolean? V_2,
                Boolean? V_3,
                Boolean? V_4)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  dup
  IL_0006:  stloc.2
  IL_0007:  stloc.s    V_4
  IL_0009:  ldloca.s   V_4
  IL_000b:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0010:  brfalse.s  IL_001b
  IL_0012:  ldloca.s   V_2
  IL_0014:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0019:  brtrue.s   IL_004d
  IL_001b:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0020:  dup
  IL_0021:  stloc.3
  IL_0022:  stloc.s    V_4
  IL_0024:  ldloca.s   V_4
  IL_0026:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_002b:  brtrue.s   IL_0039
  IL_002d:  ldloca.s   V_4
  IL_002f:  initobj    "Boolean?"
  IL_0035:  ldloc.s    V_4
  IL_0037:  br.s       IL_0053
  IL_0039:  ldloca.s   V_3
  IL_003b:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0040:  brtrue.s   IL_0045
  IL_0042:  ldloc.2
  IL_0043:  br.s       IL_0053
  IL_0045:  ldc.i4.1
  IL_0046:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_004b:  br.s       IL_0053
  IL_004d:  ldc.i4.1
  IL_004e:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0053:  stloc.1
  IL_0054:  ldloca.s   V_1
  IL_0056:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_005b:  brfalse.s  IL_0066
  IL_005d:  ldloca.s   V_1
  IL_005f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0064:  brfalse.s  IL_0082
  IL_0066:  call       "Function Module1.GetBool3() As Boolean?"
  IL_006b:  stloc.3
  IL_006c:  ldloca.s   V_3
  IL_006e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0073:  brfalse.s  IL_0082
  IL_0075:  ldloca.s   V_1
  IL_0077:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_007c:  brfalse.s  IL_0082
  IL_007e:  ldc.i4.2
  IL_007f:  stloc.0
  IL_0080:  br.s       IL_0084
  IL_0082:  ldc.i4.3
  IL_0083:  stloc.0
  IL_0084:  ldloc.0
  IL_0085:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_23()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() OrElse GetBool3()) AndAlso GetBool3() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size      149 (0x95)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1,
                Boolean? V_2,
                Boolean? V_3,
                Boolean? V_4)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  dup
  IL_0006:  stloc.2
  IL_0007:  stloc.s    V_4
  IL_0009:  ldloca.s   V_4
  IL_000b:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0010:  brfalse.s  IL_001b
  IL_0012:  ldloca.s   V_2
  IL_0014:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0019:  brtrue.s   IL_004d
  IL_001b:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0020:  dup
  IL_0021:  stloc.3
  IL_0022:  stloc.s    V_4
  IL_0024:  ldloca.s   V_4
  IL_0026:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_002b:  brtrue.s   IL_0039
  IL_002d:  ldloca.s   V_4
  IL_002f:  initobj    "Boolean?"
  IL_0035:  ldloc.s    V_4
  IL_0037:  br.s       IL_0053
  IL_0039:  ldloca.s   V_3
  IL_003b:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0040:  brtrue.s   IL_0045
  IL_0042:  ldloc.2
  IL_0043:  br.s       IL_0053
  IL_0045:  ldc.i4.1
  IL_0046:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_004b:  br.s       IL_0053
  IL_004d:  ldc.i4.1
  IL_004e:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0053:  stloc.1
  IL_0054:  ldloca.s   V_1
  IL_0056:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_005b:  brfalse.s  IL_0066
  IL_005d:  ldloca.s   V_1
  IL_005f:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0064:  brfalse.s  IL_007e
  IL_0066:  call       "Function Module1.GetBool3() As Boolean?"
  IL_006b:  stloc.3
  IL_006c:  ldloca.s   V_3
  IL_006e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0073:  brfalse.s  IL_007e
  IL_0075:  ldloca.s   V_1
  IL_0077:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_007c:  brtrue.s   IL_008d
  IL_007e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0083:  stloc.1
  IL_0084:  ldloca.s   V_1
  IL_0086:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_008b:  brfalse.s  IL_0091
  IL_008d:  ldc.i4.2
  IL_008e:  stloc.0
  IL_008f:  br.s       IL_0093
  IL_0091:  ldc.i4.3
  IL_0092:  stloc.0
  IL_0093:  ldloc.0
  IL_0094:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_24()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() OrElse GetBool3()) OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_002d
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brtrue.s   IL_002d
  IL_001e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002b:  brfalse.s  IL_0031
  IL_002d:  ldc.i4.2
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0033
  IL_0031:  ldc.i4.3
  IL_0032:  stloc.0
  IL_0033:  ldloc.0
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_25()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() OrElse GetBool3() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_002d
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brtrue.s   IL_002d
  IL_001e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002b:  brfalse.s  IL_0031
  IL_002d:  ldc.i4.2
  IL_002e:  stloc.0
  IL_002f:  br.s       IL_0033
  IL_0031:  ldc.i4.3
  IL_0032:  stloc.0
  IL_0033:  ldloc.0
  IL_0034:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_26()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() AndAlso GetBool2()) AndAlso GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1,
                Boolean? V_2,
                Boolean? V_3)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  dup
  IL_0006:  stloc.2
  IL_0007:  stloc.3
  IL_0008:  ldloca.s   V_3
  IL_000a:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000f:  brfalse.s  IL_001a
  IL_0011:  ldloca.s   V_2
  IL_0013:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0018:  brfalse.s  IL_002c
  IL_001a:  call       "Function Module1.GetBool2() As Boolean"
  IL_001f:  brtrue.s   IL_0029
  IL_0021:  ldc.i4.0
  IL_0022:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0027:  br.s       IL_0032
  IL_0029:  ldloc.2
  IL_002a:  br.s       IL_0032
  IL_002c:  ldc.i4.0
  IL_002d:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_003a:  brfalse.s  IL_0045
  IL_003c:  ldloca.s   V_1
  IL_003e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0043:  brfalse.s  IL_0059
  IL_0045:  call       "Function Module1.GetBool2() As Boolean"
  IL_004a:  brfalse.s  IL_0059
  IL_004c:  ldloca.s   V_1
  IL_004e:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0053:  brfalse.s  IL_0059
  IL_0055:  ldc.i4.2
  IL_0056:  stloc.0
  IL_0057:  br.s       IL_005b
  IL_0059:  ldc.i4.3
  IL_005a:  stloc.0
  IL_005b:  ldloc.0
  IL_005c:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_27()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool3() AndAlso GetBool2() AndAlso GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool2() As Boolean
        Return True
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       93 (0x5d)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1,
                Boolean? V_2,
                Boolean? V_3)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  dup
  IL_0006:  stloc.2
  IL_0007:  stloc.3
  IL_0008:  ldloca.s   V_3
  IL_000a:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000f:  brfalse.s  IL_001a
  IL_0011:  ldloca.s   V_2
  IL_0013:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0018:  brfalse.s  IL_002c
  IL_001a:  call       "Function Module1.GetBool2() As Boolean"
  IL_001f:  brtrue.s   IL_0029
  IL_0021:  ldc.i4.0
  IL_0022:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0027:  br.s       IL_0032
  IL_0029:  ldloc.2
  IL_002a:  br.s       IL_0032
  IL_002c:  ldc.i4.0
  IL_002d:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_003a:  brfalse.s  IL_0045
  IL_003c:  ldloca.s   V_1
  IL_003e:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0043:  brfalse.s  IL_0059
  IL_0045:  call       "Function Module1.GetBool2() As Boolean"
  IL_004a:  brfalse.s  IL_0059
  IL_004c:  ldloca.s   V_1
  IL_004e:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0053:  brfalse.s  IL_0059
  IL_0055:  ldc.i4.2
  IL_0056:  stloc.0
  IL_0057:  br.s       IL_005b
  IL_0059:  ldc.i4.3
  IL_005a:  stloc.0
  IL_005b:  ldloc.0
  IL_005c:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_28()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() OrElse GetBool3()) OrElse (GetBool3() OrElse GetBool3()) Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_003c
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brtrue.s   IL_003c
  IL_001e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002b:  brtrue.s   IL_003c
  IL_002d:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_003a:  brfalse.s  IL_0040
  IL_003c:  ldc.i4.2
  IL_003d:  stloc.0
  IL_003e:  br.s       IL_0042
  IL_0040:  ldc.i4.3
  IL_0041:  stloc.0
  IL_0042:  ldloc.0
  IL_0043:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_29()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetBool3() OrElse GetBool3()) OrElse GetBool3() OrElse GetBool3() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool3() As Boolean?
        Return True
    End Function

End Module
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  1
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_000d:  brtrue.s   IL_003c
  IL_000f:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0014:  stloc.1
  IL_0015:  ldloca.s   V_1
  IL_0017:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_001c:  brtrue.s   IL_003c
  IL_001e:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0023:  stloc.1
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_002b:  brtrue.s   IL_003c
  IL_002d:  call       "Function Module1.GetBool3() As Boolean?"
  IL_0032:  stloc.1
  IL_0033:  ldloca.s   V_1
  IL_0035:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_003a:  brfalse.s  IL_0040
  IL_003c:  ldc.i4.2
  IL_003d:  stloc.0
  IL_003e:  br.s       IL_0042
  IL_0040:  ldc.i4.3
  IL_0041:  stloc.0
  IL_0042:  ldloc.0
  IL_0043:  ret
}
                ]]>)
        End Sub

        <Fact>
        <WorkItem(38305, "https://github.com/dotnet/roslyn/issues/38305")>
        Public Sub BooleanExpression_30()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Strict On

Imports System.Text

Module Test
    Dim builder As New StringBuilder()

    Sub Main()
        Test()

        System.Console.WriteLine("Done")
    End Sub

    Sub Test()
        Dim condition = placeholder

        Dim s1 As String = GetResultString(If(condition, " => True", " => False"))

        Dim d As System.Linq.Expressions.Expression(Of System.Func(Of String)) = Function() If(placeholder, " => True", " => False")
        Dim s2 = GetResultString(d.Compile()())

        Dim s3 = GetResultString(If(placeholder, " => True", " => False"))

        Verify(d, s1, s2, s3)
    End Sub

    Private Function GetResultString(result As String) As String
        builder.Append(result)
        Dim s1 = builder.ToString()
        builder.Clear()
        Return s1
    End Function

    Private Sub Verify(d As System.Linq.Expressions.Expression(Of System.Func(Of String)), s1 As String, s2 As String, s3 As String)
        If s1 <> s3 Then
            System.Console.WriteLine("1 => ")
            System.Console.WriteLine(d.ToString())
            System.Console.WriteLine(s1)
            System.Console.WriteLine(s3)
        End If

        If s2 <> s3 Then
            System.Console.WriteLine("2 => ")
            System.Console.WriteLine(d.ToString())
            System.Console.WriteLine(s2)
            System.Console.WriteLine(s3)
        End If
    End Sub

    Function BooleanTrue(i As Integer) As Boolean
        builder.AppendFormat(" BooleanTrue({0})", i)
        Return True
    End Function
    Function BooleanFalse(i As Integer) As Boolean
        builder.AppendFormat(" BooleanFalse({0})", i)
        Return False
    End Function
    Function NullableTrue(i As Integer) As Boolean?
        builder.AppendFormat(" NullableTrue({0})", i)
        Return True
    End Function
    Function NullableFalse(i As Integer) As Boolean?
        builder.AppendFormat(" NullableFalse({0})", i)
        Return False
    End Function
    Function NullableNull(i As Integer) As Boolean?
        builder.AppendFormat(" NullableNull({0})", i)
        Return Nothing
    End Function
End Module
                    ]]></file>
                </compilation>

            Dim compilation1 = CreateCompilation(source, options:=TestOptions.ReleaseExe)
            Dim tree1 = compilation1.SyntaxTrees.Single()
            Dim placeholders = tree1.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "placeholder").ToArray()
            Assert.Equal(3, placeholders.Length)

            Dim nameInInvocation = tree1.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "Test").Single()
            Dim invocation = nameInInvocation.Ancestors().OfType(Of ExpressionStatementSyntax)().Single()
            Dim testMethod = tree1.GetRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().Where(Function(block) block.SubOrFunctionStatement.Identifier.ValueText = "Test").Single()

            Assert.Equal("Test()", invocation.ToString())

            Dim enumerator = BooleanExpression_30_Helpers.BuildConditions(2, 2).GetEnumerator()

            Const batchSize As Integer = 250
            Dim newBlocks = ArrayBuilder(Of MethodBlockSyntax).GetInstance(batchSize)

            While enumerator.MoveNext

                newBlocks.Clear()
                Do
                    Dim replacement = enumerator.Current
                    Dim newBlock = testMethod.ReplaceNodes(placeholders, Function(n1, n2) replacement)
                    newBlock = newBlock.ReplaceToken(newBlock.SubOrFunctionStatement.Identifier, SyntaxFactory.Identifier("Test" + (newBlocks.Count + 1).ToString()))
                    newBlocks.Add(newBlock)
                Loop While newBlocks.Count < batchSize AndAlso enumerator.MoveNext()

                Dim newRoot = tree1.GetRoot().ReplaceNode(invocation, Enumerable.Range(1, newBlocks.Count).
                                                                          Select(Function(i) invocation.ReplaceToken(nameInInvocation.Identifier,
                                                                                                                     SyntaxFactory.Identifier("Test" + i.ToString()))))

                Dim oldBlock = newRoot.DescendantNodes().OfType(Of MethodBlockSyntax)().Where(Function(block) block.SubOrFunctionStatement.Identifier.ValueText = "Test").Single()
                newRoot = newRoot.ReplaceNode(oldBlock, newBlocks)

                Dim tree2 = newRoot.SyntaxTree

                Dim compilation2 = compilation1.ReplaceSyntaxTree(tree1, tree2)

                CompileAndVerify(compilation2, expectedOutput:="Done")
            End While

            newBlocks.Free()
        End Sub

        Private Class BooleanExpression_30_Helpers

            Class TreeNode
                Public Left As TreeNode
                Public Right As TreeNode
            End Class

            Public Shared Iterator Function BuildConditions(fromOperators As Integer, toOperators As Integer) As IEnumerable(Of ExpressionSyntax)
                For operatorCount = fromOperators To toOperators
                    For Each shape In Shapes(operatorCount)
                        For Each operators In OperatorSets(operatorCount)
                            For Each operands In OperandSets(operatorCount + 1)
                                Yield BuildCondition(shape, operators, operands)
                            Next
                        Next
                    Next
                Next
            End Function

            Public Shared Function BuildCondition(shape As TreeNode, operators As ImmutableList(Of SyntaxKind), operands As ImmutableList(Of ExpressionSyntax)) As ExpressionSyntax
                Dim result = BuildConditionWorker(shape, operators, operands)

                Assert.Empty(operators)
                Assert.Empty(operands)
                Return result
            End Function

            Private Shared Function BuildConditionWorker(shape As TreeNode, ByRef operators As ImmutableList(Of SyntaxKind), ByRef operands As ImmutableList(Of ExpressionSyntax)) As ExpressionSyntax

                If shape Is Nothing Then
                    Dim result = operands(0)
                    operands = operands.RemoveAt(0)
                    Return result
                End If

                Dim left = BuildConditionWorker(shape.Left, operators, operands)

                Dim opKind = operators(0)
                operators = operators.RemoveAt(0)

                Dim right = BuildConditionWorker(shape.Right, operators, operands)

                Return SyntaxFactory.BinaryExpression(
                    If(opKind = SyntaxKind.OrElseKeyword, SyntaxKind.OrElseExpression, SyntaxKind.AndAlsoExpression),
                    left, SyntaxFactory.Token(opKind), right)
            End Function

            ''' <summary>
            ''' Enumerate all possible shapes of binary trees with given amount of nodes in it.
            ''' </summary>
            ''' <param name="count"></param>
            ''' <returns></returns>
            Public Shared Iterator Function Shapes(count As Integer) As IEnumerable(Of TreeNode)
                Select Case (count)
                    Case 0
                        Yield Nothing
                    Case 1
                        Yield New TreeNode()
                    Case Else
                        For i = 0 To count - 1
                            For Each leftTree In Shapes(count - 1 - i)
                                For Each rightTree In Shapes(i)
                                    Yield New TreeNode() With {.Left = leftTree, .Right = rightTree}
                                Next
                            Next
                        Next
                End Select
            End Function

            Shared Iterator Function OperatorSets(count As Integer) As IEnumerable(Of ImmutableList(Of SyntaxKind))
                Select Case (count)
                    Case 0
                        Yield ImmutableList(Of SyntaxKind).Empty
                    Case Else
                        For Each s In OperatorSets(count - 1)
                            Yield s.Add(SyntaxKind.AndAlsoKeyword)
                            Yield s.Add(SyntaxKind.OrElseKeyword)
                        Next
                End Select
            End Function

            Shared Iterator Function OperandSets(count As Integer) As IEnumerable(Of ImmutableList(Of ExpressionSyntax))
                Select Case (count)
                    Case 0
                        Yield ImmutableList(Of ExpressionSyntax).Empty
                    Case Else
                        For Each s In OperandSets(count - 1)
                            ' New Boolean?()
                            Yield s.Add(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BooleanKeyword)))))

                            For Each name In {"BooleanTrue", "BooleanFalse", "NullableTrue", "NullableFalse", "NullableNull"}
                                Yield s.Add(SyntaxFactory.InvocationExpression(
                                  SyntaxFactory.IdentifierName(name),
                                  SyntaxFactory.ArgumentList(
                                      SyntaxFactory.SeparatedList(Of ArgumentSyntax)(
                                          New ArgumentSyntax() {SyntaxFactory.SimpleArgument(
                                              SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                                              SyntaxFactory.IntegerLiteralToken(count.ToString(),
                                                                                                                LiteralBase.Decimal,
                                                                                                                TypeCharacter.None,
                                                                                                                CType(count, ULong))))}))))
                            Next
                        Next
                End Select
            End Function

        End Class

        <Fact()>
        Public Sub BooleanExpression_31()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetC()?.F Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetC() As C"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  br.s       IL_0011
  IL_000c:  ldfld      "C.F As Boolean"
  IL_0011:  brfalse.s  IL_0017
  IL_0013:  ldc.i4.2
  IL_0014:  stloc.0
  IL_0015:  br.s       IL_0019
  IL_0017:  ldc.i4.3
  IL_0018:  stloc.0
  IL_0019:  ldloc.0
  IL_001a:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub BooleanExpression_32()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool1() AndAlso GetC()?.F Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool1() As Boolean
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool1() As Boolean"
  IL_0005:  brfalse.s  IL_001e
  IL_0007:  call       "Function Module1.GetC() As C"
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0013
  IL_000f:  pop
  IL_0010:  ldc.i4.0
  IL_0011:  br.s       IL_0018
  IL_0013:  ldfld      "C.F As Boolean"
  IL_0018:  brfalse.s  IL_001e
  IL_001a:  ldc.i4.2
  IL_001b:  stloc.0
  IL_001c:  br.s       IL_0020
  IL_001e:  ldc.i4.3
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub BooleanExpression_33()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetC()?.F OrElse GetBool1() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool1() As Boolean
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetC() As C"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  br.s       IL_0011
  IL_000c:  ldfld      "C.F As Boolean"
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  call       "Function Module1.GetBool1() As Boolean"
  IL_0018:  brfalse.s  IL_001e
  IL_001a:  ldc.i4.2
  IL_001b:  stloc.0
  IL_001c:  br.s       IL_0020
  IL_001e:  ldc.i4.3
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub BooleanExpression_34()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetBool1() AndAlso GetC()?.F OrElse GetBool2() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool1() As Boolean
        Return Nothing
    End Function

    Function GetBool2() As Boolean
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetBool1() As Boolean"
  IL_0005:  brfalse.s  IL_001a
  IL_0007:  call       "Function Module1.GetC() As C"
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0013
  IL_000f:  pop
  IL_0010:  ldc.i4.0
  IL_0011:  br.s       IL_0018
  IL_0013:  ldfld      "C.F As Boolean"
  IL_0018:  brtrue.s   IL_0021
  IL_001a:  call       "Function Module1.GetBool2() As Boolean"
  IL_001f:  brfalse.s  IL_0025
  IL_0021:  ldc.i4.2
  IL_0022:  stloc.0
  IL_0023:  br.s       IL_0027
  IL_0025:  ldc.i4.3
  IL_0026:  stloc.0
  IL_0027:  ldloc.0
  IL_0028:  ret
}
                ]]>)
        End Sub

        <Fact()>
        Public Sub BooleanExpression_35()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If GetNullable() AndAlso GetC()?.F Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetNullable() As Boolean?
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetNullable() As Boolean?"
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_000d:  brfalse.s  IL_0018
  IL_000f:  ldloca.s   V_1
  IL_0011:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0016:  brfalse.s  IL_0038
  IL_0018:  call       "Function Module1.GetC() As C"
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0024
  IL_0020:  pop
  IL_0021:  ldc.i4.0
  IL_0022:  br.s       IL_0029
  IL_0024:  ldfld      "C.F As Boolean"
  IL_0029:  brfalse.s  IL_0038
  IL_002b:  ldloca.s   V_1
  IL_002d:  call       "Function Boolean?.get_HasValue() As Boolean"
  IL_0032:  brfalse.s  IL_0038
  IL_0034:  ldc.i4.2
  IL_0035:  stloc.0
  IL_0036:  br.s       IL_003a
  IL_0038:  ldc.i4.3
  IL_0039:  stloc.0
  IL_003a:  ldloc.0
  IL_003b:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38306, "https://github.com/dotnet/roslyn/issues/38306")>
        Public Sub BooleanExpression_36()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If (GetC()?.F).GetValueOrDefault() AndAlso GetBool1() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool1() As Boolean
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (Integer V_0, //Test1
                Boolean? V_1)
  IL_0000:  call       "Function Module1.GetC() As C"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0014
  IL_0008:  pop
  IL_0009:  ldloca.s   V_1
  IL_000b:  initobj    "Boolean?"
  IL_0011:  ldloc.1
  IL_0012:  br.s       IL_001e
  IL_0014:  ldfld      "C.F As Boolean"
  IL_0019:  newobj     "Sub Boolean?..ctor(Boolean)"
  IL_001e:  stloc.1
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       "Function Boolean?.GetValueOrDefault() As Boolean"
  IL_0026:  brfalse.s  IL_0033
  IL_0028:  call       "Function Module1.GetBool1() As Boolean"
  IL_002d:  brfalse.s  IL_0033
  IL_002f:  ldc.i4.2
  IL_0030:  stloc.0
  IL_0031:  br.s       IL_0035
  IL_0033:  ldc.i4.3
  IL_0034:  stloc.0
  IL_0035:  ldloc.0
  IL_0036:  ret
}
                ]]>)
        End Sub

        <Fact()>
        <WorkItem(38306, "https://github.com/dotnet/roslyn/issues/38306")>
        Public Sub BooleanExpression_37()
            Dim verifier = CompileAndVerify(
                <compilation>
                    <file name="a.vb"><![CDATA[
Option Explicit On

Module Module1

    Sub Main()
    End Sub

    Function Test1() As Integer
        If If(GetC()?.F, False) AndAlso GetBool1() Then
            Return 2
        Else
            Return 3
        End If
    End Function

    Function GetBool1() As Boolean
        Return Nothing
    End Function

    Function GetC() As C
        Return Nothing
    End Function

End Module

Class C
    Public F As Boolean
End Class
                    ]]></file>
                </compilation>)

            verifier.VerifyIL("Module1.Test1",
            <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (Integer V_0) //Test1
  IL_0000:  call       "Function Module1.GetC() As C"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000c
  IL_0008:  pop
  IL_0009:  ldc.i4.0
  IL_000a:  br.s       IL_0011
  IL_000c:  ldfld      "C.F As Boolean"
  IL_0011:  brfalse.s  IL_001e
  IL_0013:  call       "Function Module1.GetBool1() As Boolean"
  IL_0018:  brfalse.s  IL_001e
  IL_001a:  ldc.i4.2
  IL_001b:  stloc.0
  IL_001c:  br.s       IL_0020
  IL_001e:  ldc.i4.3
  IL_001f:  stloc.0
  IL_0020:  ldloc.0
  IL_0021:  ret
}
                ]]>)
        End Sub

    End Class
End Namespace
