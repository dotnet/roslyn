// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests3 : PatternMatchingTestBase
    {
        private static void AssertEmpty(SymbolInfo info)
        {
            Assert.NotNull(info);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
        }

        [Fact]
        public void PropertyPatternSymbolInfo_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Console.WriteLine(p is { X: 3, Y: 4 });
    }
}
class Point
{
    public int X = 3;
    public int Y => 4;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("System.Int32 Point.X", xSymbol.Symbol.ToTestDisplayString());

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.NotNull(ySymbol);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("System.Int32 Point.Y { get; }", ySymbol.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void PropertyPatternSymbolInfo_02()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = null;
        Console.WriteLine(p is { X: 3, Y: 4, });
    }
}
interface I1
{
    int X { get; }
    int Y { get; }
}
interface I2
{
    int X { get; }
    int Y { get; }
}
interface Point : I1, I2
{
    // X and Y inherited ambiguously
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,34): error CS0229: Ambiguity between 'I1.X' and 'I2.X'
                //         Console.WriteLine(p is { X: 3, Y: 4 });
                Diagnostic(ErrorCode.ERR_AmbigMember, "X").WithArguments("I1.X", "I2.X").WithLocation(8, 34),
                // (8,40): error CS0229: Ambiguity between 'I1.Y' and 'I2.Y'
                //         Console.WriteLine(p is { X: 3, Y: 4 });
                Diagnostic(ErrorCode.ERR_AmbigMember, "Y").WithArguments("I1.Y", "I2.Y").WithLocation(8, 40)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.Ambiguous, xSymbol.CandidateReason);
            Assert.Null(xSymbol.Symbol);
            Assert.Equal(2, xSymbol.CandidateSymbols.Length);
            Assert.Equal("System.Int32 I1.X { get; }", xSymbol.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int32 I2.X { get; }", xSymbol.CandidateSymbols[1].ToTestDisplayString());

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.Ambiguous, ySymbol.CandidateReason);
            Assert.Null(ySymbol.Symbol);
            Assert.Equal(2, ySymbol.CandidateSymbols.Length);
            Assert.Equal("System.Int32 I1.Y { get; }", ySymbol.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int32 I2.Y { get; }", ySymbol.CandidateSymbols[1].ToTestDisplayString());
        }

        [Fact]
        public void PropertyPatternSymbolInfo_03()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = null;
        Console.WriteLine(p is { X: 3, Y: 4, });
    }
}
class Point
{
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,34): error CS0117: 'Point' does not contain a definition for 'X'
                //         Console.WriteLine(p is { X: 3, Y: 4 });
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("Point", "X").WithLocation(8, 34)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Null(xSymbol.Symbol);
            Assert.Equal(0, xSymbol.CandidateSymbols.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Null(ySymbol.Symbol);
            Assert.Equal(0, ySymbol.CandidateSymbols.Length);
        }

        [Fact]
        public void PositionalPatternSymbolInfo_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = null;
        Console.WriteLine(p is ( X: 3, Y: 4 ));
    }
}
class Point
{
    public void Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("out System.Int32 X", xSymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, xSymbol.CandidateSymbols.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("out System.Int32 Y", ySymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, ySymbol.CandidateSymbols.Length);
        }

