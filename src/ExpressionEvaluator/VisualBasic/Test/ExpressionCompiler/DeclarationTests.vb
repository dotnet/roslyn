' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
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
                InspectionContextFactory.Empty.Add("3", GetType(Integer)),
                "z = $3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (Object V_0, //y
                Boolean V_1,
                Object V_2)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""z""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""z""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""3""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  unbox.any  ""Integer""
  IL_002d:  box        ""Integer""
  IL_0032:  stind.ref
  IL_0033:  ret
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
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("x", GetType(String)).Add("y", GetType(Integer)).Add("t", GetType(Object)).Add("d", "C").Add("f", GetType(Integer)),
                "If(If(If(If(If(x, y), T), F), DirectCast(D, C).F), C.G)",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x = F()",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            Assert.Equal(errorMessage, "(1,5): error BC30451: 'F' is not declared. It may be inaccessible due to its protection level.")
        End Sub

        <WorkItem(1098750)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "s = F(s)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                Nothing,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""s""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""s""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""s""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String""
  IL_002d:  call       ""Function M.F(String) As String""
  IL_0032:  stind.ref
  IL_0033:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "M(If(t, t))",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                Nothing,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""t""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""t""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_001e:  dup
  IL_001f:  brtrue.s   IL_002c
  IL_0021:  pop
  IL_0022:  ldstr      ""t""
  IL_0027:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_002c:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0031:  call       ""Sub M.M(Object)""
  IL_0036:  ret
}")
        End Sub

        <WorkItem(1100849)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "F(o)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""o""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""o""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  call       ""Function M.F(Of Object)(ByRef Object) As Object""
  IL_0023:  pop
  IL_0024:  ret
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty.Add("class", GetType(Object)),
                "[Me] = [Class]",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""me""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""me""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""class""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_002d:  stind.ref
  IL_002e:  ret
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "y = x",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""y""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldarg.0
  IL_001f:  box        ""T""
  IL_0024:  stind.ref
  IL_0025:  ret
}")
        End Sub

        <WorkItem(1101237)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData As CompilationTestData

            ' Object
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldc.i4.3
  IL_001f:  box        ""Integer""
  IL_0024:  stind.ref
  IL_0025:  ret
}")

            ' Integer
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x% = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldtoken    ""Integer""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Integer)(String) As Integer""
  IL_001e:  ldc.i4.3
  IL_001f:  stind.i4
  IL_0020:  ret
}")

            ' Long
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x& = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldtoken    ""Long""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Long)(String) As Long""
  IL_001e:  ldc.i4.3
  IL_001f:  conv.i8
  IL_0020:  stind.i8
  IL_0021:  ret
}")

            ' Single
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x! = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldtoken    ""Single""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Single)(String) As Single""
  IL_001e:  ldc.r4     3
  IL_0023:  stind.r4
  IL_0024:  ret
}")

            ' Double
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x# = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       41 (0x29)
  .maxstack  2
  IL_0000:  ldtoken    ""Double""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Double)(String) As Double""
  IL_001e:  ldc.r8     3
  IL_0027:  stind.r8
  IL_0028:  ret
}")

            ' String
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x$ = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldtoken    ""String""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of String)(String) As String""
  IL_001e:  ldc.i4.3
  IL_001f:  call       ""Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Integer) As String""
  IL_0024:  stind.ref
  IL_0025:  ret
}")

            ' Decimal
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x@ = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldtoken    ""Decimal""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Decimal)(String) As Decimal""
  IL_001e:  ldc.i4.3
  IL_001f:  conv.i8
  IL_0020:  newobj     ""Sub Decimal..ctor(Long)""
  IL_0025:  stobj      ""Decimal""
  IL_002a:  ret
}")
        End Sub

        ''' <summary>
        ''' Should not allow names with '$' prefix.
        ''' </summary>
        <WorkItem(1106819)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

            ' $1
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "$1 = 1",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Equal(errorMessage, "(1,1): error BC30037: Character is not valid.")

            ' $exception
            testData = New CompilationTestData()
            result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "$1 = 2",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Equal(errorMessage, "(1,1): error BC30037: Character is not valid.")

            ' $ReturnValue
            testData = New CompilationTestData()
            result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "$ReturnValue = 3",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Equal(errorMessage, "(1,1): error BC30037: Character is not valid.")

            ' $x
            testData = New CompilationTestData()
            result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "$x = 4",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Equal(errorMessage, "(1,1): error BC30037: Character is not valid.")
        End Sub

        <WorkItem(1101243)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "ReDim a(3)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       38 (0x26)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""a""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""a""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldc.i4.4
  IL_001f:  newarr     ""Object""
  IL_0024:  stind.ref
  IL_0025:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "ReDim Preserve a(3)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       58 (0x3a)
  .maxstack  3
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""a""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""a""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""a""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  castclass  ""System.Array""
  IL_002d:  ldc.i4.4
  IL_002e:  newarr     ""Object""
  IL_0033:  call       ""Function Microsoft.VisualBasic.CompilerServices.Utils.CopyArray(System.Array, System.Array) As System.Array""
  IL_0038:  stind.ref
  IL_0039:  ret
}")
        End Sub

        <WorkItem(1101318)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
                "x += 1",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       53 (0x35)
  .maxstack  3
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  ldc.i4.1
  IL_0029:  box        ""Integer""
  IL_002e:  call       ""Function Microsoft.VisualBasic.CompilerServices.Operators.AddObject(Object, Object) As Object""
  IL_0033:  stind.ref
  IL_0034:  ret
}")
        End Sub

        <WorkItem(1115044, "DevDiv")>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                InspectionContextFactory.Empty.Add("x", GetType(String)),
                "X",
                DkmEvaluationFlags.TreatAsExpression,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
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

        <WorkItem(1115044, "DevDiv")>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                InspectionContextFactory.Empty,
                "x = X",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            Assert.Null(errorMessage) ' Use before initialization is allowed in the EE.
            ' Note that all x's are lowercase (i.e. normalized).
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""x""
  IL_000f:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String)""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_001e:  ldstr      ""x""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0028:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_002d:  stind.ref
  IL_002e:  ret
}
")
        End Sub

    End Class

End Namespace
