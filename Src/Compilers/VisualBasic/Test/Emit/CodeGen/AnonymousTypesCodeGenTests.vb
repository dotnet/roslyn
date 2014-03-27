' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class AnonymousTypesCodeGenTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub TestSimpleAnonymousType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim a As Integer = (New With {.a = 1, .b="text"}).a
        Dim b As String = (New With {.a = 1, .b="text"}).B
        Console.WriteLine(String.Format(".a={0}; .b={1}", a, b))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
.a=1; .b=text
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1).get_a()",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Private $a As T0"
  IL_0006:  ret
}
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1).set_a(T0)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      "Private $a As T0"
  IL_0007:  ret
}
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1).get_b()",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Private $b As T1"
  IL_0006:  ret
}
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1).set_b(T1)",
            <![CDATA[
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      "Private $b As T1"
  IL_0007:  ret
}
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1)..ctor(T0, T1)",
            <![CDATA[
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      "Private $a As T0"
  IL_000d:  ldarg.0
  IL_000e:  ldarg.2
  IL_000f:  stfld      "Private $b As T1"
  IL_0014:  ret
}
]]>)
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_If1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        If False Then
            Dim dummy As Object = New With {.a = 1}
        End If
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_If2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        If False Then
            Dim dummy = New With {.a = 1}
        End If
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_If3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        If False Then
            Dim dummy = New With {.a = New With {.b = 1}}
        End If
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy As Object = If(False, New With {.a = 1}, Nothing)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy = If(False, New With {.a = 1}, Nothing)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy = If(False, New With {.a = New With {.b = 1}}, Nothing)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional4()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy = If("abc", New With {.a = New With {.b = 1}})
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional5()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy = If("abc", Sub()
                                  Dim a = New With {.a = New With {.b = 1}}
                              End Sub)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional_CollInitializer1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy As Object = If(False, { New With {.a = 1} }, Nothing)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <WorkItem(544243)>
        <Fact()>
        Public Sub TestAnonymousTypeInUnreachableCode_Conditional_CollInitializer2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Test
    Sub Main(args() As String)
        Dim dummy = If(False, { New With {.a = 1} }, Nothing)
        Console.WriteLine(If(GetType(Test).Assembly.GetType("VB$AnonymousType_0`1"), "Type Not Found"))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:="VB$AnonymousType_0`1[T0]")
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_ToString()
            ' test AnonymousType_ToString() itself
            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US")
            Try

                CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine((New With {Key .a = 1, .b="text", Key .c=123.456}).ToString())
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
{ a = 1, b = text, c = 123.456 }
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1, T2).ToString()",
                <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  5
  IL_0000:  ldstr      "{{ a = {0}, b = {1}, c = {2} }}"
  IL_0005:  ldc.i4.3
  IL_0006:  newarr     "Object"
  IL_000b:  dup
  IL_000c:  ldc.i4.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Private ReadOnly $a As T0"
  IL_0013:  box        "T0"
  IL_0018:  stelem.ref
  IL_0019:  dup
  IL_001a:  ldc.i4.1
  IL_001b:  ldarg.0
  IL_001c:  ldfld      "Private $b As T1"
  IL_0021:  box        "T1"
  IL_0026:  stelem.ref
  IL_0027:  dup
  IL_0028:  ldc.i4.2
  IL_0029:  ldarg.0
  IL_002a:  ldfld      "Private ReadOnly $c As T2"
  IL_002f:  box        "T2"
  IL_0034:  stelem.ref
  IL_0035:  call       "Function String.Format(String, ParamArray Object()) As String"
  IL_003a:  ret
}
]]>)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_IEquatable_Equals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure S
        Public X As Integer
        Public Y As Integer
        Public Sub New(_x As Integer, _y As Integer)
            Me.X = _x
            Me.Y = _y
        End Sub
        Public Sub New(_x As Integer)
            Me.X = 0
            Me.Y = 0
        End Sub
    End Structure

    Sub Main(args As String())
        Console.WriteLine((New With {Key .a = 1, .b="text", Key .c=New S(1,2)}).Equals(New With {Key .a = 1, .b="text", Key .c=New S(1,2)}))
        Console.WriteLine((New With {Key .a = 1, .b="text", Key .c=New S(1,2)}).Equals(New With {Key .a = 1, .b="DIFFERENT Text", Key .c=New S(1,2)}))
        Console.WriteLine((New With {Key .a = 1, .b="text", Key .c=New S(1,2)}).Equals(New With {Key .a = 1, .b="text", Key .c=New S(2,1)}))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
