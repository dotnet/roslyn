' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class LocalsTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub NoLocals()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.NotNull(assembly)
                    Assert.Equal(0, assembly.Count)
                    Assert.Equal(0, locals.Count)
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub Locals()
            Const source =
"Class C
    Sub M(a As Integer())
        Dim b As String
        a(1) += 1
        SyncLock New C()
#ExternalSource(""test"", 999)
            Dim c As Integer = 3
            b = a(c).ToString()
#End ExternalSource
        End SyncLock
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M", atLineNumber:=999)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.NotNull(assembly)
                    Assert.NotEqual(0, assembly.Count)
                    Assert.Equal(4, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0, //b
                Integer& V_1,
                Object V_2,
                Boolean V_3,
                Integer V_4) //c
  IL_0000:  ldarg.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "a", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0, //b
                Integer& V_1,
                Object V_2,
                Boolean V_3,
                Integer V_4) //c
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "b", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0, //b
                Integer& V_1,
                Object V_2,
                Boolean V_3,
                Integer V_4) //c
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "c", expectedILOpt:=
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (String V_0, //b
                Integer& V_1,
                Object V_2,
                Boolean V_3,
                Integer V_4) //c
  IL_0000:  ldloc.s    V_4
  IL_0002:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub LocalsAndPseudoVariables()
            Const source =
"Class C
    Sub M(o As Object)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim aliases = ImmutableArray.Create(
                ExceptionAlias(GetType(System.IO.IOException)),
                ReturnValueAlias(2, GetType(String)),
                ReturnValueAlias(),
                ObjectIdAlias(2, GetType(Boolean)),
                VariableAlias("o", "C"))
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim diagnostics = DiagnosticBag.GetInstance()

                    Dim testData = New CompilationTestData()
                    context.CompileGetLocals(
                        locals,
                        argumentsOnly:=True,
                        aliases:=aliases,
                        diagnostics:=diagnostics,
                        typeName:=typeName,
                        testData:=testData)
                    Assert.Equal(1, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "o")
                    locals.Clear()

                    testData = New CompilationTestData()
                    context.CompileGetLocals(
                        locals,
                        argumentsOnly:=False,
                        aliases:=aliases,
                        diagnostics:=diagnostics,
                        typeName:=typeName,
                        testData:=testData)
            Assert.Equal(6, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "$exception", "Error", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  ret
}")
                    ' $ReturnValue is suppressed since it always matches the last $ReturnValueN
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "$ReturnValue2", "Method M2 returned", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0006:  castclass  ""String""
  IL_000b:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "$2", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""$2""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  unbox.any  ""Boolean""
  IL_000f:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "o", expectedILOpt:=
"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""o""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""C""
  IL_000f:  ret
}")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "Me", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(5), "<>m5", "o", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    locals.Free()
                    
                    ' Confirm that the Watch window is unaffected by the filtering in the Locals window.
                    Dim errorString As String = Nothing
                    context.CompileExpression("$ReturnValue", DkmEvaluationFlags.TreatAsExpression, aliases, errorString)
                    Assert.Null(errorString)
                End Sub)
        End Sub

        ''' <summary>
        ''' No local signature (debugging a .dmp with no heap). Local
        ''' names are known but types are not so the locals are dropped.
        ''' Expressions that do not involve locals can be evaluated however.
        ''' </summary>
        <Fact>
        Public Sub NoLocalSignature()
            Const source =
"Class C
    Sub M(a As Integer())
        Dim b As String
        a(1) += 1
        SyncLock New C()
#ExternalSource(""test"", 999)
            Dim c As Integer = 3
            b = a(c).ToString()
#End ExternalSource
        End SyncLock
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp, references:=Nothing, includeLocalSignatures:=False, validator:=
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M", atLineNumber:=999)

                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "a", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    locals.Free()

                    Dim errorMessage As String = Nothing
                    testData = New CompilationTestData()
                    context.CompileExpression("b", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
                    Assert.Equal(errorMessage, "error BC30451: 'b' is not declared. It may be inaccessible due to its protection level.")

                    testData = New CompilationTestData()
                    context.CompileExpression("a(1)", errorMessage, testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  ldelem.i4
  IL_0003:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub [Me]()
            Const source = "
Class C
    Sub M([Me] As Object)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(2, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
")

                    ' Dev11 shows "Me" in the Locals window and "[Me]" in the Autos window.
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "[Me]", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
")

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub ArgumentsOnly()
            Const source = "
Class C
    Sub M(Of T)(x As T)
        Dim y As Object = x
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=True, typeName:=typeName, testData:=testData)

                    Assert.Equal(1, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0(Of T)", "x", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Object V_0) //y
  IL_0000:  ldarg.1
  IL_0001:  ret
}",
                        expectedGeneric:=True)

                    locals.Free()
                End Sub)
        End Sub

        ''' <summary>
        ''' Compiler-generated locals should be ignored.
        ''' </summary>
        <Fact>
        Public Sub CompilerGeneratedLocals()
            Const source = "
Class C
    Shared Function F(args As Object()) As Boolean
        If args Is Nothing Then
            Return True
        End If

        For Each o In args
#ExternalSource(""test"", 999)
            System.Console.WriteLine() ' Force non-hidden sequence point
#End ExternalSource
        Next

        DirectCast(Function() args(0), System.Func(Of Object))()
        Return False
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.F", atLineNumber:=999)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(3, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "args", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                Boolean V_1, //F
                Boolean V_2,
                Object() V_3,
                Integer V_4,
                Object V_5, //o
                Boolean V_6)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_args As Object()""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "F", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                Boolean V_1, //F
                Boolean V_2,
                Object() V_3,
                Integer V_4,
                Object V_5, //o
                Boolean V_6)
  IL_0000:  ldloc.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "o", expectedILOpt:=
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                Boolean V_1, //F
                Boolean V_2,
                Object() V_3,
                Integer V_4,
                Object V_5, //o
                Boolean V_6)
  IL_0000:  ldloc.s    V_5
  IL_0002:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub Constants()
            Const source = "
Class C
    Const x As Integer = 2

    Shared Function F(w As Integer) As Integer
#ExternalSource(""test"", 888)
        System.Console.WriteLine() ' Force non-hidden sequence point
#End ExternalSource
        Const y As Integer = 3
        Const v As Object = Nothing
        If v Is Nothing orelse w < 2 Then
            Const z As String = ""str""
#ExternalSource(""test"", 999)
            Dim u As String = z
            w += z.Length
#End ExternalSource
        End If
        Return w + x + y
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.F", atLineNumber:=888)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(4, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "w")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "F")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //F
                Boolean V_1,
                String V_2)
  IL_0000:  ldc.i4.3
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "v", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //F
                Boolean V_1,
                String V_2)
  IL_0000:  ldnull
  IL_0001:  ret
}
")

                    context = CreateMethodContext(runtime, methodName:="C.F", atLineNumber:=999) ' Changed this (was Nothing)
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(6, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "w")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "F")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "u")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult)
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "v", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult)
                    VerifyLocal(testData, typeName, locals(5), "<>m5", "z", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Integer V_0, //F
                Boolean V_1,
                String V_2) //u
  IL_0000:  ldstr      ""str""
  IL_0005:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub ConstantEnum()
            Const source =
