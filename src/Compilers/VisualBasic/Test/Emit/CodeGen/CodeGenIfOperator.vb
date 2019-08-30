' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenIfOperator
        Inherits BasicTestBase

        ' Conditional operator as parameter
        <Fact>
        Public Sub ConditionalOperatorAsParameter()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module C
    Sub Main(args As String())
        Dim a0 As Boolean = False
        Dim a1 As Integer = 0
        Dim a2 As Long = 1
        Dim b0 = a0
        Dim b1 = a1
        Dim b2 = a2
        Console.WriteLine((If(b0, b1, b2)) &lt;&gt; (If(a0, a1, a2)))
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
False
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  3
  .locals init (Boolean V_0, //a0
  Integer V_1, //a1
  Long V_2, //a2
  Object V_3, //b1
  Object V_4) //b2
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  conv.i8
  IL_0006:  stloc.2
  IL_0007:  ldloc.0
  IL_0008:  box        "Boolean"
  IL_000d:  ldloc.1
  IL_000e:  box        "Integer"
  IL_0013:  stloc.3
  IL_0014:  ldloc.2
  IL_0015:  box        "Long"
  IL_001a:  stloc.s    V_4
  IL_001c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0021:  brtrue.s   IL_0027
  IL_0023:  ldloc.s    V_4
  IL_0025:  br.s       IL_0028
  IL_0027:  ldloc.3
  IL_0028:  ldloc.0
  IL_0029:  brtrue.s   IL_002e
  IL_002b:  ldloc.2
  IL_002c:  br.s       IL_0030
  IL_002e:  ldloc.1
  IL_002f:  conv.i8
  IL_0030:  box        "Long"
  IL_0035:  ldc.i4.0
  IL_0036:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectNotEqual(Object, Object, Boolean) As Object"
  IL_003b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0040:  call       "Sub System.Console.WriteLine(Object)"
  IL_0045:  ret
}
]]>).Compilation

        End Sub

        ' Function call in return expression
        <WorkItem(541647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541647")>
        <Fact()>
        Public Sub FunctionCallAsArgument()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
            Imports System
            Module C
                Sub Main(args As String())
                    Dim z = If(True, fun_Exception(1), fun_int(1))
                    Dim r = If(True, fun_long(0), fun_int(1))
                    Dim s = If(False, fun_long(0), fun_int(1))
                End Sub
                Private Function fun_int(x As Integer) As Integer
                    Return x
                End Function
                Private Function fun_long(x As Integer) As Long
                    Return CLng(x)
                End Function
                Private Function fun_Exception(x As Integer) As Exception
                    Return New Exception()
                End Function
            End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Function C.fun_Exception(Integer) As System.Exception"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  pop
  IL_000c:  ldc.i4.0
  IL_000d:  call       "Function C.fun_long(Integer) As Long"
  IL_0012:  pop
  IL_0013:  ldc.i4.1
  IL_0014:  call       "Function C.fun_int(Integer) As Integer"
  IL_0019:  conv.i8
  IL_001a:  pop
  IL_001b:  ret
}]]>)
        End Sub

        ' Lambda works  in return argument
        <Fact()>
        Public Sub LambdaAsArgument_1()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module C
    Sub Main(args As String())
        Dim Y = 2
        Dim S = If(True, _
Function(z As Integer) As Integer
    System.Console.WriteLine("SUB")
    Return z * z
End Function, Y + 1)
        S = If(False, _
Sub(Z As Integer)
    System.Console.WriteLine("SUB")
End Sub, Y + 1)
        System.Console.WriteLine(S)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       77 (0x4d)
  .maxstack  2
  .locals init (Object V_0) //Y
  IL_0000:  ldc.i4.2
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_0013:  br.s       IL_002b
  IL_0015:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_001a:  ldftn      "Function C._Closure$__._Lambda$__0-0(Integer) As Integer"
  IL_0020:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_0025:  dup
  IL_0026:  stsfld     "C._Closure$__.$I0-0 As <generated method>"
  IL_002b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0030:  pop
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4.1
  IL_0033:  box        "Integer"
  IL_0038:  call       "Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object"
  IL_003d:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0042:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0047:  call       "Sub System.Console.WriteLine(Object)"
  IL_004c:  ret
}
]]>).Compilation

        End Sub

        ' Conditional on expression tree
        <Fact()>
        Public Sub ExpressionTree()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer On
