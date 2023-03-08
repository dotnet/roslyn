' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class HoistedStateMachineLocalTests
        Inherits ExpressionCompilerTestBase

        Private Const s_asyncLambdaSourceTemplate = "
Imports System
Imports System.Threading.Tasks

Public Class D
    Private t1 As Double

    Public {0} Sub M(u1 As Char)
        Dim x = 1
        dim f = async function(ch As Char) {1}
    End Sub
End Class
"

        Private Const s_genericAsyncLambdaSourceTemplate = "
Imports System
Imports System.Threading.Tasks

Public Class D(Of T)
    Private t1 As T

    Public {0} Sub M(Of U)(u1 As U)
        Dim x = 1
        dim f = async function(ch As Char) {1}
    End Sub
End Class
"

        <Fact>
        Public Sub Iterator()
            Const source = "
Imports System.Collections.Generic

Class C
    Shared Iterator Function M() As IEnumerable(Of Integer)
#ExternalSource(""Test"", 500)
        DummySequencePoint()
#End ExternalSource

        If True Then
#ExternalSource(""Test"", 550)
            Dim x = 0
#End ExternalSource
            Yield x
#ExternalSource(""Test"", 600)
            x += 1
#End ExternalSource
        End If

#ExternalSource(""Test"", 650)
        DummySequencePoint()
#End ExternalSource

        If True Then
#ExternalSource(""Test"", 700)
            Dim x = 0
#End ExternalSource
            Yield x
#ExternalSource(""Test"", 750)
            x += 1
#End ExternalSource
        End If

#ExternalSource(""Test"", 800)
        DummySequencePoint()
#End ExternalSource
    End Function

    Shared Sub DummySequencePoint()
    End Sub
End Class
"
            Const expectedErrorMessage = "error BC30451: 'x' is not declared. It may be inaccessible due to its protection level."

            Const expectedIlTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2,
                Boolean V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_M.{0} As Integer""
  IL_0006:  ret
}}
"
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context As EvaluationContext
                    Dim testData As CompilationTestData
                    Dim errorMessage As String = Nothing

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=500)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=550)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$0"))

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=600)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$0"))

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=650)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=700)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$1"))

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=750)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$1"))

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=800)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)
                End Sub)
        End Sub

        <Fact>
        Public Sub Async()
            Const source = "
Imports System.Threading.Tasks

Class C
    Shared Async Function M() As Task
#ExternalSource(""Test"", 500)
        DummySequencePoint()
#End ExternalSource

        If True Then
#ExternalSource(""Test"", 550)
            Dim x = 0
#End ExternalSource
            Await M()
#ExternalSource(""Test"", 600)
            x += 1
#End ExternalSource
        End If

#ExternalSource(""Test"", 650)
        DummySequencePoint()
#End ExternalSource

        If True Then
#ExternalSource(""Test"", 700)
            Dim x = 0
#End ExternalSource
            Await M()
#ExternalSource(""Test"", 750)
            x += 1
#End ExternalSource
        End If

#ExternalSource(""Test"", 800)
        DummySequencePoint()
#End ExternalSource
    End Function

    Shared Sub DummySequencePoint()
    End Sub
End Class
"
            Const expectedErrorMessage = "error BC30451: 'x' is not declared. It may be inaccessible due to its protection level."

            Const expectedIlTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Boolean V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                C.VB$StateMachine_1_M V_3,
                Boolean V_4,
                System.Runtime.CompilerServices.TaskAwaiter V_5,
                System.Exception V_6)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_M.{0} As Integer""
  IL_0006:  ret
}}
"
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim context As EvaluationContext
                    Dim testData As CompilationTestData
                    Dim errorMessage As String = Nothing

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=500)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=550)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$0"))

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=600)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$0"))

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=650)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=700)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$1"))

                    testData = New CompilationTestData()
                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=750)
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$1"))

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=800)
                    context.CompileExpression("x", errorMessage)
                    Assert.Equal(expectedErrorMessage, errorMessage)
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101888")>
        Public Sub Repro1101888()
            Const source = "
