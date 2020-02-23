﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.NotEqual(default, info);
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
            Assert.NotEqual(default, ySymbol);
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
                // (6,40): error CS1525: Invalid expression term 'ref'
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref x").WithArguments("ref").WithLocation(6, 40),
                // (6,40): error CS1073: Unexpected token 'ref'
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(6, 40),
                // (6,56): error CS1525: Invalid expression term 'ref'
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref y").WithArguments("ref").WithLocation(6, 56),
                // (6,56): error CS1073: Unexpected token 'ref'
                //         return ref (b switch { true => ref x, false => ref y });
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(6, 56));
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_01()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            Target t = b switch { false => s1, true => s2 };
            Console.Write(t);
            t = b switch { false => s1, true => s2 };
            Console.Write(t);
        }
    }
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var expectedOutput = "Source1 Source1 Source2 Source2 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_02()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool?[] { false, true })
        {
            Target t = b switch { false => s1, true => s2, null => (Target)null };
            Console.Write(t);
            t = b switch { false => s1, true => s2, null => (Target)null };
            Console.Write(t);
        }
    }
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var expectedOutput = "Source1 Source1 Source2 Source2 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_03()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            Target t = b switch { false => s1, true => s2 };
            Console.Write(t);
            t = b switch { false => s1, true => s2 };
            Console.Write(t);
        }
    }
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
class Source1
{
}
class Source2
{
}
";
            var expectedOutput = "Source1 Source1 Source2 Source2 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Overload()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            M(b switch { false => s1, true => s2 });
        }
    }
    static void M(Source1 s) => throw null;
    static void M(Source2 s) => throw null;
    static void M(Target t) => Console.Write(t);
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var expectedOutput = "Source1 Source2 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Lambda_01()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            M(() => b switch { false => s1, true => s2 });
        }
    }
    static void M(Func<Source1> s) => throw null;
    static void M(Func<Source2> s) => throw null;
    static void M(Func<Target> t) => Console.Write(t());
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var expectedOutput = "Source1 Source2 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Lambda_02()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1();
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            M(() => b switch { false => s1, true => s1 });
        }
    }
    static void M(Func<Source2> s) => throw null;
    static void M(Func<Target> t) => Console.Write(t());
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var expectedOutput = "Source1 Source1 ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_DoubleConversionViaNaturalType_01()
        {
            // Switch conversion is not a standard conversion, so not applicable as input to
            // a user-defined conversion.
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        bool b = false;
        Source1 s1 = new Source1();
        Ultimate u = b switch { false => s1, true => s1 };
        Console.Write(u);
        u = b switch { false => s1, true => s1 };
        Console.Write(u);
    }
}
class Ultimate
{
    private readonly string Value;
    public Ultimate(string value) => Value = value;
    public override string ToString() => Value;
    public static implicit operator Ultimate(Target t) => new Ultimate(t.ToString());
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                // (10,42): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         Ultimate u = b switch { false => s1, true => s1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(10, 42),
                // (10,54): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         Ultimate u = b switch { false => s1, true => s1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(10, 54),
                // (12,33): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         u = b switch { false => s1, true => s1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(12, 33),
                // (12,45): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         u = b switch { false => s1, true => s1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(12, 45));
        }

        [Fact]
        public void TargetTypedSwitch_DoubleConversionViaNaturalType_02()
        {
            // Switch conversion is not a standard conversion, so not applicable as input to
            // a user-defined conversion.
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        bool b = false;
        Source1 s1 = new Source1();
        Source2 s2 = new Source2();
        Ultimate u = b switch { false => s1, true => s2 };
        Console.Write(u);
        u = b switch { false => s1, true => s2 };
        Console.Write(u);
    }
}
class Ultimate
{
    private readonly string Value;
    public Ultimate(string value) => Value = value;
    public override string ToString() => Value;
    public static implicit operator Ultimate(Target t) => new Ultimate(t.ToString());
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    public static implicit operator Target(Source1 self) => new Target(""Source1 "");
}
class Source2
{
    public static implicit operator Target(Source2 self) => new Target(""Source2 "");
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                // (11,42): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         Ultimate u = b switch { false => s1, true => s2 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(11, 42),
                // (11,54): error CS0029: Cannot implicitly convert type 'Source2' to 'Ultimate'
                //         Ultimate u = b switch { false => s1, true => s2 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s2").WithArguments("Source2", "Ultimate").WithLocation(11, 54),
                // (13,33): error CS0029: Cannot implicitly convert type 'Source1' to 'Ultimate'
                //         u = b switch { false => s1, true => s2 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s1").WithArguments("Source1", "Ultimate").WithLocation(13, 33),
                // (13,45): error CS0029: Cannot implicitly convert type 'Source2' to 'Ultimate'
                //         u = b switch { false => s1, true => s2 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s2").WithArguments("Source2", "Ultimate").WithLocation(13, 45));
        }

        [Fact]
        public void SwitchExpressionDiscardedWithNoNaturalType()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        _ = true switch { true => 1, false => string.Empty };
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8506: No best type was found for the switch expression.
                //         _ = true switch { true => 1, false => string.Empty };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 18));
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_04()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source();
        var s2 = new TargetSubtype();
        foreach (var b in new bool[] { false, true })
        {
            Target t = b switch { false => s1, true => s2 };
            Console.WriteLine(t);
        }
    }
}
class Source
{
    public static implicit operator TargetSubtype(Source self)
    {
        Console.WriteLine(""Source->TargetSubtype"");
        return new TargetSubtype();
    }
    public override string ToString() => ""Source"";
}
class Target
{
    public override string ToString() => ""Target"";
}
class TargetSubtype : Target
{
    public override string ToString() => ""TargetSubtype"";
}
";
            var expectedOutput = @"Source->TargetSubtype
TargetSubtype
TargetSubtype";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_05()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source();
        var s2 = new Target();
        foreach (var b in new bool[] { false, true })
        {
            Ultimate t = b switch { false => s1, true => s2 };
            Console.WriteLine(t);
        }
    }
}
class Source
{
    public static implicit operator Target(Source self)
    {
        Console.WriteLine(""Source->Target"");
        return new Target();
    }
    public override string ToString() => ""Source"";
}
class Target
{
    public static implicit operator Ultimate(Target self)
    {
        Console.WriteLine(""Target->Ultimate"");
        return new Ultimate();
    }
    public override string ToString() => ""Target"";
}
class Ultimate
{
    public override string ToString() => ""Ultimate"";
}
";
            var expectedOutput = @"Source->Target
