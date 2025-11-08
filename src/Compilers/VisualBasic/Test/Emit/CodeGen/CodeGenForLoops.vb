' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenForLoops
        Inherits BasicTestBase

        <WorkItem(541539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541539")>
        <Fact>
        Public Sub SimpleForLoopsTest()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(2) {1, 2, 3}
        For i As Integer = 0 To myarray.Length - 1
            System.Console.WriteLine(myarray(i))
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
]]>)
        End Sub

        <Fact>
        Public Sub SimpleForLoopsTestConversion()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
option strict off
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(1) {}
        myarray(0) = 1
        myarray(1) = 2

        Dim s as double = 1.1

        For i As Integer = 0 To "1" Step s
            System.Console.WriteLine(myarray(i))
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       73 (0x49)
  .maxstack  3
  .locals init (Integer() V_0, //myarray
  Integer V_1,
  Integer V_2,
  Integer V_3) //i
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     "Integer"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.1
  IL_000a:  stelem.i4
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  ldc.i4.2
  IL_000e:  stelem.i4
  IL_000f:  ldc.r8     1.1
  IL_0018:  ldstr      "1"
  IL_001d:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
  IL_0022:  stloc.1
  IL_0023:  call       "Function System.Math.Round(Double) As Double"
  IL_0028:  conv.ovf.i4
  IL_0029:  stloc.2
  IL_002a:  ldc.i4.0
  IL_002b:  stloc.3
  IL_002c:  br.s       IL_003a
  IL_002e:  ldloc.0
  IL_002f:  ldloc.3
  IL_0030:  ldelem.i4
  IL_0031:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0036:  ldloc.3
  IL_0037:  ldloc.2
  IL_0038:  add.ovf
  IL_0039:  stloc.3
  IL_003a:  ldloc.2
  IL_003b:  ldc.i4.s   31
  IL_003d:  shr
  IL_003e:  ldloc.3
  IL_003f:  xor
  IL_0040:  ldloc.2
  IL_0041:  ldc.i4.s   31
  IL_0043:  shr
  IL_0044:  ldloc.1
  IL_0045:  xor
  IL_0046:  ble.s      IL_002e
  IL_0048:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ForLoopStepIsFloatVar()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Double = 1.1

        For i As Double = 0 To 2 Step s
            System.Console.WriteLine(i.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1.1
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       97 (0x61)
  .maxstack  2
  .locals init (Double V_0,
  Boolean V_1,
  Double V_2) //i
  IL_0000:  ldc.r8     1.1
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.r8     0
  IL_0014:  clt.un
  IL_0016:  ldc.i4.0
  IL_0017:  ceq
  IL_0019:  stloc.1
  IL_001a:  ldc.r8     0
  IL_0023:  stloc.2
  IL_0024:  br.s       IL_003b
  IL_0026:  ldloca.s   V_2
  IL_0028:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_002d:  call       "Function Double.ToString(System.IFormatProvider) As String"
  IL_0032:  call       "Sub System.Console.WriteLine(String)"
  IL_0037:  ldloc.2
  IL_0038:  ldloc.0
  IL_0039:  add
  IL_003a:  stloc.2
  IL_003b:  ldloc.1
  IL_003c:  brtrue.s   IL_004f
  IL_003e:  ldloc.2
  IL_003f:  ldc.r8     2
  IL_0048:  clt.un
  IL_004a:  ldc.i4.0
  IL_004b:  ceq
  IL_004d:  br.s       IL_005e
  IL_004f:  ldloc.2
  IL_0050:  ldc.r8     2
  IL_0059:  cgt.un
  IL_005b:  ldc.i4.0
  IL_005c:  ceq
  IL_005e:  brtrue.s   IL_0026
  IL_0060:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ForLoopStepIsDecimalVar1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Byte = 1

        For i As Decimal = 0 To 1 Step s
            System.Console.WriteLine(i)
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       92 (0x5c)
  .maxstack  2
  .locals init (Byte V_0, //s
  Decimal V_1,
  Boolean V_2,
  Decimal V_3) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub Decimal..ctor(Integer)"
  IL_000a:  ldloc.1
  IL_000b:  ldsfld     "Decimal.Zero As Decimal"
  IL_0010:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_0015:  ldc.i4.0
  IL_0016:  clt
  IL_0018:  ldc.i4.0
  IL_0019:  ceq
  IL_001b:  stloc.2
  IL_001c:  ldsfld     "Decimal.Zero As Decimal"
  IL_0021:  stloc.3
  IL_0022:  br.s       IL_0032
  IL_0024:  ldloc.3
  IL_0025:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_002a:  ldloc.3
  IL_002b:  ldloc.1
  IL_002c:  call       "Function Decimal.Add(Decimal, Decimal) As Decimal"
  IL_0031:  stloc.3
  IL_0032:  ldloc.2
  IL_0033:  brtrue.s   IL_0048
  IL_0035:  ldloc.3
  IL_0036:  ldsfld     "Decimal.One As Decimal"
  IL_003b:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_0040:  ldc.i4.0
  IL_0041:  clt
  IL_0043:  ldc.i4.0
  IL_0044:  ceq
  IL_0046:  br.s       IL_0059
  IL_0048:  ldloc.3
  IL_0049:  ldsfld     "Decimal.One As Decimal"
  IL_004e:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_0053:  ldc.i4.0
  IL_0054:  cgt
  IL_0056:  ldc.i4.0
  IL_0057:  ceq
  IL_0059:  brtrue.s   IL_0024
  IL_005b:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ForLoopStepIsDecimalVar2()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Byte = 1

        For i As Decimal = 0 To 2 Step s
            System.Console.WriteLine(i)
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
2
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (Byte V_0, //s
  Decimal V_1,
  Decimal V_2,
  Boolean V_3,
  Decimal V_4) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.2
  IL_0005:  conv.i8
  IL_0006:  call       "Sub Decimal..ctor(Long)"
  IL_000b:  ldloca.s   V_2
  IL_000d:  ldloc.0
  IL_000e:  call       "Sub Decimal..ctor(Integer)"
  IL_0013:  ldloc.2
  IL_0014:  ldsfld     "Decimal.Zero As Decimal"
  IL_0019:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_001e:  ldc.i4.0
  IL_001f:  clt
  IL_0021:  ldc.i4.0
  IL_0022:  ceq
  IL_0024:  stloc.3
  IL_0025:  ldsfld     "Decimal.Zero As Decimal"
  IL_002a:  stloc.s    V_4
  IL_002c:  br.s       IL_003f
  IL_002e:  ldloc.s    V_4
  IL_0030:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_0035:  ldloc.s    V_4
  IL_0037:  ldloc.2
  IL_0038:  call       "Function Decimal.Add(Decimal, Decimal) As Decimal"
  IL_003d:  stloc.s    V_4
  IL_003f:  ldloc.3
  IL_0040:  brtrue.s   IL_0052
  IL_0042:  ldloc.s    V_4
  IL_0044:  ldloc.1
  IL_0045:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_004a:  ldc.i4.0
  IL_004b:  clt
  IL_004d:  ldc.i4.0
  IL_004e:  ceq
  IL_0050:  br.s       IL_0060
  IL_0052:  ldloc.s    V_4
  IL_0054:  ldloc.1
  IL_0055:  call       "Function Decimal.Compare(Decimal, Decimal) As Integer"
  IL_005a:  ldc.i4.0
  IL_005b:  cgt
  IL_005d:  ldc.i4.0
  IL_005e:  ceq
  IL_0060:  brtrue.s   IL_002e
  IL_0062:  ret
}
]]>)

        End Sub

        <Fact()>
        <WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")>
        Public Sub ForLoopStepIsFloatNegativeVar()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Double = -1.1

        For i As Double = 2 To 0 Step s
            System.Console.WriteLine(i.ToString("G15", System.Globalization.CultureInfo.InvariantCulture))
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
0.9
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size      102 (0x66)
  .maxstack  3
  .locals init (Double V_0,
                Boolean V_1,
                Double V_2) //i
  IL_0000:  ldc.r8     -1.1
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.r8     0
  IL_0014:  clt.un
  IL_0016:  ldc.i4.0
  IL_0017:  ceq
  IL_0019:  stloc.1
  IL_001a:  ldc.r8     2
  IL_0023:  stloc.2
  IL_0024:  br.s       IL_0040
  IL_0026:  ldloca.s   V_2
  IL_0028:  ldstr      "G15"
  IL_002d:  call       "Function System.Globalization.CultureInfo.get_InvariantCulture() As System.Globalization.CultureInfo"
  IL_0032:  call       "Function Double.ToString(String, System.IFormatProvider) As String"
  IL_0037:  call       "Sub System.Console.WriteLine(String)"
  IL_003c:  ldloc.2
  IL_003d:  ldloc.0
  IL_003e:  add
  IL_003f:  stloc.2
  IL_0040:  ldloc.1
  IL_0041:  brtrue.s   IL_0054
  IL_0043:  ldloc.2
  IL_0044:  ldc.r8     0
  IL_004d:  clt.un
  IL_004f:  ldc.i4.0
  IL_0050:  ceq
  IL_0052:  br.s       IL_0063
  IL_0054:  ldloc.2
  IL_0055:  ldc.r8     0
  IL_005e:  cgt.un
  IL_0060:  ldc.i4.0
  IL_0061:  ceq
  IL_0063:  brtrue.s   IL_0026
  IL_0065:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub ForLoopObject()
            Dim v = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()

        Dim ctrlVar As Object
        Dim initValue As Object = 0
        Dim limit As Object = 2
        Dim stp As Object = 1

        For ctrlVar = initValue To limit Step stp
            System.Console.WriteLine(ctrlVar)
        Next

    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
