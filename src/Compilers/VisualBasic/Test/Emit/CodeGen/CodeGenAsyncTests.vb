' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class AsyncTests
        Inherits BasicTestBase

        <WorkItem(1004348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1004348")>
        <Fact>
        Public Sub StructVsClass()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System.Threading.Tasks
        
Module Module1

    Sub Main()
        Foo(123).Wait()
    End Sub

    Public Async Function Foo(a As Integer) As Task
        Await Task.Factory.StartNew(Sub() System.Console.WriteLine(a))
    End Function

End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences)
            Dim options As VisualBasicCompilationOptions

            options = TestOptions.ReleaseExe
            Assert.False(options.EnableEditAndContinue)

            CompileAndVerify(compilation.WithOptions(options),
                             expectedOutput:="123",
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim stateMachine = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Module1").GetMember(Of NamedTypeSymbol)("VB$StateMachine_1_Foo")
                                                  Assert.Equal(TypeKind.Structure, stateMachine.TypeKind)
                                              End Sub)

            options = TestOptions.DebugExe
            Assert.True(options.EnableEditAndContinue)

            CompileAndVerify(compilation.WithOptions(options),
                             expectedOutput:="123",
                             symbolValidator:=Sub(m As ModuleSymbol)
                                                  Dim stateMachine = m.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Module1").GetMember(Of NamedTypeSymbol)("VB$StateMachine_1_Foo")
                                                  Assert.Equal(TypeKind.Class, stateMachine.TypeKind)
                                              End Sub)
        End Sub

        <Fact()>
        Public Sub Simple_Void()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public handle As New AutoResetEvent(False)
    Sub Main()
        Console.Write("0 ")
        f()
        handle.WaitOne(60000)
        Console.Write("1 ")
    End Sub

    Async Sub f()
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        handle.Set()
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 1")

            c.VerifyIL("Form1.Main", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldstr      "0 "
  IL_0005:  call       "Sub System.Console.Write(String)"
  IL_000a:  call       "Sub Form1.f()"
  IL_000f:  ldsfld     "Form1.handle As System.Threading.AutoResetEvent"
  IL_0014:  ldc.i4     0xea60
  IL_0019:  callvirt   "Function System.Threading.WaitHandle.WaitOne(Integer) As Boolean"
  IL_001e:  pop
  IL_001f:  ldstr      "1 "
  IL_0024:  call       "Sub System.Console.Write(String)"
  IL_0029:  ret
}
]]>)
            c.VerifyIL("Form1.f", <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (Form1.VB$StateMachine_3_f V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Form1.VB$StateMachine_3_f"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.m1
  IL_000b:  stfld      "Form1.VB$StateMachine_3_f.$State As Integer"
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       "Function System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Create() As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_0017:  stfld      "Form1.VB$StateMachine_3_f.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldflda     "Form1.VB$StateMachine_3_f.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_0023:  ldloca.s   V_0
  IL_0025:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.Start(Of Form1.VB$StateMachine_3_f)(ByRef Form1.VB$StateMachine_3_f)"
  IL_002a:  ret
}
]]>)
            c.VerifyIL("Form1.VB$StateMachine_3_f.MoveNext", <![CDATA[
{
  // Code size      197 (0xc5)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_3_f.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldstr      "2 "
    IL_000f:  call       "Sub System.Console.Write(String)"
    IL_0014:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0021:  stloc.1
    IL_0022:  ldloca.s   V_1
    IL_0024:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      "Form1.VB$StateMachine_3_f.$State As Integer"
    IL_0034:  ldarg.0
    IL_0035:  ldloc.1
    IL_0036:  stfld      "Form1.VB$StateMachine_3_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_003b:  ldarg.0
    IL_003c:  ldflda     "Form1.VB$StateMachine_3_f.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_0041:  ldloca.s   V_1
    IL_0043:  ldarg.0
    IL_0044:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Form1.VB$StateMachine_3_f)(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Form1.VB$StateMachine_3_f)"
    IL_0049:  leave.s    IL_00c4
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Form1.VB$StateMachine_3_f.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Form1.VB$StateMachine_3_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Form1.VB$StateMachine_3_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldstr      "3 "
    IL_007b:  call       "Sub System.Console.Write(String)"
    IL_0080:  ldsfld     "Form1.handle As System.Threading.AutoResetEvent"
    IL_0085:  callvirt   "Function System.Threading.EventWaitHandle.Set() As Boolean"
    IL_008a:  pop
    IL_008b:  leave.s    IL_00af
  }
  catch System.Exception
  {
    IL_008d:  dup
    IL_008e:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0093:  stloc.3
    IL_0094:  ldarg.0
    IL_0095:  ldc.i4.s   -2
    IL_0097:  stfld      "Form1.VB$StateMachine_3_f.$State As Integer"
    IL_009c:  ldarg.0
    IL_009d:  ldflda     "Form1.VB$StateMachine_3_f.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_00a2:  ldloc.3
    IL_00a3:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_00a8:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ad:  leave.s    IL_00c4
  }
  IL_00af:  ldarg.0
  IL_00b0:  ldc.i4.s   -2
  IL_00b2:  dup
  IL_00b3:  stloc.0
  IL_00b4:  stfld      "Form1.VB$StateMachine_3_f.$State As Integer"
  IL_00b9:  ldarg.0
  IL_00ba:  ldflda     "Form1.VB$StateMachine_3_f.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_00bf:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_00c4:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_Test()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write(TestLocal({1}).Result.ToString + " ")
    End Sub

    Async Function TestLocal(p As Integer()) As Task(Of Integer)
        Return M(p(0), Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Await Task.Yield
        Return 1
    End Function

    Public Function M(ByRef x As Double, y As Integer) As Integer
        Return x + y
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="2")
        End Sub

        <Fact()>
        Public Sub Simple_Task()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        f().Wait(60000)
        Console.Write("1 ")
    End Sub

    Async Function f() As Task
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
    End Function 
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 1").
            VerifyIL("Form1.VB$StateMachine_1_f.MoveNext",
            <![CDATA[
{
  // Code size      186 (0xba)
  .maxstack  3
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldstr      "2 "
    IL_000f:  call       "Sub System.Console.Write(String)"
    IL_0014:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0019:  stloc.2
    IL_001a:  ldloca.s   V_2
    IL_001c:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0021:  stloc.1
    IL_0022:  ldloca.s   V_1
    IL_0024:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.0
    IL_002f:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0034:  ldarg.0
    IL_0035:  ldloc.1
    IL_0036:  stfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_003b:  ldarg.0
    IL_003c:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0041:  ldloca.s   V_1
    IL_0043:  ldarg.0
    IL_0044:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Form1.VB$StateMachine_1_f)(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Form1.VB$StateMachine_1_f)"
    IL_0049:  leave.s    IL_00b9
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.0
    IL_004f:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.1
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_1
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_1
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldstr      "3 "
    IL_007b:  call       "Sub System.Console.Write(String)"
    IL_0080:  leave.s    IL_00a4
  }
  catch System.Exception
  {
    IL_0082:  dup
    IL_0083:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0088:  stloc.3
    IL_0089:  ldarg.0
    IL_008a:  ldc.i4.s   -2
    IL_008c:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
    IL_0097:  ldloc.3
    IL_0098:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)"
    IL_009d:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00a2:  leave.s    IL_00b9
  }
  IL_00a4:  ldarg.0
  IL_00a5:  ldc.i4.s   -2
  IL_00a7:  dup
  IL_00a8:  stloc.0
  IL_00a9:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_00ae:  ldarg.0
  IL_00af:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
  IL_00b4:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()"
  IL_00b9:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(f().Result.ToString() + " ")
        Console.Write("1 ")
    End Sub

    Async Function f() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 123
    End Function 
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 123 1").
            VerifyIL("Form1.VB$StateMachine_1_f.MoveNext",
            <![CDATA[
{
  // Code size      192 (0xc0)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_004b
    IL_000a:  ldstr      "2 "
    IL_000f:  call       "Sub System.Console.Write(String)"
    IL_0014:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
    IL_0019:  stloc.3
    IL_001a:  ldloca.s   V_3
    IL_001c:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0021:  stloc.2
    IL_0022:  ldloca.s   V_2
    IL_0024:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
    IL_0029:  brtrue.s   IL_0067
    IL_002b:  ldarg.0
    IL_002c:  ldc.i4.0
    IL_002d:  dup
    IL_002e:  stloc.1
    IL_002f:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0034:  ldarg.0
    IL_0035:  ldloc.2
    IL_0036:  stfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_003b:  ldarg.0
    IL_003c:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldarg.0
    IL_0044:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Form1.VB$StateMachine_1_f)(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Form1.VB$StateMachine_1_f)"
    IL_0049:  leave.s    IL_00bf
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.m1
    IL_004d:  dup
    IL_004e:  stloc.1
    IL_004f:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_005a:  stloc.2
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0061:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0067:  ldloca.s   V_2
    IL_0069:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
    IL_006e:  ldloca.s   V_2
    IL_0070:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
    IL_0076:  ldstr      "3 "
    IL_007b:  call       "Sub System.Console.Write(String)"
    IL_0080:  ldc.i4.s   123
    IL_0082:  stloc.0
    IL_0083:  leave.s    IL_00a9
  }
  catch System.Exception
  {
    IL_0085:  dup
    IL_0086:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_008b:  stloc.s    V_4
    IL_008d:  ldarg.0
    IL_008e:  ldc.i4.s   -2
    IL_0090:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_0095:  ldarg.0
    IL_0096:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_009b:  ldloc.s    V_4
    IL_009d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00a2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00a7:  leave.s    IL_00bf
  }
  IL_00a9:  ldarg.0
  IL_00aa:  ldc.i4.s   -2
  IL_00ac:  dup
  IL_00ad:  stloc.1
  IL_00ae:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_00b3:  ldarg.0
  IL_00b4:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00b9:  ldloc.0
  IL_00ba:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00bf:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_1()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write((Async Function() Await f())().Result.ToString() + " ")
        Console.Write("1 ")
    End Sub

    Async Function f() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 123
    End Function 
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 123 1")

            c.VerifyIL("Form1._Closure$__._Lambda$__0-0",
            <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (Form1._Closure$__.VB$StateMachine___Lambda$__0-0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Form1._Closure$__.VB$StateMachine___Lambda$__0-0"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldarg.0
  IL_000b:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$VB$NonLocal__Closure$__ As Form1._Closure$__"
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer"
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).Create() As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_001f:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).Start(Of Form1._Closure$__.VB$StateMachine___Lambda$__0-0)(ByRef Form1._Closure$__.VB$StateMachine___Lambda$__0-0)"
  IL_0032:  ldloca.s   V_0
  IL_0034:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_0039:  call       "Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).get_Task() As System.Threading.Tasks.Task(Of Integer)"
  IL_003e:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_2()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Dim outer As Integer = 123
        Console.Write("0 ")
        Console.Write((Async Function()
                  Return Await f() + outer
                       End Function)().Result.ToString() + " ")
        Console.Write("1 ")
    End Sub

    Async Function f() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 123
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 246 1")

            c.VerifyIL("Form1._Closure$__0-0._Lambda$__0", <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (Form1._Closure$__0-0.VB$StateMachine___Lambda$__0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0"
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldarg.0
  IL_000b:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__0-0 As Form1._Closure$__0-0"
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldc.i4.m1
  IL_0013:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       "Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).Create() As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_001f:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_0024:  ldloca.s   V_0
  IL_0026:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_002b:  ldloca.s   V_0
  IL_002d:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).Start(Of Form1._Closure$__0-0.VB$StateMachine___Lambda$__0)(ByRef Form1._Closure$__0-0.VB$StateMachine___Lambda$__0)"
  IL_0032:  ldloca.s   V_0
  IL_0034:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_0039:  call       "Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).get_Task() As System.Threading.Tasks.Task(Of Integer)"
  IL_003e:  ret
}
]]>)
            c.VerifyIL("Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.MoveNext", <![CDATA[
{
  // Code size      177 (0xb1)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_003e
    IL_000a:  call       "Function Form1.f() As System.Threading.Tasks.Task(Of Integer)"
    IL_000f:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0014:  stloc.2
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_001c:  brtrue.s   IL_005a
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  dup
    IL_0021:  stloc.1
    IL_0022:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0027:  ldarg.0
    IL_0028:  ldloc.2
    IL_0029:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_002e:  ldarg.0
    IL_002f:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0034:  ldloca.s   V_2
    IL_0036:  ldarg.0
    IL_0037:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Form1._Closure$__0-0.VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Form1._Closure$__0-0.VB$StateMachine___Lambda$__0)"
    IL_003c:  leave.s    IL_00b0
    IL_003e:  ldarg.0
    IL_003f:  ldc.i4.m1
    IL_0040:  dup
    IL_0041:  stloc.1
    IL_0042:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0047:  ldarg.0
    IL_0048:  ldfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004d:  stloc.2
    IL_004e:  ldarg.0
    IL_004f:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0054:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005a:  ldloca.s   V_2
    IL_005c:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0061:  ldloca.s   V_2
    IL_0063:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0069:  ldarg.0
    IL_006a:  ldfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__0-0 As Form1._Closure$__0-0"
    IL_006f:  ldfld      "Form1._Closure$__0-0.$VB$Local_outer As Integer"
    IL_0074:  add.ovf
    IL_0075:  stloc.0
    IL_0076:  leave.s    IL_009a
  }
  catch System.Exception
  {
    IL_0078:  dup
    IL_0079:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_007e:  stloc.3
    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.s   -2
    IL_0082:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0087:  ldarg.0
    IL_0088:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_008d:  ldloc.3
    IL_008e:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_0093:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0098:  leave.s    IL_00b0
  }
  IL_009a:  ldarg.0
  IL_009b:  ldc.i4.s   -2
  IL_009d:  dup
  IL_009e:  stloc.1
  IL_009f:  stfld      "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_00a4:  ldarg.0
  IL_00a5:  ldflda     "Form1._Closure$__0-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00aa:  ldloc.0
  IL_00ab:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00b0:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_3()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Call (Async Sub() f().Wait(60000))()
        Console.Write("1 ")
        Call (Async Sub()
                  f().Wait(60000)
              End Sub)()
        Console.Write("5 ")
    End Sub

    Async Function f() As Task
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 1 2 3 5")

            c.VerifyIL("Form1._Closure$__.VB$StateMachine___Lambda$__0-1.MoveNext",
            <![CDATA[
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (Integer V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  call       "Function Form1.f() As System.Threading.Tasks.Task"
    IL_000c:  ldc.i4     0xea60
    IL_0011:  callvirt   "Function System.Threading.Tasks.Task.Wait(Integer) As Boolean"
    IL_0016:  pop
    IL_0017:  leave.s    IL_003b
  }
  catch System.Exception
  {
    IL_0019:  dup
    IL_001a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001f:  stloc.1
    IL_0020:  ldarg.0
    IL_0021:  ldc.i4.s   -2
    IL_0023:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer"
    IL_0028:  ldarg.0
    IL_0029:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-1.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_002e:  ldloc.1
    IL_002f:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0034:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0039:  leave.s    IL_0050
  }
  IL_003b:  ldarg.0
  IL_003c:  ldc.i4.s   -2
  IL_003e:  dup
  IL_003f:  stloc.0
  IL_0040:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-1.$State As Integer"
  IL_0045:  ldarg.0
  IL_0046:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-1.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_004b:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_0050:  ret
}
]]>)
            c.VerifyIL("Form1._Closure$__.VB$StateMachine___Lambda$__0-0.MoveNext",
            <![CDATA[
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (Integer V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  call       "Function Form1.f() As System.Threading.Tasks.Task"
    IL_000c:  ldc.i4     0xea60
    IL_0011:  callvirt   "Function System.Threading.Tasks.Task.Wait(Integer) As Boolean"
    IL_0016:  pop
    IL_0017:  leave.s    IL_003b
  }
  catch System.Exception
  {
    IL_0019:  dup
    IL_001a:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_001f:  stloc.1
    IL_0020:  ldarg.0
    IL_0021:  ldc.i4.s   -2
    IL_0023:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer"
    IL_0028:  ldarg.0
    IL_0029:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_002e:  ldloc.1
    IL_002f:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_0034:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0039:  leave.s    IL_0050
  }
  IL_003b:  ldarg.0
  IL_003c:  ldc.i4.s   -2
  IL_003e:  dup
  IL_003f:  stloc.0
  IL_0040:  stfld      "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$State As Integer"
  IL_0045:  ldarg.0
  IL_0046:  ldflda     "Form1._Closure$__.VB$StateMachine___Lambda$__0-0.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_004b:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_0050:  ret
}
]]>)
        End Sub

        <Fact>
        Public Sub Simple_TaskOfT_Lambda_4_nyi()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Console.Write((New CLAZZ()).F().Result.ToString + " ")
        Console.Write("X2 ")
    End Sub
End Module

Class CLAZZ
    Public FX As Integer = 1

    Public Async Function F() As Task(Of Integer)
        Dim outer As Integer = 100
        Console.Write("0 ")
        Dim a = Async Function()
                    Return outer + Me.FX + (Await f2()) + outer + Me.FX
                End Function
        Console.Write("1 ")
        Return Await a()
    End Function

    Async Function f2() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 10
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="X1 0 1 2 3 212 X2")

            c.VerifyIL("CLAZZ.VB$StateMachine_2_F.MoveNext",
            <![CDATA[
{
  // Code size      221 (0xdd)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "CLAZZ.VB$StateMachine_2_F.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0076
    IL_000a:  newobj     "Sub CLAZZ._Closure$__2-0..ctor()"
    IL_000f:  dup
    IL_0010:  ldarg.0
    IL_0011:  ldfld      "CLAZZ.VB$StateMachine_2_F.$VB$Me As CLAZZ"
    IL_0016:  stfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_001b:  dup
    IL_001c:  ldc.i4.s   100
    IL_001e:  stfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_0023:  ldstr      "0 "
    IL_0028:  call       "Sub System.Console.Write(String)"
    IL_002d:  ldftn      "Function CLAZZ._Closure$__2-0._Lambda$__0() As System.Threading.Tasks.Task(Of Integer)"
    IL_0033:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Threading.Tasks.Task(Of Integer))..ctor(Object, System.IntPtr)"
    IL_0038:  ldstr      "1 "
    IL_003d:  call       "Sub System.Console.Write(String)"
    IL_0042:  callvirt   "Function VB$AnonymousDelegate_0(Of System.Threading.Tasks.Task(Of Integer)).Invoke() As System.Threading.Tasks.Task(Of Integer)"
    IL_0047:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_004c:  stloc.2
    IL_004d:  ldloca.s   V_2
    IL_004f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0054:  brtrue.s   IL_0092
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.0
    IL_0058:  dup
    IL_0059:  stloc.1
    IL_005a:  stfld      "CLAZZ.VB$StateMachine_2_F.$State As Integer"
    IL_005f:  ldarg.0
    IL_0060:  ldloc.2
    IL_0061:  stfld      "CLAZZ.VB$StateMachine_2_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0066:  ldarg.0
    IL_0067:  ldflda     "CLAZZ.VB$StateMachine_2_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_006c:  ldloca.s   V_2
    IL_006e:  ldarg.0
    IL_006f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), CLAZZ.VB$StateMachine_2_F)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef CLAZZ.VB$StateMachine_2_F)"
    IL_0074:  leave.s    IL_00dc
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.m1
    IL_0078:  dup
    IL_0079:  stloc.1
    IL_007a:  stfld      "CLAZZ.VB$StateMachine_2_F.$State As Integer"
    IL_007f:  ldarg.0
    IL_0080:  ldfld      "CLAZZ.VB$StateMachine_2_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0085:  stloc.2
    IL_0086:  ldarg.0
    IL_0087:  ldflda     "CLAZZ.VB$StateMachine_2_F.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0092:  ldloca.s   V_2
    IL_0094:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0099:  ldloca.s   V_2
    IL_009b:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a1:  stloc.0
    IL_00a2:  leave.s    IL_00c6
  }
  catch System.Exception
  {
    IL_00a4:  dup
    IL_00a5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00aa:  stloc.3
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.s   -2
    IL_00ae:  stfld      "CLAZZ.VB$StateMachine_2_F.$State As Integer"
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     "CLAZZ.VB$StateMachine_2_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00b9:  ldloc.3
    IL_00ba:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00bf:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c4:  leave.s    IL_00dc
  }
  IL_00c6:  ldarg.0
  IL_00c7:  ldc.i4.s   -2
  IL_00c9:  dup
  IL_00ca:  stloc.1
  IL_00cb:  stfld      "CLAZZ.VB$StateMachine_2_F.$State As Integer"
  IL_00d0:  ldarg.0
  IL_00d1:  ldflda     "CLAZZ.VB$StateMachine_2_F.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00d6:  ldloc.0
  IL_00d7:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00dc:  ret
}
]]>)
            c.VerifyIL("CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext",
            <![CDATA[
{
  // Code size      249 (0xf9)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_006e
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0011:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_0016:  ldarg.0
    IL_0017:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_001c:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0021:  ldfld      "CLAZZ.FX As Integer"
    IL_0026:  add.ovf
    IL_0027:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$U1 As Integer"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0032:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0037:  call       "Function CLAZZ.f2() As System.Threading.Tasks.Task(Of Integer)"
    IL_003c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0041:  stloc.2
    IL_0042:  ldloca.s   V_2
    IL_0044:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0049:  brtrue.s   IL_008a
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.0
    IL_004d:  dup
    IL_004e:  stloc.1
    IL_004f:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldloc.2
    IL_0056:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0061:  ldloca.s   V_2
    IL_0063:  ldarg.0
    IL_0064:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)"
    IL_0069:  leave      IL_00f8
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.1
    IL_0072:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0077:  ldarg.0
    IL_0078:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  stloc.2
    IL_007e:  ldarg.0
    IL_007f:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0084:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008a:  ldarg.0
    IL_008b:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$U1 As Integer"
    IL_0090:  ldloca.s   V_2
    IL_0092:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0097:  ldloca.s   V_2
    IL_0099:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009f:  add.ovf
    IL_00a0:  ldarg.0
    IL_00a1:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00a6:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_00ab:  add.ovf
    IL_00ac:  ldarg.0
    IL_00ad:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00b2:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_00b7:  ldfld      "CLAZZ.FX As Integer"
    IL_00bc:  add.ovf
    IL_00bd:  stloc.0
    IL_00be:  leave.s    IL_00e2
  }
  catch System.Exception
  {
    IL_00c0:  dup
    IL_00c1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00c6:  stloc.3
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.s   -2
    IL_00ca:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_00cf:  ldarg.0
    IL_00d0:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00d5:  ldloc.3
    IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00db:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00e0:  leave.s    IL_00f8
  }
  IL_00e2:  ldarg.0
  IL_00e3:  ldc.i4.s   -2
  IL_00e5:  dup
  IL_00e6:  stloc.1
  IL_00e7:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_00ec:  ldarg.0
  IL_00ed:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00f2:  ldloc.0
  IL_00f3:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00f8:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_5_nyi()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Console.Write((New CLAZZ()).F().Result.ToString + " ")
        Console.Write("X2 ")
    End Sub
End Module

Class CLAZZ
    Public FX As Integer = 1

    Public Async Function F() As Task(Of Integer)
        Dim outer As Integer = 100
        Console.Write("0 ")
        Dim a = Async Function()
                    Dim result = outer + Me.FX
                    result = Await f2() + result  ' Requires stack spilling because 'result' is hoisted
                    Return result + outer + Me.FX
                End Function
        Console.Write("1 ")
        Return Await a()
    End Function

    Async Function f2() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 10
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="X1 0 1 2 3 212 X2")
            c.VerifyIL("CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext",
            <![CDATA[
{
  // Code size      261 (0x105)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_006e
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0011:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_0016:  ldarg.0
    IL_0017:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_001c:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0021:  ldfld      "CLAZZ.FX As Integer"
    IL_0026:  add.ovf
    IL_0027:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0032:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0037:  call       "Function CLAZZ.f2() As System.Threading.Tasks.Task(Of Integer)"
    IL_003c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0041:  stloc.2
    IL_0042:  ldloca.s   V_2
    IL_0044:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0049:  brtrue.s   IL_008a
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.0
    IL_004d:  dup
    IL_004e:  stloc.1
    IL_004f:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldloc.2
    IL_0056:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0061:  ldloca.s   V_2
    IL_0063:  ldarg.0
    IL_0064:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)"
    IL_0069:  leave      IL_0104
    IL_006e:  ldarg.0
    IL_006f:  ldc.i4.m1
    IL_0070:  dup
    IL_0071:  stloc.1
    IL_0072:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0077:  ldarg.0
    IL_0078:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007d:  stloc.2
    IL_007e:  ldarg.0
    IL_007f:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0084:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008a:  ldarg.0
    IL_008b:  ldloca.s   V_2
    IL_008d:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0092:  ldloca.s   V_2
    IL_0094:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009a:  ldarg.0
    IL_009b:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_00a0:  add.ovf
    IL_00a1:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_00a6:  ldarg.0
    IL_00a7:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_00ac:  ldarg.0
    IL_00ad:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00b2:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_00b7:  add.ovf
    IL_00b8:  ldarg.0
    IL_00b9:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00be:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_00c3:  ldfld      "CLAZZ.FX As Integer"
    IL_00c8:  add.ovf
    IL_00c9:  stloc.0
    IL_00ca:  leave.s    IL_00ee
  }
  catch System.Exception
  {
    IL_00cc:  dup
    IL_00cd:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00d2:  stloc.3
    IL_00d3:  ldarg.0
    IL_00d4:  ldc.i4.s   -2
    IL_00d6:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_00db:  ldarg.0
    IL_00dc:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00e1:  ldloc.3
    IL_00e2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00e7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00ec:  leave.s    IL_0104
  }
  IL_00ee:  ldarg.0
  IL_00ef:  ldc.i4.s   -2
  IL_00f1:  dup
  IL_00f2:  stloc.1
  IL_00f3:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00fe:  ldloc.0
  IL_00ff:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_0104:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_6()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Console.Write((New CLAZZ()).F(1000).Result.ToString + " ")
        Console.Write("X2 ")
    End Sub
End Module

Class CLAZZ
    Public FX As Integer = 1

    Public Async Function F(p As Integer) As Task(Of Integer)
        Dim outer As Integer = 100
        Console.Write("0 ")
        Dim a = Async Function()
                    Dim result = outer + Me.FX
                    Dim x = Await f2()
                    Return x + result + p
                End Function
        Console.Write("1 ")
        Return Await a()
    End Function

    Async Function f2() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 10
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="X1 0 1 2 3 1111 X2")
            c.VerifyIL("CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext",
            <![CDATA[
{
  // Code size      229 (0xe5)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_006b
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0011:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_0016:  ldarg.0
    IL_0017:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_001c:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0021:  ldfld      "CLAZZ.FX As Integer"
    IL_0026:  add.ovf
    IL_0027:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0032:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0037:  call       "Function CLAZZ.f2() As System.Threading.Tasks.Task(Of Integer)"
    IL_003c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0041:  stloc.2
    IL_0042:  ldloca.s   V_2
    IL_0044:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0049:  brtrue.s   IL_0087
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4.0
    IL_004d:  dup
    IL_004e:  stloc.1
    IL_004f:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0054:  ldarg.0
    IL_0055:  ldloc.2
    IL_0056:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  ldarg.0
    IL_005c:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0061:  ldloca.s   V_2
    IL_0063:  ldarg.0
    IL_0064:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)"
    IL_0069:  leave.s    IL_00e4
    IL_006b:  ldarg.0
    IL_006c:  ldc.i4.m1
    IL_006d:  dup
    IL_006e:  stloc.1
    IL_006f:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0074:  ldarg.0
    IL_0075:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007a:  stloc.2
    IL_007b:  ldarg.0
    IL_007c:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0081:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0087:  ldloca.s   V_2
    IL_0089:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_008e:  ldloca.s   V_2
    IL_0090:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0096:  ldarg.0
    IL_0097:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_009c:  add.ovf
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00a3:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_p As Integer"
    IL_00a8:  add.ovf
    IL_00a9:  stloc.0
    IL_00aa:  leave.s    IL_00ce
  }
  catch System.Exception
  {
    IL_00ac:  dup
    IL_00ad:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b2:  stloc.3
    IL_00b3:  ldarg.0
    IL_00b4:  ldc.i4.s   -2
    IL_00b6:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_00bb:  ldarg.0
    IL_00bc:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00c1:  ldloc.3
    IL_00c2:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00c7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00cc:  leave.s    IL_00e4
  }
  IL_00ce:  ldarg.0
  IL_00cf:  ldc.i4.s   -2
  IL_00d1:  dup
  IL_00d2:  stloc.1
  IL_00d3:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_00d8:  ldarg.0
  IL_00d9:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00de:  ldloc.0
  IL_00df:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00e4:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_TaskOfT_Lambda_6_D()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Console.Write((New CLAZZ()).F(1000).Result.ToString + " ")
        Console.Write("X2 ")
    End Sub
End Module

Class CLAZZ
    Public FX As Integer = 1

    Public Async Function F(p As Integer) As Task(Of Integer)
        Dim outer As Integer = 100
        Console.Write("0 ")
        Dim a = Async Function()
                    Dim result = outer + Me.FX
                    Dim x = Await f2()
                    Return x + result + p
                End Function
        Console.Write("1 ")
        Return Await a()
    End Function

    Async Function f2() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 10
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, options:=TestOptions.ReleaseDebugExe, expectedOutput:="X1 0 1 2 3 1111 X2")
            c.VerifyIL("CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext",
            <![CDATA[
{
  // Code size      243 (0xf3)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                Integer V_3, //x
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_4,
                Integer V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0070
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0011:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_outer As Integer"
    IL_0016:  ldarg.0
    IL_0017:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_001c:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0021:  ldfld      "CLAZZ.FX As Integer"
    IL_0026:  add.ovf
    IL_0027:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_002c:  ldarg.0
    IL_002d:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_0032:  ldfld      "CLAZZ._Closure$__2-0.$VB$Me As CLAZZ"
    IL_0037:  call       "Function CLAZZ.f2() As System.Threading.Tasks.Task(Of Integer)"
    IL_003c:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0041:  stloc.s    V_4
    IL_0043:  ldloca.s   V_4
    IL_0045:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_004a:  brtrue.s   IL_008d
    IL_004c:  ldarg.0
    IL_004d:  ldc.i4.0
    IL_004e:  dup
    IL_004f:  stloc.1
    IL_0050:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0055:  ldarg.0
    IL_0056:  ldloc.s    V_4
    IL_0058:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005d:  ldarg.0
    IL_005e:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0063:  ldloca.s   V_4
    IL_0065:  ldarg.0
    IL_0066:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0)"
    IL_006b:  leave      IL_00f2
    IL_0070:  ldarg.0
    IL_0071:  ldc.i4.m1
    IL_0072:  dup
    IL_0073:  stloc.1
    IL_0074:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0079:  ldarg.0
    IL_007a:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_007f:  stloc.s    V_4
    IL_0081:  ldarg.0
    IL_0082:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0087:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_008d:  ldloca.s   V_4
    IL_008f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0094:  stloc.s    V_5
    IL_0096:  ldloca.s   V_4
    IL_0098:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009e:  ldloc.s    V_5
    IL_00a0:  stloc.3
    IL_00a1:  ldloc.3
    IL_00a2:  ldarg.0
    IL_00a3:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As Integer"
    IL_00a8:  add.ovf
    IL_00a9:  ldarg.0
    IL_00aa:  ldfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As CLAZZ._Closure$__2-0"
    IL_00af:  ldfld      "CLAZZ._Closure$__2-0.$VB$Local_p As Integer"
    IL_00b4:  add.ovf
    IL_00b5:  stloc.0
    IL_00b6:  leave.s    IL_00dc
  }
  catch System.Exception
  {
    IL_00b8:  dup
    IL_00b9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00be:  stloc.s    V_6
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.s   -2
    IL_00c3:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
    IL_00c8:  ldarg.0
    IL_00c9:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00ce:  ldloc.s    V_6
    IL_00d0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00d5:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00da:  leave.s    IL_00f2
  }
  IL_00dc:  ldarg.0
  IL_00dd:  ldc.i4.s   -2
  IL_00df:  dup
  IL_00e0:  stloc.1
  IL_00e1:  stfld      "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$State As Integer"
  IL_00e6:  ldarg.0
  IL_00e7:  ldflda     "CLAZZ._Closure$__2-0.VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00ec:  ldloc.0
  IL_00ed:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00f2:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_Finalizer()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(f().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function f() As Task(Of Integer)
        Try
            Console.Write("2 ")
            Await Task.Yield
            Console.Write("3 ")
            Return 123
        Finally
            Console.Write("4 ")
        End Try
        Return -321
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 123 1")

            c.VerifyIL("Form1.VB$StateMachine_1_f.MoveNext", <![CDATA[
{
  // Code size      236 (0xec)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_000e
    IL_000a:  ldloc.1
    IL_000b:  ldc.i4.1
    IL_000c:  pop
    IL_000d:  pop
    IL_000e:  nop
    .try
    {
      IL_000f:  ldloc.1
      IL_0010:  brfalse.s  IL_0068
      IL_0012:  ldloc.1
      IL_0013:  ldc.i4.1
      IL_0014:  bne.un.s   IL_0024
      IL_0016:  ldarg.0
      IL_0017:  ldc.i4.m1
      IL_0018:  dup
      IL_0019:  stloc.1
      IL_001a:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
      IL_001f:  leave      IL_00eb
      IL_0024:  ldstr      "2 "
      IL_0029:  call       "Sub System.Console.Write(String)"
      IL_002e:  call       "Function System.Threading.Tasks.Task.Yield() As System.Runtime.CompilerServices.YieldAwaitable"
      IL_0033:  stloc.3
      IL_0034:  ldloca.s   V_3
      IL_0036:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter() As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_003b:  stloc.2
      IL_003c:  ldloca.s   V_2
      IL_003e:  call       "Function System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.get_IsCompleted() As Boolean"
      IL_0043:  brtrue.s   IL_0084
      IL_0045:  ldarg.0
      IL_0046:  ldc.i4.0
      IL_0047:  dup
      IL_0048:  stloc.1
      IL_0049:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
      IL_004e:  ldarg.0
      IL_004f:  ldloc.2
      IL_0050:  stfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_0055:  ldarg.0
      IL_0056:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
      IL_005b:  ldloca.s   V_2
      IL_005d:  ldarg.0
      IL_005e:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Form1.VB$StateMachine_1_f)(ByRef System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ByRef Form1.VB$StateMachine_1_f)"
      IL_0063:  leave      IL_00eb
      IL_0068:  ldarg.0
      IL_0069:  ldc.i4.m1
      IL_006a:  dup
      IL_006b:  stloc.1
      IL_006c:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
      IL_0071:  ldarg.0
      IL_0072:  ldfld      "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_0077:  stloc.2
      IL_0078:  ldarg.0
      IL_0079:  ldflda     "Form1.VB$StateMachine_1_f.$A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_007e:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_0084:  ldloca.s   V_2
      IL_0086:  call       "Sub System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()"
      IL_008b:  ldloca.s   V_2
      IL_008d:  initobj    "System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter"
      IL_0093:  ldstr      "3 "
      IL_0098:  call       "Sub System.Console.Write(String)"
      IL_009d:  ldc.i4.s   123
      IL_009f:  stloc.0
      IL_00a0:  leave.s    IL_00d5
    }
    finally
    {
      IL_00a2:  ldloc.1
      IL_00a3:  ldc.i4.0
      IL_00a4:  bge.s      IL_00b0
      IL_00a6:  ldstr      "4 "
      IL_00ab:  call       "Sub System.Console.Write(String)"
      IL_00b0:  endfinally
    }
  }
  catch System.Exception
  {
    IL_00b1:  dup
    IL_00b2:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00b7:  stloc.s    V_4
    IL_00b9:  ldarg.0
    IL_00ba:  ldc.i4.s   -2
    IL_00bc:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
    IL_00c1:  ldarg.0
    IL_00c2:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00c7:  ldloc.s    V_4
    IL_00c9:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00ce:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d3:  leave.s    IL_00eb
  }
  IL_00d5:  ldarg.0
  IL_00d6:  ldc.i4.s   -2
  IL_00d8:  dup
  IL_00d9:  stloc.1
  IL_00da:  stfld      "Form1.VB$StateMachine_1_f.$State As Integer"
  IL_00df:  ldarg.0
  IL_00e0:  ldflda     "Form1.VB$StateMachine_1_f.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00e5:  ldloc.0
  IL_00e6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00eb:  ret
}
]]>)
        End Sub

        <Fact, WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        Public Sub Simple_LateBinding_1()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Function GetAwaiter() As Object
        Return Nothing
    End Function

End Class

Module Program

    Async Sub Test2()
        Dim o As Object = New MyTask(Of Integer)
        Dim x = Await o
        Await o
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.DebugExe, useLatestFramework:=True)
            c.VerifyIL("Program.VB$StateMachine_0_Test2.MoveNext",
            <![CDATA[
{
  // Code size      485 (0x1e5)
  .maxstack  8
  .locals init (Integer V_0,
                Object V_1,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_2,
                System.Runtime.CompilerServices.INotifyCompletion V_3,
                Program.VB$StateMachine_0_Test2 V_4,
                Object V_5,
                System.Runtime.CompilerServices.ICriticalNotifyCompletion V_6,
                System.Runtime.CompilerServices.INotifyCompletion V_7,
                System.Exception V_8)
 ~IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
   ~IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  br.s       IL_000c
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  beq.s      IL_0017
    IL_0010:  br.s       IL_001c
    IL_0012:  br         IL_00af
    IL_0017:  br         IL_0174
   -IL_001c:  nop
   -IL_001d:  ldarg.0
    IL_001e:  newobj     "Sub MyTask(Of Integer)..ctor()"
    IL_0023:  stfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_o$0 As Object"
   -IL_0028:  ldarg.0
    IL_0029:  ldfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_o$0 As Object"
    IL_002e:  ldnull
    IL_002f:  ldstr      "GetAwaiter"
    IL_0034:  ldc.i4.0
    IL_0035:  newarr     "Object"
    IL_003a:  ldnull
    IL_003b:  ldnull
    IL_003c:  ldnull
    IL_003d:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
    IL_0042:  stloc.1
   ~IL_0043:  ldloc.1
    IL_0044:  ldnull
    IL_0045:  ldstr      "IsCompleted"
    IL_004a:  ldc.i4.0
    IL_004b:  newarr     "Object"
    IL_0050:  ldnull
    IL_0051:  ldnull
    IL_0052:  ldnull
    IL_0053:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
    IL_0058:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
    IL_005d:  brfalse.s  IL_0061
    IL_005f:  br.s       IL_00c6
    IL_0061:  ldarg.0
    IL_0062:  ldc.i4.0
    IL_0063:  dup
    IL_0064:  stloc.0
    IL_0065:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
   <IL_006a:  ldarg.0
    IL_006b:  ldloc.1
    IL_006c:  stfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_0071:  ldloc.1
    IL_0072:  isinst     "System.Runtime.CompilerServices.ICriticalNotifyCompletion"
    IL_0077:  stloc.2
    IL_0078:  ldloc.2
    IL_0079:  brfalse.s  IL_0090
    IL_007b:  ldarg.0
    IL_007c:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_0081:  ldloca.s   V_2
    IL_0083:  ldarg.0
    IL_0084:  stloc.s    V_4
    IL_0086:  ldloca.s   V_4
    IL_0088:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.ICriticalNotifyCompletion, Program.VB$StateMachine_0_Test2)(ByRef System.Runtime.CompilerServices.ICriticalNotifyCompletion, ByRef Program.VB$StateMachine_0_Test2)"
    IL_008d:  nop
    IL_008e:  br.s       IL_00aa
    IL_0090:  ldloc.1
    IL_0091:  castclass  "System.Runtime.CompilerServices.INotifyCompletion"
    IL_0096:  stloc.3
    IL_0097:  ldarg.0
    IL_0098:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_009d:  ldloca.s   V_3
    IL_009f:  ldarg.0
    IL_00a0:  stloc.s    V_4
    IL_00a2:  ldloca.s   V_4
    IL_00a4:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted(Of System.Runtime.CompilerServices.INotifyCompletion, Program.VB$StateMachine_0_Test2)(ByRef System.Runtime.CompilerServices.INotifyCompletion, ByRef Program.VB$StateMachine_0_Test2)"
    IL_00a9:  nop
    IL_00aa:  leave      IL_01e4
   >IL_00af:  ldarg.0
    IL_00b0:  ldc.i4.m1
    IL_00b1:  dup
    IL_00b2:  stloc.0
    IL_00b3:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_00b8:  ldarg.0
    IL_00b9:  ldfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_00be:  stloc.1
    IL_00bf:  ldarg.0
    IL_00c0:  ldnull
    IL_00c1:  stfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.1
    IL_00c8:  ldnull
    IL_00c9:  ldstr      "GetResult"
    IL_00ce:  ldc.i4.0
    IL_00cf:  newarr     "Object"
    IL_00d4:  ldnull
    IL_00d5:  ldnull
    IL_00d6:  ldnull
    IL_00d7:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
    IL_00dc:  ldnull
    IL_00dd:  stloc.1
    IL_00de:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
    IL_00e3:  stfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_x$1 As Object"
   -IL_00e8:  ldarg.0
    IL_00e9:  ldfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_o$0 As Object"
    IL_00ee:  ldnull
    IL_00ef:  ldstr      "GetAwaiter"
    IL_00f4:  ldc.i4.0
    IL_00f5:  newarr     "Object"
    IL_00fa:  ldnull
    IL_00fb:  ldnull
    IL_00fc:  ldnull
    IL_00fd:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
    IL_0102:  stloc.s    V_5
   ~IL_0104:  ldloc.s    V_5
    IL_0106:  ldnull
    IL_0107:  ldstr      "IsCompleted"
    IL_010c:  ldc.i4.0
    IL_010d:  newarr     "Object"
    IL_0112:  ldnull
    IL_0113:  ldnull
    IL_0114:  ldnull
    IL_0115:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(Object, System.Type, String, Object(), String(), System.Type(), Boolean()) As Object"
    IL_011a:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(Object) As Boolean"
    IL_011f:  brfalse.s  IL_0123
    IL_0121:  br.s       IL_018c
    IL_0123:  ldarg.0
    IL_0124:  ldc.i4.1
    IL_0125:  dup
    IL_0126:  stloc.0
    IL_0127:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
   <IL_012c:  ldarg.0
    IL_012d:  ldloc.s    V_5
    IL_012f:  stfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_0134:  ldloc.s    V_5
    IL_0136:  isinst     "System.Runtime.CompilerServices.ICriticalNotifyCompletion"
    IL_013b:  stloc.s    V_6
    IL_013d:  ldloc.s    V_6
    IL_013f:  brfalse.s  IL_0156
    IL_0141:  ldarg.0
    IL_0142:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_0147:  ldloca.s   V_6
    IL_0149:  ldarg.0
    IL_014a:  stloc.s    V_4
    IL_014c:  ldloca.s   V_4
    IL_014e:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.ICriticalNotifyCompletion, Program.VB$StateMachine_0_Test2)(ByRef System.Runtime.CompilerServices.ICriticalNotifyCompletion, ByRef Program.VB$StateMachine_0_Test2)"
    IL_0153:  nop
    IL_0154:  br.s       IL_0172
    IL_0156:  ldloc.s    V_5
    IL_0158:  castclass  "System.Runtime.CompilerServices.INotifyCompletion"
    IL_015d:  stloc.s    V_7
    IL_015f:  ldarg.0
    IL_0160:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_0165:  ldloca.s   V_7
    IL_0167:  ldarg.0
    IL_0168:  stloc.s    V_4
    IL_016a:  ldloca.s   V_4
    IL_016c:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted(Of System.Runtime.CompilerServices.INotifyCompletion, Program.VB$StateMachine_0_Test2)(ByRef System.Runtime.CompilerServices.INotifyCompletion, ByRef Program.VB$StateMachine_0_Test2)"
    IL_0171:  nop
    IL_0172:  leave.s    IL_01e4
   >IL_0174:  ldarg.0
    IL_0175:  ldc.i4.m1
    IL_0176:  dup
    IL_0177:  stloc.0
    IL_0178:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_017d:  ldarg.0
    IL_017e:  ldfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_0183:  stloc.s    V_5
    IL_0185:  ldarg.0
    IL_0186:  ldnull
    IL_0187:  stfld      "Program.VB$StateMachine_0_Test2.$A0 As Object"
    IL_018c:  ldloc.s    V_5
    IL_018e:  ldnull
    IL_018f:  ldstr      "GetResult"
    IL_0194:  ldc.i4.0
    IL_0195:  newarr     "Object"
    IL_019a:  ldnull
    IL_019b:  ldnull
    IL_019c:  ldnull
    IL_019d:  ldc.i4.1
    IL_019e:  call       "Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Object, System.Type, String, Object(), String(), System.Type(), Boolean(), Boolean) As Object"
    IL_01a3:  pop
    IL_01a4:  ldnull
    IL_01a5:  stloc.s    V_5
   -IL_01a7:  leave.s    IL_01ce
  }
  catch System.Exception
  {
  ~$IL_01a9:  dup
    IL_01aa:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_01af:  stloc.s    V_8
   ~IL_01b1:  ldarg.0
    IL_01b2:  ldc.i4.s   -2
    IL_01b4:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_01b9:  ldarg.0
    IL_01ba:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_01bf:  ldloc.s    V_8
    IL_01c1:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_01c6:  nop
    IL_01c7:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_01cc:  leave.s    IL_01e4
  }
 -IL_01ce:  ldarg.0
  IL_01cf:  ldc.i4.s   -2
  IL_01d1:  dup
  IL_01d2:  stloc.0
  IL_01d3:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
 ~IL_01d8:  ldarg.0
  IL_01d9:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_01de:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_01e3:  nop
  IL_01e4:  ret
}
]]>,
            sequencePoints:="Program+VB$StateMachine_0_Test2.MoveNext")
        End Sub

        <Fact, WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        Public Sub Simple_LateBinding_2()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Class MyTask(Of T)

    Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return Nothing
    End Function

