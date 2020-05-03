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

    Public Class StaticLocalsTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub StaticLocals()
            Const source =
"Class C
    Shared Function F(b As Boolean) As Object
        If b Then
            Static x As New C()
            Return x
        Else
            Static y = 2
            Return y
        End If
    End Function
    Sub M()
        Static x = Nothing
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, {MsvbRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    ' Shared method.
                    Dim context = CreateMethodContext(runtime, "C.F")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("If(x, y)", errorMessage, testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (Object V_0, //F
                Boolean V_1,
                Boolean V_2,
                Boolean V_3)
  IL_0000:  ldsfld     ""C.$STATIC$F$011C2$x As C""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_000e
  IL_0008:  pop
  IL_0009:  ldsfld     ""C.$STATIC$F$011C2$y As Object""
  IL_000e:  ret
}")
                    ' Instance method.
                    context = CreateMethodContext(runtime, "C.M")
                    testData = New CompilationTestData()
                    context.CompileExpression("If(x, Me)", errorMessage, testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (Boolean V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.$STATIC$M$2001$x As Object""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub AssignStaticLocals()
            Const source =
"Class C
    Shared Sub M()
        Static x = Nothing
        Static y = 1
    End Sub
    Sub N()
        Static z As New C()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, {MsvbRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    ' Shared method.
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileAssignment("x", "y", errorMessage, testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (Boolean V_0,
                Boolean V_1)
  IL_0000:  ldsfld     ""C.$STATIC$M$001$y As Object""
  IL_0005:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_000a:  stsfld     ""C.$STATIC$M$001$x As Object""
  IL_000f:  ret
}")
                    ' Instance method.
                    context = CreateMethodContext(runtime, "C.N")
                    testData = New CompilationTestData()
                    context.CompileAssignment("z", "Nothing", errorMessage, testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (Boolean V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""C.$STATIC$N$2001$z As C""
  IL_0007:  ret
}")
                End Sub)
        End Sub

        ''' <summary>
        ''' Static locals not exposed in the EE from lambdas.
        ''' This matches Dev12 EE behavior since there is no
        ''' map from the lambda to the containing method.
        ''' </summary>
        <Fact>
        Public Sub StaticLocalsReferenceInLambda()
            Const source =
"Class C
    Sub M(x As Object)
        Static y As Object
        Dim f = Function()
                    Return If(x, y)
                End Function
        f()
    End Sub
    Shared Sub M(x As Integer)
        Static z As Integer
        Dim f = Function()
                    Return x + z
                End Function
        f()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, {MsvbRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    ' Instance method.
                    Dim context = CreateMethodContext(runtime, "C._Closure$__1-0._Lambda$__0")
                    Dim errorMessage As String = Nothing
                    context.CompileExpression("If(x, y)", errorMessage)
                    Assert.Equal(errorMessage, "error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.")
                    ' Shared method.
                    context = CreateMethodContext(runtime, "C._Closure$__2-0._Lambda$__0")
                    context.CompileExpression("x + z", errorMessage)
                    Assert.Equal(errorMessage, "error BC30451: 'z' is not declared. It may be inaccessible due to its protection level.")
                End Sub)
        End Sub

        <Fact>
        Public Sub GetLocals()
            Const source =
"Class C
    Shared Function F(b As Boolean) As Object
        If b Then
            Static x As New C()
            Return x
        Else
            Static y As Integer = 2
            Return y
        End If
    End Function
    Shared Function F(i As Integer) As Object
        Static x = 3
        Return x
    End Function
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, {MsvbRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
                    Dim moduleVersionId As Guid = Nothing
                    Dim symReader As ISymUnmanagedReader = Nothing
                    Dim methodToken = 0
                    Dim localSignatureToken = 0
                    GetContextState(runtime, "C.F(Boolean)", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
                    Dim context = CreateMethodContext(
                        New AppDomain(),
                        blocks,
                        MakeDummyLazyAssemblyReaders(),
                        symReader,
                        moduleVersionId,
                        methodToken,
                        methodVersion:=1,
                        ilOffset:=0,
                        localSignatureToken:=localSignatureToken,
                        kind:=MakeAssemblyReferencesKind.AllAssemblies)
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(4, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "b")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "F")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "x", expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Object V_0, //F
                Boolean V_1,
                Boolean V_2,
                Boolean V_3)
  IL_0000:  ldsfld     ""C.$STATIC$F$011C2$x As C""
  IL_0005:  ret
}")
                    VerifyLocal(testData, typeName, locals(3), "<>m3", "y", expectedILOpt:=
"{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Object V_0, //F
                Boolean V_1,
                Boolean V_2,
                Boolean V_3)
  IL_0000:  ldsfld     ""C.$STATIC$F$011C2$y As Integer""
  IL_0005:  ret
}")
                    locals.Free()

                    GetContextState(runtime, "C.F(Int32)", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
                    context = CreateMethodContext(
                        New AppDomain(),
                        blocks,
                        MakeDummyLazyAssemblyReaders(),
                        symReader,
                        moduleVersionId,
                        methodToken,
                        methodVersion:=1,
                        ilOffset:=0,
                        localSignatureToken:=localSignatureToken,
                        kind:=MakeAssemblyReferencesKind.AllAssemblies)
                    testData = New CompilationTestData()
                    locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(3, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "i")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "F")
                    VerifyLocal(testData, typeName, locals(2), "<>m2", "x", expectedILOpt:="
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (Object V_0, //F
                Boolean V_1)
  IL_0000:  ldsfld     ""C.$STATIC$F$011C8$x As Object""
  IL_0005:  ret
}
")
                    locals.Free()
                End Sub)
        End Sub

        ''' <summary>
        ''' Static locals not exposed in the EE from lambdas.
        ''' This matches Dev12 EE behavior since there is no
        ''' map from the lambda to the containing method.
        ''' </summary>
        <Fact>
        Public Sub GetLocalsInLambda()
            Const source =
"Class C
    Sub M(x As Object)
        Static y As Object
        Dim f = Function()
                    Return If(x, y)
                End Function
        f()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, {MsvbRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C._Closure$__1-0._Lambda$__0")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(2, locals.Count)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "Me")
                    VerifyLocal(testData, typeName, locals(1), "<>m1", "x")
                    locals.Free()
                End Sub)
        End Sub

    End Class

End Namespace