2
]]>)
            v.VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  6
  .locals init (Object V_0, //ctrlVar
                Object V_1, //initValue
                Object V_2, //limit
                Object V_3, //stp
                Object V_4)
 -IL_0000:  ldc.i4.0
  IL_0001:  box        "Integer"
  IL_0006:  stloc.1
 -IL_0007:  ldc.i4.2
  IL_0008:  box        "Integer"
  IL_000d:  stloc.2
 -IL_000e:  ldc.i4.1
  IL_000f:  box        "Integer"
  IL_0014:  stloc.3
 -IL_0015:  ldloc.0
  IL_0016:  ldloc.1
  IL_0017:  ldloc.2
  IL_0018:  ldloc.3
  IL_0019:  ldloca.s   V_4
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0022:  brfalse.s  IL_003b
 -IL_0024:  ldloc.0
  IL_0025:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002a:  call       "Sub System.Console.WriteLine(Object)"
 -IL_002f:  ldloc.0
  IL_0030:  ldloc.s    V_4
  IL_0032:  ldloca.s   V_0
  IL_0034:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_0039:  brtrue.s   IL_0024
 -IL_003b:  ret
}
]]>, displaySequencePoints:=true)

        End Sub

        ' Step past the end value
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ForLoopStepPassedEnd()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For i As Integer = 0 To 3 Step 2
            System.Console.WriteLine(i)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
