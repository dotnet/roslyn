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

        ReadOnly latestParseOptions As VisualBasicParseOptions = TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest)

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
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_3))
            comp.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.6 or greater to use non-trailing named arguments.
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
                                            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_6))
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
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=latestParseOptions)
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
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=latestParseOptions)
            comp.AssertTheseDiagnostics(<errors>
BC30455: Argument not specified for parameter 'a' of 'Public Shared Sub M(a As Integer, b As Integer, [c As Integer = 1])'.
        M(c:=1, 2)
        ~
BC30455: Argument not specified for parameter 'b' of 'Public Shared Sub M(a As Integer, b As Integer, [c As Integer = 1])'.
        M(c:=1, 2)
        ~
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
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=latestParseOptions)

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

        '        [Fact]
        '        Public void TestNamedParams2()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(params int[] x)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(1, x: 2);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (9,14): Error CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given
        '                //         M(1, x: 2);
        '                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "x").WithArguments("x").WithLocation(9, 14)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().Single();
        '            Assert.Equal("M(1, x: 2)", invocation.ToString());
        '            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        '        }

        '        [Fact]
        '        Public void TestTwiceNamedParams()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(params int[] x)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(x: 1, x: 2);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (9,17): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         M(x: 1, x: 2);
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(9, 17)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().Single();
        '            Assert.Equal("M(x: 1, x: 2)", invocation.ToString());
        '            Assert.Equal("void C.M(params System.Int32[] x)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        '        }

        '        [Fact]
        '        Public void TestTwiceNamedParamsWithOldLangVer()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(int x, int y, int z)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(x: 1, x: 2, 3);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.Regular7_1);
        '            comp.VerifyDiagnostics(
        '                // (9,17): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         M(x: 1, x: 2, 3);
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(9, 17),
        '                // (9,23): Error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 Or greater To allow non-trailing named arguments.
        '                //         M(x: 1, x: 2, 3);
        '                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "3").WithArguments("7.2").WithLocation(9, 23)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().Single();
        '            Assert.Equal("M(x: 1, x: 2, 3)", invocation.ToString());
        '            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        '        }

        '        [Fact]
        '        Public void TestNamedParams3()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(int x, params int[] y)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(y: 1, 2);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (9,11): Error CS8321: Named argument 'y' is used out-of-position but is followed by an unnamed argument
        '                //         M(y: 1, 2);
        '                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "y").WithArguments("y").WithLocation(9, 11)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().Single();
        '            Assert.Equal("M(y: 1, 2)", invocation.ToString());
        '            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        '        }

        '        [Fact]
        '        Public void TestNamedParams4()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(int x, params int[] y)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(x: 1, y: 2, 3);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (9,9): Error CS1501: No overload For method 'M' takes 3 arguments
        '                //         M(x: 1, y: 2, 3);
        '                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "3").WithLocation(9, 9)
        '                );
        '        }

        '        [Fact]
        '        Public void TestNamedInvalidParams()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(params int[] x, int y)
        '    {
        '    }
        '    static void Main()
        '    {
        '        M(x: 1, 2);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (4,19): Error CS0231: A params parameter must be the last parameter In a formal parameter list
        '                //     static void M(params int[] x, int y)
        '                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] x").WithLocation(4, 19),
        '                // (9,14): Error CS1503 :  Argument 1: cannot convert from 'int' to 'int'
        '                //         M(x: 1, 2);
        '                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "int").WithLocation(9, 14)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().Single();
        '            Assert.Equal("M(x: 1, 2)", invocation.ToString());
        '            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        '        }

        '        [Fact]
        '        Public void TestNamedParams5()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M(int x, params int[] y)
        '    {
        '        System.Console.Write($""x={x} y[0]={y[0]} y.Length={y.Length}"");
        '    }
        '    static void Main()
        '    {
        '        M(y: 1, x: 2);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest, options: TestOptions.DebugExe);
        '            comp.VerifyDiagnostics();
        '            CompileAndVerify(comp, expectedOutput: "x=2 y[0]=1 y.Length=1");

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < InvocationExpressionSyntax > ().ElementAt(1);
        '            Assert.Equal("M(y: 1, x: 2)", invocation.ToString());
        '            Assert.Equal("void C.M(System.Int32 x, params System.Int32[] y)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        '        }


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
            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source, parseOptions:=latestParseOptions)
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
BC30455: Argument not specified for parameter 'a' of 'Public Shared Sub M(a As Integer, b As Integer, ParamArray c As Integer())'.
        M(b:=2, 3, 4)
        ~
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

        '        [Fact]
        '        Public void TestInAttribute()
        '        {
        '            var source = @"
        'using System;

        '[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        'public class MyAttribute : Attribute
        '{
        '    public int P { get; set; }
        '	public MyAttribute(bool condition, int other) { }
        '}

        '[MyAttribute(condition: true, 42)]
        '[MyAttribute(condition: true, P = 1, 42)]
        '[MyAttribute(42, condition: true)]
        'public class C
        '{
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (12,38): Error CS1016: Named attribute argument expected
        '                // [MyAttribute(condition: true, P = 1, 42)]
        '                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "42").WithLocation(12, 38),
        '                // (13,18): Error CS1744: Named argument 'condition' specifies a parameter for which a positional argument has already been given
        '                // [MyAttribute(42, condition: true)]
        '                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "condition").WithArguments("condition").WithLocation(13, 18)
        '                );

        '            var tree = comp.SyntaxTrees.First();
        '            var model = comp.GetSemanticModel(tree);
        '            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
        '            var invocation = nodes.OfType < AttributeSyntax > ().ElementAt(1);
        '            Assert.Equal("MyAttribute(condition: true, 42)", invocation.ToString());
        '            Assert.Equal("MyAttribute..ctor(System.Boolean condition, System.Int32 other)",
        '                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        '        }

        '        [Fact]
        '        Public void TestInAttribute2()
        '        {
        '            var source = @"
        'using System;

        '[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        'public class MyAttribute : Attribute
        '{
        '    public int P { get; set; }
        '	public MyAttribute(int a = 1, int b = 2, int c = 3) { }
        '}

        '[MyAttribute(c:3, 2)]
        '[MyAttribute(P=1, c:3, 2)]
        'public class C
        '{
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (11,14): Error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
        '                // [MyAttribute(c:3, 2)]
        '                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(11, 14),
        '                // (12,21): Error CS1016: Named attribute argument expected
        '                // [MyAttribute(P=1, c:3, 2)]
        '                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "3").WithLocation(12, 21),
        '                // (12,24): Error CS1016: Named attribute argument expected
        '                // [MyAttribute(P=1, c:3, 2)]
        '                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "2").WithLocation(12, 24)
        '                );
        '        }

        '        [Fact]
        '        Public void TestErrorsDoNotCascadeInInvocation()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M()
        '    {
        '        M(x: 1, x: 2, __arglist());
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (6,17): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         M(x: 1, x: 2, __arglist());
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(6, 17)
        '                );
        '        }

        '        [Fact]
        '        Public void TestErrorsDoNotCascadeInArglist()
        '        {
        '            var source = @"
        'class C
        '{
        '    static void M()
        '    {
        '        M(__arglist(x: 1, x: 2, __arglist()));
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (6,27): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         M(__arglist(x: 1, x: 2, __arglist()));
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(6, 27),
        '                // (6,33): Error CS0226: An __arglist expression may only appear inside Of a Call Or New expression
        '                //         M(__arglist(x: 1, x: 2, __arglist()));
        '                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist()").WithLocation(6, 33)
        '                );
        '        }

        '        [Fact]
        '        Public void TestErrorsDoNotCascadeInConstructorInitializer()
        '        {
        '            var source = @"
        'class C
        '{
        '    C() : this(x: 1, x: 2, 3) { }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (4,22): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //     C() : this(x: 1, x: 2, 3) { }
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(4, 22)
        '                );
        '        }

        '        [Fact]
        '        Public void TestErrorsDoNotCascadeInObjectCreation()
        '        {
        '            var source = @"
        'class C
        '{
        '    void M()
        '    {
        '        new C(x: 1, x: 2, 3);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (6,21): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         New C(x: 1, x: 2, 3);
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(6, 21)
        '                );
        '        }

        '        [Fact]
        '        Public void TestErrorsDoNotCascadeInElementAccess()
        '        {
        '            var source = @"
        'class C
        '{
        '    int this[int i] { get { throw null; } set { throw null; } }
        '    void M()
        '    {
        '        var c = new C();
        '        System.Console.Write(c[x: 1, x: 2, 3]);
        '    }
        '}";
        '            var comp = CreateStandardCompilation(source, parseOptions:  TestOptions.RegularLatest);
        '            comp.VerifyDiagnostics(
        '                // (8,38): Error CS1740: Named argument 'x' cannot be specified multiple times
        '                //         System.Console.Write(c[x: 1, x: 2, 3]);
        '                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(8, 38)
        '                );
        '        }
        '    }

    End Class
End Namespace