Target->Ultimate
Ultimate
Target->Ultimate
Ultimate";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_06()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source();
        var s2 = new Target();
        foreach (var b in new bool[] { false, true })
        {
            (int i, Ultimate t) t = (1, b switch { false => s1, true => s2 });
            Console.WriteLine(t.t);
        }
    }
}
class Source
{
    public static implicit operator Target(Source self)
    {
        Console.WriteLine(""Source->Target"");
        return new Target();
    }
    public override string ToString() => ""Source"";
}
class Target
{
    public static implicit operator Ultimate(Target self)
    {
        Console.WriteLine(""Target->Ultimate"");
        return new Ultimate();
    }
    public override string ToString() => ""Target"";
}
class Ultimate
{
    public override string ToString() => ""Ultimate"";
}
";
            var expectedOutput = @"Source->Target
Target->Ultimate
Ultimate
Target->Ultimate
Ultimate";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Assignment_07()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source();
        var s2 = new Target();
        foreach (var b in new bool[] { false, true })
        {
            (int i, Ultimate t) t = b switch { false => (1, s1), true => (2, s2) };
            Console.WriteLine(t);
        }
    }
}
class Source
{
    public static implicit operator Target(Source self)
    {
        Console.WriteLine(""Source->Target"");
        return new Target();
    }
    public override string ToString() => ""Source"";
}
class Target
{
    public static implicit operator Ultimate(Target self)
    {
        Console.WriteLine(""Target->Ultimate"");
        return new Ultimate();
    }
    public override string ToString() => ""Target"";
}
class Ultimate
{
    public override string ToString() => ""Ultimate"";
}
";
            var expectedOutput = @"Source->Target
Target->Ultimate
(1, Ultimate)
Target->Ultimate
(2, Ultimate)";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void PointerAsInputType()
        {
            var source =
@"unsafe class Program
{
    static void Main(string[] args)
    {
    }
    bool M1(int* p) => p is null; // 1
    bool M2(int* p) => p is var _; // 2
    void M3(int* p)
    {
        switch (p)
        {
            case null: // 3
                break;
        }
    }
    void M4(int* p)
    {
        switch (p)
        {
            case var _: // 4
                break;
        }
    }
}";
            CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,29): error CS8521: Pattern-matching is not permitted for pointer types.
                //     bool M1(int* p) => p is null; // 1
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "null").WithLocation(6, 29),
                // (7,29): error CS8521: Pattern-matching is not permitted for pointer types.
                //     bool M2(int* p) => p is var _; // 2
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var _").WithLocation(7, 29),
                // (12,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //             case null: // 3
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "null").WithLocation(12, 18),
                // (20,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //             case var _: // 4
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var _").WithLocation(20, 18)
                );
            CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_01()
        {
            var source = @"
class Program
{
    public static bool? GetBool(string name)
    {
        return name switch
        {
            ""a"" => true,
            ""b"" => false,
            _ => null,
        };
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_02()
        {
            var source = @"
class Program
{
    public static bool? GetBool(string name) => name switch
        {
            ""a"" => true,
            ""b"" => false,
            _ => null,
        };
}
";
            CreateCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_03()
        {
            var source = @"
class Program
{
    public static bool? GetBool(string name)
    {
        return name switch
        {
            ""a"" => true,
            _ => name switch
                {
                    ""b"" => false,
                    _ => null,
                },
        };
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_04()
        {
            var source = @"
class Program
{
    public static bool? GetBool(string name)
    {
        var result = name switch
        {
            ""a"" => true,
            ""b"" => false,
            _ => null,
        };
        return result;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,27): error CS8506: No best type was found for the switch expression.
                //         var result = name switch
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 27)
                );
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_05()
        {
            var source = @"
class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine(Get(""a"").Item1?.ToString() ?? ""null"");
    }
    public static (int?, int) Get(string name)
    {
        return name switch
        {
            ""a"" => (default, 1), // this is convertible to (int, int)
            ""b"" => (1, 2),
            _ => (3, 4),
        };
    }
}
";
            CompileAndVerify(CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(), expectedOutput: "0");
        }

        [Fact, WorkItem(38226, "https://github.com/dotnet/roslyn/issues/38226")]
        public void TargetTypedSwitch_NaturalTypeWithUntypedArm_06()
        {
            var source = @"
class Program
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine(Get(""a"").Item1?.ToString() ?? ""null"");
    }
    public static (int?, int) Get(string name)
    {
        return name switch
        {
            ""a"" => (default, 1), // this is convertible to (int?, int)
            ""b"" => (1, 2),
            _ => (null, 4),
        };
    }
}
";
            CompileAndVerify(CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(), expectedOutput: "null");
        }

        [Fact, WorkItem(38686, "https://github.com/dotnet/roslyn/issues/38686")]
        public void TargetTypedSwitch_AnyTypedSwitchWithoutTargetType()
        {
            var source = @"
#nullable enable
public static class C {
    static object o;
    static void Main() {
       // either of these would crash
        _= (C)(o switch { _ => default }); 
        _= (C)(o switch { _ => throw null! }); 
    }
}";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // (4,19): warning CS8618: Non-nullable field 'o' is uninitialized. Consider declaring the field as nullable.
                //     static object o;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "o").WithArguments("field", "o").WithLocation(4, 19),
                // (4,19): warning CS0649: Field 'C.o' is never assigned to, and will always have its default value null
                //     static object o;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "o").WithArguments("C.o", "null").WithLocation(4, 19),
                // (7,12): error CS0716: Cannot convert to static type 'C'
                //         _= (C)(o switch { _ => default }); 
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)(o switch { _ => default })").WithArguments("C").WithLocation(7, 12),
                // (8,12): error CS0716: Cannot convert to static type 'C'
                //         _= (C)(o switch { _ => throw null! }); 
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)(o switch { _ => throw null! })").WithArguments("C").WithLocation(8, 12)
            };
            string expectedFlowGraph = @"
    Block[B0] - Entry
        Statements (0)
        Next (Regular) Block[B1]
            Entering: {R1} {R2}
    .locals {R1}
    {
        CaptureIds: [0]
        .locals {R2}
        {
            CaptureIds: [1]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o')
                      Value: 
                        IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                          Instance Receiver: 
                            null
                Jump if False (Regular) to Block[B3]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: '_ => default')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o')
                      Pattern: 
                        IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object)
                    Leaving: {R2}
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'default')
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, Constant: null, IsInvalid, IsImplicit) (Syntax: 'default')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (DefaultLiteral)
                          Operand: 
                            IDefaultValueOperation (OperationKind.DefaultValue, Type: C, Constant: null, IsInvalid) (Syntax: 'default')
                Next (Regular) Block[B4]
                    Leaving: {R2}
        }
        Block[B3] - Block
            Predecessors: [B1]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: 'o switch {  ... > default }')
                  Arguments(0)
                  Initializer: 
                    null
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_= (C)(o sw ... default });')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_= (C)(o sw ...  default })')
                      Left: 
                        IDiscardOperation (Symbol: C? _) (OperationKind.Discard, Type: C) (Syntax: '_')
                      Right: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'o switch {  ... > default }')
            Next (Regular) Block[B5]
                Leaving: {R1}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        .locals {R4}
        {
            CaptureIds: [3]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o')
                      Value: 
                        IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                          Instance Receiver: 
                            null
                Jump if False (Regular) to Block[B8]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: '_ => throw null!')
                      Value: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o')
                      Pattern: 
                        IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object)
                    Leaving: {R4}
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (0)
                Next (Throw) Block[null]
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
            Block[B7] - Block [UnReachable]
                Predecessors (0)
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null!')
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid, IsImplicit) (Syntax: 'throw null!')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitThrow)
                          Operand: 
                            IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null!')
                Next (Regular) Block[B9]
                    Leaving: {R4}
        }
        Block[B8] - Block
            Predecessors: [B5]
            Statements (0)
            Next (Throw) Block[null]
                IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: 'o switch {  ... row null! }')
                  Arguments(0)
                  Initializer: 
                    null
        Block[B9] - Block [UnReachable]
            Predecessors: [B7]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_= (C)(o sw ... w null! });')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_= (C)(o sw ... ow null! })')
                      Left: 
                        IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'o switch {  ... row null! }')
            Next (Regular) Block[B10]
                Leaving: {R3}
    }
    Block[B10] - Exit [UnReachable]
        Predecessors: [B9]
        Statements (0)
