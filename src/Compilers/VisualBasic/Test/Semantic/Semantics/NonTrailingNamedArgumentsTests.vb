' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    <CompilerTrait(CompilerFeature.NonTrailingNamedArgs)>
    Public Class NonTrailingNamedArgumentsTests
        Inherits BasicTestBase

        ReadOnly parseOptions As VisualBasicParseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_6)

        <Fact>
        Public Sub TestSimple()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub
    Shared Sub M(b As Long, a As Long)
        System.Console.Write($"Second {b} {a}. ")
    End Sub
    Shared Sub Main()
        M(a:=1, 2)
        M(3, a:=4)
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2. Second 3 4.",
                                            parseOptions:=parseOptions)
            verifier.VerifyDiagnostics()

            Dim tree = verifier.Compilation.SyntaxTrees.First()
            Dim model = verifier.Compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim firstInvocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(2)
            Assert.Equal("M(a:=1, 2)", firstInvocation.ToString())
            Assert.Equal("Sub C.M(a As System.Int32, b As System.Int32)",
                model.GetSymbolInfo(firstInvocation).Symbol.ToTestDisplayString())

            Dim secondInvocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(3)
            Assert.Equal("M(3, a:=4)", secondInvocation.ToString())
            Assert.Equal("Sub C.M(b As System.Int64, a As System.Int64)",
                model.GetSymbolInfo(secondInvocation).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestPositionalUnaffected()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(first As Integer, other As Integer)
        System.Console.Write($"{first} {other}")
    End Sub
    Shared Sub Main()
        M(1, first:=2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=parseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30455: Argument not specified for parameter 'other' of 'Public Shared Sub M(first As Integer, other As Integer)'.
        M(1, first:=2)
        ~
BC30274: Parameter 'first' of 'Public Shared Sub M(first As Integer, other As Integer)' already has a matching argument.
        M(1, first:=2)
             ~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(1, first:=2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestPositionalUnaffected2()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer, Optional c As Integer = 1)
        System.Console.Write($"M {a} {b}")
    End Sub
    Shared Sub Main()
        M(c:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=parseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'c' is used out-of-position but is followed by an unnamed argument
        M(c:=1, 2)
             ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(c:=1, 2)", invocation.ToString())
            AssertEx.Equal({"Sub C.M(a As System.Int32, b As System.Int32, [c As System.Int32 = 1])"},
                model.GetSymbolInfo(invocation).CandidateSymbols.Select(Function(c) c.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub TestBadNonTrailing()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(Optional a As Integer = 1, Optional b As Integer = 2, Optional c As Integer = 3)
        System.Console.Write($"First {a} {b}. ")
    End Sub
    Shared Sub Main()
        Dim valueB = 2
        Dim valueC = 3
        M(c:=valueC, valueB)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=parseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'c' is used out-of-position but is followed by an unnamed argument
        M(c:=valueC, valueB)
             ~~~~~~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim firstInvocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(c:=valueC, valueB)", firstInvocation.ToString())
            Assert.Null(model.GetSymbolInfo(firstInvocation).Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, model.GetSymbolInfo(firstInvocation).CandidateReason)
            Assert.Equal("Sub C.M([a As System.Int32 = 1], [b As System.Int32 = 2], [c As System.Int32 = 3])",
                model.GetSymbolInfo(firstInvocation).CandidateSymbols.Single().ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestBadNonTrailing2()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(Optional a As Integer = 1, Optional b As Integer = 2, Optional c As Integer = 3)
    End Sub
    Shared Sub M(Optional c As Long = 1, Optional b As Long = 2)
        System.Console.Write($"Second {c} {b}. ")
    End Sub
    Shared Sub Main()
        Dim valueB = 2
        Dim valueC = 3
        M(c:=valueC, valueB)
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="Second 3 2.", parseOptions:=parseOptions)
            verifier.VerifyDiagnostics()

            Dim tree = verifier.Compilation.SyntaxTrees.First()
            Dim model = verifier.Compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(c:=valueC, valueB)", invocation.ToString())
            Assert.Equal("Sub C.M([c As System.Int64 = 1], [b As System.Int64 = 2])",
                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestBadNonTrailing3()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(Optional a As Long = 1, Optional b As Long = 2, Optional c As Long = 3)
    End Sub
    Shared Sub M(Optional c As Integer = 1, Optional b As Integer = 2)
        System.Console.Write($"Second {c} {b}.")
    End Sub
    Shared Sub Main()
        Dim valueB = 2
        Dim valueC = 3
        M(c:=valueC, valueB)
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="Second 3 2.", parseOptions:=parseOptions)
            verifier.VerifyDiagnostics()

            Dim tree = verifier.Compilation.SyntaxTrees.First()
            Dim model = verifier.Compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(c:=valueC, valueB)", invocation.ToString())
            Assert.Equal("Sub C.M([c As System.Int32 = 1], [b As System.Int32 = 2])",
                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestParams()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer, ParamArray c() As Integer)
    End Sub
    Shared Sub Main()
        M(b:=2, 3, 4)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source, parseOptions:=parseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'b' is used out-of-position but is followed by an unnamed argument
        M(b:=2, 3, 4)
             ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestParams2()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer, ParamArray c() As Integer)
        System.Console.Write($"{a} {b} {c(0)} {c(1)} Length:{c.Length}")
    End Sub
    Shared Sub Main()
        M(1, b:=2, 3, 4)
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="1 2 3 4 Length:2", parseOptions:=parseOptions)
            verifier.VerifyDiagnostics()
        End Sub
    End Class
End Namespace
