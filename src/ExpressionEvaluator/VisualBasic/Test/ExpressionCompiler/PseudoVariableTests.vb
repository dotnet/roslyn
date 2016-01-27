' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class PseudoVariableTests
        Inherits ExpressionCompilerTestBase

        Private Const s_simpleSource = "
Class C
    Sub M()
    End Sub
End Class
"

        <Fact>
        Public Sub UnrecognizedVariable()
            Dim errorMessage As String = Nothing
            Evaluate(
                s_simpleSource,
                methodName:="C.M",
                expr:="$v",
                aliases:=ImmutableArray(Of [Alias]).Empty,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30451: '$v' is not declared. It may be inaccessible due to its protection level.", errorMessage)
        End Sub

        <Fact>
        Public Sub GlobalName()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll)
            Dim Runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(Runtime, "C.M")

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            context.CompileExpression(
                "Global.$v",
                DkmEvaluationFlags.TreatAsExpression,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData:=Nothing)
            Assert.Equal("error BC30456: '$v' is not a member of 'Global'.", errorMessage)
        End Sub

        <Fact>
        Public Sub Qualified()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll)
            Dim Runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(Runtime, "C.M")

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            context.CompileExpression(
                "Me.$v",
                DkmEvaluationFlags.TreatAsExpression,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData:=Nothing)
            Assert.Equal("error BC30456: '$v' is not a member of 'C'.", errorMessage)
        End Sub

        <Fact>
        Public Sub Exception()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                ExceptionAlias(GetType(System.IO.IOException)),
                ExceptionAlias(GetType(System.InvalidOperationException), stowed:=True))

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "If($Exception, If($exception, $stowedexception))",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Equal(testData.Methods.Count, 2)

            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0026
  IL_000d:  pop
  IL_000e:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0013:  castclass  ""System.IO.IOException""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_0026
  IL_001b:  pop
  IL_001c:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetStowedException() As System.Exception""
  IL_0021:  castclass  ""System.InvalidOperationException""
  IL_0026:  ret
}")
        End Sub

        <Fact>
        Public Sub ReturnValue()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                ReturnValueAlias(type:=GetType(Object)),
                ReturnValueAlias(2, GetType(String)))

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "If($ReturnValue, $ReturnValue2)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0015
  IL_0009:  pop
  IL_000a:  ldc.i4.2
  IL_000b:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0010:  castclass  ""String""
  IL_0015:  ret
}")

            ' Value type $ReturnValue.
            aliases = ImmutableArray.Create(
                ReturnValueAlias(type:=GetType(Integer?)))

            testData = New CompilationTestData()
            context.CompileExpression(
                "DirectCast($ReturnValue, Integer?).HasValue",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0006:  unbox.any  ""Integer?""
  IL_000b:  stloc.0
  IL_000c:  ldloca.s   V_0
  IL_000e:  call       ""Function Integer?.get_HasValue() As Boolean""
  IL_0013:  ret
}")
        End Sub

        <Fact>
        Public Sub ReturnValueNegative()
            Const source = "
