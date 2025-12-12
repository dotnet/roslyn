' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenWithEvents
        Inherits BasicTestBase

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/79350")>
        Public Sub SimpleWithEventsTest()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Dim WithEvents X As Object = "AA", y As New Object

    Sub Main(args As String())
        Console.WriteLine(X)

        X() = 42
        Console.WriteLine(X())

        _Y = 123
        Console.WriteLine(_Y)
    End Sub
End Module
    </file>
</compilation>, expectedOutput:=<![CDATA[
AA
42
123
]]>).VerifyIL("Program.Main", <![CDATA[
{
  // Code size       70 (0x46)
  .maxstack  1
  IL_0000:  call       "Function Program.get_X() As Object"
  IL_0005:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_000a:  call       "Sub System.Console.WriteLine(Object)"
  IL_000f:  ldc.i4.s   42
  IL_0011:  box        "Integer"
  IL_0016:  call       "Sub Program.set_X(Object)"
  IL_001b:  call       "Function Program.get_X() As Object"
  IL_0020:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0025:  call       "Sub System.Console.WriteLine(Object)"
  IL_002a:  ldc.i4.s   123
  IL_002c:  box        "Integer"
  IL_0031:  stsfld     "Program._y As Object"
  IL_0036:  ldsfld     "Program._y As Object"
  IL_003b:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0040:  call       "Sub System.Console.WriteLine(Object)"
  IL_0045:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub StaticHandlesMe()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class Program
    Shared Event E1()

    Shared Sub Main(args As String())
        RaiseEvent E1()
    End Sub

    Shared Sub goo() Handles MyClass.E1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
]]>).VerifyIL("Program..cctor", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub Program.goo()"
  IL_0007:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0011:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub StaticHandlesMeInBase()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

class Base
    Shared Event E1()

    shared sub raiser()
        RaiseEvent E1()
    end sub
end class

Class Program
    Inherits Base

    Shared Sub Main(args As String())
        raiser
    End Sub

    Shared Sub goo() Handles MyClass.E1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
]]>).VerifyIL("Program..cctor", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub Program.goo()"
  IL_0007:  newobj     "Sub Base.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  call       "Sub Base.add_E1(Base.E1EventHandler)"
  IL_0011:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub InstanceHandlesMe()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
imports system

Class Program
    Event E1()

    Shared Sub Main(args As String())       
        Call (New Program).Raiser()
    End Sub

    Sub Raiser()
        RaiseEvent E1()
    End Sub

    Shared Sub goo() Handles MyClass.e1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
]]>).VerifyIL("Program..ctor", <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldnull
  IL_0008:  ldftn      "Sub Program.goo()"
  IL_000e:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0013:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0018:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub InstanceHandlesMeNeedCtor()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
imports system

Class Program
    Event E1(x As Integer)

    Sub New()
        Me.New(42)
    End Sub

    Sub New(x As Integer)

    End Sub

    Sub New(x As Long)

    End Sub

    Shared Sub Main(args As String())
        Call (New Program()).Raiser()
    End Sub

    Sub Raiser()
        RaiseEvent E1(123)
    End Sub

    Shared Sub goo() Handles MyClass.e1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
]]>).VerifyIL("Program..ctor", <![CDATA[
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  call       "Sub Program..ctor(Integer)"
  IL_0008:  ret
}
]]>).VerifyIL("Program..ctor(Integer)", <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldsfld     "Program._Closure$__.$IR6-1 As Program.E1EventHandler"
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldsfld     "Program._Closure$__.$IR6-1 As Program.E1EventHandler"
  IL_0013:  br.s       IL_002b
  IL_0015:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_001a:  ldftn      "Sub Program._Closure$__._Lambda$__R6-1(Integer)"
  IL_0020:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0025:  dup
  IL_0026:  stsfld     "Program._Closure$__.$IR6-1 As Program.E1EventHandler"
  IL_002b:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0030:  ret
}
]]>).VerifyIL("Program..ctor(Long)", <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldsfld     "Program._Closure$__.$IR7-2 As Program.E1EventHandler"
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldsfld     "Program._Closure$__.$IR7-2 As Program.E1EventHandler"
  IL_0013:  br.s       IL_002b
  IL_0015:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_001a:  ldftn      "Sub Program._Closure$__._Lambda$__R7-2(Integer)"
  IL_0020:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0025:  dup
  IL_0026:  stsfld     "Program._Closure$__.$IR7-2 As Program.E1EventHandler"
  IL_002b:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0030:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub InstanceHandlesMeMismatched()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option strict off        
Imports System

Class base
    Event E2(ByRef x As Integer)

    Sub Raiser()
        RaiseEvent E2(123)
    End Sub
End Class

Class Program
    Inherits base

    Shared Event E1(x As Integer)

    Shared Sub Main(args As String())
        Call (New Program).Raiser()
    End Sub

    Shadows Sub Raiser()
        RaiseEvent E1(123)
        MyBase.Raiser()
    End Sub

    Sub goo1() Handles MyClass.E1
        Console.WriteLine("handled1")
    End Sub

    Shared Sub goo2() Handles MyClass.E2
        Console.WriteLine("handled2")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled1
handled2
]]>).VerifyIL("Program..ctor", <![CDATA[
{
  // Code size       66 (0x42)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Sub Program._Lambda$__R0-1(Integer)"
  IL_000d:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0012:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0017:  ldarg.0
  IL_0018:  ldsfld     "Program._Closure$__.$IR0-2 As base.E2EventHandler"
  IL_001d:  brfalse.s  IL_0026
  IL_001f:  ldsfld     "Program._Closure$__.$IR0-2 As base.E2EventHandler"
  IL_0024:  br.s       IL_003c
  IL_0026:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_002b:  ldftn      "Sub Program._Closure$__._Lambda$__R0-2(ByRef Integer)"
  IL_0031:  newobj     "Sub base.E2EventHandler..ctor(Object, System.IntPtr)"
  IL_0036:  dup
  IL_0037:  stsfld     "Program._Closure$__.$IR0-2 As base.E2EventHandler"
  IL_003c:  call       "Sub base.add_E2(base.E2EventHandler)"
  IL_0041:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub InstanceHandlesMeMismatchedStrict()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Option Strict On
Imports System

Class base
    Event E2(ByRef x As Integer)

    Sub Raiser()
        RaiseEvent E2(123)
    End Sub
End Class

Class Program
    Inherits base

    Shared Event E1(x As Integer)

    Shared Sub Main(args As String())
        Call (New Program).Raiser()
    End Sub

    Shadows Sub Raiser()
        RaiseEvent E1(123)
        MyBase.Raiser()
    End Sub

    Sub goo1() Handles MyClass.E1
        Console.WriteLine("handled1")
    End Sub

    Shared Function goo2(arg As Long) As Integer Handles MyClass.E1
        Console.WriteLine("handled2")
        Return 123
    End Function
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled2
handled1
]]>).VerifyIL("Program..ctor", <![CDATA[
{
  // Code size       24 (0x18)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Sub Program._Lambda$__R1-2(Integer)"
  IL_000d:  newobj     "Sub Program.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0012:  call       "Sub Program.add_E1(Program.E1EventHandler)"
  IL_0017:  ret
}
]]>).Compilation

        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/79350")>
        Public Sub SimpleSharedWithEvents()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class cls1
    Event E1()

    Sub raiser()
        RaiseEvent E1()
    End Sub
End Class

Class cls2
    Shared WithEvents w As cls1

    Public Shared Sub Main()
        Dim o As New cls1
        o.raiser()

        w = o
        o.raiser()

        w = o
        o.raiser()

        w = Nothing
        o.raiser()
    End Sub

    Private Shared Sub handler() Handles w.E1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
handled
]]>).VerifyIL("cls2.set_w", <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (cls1.E1EventHandler V_0,
  cls1 V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub cls2.handler()"
  IL_0007:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldsfld     "cls2._w As cls1"
  IL_0012:  stloc.1
  IL_0013:  ldloc.1
  IL_0014:  brfalse.s  IL_001d
  IL_0016:  ldloc.1
  IL_0017:  ldloc.0
  IL_0018:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_001d:  ldarg.0
  IL_001e:  stsfld     "cls2._w As cls1"
  IL_0023:  ldsfld     "cls2._w As cls1"
  IL_0028:  stloc.1
  IL_0029:  ldloc.1
  IL_002a:  brfalse.s  IL_0033
  IL_002c:  ldloc.1
  IL_002d:  ldloc.0
  IL_002e:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_0033:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub SimpleInstanceWithEvents()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
imports system        
Class cls1
    Event E1()

    Sub raiser()
        RaiseEvent E1()
    End Sub
End Class

Class cls2
    WithEvents w As cls1

    Public Sub Test()
        Dim o As New cls1
        o.raiser()

        w = o
        o.raiser()

        w = o
        o.raiser()

        w = Nothing
        o.raiser()
    End Sub

    Public Shared Sub main()
        Call (New cls2).Test()
    End Sub

    Private Sub handler() Handles w.E1
        Console.WriteLine("handled")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
handled
]]>).VerifyIL("cls2.set_w", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (cls1.E1EventHandler V_0,
  cls1 V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub cls2.handler()"
  IL_0007:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "cls2._w As cls1"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001e
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  stfld      "cls2._w As cls1"
  IL_0025:  ldarg.0
  IL_0026:  ldfld      "cls2._w As cls1"
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  brfalse.s  IL_0036
  IL_002f:  ldloc.1
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_0036:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub BaseInstanceWithEventsMultiple()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class cls1
    Event E1(x As Integer)

    Sub raiser()
        RaiseEvent E1(123)
    End Sub
End Class

Class base
    Protected WithEvents w As cls1
End Class

Class cls2
    Inherits base

    Public Sub Test()
        Dim o As New cls1
        o.raiser()

        w = o
        o.raiser()

        w = o
        o.raiser()

        w = Nothing
        o.raiser()
    End Sub

    Public Shared Sub main()
        Call (New cls2).Test()
    End Sub

    Private Sub handler() Handles w.E1, w.E1
        Console.WriteLine("handled")
    End Sub

    Private shared Sub handlerS() Handles w.E1, w.E1
        Console.WriteLine("handledS")
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
handled
handledS
handledS
handled
handled
handledS
handledS
]]>).VerifyIL("cls2.set_w", <![CDATA[
{
  // Code size      196 (0xc4)
  .maxstack  2
  .locals init (cls1.E1EventHandler V_0,
                cls1.E1EventHandler V_1,
                cls1.E1EventHandler V_2,
                cls1.E1EventHandler V_3,
                cls1 V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub cls2._Lambda$__R0-1(Integer)"
  IL_0007:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldftn      "Sub cls2._Lambda$__R0-2(Integer)"
  IL_0014:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  IL_001a:  ldsfld     "cls2._Closure$__.$IR0-3 As cls1.E1EventHandler"
  IL_001f:  brfalse.s  IL_0028
  IL_0021:  ldsfld     "cls2._Closure$__.$IR0-3 As cls1.E1EventHandler"
  IL_0026:  br.s       IL_003e
  IL_0028:  ldsfld     "cls2._Closure$__.$I As cls2._Closure$__"
  IL_002d:  ldftn      "Sub cls2._Closure$__._Lambda$__R0-3(Integer)"
  IL_0033:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0038:  dup
  IL_0039:  stsfld     "cls2._Closure$__.$IR0-3 As cls1.E1EventHandler"
  IL_003e:  stloc.2
  IL_003f:  ldsfld     "cls2._Closure$__.$IR0-4 As cls1.E1EventHandler"
  IL_0044:  brfalse.s  IL_004d
  IL_0046:  ldsfld     "cls2._Closure$__.$IR0-4 As cls1.E1EventHandler"
  IL_004b:  br.s       IL_0063
  IL_004d:  ldsfld     "cls2._Closure$__.$I As cls2._Closure$__"
  IL_0052:  ldftn      "Sub cls2._Closure$__._Lambda$__R0-4(Integer)"
  IL_0058:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_005d:  dup
  IL_005e:  stsfld     "cls2._Closure$__.$IR0-4 As cls1.E1EventHandler"
  IL_0063:  stloc.3
  IL_0064:  ldarg.0
  IL_0065:  call       "Function base.get_w() As cls1"
  IL_006a:  stloc.s    V_4
  IL_006c:  ldloc.s    V_4
  IL_006e:  brfalse.s  IL_0090
  IL_0070:  ldloc.s    V_4
  IL_0072:  ldloc.0
  IL_0073:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0078:  ldloc.s    V_4
  IL_007a:  ldloc.1
  IL_007b:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0080:  ldloc.s    V_4
  IL_0082:  ldloc.2
  IL_0083:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0088:  ldloc.s    V_4
  IL_008a:  ldloc.3
  IL_008b:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0090:  ldarg.0
  IL_0091:  ldarg.1
  IL_0092:  call       "Sub base.set_w(cls1)"
  IL_0097:  ldarg.0
  IL_0098:  call       "Function base.get_w() As cls1"
  IL_009d:  stloc.s    V_4
  IL_009f:  ldloc.s    V_4
  IL_00a1:  brfalse.s  IL_00c3
  IL_00a3:  ldloc.s    V_4
  IL_00a5:  ldloc.0
  IL_00a6:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_00ab:  ldloc.s    V_4
  IL_00ad:  ldloc.1
  IL_00ae:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_00b3:  ldloc.s    V_4
  IL_00b5:  ldloc.2
  IL_00b6:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_00bb:  ldloc.s    V_4
  IL_00bd:  ldloc.3
  IL_00be:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_00c3:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub PartialHandles()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class cls1
    Event E1(x As Integer)

    Sub raiser()
        RaiseEvent E1(123)
    End Sub
End Class

Class base
    Public WithEvents w As cls1
End Class

Class cls2
    Inherits base

    Public Sub Test()
        Dim o As New cls1
        o.raiser()

        w = o
        o.raiser()

        w = o
        o.raiser()

        w = Nothing
        o.raiser()
    End Sub

    Public Shared Sub main()
        Call (New cls2).Test()
    End Sub

    Private Sub handlerConcrete() Handles w.E1, w.E1
        Console.WriteLine("handled")
    End Sub

    Partial Private Sub handlerConcrete() Handles w.E1, w.E1
    End Sub
End Class

    </file>
</compilation>, expectedOutput:=<![CDATA[handled
handled
handled
handled
handled
handled
handled
handled
]]>).VerifyIL("cls2.set_w", <![CDATA[
{
  // Code size      148 (0x94)
  .maxstack  2
  .locals init (cls1.E1EventHandler V_0,
  cls1.E1EventHandler V_1,
  cls1.E1EventHandler V_2,
  cls1.E1EventHandler V_3,
  cls1 V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub cls2._Lambda$__R0-1(Integer)"
  IL_0007:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldftn      "Sub cls2._Lambda$__R0-2(Integer)"
  IL_0014:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  IL_001a:  ldarg.0
  IL_001b:  ldftn      "Sub cls2._Lambda$__R0-3(Integer)"
  IL_0021:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0026:  stloc.2
  IL_0027:  ldarg.0
  IL_0028:  ldftn      "Sub cls2._Lambda$__R0-4(Integer)"
  IL_002e:  newobj     "Sub cls1.E1EventHandler..ctor(Object, System.IntPtr)"
  IL_0033:  stloc.3
  IL_0034:  ldarg.0
  IL_0035:  call       "Function base.get_w() As cls1"
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloc.s    V_4
  IL_003e:  brfalse.s  IL_0060
  IL_0040:  ldloc.s    V_4
  IL_0042:  ldloc.0
  IL_0043:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0048:  ldloc.s    V_4
  IL_004a:  ldloc.1
  IL_004b:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0050:  ldloc.s    V_4
  IL_0052:  ldloc.2
  IL_0053:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0058:  ldloc.s    V_4
  IL_005a:  ldloc.3
  IL_005b:  callvirt   "Sub cls1.remove_E1(cls1.E1EventHandler)"
  IL_0060:  ldarg.0
  IL_0061:  ldarg.1
  IL_0062:  call       "Sub base.set_w(cls1)"
  IL_0067:  ldarg.0
  IL_0068:  call       "Function base.get_w() As cls1"
  IL_006d:  stloc.s    V_4
  IL_006f:  ldloc.s    V_4
  IL_0071:  brfalse.s  IL_0093
  IL_0073:  ldloc.s    V_4
  IL_0075:  ldloc.0
  IL_0076:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_007b:  ldloc.s    V_4
  IL_007d:  ldloc.1
  IL_007e:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_0083:  ldloc.s    V_4
  IL_0085:  ldloc.2
  IL_0086:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_008b:  ldloc.s    V_4
  IL_008d:  ldloc.3
  IL_008e:  callvirt   "Sub cls1.add_E1(cls1.E1EventHandler)"
  IL_0093:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub PartialHandles1()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class cls1
    Event E1(x As Integer)

    Sub raiser()
        RaiseEvent E1(123)
    End Sub
End Class

Class base
    Event E2(x As Integer)

    Public WithEvents w As cls1
End Class

Class cls2
    Inherits base

    Public Sub Test()
        Dim o As New cls1
        o.raiser()

        w = o
        o.raiser()

        w = o
        o.raiser()

        w = Nothing
        o.raiser()
    End Sub

    Public Shared Sub main()
        Call (New cls2).Test()
    End Sub

    Partial Private Sub handlerPartial(x as integer) Handles w.E1
    End Sub

    Partial Private Sub handlerPartial() Handles w.E1
    End Sub

    Partial Private Sub handlerPartial1(x as integer) Handles me.E2
    End Sub

    Partial Private Sub handlerPartial2() Handles me.E2
    End Sub
End Class

    </file>
</compilation>, expectedOutput:="")

        End Sub

        <Fact>
        Public Sub ProtectedWithEvents()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System

Class c1
    Protected Event e()
    Sub raiser()
        RaiseEvent e()
    End Sub
End Class
Class c2
    Inherits c1

End Class

Class c3
    Inherits c2

    Sub goo() Handles MyBase.e
        gstrexpectedresult = "working!!"
    End Sub
End Class

Module m1
    Public gstrexpectedresult As String

    Sub main()
        Dim o As New c3
        o.raiser()
        Console.WriteLine(gstrexpectedresult)
    End Sub
End Module

    </file>
</compilation>, expectedOutput:=<![CDATA[working!!
]]>).Compilation

        End Sub

        <Fact>
        Public Sub GenericWithEvents()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System

Module m1
    Public gresult As String = ""

    Sub main()
        Dim t As New C1(Of TClass)
        t.o = New TClass
        t.o.raiser()
        Console.WriteLine(gresult)
    End Sub
End Module

Class C1(Of T As {iTest, Class})
    Public WithEvents o As T
    Sub EventHandler2() Handles o.Event2
        gresult = gresult &amp; "inside EventHandler2"
    End Sub
    Sub EventHandler1()
        gresult = gresult &amp; "inside EH1"
    End Sub
    Sub adder()
        AddHandler o.Event2, AddressOf EventHandler1
    End Sub
End Class

Class TClass
    Implements iTest
    Public Event Event21() Implements iTest.Event2
    Sub raiser()
        RaiseEvent Event21()
    End Sub
End Class

Public Interface iTest
    Event Event2()
End Interface


    </file>
</compilation>, expectedOutput:=<![CDATA[inside EventHandler2
]]>).VerifyIL("C1(Of T).set_o(T)", <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (iTest.Event2EventHandler V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub C1(Of T).EventHandler2()"
  IL_0007:  newobj     "Sub iTest.Event2EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "C1(Of T)._o As T"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  box        "T"
  IL_001a:  brfalse.s  IL_002a
  IL_001c:  ldloca.s   V_1
  IL_001e:  ldloc.0
  IL_001f:  constrained. "T"
  IL_0025:  callvirt   "Sub iTest.remove_Event2(iTest.Event2EventHandler)"
  IL_002a:  ldarg.0
  IL_002b:  ldarg.1
  IL_002c:  stfld      "C1(Of T)._o As T"
  IL_0031:  ldarg.0
  IL_0032:  ldfld      "C1(Of T)._o As T"
  IL_0037:  stloc.1
  IL_0038:  ldloc.1
  IL_0039:  box        "T"
  IL_003e:  brfalse.s  IL_004e
  IL_0040:  ldloca.s   V_1
  IL_0042:  ldloc.0
  IL_0043:  constrained. "T"
  IL_0049:  callvirt   "Sub iTest.add_Event2(iTest.Event2EventHandler)"
  IL_004e:  ret
}
]]>).Compilation

        End Sub

        <Fact>
        Public Sub GenericWithEventsNoOpt()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">

Imports System

Module m1
    Public gresult As String = ""

    Sub main()
        Dim t As New C1(Of TClass)
        t.o = New TClass
        t.o.raiser()
        Console.WriteLine(gresult)
    End Sub
End Module

Class C1(Of T As {iTest, Class})
    Public WithEvents o As T
    Sub EventHandler2() Handles o.Event2
        gresult = gresult &amp; "inside EventHandler2"
    End Sub
    Sub EventHandler1()
        gresult = gresult &amp; "inside EH1"
    End Sub
    Sub adder()
        AddHandler o.Event2, AddressOf EventHandler1
    End Sub
End Class

Class TClass
    Implements iTest
    Public Event Event21() Implements iTest.Event2
    Sub raiser()
        RaiseEvent Event21()
    End Sub
End Class

Public Interface iTest
    Event Event2()
End Interface


    </file>
</compilation>, expectedOutput:="inside EventHandler2")

            c.VerifyIL("C1(Of T).set_o(T)", <![CDATA[
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (iTest.Event2EventHandler V_0,
                T V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub C1(Of T).EventHandler2()"
  IL_0007:  newobj     "Sub iTest.Event2EventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "C1(Of T)._o As T"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  box        "T"
  IL_001a:  brfalse.s  IL_002a
  IL_001c:  ldloca.s   V_1
  IL_001e:  ldloc.0
  IL_001f:  constrained. "T"
  IL_0025:  callvirt   "Sub iTest.remove_Event2(iTest.Event2EventHandler)"
  IL_002a:  ldarg.0
  IL_002b:  ldarg.1
  IL_002c:  stfld      "C1(Of T)._o As T"
  IL_0031:  ldarg.0
  IL_0032:  ldfld      "C1(Of T)._o As T"
  IL_0037:  stloc.1
  IL_0038:  ldloc.1
  IL_0039:  box        "T"
  IL_003e:  brfalse.s  IL_004e
  IL_0040:  ldloca.s   V_1
  IL_0042:  ldloc.0
  IL_0043:  constrained. "T"
  IL_0049:  callvirt   "Sub iTest.add_Event2(iTest.Event2EventHandler)"
  IL_004e:  ret
}
]]>)

        End Sub

        <WorkItem(6214, "https://github.com/dotnet/roslyn/issues/6214")>
        <WorkItem(545182, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545182")>
        <WorkItem(545184, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545184")>
        <Fact>
        Public Sub TestHandlesForWithEventsFromBaseFromADifferentAssembly()
            Dim assembly1Compilation = CreateVisualBasicCompilation("TestHandlesForWithEventsFromBaseFromADifferentAssembly_Assembly1",
            <![CDATA[Public Class c1
    Sub New()
        goo = Me
    End Sub
    Public Event loo()
    Public WithEvents goo As c1
    Sub raise()
        RaiseEvent loo()
    End Sub
End Class]]>,
                compilationOptions:=TestOptions.ReleaseDll)

            ' Verify that "AccessedThroughPropertyAttribute" is being emitted for WithEvents field.
            Dim assembly1Verifier = CompileAndVerify(assembly1Compilation, expectedSignatures:=
            {
                Signature("c1", "_goo", ".field [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] " &
                                               "[System.Runtime.CompilerServices.AccessedThroughPropertyAttribute(""goo"")] private instance c1 _goo")
            })

            assembly1Verifier.VerifyDiagnostics()

            Dim assembly2Compilation = CreateVisualBasicCompilation("TestHandlesForWithEventsFromBaseFromADifferentAssembly_Assembly2",
            <![CDATA[Imports System
Class c2
    Inherits c1
    Public res As Boolean = False
    Sub test() Handles goo.loo 'here
        res = True
    End Sub
End Class
Public Module Program
    Sub Main()
        Dim c As New c2
        c.goo.raise()
        Console.WriteLine(c.res)
    End Sub
End Module]]>,
                compilationOptions:=TestOptions.ReleaseExe,
                referencedCompilations:={assembly1Compilation})

            CompileAndVerify(assembly2Compilation, <![CDATA[True]]>).VerifyDiagnostics()
        End Sub

        <WorkItem(6214, "https://github.com/dotnet/roslyn/issues/6214")>
        <WorkItem(545185, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545185")>
        <Fact>
        Public Sub TestNameOfWithEventsSetterParameter()
            Dim comp = CreateVisualBasicCompilation("TestNameOfWithEventsSetterParameter",
            <![CDATA[Public Class c1
    Sub New()
        goo = Me
    End Sub
    Public WithEvents goo As c1
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            Dim verifier = CompileAndVerify(comp, expectedSignatures:=
            {
                Signature("c1", "goo", ".property readwrite instance c1 goo"),
                Signature("c1", "set_goo", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public newslot strict specialname virtual instance System.Void set_goo(c1 WithEventsValue) cil managed synchronized"),
                Signature("c1", "get_goo", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public newslot strict specialname virtual instance c1 get_goo() cil managed")
            })

            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(529548, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529548")>
        <Fact()>
        Public Sub TestIssueWeFixedInNativeCompiler()
            Dim comp = CreateVisualBasicCompilation("TestIssueWeFixedInNativeCompiler",
            <![CDATA[
Imports System
Interface I1(Of U)
    Event E1(ByVal a As U)
End Interface

Class C1(Of T As {Exception, I1(Of T)})
    Dim WithEvents x As T

    'Native compiler used to report an incorrect error on the below line - "Method 'Public Sub goo(a As T)' cannot handle event 'Public Event E1(a As U)' because they do not have a compatible signature".
    'This was a bug in the native compiler (see Bug: VSWhidbey/544224) that got fixed in Roslyn.
    'See Vladimir's comments in bug 13489  for more details.
    Sub goo(ByVal a As T) Handles x.E1
    End Sub
    Sub bar()
        AddHandler x.E1, AddressOf goo 'AddHandler should also work
    End Sub
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim verifier = CompileAndVerify(comp)
            verifier.VerifyDiagnostics()
        End Sub

        <WorkItem(545188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545188")>
        <Fact>
        Public Sub Bug13470()
            Dim compilation1 = CompileAndVerify(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
        Dim c1 = GetType(C1)
        Dim _Goo = c1.GetField("_Goo", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Static Or Reflection.BindingFlags.Instance)
        Dim obsolete = _Goo.GetCustomAttributes(GetType(ObsoleteAttribute), False)

        System.Console.WriteLine(obsolete.Length = 1)
    End Sub
End Class

Class C1
    <Obsolete> WithEvents Goo as C1
End Class
    ]]></file>
</compilation>, expectedOutput:="True")
        End Sub

        <WorkItem(545187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545187")>
        <Fact>
        Public Sub Bug13469()
            'Note: Below IL is for the following VB code compiled with Dev11 (using /debug-) -
            'Public Class c1
            '   Sub New()
            '        goo = Me
            '    End Sub
            '    Public Event loo()
            '    Public WithEvents goo As C1
            '    Sub raise()
            '        RaiseEvent loo()
            '    End Sub
            'End Class

            Dim customIL = <![CDATA[
.class public auto ansi c1
       extends [mscorlib]System.Object
{
  .class auto ansi sealed nested public looEventHandler
         extends [mscorlib]System.MulticastDelegate
  {
    .method public specialname rtspecialname 
            instance void  .ctor(object TargetObject,
                                 native int TargetMethod) runtime managed
    {
    } // end of method looEventHandler::.ctor

    .method public newslot strict virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class [mscorlib]System.AsyncCallback DelegateCallback,
                        object DelegateAsyncState) runtime managed
    {
    } // end of method looEventHandler::BeginInvoke

    .method public newslot strict virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
    {
    } // end of method looEventHandler::EndInvoke

    .method public newslot strict virtual 
            instance void  Invoke() runtime managed
    {
    } // end of method looEventHandler::Invoke

  } // end of class looEventHandler

  .field private class c1/looEventHandler looEvent
  .field private class c1 _goo
  .custom instance void [mscorlib]System.Runtime.CompilerServices.AccessedThroughPropertyAttribute::.ctor(string) = ( 01 00 03 67 6F 6F 00 00 )                         // ...goo..
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.0
    IL_0008:  callvirt   instance void c1::set_goo(class c1)
    IL_000d:  ret
  } // end of method c1::.ctor

  .method public specialname instance void 
          add_loo(class c1/looEventHandler obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class c1/looEventHandler c1::looEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_000d:  castclass  c1/looEventHandler
    IL_0012:  stfld      class c1/looEventHandler c1::looEvent
    IL_0017:  ret
  } // end of method c1::add_loo

  .method public specialname instance void 
          remove_loo(class c1/looEventHandler obj) cil managed synchronized
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class c1/looEventHandler c1::looEvent
    IL_0007:  ldarg.1
    IL_0008:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_000d:  castclass  c1/looEventHandler
    IL_0012:  stfld      class c1/looEventHandler c1::looEvent
    IL_0017:  ret
  } // end of method c1::remove_loo

  .method public newslot specialname strict virtual 
          instance class c1  get_goo() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (class c1 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class c1 c1::_goo
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method c1::get_goo

  .method public newslot specialname strict virtual 
          instance void  set_goo(class c1 WithEventsValue) cil managed synchronized
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      class c1 c1::_goo
    IL_0007:  ret
  } // end of method c1::set_goo

  .method public instance void  raise() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  1
    .locals init (class c1/looEventHandler V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class c1/looEventHandler c1::looEvent
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0010

    IL_000a:  ldloc.0
    IL_000b:  callvirt   instance void c1/looEventHandler::Invoke()
    IL_0010:  ret
  } // end of method c1::raise

  .event c1/looEventHandler loo
  {
    .addon instance void c1::add_loo(class c1/looEventHandler)
    .removeon instance void c1::remove_loo(class c1/looEventHandler)
  } // end of event c1::loo
  .property instance class c1 goo()
  {
    .get instance class c1 c1::get_goo()
    .set instance void c1::set_goo(class c1)
  } // end of property c1::goo
} // end of class c1
]]>

            Dim compilation = CreateCompilationWithCustomILSource(
<compilation>
    <file name="a.vb">
Imports System
Class c2
    Inherits c1
    Public res As Boolean = False
    Sub test() Handles goo.loo 'here
        res = True
    End Sub
End Class
Public Module Program
    Sub Main()
        Dim c As New c2
        c.goo.raise()
        Console.WriteLine(c.res)
    End Sub
End Module
    </file>
</compilation>, customIL.Value, includeVbRuntime:=True, options:=TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="True").VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub SynthesizedOverridingWithEventsProperty()
            Dim source =
<compilation>
    <file>
Public Class Base
    Protected Friend WithEvents w As Base = Me
    Public Event e As System.Action

    Sub H1() Handles w.e
    End Sub
End Class

Public Class Derived
    Inherits Base

    Sub H2() Handles w.e
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact(), WorkItem(545250, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545250")>
        Public Sub HookupOrder()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Public Class Base
    Protected Friend WithEvents w As Base = Me

    Public Event e As Action(Of Integer)

    Sub BaseH1() Handles w.e
        Console.WriteLine("  BaseH1")
    End Sub

    Function BaseH2(ParamArray x() As Integer) Handles Me.e
        Console.WriteLine("  BaseH2")
        Return 0
    End Function

    Sub Raise()
        RaiseEvent e(1)
    End Sub
End Class

Public Class Derived 
    Inherits Base

    Function DerivedH2(ParamArray x() As Integer) Handles Me.e
        Console.WriteLine("  DerivedH2")
        Return 0
    End Function

    Sub DerivedH1() Handles w.e
        Console.WriteLine("  DerivedH1")
    End Sub
End Class

Public Module Program
    Sub Main()
        Console.WriteLine("Base")
        Dim x = New Base()
        x.Raise()
        Console.WriteLine("Derived")
        x = New Derived
        x.Raise()
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:=<![CDATA[Base
  BaseH2
  BaseH1
Derived
  BaseH2
  BaseH1
  DerivedH1
  DerivedH2
]]>)
        End Sub

        <Fact, WorkItem(529653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529653")>
        Public Sub TestExecutionOrder1()
            Dim vbCompilation = CreateVisualBasicCompilation("TestExecutionOrder1",
            <![CDATA[Imports System, System.Collections.Generic

Public Interface I(Of T)
    Event e As Action(Of List(Of T))
End Interface
Public Class Base(Of T,
                     U As Class,
                     V As Structure,
                     W As {T, Exception},
                     X As {Dictionary(Of U, V), IDictionary(Of U, V)},
                     Y As IList(Of IList(Of T))) : Implements I(Of T)
    Public WithEvents Base As Base(Of T, U, V, W, X, Y) = Me
    Public WithEvents w3 As I(Of T)
    Public Event e As Action(Of List(Of T)) Implements I(Of T).e
    Public Sub Raise()
        RaiseEvent e(Nothing)
    End Sub
    Public Class Base2(Of A As {T, ArgumentException},
                          B As U,
                          C As X,
                          D As A) : Inherits Base(Of A, U, V, D, C, IList(Of IList(Of A)))
        Public WithEvents w As U
        Public Shadows WithEvents Base2 As Base(Of T, U, V, W, X, Y).Base2(Of A, B, C, D) = Me
        Public Shadows WithEvents Base33 As Base3
        Public Shadows Event e As Action(Of HashSet(Of T), Dictionary(Of T, W))
        Public Sub Raise2()
            RaiseEvent e(Nothing, Nothing)
        End Sub
        Public Class Base3 : Inherits Base2(Of A, B, C, D)

            Public Shadows WithEvents Base3 As Base(Of A, U, V, D, C, IList(Of IList(Of A))) = Me

            Function Goo(x As HashSet(Of A), y As Dictionary(Of A, D)) As Integer Handles Base33.e
                Console.WriteLine("1")
                Return 0
            End Function

            Function Goo(x As List(Of A)) As Dictionary(Of T, A) Handles Base3.e
                Console.WriteLine("2")
                Return New Dictionary(Of T, A)
            End Function

        End Class
    End Class
End Class

Public Structure S

End Structure

Public Class Derived(Of T As {Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException)))}) : Inherits Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException))).
        Base2(Of ArgumentException, String, Dictionary(Of String, S), ArgumentNullException).
        Base3 : Implements I(Of ArgumentException)
    Shadows Event e As Action(Of List(Of ArgumentException)) Implements I(Of ArgumentException).e
    Public Shadows WithEvents w As T = New Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException)))
    Public WithEvents w2 As Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException))).
        Base2(Of ArgumentException, String, Dictionary(Of String, S), ArgumentNullException).
        Base3 = New Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException))).
        Base2(Of ArgumentException, String, Dictionary(Of String, S), ArgumentNullException).
        Base3
    Shadows Sub Goo(x As HashSet(Of ArgumentException), y As Dictionary(Of ArgumentException, ArgumentNullException)) Handles Base2.e
        Console.WriteLine("3")
    End Sub
    Shadows Sub Goo() Handles Base3.e
        Console.WriteLine("4")
    End Sub
    Shadows Sub Goo(x As List(Of ArgumentException)) Handles w.e, Me.e, MyClass.e
        Console.WriteLine("5")
    End Sub
    Overloads Function goo2$(x As HashSet(Of ArgumentException), y As Dictionary(Of ArgumentException, ArgumentNullException)) Handles w2.e
        Console.WriteLine("6")
        Return 1.0
    End Function
    Function [function](x As List(Of ArgumentException)) Handles w.e, Me.e, MyClass.e
        Console.WriteLine("7")
        Return 1.0
    End Function
    Sub Raise3()
        RaiseEvent e(Nothing)
    End Sub
End Class

Public Module Program
    Sub Main()
        Dim x = New Derived(Of Base(Of ArgumentException, String, S, ArgumentException,
        Dictionary(Of String, S), List(Of IList(Of ArgumentException))))
        x.Raise()
        x.Raise2()
        x.w.Raise()
        x.w2.Raise()
        x.w2.Raise2()
        x.Raise3()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication))

            'Breaking Change: Dev11 processes event handlers in a different order than Roslyn.
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[2
4
3
5
7
2
6
5
5
7
7]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(529653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529653")>
        Public Sub TestExecutionOrder2()
            Dim assembly1Compilation = CreateVisualBasicCompilation("TestExecutionOrder2",
            <![CDATA[Option Strict Off
Imports System, System.Runtime.CompilerServices
Imports AliasedType = Base

<Assembly: InternalsVisibleTo("Assembly2")>
Public Class Base
    Protected WithEvents w As AliasedType = Me, x As Base = Me, y As Base = Me, z As New System.Collections.Generic.List(Of String) From {"123"}
    Protected Friend Event e As Action(Of Integer)

    Friend Sub H1() Handles w.e, x.e
        Console.WriteLine("Base H1")
    End Sub
    Public Function H2(ParamArray x() As Integer) Handles x.e, w.e, Me.e, MyClass.e
        Console.WriteLine("Base H2")
        Return 0
    End Function

    Overridable Sub Raise()
        RaiseEvent e(1)
    End Sub
End Class

Public Class Base2 : Inherits Base
End Class]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            Dim assembly1Verifier = CompileAndVerify(assembly1Compilation)
            assembly1Verifier.VerifyDiagnostics()

            Dim assembly2Compilation = CreateVisualBasicCompilation("Assembly2",
            <![CDATA[Imports System

Public Class Derived : Inherits Base2
    Shadows Event e As Action(Of Exception)
    Private Shadows Sub H1() Handles y.e, MyBase.e, x.e, w.e
        Console.WriteLine("Derived H1")
    End Sub
    Protected Shadows Function H2(ParamArray x() As Integer) Handles MyBase.e
        Console.WriteLine("Derived H2")
        Return 0
    End Function
End Class

Public Module Program
    Sub Main()
        Console.WriteLine("Base")
        Dim x = New Base()
        x.Raise()

        Console.WriteLine("Derived")
        x = New Derived
        x.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={assembly1Compilation})

            'Breaking Change: Dev11 processes event handlers in a different order than Roslyn.
            Dim assembly2Verifier = CompileAndVerify(assembly2Compilation,
                expectedOutput:=<![CDATA[Base
Base H2
Base H2
Base H1
Base H2
Base H1
Base H2
Derived
Base H2
Base H2
Base H1
Base H2
Derived H1
Base H1
Base H2
Derived H1
Derived H1
Derived H1
Derived H2]]>)
            assembly2Verifier.VerifyDiagnostics()
        End Sub

        <Fact(), WorkItem(545250, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545250"), WorkItem(529653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529653")>
        Public Sub TestCrossLanguageOptionalAndParamarray1()
            Dim csCompilation = CreateCSharpCompilation("TestCrossLanguageOptionalAndParamarray1_CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(string x = "");
    public event bar ev;
    public void raise()
    {
        ev("BASE");
    }
}]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("TestCrossLanguageOptionalAndParamarray1_VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED1")
        Return 0
    End Function
    Function Goo(x As String, ParamArray y() As Integer) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED2")
        Return 0
    End Function
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED3")
        Return 0
    End Function
    Function Goo2(ParamArray x() As String) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED4")
        Return 0
    End Function
    Function Goo2(x As String, Optional y As Integer = 0) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED5")
        Return 0
    End Function
    Function Goo3(Optional x As String = "", Optional y As Integer = 0) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED6")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})

            'Breaking Change: Dev11 processes event handlers in a different order than Roslyn.
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[BASE
DERIVED1
BASE
DERIVED1
BASE
DERIVED2
BASE
DERIVED2
BASE
DERIVED3
BASE
DERIVED3
System.String[]
DERIVED4
System.String[]
DERIVED4
BASE
DERIVED5
BASE
DERIVED5
BASE
DERIVED6
BASE
DERIVED6
BASE
DERIVED1
BASE
DERIVED2
BASE
DERIVED3
System.String[]
DERIVED4
BASE
DERIVED5
BASE
DERIVED6]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCrossLanguageOptionalAndPAramarray2()
            Dim csCompilation = CreateCSharpCompilation("TestCrossLanguageOptionalAndPAramarray2_CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(params int[] y);
    public event bar ev;
    public void raise()
    {
        ev(1, 2, 3);
    }
}]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("TestCrossLanguageOptionalAndPAramarray2_VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(x As Integer()) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED1")
        Return 0
    End Function
    Function Goo2(ParamArray x As Integer()) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED2")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[System.Int32[]
DERIVED1
System.Int32[]
DERIVED1
System.Int32[]
DERIVED2
System.Int32[]
DERIVED2
System.Int32[]
DERIVED1
System.Int32[]
DERIVED2]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestCrossLanguageOptionalAndParamarray_Error1()
            Dim csCompilation = CreateCSharpCompilation("TestCrossLanguageOptionalAndParamarray_Error1_CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(params int[] y);
    public event bar ev;
    public void raise()
    {
        ev(1, 2, 3);
    }
}]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("TestCrossLanguageOptionalAndParamarray_Error1_VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo2(x As Integer) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED1")
        Return 0
    End Function
    Function Goo2(x As Integer, Optional y As Integer = 1) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("DERIVED2")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})

            vbCompilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"))
        End Sub

        <Fact, WorkItem(545257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545257")>
        Public Sub TestCrossLanguageOptionalAndParamarray_Error2()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(params int[] y);
    public event bar ev;
    public void raise()
    {
        ev(1, 2, 3);
    }
}]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(Optional x As Integer = 1) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            vbCompilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo", "ev"))
        End Sub

        <Fact>
        Public Sub DelegateRelaxationStubsUniqueNaming1()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class WE
    Event V As Action(Of Integer, Integer)
    Event W As Action(Of Long, Long)
