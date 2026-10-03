' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
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
  // Code size      184 (0xb8)
  .maxstack  5
  .locals init (Integer V_0,
                Boolean V_1, //b
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  switch    (
        IL_001b,
        IL_0067,
        IL_008f)
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
  IL_0036:  stfld      ""A.VB$StateMachine_1_YieldString.$S1 As Integer""
  IL_003b:  br.s       IL_00a6
  IL_003d:  ldarg.0
  IL_003e:  ldfld      ""A.VB$StateMachine_1_YieldString.$S0 As Boolean()""
  IL_0043:  ldarg.0
  IL_0044:  ldfld      ""A.VB$StateMachine_1_YieldString.$S1 As Integer""
  IL_0049:  ldelem.u1
  IL_004a:  stloc.1
  IL_004b:  ldc.i4.1
  IL_004c:  stloc.2
  IL_004d:  ldloc.2
  IL_004e:  ldloc.1
  IL_004f:  bne.un.s   IL_0072
  IL_0051:  ldarg.0
  IL_0052:  ldstr      ""True""
  IL_0057:  stfld      ""A.VB$StateMachine_1_YieldString.$Current As String""
  IL_005c:  ldarg.0
  IL_005d:  ldc.i4.1
  IL_005e:  dup
  IL_005f:  stloc.0
  IL_0060:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0065:  ldc.i4.1
  IL_0066:  ret
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.m1
  IL_0069:  dup
  IL_006a:  stloc.0
  IL_006b:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0070:  br.s       IL_0098
  IL_0072:  ldloc.2
  IL_0073:  ldloc.1
  IL_0074:  ldc.i4.0
  IL_0075:  ceq
  IL_0077:  bne.un.s   IL_0098
  IL_0079:  ldarg.0
  IL_007a:  ldstr      ""False""
  IL_007f:  stfld      ""A.VB$StateMachine_1_YieldString.$Current As String""
  IL_0084:  ldarg.0
  IL_0085:  ldc.i4.2
  IL_0086:  dup
  IL_0087:  stloc.0
  IL_0088:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_008d:  ldc.i4.1
  IL_008e:  ret
  IL_008f:  ldarg.0
  IL_0090:  ldc.i4.m1
  IL_0091:  dup
  IL_0092:  stloc.0
  IL_0093:  stfld      ""A.VB$StateMachine_1_YieldString.$State As Integer""
  IL_0098:  ldarg.0
  IL_0099:  ldarg.0
  IL_009a:  ldfld      ""A.VB$StateMachine_1_YieldString.$S1 As Integer""
  IL_009f:  ldc.i4.1
  IL_00a0:  add.ovf
  IL_00a1:  stfld      ""A.VB$StateMachine_1_YieldString.$S1 As Integer""
  IL_00a6:  ldarg.0
  IL_00a7:  ldfld      ""A.VB$StateMachine_1_YieldString.$S1 As Integer""
  IL_00ac:  ldarg.0
  IL_00ad:  ldfld      ""A.VB$StateMachine_1_YieldString.$S0 As Boolean()""
  IL_00b2:  ldlen
  IL_00b3:  conv.i4
  IL_00b4:  blt.s      IL_003d
  IL_00b6:  ldc.i4.0
  IL_00b7:  ret
}
").VerifyIL("A.VB$StateMachine_1_YieldString.IEnumerable.GetEnumerator", "
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
        For Each i In Goo
            Console.WriteLine(i)
        Next
    End Sub

    iterator function Goo as IEnumerable(of Integer)
        Yield 42
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.s   42
  IL_001c:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.1
  IL_0023:  dup
  IL_0024:  stloc.0
  IL_0025:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_002a:  ldc.i4.1
  IL_002b:  ret
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.m1
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0035:  ldc.i4.0
  IL_0036:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Goo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Goo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
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
        For Each i In Goo
            Console.Write(i)
        Next
    End Sub

    Iterator Function Goo() As IEnumerable(Of Integer)
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

            CompileAndVerify(source, expectedOutput:="424200979798989999").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size      340 (0x154)
  .maxstack  3
  .locals init (Integer V_0,
                Integer() V_1) //arr
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
  IL_0027:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_002c:  ldc.i4.2
  IL_002d:  newarr     "Integer"
  IL_0032:  stloc.1
  IL_0033:  ldloc.1
  IL_0034:  ldc.i4.0
  IL_0035:  ldc.i4.s   42
  IL_0037:  stelem.i4
  IL_0038:  ldarg.0
  IL_0039:  ldloc.1
  IL_003a:  stfld      "Module1.VB$StateMachine_1_Goo.$S0 As Integer()"
  IL_003f:  ldarg.0
  IL_0040:  ldc.i4.0
  IL_0041:  stfld      "Module1.VB$StateMachine_1_Goo.$S2 As Integer"
  IL_0046:  br.s       IL_00a9
  IL_0048:  ldarg.0
  IL_0049:  ldarg.0
  IL_004a:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As Integer()"
  IL_004f:  ldarg.0
  IL_0050:  ldfld      "Module1.VB$StateMachine_1_Goo.$S2 As Integer"
  IL_0055:  ldelem.i4
  IL_0056:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$1 As Integer"
  IL_005b:  ldarg.0
  IL_005c:  ldarg.0
  IL_005d:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$1 As Integer"
  IL_0062:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_0067:  ldarg.0
  IL_0068:  ldc.i4.1
  IL_0069:  dup
  IL_006a:  stloc.0
  IL_006b:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0070:  ldc.i4.1
  IL_0071:  ret
  IL_0072:  ldarg.0
  IL_0073:  ldc.i4.m1
  IL_0074:  dup
  IL_0075:  stloc.0
  IL_0076:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_007b:  ldarg.0
  IL_007c:  ldarg.0
  IL_007d:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$1 As Integer"
  IL_0082:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_0087:  ldarg.0
  IL_0088:  ldc.i4.2
  IL_0089:  dup
  IL_008a:  stloc.0
  IL_008b:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0090:  ldc.i4.1
  IL_0091:  ret
  IL_0092:  ldarg.0
  IL_0093:  ldc.i4.m1
  IL_0094:  dup
  IL_0095:  stloc.0
  IL_0096:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_009b:  ldarg.0
  IL_009c:  ldarg.0
  IL_009d:  ldfld      "Module1.VB$StateMachine_1_Goo.$S2 As Integer"
  IL_00a2:  ldc.i4.1
  IL_00a3:  add.ovf
  IL_00a4:  stfld      "Module1.VB$StateMachine_1_Goo.$S2 As Integer"
  IL_00a9:  ldarg.0
  IL_00aa:  ldfld      "Module1.VB$StateMachine_1_Goo.$S2 As Integer"
  IL_00af:  ldarg.0
  IL_00b0:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As Integer()"
  IL_00b5:  ldlen
  IL_00b6:  conv.i4
  IL_00b7:  blt.s      IL_0048
  IL_00b9:  ldarg.0
  IL_00ba:  ldstr      "abc"
  IL_00bf:  stfld      "Module1.VB$StateMachine_1_Goo.$S3 As String"
  IL_00c4:  ldarg.0
  IL_00c5:  ldc.i4.0
  IL_00c6:  stfld      "Module1.VB$StateMachine_1_Goo.$S5 As Integer"
  IL_00cb:  br.s       IL_013c
  IL_00cd:  ldarg.0
  IL_00ce:  ldarg.0
  IL_00cf:  ldfld      "Module1.VB$StateMachine_1_Goo.$S3 As String"
  IL_00d4:  ldarg.0
  IL_00d5:  ldfld      "Module1.VB$StateMachine_1_Goo.$S5 As Integer"
  IL_00da:  callvirt   "Function String.get_Chars(Integer) As Char"
  IL_00df:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$4 As Char"
  IL_00e4:  ldarg.0
  IL_00e5:  ldarg.0
  IL_00e6:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$4 As Char"
  IL_00eb:  call       "Function System.Convert.ToInt32(Char) As Integer"
  IL_00f0:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_00f5:  ldarg.0
  IL_00f6:  ldc.i4.3
  IL_00f7:  dup
  IL_00f8:  stloc.0
  IL_00f9:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_00fe:  ldc.i4.1
  IL_00ff:  ret
  IL_0100:  ldarg.0
  IL_0101:  ldc.i4.m1
  IL_0102:  dup
  IL_0103:  stloc.0
  IL_0104:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0109:  ldarg.0
  IL_010a:  ldarg.0
  IL_010b:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$4 As Char"
  IL_0110:  call       "Function System.Convert.ToInt32(Char) As Integer"
  IL_0115:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_011a:  ldarg.0
  IL_011b:  ldc.i4.4
  IL_011c:  dup
  IL_011d:  stloc.0
  IL_011e:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0123:  ldc.i4.1
  IL_0124:  ret
  IL_0125:  ldarg.0
  IL_0126:  ldc.i4.m1
  IL_0127:  dup
  IL_0128:  stloc.0

  IL_0129:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_012e:  ldarg.0
  IL_012f:  ldarg.0
  IL_0130:  ldfld      "Module1.VB$StateMachine_1_Goo.$S5 As Integer"
  IL_0135:  ldc.i4.1
  IL_0136:  add.ovf
  IL_0137:  stfld      "Module1.VB$StateMachine_1_Goo.$S5 As Integer"
  IL_013c:  ldarg.0
  IL_013d:  ldfld      "Module1.VB$StateMachine_1_Goo.$S5 As Integer"
  IL_0142:  ldarg.0
  IL_0143:  ldfld      "Module1.VB$StateMachine_1_Goo.$S3 As String"
  IL_0148:  callvirt   "Function String.get_Length() As Integer"
  IL_014d:  blt        IL_00cd
  IL_0152:  ldc.i4.0
  IL_0153:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Goo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Goo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
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
        For Each i In Goo(42)
            Console.WriteLine(i)
        Next
    End Sub

    iterator function Goo(x as integer) as IEnumerable(of Integer)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$Local_x As Integer"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
        For Each i In Goo({1,2,3,4,5})
            Console.Write(i)
        Next
    End Sub

    iterator function Goo(x As IEnumerable(of Integer)) as IEnumerable(of Integer)
        For Each i In x
            Yield i
        Next
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="12345").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size      158 (0x9e)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Integer V_2) //i
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_001e
  IL_000c:  ldloc.1
  IL_000d:  brfalse.s  IL_0015
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4.1
  IL_0011:  beq.s      IL_001e
  IL_0013:  ldc.i4.0
  IL_0014:  ret
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.m1
  IL_0017:  dup
  IL_0018:  stloc.1
  IL_0019:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_001e:  nop
  .try
  {
    IL_001f:  ldloc.1
    IL_0020:  ldc.i4.s   -4
    IL_0022:  beq.s      IL_002a
    IL_0024:  ldloc.1
    IL_0025:  ldc.i4.1
    IL_0026:  beq.s      IL_006a
    IL_0028:  br.s       IL_0037
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.m1
    IL_002c:  dup
    IL_002d:  stloc.1
    IL_002e:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_0033:  ldc.i4.1
    IL_0034:  stloc.0
    IL_0035:  leave.s    IL_009c
    IL_0037:  ldarg.0
    IL_0038:  ldarg.0
    IL_0039:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$Local_x As System.Collections.Generic.IEnumerable(Of Integer)"
    IL_003e:  callvirt   "Function System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0043:  stfld      "Module1.VB$StateMachine_1_Goo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0048:  br.s       IL_0073
    IL_004a:  ldarg.0
    IL_004b:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0050:  callvirt   "Function System.Collections.Generic.IEnumerator(Of Integer).get_Current() As Integer"
    IL_0055:  stloc.2
    IL_0056:  ldarg.0
    IL_0057:  ldloc.2
    IL_0058:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
    IL_005d:  ldarg.0
    IL_005e:  ldc.i4.1
    IL_005f:  dup
    IL_0060:  stloc.1
    IL_0061:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_0066:  ldc.i4.1
    IL_0067:  stloc.0
    IL_0068:  leave.s    IL_009c
    IL_006a:  ldarg.0
    IL_006b:  ldc.i4.m1
    IL_006c:  dup
    IL_006d:  stloc.1
    IL_006e:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_0073:  ldarg.0
    IL_0074:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0079:  callvirt   "Function System.Collections.IEnumerator.MoveNext() As Boolean"
    IL_007e:  brtrue.s   IL_004a
    IL_0080:  leave.s    IL_009a
  }
  finally
  {
    IL_0082:  ldloc.1
    IL_0083:  ldc.i4.0
    IL_0084:  bge.s      IL_0099
    IL_0086:  ldarg.0
    IL_0087:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_008c:  brfalse.s  IL_0099
    IL_008e:  ldarg.0
    IL_008f:  ldfld      "Module1.VB$StateMachine_1_Goo.$S0 As System.Collections.Generic.IEnumerator(Of Integer)"
    IL_0094:  callvirt   "Sub System.IDisposable.Dispose()"
    IL_0099:  endfinally
  }
  IL_009a:  ldc.i4.0
  IL_009b:  ret
  IL_009c:  ldloc.0
  IL_009d:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Goo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Goo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
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
        For Each i In Goo()
            Console.Write(i)
        Next
    End Sub

    iterator function Goo() as IEnumerable(of Integer)
        For i = 1 to 5
            Yield i
        Next
    End Function