Class C
    Sub M()
        Microsoft.VisualBasic.VBMath.Randomize()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                methodName:="C.M",
                expr:="$returnvalue-2",
                aliases:=ImmutableArray.Create(ReturnValueAlias()),
                errorMessage:=errorMessage) ' Subtraction, not a negative index.
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0006:  ldc.i4.2
  IL_0007:  box        ""Integer""
  IL_000c:  call       ""Function Microsoft.VisualBasic.CompilerServices.Operators.SubtractObject(Object, Object) As Object""
  IL_0011:  ret
}")
        End Sub

        <Fact>
        Public Sub ObjectId()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = Evaluate(
                source,
                methodName:="C.M",
                expr:="If($23, $4.BaseType)",
                aliases:=ImmutableArray.Create(ObjectIdAlias(23, GetType(String)), ObjectIdAlias(4, GetType(Type))),
                errorMessage:=errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldstr      ""$23""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""String""
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_0027
  IL_0012:  pop
  IL_0013:  ldstr      ""$4""
  IL_0018:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_001d:  castclass  ""System.Type""
  IL_0022:  callvirt   ""Function System.Type.get_BaseType() As System.Type""
  IL_0027:  ret
}")
        End Sub

        <Fact>
        Public Sub PlaceholderMethodNameNormalization()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                ExceptionAlias(GetType(System.IO.IOException)),
                ExceptionAlias(GetType(System.Exception), stowed:=True))

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "If($ExcEptIOn, $SToWeDeXCePTioN)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  castclass  ""System.IO.IOException""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetStowedException() As System.Exception""
  IL_0013:  ret
}")
        End Sub

        <WorkItem(1101017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1101017")>
        <Fact>
        Public Sub NestedGenericValueType()
            Const source =
"Class C
    Friend Structure S(Of T)
        Friend F As T
    End Structure
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                VariableAlias("s", "C+S`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"))
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "s.F + 1",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  unbox.any  ""C.S(Of Integer)""
  IL_000f:  ldfld      ""C.S(Of Integer).F As Integer""
  IL_0014:  ldc.i4.1
  IL_0015:  add.ovf
  IL_0016:  ret
}")
        End Sub

        <Fact>
        Public Sub ArrayType()
            Const source =
"Class C
    Friend F As Object
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                VariableAlias("a", "C[]"),
                VariableAlias("b", "System.Int32[,], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"))
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "a(b(1, 0)).F",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       44 (0x2c)
  .maxstack  4
  IL_0000:  ldstr      ""a""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""C()""
  IL_000f:  ldstr      ""b""
  IL_0014:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0019:  castclass  ""Integer(,)""
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""Integer(*,*).Get""
  IL_0025:  ldelem.ref
  IL_0026:  ldfld      ""C.F As Object""
  IL_002b:  ret
}")
        End Sub

        ''' <summary>
        ''' The assembly-qualified type name may be from an
        ''' unrecognized assembly. For instance, if the type was
        ''' defined in a previous evaluation, say an anonymous
        ''' type (e.g.: evaluate "o" after "o = New With { .P = 1 }").
        ''' </summary>
        <WorkItem(1449, "https://github.com/dotnet/roslyn/issues/1449")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/1449")>
        Public Sub UnrecognizedAssembly()
            Const source =
"Friend Structure S(Of T)
    Friend F As T
End Structure
Class C
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()

            ' Unrecognized type.
            Dim context = CreateMethodContext(runtime, "C.M")
            context.CompileExpression(
                "o.P",
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray.Create(VariableAlias("o", "T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C25AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")),
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "...")

            ' Unrecognized array element type.
            context = CreateMethodContext(runtime, "C.M")
            context.CompileExpression(
                "a(0).P",
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray.Create(VariableAlias("a", "T[], 9BAC6622-86EB-4EC5-94A1-9A1E6D0C25AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")),
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "...")

            ' Unrecognized generic type argument.
            context = CreateMethodContext(runtime, "C.M")
            context.CompileExpression(
                "s.F",
                DkmEvaluationFlags.TreatAsExpression,
                ImmutableArray.Create(VariableAlias("s", "S`1[[T, 9BAC6622-86EB-4EC5-94A1-9A1E6D0C25AB, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")),
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "...")
        End Sub

        <Fact>
        Public Sub Variables()
            CheckVariable("$exception", ExceptionAlias(), valid:=True)
            CheckVariable("$eXCePTioN", ExceptionAlias(), valid:=True)
            CheckVariable("$stowedexception", ExceptionAlias(stowed:=True), valid:=True)
            CheckVariable("$stOwEdExcEptIOn", ExceptionAlias(stowed:=True), valid:=True)
            CheckVariable("$ReturnValue", ReturnValueAlias(), valid:=True)
            CheckVariable("$rEtUrnvAlUe", ReturnValueAlias(), valid:=True)
            CheckVariable("$ReturnValue0", ReturnValueAlias(0), valid:=True)
            CheckVariable("$ReturnValue21", ReturnValueAlias(21), valid:=True)
            CheckVariable("$ReturnValue3A", ReturnValueAlias(&H3A), valid:=False)
            CheckVariable("$33", ObjectIdAlias(33), valid:=True)
            CheckVariable("$03", ObjectIdAlias(3), valid:=False)
            CheckVariable("$3A", ObjectIdAlias(&H3A), valid:=False)
            CheckVariable("$0", ObjectIdAlias(1), valid:=False)
            CheckVariable("$", ObjectIdAlias(1), valid:=False)
            CheckVariable("$Unknown", ObjectIdAlias(1), valid:=False)
        End Sub

        Private Sub CheckVariable(variableName As String, [alias] As [Alias], valid As Boolean)
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                methodName:="C.M",
                expr:=variableName,
                aliases:=ImmutableArray.Create([alias]),
                errorMessage:=errorMessage)

            If valid Then
                Dim expectedNames = {"<>x.<>m0(C)", "<invalid-global-code>..ctor()"} ' Unnecessary <invalid-global-code> (DevDiv #1010243)
                Dim actualNames = testData.Methods.Keys
                AssertEx.SetEqual(expectedNames, actualNames)
            Else
                Assert.Equal(
                    String.Format("error BC30451: '{0}' is not declared. It may be inaccessible due to its protection level.", variableName),
                    errorMessage)
            End If
        End Sub

        <Fact>
        Public Sub CheckViability()
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                methodName:="C.M",
                aliases:=ImmutableArray.Create(ReturnValueAlias()),
                expr:="$ReturnValue(Of Object)",
                errorMessage:=errorMessage)
            Assert.Equal("error BC32045: '$ReturnValue' has no type parameters and so cannot have type arguments.", errorMessage)

            Const source = "