";
            string expectedOperationTree = @"
    IMethodBodyOperation (OperationKind.MethodBody, Type: null, IsInvalid) (Syntax: 'static void ... }')
      BlockBody: 
        IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_= (C)(o sw ... default });')
            Expression: 
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_= (C)(o sw ...  default })')
                Left: 
                  IDiscardOperation (Symbol: C? _) (OperationKind.Discard, Type: C) (Syntax: '_')
                Right: 
                  ISwitchExpressionOperation (1 arms) (OperationKind.SwitchExpression, Type: C, IsInvalid) (Syntax: 'o switch {  ... > default }')
                    Value: 
                      IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                        Instance Receiver: 
                          null
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => default')
                          Pattern: 
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object)
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, Constant: null, IsInvalid, IsImplicit) (Syntax: 'default')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IDefaultValueOperation (OperationKind.DefaultValue, Type: C, Constant: null, IsInvalid) (Syntax: 'default')
          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_= (C)(o sw ... w null! });')
            Expression: 
              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_= (C)(o sw ... ow null! })')
                Left: 
                  IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                Right: 
                  ISwitchExpressionOperation (1 arms) (OperationKind.SwitchExpression, Type: C, IsInvalid) (Syntax: 'o switch {  ... row null! }')
                    Value: 
                      IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                        Instance Receiver: 
                          null
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => throw null!')
                          Pattern: 
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object)
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid, IsImplicit) (Syntax: 'throw null!')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null!')
                                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    Operand: 
                                      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
      ExpressionBody: 
        null
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(expectedDiagnostics);
            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();
            compilation.VerifyOperationTree(node1, expectedOperationTree: expectedOperationTree);
            VerifyFlowGraph(compilation, node1, expectedFlowGraph: expectedFlowGraph);
        }

        [Fact, WorkItem(39082, "https://github.com/dotnet/roslyn/issues/39082")]
        public void TargetTypedSwitch_CastSwitchContainingOnlyLambda()
        {
            var source = @"
using System;
public static class C {
    static void Main() {
        var x = ((Func<int, decimal>)(0 switch { 0 => _ => {}}))(0);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,41): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                //         var x = ((Func<int, decimal>)(0 switch { 0 => _ => {}}))(0);
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(5, 41),
                // (5,57): error CS1643: Not all code paths return a value in lambda expression of type 'Func<int, decimal>'
                //         var x = ((Func<int, decimal>)(0 switch { 0 => _ => {}}))(0);
                Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<int, decimal>").WithLocation(5, 57)
                );
        }

        [Fact, WorkItem(39082, "https://github.com/dotnet/roslyn/issues/39082")]
        public void TargetTypedSwitch_CastSwitchContainingOnlyMethodGroup()
        {
            var source = @"
using System;
public static class C {
    static void Main() {
        var x = ((Func<int, decimal>)(0 switch { 0 => M }))(0);
    }
    static void M(int x) {}
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,41): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                //         var x = ((Func<int, decimal>)(0 switch { 0 => M }))(0);
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(5, 41),
                // (5,55): error CS0407: 'void C.M(int)' has the wrong return type
                //         var x = ((Func<int, decimal>)(0 switch { 0 => M }))(0);
                Diagnostic(ErrorCode.ERR_BadRetType, "M").WithArguments("C.M(int)", "void").WithLocation(5, 55)
                );
        }

        [Fact, WorkItem(39767, "https://github.com/dotnet/roslyn/issues/39767")]
        public void PreferUserDefinedConversionOverSwitchExpressionConversion()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var s1 = new Source1(""Source1"");
        var s2 = new Source2();
        foreach (var b in new bool[] { false, true })
        {
            Target t = b switch { false => s1, true => s2 };
            Console.Write(t + "" "");
        }
    }
}
class Target
{
    private readonly string Value;
    public Target(string value) => Value = value;
    public override string ToString() => Value;
}
class Source1
{
    private readonly string Value;
    public Source1(string value) => Value = value;
    public override string ToString() => Value;
    public static implicit operator Target(Source1 self) => new Target(self.Value+""->Target"");
}
class Source2
{
    public static implicit operator Source1(Source2 self) => new Source1(""Source2->Source1"");
    public static implicit operator Target(Source2 self) => new Target(""Source2->Target"");
}
";
            var expectedOutput = "Source1->Target Source2->Source1->Target ";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [WorkItem(40295, "https://github.com/dotnet/roslyn/issues/40295")]
        [Fact]
        public void SwitchExpressionWithAmbiguousImplicitConversion_01()
        {
            var source = @"
class A
{
  public static implicit operator B(A a) => new B();
}

class B
{
  public static implicit operator B(A a) => new B();
}

class C
{
  static void M(string s)
  {
    (B, B) x = s switch { _ => (new A(), new A()), };
    x.Item1.ToString();
  }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,33): error CS0457: Ambiguous user defined conversions 'A.implicit operator B(A)' and 'B.implicit operator B(A)' when converting from 'A' to 'B'
                //     (B, B) x = s switch { _ => (new A(), new A()), };
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "new A()").WithArguments("A.implicit operator B(A)", "B.implicit operator B(A)", "A", "B").WithLocation(16, 33),
                // (16,42): error CS0457: Ambiguous user defined conversions 'A.implicit operator B(A)' and 'B.implicit operator B(A)' when converting from 'A' to 'B'
                //     (B, B) x = s switch { _ => (new A(), new A()), };
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "new A()").WithArguments("A.implicit operator B(A)", "B.implicit operator B(A)", "A", "B").WithLocation(16, 42)
                );
        }

        [WorkItem(40295, "https://github.com/dotnet/roslyn/issues/40295")]
        [Fact]
        public void SwitchExpressionWithAmbiguousImplicitConversion_02()
        {
            var source = @"
class A
{
  public static implicit operator B(A a) => new B();
}

class B
{
  public static implicit operator B(A a) => new B();
}

class C
{
  static void M(int i)
  {
    var x = i switch { 1 => new A(), _ => new B() };
    x.ToString();
  }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,29): error CS0457: Ambiguous user defined conversions 'A.implicit operator B(A)' and 'B.implicit operator B(A)' when converting from 'A' to 'B'
                //     var x = i switch { 1 => new A(), _ => new B() };
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "new A()").WithArguments("A.implicit operator B(A)", "B.implicit operator B(A)", "A", "B").WithLocation(16, 29)
                );
        }

        [WorkItem(40295, "https://github.com/dotnet/roslyn/issues/40295")]
        [Fact]
        public void SwitchExpressionWithAmbiguousImplicitConversion_03()
        {
            var source = @"
class A
{
  public static implicit operator B(A a) => new B();
}

class B
{
  public static implicit operator B(A a) => new B();
}

class C
{
  static void M(int i)
  {
    B x = i switch { _ => new A() };
    x.ToString();
  }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,27): error CS0457: Ambiguous user defined conversions 'A.implicit operator B(A)' and 'B.implicit operator B(A)' when converting from 'A' to 'B'
                //     B x = i switch { _ => new A() };
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "new A()").WithArguments("A.implicit operator B(A)", "B.implicit operator B(A)", "A", "B").WithLocation(16, 27)
                );
        }

        [WorkItem(40295, "https://github.com/dotnet/roslyn/issues/40295")]
        [Fact]
        public void SwitchExpressionWithAmbiguousImplicitConversion_04()
        {
            var source = @"
class A
{
  public static implicit operator B(A a) => new B();
}

class B
{
  public static implicit operator B(A a) => new B();
}

class C
{
  static void M(int i)
  {
    B x = i switch { _ => i switch { _ => new A() } };
    x.ToString();
  }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,43): error CS0457: Ambiguous user defined conversions 'A.implicit operator B(A)' and 'B.implicit operator B(A)' when converting from 'A' to 'B'
                //     B x = i switch { _ => i switch { _ => new A() } };
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "new A()").WithArguments("A.implicit operator B(A)", "B.implicit operator B(A)", "A", "B").WithLocation(16, 43)
                );
        }

        [WorkItem(40714, "https://github.com/dotnet/roslyn/issues/40714")]
        [Fact]
        public void BadGotoCase_01()
        {
            var source = @"
class C
{
    static void Example(object a, object b)
    {
        switch ((a, b))
        {
            case (string str, int[] arr) _:
                goto case (string str, decimal[] arr);
            case (string str, decimal[] arr) _:
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,13): error CS0163: Control cannot fall through from one case label ('case (string str, int[] arr) _:') to another
                //             case (string str, int[] arr) _:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case (string str, int[] arr) _:").WithArguments("case (string str, int[] arr) _:").WithLocation(8, 13),
                // (8,26): error CS0136: A local or parameter named 'str' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case (string str, int[] arr) _:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "str").WithArguments("str").WithLocation(8, 26),
                // (8,37): error CS0136: A local or parameter named 'arr' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case (string str, int[] arr) _:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "arr").WithArguments("arr").WithLocation(8, 37),
                // (9,17): error CS0150: A constant value is expected
                //                 goto case (string str, decimal[] arr);
                Diagnostic(ErrorCode.ERR_ConstantExpected, "goto case (string str, decimal[] arr);").WithLocation(9, 17),
                // (9,28): error CS8185: A declaration is not allowed in this context.
                //                 goto case (string str, decimal[] arr);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "string str").WithLocation(9, 28),
                // (9,28): error CS0165: Use of unassigned local variable 'str'
                //                 goto case (string str, decimal[] arr);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "string str").WithArguments("str").WithLocation(9, 28),
                // (9,40): error CS8185: A declaration is not allowed in this context.
                //                 goto case (string str, decimal[] arr);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "decimal[] arr").WithLocation(9, 40),
                // (9,40): error CS0165: Use of unassigned local variable 'arr'
                //                 goto case (string str, decimal[] arr);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "decimal[] arr").WithArguments("arr").WithLocation(9, 40),
                // (10,26): error CS0136: A local or parameter named 'str' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case (string str, decimal[] arr) _:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "str").WithArguments("str").WithLocation(10, 26),
                // (10,41): error CS0136: A local or parameter named 'arr' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case (string str, decimal[] arr) _:
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "arr").WithArguments("arr").WithLocation(10, 41)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var strDecl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(s => s.Identifier.ValueText == "str").ToArray();
            Assert.Equal(3, strDecl.Length);
            VerifyModelForDuplicateVariableDeclarationInSameScope(model, strDecl[1], LocalDeclarationKind.DeclarationExpressionVariable);

            var arrDecl = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(s => s.Identifier.ValueText == "arr").ToArray();
            Assert.Equal(3, arrDecl.Length);
            VerifyModelForDuplicateVariableDeclarationInSameScope(model, arrDecl[1], LocalDeclarationKind.DeclarationExpressionVariable);
        }

        [WorkItem(40714, "https://github.com/dotnet/roslyn/issues/40714")]
        [Fact]
        public void BadGotoCase_02()
        {
            var source = @"
class C
{
    static void Example(object a, object b)
    {
        switch ((a, b))
        {
            case (string str, int[] arr) _:
                goto case a is (var x1, var x2);
                x1 = x2;
            case (string str, decimal[] arr) _:
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,13): error CS0163: Control cannot fall through from one case label ('case (string str, int[] arr) _:') to another
                //             case (string str, int[] arr) _:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case (string str, int[] arr) _:").WithArguments("case (string str, int[] arr) _:").WithLocation(8, 13),
                // (9,17): error CS0029: Cannot implicitly convert type 'bool' to '(object a, object b)'
                //                 goto case a is (var x1, var x2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goto case a is (var x1, var x2);").WithArguments("bool", "(object a, object b)").WithLocation(9, 17),
                // (9,32): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //                 goto case a is (var x1, var x2);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(var x1, var x2)").WithArguments("object", "Deconstruct").WithLocation(9, 32),
                // (9,32): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //                 goto case a is (var x1, var x2);
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(var x1, var x2)").WithArguments("object", "2").WithLocation(9, 32)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x1Decl[0], x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").ToArray();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(1, x2Decl.Length);
            Assert.Equal(1, x2Ref.Length);
            VerifyModelForDeclarationOrVarSimplePattern(model, x2Decl[0], x2Ref);
        }

        [Fact, WorkItem(40533, "https://github.com/dotnet/roslyn/issues/40533")]
        public void DisallowDesignatorsUnderNotAndOr()
        {
            var source = @"
class C
{
    void Good(object o)
    {
        if (o is int and 1) { }
        if (o is int x1 and 1) { }
        if (o is int x2 and (1 or 2)) { }
        if (o is 1 and int x3) { }
        if (o is (1 or 2) and int x4) { }
        if (o is not (1 or 2) and int x5) { }
    }

    void Bad(object o)
    {
        if (o is int y1 or 1) { }
        if (o is int y2 or (1 or 2)) { }
        if (o is 1 or int y3) { }
        if (o is (1 or 2) or int y4) { }
        if (o is not int y5) { }
        if (o is not (1 and int y6)) { }
        if (o is Point { X: var y7 } or Animal _) { }
        if (o is Point(var y8, _) or Animal _) { }
        if (o is object or (1 or var y9)) { }
    }

    void NotBad(object o)
    {
        if (o is int _ or 1) { }
        if (o is int _ or (1 or 2)) { }
        if (o is 1 or int _) { }
        if (o is (1 or 2) or int _) { }
        if (o is not int _) { }
        if (o is not (1 and int _)) { }
    }
}
class Point
{
    public int X => 3;
    public void Deconstruct(out int X, out int Y) => (X, Y) = (3, 4);
}
class Animal { }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                // (16,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is int y1 or 1) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y1").WithLocation(16, 22),
                // (17,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is int y2 or (1 or 2)) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y2").WithLocation(17, 22),
                // (18,27): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is 1 or int y3) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y3").WithLocation(18, 27),
                // (19,34): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is (1 or 2) or int y4) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y4").WithLocation(19, 34),
                // (20,26): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is not int y5) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y5").WithLocation(20, 26),
                // (21,33): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is not (1 and int y6)) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y6").WithLocation(21, 33),
                // (22,33): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is Point { X: var y7 } or Animal _) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y7").WithLocation(22, 33),
                // (23,28): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is Point(var y8, _) or Animal _) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y8").WithLocation(23, 28),
                // (24,38): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is object or (1 or var y9)) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y9").WithLocation(24, 38)
                );
        }

        [Fact, WorkItem(40149, "https://github.com/dotnet/roslyn/issues/40149")]
        public void ArrayTypePattern_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        M1(new int[0]);
        M1(new long[0]);
        M1(new A[0]);
        M1(new G<B>[0]);
        M1(new N.G<B>[0]);
    }
    static void M1(object o)
    {
        switch (o)
        {
            case int[]: System.Console.WriteLine(""int[]""); break;
            case System.Int64[]: System.Console.WriteLine(""long[]""); break;
            case A[]: System.Console.WriteLine(""A[]""); break;
            case G<B>[]: System.Console.WriteLine(""G<B>[]""); break;
            case N.G<B>[]: System.Console.WriteLine(""N.G<B>[]""); break;
        }
        System.Console.WriteLine(o switch
        {
            int[] => ""int[]"",
            System.Int64[] => ""long[]"",
            A[] => ""A[]"",
            G<B>[] => ""G<B>[]"",
            N.G<B>[] => ""N.G<B>[]"",
            _ => throw null,
        });
        if (o is int[]) System.Console.WriteLine(""int[]"");
        if (o is System.Int32[]) System.Console.WriteLine(""long[]"");
        if (o is A[]) System.Console.WriteLine(""A[]"");
        if (o is G<B>[]) System.Console.WriteLine(""G<B>[]"");
        if (o is N.G<B>[]) System.Console.WriteLine(""N.G<B>[]"");
        if ((o, o) is (A[], A[])) System.Console.WriteLine(""Twice."");
    }
}
class A { }
class G<T> { }
class B { }
namespace N
{
    class G<T> { }
}
";
            var expectedOutput =
