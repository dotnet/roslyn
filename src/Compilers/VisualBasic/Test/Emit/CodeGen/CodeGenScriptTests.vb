' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit

    ' TODO (tomat): review tests

    Public Class CodeGenScriptTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestTopLevelClassBinding()
            Dim source = <text>
Class C
    Dim f As C
End Class
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            Dim typeSyntax = DirectCast(DirectCast(tree.GetCompilationUnitRoot().Members(0), ClassBlockSyntax).Members(0), FieldDeclarationSyntax).Declarators(0).AsClause.Type

            Dim model = c.GetSemanticModel(tree)
            Dim info = model.GetSpeculativeSymbolInfo(typeSyntax.Position, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace)
            Dim type = TryCast(info.Symbol, TypeSymbol)

            Assert.Equal("C", type.Name)
            Assert.Equal(c.ScriptClass, type.ContainingType)
        End Sub

        <Fact>
        Public Sub CallStatement()
            Dim source = <text>
System.Console.WriteLine(1+1)
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            CompileAndVerify(c, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub CallStatement_RegularCode()
            Dim source =
    <compilation>
        <file>
System.Console.WriteLine(1+1)
    </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "System.Console.WriteLine(1+1)"))
        End Sub

        <Fact>
        Public Sub ReturnStatement()
            Dim source = <text>
Return 1
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = CreateCompilationWithMscorlib461({tree})

            c.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub MeKeyword()
            Dim source = <text>
Sub Goo
    Me.Bar
End Sub

Sub Bar
    System.Console.WriteLine(1+1)
End Sub

Me.Goo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"))
        End Sub

        <Fact>
        Public Sub MyBaseAndMyClassKeyword()
            Dim source = <text>
Sub Goo
    MyClass.Bar
End Sub

Sub Bar
    System.Console.WriteLine(1+1)
End Sub

MyBase.Goo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyClass").WithArguments("MyClass"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyBase").WithArguments("MyBase"))
        End Sub

        <Fact>
        Public Sub SubStatement()
            Dim source = <text>
Sub Goo
    System.Console.WriteLine(1+1)
End Sub

Goo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            CompileAndVerify(c, expectedOutput:="2")
        End Sub

        <Fact>
        Public Sub SubStatement_RegularCode()
            Dim source =
    <compilation>
        <file>
Sub Goo
    System.Console.WriteLine(1+1)
End Sub
    </file>
    </compilation>

            CreateCompilationWithMscorlib40(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidInNamespace, "Sub Goo"))
        End Sub

        <Fact>
        Public Sub FunctionStatement()
            Dim source = <text>
Function Goo As Integer
    Return 3
End Function

System.Console.WriteLine(Goo)
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            CompileAndVerify(c, expectedOutput:="3")
        End Sub

        <Fact>
        Public Sub ForStatement()
            Dim source = <text>
For i = 0 To 2
    System.Console.Write(i)