Class C
    Sub M()
        Microsoft.VisualBasic.VBMath.Randomize()
    End Sub
End Class
"

            ' Since the type of $ReturnValue2 is object, late binding will be attempted.
            testData = Evaluate(
                source,
                methodName:="C.M",
                expr:="$ReturnValue2()",
                aliases:=ImmutableArray.Create(ReturnValueAlias(2)),
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       19 (0x13)
  .maxstack  3
  IL_0000:  ldc.i4.2
  IL_0001:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetReturnValue(Integer) As Object""
  IL_0006:  ldc.i4.0
  IL_0007:  newarr     ""Object""
  IL_000c:  ldnull
  IL_000d:  call       ""Function Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateIndexGet(Object, Object(), String()) As Object""
  IL_0012:  ret
}")
        End Sub

        ''' <summary>
        ''' $exception may be accessed from closure class.
        ''' </summary>
        <Fact>
        Public Sub ExceptionInDisplayClass()
            Const source = "
Imports System

Class C
    Shared Function F(f1 as System.Func(Of Object)) As Object
        Return f1()
    End Function
    
    Shared Sub M(o As Object)
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                methodName:="C.M",
                expr:="F(Function() If(o, $exception))",
                aliases:=ImmutableArray.Create(ExceptionAlias()),
                errorMessage:=errorMessage)
            testData.GetMethodData("<>x._Closure$__0-0._Lambda$__0()").VerifyIL(
"{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>x._Closure$__0-0.$VB$Local_o As Object""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000f
  IL_0009:  pop
  IL_000a:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_000f:  ret
}")
        End Sub

        <Fact>
        Public Sub AssignException()
            Const source = "
Class C
    Shared Sub M(e As System.Exception)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileAssignment(
                "e",
                "If($exception.InnerException, $exception)",
                ImmutableArray.Create(ExceptionAlias()),
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  callvirt   ""Function System.Exception.get_InnerException() As System.Exception""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0013
  IL_000d:  pop
  IL_000e:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0013:  starg.s    V_0
  IL_0015:  ret
}
")
        End Sub

        <Fact>
        Public Sub AssignToException()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileAssignment(
                "$exception",
                "Nothing",
                ImmutableArray.Create(ExceptionAlias()),
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            ' CONSIDER: ERR_LValueRequired would be clearer.
            Assert.Equal("error BC30064: 'ReadOnly' variable cannot be the target of an assignment.", errorMessage)
        End Sub

        <WorkItem(1100849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100849")>
        <Fact>
        Public Sub PassByRef()
            Const source = "
Class C
    Shared Function F(Of T)(ByRef t1 As T) As T
        t1 = Nothing
        Return t1
    End Function    
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                "C.F")
            Dim aliases = ImmutableArray.Create(
                ExceptionAlias(),
                ReturnValueAlias(),
                ObjectIdAlias(1),
                VariableAlias("x", GetType(Integer)))
            Dim errorMessage As String = Nothing

            ' $exception
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "$exception = Nothing",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30064: 'ReadOnly' variable cannot be the target of an assignment.")
            testData = New CompilationTestData()
            context.CompileExpression(
                "F($exception)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            ' In VB, non-l-values can be passed by ref - we
            ' just synthesize a temp and pass that.
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (T V_0, //F
  System.Exception V_1)
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  stloc.1
  IL_0006:  ldloca.s   V_1
  IL_0008:  call       ""Function C.F(Of System.Exception)(ByRef System.Exception) As System.Exception""
  IL_000d:  ret
}")

            ' $ReturnValue
            testData = New CompilationTestData()
            context.CompileExpression(
                "$ReturnValue = Nothing",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30064: 'ReadOnly' variable cannot be the target of an assignment.")
            testData = New CompilationTestData()
            context.CompileExpression(
                "F($ReturnValue)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)

            ' Object id
            testData = New CompilationTestData()
            context.CompileExpression(
                "$1 = Nothing",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Equal(errorMessage, "error BC30064: 'ReadOnly' variable cannot be the target of an assignment.")
            testData = New CompilationTestData()
            context.CompileExpression(
                "F($1)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)

            ' Existing pseudo-variable
            testData = New CompilationTestData()
            context.CompileExpression(
                "x = Nothing",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (T V_0) //F
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Integer)(String) As Integer""
  IL_000a:  ldc.i4.0
  IL_000b:  stind.i4
  IL_000c:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                "F(x)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (T V_0) //F
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Integer)(String) As Integer""
  IL_000a:  call       ""Function C.F(Of Integer)(ByRef Integer) As Integer""
  IL_000f:  ret
}")

            ' Implicitly declared variable
            testData = New CompilationTestData()
            context.CompileExpression(
                "y = Nothing",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       43 (0x2b)
  .maxstack  4
  .locals init (T V_0, //F
                System.Guid V_1)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldnull
  IL_0029:  stind.ref
  IL_002a:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression(
                "F(y)",
                DkmEvaluationFlags.None,
                aliases,
                errorMessage,
                testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (T V_0, //F
                System.Guid V_1)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  call       ""Function C.F(Of Object)(ByRef Object) As Object""
  IL_002d:  pop
  IL_002e:  ret
}")
        End Sub

        ''' <summary>
        ''' Assembly-qualified type names from the debugger refer to runtime assemblies
        ''' which may be different versions than the assembly references in metadata.
        ''' </summary>
        <WorkItem(1087458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087458")>
        <Fact>
        Public Sub DifferentAssemblyVersion()
            Const sourceA =
"Public Class A(Of T)
End Class"
            Const sourceB =
"Class B(Of T)
End Class
Class C
    Shared Sub M()
        Dim o As New A(Of Object)()
    End Sub
End Class"
            Dim assemblyNameA = "397300B1-A"
            Dim publicKeyA = ImmutableArray.CreateRange(Of Byte)({&H00, &H24, &H00, &H00, &H04, &H80, &H00, &H00, &H94, &H00, &H00, &H00, &H06, &H02, &H00, &H00, &H00, &H24, &H00, &H00, &H52, &H53, &H41, &H31, &H00, &H04, &H00, &H00, &H01, &H00, &H01, &H00, &HED, &HD3, &H22, &HCB, &H6B, &HF8, &HD4, &HA2, &HFC, &HCC, &H87, &H37, &H04, &H06, &H04, &HCE, &HE7, &HB2, &HA6, &HF8, &H4A, &HEE, &HF3, &H19, &HDF, &H5B, &H95, &HE3, &H7A, &H6A, &H28, &H24, &HA4, &H0A, &H83, &H83, &HBD, &HBA, &HF2, &HF2, &H52, &H20, &HE9, &HAA, &H3B, &HD1, &HDD, &HE4, &H9A, &H9A, &H9C, &HC0, &H30, &H8F, &H01, &H40, &H06, &HE0, &H2B, &H95, &H62, &H89, &H2A, &H34, &H75, &H22, &H68, &H64, &H6E, &H7C, &H2E, &H83, &H50, &H5A, &HCE, &H7B, &H0B, &HE8, &HF8, &H71, &HE6, &HF7, &H73, &H8E, &HEB, &H84, &HD2, &H73, &H5D, &H9D, &HBE, &H5E, &HF5, &H90, &HF9, &HAB, &H0A, &H10, &H7E, &H23, &H48, &HF4, &HAD, &H70, &H2E, &HF7, &HD4, &H51, &HD5, &H8B, &H3A, &HF7, &HCA, &H90, &H4C, &HDC, &H80, &H19, &H26, &H65, &HC9, &H37, &HBD, &H52, &H81, &HF1, &H8B, &HCD})
            Dim compilationA1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(1, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef_v20},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceA1 = compilationA1.EmitToImageReference()
            Dim assemblyNameB = "397300B1-B"
            Dim compilationB1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameB, New Version(1, 2, 2, 2)),
                {sourceB},
                references:={MscorlibRef_v20, referenceA1},
                options:=TestOptions.DebugDll)

            ' Use mscorlib v4.0.0.0 and A v2.1.2.1 at runtime.
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            compilationB1.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim compilationA2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(2, 1, 2, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef_v20},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceA2 = compilationA2.EmitToImageReference()
            Dim runtime = CreateRuntimeInstance(
                assemblyNameB,
                ImmutableArray.Create(MscorlibRef, referenceA2).AddIntrinsicAssembly(),
                exeBytes,
                SymReaderFactory.CreateReader(pdbBytes))

            ' GetType(Exception), GetType(A(Of B(Of Object))), GetType(B(Of A(Of Object)()))
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                ExceptionAlias("System.Exception, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                ObjectIdAlias(1, "A`1[[B`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], 397300B1-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null]], 397300B1-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=1f8a32457d187bf3"),
                ObjectIdAlias(2, "B`1[[A`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][], 397300B1-A, Version=2.1.2.1, Culture=neutral, PublicKeyToken=1f8a32457d187bf3]], 397300B1-B, Version=1.2.2.2, Culture=neutral, PublicKeyToken=null"))
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression(
                "If(If($exception, $1), $2)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (A(Of Object) V_0) //o
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_0018
  IL_0008:  pop
  IL_0009:  ldstr      ""$1""
  IL_000e:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0013:  castclass  ""A(Of B(Of Object))""
  IL_0018:  dup
  IL_0019:  brtrue.s   IL_002b
  IL_001b:  pop
  IL_001c:  ldstr      ""$2""
  IL_0021:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0026:  castclass  ""B(Of A(Of Object)())""
  IL_002b:  ret
}")
        End Sub

        ''' <summary>
        ''' The assembly-qualified type may reference an assembly
        ''' outside of the current module and its references.
        ''' </summary>
        <WorkItem(1092680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092680")>
        <Fact>
        Public Sub TypeOutsideModule()
            Const sourceA =
"Imports System
Public Class A(Of T)
    Public Shared Sub M(f As Action)
        Dim o As Object
        Try
            f()
        Catch e As Exception
        End Try
    End Sub
End Class"
            Const sourceB =
"Imports System
Class E
    Inherits Exception
    Friend F As Object
End Class
Class B
    Shared Sub Main()
        A(Of Integer).M(Sub()
                Throw New E()
            End Sub)
    End Sub
End Class"
            Dim assemblyNameA = "0B93FF0B-31A2-47C8-B24D-16A2D77AB5C5"
            Dim compilationA = CreateCompilationWithMscorlibAndVBRuntime(MakeSources(sourceA, assemblyName:=assemblyNameA), options:=TestOptions.DebugDll)
            Dim exeA As Byte() = Nothing
            Dim pdbA As Byte() = Nothing
            Dim referencesA As ImmutableArray(Of MetadataReference) = Nothing
            compilationA.EmitAndGetReferences(exeA, pdbA, referencesA)
            Dim referenceA = AssemblyMetadata.CreateFromImage(exeA).GetReference()

            Dim assemblyNameB = "9BBC6622-86EB-4EC5-94A1-9A1E6D0C24B9"
            Dim compilationB = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(MakeSources(sourceB, assemblyName:=assemblyNameB), options:=TestOptions.DebugDll, additionalRefs:={referenceA})
            Dim exeB As Byte() = Nothing
            Dim pdbB As Byte() = Nothing
            Dim referencesB As ImmutableArray(Of MetadataReference) = Nothing
            compilationB.EmitAndGetReferences(exeB, pdbB, referencesB)
            Dim referenceB = AssemblyMetadata.CreateFromImage(exeB).GetReference()

            Dim modulesBuilder = ArrayBuilder(Of ModuleInstance).GetInstance()
            modulesBuilder.Add(MscorlibRef.ToModuleInstance(fullImage:=Nothing, symReader:=Nothing))
            modulesBuilder.Add(referenceA.ToModuleInstance(fullImage:=exeA, symReader:=SymReaderFactory.CreateReader(pdbA)))
            modulesBuilder.Add(referenceB.ToModuleInstance(fullImage:=exeB, symReader:=SymReaderFactory.CreateReader(pdbB)))
            modulesBuilder.Add(ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance(fullImage:=Nothing, symReader:=Nothing))

            Using runtime = New RuntimeInstance(modulesBuilder.ToImmutableAndFree())
                Dim context = CreateMethodContext(
                    runtime,
                    "A.M")
                Dim aliases = ImmutableArray.Create(
                    ExceptionAlias("E, 9BBC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
                Dim errorMessage As String = Nothing
                Dim testData = New CompilationTestData()
                context.CompileExpression(
                    "$exception",
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    errorMessage,
                    testData)
                Assert.Null(errorMessage)
                testData.GetMethodData("<>x(Of T).<>m0").VerifyIL(
"{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Object V_0, //o
                System.Exception V_1)
  IL_0000:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetException() As System.Exception""
  IL_0005:  castclass  ""E""
  IL_000a:  ret
}")
                context = CreateMethodContext(
                    runtime,
                    "A.M")
                aliases = ImmutableArray.Create(
                    ObjectIdAlias(1, "A`1[[B, 9BBC6622-86EB-4EC5-94A1-9A1E6D0C24B9, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], 0B93FF0B-31A2-47C8-B24D-16A2D77AB5C5, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"))
                Dim resultProperties As ResultProperties = Nothing
                Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                testData = New CompilationTestData()
                context.CompileAssignment(
                    "o",
                    "$1",
                    aliases,
                    DebuggerDiagnosticFormatter.Instance,
                    resultProperties,
                    errorMessage,
                    missingAssemblyIdentities,
                    EnsureEnglishUICulture.PreferredOrNull,
                    testData)
                Assert.Empty(missingAssemblyIdentities)
                testData.GetMethodData("<>x(Of T).<>m0").VerifyIL(
"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (Object V_0, //o
                System.Exception V_1)
  IL_0000:  ldstr      ""$1""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""A(Of B)""
  IL_000f:  stloc.0
  IL_0010:  ret
}")
            End Using
        End Sub

        Private Overloads Function Evaluate(
            source As String,
            methodName As String,
            expr As String,
            aliases As ImmutableArray(Of [Alias]),
            ByRef errorMessage As String) As CompilationTestData

            Dim comp = CreateCompilationWithReferences(
                {Parse(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef, MsvbRef},
                options:=TestOptions.DebugDll)

            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName)
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(
                    expr,
                    DkmEvaluationFlags.TreatAsExpression,
                    aliases,
                    errorMessage,
                    testData)
            Return testData
        End Function

    End Class
End Namespace
