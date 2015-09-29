' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Emit
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

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "System.Console.WriteLine(1+1)"))
        End Sub


        <Fact>
        Public Sub ReturnStatement()
            Dim source = <text>
Return Foo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            c.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ReturnFromNonFunction, "Return Foo").WithLocation(2, 1),
                Diagnostic(ERRID.ERR_NameNotDeclared1, "Foo").WithArguments("Foo").WithLocation(2, 8))
        End Sub

        <Fact>
        Public Sub MeKeyword()
            Dim source = <text>
Sub Foo
    Me.Bar
End Sub

Sub Bar
    System.Console.WriteLine(1+1)
End Sub

Me.Foo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"))
        End Sub

        <Fact>
        Public Sub MyBaseAndMyClassKeyword()
            Dim source = <text>
Sub Foo
    MyClass.Bar
End Sub

Sub Bar
    System.Console.WriteLine(1+1)
End Sub

MyBase.Foo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, LatestVbReferences)

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyClass").WithArguments("MyClass"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyBase").WithArguments("MyBase"))
        End Sub

        <Fact>
        Public Sub SubStatement()
            Dim source = <text>
Sub Foo
    System.Console.WriteLine(1+1)
End Sub

Foo
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
Sub Foo
    System.Console.WriteLine(1+1)
End Sub
    </file>
    </compilation>

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(Diagnostic(ERRID.ERR_InvalidInNamespace, "Sub Foo"))
        End Sub

        <Fact>
        Public Sub FunctionStatement()
            Dim source = <text>
Function Foo As Integer
    Return 3
End Function

System.Console.WriteLine(Foo)
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

            Dim s0 = VisualBasicCompilation.CreateSubmission("s0.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "Dim x = New With {.a = 1}", options:=TestOptions.Interactive),
                                                  references:=references,
                                                  returnType:=GetType(Object))

            Dim s__ = VisualBasicCompilation.CreateSubmission("s__.dll",
                                                   syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                       "Dim y = New With {.b = 1}", options:=TestOptions.Interactive),
                                                   previousSubmission:=s0,
                                                   references:=references,
                                                   returnType:=GetType(Object))

            Dim s1 = VisualBasicCompilation.CreateSubmission("s1.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "Dim y = New With {.a = New With {.b = 1} }", options:=TestOptions.Interactive),
                                                  previousSubmission:=s0,
                                                  references:=references,
                                                  returnType:=GetType(Object))

            Dim s2 = VisualBasicCompilation.CreateSubmission("s2.dll",
                                                  syntaxTree:=VisualBasicSyntaxTree.ParseText(
                                                      "? x.GetType() Is y.GetType()", options:=TestOptions.Interactive),
                                                  previousSubmission:=s1,
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
        <WorkItem(530986)>
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
            Dim submission = VisualBasicCompilation.CreateSubmission("sub1", tree, {MscorlibRef})
            Dim model = submission.GetSemanticModel(tree)
            Assert.Empty(model.LookupLabels(source.Length - 1))
        End Sub

        <WorkItem(3795, "https:'github.com/dotnet/roslyn/issues/3795")>
        <Fact>
        Public Sub ErrorInUsing()
            Dim submission = VisualBasicCompilation.CreateSubmission("sub1", Parse("Imports Unknown", options:=TestOptions.Script), {MscorlibRef})

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

    End Class
End Namespace

