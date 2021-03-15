' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    <CompilerTrait(CompilerFeature.NonTrailingNamedArgs)>
    Public Class NonTrailingNamedArgumentsTests
        Inherits BasicTestBase

        Private Shared ReadOnly latestParseOptions As VisualBasicParseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest)

        <Fact>
        Public Sub TestSimpleWithOldLangVer()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer)
    End Sub
    Shared Sub Main()
        M(a:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M(a:=1, 2)
                ~
                                        </errors>)
        End Sub

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
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim tree = verifier.Compilation.SyntaxTrees.First()
            Dim model = verifier.Compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim firstInvocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(2)
            Assert.Equal("M(a:=1, 2)", firstInvocation.ToString())
            Assert.Equal("Sub C.M(a As System.Int32, b As System.Int32)",
                model.GetSymbolInfo(firstInvocation).Symbol.ToTestDisplayString())

            Dim firstNamedArgA = nodes.OfType(Of NameColonEqualsSyntax)().ElementAt(0)
            Assert.Equal("a:=1", firstNamedArgA.Parent.ToString())
            Dim firstASymbol = model.GetSymbolInfo(firstNamedArgA.Name)
            Assert.Equal(SymbolKind.Parameter, firstASymbol.Symbol.Kind)
            Assert.Equal("a", firstASymbol.Symbol.Name)
            Assert.Equal("Sub C.M(a As System.Int32, b As System.Int32)", firstASymbol.Symbol.ContainingSymbol.ToTestDisplayString())

            Dim secondInvocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(3)
            Assert.Equal("M(3, a:=4)", secondInvocation.ToString())
            Assert.Equal("Sub C.M(b As System.Int64, a As System.Int64)",
                model.GetSymbolInfo(secondInvocation).Symbol.ToTestDisplayString())

            Dim secondNamedArgA = nodes.OfType(Of NameColonEqualsSyntax)().ElementAt(1)
            Assert.Equal("a:=4", secondNamedArgA.Parent.ToString())
            Dim secondASymbol = model.GetSymbolInfo(secondNamedArgA.Name)
            Assert.Equal(SymbolKind.Parameter, secondASymbol.Symbol.Kind)
            Assert.Equal("a", secondASymbol.Symbol.Name)
            Assert.Equal("Sub C.M(b As System.Int64, a As System.Int64)", secondASymbol.Symbol.ContainingSymbol.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestSimpleConstructor()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Sub New(a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub
    Shared Sub Main()
        Dim c = New C(a:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        Dim c = New C(a:=1, 2)
                            ~
                                            </errors>)

        End Sub

        <Fact>
        Public Sub TestSimpleThis()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Sub New(a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub
    Sub New()
        Me.New(a:=1, 2)
    End Sub
    Shared Sub Main()
        Dim c = New C()
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        Me.New(a:=1, 2)
                     ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestSimpleBase()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Sub New(a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub
End Class
Class Derived
    Inherits C

    Sub New()
        MyBase.New(a:=1, 2)
    End Sub
    Shared Sub Main()
        Dim derived = New Derived()
    End Sub
End Class
    </file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestSimpleExtension()
            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Module Extensions
    <System.Runtime.CompilerServices.Extension>
    Sub M(ByVal c As C, a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub
End Module
Class C
    Shared Sub Main()
        Dim c = New C()
        c.M(a:=1, 2)
    End Sub
End Class
    ]]></file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        c.M(a:=1, 2)
                  ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestSimpleDelegate()
            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Class C
    Delegate Sub MyDelegate(a As Integer, b As Integer)

    Shared Sub M(a As Integer, b As Integer)
        System.Console.Write($"First {a} {b}. ")
    End Sub

    Shared Sub Main()
        Dim f As MyDelegate = AddressOf M
        f(a:=1, 2)
    End Sub
End Class
    ]]></file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        f(a:=1, 2)
                ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestSimpleIndexer()
            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Class C
    Default ReadOnly Property Item(a As Integer, b As Integer) As Integer
        Get
            System.Console.Write($"First {a} {b}. ")
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim c = New C()
        Dim x = c(a:=1, 2)
    End Sub
End Class
    ]]></file>
