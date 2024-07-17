// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.NonTrailingNamedArgs)]
    public class NonTrailingNamedArgumentsTests : CompilingTestBase
    {
        [Fact]
        public void TestSimple()
        {
            var source = @"
class C
{
    static void M(int a, int b)
    {
        System.Console.Write($""First {a} {b}. "");
    }
    static void M(long b, long a)
    {
        System.Console.Write($""Second {b} {a}. "");
    }
    static void Main()
    {
        M(a: 1, 2);
        M(3, a: 4);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "First 1 2. Second 3 4.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.First();
            var model = verifier.Compilation.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var firstInvocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(2);
            Assert.Equal("M(a: 1, 2)", firstInvocation.ToString());
            Assert.Equal("void C.M(System.Int32 a, System.Int32 b)",
                model.GetSymbolInfo(firstInvocation).Symbol.ToTestDisplayString());

            var firstNamedArgA = nodes.OfType<NameColonSyntax>().ElementAt(0);
            Assert.Equal("a: 1", firstNamedArgA.Parent.ToString());
            var firstASymbol = model.GetSymbolInfo(firstNamedArgA.Name);
            Assert.Equal(SymbolKind.Parameter, firstASymbol.Symbol.Kind);
            Assert.Equal("a", firstASymbol.Symbol.Name);
            Assert.Equal("void C.M(System.Int32 a, System.Int32 b)", firstASymbol.Symbol.ContainingSymbol.ToTestDisplayString());

            var secondInvocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(3);
            Assert.Equal("M(3, a: 4)", secondInvocation.ToString());
            Assert.Equal("void C.M(System.Int64 b, System.Int64 a)",
                model.GetSymbolInfo(secondInvocation).Symbol.ToTestDisplayString());

            var secondNamedArgA = nodes.OfType<NameColonSyntax>().ElementAt(1);
            Assert.Equal("a: 4", secondNamedArgA.Parent.ToString());
            var secondASymbol = model.GetSymbolInfo(secondNamedArgA.Name);
            Assert.Equal(SymbolKind.Parameter, secondASymbol.Symbol.Kind);
            Assert.Equal("a", secondASymbol.Symbol.Name);
            Assert.Equal("void C.M(System.Int64 b, System.Int64 a)", secondASymbol.Symbol.ContainingSymbol.ToTestDisplayString());
        }

        [Fact]
        public void TestSimpleConstructor()
        {
            var source = @"
class C
{
    C(int a, int b)
    {
        System.Console.Write($""{a} {b}."");
    }
    static void Main()
    {
        new C(a: 1, 2);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (10,21): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         new C(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(10, 21)
                );
        }

        [Fact]
        public void TestSimpleThis()
        {
            var source = @"
class C
{
    C(int a, int b)
    {
        System.Console.Write($""{a} {b}."");
    }
    C() : this(a: 1, 2) { }

    static void Main()
    {
        new C();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestSimpleBase()
        {
            var source = @"
public class C
{
    public C(int a, int b)
    {
        System.Console.Write($""{a} {b}."");
    }
}
class Derived : C
{
    Derived() : base(a: 1, 2) { }

    static void Main()
    {
        new Derived();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestSimpleExtension()
        {
            var source = @"
public static class Extension
{
    public static void M(this C c, int a, int b)
    {
        System.Console.Write($""{a} {b}."");
    }
}
public class C
{
    static void Main()
    {
        var c = new C();
        c.M(a: 1, 2);
    }
}";
            var verifier = CompileAndVerifyWithMscorlib40(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2, references: new[] { TestMetadata.Net40.SystemCore });
            verifier.VerifyDiagnostics();

            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.Regular7_1, references: new[] { TestMetadata.Net40.SystemCore });
            comp.VerifyDiagnostics(
                // (14,19): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         c.M(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(14, 19)
                );
        }

        [Fact]
        public void TestSimpleDelegate()
        {
            var source = @"
class C
{
    delegate void MyDelegate(int a, int b);
    event MyDelegate e;

    static void M(int a, int b)
    {
        System.Console.Write($""{a} {b}. "");
    }

    static void Main()
    {
        var c = new C();
        c.e += M;
        c.e.Invoke(a: 1, 2);
        c.e(a: 1, 2);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2. 1 2.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (16,26): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         c.e.Invoke(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(16, 26),
                // (17,19): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         c.e(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(17, 19)
                );
        }

        [Fact]
        public void TestSimpleLocalFunction()
        {
            var source = @"
class C
{
    static void Main()
    {
        var c = new C();
        local(a: 1, 2);

        void local(int a, int b)
        {
            System.Console.Write($""{a} {b}."");
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (7,21): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         local(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(7, 21)
                );
        }

        [Fact]
        public void TestSimpleIndexer()
        {
            var source = @"
class C
{
    int this[int a, int b]
    {
        get
        {
            System.Console.Write($""Get {a} {b}. "");
            return 0;
        }
        set
        {
            System.Console.Write($""Set {a} {b} {value}."");
        }
    }
    static void Main()
    {
        var c = new C();
        _ = c[a: 1, 2];
        c[a: 3, 4] = 5;
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "Get 1 2. Set 3 4 5.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (19,21): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         _ = c[a: 1, 2];
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(19, 21),
                // (20,17): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         c[a: 3, 4] = 5;
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "4").WithArguments("7.2").WithLocation(20, 17)
                );
        }

        [Fact]
        public void TestSimpleError()
        {
            var source = @"
class C
{
    int this[int a, int b]
    {
        get
        {
            throw null;
        }
    }

    C(int a, int b) { }

    static void Main()
    {
        var c = new C(b: 1, 2);
        _ = c[b: 1, 2];
        local(b: 1, 2);

        void local(int a, int b) { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (16,23): error CS8322: Named argument 'b' is used out-of-position but is followed by an unnamed argument
                //         var c = new C(b: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "b").WithArguments("b").WithLocation(16, 23),
                // (17,15): error CS8322: Named argument 'b' is used out-of-position but is followed by an unnamed argument
                //         _ = c[b: 1, 2];
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "b").WithArguments("b").WithLocation(17, 15),
                // (18,15): error CS8322: Named argument 'b' is used out-of-position but is followed by an unnamed argument
                //         local(b: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "b").WithArguments("b").WithLocation(18, 15)
                );
        }

        [Fact]
        public void TestMetadataAndPESymbols()
        {
            var lib_cs = @"
public class C
{
    public static void M(int a, int b)
    {
        System.Console.Write($""{a} {b}."");
    }
}";

            var source = @"
class D
{
    static void Main()
    {
        C.M(a: 1, 2);
    }
}";
            var lib = CreateCompilation(lib_cs, parseOptions: TestOptions.Regular7);

            var verifier1 = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2, references: new[] { lib.ToMetadataReference() });
            verifier1.VerifyDiagnostics();

            var verifier2 = CompileAndVerify(source, expectedOutput: "1 2.", parseOptions: TestOptions.Regular7_2, references: new[] { lib.EmitToImageReference() });
            verifier2.VerifyDiagnostics();
        }

        [Fact]
        public void TestPositionalUnaffected()
        {
            var source = @"
class C
{
    static void M(int first, int other)
    {
        System.Console.Write($""{first} {other}"");
    }
    static void Main()
    {
        M(1, first: 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,14): error CS1744: Named argument 'first' specifies a parameter for which a positional argument has already been given
                //         M(1, first: 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "first").WithArguments("first").WithLocation(10, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(1, first: 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestGenericInference()
        {
            var source = @"
class C
{
    static void M<T1, T2>(T1 a, T2 b)
    {
        System.Console.Write($""{a} {b}."");
    }
    static void Main()
    {
        C.M(a: 1, ""hi"");
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 hi.", parseOptions: TestOptions.Regular7_2);
            verifier.VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.First();
            var model = verifier.Compilation.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal(@"C.M(a: 1, ""hi"")", invocation.ToString());
            Assert.Equal("void C.M<System.Int32, System.String>(System.Int32 a, System.String b)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestPositionalUnaffected2()
        {
            var source = @"
class C
{
    static void M(int a, int b, int c = 1)
    {
        System.Console.Write($""M {a} {b}"");
    }
    static void Main()
    {
        M(c: 1, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,11): error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
                //         M(c: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(10, 11)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(c: 1, 2)", invocation.ToString());
            SymbolInfo symbol = model.GetSymbolInfo(invocation);
            AssertEx.Equal(new[] { "void C.M(System.Int32 a, System.Int32 b, [System.Int32 c = 1])" },
                symbol.CandidateSymbols.Select(c => c.ToTestDisplayString()));
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbol.CandidateReason);
        }

        [Fact]
        public void TestNamedParams()
        {
            var source = @"
class C
{
    static void M(params int[] x)
    {
    }
    static void Main()
    {
        M(x: 1, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,9): error CS1501: No overload for method 'M' takes 2 arguments
                //         M(x: 1, 2);
                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "2").WithLocation(9, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(x: 1, 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestNamedParams2()
        {
            var source = @"
class C
{
    static void M(params int[] x)
    {
    }
    static void Main()
    {
        M(1, x: 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,14): error CS1744: Named argument 'x' specifies a parameter for which a positional argument has already been given
                //         M(1, x: 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "x").WithArguments("x").WithLocation(9, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(1, x: 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestNamedParamsVariousForms()
        {
            var source = @"
class C
{
    static void M(int x, params string[] y)
    {
        System.Console.Write($""{x} {string.Join("","", y)}. "");
    }
    static void Main()
    {
        M(x: 1, y: ""2"");
        M(x: 2, ""3"");
        M(x: 3, new[] { ""4"", ""5"" });
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "1 2. 2 3. 3 4,5.");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestTwiceNamedParams()
        {
            var source = @"
class C
{
    static void M(params int[] x)
    {
    }
    static void Main()
    {
        M(x: 1, x: 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS1740: Named argument 'x' cannot be specified multiple times
                //         M(x: 1, x: 2);
                Diagnostic(ErrorCode.ERR_DuplicateNamedArgument, "x").WithArguments("x").WithLocation(9, 17)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(x: 1, x: 2)", invocation.ToString());

            var symbolInfo = model.GetSymbolInfo(invocation);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            Assert.Equal("void C.M(params System.Int32[] x)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
        }

        [Fact]
        public void TestTwiceNamedParamsWithOldLangVer()
        {
            var source = @"
class C
{
    static void M(int x, int y, int z)
    {
    }
    static void Main()
    {
        M(x: 1, x: 2, 3);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (9,23): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         M(x: 1, x: 2, 3);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "3").WithArguments("7.2").WithLocation(9, 23),
                // (9,17): error CS8323: Named argument 'x' is used out-of-position but is followed by an unnamed argument
                //         M(x: 1, x: 2, 3);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "x").WithArguments("x").WithLocation(9, 17));

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(x: 1, x: 2, 3)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestNamedParams3()
        {
            var source = @"
class C
{
    static void M(int x, params int[] y)
    {
    }
    static void Main()
    {
        M(y: 1, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,11): error CS8321: Named argument 'y' is used out-of-position but is followed by an unnamed argument
                //         M(y: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "y").WithArguments("y").WithLocation(9, 11)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(y: 1, 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestNamedParams4()
        {
            var source = @"
class C
{
    static void M(int x, params int[] y)
    {
    }
    static void Main()
    {
        M(x: 1, y: 2, 3);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,9): error CS1501: No overload for method 'M' takes 3 arguments
                //         M(x: 1, y: 2, 3);
                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "3").WithLocation(9, 9)
                );
        }

        [Fact]
        public void TestNamedInvalidParams()
        {
            var source = @"
class C
{
    static void M(params int[] x, int y)
    {
    }
    static void Main()
    {
        M(x: 1, 2);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,19): error CS0231: A params parameter must be the last parameter in a parameter list
                //     static void M(params int[] x, int y)
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] x").WithLocation(4, 19),
                // (9,14): error CS1503: Argument 1: cannot convert from 'int' to 'params int[]'
                //         M(x: 1, 2);
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "params int[]").WithLocation(9, 14)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(x: 1, 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestNamedParams5()
        {
            var source = @"
class C
{
    static void M(int x, params int[] y)
    {
        System.Console.Write($""x={x} y[0]={y[0]} y.Length={y.Length}"");
    }
    static void Main()
    {
        M(y: 1, x: 2);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "x=2 y[0]=1 y.Length=1");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(y: 1, x: 2)", invocation.ToString());
            Assert.Equal("void C.M(System.Int32 x, params System.Int32[] y)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestBadNonTrailing()
        {
            var source = @"
class C
{
    static void M(int a = 1, int b = 2, int c = 3)
    {
    }
    static void Main()
    {
        int valueB = 2;
        int valueC = 3;
        M(c: valueC, valueB);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,11): error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
                //         M(c: valueC, valueB);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(11, 11)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var firstInvocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(c: valueC, valueB)", firstInvocation.ToString());
            Assert.Null(model.GetSymbolInfo(firstInvocation).Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, model.GetSymbolInfo(firstInvocation).CandidateReason);
            Assert.Equal("void C.M([System.Int32 a = 1], [System.Int32 b = 2], [System.Int32 c = 3])",
                model.GetSymbolInfo(firstInvocation).CandidateSymbols.Single().ToTestDisplayString());
        }

        [Fact]
        public void TestPickGoodOverload()
        {
            var source = @"
class C
{
    static void M(int a = 1, int b = 2, int c = 3)
    {
    }
    static void M(long c = 1, long b = 2)
    {
        System.Console.Write($""Second {c} {b}. "");
    }
    static void Main()
    {
        int valueB = 2;
        int valueC = 3;
        M(c: valueC, valueB);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "Second 3 2.");
            verifier.VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.First();
            var model = verifier.Compilation.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(c: valueC, valueB)", invocation.ToString());
            Assert.Equal("void C.M([System.Int64 c = 1], [System.Int64 b = 2])",
                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestPickGoodOverload2()
        {
            var source = @"
class C
{
    static void M(long a = 1, long b = 2, long c = 3)
    {
    }
    static void M(int c = 1, int b = 2)
    {
        System.Console.Write($""Second {c} {b}."");
    }
    static void Main()
    {
        int valueB = 2;
        int valueC = 3;
        M(c: valueC, valueB);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "Second 3 2.");
            verifier.VerifyDiagnostics();

            var tree = verifier.Compilation.SyntaxTrees.First();
            var model = verifier.Compilation.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(c: valueC, valueB)", invocation.ToString());
            Assert.Equal("void C.M([System.Int32 c = 1], [System.Int32 b = 2])",
                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestOptionalValues()
        {
            var source = @"
class C
{
    static void M(int a, int b, int c = 42)
    {
        System.Console.Write(c);
    }
    static void Main()
    {
        M(a: 1, 2);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "42");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestDynamicInvocation()
        {
            var source = @"
class C
{
    void M()
    {
        dynamic d = new object();
        d.M(a: 1, 2);
        d.M(1, 2);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (7,19): error CS8323: Named argument specifications must appear after all fixed arguments have been specified in a dynamic invocation.
                //         d.M(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation, "2").WithLocation(7, 19)
                );

            var comp2 = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp2.VerifyDiagnostics(
                // (7,19): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                //         d.M(a: 1, 2);
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "2").WithArguments("7.2").WithLocation(7, 19)
                );
        }

        [Fact]
        public void TestInvocationWithDynamicInLocalFunctionParams()
        {
            var source = @"
class C
{
    void M()
    {
        dynamic d = new object[] { 0 };
        local(x: 1, d); 
        void local(int x, params object[] y) { }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (7,21): error CS8108: Cannot pass argument with dynamic type to params parameter 'y' of local function 'local'.
                //         local(x: 1, d); 
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "d").WithArguments("y", "local").WithLocation(7, 21),
                // (7,21): error CS8323: Named argument specifications must appear after all fixed arguments have been specified in a dynamic invocation.
                //         local(x: 1, d); 
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation, "d").WithLocation(7, 21)
                );
        }

        [Fact]
        public void TestDynamicWhenNotInvocation_01()
        {
            var source = @"
class C
{
    int this[int a, int b]
    {
        get
        {
            System.Console.Write($""{a} {b}."");
            return 0;
        }
    }
    void M(C c)
    {
        dynamic d = new object();
        c[a: 1, d] = d;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void TestDynamicWhenNotInvocation_02()
        {
            var source = @"
class C
{
    int this[int a, int b]
    {
        get
        {
            System.Console.Write($""{a} {b}."");
            return 0;
        }
    }
    int this[int a, long b]
    {
        get
        {
            return 0;
        }
    }
    void M(C c)
    {
        dynamic d = new object();
        c[a: 1, d] = d;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void TestParams()
        {
            var source = @"
class C
{
    static void M(int a, int b, params int[] c)
    {
    }
    static void Main()
    {
        M(b: 2, 3, 4);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,11): error CS8321: Named argument 'b' is used out-of-position but is followed by an unnamed argument
                //         M(b: 2, 3, 4);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "b").WithArguments("b").WithLocation(9, 11)
                );
        }

        [Fact]
        public void TestParams2()
        {
            var source = @"
class C
{
    static void M(int a, int b, params int[] c)
    {
        System.Console.Write($""{a} {b} {c[0]} {c[1]} Length:{c.Length}"");
    }
    static void Main()
    {
        M(1, b: 2, 3, 4);
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1 2 3 4 Length:2");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void TestInAttribute()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public int P { get; set; }
	public MyAttribute(bool condition, int other) { }
}

[MyAttribute(condition: true, 42)]
[MyAttribute(condition: true, P = 1, 42)]
[MyAttribute(42, condition: true)]
public class C
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,38): error CS1016: Named attribute argument expected
                // [MyAttribute(condition: true, P = 1, 42)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "42").WithLocation(12, 38),
                // (13,18): error CS1744: Named argument 'condition' specifies a parameter for which a positional argument has already been given
                // [MyAttribute(42, condition: true)]
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "condition").WithArguments("condition").WithLocation(13, 18)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<AttributeSyntax>().ElementAt(1);
            Assert.Equal("MyAttribute(condition: true, 42)", invocation.ToString());
            Assert.Equal("MyAttribute..ctor(System.Boolean condition, System.Int32 other)",
                model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestInAttributeWithOldLangVersion()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public int P { get; set; }
	public MyAttribute(bool condition, int other) { }
}

[MyAttribute(condition: true, 42)]
[MyAttribute(condition: true, P = 1, 42)]
[MyAttribute(42, condition: true)]
public class C
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_1);
            comp.VerifyDiagnostics(
                // (11,31): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                // [MyAttribute(condition: true, 42)]
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "42").WithArguments("7.2").WithLocation(11, 31),
                // (12,38): error CS1016: Named attribute argument expected
                // [MyAttribute(condition: true, P = 1, 42)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "42").WithLocation(12, 38),
                // (12,38): error CS1738: Named argument specifications must appear after all fixed arguments have been specified. Please use language version 7.2 or greater to allow non-trailing named arguments.
                // [MyAttribute(condition: true, P = 1, 42)]
                Diagnostic(ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument, "42").WithArguments("7.2").WithLocation(12, 38),
                // (13,18): error CS1744: Named argument 'condition' specifies a parameter for which a positional argument has already been given
                // [MyAttribute(42, condition: true)]
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "condition").WithArguments("condition").WithLocation(13, 18)
                );
        }

        [Fact]
        public void TestInAttribute2()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class MyAttribute : Attribute
{
    public int P { get; set; }
	public MyAttribute(int a = 1, int b = 2, int c = 3) { }
}

[MyAttribute(c:3, 2)]
[MyAttribute(P=1, c:3, 2)]
public class C
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,14): error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
                // [MyAttribute(c:3, 2)]
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(11, 14),
                // (12,21): error CS1016: Named attribute argument expected
                // [MyAttribute(P=1, c:3, 2)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "3").WithLocation(12, 21),
                // (12,24): error CS1016: Named attribute argument expected
                // [MyAttribute(P=1, c:3, 2)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "2").WithLocation(12, 24),
                // (12,19): error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
                // [MyAttribute(P=1, c:3, 2)]
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(12, 19)
                );
        }

        [Fact]
        public void TestErrorsDoNotCascadeInInvocation()
        {
            var source = @"
class C
{
    static void M()
    {
        M(x: 1, x: 2, __arglist());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,11): error CS1739: The best overload for 'M' does not have a parameter named 'x'
                //         M(x: 1, x: 2, __arglist());
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("M", "x").WithLocation(6, 11));
        }

        [Fact]
        public void TestErrorsDoNotCascadeInArglist()
        {
            var source = @"
class C
{
    static void M()
    {
        M(__arglist(x: 1, x: 2, __arglist()));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0226: An __arglist expression may only appear inside of a call or new expression
                //         M(__arglist(x: 1, x: 2, __arglist()));
                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist()").WithLocation(6, 33));
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void TestSimpleArglist()
        {
            var source = @"
using System;
class C
{
    static void M(int x, int y, __arglist)
    {
        System.Console.Write($""{x} {y} {ArgListToString(new ArgIterator(__arglist))}. "");
    }
    static void Main()
    {
        M(1, 2, __arglist(3, 4));
        M(x: 1, 2, __arglist(5, 6));
    }

    static string ArgListToString(ArgIterator args)
    {
        int argCount = args.GetRemainingCount();
        string result = """";

        for (int i = 0; i < argCount; i++)
        {
            TypedReference tr = args.GetNextArg();
            result += TypedReference.ToObject(tr);
        }

        return result;
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "1 2 34. 1 2 56.");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestSimpleArglistAfterOutOfPositionArg()
        {
            var source = @"
class C
{
    static void M(int x, int y, __arglist)
    {
        M(y: 1, x: 2, __arglist(3));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,11): error CS8322: Named argument 'y' is used out-of-position but is followed by an unnamed argument
                //         M(y: 1, x: 2, __arglist(3));
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "y").WithArguments("y").WithLocation(6, 11)
                );
        }

        [Fact]
        public void TestErrorsDoNotCascadeInConstructorInitializer()
        {
            var source = @"
class C
{
    C() : this(x: 1, x: 2, 3) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS1739: The best overload for '.ctor' does not have a parameter named 'x'
                //     C() : this(x: 1, x: 2, 3) { }
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments(".ctor", "x").WithLocation(4, 16));
        }

        [Fact]
        public void TestErrorsDoNotCascadeInObjectCreation()
        {
            var source = @"
class C
{
    void M()
    {
        new C(x: 1, x: 2, 3);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,15): error CS1739: The best overload for 'C' does not have a parameter named 'x'
                //         new C(x: 1, x: 2, 3);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("C", "x").WithLocation(6, 15));
        }

        [Fact]
        public void TestErrorsDoNotCascadeInElementAccess()
        {
            var source = @"
class C
{
    int this[int i] { get { throw null; } set { throw null; } }
    void M()
    {
        var c = new C();
        System.Console.Write(c[x: 1, x: 2, 3]);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,32): error CS1739: The best overload for 'this' does not have a parameter named 'x'
                //         System.Console.Write(c[x: 1, x: 2, 3]);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "x").WithArguments("this", "x").WithLocation(8, 32));
        }
    }
}