2
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.2
  IL_000a:  add.ovf
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.3
  IL_000e:  ble.s      IL_0002
  IL_0010:  ret
}]]>)

        End Sub

        ' Step past the end value
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ForLoopStepPassedEndStepNegative()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For i As Integer = 2 To -1 Step -2
            System.Console.WriteLine(i)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
0
]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.s   -2
  IL_000b:  add.ovf
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.m1
  IL_000f:  bge.s      IL_0002
  IL_0011:  ret
}]]>)

        End Sub

        ' Use a FOR/NEXT with Decimal type and a positive STEP where start
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ForLoopStepIsDecimalVar3()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For Counter = 1.05@ To 2.06@ Step 0.1@
            System.Console.WriteLine(Counter.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Next Counter
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1.05
1.15
1.25
1.35
1.45
1.55
1.65
1.75
1.85
1.95
2.05
]]>)

        End Sub

        ' FOR/NEXT loops can be nested
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ForLoopNested()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For AVarName = 1 To 2
            For B = 1 To 2
                For C = 1 To 2
                    For D = 1 To 2
                    Next D, C, B
        Next AVarName
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (Integer V_0, //AVarName
  Integer V_1, //B
  Integer V_2, //C
  Integer V_3) //D
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  stloc.2
  IL_0006:  ldc.i4.1
  IL_0007:  stloc.3
  IL_0008:  ldloc.3
  IL_0009:  ldc.i4.1
  IL_000a:  add.ovf
  IL_000b:  stloc.3
  IL_000c:  ldloc.3
  IL_000d:  ldc.i4.2
  IL_000e:  ble.s      IL_0008
  IL_0010:  ldloc.2
  IL_0011:  ldc.i4.1
  IL_0012:  add.ovf
  IL_0013:  stloc.2
  IL_0014:  ldloc.2
  IL_0015:  ldc.i4.2
  IL_0016:  ble.s      IL_0006
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.1
  IL_001a:  add.ovf
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  ldc.i4.2
  IL_001e:  ble.s      IL_0004
  IL_0020:  ldloc.0
  IL_0021:  ldc.i4.1
  IL_0022:  add.ovf
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.2
  IL_0026:  ble.s      IL_0002
  IL_0028:  ret
}]]>)

        End Sub

        ' When NEXT is used without a variable, the current loop is incremented
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ForLoopNextWithoutVariable()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For A = 1 To 2
            For B = 3 To 4
            Next
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Integer V_0, //A
  Integer V_1) //B
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.3
  IL_0003:  stloc.1
  IL_0004:  ldloc.1
  IL_0005:  ldc.i4.1
  IL_0006:  add.ovf
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldc.i4.4
  IL_000a:  ble.s      IL_0004
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  add.ovf
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ldc.i4.2
  IL_0012:  ble.s      IL_0002
  IL_0014:  ret
}]]>)
        End Sub

        ' Change outer variable in inner for Loops 
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ChangeOuterVarInInnerFor()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = 1 To 2
                I = 3
                System.Console.WriteLine(I)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