</compilation>
            Dim verifier = CompileAndVerify(source, expectedOutput:="First 1 2.",
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier.VerifyDiagnostics()

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        Dim x = c(a:=1, 2)
                        ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestSimpleError()
            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Class C
    Sub New(a As Integer, b As Integer)
    End Sub

    Default ReadOnly Property Item(a As Integer, b As Integer) As Integer
        Get
            Return 0
        End Get
    End Property

    Shared Sub Main()
        Dim c = New C(b:=1, 2)
        Dim x = c(b:=1, 2)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source,
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'b' is used out-of-position but is followed by an unnamed argument
        Dim c = New C(b:=1, 2)
                      ~
BC37302: Named argument 'b' is used out-of-position but is followed by an unnamed argument
        Dim x = c(b:=1, 2)
                  ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestMetadataAndPESymbols()
            Dim lib_vb =
<compilation>
    <file name="Lib.vb"><![CDATA[
Public Class C
    Public Shared Sub M(a As Integer, b As Integer)
        System.Console.Write($"{a} {b}. ")
    End Sub
End Class
    ]]></file>
</compilation>

            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Class D
    Shared Sub Main()
        C.M(a:=1, 2)
    End Sub
End Class
    ]]></file>
</compilation>

            Dim libComp = CreateCompilationWithMscorlib40AndVBRuntime(lib_vb, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            Dim verifier1 = CompileAndVerify(source, expectedOutput:="1 2.", references:={libComp.ToMetadataReference()},
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier1.VerifyDiagnostics()

            Dim verifier2 = CompileAndVerify(source, expectedOutput:="1 2.", references:={libComp.EmitToImageReference()},
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))
            verifier2.VerifyDiagnostics()
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
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
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
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
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestPositionalUnaffected2WithOmitted()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(a As Integer, b As Integer, Optional c As Integer = 1, Optional d As Integer = 2)
        System.Console.Write($"M {a} {b}")
    End Sub
    Shared Sub Main()
        M(c:=1, 2,)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'c' is used out-of-position but is followed by an unnamed argument
        M(c:=1, 2,)
          ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(1)
            Assert.Equal("M(c:=1, 2,)", invocation.ToString())
            AssertEx.Equal({"Sub C.M(a As System.Int32, b As System.Int32, [c As System.Int32 = 1], [d As System.Int32 = 2])"},
                model.GetSymbolInfo(invocation).CandidateSymbols.Select(Function(c) c.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub TestNamedParams()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(ByVal ParamArray c() As Integer)
    End Sub
    Shared Sub Main()
        M(c:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)

            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(c:=1, 2)
          ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(c:=1, 2)", invocation.ToString())
            AssertEx.Equal({"Sub C.M(ParamArray c As System.Int32())"},
                model.GetSymbolInfo(invocation).CandidateSymbols.Select(Function(c) c.ToTestDisplayString()))
        End Sub

        <Fact>
        Public Sub TestNamedParams2()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(ParamArray x() As Integer)
    End Sub
    Shared Sub Main()
        M(1, x:=2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(1, x:=2)
             ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(1, x:=2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestTwiceNamedParams()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(ParamArray x() As Integer)
    End Sub
    Shared Sub Main()
        M(x:=1, x:=2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(x:=1, x:=2)
          ~
BC30587: Named argument cannot match a ParamArray parameter.
        M(x:=1, x:=2)
                ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(x:=1, x:=2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestTwiceNamedParameters()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(x As Integer, y As Integer, z As Integer)
    End Sub
    Shared Sub Main()
        M(x:=1, x:=2, 3)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30274: Parameter 'x' of 'Public Shared Sub M(x As Integer, y As Integer, z As Integer)' already has a matching argument.
        M(x:=1, x:=2, 3)
                ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(x:=1, x:=2, 3)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestTwiceNamedParametersWithOldLangVer()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(x As Integer, y As Integer, z As Integer)
    End Sub
    Shared Sub Main()
        M(x:=1, x:=2, 3)
    End Sub
End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30274: Parameter 'x' of 'Public Shared Sub M(x As Integer, y As Integer, z As Integer)' already has a matching argument.
        M(x:=1, x:=2, 3)
                ~
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M(x:=1, x:=2, 3)
                      ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(x:=1, x:=2, 3)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestNamedParams3()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(x As Integer, ParamArray y() As Integer)
    End Sub
    Shared Sub Main()
        M(y:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(y:=1, 2)
          ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(y:=1, 2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestNamedParams4()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(x As Integer, ParamArray y() As Integer)
    End Sub
    Shared Sub Main()
        M(x:=1, y:=2, 3)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(x:=1, y:=2, 3)
                ~
                                        </errors>)
        End Sub

        <Fact>
        Public Sub TestNamedInvalidParams()

            Dim source =
<compilation>
    <file name="Program.vb">
class C

    Shared Sub M(ParamArray x() As Integer, y As Integer)
    End Sub
    Shared Sub Main()
        M(x:=1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30192: End of parameter list expected. Cannot define parameters after a paramarray parameter.
    Shared Sub M(ParamArray x() As Integer, y As Integer)
                                            ~~~~~~~~~~~~
BC30311: Value of type 'Integer' cannot be converted to 'Integer()'.
        M(x:=1, 2)
             ~
                                        </errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().Single()
            Assert.Equal("M(x:=1, 2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestNamedParams5()

            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(x As Integer, ParamArray y() As Integer)
        System.Console.Write($"x={x} y(0)={y(0)} y.Length={y.Length}")
    End Sub
    Shared Sub Main()
        M(y:=1, x:=2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions, options:=TestOptions.DebugExe)
            comp.AssertTheseDiagnostics(<errors>
BC30587: Named argument cannot match a ParamArray parameter.
        M(y:=1, x:=2)
          ~
                                        </errors>)
            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of InvocationExpressionSyntax)().ElementAt(2)
            Assert.Equal("M(y:=1, x:=2)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestBadNonTrailing()
            Dim source =
<compilation>
    <file name="Program.vb">
Class C
    Shared Sub M(Optional a As Integer = 1, Optional b As Integer = 2, Optional c As Integer = 3)
        System.Console.Write($"First a b. ")
    End Sub
    Shared Sub Main()
        Dim valueB = 2
        Dim valueC = 3
        M(c:=valueC, valueB)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37302: Named argument 'c' is used out-of-position but is followed by an unnamed argument
        M(c:=valueC, valueB)
          ~
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
        Public Sub TestPickGoodOverload()
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
            Dim verifier = CompileAndVerify(source, expectedOutput:="Second 3 2.", parseOptions:=latestParseOptions)
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
        Public Sub TestPickGoodOverload2()
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
            Dim verifier = CompileAndVerify(source, expectedOutput:="Second 3 2.", parseOptions:=latestParseOptions)
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
        Public Sub TestDynamicInvocation()
            Dim source =
<compilation>
    <file name="Program.vb">
Option Strict Off
Class C
    Shared Sub Main()
        Dim d = New Object()
        d.M(a:=1, 2)
        d.M(1, 2)
    End Sub
End Class
    </file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC37304: Named argument specifications must appear after all fixed arguments have been specified in a late bound invocation.
        d.M(a:=1, 2)
                  ~
                                        </errors>)

            Dim comp2 = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))
            comp2.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        d.M(a:=1, 2)
                  ~
                                         </errors>)
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
            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(source, parseOptions:=latestParseOptions)
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
            Dim verifier = CompileAndVerify(source, expectedOutput:="1 2 3 4 Length:2", parseOptions:=latestParseOptions)
            verifier.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub TestInAttribute()

            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class MyAttribute
    Inherits Attribute

    Public Dim P As Integer
    Public Dim condition As Boolean

    Public Sub New(condition As Boolean, other As Integer)
    End Sub
End Class

<MyAttribute(condition:=true, 42)>
<MyAttribute(condition:=true, P:=1, 42)>
<MyAttribute(42, condition:=True)>
Public Class C
End Class
    ]]></file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30455: Argument not specified for parameter 'condition' of 'Public Sub New(condition As Boolean, other As Integer)'.
<MyAttribute(condition:=true, 42)>
 ~~~~~~~~~~~
BC30455: Argument not specified for parameter 'other' of 'Public Sub New(condition As Boolean, other As Integer)'.
<MyAttribute(condition:=true, 42)>
 ~~~~~~~~~~~
BC30661: Field or property '' is not found.
<MyAttribute(condition:=true, 42)>
                              ~
BC37303: Named argument expected.
<MyAttribute(condition:=true, 42)>
                              ~
BC30455: Argument not specified for parameter 'condition' of 'Public Sub New(condition As Boolean, other As Integer)'.
<MyAttribute(condition:=true, P:=1, 42)>
 ~~~~~~~~~~~
BC30455: Argument not specified for parameter 'other' of 'Public Sub New(condition As Boolean, other As Integer)'.
<MyAttribute(condition:=true, P:=1, 42)>
 ~~~~~~~~~~~
BC30661: Field or property '' is not found.
<MyAttribute(condition:=true, P:=1, 42)>
                                    ~
BC37303: Named argument expected.
<MyAttribute(condition:=true, P:=1, 42)>
                                    ~
BC30455: Argument not specified for parameter 'other' of 'Public Sub New(condition As Boolean, other As Integer)'.
<MyAttribute(42, condition:=True)>
 ~~~~~~~~~~~
                                        ]]></errors>)

            Dim tree = comp.SyntaxTrees.First()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim invocation = nodes.OfType(Of AttributeSyntax)().ElementAt(1)
            Assert.Equal("MyAttribute(condition:=true, 42)", invocation.ToString())
            Assert.Null(model.GetSymbolInfo(invocation).Symbol)
        End Sub

        <Fact>
        Public Sub TestInAttribute2()

            Dim source =
<compilation>
    <file name="Program.vb"><![CDATA[
Imports System

<AttributeUsage(AttributeTargets.All, AllowMultiple:=True)>
Public Class MyAttribute
    Inherits Attribute

    Public Dim P As Integer
    Public Dim c As Integer
    Public Sub New()
    End Sub
End Class

<MyAttribute(c:=3, 2)>
<MyAttribute(P:=1, c:=3, 2)>
Public Class C
End Class
    ]]></file>
</compilation>
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30661: Field or property '' is not found.
<MyAttribute(c:=3, 2)>
                   ~
BC37303: Named argument expected.
<MyAttribute(c:=3, 2)>
                   ~
BC30661: Field or property '' is not found.
<MyAttribute(P:=1, c:=3, 2)>
                         ~
BC37303: Named argument expected.
<MyAttribute(P:=1, c:=3, 2)>
                         ~
                                        ]]></errors>)
        End Sub

        <Fact>
        Public Sub OmittedArgumentAfterNamedArgument()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub M(y As Integer, Optional z As Integer = 1)
        M(y:=Nothing,)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M(y:=Nothing,)
                     ~