        [Fact]
        public void PositionalPatternSymbolInfo_02()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = null;
        Console.WriteLine(p is (X: 3, Y: 4));
    }
}
class Point
{
    public void Deconstruct(out int Z, out int W) => (Z, W) = (3, 4);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,33): error CS8417: The name 'X' does not match the corresponding 'Deconstruct' parameter 'Z'.
                //         Console.WriteLine(p is (X: 3, Y: 4));
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "X").WithArguments("X", "Z").WithLocation(8, 33),
                // (8,39): error CS8417: The name 'Y' does not match the corresponding 'Deconstruct' parameter 'W'.
                //         Console.WriteLine(p is (X: 3, Y: 4));
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "Y").WithArguments("Y", "W").WithLocation(8, 39)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            // No matter what you write for the name, it is deemed to designate the parameter in that position.
            // The name if present is checked against the parameter name.
            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("out System.Int32 Z", xSymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, xSymbol.CandidateSymbols.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("out System.Int32 W", ySymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, ySymbol.CandidateSymbols.Length);
        }

        [Fact]
        public void PositionalPatternSymbolInfo_03()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        var p = (X: 3, Y: 4);
        Console.WriteLine(p is (X: 3, Y: 4));
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Equal("System.Int32 (System.Int32 X, System.Int32 Y).X", xSymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, xSymbol.CandidateSymbols.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Equal("System.Int32 (System.Int32 X, System.Int32 Y).Y", ySymbol.Symbol.ToTestDisplayString());
            Assert.Equal(0, ySymbol.CandidateSymbols.Length);
        }

        [Fact]
        public void PositionalPatternSymbolInfo_04()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        var p = (Z: 3, W: 4);
        Console.WriteLine(p is (X: 3, Y: 4));
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,33): error CS8416: The name 'X' does not identify tuple element 'Item1'.
                //         Console.WriteLine(p is (X: 3, Y: 4));
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "X").WithArguments("X", "Item1").WithLocation(8, 33),
                // (8,39): error CS8416: The name 'Y' does not identify tuple element 'Item2'.
                //         Console.WriteLine(p is (X: 3, Y: 4));
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "Y").WithArguments("Y", "Item2").WithLocation(8, 39)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var subpatterns = tree.GetRoot().DescendantNodes().OfType<SubpatternSyntax>().ToArray();
            Assert.Equal(2, subpatterns.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[0]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[0].NameColon));
            var x = subpatterns[0].NameColon.Name;
            var xSymbol = model.GetSymbolInfo(x);
            Assert.Equal(CandidateReason.None, xSymbol.CandidateReason);
            Assert.Null(xSymbol.Symbol);
            Assert.Equal(0, xSymbol.CandidateSymbols.Length);

            AssertEmpty(model.GetSymbolInfo(subpatterns[1]));
            AssertEmpty(model.GetSymbolInfo(subpatterns[1].NameColon));
            var y = subpatterns[1].NameColon.Name;
            var ySymbol = model.GetSymbolInfo(y);
            Assert.Equal(CandidateReason.None, ySymbol.CandidateReason);
            Assert.Null(ySymbol.Symbol);
            Assert.Equal(0, ySymbol.CandidateSymbols.Length);
        }

        [Fact]
        public void PatternMatchPointerVsVarPattern()
        {
            var source =
@"class Test
{
    unsafe static void Main()
    {
        fixed (char *x = string.Empty)
        {
            if (x is var y1) { }
            switch (x)
            {
                case var y2: break;
            }
            var z = x switch { var y3 => 1 };
        }
    }
}";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void PropertyPatternVsAnonymousType()
        {
            var source = @"
using System;
public class C
{
    public static void Main()
    {
        var arr = new[] {
            new { B = true,  V = 1 },
            new { B = false, V = 2 },
            new { B = true,  V = 4 },
            new { B = false, V = 8 },
            new { B = true,  V = 16 },
        };
        int sum = 0;
        foreach (var anon in arr)
        {
            switch (anon)
            {
                case { B: true, V: var val, }:
                    sum += val;
                    break;
            }
        }
        Console.WriteLine(sum);
    }
}";
            var expectedOutput = "21";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(35278, "https://github.com/dotnet/roslyn/issues/35278")]
        public void ValEscapeForSwitchExpression_01()
        {
            var source = @"
class Program
{
    public enum Rainbow
    {
        Red,
        Orange,
    }

    public ref struct RGBColor
    {
        int _r, _g, _b;

        public int R => _r;
        public int G => _g;
        public int B => _b;

        public RGBColor(int r, int g, int b)
        {
            _r = r;
            _g = g;
            _b = b;
        }

        public new string ToString() => $""RGBColor(0x{_r:X2}, 0x{_g:X2}, 0x{_b:X2})"";
    }

    static void Main(string[] args)
    {
        System.Console.WriteLine(FromRainbow(Rainbow.Red).ToString());
    }

    public static RGBColor FromRainbow(Rainbow colorBand) =>
        colorBand switch
    {
        Rainbow.Red => new RGBColor(0xFF, 0x00, 0x00),
        Rainbow.Orange => new RGBColor(0xFF, 0x7F, 0x00),
        _ => throw null!
    };
}";
            var expectedOutput = "RGBColor(0xFF, 0x00, 0x00)";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(35278, "https://github.com/dotnet/roslyn/issues/35278")]
        public void ValEscapeForSwitchExpression_02()
        {
            var source = @"
using System;
class Program
{
    public ref struct RGBColor
    {
        public RGBColor(Span<int> span)
        {
        }
    }

    public static RGBColor FromSpan(int r, int g, int b)
    {
        Span<int> span = stackalloc int[] { r, g, b };
        return 1 switch
        {
            1 => new RGBColor(span),
            _ => throw null!
        };
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (17,18): error CS8347: Cannot use a result of 'Program.RGBColor.RGBColor(Span<int>)' in this context because it may expose variables referenced by parameter 'span' outside of their declaration scope
                //             1 => new RGBColor(span),
                Diagnostic(ErrorCode.ERR_EscapeCall, "new RGBColor(span)").WithArguments("Program.RGBColor.RGBColor(System.Span<int>)", "span").WithLocation(17, 18),
                // (17,31): error CS8352: Cannot use local 'span' in this context because it may expose referenced variables outside of their declaration scope
                //             1 => new RGBColor(span),
                Diagnostic(ErrorCode.ERR_EscapeLocal, "span").WithArguments("span").WithLocation(17, 31));
        }

        [Fact]
        public void NoRefSwitch_01()
        {
            var source = @"
class Program
{
    ref int M(bool b, ref int x, ref int y)
    {
        return ref (b switch { true => x, false => y });
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (6,21): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref (b switch { true => x, false => y });
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "b switch { true => x, false => y }").WithLocation(6, 21));
        }

        [Fact]
        public void NoRefSwitch_02()
        {
            var source = @"
class Program
{
    ref int M(bool b, ref int x, ref int y)
    {
        return ref (b switch { true => ref x, false => ref y });
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSpan(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (6,23): warning CS8509: The switch expression does not handle all possible inputs (it is not exhaustive).
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(6, 23),
                // (6,40): error CS1525: Invalid expression term 'ref'
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(6, 40),
                // (6,40): error CS1003: Syntax error, ',' expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(6, 40),
                // (6,40): error CS1513: } expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_RbraceExpected, "ref").WithLocation(6, 40),
                // (6,40): error CS1026: ) expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(6, 40),
                // (6,40): error CS1002: ; expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(6, 40),
                // (6,40): warning CS0162: Unreachable code detected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ref").WithLocation(6, 40),
                // (6,44): error CS0118: 'x' is a variable but is used like a type
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_BadSKknown, "x").WithArguments("x", "variable", "type").WithLocation(6, 44),
                // (6,45): error CS1001: Identifier expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(6, 45),
                // (6,45): error CS8174: A declaration of a by-reference variable must have an initializer
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "").WithLocation(6, 45),
                // (6,47): error CS1001: Identifier expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "false").WithLocation(6, 47),
                // (6,47): error CS1002: ; expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "false").WithLocation(6, 47),
                // (6,47): error CS8174: A declaration of a by-reference variable must have an initializer
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "").WithLocation(6, 47),
                // (6,53): error CS1002: ; expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=>").WithLocation(6, 53),
                // (6,53): error CS1513: } expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 53),
                // (6,60): error CS0118: 'y' is a variable but is used like a type
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_BadSKknown, "y").WithArguments("y", "variable", "type").WithLocation(6, 60),
                // (6,62): error CS1001: Identifier expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "}").WithLocation(6, 62),
                // (6,62): error CS1002: ; expected
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 62),
                // (6,62): error CS8174: A declaration of a by-reference variable must have an initializer
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "").WithLocation(6, 62),
                // (6,63): error CS1519: Invalid token ')' in class, struct, or interface member declaration
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ")").WithArguments(")").WithLocation(6, 63),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));
        }
    }
}