@"int[]
int[]
int[]
long[]
long[]
long[]
A[]
A[]
A[]
Twice.
G<B>[]
G<B>[]
G<B>[]
N.G<B>[]
N.G<B>[]
N.G<B>[]
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(40149, "https://github.com/dotnet/roslyn/issues/40149")]
        public void ArrayTypePattern_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        M1(new int[0]);
        M1(new long[0]);
        M1(new A[0]);
        M1(new G<B>[0]);
        M1(new N.G<B>[0]);
    }
    static void M1(object o)
    {
        switch (o)
        {
            case (int[]): System.Console.WriteLine(""int[]""); break;
            case (System.Int64[]): System.Console.WriteLine(""long[]""); break;
            case (A[]): System.Console.WriteLine(""A[]""); break;
            case (G<B>[]): System.Console.WriteLine(""G<B>[]""); break;
            case (N.G<B>[]): System.Console.WriteLine(""N.G<B>[]""); break;
        }
        System.Console.WriteLine(o switch
        {
            (int[]) => ""int[]"",
            (System.Int64[]) => ""long[]"",
            (A[]) => ""A[]"",
            (G<B>[]) => ""G<B>[]"",
            (N.G<B>[]) => ""N.G<B>[]"",
            (_) => throw null,
        });
        if (o is (int[])) System.Console.WriteLine(""int[]"");
        if (o is (System.Int32[])) System.Console.WriteLine(""long[]"");
        if (o is (A[])) System.Console.WriteLine(""A[]"");
        if (o is (G<B>[])) System.Console.WriteLine(""G<B>[]"");
        if (o is (N.G<B>[])) System.Console.WriteLine(""N.G<B>[]"");
        if ((o, o) is ((A[]), (A[]))) System.Console.WriteLine(""Twice."");
    }
}
class A { }
class G<T> { }
class B { }
namespace N
{
    class G<T> { }
}
";
            var expectedOutput =