Next
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            CompileAndVerify(c, expectedOutput:="012")
        End Sub

        <Fact>
        Public Sub ChainingAnonymousTypeTemplates()
            Dim references = LatestVbReferences

            Dim s0 = VisualBasicCompilation.CreateScriptCompilation("s0.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "Dim x = New With {.a = 1}", options:=TestOptions.Script),
                                                  references:=references,
                                                  returnType:=GetType(Object))

            Dim s__ = VisualBasicCompilation.CreateScriptCompilation("s__.dll",
                                                   syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                       "Dim y = New With {.b = 1}", options:=TestOptions.Script),
                                                   previousScriptCompilation:=s0,
                                                   references:=references,
                                                   returnType:=GetType(Object))

            Dim s1 = VisualBasicCompilation.CreateScriptCompilation("s1.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "Dim y = New With {.a = New With {.b = 1} }", options:=TestOptions.Script),
                                                  previousScriptCompilation:=s0,
                                                  references:=references,
                                                  returnType:=GetType(Object))

            Dim s2 = VisualBasicCompilation.CreateScriptCompilation("s2.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "? x.GetType() Is y.GetType()", options:=TestOptions.Script),
                                                  previousScriptCompilation:=s1,
                                                  references:=references,
                                                  returnType:=GetType(Object))

            Using stream As MemoryStream = New MemoryStream()
                s2.Emit(stream)
            End Using

            Assert.True(s2.AnonymousTypeManager.AreTemplatesSealed)
            Assert.Equal(0, s2.AnonymousTypeManager.AllCreatedTemplates.Length)

            Assert.True(s1.AnonymousTypeManager.AreTemplatesSealed)
            Assert.Equal(1, s1.AnonymousTypeManager.AllCreatedTemplates.Length)

            Assert.True(s0.AnonymousTypeManager.AreTemplatesSealed)
            Assert.Equal(1, s0.AnonymousTypeManager.AllCreatedTemplates.Length)

            Assert.False(s__.AnonymousTypeManager.AreTemplatesSealed)
        End Sub

        ''' <summary>
        ''' LookupSymbols should not include the submission class.
        ''' </summary>
        <WorkItem(530986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530986")>
        <Fact>
        Public Sub LookupSymbols()
            Dim text = "1 + "
            Dim compilation = CreateSubmission(text)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ObsoleteLineNumbersAreLabels, "1 "))

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim symbols = model.LookupSymbols(text.Length)

            ' Should return some symbols, but not the submission class.
            Assert.True(symbols.Length > 0)
            For Each symbol In symbols
                If symbol.Kind = SymbolKind.NamedType Then
                    Dim type = DirectCast(symbol, NamedTypeSymbol)
                    Assert.False(type.IsScriptClass)
                    Assert.False(type.IsSubmissionClass)
                    Assert.NotEqual(type.TypeKind, TypeKind.Submission)
                End If
            Next

            ' #1010871
            'Assert.False(symbols.Any(Function(s) s.Name = "Roslyn"))
        End Sub

        <WorkItem(3817, "https://github.com/dotnet/roslyn/issues/3817")>
        <Fact>
        Public Sub LabelLookup()
            Const source = "Imports System : 1"
            Dim tree = Parse(source, options:=TestOptions.Script)
            Dim submission = VisualBasicCompilation.CreateScriptCompilation("sub1", tree, {MscorlibRef})
            Dim model = submission.GetSemanticModel(tree)
            Assert.Empty(model.LookupLabels(source.Length - 1))
        End Sub

        <WorkItem(3795, "https://github.com/dotnet/roslyn/issues/3795")>
        <Fact>
        Public Sub ErrorInUsing()
            Dim submission = VisualBasicCompilation.CreateScriptCompilation("sub1", Parse("Imports Unknown", options:=TestOptions.Script), {MscorlibRef})

            Dim expectedErrors = <errors><![CDATA[
BC40056: Namespace or type specified in the Imports 'Unknown' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports Unknown
        ~~~~~~~
]]></errors>

            ' Emit produces the same diagnostics as GetDiagnostics (below).
            Using stream As New MemoryStream()
                Dim emitResult = submission.Emit(stream)
                Assert.False(emitResult.Success)
                emitResult.Diagnostics.AssertTheseDiagnostics(expectedErrors)
            End Using

            submission.GetDiagnostics().AssertTheseDiagnostics(expectedErrors)
        End Sub

        ''' <summary>
        ''' The script entry point should complete synchronously.
        ''' </summary>
        <WorkItem(4495, "https://github.com/dotnet/roslyn/issues/4495")>
        <Fact>
        Public Sub ScriptEntryPoint()
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(
                <compilation>
                    <file name="a.vbx"><![CDATA[
System.Threading.Tasks.Task.Delay(100)
System.Console.Write("complete")
]]></file>
                </compilation>,
                parseOptions:=TestOptions.Script,
                options:=TestOptions.DebugExe,
                references:={SystemCoreRef})
            Dim verifier = CompileAndVerify(comp, expectedOutput:="complete")
            Dim methodData = verifier.TestData.GetMethodData("Script.<Initialize>")
            Assert.Equal("System.Threading.Tasks.Task(Of Object)", methodData.Method.ReturnType.ToDisplayString())
            methodData.VerifyIL(
"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Script.VB$StateMachine_1_<Initialize> V_0)
  IL_0000:  newobj     ""Sub Script.VB$StateMachine_1_<Initialize>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$VB$Me As Script""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$State As Integer""
  IL_0014:  ldloc.0
  IL_0015:  call       ""Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).Create() As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_001a:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_001f:  ldloc.0
  IL_0020:  ldflda     ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).Start(Of Script.VB$StateMachine_1_<Initialize>)(ByRef Script.VB$StateMachine_1_<Initialize>)""
  IL_002c:  nop
  IL_002d:  ldloc.0
  IL_002e:  ldflda     ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_0033:  call       ""Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).get_Task() As System.Threading.Tasks.Task(Of Object)""
  IL_0038:  ret
}")
            methodData = verifier.TestData.GetMethodData("Script.<Main>")
            Assert.True(methodData.Method.ReturnsVoid)
            methodData.VerifyIL(
"{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (System.Runtime.CompilerServices.TaskAwaiter V_0)
  IL_0000:  newobj     ""Sub Script..ctor()""
  IL_0005:  callvirt   ""Function Script.<Initialize>() As System.Threading.Tasks.Task(Of Object)""
  IL_000a:  callvirt   ""Function System.Threading.Tasks.Task.GetAwaiter() As System.Runtime.CompilerServices.TaskAwaiter""
  IL_000f:  stloc.0
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""Sub System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
  IL_0017:  ret
}")
        End Sub

        <Fact>
        Public Sub SubmissionEntryPoint()
            Dim parseOptions = TestOptions.Script
            Dim references = {MscorlibRef_v4_0_30316_17626, SystemCoreRef, MsvbRef}
            Dim source0 = <![CDATA[
System.Threading.Tasks.Task.Delay(100)
System.Console.Write("complete")
]]>
            Dim s0 = VisualBasicCompilation.CreateScriptCompilation(
                "s0.dll",
                syntaxTree:=Parse(source0.Value, parseOptions),
                references:=references)
            ' PEVerify: Error: Assembly name contains leading spaces or path or extension.
            Dim verifier = CompileAndVerify(s0, verify:=Verification.FailsPEVerify)
            Dim methodData = verifier.TestData.GetMethodData("Script.<Initialize>")
            Assert.Equal("System.Threading.Tasks.Task(Of Object)", methodData.Method.ReturnType.ToDisplayString())
            methodData.VerifyIL(
"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (Script.VB$StateMachine_1_<Initialize> V_0)
  IL_0000:  newobj     ""Sub Script.VB$StateMachine_1_<Initialize>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$VB$Me As Script""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$State As Integer""
  IL_0014:  ldloc.0
  IL_0015:  call       ""Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).Create() As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_001a:  stfld      ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_001f:  ldloc.0
  IL_0020:  ldflda     ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""Sub System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).Start(Of Script.VB$StateMachine_1_<Initialize>)(ByRef Script.VB$StateMachine_1_<Initialize>)""
  IL_002c:  nop
  IL_002d:  ldloc.0
  IL_002e:  ldflda     ""Script.VB$StateMachine_1_<Initialize>.$Builder As System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object)""
  IL_0033:  call       ""Function System.Runtime.CompilerServices.AsyncTaskMethodBuilder(Of Object).get_Task() As System.Threading.Tasks.Task(Of Object)""
  IL_0038:  ret
}")
            methodData = verifier.TestData.GetMethodData("Script.<Factory>")
            Assert.Equal("System.Threading.Tasks.Task(Of Object)", methodData.Method.ReturnType.ToDisplayString())
            methodData.VerifyIL(
"{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""Sub Script..ctor(Object())""
  IL_0006:  callvirt   ""Function Script.<Initialize>() As System.Threading.Tasks.Task(Of Object)""
  IL_000b:  ret
}")
        End Sub

        <ConditionalFact(GetType(NoUsedAssembliesValidation))> ' https://github.com/dotnet/roslyn/issues/40682: The test hook is blocked by this issue.
        <WorkItem(40682, "https://github.com/dotnet/roslyn/issues/40682")>
        Public Sub ScriptEntryPoint_MissingMethods()
            Dim comp = CreateCompilationWithMscorlib40(
                <compilation>
                    <file name="a.vbx"><![CDATA[
System.Console.WriteLine(1)
]]></file>
                </compilation>,
                parseOptions:=TestOptions.Script,
                options:=TestOptions.DebugExe)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_MissingRuntimeHelper).WithArguments("Task.GetAwaiter").WithLocation(1, 1))
        End Sub

    End Class
End Namespace

