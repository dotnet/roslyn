' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
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
  IL_0001:  ldfld      "VB$AnonymousType_0(Of T0, T1).$a As T0"
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
  IL_0002:  stfld      "VB$AnonymousType_0(Of T0, T1).$a As T0"
  IL_0007:  ret
}
]]>).
            VerifyIL("VB$AnonymousType_0(Of T0, T1).get_b()",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "VB$AnonymousType_0(Of T0, T1).$b As T1"
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
  IL_0002:  stfld      "VB$AnonymousType_0(Of T0, T1).$b As T1"
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
  IL_0008:  stfld      "VB$AnonymousType_0(Of T0, T1).$a As T0"
  IL_000d:  ldarg.0
  IL_000e:  ldarg.2
  IL_000f:  stfld      "VB$AnonymousType_0(Of T0, T1).$b As T1"
  IL_0014:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub AnonymousTypeSymbol_Simple_Threadsafety()

            Dim source = <compilation>
                             <file name="a.vb">
Module Module1

    Sub Main()
        Dim at1 = New With {.a = 1, .b = 2}
        Dim at2 = New With {.a = 1, .b = 2, .c = 3}
        Dim at3 = New With {.a = 1, .b = 2, .c = 3, .d = 4}
        Dim at4 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5}
        Dim at5 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6}
        Dim at6 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7}
        Dim at7 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8}
        Dim at8 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9}
        Dim at9 = New With {.a = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9, .k = 10}

        Dim at11 = New With {.aa = 1, .b = 2}
        Dim at12 = New With {.aa = 1, .b = 2, .c = 3}
        Dim at13 = New With {.aa = 1, .b = 2, .c = 3, .d = 4}
        Dim at14 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5}
        Dim at15 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6}
        Dim at16 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7}
        Dim at17 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8}
        Dim at18 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9}
        Dim at19 = New With {.aa = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9, .k = 10}

        Dim at21 = New With {.ba = 1, .b = 2}
        Dim at22 = New With {.ba = 1, .b = 2, .c = 3}
        Dim at23 = New With {.ba = 1, .b = 2, .c = 3, .d = 4}
        Dim at24 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5}
        Dim at25 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6}
        Dim at26 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7}
        Dim at27 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8}
        Dim at28 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9}
        Dim at29 = New With {.ba = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9, .k = 10}

        Dim at31 = New With {.ca = 1, .b = 2}
        Dim at32 = New With {.ca = 1, .b = 2, .c = 3}
        Dim at33 = New With {.ca = 1, .b = 2, .c = 3, .d = 4}
        Dim at34 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5}
        Dim at35 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6}
        Dim at36 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7}
        Dim at37 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8}
        Dim at38 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9}
        Dim at39 = New With {.ca = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9, .k = 10}

        Dim at41 = New With {.da = 1, .b = 2}
        Dim at42 = New With {.da = 1, .b = 2, .c = 3}
        Dim at43 = New With {.da = 1, .b = 2, .c = 3, .d = 4}
        Dim at44 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5}
        Dim at45 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6}
        Dim at46 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7}
        Dim at47 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8}
        Dim at48 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9}
        Dim at49 = New With {.da = 1, .b = 2, .c = 3, .d = 4, .e = 5, .f = 6, .g = 7, .h = 8, .j = 9, .k = 10}

    End Sub

