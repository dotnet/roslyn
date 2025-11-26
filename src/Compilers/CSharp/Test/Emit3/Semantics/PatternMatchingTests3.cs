// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests3 : PatternMatchingTestBase
    {
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
            // ILVerify: Return type is ByRef, TypedReference, ArgHandle, or ArgIterator.
            var comp = CompileAndVerify(compilation, verify: Verification.FailsILVerify, expectedOutput: expectedOutput);
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
                // (17,31): error CS8352: Cannot use variable 'span' in this context because it may expose referenced variables outside of their declaration scope
                //             1 => new RGBColor(span),
                Diagnostic(ErrorCode.ERR_EscapeVariable, "span").WithArguments("span").WithLocation(17, 31));
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
        public void TargetTypedSwitch_Overload_01()
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
        public void TargetTypedSwitch_Overload_02()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        M(b switch { false => 1, true => 2 });
    }
    static void M(Int16 s) => Console.Write(nameof(Int16));
    static void M(Int64 l) => Console.Write(nameof(Int64));
}
";
            var expectedOutput = "Int16";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithNullableContextOptions(NullableContextOptions.Disable));
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TargetTypedSwitch_Overload_03()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        bool b = true;
        M(b switch { false => 1, true => 2 });
    }
    static void M(Int16 s) => Console.Write(nameof(Int16));
}
";
            var expectedOutput = "Int16";
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
                // (6,29): error CS8370: Feature 'null pointer constant pattern' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     bool M1(int* p) => p is null; // 1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "null").WithArguments("null pointer constant pattern", "8.0").WithLocation(6, 29),
                // (7,29): error CS8521: Pattern-matching is not permitted for pointer types.
                //     bool M2(int* p) => p is var _; // 2
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var _").WithLocation(7, 29),
                // (12,18): error CS8370: Feature 'null pointer constant pattern' is not available in C# 7.3. Please use language version 8.0 or greater.
                //             case null: // 3
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "null").WithArguments("null pointer constant pattern", "8.0").WithLocation(12, 18),
                // (20,18): error CS8521: Pattern-matching is not permitted for pointer types.
                //             case var _: // 4
                Diagnostic(ErrorCode.ERR_PointerTypeInPatternMatching, "var _").WithLocation(20, 18)
            );
            CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular8).VerifyDiagnostics();
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
                // (4,19): warning CS8618: Non-nullable field 'o' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                //     static object o;
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "o").WithArguments("field", "o").WithLocation(4, 19),
                // (4,19): warning CS0649: Field 'C.o' is never assigned to, and will always have its default value null
                //     static object o;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "o").WithArguments("C.o", "null").WithLocation(4, 19),
                // (7,12): error CS0716: Cannot convert to static type 'C'
                //         _= (C)(o switch { _ => default }); 
                Diagnostic(ErrorCode.ERR_ConvertToStaticClass, "(C)(o switch { _ => default })").WithArguments("C").WithLocation(7, 12),
                // (7,12): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         _= (C)(o switch { _ => default }); 
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "(C)(o switch { _ => default })").WithLocation(7, 12),
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
                    IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object, NarrowedType: System.Object)
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'default')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'default')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DefaultLiteral)
                      Operand: 
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: C?, Constant: null, IsInvalid) (Syntax: 'default')
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
                    IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid) (Syntax: '(C)(o switc ...  default })')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (SwitchExpression)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C?, IsInvalid, IsImplicit) (Syntax: 'o switch {  ... > default }')
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
                    IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object, NarrowedType: System.Object)
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
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid) (Syntax: '(C)(o switc ... ow null! })')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (SwitchExpression)
                      Operand: 
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
              IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid) (Syntax: '(C)(o switc ...  default })')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: C?, IsInvalid) (Syntax: 'o switch {  ... > default }')
                    Value: 
                      IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                        Instance Receiver: 
                          null
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => default')
                          Pattern: 
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object, NarrowedType: System.Object)
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'default')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                IDefaultValueOperation (OperationKind.DefaultValue, Type: C?, Constant: null, IsInvalid) (Syntax: 'default')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_= (C)(o sw ... w null! });')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: '_= (C)(o sw ... ow null! })')
            Left: 
              IDiscardOperation (Symbol: C _) (OperationKind.Discard, Type: C) (Syntax: '_')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid) (Syntax: '(C)(o switc ... ow null! })')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ISwitchExpressionOperation (1 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: C, IsInvalid) (Syntax: 'o switch {  ... row null! }')
                    Value: 
                      IFieldReferenceOperation: System.Object C.o (Static) (OperationKind.FieldReference, Type: System.Object, IsInvalid) (Syntax: 'o')
                        Instance Receiver: 
                          null
                    Arms(1):
                        ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '_ => throw null!')
                          Pattern: 
                            IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null, IsInvalid) (Syntax: '_') (InputType: System.Object, NarrowedType: System.Object)
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
                    // (5,41): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '1' is not covered.
                    //         var x = ((Func<int, decimal>)(0 switch { 0 => _ => {}}))(0);
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("1").WithLocation(5, 41),
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
                    // (5,41): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '1' is not covered.
                    //         var x = ((Func<int, decimal>)(0 switch { 0 => M }))(0);
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("1").WithLocation(5, 41),
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

        if (o is not int x6) { }
        if (o is not (1 and int x7)) { }
    }

    void Bad(object o)
    {
        if (o is int y1 or 1) { }
        if (o is int y2 or (1 or 2)) { }
        if (o is 1 or int y3) { }
        if (o is (1 or 2) or int y4) { }
        if (o is Point { X: var y5 } or Animal _) { }
        if (o is Point(var y6, _) or Animal _) { }
        if (o is object or (1 or var y7)) { }
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            compilation.VerifyDiagnostics(
                // (19,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is int y1 or 1) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y1").WithLocation(19, 22),
                // (20,22): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is int y2 or (1 or 2)) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y2").WithLocation(20, 22),
                // (21,27): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is 1 or int y3) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y3").WithLocation(21, 27),
                // (22,34): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is (1 or 2) or int y4) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y4").WithLocation(22, 34),
                // (23,33): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is Point { X: var y5 } or Animal _) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y5").WithLocation(23, 33),
                // (24,28): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is Point(var y6, _) or Animal _) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y6").WithLocation(24, 28),
                // (25,13): warning CS8794: An expression of type 'object' always matches the provided pattern.
                //         if (o is object or (1 or var y7)) { }
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "o is object or (1 or var y7)").WithArguments("object").WithLocation(25, 13),
                // (25,38): error CS8780: A variable may not be declared within a 'not' or 'or' pattern.
                //         if (o is object or (1 or var y7)) { }
                Diagnostic(ErrorCode.ERR_DesignatorBeneathPatternCombinator, "y7").WithLocation(25, 38)
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.ReleaseExe);
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyDiagnostics();
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.ReleaseExe);
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyDiagnostics();
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case 1L or 2L: Console.Write(2); break;
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "1L or 2L").WithLocation(9, 18),
                // (14,15): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
                //         _ = o switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("0").WithLocation(14, 15),
                // (17,13): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
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
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            case >= 0m: // error: subsumed
                break;
        }
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (14,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case >= 0m: // error: subsumed
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, ">= 0m").WithLocation(14, 18)
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
                var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9), options: TestOptions.DebugExe);
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (7,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //         'a'                                    => true, // error 1
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "'a'").WithLocation(7, 9),
                // (8,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (12,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (6,9): error CS1525: Invalid expression term '|'
                //         | 3 => 3,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "|").WithArguments("|").WithLocation(6, 9),
                // (7,9): error CS1525: Invalid expression term '||'
                //         || 4 => 4,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "||").WithArguments("||").WithLocation(7, 9),
                // (8,11): error CS0211: Cannot take the address of the given expression
                //         & 5 => 5,
                Diagnostic(ErrorCode.ERR_InvalidAddrOp, "5").WithLocation(8, 11),
                // (9,9): error CS1525: Invalid expression term '&&'
                //         && 6 => 6,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "&&").WithArguments("&&").WithLocation(9, 9));
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
                var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
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
                var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
                compilation.VerifyDiagnostics(
                    );
                var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }
        }

        [Fact]
        public void Relational_UnsignedEnumExhaustive()
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
                    var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
                    if (withExhaustive)
                    {
                        compilation.VerifyDiagnostics(
                            );
                    }
                    else
                    {
                        compilation.VerifyDiagnostics(
                            // (15,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'E.Five' is not covered.
                            //     static int M(E c) => c switch
                            Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("E.Five").WithLocation(15, 28)
                            );
                    }
                }
            }
        }

        [ConditionalTheory(typeof(NoUsedAssembliesValidation))] // The test hook is blocked by https://github.com/dotnet/roslyn/issues/44657
        [WorkItem(44657, "https://github.com/dotnet/roslyn/issues/44657")]
        [InlineData("sbyte", true)]
        [InlineData("short", true)]
        [InlineData("int", true)]
        [InlineData("long", true)]
        [InlineData("sbyte", false)]
        [InlineData("short", false)]
        [InlineData("int", false)]
        [InlineData("long", false)]
        public void Relational_SignedEnumExhaustive(string typeName, bool withExhaustive)
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            if (withExhaustive)
            {
                compilation.VerifyEmitDiagnostics(
                    );
            }
            else
            {
                compilation.VerifyEmitDiagnostics(
                    // (15,28): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'E.Five' is not covered.
                    //     static int M(E c) => c switch
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("E.Five").WithLocation(15, 28)
                    );
            }
        }

        [Fact]
        public void Relational_10()
        {
            var source = @"
class C
{
    public int M(uint c) => /*<bind>*/c switch
    {
        == 0 => 1,
        != 2 => 2,
        _ => 7
    }/*</bind>*/;
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (6,9): error CS9344: The '==' operator is not supported in a pattern.
                //         == 0 => 1,
                Diagnostic(ErrorCode.ERR_EqualityOperatorInPatternNotSupported, "==").WithLocation(6, 9),
                // (6,12): error CS9135: A constant value of type 'uint' is expected
                //         == 0 => 1,
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "0").WithArguments("uint").WithLocation(6, 12),
                // (7,9): error CS9345: The '!=' operator is not supported in a pattern. Use 'not' to represent a negated pattern.
                //         != 2 => 2,
                Diagnostic(ErrorCode.ERR_InequalityOperatorInPatternNotSupported, "!=").WithLocation(7, 9));

            VerifyOperationTreeForTest<SwitchExpressionSyntax>(compilation, @"
ISwitchExpressionOperation (3 arms, IsExhaustive: True) (OperationKind.SwitchExpression, Type: System.Int32, IsInvalid) (Syntax: 'c switch ... }')
  Value:
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.UInt32) (Syntax: 'c')
  Arms(3):
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '0 => 1')
        Pattern:
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid) (Syntax: '0') (InputType: System.UInt32, NarrowedType: System.UInt32)
            Value:
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, IsInvalid, IsImplicit) (Syntax: '0')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand:
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
        Value:
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null, IsInvalid) (Syntax: '        != 2 => 2')
        Pattern:
          INegatedPatternOperation (OperationKind.NegatedPattern, Type: null, IsInvalid) (Syntax: '        != 2') (InputType: System.UInt32, NarrowedType: System.UInt32)
            Pattern:
              IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '2') (InputType: System.UInt32, NarrowedType: System.UInt32)
                Value:
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand:
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Value:
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals) (OperationKind.SwitchExpressionArm, Type: null) (Syntax: '_ => 7')
        Pattern:
          IDiscardPatternOperation (OperationKind.DiscardPattern, Type: null) (Syntax: '_') (InputType: System.UInt32, NarrowedType: System.UInt32)
        Value:
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7) (Syntax: '7')");
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
            var compilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                // (23,9): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //         _ => 7, // 1
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "_").WithLocation(23, 9)
                );
        }

        [Fact]
        public void Relational_11()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        Test(0.000012m);
        Test(40m);
        Test(46.12m);
        Test(50m);
        Test(56.12m);
        Test(60m);
        Test(66.12m);
        Test(69.999m);
        Test(70.000m);
        Test(70.001m);
        Test(76.12m);
        Test(80m);
        Test(86.12m);
        Test(90m);
        Test(96.12m);
        Test(100m);
        Test(106.12m);
        Test(110m);
        Test(111111111m);
    }
    private static void Test(decimal percent)
    {
        Console.WriteLine(FormattableString.Invariant($""{percent} => {Grade(percent)}""));
    }
    public static char Grade(decimal score) => score switch
    {
        <= 60.000m => 'F',
        <= 70.00m => 'D',
        <= 80.0m => 'C',
        <= 90m => 'B',
        > 90m => 'A',
    };
}";
            var expectedOutput = @"0.000012 => F