"Enum E
    A
    B
End Enum
Class C
    Shared Sub M(x As E)
        Const y = E.B
    End Sub
    Shared Sub Main()
        M(E.A)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugExe)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)

                    Dim method = DirectCast(testData.GetMethodData("<>x.<>m0").Method, MethodSymbol)
                    Assert.Equal(method.Parameters(0).Type, method.ReturnType)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}")

                    method = DirectCast(testData.GetMethodData("<>x.<>m1").Method, MethodSymbol)
                    Assert.Equal(method.Parameters(0).Type, method.ReturnType)

                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub ConstantEnumAndTypeParameter()
            Const source =
"Class C(Of T)
    Enum E
        A
    End Enum
    Friend Shared Sub M(Of U As T)()
        Const x As C(Of T).E = E.A
        Const y As C(Of U).E = Nothing
    End Sub
End Class
Class P
    Shared Sub Main()
        C(Of Object).M(Of String)()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugExe)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, "<>x(Of T)", locals(0), "<>m0(Of U)", "x", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedGeneric:=True, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, "<>x(Of T)", locals(1), "<>m1(Of U)", "y", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedGeneric:=True, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, "<>x(Of T)", locals(2), "<>m2(Of U)", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedGeneric:=True, expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  newobj     ""Sub <>c__TypeVariables(Of T, U)..ctor()""
  IL_0005:  ret
}")
                    Assert.Equal(3, locals.Count)
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub CapturedLocalsOutsideLambda()
            Const source = "
Imports System

Class C
    Shared Sub F(f As Func(Of Object))
    End Sub
    
    Sub M(x As C)
        Dim y As New C()
        F(Function() If(x, If(y, Me)))
        If x IsNot Nothing
#ExternalSource(""test"", 999)
            Dim z = 6
            Dim w = 7
            F(Function() If(y, DirectCast(w, Object)))
#End ExternalSource
        End If
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.M", atLineNumber:=999)
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(5, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Boolean V_1,
                C._Closure$__2-1 V_2, //$VB$Closure_1
                Integer V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Me As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "x", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Boolean V_1,
                C._Closure$__2-1 V_2, //$VB$Closure_1
                Integer V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Local_x As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "z", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Boolean V_1,
                C._Closure$__2-1 V_2, //$VB$Closure_1
                Integer V_3) //z
  IL_0000:  ldloc.3
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Boolean V_1,
                C._Closure$__2-1 V_2, //$VB$Closure_1
                Integer V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Local_y As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "w", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Boolean V_1,
                C._Closure$__2-1 V_2, //$VB$Closure_1
                Integer V_3) //z
  IL_0000:  ldloc.2
  IL_0001:  ldfld      ""C._Closure$__2-1.$VB$Local_w As Integer""
  IL_0006:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub CapturedLocalsInsideLambda()
            Const source = "
Imports System

Class C
    Shared Sub F(ff As Func(Of Object, Object))
        ff(Nothing)
    End Sub

    Sub M()
        Dim x As New Object()
        F(Function(_1)
            Dim y As New Object()
            F(Function(_2) y)
            Return If(x, Me)
          End Function)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C._Closure$__2-0._Lambda$__1")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 2)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "_2", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Local_y As Object""
  IL_0006:  ret
}")

                    context = CreateMethodContext(runtime, methodName:="C._Closure$__2-1._Lambda$__0")
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 4)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__2-1.$VB$Me As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "_1", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Object V_1)
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Object V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Local_y As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "x", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                Object V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__2-1.$VB$Local_x As Object""
  IL_0006:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub NestedLambdas()
            Const source = "
Imports System

Class C
    Shared Sub Main()
        Dim f As Func(Of Object, Object, Object, Object, Func(Of Object, Object, Object, Func(Of Object, Object, Func(Of Object, Object)))) =
            Function(x1, x2, x3, x4)
                If x1 Is Nothing Then Return Nothing
                Return Function(y1, y2, y3)
                           If If(y1, x2) Is Nothing Then Return Nothing
                           Return Function(z1, z2)
                                      If If(z1, If(y2, x3)) Is Nothing Then Return Nothing
                                      Return Function(w1)
                                                 If If(z2, If(y3, x4)) Is Nothing Then Return Nothing
                                                 Return w1
                                             End Function
                                  End Function
                       End Function
            End Function
        f(1, 2, 3, 4)(5, 6, 7)(8, 9)(10)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C._Closure$__._Lambda$__1-0")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 4)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x2", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                System.Func(Of Object, Object, Object, System.Func(Of Object, Object, System.Func(Of Object, Object))) V_1,
                Boolean V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_x2 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "x3")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "x4")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "x1")

                    context = CreateMethodContext(runtime, methodName:="C._Closure$__1-0._Lambda$__1")
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 6)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "y2", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__1-1 V_0, //$VB$Closure_0
                System.Func(Of Object, Object, System.Func(Of Object, Object)) V_1,
                Boolean V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__1-1.$VB$Local_y2 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y3")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y1")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "x2")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "x3", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__1-1 V_0, //$VB$Closure_0
                System.Func(Of Object, Object, System.Func(Of Object, Object)) V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_x3 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(5), "<>m5", "x4")

                    context = CreateMethodContext(runtime, methodName:="C._Closure$__1-1._Lambda$__2")
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 7)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "z2", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C._Closure$__1-2 V_0, //$VB$Closure_0
                System.Func(Of Object, Object) V_1,
                Boolean V_2)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C._Closure$__1-2.$VB$Local_z2 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "z1")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y2")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y3")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "x2")
                    VerifyLocal(testData, typeName, locals(5), "<>m5", "x3", expectedILOpt:=
"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (C._Closure$__1-2 V_0, //$VB$Closure_0
                System.Func(Of Object, Object) V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As C._Closure$__1-0""
  IL_0006:  ldfld      ""C._Closure$__1-0.$VB$Local_x3 As Object""
  IL_000b:  ret
}")
                    VerifyLocal(testData, typeName, locals(6), "<>m6", "x4")

                    context = CreateMethodContext(runtime, methodName:="C._Closure$__1-2._Lambda$__3")
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(locals.Count, 7)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "w1")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "z2", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0,
                Boolean V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-2.$VB$Local_z2 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y2")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y3")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "x2")
                    VerifyLocal(testData, typeName, locals(5), "<>m5", "x3")
                    VerifyLocal(testData, typeName, locals(6), "<>m6", "x4", expectedILOpt:=
"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (Object V_0,
                Boolean V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-2.$VB$NonLocal_$VB$Closure_3 As C._Closure$__1-1""
  IL_0006:  ldfld      ""C._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As C._Closure$__1-0""
  IL_000b:  ldfld      ""C._Closure$__1-0.$VB$Local_x4 As Object""
  IL_0010:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        ''' <summary>
        ''' Should not include "Me" inside display class instance method if
        ''' "Me" is not captured.
        ''' </summary>
        <Fact>
        Public Sub NoMeInsideDisplayClassInstanceMethod()
            Const source = "
Imports System

Class C
    Sub M(Of T As Class)(x As T)
        Dim f As Func(Of Object, Func(Of T, Object)) = Function(y) _
            Function(z)
                return If(x, If(DirectCast(y, Object), z))
            End Function
        f(2)(x)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C._Closure$__1-0._Lambda$__0")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(3, locals.Count)
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(0), "<>m0", "y")
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(1), "<>m1", "x")
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(2), "<>m2", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult)

                    context = CreateMethodContext(runtime, methodName:="C._Closure$__1-1._Lambda$__1")
                    testData = New CompilationTestData()
                    locals.Clear()
                    typeName = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(4, locals.Count)
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(0), "<>m0", "z")
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(1), "<>m1", "y")
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(2), "<>m2", "x")
                    VerifyLocal(testData, "<>x(Of $CLS0)", locals(3), "<>m3", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult)

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub GenericMethod()
            Const source = "