Imports System
Imports System.Linq.Expressions
Module Program
    Sub Main(args As String())
        Dim testExpr As Expression(Of Func(Of Boolean, Long, Integer, Long)) = Function(x, y, z) If(x, y, z)
        Dim testFunc = testExpr.Compile()
        Dim testResult = testFunc(False, CLng(3), 100)
        Console.WriteLine(testResult)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
100
]]>).Compilation

        End Sub

        ' Conditional on expression tree
        <Fact()>
        Public Sub ExpressionTree_2()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer on
Imports System
Imports System.Linq.Expressions
Module Program
    Sub Main(args As String())
        Dim testExpr As Expression(Of Func(Of TestStruct, Long?, Integer, Integer?)) = Function(x, y, z) If(x, y, z)
        Dim testFunc = testExpr.Compile()
        Dim testResult1 = testFunc(New TestStruct(), Nothing, 10)
        Dim testResult2 = testFunc(New TestStruct(), 10, Nothing)
        Console.WriteLine (testResult1)
        Console.WriteLine (testResult2)
    End Sub
End Module
Public Structure TestStruct
    Public Shared Operator IsTrue(ts As TestStruct) As Boolean
        Return False
    End Operator

    Public Shared Operator IsFalse(ts As TestStruct) As Boolean
        Return True
    End Operator
End Structure
    </file>
</compilation>, expectedOutput:=<![CDATA[
10
0
]]>).Compilation

        End Sub

        ' Multiple conditional  operator in expression
        <Fact>
        Public Sub MultipleConditional()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module C
    Sub Main(args As String())
        Dim S = If(False, 1, If(True, 2, 3))
        Console.Write(S)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
]]>).VerifyIL("C.Main", <![CDATA[
                {
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  call       "Sub System.Console.Write(Object)"
  IL_0010:  ret
}]]>).Compilation

        End Sub

        ' Arguments are of types: bool, constant, enum
        <Fact>
        Public Sub EnumAsArguments()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module C
    Sub Main(args As String())
        Dim testResult = If(False, 0, color.Blue)
        Console.WriteLine(testResult)
        testResult = If(False, 5, color.Blue)
        Console.WriteLine(testResult)
    End Sub
End Module
Enum color
    Red
    Green
    Blue
End Enum
    </file>
</compilation>, expectedOutput:=<![CDATA[
2
2
]]>).VerifyIL("C.Main", <![CDATA[
                {
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0010:  ldc.i4.2
  IL_0011:  box        "Integer"
  IL_0016:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_001b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0020:  ret
}]]>).Compilation

        End Sub

        ' Implicit type conversion on conditional
        <Fact>
        Public Sub ImplicitConversionForConditional()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main(args As String())
        Dim valueFromDatabase As Object
        Dim result As Decimal
        valueFromDatabase = DBNull.Value
        result = CDec(If(valueFromDatabase IsNot DBNull.Value, valueFromDatabase, 0))
        Console.WriteLine(result)
        result = (If(valueFromDatabase IsNot DBNull.Value, CDec(valueFromDatabase), CDec(0)))
        Console.WriteLine(result)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
