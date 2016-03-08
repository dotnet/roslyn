' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenIterators
        Inherits BasicTestBase

        <Fact>
        <WorkItem(1081584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1081584")>
        Public Sub TestYieldInSelectCase()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic

Module A
    Public Sub Main()
        For Each s In YieldString()
            Console.WriteLine(s)
        Next
    End Sub

    Public Iterator Function YieldString() As IEnumerable(Of String)
      For Each b In { True, False }
          Select Case True
            Case b
              Yield "True"
            Case Not b
              Yield "False"
          End Select
      Next
    End Function
End Module
    </file>
</compilation>, expectedOutput:="True
False").VerifyIL("A.VB$StateMachine_1_YieldString.MoveNext", "
{
  // Code size      220 (0xdc)
  .maxstack  5
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001b,
        IL_007e,
        IL_00b0)
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  dup
  IL_001e:  stloc.0
  IL_001f:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0024:  ldarg.0
  IL_0025:  ldc.i4.2
  IL_0026:  newarr     ""Boolean""
  IL_002b:  dup
  IL_002c:  ldc.i4.0
  IL_002d:  ldc.i4.1
  IL_002e:  stelem.i1
  IL_002f:  stfld      ""A.VB$StateMachine_1_YieldString.$S0 As Boolean()""
  IL_0034:  ldarg.0
  IL_0035:  ldc.i4.0
  IL_0036:  stfld      ""A.VB$StateMachine_1_YieldString.$S3 As Integer""
  IL_003b:  br         IL_00c7
  IL_0040:  ldarg.0
  IL_0041:  ldarg.0
  IL_0042:  ldfld      ""A.VB$StateMachine_1_YieldString.$S0 As Boolean()""
  IL_0047:  ldarg.0
  IL_0048:  ldfld      ""A.VB$StateMachine_1_YieldString.$S3 As Integer""
  IL_004d:  ldelem.u1
  IL_004e:  stfld      ""A.VB$StateMachine_1_YieldString.$VB$ResumableLocal_b$2 As Boolean""
  IL_0053:  ldarg.0
  IL_0054:  ldc.i4.1
  IL_0055:  stfld      ""A.VB$StateMachine_1_YieldString.$S1 As Boolean""
  IL_005a:  ldarg.0
  IL_005b:  ldfld      ""A.VB$StateMachine_1_YieldString.$S1 As Boolean""
  IL_0060:  ldarg.0
  IL_0061:  ldfld      ""A.VB$StateMachine_1_YieldString.$VB$ResumableLocal_b$2 As Boolean""
  IL_0066:  bne.un.s   IL_0089
  IL_0068:  ldarg.0
  IL_0069:  ldstr      ""True""
  IL_006e:  stfld      ""A.VB$StateMachine_1_YieldString.$Current As String""
  IL_0073:  ldarg.0
  IL_0074:  ldc.i4.1
  IL_0075:  dup
  IL_0076:  stloc.0
  IL_0077:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_007c:  ldc.i4.1
  IL_007d:  ret
  IL_007e:  ldarg.0
  IL_007f:  ldc.i4.m1
  IL_0080:  dup
  IL_0081:  stloc.0
  IL_0082:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0087:  br.s       IL_00b9
  IL_0089:  ldarg.0
  IL_008a:  ldfld      ""A.VB$StateMachine_1_YieldString.$S1 As Boolean""
  IL_008f:  ldarg.0
  IL_0090:  ldfld      ""A.VB$StateMachine_1_YieldString.$VB$ResumableLocal_b$2 As Boolean""
  IL_0095:  ldc.i4.0
  IL_0096:  ceq
  IL_0098:  bne.un.s   IL_00b9
  IL_009a:  ldarg.0
  IL_009b:  ldstr      ""False""
  IL_00a0:  stfld      ""A.VB$StateMachine_1_YieldString.$Current As String""
  IL_00a5:  ldarg.0
  IL_00a6:  ldc.i4.2
  IL_00a7:  dup
  IL_00a8:  stloc.0
  IL_00a9:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_00ae:  ldc.i4.1
  IL_00af:  ret
  IL_00b0:  ldarg.0
  IL_00b1:  ldc.i4.m1
  IL_00b2:  dup
  IL_00b3:  stloc.0
  IL_00b4:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_00b9:  ldarg.0
  IL_00ba:  ldarg.0
  IL_00bb:  ldfld      ""A.VB$StateMachine_1_YieldString.$S3 As Integer""
  IL_00c0:  ldc.i4.1
  IL_00c1:  add.ovf
  IL_00c2:  stfld      ""A.VB$StateMachine_1_YieldString.$S3 As Integer""
  IL_00c7:  ldarg.0
  IL_00c8:  ldfld      ""A.VB$StateMachine_1_YieldString.$S3 As Integer""
  IL_00cd:  ldarg.0
  IL_00ce:  ldfld      ""A.VB$StateMachine_1_YieldString.$S0 As Boolean()""
  IL_00d3:  ldlen
  IL_00d4:  conv.i4
  IL_00d5:  blt        IL_0040
  IL_00da:  ldc.i4.0
  IL_00db:  ret
}").VerifyIL("A.VB$StateMachine_1_YieldString.IEnumerable.GetEnumerator", "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""Function A.VB$StateMachine_1_YieldString.GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)""
  IL_0006:  ret
}")
        End Sub

        <Fact>
        Public Sub SingletonIterator()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo
            Console.WriteLine(i)
        Next
    End Sub

    iterator function Foo as IEnumerable(of Integer)
        Yield 42
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_002c
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.s   42
  IL_001c:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.1
  IL_0023:  dup
  IL_0024:  stloc.0
  IL_0025:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_002a:  ldc.i4.1
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0035:  ldc.i4.0
  IL_0036:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Foo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Foo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TwoVarsSameName()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1

    Sub Main()
        For Each i In Foo
            Console.Write(i)
        Next
    End Sub

    Iterator Function Foo() As IEnumerable(Of Integer)
        Dim arr(1) As Integer
        arr(0) = 42

        For Each x In arr
            Yield x
            Yield x
        Next

        For Each x In "abc"
            Yield System.Convert.ToInt32(x)
            Yield System.Convert.ToInt32(x)
        Next

    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="424200979798989999").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size      340 (0x154)
  .maxstack  3
  .locals init (Integer V_0,
                Integer() V_1) //arr
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_0023,
        IL_0072,
        IL_0092,
        IL_0100,
        IL_0125)
  IL_0021:  ldc.i4.0
  IL_0022:  ret
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.m1
  IL_0025:  dup
  IL_0026:  stloc.0
  IL_0027:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_002c:  ldc.i4.2
  IL_002d:  newarr     "Integer"
  IL_0032:  stloc.1
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4.0
  IL_0035:  ldc.i4.s   42
  IL_0037:  stelem.i4
  IL_0038:  ldarg.0
  IL_0039:  ldloc.1
  IL_003a:  stfld      "Module1.VB$StateMachine_1_Foo.$S0 As Integer()"
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.0
  IL_0041:  stfld      "Module1.VB$StateMachine_1_Foo.$S2 As Integer"
  IL_0046:  br.s       IL_00a9
  IL_0048:  ldarg.0
  IL_0049:  ldarg.0
  IL_004a:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As Integer()"
  IL_004f:  ldarg.0
  IL_0050:  ldfld      "Module1.VB$StateMachine_1_Foo.$S2 As Integer"
  IL_0055:  ldelem.i4
  IL_0056:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$1 As Integer"
  IL_005b:  ldarg.0
  IL_005c:  ldarg.0
  IL_005d:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$1 As Integer"
  IL_0062:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.1
  IL_0069:  dup
  IL_006a:  stloc.0
  IL_006b:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0070:  ldc.i4.1
  IL_0071:  ret
  IL_0072:  ldarg.0
  IL_0073:  ldc.i4.m1
  IL_0074:  dup
  IL_0075:  stloc.0
  IL_0076:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_007b:  ldarg.0
  IL_007c:  ldarg.0
  IL_007d:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$1 As Integer"
  IL_0082:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_0087:  ldarg.0
  IL_0088:  ldc.i4.2
  IL_0089:  dup
  IL_008a:  stloc.0
  IL_008b:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0090:  ldc.i4.1
  IL_0091:  ret
  IL_0092:  ldarg.0
  IL_0093:  ldc.i4.m1
  IL_0094:  dup
  IL_0095:  stloc.0
  IL_0096:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_009b:  ldarg.0
  IL_009c:  ldarg.0
  IL_009d:  ldfld      "Module1.VB$StateMachine_1_Foo.$S2 As Integer"
  IL_00a2:  ldc.i4.1
  IL_00a3:  add.ovf
  IL_00a4:  stfld      "Module1.VB$StateMachine_1_Foo.$S2 As Integer"
  IL_00a9:  ldarg.0
  IL_00aa:  ldfld      "Module1.VB$StateMachine_1_Foo.$S2 As Integer"
  IL_00af:  ldarg.0
  IL_00b0:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As Integer()"
  IL_00b5:  ldlen
  IL_00b6:  conv.i4
  IL_00b7:  blt.s      IL_0048
  IL_00b9:  ldarg.0
  IL_00ba:  ldstr      "abc"
  IL_00bf:  stfld      "Module1.VB$StateMachine_1_Foo.$S3 As String"
  IL_00c4:  ldarg.0
  IL_00c5:  ldc.i4.0
  IL_00c6:  stfld      "Module1.VB$StateMachine_1_Foo.$S5 As Integer"
  IL_00cb:  br.s       IL_013c
  IL_00cd:  ldarg.0
  IL_00ce:  ldarg.0
  IL_00cf:  ldfld      "Module1.VB$StateMachine_1_Foo.$S3 As String"
  IL_00d4:  ldarg.0
  IL_00d5:  ldfld      "Module1.VB$StateMachine_1_Foo.$S5 As Integer"
  IL_00da:  callvirt   "Function String.get_Chars(Integer) As Char"
  IL_00df:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$4 As Char"
  IL_00e4:  ldarg.0
  IL_00e5:  ldarg.0
  IL_00e6:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$4 As Char"
  IL_00eb:  call       "Function System.Convert.ToInt32(Char) As Integer"
  IL_00f0:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.3
  IL_00f7:  dup
  IL_00f8:  stloc.0
  IL_00f9:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_00fe:  ldc.i4.1
  IL_00ff:  ret
  IL_0100:  ldarg.0
  IL_0101:  ldc.i4.m1
  IL_0102:  dup
  IL_0103:  stloc.0
  IL_0104:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0109:  ldarg.0
  IL_010a:  ldarg.0
  IL_010b:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$4 As Char"
  IL_0110:  call       "Function System.Convert.ToInt32(Char) As Integer"
  IL_0115:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_011a:  ldarg.0
  IL_011b:  ldc.i4.4
  IL_011c:  dup
  IL_011d:  stloc.0
  IL_011e:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0123:  ldc.i4.1
  IL_0124:  ret
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.m1
  IL_0127:  dup
  IL_0128:  stloc.0

  IL_0129:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_012e:  ldarg.0
  IL_012f:  ldarg.0
  IL_0130:  ldfld      "Module1.VB$StateMachine_1_Foo.$S5 As Integer"
  IL_0135:  ldc.i4.1
  IL_0136:  add.ovf
  IL_0137:  stfld      "Module1.VB$StateMachine_1_Foo.$S5 As Integer"
  IL_013c:  ldarg.0
  IL_013d:  ldfld      "Module1.VB$StateMachine_1_Foo.$S5 As Integer"
  IL_0142:  ldarg.0
  IL_0143:  ldfld      "Module1.VB$StateMachine_1_Foo.$S3 As String"
  IL_0148:  callvirt   "Function String.get_Length() As Integer"
  IL_014d:  blt        IL_00cd
  IL_0152:  ldc.i4.0
  IL_0153:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Foo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Foo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ReadonlyProperty()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        For Each x In p1
            Console.Write(x)
        Next
    End Sub

    Public ReadOnly Iterator Property p1 As IEnumerable(Of Integer)
        Get
            Yield 42
        End Get
    End Property