Class A (Of T)
    Structure B(Of U, V)
        Sub M(Of W)(o As A(Of U).B(Of V, Object)())
            Dim t1 As T = Nothing
            Dim u1 As U = Nothing
            Dim w1 As W = Nothing
        End Sub
    End Structure
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="A.B.M")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(6, locals.Count)

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(0), "<>m0(Of W)", "Me", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""A(Of T).B(Of U, V)""
  IL_0006:  ret
}",
                        expectedGeneric:=True)

                    Dim method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m0(Of W)").Method, MethodSymbol)

                    Dim containingType = method.ContainingType
                    Dim containingTypeTypeParameters = containingType.TypeParameters
                    Dim typeParameterT As TypeParameterSymbol = containingTypeTypeParameters(0)
                    Dim typeParameterU As TypeParameterSymbol = containingTypeTypeParameters(1)
                    Dim typeParameterV As TypeParameterSymbol = containingTypeTypeParameters(2)

                    Dim returnType = DirectCast(method.ReturnType, NamedTypeSymbol)
                    Assert.Equal(typeParameterU, returnType.TypeArguments(0))
                    Assert.Equal(typeParameterV, returnType.TypeArguments(1))
                    returnType = returnType.ContainingType
                    Assert.Equal(typeParameterT, returnType.TypeArguments(0))

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(1), "<>m1(Of W)", "o", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  ldarg.1
  IL_0001:  ret
}",
                        expectedGeneric:=True)

                    method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m1(Of W)").Method, MethodSymbol)
                    ' method.ReturnType: A(Of U).B(Of V, object)()
                    returnType = DirectCast(DirectCast(method.ReturnType, ArrayTypeSymbol).ElementType, NamedTypeSymbol)
                    Assert.Equal(typeParameterV, returnType.TypeArguments(0))
                    returnType = returnType.ContainingType
                    Assert.Equal(typeParameterU, returnType.TypeArguments(0))

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(2), "<>m2(Of W)", "t1", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  ldloc.0
  IL_0001:  ret
}",
                        expectedGeneric:=True)

                    method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m2(Of W)").Method, MethodSymbol)
                    Assert.Equal(typeParameterT, method.ReturnType)

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(3), "<>m3(Of W)", "u1", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  ldloc.1
  IL_0001:  ret
}",
                        expectedGeneric:=True)

                    method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m3(Of W)").Method, MethodSymbol)
                    Assert.Equal(typeParameterU, method.ReturnType)

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(4), "<>m4(Of W)", "w1", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  ldloc.2
  IL_0001:  ret
}",
                        expectedGeneric:=True)

                    method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m4(Of W)").Method, MethodSymbol)
                    Assert.Equal(method.TypeParameters.Single(), method.ReturnType)

                    VerifyLocal(testData, "<>x(Of T, U, V)", locals(5), "<>m5(Of W)", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (T V_0, //t1
                U V_1, //u1
                W V_2) //w1
  IL_0000:  newobj     ""Sub <>c__TypeVariables(Of T, U, V, W)..ctor()""
  IL_0005:  ret
}",
                        expectedGeneric:=True)

                    method = DirectCast(testData.GetMethodData("<>x(Of T, U, V).<>m5(Of W)").Method, MethodSymbol)
                    returnType = DirectCast(method.ReturnType, NamedTypeSymbol)
                    Assert.Equal(typeParameterT, returnType.TypeArguments(0))
                    Assert.Equal(typeParameterU, returnType.TypeArguments(1))
                    Assert.Equal(typeParameterV, returnType.TypeArguments(2))
                    Assert.Equal(method.TypeParameters.Single(), returnType.TypeArguments(3))

                    ' Verify <>c__TypeVariables types was emitted (#976772)
                    Using metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(assembly))
                        Dim reader = metadata.MetadataReader
                        Dim typeDef = reader.GetTypeDef("<>c__TypeVariables")
                        reader.CheckTypeParameters(typeDef.GetGenericParameters(), "T", "U", "V", "W")
                    End Using

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub GenericLambda()
            Const source = "
Imports System

Class C(Of T As Class)
    Shared Sub M(Of U)(t1 As T)
        Dim u1 As U = Nothing
        Dim f As Func(Of Object) = Function() If(t1, DirectCast(u1, Object))
        f()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C._Closure$__1-0._Lambda$__0")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(3, locals.Count)

                    ' NOTE: $CLS0 does not appear in the UI.
                    VerifyLocal(testData, "<>x(Of T, $CLS0)", locals(0), "<>m0", "t1")
                    VerifyLocal(testData, "<>x(Of T, $CLS0)", locals(1), "<>m1", "u1", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T)._Closure$__1-0(Of $CLS0).$VB$Local_u1 As $CLS0""
  IL_0006:  ret
}")
                    VerifyLocal(testData, "<>x(Of T, $CLS0)", locals(2), "<>m2", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  newobj     ""Sub <>c__TypeVariables(Of T, $CLS0)..ctor()""
  IL_0005:  ret
}")

                    Dim method = DirectCast(testData.GetMethodData("<>x(Of T, $CLS0).<>m1").Method, MethodSymbol)
                    Dim containingType = method.ContainingType
                    Assert.Equal(containingType.TypeParameters(1), method.ReturnType)

                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub Iterator_InstanceMethod()
            Const source = "
Imports System.Collections

Class C
    Private ReadOnly _c As Object()

    Friend Sub New(c As Object())
        _c = c
    End Sub

    Friend Iterator Function F() As IEnumerable
        For Each o In _c
#ExternalSource(""test"", 999)
            Yield o
