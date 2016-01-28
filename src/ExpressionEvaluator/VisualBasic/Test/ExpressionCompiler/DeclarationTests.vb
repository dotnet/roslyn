' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class DeclarationTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub Declarations()
            Const source =
"Class C
    Private Shared F As Object
    Shared Sub M(Of T)(x As Object)
        Dim y As Object
        If x Is Nothing Then
            Dim z As Object
        End If
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "z = $3",
                DkmEvaluationFlags.None,
                ImmutableArray.Create(ObjectIdAlias(3, GetType(Integer))),
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            Assert.Null(errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (Object V_0, //y
                Boolean V_1,
                Object V_2,
                System.Guid V_3)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""z""
  IL_000f:  ldloca.s   V_3
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.3
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""z""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""$3""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  unbox.any  ""Integer""
  IL_0037:  box        ""Integer""
  IL_003c:  stind.ref
  IL_003d:  ret
}")
        End Sub

        <Fact>
        Public Sub References()
            Const source =
"Class C
    Delegate Sub D()
    Friend F As Object
    Private Shared G As Object
    Shared Sub M(Of T)(x As Object)
        Dim y As Object
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                VariableAlias("x", GetType(String)),
                VariableAlias("y", GetType(Integer)),
                VariableAlias("t", GetType(Object)),
                VariableAlias("d", "C"),
                VariableAlias("f", GetType(Integer)))
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "If(If(If(If(If(x, y), T), F), DirectCast(D, C).F), C.G)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Equal(testData.Methods.Count, 2)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       78 (0x4e)
  .maxstack  2
  .locals init (Object V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0006
  IL_0004:  pop
  IL_0005:  ldloc.0
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0014
  IL_0009:  pop
  IL_000a:  ldstr      ""t""
  IL_000f:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002c
  IL_0017:  pop
  IL_0018:  ldstr      ""f""
  IL_001d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0022:  unbox.any  ""Integer""
  IL_0027:  box        ""Integer""
  IL_002c:  dup
  IL_002d:  brtrue.s   IL_0044
  IL_002f:  pop
  IL_0030:  ldstr      ""d""
  IL_0035:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_003a:  castclass  ""C""
  IL_003f:  ldfld      ""C.F As Object""
  IL_0044:  dup
  IL_0045:  brtrue.s   IL_004d
  IL_0047:  pop
  IL_0048:  ldsfld     ""C.G As Object""
  IL_004d:  ret
}
")
        End Sub

        <Fact>
        Public Sub BindingError_Initializer()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "x = F()",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30451: 'F' is not declared. It may be inaccessible due to its protection level.")
        End Sub

        <WorkItem(1098750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1098750")>
        <Fact>
        Public Sub ReferenceInSameDeclaration()
            Const source =
"Module M
    Function F(s As String) As String
        Return s
    End Function
    Sub M(o As Object)
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "s = F(s)",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       62 (0x3e)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""s""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""s""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String""
  IL_0037:  call       ""Function M.F(String) As String""
  IL_003c:  stind.ref
  IL_003d:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                "M(If(t, t))",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""t""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""t""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  dup
  IL_0029:  brtrue.s   IL_0036
  IL_002b:  pop
  IL_002c:  ldstr      ""t""
  IL_0031:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0036:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_003b:  call       ""Sub M.M(Object)""
  IL_0040:  ret
}")
        End Sub

        <WorkItem(1100849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100849")>
        <Fact>
        Public Sub PassByRef()
            Const source =
"Module M
    Function F(Of T)(ByRef t1 As T) As T
        t1 = Nothing
        Return t1
    End Function
    Sub M()
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "F(o)",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""o""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""o""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  call       ""Function M.F(Of Object)(ByRef Object) As Object""
  IL_002d:  pop
  IL_002e:  ret
}")
        End Sub

        <Fact>
        Public Sub Keyword()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "[Me] = [Class]",
                DkmEvaluationFlags.None,
                ImmutableArray.Create(VariableAlias("class")),
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""Me""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""Me""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""class""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0037:  stind.ref
  IL_0038:  ret
}")
        End Sub

        <Fact>
        Public Sub Generic()
            Const source =
