' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class CompileExpressionsTests
        Inherits ExpressionCompilerTestBase

        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=482753")>
        Public Sub LocalsInAsync()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Shared Function E(o As Object, p As Func(Of Object, Boolean)) As Task(Of Object)
        Throw New NotImplementedException()
    End Function
    Function F() As Object
        Throw New NotImplementedException()
    End Function
    Function G(o As Object) As Task(Of Object)
        Throw New NotImplementedException()
    End Function
    Async Function M(x As Object) As Task
        Dim z = Await E(F(), Function(y) x = y)
#ExternalSource(""Test"", 999)
        Await G(z)
#End ExternalSource
    End Function
End Class"
            ' Test with CompileExpression rather than CompileExpressions
            ' so field references in IL are named.
            ' Debug build.
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
                {VisualBasicSyntaxTree.ParseText(source)},
                options:=TestOptions.DebugDll,
                references:={SystemCoreRef})
            Dim testData As CompilationTestData
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_4_M.MoveNext()", atLineNumber:=999)
                    Dim errorMessage As String = Nothing
                    testData = New CompilationTestData()
                    Dim result = context.CompileExpression("If(z, x)", errorMessage, testData)
                    Assert.NotNull(result.Assembly)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_1,
                C.VB$StateMachine_4_M V_2,
                Object V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_4_M.$VB$ResumableLocal_z$1 As Object""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""C.VB$StateMachine_4_M.$VB$ResumableLocal_$VB$Closure_$0 As C._Closure$__4-0""
  IL_0010:  ldfld      ""C._Closure$__4-0.$VB$Local_x As Object""
  IL_0015:  ret
}")
                End Sub)
            ' Release build.
            comp = CreateCompilationWithMscorlib45AndVBRuntime(
                {VisualBasicSyntaxTree.ParseText(source)},
                options:=TestOptions.ReleaseDll,
                references:={SystemCoreRef})
            ' Note from MoveNext() below that local $VB$Closure_0 should not be
            ' used in the compiled expression to access the display class since that
            ' local is only set the first time through MoveNext() (see loc.1 below).
            testData = New CompilationTestData()
            comp.EmitToArray(testData:=testData)
            testData.GetMethodData("C.VB$StateMachine_4_M.MoveNext()").VerifyIL(
                "{
  // Code size      338 (0x152)
  .maxstack  3
  .locals init (Integer V_0,
                C._Closure$__4-0 V_1, //$VB$Closure_0
                Object V_2, //z
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_4_M.$State As Integer""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0076
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00e9
    IL_0011:  newobj     ""Sub C._Closure$__4-0..ctor()""
    IL_0016:  stloc.1
    IL_0017:  ldloc.1
    IL_0018:  ldarg.0
    IL_0019:  ldfld      ""C.VB$StateMachine_4_M.$VB$Local_x As Object""
    IL_001e:  stfld      ""C._Closure$__4-0.$VB$Local_x As Object""
    IL_0023:  ldarg.0
    IL_0024:  ldfld      ""C.VB$StateMachine_4_M.$VB$Me As C""
    IL_0029:  callvirt   ""Function C.F() As Object""
    IL_002e:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_0033:  ldloc.1
    IL_0034:  ldftn      ""Function C._Closure$__4-0._Lambda$__0(Object) As Boolean""
    IL_003a:  newobj     ""Sub System.Func(Of Object, Boolean)..ctor(Object, System.IntPtr)""
    IL_003f:  call       ""Function C.E(Object, System.Func(Of Object, Boolean)) As System.Threading.Tasks.Task(Of Object)""
    IL_0044:  callvirt   ""Function System.Threading.Tasks.Task(Of Object).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0049:  stloc.3
    IL_004a:  ldloca.s   V_3
    IL_004c:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Object).get_IsCompleted() As Boolean""
    IL_0051:  brtrue.s   IL_0092
    IL_0053:  ldarg.0
    IL_0054:  ldc.i4.0
    IL_0055:  dup
    IL_0056:  stloc.0
    IL_0057:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
    IL_005c:  ldarg.0
    IL_005d:  ldloc.3
    IL_005e:  stfld      ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0063:  ldarg.0
    IL_0064:  ldflda     ""C.VB$StateMachine_4_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_0069:  ldloca.s   V_3
    IL_006b:  ldarg.0
    IL_006c:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Object), C.VB$StateMachine_4_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Object), ByRef C.VB$StateMachine_4_M)""
    IL_0071:  leave      IL_0151
    IL_0076:  ldarg.0
    IL_0077:  ldc.i4.m1
    IL_0078:  dup
    IL_0079:  stloc.0
    IL_007a:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0085:  stloc.3
    IL_0086:  ldarg.0
    IL_0087:  ldflda     ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_008c:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0092:  ldloca.s   V_3
    IL_0094:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Object).GetResult() As Object""
    IL_0099:  ldloca.s   V_3
    IL_009b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_00a1:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_00a6:  stloc.2
    IL_00a7:  ldarg.0
    IL_00a8:  ldfld      ""C.VB$StateMachine_4_M.$VB$Me As C""
    IL_00ad:  ldloc.2
    IL_00ae:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
    IL_00b3:  callvirt   ""Function C.G(Object) As System.Threading.Tasks.Task(Of Object)""
    IL_00b8:  callvirt   ""Function System.Threading.Tasks.Task(Of Object).GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_00bd:  stloc.s    V_4
    IL_00bf:  ldloca.s   V_4
    IL_00c1:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Object).get_IsCompleted() As Boolean""
    IL_00c6:  brtrue.s   IL_0106
    IL_00c8:  ldarg.0
    IL_00c9:  ldc.i4.1
    IL_00ca:  dup
    IL_00cb:  stloc.0
    IL_00cc:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
    IL_00d1:  ldarg.0
    IL_00d2:  ldloc.s    V_4
    IL_00d4:  stfld      ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_00d9:  ldarg.0
    IL_00da:  ldflda     ""C.VB$StateMachine_4_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_00df:  ldloca.s   V_4
    IL_00e1:  ldarg.0
    IL_00e2:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted(Of System.Runtime.CompilerServices.TaskAwaiter(Of Object), C.VB$StateMachine_4_M)(ByRef System.Runtime.CompilerServices.TaskAwaiter(Of Object), ByRef C.VB$StateMachine_4_M)""
    IL_00e7:  leave.s    IL_0151
    IL_00e9:  ldarg.0
    IL_00ea:  ldc.i4.m1
    IL_00eb:  dup
    IL_00ec:  stloc.0
    IL_00ed:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
    IL_00f2:  ldarg.0
    IL_00f3:  ldfld      ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_00f8:  stloc.s    V_4
    IL_00fa:  ldarg.0
    IL_00fb:  ldflda     ""C.VB$StateMachine_4_M.$A0 As System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0100:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0106:  ldloca.s   V_4
    IL_0108:  call       ""Function System.Runtime.CompilerServices.TaskAwaiter(Of Object).GetResult() As Object""
    IL_010d:  pop
    IL_010e:  ldloca.s   V_4
    IL_0110:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter(Of Object)""
    IL_0116:  leave.s    IL_013c
  }
  catch System.Exception
  {
    IL_0118:  dup
    IL_0119:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_011e:  stloc.s    V_5
    IL_0120:  ldarg.0
    IL_0121:  ldc.i4.s   -2
    IL_0123:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
    IL_0128:  ldarg.0
    IL_0129:  ldflda     ""C.VB$StateMachine_4_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
    IL_012e:  ldloc.s    V_5
    IL_0130:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0135:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_013a:  leave.s    IL_0151
  }
  IL_013c:  ldarg.0
  IL_013d:  ldc.i4.s   -2
  IL_013f:  dup
  IL_0140:  stloc.0
  IL_0141:  stfld      ""C.VB$StateMachine_4_M.$State As Integer""
  IL_0146:  ldarg.0
  IL_0147:  ldflda     ""C.VB$StateMachine_4_M.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder""
  IL_014c:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0151:  ret
}")
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_4_M.MoveNext()", atLineNumber:=999)
                    Dim errorMessage As String = Nothing
                    testData = New CompilationTestData()
                    Dim result = context.CompileExpression("If(z, x)", errorMessage, testData)
                    Assert.NotNull(result.Assembly)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (Integer V_0,
                C._Closure$__4-0 V_1, //$VB$Closure_0
                Object V_2, //z
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_4,
                System.Exception V_5)
  IL_0000:  ldloc.2
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000b
  IL_0004:  pop
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""C.VB$StateMachine_4_M.$VB$Local_x As Object""
  IL_000b:  ret
}")
                End Sub)
        End Sub

    End Class

End Namespace