#End ExternalSource
        Next
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_2_F.MoveNext", atLineNumber:=999)
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(2, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_F.$VB$Me As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "o", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_F.$VB$ResumableLocal_o$2 As Object""
  IL_0006:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        <Fact()>
        Public Sub Iterator_StaticMethod_Generic()
            Const source = "
Imports System.Collections.Generic

Class C
    Friend Shared Iterator Function F(Of T)(o As T()) As IEnumerable(Of T)
        For i = 1 To o.Length
#ExternalSource(""test"", 999)
            Dim t1 As T = Nothing
            Yield t1
            Yield o(i)
#End ExternalSource
        Next
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_1_F.MoveNext", atLineNumber:=999)
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(4, locals.Count)

                    VerifyLocal(testData, "<>x(Of T)", locals(0), "<>m0", "o", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F(Of T).$VB$Local_o As T()""
  IL_0006:  ret
}")
                    VerifyLocal(testData, "<>x(Of T)", locals(1), "<>m1", "i", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F(Of T).$VB$ResumableLocal_i$1 As Integer""
  IL_0006:  ret
}")
                    VerifyLocal(testData, "<>x(Of T)", locals(2), "<>m2", "t1", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F(Of T).$VB$ResumableLocal_t1$2 As T""
  IL_0006:  ret
}
")
                    VerifyLocal(testData, "<>x(Of T)", locals(3), "<>m3", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  newobj     ""Sub <>c__TypeVariables(Of T)..ctor()""
  IL_0005:  ret
}")

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        Public Sub Async_InstanceMethod_Generic()
            Const source = "
Imports System.Threading.Tasks

Structure S(Of T As Class)
    Private x As T

    Friend Async Function F(Of U As Class)(y As u) As Task(Of Object)
        Dim z As T = Nothing
        Return If(Me.x, If(DirectCast(y, Object), z))
    End Function
End Structure
"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)

            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="S.VB$StateMachine_2_F.MoveNext")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(4, locals.Count)

                    VerifyLocal(testData, "<>x(Of T, U)", locals(0), "<>m0", "Me", expectedILOpt:="
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Object) V_2,
                T V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""S(Of T).VB$StateMachine_2_F(Of U).$VB$Me As S(Of T)""
  IL_0006:  ret
}
")
                    VerifyLocal(testData, "<>x(Of T, U)", locals(1), "<>m1", "y", expectedILOpt:="
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Object) V_2,
                T V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""S(Of T).VB$StateMachine_2_F(Of U).$VB$Local_y As U""
  IL_0006:  ret
}
")
                    VerifyLocal(testData, "<>x(Of T, U)", locals(2), "<>m2", "z", expectedILOpt:="
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Object) V_2,
                T V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""S(Of T).VB$StateMachine_2_F(Of U).$VB$ResumableLocal_z$0 As T""
  IL_0006:  ret
}
")
                    VerifyLocal(testData, "<>x(Of T, U)", locals(3), "<>m3", "<>TypeVariables", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:="
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Object V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Object) V_2,
                T V_3,
                System.Exception V_4)
  IL_0000:  newobj     ""Sub <>c__TypeVariables(Of T, U)..ctor()""
  IL_0005:  ret
}
")

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        Public Sub Async_StaticMethod()
            Const source = "
Imports System.Threading.Tasks

Class C
    Shared Async Function F(o As Object) As Task(Of Object)
        Return o
    End Function

    Shared Async Function M(x As Object) As task
        Dim y = Await F(x)
        Await F(y)
    End Function
End Class
"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)

            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_2_M.MoveNext")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)

                    Assert.Equal(2, locals.Count)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:="
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_1,
                C.VB$StateMachine_2_M V_2,
                Object V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$Local_x As Object""
  IL_0006:  ret
}
")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:="
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_1,
                C.VB$StateMachine_2_M V_2,
                Object V_3,
                System.Runtime.CompilerServices.TaskAwaiter(Of Object) V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$ResumableLocal_y$0 As Object""
  IL_0006:  ret
}
")

                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(995976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995976")>
        <WorkItem(997613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997613")>
        <WorkItem(1002672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1002672")>
        <WorkItem(1085911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085911")>
        <Fact>
        Public Sub AsyncAndLambda()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Shared Async Function F() As Task
    End Function
    Shared Sub G(a As Action)
        a()
    End Sub
    Shared Async Function M(x As Integer) As Task(Of Integer)
        Dim y = x + 1
        Await F()
        G(Sub()
              x = x + 2
              y = y + 2
          End Sub)
        x = x + y
        Return x
    End Function
End Class"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_3_M.MoveNext")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.VB$StateMachine_3_M V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_M.$VB$Local_x As Integer""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=
"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Runtime.CompilerServices.TaskAwaiter V_3,
                C.VB$StateMachine_3_M V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_M.$VB$ResumableLocal_$VB$Closure_$0 As C._Closure$__3-0""
  IL_0006:  ldfld      ""C._Closure$__3-0.$VB$Local_y As Integer""
  IL_000b:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(2240, "https://github.com/dotnet/roslyn/issues/2240")>
        <Fact>
        Public Sub AsyncLambda()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Shared Sub M()
        Dim f As Func(Of Integer, Task) = Async Function (x)
            Dim y = 42
        End Function
    End Sub
End Class"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)

            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C._Closure$__.VB$StateMachine___Lambda$__1-0.MoveNext")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Threading.Tasks.Task V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__1-0.$VB$Local_x As Integer""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Threading.Tasks.Task V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__1-0.$VB$ResumableLocal_y$0 As Integer""
  IL_0006:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(996571, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/996571")>
        <Fact>
        Public Sub MissingReference()
            Const source0 =
"Public Class A
End Class
Public Structure B
End Structure"
            Const source1 =
"Class C
    Shared Sub M(a As A, b As B, c As C)
    End Sub
End Class"
            Dim comp0 = CreateCompilationWithMscorlib({source0}, options:=TestOptions.DebugDll)
            Dim comp1 = CreateCompilationWithMscorlib({source1}, options:=TestOptions.DebugDll, references:={comp0.EmitToImageReference()})

            ' no reference to compilation0
            WithRuntimeInstance(comp1, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData, expectedDiagnostics:=
                    {
                        Diagnostic(ERRID.ERR_TypeRefResolutionError3, "a").WithArguments("A", "Test.dll").WithLocation(1, 1)
                    })

                    Assert.Equal(0, locals.Count)
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub AssignmentToLockLocal()
            Const source = "
Class C
    Sub M(o As Object)
        SyncLock(o)
#ExternalSource(""test"", 999)
            Dim x As Integer = 1
#End ExternalSource
        End SyncLock
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.M", atLineNumber:=999)

                    Dim errorMessage As String = Nothing
                    Dim testData As New CompilationTestData()
                    context.CompileAssignment("o", "Nothing", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
                    Assert.Null(errorMessage) ' In regular code, there would be an error about modifying a lock local.

                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (Object V_0,
                Boolean V_1,
                Integer V_2) //x
  IL_0000:  ldnull
  IL_0001:  starg.s    V_1
  IL_0003:  ret
}")
                End Sub)
        End Sub

        <WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")>
        <Fact>
        Public Sub LocalDateConstant()
            Const source = "
Class C
    Shared Sub M()
        Const d = #2010/01/02#
        Dim dt As New System.DateTime(2010, 1, 2) ' It's easier to figure out the signature to pass if this is here.
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)

            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim errorMessage As String = Nothing
                    context.CompileAssignment("d", "Nothing", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
                    Assert.Equal("error BC30074: Constant cannot be the target of an assignment.", errorMessage)

                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim testData As New CompilationTestData()
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "dt", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Date V_0) //dt
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "d", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (Date V_0) //dt
  IL_0000:  ldc.i8     0x8cc5955a94ec000
  IL_0009:  newobj     ""Sub Date..ctor(Long)""
  IL_000e:  ret
}")
                End Sub)
        End Sub

        <WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")>
        <Fact>
        Public Sub LocalDecimalConstant()
            Const source = "
