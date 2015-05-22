' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.VisualBasic
Imports Microsoft.CodeAnalysis.Scripting.Test
Imports Roslyn.Test.Utilities
Imports Xunit


Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic.Test

    Public Class InteractiveSessionTests
        Inherits BasicTestBase

        Shared Sub New()
            ScriptBuilder.DisableJitOptimizations = True
        End Sub

#Region "Chaining"
        <Fact()>
        Public Sub Fields()
            Dim engine = New VisualBasicScriptEngine()
            Dim session As Session = engine.CreateSession()

            session.Execute("Dim x As Integer = 1")
            session.Execute("Dim y As Integer = 2")
            Dim result = session.Execute("?x + y")
            Assert.Equal(3, result)
        End Sub

        <Fact()>
        Public Sub ChainingAnonymousTypeTemplates()
            Dim references As IEnumerable(Of MetadataReference) =
                    {MscorlibRef, MsvbRef, SystemCoreRef}

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

#End Region

#Region "Statements and Expressions"

        <Fact()>
        Public Sub TestTopLevelClassBinding()
            Dim source = <text>
Class C
    Dim f As C
End Class
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            Dim typeSyntax = DirectCast(DirectCast(tree.GetCompilationUnitRoot().Members(0), ClassBlockSyntax).Members(0), FieldDeclarationSyntax).Declarators(0).AsClause.Type

            Dim model = c.GetSemanticModel(tree)
            Dim info = model.GetSpeculativeSymbolInfo(typeSyntax.Position, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace)
            Dim type = TryCast(info.Symbol, TypeSymbol)

            Assert.Equal("C", type.Name)
            Assert.Equal(c.ScriptClass, type.ContainingType)
        End Sub

        <Fact()>
        Public Sub CallStatement()
            Dim source = <text>
System.Console.WriteLine(1+1)
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            CompileAndVerify(c, expectedOutput:="2")
        End Sub

        <Fact()>
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

        <Fact()>
        Public Sub ReturnStatement()
            Dim source = <text>
Return Foo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Return Foo").WithArguments("Return"))
        End Sub

        <Fact()>
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
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "Me").WithArguments("Me"))
        End Sub

        <Fact()>
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
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            c.VerifyDiagnostics(Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyClass").WithArguments("MyClass"),
                                Diagnostic(ERRID.ERR_KeywordNotAllowedInScript, "MyBase").WithArguments("MyBase"))
        End Sub

        <Fact()>
        Public Sub SubStatement()
            Dim source = <text>
Sub Foo
    System.Console.WriteLine(1+1)
End Sub

Foo
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            CompileAndVerify(c, expectedOutput:="2")
        End Sub

        <Fact()>
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

        <Fact()>
        Public Sub FunctionStatement()
            Dim source = <text>
Function Foo As Integer
    Return 3
End Function

System.Console.WriteLine(Foo)
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            CompileAndVerify(c, expectedOutput:="3")
        End Sub

        <Fact()>
        Public Sub ForStatement()
            Dim source = <text>
For i = 0 To 2
    System.Console.Write(i)
Next
</text>.Value

            Dim tree = VisualBasicSyntaxTree.ParseText(source, options:=TestOptions.Script)
            Dim c = VisualBasicCompilation.Create("Test", {tree}, {MscorlibRef})

            CompileAndVerify(c, expectedOutput:="012")
        End Sub

        <Fact()>
        Public Sub StatementExpressions_LineContinuation()
            Dim source = <text>
?1 _
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim result = engine.CreateCollectibleSession().Execute(source)
            Assert.Equal(result, 1)
        End Sub

        <Fact()>
        Public Sub StatementExpressions_IntLiteral()
            Dim source = <text>
?1
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim result = engine.CreateCollectibleSession().Execute(source)
            Assert.Equal(result, 1)
        End Sub

        <Fact()>
        Public Sub StatementExpressions_Nothing()
            Dim source = <text>