0
]]>).VerifyIL("C.Main", <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (Object V_0) //valueFromDatabase
  IL_0000:  ldsfld     "System.DBNull.Value As System.DBNull"
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldsfld     "System.DBNull.Value As System.DBNull"
  IL_000c:  bne.un.s   IL_0016
  IL_000e:  ldc.i4.0
  IL_000f:  box        "Integer"
  IL_0014:  br.s       IL_0017
  IL_0016:  ldloc.0
  IL_0017:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDecimal(Object) As Decimal"
  IL_001c:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_0021:  ldloc.0
  IL_0022:  ldsfld     "System.DBNull.Value As System.DBNull"
  IL_0027:  bne.un.s   IL_0030
  IL_0029:  ldsfld     "Decimal.Zero As Decimal"
  IL_002e:  br.s       IL_0036
  IL_0030:  ldloc.0
  IL_0031:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToDecimal(Object) As Decimal"
  IL_0036:  call       "Sub System.Console.WriteLine(Decimal)"
  IL_003b:  ret
}]]>).Compilation

        End Sub

        ' Not boolean type as conditional-argument
        <WorkItem(541647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541647")>
        <Fact()>
        Public Sub NotBooleanAsConditionalArgument()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main(args As String())
        Dim x = 1
        Dim s = If("", x, 2)
        'invalid
        s = If("True", x, 2)
        'valid
        s = If("1", x, 2)
        'valid
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       36 (0x24)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""
  IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(String) As Boolean"
  IL_000c:  pop
  IL_000d:  ldstr      "True"
  IL_0012:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(String) As Boolean"
  IL_0017:  pop
  IL_0018:  ldstr      "1"
  IL_001d:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(String) As Boolean"
  IL_0022:  pop
  IL_0023:  ret
}]]>).Compilation

        End Sub

        ' Not boolean type as conditional-argument
        <WorkItem(541647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541647")>
        <Fact()>
        Public Sub NotBooleanAsConditionalArgument_2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main(args As String())
        Dim x = 1
        Dim s = If(color.Green, x, 2)
        Console.WriteLine(S)
        s = If(color.Red, x, 2)
        Console.WriteLine(s)
    End Sub
End Module
Public Enum color
    Red
    Blue
    Green
End Enum
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
2
]]>)
        End Sub

        <WorkItem(541647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541647")>
        <Fact>
        Public Sub FunctionWithNoReturnType()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module C
    Sub Main(args As String())
        Dim x = 1
        Dim s = If(fun1(), x, 2)
    End Sub
    Private Function fun1()
    End Function
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (Object V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  stloc.0
  IL_0007:  call       "Function C.fun1() As Object"
  IL_000c:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
  IL_0011:  brtrue.s   IL_001b
  IL_0013:  ldc.i4.2
  IL_0014:  box        "Integer"
  IL_0019:  br.s       IL_001c
  IL_001b:  ldloc.0
  IL_001c:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0021:  pop
  IL_0022:  ret
}]]>).Compilation

        End Sub

        ' Const as conditional- argument
        <WorkItem(541452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541452")>
        <Fact()>
        Public Sub ConstAsArgument()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module C
    Sub Main(args As String())
        Const con As Boolean = True
        Dim s1 = If(con, 1, 2)
        Console.Write(s1)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("C.Main", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       "Sub System.Console.Write(Integer)"
  IL_0006:  ret
}
]]>).Compilation

        End Sub

        ' Const i As Integer = IF
        <Fact>
        Public Sub AssignIfToConst()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Const s As Integer = If(1>2, 9, 92)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Program.Main", <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}]]>).Compilation
        End Sub

        ' IF used in Redim
        <WorkItem(528563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528563")>
        <Fact>
        Public Sub IfUsedInRedim()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Imports System
Module Program
    Sub Main(args As String())
        Dim s1 As Integer()
        ReDim Preserve s1(If(True, 10, 101))
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  .locals init (Integer() V_0) //s1
  IL_0000:  ldloc.0
  IL_0001:  ldc.i4.s   11
  IL_0003:  newarr     "Integer"
  IL_0008:  call       "Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array"
  IL_000d:  castclass  "Integer()"
  IL_0012:  stloc.0
  IL_0013:  ret
}]]>).Compilation
        End Sub

        ' IF on attribute
        <Fact>
        Public Sub IFOnAttribute()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
&lt;Assembly: CLSCompliant(If(True, False, True))&gt;
Public Class base
    Public Shared sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyDiagnostics()
        End Sub

        ' #const val =if
        <Fact>
        Public Sub PredefinedConst()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Public Class Program
    Public Shared Sub Main()