End Module
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_2_get_p1.MoveNext()", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_2_get_p1.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_002c
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_2_get_p1.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.s   42
  IL_001c:  stfld      "Module1.VB$StateMachine_2_get_p1.$Current As Integer"
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.1
  IL_0023:  dup
  IL_0024:  stloc.0
  IL_0025:  stfld      "Module1.VB$StateMachine_2_get_p1.$State As Integer"
  IL_002a:  ldc.i4.1
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "Module1.VB$StateMachine_2_get_p1.$State As Integer"
  IL_0035:  ldc.i4.0
  IL_0036:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub ReadWriteProperty()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()

        System.Console.Write(save Is Nothing)

        p1 = p1

        For Each x In save
            Console.Write(x)
        Next
    End Sub

    Private save As IEnumerable(Of Integer)

    Public Iterator Property p1 As IEnumerable(Of Integer)
        Get
            Yield 42
        End Get
        Set(value As IEnumerable(Of Integer))
            save = value
        End Set
    End Property
End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True42")
        End Sub

        <Fact>
        Public Sub ParamCapture()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo(42)
            Console.WriteLine(i)
        Next
    End Sub

    iterator function Foo(x as integer) as IEnumerable(of Integer)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0030
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$Local_x As Integer"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0039:  ldc.i4.0
  IL_003a:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub NestedForeach()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo({1,2,3,4,5})
            Console.Write(i)
        Next
    End Sub

    iterator function Foo(x As IEnumerable(of Integer)) as IEnumerable(of Integer)
        For Each i In x
            Yield i
        Next
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="12345").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size      161 (0xa1)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Integer V_2) //i
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001b,
        IL_0024,
        IL_0024)
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  dup
  IL_001e:  stloc.1
  IL_001f:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0024:  nop
  .try
  {
    IL_0025:  ldloc.1
    IL_0026:  ldc.i4.1
    IL_0027:  beq.s      IL_006d
    IL_0029:  ldloc.1
    IL_002a:  ldc.i4.2
    IL_002b:  bne.un.s   IL_003a
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4.m1
    IL_002f:  dup
    IL_0030:  stloc.1
    IL_0031:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0036:  ldc.i4.1
    IL_0037:  stloc.0
    IL_0038:  leave.s    IL_009f
    IL_003a:  ldarg.0
    IL_003b:  ldarg.0
    IL_003c:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$Local_x As System.Collections.Generic.IEnumerable(Of Integer)"
    IL_0041:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0046:  stfld      "Module1.VB$StateMachine_1_Foo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_004b:  br.s       IL_0076
    IL_004d:  ldarg.0
    IL_004e:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0053:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_0058:  stloc.2
    IL_0059:  ldarg.0
    IL_005a:  ldloc.2
    IL_005b:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
    IL_0060:  ldarg.0
    IL_0061:  ldc.i4.1
    IL_0062:  dup
    IL_0063:  stloc.1
    IL_0064:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0069:  ldc.i4.1
    IL_006a:  stloc.0
    IL_006b:  leave.s    IL_009f
    IL_006d:  ldarg.0
    IL_006e:  ldc.i4.m1
    IL_006f:  dup
    IL_0070:  stloc.1
    IL_0071:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0076:  ldarg.0
    IL_0077:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_007c:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_0081:  brtrue.s   IL_004d
    IL_0083:  leave.s    IL_009d
  }
  finally
  {
    IL_0085:  ldloc.1
    IL_0086:  ldc.i4.0
    IL_0087:  bge.s      IL_009c
    IL_0089:  ldarg.0
    IL_008a:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_008f:  brfalse.s  IL_009c
    IL_0091:  ldarg.0
    IL_0092:  ldfld      "Module1.VB$StateMachine_1_Foo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0097:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_009c:  endfinally
  }
  IL_009d:  ldc.i4.0
  IL_009e:  ret
  IL_009f:  ldloc.0
  IL_00a0:  ret

}
]]>).VerifyIL("Module1.VB$StateMachine_1_Foo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Foo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub NestedFor()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo()
            Console.Write(i)
        Next
    End Sub

    iterator function Foo() as IEnumerable(of Integer)
        For i = 1 to 5
            Yield i
        Next
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="12345").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0037
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_i$0 As Integer"
  IL_0020:  ldarg.0
  IL_0021:  ldarg.0
  IL_0022:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_i$0 As Integer"
  IL_0027:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  dup
  IL_003a:  stloc.0
  IL_003b:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0040:  ldarg.0
  IL_0041:  ldarg.0
  IL_0042:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_i$0 As Integer"
  IL_0047:  ldc.i4.1
  IL_0048:  add.ovf
  IL_0049:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_i$0 As Integer"
  IL_004e:  ldarg.0
  IL_004f:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_i$0 As Integer"
  IL_0054:  ldc.i4.5
  IL_0055:  ble.s      IL_0020
  IL_0057:  ldc.i4.0
  IL_0058:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Foo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Foo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub LocalsInDispose()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo()
            Console.Write(i)
        Next
    End Sub

    iterator function Foo() as IEnumerable(of Integer)
        Dim x = 1
        Try
            x += 1
            Yield x
            Exit Function
        Finally
            x += 1
            Dim y = x
            Console.Write(x)
            Console.Write(y.ToString())
        End Try
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="233").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size      168 (0xa8)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Integer V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001b,
        IL_002b,
        IL_002b)
  IL_0019:  ldc.i4.0
  IL_001a:  ret
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.m1
  IL_001d:  dup
  IL_001e:  stloc.1
  IL_001f:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0024:  ldarg.0
  IL_0025:  ldc.i4.1
  IL_0026:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
  IL_002b:  nop
  .try
  {
    IL_002c:  ldloc.1
    IL_002d:  ldc.i4.1
    IL_002e:  beq.s      IL_0068
    IL_0030:  ldloc.1
    IL_0031:  ldc.i4.2
    IL_0032:  bne.un.s   IL_0041
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.m1
    IL_0036:  dup
    IL_0037:  stloc.1
    IL_0038:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_003d:  ldc.i4.1
    IL_003e:  stloc.0
    IL_003f:  leave.s    IL_00a6
    IL_0041:  ldarg.0
    IL_0042:  ldarg.0
    IL_0043:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_0048:  ldc.i4.1
    IL_0049:  add.ovf
    IL_004a:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_004f:  ldarg.0
    IL_0050:  ldarg.0
    IL_0051:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_0056:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0064:  ldc.i4.1
    IL_0065:  stloc.0
    IL_0066:  leave.s    IL_00a6
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.1
    IL_006c:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0071:  leave.s    IL_00a4
  }
  finally
  {
    IL_0073:  ldloc.1
    IL_0074:  ldc.i4.0
    IL_0075:  bge.s      IL_00a3
    IL_0077:  ldarg.0
    IL_0078:  ldarg.0
    IL_0079:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_007e:  ldc.i4.1
    IL_007f:  add.ovf
    IL_0080:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_0085:  ldarg.0
    IL_0086:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_008b:  stloc.2
    IL_008c:  ldarg.0
    IL_008d:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_x$0 As Integer"
    IL_0092:  call       "Sub System.Console.Write(Integer)"
    IL_0097:  ldloca.s   V_2
    IL_0099:  call       "Function Integer.ToString() As String"
    IL_009e:  call       "Sub System.Console.Write(String)"
    IL_00a3:  endfinally
  }
  IL_00a4:  ldc.i4.0
  IL_00a5:  ret
  IL_00a6:  ldloc.0
  IL_00a7:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Foo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Foo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TryCatch()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo()
            Console.Write(i.Message)
        Next
    End Sub

    iterator function Foo() as IEnumerable(of Exception)

        Dim ex As exception = nothing

        Try
            yield New Exception("1")
            Throw New Exception("2")
        Catch ex           
        End Try

        Yield ex

    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="12").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  switch    (
        IL_001f,
        IL_002f,
        IL_002f,
        IL_00a2)
  IL_001d:  ldc.i4.0
  IL_001e:  ret
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4.m1
  IL_0021:  dup
  IL_0022:  stloc.1
  IL_0023:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0028:  ldarg.0
  IL_0029:  ldnull
  IL_002a:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_ex$0 As System.Exception"
  IL_002f:  nop
  .try
  {
    IL_0030:  ldloc.1
    IL_0031:  ldc.i4.1
    IL_0032:  beq.s      IL_0062
    IL_0034:  ldloc.1
    IL_0035:  ldc.i4.2
    IL_0036:  bne.un.s   IL_0045
    IL_0038:  ldarg.0
    IL_0039:  ldc.i4.m1
    IL_003a:  dup
    IL_003b:  stloc.1
    IL_003c:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_0041:  ldc.i4.1
    IL_0042:  stloc.0
    IL_0043:  leave.s    IL_00ad
    IL_0045:  ldarg.0
    IL_0046:  ldstr      "1"
    IL_004b:  newobj     "Sub System.Exception..ctor(String)"
    IL_0050:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As System.Exception"
    IL_0055:  ldarg.0
    IL_0056:  ldc.i4.1
    IL_0057:  dup
    IL_0058:  stloc.1
    IL_0059:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_005e:  ldc.i4.1
    IL_005f:  stloc.0
    IL_0060:  leave.s    IL_00ad
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.m1
    IL_0064:  dup
    IL_0065:  stloc.1
    IL_0066:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
    IL_006b:  ldstr      "2"
    IL_0070:  newobj     "Sub System.Exception..ctor(String)"
    IL_0075:  throw
  }
  catch System.Exception
  {
    IL_0076:  dup
    IL_0077:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_007c:  stloc.2
    IL_007d:  ldarg.0
    IL_007e:  ldloc.2
    IL_007f:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_ex$0 As System.Exception"
    IL_0084:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0089:  leave.s    IL_008b
  }
  IL_008b:  ldarg.0
  IL_008c:  ldarg.0
  IL_008d:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$ResumableLocal_ex$0 As System.Exception"
  IL_0092:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As System.Exception"
  IL_0097:  ldarg.0
  IL_0098:  ldc.i4.3
  IL_0099:  dup
  IL_009a:  stloc.1
  IL_009b:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_00a0:  ldc.i4.1
  IL_00a1:  ret
  IL_00a2:  ldarg.0
  IL_00a3:  ldc.i4.m1
  IL_00a4:  dup
  IL_00a5:  stloc.1
  IL_00a6:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_00ab:  ldc.i4.0
  IL_00ac:  ret
  IL_00ad:  ldloc.0
  IL_00ae:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub TryFinally()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo()
            Console.Write(i.Message)
        Next
    End Sub

    iterator function Foo() as IEnumerable(of Exception)

        Dim ex As exception = nothing

        Try
            yield New Exception("1")
        Finally
            ex = New Exception("2")          
        End Try

        Yield ex

        Try
            Try
                yield New Exception("3")
                Throw New Exception()
            Finally
                ex = New Exception("4")          
            End Try
        Catch ex1 As Exception
        End Try

        Yield ex
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="1234")
        End Sub

        <Fact>
        Public Sub TryYieldFinally()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        For Each i In Foo()
            Console.Write(i)
        Next
    End Sub

    iterator function Foo() as IEnumerable(of integer)
            Dim j as String = ""
            Dim i = 1

            Try
                Try
                    throw new exception

                Catch ex as exception
                    j = j + "#" + i.ToString() + "#"
                End try

                Yield i
            Finally
                Console.Write(j)
            End Try
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="1#1#")
        End Sub

        <Fact>
        Public Sub TryInLoop()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections

Module Module1

    Sub Main()
        For Each i In f()
            Console.Write(i)  
        Next
    End Sub

    Iterator Function f() As IEnumerable
        For i = 1 To 3
            Dim j as String = ""
            Try
                Try
                    Yield i

                    throw new exception

                Catch ex as exception
                    j = j + "#" + i.ToString() + "#"
                End try

                Yield i
            Finally
                Console.Write(j)
            End Try
        Next
    End Function
End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="11#1#22#2#33#3#")
        End Sub

        <Fact>
        Public Sub InStruct()
            Dim source =
    <compilation>
        <file name="a.vb">

Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim s As New S1

        For Each i In s.f
            Console.Write(i)
        Next
        For Each i In s.f
            Console.Write(i)
        Next

        s.x = 42
        For Each i In s.f
            Console.Write(i)
        Next
    End Sub

    Structure S1
        Public x As integer

        public Iterator Function f() As IEnumerable(of Integer)
            Yield x
            x += 1

            Yield x
        End Function
    End Structure

End Module

    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:="01014243")
        End Sub

        <Fact>
        Public Sub UnusedParams()
            Dim source =
    <compilation>
        <file name="a.vb">

Imports System
Imports System.Collections
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim s As New cls1

        For Each i In s.f(Nothing, Nothing)
            Console.Write(i)
        Next
    End Sub

    class cls1
        public Iterator Function f(x As Guid, y As Guid) As IEnumerable(of Integer)
            Yield 1
            Yield 2
        End Function
    End class