3
3]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (Integer V_0, //I
  Integer V_1) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.3
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000c:  ldloc.1
  IL_000d:  ldc.i4.1
  IL_000e:  add.ovf
  IL_000f:  stloc.1
  IL_0010:  ldloc.1
  IL_0011:  ldc.i4.2
  IL_0012:  ble.s      IL_0004
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  add.ovf
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.2
  IL_001a:  ble.s      IL_0002
  IL_001c:  ret
}]]>)

        End Sub

        ' Inner for loop referencing the outer for loop iteration variable
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub InnerForRefOuterForVar()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = I + 1 To 2
                System.Console.WriteLine(J)
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
2]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (Integer V_0, //I
  Integer V_1) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.1
  IL_0006:  br.s       IL_0012
  IL_0008:  ldloc.1
  IL_0009:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  add.ovf
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.2
  IL_0014:  ble.s      IL_0008
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  add.ovf
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.2
  IL_001c:  ble.s      IL_0002
  IL_001e:  ret
}]]>)

        End Sub

        ' Exit for nested for loops
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ExitNestedFor()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = 1 To 2
                Exit For
            Next
            System.Console.WriteLine(I)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (Integer V_0, //I
  Integer V_1) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.2
  IL_0010:  ble.s      IL_0002
  IL_0012:  ret
}]]>)

        End Sub

        ' Continue for nested for loops
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ContinueNestedFor()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2
            For J = 1 To 2
                System.Console.WriteLine(I)
                Continue For
            Next
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
1
2
2]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (Integer V_0, //I
  Integer V_1) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.1
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000a:  ldloc.1
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.1
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.2
  IL_0010:  ble.s      IL_0004
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  add.ovf
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.2
  IL_0018:  ble.s      IL_0002
  IL_001a:  ret
}]]>)

        End Sub

        ' Use nothing as the start value
        <WorkItem(542033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542033")>
        <Fact>
        Public Sub NothingAsStart()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim x = 1
        For J = Nothing To 5 Step x
            System.Console.WriteLine(J)
            x = 2
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  6
  .locals init (Integer V_0, //x
  Object V_1,
  Object V_2) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.2
  IL_0003:  ldnull
  IL_0004:  ldc.i4.5
  IL_0005:  box        "Integer"
  IL_000a:  ldloc.0
  IL_000b:  box        "Integer"
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloca.s   V_2
  IL_0014:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0019:  brfalse.s  IL_0033
  IL_001b:  ldloc.2
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  call       "Sub System.Console.WriteLine(Object)"
  IL_0026:  ldc.i4.2
  IL_0027:  stloc.0
  IL_0028:  ldloc.2
  IL_0029:  ldloc.1
  IL_002a:  ldloca.s   V_2
  IL_002c:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_0031:  brtrue.s   IL_001b
  IL_0033:  ret
}
]]>)

        End Sub

        ' Use nothing as the end value
        <WorkItem(542033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542033")>
        <Fact>
        Public Sub NothingAsEnd()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim x = 1
        For J = 0 To Nothing Step x
            System.Console.WriteLine(J)
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       50 (0x32)
  .maxstack  6
  .locals init (Integer V_0, //x
  Object V_1,
  Object V_2) //J
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.2
  IL_0003:  ldc.i4.0
  IL_0004:  box        "Integer"
  IL_0009:  ldnull
  IL_000a:  ldloc.0
  IL_000b:  box        "Integer"
  IL_0010:  ldloca.s   V_1
  IL_0012:  ldloca.s   V_2
  IL_0014:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0019:  brfalse.s  IL_0031
  IL_001b:  ldloc.2
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  call       "Sub System.Console.WriteLine(Object)"
  IL_0026:  ldloc.2
  IL_0027:  ldloc.1
  IL_0028:  ldloca.s   V_2
  IL_002a:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_002f:  brtrue.s   IL_001b
  IL_0031:  ret
}
]]>)

        End Sub

        ' Use nothing as the step value
        <WorkItem(542033, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542033")>
        <Fact>
        Public Sub NothingAsStep()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For J = 0 To 5 Step Nothing
            System.Console.WriteLine(J)
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  6
  .locals init (Object V_0,
  Object V_1) //J
  IL_0000:  ldloc.1
  IL_0001:  ldc.i4.0
  IL_0002:  box        "Integer"
  IL_0007:  ldc.i4.5
  IL_0008:  box        "Integer"
  IL_000d:  ldnull
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldloca.s   V_1
  IL_0012:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0017:  brfalse.s  IL_002f
  IL_0019:  ldloc.1
  IL_001a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001f:  call       "Sub System.Console.WriteLine(Object)"
  IL_0024:  ldloc.1
  IL_0025:  ldloc.0
  IL_0026:  ldloca.s   V_1
  IL_0028:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_002d:  brtrue.s   IL_0019
  IL_002f:  ret
}
]]>)

        End Sub

        ' Use a function as the start and end value
        <WorkItem(542036, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542036")>
        <Fact>
        Public Sub FunctionCallAsStart()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For IntCounter = Function1(1) To Function1(50) Step Function1(1)
        Next IntCounter
    End Sub
    Shared Function Function1(ByRef arg)
        Function1 = arg * 2
    End Function
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  6
  .locals init (Object V_0,
  Object V_1, //IntCounter
  Object V_2)
  IL_0000:  ldloc.1
  IL_0001:  ldc.i4.1
  IL_0002:  box        "Integer"
  IL_0007:  stloc.2
  IL_0008:  ldloca.s   V_2
  IL_000a:  call       "Function MyClass1.Function1(ByRef Object) As Object"
  IL_000f:  ldc.i4.s   50
  IL_0011:  box        "Integer"
  IL_0016:  stloc.2
  IL_0017:  ldloca.s   V_2
  IL_0019:  call       "Function MyClass1.Function1(ByRef Object) As Object"
  IL_001e:  ldc.i4.1
  IL_001f:  box        "Integer"
  IL_0024:  stloc.2
  IL_0025:  ldloca.s   V_2
  IL_0027:  call       "Function MyClass1.Function1(ByRef Object) As Object"
  IL_002c:  ldloca.s   V_0
  IL_002e:  ldloca.s   V_1
  IL_0030:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0035:  brfalse.s  IL_0042
  IL_0037:  ldloc.1
  IL_0038:  ldloc.0
  IL_0039:  ldloca.s   V_1
  IL_003b:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_0040:  brtrue.s   IL_0037
  IL_0042:  ret
}
]]>)

        End Sub

        ' Overflow check while increase by steps
        <WorkItem(542041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542041")>
        <Fact>
        Public Sub OverflowCheck()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Class MyClass1
    Public Shared Sub Main()
        For ByteLooper As Byte = 250 To 255 Step 15
        Next ByteLooper
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Byte V_0) //ByteLooper
  IL_0000:  ldc.i4     0xfa
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   15
  IL_0009:  add
  IL_000a:  conv.ovf.u1.un
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4     0xff
  IL_0012:  ble.un.s   IL_0006
  IL_0014:  ret
}]]>)

        End Sub

        ' For With Object
        <WorkItem(542042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542042")>
        <Fact>
        Public Sub ObjectAsStart()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim ObjStart As Object = 1
        Dim ObjEndProp As Object = 50
        Dim ObjStep As Object = 2
        For Idx = ObjStart To ObjEndProp Step ObjStep
        Next Idx
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="").VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       51 (0x33)
  .maxstack  6
  .locals init (Object V_0, //ObjStart
  Object V_1, //ObjEndProp
  Object V_2, //ObjStep
  Object V_3,
  Object V_4) //Idx
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.s   50
  IL_0009:  box        "Integer"
  IL_000e:  stloc.1
  IL_000f:  ldc.i4.2
  IL_0010:  box        "Integer"
  IL_0015:  stloc.2
  IL_0016:  ldloc.s    V_4
  IL_0018:  ldloc.0
  IL_0019:  ldloc.1
  IL_001a:  ldloc.2
  IL_001b:  ldloca.s   V_3
  IL_001d:  ldloca.s   V_4
  IL_001f:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Object, Object, Object, Object, ByRef Object, ByRef Object) As Boolean"
  IL_0024:  brfalse.s  IL_0032
  IL_0026:  ldloc.s    V_4
  IL_0028:  ldloc.3
  IL_0029:  ldloca.s   V_4
  IL_002b:  call       "Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Object, Object, ByRef Object) As Boolean"
  IL_0030:  brtrue.s   IL_0026
  IL_0032:  ret
}
]]>)

        End Sub

        <WorkItem(542045, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542045")>
        <Fact>
        Public Sub EnumAsStart()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Public Class MyClass1
    Public Shared Sub Main()
        For x As e1 = e1.a To e1.c
        Next
    End Sub
End Class
Enum e1
    a
    b
    c
End Enum
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (e1 V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  ble.s      IL_0002
  IL_000a:  ret
}]]>)

        End Sub

        ' Declare a loop variable and initialize it with a property using the variable as argument
        <WorkItem(542046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542046")>
        <Fact>
        Public Sub PropertyAsStart()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer Off
Public Class MyClass1
    Property P1(ByVal x As Long) As Byte
        Get
            Return x - 10
        End Get
        Set(ByVal Value As Byte)
        End Set
    End Property
    Public Shared Sub Main()
    End Sub
    Public Sub Goo()
        For i As Integer = P1(30 + i) To 30
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="").VerifyIL("MyClass1.Goo", <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (Integer V_0) //i
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   30
  IL_0003:  ldloc.0
  IL_0004:  add.ovf
  IL_0005:  conv.i8
  IL_0006:  call       "Function MyClass1.get_P1(Long) As Byte"
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_0012
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  add.ovf
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.s   30
  IL_0015:  ble.s      IL_000e
  IL_0017:  ret
}]]>)

        End Sub

        ' Use a Field variable x that has the same name as the variable we are initializing
        <Fact>
        Public Sub FieldNameAsIteration()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Dim global_x As Integer = 10
    Const global_y As Long = 20
    Public Shared Sub Main()
        For global_x As Integer = global_y To 10
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:="").VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Integer V_0) //global_x
  IL_0000:  ldc.i4.s   20
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0009
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  add.ovf
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.s   10
  IL_000c:  ble.s      IL_0005
  IL_000e:  ret
}]]>)

        End Sub

        ' Use a Field variable x that has the same name as the variable we are initializing
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub FieldNameAsIteration_1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public y As Integer
    Public Shared Sub Main()
    End Sub
    Function Goo()
        For Me.y = 1 To 10
        Next
    End Function