End Class

Structure MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Friend m_Task As MyTask(Of T)
    ReadOnly Property IsCompleted As Boolean
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Sub OnCompleted(r As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub

    Function GetResult() As Object
        Throw New NotImplementedException()
    End Function
End Structure

Module Program

    Async Sub Test2()
        Dim o As New MyTask(Of Integer)
        Dim x As Integer = Await o
        System.Console.WriteLine(x)
    End Sub

    Sub Main()
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.DebugExe, useLatestFramework:=True)

            c.VerifyIL("Program.VB$StateMachine_0_Test2.MoveNext",
            <![CDATA[
{
  // Code size      223 (0xdf)
  .maxstack  3
  .locals init (Integer V_0,
                MyTaskAwaiter(Of Integer) V_1,
                Program.VB$StateMachine_0_Test2 V_2,
                Object V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_000c
    IL_000a:  br.s       IL_000e
    IL_000c:  br.s       IL_0056
    IL_000e:  nop
    IL_000f:  ldarg.0
    IL_0010:  newobj     "Sub MyTask(Of Integer)..ctor()"
    IL_0015:  stfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_o$0 As MyTask(Of Integer)"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_o$0 As MyTask(Of Integer)"
    IL_0020:  callvirt   "Function MyTask(Of Integer).GetAwaiter() As MyTaskAwaiter(Of Integer)"
    IL_0025:  stloc.1
    IL_0026:  ldloca.s   V_1
    IL_0028:  call       "Function MyTaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_002d:  brtrue.s   IL_0074
    IL_002f:  ldarg.0
    IL_0030:  ldc.i4.0
    IL_0031:  dup
    IL_0032:  stloc.0
    IL_0033:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_0038:  ldarg.0
    IL_0039:  ldloc.1
    IL_003a:  stfld      "Program.VB$StateMachine_0_Test2.$A0 As MyTaskAwaiter(Of Integer)"
    IL_003f:  ldarg.0
    IL_0040:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_0045:  ldloca.s   V_1
    IL_0047:  ldarg.0
    IL_0048:  stloc.2
    IL_0049:  ldloca.s   V_2
    IL_004b:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted(Of MyTaskAwaiter(Of Integer), Program.VB$StateMachine_0_Test2)(ByRef MyTaskAwaiter(Of Integer), ByRef Program.VB$StateMachine_0_Test2)"
    IL_0050:  nop
    IL_0051:  leave      IL_00de
    IL_0056:  ldarg.0
    IL_0057:  ldc.i4.m1
    IL_0058:  dup
    IL_0059:  stloc.0
    IL_005a:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_005f:  ldarg.0
    IL_0060:  ldfld      "Program.VB$StateMachine_0_Test2.$A0 As MyTaskAwaiter(Of Integer)"
    IL_0065:  stloc.1
    IL_0066:  ldarg.0
    IL_0067:  ldflda     "Program.VB$StateMachine_0_Test2.$A0 As MyTaskAwaiter(Of Integer)"
    IL_006c:  initobj    "MyTaskAwaiter(Of Integer)"
    IL_0072:  br.s       IL_0074
    IL_0074:  ldarg.0
    IL_0075:  ldloca.s   V_1
    IL_0077:  call       "Function MyTaskAwaiter(Of Integer).GetResult() As Object"
    IL_007c:  stloc.3
    IL_007d:  ldloca.s   V_1
    IL_007f:  initobj    "MyTaskAwaiter(Of Integer)"
    IL_0085:  ldloc.3
    IL_0086:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
    IL_008b:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
    IL_0090:  stfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_x$1 As Integer"
    IL_0095:  ldarg.0
    IL_0096:  ldfld      "Program.VB$StateMachine_0_Test2.$VB$ResumableLocal_x$1 As Integer"
    IL_009b:  call       "Sub System.Console.WriteLine(Integer)"
    IL_00a0:  nop
    IL_00a1:  leave.s    IL_00c8
  }
  catch System.Exception
  {
    IL_00a3:  dup
    IL_00a4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00a9:  stloc.s    V_4
    IL_00ab:  ldarg.0
    IL_00ac:  ldc.i4.s   -2
    IL_00ae:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
    IL_00b3:  ldarg.0
    IL_00b4:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
    IL_00b9:  ldloc.s    V_4
    IL_00bb:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)"
    IL_00c0:  nop
    IL_00c1:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00c6:  leave.s    IL_00de
  }
  IL_00c8:  ldarg.0
  IL_00c9:  ldc.i4.s   -2
  IL_00cb:  dup
  IL_00cc:  stloc.0
  IL_00cd:  stfld      "Program.VB$StateMachine_0_Test2.$State As Integer"
  IL_00d2:  ldarg.0
  IL_00d3:  ldflda     "Program.VB$StateMachine_0_Test2.$Builder As System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
  IL_00d8:  call       "Sub System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()"
  IL_00dd:  nop
  IL_00de:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub Simple_Generics()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Call (New BASE(Of Object).CLAZZ(Of String)()).F(Of Integer)(1).Wait(60000)
        Console.Write("X2 ")
    End Sub
End Module

Public Class BASE(Of T)
    Public Class CLAZZ(Of U As T)
        Public FX As T

        Public Async Function F(Of V As Structure)(p As V) As Task(Of Integer)
            Dim outer As U = Nothing
            Console.Write("0 ")
            Dim a = Async Function()
                        Dim result As String = outer.ToString &amp; Me.FX.ToString
                        Dim x = Await f2()
                        Return result &amp; p.ToString
                    End Function
            Console.Write("1 ")
            Return Await a()
        End Function

        Async Function f2() As Task(Of Integer)
            Console.Write("2 ")
            Await Task.Yield
            Console.Write("3 ")
            Return 123
        End Function
    End Class
End Class
    </file>
</compilation>, useLatestFramework:=True)

            c.VerifyIL("BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).MoveNext",
            <![CDATA[
{
  // Code size      242 (0xf2)
  .maxstack  3
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of String) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0086
    IL_000a:  newobj     "Sub BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of SM$V)..ctor()"
    IL_000f:  dup
    IL_0010:  ldarg.0
    IL_0011:  ldfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$VB$Me As BASE(Of T).CLAZZ(Of U)"
    IL_0016:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of SM$V).$VB$Me As BASE(Of T).CLAZZ(Of U)"
    IL_001b:  dup
    IL_001c:  ldarg.0
    IL_001d:  ldfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$VB$Local_p As SM$V"
    IL_0022:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of SM$V).$VB$Local_p As SM$V"
    IL_0027:  dup
    IL_0028:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of SM$V).$VB$Local_outer As U"
    IL_002d:  initobj    "U"
    IL_0033:  ldstr      "0 "
    IL_0038:  call       "Sub System.Console.Write(String)"
    IL_003d:  ldftn      "Function BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of SM$V)._Lambda$__0() As System.Threading.Tasks.Task(Of String)"
    IL_0043:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Threading.Tasks.Task(Of String))..ctor(Object, System.IntPtr)"
    IL_0048:  ldstr      "1 "
    IL_004d:  call       "Sub System.Console.Write(String)"
    IL_0052:  callvirt   "Function VB$AnonymousDelegate_0(Of System.Threading.Tasks.Task(Of String)).Invoke() As System.Threading.Tasks.Task(Of String)"
    IL_0057:  callvirt   "Function System.Threading.Tasks.Task(Of String).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_005c:  stloc.2
    IL_005d:  ldloca.s   V_2
    IL_005f:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of String).get_IsCompleted() As Boolean"
    IL_0064:  brtrue.s   IL_00a2
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.0
    IL_0068:  dup
    IL_0069:  stloc.1
    IL_006a:  stfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$State As Integer"
    IL_006f:  ldarg.0
    IL_0070:  ldloc.2
    IL_0071:  stfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_0076:  ldarg.0
    IL_0077:  ldflda     "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_007c:  ldloca.s   V_2
    IL_007e:  ldarg.0
    IL_007f:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of String), BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V))(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of String), ByRef BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V))"
    IL_0084:  leave.s    IL_00f1
    IL_0086:  ldarg.0
    IL_0087:  ldc.i4.m1
    IL_0088:  dup
    IL_0089:  stloc.1
    IL_008a:  stfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$State As Integer"
    IL_008f:  ldarg.0
    IL_0090:  ldfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_0095:  stloc.2
    IL_0096:  ldarg.0
    IL_0097:  ldflda     "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_009c:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_00a2:  ldloca.s   V_2
    IL_00a4:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of String).GetResult() As String"
    IL_00a9:  ldloca.s   V_2
    IL_00ab:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of String)"
    IL_00b1:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(String) As Integer"
    IL_00b6:  stloc.0
    IL_00b7:  leave.s    IL_00db
  }
  catch System.Exception
  {
    IL_00b9:  dup
    IL_00ba:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00bf:  stloc.3
    IL_00c0:  ldarg.0
    IL_00c1:  ldc.i4.s   -2
    IL_00c3:  stfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$State As Integer"
    IL_00c8:  ldarg.0
    IL_00c9:  ldflda     "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00ce:  ldloc.3
    IL_00cf:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_00d4:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00d9:  leave.s    IL_00f1
  }
  IL_00db:  ldarg.0
  IL_00dc:  ldc.i4.s   -2
  IL_00de:  dup
  IL_00df:  stloc.1
  IL_00e0:  stfld      "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$State As Integer"
  IL_00e5:  ldarg.0
  IL_00e6:  ldflda     "BASE(Of T).CLAZZ(Of U).VB$StateMachine_2_F(Of SM$V).$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_00eb:  ldloc.0
  IL_00ec:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_00f1:  ret
}
]]>)
            c.VerifyIL("BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.MoveNext", <![CDATA[
{
  // Code size      273 (0x111)
  .maxstack  3
  .locals init (String V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0088
    IL_000a:  ldarg.0
    IL_000b:  ldarg.0
    IL_000c:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0)"
    IL_0011:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).$VB$Local_outer As U"
    IL_0016:  constrained. "U"
    IL_001c:  callvirt   "Function Object.ToString() As String"
    IL_0021:  ldarg.0
    IL_0022:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0)"
    IL_0027:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).$VB$Me As BASE(Of T).CLAZZ(Of U)"
    IL_002c:  ldflda     "BASE(Of T).CLAZZ(Of U).FX As T"
    IL_0031:  constrained. "T"
    IL_0037:  callvirt   "Function Object.ToString() As String"
    IL_003c:  call       "Function String.Concat(String, String) As String"
    IL_0041:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As String"
    IL_0046:  ldarg.0
    IL_0047:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0)"
    IL_004c:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).$VB$Me As BASE(Of T).CLAZZ(Of U)"
    IL_0051:  call       "Function BASE(Of T).CLAZZ(Of U).f2() As System.Threading.Tasks.Task(Of Integer)"
    IL_0056:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_005b:  stloc.2
    IL_005c:  ldloca.s   V_2
    IL_005e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_0063:  brtrue.s   IL_00a4
    IL_0065:  ldarg.0
    IL_0066:  ldc.i4.0
    IL_0067:  dup
    IL_0068:  stloc.1
    IL_0069:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$State As Integer"
    IL_006e:  ldarg.0
    IL_006f:  ldloc.2
    IL_0070:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0075:  ldarg.0
    IL_0076:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_007b:  ldloca.s   V_2
    IL_007d:  ldarg.0
    IL_007e:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0)"
    IL_0083:  leave      IL_0110
    IL_0088:  ldarg.0
    IL_0089:  ldc.i4.m1
    IL_008a:  dup
    IL_008b:  stloc.1
    IL_008c:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$State As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0097:  stloc.2
    IL_0098:  ldarg.0
    IL_0099:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_009e:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00a4:  ldloca.s   V_2
    IL_00a6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_00ab:  ldloca.s   V_2
    IL_00ad:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b3:  pop
    IL_00b4:  ldarg.0
    IL_00b5:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$ResumableLocal_result$0 As String"
    IL_00ba:  ldarg.0
    IL_00bb:  ldfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0)"
    IL_00c0:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).$VB$Local_p As $CLS0"
    IL_00c5:  constrained. "$CLS0"
    IL_00cb:  callvirt   "Function System.ValueType.ToString() As String"
    IL_00d0:  call       "Function String.Concat(String, String) As String"
    IL_00d5:  stloc.0
    IL_00d6:  leave.s    IL_00fa
  }
  catch System.Exception
  {
    IL_00d8:  dup
    IL_00d9:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_00de:  stloc.3
    IL_00df:  ldarg.0
    IL_00e0:  ldc.i4.s   -2
    IL_00e2:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$State As Integer"
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
    IL_00ed:  ldloc.3
    IL_00ee:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetException(System.Exception)"
    IL_00f3:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_00f8:  leave.s    IL_0110
  }
  IL_00fa:  ldarg.0
  IL_00fb:  ldc.i4.s   -2
  IL_00fd:  dup
  IL_00fe:  stloc.1
  IL_00ff:  stfld      "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$State As Integer"
  IL_0104:  ldarg.0
  IL_0105:  ldflda     "BASE(Of T).CLAZZ(Of U)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String)"
  IL_010a:  ldloc.0
  IL_010b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of String).SetResult(String)"
  IL_0110:  ret
}
]]>)
        End Sub

        <WorkItem(553894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/553894")>
        <Fact()>
        Public Sub Simple_TaskOfT_EmitMetadataOnly()
            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("X1 ")
        Call (New CLAZZ()).F()
        Console.Write("X2 ")
    End Sub
End Module

Class CLAZZ
    Public FX As Integer

    Public Async Function F() As Task(Of Integer)
        Dim outer As Integer = 123
        Console.Write("0 ")
        Return Await f2()
        Console.Write("1 ")
    End Function

    Async Function f2() As Task(Of Integer)
        Console.Write("2 ")
        Await Task.Yield
        Console.Write("3 ")
        Return 123
    End Function
End Class
    </file>
</compilation>, references:=LatestVbReferences).VerifyDiagnostics()

            Using stream As New MemoryStream()
                Dim emitResult = compilation.Emit(stream, options:=New EmitOptions(metadataOnly:=True))
                ' This should not crash
            End Using
        End Sub

        <Fact()>
        Public Sub SpilledArrayAccessAndFieldAccess()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of Integer)
        Dim s(1, 1) As S
        s(0, 0).I = 1
        s(0, 1).I = 0
        s(1, 1).I = 10

        Console.Write("2 ")
        Return M(s(s(0, 0).I, s(0, 1).I + 1).I, Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 1
    End Function

    Public Function M(ByRef x As Integer, y As Integer) As Integer
        Console.Write("5 ")
        Return x + y
    End Function

    Public Structure S
        Public I As Integer
    End Structure
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 11 1")

            c.VerifyIL("Form1.VB$StateMachine_1_Test.MoveNext",
            <![CDATA[
{
  // Code size      369 (0x171)
  .maxstack  4
  .locals init (Integer V_0,
                Integer V_1,
                Form1.S(,) V_2, //s
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse    IL_00e0
    IL_000d:  ldc.i4.2
    IL_000e:  ldc.i4.2
    IL_000f:  newobj     "Form1.S(*,*)..ctor"
    IL_0014:  stloc.2
    IL_0015:  ldloc.2
    IL_0016:  ldc.i4.0
    IL_0017:  ldc.i4.0
    IL_0018:  call       "Form1.S(*,*).Address"
    IL_001d:  ldc.i4.1
    IL_001e:  stfld      "Form1.S.I As Integer"
    IL_0023:  ldloc.2
    IL_0024:  ldc.i4.0
    IL_0025:  ldc.i4.1
    IL_0026:  call       "Form1.S(*,*).Address"
    IL_002b:  ldc.i4.0
    IL_002c:  stfld      "Form1.S.I As Integer"
    IL_0031:  ldloc.2
    IL_0032:  ldc.i4.1
    IL_0033:  ldc.i4.1
    IL_0034:  call       "Form1.S(*,*).Address"
    IL_0039:  ldc.i4.s   10
    IL_003b:  stfld      "Form1.S.I As Integer"
    IL_0040:  ldstr      "2 "
    IL_0045:  call       "Sub System.Console.Write(String)"
    IL_004a:  ldarg.0
    IL_004b:  ldloc.2
    IL_004c:  stfld      "Form1.VB$StateMachine_1_Test.$U1 As Form1.S(,)"
    IL_0051:  ldarg.0
    IL_0052:  ldloc.2
    IL_0053:  ldc.i4.0
    IL_0054:  ldc.i4.0
    IL_0055:  call       "Form1.S(*,*).Address"
    IL_005a:  ldfld      "Form1.S.I As Integer"
    IL_005f:  stfld      "Form1.VB$StateMachine_1_Test.$U2 As Integer"
    IL_0064:  ldarg.0
    IL_0065:  ldloc.2
    IL_0066:  ldc.i4.0
    IL_0067:  ldc.i4.1
    IL_0068:  call       "Form1.S(*,*).Address"
    IL_006d:  ldfld      "Form1.S.I As Integer"
    IL_0072:  ldc.i4.1
    IL_0073:  add.ovf
    IL_0074:  stfld      "Form1.VB$StateMachine_1_Test.$U3 As Integer"
    IL_0079:  ldarg.0
    IL_007a:  ldfld      "Form1.VB$StateMachine_1_Test.$U1 As Form1.S(,)"
    IL_007f:  ldarg.0
    IL_0080:  ldfld      "Form1.VB$StateMachine_1_Test.$U2 As Integer"
    IL_0085:  ldarg.0
    IL_0086:  ldfld      "Form1.VB$StateMachine_1_Test.$U3 As Integer"
    IL_008b:  call       "Form1.S(*,*).Get"
    IL_0090:  pop
    IL_0091:  ldarg.0
    IL_0092:  ldfld      "Form1.VB$StateMachine_1_Test.$U1 As Form1.S(,)"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Form1.VB$StateMachine_1_Test.$U2 As Integer"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "Form1.VB$StateMachine_1_Test.$U3 As Integer"
    IL_00a3:  call       "Form1.S(*,*).Get"
    IL_00a8:  pop
    IL_00a9:  call       "Function Form1.F() As System.Threading.Tasks.Task(Of Integer)"
    IL_00ae:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00b3:  stloc.3
    IL_00b4:  ldloca.s   V_3
    IL_00b6:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00bb:  brtrue.s   IL_00fc
    IL_00bd:  ldarg.0
    IL_00be:  ldc.i4.0
    IL_00bf:  dup
    IL_00c0:  stloc.1
    IL_00c1:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_00c6:  ldarg.0
    IL_00c7:  ldloc.3
    IL_00c8:  stfld      "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00cd:  ldarg.0
    IL_00ce:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00d3:  ldloca.s   V_3
    IL_00d5:  ldarg.0
    IL_00d6:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Form1.VB$StateMachine_1_Test)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Form1.VB$StateMachine_1_Test)"
    IL_00db:  leave      IL_0170
    IL_00e0:  ldarg.0
    IL_00e1:  ldc.i4.m1
    IL_00e2:  dup
    IL_00e3:  stloc.1
    IL_00e4:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_00e9:  ldarg.0
    IL_00ea:  ldfld      "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00ef:  stloc.3
    IL_00f0:  ldarg.0
    IL_00f1:  ldflda     "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f6:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00fc:  ldarg.0
    IL_00fd:  ldfld      "Form1.VB$StateMachine_1_Test.$U1 As Form1.S(,)"
    IL_0102:  ldarg.0
    IL_0103:  ldfld      "Form1.VB$StateMachine_1_Test.$U2 As Integer"
    IL_0108:  ldarg.0
    IL_0109:  ldfld      "Form1.VB$StateMachine_1_Test.$U3 As Integer"
    IL_010e:  call       "Form1.S(*,*).Address"
    IL_0113:  ldflda     "Form1.S.I As Integer"
    IL_0118:  ldloca.s   V_3
    IL_011a:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_011f:  ldloca.s   V_3
    IL_0121:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0127:  call       "Function Form1.M(ByRef Integer, Integer) As Integer"
    IL_012c:  stloc.0
    IL_012d:  ldarg.0
    IL_012e:  ldnull
    IL_012f:  stfld      "Form1.VB$StateMachine_1_Test.$U1 As Form1.S(,)"
    IL_0134:  leave.s    IL_015a
  }
  catch System.Exception
  {
    IL_0136:  dup
    IL_0137:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_013c:  stloc.s    V_4
    IL_013e:  ldarg.0
    IL_013f:  ldc.i4.s   -2
    IL_0141:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_0146:  ldarg.0
    IL_0147:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_014c:  ldloc.s    V_4
    IL_014e:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_0153:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0158:  leave.s    IL_0170
  }
  IL_015a:  ldarg.0
  IL_015b:  ldc.i4.s   -2
  IL_015d:  dup
  IL_015e:  stloc.1
  IL_015f:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
  IL_0164:  ldarg.0
  IL_0165:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_016a:  ldloc.0
  IL_016b:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_0170:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub HoistedArrayAccessAndFieldAccess()
            Dim c = CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of Integer)
        Dim s(1, 1) As S
        s(0, 0).I = 1
        s(0, 1).I = 0
        s(1, 1).I = 10

        Console.Write("2 ")
        Return M(s(s(0, 0).I, s(0, 1).I + 1).I, Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ") 
        Return 1
    End Function

    Public Function M(ByRef x As Double, y As Integer) As Integer
        Console.Write("5 ")
        Return x + y
    End Function

    Public Structure S
        Public I As Integer
    End Structure
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 11 1")

            c.VerifyIL("Form1.VB$StateMachine_1_Test.MoveNext", <![CDATA[
{
  // Code size      393 (0x189)
  .maxstack  5
  .locals init (Integer V_0,
                Integer V_1,
                System.Runtime.CompilerServices.TaskAwaiter(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
  IL_0006:  stloc.1
  .try
  {
    IL_0007:  ldloc.1
    IL_0008:  brfalse    IL_00ea
    IL_000d:  ldarg.0
    IL_000e:  ldc.i4.2
    IL_000f:  ldc.i4.2
    IL_0010:  newobj     "Form1.S(*,*)..ctor"
    IL_0015:  stfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_001a:  ldarg.0
    IL_001b:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0020:  ldc.i4.0
    IL_0021:  ldc.i4.0
    IL_0022:  call       "Form1.S(*,*).Address"
    IL_0027:  ldc.i4.1
    IL_0028:  stfld      "Form1.S.I As Integer"
    IL_002d:  ldarg.0
    IL_002e:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0033:  ldc.i4.0
    IL_0034:  ldc.i4.1
    IL_0035:  call       "Form1.S(*,*).Address"
    IL_003a:  ldc.i4.0
    IL_003b:  stfld      "Form1.S.I As Integer"
    IL_0040:  ldarg.0
    IL_0041:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0046:  ldc.i4.1
    IL_0047:  ldc.i4.1
    IL_0048:  call       "Form1.S(*,*).Address"
    IL_004d:  ldc.i4.s   10
    IL_004f:  stfld      "Form1.S.I As Integer"
    IL_0054:  ldstr      "2 "
    IL_0059:  call       "Sub System.Console.Write(String)"
    IL_005e:  ldarg.0
    IL_005f:  ldarg.0
    IL_0060:  ldarg.0
    IL_0061:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0066:  ldc.i4.0
    IL_0067:  ldc.i4.0
    IL_0068:  call       "Form1.S(*,*).Address"
    IL_006d:  ldfld      "Form1.S.I As Integer"
    IL_0072:  stfld      "Form1.VB$StateMachine_1_Test.$V1 As Integer"
    IL_0077:  ldarg.0
    IL_0078:  ldarg.0
    IL_0079:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_007e:  ldc.i4.0
    IL_007f:  ldc.i4.1
    IL_0080:  call       "Form1.S(*,*).Address"
    IL_0085:  ldfld      "Form1.S.I As Integer"
    IL_008a:  ldc.i4.1
    IL_008b:  add.ovf
    IL_008c:  stfld      "Form1.VB$StateMachine_1_Test.$V2 As Integer"
    IL_0091:  ldarg.0
    IL_0092:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0097:  ldarg.0
    IL_0098:  ldfld      "Form1.VB$StateMachine_1_Test.$V1 As Integer"
    IL_009d:  ldarg.0
    IL_009e:  ldfld      "Form1.VB$StateMachine_1_Test.$V2 As Integer"
    IL_00a3:  call       "Form1.S(*,*).Address"
    IL_00a8:  ldfld      "Form1.S.I As Integer"
    IL_00ad:  conv.r8
    IL_00ae:  stfld      "Form1.VB$StateMachine_1_Test.$S1 As Double"
    IL_00b3:  call       "Function Form1.F() As System.Threading.Tasks.Task(Of Integer)"
    IL_00b8:  callvirt   "Function System.Threading.Tasks.Task(Of Integer).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00bd:  stloc.2
    IL_00be:  ldloca.s   V_2
    IL_00c0:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).get_IsCompleted() As Boolean"
    IL_00c5:  brtrue.s   IL_0106
    IL_00c7:  ldarg.0
    IL_00c8:  ldc.i4.0
    IL_00c9:  dup
    IL_00ca:  stloc.1
    IL_00cb:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_00d0:  ldarg.0
    IL_00d1:  ldloc.2
    IL_00d2:  stfld      "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00d7:  ldarg.0
    IL_00d8:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_00dd:  ldloca.s   V_2
    IL_00df:  ldarg.0
    IL_00e0:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Integer), Form1.VB$StateMachine_1_Test)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Integer), ByRef Form1.VB$StateMachine_1_Test)"
    IL_00e5:  leave      IL_0188
    IL_00ea:  ldarg.0
    IL_00eb:  ldc.i4.m1
    IL_00ec:  dup
    IL_00ed:  stloc.1
    IL_00ee:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_00f3:  ldarg.0
    IL_00f4:  ldfld      "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_00f9:  stloc.2
    IL_00fa:  ldarg.0
    IL_00fb:  ldflda     "Form1.VB$StateMachine_1_Test.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0100:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_0106:  ldarg.0
    IL_0107:  ldflda     "Form1.VB$StateMachine_1_Test.$S1 As Double"
    IL_010c:  ldloca.s   V_2
    IL_010e:  call       "Function System.Runtime.CompilerServices.TaskAwaiter(Of Integer).GetResult() As Integer"
    IL_0113:  ldloca.s   V_2
    IL_0115:  initobj    "System.Runtime.CompilerServices.TaskAwaiter(Of Integer)"
    IL_011b:  call       "Function Form1.M(ByRef Double, Integer) As Integer"
    IL_0120:  ldarg.0
    IL_0121:  ldfld      "Form1.VB$StateMachine_1_Test.$VB$ResumableLocal_s$0 As Form1.S(,)"
    IL_0126:  ldarg.0
    IL_0127:  ldfld      "Form1.VB$StateMachine_1_Test.$V1 As Integer"
    IL_012c:  ldarg.0
    IL_012d:  ldfld      "Form1.VB$StateMachine_1_Test.$V2 As Integer"
    IL_0132:  call       "Form1.S(*,*).Address"
    IL_0137:  ldarg.0
    IL_0138:  ldfld      "Form1.VB$StateMachine_1_Test.$S1 As Double"
    IL_013d:  call       "Function System.Math.Round(Double) As Double"
    IL_0142:  call       "Function System.Math.Round(Double) As Double"
    IL_0147:  conv.ovf.i4
    IL_0148:  stfld      "Form1.S.I As Integer"
    IL_014d:  stloc.0
    IL_014e:  leave.s    IL_0172
  }
  catch System.Exception
  {
    IL_0150:  dup
    IL_0151:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)"
    IL_0156:  stloc.3
    IL_0157:  ldarg.0
    IL_0158:  ldc.i4.s   -2
    IL_015a:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
    IL_015f:  ldarg.0
    IL_0160:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
    IL_0165:  ldloc.3
    IL_0166:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetException(System.Exception)"
    IL_016b:  call       "Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()"
    IL_0170:  leave.s    IL_0188
  }
  IL_0172:  ldarg.0
  IL_0173:  ldc.i4.s   -2
  IL_0175:  dup
  IL_0176:  stloc.1
  IL_0177:  stfld      "Form1.VB$StateMachine_1_Test.$State As Integer"
  IL_017c:  ldarg.0
  IL_017d:  ldflda     "Form1.VB$StateMachine_1_Test.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer)"
  IL_0182:  ldloc.0
  IL_0183:  call       "Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Integer).SetResult(Integer)"
  IL_0188:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CapturingMeOfStructureAsLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Structure Form1
    Shared Sub Main()
        Console.Write("0 ")
        Dim f As New Form1()
        f.FLD = 1
        Console.Write((f.Test().Result + f.FLD).ToString + " ")
        Console.Write("1 ")
    End Sub

    Public FLD As Integer

    Async Function Test() As Task(Of Integer)
        Console.Write("2 ")
        Dim result = M(Me, Await F()) + Me.FLD
        Me.FLD = 100
        Return result
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 1000
    End Function

    Public Function M(ByRef x As Form1, y As Integer) As Integer
        Console.Write("5 ")
        Dim result = x.FLD + y
        x.FLD = 10
        Return result
    End Function
End Structure
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 1003 1")
        End Sub

        <Fact()>
        Public Sub CapturingMeOfClassAsRValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class Form1
    Shared Sub Main()
        Console.Write("0 ")
        Dim f As New Form1()
        f.FLD = 1
        Console.Write((f.Test().Result + f.FLD).ToString + " ")
        Console.Write("1 ")
    End Sub

    Public FLD As Integer

    Async Function Test() As Task(Of Integer)
        Console.Write("2 ")
        Dim result = M(Me, Await F()) + Me.FLD
        Me.FLD = 100
        Return result
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 1000
    End Function

    Public Function M(ByRef x As Form1, y As Integer) As Integer
        Console.Write("5 ")
        Dim result = x.FLD + y
        x.FLD = 10
        Return result
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 1111 1")
        End Sub

        <Fact()>
        Public Sub MeMyClassMyBase()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class Form0
    Public FLD As Integer = 1

    Public Overridable Async Function F() As Task(Of Integer)
        Console.Write("0 ")
        Await Task.Yield
        Console.Write("1 ")
        Return 1000
    End Function
End Class

Class Form1
    Inherits Form0

    Shared Sub Main()
        Console.Write("2 ")
        Dim f As New Form1()
        f.FLD = 10000
        Console.Write((f.Test().Result + f.FLD).ToString + " ")
        Console.Write("3 ")
    End Sub

    Public Shadows FLD As Integer

    Async Function Test() As Task(Of Integer)
        Console.Write("4 ")
        Dim result = M(Me, Await MyClass.F()) + MyBase.FLD + M(Me, Await MyBase.F()) + MyClass.FLD
        Me.FLD = 100
        Return result
    End Function

    Public Overrides Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 100000
    End Function

    Public Function M(ByRef x As Form1, y As Integer) As Integer
        Console.Write("7 ")
        Dim result = x.FLD + y
        x.FLD = 10
        Return result
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="2 4 5 6 7 0 1 7 111121 3")
        End Sub

        <Fact()>
        Public Sub ArrayLengthAndInitialization()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of Integer)
        Return Await Reflect((Await Reflect(Await F())).Length)
    End Function

    Public Async Function F() As Task(Of Integer())
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return {1, 2, 3, 4, 5, 6, 7, 8, 9, 10}
    End Function

    Public Async Function Reflect(Of T)(p As T) As Task(Of T)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return p
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 4 5 6 5 6 10 1")
        End Sub

        <Fact()>
        Public Sub UnaryOperator()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of Integer)
        Return Await Reflect(-(Await Reflect(+Await F())))
    End Function

    Public Async Function F() As Task(Of S)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return New S() With {.F = 12345}
    End Function

    Public Async Function Reflect(Of T)(p As T) As Task(Of T)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return p
    End Function

    Structure S
        Public F As Integer
        Public Shared Operator +(s As S) As Integer
            Return s.F
        End Operator
    End Structure
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 4 5 6 5 6 -12345 1")
        End Sub

        <Fact()>
        Public Sub BinaryOperator()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of Integer)
        Return Await Reflect(1 + (Await Reflect(Await F() + 10000)))
    End Function

    Public Async Function F() As Task(Of S)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return New S() With {.F = 100}
    End Function

    Public Async Function Reflect(Of T)(p As T) As Task(Of T)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return p
    End Function

    Structure S
        Public F As Integer
        Public Shared Operator +(s As S, i As Integer) As Integer
            Return s.F + i
        End Operator
    End Structure
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 4 5 6 5 6 10101 1")
        End Sub

        <Fact()>
        Public Sub BinarySortCircuitOperator()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Async Function A(Of T)(b As T, s As String) As Task(Of T)
        Await Task.Yield
        Console.Write(s)
        Console.Write(" ")
        Return b
    End Function

    Async Function Test() As Task
        Do
        Loop Until Await A(False, "1") OrElse Await A(True, "2") OrElse Await A(False, "3")

        While Await A(True, "4") AndAlso Await A(False, "5") AndAlso Await A(True, "6")
        End While

        If If(Await A(False, "7"), Await A(False, "8"), Await A(True, "9")) Then
        End If

        Dim y = If(Await A(CType("", String), "10"), Await A("", "11"))
        Dim x = If(Await A(CType(Nothing, String), "12"), Await A("", "13"))
    End Function

    Sub Main()
        Test().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 4 5 7 9 10 12 13")
        End Sub

        <Fact()>
        Public Sub BinaryAndTernaryConditional()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of String)
        Return Await Reflect(
            If(Await F(), Await Reflect(10000)).ToString() +
            If(Await F() IsNot Nothing, Await Reflect(1), Await Reflect(2)).ToString())
    End Function

    Public Async Function F() As Task(Of Object)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 100
    End Function

    Public Async Function Reflect(Of T)(p As T) As Task(Of T)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return p
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 4 3 4 5 6 5 6 1001 1")
        End Sub

        <Fact()>
        Public Sub TypeOfExpression()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test().Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test() As Task(Of String)
        Return (Await Reflect(TypeOf (Await Reflect(Await F())) Is String)).ToString
    End Function

    Public Async Function F() As Task(Of Object)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return "STR"
    End Function

    Public Async Function Reflect(Of T)(p As T) As Task(Of T)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return p
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 4 5 6 5 6 True 1")
        End Sub

        <Fact()>
        Public Sub CaptureParameterSimple()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test(100).Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test(p As Integer) As Task(Of Integer)
        Console.Write("2 ")
        Return M(p, Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 10
    End Function

    Public Function M(ByRef x As Integer, y As Integer) As Integer
        Console.Write("5 ")
        Return x + y
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 110 1")
        End Sub

        <Fact()>
        Public Sub CaptureParameterInLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test(New S() With {.I = 100}).Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test(p As S) As Task(Of Integer)
        Console.Write("2 ")
        Return M(p.I, Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 10
    End Function

    Public Function M(ByRef x As Integer, y As Integer) As Integer
        Console.Write("5 ")
        Return x + y
    End Function

    Structure S
        Public I As Integer
    End Structure
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 110 1")
        End Sub

        <Fact()>
        Public Sub CaptureByRefLocalWithParameterAndFieldAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(Test(New S() With {.I = 100}).Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function Test(p As S) As Task(Of Integer)
        Console.Write("2 ")
        Return M(p.I, Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("3 ")
        Await Task.Yield
        Console.Write("4 ")
        Return 10
    End Function

    Public Function M(ByRef x As Double, y As Integer) As Integer
        Console.Write("5 ")
        Return x + y
    End Function

    Structure S
        Public I As Integer
    End Structure
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 3 4 5 110 1")
        End Sub

        <Fact()>
        Public Sub CaptureByRefLocalWithMeMyBaseMyClassAndArrayAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Class BASE
    Public FLD As Integer = 4

    Public Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 0
    End Function

    Public Function M(ByRef x As Double, y As Integer) As Integer
        Console.Write("7 ")
        Return x + y
    End Function
End Class

Class Form1
    Inherits BASE

    Public Shadows FLD As Integer

    Shared Sub Main()
        Console.Write("0 ")
        Console.Write((New Form1() With {.FLD = 1}).TestMe({770, 771, 772, 773, 774}).Result.ToString + " ")
        Console.Write((New Form1() With {.FLD = 2}).TestMyBase({770, 771, 772, 773, 774}).Result.ToString + " ")
        Console.Write((New Form1() With {.FLD = 3}).TestMyClass({770, 771, 772, 773, 774}).Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function TestMe(p As Integer()) As Task(Of Integer)
        Console.Write("2 ")
        Return M(p(Me.FLD), Await F())
    End Function

    Async Function TestMyBase(p As Integer()) As Task(Of Integer)
        Console.Write("3 ")
        Return M(p(MyBase.FLD), Await F())
    End Function

    Async Function TestMyClass(p As Integer()) As Task(Of Integer)
        Console.Write("4 ")
        Return M(p(MyClass.FLD), Await F())
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 5 6 7 771 3 5 6 7 774 4 5 6 7 773 1")
        End Sub

        <Fact()>
        Public Sub CaptureByRefLocalWithLocalConstAndRValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Console.Write("0 ")
        Console.Write(TestLocal({1111, 1, 10, 100, 1000}).Result.ToString + " ")
        Console.Write(TestRValue({1111, 1, 10, 100, 1000}).Result.ToString + " ")
        Console.Write(TestConst({1111, 1, 10, 100, 1000}).Result.ToString + " ")
        Console.Write("1 ")
    End Sub

    Async Function TestLocal(p As Integer()) As Task(Of Integer)
        Console.Write("2 ")
        Dim loc As Integer = 1
        Return M(p(loc), Await F())
    End Function

    Async Function TestRValue(p As Integer()) As Task(Of Integer)
        Console.Write("3 ")
        Dim loc As Integer = 1
        Return M(p(1 + loc), Await F())
    End Function

    Async Function TestConst(p As Integer()) As Task(Of Integer)
        Console.Write("4 ")
        Return M(p(3), Await F())
    End Function

    Public Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 10000
    End Function

    Public Function M(ByRef x As Double, y As Integer) As Integer
        Console.Write("7 ")
        Return x + y
    End Function
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 2 5 6 7 10001 3 5 6 7 10010 4 5 6 7 10100 1")
        End Sub

        <Fact()>
        Public Sub Spilling_ExceptionInArrayAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("0 ")
            Test(1).Wait(60000)
            Console.Write("1 ")
            Test(2).Wait(60000)
            Console.Write("2 ")
        Catch ex As AggregateException
            Console.Write("EXC(" + ex.InnerExceptions(0).Message + ")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Async Function Test(p As Integer) As Task
        Console.Write("3 ")
        Dim a(1) As Integer
        M(a(p), Await F())
        Console.Write("4 ")
        Console.Write(a(p).ToString() + " ")
    End Function

    Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 100
    End Function

    Public Sub M(ByRef x As Integer, y As Integer)
        Console.Write("7 ")
        x += 10000
    End Sub
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 5 6 7 4 10000 1 3 EXC(Index was outside the bounds of the array.)")
        End Sub

        <Fact()>
        Public Sub Spilling_ExceptionInFieldAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main(args As String())
        Test().Wait(60000)
    End Sub

    Async Function Test() As Task
        Dim b As Box(Of String) = Nothing
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("1 ")
            M(b.field, g(), Await t())
        Catch ex As NullReferenceException
            Console.Write("EXC(")
            Console.Write(ex.Message)
            Console.Write(")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Function

    Function g() As Integer
        Console.Write("!!ERROR!! ")
        Return 1
    End Function

    Sub M(ByRef s As String, i As Integer, j As Integer)
        Console.Write("3 ")
    End Sub

    Async Function t() As Task(Of Integer)
        Console.Write("!!ERROR!! ")
        Await Task.Yield()
        Return 1
    End Function

    Class Box(Of T)
        Public field As T
    End Class
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 EXC(Object reference not set to an instance of an object.)")
        End Sub

        <Fact()>
        Public Sub Capture_ExceptionInArrayAccess()
            Dim source = <compilation>
                             <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("0 ")
            Test(1).Wait(60000)
            Console.Write("1 ")
            Test(2).Wait(60000)
            Console.Write("2 ")
        Catch ex As AggregateException
            Console.Write("EXC(" + ex.InnerExceptions(0).Message + ")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Async Function Test(p As Integer) As Task
        Console.Write("3 ")
        Dim a(1) As Integer
        M(a(p), Await F())
        Console.Write("4 ")
        Console.Write(a(p).ToString() + " ")
    End Function

    Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 100
    End Function

    Public Sub M(ByRef x As Double, y As Integer)
        Console.Write("7 ")
        x += 10000
    End Sub
End Module

    </file>
                         </compilation>

            CompileAndVerify(source, useLatestFramework:=True, options:=TestOptions.ReleaseExe, expectedOutput:="0 3 5 6 7 4 10000 1 3 EXC(Index was outside the bounds of the array.)")
            CompileAndVerify(source, useLatestFramework:=True, options:=TestOptions.DebugExe, expectedOutput:="0 3 5 6 7 4 10000 1 3 EXC(Index was outside the bounds of the array.)")
        End Sub

        <Fact()>
        Public Sub Capture_ExceptionInFieldAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main(args As String())
        Test().Wait(60000)
    End Sub

    Async Function Test() As Task
        Dim b As Box(Of String) = Nothing
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("1 ")
            M(b.field, g(), Await t())
        Catch ex As NullReferenceException
            Console.Write("EXC(")
            Console.Write(ex.Message)
            Console.Write(")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Function

    Function g() As Integer
        Console.Write("!!ERROR!! ")
        Return 1
    End Function

    Sub M(ByRef s As Double, i As Integer, j As Integer)
        Console.Write("3 ")
    End Sub

    Async Function t() As Task(Of Integer)
        Console.Write("!!ERROR!! ")
        Await Task.Yield()
        Return 1
    End Function

    Class Box(Of T)
        Public field As T
    End Class
End Module

    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 EXC(Object reference not set to an instance of an object.)")
        End Sub

        <Fact()>
        Public Sub Spilling_ExceptionInArrayAccess2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("0 ")
            Test({10, 20}, 1, 0).Wait(60000)
            Console.Write("1 ")
            Test({10, 20}, 2, 1).Wait(60000)
            Console.Write("2 ")
        Catch ex As AggregateException
            Console.Write("EXC(" + ex.InnerExceptions(0).Message + ")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Async Function Test(a() As Integer, p1 As Integer, p2 As Integer) As Task
        Console.Write("3 ")
        M(a(INDX(p1)), a(INDX(p2)), Await F())
        Console.Write("4 ")
    End Function

    Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 100
    End Function

    Public Sub M(ByRef x As Integer, ByRef y As Integer, z As Integer)
        Console.Write("7 ")
        x += 10000
        y += 100
        Console.Write(x.ToString() + " ")
        Console.Write(y.ToString() + " ")
    End Sub

    Function INDX(i As Integer) As Integer
        Console.Write("8 ")
        Return i
    End Function

End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 8 8 5 6 7 10020 110 4 1 3 8 EXC(Index was outside the bounds of the array.)")
        End Sub

        <Fact()>
        Public Sub Capture_ExceptionInArrayAccess2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Dim saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture
        Try
            Console.Write("0 ")
            Test({10, 20}, 1, 0).Wait(60000)
            Console.Write("1 ")
            Test({10, 20}, 2, 1).Wait(60000)
            Console.Write("2 ")
        Catch ex As AggregateException
            Console.Write("EXC(" + ex.InnerExceptions(0).Message + ")")
        Finally
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture
        End Try
    End Sub

    Async Function Test(a() As Integer, p1 As Integer, p2 As Integer) As Task
        Console.Write("3 ")
        M(a(INDX(p1)), a(INDX(p2)), Await F())
        Console.Write("4 ")
    End Function

    Async Function F() As Task(Of Integer)
        Console.Write("5 ")
        Await Task.Yield
        Console.Write("6 ")
        Return 100
    End Function

    Public Sub M(ByRef x As Double, ByRef y As Double, z As Integer)
        Console.Write("7 ")
        x += 10000
        y += 100
        Console.Write(x.ToString() + " ")
        Console.Write(y.ToString() + " ")
    End Sub

    Function INDX(i As Integer) As Integer
        Console.Write("8 ")
        Return i
    End Function

End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0 3 8 8 5 6 7 10020 110 4 1 3 8 EXC(Index was outside the bounds of the array.)")
        End Sub

        <Fact()>
        Public Sub Imported_VoidReturningAsync()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Dim i As Integer = 0

    Public Async Sub F(handle As AutoResetEvent)
        Await Task.Factory.StartNew(Sub()
                                        Form1.i += 1
                                    End Sub)
        handle.Set()
    End Sub

    Public Sub Main()
        Dim handle As New AutoResetEvent(False)
        F(handle)
        handle.WaitOne(1000)
        Console.WriteLine(i)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1")
        End Sub

        <Fact(Skip:="785170"), WorkItem(785170, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/785170")>
        Public Sub Imported_AsyncWithEH()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Dim awaitCount As Integer = 0
    Dim finallyCount As Integer = 0

    Sub LogAwait()
        awaitCount += 1
    End Sub

    Sub LogException()
        finallyCount += 1
    End Sub

    Public Async Sub F(handle As AutoResetEvent)
        Await Task.Factory.StartNew(AddressOf LogAwait)
        Try
            Await Task.Factory.StartNew(AddressOf LogAwait)
            Try
                Await Task.Factory.StartNew(AddressOf LogAwait)
                Try
                    Await Task.Factory.StartNew(AddressOf LogAwait)
                    Throw New Exception()
                Catch ex As Exception
                Finally
                    LogException()
                End Try

                Await Task.Factory.StartNew(AddressOf LogAwait)
                Throw New Exception()

            Catch ex As Exception
            Finally
                LogException()
            End Try

            Await Task.Factory.StartNew(AddressOf LogAwait)
            Throw New Exception()

        Catch ex As Exception
        Finally
            LogException()
        End Try

        Await Task.Factory.StartNew(AddressOf LogAwait)

        handle.Set()
    End Sub

    Public Sub Main2(i As Integer)
        awaitCount = 0
        finallyCount = 0
        Dim handle As New AutoResetEvent(False)
        F(handle)
        Dim completed = handle.WaitOne(4000)
        If completed Then
            If Not (awaitCount = 7 And finallyCount = 3) Then
                Throw New Exception("failed at " &amp; i)
            End If
        Else
            Throw New Exception("did not complete in time: " &amp; i)
        End If
    End Sub

    Public Sub Main()
        For i As Integer = 0 To 2000
            Main2(i)
        Next
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub Imported_TaskReturningAsync()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Dim i As Integer = 0

    Public Async Sub F(handle As AutoResetEvent)
        Await Task.Factory.StartNew(Sub()
                                        i = 42
                                    End Sub)
        handle.Set()
    End Sub

    Public Sub Main()
        Dim handle As New AutoResetEvent(False)
        F(handle)
        handle.WaitOne(1000)
        Console.Write(i)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_GenericTaskReturningAsync()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F() As Task(Of String)
        Return Await Task.Factory.StartNew(Function()
                                               Return "O brave new world..."
                                           End Function)
    End Function

    Public Sub Main()
        Console.Write(F().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="O brave new world...")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithLocals()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function()
                                               Return x
                                           End Function)
    End Function

    Public Async Function G(x As Integer) As Task(Of Integer)
        Dim c As Integer = 0
        Await F(x)
        c += x
        Await F(x)
        c += x
        Return c
    End Function

    Public Sub Main()
        Console.Write(G(21).Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithParam()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function G(x As Integer) As Task(Of Integer)
        x = 21 + Await Task.Factory.StartNew(Function()
                                                 Return x
                                             End Function)

        Return 21 + Await Task.Factory.StartNew(Function() x)
    End Function

    Public Sub Main()
        Console.Write(G(0).Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AwaitInExpr()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F() As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() 21)
    End Function

    Public Async Function G() As Task(Of Integer)
        Dim c As Integer = 0
        c = (Await f()) + 21
        Return c
    End Function

    Public Sub Main()
        Console.Write(G().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithParamsAndLocals_Hoisted()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G(x As Integer) As Task(Of Integer)
        Dim c As Integer = 0
        c = (Await F(x)) + 21
        Return c
    End Function

    Public Sub Main()
        Console.Write(G(21).Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithParamsAndLocals_DoubleAwait_Spilling()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G(x As Integer) As Task(Of Integer)
        Dim c As Integer = 0
        c = (Await F(x)) + c
        c = (Await F(x)) + c
        Return c
    End Function

    Public Sub Main()
        Console.Write(G(21).Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithDynamic()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F(x As Object) As Task(Of Integer)
        Return Await x
    End Function

    Public Sub Main()
        Console.Write(Task.Factory.StartNew(Function() 42).Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithThisRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class C
    Public x As Integer = 42

    Public Async Function F() As Task(Of Integer)
        Dim c = Me.x
        Return Await Task.Factory.StartNew(Function() c)
    End Function
End Class

Module Form1
    Sub Main()
        Console.WriteLine(New C().F().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithBaseRef()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class B
    Protected x As Integer = 42
End Class

Class C
    Inherits B

    Public Async Function F() As Task(Of Integer)
        Dim c = MyBase.x
        Return Await Task.Factory.StartNew(Function() c)
    End Function
End Class

Module Form1
    Sub Main()
        Console.WriteLine(New C().F().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithException1()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Async Function F() As Task(Of Integer)
        Throw New Exception()
    End Function

    Async Function G() As Task(Of Integer)
        Try
            Return Await F()
        Catch ex As Exception
            Return -1
        End Try
    End Function

    Sub Main()
        Console.WriteLine(G().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="-1")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncWithException2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Async Function F() As Task(Of Integer)
        Throw New Exception()
    End Function

    Async Function H() As Task(Of Integer)
        Return Await F()
    End Function

    Sub Main()
        Dim t = H()
        Try
            t.Wait(60000)
        Catch ex As AggregateException
            Console.WriteLine("exception")
        End Try
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="exception")
        End Sub

        <Fact()>
        Public Sub Imported_Conformance_Awaiting_Methods_Generic01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Runtime.CompilerServices

Public Class MyTask(Of T)
    Public Function GetAwaiter() As MyTaskAwaiter(Of T)
        Return New MyTaskAwaiter(Of T)()
    End Function

    Public Async Function Run(Of U As {MyTask(Of Integer), New})(uu As U) As Task
        Dim tests = 0
        tests += 1

        Dim rez = Await uu

        If rez = 0 Then
            Form1.Count += 1
        End If

        Result = Form1.Count - tests
    End Function
End Class

Public Class MyTaskAwaiter(Of T)
    Implements INotifyCompletion

    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted

    End Sub

    Public Function GetResult() As T
        Return Nothing
    End Function

    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call New MyTask(Of Integer)().Run(Of MyTask(Of Integer))(New MyTask(Of Integer)()).Wait(60000)
        Console.WriteLine(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Conformance_Awaiting_Methods_Method01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Public Interface IExplicit
    Function Method(Optional x As Integer = 4)
End Interface

Class C1
    Implements IExplicit

    Private Function Method(Optional x As Integer = 4) As Object Implements IExplicit.Method
        Return Task.Run(Async Function()
                            Await Task.Yield
                            form1.Count += 1
                        End Function)
    End Function
End Class

Class TestCase
    Public Async Function Run() As Task
        Dim tests = 0
        tests += 1

        Dim c As New C1()
        Dim e = DirectCast(c, IExplicit)
        Await e.Method()

        Result = Form1.Count - tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call (New TestCase()).Run().Wait(60000)
        Console.WriteLine(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_DoFinallyBodies()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public finally_count As Integer = 0

    Async Function F() As Task
        Try
            Await Task.Factory.StartNew(Sub()
                                        End Sub)
        Finally
            finally_count += 1
        End Try
    End Function

    Sub Main()
        F().Wait(60000)
        Console.WriteLine(finally_count)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1")
        End Sub

        <Fact()>
        Public Sub Imported_Conformance_Awaiting_Methods_Parameter003()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Shared Count = 0

    Public Shared Function Foo(Of T)(tt As T) As T
        Return tt
    End Function

    Public Shared Async Function Bar(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Shared Async Function Run() As Task
        Dim x1 = Foo(Await Bar(4))
        Dim t = Bar(5)
        Dim x2 = Foo(Await t)
        If x1 &lt;&gt; 4 Then
            Count += 1
        End If
        If x2 &lt;&gt; 5 Then
            Count += 1
        End If
    End Function
End Class

Module Form1
    Sub Main()
        TestCase.Run().Wait(60000)
        Console.WriteLine(TestCase.Count)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Conformance_Awaiting_Methods_Method05()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class C
    Public Status As Integer
End Class

Interface IImplicit
    Function Method(Of T As Task(Of C))(ParamArray d() As Decimal) As T
End Interface

Class Impl
    Implements IImplicit

    Public Function Method(Of T As Task(Of C))(ParamArray d() As Decimal) As T Implements IImplicit.Method
        Return Task.Run(Async Function()
                            Await Task.Yield
                            Count += 1
                            Return New C() With {.Status = 1}
                        End Function)
    End Function
End Class

Class TestCase
    Public Async Function Run() As Task
        Dim tests = 0
        Dim i As New Impl()

        tests += 1
        Await i.Method(Of Task(Of C))(3, 4)

        Result = Count - tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call (New TestCase()).Run().Wait(60000)
        Console.WriteLine(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Conformance_Awaiting_Methods_Accessible010()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase : Inherits Test
    Public Shared Count = 0
    Public Shared Async Function Run() As Task
        Dim x = Await Test.GetValue(Of Integer)(1)
        If Not (x = 1) Then
            Count += 1
        End If
    End Function
End Class

Class Test
    Protected Shared Async Function GetValue(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.WriteLine(TestCase.Count)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_NestedUnary()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F() As Task(Of Integer)
        Return 1
    End Function

    Public Async Function G1() As Task(Of Integer)
        Return -(Await F())
    End Function

    Public Async Function G2() As Task(Of Integer)
        Return -(-(Await F()))
    End Function

    Public Async Function G3() As Task(Of Integer)
        Return -(-(-(Await F())))
    End Function

    Public Sub WaitAndPrint(t As Task(Of Integer))
        t.Wait(60000)
        Console.Write(t.Result)
        Console.Write(" ")
    End Sub

    Sub Main()
        WaitAndPrint(G1())
        WaitAndPrint(G2())
        WaitAndPrint(G3())
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="-1 1 -1")
        End Sub

        <Fact()>
        Public Sub Imported_SpillCall()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Sub Printer(ParamArray a() As Integer)
        For Each x In a
            Console.Write(" ")
            Console.Write(x)
        Next
    End Sub

    Public Function Get_(x As Integer) As Integer
        Console.Write(" > " + x.ToString)
        Return x
    End Function

    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G() As Task
        Printer(Get_(111), Get_(222), Get_(333), Await F(Get_(444)), Get_(555))
    End Function

    Sub Main()
        G().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="> 111 > 222 > 333 > 444 > 555 111 222 333 444 555")
        End Sub

        <Fact()>
        Public Sub Imported_SpillCall2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Sub Printer(ParamArray a() As Integer)
        For Each x In a
            Console.Write(" ")
            Console.Write(x)
        Next
    End Sub

    Public Function Get_(x As Integer) As Integer
        Console.Write(" > " + x.ToString)
        Return x
    End Function

    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G() As Task
        Printer(Get_(111), Await F(Get_(222)), Get_(333), Await F(Get_(444)), Get_(555))
    End Function

    Sub Main()
        G().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="> 111 > 222 > 333 > 444 > 555 111 222 333 444 555")
        End Sub

        <Fact()>
        Public Sub Imported_SpillCall3()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Sub Printer(ParamArray a() As Integer)
        For Each x In a
            Console.Write(" ")
            Console.Write(x)
        Next
    End Sub

    Public Function Get_(x As Integer) As Integer
        Console.Write(" > " + x.ToString)
        Return x
    End Function

    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G() As Task
        Printer(1, Await F(2), 3, await F(await F(await F(await F(4)))), Await F(5), 6)
    End Function

    Sub Main()
        G().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3 4 5 6")
        End Sub

        <Fact()>
        Public Sub Imported_SpillCall4()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Sub Printer(ParamArray a() As Integer)
        For Each x In a
            Console.Write(" ")
            Console.Write(x)
        Next
    End Sub

    Public Async Function F(x As Integer) As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() x)
    End Function

    Public Async Function G() As Task
        Printer(1, Await F(Await F(2)))
    End Function

    Sub Main()
        G().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2")
        End Sub

        <Fact()>
        Public Sub Imported_Array01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run(Of T As Structure)(tt As t) as Task
        Dim tests = 0

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        Dim arr(Await GetVal(3)) As Integer
        If arr.Length = 4 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        Dim arr2(Await GetVal(3), Await GetVal(3)) As Decimal
        If arr2.Rank = 2 AndAlso arr2.Length = 16 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        arr2 = New Decimal(3, Await GetVal(3)) {}
        If arr2.Rank = 2 AndAlso arr2.Length = 16 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        ReDim arr2(4, Await GetVal(4))
        If arr2.Rank = 2 AndAlso arr2.Length = 25 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        ReDim Preserve arr2(4, Await GetVal(2))
        If arr2.Rank = 2 AndAlso arr2.Length = 15 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        arr2 = New Decimal(Await GetVal(3), 3) {}
        If arr2.Rank = 2 AndAlso arr2.Length = 16 Then
            Count += 1
        End If

        tests += 1
        Console.Write(tests)
        Console.Write(" ")
        Dim arr3 As Decimal?()() = New Decimal?(Await GetVal(3))() {}
        If arr3.Rank = 1 AndAlso arr3.Length = 4 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

End Class

Module Form1
    Public Result = -1
    Public Count = 0

    Sub Main()
        Call New TestCase().Run(6).Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3 4 5 6 7 0")
        End Sub

        <Fact()>
        Public Sub Imported_Array02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run(Of T As Structure)(tt As t) As Task
        Dim tests = 0

        tests += 1
        Dim arr(Await GetVal(3)) As Integer
        If arr.Length = 4 Then
            Count += 1
        End If

        tests += 1
        arr(0) = Await GetVal(4)
        If arr(0) = 4 Then
            Count += 1
        End If

        tests += 1
        arr(0) += Await GetVal(4)
        If arr(0) = 8 Then
            Count += 1
        End If

        tests += 1
        arr(1) += Await (GetVal(arr(0)))
        If arr(1) = 8 Then
            Count += 1
        End If

        tests += 1
        arr(1) += Await (GetVal(arr(Await GetVal(0))))
        If arr(1) = 16 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call New TestCase().Run(6).Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Array03()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run(Of T As Structure)(tt As t) as Task
        Dim tests = 0

        tests += 1
        Dim arr(Await GetVal(3), Await GetVal(3)) As Integer
        arr(0, 0) = Await GetVal(4)
        If arr(0, Await (GetVal(0))) = 4 Then
            Count += 1
        End If

        tests += 1
        arr(0, 0) += Await GetVal(4)
        If arr(0, Await (GetVal(0))) = 8 Then
            Count += 1
        End If

        tests += 1
        arr(1, 1) += Await (GetVal(arr(0, 0)))
        If arr(1, 1) = 8 Then
            Count += 1
        End If

        tests += 1
        arr(1, 1) += Await (GetVal(arr(0, Await GetVal(0))))
        If arr(1, 1) = 16 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

End Class

Module Form1
    Public Result = -1
    Public Count = 0

    Sub Main()
        Call New TestCase().Run(6).Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Array04()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure MyStruct(Of T)
    Public Property TT As T
    Default Public Property This(index As T) As T
        Get
            Return index
        End Get
        Set(value As T)
            TT = value
        End Set
    End Property
End Structure

Structure TestCase
    Public Async Function Run() As Task
        Dim ms As New MyStruct(Of Integer)()
        Dim x = ms(index:=Await Foo())
        Console.Write(x + 100)
    End Function

    Public Async Function Foo() As Task(Of Integer)
        Await Task.Yield
        Return 10
    End Function
End Structure

Module Form1
    Sub Main()
        Call New TestCase().Run().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="110")
        End Sub

        <Fact()>
        Public Sub Imported_ArrayAssign()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public arr(3) As Integer

    Async Function Run() As Task
        arr(0) = Await Task.Factory.StartNew(Function() 42)
    End Function

    Sub Main()
        Run().Wait(60000)
        Console.Write(arr(0))
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_CaptureThis()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function Run() As Task(Of Integer)
        Return Await Foo()
    End Function

    Public Async Function Foo() As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() 42)
    End Function
End Class

Module Form1
    Sub Main()
        Console.Write(New TestCase().Run().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_SpillArrayLocal()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run(Of t As Structure)(tt As t) As Task
        Dim arr() As Integer = {-1, 42}

        Dim tests = 0

        tests += 1
        Dim t1 = arr(Await GetVal(1))
        If t1 = 42 Then
            Count += 1
        End If

        Result = Count - tests
    End Function
End Class

Module Form1
    Public Result = -1
    Public Count = 0

    Sub Main()
        Call New TestCase().Run(6).Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_SpillArrayCompoundAssignmentLValue()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public arr() As Integer

    Async Function Run() As Task
        arr = {1}
        arr(0) += Await Task.Factory.StartNew(Function() 41)
    End Function

    Sub Main()
        Call Run().Wait(60000)
        Console.Write(arr(0))
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_SpillArrayCompoundAssignmentLValueAwait()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public arr() As Integer

    Async Function Run() As Task
        arr = {1}
        arr(Await Task.Factory.StartNew(Function() 0)) += Await Task.Factory.StartNew(Function() 41)
    End Function

    Sub Main()
        Call Run().Wait(60000)
        Console.Write(arr(0))
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_SpillArrayCompoundAssignmentLValueAwait2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure S1
    Public X As Integer
End Structure

Structure S2
    Public S As S1
End Structure

Module Form1
    Public arr() As S2

    Async Function Run() As Task(Of Integer)
        arr = {New S2() With {.S = New S1() With {.X = 1}}}
        arr(Await Task.Factory.StartNew(Function() 0)).S.X += Await Task.Factory.StartNew(Function() 41)
        Return arr(Await Task.Factory.StartNew(Function() 0)).S.X
    End Function

    Sub Main()
        Console.Write(Run().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_DoubleSpillArrayCompoundAssignment()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure S1
    Public X As Integer
End Structure

Structure S2
    Public S As S1
End Structure

Module Form1
    Public arr() As S2

    Async Function Run() As Task(Of Integer)
        arr = {New S2() With {.S = New S1() With {.X = 1}}}

        arr(Await Task.Factory.StartNew(Function() 0)).S.X +=
            arr((Await Task.Factory.StartNew(Async Function()
                                                 Return Await Task.Factory.StartNew(Function() 1)
                                             End Function)).Result - 1).S.X +
            Await Task.Factory.StartNew(Function() 40)

        Return arr(Await Task.Factory.StartNew(Function() 0)).S.X
    End Function

    Sub Main()
        Console.Write(Run().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_Array05()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run() As Task
        Dim tests = 0

        tests += 1
        Dim arr1()() As Integer =
            New Integer()() {
                New Integer() {Await GetVal(2), Await GetVal(3)},
                New Integer() {4, Await GetVal(5), Await GetVal(6)}
            }
        If arr1(0)(1) = 3 AndAlso arr1(1)(1) = 5 AndAlso arr1(1)(2) = 6 Then
            Count += 1
        End If

        tests += 1
        Dim arr2()() As Integer =
            New Integer()() {
                New Integer() {Await GetVal(2), Await GetVal(3)},
                Await Foo()
            }
        If arr2(0)(1) = 3 AndAlso arr2(1)(1) = 2 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

    Public Async Function Foo() As Task(Of Integer())
        Await Task.Yield
        Return {1, 2, 3}
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Array06()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run() As Task
        Dim tests = 0

        tests += 1
        Dim arr1(,) As Integer =
            {
                {Await GetVal(2), Await GetVal(3)},
                {Await GetVal(5), Await GetVal(6)}
            }
        If arr1(0, 1) = 3 AndAlso arr1(1, 0) = 5 AndAlso arr1(1, 1) = 6 Then
            Count += 1
        End If

        tests += 1
        Dim arr2(,) As Integer =
            {
                {Await GetVal(2), 3},
                {4, Await GetVal(5)}
            }
        If arr2(0, 1) = 3 AndAlso arr2(1, 1) = 5 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

    Public Async Function Foo() As Task(Of Integer())
        Await Task.Yield
        Return {1, 2, 3}
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Array07()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run() As Task
        Dim tests = 0

        tests += 1
        Dim arr1()() As Integer =
            New Integer()() {
                New Integer() {Await GetVal(2), Await Task.Run(Of Integer)(Async Function()
                                                                               Await Task.Yield
                                                                               Return 3
                                                                           End Function)},
                New Integer() {Await GetVal(5), 4, Await Task.Run(Of Integer)(Async Function()
                                                                                  Await Task.Yield
                                                                                  Return 6
                                                                              End Function)}
            }
        If arr1(0)(1) = 3 AndAlso arr1(1)(1) = 4 AndAlso arr1(1)(2) = 6 Then
            Count += 1
        End If

        tests += 1
        Dim arr2()() As Integer =
            New Integer()() {
                New Integer() {Await GetVal(2), 3},
                Await Foo()
            }
        If arr2(0)(1) = 3 AndAlso arr2(1)(1) = 2 Then
            Count += 1
        End If

        Result = Count - tests
    End Function

    Public Async Function Foo() As Task(Of Integer())
        Await Task.Yield
        Return {1, 2, 3}
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        Private Function GetFieldSignatures(type As NamedTypeSymbol) As String()
            Return (From member In type.GetMembers()
                    Where member.Kind = SymbolKind.Field
                    Select member.ToDisplayString()).ToArray()
        End Function

        Private Function ArrayToSortedString(Of T)(arr() As T) As String
            Array.Sort(arr)
            Dim builder As New System.Text.StringBuilder()
            For Each value In arr
                builder.AppendLine(value.ToString)
            Next
            Return builder.ToString()
        End Function

        Private Sub CheckFields(m As ModuleSymbol, typeName As String, methodName As String, expected As String)
            Dim TestCaseClass = m.ContainingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)(typeName)
            For Each member In TestCaseClass.GetTypeMembers()
                If member.Name.IndexOf(methodName, StringComparison.Ordinal) >= 0 Then
                    Assert.Equal(expected, ArrayToSortedString(GetFieldSignatures(member)))
                    Return
                End If
            Next
            Assert.True(False)
        End Sub

        Private Sub CheckFields(m As ModuleSymbol, typeName As String, methodName As String, expected() As String)
            CheckFields(m, typeName, methodName, ArrayToSortedString(expected))
        End Sub

        <Fact()>
        Public Sub Imported_ReuseFields()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Shared Sub F1(x As Integer, y As Integer)
        Console.Write(x)
        Console.Write(" ")
    End Sub

    Shared Async Function F2() As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() 42)
    End Function

    Public Shared Async Function Run() As task
        Dim x = 1
        F1(x, Await F2())

        Dim y = 2
        F1(y, Await F2())

        Dim z = 3
        F1(z, Await F2())
    End Function
End Class

Module Form1
    Sub Main()
        TestCase.Run().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3",
    symbolValidator:=Sub(m)
                         CheckFields(m, "TestCase", "Run",
                            {
                                "Friend $A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Integer)",
                                "Friend $U1 As Integer",
                                "Public $Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                                "Public $State As Integer"
                            })
                     End Sub)
        End Sub

        <Fact()>
        Public Sub AllParametersAreToBeCaptured()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class CLS
    Async Function F1(x As String, y As Integer) as Task
    End Function
End Class

Module Form1
    Sub Main()
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="",
    symbolValidator:=Sub(m)
                         CheckFields(m, "CLS", "F1",
                            {
                                "Friend $VB$Local_x As String",
                                "Friend $VB$Local_y As Integer",
                                "Friend $VB$Me As CLS",
                                "Public $Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                                "Public $State As Integer"
                            })
                     End Sub)
        End Sub

        <Fact()>
        Public Sub Imported_NestedExpressionInArrayInitializer()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function Run() As Task(Of Integer(,))
        Return New Integer(,) {{1, 2, 21 + (Await Task.Factory.StartNew(Function() 21))}}
    End Function

    Sub Main()
        For Each i In Run().Result
            Console.Write(i)
            Console.Write(" ")
        Next
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 42")
        End Sub

        <Fact()>
        Public Sub Imported_Basic02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Shared test As Integer = 0
    Shared count As Integer = 0

    Shared Sub F1(x As Integer, y As Integer)
        Console.Write(x)
        Console.Write(" ")
    End Sub

    Shared Async Function F2() As Task(Of Integer)
        Return Await Task.Factory.StartNew(Function() 42)
    End Function

    Public Shared Async Function Run() As task
        test += 1
        Dim f = Await Bar()
        Dim x = f(1)
        If Not x.Equals("1") Then
            count -= 1
        End If
        Result = test - count
    End Function

    Shared Async Function Bar() As Task(Of Converter(Of Integer, Object))
        count += 1
        Await Task.Yield
        Return Function(p1 As Integer)
                   Return p1.ToString()
               End Function
    End Function

End Class

Module Form1
    Public Result As Integer = -1

    Sub Main()
        Call TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Argument03()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Shared sb As New System.Text.StringBuilder()

    Public Async Function Run() As Task
        Bar(One(), Await Two())
        If sb.ToString() = "OneTwo" Then
            Result = 0
        End If
    End Function

    Function One() As Integer
        sb.Append("One")
        Return 1
    End Function

    Async Function Two() As Task(Of Integer)
        Await Task.Yield
        sb.Append("Two")
        Return 2
    End Function

    Sub Bar(ParamArray a() As Object)
        For Each x In a
            Console.Write(x.ToString())
            Console.Write(" ")
        Next
    End Sub
End Class

Module Form1
    Public Result As Integer = -1

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 0")
        End Sub

        <Fact()>
        Public Sub Imported_ObjectInit02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections
Imports System.Collections.Generic

Structure TestCase
    Implements IEnumerable

    Public X As Integer

    Public Async Function Run() As Task
        Dim test = 0
        Dim count = 0

        test += 1
        Dim x = New TestCase With {.X = Await Bar()}
        If x.X = 1 Then
            count += 1
        End If

        Result = test - count
    End Function

    Async Function Bar() As Task(Of Integer)
        Await Task.Yield
        Return 1
    End Function

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New Exception()
    End Function
End Structure

Module Form1
    Public Result As Integer = -1

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Generic01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Shared test As Integer = 0
    Shared count As Integer = 0

    Public Async Function Run() As Task
        test += 1
        Qux(Async Function()
                Return 1
            End Function)

        Await Task.Yield

        Result = test - count
    End Function

    Shared Async Sub Qux(Of T)(x As Func(Of Task(Of T)))
        Dim y = Await x()
        If DirectCast(DirectCast(y, Object), Integer) = 1 Then
            count += 1
        End If
    End Sub
End Class

Module Form1
    Public Result As Integer = -1

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Ref01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class BaseTestCase
    Public Sub FooRef(ByRef d As Decimal, x As Integer, ByRef od As Decimal)
        od = d
        d += 1
    End Sub

    Public Async Function GetVal(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function
End Class

Class TestCase : Inherits BaseTestCase
    Public Async Function Run() As Task
        Dim tests = 0

        Dim d As Decimal = 1
        Dim od As Decimal

        tests += 1
        MyBase.FooRef(d, Await MyBase.GetVal(4), od)
        If d = 2 AndAlso od = 1 Then
            count += 1
        End If

        Result = count - tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public count As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Struct02a()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure TestCase
    Private t As Task(Of Integer)
    Public Async Function Run() As Task
        Tests += 1
        t = Task.Run(Async Function()
                         Await Task.Yield
                         Return 1
                     End Function)
        Dim x = Await t
        If x = 1 Then
            Count += 1
        End If

        Tests += 1
        t = Task.Run(Async Function()
                         Await Task.Yield
                         Return 1
                     End Function)
        Dim x2 = Await Me.t
        If x2 = 1 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Structure

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Struct02b()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure TT
    Public t As Task(Of Integer)
End Structure

Structure TTT
    Public t As TT
End Structure

Structure TestCase
    Private t As TTT
    Public Async Function Run() As Task
        Tests += 1
        t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x = Await t.t.t
        If x = 1 Then
            Count += 1
        End If

        Tests += 1
        t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x2 = Await Me.t.t.t
        If x2 = 1 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Structure

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Struct02c()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure TT
    Public t As Task(Of Integer)
End Structure

Structure TTT
    Public t As TT
End Structure

Structure TestCase
    Private t As TTT
    Public Async Function Run() As Task
        Tests += 1
        MyClass.t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x = Await t.t.t
        If x = 1 Then
            Count += 1
        End If

        Tests += 1
        t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x2 = Await MyClass.t.t.t
        If x2 = 1 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Structure

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Struct02d()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure TT
    Public t As Task(Of Integer)
End Structure

Structure TTT
    Public t As TT
End Structure

Class Base
    Protected t As TTT
End Class

Class TestCase: Inherits Base
    Public Async Function Run() As Task
        Tests += 1
        MyBase.t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x = Await MyBase.t.t.t
        If x = 1 Then
            Count += 1
        End If

        Tests += 1
        MyBase.t.t.t = Task.Run(Async Function()
                             Await Task.Yield
                             Return 1
                         End Function)
        Dim x2 = Await MyBase.t.t.t
        If x2 = 1 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_StackSpill_Operator_Compound02()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run() As Task
        Dim x() As Integer = {1, 2, 3, 4}

        Tests += 1
        x(Await GetVal(0)) += Await GetVal(4)
        If x(0) = 5 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_AwaitSwitch()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function Run() As Task
        Tests += 1
        Select Case Await (Async Function()
                               Await Task.Yield
                               Return 5
                           End Function)()
            Case 1
            Case 2
            Case 5
                Count += 1
            Case Else
        End Select

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Inference()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure Test
    Public ReadOnly Property Foo As Task(Of String)
        Get
            Return Task.Run(Of String)(Async Function()
                                           Await Task.Yield
                                           Return "abc"
                                       End Function)
        End Get
    End Property
End Structure

Class TestCase(Of U)
    Public Shared Async Function GetVal(tt As Object) As Task(Of Object)
        Await Task.Yield
        Return tt
    End Function

    Public Shared Function GetVal1(Of T As Task(Of U))(tt As T) As T
        Return tt
    End Function

    Public Async Function Run() As Task
        Dim t As New Test()

        Tests += 1
        Dim x1 = Await TestCase(Of String).GetVal(Await t.Foo)
        If x1 = "abc" Then
            Count += 1
        End If

        Tests += 1
        Dim x2 = Await TestCase(Of String).GetVal1(t.Foo)
        If x2 = "abc" Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase(Of Integer)().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Operator05()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase(Of U)
    Public Async Function Foo1() As Task(Of Integer)
        Await Task.Yield
        Count += 1
        Dim i = 42
        Return i
    End Function

    Public Async Function Foo2() As Task(Of Object)
        Await Task.Yield
        Count += 1
        Return "string"
    End Function

    Public Async Function Run() As Task
        Dim x1 = TryCast(Await Foo1(), Object)
        Dim x2 = TypeOf (Await Foo2()) Is String
        If x1.Equals(42) Then
            Tests += 1
        End If
        If x2 = True Then
            Tests += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase(Of Integer)().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Property21()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class Base
    Public Overridable Property MyProp As Integer
        Get
            Return 42
        End Get
        Protected Set(value As Integer)
        End Set
    End Property
End Class

Class TestCase : Inherits Base
    Async Function GetBaseMyProp() As Task(Of Integer)
        Await Task.Yield
        Return MyBase.MyProp
    End Function

    Public Async Function Run() As Task
        Result = Await GetBaseMyProp() - 42
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_AnonType32()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function Run() As Task
        Tests += 1
        Try
            Throw New Exception(
                Await (New With {
                        .Task = Task.Run(Of String)(
                            Async Function()
                                Await Task.Yield
                                Return "0-0"
                            End Function)}).Task)
        Catch ex As Exception
            If ex.Message = "0-0" Then
                Count += 1
            End If
        End Try

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Init19()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class ObjInit
    Public async As Integer
    Public t As Task
    Public l As Long
End Class

Class TestCase
    Private Function [Throw](Of T)(i As T) As T
        Throw New OverflowException()
    End Function

    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        [Throw](tt)
        Return tt
    End Function

    Public Property MyProperty As Task(Of Long)

    Public Async Function Run() As Task
        Dim t = Task.Run(Of Integer)(Async Function()
                                         Await Task.Yield
                                         Throw New FieldAccessException()
                                         Return 1
                                     End Function)

        Tests += 1
        Try
            MyProperty = Task.Run(Of Long)(Async Function()
                                               Await Task.Yield
                                               Throw New DataMisalignedException()
                                               Return 1
                                           End Function)

            Dim obj As New ObjInit() With {
                .async = Await t,
                .t = GetVal((Task.Run(Async Sub()
                                          Await Task.Yield
                                      End Sub))),
                .l = Await MyProperty
            }

            Await obj.t
        Catch fieldex As FieldAccessException
            Count += 1
        Catch ex As Exception
            Count -= 1
        End Try

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call New TestCase().Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Dynamic()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Public Async Function F1(d As Object) As Task(Of Object)
        Return Await d
    End Function

    Public Async Function F2(d As Task(Of Integer)) As Task(Of Integer)
        Return Await d
    End Function

    Public Async Function Run() As Task(Of Integer)
        Dim a As Integer = Await F1(Task.Factory.StartNew(Function() 21))
        Dim b = Await F2(Task.Factory.StartNew(Function() 21))
        Return a + b
    End Function

    Sub Main()
        Console.Write(Run().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_Await15()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure DynamicClass
    Public Async Function Foo(Of T)(tt As T) As Task(Of Object)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Bar(i As Integer) As Task(Of Task(Of Object))
        Await Task.Yield
        Return Task.Run(Of Object)(Async Function()
                                       Await Task.Yield
                                       Return i
                                   End Function)
    End Function
End Structure

Class TestCase
    Public Shared Async Function Run() As Task
        Dim dc As New DynamicClass()
        Dim d As Object = 123

        Tests += 1
        Dim x1 = Await dc.Foo("")
        If x1 = "" Then
            Count += 1
        End If

        Tests += 1
        Dim x2 = Await Await dc.Bar(d)
        If x2 = 123 Then
            Count += 1
        End If

        Tests += 1
        Dim x3 = Await Await dc.Bar(Await dc.Foo(234))
        If x3 = 234 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Await40()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class C1
    Public Async Function Method(x As Integer) As Task(Of Integer)
        Await Task.Yield
        Return x
    End Function
End Class

Class C2
    Public Status As Integer
    Public Sub New(Optional x As Integer = 5)
        Status = x
    End Sub
    Public Sub New(x As Integer, y As Integer)
        Status = x + y
    End Sub
    Public Function Bar(x As Integer) As Integer
        Return x
    End Function
End Class

Class TestCase
    Public Shared Async Function Run() As Task

        tests += 1
        Dim c As Object = New C1()
        Dim cc As New C2(x:=Await c.Method(1))
        If cc.Status = 1 Then
            Count += 1
        End If

        tests += 1
        Dim f As Object = (Async Function()
                               Await Task.Yield
                               Return 4
                           End Function)

        cc = New C2(Await c.Method(2), Await f.Invoke())
        If cc.Status = 6 Then
            Count += 1
        End If

        tests += 1
        Dim x = New C2(2).Bar(Await c.Method(1))
        If x = 1 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Await43()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure MyClazz
    Public Shared Operator *(c As MyClazz, i As Integer) As Task
        Return Task.Run(Async Function() As task
                            Await Task.Yield
                            Count += 1
                        End Function)
    End Operator
    Public Shared Operator +(c As MyClazz, i As Integer) As Task
        Return Task.Run(Async Function() As task
                            Await Task.Yield
                            Count += 1
                        End Function)
    End Operator
End Structure

Class TestCase
    Public Shared Async Function Run() As Task
        Dim dy As Object = Task.Run(Of MyClazz)(Async Function()
                                                    Await Task.Yield
                                                    Return New MyClazz()
                                                End Function)
        Tests += 1
        Await ((Await dy) * 5)

        Tests += 1
        Dim d As Object = New MyClazz()
        Dim dd As Object = Task.Run(Of Long)(Async Function()
                                                 Await Task.Yield
                                                 Return 1L
                                             End Function)
        Await (d + Await dd)

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Await44()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure MyClazz
    Public Shared Narrowing Operator CType(c As MyClazz) As task
        Return Task.Run(Async Function() As task
                            Await Task.Yield
                            Count += 1
                        End Function)
    End Operator
End Structure

Class TestCase
    Public Shared Async Function Run() As Task
        Dim mc As New MyClazz()

        Tests += 1
        Dim t1 As Task = mc
        Await t1

        Tests += 1
        Dim t2 As Object = CType(mc, Task)
        Await t2

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Async_Conformance_Awaiting_indexer23_ValueType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure MyClazz(Of T As Task(Of Func(Of Integer)))
    Public Property P As T
    Public F As T

    Default Public Property This(index As T) As T
        Get
            Return P
        End Get
        Set(value As T)
            P = value
        End Set
    End Property
End Structure

Class TestCase
    Public Shared Async Function Foo(d As Task(Of Func(Of Integer))) As Task(Of Task(Of Func(Of Integer)))
        Await Task.Yield
        Interlocked.Increment(Count)
        Return d
    End Function

    Public Shared Async Function Run() As Task
        Dim ms As New MyClazz(Of Task(Of Func(Of Integer)))()

        ms(index:=Nothing) = Task.Run(Of Func(Of Integer))(Async Function()
                                                               Await Task.Yield
                                                               Interlocked.Increment(Count)
                                                               Return Function()
                                                                          Return 123
                                                                      End Function
                                                           End Function)
        Tests += 1
        Dim x = Await ms(index:=Await Foo(Nothing))
        If x IsNot Nothing AndAlso x() = 123 Then
            Tests += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Async_Conformance_Awaiting_indexer23_ReferenceType()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class MyClazz(Of T As Task(Of Func(Of Integer)))
    Public Property P As T
    Public F As T

    Default Public Property This(index As T) As T
        Get
            Return P
        End Get
        Set(value As T)
            P = value
        End Set
    End Property
End Class

Class TestCase
    Public Shared Async Function Foo(d As Task(Of Func(Of Integer))) As Task(Of Task(Of Func(Of Integer)))
        Await Task.Yield
        Interlocked.Increment(Count)
        Return d
    End Function

    Public Shared Async Function Run() As Task
        Dim ms As New MyClazz(Of Task(Of Func(Of Integer)))()

        ms(index:=Nothing) = Task.Run(Of Func(Of Integer))(Async Function()
                                                               Await Task.Yield
                                                               Interlocked.Increment(Count)
                                                               Return Function()
                                                                          Return 123
                                                                      End Function
                                                           End Function)
        Tests += 1
        Dim x = Await ms(index:=Await Foo(Nothing))
        If x IsNot Nothing AndAlso x() = 123 Then
            Tests += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        TestCase.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_Async_StackSpill_Argument_Generic04()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Public Class MyClazz(Of T)
    Public Async Function Foo(Of V)(tt As T, vv As V) As Task(Of Object)
        Await Task.Yield
        Return vv
    End Function
End Class

Class TestCase
    Public Shared Async Function Foo() As Task(Of Integer)
        Dim mc As Object = New MyClazz(Of String)()
        Dim rez = Await mc.Foo(Of String)(Nothing, Await (Async Function() As Task(Of String)
                                                              Await Task.Yield
                                                              Return "Test"
                                                          End Function)())
        If rez = "Test" Then
            Return 0
        End If
        Return 1
    End Function
End Class

Module Form1
    Sub Main()
        Console.Write(TestCase.Foo().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_AsyncStackSpill_assign01()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Private val As Integer

    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Async Function Run() As Task
        Tests += 1
        Dim x() As Integer = {1, 2, 3, 4}
        val = Await (Async Function()
                         x(Await GetVal(0)) += Await GetVal(4)
                         Return x(Await GetVal(0))
                     End Function())

        If x(0) = 5 AndAlso val = Await GetVal(5) Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call (New TestCase()).Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_MyTask_08()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Public Class MyTask
    Public Shared Async Function Run() As Task
        Tests += 1
        Dim myTask As New MyTask()
        Dim x = Await myTask
        If x = 123 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Public Class MyTaskAwaiter : Implements System.Runtime.CompilerServices.INotifyCompletion
    Public Sub OnCompleted(continuation As Action) Implements System.Runtime.CompilerServices.INotifyCompletion.OnCompleted
    End Sub
    Public Function GetResult() As Integer
        Return 123
    End Function
    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class

Public Module Extension
    &lt;System.Runtime.CompilerServices.Extension&gt;
    Public Function GetAwaiter(this As MyTask) As MyTaskAwaiter
        Return New MyTaskAwaiter()
    End Function
End Module

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        MyTask.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_MyTask_16()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Public Class MyTask
    Public Function GetAwaiter() As MyTaskAwaiter
        Return New MyTaskAwaiter()
    End Function

    Public Shared Async Function Run() As Task
        Tests += 1
        Dim myTask As New MyTask()
        Dim x = Await myTask
        If x = 123 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Public Class MyTaskBaseAwaiter : Implements System.Runtime.CompilerServices.INotifyCompletion
    Public Sub OnCompleted(continuation As Action) Implements System.Runtime.CompilerServices.INotifyCompletion.OnCompleted
    End Sub
    Public Function GetResult() As Integer
        Return 123
    End Function
    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return True
        End Get
    End Property
End Class

Public Class MyTaskAwaiter : Inherits MyTaskBaseAwaiter
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        MyTask.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_InitCollection_045()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks

Structure PrivateCollection : Implements IEnumerable
    Public lst As List(Of Integer)
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return lst
    End Function
    Public Sub Add(x As Integer)
        If lst Is Nothing Then
            lst = New List(Of Integer)
        End If
        lst.Add(x)
    End Sub
End Structure

Public Class MyTask
    Public Shared Async Function GetVal(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Shared Async Function Run() As Task
        Tests += 1
        Dim myCol = New PrivateCollection() From {Await GetVal(1), Await GetVal(2)}
        If myCol.lst(0) = 1 AndAlso myCol.lst(1) = 2 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        MyTask.Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_RefExpr()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class MyClazz
    Public Field As Integer
End Class

Public Class MyTask
    Public Shared Function Foo(ByRef x As Integer, y As Integer) As Integer
        Return x + y
    End Function

    Public Shared Async Function GetVal(Of T)(tt As T) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Public Shared Async Function Run() As Task(Of Integer)
        Return Foo((New MyClazz() With {.Field = 21}.Field), Await Task.Factory.StartNew(Function() 21))
    End Function
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        'MyTask.Run()
        'CompletedSignal.WaitOne(60000)
        'Console.Write(Result)
        Console.Write(MyTask.Run().Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub Imported_ManagedPointerSpillAssign03()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Public Async Function GetVal(Of T)(tt As t) As Task(Of T)
        Await Task.Yield
        Return tt
    End Function

    Class PrivClass
        Friend Structure ValueT
            Public Field As Integer
        End Structure

        Friend arr(2) As ValueT

    End Class

    Private myPrivClass As PrivClass

    Public Async Function Run() As Task
        Me.myPrivClass = New PrivClass()

        Tests += 1
        Me.myPrivClass.arr(0).Field = Await GetVal(4)
        If Me.myPrivClass.arr(0).Field = 4 Then
            Count += 1
        End If

        Tests += 1
        Me.myPrivClass.arr(0).Field += Await GetVal(4)
        If Me.myPrivClass.arr(0).Field = 8 Then
            Count += 1
        End If

        Tests += 1
        Me.myPrivClass.arr(Await GetVal(1)).Field += Await GetVal(4)
        If Me.myPrivClass.arr(1).Field = 4 Then
            Count += 1
        End If

        Tests += 1
        Me.myPrivClass.arr(Await GetVal(1)).Field += 1
        If Me.myPrivClass.arr(1).Field = 5 Then
            Count += 1
        End If

        Result = Count - Tests
    End Function

End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        Call (New TestCase()).Run().Wait(60000)
        Console.Write(Result)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_SacrificialRead()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Class TestCase
    Shared Sub F1(ByRef x As Integer, y As Integer, z As Integer)
        x += y + z
    End Sub

    Shared Function F0() As Integer
        Console.Write(-1)
        Return 0
    End Function

    Shared Async Function F2() As Task(Of Integer)
        Dim x() As Integer = {21}
        x = Nothing
        F1(x(0), F0(), Await Task.Factory.StartNew(Function() 21))
        Return x(0)
    End Function

End Class

Module Form1
    Sub Main()
        Dim t = TestCase.F2()
        Try
            t.Wait(60000)
        Catch ex As Exception
            Console.Write(0)
            Return
        End Try
        Console.Write(-1)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="0")
        End Sub

        <Fact()>
        Public Sub Imported_RefThisStruct()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Structure S1
    Public X As Integer

    Public Async Sub Foo1()
        Bar(Me, Await Task(Of Integer).FromResult(42))
    End Sub

    Public Sub Foo2()
        Bar(Me, 42)
    End Sub

    Public Sub Bar(ByRef x As S1, y As Integer)
        x.X = 42
    End Sub
End Structure

Class C1
    Public X As Integer

    Public Async Sub Foo1()
        Bar(Me, Await Task(Of Integer).FromResult(42))
    End Sub

    Public Sub Foo2()
        Bar(Me, 42)
    End Sub

    Public Sub Bar(ByRef x As C1, y As Integer)
        x.X = 42
    End Sub
End Class

Module Form1
    Public Result As Integer = -1
    Public Count As Integer = 0
    Public Tests As Integer = 0

    Sub Main()
        If True Then
            Dim s As S1
            s.X = -1
            s.Foo1()
            Console.Write(s.X)
            Console.Write(" ")
        End If
        If True Then
            Dim s As S1
            s.X = -1
            s.Foo2()
            Console.Write(s.X)
            Console.Write(" ")
        End If
        If True Then
            Dim s As S1
            s.X = -1
            s.Bar(s, 42)
            Console.Write(s.X)
            Console.Write(" ")
        End If
        If True Then
            Dim s As New C1
            s.X = -1
            s.Foo1()
            Console.Write(s.X)
            Console.Write(" ")
        End If
        If True Then
            Dim s As New C1
            s.X = -1
            s.Foo2()
            Console.Write(s.X)
            Console.Write(" ")
        End If
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="-1 -1 42 42 42")
        End Sub

        <Fact()>
        Public Sub FieldReuseOnStatementLevel()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Collections
Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Async Function F(Of T)(p As T, s As String) As Task(Of T)
        Console.Write(s)
        Console.Write(" ")
        Await Task.Yield
        Return p
    End Function

    Function S(Of T)(ParamArray a() As T) As T
        Console.Write("S(")
        For i = 0 To a.Count - 1
            If i > 0 Then
                Console.Write(",")
            End If
            Console.Write(a(i).ToString())
        Next
        Console.Write(") ")
        Return a(0)
    End Function

    Async Function Test() As Task
        Await Task.Yield
        S(Await F(True, "1"),
          Await F(True, "2"),
          Await F(True, "3"),
          If(Await F(False, "4"),
             Await F(False, "5"),
             S(Await F(False, "6"),
               Await F(False, "7"),
               Await F(False, "8"))),
         Await F(True, "9"))
    End Function

    Sub Main()
        Test().Wait(60000)
    End Sub
End Module
            </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3 4 6 7 8 S(False,False,False) 9 S(True,True,True,False,True)",
    symbolValidator:=Sub(m)
                         CheckFields(m, "Form1", "Test",
                            {
                                "Friend $A0 As System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter",
                                "Friend $A1 As System.Runtime.CompilerServices.TaskAwaiter(Of Boolean)",
                                "Friend $U1 As Boolean",
                                "Friend $U2 As Boolean",
                                "Friend $U3 As Boolean",
                                "Friend $U4 As Boolean",
                                "Friend $U5 As Boolean",
                                "Friend $U6 As Boolean",
                                "Friend $U7 As Boolean",
                                "Public $Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
                                "Public $State As Integer"
                            })
                     End Sub)
        End Sub

        <Fact()>
        Public Sub SpillValueRequiringCleanUp()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Sub M(Of T)(x As T, y As T)
        Console.Write("M ")
    End Sub

    Async Function F(Of T As New)(v As T) As Task(Of T)
        Console.Write("F ")
        Await Task.Yield
        Return v
    End Function

    Class C
    End Class

    Async Function Test(Of T As New)() As Task
        Console.Write("Test ")
        M(New T, Await F(New T))
    End Function

    Sub Main()
        Test(Of C)().Wait(60000)
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="Test F M")
        End Sub

        <Fact()>
        Public Sub CapturedExceptionInCatchBlock()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Async Function Test() As task
        Dim col = {1, 2, 3}
        Try
            Throw New Exception("test")
        Catch ex As Exception
            Dim q = From i In col Where ex.Message = "test" Select p = ex.Message
            For Each t In q
                Console.Write(t)
                Console.Write(" ")
            Next
        End Try
        Await task.Delay(5)
    End Function

    Sub Main()
        Test().Wait()
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="test test test")
        End Sub

        <Fact()>
        Public Sub ForLoopAndLateBinding()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Async Function x() As Task(Of Integer)
        Await Task.Yield
        Return 1
    End Function

    Async Function y() As Task(Of Integer)
        Await Task.Yield
        Return 10
    End Function

    Async Function z() As Task(Of Integer)
        Await Task.Yield
        Return 2
    End Function

    Async Function Test() As Task
        'Try
            Dim a As Object = x()
            Dim b As Object = y()
            Dim c As Object = z()
            Dim iCount As Integer = 0

            For i = Await a To Await b Step Await c
                Console.Write(i)
                Console.Write(" ")
            Next

        'Catch ex As Exception
        '    Console.Write(" EXC(")
        '    Console.Write(ex.Message)
        '    Console.Write(")")
        'End Try
   End Function

    Sub Main()
        Test().Wait()
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 3 5 7 9")
        End Sub

        <Fact, WorkItem(1003196, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003196")>
        Public Sub AsyncAndPartialMethods()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Form1
    Sub Main()
        Call (New C).CallingMethod()
    End Sub
End Module

Partial Class C
    Public Async Sub CallingMethod()
        Await Task.Yield
        F()
    End Sub

    Partial Private Sub F()
    End Sub
End Class

Partial Class C
    Private Async Sub F()
        Await Task.Yield
    End Sub
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="")
        End Sub

        <Fact()>
        Public Sub NoNeedToProcessUnstructuredExceptions()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Program
    Sub Main(args As String())
        Console.Write(New TestCase_named_04().Concatenate("1", " ", "2").Result)
    End Sub
End Module

Class TestCase_named_04
    Public Async Function Concatenate(ParamArray vals As String()) As Task(Of String)
        Await Task.Yield
        Dim rez As String = String.Empty
        For i = 0 To vals.Length - 1
            rez += vals(i)
        Next
        Return rez
    End Function
End Class
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2")
        End Sub

        <Fact()>
        Public Sub ExceptionsInPropertyAccess()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Module1
    Public Sub Main(args As String())
        Test1().Wait()
        Test2().Wait()
        Test3().Wait()
        Test4().Wait()
    End Sub

    Async Function Test1() As Task
        Try
            M().PropA() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
    End Function

    Async Function Test2() As Task
        Try
            M().PropA() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
        Try
            M().PropA() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += L()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += O()
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
    End Function

    Async Function Test3() As Task
        Try
            M().PropA() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() = (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
    End Function

    Async Function Test4() As Task
        Try
            M().PropA() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
        Try
            M().PropA() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropA() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            M().PropB() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropA() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += (Await LL())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try

        Try
            N().PropB() += (Await OO())
        Catch ex As Exception
            Console.Write(ex.Message)
            Console.Write(" ")
        End Try
    End Function

    Async Function F(ParamArray a() As Integer) As Task(Of String)
        Await Task.Yield
        Return ""
    End Function

    Private Function L() As String
        Throw New Exception("L()")
    End Function

    Private Async Function LL() As Task(Of String)
        Throw New Exception("L()")
    End Function

    Private Function M() As Clazz
        Throw New Exception("M()")
    End Function

    Private Function N() As Clazz
        Return New Clazz()
    End Function

    Private Function O() As String
        Return ""
    End Function

    Private Async Function OO() As Task(Of String)
        Return ""
    End Function

End Module

Public Class Clazz
    Public Property PropA As String
        Get
            Throw New Exception("get_Prop")
        End Get
        Set(value As String)
            Throw New Exception("set_Prop")
        End Set
    End Property
    Public Property PropB As String
        Get
            Throw New Exception("get_Prop")
        End Get
        Set(value As String)
            Throw New Exception("set_Prop")
        End Set
    End Property
End Class
    </file>
</compilation>, useLatestFramework:=True,
                expectedOutput:="M() M() M() M() L() set_Prop L() set_Prop M() M() M() M() L() set_Prop L() set_Prop M()" +
                                " M() M() M() get_Prop get_Prop get_Prop get_Prop M() M() M() M() get_Prop get_Prop get_Prop" +
                                " get_Prop M() M() M() M() L() set_Prop L() set_Prop M() M() M() M() L() set_Prop L() set_Prop" +
                                " M() M() M() M() get_Prop get_Prop get_Prop get_Prop M() M() M() M() get_Prop get_Prop get_Prop get_Prop")
        End Sub

        <Fact()>
        Public Sub RewritingBlocksIntoBoundStateMachineScope()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Module Program
    Sub Main(args As String())
        Console.Write(Select3().Result)
    End Sub

    Async Function Select3() As Task(Of Object)
        Dim outer = 1
        Dim s As Object
        Select Case 1
            Case 1
                Dim inner1 = 41
                Await Task.Yield
                s = inner1
            Case Else
                Dim inner2 = 1
                s = inner2
        End Select
        s = outer + s
        Return s
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub AwaitWithPlaceholderInLambda()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Reflection

Module Module1
    Dim t As Task(Of Integer) = Task.Factory.StartNew(Function() 4)

    Sub Main()
        f1(t).Wait()
        Console.Write(f2(2).Result)
        g(t, 4)
    End Sub

    Async Function f1(Of U)(ByVal x As Task(Of U)) As Task
        Console.Write(Await x)
    End Function

    Async Function f2(Of U)(ByVal y As U) As Task(Of U)
        Return y
    End Function

    Sub g(Of U)(ByVal x As Task(Of U), ByVal y As U)
        Dim lambda1 = Async Function(x1 As Task(Of U))
                          Dim z = Await x1
                      End Function

        Dim lambda2 = Async Function(y2 As U)
                          Return y2
                      End Function

        lambda1(x).Wait()
        lambda2(y).Wait()
    End Sub
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="42")
        End Sub

        <Fact()>
        Public Sub GenericLambdasAndAsyncFunctions()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Reflection
Imports System.Collections

Module Module1
    Sub Main()
        h(Of Integer)(Function() b(Of Integer)(),
                      b(Of Integer)(),
                      Function() d(Of Integer)(),
                      d(Of Integer)())
    End Sub

    Sub h(Of T)(ByVal p1 As Func(Of AwaitableStructure(Of T)), ByVal p2 As AwaitableStructure(Of T),
                      ByVal p3 As Func(Of Task(Of T)), ByVal p4 As Task(Of T))

        Dim lambda = Async Function()
                         Console.Write("1 ")
                         Await e(Of T)()
                         Console.Write(Await e(Of T)())
                         Console.Write(" ")
                         Dim e1 = e(Of T)()
                         Await e1
                         Console.Write(Await e1)
                         Console.Write(" ")
                     End Function
        lambda().Wait()
    End Sub

    Public Structure AwaitableStructure
        Public Function GetAwaiter() As AwaiterStructure
            Console.Write("2 ")
            Return New AwaiterStructure
        End Function
    End Structure

    Public Structure AwaiterStructure : Implements System.Runtime.CompilerServices.INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean
            Get
                Console.Write("3 ")
                Return True
            End Get
        End Property

        Public Sub OnCompleted(ByVal continuationAction As Action) Implements System.Runtime.CompilerServices.INotifyCompletion.OnCompleted
            Console.Write("4 ")
        End Sub

        Public Sub GetResult()
            Console.Write("5 ")
        End Sub
    End Structure


    Public Function b(Of T)() As AwaitableStructure(Of T)
        Console.Write("6 ")
        Return New AwaitableStructure(Of T)
    End Function

    Public Structure AwaitableStructure(Of T)
        Public Function GetAwaiter() As AwaiterStructure(Of T)
            Console.Write("7 ")
            Return New AwaiterStructure(Of T)
        End Function
    End Structure

    Public Structure AwaiterStructure(Of T) : Implements System.Runtime.CompilerServices.INotifyCompletion
        Public ReadOnly Property IsCompleted As Boolean
            Get
                Console.Write("8 ")
                Return True
            End Get
        End Property

        Public Sub OnCompleted(ByVal continuationAction As Action) Implements System.Runtime.CompilerServices.INotifyCompletion.OnCompleted
            Console.Write("9 ")
        End Sub

        Public Function GetResult() As T
            Console.Write("10 ")
            Return Nothing
        End Function
    End Structure

    Public Function d(Of T)() As Task(Of T)
        Console.Write("11 ")
        Return Task.Factory.StartNew(Of T)(Function() Nothing)
    End Function

    Public Function e(Of T)() As RegularStructure(Of T)
        Console.Write("12 ")
        Return New RegularStructure(Of T)
    End Function

    Public Structure RegularStructure(Of T)
    End Structure

    &lt;System.Runtime.CompilerServices.Extension()&gt; Function GetAwaiter(Of T)(ByVal self As RegularStructure(Of T)) As AwaiterStructure(Of T)
        Console.Write("13 ")
        Return New AwaiterStructure(Of T)
    End Function
End Module
            </file>
</compilation>, useLatestFramework:=True, expectedOutput:="6 11 1 12 13 8 10 12 13 8 10 0 12 13 8 10 13 8 10 0")
        End Sub

        <Fact>
        Public Sub LiftApparentlyEmptyStructs()
            Dim csCompilation = CreateCSharpCompilation("Empty_cs", <![CDATA[
/// <summary>
/// An apparently empty struct that actually encapsulates a byte. Used to see how
/// the compiler treats empty structs.
/// </summary>
public struct Empty
{
    public byte Value
    {
        get
        {
            unsafe
            {
                byte* p = Ptr(ref this);
                return *p;
            }
        }
        set
        {
            unsafe
            {
                byte* p = Ptr(ref this);
                *p = value;
            }
        }
    }
    private unsafe byte* Ptr(ref Empty e)
    {
        fixed (Empty* p = &e)
        {
            return (byte*)p;
        }
    }
}]]>,
                compilationOptions:=New CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel:=OptimizationLevel.Release, allowUnsafe:=True))

            csCompilation.VerifyDiagnostics()
            Dim vbexeCompilation = CreateVisualBasicCompilation("VBExe", <![CDATA[
Imports System
Imports System.Threading.Tasks

Module Module1

    Sub Main()
        Sample().Wait()
    End Sub

    Private Async Function Sample() As Task
        Dim e1 As Empty
        e1.Value = 12
        Await Task.Delay(5)
        Console.WriteLine(e1.Value)
    End Function

End Module]]>,
                compilationOptions:=TestOptions.ReleaseExe,
                referencedCompilations:={csCompilation},
                referencedAssemblies:=LatestVbReferences)

            Dim vbexeVerifier = CompileAndVerify(vbexeCompilation, expectedOutput:="12")
            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub HoistingUninitializerVars()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Reflection

Module Form1
    Sub Main()
        UninitializedVar().Wait(60000)
    End Sub

    Async Function UninitializedVar() As Task
        For q = 1 To 2
            For i = 1 To 3
                Dim y As Integer
                Dim x = y + 1
                y = x
                Console.Write(x)
                Console.Write(" ")
            Next
            Await Task.Yield()
        Next
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3 4 5 6")
        End Sub

        <Fact()>
        Public Sub HoistingUninitializerVars2()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Reflection

Module Form1
    Sub Main()
        RecursiveVar().Wait(60000)
    End Sub

    Async Function RecursiveVar() As Task
        For q = 1 To 2
            For i = 1 To 3
                Dim x As Integer = x + 1
                Console.Write(x)
                Console.Write(" ")
            Next
            Await Task.Yield()
        Next
    End Function
End Module
    </file>
</compilation>, useLatestFramework:=True, expectedOutput:="1 2 3 4 5 6")
        End Sub

        Private Sub EmittedSymbolsCheck(dbg As Boolean)
            Dim TypeNamePattern As New Regex("^VB\$StateMachine_(\d)+_(\w)+$", RegexOptions.Singleline)
            Dim FieldPattern As New Regex("^((\$Builder)|(\$Stack)|(\$State)|(\$VB\$Me)|(\$A(\d)+)|(\$S(\d)+)|(\$I(\d)+)|(\$VB\$ResumableLocal_\$(\d)+)|(\$U(\d)+))$", RegexOptions.Singleline)

            Dim FormatAttribute As Func(Of VisualBasicAttributeData, String) =
                Function(attr)
                    Dim result = attr.AttributeClass.ToDisplayString & "("

                    Dim first = True
                    For Each arg In attr.ConstructorArguments()
                        If first Then
                            first = False
                        Else
                            result &= ","
                        End If
                        result &= arg.Value.ToString()
                    Next

                    For Each arg In attr.NamedArguments
                        If first Then
                            first = False
                        Else
                            result &= ","
                        End If
                        result &= arg.Key
                        result &= "="
                        result &= arg.Value.ToString
                    Next

                    result &= ")"
                    Return result
                End Function

            Dim attributeValidator As Action(Of Symbol, String()) =
                Sub(symbol, attrs)
                    Assert.Equal(ArrayToSortedString(attrs),
                                 ArrayToSortedString((From a In symbol.GetAttributes() Select FormatAttribute(a)).ToArray()))
                End Sub

            Dim methodValidator As Action(Of MethodSymbol) =
                Sub(method)
                    Select Case method.Name
                        Case ".ctor"
                            ' This is an auto-generated constructor, ignore it

                        Case "System.Runtime.CompilerServices.IAsyncStateMachine.SetStateMachine"
                            Assert.Equal(Accessibility.Private, method.DeclaredAccessibility)
                            Assert.Equal(1, method.ExplicitInterfaceImplementations.Length)
                            Assert.Equal("Sub SetStateMachine(stateMachine As System.Runtime.CompilerServices.IAsyncStateMachine)", method.ExplicitInterfaceImplementations(0).ToDisplayString)
                            attributeValidator(method, If(dbg,
                                                          {"System.Diagnostics.DebuggerNonUserCodeAttribute()"},
                                                          {}))

                        Case "MoveNext"
                            Assert.Equal(Accessibility.Friend, method.DeclaredAccessibility)
                            Assert.Equal(1, method.ExplicitInterfaceImplementations.Length)
                            Assert.Equal("Sub MoveNext()", method.ExplicitInterfaceImplementations(0).ToDisplayString)
                            attributeValidator(method, {"System.Runtime.CompilerServices.CompilerGeneratedAttribute()"})

                        Case Else
                            Assert.True(False)
                    End Select
                End Sub

            Dim fieldValidator As Action(Of FieldSymbol) =
                Sub(field)
                    Assert.True(FieldPattern.IsMatch(field.Name))
                    ' TODO: $Builder and $State are public
                    'Assert.Equal(Accessibility.Internal, field.DeclaredAccessibility)
                    attributeValidator(field, {})
                End Sub

            Dim stateMachineValidator As Action(Of NamedTypeSymbol) =
                Sub(type)
                    Assert.Equal(Accessibility.Private, type.DeclaredAccessibility)
                    Assert.True(type.IsNotInheritable)
                    Assert.Equal(1, type.Interfaces.Length)
                    Assert.Equal("System.Runtime.CompilerServices.IAsyncStateMachine", type.Interfaces(0).ToDisplayString())
                    attributeValidator(type, {"System.Runtime.CompilerServices.CompilerGeneratedAttribute()"})

                    Dim processed As New HashSet(Of String)
                    For Each member In type.GetMembers()
                        Dim added = processed.Add(member.Name)
                        Debug.Assert(added)

                        Select Case member.Kind
                            Case SymbolKind.Method
                                methodValidator(DirectCast(member, MethodSymbol))

                            Case SymbolKind.Field
                                fieldValidator(DirectCast(member, FieldSymbol))

                            Case Else
                                Assert.True(False)
                        End Select
                    Next
                End Sub

            Dim moduleValidator As Action(Of ModuleSymbol) =
                Sub([module])
                    Dim testCaseType As NamedTypeSymbol = [module].ContainingAssembly.GetTypeByMetadataName("TestCase")
                    Assert.NotNull(testCaseType)

                    Dim runMethod = testCaseType.GetMember(Of MethodSymbol)("Run")
                    Assert.NotNull(runMethod)

                    If dbg Then
                        attributeValidator(runMethod, {"System.Diagnostics.DebuggerStepThroughAttribute()",
                                                       "System.Runtime.CompilerServices.AsyncStateMachineAttribute(TestCase.VB$StateMachine_4_Run)"})
                    Else
                        attributeValidator(runMethod, {"System.Runtime.CompilerServices.AsyncStateMachineAttribute(TestCase.VB$StateMachine_4_Run)"})
                    End If

                    Dim f2Method = testCaseType.GetMember(Of MethodSymbol)("F2")
                    Assert.NotNull(f2Method)

                    If dbg Then
                        attributeValidator(f2Method, {"System.Diagnostics.DebuggerStepThroughAttribute()",
                                                      "System.Runtime.CompilerServices.AsyncStateMachineAttribute(TestCase.VB$StateMachine_2_F2)"})
                    Else
                        attributeValidator(f2Method, {"System.Runtime.CompilerServices.AsyncStateMachineAttribute(TestCase.VB$StateMachine_2_F2)"})
                    End If

                    For Each nestedType In testCaseType.GetTypeMembers()
                        Assert.True(TypeNamePattern.IsMatch(nestedType.Name))
                        stateMachineValidator(nestedType)
                    Next
                End Sub

            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading
Imports System.Threading.Tasks

Public Class TestCase
    Public field As Integer

    Async Function F2() As Task(Of Integer)
        Await Task.Yield
        Return 1
    End Function

    Function FFF(x As Integer, ByRef y As Double, z As Integer) As Integer
        Return x + y + z
    End Function

    Public Async Function Run() As Task(Of Integer)
        Dim x = 1
        x += Await F2()
        Dim y = 2
        Return FFF(y + x, Me.field, Await F2())
    End Function
End Class
    </file>
</compilation>, options:=If(dbg, TestOptions.DebugDll, TestOptions.ReleaseDll),
                useLatestFramework:=True,
                symbolValidator:=moduleValidator)
        End Sub

        <Fact, WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        Public Sub EmittedSymbolsCheck_Debug()
            EmittedSymbolsCheck(True)
        End Sub

        <Fact>
        Public Sub EmittedSymbolsCheck_Release()
            EmittedSymbolsCheck(False)
        End Sub

        <WorkItem(840843, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/840843")>
        <Fact>
        Public Sub MissingAsyncMethodBuilder()
            Dim source = <compilation name="Async">
                             <file name="a.vb">
Public Class TestCase
    Async Sub M()
    End Sub
End Class
    </file>
                         </compilation>

            Dim comp = CreateCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseDll) ' NOTE: 4.0, Not 4.5, so it's missing the async helpers.

            Using stream As New MemoryStream()
                AssertTheseDiagnostics(comp.Emit(stream).Diagnostics, <errors><![CDATA[
BC31091: Import of type 'AsyncVoidMethodBuilder' from assembly or module 'Async.dll' failed.
    Async Sub M()
    ~~~~~~~~~~~~~~
BC31091: Import of type 'AsyncVoidMethodBuilder' from assembly or module 'Async.dll' failed.
    Async Sub M()
    ~~~~~~~~~~~~~~
BC31091: Import of type 'IAsyncStateMachine' from assembly or module 'Async.dll' failed.
    Async Sub M()
    ~~~~~~~~~~~~~~
BC35000: Requested operation is not available because the runtime library function 'System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext' is not defined.
    Async Sub M()
    ~~~~~~~~~~~~~~
BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
    Async Sub M()
              ~
]]></errors>)
            End Using
        End Sub

        <WorkItem(863, "https://github.com/dotnet/roslyn/issues/863")>
        <Fact()>
        Public Sub CatchInIteratorStateMachine()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections
Class C
    Shared Function F() As Object
        Throw New ArgumentException("Value does not fall within the expected range.")
    End Function
    Shared Iterator Function M() As IEnumerable
        Dim o As Object
        Try
            o = F()
        Catch e As Exception
            o = e
        End Try
        Yield o
    End Function
    Shared Sub Main()
        For Each e As Exception in M()
            ' Cannot just call .ToString() on the exception, because the exact format of a stack trace depends on a localization
            Console.WriteLine($"{e.GetType()}: {e.Message}")
            For Each frame In New Diagnostics.StackTrace(e).GetFrames()
                Dim m = frame.GetMethod()
                Console.WriteLine($"   at {m.DeclaringType.FullName.Replace("+"c, "."c)}.{m.Name}({String.Join(",", DirectCast(m.GetParameters(), Object()))})")
            Next
        Next
    End Sub
End Class
    </file>
</compilation>,
                options:=TestOptions.DebugExe,
                useLatestFramework:=True,
                expectedOutput:=
"System.ArgumentException: Value does not fall within the expected range.
   at C.F()
   at C.VB$StateMachine_2_M.MoveNext()")
        End Sub

        <WorkItem(863, "https://github.com/dotnet/roslyn/issues/863")>
        <Fact()>
        Public Sub CatchInAsyncStateMachine()
            CompileAndVerify(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks
Class C
    Shared Function F() As Object
        Throw New ArgumentException("Value does not fall within the expected range.")
    End Function
    Shared Async Function M() As Task(Of Object)
        Dim o As Object
        Try
            o = F()
        Catch e As Exception
            o = e
        End Try
        Return o
    End Function
    Shared Sub Main()
        Dim e = DirectCast(M().Result, Exception)
        ' Cannot just call .ToString() on the exception, because the exact format of a stack trace depends on a localization
        Console.WriteLine($"{e.GetType()}: {e.Message}")
        For Each frame In New Diagnostics.StackTrace(e).GetFrames()
            Dim m = frame.GetMethod()
            Console.WriteLine($"   at {m.DeclaringType.FullName.Replace("+"c, "."c)}.{m.Name}({String.Join(",", DirectCast(m.GetParameters(), Object()))})")
        Next
    End Sub
End Class
    </file>
</compilation>,
                options:=TestOptions.DebugExe,
                useLatestFramework:=True,
                expectedOutput:=
"System.ArgumentException: Value does not fall within the expected range.
   at C.F()
   at C.VB$StateMachine_2_M.MoveNext()")
        End Sub

        <Fact, WorkItem(1942, "https://github.com/dotnet/roslyn/issues/1942")>
        Public Sub HoistStructure()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Structure TestStruct
    Public i As Long
    Public j As Long
End Structure

Class Program
    Shared Async Function TestAsync() As Task
        Dim t As TestStruct
        t.i = 12
        Console.WriteLine("Before {0}", t.i)
        Await Task.Delay(100)
        Console.WriteLine("After {0}", t.i)
    End Function

    Shared Sub Main()
        TestAsync().Wait()
    End Sub
End Class
    </file>
</compilation>

            Dim expectedOutput = <![CDATA[Before 12
After 12]]>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput)

            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:=expectedOutput)
        End Sub

        <Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")>
        Public Sub HoistUsing001()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            Console.WriteLine("disposed")
        End Sub
    End Class

    Sub Main()
        Console.WriteLine(Test.Result)
    End Sub

    Private Async Function Test() As Task(Of String)
        Console.WriteLine("Pre")

        Using window = New D
            Console.WriteLine("show")

            For index = 1 To 2
                Await Task.Delay(100)
            Next
        End Using

        Console.WriteLine("Post")
        Return "result"
    End Function
End Module

    </file>
</compilation>

            Dim expectedOutput = <![CDATA[Pre
show
disposed
Post
result]]>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput)
            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:=expectedOutput)
        End Sub

        <Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")>
        Public Sub HoistUsing002()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            Console.WriteLine("disposed")
        End Sub
    End Class

    Sub Main()
        Console.WriteLine(Test.Result)
    End Sub

    Private Async Function Test() As Task(Of String)
        Console.WriteLine("Pre")

        Dim window = New D
        Try
            Console.WriteLine("show")

            For index = 1 To 2
                Await Task.Delay(100)
            Next
        Finally
            window.Dispose()
        End Try

        Console.WriteLine("Post")
        Return "result"
    End Function
End Module

    </file>
</compilation>

            Dim expectedOutput = <![CDATA[Pre
show
disposed
Post
result]]>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput)
            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:=expectedOutput)
        End Sub

        <Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")>
        Public Sub HoistUsing003()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            Console.WriteLine("disposed")
        End Sub
    End Class

    Sub Main()
        Console.WriteLine(Test.Result)
    End Sub

    Private Async Function Test() As Task(Of String)
        Console.WriteLine("Pre")

        Dim window as D
        Try
            window = New D

            Console.WriteLine("show")

            For index = 1 To 2
                Await Task.Delay(100)
            Next
        Finally
            window.Dispose()
        End Try

        Console.WriteLine("Post")
        Return "result"
    End Function
End Module

    </file>
</compilation>

            Dim expectedOutput = <![CDATA[Pre
show
disposed
Post
result]]>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput)
            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:=expectedOutput)
        End Sub

        <Fact, WorkItem(7669, "https://github.com/dotnet/roslyn/issues/7669")>
        Public Sub HoistUsing004()
            Dim source =
<compilation name="Async">
    <file name="a.vb">
Imports System
Imports System.Threading.Tasks

Module Module1
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
            Console.WriteLine("disposed")
        End Sub
    End Class

    Sub Main()
        Console.WriteLine(Test.Result)
    End Sub

    Private Async Function Test() As Task(Of String)
        Console.WriteLine("Pre")

        Using window1 = New D
            Console.WriteLine("show")

            Using window = New D
                Console.WriteLine("show")

                For index = 1 To 2
                    Await Task.Delay(100)
                Next
            End Using
        End Using

        Console.WriteLine("Post")
        Return "result"
    End Function
End Module

    </file>
</compilation>

            Dim expectedOutput = <![CDATA[Pre
show
show
disposed
disposed
Post
result]]>

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(source, references:=LatestVbReferences, options:=TestOptions.DebugExe)
            CompileAndVerify(compilation, expectedOutput:=expectedOutput)
            CompileAndVerify(compilation.WithOptions(TestOptions.ReleaseExe), expectedOutput:=expectedOutput)
        End Sub

    End Class
End Namespace