Class C
    Shared Sub M()
        Const d As Decimal = 1.5D
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim errorMessage As String = Nothing
                    context.CompileAssignment("d", "Nothing", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
                    Assert.Equal("error BC30074: Constant cannot be the target of an assignment.", errorMessage)

                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim testData As New CompilationTestData()
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(1, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "d", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size       12 (0xc)
  .maxstack  5
  IL_0000:  ldc.i4.s   15
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.0
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.1
  IL_0006:  newobj     ""Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)""
  IL_000b:  ret
}")
                End Sub)
        End Sub

        <Fact, WorkItem(1022165, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1022165"), WorkItem(1028883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028883"), WorkItem(1034204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034204")>
        Public Sub KeywordIdentifiers()
            Const source = "
Class C
    Sub M([Nothing] As Integer)
        Dim [Me] = 1
        Dim [True] = ""t""c
        Dim [Namespace] = ""NS""
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.M")

                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.NotNull(assembly)
                    Assert.NotEqual(assembly.Count, 0)

                    Assert.Equal(locals.Count, 5)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //Me
                Char V_1, //True
                String V_2) //Namespace
  IL_0000:  ldarg.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "[Nothing]", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //Me
                Char V_1, //True
                String V_2) //Namespace
  IL_0000:  ldarg.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "[Me]", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //Me
                Char V_1, //True
                String V_2) //Namespace
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "[True]", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //Me
                Char V_1, //True
                String V_2) //Namespace
  IL_0000:  ldloc.1
  IL_0001:  ret
}")
                    VerifyLocal(testData, typeName, locals(4), "<>m4", "[Namespace]", expectedILOpt:="
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //Me
                Char V_1, //True
                String V_2) //Namespace
  IL_0000:  ldloc.2
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub ExtensionIterator()
            Const source = "
Module M
    <System.Runtime.CompilerServices.Extension>
    Iterator Function F(x As Integer) As System.Collections.IEnumerable
        Yield x
    End Function
End Module
"

            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""M.VB$StateMachine_0_F.$VB$Local_x As Integer""
  IL_0006:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "M.VB$StateMachine_0_F.MoveNext")

                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.NotNull(assembly)
                    Assert.NotEqual(0, assembly.Count)

                    Assert.Equal(1, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=expectedIL)
                    Assert.Equal(SpecialType.System_Int32, testData.GetMethodData(typeName & ".<>m0").Method.ReturnType.SpecialType)
                    locals.Free()

                    testData = New CompilationTestData()
                    Dim errorMessage As String = Nothing
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    methodData.VerifyIL(expectedIL)
                    Assert.Equal(SpecialType.System_Int32, methodData.Method.ReturnType.SpecialType)
                End Sub)
        End Sub

        <WorkItem(1014763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014763")>
        <Fact>
        Public Sub TypeVariablesTypeParameterNames()
            Const source = "
Imports System.Collections.Generic

Class C
    Iterator Shared Function I(Of T)() As IEnumerable(Of T)
        Yield Nothing
    End Function
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_I.MoveNext")

                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.NotNull(assembly)
                    Assert.NotEqual(0, assembly.Count)

                    Dim local = locals.Single()
                    Assert.Equal("<>TypeVariables", local.LocalName)
                    Assert.Equal("<>m0", local.MethodName)

                    Dim method = testData.GetMethodData("<>x(Of T).<>m0").Method
                    Dim typeVariablesType = DirectCast(method.ReturnType, NamedTypeSymbol)
                    Assert.Equal("T", typeVariablesType.TypeParameters.Single().Name)
                    Assert.Equal("T", typeVariablesType.TypeArguments.Single().Name)
                End Sub)
        End Sub

        <Fact, WorkItem(1063254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")>
        Public Sub OverloadedIteratorDifferentParameterTypes_ArgumentsOnly()
            Dim source = "
Imports System.Collections.Generic
Class C
    Iterator Function M1(x As Integer, y As Integer) As IEnumerable(Of Integer)
        Dim local = 0
        Yield local
    End Function
    Iterator Function M1(x As Integer, y As Single) As IEnumerable(Of Single)
        Dim local = 0.0F
        Yield local
    End Function
    Shared Iterator Function M2(x As Integer, y As Single) As IEnumerable(Of Single)
        Dim local = 0.0F
        Yield local
    End Function
    Shared Iterator Function M2(Of T)(x As Integer, y As T) As IEnumerable(Of T)
        Dim local As T = Nothing
        Yield local
    End Function
    Shared Iterator Function M2(x As Integer, y As Integer) As IEnumerable(Of Integer)
        Dim local = 0
        Yield local
    End Function
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim displayClassName As String
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    Dim ilTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.{1}.$VB$Local_{2} As {0}""
  IL_0006:  ret
}}"

                    ' M1(Integer, Integer)
                    displayClassName = "VB$StateMachine_1_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "y"))
                    locals.Clear()

                    ' M1(Integer, Single)
                    displayClassName = "VB$StateMachine_2_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Single", displayClassName, "y"))
                    locals.Clear()

                    ' M2(Integer, Single)
                    displayClassName = "VB$StateMachine_3_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Single", displayClassName, "y"))
                    locals.Clear()

                    ' M2(Integer, T)
                    displayClassName = "VB$StateMachine_4_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    typeName += "(Of T)"
                    displayClassName += "(Of T)"
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "T", displayClassName, "y"))
                    locals.Clear()

                    ' M2(Integer, Integer)
                    displayClassName = "VB$StateMachine_5_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Integer", displayClassName, "y"))
                    locals.Clear()

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1063254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")>
        Public Sub OverloadedAsyncDifferentParameterTypes_ArgumentsOnly()
            Dim source = "
Imports System.Threading.Tasks
Class C
    Async Function M1(x As Integer) As Task(Of Integer)
        Dim local = 0
        Return local
    End Function
    Async Function M1(x As Integer, y As Single) As Task(Of Single)
        Dim local = 0.0F
        Return local
    End Function
    Shared Async Function M2(x As Integer, y As Single) As Task(Of Single)
        Dim local = 0.0F
        return local
    End Function
    Shared Async Function M2(Of T)(x As T) As Task(Of T)
        Dim local As T = Nothing
        Return local
    End Function
    Shared Async Function M2(x As Integer) As Task(Of Integer)
        Dim local = 0
        Return local
    End Function
End Class"
            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim displayClassName As String
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    Dim ilTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init ({0} V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of {0}) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.{2}.$VB$Local_{3} As {1}""
  IL_0006:  ret
}}"

                    ' M1(Integer)
                    displayClassName = "VB$StateMachine_1_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", "Integer", displayClassName, "x"))
                    locals.Clear()

                    ' M1(Integer, Single)
                    displayClassName = "VB$StateMachine_2_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Single", "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Single", "Single", displayClassName, "y"))
                    locals.Clear()

                    ' M2(Integer, Single)
                    displayClassName = "VB$StateMachine_3_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Single", "Integer", displayClassName, "x"))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(ilTemplate, "Single", "Single", displayClassName, "y"))
                    locals.Clear()

                    ' M2(T)
                    displayClassName = "VB$StateMachine_4_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of T)", locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "T", "T", displayClassName + "(Of T)", "x"))
                    locals.Clear()

                    ' M2(Integer)
                    displayClassName = "VB$StateMachine_5_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(ilTemplate, "Integer", "Integer", displayClassName, "x"))
                    locals.Clear()

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1063254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")>
        Public Sub MultipleLambdasDifferentParameterNames_ArgumentsOnly()
            Dim source = "