?  Nothing
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            Dim result = session.Execute(source)
            Assert.Equal(result, Nothing)
        End Sub

        Public Class B
            Public x As Integer = 1, w As Integer = 4
        End Class

        <WorkItem(10856, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub IfStatement()
            Dim source = <text>
Dim x As Integer
If (True)
   x = 5
Else
   x = 6
End If

?x + 1
</text>.Value

            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            Dim result = session.Execute(source)

            Assert.Equal(6, result)
        End Sub

        Private Function CreateSubmission(code As String, options As VisualBasicParseOptions, Optional expectedErrorCount As Integer = 0) As VisualBasicCompilation
            Dim submission = VisualBasicCompilation.CreateSubmission(
                "sub",
                references:={MetadataReference.CreateFromAssembly(GetType(Object).Assembly)},
                syntaxTree:=Parse(code, options:=options))

            Assert.Equal(expectedErrorCount, submission.GetDiagnostics(CompilationStage.Declare, True).Length())

            Return submission
        End Function

        Private Shared Sub TestResult(s As VisualBasicCompilation, expectedType As SpecialType?, expectedHasValue As Boolean)
            Dim hasValue As Boolean
            Dim type = s.GetSubmissionResultType(hasValue)
            Assert.Equal(expectedType, If(type IsNot Nothing, type.SpecialType, DirectCast(Nothing, SpecialType?)))
            Assert.Equal(expectedHasValue, hasValue)
        End Sub

        Private Shared Sub TestResult(s As VisualBasicCompilation, expectedType As Func(Of TypeSymbol, Boolean), expectedHasValue As Boolean)
            Dim hasValue As Boolean
            Dim type = s.GetSubmissionResultType(hasValue)
            Assert.True(expectedType(type), "unexpected type")
            Assert.Equal(expectedHasValue, hasValue)
        End Sub

        <Fact()>
        Public Sub SubmissionResultType()
            Dim submission = VisualBasicCompilation.CreateSubmission("sub")
            Dim hasValue As Boolean
            Assert.Equal(SpecialType.System_Void, submission.GetSubmissionResultType(hasValue).SpecialType)
            Assert.False(hasValue)

            TestResult(CreateSubmission("?1", TestOptions.Script, expectedErrorCount:=1), expectedType:=SpecialType.System_Void, expectedHasValue:=False)
            TestResult(CreateSubmission("?1", TestOptions.Interactive), expectedType:=SpecialType.System_Int32, expectedHasValue:=True)

            ' TODO (tomat): optional ?
            ' TestResult(CreateSubmission("1", OptionsInteractive), expectedType:=SpecialType.System_Int32, expectedHasValue:=True)

            TestResult(CreateSubmission(<text>
Sub Foo() 
End Sub
        </text>.Value, TestOptions.Interactive), expectedType:=SpecialType.System_Void, expectedHasValue:=False)

            TestResult(CreateSubmission("Imports System", TestOptions.Interactive), expectedType:=SpecialType.System_Void, expectedHasValue:=False)
            TestResult(CreateSubmission("Dim i As Integer", TestOptions.Interactive), expectedType:=SpecialType.System_Void, expectedHasValue:=False)
            TestResult(CreateSubmission("System.Console.WriteLine()", TestOptions.Interactive), expectedType:=SpecialType.System_Void, expectedHasValue:=False)
            TestResult(CreateSubmission("?System.Console.WriteLine()", TestOptions.Interactive), expectedType:=SpecialType.System_Void, expectedHasValue:=True)
            TestResult(CreateSubmission("System.Console.ReadLine()", TestOptions.Interactive), expectedType:=SpecialType.System_String, expectedHasValue:=True)
            TestResult(CreateSubmission("?System.Console.ReadLine()", TestOptions.Interactive), expectedType:=SpecialType.System_String, expectedHasValue:=True)
            TestResult(CreateSubmission("?Nothing", TestOptions.Interactive), expectedType:=SpecialType.System_Object, expectedHasValue:=True)
            TestResult(CreateSubmission("?AddressOf System.Console.WriteLine", TestOptions.Interactive), expectedType:=DirectCast(Nothing, SpecialType?), expectedHasValue:=True)
            TestResult(CreateSubmission("?Function(x) x", TestOptions.Interactive), expectedType:=AddressOf IsDelegateType, expectedHasValue:=True)
        End Sub

        <WorkItem(530404)>
        <Fact()>
        Public Sub DiagnosticsPass()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()
            session.AddReference(GetType(System.Linq.Expressions.Expression).Assembly)
            session.Execute(
"Function F(e As System.Linq.Expressions.Expression(Of System.Func(Of Object))) As Object
    Return e.Compile()()
End Function")
            ScriptingTestHelpers.AssertCompilationError(
                session,
                "F(Function()
                        Return Nothing
                    End Function)",
                Diagnostic(ERRID.ERR_StatementLambdaInExpressionTree, "Function()
                        Return Nothing
                    End Function").WithLocation(1, 3))
        End Sub

        ''' <summary>
        ''' LookupSymbols should not include the submission class.
        ''' </summary>
        <WorkItem(530986)>
        <Fact()>
        Public Sub LookupSymbols()
            Dim text = "1 + "
            Dim compilation = CreateSubmission(text, TestOptions.Interactive, expectedErrorCount:=1)
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

#End Region

#Region "Anonymous types"

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()

            session.Execute(
    <text>
Option Infer On
Dim a = New With { .f = 1 }
</text>.Value)

            session.Execute(
    <text>
Option Infer On
Dim b = New With { Key .f = 1 }
</text>.Value)

            Dim result = session.Execute(Of Object)(
            <![CDATA[
    Option Infer On
    Dim c = New With { .F = 222 }
    Dim d = New With { Key .F = 777 }
    ? (a.GetType() is c.GetType()).ToString() _
        & " " & (a.GetType() is b.GetType()).ToString() _ 
        & " " & (b.GetType() is d.GetType()).ToString()
    ]]>.Value)

            Assert.Equal("True False True", result.ToString)
        End Sub

        <Fact>
        Public Sub AnonymousTypes_TopLevel_MultipleSubmissions2()
            Dim engine = New VisualBasicScriptEngine()
            Dim session = engine.CreateSession()

            session.Execute(
    <text>
Option Infer On
Dim a = Sub()
        End Sub
</text>.Value)

            session.Execute(
    <text>
Option Infer On
Dim b = Function () As Integer
            Return 0
        End Function
</text>.Value)

            Dim result = session.Execute(Of Object)(
            <![CDATA[
    Option Infer On
    Dim c = Sub()
            End Sub
    Dim d = Function () As Integer
                Return 0
            End Function
    ? (a.GetType() is c.GetType()).ToString() _
        & " " & (a.GetType() is b.GetType()).ToString() _ 
        & " " & (b.GetType() is d.GetType()).ToString()
    ]]>.Value)

            Assert.Equal("True False True", result.ToString)
        End Sub

#End Region

    End Class

End Namespace