</expected>)
        End Sub

        <Fact>
        Public Sub TestInIf()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Module Module1
    Sub M(b As Boolean)
        Dim x = If(b:=b, Nothing, Nothing)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC33105: 'If' operands cannot be named arguments.
        Dim x = If(b:=b, Nothing, Nothing)
                   ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestInIndexer()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class C
    Public ReadOnly Property P(ByVal index1 As Integer, Optional ByVal index2 As Integer = -1) As String
        Get
            Return System.String.Format($"{index1} {index2}")
        End Get
    End Property
    Sub M()
        System.Console.Write(P(index1:=1, 2))
        System.Console.Write(P(index1:=1, ))
        System.Console.Write(P(index1:=1, 0 to 5))
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        System.Console.Write(P(index1:=1, 2))
                                          ~
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        System.Console.Write(P(index1:=1, ))
                                          ~
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        System.Console.Write(P(index1:=1, 0 to 5))
                                          ~
BC32017: Comma, ')', or a valid expression continuation expected.
        System.Console.Write(P(index1:=1, 0 to 5))
                                            ~~~~
</expected>)
        End Sub

        <Fact>
        Public Sub TestInIndexer2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class C
    Public ReadOnly Property P(ByVal index1 As Integer, Optional ByVal index2 As Integer = -1) As String
        Get
            Return System.String.Format($"{index1} {index2}. ")
        End Get
    End Property
    Sub M()
        System.Console.Write(P(index1:=1, 2))
        System.Console.Write(P(index1:=3, ))
        System.Console.Write(P(index1:=4, index2:=5))
        System.Console.Write(P(index2:=7, index1:=6))
    End Sub
    Shared Sub Main()
        Dim c = New C()
        c.M()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options:=TestOptions.DebugExe)

            CompilationUtils.AssertNoDiagnostics(compilation)
            CompileAndVerify(compilation, expectedOutput:="1 2. 3 -1. 4 5. 6 7.")
        End Sub

        <Fact>
        Public Sub TestInConstructor()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class C
    Public Sub New(input1 As Integer, Optional input2 As Integer = 2, Optional input3 As Integer = 3)
    End Sub
    Shared Sub Main()
        Dim c = New C(input1:=1, , bad:=3)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options:=TestOptions.DebugExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <errors>
BC30272: 'bad' is not a parameter of 'Public Sub New(input1 As Integer, [input2 As Integer = 2], [input3 As Integer = 3])'.
        Dim c = New C(input1:=1, , bad:=3)
                                   ~~~
                                                                 </errors>)
        End Sub

        <Fact>
        Public Sub TestInConstructor2()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Class C
    Public Sub New(input1 As Integer, Optional input2 As Integer = 2, Optional input3 As Integer = 3)
        System.Console.Write($"{input1} {input2} {input3}. ")
    End Sub
    Shared Sub Main()
        Dim c = New C(input1:=1, , 3)
        c = New C(input1:=1, , 0 to 4)
        c = New C(input1:=1, , input3:=5)
        c = New C(input1:=1, 0 to 6, input3:=7)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef,
                parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options:=TestOptions.DebugExe)

            CompilationUtils.AssertNoDiagnostics(compilation)
            CompileAndVerify(compilation, expectedOutput:="1 2 3. 1 2 4. 1 2 5. 1 6 7.")
        End Sub

    End Class
End Namespace