End Module


    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="12345").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldc.i4.1
  IL_001b:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_i$0 As Integer"
  IL_0020:  ldarg.0
  IL_0021:  ldarg.0
  IL_0022:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_i$0 As Integer"
  IL_0027:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_002c:  ldarg.0
  IL_002d:  ldc.i4.1
  IL_002e:  dup
  IL_002f:  stloc.0
  IL_0030:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0035:  ldc.i4.1
  IL_0036:  ret
  IL_0037:  ldarg.0
  IL_0038:  ldc.i4.m1
  IL_0039:  dup
  IL_003a:  stloc.0
  IL_003b:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0040:  ldarg.0
  IL_0041:  ldarg.0
  IL_0042:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_i$0 As Integer"
  IL_0047:  ldc.i4.1
  IL_0048:  add.ovf
  IL_0049:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_i$0 As Integer"
  IL_004e:  ldarg.0
  IL_004f:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_i$0 As Integer"
  IL_0054:  ldc.i4.5
  IL_0055:  ble.s      IL_0020
  IL_0057:  ldc.i4.0
  IL_0058:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Goo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Goo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
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
        For Each i In Goo()
            Console.Write(i)
        Next
    End Sub

    iterator function Goo() as IEnumerable(of Integer)
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

            CompileAndVerify(source, expectedOutput:="233").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size      165 (0xa5)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                Integer V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_0025
  IL_000c:  ldloc.1
  IL_000d:  brfalse.s  IL_0015
  IL_000f:  ldloc.1
  IL_0010:  ldc.i4.1
  IL_0011:  beq.s      IL_0025
  IL_0013:  ldc.i4.0
  IL_0014:  ret
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.m1
  IL_0017:  dup
  IL_0018:  stloc.1
  IL_0019:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_001e:  ldarg.0
  IL_001f:  ldc.i4.1
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
  IL_0025:  nop
  .try
  {
    IL_0026:  ldloc.1
    IL_0027:  ldc.i4.s   -4
    IL_0029:  beq.s      IL_0031
    IL_002b:  ldloc.1
    IL_002c:  ldc.i4.1
    IL_002d:  beq.s      IL_0065
    IL_002f:  br.s       IL_003e
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.m1
    IL_0033:  dup
    IL_0034:  stloc.1
    IL_0035:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_003a:  ldc.i4.1
    IL_003b:  stloc.0
    IL_003c:  leave.s    IL_00a3
    IL_003e:  ldarg.0
    IL_003f:  ldarg.0
    IL_0040:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_0045:  ldc.i4.1
    IL_0046:  add.ovf
    IL_0047:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_004c:  ldarg.0
    IL_004d:  ldarg.0
    IL_004e:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_0053:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
    IL_0058:  ldarg.0
    IL_0059:  ldc.i4.1
    IL_005a:  dup
    IL_005b:  stloc.1
    IL_005c:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_0061:  ldc.i4.1
    IL_0062:  stloc.0
    IL_0063:  leave.s    IL_00a3
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.m1
    IL_0067:  dup
    IL_0068:  stloc.1
    IL_0069:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_006e:  leave.s    IL_00a1
  }
  finally
  {
    IL_0070:  ldloc.1
    IL_0071:  ldc.i4.0
    IL_0072:  bge.s      IL_00a0
    IL_0074:  ldarg.0
    IL_0075:  ldarg.0
    IL_0076:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_007b:  ldc.i4.1
    IL_007c:  add.ovf
    IL_007d:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_0082:  ldarg.0
    IL_0083:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_0088:  stloc.2
    IL_0089:  ldarg.0
    IL_008a:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$ResumableLocal_x$0 As Integer"
    IL_008f:  call       "Sub System.Console.Write(Integer)"
    IL_0094:  ldloca.s   V_2
    IL_0096:  call       "Function Integer.ToString() As String"
    IL_009b:  call       "Sub System.Console.Write(String)"
    IL_00a0:  endfinally
  }
  IL_00a1:  ldc.i4.0
  IL_00a2:  ret
  IL_00a3:  ldloc.0
  IL_00a4:  ret
}
]]>).VerifyIL("Module1.VB$StateMachine_1_Goo.IEnumerable.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       "Function Module1.VB$StateMachine_1_Goo.GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer)"
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
        For Each i In Goo()
            Console.Write(i.Message)
        Next
    End Sub

    iterator function Goo() as IEnumerable(of Exception)

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

            CompileAndVerify(source, expectedOutput:="12").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1,
                System.Exception V_2) //ex
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -4
  IL_000a:  sub
  IL_000b:  switch    (
        IL_0039,
        IL_002c,
        IL_002c,
        IL_002c,
        IL_002e,
        IL_0039,
        IL_00a3)
  IL_002c:  ldc.i4.0
  IL_002d:  ret
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.m1
  IL_0030:  dup
  IL_0031:  stloc.1
  IL_0032:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0037:  ldnull
  IL_0038:  stloc.2
  IL_0039:  nop
  .try
  {
    IL_003a:  ldloc.1
    IL_003b:  ldc.i4.s   -4
    IL_003d:  beq.s      IL_0045
    IL_003f:  ldloc.1
    IL_0040:  ldc.i4.1
    IL_0041:  beq.s      IL_006f
    IL_0043:  br.s       IL_0052
    IL_0045:  ldarg.0
    IL_0046:  ldc.i4.m1
    IL_0047:  dup
    IL_0048:  stloc.1
    IL_0049:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_004e:  ldc.i4.1
    IL_004f:  stloc.0
    IL_0050:  leave.s    IL_00ae
    IL_0052:  ldarg.0
    IL_0053:  ldstr      "1"
    IL_0058:  newobj     "Sub System.Exception..ctor(String)"
    IL_005d:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As System.Exception"
    IL_0062:  ldarg.0
    IL_0063:  ldc.i4.1
    IL_0064:  dup
    IL_0065:  stloc.1
    IL_0066:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_006b:  ldc.i4.1
    IL_006c:  stloc.0
    IL_006d:  leave.s    IL_00ae
    IL_006f:  ldarg.0
    IL_0070:  ldc.i4.m1
    IL_0071:  dup
    IL_0072:  stloc.1
    IL_0073:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
    IL_0078:  ldstr      "2"
    IL_007d:  newobj     "Sub System.Exception..ctor(String)"
    IL_0082:  throw
  }
  catch System.Exception
  {
    IL_0083:  dup
    IL_0084:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0089:  stloc.2
    IL_008a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_008f:  leave.s    IL_0091
  }
  IL_0091:  ldarg.0
  IL_0092:  ldloc.2
  IL_0093:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As System.Exception"
  IL_0098:  ldarg.0
  IL_0099:  ldc.i4.2
  IL_009a:  dup
  IL_009b:  stloc.1
  IL_009c:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_00a1:  ldc.i4.1
  IL_00a2:  ret
  IL_00a3:  ldarg.0
  IL_00a4:  ldc.i4.m1
  IL_00a5:  dup
  IL_00a6:  stloc.1
  IL_00a7:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_00ac:  ldc.i4.0
  IL_00ad:  ret
  IL_00ae:  ldloc.0
  IL_00af:  ret
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
        For Each i In Goo()
            Console.Write(i.Message)
        Next
    End Sub

    iterator function Goo() as IEnumerable(of Exception)

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
        For Each i In Goo()
            Console.Write(i)
        Next
    End Sub

    iterator function Goo() as IEnumerable(of integer)
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

        goo(i)

        Dim i1 = Iterator Function()
                End Function

        goo(i1)

        Dim i2 as Func(Of IEnumerator(of integer)) = Iterator Function()
                                                         Yield 1
                                                         Yield "aa"
                                                    End Function

        goo(i2)
    End Sub

    Public Sub goo(Of T)(x As Func(of T))
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
        Dim e = Goo(42)

        While e.MoveNext
            Console.WriteLine(e.Current)
        End While
    End Sub

    iterator function Goo(x as integer) as IEnumerator(of Integer)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Goo.MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
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
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Goo.$VB$Local_x As Integer"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Goo.$Current As Integer"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Goo.$State As Integer"
  IL_0039:  ldc.i4.0
  IL_003a:  ret
}
]]>).VerifyIL("Module1.Goo", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Module1.VB$StateMachine_1_Goo..ctor(Integer)"
  IL_0006:  dup
  IL_0007:  ldarg.0
  IL_0008:  stfld      "Module1.VB$StateMachine_1_Goo.$VB$Local_x As Integer"
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
        Dim e = Goo(42)

        While e.MoveNext
            Console.WriteLine(e.Current)
        End While
    End Sub

    iterator function Goo(of T)(x as T) as IEnumerator(of T)
        Yield x
    End Function

End Module

    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="42").VerifyIL("Module1.VB$StateMachine_1_Goo(Of SM$T).MoveNext", <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$State As Integer"
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
  IL_0014:  stfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$State As Integer"
  IL_0019:  ldarg.0
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$VB$Local_x As SM$T"
  IL_0020:  stfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$Current As SM$T"
  IL_0025:  ldarg.0
  IL_0026:  ldc.i4.1
  IL_0027:  dup
  IL_0028:  stloc.0
  IL_0029:  stfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$State As Integer"
  IL_002e:  ldc.i4.1
  IL_002f:  ret
  IL_0030:  ldarg.0
  IL_0031:  ldc.i4.m1
  IL_0032:  dup
  IL_0033:  stloc.0
  IL_0034:  stfld      "Module1.VB$StateMachine_1_Goo(Of SM$T).$State As Integer"
  IL_0039:  ldc.i4.0
  IL_003a:  ret
}
]]>).VerifyIL("Module1.Goo", <![CDATA[
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Module1.VB$StateMachine_1_Goo(Of T)..ctor(Integer)"
  IL_0006:  dup
  IL_0007:  ldarg.0
  IL_0008:  stfld      "Module1.VB$StateMachine_1_Goo(Of T).$VB$Local_x As T"
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
    Public Shared Iterator Function Goo() As IEnumerable(Of Integer)
        Yield 1
    End Function
End Class]]></file>
                         </compilation>
            Dim comp = CreateEmptyCompilationWithReferences(source, {MscorlibRef_v4_0_30316_17626}, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim verifier = Me.CompileAndVerify(comp)
            Dim il = verifier.VisualizeIL("Program.VB$StateMachine_1_Goo.GetEnumerator()")
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
        For Each i In Goo()
            Console.Write(i)
        Next
    End Sub

    Iterator Function Goo() As IEnumerable(Of Integer)
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
            Dim compilation = CompilationUtils.CreateEmptyCompilation({Parse(source), Parse(corlib)})
            Dim verifier = CompileAndVerify(compilation, verify:=Verification.Fails)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(9463, "https://github.com/dotnet/roslyn/issues/9463")>
        Public Sub IEnumerableIteratorReportsDiagnosticsWhenCoreTypesAreMissing()
            ' Note that IDisposable.Dispose, IEnumerator.Current and other types are missing
            ' Also, IEnumerator(Of T) doesn't have a get accessor
            Dim source = "
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
    Public Class ValueType
    End Class
    Public Class [Enum]
    End Class
    Public Class Void
    End Class
    Public Interface IDisposable
    End Interface
End Namespace

Namespace System.Collections
    Public Interface IEnumerable
    End Interface
    Public Interface IEnumerator
    End Interface
End Namespace

Namespace System.Collections.Generic
    Public Interface IEnumerator(Of T)
        WriteOnly Property Current As T
    End Interface
End Namespace

Class C
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
        Yield Nothing
    End Function
End Class
"
            Dim compilation = CreateEmptyCompilation({Parse(source)})

            compilation.AssertTheseEmitDiagnostics(<expected>
BC30002: Type 'System.Collections.Generic.IEnumerable' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30524: Property 'System.Collections.Generic.IEnumerator(Of T).Current' is 'WriteOnly'.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.Generic.IEnumerable`1.GetEnumerator' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerable.GetEnumerator' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerator.Current' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerator.MoveNext' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerator.Reset' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.IDisposable.Dispose' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerable
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                </expected>)
        End Sub

        <Fact, WorkItem(9463, "https://github.com/dotnet/roslyn/issues/9463")>
        Public Sub IEnumeratorIteratorReportsDiagnosticsWhenCoreTypesAreMissing()
            ' Note that IDisposable.Dispose and other types are missing
            ' Also IEnumerator.Current lacks a get accessor
            Dim source = "
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
    Public Class ValueType
    End Class
    Public Class [Enum]
    End Class
    Public Class Void
    End Class
    Public Interface IDisposable
    End Interface
End Namespace

Namespace System.Collections
    Public Interface IEnumerable
    End Interface
    Public Interface IEnumerator
        WriteOnly Property Current As Object
    End Interface
End Namespace

Class C
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
        Yield Nothing
    End Function
End Class
"
            Dim compilation = CreateEmptyCompilation({Parse(source)})

            ' No error about IEnumerable
            compilation.AssertTheseEmitDiagnostics(<expected>
BC30002: Type 'System.Collections.Generic.IEnumerator' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30524: Property 'System.Collections.IEnumerator.Current' is 'WriteOnly'.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.Generic.IEnumerator`1.Current' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerator.MoveNext' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Collections.IEnumerator.Reset' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.IDisposable.Dispose' is not defined.
    Public Iterator Function SomeNumbers() As System.Collections.IEnumerator
    ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                </expected>)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldReturn()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()
        Console.Write("disposed ")

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="True one disposed False one")
            verifier.VerifyIL("C.VB$StateMachine_1_GetEnumerator.Dispose()", "
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0008:  ret
}
")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()
        Console.Write(Object.ReferenceEquals(enumerable, enumerator))

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()
        Console.Write("disposed ")

        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of String)
        Yield " one "
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="TrueTrue one disposed -3 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_DisposeBeforeIteration()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator()

        enumerator.Dispose()
        Console.Write("disposed ")

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current Is Nothing)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator() As IEnumerator(Of String)
        Yield " one "
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="disposed FalseTrue")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Console.Write(CType(enumerable.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerable), Integer))
        Console.Write(" ")

        Dim enumerator = enumerable.GetEnumerator()
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))

        enumerator.Dispose()
        Console.Write(" disposed ")

        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of String)
        Yield " one "
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="-2 0 disposed -3 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldReturn_IEnumerable()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.Produce().GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()
        Console.Write("disposed ")

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of String)
        Yield " one "
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one disposed False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection


Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(Object.ReferenceEquals(enumerable, enumerator))
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Console.Write(enumerator.MoveNext())
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        enumerator.Dispose()

        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        enumerator.Dispose()
        enumerator.Dispose()

        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 42
        Yield 43
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="TrueTrueTrueTrue -3 True -3 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_DisposeTwice()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()
        Console.Write("disposed ")

        enumerator.Dispose()
        Console.Write("disposed2 ")

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)
        Dim local As String = ""
        Yield " one "
        local.ToString()
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one disposed disposed2 False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())

        enumerator.Dispose()
        Console.Write(" disposed ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        enumerator.Dispose()
        Console.Write(" disposed2 ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of Integer)
        Dim local As String = ""
        Yield 1
        local.ToString()
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="True disposed -3 True disposed2 -3 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldBreak()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        If b Then
            Return
        End If
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one False one False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce(True)
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Console.Write(Not enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce(b As Boolean) As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 42
        If b Then
            Return
        End If
        Yield 43
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="True 1 TrueTrue -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_EndOfBody()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        Console.Write("done ")
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one done False one False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce(True)
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Console.Write(Not enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce(b As Boolean) As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 42
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="True 1 TrueTrue -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_ThrowException()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Try
            Console.Write(enumerator.MoveNext())
        Catch e As Exception
            Console.Write(e.Message)
        End Try

        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        Throw New Exception("exception")
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one exception one False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Try
            enumerator.MoveNext()
        Catch
            Console.Write(" ")
            Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
            Console.Write(" ")
            Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
        End Try
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of Integer)
        Yield 42
        Throw New Exception("exception")
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="TrueTrue -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldReturn_InTryFinally()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write("disposing ")
        Try
            enumerator.Dispose()
        Catch e As Exception
            Console.Write(e.Message)
        End Try
        Console.Write(" disposed ")

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of String)
        Try
            Yield " one "
        Finally
            Throw New Exception("exception")
        End Try
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="True one disposing exception disposed False one")
            verifier.VerifyIL("C.VB$StateMachine_1_GetEnumerator.Dispose()", "
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  bne.un.s   IL_0015
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.s   -4
  IL_000e:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0013:  br.s       IL_001c
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.m1
  IL_0017:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_001c:  ldarg.0
  IL_001d:  call       ""Function C.VB$StateMachine_1_GetEnumerator.MoveNext() As Boolean""
  IL_0022:  pop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.s   -3
  IL_0026:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_002b:  ret
}
")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Try
            enumerator.Dispose()
        Catch
            Console.Write(" ")
            Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
            Console.Write(" ")
            Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
        End Try
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As System.Collections.Generic.IEnumerable(Of Integer)
        Try
            Yield 42
        Finally
            Throw New Exception("exception")
        End Try
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="TrueTrue -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldBreak_InTryFinally()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        Try
            If b Then
                Return
            End If
        Finally
            Console.Write("finally ")
        End Try
        Yield " two "
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one finally False one False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Collections.Generic
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce(True)
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))

        Console.Write(Not enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce(b As Boolean) As IEnumerable(Of Integer)
        Yield 42
        Try
            If b Then
                Exit Function
            End If
        Finally
            Console.Write(" finally ")
        End Try
        Yield 43
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="TrueTrue finally True -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_ThrowException_InTryFinally()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Try
            Console.Write(enumerator.MoveNext())
        Catch e As Exception
            Console.Write(e.Message)
        End Try

        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Yield " one "
        Try
            Throw New Exception("exception")
        Finally
            Console.Write("finally ")
        End Try
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:="True one finally exception one False one False one")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Collections.Generic
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())

        Try
            enumerator.MoveNext()
        Catch ex As Exception
            Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
            Console.Write(" ")
            Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
        End Try

        enumerator.Dispose()
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As IEnumerable(Of Integer)
        Yield 42
        Try
            Throw New Exception("exception")
        Finally
            Console.Write(" finally ")
        End Try
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="True finally -1 True -3 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_ThrowException_InTryFinally_WithYieldInTry()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Try
            Console.Write(enumerator.MoveNext())
        Catch e As Exception
            Console.Write(e.Message)
        End Try

        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As System.Collections.Generic.IEnumerator(Of String)
        Try
            Yield " one "
            If b Then
                Throw New Exception("exception")
            End If
        Finally
            Console.Write("finally ")
        End Try
    End Function