True
True
False
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1, T2).Equals(VB$AnonymousType_0(Of T0, T1, T2))",
            <![CDATA[
{
  // Code size      105 (0x69)
  .maxstack  2
  .locals init (Object V_0,
  Object V_1)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0067
  IL_0003:  ldarg.0
  IL_0004:  ldfld      "Private ReadOnly $a As T0"
  IL_0009:  box        "T0"
  IL_000e:  stloc.0
  IL_000f:  ldarg.1
  IL_0010:  ldfld      "Private ReadOnly $a As T0"
  IL_0015:  box        "T0"
  IL_001a:  stloc.1
  IL_001b:  ldloc.0
  IL_001c:  brfalse.s  IL_0021
  IL_001e:  ldloc.1
  IL_001f:  brtrue.s   IL_0027
  IL_0021:  ldloc.0
  IL_0022:  ldloc.1
  IL_0023:  ceq
  IL_0025:  br.s       IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldloc.1
  IL_0029:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002e:  callvirt   "Function Object.Equals(Object) As Boolean"
  IL_0033:  brfalse.s  IL_0065
  IL_0035:  ldarg.0
  IL_0036:  ldfld      "Private ReadOnly $c As T2"
  IL_003b:  box        "T2"
  IL_0040:  stloc.0
  IL_0041:  ldarg.1
  IL_0042:  ldfld      "Private ReadOnly $c As T2"
  IL_0047:  box        "T2"
  IL_004c:  stloc.1
  IL_004d:  ldloc.0
  IL_004e:  brfalse.s  IL_0053
  IL_0050:  ldloc.1
  IL_0051:  brtrue.s   IL_0058
  IL_0053:  ldloc.0
  IL_0054:  ldloc.1
  IL_0055:  ceq
  IL_0057:  ret
  IL_0058:  ldloc.0
  IL_0059:  ldloc.1
  IL_005a:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_005f:  callvirt   "Function Object.Equals(Object) As Boolean"
  IL_0064:  ret
  IL_0065:  ldc.i4.0
  IL_0066:  ret
  IL_0067:  ldc.i4.0
  IL_0068:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_Equals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Structure S
        Public X As Integer
        Public Y As Integer
        Public Sub New(_x As Integer, _y As Integer)
            Me.X = _x
            Me.Y = _y
        End Sub
        Public Sub New(_x As Integer)
            Me.X = 0
            Me.Y = 0
        End Sub
    End Structure

    Sub Main(args As String())
        Dim o1 As Object = New With {Key .a = 1, .b="text", Key .c=New S(1,2)}
        Dim o2 As Object = New With {Key .a = 1, .b="text", Key .c=New S(1,2)}
        Dim o3 As Object = New With {Key .a = 1, .b="DIFFERENT Text", Key .c=New S(1,2)}
        Dim o4 As Object = New With {Key .a = 1, .b="text", Key .c=New S(2,1)}

        Console.WriteLine(o1.Equals(o1))
        Console.WriteLine(o1.Equals(o2))
        Console.WriteLine(o1.Equals(o3))
        Console.WriteLine(o1.Equals(o4))

        Console.WriteLine(o2.Equals(o1))
        Console.WriteLine(o2.Equals(o2))
        Console.WriteLine(o2.Equals(o3))
        Console.WriteLine(o2.Equals(o4))

        Console.WriteLine(o3.Equals(o1))
        Console.WriteLine(o3.Equals(o2))
        Console.WriteLine(o3.Equals(o3))
        Console.WriteLine(o3.Equals(o4))

        Console.WriteLine(o4.Equals(o1))
        Console.WriteLine(o4.Equals(o2))
        Console.WriteLine(o4.Equals(o3))
        Console.WriteLine(o4.Equals(o4))
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
True
True
True
False
True
True
True
False
True
True
True
False
False
False
False
True
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1, T2).Equals(Object)",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     "VB$AnonymousType_0(Of T0, T1, T2)"
  IL_0007:  call       "Public Overloads Function Equals(val As VB$AnonymousType_0(Of T0, T1, T2)) As Boolean"
  IL_000c:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_GetHashCode()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program
    Sub Main(args As String())
        Dim obj As Object = New With {Key .a = 1, .b="text", Key .C = 1}
    End Sub
End Module
    </file>
</compilation>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1, T2).GetHashCode()",
New XCData(<![CDATA[
{
  // Code size       92 (0x5c)
  .maxstack  2
  .locals init (T0 V_0,
  T2 V_1)
  IL_0000:  ldc.i4     0x526a854f
  IL_0005:  ldc.i4     0xa5555529
  IL_000a:  mul
  IL_000b:  ldarg.0
  IL_000c:  ldfld      "Private ReadOnly $a As T0"
  IL_0011:  box        "T0"
  IL_0016:  brfalse.s  IL_002e
  IL_0018:  ldarg.0
  IL_0019:  ldfld      "Private ReadOnly $a As T0"
  IL_001e:  stloc.0
  IL_001f:  ldloca.s   V_0
  IL_0021:  constrained. "T0"
  IL_0027:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_002c:  br.s       IL_002f
  IL_002e:  ldc.i4.0
  IL_002f:  add
  IL_0030:  ldc.i4     0xa5555529
  IL_0035:  mul
  IL_0036:  ldarg.0
  IL_0037:  ldfld      "Private ReadOnly $C As T2"
  IL_003c:  box        "T2"
  IL_0041:  brfalse.s  IL_0059
  IL_0043:  ldarg.0
  IL_0044:  ldfld      "Private ReadOnly $C As T2"
  IL_0049:  stloc.1
  IL_004a:  ldloca.s   V_1
  IL_004c:  constrained. "T2"
  IL_0052:  callvirt   "Function Object.GetHashCode() As Integer"
  IL_0057:  br.s       IL_005a
  IL_0059:  ldc.i4.0
  IL_005a:  add
  IL_005b:  ret
}
]]>.Value))

        End Sub

        <WorkItem(531571)>
        <Fact()>
        Public Sub Bug_531571()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Friend Module Program
    Sub Main()
        Console.WriteLine((New With {Key .prop1 = 1, Key .prop2 = 5.5}).GetHashCode())
    End Sub