Imports System.Collections.Generic

Class C
    Shared Iterator Function M() As IEnumerable(Of Integer)
        If True Then
            Dim x = 0
            Dim y = 0
            Dim z = 0
            Dim w = 0
            Yield x
#ExternalSource(""Test"", 600)
            x += 1
            y += 1
            z += 1
            w += 1
#End ExternalSource
        End If
    End Function
End Class
"
            Const expectedIlTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_M.{0} As Integer""
  IL_0006:  ret
}}
"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim context As EvaluationContext
                    Dim testData As CompilationTestData
                    Dim errorMessage As String = Nothing

                    context = CreateMethodContext(runtime, "C.VB$StateMachine_1_M.MoveNext", atLineNumber:=600)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_x$0"))

                    testData = New CompilationTestData()
                    context.CompileExpression("y", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_y$1"))

                    testData = New CompilationTestData()
                    context.CompileExpression("z", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_z$2"))

                    testData = New CompilationTestData()
                    context.CompileExpression("w", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(String.Format(expectedIlTemplate, "$VB$ResumableLocal_w$3"))
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureNothing()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "1")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__.VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureLocal()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "x")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch", "x")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureParameter()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "u1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("u1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Local_u1 As Char""
  IL_000b:  ret
}
")

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch", "u1")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureLambdaParameter()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "ch.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__.VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureThis()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "t1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("t1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.VB$StateMachine___Lambda$__2-0.$VB$Me As D""
  IL_0006:  ldfld      ""D.t1 As Double""
  IL_000b:  ret
}
")

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D.VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "Me", "ch")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Instance_CaptureThisAndLocal()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "", "x + t1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("t1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Me As D""
  IL_000b:  ldfld      ""D.t1 As Double""
  IL_0010:  ret
}
")

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "Me", "ch", "x")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Static_CaptureNothing()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "Shared", "1")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__.VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Static_CaptureLocal()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "Shared", "x")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch", "x")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Static_CaptureParameter()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "Shared", "u1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    Dim testData = New CompilationTestData()
                    context.CompileExpression("u1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D._Closure$__2-0""
  IL_0006:  ldfld      ""D._Closure$__2-0.$VB$Local_u1 As Char""
  IL_000b:  ret
}
")

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__2-0.VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch", "u1")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub AsyncLambda_Static_CaptureLambdaParameter()
            Dim source = String.Format(s_asyncLambdaSourceTemplate, "Shared", "ch.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D._Closure$__.VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")
                    AssertEx.SetEqual(GetLocalNames(context), "ch")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureNothing()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "1")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2(Of $CLS0).VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureLocal()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "x")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "x", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureParameter()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "u1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("u1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Local_u1 As $CLS0""
  IL_000b:  ret
}
")

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "u1", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureLambdaParameter()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "ch.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30043: 't1' is valid only within an instance method.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2(Of $CLS0).VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureThis()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "t1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("t1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T).VB$StateMachine___Lambda$__2-0(Of $CLS0).$VB$Me As D(Of T)""
  IL_0006:  ldfld      ""D(Of T).t1 As T""
  IL_000b:  ret
}
")

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T).VB$StateMachine___Lambda$__2-0(Of $CLS0).$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "Me", "ch", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Instance_CaptureThisAndLocal()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "", "x + t1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("t1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Me As D(Of T)""
  IL_000b:  ldfld      ""D(Of T).t1 As T""
  IL_0010:  ret
}
")

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "Me", "ch", "x", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Static_CaptureNothing()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "Shared", "1")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2(Of $CLS0).VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Static_CaptureLocal()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "Shared", "x")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Local_x As Integer""
  IL_000b:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "x", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Static_CaptureParameter()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "Shared", "u1.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2-0.VB$StateMachine___Lambda$__0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("u1", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$NonLocal__Closure$__2-0 As D(Of T)._Closure$__2-0(Of $CLS0)""
  IL_0006:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).$VB$Local_u1 As $CLS0""
  IL_000b:  ret
}
")

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2-0(Of $CLS0).VB$StateMachine___Lambda$__0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "u1", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        Public Sub GenericAsyncLambda_Static_CaptureLambdaParameter()
            Dim source = String.Format(s_genericAsyncLambdaSourceTemplate, "Shared", "ch.GetHashCode()")
            Dim comp = CreateCompilation(source)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "D._Closure$__2.VB$StateMachine___Lambda$__2-0.MoveNext")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing

                    context.CompileExpression("t1", errorMessage)
                    Assert.Equal("error BC30369: Cannot refer to an instance member of a class from within a shared method or shared member initializer without an explicit instance of the class.", errorMessage)

                    context.CompileExpression("u1", errorMessage)
                    Assert.Equal("error BC30451: 'u1' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    context.CompileExpression("x", errorMessage)
                    Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    testData = New CompilationTestData()
                    context.CompileExpression("ch", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""D(Of T)._Closure$__2(Of $CLS0).VB$StateMachine___Lambda$__2-0.$VB$Local_ch As Char""
  IL_0006:  ret
}
")

                    context.CompileExpression("GetType(T)", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("GetType(U)", errorMessage)
                    Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage) ' As in Dev12.

                    AssertEx.SetEqual(GetLocalNames(context), "ch", "<>TypeVariables")
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1134746")>
        Public Sub CacheInvalidation()
            Const source = "
Imports System.Collections.Generic

Class C
    Shared Iterator Function M() As IEnumerable(Of Integer)
#ExternalSource(""Test"", 100)
        Dim x As Integer = 1
        Yield x
#End ExternalSource

        If True Then
#ExternalSource(""Test"", 200)
            Dim y As Integer = x + 1
            Yield y
#End ExternalSource
        End If
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
                    Dim moduleVersionId As Guid = Nothing
                    Dim symReader As ISymUnmanagedReader = Nothing
                    Dim methodToken = 0
                    Dim localSignatureToken = 0
                    GetContextState(runtime, "C.VB$StateMachine_1_M.MoveNext", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
                    Const methodVersion = 1

                    Dim appDomain = New AppDomain()
                    Dim ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber:=100)
                    Dim context = CreateMethodContext(
                        appDomain,
                        blocks,
                        MakeDummyLazyAssemblyReaders(),
                        symReader,
                        moduleVersionId,
                        methodToken,
                        methodVersion,
                        ilOffset,
                        localSignatureToken,
                        MakeAssemblyReferencesKind.AllAssemblies)

                    Dim errorMessage As String = Nothing
                    context.CompileExpression("x", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("y", errorMessage)
                    Assert.Equal("error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.", errorMessage)

                    ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader, atLineNumber:=200)
                    context = CreateMethodContext(
                        appDomain,
                        blocks,
                        MakeDummyLazyAssemblyReaders(),
                        symReader,
                        moduleVersionId,
                        methodToken,
                        methodVersion,
                        ilOffset,
                        localSignatureToken,
                        MakeAssemblyReferencesKind.AllAssemblies)

                    context.CompileExpression("x", errorMessage)
                    Assert.Null(errorMessage)
                    context.CompileExpression("y", errorMessage)
                    Assert.Null(errorMessage)
                End Sub)
        End Sub

        Private Shared Function GetLocalNames(context As EvaluationContext) As String()
            Dim unused As String = Nothing
            Dim locals = New ArrayBuilder(Of LocalAndMethod)()
            context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=unused, testData:=New CompilationTestData())
            Return locals.Select(Function(l) l.LocalName).ToArray()
        End Function

        Private Shared Function CreateCompilation(source As String) As VisualBasicCompilation
            Return CreateEmptyCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                options:=TestOptions.DebugDll,
                assemblyName:=GetUniqueName())
        End Function

    End Class
End Namespace