End Class
    </file>
</compilation>, expectedOutput:="").VerifyIL("MyClass1.Goo", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  3
  .locals init (Object V_0) //Goo
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      "MyClass1.y As Integer"
  IL_0007:  ldarg.0
  IL_0008:  ldarg.0
  IL_0009:  ldfld      "MyClass1.y As Integer"
  IL_000e:  ldc.i4.1
  IL_000f:  add.ovf
  IL_0010:  stfld      "MyClass1.y As Integer"
  IL_0015:  ldarg.0
  IL_0016:  ldfld      "MyClass1.y As Integer"
  IL_001b:  ldc.i4.s   10
  IL_001d:  ble.s      IL_0007
  IL_001f:  ldloc.0
  IL_0020:  ret
}]]>)

        End Sub

        ' Use a global variable x that has the same name as the variable we are initializing via a
        ' function
        <WorkItem(542126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542126")>
        <WorkItem(528679, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528679")>
        <Fact>
        Public Sub GlobalNameAsIteration()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Const global_y As Long = 20
    Public Shared Sub Main()
    End Sub
    Function goo(ByRef x As Integer) As Integer
        x = x + 10
        Return x + 10
    End Function
    sub Goo1()
        For global_y As Integer = goo(global_y) To 30
        Next
    End sub
End Class
    </file>
</compilation>, expectedOutput:="").VerifyIL("MyClass1.Goo1", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (Integer V_0) //global_y
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  call       "Function MyClass1.goo(ByRef Integer) As Integer"
  IL_0008:  stloc.0
  IL_0009:  br.s       IL_000f
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.1
  IL_000d:  add.ovf
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.s   30
  IL_0012:  ble.s      IL_000b
  IL_0014:  ret
}]]>)

        End Sub

        ' Use the declared variable to initialize limit and step
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub UseDeclaredVarToLInitLimit()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For x As Integer = 10 To x + 20 Step x + 2
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
10
12
14
16
18
20]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (Integer V_0,
  Integer V_1,
  Integer V_2) //x
  IL_0000:  ldloc.2
  IL_0001:  ldc.i4.s   20
  IL_0003:  add.ovf
  IL_0004:  stloc.0
  IL_0005:  ldloc.2
  IL_0006:  ldc.i4.2
  IL_0007:  add.ovf
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.s   10
  IL_000b:  stloc.2
  IL_000c:  br.s       IL_0018
  IL_000e:  ldloc.2
  IL_000f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0014:  ldloc.2
  IL_0015:  ldloc.1
  IL_0016:  add.ovf
  IL_0017:  stloc.2
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.s   31
  IL_001b:  shr
  IL_001c:  ldloc.2
  IL_001d:  xor
  IL_001e:  ldloc.1
  IL_001f:  ldc.i4.s   31
  IL_0021:  shr
  IL_0022:  ldloc.0
  IL_0023:  xor
  IL_0024:  ble.s      IL_000e
  IL_0026:  ret
}
]]>)

        End Sub

        ' Iteration variable is a variable declared outside the loop
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub VarDeclaredOutOfLoop()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim x As Integer
        For x = 1 To 10
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   10
  IL_0009:  ble.s      IL_0002
  IL_000b:  ret
}]]>)
        End Sub

        ' Change limit value in for loop
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub ChangeLimitInloop()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim x = 5
        For J = 1 To x
            System.Console.WriteLine(J)
            x += 10
        Next
    End Sub