End Module

    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:="12").VerifyIL("Module1.cls1.VB$StateMachine_1_f.GetEnumerator()", <![CDATA[
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (Module1.cls1.VB$StateMachine_1_f V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.cls1.VB$StateMachine_1_f.$State As Integer"
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0027
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "Module1.cls1.VB$StateMachine_1_f.$InitialThreadId As Integer"
  IL_0010:  call       "Function System.Threading.Thread.get_CurrentThread() As System.Threading.Thread"
  IL_0015:  callvirt   "Function System.Threading.Thread.get_ManagedThreadId() As Integer"
  IL_001a:  bne.un.s   IL_0027
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.0
  IL_001e:  stfld      "Module1.cls1.VB$StateMachine_1_f.$State As Integer"
  IL_0023:  ldarg.0
  IL_0024:  stloc.0
  IL_0025:  br.s       IL_003a
  IL_0027:  ldc.i4.0
  IL_0028:  newobj     "Sub Module1.cls1.VB$StateMachine_1_f..ctor(Integer)"
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  ldarg.0
  IL_0030:  ldfld      "Module1.cls1.VB$StateMachine_1_f.$VB$Me As Module1.cls1"
  IL_0035:  stfld      "Module1.cls1.VB$StateMachine_1_f.$VB$Me As Module1.cls1"
  IL_003a:  ldloc.0
  IL_003b:  ldarg.0
  IL_003c:  ldfld      "Module1.cls1.VB$StateMachine_1_f.$P_x As System.Guid"
  IL_0041:  stfld      "Module1.cls1.VB$StateMachine_1_f.$VB$Local_x As System.Guid"
  IL_0046:  ldloc.0
  IL_0047:  ldarg.0
  IL_0048:  ldfld      "Module1.cls1.VB$StateMachine_1_f.$P_y As System.Guid"
  IL_004d:  stfld      "Module1.cls1.VB$StateMachine_1_f.$VB$Local_y As System.Guid"
  IL_0052:  ldloc.0
  IL_0053:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub InferredEnumerable()
            Dim source =
    <compilation>
        <file name="a.vb">

Imports System
Imports System.Collections.Generic

Module Module1
    Sub Main()
        Dim i = Iterator Function()
                    yield 123
                End Function

        foo(i)

        Dim i1 = Iterator Function()
                End Function

        foo(i1)

        Dim i2 as Func(Of IEnumerator(of integer)) = Iterator Function()
                                                         Yield 1
                                                         Yield "aa"
                                                    End Function

        foo(i2)
    End Sub

    Public Sub foo(Of T)(x As Func(of T))
        Console.WriteLine(GetType(T))
    End Sub
End Module

    </file>
    </compilation>

            CompileAndVerify(source, expectedOutput:=<![CDATA[
System.Collections.Generic.IEnumerable`1[System.Int32]
System.Collections.Generic.IEnumerable`1[System.Object]
System.Collections.Generic.IEnumerator`1[System.Int32]
]]>)
        End Sub

        <Fact>
        Public Sub EnumeratorWithParameter()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        Dim e = Foo(42)

        While e.MoveNext
            Console.WriteLine(e.Current)
        End While
    End Sub

    iterator function Foo(x as integer) as IEnumerator(of Integer)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Foo.MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0030
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Foo.$VB$Local_x As Integer"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Foo.$Current As Integer"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Foo.$State As Integer"
  IL_0039:  ldc.i4.0
  IL_003a:  ret
}
]]>).VerifyIL("Module1.Foo", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Module1.VB$StateMachine_1_Foo..ctor(Integer)"
  IL_0006:  dup
  IL_0007:  ldarg.0
  IL_0008:  stfld      "Module1.VB$StateMachine_1_Foo.$VB$Local_x As Integer"
  IL_000d:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub GenericEnumerator()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic
        
Module Module1

    Sub Main()
        Dim e = Foo(42)

        While e.MoveNext
            Console.WriteLine(e.Current)
        End While
    End Sub

    iterator function Foo(of T)(x as T) as IEnumerator(of T)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Foo(Of SM$T).MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$State As Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_0030
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  dup
  IL_0013:  stloc.0
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$VB$Local_x As SM$T"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$Current As SM$T"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Foo(Of SM$T).$State As Integer"
  IL_0039:  ldc.i4.0
  IL_003a:  ret
}
]]>).VerifyIL("Module1.Foo", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Module1.VB$StateMachine_1_Foo(Of T)..ctor(Integer)"
  IL_0006:  dup
  IL_0007:  ldarg.0
  IL_0008:  stfld      "Module1.VB$StateMachine_1_Foo(Of T).$VB$Local_x As T"
  IL_000d:  ret
}
]]>)
        End Sub
        <Fact>
        <WorkItem(703361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703361")>
        Public Sub VerifyHelpers()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Public Class Program
    Public Shared Iterator Function Foo() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class]]></file>
                         </compilation>
            Dim comp = CreateCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626}, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim verifier = Me.CompileAndVerify(comp)
            Dim il = verifier.VisualizeIL("Program.VB$StateMachine_1_Foo.GetEnumerator()")
            Assert.Contains("System.Environment.get_CurrentManagedThreadId()", il, StringComparison.Ordinal)
        End Sub

        <WorkItem(835430, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835430")>
        <Fact>
        Public Sub YieldInWith()
            Dim source =
<compilation>
    <file name="a.vb">

Imports System
Imports System.Collections.Generic

Public Structure MyStruct
    Public a As Long
End Structure

Module Module1

    Public MyStructs As MyStruct() = New MyStruct() {Nothing}

    Sub Main()
        For Each i In Foo()
            Console.Write(i)
        Next
    End Sub

    Iterator Function Foo() As IEnumerable(Of Integer)
        For k = 1 To 2
            With MyStructs(0)
                Yield 42

                System.Console.Write( .a)
            End With
        Next
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="420420")
        End Sub

        <Fact, WorkItem(9167, "https://github.com/dotnet/roslyn/issues/9167")>
        Public Sub IteratorShouldCompileWithoutOptionalAttributes()

#Region "IL For corlib without CompilerGeneratedAttribute Or DebuggerNonUserCodeAttribute"
            Dim corlib = "
Namespace System
    Public Class [Object]
    End Class
    Public Class [Int32]
    End Class
    Public Class [Boolean]
    End Class
    Public Class [String]
    End Class
    Public Class Exception
    End Class
    Public Class NotSupportedException
        Inherits Exception
    End Class
    Public Class ValueType
    End Class
    Public Class [Enum]
    End Class
    Public Class Void
    End Class
    Public Interface IDisposable
        Sub Dispose()
    End Interface
End Namespace
Namespace System.Collections
    Public Interface IEnumerable
        Function GetEnumerator() As IEnumerator
    End Interface
    Public Interface IEnumerator
        Function MoveNext() As Boolean
        Property Current As Object
        Sub Reset()
    End Interface
    Namespace Generic
        Public Interface IEnumerable(Of T)
            Inherits IEnumerable
            Overloads Function GetEnumerator() As IEnumerator(Of T)
        End Interface
        Public Interface IEnumerator(Of T)
            Inherits IEnumerator
            Overloads Property Current As T
        End Interface
    End Namespace
End Namespace
Namespace System.Threading
    Public Class Thread
        Shared Property CurrentThread As Thread
        Property ManagedThreadId As Integer
    End Class
End Namespace
"
#End Region

            Dim source = "
Class C
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
        Yield Nothing
    End Function
End Class
"
            ' The compilation succeeds even though CompilerGeneratedAttribute and DebuggerNonUserCodeAttribute are not available.
            Dim compilation = CompilationUtils.CreateCompilation({Parse(source), Parse(corlib)})
            Dim verifier = CompileAndVerify(compilation, verify:=False)
            verifier.VerifyDiagnostics()
        End Sub
    End Class
End Namespace