@"int[]
int[]
int[]
long[]
long[]
long[]
A[]
A[]
A[]
Twice.
G<B>[]
G<B>[]
G<B>[]
N.G<B>[]
N.G<B>[]
N.G<B>[]";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(40149, "https://github.com/dotnet/roslyn/issues/40149")]
        public void ArrayTypePattern_03()
        {
            var source = @"
class C
{
    static void Main()
    {
        M1(new int[0]);
        M1(new long[0]);
        M1(new A[0]);
        M1(new G<B>[0]);
        M1(new N.G<B>[0]);
    }
    static void M1(object o)
    {
        switch (o)
        {
            case ((int[])): System.Console.WriteLine(""int[]""); break;
            case ((System.Int64[])): System.Console.WriteLine(""long[]""); break;
            case ((A[])): System.Console.WriteLine(""A[]""); break;
            case ((G<B>[])): System.Console.WriteLine(""G<B>[]""); break;
            case ((N.G<B>[])): System.Console.WriteLine(""N.G<B>[]""); break;
        }
        System.Console.WriteLine(o switch
        {
            ((int[])) => ""int[]"",
            ((System.Int64[])) => ""long[]"",
            ((A[])) => ""A[]"",
            ((G<B>[])) => ""G<B>[]"",
            ((N.G<B>[])) => ""N.G<B>[]"",
            ((_)) => throw null,
        });
        if (o is ((int[]))) System.Console.WriteLine(""int[]"");
        if (o is ((System.Int32[]))) System.Console.WriteLine(""long[]"");
        if (o is ((A[]))) System.Console.WriteLine(""A[]"");
        if (o is ((G<B>[]))) System.Console.WriteLine(""G<B>[]"");
        if (o is ((N.G<B>[]))) System.Console.WriteLine(""N.G<B>[]"");
        if ((o, o) is ((((A[])), ((A[]))))) System.Console.WriteLine(""Twice."");
    }
}
class A { }
class G<T> { }
class B { }
namespace N
{
    class G<T> { }
}
";
            var expectedOutput =
@"int[]
int[]
int[]
long[]
long[]
long[]
A[]
A[]
A[]
Twice.
G<B>[]
G<B>[]
G<B>[]
N.G<B>[]
N.G<B>[]
N.G<B>[]";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(40149, "https://github.com/dotnet/roslyn/issues/40149")]
        public void ParsedAsExpressionBoundAsType_01()
        {
            var source = @"
class C
{
    static void Main()
    {
        M1(new A());
        M1(new G<B>.D());
        M1(new N.G<B>.D());
        M1(0);
    }
    static void M1(object o)
    {
        switch (o)
        {
            case A: System.Console.WriteLine(""A""); break;
            case G<B>.D: System.Console.WriteLine(""G<B>.D""); break;
            case N.G<B>.D: System.Console.WriteLine(""N.G<B>.D""); break;
            case System.Int32: System.Console.WriteLine(""System.Int32""); break;
        }
    }
}
class A { }
class G<T>
{
    public class D { }
}
class B { }
namespace N
{
    class G<T>
    {
        public class D { }
    }
}
";
            var expectedOutput =
@"A
G<B>.D
N.G<B>.D
System.Int32";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(40149, "https://github.com/dotnet/roslyn/issues/40149")]
        public void ParsedAsExpressionBoundAsType_02()
        {
            var source = @"
class C
{
    static void Main()
    {
        M1(new A());
        M1(new G<B>.D());
        M1(new N.G<B>.D());
        M1(0);
    }
    static void M1(object o)
    {
        switch (o)
        {
            case (A): System.Console.WriteLine(""A""); break;
            case (G<B>.D): System.Console.WriteLine(""G<B>.D""); break;
            case (N.G<B>.D): System.Console.WriteLine(""N.G<B>.D""); break;
            case (System.Int32): System.Console.WriteLine(""System.Int32""); break;
        }
    }
}
class A { }
class G<T>
{
    public class D { }
}
class B { }
namespace N
{
    class G<T>
    {
        public class D { }
    }
}
";
            var expectedOutput =
@"A
G<B>.D
N.G<B>.D
System.Int32";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void PatternCombinatorSubsumptionAndCompleteness_01()
        {
            var source = @"using System;
class C
{
    static void Main()
    {
        M(1);
        M(1L);
        M(2);
        M(2L);
        M(3);
        M(3L);
    }
    static void M(object o)
    {
        switch (o)
        {
            case 1 or long or 2: Console.Write(1); break;
            case 1L or int or 2L: Console.Write(2); break;
        }
    }
}";
            var expectedOutput = @"111121";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brfalse.s  IL_0017
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""int""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  sub
  IL_0012:  ldc.i4.1
  IL_0013:  ble.un.s   IL_001f
  IL_0015:  br.s       IL_0026
  IL_0017:  ldarg.0
  IL_0018:  isinst     ""long""
  IL_001d:  brfalse.s  IL_002c
  IL_001f:  ldc.i4.1
  IL_0020:  call       ""void System.Console.Write(int)""
  IL_0025:  ret
  IL_0026:  ldc.i4.2
  IL_0027:  call       ""void System.Console.Write(int)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void PatternCombinatorSubsumptionAndCompleteness_02()
        {
            var source = @"using System;
class C
{
    static void Main()
    {
        M(1);
        M(1L);
        M(2);
        M(2L);
        M(3);
        M(3L);
    }
    static void M(object o)
    {
        switch (o)
        {
            case (1 or not long) or 2: Console.Write(1); break;
            case 1L or (not int or 2L): Console.Write(2); break;
        }
    }
}";
            var expectedOutput = @"121212";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.M",
@"{
  // Code size       30 (0x1e)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brtrue.s   IL_0010
  IL_0008:  ldarg.0
  IL_0009:  isinst     ""long""
  IL_000e:  brtrue.s   IL_0017
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""void System.Console.Write(int)""
  IL_0016:  ret
  IL_0017:  ldc.i4.2
  IL_0018:  call       ""void System.Console.Write(int)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void PatternCombinatorSubsumptionAndCompleteness_03()
        {
            var source = @"using System;
class C
{
    static void M1(object o)
    {
        switch (o)
        {
            case 1 or not int or 2: Console.Write(1); break;
            case 1L or 2L: Console.Write(2); break;
        }
    }
    static void M2(object o)
    {
        _ = o switch
        {
            1 or not int or 2 => 1,
            1L or 2L => 2,
        };
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (9,18): error CS8120: The switch case has already been handled by a previous case.
                //             case 1L or 2L: Console.Write(2); break;
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "1L or 2L").WithLocation(9, 18),
                // (14,15): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                //         _ = o switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(14, 15),
                // (17,13): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //             1L or 2L => 2,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "1L or 2L").WithLocation(17, 13)
                );
        }

        [Fact]
        public void Relational_01()
        {
            var source = @"
class C
{
    static void M1(string s)
    {
        System.Console.WriteLine(s switch
        {
            <""0"" => ""negative"",
            ""0"" => ""zero"",
            >null => ""positive"",
        });
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (8,13): error CS8781: Relational patterns may not be used for a value of type 'string'.
                //             <"0" => "negative",
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, @"<""0""").WithArguments("string").WithLocation(8, 13),
                // (10,13): error CS8781: Relational patterns may not be used for a value of type 'string'.
                //             >null => "positive",
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, ">null").WithArguments("string").WithLocation(10, 13)
                );
        }

        [Fact]
        public void Relational_02()
        {
            foreach (string typeName in new[] { "sbyte", "short", "int" })
            {
                var source = @$"
class C
{{
    static void Main()
    {{
        M1(-100);
        M1(0);
        M1(100);
    }}
    static void M1({typeName} i)
    {{
        System.Console.WriteLine(i switch
        {{
            <0 => ""negative"",
            0 => ""zero"",
            >0 => ""positive"",
        }});
    }}
}}
";
                var expectedOutput =
    @"negative
zero
positive";
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
                compilation.VerifyDiagnostics(
                    );
                var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_03()
        {
            foreach (string typeName in new[] { "byte", "sbyte", "short", "ushort", "int" })
            {
                var source = @$"
class C
{{
    static void Main()
    {{
        M1(0);
        M1(12);
        M1(50);
        M1(100);
    }}
    static void M1({typeName} i)
    {{
        System.Console.WriteLine(i switch
        {{
            <50 => ""less"",
            50 => ""same"",
            >50 => ""more"",
        }});
    }}
}}
";
                var expectedOutput =
    @"less
less
same
more";
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
                compilation.VerifyDiagnostics(
                    );
                var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_04()
        {
            var source = @"
class C
{
    static void M1(object o, decimal d)
    {
        _ = o is < 12m;
        _ = d is < 0;
        _ = o is < 10;
        switch (d)
        {
            case < 0m:
            case <= 0m:
            case > 0m:
            case >= 0m:
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //         _ = o is < 12m;
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< 12m").WithArguments("decimal").WithLocation(6, 18),
                // (7,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //         _ = d is < 0;
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< 0").WithArguments("decimal").WithLocation(7, 18),
                // (11,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //             case < 0m:
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< 0m").WithArguments("decimal").WithLocation(11, 18),
                // (12,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //             case <= 0m:
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "<= 0m").WithArguments("decimal").WithLocation(12, 18),
                // (13,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //             case > 0m:
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "> 0m").WithArguments("decimal").WithLocation(13, 18),
                // (14,18): error CS8781: Relational patterns may not be used for a value of type 'decimal'.
                //             case >= 0m:
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, ">= 0m").WithArguments("decimal").WithLocation(14, 18)
                );
        }

        [Fact]
        public void Relational_05()
        {
            foreach (string typeName in new[] { "byte", "sbyte", "short", "ushort", "int" })
            {
                var source = string.Format(@"
class C
{{
    static void Main()
    {{
        M1(({0})0);
        M1(({0})12);
        M1(({0})50);
        M1(({0})100);
        M1(12m);
    }}
    static void M1(object i)
    {{
        System.Console.WriteLine(i switch
        {{
            < ({0})50 => ""less"",
            ({0})50 => ""same"",
            > ({0})50 => ""more"",
            _ => ""incomparable"",
        }});
    }}
}}
", typeName);
                var expectedOutput =
    @"less
less
same
more
incomparable";
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview), options: TestOptions.DebugExe);
                compilation.VerifyDiagnostics(
                    );
                var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_06()
        {
            var source = @"
class C
{
    bool M1(object o) => o is < (0.0d / 0.0d);
    bool M2(object o) => o is < (0.0f / 0.0f);
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (4,33): error CS8782: Relational patterns may not be used for a floating-point NaN.
                //     bool M1(object o) => o is < (0.0d / 0.0d);
                Diagnostic(ErrorCode.ERR_RelationalPatternWithNaN, "(0.0d / 0.0d)").WithLocation(4, 33),
                // (5,33): error CS8782: Relational patterns may not be used for a floating-point NaN.
                //     bool M2(object o) => o is < (0.0f / 0.0f);
                Diagnostic(ErrorCode.ERR_RelationalPatternWithNaN, "(0.0f / 0.0f)").WithLocation(5, 33));
        }

        [Fact]
        public void Relational_07()
        {
            var source = @"
class C
{
    public bool M(char c) => c switch
    {
        >= 'A' and <= 'Z' or >= 'a' and <= 'z' => true,
        'a'                                    => true, // error 1
        > 'k' and < 'o'                        => true, // error 2
        '0'                                    => true,
        >= '0' and <= '9'                      => true,
        _                                      => false,
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (7,9): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //         'a'                                    => true, // error 1
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "'a'").WithLocation(7, 9),
                // (8,9): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //         > 'k' and < 'o'                        => true, // error 2
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "> 'k' and < 'o'").WithLocation(8, 9)
                );
        }

        [Fact]
        public void Relational_08()
        {
            var source = @"
class C
{
    public int M(uint c) => c switch
    {
        >= 5 => 1,
        4 => 2,
        3 => 3,
        2 => 4,
        1 => 5,
        0 => 6,
        _ => 7,
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (12,9): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //         _ => 7,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(12, 9)
                );
        }

        [Fact]
        public void Relational_09()
        {
            var source = @"
class C
{
    public int M(uint c) => c switch
    {
        | 3 => 3,
        || 4 => 4,
        & 5 => 5,
        && 6 => 6,
        _ => 7
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (5,6): error CS1525: Invalid expression term '|'
                //     {
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("|").WithLocation(5, 6),
                // (6,18): error CS1525: Invalid expression term '||'
                //         | 3 => 3,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("||").WithLocation(6, 18),
                // (8,11): error CS0211: Cannot take the address of the given expression
                //         & 5 => 5,
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "5").WithLocation(8, 11),
                // (8,18): error CS1525: Invalid expression term '&&'
                //         & 5 => 5,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("&&").WithLocation(8, 18)
                );
        }

        [Fact]
        public void Relational_10()
        {
            var source = @"
class C
{
    public int M(uint c) => c switch
    {
        == 0 => 1,
        != 2 => 2,
        _ => 7
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (6,9): error CS1525: Invalid expression term '=='
                //         == 0 => 1,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "==").WithArguments("==").WithLocation(6, 9),
                // (7,9): error CS1525: Invalid expression term '!='
                //         != 2 => 2,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "!=").WithArguments("!=").WithLocation(7, 9)
                );
        }

        [Fact]
        public void Relational_EnumSubsumption()
        {
            var source = @"
enum E : uint
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}
class C
{
    public int M(E c) => c switch
    {
        >= E.Five => 1,
        E.Four => 2,
        E.Three => 3,
        E.Two => 4,
        E.One => 5,
        E.Zero => 6,
        _ => 7, // 1
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
            compilation.VerifyDiagnostics(
                // (23,9): error CS8510: The pattern has already been handled by a previous arm of the switch expression.
                //         _ => 7, // 1
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(23, 9)
                );
        }

        [Fact]
        public void Relational_SignedEnumComplete()
        {
            foreach (var typeName in new[] { "sbyte", "short", "int", "long" })
            {
                var source = @"
using System;
enum E : " + typeName + @"
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}
class C
{
    static void Main()
    {
        Console.WriteLine(M(E.Zero));
        Console.WriteLine(M(E.One));
        Console.WriteLine(M(E.Two));
        Console.WriteLine(M(E.Three));
        Console.WriteLine(M(E.Four));
        Console.WriteLine(M(E.Five));
        Console.WriteLine(M(E.Six));
        Console.WriteLine(M(E.Seven));
        Console.WriteLine(M((E)100));
        Console.WriteLine(M((E)(-100)));
    }
    static int M(E c) => c switch
    {
        >= E.Five => 1,
        E.Four => 2,
        E.Three => 3,
        E.Two => 4,
        E.One => 5,
        E.Zero => 6,
        _ => 7, // handles negative values
    };
}";
                var expectedOutput = @"6
5
4
3
2
1
1
1
1
7";
                var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
                compilation.VerifyDiagnostics(
                    );
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_UnsignedEnumComplete()
        {
            foreach (var typeName in new[] { "byte", "ushort", "uint", "ulong" })
            {
                var source = @"
using System;
enum E : " + typeName + @"
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}
class C
{
    static void Main()
    {
        Console.WriteLine(M(E.Zero));
        Console.WriteLine(M(E.One));
        Console.WriteLine(M(E.Two));
        Console.WriteLine(M(E.Three));
        Console.WriteLine(M(E.Four));
        Console.WriteLine(M(E.Five));
        Console.WriteLine(M(E.Six));
        Console.WriteLine(M(E.Seven));
        Console.WriteLine(M((E)100));
    }
    static int M(E c) => c switch
    {
        > E.Five => 1,
        E.Four => 2,
        E.Three => 3,
        E.Two => 4,
        E.One => 5,
        E.Zero => 6,
        _ => 7, // handles E.Five
    };
}";
                var expectedOutput = @"6
5
4
3
2
7
1
1
1";
                var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
                compilation.VerifyDiagnostics(
                    );
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_SignedEnumExhaustive()
        {
            foreach (var typeName in new[] { "byte", "ushort", "uint", "ulong" })
            {
                foreach (var withExhaustive in new[] { false, true })
                {
                    var source = @"
enum E : " + typeName + @"
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}
class C
{
    static int M(E c) => c switch
    {
        > E.Five => 1,
        E.Four => 2,
        E.Three => 3,
        E.Two => 4,
        E.One => 5,
        E.Zero => 6,
" +
(withExhaustive ? @"        E.Five => 7, // exhaustive
" : "")
+ @"    };
}";
                    var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
                    if (withExhaustive)
                    {
                        compilation.VerifyDiagnostics(
                            );
                    }
                    else
                    {
                        compilation.VerifyDiagnostics(
                            // (15,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                            //     static int M(E c) => c switch
                            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(15, 28)
                            );
                    }
                }
            }
        }

        [Fact]
        public void Relational_UnsignedEnumExhaustive()
        {
            foreach (var typeName in new[] { "sbyte", "short", "int", "long" })
            {
                foreach (var withExhaustive in new[] { false, true })
                {
                    var source = @"
enum E : " + typeName + @"
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven
}
class C
{
    static int M(E c) => c switch
    {
        > E.Five => 1,
        E.Four => 2,
        E.Three => 3,
        E.Two => 4,
        E.One => 5,
        <= E.Zero => 6,
" +
(withExhaustive ? @"        E.Five => 7, // exhaustive
" : "")
+ @"    };
}";
                    var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Preview));
                    if (withExhaustive)
                    {
                        compilation.VerifyDiagnostics(
                            );
                    }
                    else
                    {
                        compilation.VerifyDiagnostics(
                            // (15,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                            //     static int M(E c) => c switch
                            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(15, 28)
                            );
                    }
                }
            }
        }
    }
}