Imports System
Class C
    Sub M1(x As Integer)
        Dim a As Action(Of Integer) = Sub(y) x.ToString()
        Dim f As Func(Of Integer, Integer) = Function(z) x
    End Sub
    Shared Sub M2(Of T)(x As Integer)
        Dim a As Action(Of Integer) = Sub(y) y.ToString()
        Dim f As Func(Of Integer, Integer) = Function(z) z
        Dim g As Func(Of T, T) = Function(ti) ti
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim displayClassName As String
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    Dim voidRetILTemplate = "
{{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.{0}
  IL_0001:  ret
}}"
                    Dim funcILTemplate = "
{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ({0} V_0)
  IL_0000:  ldarg.{1}
  IL_0001:  ret
}}"

                    ' Sub(y) x.ToString()
                    displayClassName = "_Closure$__1-0"
                    GetLocals(runtime, "C." + displayClassName + "._Lambda$__0", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "y", expectedILOpt:=String.Format(voidRetILTemplate, 1))
                    locals.Clear()

                    ' Function(z) x
                    displayClassName = "_Closure$__1-0"
                    GetLocals(runtime, "C." + displayClassName + "._Lambda$__1", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "z", expectedILOpt:=String.Format(funcILTemplate, "Integer", 1))
                    locals.Clear()

                    ' Sub(y) y.ToString()
                    displayClassName = "_Closure$__2"
                    GetLocals(runtime, "C." + displayClassName + "._Lambda$__2-0", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of $CLS0)", locals(0), "<>m0", "y", expectedILOpt:=String.Format(voidRetILTemplate, 1))
                    locals.Clear()

                    ' Function(z) z
                    displayClassName = "_Closure$__2"
                    GetLocals(runtime, "C." + displayClassName + "._Lambda$__2-1", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of $CLS0)", locals(0), "<>m0", "z", expectedILOpt:=String.Format(funcILTemplate, "Integer", 1))
                    locals.Clear()

                    ' Function(ti) ti
                    displayClassName = "_Closure$__2"
                    GetLocals(runtime, "C." + displayClassName + "._Lambda$__2-2", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of $CLS0)", locals(0), "<>m0", "ti", expectedILOpt:=String.Format(funcILTemplate, "$CLS0", 1))
                    locals.Clear()

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1063254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")>
        Public Sub OverloadedRegularMethodDifferentParameterTypes_ArgumentsOnly()
            Dim source = "
Class C
    Sub M1(x As Integer, y As Integer)
        Dim local = 0
    End Sub
    Function M1(x As Integer, y As String) As String
        Dim local As String = Nothing
        return local
    End Function
    Shared Sub M2(x As Integer, y As String)
        Dim local As String = Nothing
    End Sub
    Shared Function M2(Of T)(x As Integer, y As T) As T
        Dim local As T = Nothing
        Return local
    End Function
    Shared Function M2(x As Integer, ByRef y As Integer) As Integer
        Dim local As Integer = 0
        Return local 
    End Function
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    Dim voidRetILTemplate = "
{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ({0} V_0) //local
  IL_0000:  ldarg.{1}
  IL_0001:  ret
}}"
                    Dim funcILTemplate = "
{{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init ({0} V_0, {1}
                {0} V_1) //local
  IL_0000:  ldarg.{2}
  IL_0001:  ret
}}"
                    Dim refParamILTemplate = "
{{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init ({0} V_0, {1}
                {0} V_1) //local
  IL_0000:  ldarg.{2}
  IL_0001:  ldind.i4
  IL_0002:  ret
}}"

                    ' M1(Integer, Integer)
                    GetLocals(runtime, "C.M1(Int32,Int32)", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(voidRetILTemplate, "Integer", 1))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(voidRetILTemplate, "Integer", 2))
                    locals.Clear()

                    ' M1(Integer, String)
                    GetLocals(runtime, "C.M1(Int32,String)", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(funcILTemplate, "String", "//M1", 1))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(funcILTemplate, "String", "//M1", 2))
                    locals.Clear()

                    ' M2(Integer, String)
                    GetLocals(runtime, "C.M2(Int32,String)", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(voidRetILTemplate, "String", 0))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(voidRetILTemplate, "String", 1))
                    locals.Clear()

                    ' M2(Integer, T)
                    GetLocals(runtime, "C.M2(Int32,T)", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0(Of T)", "x", expectedILOpt:=String.Format(funcILTemplate, "T", "//M2", 0), expectedGeneric:=True)
                    VerifyLocal(testData, typeName, locals(1), "<>m1(Of T)", "y", expectedILOpt:=String.Format(funcILTemplate, "T", "//M2", 1), expectedGeneric:=True)
                    locals.Clear()

                    ' M2(Integer, Integer)
                    GetLocals(runtime, "C.M2(Int32,Int32)", argumentsOnly:=True, locals:=locals, count:=2, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=String.Format(funcILTemplate, "Integer", "//M2", 0))
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=String.Format(refParamILTemplate, "Integer", "//M2", 1))
                    locals.Clear()

                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem(1063254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063254")>
        Public Sub MultipleMethodsLocalConflictsWithParameterName_ArgumentsOnly()
            Dim source = "
Imports System.Collections.Generic
Imports System.Threading.Tasks
Class C(Of T)
    Iterator Function M1() As IEnumerable(Of Integer)
        Dim x = 0
        Yield x
    End Function
    Iterator Function M1(x As Integer) As IEnumerable(Of Integer)
        Yield x
    End Function
    Iterator Function M2(x As Integer) As IEnumerable(Of Integer)
        Yield x
    End Function
    Iterator Function M2() As IEnumerable(Of Integer)
        Dim x = 0
        Yield x
    End Function
    Shared Async Function M3() As Task(Of T)
        Dim x As T = Nothing
        return x
    End Function
    Shared Async Function M3(x As T) As Task(Of T)
        Return x
    End Function
    Shared Async Function M4(x As T) As Task(Of T)
        Return x
    End Function
    Shared Async Function M4() As Task(Of T)
        Dim x As T = Nothing
        Return x
    End Function
End Class"
            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim displayClassName As String
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    Dim iteratorILTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                {0} V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T).{1}.$VB$Local_{2} As {0}""
  IL_0006:  ret
}}"
                    Dim asyncILTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init ({0} V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of {0}) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T).{1}.$VB$Local_{2} As {0}""
  IL_0006:  ret
}}"

                    ' M1()
                    displayClassName = "VB$StateMachine_1_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=0, typeName:=typeName, testData:=testData)
                    locals.Clear()

                    ' M1(Integer)
                    displayClassName = "VB$StateMachine_2_M1"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of T)", locals(0), "<>m0", "x", expectedILOpt:=String.Format(iteratorILTemplate, "Integer", displayClassName, "x"))
                    locals.Clear()

                    ' M2(Integer)
                    displayClassName = "VB$StateMachine_3_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of T)", locals(0), "<>m0", "x", expectedILOpt:=String.Format(iteratorILTemplate, "Integer", displayClassName, "x"))
                    locals.Clear()

                    ' M2()
                    displayClassName = "VB$StateMachine_4_M2"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=0, typeName:=typeName, testData:=testData)
                    locals.Clear()

                    ' M3()
                    displayClassName = "VB$StateMachine_5_M3"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=0, typeName:=typeName, testData:=testData)
                    locals.Clear()

                    ' M3(Integer)
                    displayClassName = "VB$StateMachine_6_M3"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of T)", locals(0), "<>m0", "x", expectedILOpt:=String.Format(asyncILTemplate, "T", displayClassName, "x"))
                    locals.Clear()

                    ' M4(Integer)
                    displayClassName = "VB$StateMachine_7_M4"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName + "(Of T)", locals(0), "<>m0", "x", expectedILOpt:=String.Format(asyncILTemplate, "T", displayClassName, "x"))
                    locals.Clear()

                    ' M4()
                    displayClassName = "VB$StateMachine_8_M4"
                    GetLocals(runtime, "C." + displayClassName + ".MoveNext", argumentsOnly:=True, locals:=locals, count:=0, typeName:=typeName, testData:=testData)
                    locals.Clear()

                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(1115044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115044")>
        <Fact>
        Public Sub CaseSensitivity()
            Const source = "
Class C
    Shared Sub M(p As Integer)
        Dim s As String
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData

                    testData = New CompilationTestData()
                    context.CompileExpression("P", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0) //s
  IL_0000:  ldarg.0
  IL_0001:  ret
}
")

                    testData = New CompilationTestData()
                    context.CompileExpression("S", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0) //s
  IL_0000:  ldloc.0
  IL_0001:  ret
}
")
                End Sub)
        End Sub

        <WorkItem(1115030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115030")>
        <Fact>
        Public Sub CatchInAsyncStateMachine()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Shared Function F() As Object
        Throw New ArgumentException()
    End Function
    Shared Async Function M() As Task
        Dim o As Object
        Try
            o = F()
        Catch e As Exception
#ExternalSource(""test"", 999)
            o = e
#End ExternalSource
        End Try
    End Function
End Class"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_2_M.MoveNext", atLineNumber:=999)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "o", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Exception V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$ResumableLocal_o$0 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "e", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Exception V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$ResumableLocal_e$1 As System.Exception""
  IL_0006:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(1115030, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115030")>
        <Fact>
        Public Sub CatchInIteratorStateMachine()
            Const source =
"Imports System
Imports System.Collections
Class C
    Shared Function F() As Object
        Throw New ArgumentException()
    End Function
    Shared Iterator Function M() As IEnumerable
        Dim o As Object
        Try
            o = F()
        Catch e As Exception
#ExternalSource(""test"", 999)
            o = e
#End ExternalSource
        End Try
        Yield o
    End Function
End Class"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_2_M.MoveNext", atLineNumber:=999)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "o", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$ResumableLocal_o$0 As Object""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "e", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                System.Exception V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_M.$VB$ResumableLocal_e$1 As System.Exception""
  IL_0006:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact>
        Public Sub DuplicateEditorBrowsableAttributes()
            Const libSource = "