End Class
    </file>
</compilation>

            Dim verifier = CompileAndVerify(source, expectedOutput:="True one finally exception one False one")
            verifier.VerifyIL("C.VB$StateMachine_1_GetEnumerator.Dispose()", "
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  bne.un.s   IL_0015
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.s   -4
  IL_000e:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0013:  br.s       IL_001c
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.m1
  IL_0017:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_001c:  ldarg.0
  IL_001d:  call       ""Function C.VB$StateMachine_1_GetEnumerator.MoveNext() As Boolean""
  IL_0022:  pop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.s   -3
  IL_0026:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_002b:  ret
}
")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Collections.Generic
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())

        Try
            enumerator.MoveNext()
        Catch ex As Exception
            Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
            Console.Write(" ")
            Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
        End Try
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As IEnumerable(Of Integer)
        Try
            Yield 42
            Throw New Exception("exception")
        Finally
            Console.Write(" finally ")
        End Try
    End Function
End Class
    </file>
</compilation>

            ' We're not setting the state to "after"/"finished"
            ' Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(source2, expectedOutput:="True finally -1 True")
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")>
        Public Sub StateAfterMoveNext_YieldReturn_AfterTryFinally()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic

Module Program
    Sub Main()
        Dim enumerator = C.GetEnumerator(True)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)

        enumerator.Dispose()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.Current)
    End Sub