"Class C
    Shared Sub M(Of T)(x As T)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "y = x",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldarg.0
  IL_0029:  box        ""T""
  IL_002e:  stind.ref
  IL_002f:  ret
}")
        End Sub

        <WorkItem(1101237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101237")>
        <Fact>
        Public Sub TypeChar()
            Const source =
"Module M
    Sub M()
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing
            Dim testData As CompilationTestData

            ' Object
            testData = New CompilationTestData()
            context.CompileExpression(
                "x = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldc.i4.3
  IL_0029:  box        ""Integer""
  IL_002e:  stind.ref
  IL_002f:  ret
}")

            ' Integer
            testData = New CompilationTestData()
            context.CompileExpression(
                "x% = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Integer""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Integer)(String) As Integer""
  IL_0028:  ldc.i4.3
  IL_0029:  stind.i4
  IL_002a:  ret
}")

            ' Long
            testData = New CompilationTestData()
            context.CompileExpression(
                "x& = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       44 (0x2c)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Long""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Long)(String) As Long""
  IL_0028:  ldc.i4.3
  IL_0029:  conv.i8
  IL_002a:  stind.i8
  IL_002b:  ret
}")

            ' Single
            testData = New CompilationTestData()
            context.CompileExpression(
                "x! = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Single""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Single)(String) As Single""
  IL_0028:  ldc.r4     3
  IL_002d:  stind.r4
  IL_002e:  ret
}")

            ' Double
            testData = New CompilationTestData()
            context.CompileExpression(
                "x# = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Double""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Double)(String) As Double""
  IL_0028:  ldc.r8     3
  IL_0031:  stind.r8
  IL_0032:  ret
}")

            ' String
            testData = New CompilationTestData()
            context.CompileExpression(
                "x$ = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""String""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of String)(String) As String""
  IL_0028:  ldc.i4.3
  IL_0029:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String""
  IL_002e:  stind.ref
  IL_002f:  ret
}")

            ' Decimal
            testData = New CompilationTestData()
            context.CompileExpression(
                "x@ = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       53 (0x35)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Decimal""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Decimal)(String) As Decimal""
  IL_0028:  ldc.i4.3
  IL_0029:  conv.i8
  IL_002a:  newobj     ""Sub Decimal..ctor(Long)""
  IL_002f:  stobj      ""Decimal""
  IL_0034:  ret
}")
        End Sub

        ''' <summary>
        ''' Should not allow names with '$' prefix.
        ''' </summary>
        <WorkItem(1106819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106819")>
        <Fact>
        Public Sub NoPrefix()
            Const source =
"Module M
    Sub M()
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing

            ' $1
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                "$1 = 1",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30037: Character is not valid.")

            ' $exception
            testData = New CompilationTestData()
            result = context.CompileExpression(
                "$1 = 2",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30037: Character is not valid.")

            ' $ReturnValue
            testData = New CompilationTestData()
            result = context.CompileExpression(
                "$ReturnValue = 3",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30037: Character is not valid.")

            ' $x
            testData = New CompilationTestData()
            result = context.CompileExpression(
                "$x = 4",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30037: Character is not valid.")
        End Sub

        <WorkItem(1101243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101243")>
        <Fact>
        Public Sub [ReDim]()
            Const source =
"Module M
    Sub M()
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "ReDim a(3)",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""a""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""a""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldc.i4.4
  IL_0029:  newarr     ""Object""
  IL_002e:  stind.ref
  IL_002f:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                "ReDim Preserve a(3)",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       68 (0x44)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""a""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""a""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""a""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  castclass  ""System.Array""
  IL_0037:  ldc.i4.4
  IL_0038:  newarr     ""Object""
  IL_003d:  call       ""Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array""
  IL_0042:  stind.ref
  IL_0043:  ret
}")
        End Sub

        <WorkItem(1101318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101318")>
        <Fact>
        Public Sub CompoundAssignment()
            Const source =
"Module M
    Sub M()
    End Sub
End Module"
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(source, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName()), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "M.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "x += 1",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       63 (0x3f)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""x""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  ldc.i4.1
  IL_0033:  box        ""Integer""
  IL_0038:  call       ""Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object""
  IL_003d:  stind.ref
  IL_003e:  ret
}")
        End Sub

        <WorkItem(1115044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115044")>
        <Fact>
        Public Sub CaseSensitivity()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                "X",
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray.Create(VariableAlias("x", GetType(String))),
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""String""
  IL_000f:  ret
}
")
        End Sub

        <WorkItem(1115044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115044")>
        <Fact>
        Public Sub CaseSensitivity_ImplicitDeclaration()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                "x = X",
                DkmEvaluationFlags.None,
                NoAliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage) ' Use before initialization is allowed in the EE.
            ' Note that all x's are lowercase (i.e. normalized).
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       57 (0x39)
  .maxstack  4
  .locals init (System.Guid V_0)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  ldloca.s   V_0
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.0
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldstr      ""x""
  IL_002d:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0032:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0037:  stind.ref
  IL_0038:  ret
}
")
        End Sub

    End Class

End Namespace