End Class
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
3
4
5]]>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (Integer V_0, //x
  Integer V_1,
  Integer V_2) //J
  IL_0000:  ldc.i4.5
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  stloc.2
  IL_0006:  br.s       IL_0017
  IL_0008:  ldloc.2
  IL_0009:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.s   10
  IL_0011:  add.ovf
  IL_0012:  stloc.0
  IL_0013:  ldloc.2
  IL_0014:  ldc.i4.1
  IL_0015:  add.ovf
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  ldloc.1
  IL_0019:  ble.s      IL_0008
  IL_001b:  ret
}
]]>)

        End Sub

        ' Whole For loop on the same line
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub SingleLine()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For x As Integer = 0 To 10 : Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   10
  IL_0009:  ble.s      IL_0002
  IL_000b:  ret
}]]>)
        End Sub

        ' For statement is split in every possible place
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub SplitForLoop()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For _
x _
As _
Integer _
= _
0 _
To _
10
        Next
    End Sub
End Class
    </file>
</compilation>).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   10
  IL_0009:  ble.s      IL_0002
  IL_000b:  ret
}]]>)
        End Sub

        ' Infinite loop
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub InfiniteLoop()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Module MyClass1
    Sub Main(args As String())
        For i As Integer = 0 To 1 Step 0
        Next
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       11 (0xb)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.0
  IL_0004:  add.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  ble.s      IL_0002
  IL_000a:  ret
}]]>)

        End Sub

        ' Infinite loop
        <WorkItem(542032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542032")>
        <Fact>
        Public Sub InfiniteLoop_1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Module MyClass1
    Sub Main(args As String())
        For i As Integer = 0 To 2
            i = i - 1
        Next
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  sub.ovf
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  add.ovf
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.2
  IL_000c:  ble.s      IL_0002
  IL_000e:  ret
}]]>)

        End Sub

        ' The Step expression must evaluate to a value that can be added to the iteration variable
        <Fact>
        Public Sub SimpleTest_2()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Option Infer Off