End Class

Class B
    Public WithEvents A3 As WE
End Class

Class D
    Inherits B

    Public WithEvents A1 As WE
    Public WithEvents A2 As WE
End Class

Class E
    Inherits D

    Sub G(z1 As Double, z2 As Double) Handles A1.V, A3.W
        Console.WriteLine(z1 + z2)
    End Sub

    Sub G(z1 As Double, z2 As Integer) Handles A1.V, A3.W
        Console.WriteLine(z1 + z2)
    End Sub

    Sub G(z1 As Integer, z2 As Double) Handles A2.V, A3.W, A1.V
        Console.WriteLine(z1 + z2)
    End Sub

    Sub G(z1 As Byte, z2 As Integer) Handles A2.V, A3.W, A1.V
        Console.WriteLine(z1 + z2)
    End Sub

    Sub F(z1 As Byte, z2 As Integer) Handles A2.V, A3.W, A1.V
        Console.WriteLine(z1 + z2)
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact>
        Public Sub DelegateRelaxationStubsUniqueNaming2()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System

Class WE
    Public Event V1 As Action(Of Integer)
    Public Event V2 As Action(Of Integer)
End Class

Class B
    Public WithEvents we As WE = New WE
    
    Public Event W1 As Action(Of Integer)
    Public Event W2 As Action(Of Integer)