End Module
    </file>
</compilation>, expectedOutput:="-1644666626")
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_GetHashCode02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program

    Sub Main()
        Dim at1 As Object = New With {.f1 = 123, Key .f2 = 456, Key .f3 = "XXX", .f4 = 123.456!}
        ' Changes in non-key fields
        Dim at2 As Object = New With {.f1 = "YYY", Key .f2 = 456, Key .f3 = "XXX", .f4 = Nothing }
        ' Changes in Key fields
        Dim at3 As Object = New With {.f1 = 123, Key .f2 = 455, Key .f3 = "XXX", .f4 = 123.456!}

        Dim hc1 = at1.GetHashCode()      
        Console.WriteLine(hc1 = at2.GetHashCode )
        Console.WriteLine(hc1 = at3.GetHashCode)
    
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
True
False
]]>)
        End Sub

        <Fact()>
        Public Sub TestAnonymousType_SequenceOfInitializers()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program

    Property Index As Integer
    
    Function F() As Integer
        Index = Index + 1
        Console.WriteLine(Index)
        Return Index
    End Function

    Sub Main()
        Console.WriteLine(New With {F, Key .a = F, Key .b = .a, .c = F, .d = .b})
    End Sub
End Module
    </file>
</compilation>,
expectedOutput:=<![CDATA[
1
2
3
{ F = 1, a = 2, b = 2, c = 3, d = 2 }
]]>)

        End Sub

        <Fact()>
        Public Sub TestAnonymousType_LocalAsNewWith()
            ' AnonymousType ToString
            Dim currCulture = System.Threading.Thread.CurrentThread.CurrentCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("en-US")
            Try

                CompileAndVerify(
        <compilation>
            <file name="a.vb">
Imports System
Module Program
    Sub Main()
        Dim a As New With { Key .a = 1, .b = .a * 0.1 }
        Console.WriteLine(a.ToString())
    End Sub
End Module
    </file>
        </compilation>,
        expectedOutput:=<![CDATA[{ a = 1, b = 0.1 }]]>).
                VerifyIL("Program.Main()",
                <![CDATA[
{
  // Code size       31 (0x1f)
  .maxstack  3
  .locals init (Integer V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  conv.r8
  IL_0005:  ldc.r8     0.1
  IL_000e:  mul
  IL_000f:  newobj     "Sub VB$AnonymousType_0(Of Integer, Double)..ctor(Integer, Double)"
  IL_0014:  callvirt   "Function VB$AnonymousType_0(Of Integer, Double).ToString() As String"
  IL_0019:  call       "Sub System.Console.WriteLine(String)"
  IL_001e:  ret
}
]]>)

            Catch ex As Exception
                Assert.Null(ex)
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currCulture
            End Try

        End Sub

        <Fact()>
        Public Sub TestAnonymousTypeWithOptionInferOn()
            CompileAndVerify(
<compilation>
    <file name="at.vb"><![CDATA[
Option Infer On
Imports System

Friend Module AnonTProp001mod
    Sub Main()
        Dim obj = New C
        Try

            Dim scen1 = New With {.With = "aclass", ._p_ = "C"c, Foo, Key New C().extMethod}
            Console.WriteLine("{0},{1},{2},{3}", scen1.With, scen1._p_, scen1.foo, scen1.Extmethod)

            Dim scen2 = New With {obj.Extmethod02, obj!_123, C.APROP}
            Console.WriteLine("{0},{1},{2}", scen2.ExtMethod02, scen2._123, scen2.aprop)

            Try
                Dim scen4 = New With {.prop1 = FooEx("testing")}
                Console.WriteLine("NO EX")
            Catch ex As Exception
                Console.WriteLine("Exp EX")
            End Try
        Catch
        Finally
        End Try
    End Sub

    Function Foo() As String
        Return "Abc"
    End Function

    Function FooEx(ByVal p1 As String) As String
        Throw New Exception("This exception is expected")
    End Function

    <System.Runtime.CompilerServices.Extension()> _
    Friend Function ExtMethod(ByVal p1 As C) As String
        Return "Extended"
    End Function

    <System.Runtime.CompilerServices.Extension()> _
    Function ExtMethod02(p1 As C) As Byte
        Return 127
    End Function

    Class C
        Public ReadOnly Default Property Idx(p As String) As String
            Get
                Return p & p
            End Get
        End Property

        Friend Shared Property aprop() As String
            Get
                Return "wello horld"
            End Get
            Set(ByVal value As String)

            End Set
        End Property
    End Class
End Module
    ]]></file>
</compilation>,
expectedOutput:=<![CDATA[
aclass,C,Abc,Extended
127,_123_123,wello horld
Exp EX
]]>)

        End Sub

    End Class

End Namespace