Module MyClass1
    Sub Main(args As String())
        For i As Integer = 0 To -1 Step 1
            System.Console.WriteLine(i)
        Next
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MyClass1.Main", <![CDATA[
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (Integer V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_000e
  IL_0004:  ldloc.0
  IL_0005:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  add.ovf
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.m1
  IL_0010:  ble.s      IL_0004
  IL_0012:  ret
}]]>)

        End Sub

        <Fact>
        Public Sub NullableEnum()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System        
Module Program

    Enum e1
        a
        b
        c
    End Enum

    Sub main()
        For i As e1? = e1.a To e1.c
            Console.WriteLine(i)
        Next
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
a
b
c
]]>).VerifyIL("Program.main", <![CDATA[
{
  // Code size      175 (0xaf)
  .maxstack  3
  .locals init (Program.e1? V_0,
  Program.e1? V_1,
  Boolean V_2,
  Program.e1? V_3, //i
  Program.e1? V_4)
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     "Sub Program.e1?..ctor(Program.e1)"
  IL_0006:  ldloca.s   V_0
  IL_0008:  ldc.i4.2
  IL_0009:  call       "Sub Program.e1?..ctor(Program.e1)"
  IL_000e:  ldloca.s   V_1
  IL_0010:  ldc.i4.1
  IL_0011:  call       "Sub Program.e1?..ctor(Program.e1)"
  IL_0016:  ldloca.s   V_1
  IL_0018:  call       "Function Program.e1?.get_HasValue() As Boolean"
  IL_001d:  brfalse.s  IL_002e
  IL_001f:  ldloca.s   V_1
  IL_0021:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_0026:  ldc.i4.0
  IL_0027:  clt
  IL_0029:  ldc.i4.0
  IL_002a:  ceq
  IL_002c:  br.s       IL_002f
  IL_002e:  ldc.i4.0
  IL_002f:  stloc.2
  IL_0030:  stloc.3
  IL_0031:  br.s       IL_0070
  IL_0033:  ldloc.3
  IL_0034:  box        "Program.e1?"
  IL_0039:  call       "Sub System.Console.WriteLine(Object)"
  IL_003e:  ldloca.s   V_1
  IL_0040:  call       "Function Program.e1?.get_HasValue() As Boolean"
  IL_0045:  ldloca.s   V_3
  IL_0047:  call       "Function Program.e1?.get_HasValue() As Boolean"
  IL_004c:  and
  IL_004d:  brtrue.s   IL_005b
  IL_004f:  ldloca.s   V_4
  IL_0051:  initobj    "Program.e1?"
  IL_0057:  ldloc.s    V_4
  IL_0059:  br.s       IL_006f
  IL_005b:  ldloca.s   V_3
  IL_005d:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_0062:  ldloca.s   V_1
  IL_0064:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_0069:  add
  IL_006a:  newobj     "Sub Program.e1?..ctor(Program.e1)"
  IL_006f:  stloc.3
  IL_0070:  ldloca.s   V_0
  IL_0072:  call       "Function Program.e1?.get_HasValue() As Boolean"
  IL_0077:  ldloca.s   V_3
  IL_0079:  call       "Function Program.e1?.get_HasValue() As Boolean"
  IL_007e:  and
  IL_007f:  brfalse.s  IL_00ae
  IL_0081:  ldloc.2
  IL_0082:  brtrue.s   IL_0099
  IL_0084:  ldloca.s   V_3
  IL_0086:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_008b:  ldloca.s   V_0
  IL_008d:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_0092:  clt
  IL_0094:  ldc.i4.0
  IL_0095:  ceq
  IL_0097:  br.s       IL_00ac
  IL_0099:  ldloca.s   V_3
  IL_009b:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_00a0:  ldloca.s   V_0
  IL_00a2:  call       "Function Program.e1?.GetValueOrDefault() As Program.e1"
  IL_00a7:  cgt
  IL_00a9:  ldc.i4.0
  IL_00aa:  ceq
  IL_00ac:  brtrue.s   IL_0033
  IL_00ae:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub EnumNonconstantStep()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
imports System        
Module Program

    Enum e1
        a
        b
        c
    End Enum

    Sub main()
        For i As e1 = e1.a To e1.c Step goo()
            Console.WriteLine(i)
        Next
    End Sub

    Function goo() As e1
        Return 1
    End Function
End Module


    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
2
]]>).VerifyIL("Program.main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  .locals init (Program.e1 V_0,
  Program.e1 V_1) //i
  IL_0000:  call       "Function Program.goo() As Program.e1"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0014
  IL_000a:  ldloc.1
  IL_000b:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0010:  ldloc.1
  IL_0011:  ldloc.0
  IL_0012:  add
  IL_0013:  stloc.1
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.s   31
  IL_0017:  shr
  IL_0018:  ldloc.1
  IL_0019:  xor
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.s   31
  IL_001d:  shr
  IL_001e:  ldc.i4.2
  IL_001f:  xor
  IL_0020:  ble.s      IL_000a
  IL_0022:  ret
}
]]>)

        End Sub

        <Fact>
        Public Sub EnumNonconstantStep1()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