#Const ifconst = If(True, 1, 2)
#If ifconst = 1 Then
#End If
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Program.Main", <![CDATA[
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}]]>).Compilation
        End Sub

        ' IF as  function name
        <Fact>
        Public Sub IFAsFunctionName()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Module M1
    Sub Main()
    End Sub
    Public Function [if](ByVal arg As String) As String
        Return arg
    End Function
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("M1.if", <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}]]>).Compilation
        End Sub

        ' IF as Optional parameter
        <Fact()>
        Public Sub IFAsOptionalParameter()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Module M1
    Sub Main()
        goo()
    End Sub
    Public Sub goo(Optional ByVal arg As String = If(False, "6", "61"))
        System.Console.WriteLine(arg)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="61").Compilation
        End Sub

        ' IF used in For step
        <Fact()>
        Public Sub IFUsedInForStep()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        Dim s10 As Boolean = False
        For c As Integer = 1 To 10 Step If(s10, 1, 2)
        Next
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe)
        End Sub

        ' Passing IF as byref arg
        <WorkItem(541647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541647")>
        <Fact()>
        Public Sub IFAsByrefArg()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Module M1
    Sub Main()
        Dim X = "123"
        Dim Y = "456"
        Dim Z = If(1 > 2, goo(X), goo(Y))
    End Sub
    Private Function goo(ByRef p1 As String)
        p1 = "HELLO"
    End Function
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("M1.Main", <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (String V_0, //X
  String V_1) //Y
  IL_0000:  ldstr      "123"
  IL_0005:  stloc.0
  IL_0006:  ldstr      "456"
  IL_000b:  stloc.1
  IL_000c:  ldloca.s   V_1
  IL_000e:  call       "Function M1.goo(ByRef String) As Object"
  IL_0013:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0018:  pop
  IL_0019:  ret
}]]>)
        End Sub

        <WorkItem(541674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541674")>
        <Fact>
        Public Sub TypeConversionInRuntime()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Collections.Generic
Public Class Test
    Private Shared Sub Main()
        Dim a As Integer() = New Integer() {}
        Dim b As New List(Of Integer)()
        Dim c As IEnumerable(Of Integer) = If(a.Length > 0, a, DirectCast(b, IEnumerable(Of Integer)))
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseDll).VerifyIL("Test.Main", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (Integer() V_0, //a
  System.Collections.Generic.List(Of Integer) V_1) //b
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     "Integer"
  IL_0006:  stloc.0
  IL_0007:  newobj     "Sub System.Collections.Generic.List(Of Integer)..ctor()"
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  ldlen
  IL_000f:  conv.i4
  IL_0010:  ldc.i4.0
  IL_0011:  bgt.s      IL_0016
  IL_0013:  ldloc.1
  IL_0014:  pop
  IL_0015:  ret
  IL_0016:  ldloc.0
  IL_0017:  pop
  IL_0018:  ret
}
]]>).Compilation
        End Sub

        <WorkItem(541673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541673")>
        <Fact>
        Public Sub TypeConversionInRuntime_1()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Public Interface IB
    Function f() As Integer
End Interface
Public Class AB1
    Implements IB
    Public Function f() As Integer Implements IB.f
        Return 42
    End Function
End Class
Public Class AB2
    Implements IB
    Public Function f() As Integer Implements IB.f
        Return 1
    End Function
End Class
Class MainClass
    Public Shared Sub g(p As Boolean)
        Dim x = (If(p, DirectCast(New AB1(), IB), DirectCast(New AB2(), IB))).f()
    End Sub
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MainClass.g", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (IB V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  newobj     "Sub AB2..ctor()"
  IL_0008:  br.s       IL_0011
  IL_000a:  newobj     "Sub AB1..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Function IB.f() As Integer"
  IL_0016:  box        "Integer"
  IL_001b:  pop
  IL_001c:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInRuntime_2()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Public Interface IB
    Function f() As Integer
End Interface
Public Class AB1
    Implements IB
    Public Function f() As Integer Implements IB.f
        Return 42
    End Function
End Class
Class MainClass
    Public Shared Sub g(p As Boolean)
        Dim x = (If(p, DirectCast(New AB1(), IB), DirectCast(New AB1(), IB))).f()
    End Sub
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MainClass.g", <![CDATA[
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (IB V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  newobj     "Sub AB1..ctor()"
  IL_0008:  br.s       IL_0011
  IL_000a:  newobj     "Sub AB1..ctor()"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  callvirt   "Function IB.f() As Integer"
  IL_0016:  box        "Integer"
  IL_001b:  pop
  IL_001c:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInRuntime_3()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Public Interface IB
    Function f() As Integer
End Interface
Public Class AB1
    Implements IB
    Public Function f() As Integer Implements IB.f
        Return 42
    End Function
End Class

Class MainClass
    Public Shared Sub g(p As Boolean)
        Dim x = (If(DirectCast(New AB1(), IB), DirectCast(New AB1(), IB))).f()
    End Sub
    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("MainClass.g", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (IB V_0)
  IL_0000:  newobj     "Sub AB1..ctor()"
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0010
  IL_0008:  pop
  IL_0009:  newobj     "Sub AB1..ctor()"
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  callvirt   "Function IB.f() As Integer"
  IL_0015:  box        "Integer"
  IL_001a:  pop
  IL_001b:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBase
    Function m1() As IBase
End Interface
Class Derived
    Public Shared mask As Integer = 1
End Class
Class Test1
    Implements IBase
    Private y As IBase
    Private x As IBase
    Private cnt As Integer = 0
    Public Sub New(link As IBase)
        x = Me
        y = link
        If y Is Nothing Then
            y = Me
        End If
    End Sub
    Public Shared Sub Main()
    End Sub
    Public Function m1() As IBase Implements IBase.m1
        Return If(Derived.mask = 0, x, y)
    End Function
    'version1 (explicit impl in original repro)
    Public Function m2() As IBase
        Return If(Derived.mask = 0, Me, y)
    End Function
    'version2
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Test1.m1", <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldsfld     "Derived.mask As Integer"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "Test1.y As IBase"
  IL_000d:  ret
  IL_000e:  ldarg.0
  IL_000f:  ldfld      "Test1.x As IBase"
  IL_0014:  ret
}]]>).VerifyIL("Test1.m2", <![CDATA[
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldsfld     "Derived.mask As Integer"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "Test1.y As IBase"
  IL_000d:  ret
  IL_000e:  ldarg.0
  IL_000f:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_1()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBase
    Function m1() As IBase
End Interface
Class Derived
    Public Shared mask As Integer = 1
End Class
Class Test1
    Implements IBase
    Private y As IBase
    Private x As IBase
    Private cnt As Integer = 0
    Public Sub New(link As IBase)
        x = Me
        y = link
        If y Is Nothing Then
            y = Me
        End If
    End Sub
    Public Shared Sub Main()
    End Sub
    Public Function m1() As IBase Implements IBase.m1
        Return If(x, y)
    End Function
    'version1 (explicit impl in original repro)
    Public Function m2() As IBase
        Return If(Me, y)
    End Function
    'version2
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Test1.m1", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Test1.x As IBase"
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0010
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "Test1.y As IBase"
  IL_0010:  ret
}]]>).VerifyIL("Test1.m2", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000b
  IL_0004:  pop
  IL_0005:  ldarg.0
  IL_0006:  ldfld      "Test1.y As IBase"
  IL_000b:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_2()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBase
    Function m1() As IBase
End Interface
Class Derived
    Public Shared mask As Integer = 1
End Class
Class Test1
    Implements IBase
    Private y As IBase
    Private x As IBase
    Private cnt As Integer = 0
    Public Sub New(link As IBase)
        x = Me
        y = link
        If y Is Nothing Then
            y = Me
        End If
    End Sub
    Public Shared Sub Main()
    End Sub
    Public Function m1() As IBase Implements IBase.m1
        Return If (x, DirectCast(y, IBase))
    End Function
    'version1 (explicit impl in original repro)
    Public Function m2() As IBase
        Return If (DirectCast(Me, IBase), y)
    End Function
    'version2
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Test1.m1", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Test1.x As IBase"
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0010
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "Test1.y As IBase"
  IL_0010:  ret
}]]>).VerifyIL("Test1.m2", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000b
  IL_0004:  pop
  IL_0005:  ldarg.0
  IL_0006:  ldfld      "Test1.y As IBase"
  IL_000b:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_3()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System.Collections
Imports System.Collections.Generic

Class Test1
    Implements IEnumerable
    Dim x As IEnumerator
    Dim y As IEnumerator
    Public Shared Sub Main()
    End Sub
    Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return If(Me IsNot Nothing, DirectCast(x, IEnumerator), DirectCast(y, IEnumerator))
    End Function
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("Test1.GetEnumerator", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldarg.0
  IL_0004:  ldfld      "Test1.y As System.Collections.IEnumerator"
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldfld      "Test1.x As System.Collections.IEnumerator"
  IL_0010:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_4()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBase
    Function GetEnumerator() As IBase
End Interface
Interface IDerived
    Inherits IBase
End Interface
Structure struct
    Implements IBase
    Public Shared Sub Main()
    End Sub
    Function GetEnumerator() As IBase Implements IBase.GetEnumerator
        Dim x As IDerived
        Dim y As IDerived
        Return If(x IsNot Nothing, DirectCast(x, IBase), DirectCast(y, IBase))
    End Function
End Structure
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("struct.GetEnumerator", <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (IDerived V_0, //x
  IDerived V_1, //y
  IBase V_2)
  IL_0000:  ldloc.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldloc.1
  IL_0004:  ret
  IL_0005:  ldloc.0
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  ret
}
]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_5()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Interface IBase
    Function GetEnumerator() As IBase
End Interface
Interface IDerived
    Inherits IBase
End Interface
Structure struct
    Implements IDerived
    Public Shared Sub Main()
    End Sub
    Function GetEnumerator() As IBase Implements IBase.GetEnumerator
        Dim x As IDerived
        Dim y As IDerived
        Return If(x IsNot Nothing, DirectCast(x, IDerived), DirectCast(y, IDerived))
    End Function
End Structure
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("struct.GetEnumerator", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (IDerived V_0, //x
  IDerived V_1) //y
  IL_0000:  ldloc.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldloc.1
  IL_0004:  ret
  IL_0005:  ldloc.0
  IL_0006:  ret
}]]>).Compilation
        End Sub

        <Fact>
        Public Sub TypeConversionInterface_6()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Infer Off
