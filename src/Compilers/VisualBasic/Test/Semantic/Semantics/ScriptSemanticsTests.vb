' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ScriptSemanticsTests
        Inherits BasicTestBase

        <WorkItem(530404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530404")>
        <Fact>
        Public Sub DiagnosticsPass()
            Dim source0 = "
Function F(e As System.Linq.Expressions.Expression(Of System.Func(Of Object))) As Object
    Return e.Compile()()
End Function"

            Dim c0 = CreateSubmission(source0, {SystemCoreRef})

            Dim source1 = "
F(Function()
    Return Nothing
  End Function)
"
            Dim c1 = CreateSubmission(source1, {SystemCoreRef}, previous:=c0)

            AssertTheseDiagnostics(c1,
<errors>
BC36675: Statement lambdas cannot be converted to expression trees.
F(Function()
  ~~~~~~~~~~~
</errors>)
        End Sub

        <Fact>
        <WorkItem(10023, "https://github.com/dotnet/roslyn/issues/10023")>
        Public Sub Errors_01()
            Dim code = "System.Console.WriteLine(1)"
            Dim compilationUnit = VisualBasic.SyntaxFactory.ParseCompilationUnit(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            Dim syntaxTree = compilationUnit.SyntaxTree
            Dim compilation = CreateCompilationWithMscorlib461({syntaxTree}, assemblyName:="Errors_01", options:=TestOptions.ReleaseExe)
            Dim semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            Dim node5 As MemberAccessExpressionSyntax = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol)

            compilation.AssertTheseDiagnostics(
<expected>
BC30420: 'Sub Main' was not found in 'Errors_01'.
BC30001: Statement is not valid in a namespace.
System.Console.WriteLine(1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )

            compilation = CreateCompilationWithMscorlib461({syntaxTree}, options:=TestOptions.ReleaseExe.WithScriptClassName("Script"), assemblyName:="Errors_01")
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Null(semanticModel.GetSymbolInfo(node5.Name).Symbol)

            compilation.AssertTheseDiagnostics(
<expected>
BC30420: 'Sub Main' was not found in 'Errors_01'.
BC30001: Statement is not valid in a namespace.
System.Console.WriteLine(1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree}, options:=TestOptions.ReleaseExe)
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree}, options:=TestOptions.ReleaseExe.WithScriptClassName("Script"))
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString())

            CompileAndVerify(compilation, expectedOutput:="1").VerifyDiagnostics()

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree}, options:=TestOptions.ReleaseExe.WithScriptClassName(""))
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(
<expected>
BC2014: the value '' is invalid for option 'ScriptClassName'
</expected>
            )

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree}, options:=TestOptions.ReleaseExe.WithScriptClassName(Nothing))
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(
<expected>
BC2014: the value 'Nothing' is invalid for option 'ScriptClassName'
</expected>
            )

            syntaxTree = SyntaxFactory.ParseSyntaxTree(code, options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            compilation = CreateCompilationWithMscorlib461AndVBRuntime({syntaxTree}, options:=TestOptions.ReleaseExe.WithScriptClassName("a" + ChrW(0) + "b"))
            semanticModel = compilation.GetSemanticModel(syntaxTree, True)
            node5 = ErrorTestsGetNode(syntaxTree)
            Assert.Equal("WriteLine", node5.Name.ToString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel.GetSymbolInfo(node5.Name).Symbol.ToTestDisplayString())

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidSwitchValue).WithArguments("ScriptClassName", "a" + ChrW(0) + "b").WithLocation(1, 1)
                )
        End Sub

        <Fact>
        <WorkItem(10023, "https://github.com/dotnet/roslyn/issues/10023")>
        Public Sub Errors_02()
            Dim compilationUnit = VisualBasic.SyntaxFactory.ParseCompilationUnit("System.Console.WriteLine(1)", options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            Dim syntaxTree1 = compilationUnit.SyntaxTree
            Dim syntaxTree2 = SyntaxFactory.ParseSyntaxTree("System.Console.WriteLine(2)", options:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
            Dim node1 As MemberAccessExpressionSyntax = ErrorTestsGetNode(syntaxTree1)
            Assert.Equal("WriteLine", node1.Name.ToString())
            Dim node2 As MemberAccessExpressionSyntax = ErrorTestsGetNode(syntaxTree2)
            Assert.Equal("WriteLine", node2.Name.ToString())

            Dim compilation = CreateCompilationWithMscorlib461({syntaxTree1, syntaxTree2})
            Dim semanticModel1 = compilation.GetSemanticModel(syntaxTree1, True)
            Dim semanticModel2 = compilation.GetSemanticModel(syntaxTree2, True)
            Assert.Null(semanticModel1.GetSymbolInfo(node1.Name).Symbol)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel2.GetSymbolInfo(node2.Name).Symbol.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(
<expected>
BC30001: Statement is not valid in a namespace.
System.Console.WriteLine(1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )

            compilation = CreateCompilationWithMscorlib461({syntaxTree2, syntaxTree1})
            semanticModel1 = compilation.GetSemanticModel(syntaxTree1, True)
            semanticModel2 = compilation.GetSemanticModel(syntaxTree2, True)
            Assert.Null(semanticModel1.GetSymbolInfo(node1.Name).Symbol)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", semanticModel2.GetSymbolInfo(node2.Name).Symbol.ToTestDisplayString())

            compilation.AssertTheseDiagnostics(
<expected>
BC30001: Statement is not valid in a namespace.
System.Console.WriteLine(1)
~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>
            )
        End Sub

        Private Shared Function ErrorTestsGetNode(syntaxTree As SyntaxTree) As MemberAccessExpressionSyntax
            Dim node1 = DirectCast(syntaxTree.GetRoot(), CompilationUnitSyntax)
            Dim node3 = DirectCast(node1.Members.First(), ExpressionStatementSyntax)
            Dim node4 = DirectCast(node3.Expression, InvocationExpressionSyntax)
            Dim node5 = DirectCast(node4.Expression, MemberAccessExpressionSyntax)
            Return node5
        End Function

    End Class
End Namespace