End Module

Class C
    Public Shared Iterator Function GetEnumerator(b As Boolean) As IEnumerator(Of String)
        Try
            Yield " one "
        Finally
            Console.Write("finally ")
        End Try

        Yield " two "
        Console.Write("not executed after disposal")
    End Function
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="True one finally True two False two")
            verifier.VerifyIL("C.VB$StateMachine_1_GetEnumerator.MoveNext()", "
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0006:  stloc.1
  IL_0007:  ldloc.1
  IL_0008:  ldc.i4.s   -4
  IL_000a:  sub
  IL_000b:  switch    (
        IL_0037,
        IL_002c,
        IL_002c,
        IL_002c,
        IL_002e,
        IL_0037,
        IL_0098)
  IL_002c:  ldc.i4.0
  IL_002d:  ret
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4.m1
  IL_0030:  dup
  IL_0031:  stloc.1
  IL_0032:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0037:  nop
  .try
  {
    IL_0038:  ldloc.1
    IL_0039:  ldc.i4.s   -4
    IL_003b:  beq.s      IL_0043
    IL_003d:  ldloc.1
    IL_003e:  ldc.i4.1
    IL_003f:  beq.s      IL_0068
    IL_0041:  br.s       IL_0050
    IL_0043:  ldarg.0
    IL_0044:  ldc.i4.m1
    IL_0045:  dup
    IL_0046:  stloc.1
    IL_0047:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
    IL_004c:  ldc.i4.1
    IL_004d:  stloc.0
    IL_004e:  leave.s    IL_00ad
    IL_0050:  ldarg.0
    IL_0051:  ldstr      "" one ""
    IL_0056:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$Current As String""
    IL_005b:  ldarg.0
    IL_005c:  ldc.i4.1
    IL_005d:  dup
    IL_005e:  stloc.1
    IL_005f:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
    IL_0064:  ldc.i4.1
    IL_0065:  stloc.0
    IL_0066:  leave.s    IL_00ad
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.m1
    IL_006a:  dup
    IL_006b:  stloc.1
    IL_006c:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
    IL_0071:  leave.s    IL_0082
  }
  finally
  {
    IL_0073:  ldloc.1
    IL_0074:  ldc.i4.0
    IL_0075:  bge.s      IL_0081
    IL_0077:  ldstr      ""finally ""
    IL_007c:  call       ""Sub System.Console.Write(String)""
    IL_0081:  endfinally
  }
  IL_0082:  ldarg.0
  IL_0083:  ldstr      "" two ""
  IL_0088:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$Current As String""
  IL_008d:  ldarg.0
  IL_008e:  ldc.i4.2
  IL_008f:  dup
  IL_0090:  stloc.1
  IL_0091:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0096:  ldc.i4.1
  IL_0097:  ret
  IL_0098:  ldarg.0
  IL_0099:  ldc.i4.m1
  IL_009a:  dup
  IL_009b:  stloc.1
  IL_009c:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_00a1:  ldstr      ""not executed after disposal""
  IL_00a6:  call       ""Sub System.Console.Write(String)""
  IL_00ab:  ldc.i4.0
  IL_00ac:  ret
  IL_00ad:  ldloc.0
  IL_00ae:  ret
}
")
            verifier.VerifyIL("C.VB$StateMachine_1_GetEnumerator.Dispose()", "
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.1
  IL_0009:  bne.un.s   IL_0015
  IL_000b:  ldarg.0
  IL_000c:  ldc.i4.s   -4
  IL_000e:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_0013:  br.s       IL_001c
  IL_0015:  ldarg.0
  IL_0016:  ldc.i4.m1
  IL_0017:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_001c:  ldarg.0
  IL_001d:  call       ""Function C.VB$StateMachine_1_GetEnumerator.MoveNext() As Boolean""
  IL_0022:  pop
  IL_0023:  ldarg.0
  IL_0024:  ldc.i4.s   -3
  IL_0026:  stfld      ""C.VB$StateMachine_1_GetEnumerator.$State As Integer""
  IL_002b:  ret
}
")

            ' Verify GetEnumerator
            Dim source2 =
<compilation>
    <file name="a2.vb">
Imports System
Imports System.Collections.Generic
Imports System.Reflection

Module Program
    Sub Main()
        Dim enumerable = C.Produce()
        Dim enumerator = enumerable.GetEnumerator()

        Console.Write(enumerator.MoveNext())
        Console.Write(enumerator.MoveNext())
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
 
        enumerator.Dispose()
        Console.Write(" ")
        Console.Write(CType(enumerator.GetType().GetField("$State", BindingFlags.Public Or BindingFlags.Instance).GetValue(enumerator), Integer))
        Console.Write(" ")
        Console.Write(Not Object.ReferenceEquals(enumerable, enumerable.GetEnumerator()))
    End Sub
End Module

Class C
    Public Shared Iterator Function Produce() As IEnumerable(Of Integer)
        Try
            Yield 42
        Finally
            Console.Write(" finally ")
        End Try

        Yield 43
        Console.Write("not executed after disposal")
    End Function
End Class
    </file>
</compilation>

            CompileAndVerify(source2, expectedOutput:="True finally True 2 True -3 True")
        End Sub
    End Class
End Namespace