imports System        
Module Program

    Enum e1 as ushort
        a
        b
        c
    End Enum

    Sub main()
        For i As e1 = e1.a To e1.c Step goo()
            Console.WriteLine(i)
        Next
    End Sub

    Function goo() As e1
        Return 1
    End Function
End Module


    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
2
]]>).VerifyIL("Program.main", <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (Program.e1 V_0,
  Program.e1 V_1) //i
  IL_0000:  call       "Function Program.goo() As Program.e1"
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  call       "Sub System.Console.WriteLine(Integer)"
  IL_000e:  ldloc.1
  IL_000f:  ldloc.0
  IL_0010:  add
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.2
  IL_0014:  ble.s      IL_0008
  IL_0016:  ret
}
]]>)

        End Sub

        ' For With Object
        <Fact>
        Public Sub Regress246410()
            Dim TEMP = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict Off

Imports System

Module BugVSW246410
    Sub Main()
        Console.Write("Bug VSW_246410 (a): ")
        Try
            Dim cls1_step As ForCls1 = New ForCls1(1)
            Dim cls1_start As ForCls1 = New ForCls1(10)
            Dim cls1_limit As ForCls1 = New ForCls1(20)
            Dim Result As Object

            For Result = cls1_start To cls1_limit Step cls1_step
            Next

            If CType(Result, ForCls1).m_value = 21 Then
                Console.WriteLine("Passed!!!")
            Else
                Console.WriteLine("Failed!!!")
            End If
        Catch Ex As Exception
            Console.WriteLine("Failed!!!")
        End Try
    End Sub

    Class ForCls1
        Public m_value As Integer
        Sub New(ByVal n As Integer)
            m_value = n
        End Sub
        Shared Operator +(ByVal x As ForCls1, ByVal y As ForCls1) As ForCls1
            Return New ForCls1(x.m_value + y.m_value)
        End Operator
        Shared Operator -(ByVal x As ForCls1, ByVal y As ForCls1) As ForCls1
            Return New ForCls1(x.m_value - y.m_value)
        End Operator
        Shared Operator &gt;=(ByVal x As ForCls1, ByVal y As ForCls1) As Boolean
            Return x.m_value &gt;= y.m_value
        End Operator
        Shared Operator &lt;=(ByVal x As ForCls1, ByVal y As ForCls1) As Boolean
            Return x.m_value  &lt;= y.m_value
        End Operator
    End Class
End Module

    </file>
</compilation>, expectedOutput:="Bug VSW_246410 (a): Passed!!!")

        End Sub
    End Class
End Namespace