End Class

Class C 
    Inherits B

    Public Event U1 As Action(Of Integer)
    Public Event U2 As Action(Of Integer)
    
    Sub F() Handles we.V1, we.V2, MyBase.W1, MyBase.W2, Me.U1, Me.U2   
    End Sub
End Class
    </file>
</compilation>

            CompileAndVerify(source)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/79350"), WorkItem(4544, "https://github.com/dotnet/roslyn/issues/4544")>
        Public Sub MultipleInitializationsWithAsNew_01()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Shared WithEvents a1, b1, c1 As New C2()
    WithEvents a2, b2, c2 As New C2()
    Shared a3, b3, c3 As New C2()
    Dim a4, b4, c4 As New C2()

    Shared Sub Main()
        Check(a1, b1, c1)
        Check(a3, b3, c3)
        
        Dim c as New C1()
        Check(c.a2, c.b2, c.c2)
        Check(c.a4, c.b4, c.c4)
    End Sub

    Private Shared Sub Check(a As Object, b As Object, c As Object)
        System.Console.WriteLine(a Is Nothing)
        System.Console.WriteLine(b Is Nothing)
        System.Console.WriteLine(c Is Nothing)
        System.Console.WriteLine(a Is b)
        System.Console.WriteLine(a Is c)
        System.Console.WriteLine(b Is c)
    End Sub