Interface IBase
    Function GetEnumerator() As IBase
End Interface
Interface IDerived
    Inherits IBase
End Interface
Structure struct
    Implements IDerived
    Public Shared Sub Main()
    End Sub
    Function GetEnumerator() As IBase Implements IBase.GetEnumerator
        Dim x = GetInterface()
        Dim y = GetInterface()
        Dim z = If(x IsNot Nothing, DirectCast(x, IBase), DirectCast(y, IBase))
    End Function
    Function GetInterface() As IBase
        Return Nothing
    End Function
End Structure
    </file>
</compilation>, options:=TestOptions.ReleaseExe).VerifyIL("struct.GetEnumerator", <![CDATA[
{
  // Code size       34 (0x22)
  .maxstack  1
  .locals init (IBase V_0, //GetEnumerator
  Object V_1, //x
  Object V_2) //y
  IL_0000:  ldarg.0
  IL_0001:  call       "Function struct.GetInterface() As IBase"
  IL_0006:  stloc.1
  IL_0007:  ldarg.0
  IL_0008:  call       "Function struct.GetInterface() As IBase"
  IL_000d:  stloc.2
  IL_000e:  ldloc.1
  IL_000f:  brtrue.s   IL_0019
  IL_0011:  ldloc.2
  IL_0012:  castclass  "IBase"
  IL_0017:  br.s       IL_001f
  IL_0019:  ldloc.1
  IL_001a:  castclass  "IBase"
  IL_001f:  pop
  IL_0020:  ldloc.0
  IL_0021:  ret
}]]>).Compilation
        End Sub

        <Fact, WorkItem(545065, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545065")>
        Public Sub IfOnConstrainedMethodTypeParameter()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Friend Module BIFOpResult0011mod

        Public Sub scen7(Of T As Class)(ByVal arg As T)
            Dim s7_a As T = arg
            Dim s7_b As T = Nothing
            Dim s7_c = If(s7_a, s7_b)
            System.Console.Write(s7_c)
        End Sub

        Sub Main()
            scen7(Of String)("Q")
        End Sub
    End Module
    </file>
</compilation>, expectedOutput:="Q").VerifyIL("BIFOpResult0011mod.scen7", <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (T V_0) //s7_b
  IL_0000:  ldarg.0
  IL_0001:  ldloca.s   V_0
  IL_0003:  initobj    "T"
  IL_0009:  dup
  IL_000a:  box        "T"
  IL_000f:  brtrue.s   IL_0013
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  box        "T"
  IL_0018:  call       "Sub System.Console.Write(Object)"
  IL_001d:  ret
}]]>)
        End Sub

        <Fact>
        Public Sub IfOnUnconstrainedMethodTypeParameter()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Friend Module Mod1
    Sub M1(Of T)(arg1 As T, arg2 As T)
        System.Console.WriteLine(If(arg1, arg2))
    End Sub

    Sub M2(Of T1, T2 As T1)(arg1 as T1, arg2 As T2)
        System.Console.WriteLine(If(arg2, arg1))
    End Sub

    Sub Main()
        M1(Nothing, 1000)
        M1(1, 1000)
        M1(Nothing, "String Parameter 1")
        M1("String Parameter 2", "Should not print")
        M1(Of Integer?)(Nothing, 4)
        M1(Of Integer?)(5, 1000)
        M2(1000, 6)
        M2(Of Object, Integer?)(7, Nothing)
        M2(Of Object, Integer?)(1000, 8)
        M2(Of Integer?, Integer?)(9, Nothing)
        M2(Of Integer?, Integer?)(1000, 10)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