Namespace System.ComponentModel

    Public Enum EditorBrowsableState
        Always = 0
        Never = 1
        Advanced = 2
    End Enum

    <AttributeUsage(AttributeTargets.All)>
    Friend NotInheritable Class EditorBrowsableAttribute
        Inherits Attribute
    
        Public Sub New(state As EditorBrowsableState)
        End Sub
    End Class

End Namespace
"

            Const source = "
<System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
Class C
    Sub M()
    End Sub
End Class
"

            Dim libRef = CreateCompilationWithMscorlib({libSource}, options:=TestOptions.DebugDll).EmitToImageReference()
            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef, SystemRef}, TestOptions.DebugDll)

            ' Referencing SystemCoreRef and SystemXmlLinqRef will cause Microsoft.VisualBasic.Embedded to be compiled
            ' and it depends on EditorBrowsableAttribute.
            WithRuntimeInstance(comp, {MscorlibRef, SystemRef, SystemCoreRef, SystemXmlLinqRef, libRef},
                Sub(runtime)
                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, "C.M", argumentsOnly:=False, locals:=locals, count:=1, typeName:=typeName, testData:=testData)
                    Assert.Equal("Me", locals.Single().LocalName)
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(2089, "https://github.com/dotnet/roslyn/issues/2089")>
        <Fact>
        Public Sub MultipleMeFields()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Async Shared Function F(a As Action) As Task
        a()
    End Function
    Sub G(s As String)
    End Sub
    Async Sub M()
        Dim s As String = Nothing
        Await F(Sub() G(s))
    End Sub
End Class"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_3_M.MoveNext")
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me", expectedILOpt:=
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_3_M V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_M.$VB$Me As C""
  IL_0006:  ret
}")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "s", expectedILOpt:=
"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_3_M V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_3_M.$VB$ResumableLocal_$VB$Closure_$0 As C._Closure$__3-0""
  IL_0006:  ldfld      ""C._Closure$__3-0.$VB$Local_s As String""
  IL_000b:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(2336, "https://github.com/dotnet/roslyn/issues/2336")>
        <Fact>
        Public Sub LocalsOnAsyncEndSub()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class C
    Async Sub M()
        Dim s As String = Nothing
#ExternalSource(""test"", 999)
    End Sub