40 => F
46.12 => F
50 => F
56.12 => F
60 => F
66.12 => D
69.999 => D
70.000 => D
70.001 => C
76.12 => C
80 => C
86.12 => B
90 => B
96.12 => A
100 => A
106.12 => A
110 => A
111111111 => A";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.Grade", @"
    {
      // Code size      120 (0x78)
      .maxstack  6
      .locals init (char V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  brtrue.s   IL_0004
      IL_0003:  nop
      IL_0004:  ldarg.0
      IL_0005:  ldc.i4     0x320
      IL_000a:  ldc.i4.0
      IL_000b:  ldc.i4.0
      IL_000c:  ldc.i4.0
      IL_000d:  ldc.i4.1
      IL_000e:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
      IL_0013:  call       ""bool decimal.op_LessThanOrEqual(decimal, decimal)""
      IL_0018:  brfalse.s  IL_0048
      IL_001a:  ldarg.0
      IL_001b:  ldc.i4     0xea60
      IL_0020:  ldc.i4.0
      IL_0021:  ldc.i4.0
      IL_0022:  ldc.i4.0
      IL_0023:  ldc.i4.3
      IL_0024:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
      IL_0029:  call       ""bool decimal.op_LessThanOrEqual(decimal, decimal)""
      IL_002e:  brtrue.s   IL_0059
      IL_0030:  ldarg.0
      IL_0031:  ldc.i4     0x1b58
      IL_0036:  ldc.i4.0
      IL_0037:  ldc.i4.0
      IL_0038:  ldc.i4.0
      IL_0039:  ldc.i4.2
      IL_003a:  newobj     ""decimal..ctor(int, int, int, bool, byte)""
      IL_003f:  call       ""bool decimal.op_LessThanOrEqual(decimal, decimal)""
      IL_0044:  brtrue.s   IL_005e
      IL_0046:  br.s       IL_0063
      IL_0048:  ldarg.0
      IL_0049:  ldc.i4.s   90
      IL_004b:  newobj     ""decimal..ctor(int)""
      IL_0050:  call       ""bool decimal.op_LessThanOrEqual(decimal, decimal)""
      IL_0055:  brtrue.s   IL_0068
      IL_0057:  br.s       IL_006d
      IL_0059:  ldc.i4.s   70
      IL_005b:  stloc.0
      IL_005c:  br.s       IL_0072
      IL_005e:  ldc.i4.s   68
      IL_0060:  stloc.0
      IL_0061:  br.s       IL_0072
      IL_0063:  ldc.i4.s   67
      IL_0065:  stloc.0
      IL_0066:  br.s       IL_0072
      IL_0068:  ldc.i4.s   66
      IL_006a:  stloc.0
      IL_006b:  br.s       IL_0072
      IL_006d:  ldc.i4.s   65
      IL_006f:  stloc.0
      IL_0070:  br.s       IL_0072
      IL_0072:  ldc.i4.1
      IL_0073:  brtrue.s   IL_0076
      IL_0075:  nop
      IL_0076:  ldloc.0
      IL_0077:  ret
    }
");
        }

        [Fact]
        public void OutputType_01()
        {
            var source = @"using System;
class C
{
    static void Main()
    {
        // 1. If P is a type pattern, the narrowed type is the type of the type pattern's type.
        object o = 1;
        { if (o is int and var i) M(i); } // System.Int32

        // 2. If P is a declaration pattern, the narrowed type is the type of the declaration pattern's type.
        o = 1L;
        { if (o is long q and var i) M(i); } // System.Int64

        // 3. If P is a recursive pattern that gives an explicit type, the narrowed type is that type.
        o = 1UL;
        { if (o is ulong {} and var i) M(i); } // System.UInt64

        // 4. If P is a constant pattern where the constant is not the null constant and where the expression has no constant expression conversion to the input type, the narrowed type is the type of the constant.
        o = (byte)1;
        { if (o is (byte)1 and var i) M(i); } // System.Byte

        // 5. If P is a relational pattern where the constant expression has no constant expression conversion to the input type, the narrowed type is the type of the constant.
        o = (uint)1;
        { if (o is <= 10U and var i) M(i); } // System.UInt32

        // 6. If P is an or pattern, the narrowed type is the common type of the narrowed type of the left pattern and the narrowed type of the right pattern if such a common type exists.
        o = 1;
        { if (o is (1 or 2) and var i) M(i); } // System.Int32

        // 7. If P is an and pattern, the narrowed type is the narrowed type of the right pattern. Moreover, the narrowed type of the left pattern is the input type of the right pattern.
        o = ""SomeString"";
        { if (o is (var x and string y) and var i) M(i); } // System.String

        // 8. Otherwise the narrowed type of P is P's input type.
        o = new Q();
        { if (o is (3, 4) and var i) M(i); } // System.Runtime.CompilerServices.ITuple

        o = null;
        { if (o is null and var i) M(i); } // System.Object
        { if (o is (var x and null) and var i) M(i); } // System.Object
        { long x = 42; if (x is 42 and var i) M(i); } // expect System.Int64
    }
    static void M<T>(T t)
    {
        Console.WriteLine(typeof(T));
    }
    class Q: System.Runtime.CompilerServices.ITuple
    {
        public int Length => 2;
        public object this[int index] => index + 3;
    }
}";
            var expectedOutput =
@"System.Int32
System.Int64
System.UInt64
System.Byte
System.UInt32
System.Int32
System.String
System.Runtime.CompilerServices.ITuple
System.Object
System.Object
System.Int64
";
            var compilation = CreateCompilation(source + _iTupleSource, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(43377, "https://github.com/dotnet/roslyn/issues/43377")]
        public void OutputType_02()
        {
            var source = @"#nullable enable
using System;
using System.Collections.Generic;

class C
{
    static void Main()
    {
        // The narrowed type of an or pattern combines the narrowed type of all subpatterns
        // beneath all of the or combinators.

        // Identity conversions
        object o = new Dictionary<object, object>();
        { if (o is (Dictionary<object, dynamic> or Dictionary<dynamic?, object?>) and var i) M(i); } // System.Collections.Generic.Dictionary<object, object>

        // Boxing conversions
        o = 1;
        { if (o is (long or IComparable) and var i) M(i); } // System.IComparable
        { if (o is (IComparable or long) and var i) M(i); } // System.IComparable

        // Incomparable types
        o = 1L;
        { if (o is (IEquatable<string> or long) and var i) M(i); } // System.Object
        { if (o is (long or IEquatable<string>) and var i) M(i); } // System.Object
        o = new Derived1();
        { if (o is (Derived1 or Derived2) and var i) M(i); } // System.Object

        // Implicit reference conversions
        o = new Derived1();
        { if (o is (Derived1 or Base) and var i) M(i); } // Base
        { if (o is (Base or Derived1) and var i) M(i); } // Base

        // Implicit reference conversions involving variance
        o = new X();
        { if (o is (IIn<Derived1> or IIn<Base>) and var i) M(i); } // IIn<Derived1>
        { if (o is (IIn<Base> or IIn<Derived1>) and var i) M(i); } // IIn<Derived1>
        { if (o is (IOut<Derived1> or IOut<Base>) and var i) M(i); } // IOut<Base>
        { if (o is (IOut<Base> or IOut<Derived1>) and var i) M(i); } // IOut<Base>

        // Multiple layers of or patterns
        o = new Derived1();
        { if (o is (Derived1 or Derived2 or Base) and var i) M(i); } // Base
        { if (o is ((Derived1 or Derived2) or Base) and var i) M(i); } // Base
        { if (o is (Base or (Derived1 or Derived2)) and var i) M(i); } // Base
        { if (o is (Derived1 or Base or Derived2) and var i) M(i); } // Base
        { if (o is ((Derived1 or Derived2) or (Derived1 or Derived2) or Base or (Derived1 or Derived2)) and var i) M(i); } // Base

    }
    static void M<T>(T t)
    {
        Console.WriteLine(typeof(T));
    }
}
class Base { }
class Derived1 : Base { }
class Derived2 : Base { }
interface IIn<in T> { }
interface IOut<out T> { }
class X : IIn<Base>, IOut<Base> { }
";
            var expectedOutput =
@"System.Collections.Generic.Dictionary`2[System.Object,System.Object]
System.IComparable
System.IComparable
System.Object
System.Object
System.Object
Base
Base
IIn`1[Derived1]
IIn`1[Derived1]
IOut`1[Base]
IOut`1[Base]
Base
Base
Base
Base
Base
";
            var compilation = CreateCompilation(source + _iTupleSource, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void DoNotShareTempMutatedThroughReceiver_01()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        S s;
        s = new S(1);
        Console.Write(s switch
        {
            { N: 1 } when s.No() => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(new S(1) switch
        {
            { N: 1 } s0 when s0.No() => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = new S(1);
        Console.Write(s switch
        {
            { N: 1 } when s.Nope => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(new S(1) switch
        {
            { N: 1 } s0 when s0.Nope => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = new S(1);
        Console.Write(s switch
        {
            { N: 1 } when s[0] => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(new S(1) switch
        {
            { N: 1 } s0 when s0[0] => 1,
            { Q: 1 } => 2,
            _ => 3,
        });
    }
}
struct S
{
    public int N;
    public int Q => N;

    public S(int n) => N = n;

    public bool No() { N++; return false; }
    public bool Nope { get { N++; return false; } }
    public bool this[int t] { get { N++; return false; } }
}
";
            var expectedOutput = "222222";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DoNotShareTempMutatedThroughPointer_01()
        {
            var source = @"
using System;
class Program
{
    static unsafe void Main()
    {
        S s;
        s = new S(1);
        Console.Write(s switch
        {
            { N: 1 } when Mutate(&s.N) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(new S(1) switch
        {
            { N: 1 } s0 when Mutate(&s0.N) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });
    }

    static unsafe bool Mutate(int* p)
    {
        (*p) ++;
        return false;
    }
}
struct S
{
    public int N;
    public int Q => N;

    public S(int n) => N = n;
}
";
            var expectedOutput = "22";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Skipped);
        }

        [Fact]
        public void DoNotShareTempMutatedThroughReceiver_02()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        M<S>(new S(1));
    }

    static void M<S>(S news) where S : I
    {
        S s;
        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when s.No() => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when s0.No() => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when s.Nope => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when s0.Nope => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when s[0] => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when s0[0] => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        S copy() => news;
    }
}
interface I
{
    int N { get; }
    int Q { get; }

    bool No();
    bool Nope { get; }
    bool this[int t] { get; }
}
struct S : I
{
    public int N { get; private set; }
    public int Q => N;

    public S(int n) => N = n;

    public bool No() { N++; return false; }
    public bool Nope { get { N++; return false; } }
    public bool this[int t] { get { N++; return false; } }
}
";
            var expectedOutput = "222222";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DoNotShareTempMutatedThroughReceiverInExpressionTree_02()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        M<S>(new S(1));
    }

    static void M<S>(S news) where S : I
    {
        S s;
        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when Invoke(() => s.No()) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when Invoke(() => s0.No()) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when Invoke(() => s.Nope) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when Invoke(() => s0.Nope) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        s = copy();
        Console.Write(s switch
        {
            { N: 1 } when Invoke(() => s[0]) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        Console.Write(copy() switch
        {
            { N: 1 } s0 when Invoke(() => s0[0]) => 1,
            { Q: 1 } => 2,
            _ => 3,
        });

        S copy() => news;
    }

    static bool Invoke(Expression<Func<bool>> e) => e.Compile()();
}
interface I
{
    int N { get; }
    int Q { get; }

    bool No();
    bool Nope { get; }
    bool this[int t] { get; }
}
struct S : I
{
    public int N { get; private set; }
    public int Q => N;

    public S(int n) => N = n;

    public bool No() { N++; return false; }
    public bool Nope { get { N++; return false; } }
    public bool this[int t] { get { N++; return false; } }
}
";
            var expectedOutput = "222222";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DoNotShareTempMutatedThroughLambda_01()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        S s = new S(1);
        Func<bool> condition = () => { s = new S(10); return false; };
        Console.Write(s switch
        {
            { N: 1 } when condition() => 1,
            { Q: 1 } => 2,
            _ => 3,
        });
    }
}
struct S
{
    public readonly int N;
    public int Q => N;

    public S(int n) => N = n;
}
";
            var expectedOutput = "2";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ReadonlyAffectsSharing_01()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        M1();
        M2();
    }
    static void M1()
    {
        Console.Write(new S1(1) switch
        {
            { N: 1 } x when x.Q == 2 => 1,
            { Q: 1 } => 2,
            _ => 3,
        });
    }
    static void M2()
    {
        Console.Write(new S2(1) switch
        {
            { N: 1 } x when x.Q == 2 => 1,
            { Q: 1 } => 2,
            _ => 3,
        });
    }
}
struct S1
{
    public readonly int N;
    public int Q => N;

    public S1(int n) => N = n;
}
struct S2
{
    public readonly int N;
    public readonly int Q => N;

    public S2(int n) => N = n;
}
";
            var expectedOutput = "22";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            // Note that each of the methods M1 and M2 have two calls to Q.get.  In M1
            // we cannot apply the sharing optimization (because the getter might mutate the
            // receiver) and so the two invocations are on different variables.  In M2
            // we can apply that optimization because Q.get is readonly, so the two invocations
            // are on the same variable.
            compVerifier.VerifyIL("Program.M1", @"
    {
      // Code size       78 (0x4e)
      .maxstack  2
      .locals init (S1 V_0, //x
                    int V_1,
                    S1 V_2,
                    int V_3,
                    int V_4)
      IL_0000:  nop
      IL_0001:  ldloca.s   V_2
      IL_0003:  ldc.i4.1
      IL_0004:  call       ""S1..ctor(int)""
      IL_0009:  ldc.i4.1
      IL_000a:  brtrue.s   IL_000d
      IL_000c:  nop
      IL_000d:  ldloc.2
      IL_000e:  ldfld      ""int S1.N""
      IL_0013:  stloc.3
      IL_0014:  ldloc.3
      IL_0015:  ldc.i4.1
      IL_0016:  beq.s      IL_0028
      IL_0018:  ldloca.s   V_2
      IL_001a:  call       ""int S1.Q.get""
      IL_001f:  stloc.s    V_4
      IL_0021:  ldloc.s    V_4
      IL_0023:  ldc.i4.1
      IL_0024:  beq.s      IL_003a
      IL_0026:  br.s       IL_003e
      IL_0028:  ldloc.2
      IL_0029:  stloc.0
      IL_002a:  ldloca.s   V_0
      IL_002c:  call       ""int S1.Q.get""
      IL_0031:  ldc.i4.2
      IL_0032:  beq.s      IL_0036
      IL_0034:  br.s       IL_0018
      IL_0036:  ldc.i4.1
      IL_0037:  stloc.1
      IL_0038:  br.s       IL_0042
      IL_003a:  ldc.i4.2
      IL_003b:  stloc.1
      IL_003c:  br.s       IL_0042
      IL_003e:  ldc.i4.3
      IL_003f:  stloc.1
      IL_0040:  br.s       IL_0042
      IL_0042:  ldc.i4.1
      IL_0043:  brtrue.s   IL_0046
      IL_0045:  nop
      IL_0046:  ldloc.1
      IL_0047:  call       ""void System.Console.Write(int)""
      IL_004c:  nop
      IL_004d:  ret
    }
");
            compVerifier.VerifyIL("Program.M2", @"
    {
      // Code size       74 (0x4a)
      .maxstack  2
      .locals init (S2 V_0, //x
                    int V_1,
                    int V_2,
                    int V_3)
      IL_0000:  nop
      IL_0001:  ldloca.s   V_0
      IL_0003:  ldc.i4.1
      IL_0004:  call       ""S2..ctor(int)""
      IL_0009:  ldc.i4.1
      IL_000a:  brtrue.s   IL_000d
      IL_000c:  nop
      IL_000d:  ldloc.0
      IL_000e:  ldfld      ""int S2.N""
      IL_0013:  stloc.2
      IL_0014:  ldloc.2
      IL_0015:  ldc.i4.1
      IL_0016:  beq.s      IL_0026
      IL_0018:  ldloca.s   V_0
      IL_001a:  call       ""readonly int S2.Q.get""
      IL_001f:  stloc.3
      IL_0020:  ldloc.3
      IL_0021:  ldc.i4.1
      IL_0022:  beq.s      IL_0036
      IL_0024:  br.s       IL_003a
      IL_0026:  ldloca.s   V_0
      IL_0028:  call       ""readonly int S2.Q.get""
      IL_002d:  ldc.i4.2
      IL_002e:  beq.s      IL_0032
      IL_0030:  br.s       IL_0018
      IL_0032:  ldc.i4.1
      IL_0033:  stloc.1
      IL_0034:  br.s       IL_003e
      IL_0036:  ldc.i4.2
      IL_0037:  stloc.1
      IL_0038:  br.s       IL_003e
      IL_003a:  ldc.i4.3
      IL_003b:  stloc.1
      IL_003c:  br.s       IL_003e
      IL_003e:  ldc.i4.1
      IL_003f:  brtrue.s   IL_0042
      IL_0041:  nop
      IL_0042:  ldloc.1
      IL_0043:  call       ""void System.Console.Write(int)""
      IL_0048:  nop
      IL_0049:  ret
    }
");
        }

        [Fact]
        public void PatternLocalMutatedInWhenClause_01()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        Console.Write(M1());
        Console.Write(M2());
        Console.Write(M3());
        Console.Write(M4());
    }
    static int M1()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke(() => { x.N = 3; }):
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M2()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke(new Action(() => { x.N = 3; })):
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M3()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke((Action)(() => { x.N = 3; })):
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M4()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke((() => { x.N = 3; }, () => { x.N = 3; })):
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static bool Invoke(Action a)
    {
        a();
        return false;
    }
    static bool Invoke((Action, Action) a2)
    {
        a2.Item1();
        return false;
    }
}
struct S
{
    public int N;
    public int Q => N;
}
";
            var expectedOutput = "2222";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void PatternLocalMutatedInLocalFunctionConvertedToDelegate_01()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        Console.Write(M1());
        Console.Write(M2());
        Console.Write(M3());
        Console.Write(M4());
    }
    static int M1()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke(Local):
                void Local()
                {
                    x.N = 3;
                }
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M2()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke(new Action(Local)):
                void Local()
                {
                    x.N = 3;
                }
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M3()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke((Action)(Local)):
                void Local()
                {
                    x.N = 3;
                }
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static int M4()
    {
        switch (new S() { N = 1 })
        {
            case { N: 1 } x when Invoke((Local, Local)):
                void Local()
                {
                    x.N = 3;
                }
                return 1;
            case { Q: 1 }:
                return 2;
            default:
                return 3;
        }
    }
    static bool Invoke(Action a)
    {
        a();
        return false;
    }
    static bool Invoke((Action, Action) a2)
    {
        a2.Item1();
        return false;
    }
}
struct S
{
    public int N;
    public int Q => N;
}
";
            var expectedOutput = "2222";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation))] // The test hook is blocked by https://github.com/dotnet/roslyn/issues/44658
        [WorkItem(44658, "https://github.com/dotnet/roslyn/issues/44658")]
        public void New9PatternsSemanticModel_01()
        {
            // Tests for the semantic model in new patterns as of C# 9.0.
            var source =
@"
using System;
class Program
{
    void M(object o)
    {
        const int N = 12;
        const char A = 'A';
        const char Z = 'Z';
        const char a = 'a';
        const char z = 'z';
        const char c0 = '0';
        const char c9 = '9';
        switch (o)
        {
            // Parenthesized patterns
            case ((N, N)): break;
            case (((long), (long))): break;
            case ((N)): break;
            // type patterns
            case (int, int): break;
            case (System.Int64, System.Int32): break;
            case int: break;
            // Conjunctive and disjunctive patterns
            case (>= A and <= Z) or (>= a and <= z): break;
            // Negated patterns
            case not (> c0 and < c9): break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var patterns = tree.GetRoot().DescendantNodes().OfType<PatternSyntax>().ToArray();
            Assert.Equal(31, patterns.Length);
            for (int i = 0; i < 31; i++)
            {
                var pattern = patterns[i];
                AssertEmpty(model.GetSymbolInfo(pattern));
                switch (i)
                {
                    case 0:
                        Assert.Equal("((N, N))", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 1:
                        Assert.Equal("(N, N)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 2:
                    case 3:
                        Assert.Equal("N", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int32 N", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).ConvertedType.ToTestDisplayString());
                        break;
                    case 4:
                        Assert.Equal("(((long), (long)))", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 5:
                        Assert.Equal("((long), (long))", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 6:
                    case 8:
                        Assert.Equal("(long)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 7:
                    case 9:
                        Assert.Equal("long", pattern.ToString());
                        Assert.Equal(SyntaxKind.TypePattern, pattern.Kind());
                        Assert.Equal("System.Int64", model.GetSymbolInfo(((TypePatternSyntax)pattern).Type).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 10:
                        Assert.Equal("(int, int)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 11:
                    case 12:
                    case 16:
                        Assert.Equal("int", pattern.ToString());
                        Assert.Equal(SyntaxKind.TypePattern, pattern.Kind());
                        Assert.Equal("System.Int32", model.GetSymbolInfo(((TypePatternSyntax)pattern).Type).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 13:
                        Assert.Equal("(System.Int64, System.Int32)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 14:
                        Assert.Equal("System.Int64", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int64", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 15:
                        Assert.Equal("System.Int32", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int32", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 17:
                        Assert.Equal("(>= A and <= Z) or (>= a and <= z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.OrPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 18:
                        Assert.Equal("(>= A and <= Z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 19:
                        Assert.Equal(">= A and <= Z", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 20:
                        Assert.Equal(">= A", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char A", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 21:
                        Assert.Equal("<= Z", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char Z", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 22:
                        Assert.Equal("(>= a and <= z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 23:
                        Assert.Equal(">= a and <= z", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 24:
                        Assert.Equal(">= a", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char a", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 25:
                        Assert.Equal("<= z", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char z", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 26:
                        Assert.Equal("not (> c0 and < c9)", pattern.ToString());
                        Assert.Equal(SyntaxKind.NotPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 27:
                        Assert.Equal("(> c0 and < c9)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 28:
                        Assert.Equal("> c0 and < c9", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 29:
                        Assert.Equal("> c0", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char c0", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 30:
                        Assert.Equal("< c9", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char c9", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                }
            }
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation))] // The test hook is blocked by https://github.com/dotnet/roslyn/issues/44658
        [WorkItem(44658, "https://github.com/dotnet/roslyn/issues/44658")]
        public void New9PatternsSemanticModel_02()
        {
            // Tests for the semantic model in new patterns as of C# 9.0.
            var source =
@"
using System;
class Program
{
    void M(object o)
    {
        const int N = 12;
        const char A = 'A';
        const char Z = 'Z';
        const char a = 'a';
        const char z = 'z';
        const char c0 = '0';
        const char c9 = '9';

        // Parenthesized patterns
        _ = o is ((N, N));
        _ = o is (((long), (long)));
        _ = o is ((N));
        // type patterns
        _ = o is (int, int);
        _ = o is (System.Int64, System.Int32);
        _ = o is int;
        // Conjunctive and disjunctive patterns
        _ = o is (>= A and <= Z) or (>= a and <= z);
        // Negated patterns
        _ = o is not (> c0 and < c9);
    }
}
";
            var compilation = CreatePatternCompilation(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var patterns = tree.GetRoot().DescendantNodes().OfType<PatternSyntax>().ToArray();
            Assert.Equal(31, patterns.Length);
            for (int i = 0; i < 31; i++)
            {
                var pattern = patterns[i];
                AssertEmpty(model.GetSymbolInfo(pattern));
                switch (i)
                {
                    case 0:
                        Assert.Equal("((N, N))", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 1:
                        Assert.Equal("(N, N)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 2:
                    case 3:
                        Assert.Equal("N", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int32 N", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).ConvertedType.ToTestDisplayString());
                        break;
                    case 4:
                        Assert.Equal("(((long), (long)))", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 5:
                        Assert.Equal("((long), (long))", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 6:
                    case 8:
                        Assert.Equal("(long)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 7:
                    case 9:
                        Assert.Equal("long", pattern.ToString());
                        Assert.Equal(SyntaxKind.TypePattern, pattern.Kind());
                        Assert.Equal("System.Int64", model.GetSymbolInfo(((TypePatternSyntax)pattern).Type).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 10:
                        Assert.Equal("((N))", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(((ConstantPatternSyntax)pattern).Expression).ConvertedType.ToTestDisplayString());
                        break;
                    case 11:
                        Assert.Equal("(int, int)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 12:
                    case 13:
                        Assert.Equal("int", pattern.ToString());
                        Assert.Equal(SyntaxKind.TypePattern, pattern.Kind());
                        Assert.Equal("System.Int32", model.GetSymbolInfo(((TypePatternSyntax)pattern).Type).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 14:
                        Assert.Equal("(System.Int64, System.Int32)", pattern.ToString());
                        Assert.Equal(SyntaxKind.RecursivePattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Runtime.CompilerServices.ITuple", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 15:
                        Assert.Equal("System.Int64", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int64", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int64", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 16:
                        Assert.Equal("System.Int32", pattern.ToString());
                        Assert.Equal(SyntaxKind.ConstantPattern, pattern.Kind());
                        Assert.Equal("System.Int32", model.GetSymbolInfo(((ConstantPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 17:
                        Assert.Equal("(>= A and <= Z) or (>= a and <= z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.OrPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 18:
                        Assert.Equal("(>= A and <= Z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 19:
                        Assert.Equal(">= A and <= Z", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 20:
                        Assert.Equal(">= A", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char A", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 21:
                        Assert.Equal("<= Z", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char Z", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 22:
                        Assert.Equal("(>= a and <= z)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 23:
                        Assert.Equal(">= a and <= z", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 24:
                        Assert.Equal(">= a", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char a", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 25:
                        Assert.Equal("<= z", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char z", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 26:
                        Assert.Equal("not (> c0 and < c9)", pattern.ToString());
                        Assert.Equal(SyntaxKind.NotPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 27:
                        Assert.Equal("(> c0 and < c9)", pattern.ToString());
                        Assert.Equal(SyntaxKind.ParenthesizedPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 28:
                        Assert.Equal("> c0 and < c9", pattern.ToString());
                        Assert.Equal(SyntaxKind.AndPattern, pattern.Kind());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 29:
                        Assert.Equal("> c0", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char c0", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Object", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                    case 30:
                        Assert.Equal("< c9", pattern.ToString());
                        Assert.Equal(SyntaxKind.RelationalPattern, pattern.Kind());
                        Assert.Equal("System.Char c9", model.GetSymbolInfo(((RelationalPatternSyntax)pattern).Expression).Symbol.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).Type.ToTestDisplayString());
                        Assert.Equal("System.Char", model.GetTypeInfo(pattern).ConvertedType.ToTestDisplayString());
                        break;
                }
            }
        }

        [Fact]
        public void Relational_12()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        Test(-1);
        Test(0);
        Test(1);
        Test(2);
        Test(3);
        Test(4);
        Test(5);
        Test(6);
        Test(7);
        Test(11);
        Test(12);
        Test(13);
        Test(19);
        Test(20);
        Test(21);
        Test(39);
        Test(40);
        Test(41);
        Test(64);
        Test(65);
        Test(66);
        Test(68);
        Test(70);
        Test(80);
    }
    static void Test(int age)
    {
        Console.WriteLine($""{age} -> {LifeStageAtAge(age)}"");
    }
    static LifeStage LifeStageAtAge(int age) => age switch
    {
        < 0 =>  LifeStage.Prenatal,
        < 2 =>  LifeStage.Infant,
        < 4 =>  LifeStage.Toddler,
        < 6 =>  LifeStage.EarlyChild,
        < 12 => LifeStage.MiddleChild,
        < 20 => LifeStage.Adolescent,
        < 40 => LifeStage.EarlyAdult,
        < 65 => LifeStage.MiddleAdult,
        _ =>    LifeStage.LateAdult,
    };
}
enum LifeStage
{
    Prenatal,
    Infant,
    Toddler,
    EarlyChild,
    MiddleChild,
    Adolescent,
    EarlyAdult,
    MiddleAdult,
    LateAdult,
}
";
            var expectedOutput =
@"-1 -> Prenatal
0 -> Infant
1 -> Infant
2 -> Toddler
3 -> Toddler
4 -> EarlyChild
5 -> EarlyChild
6 -> MiddleChild
7 -> MiddleChild
11 -> MiddleChild
12 -> Adolescent
13 -> Adolescent
19 -> Adolescent
20 -> EarlyAdult
21 -> EarlyAdult
39 -> EarlyAdult
40 -> MiddleAdult
41 -> MiddleAdult
64 -> MiddleAdult
65 -> LateAdult
66 -> LateAdult
68 -> LateAdult
70 -> LateAdult
80 -> LateAdult
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            compVerifier.VerifyIL("C.LifeStageAtAge", @"
    {
      // Code size       90 (0x5a)
      .maxstack  2
      .locals init (LifeStage V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  brtrue.s   IL_0004
      IL_0003:  nop
      IL_0004:  ldarg.0
      IL_0005:  ldc.i4.s   12
      IL_0007:  bge.s      IL_001d
      IL_0009:  ldarg.0
      IL_000a:  ldc.i4.4
      IL_000b:  bge.s      IL_0017
      IL_000d:  ldarg.0
      IL_000e:  ldc.i4.0
      IL_000f:  blt.s      IL_0030
      IL_0011:  ldarg.0
      IL_0012:  ldc.i4.2
      IL_0013:  blt.s      IL_0034
      IL_0015:  br.s       IL_0038
      IL_0017:  ldarg.0
      IL_0018:  ldc.i4.6
      IL_0019:  blt.s      IL_003c
      IL_001b:  br.s       IL_0040
      IL_001d:  ldarg.0
      IL_001e:  ldc.i4.s   40
      IL_0020:  bge.s      IL_0029
      IL_0022:  ldarg.0
      IL_0023:  ldc.i4.s   20
      IL_0025:  blt.s      IL_0044
      IL_0027:  br.s       IL_0048
      IL_0029:  ldarg.0
      IL_002a:  ldc.i4.s   65
      IL_002c:  blt.s      IL_004c
      IL_002e:  br.s       IL_0050
      IL_0030:  ldc.i4.0
      IL_0031:  stloc.0
      IL_0032:  br.s       IL_0054
      IL_0034:  ldc.i4.1
      IL_0035:  stloc.0
      IL_0036:  br.s       IL_0054
      IL_0038:  ldc.i4.2
      IL_0039:  stloc.0
      IL_003a:  br.s       IL_0054
      IL_003c:  ldc.i4.3
      IL_003d:  stloc.0
      IL_003e:  br.s       IL_0054
      IL_0040:  ldc.i4.4
      IL_0041:  stloc.0
      IL_0042:  br.s       IL_0054
      IL_0044:  ldc.i4.5
      IL_0045:  stloc.0
      IL_0046:  br.s       IL_0054
      IL_0048:  ldc.i4.6
      IL_0049:  stloc.0
      IL_004a:  br.s       IL_0054
      IL_004c:  ldc.i4.7
      IL_004d:  stloc.0
      IL_004e:  br.s       IL_0054
      IL_0050:  ldc.i4.8
      IL_0051:  stloc.0
      IL_0052:  br.s       IL_0054
      IL_0054:  ldc.i4.1
      IL_0055:  brtrue.s   IL_0058
      IL_0057:  nop
      IL_0058:  ldloc.0
      IL_0059:  ret
    }
");
        }

        [Fact]
        public void Relational_13()
        {
            var source = @"
using System;
class C
{
    public static void Main()
    {
        Test(-1);
        Test(0);
        Test(1);
        Test(2);
        Test(3);
        Test(4);
        Test(5);
        Test(6);
        Test(7);
        Test(11);
        Test(12);
        Test(13);
        Test(19);
        Test(20);
        Test(21);
        Test(39);
        Test(40);
        Test(41);
        Test(64);
        Test(65);
        Test(66);
        Test(68);
        Test(70);
        Test(80);
    }
    static void Test(int age)
    {
        Console.WriteLine($""{age} -> {LifeStageAtAge(age)}"");
    }
    static LifeStage LifeStageAtAge(int age) => age switch
    {
        >= 20 and < 40 => LifeStage.EarlyAdult,
        >= 12 and < 20 => LifeStage.Adolescent,
        >= 4 and < 6 =>  LifeStage.EarlyChild,
        >= 6 and < 12 => LifeStage.MiddleChild,
        < 0 =>  LifeStage.Prenatal,
        >= 0 and < 2 =>  LifeStage.Infant,
        >= 2 and < 4 =>  LifeStage.Toddler,
        >= 65 =>    LifeStage.LateAdult,
        >= 40 and < 65 => LifeStage.MiddleAdult,
    };
}
enum LifeStage
{
    Prenatal,
    Infant,
    Toddler,
    EarlyChild,
    MiddleChild,
    Adolescent,
    EarlyAdult,
    MiddleAdult,
    LateAdult,
}
";
            var expectedOutput =
@"-1 -> Prenatal
0 -> Infant
1 -> Infant
2 -> Toddler
3 -> Toddler
4 -> EarlyChild
5 -> EarlyChild
6 -> MiddleChild
7 -> MiddleChild
11 -> MiddleChild
12 -> Adolescent
13 -> Adolescent
19 -> Adolescent
20 -> EarlyAdult
21 -> EarlyAdult
39 -> EarlyAdult
40 -> MiddleAdult
41 -> MiddleAdult
64 -> MiddleAdult
65 -> LateAdult
66 -> LateAdult
68 -> LateAdult
70 -> LateAdult
80 -> LateAdult
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput).VerifyDiagnostics();
            compVerifier.VerifyIL("C.LifeStageAtAge", @"
    {
      // Code size       88 (0x58)
      .maxstack  2
      .locals init (LifeStage V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  brtrue.s   IL_0004
      IL_0003:  nop
      IL_0004:  ldarg.0
      IL_0005:  ldc.i4.s   20
      IL_0007:  blt.s      IL_0015
      IL_0009:  ldarg.0
      IL_000a:  ldc.i4.s   40
      IL_000c:  blt.s      IL_002e
      IL_000e:  ldarg.0
      IL_000f:  ldc.i4.s   65
      IL_0011:  bge.s      IL_004a
      IL_0013:  br.s       IL_004e
      IL_0015:  ldarg.0
      IL_0016:  ldc.i4.4
      IL_0017:  blt.s      IL_0024
      IL_0019:  ldarg.0
      IL_001a:  ldc.i4.s   12
      IL_001c:  bge.s      IL_0032
      IL_001e:  ldarg.0
      IL_001f:  ldc.i4.6
      IL_0020:  blt.s      IL_0036
      IL_0022:  br.s       IL_003a
      IL_0024:  ldarg.0
      IL_0025:  ldc.i4.0
      IL_0026:  blt.s      IL_003e
      IL_0028:  ldarg.0
      IL_0029:  ldc.i4.2
      IL_002a:  blt.s      IL_0042
      IL_002c:  br.s       IL_0046
      IL_002e:  ldc.i4.6
      IL_002f:  stloc.0
      IL_0030:  br.s       IL_0052
      IL_0032:  ldc.i4.5
      IL_0033:  stloc.0
      IL_0034:  br.s       IL_0052
      IL_0036:  ldc.i4.3
      IL_0037:  stloc.0
      IL_0038:  br.s       IL_0052
      IL_003a:  ldc.i4.4
      IL_003b:  stloc.0
      IL_003c:  br.s       IL_0052
      IL_003e:  ldc.i4.0
      IL_003f:  stloc.0
      IL_0040:  br.s       IL_0052
      IL_0042:  ldc.i4.1
      IL_0043:  stloc.0
      IL_0044:  br.s       IL_0052
      IL_0046:  ldc.i4.2
      IL_0047:  stloc.0
      IL_0048:  br.s       IL_0052
      IL_004a:  ldc.i4.8
      IL_004b:  stloc.0
      IL_004c:  br.s       IL_0052
      IL_004e:  ldc.i4.7
      IL_004f:  stloc.0
      IL_0050:  br.s       IL_0052
      IL_0052:  ldc.i4.1
      IL_0053:  brtrue.s   IL_0056
      IL_0055:  nop
      IL_0056:  ldloc.0
      IL_0057:  ret
    }
");
        }

        [Fact]
        public void RelationalFuzz_2020202_2_F()
        {
            var source = @"
class C
{
    static int M(float d)
    {
        return d switch
        {
            >= 3F and < 6F => 1,
            >= 6F and < 9F => 2,
            _ => 0,
        };
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
        }

        /// <summary>
        /// A test intended to stress the machinery in lowering used to build the balanced tree of tests.
        /// </summary>
        [Theory]
        [InlineData(1169113187, 100, 'D', "")]
        [InlineData(1415490180, 100, 'F', "")]
        [InlineData(1965461556, 100, 'M', "")]
        [InlineData(1745927739, 100, 'D', ".1")]
        [InlineData(652662048, 100, 'F', ".1")]
        [InlineData(201887198, 100, 'M', ".1")]
        [InlineData(1323313104, 100, 'L', "")]
        [InlineData(349816033, 100, 'U', "")]
        [InlineData(1179638331, 100, 'x', "")]
        [InlineData(337638347, 100, 'y', "")]
        [InlineData(834733763, 100, 'z', "")]
        public void RelationalFuzz_01(int seed, int numCases, char kind, string point)
        {
            string type = kind switch
            {
                'D' => "double",
                'F' => "float",
                'M' => "decimal",
                'L' => "long",
                'U' => "uint",
                'x' => "nint",
                'y' => "nuint",
                'z' => "int",
                _ => throw new ArgumentException("unexpected", nameof(kind)),
            };
            if (kind is 'x' || kind is 'y' || kind is 'z')
            {
                kind = ' ';
            }

            // A 
            Random random = new Random(seed);
            int nextInt = 1;
            int nextValue() => nextInt += random.Next(1, 3);
            var tests = new StringBuilder();
            var cases = new ArrayBuilder<string>();
            var expected = new StringBuilder();
            int previousValue = nextValue();
            for (int i = 1; i <= numCases; i++)
            {
                int limit = nextValue();
                if (limit == previousValue + 1)
                    cases.Add(FormattableString.Invariant($"            {previousValue}{point}{kind} => {i},"));
                else
                    cases.Add(FormattableString.Invariant($"            >= {previousValue}{point}{kind} and < {limit}{point}{kind} => {i},"));

                for (int t = previousValue; t < limit; t++)
                {
                    tests.AppendLine(FormattableString.Invariant($"        Console.WriteLine(M({t}{point}{kind}));"));
                    expected.AppendLine(FormattableString.Invariant($"{i}"));
                }

                previousValue = limit;
            }

            var sourceTemplate = @"using System;
class C
{
    static void Main()
    {
TESTS
    }
    static int M(TYPE d)
    {
        return d switch
        {
CASES
            _ => 0,
        };
    }
}
";
            var expectedOutput = expected.ToString();

            // try with cases in order
            runTest();

            // try with cases in random order
            shuffle(cases);
            runTest();

            void runTest()
            {
                var casesString = string.Join("\n", cases);
                var source = sourceTemplate.Replace("TESTS", tests.ToString()).Replace("CASES", casesString).Replace("TYPE", type);
                var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
                compilation.VerifyDiagnostics(
                    );
                var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            }

            void shuffle(ArrayBuilder<string> cases)
            {
                for (int i = 0; i < cases.Count; i++)
                {
                    int o = random.Next(i, cases.Count);
                    (cases[o], cases[i]) = (cases[i], cases[o]);
                }
            }
        }

        [Fact]
        public void ByteEnumConstantPattern()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        BoundCollectionElementInitializer initializer = new BoundCollectionElementInitializer();
        switch (initializer.Kind)
        {
            case BoundKind.CollectionElementInitializer:
                Console.WriteLine(true);
                break;
            default:
                Console.WriteLine(false);
                break;
        }
    }
    static void Main2()
    {
        BoundCollectionElementInitializer initializer = new BoundCollectionElementInitializer();
        if (initializer.Kind is BoundKind.CollectionElementInitializer)
        {
            Console.WriteLine(true);
        }
        else
        {
            Console.WriteLine(false);
        }
    }
    static void Main3()
    {
        BoundCollectionElementInitializer initializer = new BoundCollectionElementInitializer();
        if (initializer.Kind == BoundKind.CollectionElementInitializer)
        {
            Console.WriteLine(true);
        }
        else
        {
            Console.WriteLine(false);
        }
    }
}

enum BoundKind: byte
{
    CollectionElementInitializer = 0x94,
}
class BoundCollectionElementInitializer: BoundNode
{
    public BoundCollectionElementInitializer() : base(BoundKind.CollectionElementInitializer) { }
}
class BoundNode
{
    public readonly BoundKind Kind;
    public BoundNode(BoundKind kind) => this.Kind = kind;
}
";
            string expectedOutput = "True";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            var code = @"
    {
      // Code size       31 (0x1f)
      .maxstack  2
      IL_0000:  newobj     ""BoundCollectionElementInitializer..ctor()""
      IL_0005:  ldfld      ""BoundKind BoundNode.Kind""
      IL_000a:  ldc.i4     0x94
      IL_000f:  bne.un.s   IL_0018
      IL_0011:  ldc.i4.1
      IL_0012:  call       ""void System.Console.WriteLine(bool)""
      IL_0017:  ret
      IL_0018:  ldc.i4.0
      IL_0019:  call       ""void System.Console.WriteLine(bool)""
      IL_001e:  ret
    }
";
            compVerifier.VerifyIL("C.Main", code);
            compVerifier.VerifyIL("C.Main2", code);
            compVerifier.VerifyIL("C.Main3", code);
        }

        [Fact, WorkItem(38665, "https://github.com/dotnet/roslyn/issues/38665")]
        public void SpanForFallThrough()
        {
            var source = @"
class C
{
    public void M(object o)
    {
        switch (o)
        {
            case 0:
                _ = 2;
            case string s:
                _ = 3;
            case int i:
                _ = 4;
            case long l when l != 0:
                _ = 5;
        }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (8,13): error CS0163: Control cannot fall through from one case label ('case 0:') to another
                //             case 0:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 0:").WithArguments("case 0:").WithLocation(8, 13),
                // (10,13): error CS0163: Control cannot fall through from one case label ('case string s:') to another
                //             case string s:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case string s:").WithArguments("case string s:").WithLocation(10, 13),
                // (12,13): error CS0163: Control cannot fall through from one case label ('case int i:') to another
                //             case int i:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case int i:").WithArguments("case int i:").WithLocation(12, 13),
                // (14,13): error CS8070: Control cannot fall out of switch from final case label ('case long l when l != 0:')
                //             case long l when l != 0:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case long l when l != 0:").WithArguments("case long l when l != 0:").WithLocation(14, 13)
                );
        }

        [Theory]
        [InlineData("int", "int")]
        [InlineData("uint", "uint")]
        [InlineData("long", "long")]
        [InlineData("ulong", "ulong")]
        [InlineData("ulong", "uint")]
        [InlineData("long", "int")]
        [InlineData("nint", "int")]
        [InlineData("nuint", "uint")]
        public void SwitchingAtTheBorder(string type, string constantType)
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        M1();
        M2();
        M3();
        M4();
        Console.WriteLine(""Done"");
    }
    static unsafe void M1()
    {
        var min = (TYPE)KTYPE.MinValue;
        var max = (TYPE)KTYPE.MaxValue;
        bool wrap = sizeof(TYPE) == sizeof(KTYPE);
        Assert.Equal(1, L(min));
        Assert.Equal(wrap ? 2 : 3, L(min - 1));
        Assert.Equal(3, L(min + 1));
        Assert.Equal(2, L(max));
        Assert.Equal(wrap ? 1 : 3, L(max + 1));
        Assert.Equal(3, L(max - 1));
        static int L(TYPE t)
        {
            switch (t)
            {
                case (TYPE)KTYPE.MinValue:
                    return 1;
                case (TYPE)KTYPE.MaxValue:
                    return 2;
                default:
                    return 3;
            }
        }
    }
    static unsafe void M2()
    {
        var min = (TYPE)KTYPE.MinValue;
        var max = (TYPE)KTYPE.MaxValue;
        bool wrap = sizeof(TYPE) == sizeof(KTYPE);
        Assert.Equal(1, L(min));
        Assert.Equal(3, L(min - 1));
        Assert.Equal(3, L(min + 1));
        Assert.Equal(3, L(max));
        Assert.Equal(wrap ? 1 : 3, L(max + 1));
        Assert.Equal(3, L(max - 1));
        static int L(TYPE t)
        {
            switch (t)
            {
                case (TYPE)KTYPE.MinValue:
                    return 1;
                default:
                    return 3;
            }
        }
    }
    static unsafe void M3()
    {
        var min = (TYPE)KTYPE.MinValue;
        var max = (TYPE)KTYPE.MaxValue;
        bool wrap = sizeof(TYPE) == sizeof(KTYPE);
        Assert.Equal(3, L(min));
        Assert.Equal(wrap ? 2 : 3, L(min - 1));
        Assert.Equal(3, L(min + 1));
        Assert.Equal(2, L(max));
        Assert.Equal(3, L(max + 1));
        Assert.Equal(3, L(max - 1));
        static int L(TYPE t)
        {
            switch (t)
            {
                case (TYPE)KTYPE.MaxValue:
                    return 2;
                default:
                    return 3;
            }
        }
    }
    static unsafe void M4()
    {
        var min = (TYPE)KTYPE.MinValue;
        var max = (TYPE)KTYPE.MaxValue;
        bool wrap = sizeof(TYPE) == sizeof(KTYPE);
        Assert.Equal(1, L(min));
        Assert.Equal(wrap ? 10 : 11, L(min - 1));
        Assert.Equal(2, L(min + 1));
        Assert.Equal(10, L(max));
        Assert.Equal(wrap ? 1 : 11, L(max + 1));
        Assert.Equal(9, L(max - 1));
        static int L(TYPE t)
        {
            switch (t)
            {
                case (TYPE)KTYPE.MinValue:
                    return 1;
                case (TYPE)KTYPE.MinValue + 1:
                    return 2;
                case (TYPE)KTYPE.MinValue + 2:
                    return 3;
                case (TYPE)KTYPE.MinValue + 3:
                    return 4;
                case (TYPE)KTYPE.MinValue + 4:
                    return 5;
                case (TYPE)KTYPE.MaxValue - 4:
                    return 6;
                case (TYPE)KTYPE.MaxValue - 3:
                    return 7;
                case (TYPE)KTYPE.MaxValue - 2:
                    return 8;
                case (TYPE)KTYPE.MaxValue - 1:
                    return 9;
                case (TYPE)KTYPE.MaxValue:
                    return 10;
                default:
                    return 11;
            }
        }
    }
}
static class Assert
{
    public static void Equal<T>(T expected, T value)
    {
        if (!expected.Equals(value)) throw new System.InvalidOperationException($""{expected} != {value}"");
    }
    public static void True(bool v) => Equals(true, v);
    public static void False(bool v) => Equals(false, v);
}
";
            source = source.Replace("KTYPE", constantType).Replace("TYPE", type);
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "Done");
        }

        [InlineData("nint", "int")]
        [InlineData("nuint", "uint")]
        [Theory]
        public void SwitchingAtTheBorder_Native(string type, string constantType)
        {
            var source = @"
using System;
class C
{
    static unsafe void Main()
    {
        var min = (TYPE)KTYPE.MinValue;
        var max = (TYPE)KTYPE.MaxValue;
        bool wrap = sizeof(TYPE) == sizeof(KTYPE);
        Assert.Equal(1, L(min));
        Assert.Equal(wrap ? 10 : 11, L(min - 1));
        Assert.Equal(2, L(min + 1));
        Assert.Equal(10, L(max));
        Assert.Equal(wrap ? 1 : 11, L(max + 1));
        Assert.Equal(9, L(max - 1));
        Console.WriteLine(""Done"");
    }

    static int L(TYPE t)
    {
        switch (t)
        {
            case (TYPE)KTYPE.MinValue:
                return 1;
            case (TYPE)KTYPE.MinValue + 1:
                return 2;
            case (TYPE)KTYPE.MinValue + 2:
                return 3;
            case (TYPE)KTYPE.MinValue + 3:
                return 4;
            case (TYPE)KTYPE.MinValue + 4:
                return 5;
            case (TYPE)KTYPE.MaxValue - 4:
                return 6;
            case (TYPE)KTYPE.MaxValue - 3:
                return 7;
            case (TYPE)KTYPE.MaxValue - 2:
                return 8;
            case (TYPE)KTYPE.MaxValue - 1:
                return 9;
            case (TYPE)KTYPE.MaxValue:
                return 10;
            default:
                return 11;
        }
    }
}
static class Assert
{
    public static void Equal<T>(T expected, T value)
    {
        if (!expected.Equals(value)) throw new System.InvalidOperationException($""{expected } != {value}"");
    }
    public static void True(bool v) => Equals(true, v);
    public static void False(bool v) => Equals(false, v);
}
";
            source = source.Replace("KTYPE", constantType).Replace("TYPE", type);
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithAllowUnsafe(true), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "Done");
            compVerifier.VerifyIL("C.L", type switch
            {
                "nint" => @"
    {
      // Code size      145 (0x91)
      .maxstack  3
      .locals init (System.IntPtr V_0,
                    System.IntPtr V_1,
                    long V_2,
                    int V_3)
      IL_0000:  nop
      IL_0001:  ldarg.0
      IL_0002:  stloc.1
      IL_0003:  ldloc.1
      IL_0004:  stloc.0
      IL_0005:  ldloc.0
      IL_0006:  conv.i8
      IL_0007:  stloc.2
      IL_0008:  ldloc.2
      IL_0009:  ldc.i4     0x80000000
      IL_000e:  conv.i8
      IL_000f:  sub
      IL_0010:  dup
      IL_0011:  ldc.i4.4
      IL_0012:  conv.i8
      IL_0013:  ble.un.s   IL_0018
      IL_0015:  pop
      IL_0016:  br.s       IL_0034
      IL_0018:  conv.u4
      IL_0019:  switch    (
            IL_0060,
            IL_0064,
            IL_0068,
            IL_006c,
            IL_0070)
      IL_0032:  br.s       IL_0034
      IL_0034:  ldloc.2
      IL_0035:  ldc.i4     0x7ffffffb
      IL_003a:  conv.i8
      IL_003b:  sub
      IL_003c:  dup
      IL_003d:  ldc.i4.4
      IL_003e:  conv.i8
      IL_003f:  ble.un.s   IL_0044
      IL_0041:  pop
      IL_0042:  br.s       IL_008a
      IL_0044:  conv.u4
      IL_0045:  switch    (
            IL_0074,
            IL_0078,
            IL_007c,
            IL_0080,
            IL_0085)
      IL_005e:  br.s       IL_008a
      IL_0060:  ldc.i4.1
      IL_0061:  stloc.3
      IL_0062:  br.s       IL_008f
      IL_0064:  ldc.i4.2
      IL_0065:  stloc.3
      IL_0066:  br.s       IL_008f
      IL_0068:  ldc.i4.3
      IL_0069:  stloc.3
      IL_006a:  br.s       IL_008f
      IL_006c:  ldc.i4.4
      IL_006d:  stloc.3
      IL_006e:  br.s       IL_008f
      IL_0070:  ldc.i4.5
      IL_0071:  stloc.3
      IL_0072:  br.s       IL_008f
      IL_0074:  ldc.i4.6
      IL_0075:  stloc.3
      IL_0076:  br.s       IL_008f
      IL_0078:  ldc.i4.7
      IL_0079:  stloc.3
      IL_007a:  br.s       IL_008f
      IL_007c:  ldc.i4.8
      IL_007d:  stloc.3
      IL_007e:  br.s       IL_008f
      IL_0080:  ldc.i4.s   9
      IL_0082:  stloc.3
      IL_0083:  br.s       IL_008f
      IL_0085:  ldc.i4.s   10
      IL_0087:  stloc.3
      IL_0088:  br.s       IL_008f
      IL_008a:  ldc.i4.s   11
      IL_008c:  stloc.3
      IL_008d:  br.s       IL_008f
      IL_008f:  ldloc.3
      IL_0090:  ret
    }
",
                "nuint" => @"
    {
      // Code size      135 (0x87)
      .maxstack  3
      .locals init (System.UIntPtr V_0,
                    System.UIntPtr V_1,
                    ulong V_2,
                    int V_3)
      IL_0000:  nop
      IL_0001:  ldarg.0
      IL_0002:  stloc.1
      IL_0003:  ldloc.1
      IL_0004:  stloc.0
      IL_0005:  ldloc.0
      IL_0006:  conv.u8
      IL_0007:  stloc.2
      IL_0008:  ldloc.2
      IL_0009:  dup
      IL_000a:  ldc.i4.4
      IL_000b:  conv.i8
      IL_000c:  ble.un.s   IL_0011
      IL_000e:  pop
      IL_000f:  br.s       IL_002d
      IL_0011:  conv.u4
      IL_0012:  switch    (
            IL_0056,
            IL_005a,
            IL_005e,
            IL_0062,
            IL_0066)
      IL_002b:  br.s       IL_002d
      IL_002d:  ldloc.2
      IL_002e:  ldc.i4.s   -5
      IL_0030:  conv.u8
      IL_0031:  sub
      IL_0032:  dup
      IL_0033:  ldc.i4.4
      IL_0034:  conv.i8
      IL_0035:  ble.un.s   IL_003a
      IL_0037:  pop
      IL_0038:  br.s       IL_0080
      IL_003a:  conv.u4
      IL_003b:  switch    (
            IL_006a,
            IL_006e,
            IL_0072,
            IL_0076,
            IL_007b)
      IL_0054:  br.s       IL_0080
      IL_0056:  ldc.i4.1
      IL_0057:  stloc.3
      IL_0058:  br.s       IL_0085
      IL_005a:  ldc.i4.2
      IL_005b:  stloc.3
      IL_005c:  br.s       IL_0085
      IL_005e:  ldc.i4.3
      IL_005f:  stloc.3
      IL_0060:  br.s       IL_0085
      IL_0062:  ldc.i4.4
      IL_0063:  stloc.3
      IL_0064:  br.s       IL_0085
      IL_0066:  ldc.i4.5
      IL_0067:  stloc.3
      IL_0068:  br.s       IL_0085
      IL_006a:  ldc.i4.6
      IL_006b:  stloc.3
      IL_006c:  br.s       IL_0085
      IL_006e:  ldc.i4.7
      IL_006f:  stloc.3
      IL_0070:  br.s       IL_0085
      IL_0072:  ldc.i4.8
      IL_0073:  stloc.3
      IL_0074:  br.s       IL_0085
      IL_0076:  ldc.i4.s   9
      IL_0078:  stloc.3
      IL_0079:  br.s       IL_0085
      IL_007b:  ldc.i4.s   10
      IL_007d:  stloc.3
      IL_007e:  br.s       IL_0085
      IL_0080:  ldc.i4.s   11
      IL_0082:  stloc.3
      IL_0083:  br.s       IL_0085
      IL_0085:  ldloc.3
      IL_0086:  ret
    }
",
                _ => throw new System.InvalidOperationException(),
            });
        }

        [Fact, WorkItem(43308, "https://github.com/dotnet/roslyn/issues/43308")]
        public void RelationalEdgeTest_01()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        var x = 5;
        var str = x switch // does not handle zero
        {
            1 => ""a"",
            > 2 => ""b"",
            > 1 and <= 2 => ""c"",
            < 0 => ""d""
        };
        Console.Write(str);
        str = x switch // does not handle zero
        {
            1 => ""a"",
            > 2 => ""b"",
            <= 2 and > 1 => ""c"",
            < 0 => ""d""
        };
        Console.Write(str);
    }
}";
            string expectedOutput = "bb";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (7,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
                    //         var str = x switch // does not handle zero
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("0").WithLocation(7, 21),
                    // (15,17): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
                    //         str = x switch // does not handle zero
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("0").WithLocation(15, 17)
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_01()
        {
            var source =
@"
class C
{
    int M(Color color) => color switch
    {
        Color.Red => 0,
        Color.Blue => 2,
    };
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,33): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Color.Greed' is not covered.
                    //     int M(Color color) => color switch
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Color.Greed").WithLocation(4, 33)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_02()
        {
            var source =
@"
class C
{
    int M(Color color) => color switch
    {
        <= Color.Greed => 0,
    };
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,33): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Color.Blue' is not covered.
                    //     int M(Color color) => color switch
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Color.Blue").WithLocation(4, 33)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_03()
        {
            var source =
@"
class C
{
    int M(Color color) => color switch
    {
        <= Color.Greed => 0,
        > Color.Greed => 1,
    };
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void NonexhaustiveWithUnnamedEnumValue_01()
        {
            var source =
@"
class C
{
    int M(Color color) => color switch
    {
        Color.Red => 0,
        Color.Greed => 1,
        Color.Blue => 2,
    };
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,33): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(Color)3' is not covered.
                //     int M(Color color) => color switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch", isSuppressed: false).WithArguments("(Color)3").WithLocation(4, 33)
                );
        }

        [Fact]
        public void NonexhaustiveWithUnnamedEnumValue_02()
        {
            var source =
@"
class C
{
    int M() => this switch
    {
        (_, Color.Red) => 0,
        (_, Color.Greed) => 1,
        (_, Color.Blue) => 2,
    };
    public void Deconstruct(out int X, out Color Color) => throw null;
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,21): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(_, (Color)3)' is not covered.
                //     int M() => this switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch", isSuppressed: false).WithArguments("(_, (Color)3)").WithLocation(4, 21)
                );
        }

        [Fact]
        public void NonexhaustiveWithUnnamedEnumValue_03()
        {
            var source =
@"
class C
{
    int M() => this switch
    {
        { X: _, Color: Color.Red } => 0,
        { X: _, Color: Color.Greed } => 1,
        { X: _, Color: Color.Blue } => 2,
    };
    public int X => 1;
    public Color Color => Color.Red;
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,21): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '{ Color: (Color)3 }' is not covered.
                //     int M() => this switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch", isSuppressed: false).WithArguments("{ Color: (Color)3 }").WithLocation(4, 21)
                );
        }

        [Fact]
        public void NonexhaustiveWithUnnamedEnumValue_04()
        {
            var source =
@"
class C
{
    int M(int i, Color c) => (i, c) switch
    {
        (_, Color.Red) => 0,
        (_, Color.Greed) => 1,
        (_, Color.Blue) => 2,
    };
}
enum Color { Red, Greed, Blue }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators, targetFramework: TargetFramework.NetCoreApp);
            compilation.VerifyDiagnostics(
                // (4,37): warning CS8524: The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value. For example, the pattern '(_, (Color)3)' is not covered.
                //     int M(int i, Color c) => (i, c) switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue, "switch", isSuppressed: false).WithArguments("(_, (Color)3)").WithLocation(4, 37)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_05()
        {
            var source =
@"#nullable enable
class C
{
    int M(bool a, bool b) => (a, b) switch
    {
        (true, _) => 1,
        (_, true) => 2,
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,37): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(false, false)' is not covered.
                    //     int M(bool a, bool b) => (a, b) switch
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(false, false)").WithLocation(4, 37)
                );
        }

        [Fact]
        public void NonexhaustiveSwitchDiagnostic_06()
        {
            var source =
@"#nullable enable
class C
{
    int M(string? a, string? b) => (a, b) switch
    {
        (string, _) => 1,
        (_, string) => 2,
    };
    void M2(object? a, object? b)
    {
        _ = (a, b) switch { (null, _) => 1 };
        _ = (a, b) switch { (null, _) => 1, (_, null) => 2 };
        _ = (a, b) switch { (_, not null) => 1 };
        _ = (a, b) switch { (_, not null) => 1, (not null, _) => 2 };
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,43): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, null)' is not covered.
                //     int M(string? a, string? b) => (a, b) switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, null)").WithLocation(4, 43),
                // (11,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(not null, _)' is not covered.
                //         _ = (a, b) switch { (null, _) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(not null, _)").WithLocation(11, 20),
                // (12,20): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(not null, not null)' is not covered.
                //         _ = (a, b) switch { (null, _) => 1, (_, null) => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(not null, not null)").WithLocation(12, 20),
                // (13,20): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(_, null)' is not covered.
                //         _ = (a, b) switch { (_, not null) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(_, null)").WithLocation(13, 20),
                // (14,20): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, null)' is not covered.
                //         _ = (a, b) switch { (_, not null) => 1, (not null, _) => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("(null, null)").WithLocation(14, 20)
                );
        }

        [Fact]
        public void NonexhaustiveSwitchDiagnostic_07()
        {
            var source =
@"#nullable enable
class C
{
    int M1(nint a) => a switch { <= (nint)int.MaxValue => 1 };
    int M2(nint a) => a switch { >= (nint)int.MinValue => 1 };
    int M3(nuint a) => a switch { <= (nuint)uint.MaxValue => 1 };
    // Cannot test these cases due to https://github.com/dotnet/roslyn/issues/44651
    //int M4(Enint a) => a switch { <= (Enint)int.MaxValue => 1 };
    //int M5(Enint a) => a switch { >= (Enint)int.MinValue => 1 };
    //int M6(Enuint a) => a switch { <= (Enuint)uint.MaxValue => 1 };
}
//enum Enint : nint { }
//enum Enuint : nuint { }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,25): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '> (nint)int.MaxValue' is not covered.
                    //     int M1(nint a) => a switch { <= (nint)int.MaxValue => 1 };
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("> (nint)int.MaxValue").WithLocation(4, 25),
                    // (5,25): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '< (nint)int.MinValue' is not covered.
                    //     int M2(nint a) => a switch { >= (nint)int.MinValue => 1 };
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("< (nint)int.MinValue").WithLocation(5, 25),
                    // (6,26): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '> (nuint)uint.MaxValue' is not covered.
                    //     int M3(nuint a) => a switch { <= (nuint)uint.MaxValue => 1 };
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("> (nuint)uint.MaxValue").WithLocation(6, 26)
                );
        }

        [Fact]
        public void NonexhaustiveSwitchDiagnostic_08()
        {
            var source =
@"
class C
{
    int M1(float f) => f switch { < 0 => 1, >= 0 => 2 }; // float.NaN
    int M2(float f) => f switch { > float.NegativeInfinity => 1, float.NaN => 2 }; // float.NegativeInfinity
    int M3(float f) => f switch { < float.PositiveInfinity => 1, float.NaN => 2 }; // float.PositiveInfinity
    int M1(double f) => f switch { < 0 => 1, >= 0 => 2 }; // double.NaN
    int M2(double f) => f switch { > double.NegativeInfinity => 1, double.NaN => 2 }; // double.NegativeInfinity
    int M3(double f) => f switch { < double.PositiveInfinity => 1, double.NaN => 2 }; // double.PositiveInfinity
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,26): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'float.NaN' is not covered.
                    //     int M1(float f) => f switch { < 0 => 1, >= 0 => 2 }; // float.NaN
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("float.NaN").WithLocation(4, 26),
                    // (5,26): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'float.NegativeInfinity' is not covered.
                    //     int M2(float f) => f switch { > float.NegativeInfinity => 1, float.NaN => 2 }; // float.NegativeInfinity
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("float.NegativeInfinity").WithLocation(5, 26),
                    // (6,26): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'float.PositiveInfinity' is not covered.
                    //     int M3(float f) => f switch { < float.PositiveInfinity => 1, float.NaN => 2 }; // float.PositiveInfinity
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("float.PositiveInfinity").WithLocation(6, 26),
                    // (7,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'double.NaN' is not covered.
                    //     int M1(double f) => f switch { < 0 => 1, >= 0 => 2 }; // double.NaN
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("double.NaN").WithLocation(7, 27),
                    // (8,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'double.NegativeInfinity' is not covered.
                    //     int M2(double f) => f switch { > double.NegativeInfinity => 1, double.NaN => 2 }; // double.NegativeInfinity
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("double.NegativeInfinity").WithLocation(8, 27),
                    // (9,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'double.PositiveInfinity' is not covered.
                    //     int M3(double f) => f switch { < double.PositiveInfinity => 1, double.NaN => 2 }; // double.PositiveInfinity
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("double.PositiveInfinity").WithLocation(9, 27)
                );
        }

        [Fact]
        public void NonexhaustiveSwitchDiagnostic_09()
        {
            var source =
@"
class C
{
    int M1(string s) => s switch { """" => 1, ""1"" => 2, ""A"" => 3, };
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                    // (4,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '"B"' is not covered.
                    //     int M1(string s) => s switch { "" => 1, "1" => 2, "A" => 3, };
                    Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("\"B\"").WithLocation(4, 27)
                );
        }

        [Fact, WorkItem(44398, "https://github.com/dotnet/roslyn/issues/44398")]
        public void MismatchedExpressionPattern()
        {
            var source =
@"class C
{
    static void M(int a)
    {
        if (a is a is > 0 and < 500) { }
        if (true is < 0) { }
        if (true is 0) { }
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (5,18): error CS9133: A constant value of type 'int' is expected
                //         if (a is a is > 0 and < 500) { }
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "a").WithArguments("int").WithLocation(5, 18),
                // (5,25): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         if (a is a is > 0 and < 500) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "bool").WithLocation(5, 25),
                // (5,33): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         if (a is a is > 0 and < 500) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "500").WithArguments("int", "bool").WithLocation(5, 33),
                // (6,21): error CS8781: Relational patterns may not be used for a value of type 'bool'.
                //         if (true is < 0) { }
                Diagnostic(ErrorCode.ERR_UnsupportedTypeForRelationalPattern, "< 0").WithArguments("bool").WithLocation(6, 21),
                // (6,23): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         if (true is < 0) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "bool").WithLocation(6, 23),
                // (7,21): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //         if (true is 0) { }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "bool").WithLocation(7, 21)
            );
        }

        [Fact, WorkItem(44518, "https://github.com/dotnet/roslyn/issues/44518")]
        public void ErrorRecovery_01()
        {
            var source =
@"class C
{
    void M()
    {
        _ = is < true;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (5,13): error CS1525: Invalid expression term 'is'
                //         _ = is < true;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "is").WithArguments("is").WithLocation(5, 13)
                );
        }

        [Fact, WorkItem(44540, "https://github.com/dotnet/roslyn/issues/44540")]
        public void ErrorRecovery_02()
        {
            var source =
@"using System;
public class C {
    public void M(nint x) {
        int z = x switch{
                1=>//1,
                2=>2,
        };
        Console.WriteLine(z);
    }
    public void M(nuint x) {
        int z = x switch{
                1=>//1,
                2=>2,
        };
        Console.WriteLine(z);
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (6,18): error CS1003: Syntax error, ',' expected
                //                 2=>2,
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 18),
                // (6,18): error CS8504: Pattern missing
                //                 2=>2,
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(6, 18),
                // (13,18): error CS1003: Syntax error, ',' expected
                //                 2=>2,
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(13, 18),
                // (13,18): error CS8504: Pattern missing
                //                 2=>2,
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(13, 18)
                );
        }

        [Fact, WorkItem(44540, "https://github.com/dotnet/roslyn/issues/44540")]
        public void ErrorRecovery_03()
        {
            var source =
@"public class C {
    public void M(nint x) {
        _ = x is >= 1 and;
    }
    public void M(nuint x) {
        _ = x is >= 1 and;
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,26): error CS8504: Pattern missing
                //         _ = x is >= 1 and;
                Diagnostic(ErrorCode.ERR_MissingPattern, ";").WithLocation(3, 26),
                // (6,26): error CS8504: Pattern missing
                //         _ = x is >= 1 and;
                Diagnostic(ErrorCode.ERR_MissingPattern, ";").WithLocation(6, 26)
                );
        }

        [Fact, WorkItem(44540, "https://github.com/dotnet/roslyn/issues/44540")]
        public void ErrorRecovery_04()
        {
            var source =
@"public class C {
    public void M() {
        _ = 0f is >= 0/0;
        _ = 0d is >= 0/0;
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,22): error CS0020: Division by constant zero
                //         _ = 0f is >= 0/0;
                Diagnostic(ErrorCode.ERR_IntDivByZero, "0/0").WithLocation(3, 22),
                // (4,22): error CS0020: Division by constant zero
                //         _ = 0d is >= 0/0;
                Diagnostic(ErrorCode.ERR_IntDivByZero, "0/0").WithLocation(4, 22)
                );
        }

        [Fact]
        public void IsNot_01()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        object o = ""s"";
        if (o is not string s) return;
        Console.WriteLine(s);
    }
}";
            string expectedOutput = "s";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void IsNot_02()
        {
            var source =
@"using System;
class C
{
    static void Main()
    {
        object o = ""s"";
        if (o is (not (string s))) return;
        Console.WriteLine(s);
    }
}";
            string expectedOutput = "s";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                );
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void IsNot_03()
        {
            var source =
@"class C
{
    static void Main()
    {
        object o = ""s"";
        {
            if (o is string s)
                _ = s;
            else
                _ = s; // 1
        }
        {
            if (o is not string s)
                _ = s; // 2
            else
                _ = s;
        }
        {
            if (o is not not string s)
                _ = s;
            else
                _ = s; // 3
        }
        {
            if (o is not not not string s)
                _ = s; // 4
            else
                _ = s;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (10,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(10, 21),
                // (14,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 2
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(14, 21),
                // (22,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(22, 21),
                // (26,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 4
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(26, 21)
                );
        }

        [Fact]
        public void IsNot_04()
        {
            var source =
@"class C
{
    static void Main()
    {
        object o = ""s"";
        {
            if (o is (string s))
                _ = s;
            else
                _ = s; // 1
        }
        {
            if (o is (not (string s)))
                _ = s; // 2
            else
                _ = s;
        }
        {
            if (o is (not (not (string s))))
                _ = s;
            else
                _ = s; // 3
        }
        {
            if (o is (not (not (not (string s)))))
                _ = s; // 4
            else
                _ = s;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (10,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(10, 21),
                // (14,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 2
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(14, 21),
                // (22,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(22, 21),
                // (26,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 4
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(26, 21)
                );
        }

        [Fact]
        public void IsNot_05()
        {
            var source =
@"class C
{
    static void Main()
    {
        (object, object) o = (1, 2);
        {
            if (o is (1, string s))
                _ = s;
            else
                _ = s; // 1
        }
        {
            if (o is (not (1, string s)))
                _ = s; // 2
            else
                _ = s;
        }
        {
            if (o is (not (not (1, string s))))
                _ = s;
            else
                _ = s; // 3
        }
        {
            if (o is (not (not (not (1, string s)))))
                _ = s; // 4
            else
                _ = s;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (10,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(10, 21),
                // (14,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 2
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(14, 21),
                // (22,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 3
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(22, 21),
                // (26,21): error CS0165: Use of unassigned local variable 's'
                //                 _ = s; // 4
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s").WithLocation(26, 21)
                );
        }

        [Theory]
        [InlineData("!(o is C c1)")]
        [InlineData("o is not C c1")]
        public void IsNot_06(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(object o)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
    }}
    int F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput: "42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (C V_0) //c1
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""C""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brtrue.s   IL_000b
  IL_000a:  ret
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""int C.F""
  IL_0011:  call       ""void System.Console.WriteLine(int)""
  IL_0016:  ret
}");
        }

        [Theory]
        [InlineData("!(c is C)")]
        [InlineData("!(c is { })")]
        [InlineData("c is not C")]
        [InlineData("c is not { }")]
        [InlineData("c is (not C)")]
        [InlineData("c is (not { })")]
        public void IsNot_07(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(c.F);
    }}
    int F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput: "42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0004
  IL_0003:  ret
  IL_0004:  ldarg.0
  IL_0005:  ldfld      ""int C.F""
  IL_000a:  call       ""void System.Console.WriteLine(int)""
  IL_000f:  ret
}");
        }

        [Theory]
        [InlineData("!(c is C c1)")]
        [InlineData("!(c is (C c1))")]
        [InlineData("!(c is { } c1)")]
        [InlineData("c is not C c1")]
        [InlineData("c is not (C c1)")]
        [InlineData("c is not (not not C c1)")]
        [InlineData("c is not not (not C c1)")]
        [InlineData("c is not { } c1")]
        [InlineData("!(c is not not { } c1)")]
        public void IsNot_08(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
    }}
    int F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput: "42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (C V_0) //c1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0007
  IL_0003:  ldarg.0
  IL_0004:  stloc.0
  IL_0005:  br.s       IL_0008
  IL_0007:  ret
  IL_0008:  ldloc.0
  IL_0009:  ldfld      ""int C.F""
  IL_000e:  call       ""void System.Console.WriteLine(int)""
  IL_0013:  ret
}");
        }

        [Theory]
        [InlineData("!(c is { F: 42 } c1)")]
        [InlineData("c is not { F: 42 } c1")]
        public void IsNot_09(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
    }}
    int F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput: "42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (C V_0) //c1
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0011
  IL_0003:  ldarg.0
  IL_0004:  ldfld      ""int C.F""
  IL_0009:  ldc.i4.s   42
  IL_000b:  bne.un.s   IL_0011
  IL_000d:  ldarg.0
  IL_000e:  stloc.0
  IL_000f:  br.s       IL_0012
  IL_0011:  ret
  IL_0012:  ldloc.0
  IL_0013:  ldfld      ""int C.F""
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ret
}");
        }

        [Theory]
        [InlineData("!(c is { F: var f } c1)")]
        [InlineData("c is not { F: var f } c1")]
        public void IsNot_10(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(f);
        WriteLine(c1.F);
    }}
    object F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"42
42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       33 (0x21)
  .maxstack  1
  .locals init (C V_0, //c1
                object V_1) //f
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000e
  IL_0003:  ldarg.0
  IL_0004:  ldfld      ""object C.F""
  IL_0009:  stloc.1
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000f
  IL_000e:  ret
  IL_000f:  ldloc.1
  IL_0010:  call       ""void System.Console.WriteLine(object)""
  IL_0015:  ldloc.0
  IL_0016:  ldfld      ""object C.F""
  IL_001b:  call       ""void System.Console.WriteLine(object)""
  IL_0020:  ret
}");
        }

        [Theory]
        [InlineData("!(o is (41 or 42))")]
        [InlineData("o is not (41 or 42)")]
        public void IsNot_11(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        object o = 42;
        M(o);
    }}
    static void M(object o)
    {{
        if ({pattern}) return;
        WriteLine(o);
    }}
}}";
            var verifier = CompileAndVerify(source, expectedOutput: "42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (int V_0,
                bool V_1)
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""int""
  IL_0006:  brfalse.s  IL_001a
  IL_0008:  ldarg.0
  IL_0009:  unbox.any  ""int""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.s   41
  IL_0012:  sub
  IL_0013:  ldc.i4.1
  IL_0014:  bgt.un.s   IL_001a
  IL_0016:  ldc.i4.1
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_001c
  IL_001a:  ldc.i4.0
  IL_001b:  stloc.1
  IL_001c:  ldloc.1
  IL_001d:  brtrue.s   IL_0020
  IL_001f:  ret
  IL_0020:  ldarg.0
  IL_0021:  call       ""void System.Console.WriteLine(object)""
  IL_0026:  ret
}");
        }

        [Fact]
        [WorkItem(49262, "https://github.com/dotnet/roslyn/issues/49262")]
        public void IsNot_12()
        {
            var source =
@"using static System.Console;
class C
{
    const string s = null;
    static void Main()
    {
        M1();
        M2();
    }
    static void M1()
    {
        if (!(s is string t)) return;
        WriteLine(t.Length);
    }
    static void M2()
    {
        if (s is not string t) return;
        WriteLine(t.Length);
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8519: The given expression never matches the provided pattern.
                //         if (!(s is string t)) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "s is string t").WithLocation(12, 15),
                // (17,13): warning CS8793: The given expression always matches the provided pattern.
                //         if (s is not string t) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "s is not string t").WithLocation(17, 13));
            var verifier = CompileAndVerify(source, expectedOutput: "");
            var expectedIL =
@"{
  // Code size        1 (0x1)
  .maxstack  1
  .locals init (string V_0) //t
  IL_0000:  ret
}";
            verifier.VerifyIL("C.M1", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Theory]
        [InlineData("!(o is (C c1 and C c2))")]
        [InlineData("o is not (C c1 and C c2)")]
        public void IsNot_13(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C());
    }}
    static void M(object o)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
        WriteLine(c2.F);
    }}
    object F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"42
42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       38 (0x26)
  .maxstack  1
  .locals init (C V_0, //c1
                C V_1) //c2
  IL_0000:  ldarg.0
  IL_0001:  isinst     ""C""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_000e
  IL_000a:  ldloc.0
  IL_000b:  stloc.1
  IL_000c:  br.s       IL_000f
  IL_000e:  ret
  IL_000f:  ldloc.0
  IL_0010:  ldfld      ""object C.F""
  IL_0015:  call       ""void System.Console.WriteLine(object)""
  IL_001a:  ldloc.1
  IL_001b:  ldfld      ""object C.F""
  IL_0020:  call       ""void System.Console.WriteLine(object)""
  IL_0025:  ret
}");
        }

        [Theory]
        [InlineData("!(c is { P: C c1, Q: C c2 })")]
        [InlineData("c is not { P: C c1, Q: C c2 }")]
        public void IsNot_14(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C() {{ P = new C(), Q = new C() }});
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
        WriteLine(c2.F);
    }}
    object P {{ get; set; }}
    object Q {{ get; set; }}
    object F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"42
42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       57 (0x39)
  .maxstack  1
  .locals init (C V_0, //c1
                C V_1) //c2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0021
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""object C.P.get""
  IL_0009:  isinst     ""C""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  brfalse.s  IL_0021
  IL_0012:  ldarg.0
  IL_0013:  callvirt   ""object C.Q.get""
  IL_0018:  isinst     ""C""
  IL_001d:  stloc.1
  IL_001e:  ldloc.1
  IL_001f:  brtrue.s   IL_0022
  IL_0021:  ret
  IL_0022:  ldloc.0
  IL_0023:  ldfld      ""object C.F""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldloc.1
  IL_002e:  ldfld      ""object C.F""
  IL_0033:  call       ""void System.Console.WriteLine(object)""
  IL_0038:  ret
}");
        }

        [Theory]
        [InlineData("!(c is { P: C c1, P: C c2 })")]
        [InlineData("c is not { P: C c1, P: C c2 }")]
        public void IsNot_15(string pattern)
        {
            var source =
$@"using static System.Console;
class C
{{
    static void Main()
    {{
        M(new C() {{ P = new C() }});
    }}
    static void M(C c)
    {{
        if ({pattern}) return;
        WriteLine(c1.F);
        WriteLine(c2.F);
    }}
    object P {{ get; set; }}
    object F = 42;
}}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"42
42");
            verifier.VerifyIL("C.M",
@"{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (C V_0, //c1
                C V_1) //c2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_0016
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""object C.P.get""
  IL_0009:  isinst     ""C""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  brfalse.s  IL_0016
  IL_0012:  ldloc.0
  IL_0013:  stloc.1
  IL_0014:  br.s       IL_0017
  IL_0016:  ret
  IL_0017:  ldloc.0
  IL_0018:  ldfld      ""object C.F""
  IL_001d:  call       ""void System.Console.WriteLine(object)""
  IL_0022:  ldloc.1
  IL_0023:  ldfld      ""object C.F""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ret
}");
        }

        [Fact]
        [WorkItem(49262, "https://github.com/dotnet/roslyn/issues/49262")]
        public void IsNot_16()
        {
            var source =
@"#nullable enable
class C
{
    static void M1(C? c)
    {
        if (!(c is C)) _ = c.ToString(); // 1
        else _ = c.ToString();
    }
    static void M2(C? c)
    {
        if (c is not C) _ = c.ToString(); // 2
        else _ = c.ToString();
    }
    static void M3(C? c)
    {
        if (c is not not C) _ = c.ToString();
        else _ = c.ToString(); // 3
    }
    static void M4(C? c)
    {
        if (c is not not not C) _ = c.ToString(); // 4
        else _ = c.ToString();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,28): warning CS8602: Dereference of a possibly null reference.
                //         if (!(c is C)) _ = c.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(6, 28),
                // (11,29): warning CS8602: Dereference of a possibly null reference.
                //         if (c is not C) _ = c.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(11, 29),
                // (17,18): warning CS8602: Dereference of a possibly null reference.
                //         else _ = c.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(17, 18),
                // (21,37): warning CS8602: Dereference of a possibly null reference.
                //         if (c is not not not C) _ = c.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "c").WithLocation(21, 37));
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_10()
        {
            var source =
@"#nullable enable
class C
{
    int M1(string? s) => s switch { string => 1 };
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,28): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //     int M1(string? s) => s switch { string => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(4, 28)
                );
        }

        [Theory]
        [InlineData("  2 and int t")]
        [InlineData("<=2 and int t")]
        [InlineData(">=2 and int t")]
        [InlineData(" <3 and int t")]
        [InlineData(" >1 and int t")]
        public void IsNot_17(string pattern)
        {
            var source = $$"""
using static System.Console;
class C
{
    const int i = 2;
    static void Main()
    {
        M1();
        M2();
    }
    static void M1()
    {
        if (!(i is {{pattern}})) return;
        Write(t.ToString());
    }
    static void M2()
    {
        if (i is not ({{pattern}})) return;
        Write(t.ToString());
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8793: The given expression always matches the provided pattern.
                //         if (!(i is 2 and int t)) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, $"i is {pattern}").WithLocation(12, 15),
                // (17,13): warning CS8519: The given expression never matches the provided pattern.
                //         if (i is not (2 and int t)) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, $"i is not ({pattern})").WithLocation(17, 13)
                );
            var verifier = CompileAndVerify(source, expectedOutput: "22");
            var expectedIL =
"""
{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (int V_0) //t
  IL_0000:  ldc.i4.2
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  call       "string int.ToString()"
  IL_0009:  call       "void System.Console.Write(string)"
  IL_000e:  ret
 }
""";
            verifier.VerifyIL("C.M1", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Theory]
        [InlineData("  3 and int t")]
        [InlineData("<=1 and int t")]
        [InlineData(">=3 and int t")]
        [InlineData(" <2 and int t")]
        [InlineData(" >2 and int t")]
        public void IsNot_18(string pattern)
        {
            var source = $$"""
using static System.Console;
class C
{
    const int i = 2;
    static void Main()
    {
        M1();
        M2();
    }
    static void M1()
    {
        if (!(i is {{pattern}})) return;
        Write(t.ToString());
    }
    static void M2()
    {
        if (i is not ({{pattern}})) return;
        Write(t.ToString());
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (12,15): warning CS8793: The given expression always matches the provided pattern.
                //         if (!(i is 2 and int t)) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, $"i is {pattern}").WithLocation(12, 15),
                // (17,13): warning CS8519: The given expression never matches the provided pattern.
                //         if (i is not (2 and int t)) return;
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, $"i is not ({pattern})").WithLocation(17, 13)
            );
            var verifier = CompileAndVerify(comp, expectedOutput: "");
            var expectedIL =
"""
{
  // Code size        1 (0x1)
  .maxstack  1
  .locals init (int V_0) //t
  IL_0000:  ret
}
""";
            verifier.VerifyIL("C.M1", expectedIL);
            verifier.VerifyIL("C.M2", expectedIL);
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_11()
        {
            var source =
@"#nullable enable
class C
{
    int M1(object o) => o switch { not Q(1, 2.5) => 1 };
    int M2(object o) => o switch { not Q(1L, 2.5F) => 1 };
    int M3(object o) => o switch { not Q((byte)1, (short)2) => 1 };
    int M4(object o) => o switch { not Q((uint)1, (ulong)2) => 1 };
    int M5(object o) => o switch { not Q((long)1, (sbyte)2) => 1 };
}
class Q
{
    public void Deconstruct(out object o1, out object o2) => throw null!;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1, 2.5D)' is not covered.
                //     int M1(object o) => o switch { not Q(1, 2.5) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1, 2.5D)").WithLocation(4, 27),
                // (5,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1L, 2.5F)' is not covered.
                //     int M2(object o) => o switch { not Q(1L, 2.5F) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1L, 2.5F)").WithLocation(5, 27),
                // (6,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q((byte)1, (short)2)' is not covered.
                //     int M3(object o) => o switch { not Q((byte)1, (short)2) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q((byte)1, (short)2)").WithLocation(6, 27),
                // (7,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1U, 2UL)' is not covered.
                //     int M4(object o) => o switch { not Q((uint)1, (ulong)2) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1U, 2UL)").WithLocation(7, 27),
                // (8,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1L, (sbyte)2)' is not covered.
                //     int M5(object o) => o switch { not Q((long)1, (sbyte)2) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1L, (sbyte)2)").WithLocation(8, 27)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_12()
        {
            var source =
@"#nullable enable
class C
{
    int M1(object o) => o switch { not (Q(1, 2.5) or I) => 1 };
    int M2(object o) => o switch { not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
}
class Q
{
    public void Deconstruct(out object o1, out object o2) => throw null!;
    public void Deconstruct(out object o1, out object o2, out object o3) => throw null!;
    public int P1 = 5;
    public int P2 = 6;
}
interface I
{
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'I' is not covered.
                //     int M1(object o) => o switch { not (Q(1, 2.5) or I) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("I").WithLocation(4, 27),
                // (5,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }' is not covered.
                //     int M2(object o) => o switch { not (Q(1, 2.5) { P1: 1 } and Q(3, 4, 5) { P2: 2 }) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q(1, 2.5D) and (3, 4, 5) { P1: 1,  P2: 2 }").WithLocation(5, 27)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_13()
        {
            var source =
@"#nullable enable
class C
{
    int M1(Q? q) => q switch { not null => 1 };
    int M2(Q? q) => q switch { null => 1 };
}
class Q
{
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,23): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //     int M1(Q? q) => q switch { not null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(4, 23),
                // (5,23): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
                //     int M2(Q? q) => q switch { null => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(5, 23)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_14()
        {
            var source =
@"#nullable enable
class C
{
    int M1((int x, int y) t) => t switch { not ((1, _) { y: 2 }) => 1 };
    int M2((int x, int y) t) => t switch { not ((1, _) { Item2: 2 }) => 1 };
    int M3((int x, int y)? t) => t switch { (_, _) => 1 };
    int M4((int x, int y)? t) => t switch { not (_, _) => 1 };
}
class Q
{
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,35): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(1, 2)' is not covered.
                //     int M1((int x, int y) t) => t switch { not ((1, _) { y: 2 }) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(1, 2)").WithLocation(4, 35),
                // (5,35): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(1, 2)' is not covered.
                //     int M2((int x, int y) t) => t switch { not ((1, _) { Item2: 2 }) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(1, 2)").WithLocation(5, 35),
                // (6,36): warning CS8655: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern 'null' is not covered.
                //     int M3((int x, int y)? t) => t switch { (_, _) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull, "switch").WithArguments("null").WithLocation(6, 36),
                // (7,36): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'not null' is not covered.
                //     int M4((int x, int y)? t) => t switch { not (_, _) => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("not null").WithLocation(7, 36)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_15()
        {
            var source =
@"#nullable enable
class C
{
    int M1(Q q) => q switch { { P1: < 10 } => 1 };
    int M2(Q q) => q switch { { P1: > -100, P2: < int.MaxValue } => 1 };
}
class Q
{
    public int P1 = 5;
    public int P2 = 6;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ P1: 10 }' is not covered.
                //     int M1(Q q) => q switch { { P1: < 10 } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ P1: 10 }").WithLocation(4, 22),
                // (5,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ P1: -100 }' is not covered.
                //     int M2(Q q) => q switch { { P1: > -100, P2: < int.MaxValue } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ P1: -100 }").WithLocation(5, 22)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_16()
        {
            var source =
@"#nullable enable
class C
{
    int M1(Q q) => q switch { { P1: < 10 } => 1, null => 2 };
    int M2(Q q) => q switch { { P1: > -100, P2: < int.MaxValue } => 1, null => 2 };
}
class Q
{
    public int P1 = 5;
    public int P2 = 6;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ P1: 10 }' is not covered.
                //     int M1(Q q) => q switch { { P1: < 10 } => 1, null => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ P1: 10 }").WithLocation(4, 22),
                // (5,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ P1: -100 }' is not covered.
                //     int M2(Q q) => q switch { { P1: > -100, P2: < int.MaxValue } => 1, null => 2 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ P1: -100 }").WithLocation(5, 22)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_17()
        {
            var source =
@"#nullable enable
class C
{
    int M1(object o) => o switch { not Q => 1 };
    int M2(object o) => o switch { not Q and not W => 1 };
    int M3(object o) => o switch { not W and not Q => 1 };
}
class Q { }
class W { }
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q' is not covered.
                //     int M1(object o) => o switch { not Q => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q").WithLocation(4, 27),
                // (5,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'Q' is not covered.
                //     int M2(object o) => o switch { not Q and not W => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("Q").WithLocation(5, 27),
                // (6,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern 'W' is not covered.
                //     int M3(object o) => o switch { not W and not Q => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("W").WithLocation(6, 27)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_18()
        {
            var source =
@"#nullable enable
class C
{
    int M1(Q q) => q switch { not (1) { } => 1 };
    int M2(System.ValueTuple<int> q) => q switch { not (1) { } => 1 };
}
class Q 
{
    public void Deconstruct(out int X) => throw null!;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(1) { }' is not covered.
                //     int M1(Q q) => q switch { not (1) { } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(1) { }").WithLocation(4, 22),
                // (5,43): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(1) { }' is not covered.
                //     int M2(System.ValueTuple<int> q) => q switch { not (1) { } => 1 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(1) { }").WithLocation(5, 43)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_19()
        {
            var source =
@"#nullable enable
class C
{
    int M((bool, bool, bool, bool) t) => t switch
    {
        (false, true, true, true) => 3,
        // (false, true, false, true) => 4,
        (false, false, false, true) => 5,
        (false, false, true, true) => 6,
        (true, _, false, _) => 1,
        (true, _, true, _) => 2,
        (false, _, _, false) => 7,
    };
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,44): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(false, true, false, true)' is not covered.
                //     int M((bool, bool, bool, bool) t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(false, true, false, true)").WithLocation(4, 44)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_20()
        {
            // The is an example of a situation which is too complicated for our implementation to produce a precise
            // example pattern that is not covered. We fall back to suggesting the pattern `_` in that case.
            // For situations such as this, that is probably better than producing a precise pattern.
            var source =
@"#nullable enable
class C
{
    int M(string s) => s switch
    {
        """" => 0,
        ""A"" => 1,
        ""BB"" => 2,
        { Length: 1 } => 3,
    };
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,26): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //     int M(string s) => s switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(4, 26)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_21()
        {
            var source =
@"#nullable enable
class C
{
    int M(Q t) => t switch
    {
        { A: false, B: true, C: true, D: true } => 3,
        // { A: false, B: true, C: false, D: true } => 4,
        { A: false, B: false, C: false, D: true } => 5,
        { A: false, B: false, C: true, D: true } => 6,
        { A: true, C: false } => 1,
        { A: true, C: true } => 2,
        { A: false, D: false } => 7,
    };
}
class Q
{
    public bool A = true;
    public bool B = true;
    public bool C = true;
    public bool D = true;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ A: false,  B: true,  C: false,  D: true }' is not covered.
                //     int M(Q t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ A: false,  B: true,  C: false,  D: true }").WithLocation(4, 21)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_22()
        {
            var source =
@"#nullable enable
class C
{
    int M(Q t) => t switch
    {
        { A: true, C: true } => 2,
        { A: false, D: false } => 7,
        { A: false, B: true, C: true, D: true } => 3,
        // { A: false, B: true, C: false, D: true } => 4,
        { A: false, B: false, C: false, D: true } => 5,
        { A: false, B: false, C: true, D: true } => 6,
        { A: true, C: false } => 1,
    };
}
class Q
{
    public bool A = true;
    public bool B = true;
    public bool C = true;
    public bool D = true;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ A: false,  D: true,  B: true,  C: false }' is not covered.
                //     int M(Q t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ A: false,  D: true,  B: true,  C: false }").WithLocation(4, 21)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_23()
        {
            var source =
@"#nullable enable
class C
{
    int M(Q t) => t switch
    {
        (0, """") => 0,
        (1, ""A"") => 1,
    };
}
class Q
{
}
static class Extensions
{
    public static void Deconstruct(this Q q, out int X, out string Y)
    {
        X = 2;
        Y = ""Y"";
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,21): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(0, "A")' is not covered.
                //     int M(Q t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments(@"(0, ""A"")").WithLocation(4, 21)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_24()
        {
            var source =
@"#nullable enable
class C
{
    int M1(Q o) => o switch { not ((1) { } and (2, 3)) => 0 };
    int M2(Q o) => o switch { not ((2, 3) and (1) { }) => 0 };
}
class Q
{
    public void Deconstruct(out object o1) => throw null!;
    public void Deconstruct(out object o1, out object o2) => throw null!;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(1) { } and (2, 3)' is not covered.
                //     int M1(Q o) => o switch { not ((1) { } and (2, 3)) => 0 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(1) { } and (2, 3)").WithLocation(4, 22),
                // (5,22): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(2, 3) and (1) { }' is not covered.
                //     int M2(Q o) => o switch { not ((2, 3) and (1) { }) => 0 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("(2, 3) and (1) { }").WithLocation(5, 22)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_25()
        {
            var source =
@$"class C
{{
    static int M(string s) => s switch
    {{
        """" => 0,
        {Enumerable.Range((int)'A', (int)('z' - 'A') + 1).Select(x => (char)x)
                .Aggregate("", (s, c) => s + $"{ObjectDisplay.FormatPrimitive(c.ToString(), ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes)} => 0, ")}
    }};
}}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,33): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '"0"' is not covered.
                //     static int M(string s) => s switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments(@"""0""").WithLocation(3, 33)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_26()
        {
            var source =
@$"class C
{{
    static int M(string s) => s switch
    {{
        """" => 0,
        {Enumerable.Range((int)'A', (int)('z' - 'A') + 1).Select(x => (char)x)
                .Aggregate("", (s, c) => s + $"{ObjectDisplay.FormatPrimitive(c.ToString(), ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes)} => 0, ")}
        {Enumerable.Range(0, 20)
                .Aggregate("", (s, i) => s + $"{ObjectDisplay.FormatPrimitive(i.ToString(), ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes)} => 0, ")}
    }};
}}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,33): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '"20"' is not covered.
                //     static int M(string s) => s switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments(@"""20""").WithLocation(3, 33)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_27()
        {
            var source =
@"class C
{
    static int M(string s) => s switch
    {
        not { Length: 1, Length: 1 } => 0
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,33): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ Length: 1 }' is not covered.
                //     static int M(string s) => s switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ Length: 1 }").WithLocation(3, 33)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_28()
        {
            // Note that "not" of an impossible pattern handles everything.
            var source =
@"class C
{
    static int M(string s) => s switch
    {
        not { Length: 1, Length: 2 } => 0
    };
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_29()
        {
            var source =
@"class C
{
    static int M((int x, int y) s) => s switch
    {
        not (1, 2) { Extra: 3 } => 0
    };
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public int Extra;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (3,41): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '{ x: 1,  y: 2,  Extra: 3 }' is not covered.
                //     static int M((int x, int y) s) => s switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("{ x: 1,  y: 2,  Extra: 3 }").WithLocation(3, 41)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_30()
        {
            var source =
@"#nullable enable
class C
{
    int M((bool, bool, bool, bool) t) => t switch
    {
        (false, true, true, true) => 3,
        (false, true, false, true) when b => 4,
        (false, false, false, true) => 5,
        (false, false, true, true) => 6,
        (true, _, false, _) => 1,
        (true, _, true, _) => 2,
        (false, _, _, false) => 7,
    };
    bool b => true;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,44): warning CS8846: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '(false, true, false, true)' is not covered. However, a pattern with a 'when' clause might successfully match this value.

                //     int M((bool, bool, bool, bool) t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen, "switch").WithArguments("(false, true, false, true)").WithLocation(4, 44)
                );
        }

        [Fact]
        public void NonexhaustiveEnumDiagnostic_31()
        {
            var source =
@"#nullable enable
class C
{
    int M((object?, bool, bool, bool) t) => t switch
    {
        (null, true, true, true) => 3,
        (null, true, false, true) when b => 4,
        (null, false, false, true) => 5,
        (null, false, true, true) => 6,
        ({ }, _, false, _) => 1,
        ({ }, _, true, _) => 2,
        (null, _, _, false) => 7,
    };
    bool b => true;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators);
            compilation.VerifyDiagnostics(
                // (4,47): warning CS8847: The switch expression does not handle some null inputs (it is not exhaustive). For example, the pattern '(null, true, false, true)' is not covered. However, a pattern with a 'when' clause might successfully match this value.
                //     int M((object?, bool, bool, bool) t) => t switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen, "switch").WithArguments("(null, true, false, true)").WithLocation(4, 47),
                // (11,18): hidden CS9271: The pattern is redundant.
                //         ({ }, _, true, _) => 2,
                Diagnostic(ErrorCode.HDN_RedundantPattern, "true").WithLocation(11, 18),
                // (12,10): hidden CS9271: The pattern is redundant.
                //         (null, _, _, false) => 7,
                Diagnostic(ErrorCode.HDN_RedundantPattern, "null").WithLocation(12, 10)
                );
        }

        [Fact]
        [WorkItem(50232, "https://github.com/dotnet/roslyn/issues/50232")]
        public void Nonexhaustive_InvalidConstant_01()
        {
            var source =
@"enum Color
{
    Red
}
static class Program
{
    static int ToArgb(this Color color)
    {
        return 0;
    }
    static int GetValue(Color color)
    {
        const int red = Color.Red.ToArgb();
        return color.ToArgb() switch
        {
            red => 1
        };
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,25): error CS0133: The expression being assigned to 'red' must be constant
                //         const int red = Color.Red.ToArgb();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "Color.Red.ToArgb()").WithArguments("red").WithLocation(13, 25),
                // (14,31): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         return color.ToArgb() switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(14, 31));
        }

        [Fact]
        [WorkItem(50232, "https://github.com/dotnet/roslyn/issues/50232")]
        public void Nonexhaustive_InvalidConstant_02()
        {
            var source =
@"enum Color
{
    Red
}
static class Program
{
    static int ToArgb(this Color color)
    {
        return 0;
    }
    static int GetValue(Color color)
    {
        const int red = Color.Red.ToArgb();
        return color.ToArgb() switch
        {
            0 => 0,
            red => 1
        };
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,25): error CS0133: The expression being assigned to 'red' must be constant
                //         const int red = Color.Red.ToArgb();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "Color.Red.ToArgb()").WithArguments("red").WithLocation(13, 25),
                // (14,31): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '1' is not covered.
                //         return color.ToArgb() switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("1").WithLocation(14, 31));
        }

        [Fact]
        [WorkItem(50232, "https://github.com/dotnet/roslyn/issues/50232")]
        public void Nonexhaustive_InvalidConstant_03()
        {
            var source =
@"enum Color
{
    Red
}
static class Program
{
    static int ToArgb(this Color color)
    {
        return 0;
    }
    static int GetValue(Color color)
    {
        const int red = Color.Red.ToArgb();
        return color.ToArgb() switch
        {
            < red => 1
        };
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (13,25): error CS0133: The expression being assigned to 'red' must be constant
                //         const int red = Color.Red.ToArgb();
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "Color.Red.ToArgb()").WithArguments("red").WithLocation(13, 25));
        }

        [WorkItem(51936, "https://github.com/dotnet/roslyn/issues/51936")]
        [Theory]
        [InlineData("sbyte")]
        [InlineData("byte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("nint")]
        [InlineData("nuint")]
        [InlineData("long")]
        [InlineData("ulong")]
        public void MismatchedConstantType_01(string type)
        {
            var sourceA =
@"class Program
{
    static void Main()
    {
        if (shape is Circle { Radius: >= 100.0 })
        {
        }
    }
}";
            var sourceB =
$@"class Circle
{{
    public {type} Radius {{ get; set; }}
}}";
            var compilation = CreateCompilation(new[] { sourceA, sourceB });
            compilation.VerifyDiagnostics(
                // (5,13): error CS0103: The name 'shape' does not exist in the current context
                //         if (shape is Circle { Radius: >= 100.0 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "shape").WithArguments("shape").WithLocation(5, 13),
                // (5,42): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         if (shape is Circle { Radius: >= 100.0 })
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "100.0").WithArguments("double", type).WithLocation(5, 42));
        }

        [WorkItem(51936, "https://github.com/dotnet/roslyn/issues/51936")]
        [Theory]
        [InlineData("float")]
        [InlineData("double")]
        [InlineData("decimal")]
        public void MismatchedConstantType_02(string type)
        {
            var sourceA =
@"class Program
{
    static void Main()
    {
        if (shape is Circle { Radius: >= 100 })
        {
        }
    }
}";
            var sourceB =
$@"class Circle
{{
    public {type} Radius {{ get; set; }}
}}";
            var compilation = CreateCompilation(new[] { sourceA, sourceB });
            compilation.VerifyDiagnostics(
                // (5,13): error CS0103: The name 'shape' does not exist in the current context
                //         if (shape is Circle { Radius: >= 100 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "shape").WithArguments("shape").WithLocation(5, 13));
        }

        [WorkItem(51936, "https://github.com/dotnet/roslyn/issues/51936")]
        [Fact]
        public void MismatchedConstantType_03()
        {
            var sourceA =
@"class Program
{
    static void Main()
    {
        if (shape is Circle { Radius: >= 100 })
        {
        }
    }
}";
            var sourceB =
@"class Circle
{
    public event System.Func<double> Radius;
}";
            var compilation = CreateCompilation(new[] { sourceA, sourceB });
            compilation.VerifyDiagnostics(
                // (3,38): warning CS0067: The event 'Circle.Radius' is never used
                //     public event System.Func<double> Radius;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Radius").WithArguments("Circle.Radius").WithLocation(3, 38),
                // (5,13): error CS0103: The name 'shape' does not exist in the current context
                //         if (shape is Circle { Radius: >= 100 })
                Diagnostic(ErrorCode.ERR_NameNotInContext, "shape").WithArguments("shape").WithLocation(5, 13));
        }

        [Fact]
        [WorkItem(56199, "https://github.com/dotnet/roslyn/issues/56199")]
        public void ArrayLength()
        {
            var source = @"using System;
class C
{
    static void Main()
    {
        Console.Write(M(new int[1]) ? 1 : 0);
        Console.Write(M(new int[2]) ? 1 : 0);
    }

    static bool M(int[] a)
    {
        return a is {Length:1};
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            var compVerifier = CompileAndVerify(compilation, expectedOutput: "10");
            compVerifier.VerifyIL("C.M",
@"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000a
  IL_0003:  ldarg.0
  IL_0004:  ldlen
  IL_0005:  conv.i4
  IL_0006:  ldc.i4.1
  IL_0007:  ceq
  IL_0009:  ret
  IL_000a:  ldc.i4.0
  IL_000b:  ret
}
");
        }
    }
}