0
1
String Parameter 1
String Parameter 2
4
5
6
7
8
9
10
]]>).VerifyIL("Mod1.M1", <![CDATA[
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  box        "T"
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ldarg.1
  IL_000b:  box        "T"
  IL_0010:  call       "Sub System.Console.WriteLine(Object)"
  IL_0015:  ret
}
]]>).VerifyIL("Mod1.M2", <![CDATA[
{
  // Code size       33 (0x21)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        "T2"
  IL_0006:  brtrue.s   IL_000b
  IL_0008:  ldarg.0
  IL_0009:  br.s       IL_0016
  IL_000b:  ldarg.1
  IL_000c:  box        "T2"
  IL_0011:  unbox.any  "T1"
  IL_0016:  box        "T1"
  IL_001b:  call       "Sub System.Console.WriteLine(Object)"
  IL_0020:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub IfOnUnconstrainedTypeParameterWithNothingLHS()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Friend Module Mod1
        Sub M1(Of T)(arg As T)
            Console.WriteLine(If(Nothing, arg))
        End Sub

        Sub Main()
            ' Note that this behavior is different than C#'s behavior. This is consistent with Roslyn's handling
            ' of If(Nothing, 1), which will evaluate to 1
            M1(1)
            Console.WriteLine(If(Nothing, 1))
            M1("String Parameter 1")
            M1(Of Integer?)(3)
        End Sub
    End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
1
1
String Parameter 1
3
]]>).VerifyIL("Mod1.M1", <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        "T"
  IL_0006:  call       "Sub System.Console.WriteLine(Object)"
  IL_000b:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub IfOnUnconstrainedTypeParametersOldLanguageVersion()
            CreateCompilation(
<compilation>
    <file name="a.vb">
Friend Module Mod1
    Sub M1(Of T)(arg1 As T, arg2 As T)
        System.Console.WriteLine(If(arg1, arg2))
    End Sub
End Module
    </file>
</compilation>, parseOptions:=TestOptions.Regular15_5).AssertTheseDiagnostics(<![CDATA[
BC36716: Visual Basic 15.5 does not support unconstrained type parameters in binary conditional expressions.
        System.Console.WriteLine(If(arg1, arg2))
                                 ~~~~~~~~~~~~~~
]]>)
        End Sub
    End Class
End Namespace