#End ExternalSource
End Class
"
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                {MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstancePortableBug(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, methodName:="C.VB$StateMachine_1_M.MoveNext", atLineNumber:=999)
                    Dim testData As New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "s")
                    locals.Free()
                End Sub)
        End Sub

        <WorkItem(1139013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")>
        <Fact>
        Public Sub TransparentIdentifiers_FromParameter()
            Const source = "
Imports System.Linq

Class C
    Sub M(args As String())
        Dim concat =
            From x In args
            Let y = x.ToString()
            Let z = x.GetHashCode()
            Select x & y & z
    End Sub
End Class
"

            Const methodName = "C._Closure$__._Lambda$__1-2"

            Const zIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$z As Integer""
  IL_0006:  ret
}
"

            Const xIL = "
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$$VB$It As VB$AnonymousType_0(Of String, String)""
  IL_0006:  ldfld      ""VB$AnonymousType_0(Of String, String).$x As String""
  IL_000b:  ret
}
"

            Const yIL = "
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$$VB$It As VB$AnonymousType_0(Of String, String)""
  IL_0006:  ldfld      ""VB$AnonymousType_0(Of String, String).$y As String""
  IL_000b:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=3, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "z", expectedILOpt:=zIL)
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "x", expectedILOpt:=xIL)
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y", expectedILOpt:=yIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("z", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(zIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("y", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(yIL)
                End Sub)
        End Sub

        <WorkItem(1139013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")>
        <Fact>
        Public Sub TransparentIdentifiers_FromDisplayClassField()
            Const source = "
Imports System.Linq

Class C
    Sub M(args As String())
        Dim concat =
            From x In args
            Let y = x.ToString()
            Let z = x.GetHashCode()
            Select x.Select(Function(c) y & z)
    End Sub
End Class
"

            Const methodName = "C._Closure$__1-0._Lambda$__3"

            Const cIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldarg.1
  IL_0001:  ret
}
"

            Const zIL = "
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_$VB$It As VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer)""
  IL_0006:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$z As Integer""
  IL_000b:  ret
}
"

            Const xIL = "
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_$VB$It As VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer)""
  IL_0006:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$$VB$It As VB$AnonymousType_0(Of String, String)""
  IL_000b:  ldfld      ""VB$AnonymousType_0(Of String, String).$x As String""
  IL_0010:  ret
}
"

            Const yIL = "
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__1-0.$VB$Local_$VB$It As VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer)""
  IL_0006:  ldfld      ""VB$AnonymousType_1(Of VB$AnonymousType_0(Of String, String), Integer).$$VB$It As VB$AnonymousType_0(Of String, String)""
  IL_000b:  ldfld      ""VB$AnonymousType_0(Of String, String).$y As String""
  IL_0010:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=4, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "c", expectedILOpt:=cIL)
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "z", expectedILOpt:=zIL)
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "x", expectedILOpt:=xIL)
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y", expectedILOpt:=yIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("c", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(cIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("z", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(zIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("y", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(yIL)
                End Sub)
        End Sub

        <WorkItem(1139013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")>
        <Fact>
        Public Sub TransparentIdentifiers_It1()
            Const source = "
Imports System.Linq

Class C
    Sub M(args As String())
        Dim concat =
            From x In args, y In args
            From z In args
            Select x & y & z
    End Sub
End Class
"

            Const methodName = "C._Closure$__._Lambda$__1-3"

            Const zIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.2
  IL_0001:  ret
}
"

            Const xIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""VB$AnonymousType_0(Of String, String).$x As String""
  IL_0006:  ret
}
"

            Const yIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldfld      ""VB$AnonymousType_0(Of String, String).$y As String""
  IL_0006:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=3, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "z", expectedILOpt:=zIL)
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "x", expectedILOpt:=xIL)
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "y", expectedILOpt:=yIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("z", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(zIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("y", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(yIL)
                End Sub)
        End Sub

        <WorkItem(1139013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")>
        <Fact>
        Public Sub TransparentIdentifiers_It2()
            Const source = "
Imports System.Linq

Class C
    Sub M(nums As Integer())
        Dim q = From x In nums
                Join y In nums
                    Join z In nums
                    On z Equals y
                On x Equals y And z Equals x
    End Sub
End Class
"

            Const methodName = "C._Closure$__._Lambda$__1-5"

            Const xIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
"

            Const yIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.2
  IL_0001:  ldfld      ""VB$AnonymousType_1(Of Integer, Integer).$y As Integer""
  IL_0006:  ret
}
"

            Const zIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.2
  IL_0001:  ldfld      ""VB$AnonymousType_1(Of Integer, Integer).$z As Integer""
  IL_0006:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=3, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=xIL)
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "y", expectedILOpt:=yIL)
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "z", expectedILOpt:=zIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("y", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(yIL)

                    testData = New CompilationTestData()
                    context.CompileExpression("z", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(zIL)
                End Sub)
        End Sub

        <WorkItem(1139013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1139013")>
        <Fact>
        Public Sub TransparentIdentifiers_ItAnonymous()
            Const source = "
Imports System.Linq

Class C
    Sub M(args As String())
        Dim concat =
            From x In args
            Group y = x.GetHashCode() By x
                Into s = Sum(y + 3)
    End Sub
End Class
"

            Const methodName = "C._Closure$__._Lambda$__1-2"

            Const xIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=1, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "x", expectedILOpt:=xIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)
                End Sub)
        End Sub

        <WorkItem(3236, "https://github.com/dotnet/roslyn/pull/3236")>
        <Fact>
        Public Sub AnonymousTypeParameter()
            Const source = "
Imports System.Linq

Class C
    Shared Sub Main(args As String())
        Dim anonymousTypes =
            From a In args
            Select New With {.Value = a, .Length = a.Length}
        Dim values =
            From t In anonymousTypes
            Select t.Value
    End Sub
End Class
"

            Const methodName = "C._Closure$__._Lambda$__1-1"

            Const xIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ret
}
"

            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim typeName As String = Nothing
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim testData As CompilationTestData = Nothing
                    GetLocals(runtime, methodName, argumentsOnly:=False, locals:=locals, count:=1, typeName:=typeName, testData:=testData)

                    VerifyLocal(testData, typeName, locals(0), "<>m0", "t", expectedILOpt:=xIL)
                    locals.Free()

                    Dim context = CreateMethodContext(runtime, methodName)
                    Dim errorMessage As String = Nothing

                    testData = New CompilationTestData()
                    context.CompileExpression("t", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(xIL)
                End Sub)
        End Sub

        <WorkItem(955, "https://github.com/aspnet/Home/issues/955")>
        <Fact>
        Public Sub ConstantWithErrorType()
            Const source = "
Module Module1
    Sub Main()
        Const a = 1
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlib({source}, {MsvbRef}, options:=TestOptions.DebugExe)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim badConst = New MockSymUnmanagedConstant(
                        "a",
                        1,
                        Function(bufferLength As Integer, ByRef count As Integer, name() As Byte)
                            count = 0
                            Return DiaSymReader.SymUnmanagedReaderExtensions.E_NOTIMPL
                        End Function)

                    Dim debugInfo = New MethodDebugInfoBytes.Builder(constants:={badConst}).Build()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()

                    GetLocals(runtime, "Module1.Main", debugInfo, locals, count:=0)

                    locals.Free()
                End Sub)
        End Sub

        Private Shared Sub GetLocals(runtime As RuntimeInstance, methodName As String, argumentsOnly As Boolean, locals As ArrayBuilder(Of LocalAndMethod), count As Integer, ByRef typeName As String, ByRef testData As CompilationTestData)
            Dim context = CreateMethodContext(runtime, methodName)

            testData = New CompilationTestData()
            Dim assembly = context.CompileGetLocals(locals, argumentsOnly, typeName, testData)

            Assert.NotNull(assembly)
            If count = 0 Then
                Assert.Equal(0, assembly.Count)
            Else
                Assert.InRange(assembly.Count, 0, Integer.MaxValue)
            End If
            Assert.Equal(count, locals.Count)
        End Sub

        Private Shared Sub GetLocals(runtime As RuntimeInstance, methodName As String, debugInfo As MethodDebugInfoBytes, locals As ArrayBuilder(Of LocalAndMethod), count As Integer)
            Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim methodToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, methodName, blocks, moduleVersionId, symReader:=Nothing, methodOrTypeToken:=methodToken, localSignatureToken:=localSignatureToken)

            Dim symReader = New MockSymUnmanagedReader(
                New Dictionary(Of Integer, MethodDebugInfoBytes)() From
                {
                    {methodToken, debugInfo}
                }.ToImmutableDictionary())
            Dim context = EvaluationContext.CreateMethodContext(
                Nothing,
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion:=1,
                ilOffset:=0,
                localSignatureToken:=localSignatureToken)

            Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=Nothing, testData:=Nothing)

            Assert.NotNull(assembly)
            If count = 0 Then
                Assert.Equal(0, assembly.Count)
            Else
                Assert.InRange(assembly.Count, 0, Integer.MaxValue)
            End If
            Assert.Equal(count, locals.Count)
        End Sub

    End Class

End Namespace
