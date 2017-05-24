// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var secondInvocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(3);
            Assert.Equal("M(3, a: 4)", secondInvocation.ToString());
            Assert.Equal("void C.M(System.Int64 b, System.Int64 a)",
                model.GetSymbolInfo(secondInvocation).Symbol.ToTestDisplayString());
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (10,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'C.M(int, int, int)'
                //         M(c: 1, 2);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("a", "C.M(int, int, int)").WithLocation(10, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(c: 1, 2)", invocation.ToString());
            AssertEx.Equal(new[] { "void C.M(System.Int32 a, System.Int32 b, [System.Int32 c = 1])" },
                model.GetSymbolInfo(invocation).CandidateSymbols.Select(c => c.ToTestDisplayString()));
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (9,11): error CS8321: Named argument 'x' is used out-of-position but is followed by an unnamed argument
                //         M(x: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "x").WithArguments("x").WithLocation(9, 11)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(x: 1, 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol); // PROTOTYPE(non-trailing)
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
        M(x: 1, x: 2);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
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
            Assert.Equal("void C.M(params System.Int32[] x)", model.GetSymbolInfo(invocation).Symbol.ToTestDisplayString());
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (9,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'x' of 'C.M(int, params int[])'
                //         M(y: 1, 2);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("x", "C.M(int, params int[])").WithLocation(9, 9)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var invocation = nodes.OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("M(y: 1, 2)", invocation.ToString());
            Assert.Null(model.GetSymbolInfo(invocation).Symbol);
        }

        [Fact]
        public void TestBadNonTrailing()
        {
            var source = @"
class C
{
    static void M(int a = 1, int b = 2, int c = 3)
    {
        System.Console.Write($""First {a} {b}. "");
    }
    static void Main()
    {
        int valueB = 2;
        int valueC = 3;
        M(c: valueC, valueB);
    }
}";
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (12,11): error CS8321: Named argument 'c' is used out-of-position but is followed by an unnamed argument
                //         M(c: valueC, valueB);
                Diagnostic(ErrorCode.ERR_BadNonTrailingNamedArgument, "c").WithArguments("c").WithLocation(12, 11)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var nodes = tree.GetCompilationUnitRoot().DescendantNodes();
            var firstInvocation = nodes.OfType<InvocationExpressionSyntax>().ElementAt(1);
            Assert.Equal("M(c: valueC, valueB)", firstInvocation.ToString());
            Assert.Null(model.GetSymbolInfo(firstInvocation).Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, model.GetSymbolInfo(firstInvocation).CandidateReason);
            Assert.Equal("void C.M([System.Int32 a = 1], [System.Int32 b = 2], [System.Int32 c = 3])",
                model.GetSymbolInfo(firstInvocation).CandidateSymbols.Single().ToTestDisplayString());
        }

        [Fact]
        public void TestBadNonTrailing2()
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
            var verifier = CompileAndVerify(source, expectedOutput: "Second 3 2.", parseOptions: TestOptions.Regular7_2);
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
        public void TestBadNonTrailing3()
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
            var verifier = CompileAndVerify(source, expectedOutput: "Second 3 2.", parseOptions: TestOptions.Regular7_2);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
            comp.VerifyDiagnostics(
                // (9,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'C.M(int, int, params int[])'
                //         M(b: 2, 3, 4);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("a", "C.M(int, int, params int[])").WithLocation(9, 9)
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
            var verifier = CompileAndVerify(source, expectedOutput: "1 2 3 4 Length:2", parseOptions: TestOptions.Regular7_2);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
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
            var comp = CreateStandardCompilation(source, parseOptions: TestOptions.Regular7_2);
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

        // PROTOTYPE(non-trailing) add the error code to UpgradeProject code fixer
        // new C(...)
        // Constructor(...) : this(...)
        // delegate invocation
    }
}