End Class

Class C2
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=
            <![CDATA[
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
False
]]>)
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/79350"), WorkItem(4544, "https://github.com/dotnet/roslyn/issues/4544")>
        Public Sub MultipleInitializationsWithAsNew_02()
            Dim source =
<compilation>
    <file name="a.vb">
Class C1
    Shared WithEvents a, b, c As New C1() With {.P1 = 2}

    Shared Sub Main()
        System.Console.WriteLine(a.P1)
        System.Console.WriteLine(b.P1)
        System.Console.WriteLine(c.P1)
        System.Console.WriteLine(a Is b)
        System.Console.WriteLine(a Is c)
        System.Console.WriteLine(b Is c)
    End Sub

    Public P1 As Integer
End Class
    </file>
</compilation>

            CompileAndVerify(source, expectedOutput:=
            <![CDATA[
2
2
2
False
False
False
]]>).
            VerifyIL("C1..cctor",
            <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  3
  IL_0000:  newobj     "Sub C1..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.2
  IL_0007:  stfld      "C1.P1 As Integer"
  IL_000c:  call       "Sub C1.set_a(C1)"
  IL_0011:  newobj     "Sub C1..ctor()"
  IL_0016:  dup
  IL_0017:  ldc.i4.2
  IL_0018:  stfld      "C1.P1 As Integer"
  IL_001d:  call       "Sub C1.set_b(C1)"
  IL_0022:  newobj     "Sub C1..ctor()"
  IL_0027:  dup
  IL_0028:  ldc.i4.2
  IL_0029:  stfld      "C1.P1 As Integer"
  IL_002e:  call       "Sub C1.set_c(C1)"
  IL_0033:  ret
}
]]>)
        End Sub

    End Class
End Namespace