End Module
    </file>
                         </compilation>

            For i = 0 To 100
                Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source)

                Dim tasks(10) As Task
                For jj = 0 To tasks.Length - 1
                    Dim j = jj
                    tasks(j) = Task.Run(Sub()
                                            Dim stream = New MemoryStream()
                                            Dim result = compilation.Emit(stream, options:=New EmitOptions(metadataOnly:=j Mod 2 = 0))
                                            result.Diagnostics.Verify()
                                        End Sub)
                Next

                ' this should Not fail. if you ever see a NRE Or some kind of crash here enter a bug.
                ' it may be reproducing just once in a while, in Release only... 
                ' it Is still a bug.
                Task.WaitAll(tasks)
            Next
        End Sub

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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

        <WorkItem(544243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544243")>
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
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
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
  // Code size       60 (0x3c)
  .maxstack  6
  IL_0000:  ldnull
  IL_0001:  ldstr      "{{ a = {0}, b = {1}, c = {2} }}"
  IL_0006:  ldc.i4.3
  IL_0007:  newarr     "Object"
  IL_000c:  dup
  IL_000d:  ldc.i4.0
  IL_000e:  ldarg.0
  IL_000f:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$a As T0"
  IL_0014:  box        "T0"
  IL_0019:  stelem.ref
  IL_001a:  dup
  IL_001b:  ldc.i4.1
  IL_001c:  ldarg.0
  IL_001d:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$b As T1"
  IL_0022:  box        "T1"
  IL_0027:  stelem.ref
  IL_0028:  dup
  IL_0029:  ldc.i4.2
  IL_002a:  ldarg.0
  IL_002b:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$c As T2"
  IL_0030:  box        "T2"
  IL_0035:  stelem.ref
  IL_0036:  call       "Function String.Format(System.IFormatProvider, String, ParamArray Object()) As String"
  IL_003b:  ret
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
  // Code size      111 (0x6f)
  .maxstack  2
  .locals init (Object V_0,
                Object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  beq.s      IL_006d
  IL_0004:  ldarg.1
  IL_0005:  brfalse.s  IL_006b
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$a As T0"
  IL_000d:  box        "T0"
  IL_0012:  stloc.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$a As T0"
  IL_0019:  box        "T0"
  IL_001e:  stloc.1
  IL_001f:  ldloc.0
  IL_0020:  brfalse.s  IL_0025
  IL_0022:  ldloc.1
  IL_0023:  brtrue.s   IL_002b
  IL_0025:  ldloc.0
  IL_0026:  ldloc.1
  IL_0027:  ceq
  IL_0029:  br.s       IL_0037
  IL_002b:  ldloc.0
  IL_002c:  ldloc.1
  IL_002d:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0032:  callvirt   "Function Object.Equals(Object) As Boolean"
  IL_0037:  brfalse.s  IL_0069
  IL_0039:  ldarg.0
  IL_003a:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$c As T2"
  IL_003f:  box        "T2"
  IL_0044:  stloc.0
  IL_0045:  ldarg.1
  IL_0046:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$c As T2"
  IL_004b:  box        "T2"
  IL_0050:  stloc.1
  IL_0051:  ldloc.0
  IL_0052:  brfalse.s  IL_0057
  IL_0054:  ldloc.1
  IL_0055:  brtrue.s   IL_005c
  IL_0057:  ldloc.0
  IL_0058:  ldloc.1
  IL_0059:  ceq
  IL_005b:  ret
  IL_005c:  ldloc.0
  IL_005d:  ldloc.1
  IL_005e:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0063:  callvirt   "Function Object.Equals(Object) As Boolean"
  IL_0068:  ret
  IL_0069:  ldc.i4.0
  IL_006a:  ret
  IL_006b:  ldc.i4.0
  IL_006c:  ret
  IL_006d:  ldc.i4.1
  IL_006e:  ret
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
  IL_0007:  call       "Function VB$AnonymousType_0(Of T0, T1, T2).Equals(VB$AnonymousType_0(Of T0, T1, T2)) As Boolean"
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
  IL_000c:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$a As T0"
  IL_0011:  box        "T0"
  IL_0016:  brfalse.s  IL_002e
  IL_0018:  ldarg.0
  IL_0019:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$a As T0"
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
  IL_0037:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$C As T2"
  IL_003c:  box        "T2"
  IL_0041:  brfalse.s  IL_0059
  IL_0043:  ldarg.0
  IL_0044:  ldfld      "VB$AnonymousType_0(Of T0, T1, T2).$C As T2"
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

        <WorkItem(531571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531571")>
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
        Public Sub TestAnonymousType_GetHashCode03()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Module Program

    Sub Main()
        Dim at1 As Object = New With {.Ǉ1 = 123, Key .Ǉ2 = 456, Key .Ǉ3 = "XXX", .Ǉ4 = 123.456!}
        ' Value changes in non-key fields, casing changes in all fields that require a recent unicode version
        Dim at2 As Object = New With {.ǈ1 = "YYY", Key .ǈ2 = 456, Key .ǈ3 = "XXX", .ǈ4 = Nothing }
        ' Value changes in Key fields
        Dim at3 As Object = New With {.Ǉ1 = 123, Key .Ǉ2 = 455, Key .Ǉ3 = "XXX", .Ǉ4 = 123.456!}

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
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture
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

        <ConditionalFact(GetType(DesktopOnly))>
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

            Dim scen1 = New With {.With = "aclass", ._p_ = "C"c, Goo, Key New C().extMethod}
            Console.WriteLine("{0},{1},{2},{3}", scen1.With, scen1._p_, scen1.goo, scen1.Extmethod)

            Dim scen2 = New With {obj.Extmethod02, obj!_123, C.APROP}
            Console.WriteLine("{0},{1},{2}", scen2.ExtMethod02, scen2._123, scen2.aprop)

            Try
                Dim scen4 = New With {.prop1 = GooEx("testing")}
                Console.WriteLine("NO EX")
            Catch ex As Exception
                Console.WriteLine("Exp EX")
            End Try
        Catch
        Finally
        End Try
    End Sub

    Function Goo() As String
        Return "Abc"
    End Function

    Function GooEx(ByVal p1 As String) As String
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
