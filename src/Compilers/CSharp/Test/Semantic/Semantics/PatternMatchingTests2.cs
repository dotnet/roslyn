// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PatternMatchingTests2 : PatternMatchingTestBase
    {
        CSharpCompilation CreatePatternCompilation(string source)
        {
            return CreateStandardCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
        }

        [Fact]
        public void Patterns2_00()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.WriteLine(1 is int {} x ? x : -1);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"1");
        }

        [Fact]
        public void Patterns2_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(3, 4) { Length: 5 } q1 && Check(p, q1));
        Check(false, p is Point(1, 4) { Length: 5 });
        Check(false, p is Point(3, 1) { Length: 5 });
        Check(false, p is Point(3, 4) { Length: 1 });
        Check(true, p is (3, 4) { Length: 5 } q2 && Check(p, q2));
        Check(false, p is (1, 4) { Length: 5 });
        Check(false, p is (3, 1) { Length: 5 });
        Check(false, p is (3, 4) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_02()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(3, 4) { Length: 5 } q1 && Check(p, q1));
        Check(false, p is Point(1, 4) { Length: 5 });
        Check(false, p is Point(3, 1) { Length: 5 });
        Check(false, p is Point(3, 4) { Length: 1 });
        Check(true, p is (3, 4) { Length: 5 } q2 && Check(p, q2));
        Check(false, p is (1, 4) { Length: 5 });
        Check(false, p is (3, 1) { Length: 5 });
        Check(false, p is (3, 4) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public int Length => 5;
}
public static class PointExtensions
{
    public static void Deconstruct(this Point p, out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
}
";
            // We use a compilation profile that provides System.Runtime.CompilerServices.ExtensionAttribute needed for this test
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_03()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        var p = (x: 3, y: 4);
        Check(true, p is (3, 4) q1 && Check(p, q1));
        Check(false, p is (1, 4) { x: 3 });
        Check(false, p is (3, 1) { y: 4 });
        Check(false, p is (3, 4) { x: 1 });
        Check(true, p is (3, 4) { x: 3 } q2 && Check(p, q2));
        Check(false, p is (1, 4) { x: 3 });
        Check(false, p is (3, 1) { x: 3 });
        Check(false, p is (3, 4) { x: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_DiscardPattern_01()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Point p = new Point();
        Check(true, p is Point(_, _) { Length: _ } q1 && Check(p, q1));
        Check(false, p is Point(1, _) { Length: _ });
        Check(false, p is Point(_, 1) { Length: _ });
        Check(false, p is Point(_, _) { Length: 1 });
        Check(true, p is (_, _) { Length: _ } q2 && Check(p, q2));
        Check(false, p is (1, _) { Length: _ });
        Check(false, p is (_, 1) { Length: _ });
        Check(false, p is (_, _) { Length: 1 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "");
        }

        [Fact]
        public void Patterns2_Switch01()
        {
            var sourceTemplate =
@"
class Program
{{
    public static void Main()
    {{
        var p = (true, false);
        switch (p)
        {{
            {0}
            {1}
            {2}
            case (_, _): // error - subsumed
                break;
        }}
    }}
}}
namespace System
{{
    public struct ValueTuple<T1, T2>
    {{
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {{
            this.Item1 = item1;
            this.Item2 = item2;
        }}
    }}
}}";
            void testErrorCase(string s1, string s2, string s3)
            {
                var source = string.Format(sourceTemplate, s1, s2, s3);
                var compilation = CreatePatternCompilation(source);
                compilation.VerifyDiagnostics(
                    // (12,13): error CS8120: The switch case has already been handled by a previous case.
                    //             case (_, _): // error - subsumed
                    Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case (_, _):").WithLocation(12, 13)
                    );
            }
            void testGoodCase(string s1, string s2)
            {
                var source = string.Format(sourceTemplate, s1, s2, string.Empty);
                var compilation = CreatePatternCompilation(source);
                compilation.VerifyDiagnostics(
                    );
            }
            var c1 = "case (true, _):";
            var c2 = "case (false, false):";
            var c3 = "case (_, true):";
            testErrorCase(c1, c2, c3);
            testErrorCase(c2, c3, c1);
            testErrorCase(c3, c1, c2);
            testErrorCase(c1, c3, c2);
            testErrorCase(c3, c2, c1);
            testErrorCase(c2, c1, c3);
            testGoodCase(c1, c2);
            testGoodCase(c1, c3);
            testGoodCase(c2, c3);
            testGoodCase(c2, c1);
            testGoodCase(c3, c1);
            testGoodCase(c3, c2);
        }

        [Fact(Skip = "PROTOTYPE(patterns2): lowering not yet implemented for recursive pattern switch")]
        public void Patterns2_Switch02()
        {
            var source =
@"
class Program
{
    public static void Main()
    {
        Point p = new Point();
        switch (p)
        {
            case Point(3, 4) { Length: 5 }:
                System.Console.WriteLine(true);
                break;
            default:
                System.Console.WriteLine(false);
                break;
        }
    }
}
public class Point
{
    public void Deconstruct(out int X, out int Y)
    {
        X = 3;
        Y = 4;
    }
    public int Length => 5;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: "True");
        }

        [Fact]
        public void DefaultPattern()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        int i = 12;
        if (i is default) {} // error 1
        if (i is (default)) {} // error 2
        switch (i) { case default: break; } // warning 3
        switch (i) { case default when true: break; } // error 4
        switch ((1, 2)) { case (1, default): break; } // error 5
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,18): error CS8405: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is default) {} // error 1
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(6, 18),
                // (7,19): error CS8405: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default)) {} // error 2
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(7, 19),
                // (8,27): warning CS8313: Did you mean to use the default switch label ('default:') rather than 'case default:'? If you really mean to use the default value, use another literal ('case 0:' or 'case null:') as appropriate.
                //         switch (i) { case default: break; } // warning 3
                Diagnostic(ErrorCode.WRN_DefaultInSwitch, "default").WithLocation(8, 27),
                // (9,27): error CS8405: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case default when true: break; } // error 4
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(9, 27),
                // (10,36): error CS8405: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch ((1, 2)) { case (1, default): break; } // error 5
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 36)
                );
        }

        [Fact]
        public void SwitchExpression_01()
        {
            // test appropriate language version or feature flag
            var source =
@"class Program
{
    public static void Main()
    {
        var r = 1 switch { _ => 0 };
    }
}";
            CreateStandardCompilation(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (5,17): error CS8058: Feature 'recursive patterns' is experimental and unsupported; use '/features:patterns2' to enable.
                //         var r = 1 switch ( _ => 0 );
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "1 switch { _ => 0 }").WithArguments("recursive patterns", "patterns2").WithLocation(5, 17)
                );
        }

        [Fact]
        public void SwitchExpression_02()
        {
            // test switch expression's governing expression has no type
            // test switch expression's governing expression has type void
            var source =
@"class Program
{
    public static void Main()
    {
        var r1 = (1, null) switch { _ => 0 };
        var r2 = System.Console.Write(1) switch { _ => 0 };
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (5,18): error CS8117: Invalid operand for pattern match; value required, but found '(int, <null>)'.
                //         var r1 = (1, null) switch ( _ => 0 );
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "(1, null)").WithArguments("(int, <null>)").WithLocation(5, 18),
                // (6,18): error CS8117: Invalid operand for pattern match; value required, but found 'void'.
                //         var r2 = System.Console.Write(1) switch ( _ => 0 );
                Diagnostic(ErrorCode.ERR_BadPatternExpression, "System.Console.Write(1)").WithArguments("void").WithLocation(6, 18)
                );
        }

        [Fact]
        public void SwitchExpression_03()
        {
            // test that a ternary expression is not at an appropriate precedence
            // for the constant expression of a constant pattern in a switch expression arm.
            var source =
@"class Program
{
    public static void Main()
    {
        bool b = true;
        var r1 = b switch { true ? true : true => true, false => false };
        var r2 = b switch { (true ? true : true) => true, false => false };
    }
}";
            // PROTOTYPE(patterns2): This is admittedly poor syntax error recovery (for the line declaring r2),
            // but this test demonstrates that it is a syntax error.
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (6,34): error CS1003: Syntax error, '=>' expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>", "?").WithLocation(6, 34),
                // (6,34): error CS1525: Invalid expression term '?'
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(6, 34),
                // (6,48): error CS1513: } expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 48),
                // (6,48): error CS1003: Syntax error, ',' expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(6, 48),
                // (6,51): error CS1002: ; expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "true").WithLocation(6, 51),
                // (6,55): error CS1002: ; expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 55),
                // (6,55): error CS1513: } expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 55),
                // (6,63): error CS1002: ; expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=>").WithLocation(6, 63),
                // (6,63): error CS1513: } expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 63),
                // (6,72): error CS1002: ; expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 72),
                // (6,73): error CS1597: Semicolon after method or accessor block is not valid
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(6, 73),
                // (9,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(9, 1),
                // (7,9): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         var r2 = b switch { (true ? true : true) => true, false => false };
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(7, 9),
                // (7,18): error CS0103: The name 'b' does not exist in the current context
                //         var r2 = b switch { (true ? true : true) => true, false => false };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(7, 18)
                );
        }

        [Fact]
        public void SwitchExpression_04()
        {
            // test that a ternary expression is permitted as a constant pattern in recursive contexts and the case expression.
            var source =
@"class Program
{
    public static void Main()
    {
        var b = (true, false);
        var r1 = b switch { (true ? true : true, _) => true, _ => false };
        var r2 = b is (true ? true : true, _);
        switch (b.Item1) { case true ? true : true: break; }
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SwitchExpression_05()
        {
            // test throw expression in match arm.
            var source =
@"class Program
{
    public static void Main()
    {
        var x = 1 switch { 1 => 1, _ => throw null };
    }
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SwitchExpression_06()
        {
            // test common type vs delegate in match expression
            var source =
@"class Program
{
    public static void Main()
    {
        var x = 1 switch { 0 => M, 1 => new D(M), 2 => M };
        x();
    }
    public static void M() {}
    public delegate void D();
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SwitchExpression_07()
        {
            // test flow analysis of the switch expression
            var source =
@"class Program
{
    public static void Main()
    {
        int q = 1;
        int u;
        var x = q switch { 0 => u=0, 1 => u=1, _ => u=2 };
        System.Console.WriteLine(u);
    }
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SwitchExpression_08()
        {
            // test flow analysis of the switch expression
            var source =
@"class Program
{
    public static void Main()
    {
        int q = 1;
        int u;
        var x = q switch { 0 => u=0, 1 => 1, _ => u=2 };
        System.Console.WriteLine(u);
    }
    static int M(int i) => i;
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (8,34): error CS0165: Use of unassigned local variable 'u'
                //         System.Console.WriteLine(u);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "u").WithArguments("u").WithLocation(8, 34)
                );
        }

        [Fact]
        public void SwitchExpression_09()
        {
            // test flow analysis of the switch expression
            var source =
@"class Program
{
    public static void Main()
    {
        int q = 1;
        int u;
        var x = q switch { 0 => u=0, 1 => u=M(u), _ => u=2 };
        System.Console.WriteLine(u);
    }
    static int M(int i) => i;
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (7,47): error CS0165: Use of unassigned local variable 'u'
                //         var x = q switch { 0 => u=0, 1 => u=M(u), _ => u=2 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "u").WithArguments("u").WithLocation(7, 47)
                );
        }

        [Fact]
        public void SwitchExpression_10()
        {
            // test lazily inferring variables in the pattern
            // test lazily inferring variables in the when clause
            // test lazily inferring variables in the arrow expression
            var source =
@"class Program
{
    public static void Main()
    {
        int a = 1;
        var b = a switch { var x1 => x1 };
        var c = a switch { var x2 when x2 is var x3 => x3 };
        var d = a switch { var x4 => x4 is var x5 ? x5 : 1 };
    }
    static int M(int i) => i;
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var names = new[] { "x1", "x2", "x3", "x4", "x5" };
            var tree = compilation.SyntaxTrees[0];
            foreach (var designation in tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                var model = compilation.GetSemanticModel(tree);
                var symbol = model.GetDeclaredSymbol(designation);
                Assert.Equal(SymbolKind.Local, symbol.Kind);
                Assert.Equal("int", ((LocalSymbol)symbol).Type.ToDisplayString());
            }
            foreach (var ident in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                var model = compilation.GetSemanticModel(tree);
                var typeInfo = model.GetTypeInfo(ident);
                Assert.Equal("int", typeInfo.Type.ToDisplayString());
            }
        }

        [Fact]
        public void ShortDiscardInIsPattern()
        {
            // test that we forbid a short discard at the top level of an is-pattern expression
            var source =
@"class Program
{
    public static void Main()
    {
        int a = 1;
        if (a is _) { }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,18): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //         if (a is _) { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(6, 18)
                );
        }

        [Fact]
        public void Patterns2_04()
        {
            // Test that a single-element deconstruct pattern is an error if no further elements disambiguate.
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        var t = new System.ValueTuple<int>(1);
        if (t is (int x)) { }                           // error 1
        switch (t) { case (_): break; }                 // error 2
        var u = t switch { (int y) => y, _ => 2 };      // error 3
        if (t is (int z1) _) { }                        // error 4
        if (t is (Item1: int z2)) { }                   // error 5
        if (t is (int z3) { }) { }                      // error 6
        if (t is ValueTuple<int>(int z4)) { }           // ok
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
    }
}
namespace System
{
    public struct ValueTuple<T>
    {
        public T Item1;

        public ValueTuple(T item1)
        {
            this.Item1 = item1;
        }
    }
}";;
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,18): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         if (t is (int x)) { }                           // error 1
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(int x)").WithLocation(8, 18),
                // (9,27): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         switch (t) { case (_): break; }                 // error 2
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(_)").WithLocation(9, 27),
                // (10,28): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         var u = t switch { (int y) => y, _ => 2 };      // error 3
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(int y)").WithLocation(10, 28),
                // (11,18): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         if (t is (int z1) _) { }                        // error 4
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(int z1) _").WithLocation(11, 18),
                // (12,18): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         if (t is (Item1: int z2)) { }                   // error 5
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(Item1: int z2)").WithLocation(12, 18),
                // (13,18): error CS8407: A single-element deconstruct pattern requires a type before the open parenthesis.
                //         if (t is (int z3) { }) { }                      // error 6
                Diagnostic(ErrorCode.ERR_SingleElementPositionalPatternRequiresType, "(int z3) { }").WithLocation(13, 18)
                );
        }

        [Fact]
        public void Patterns2_05()
        {
            // Test parsing the var pattern
            // Test binding the var pattern
            // Test lowering the var pattern for the is-expression
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        var t = (1, 2);
        { Check(true, t is var (x, y) && x == 1 && y == 2); }
        { Check(false, t is var (x, y) && x == 1 && y == 3); }
    }
    private static void Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""Expected: '{expected}', Actual: '{actual}'"");
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"");
        }

        [Fact]
        public void Patterns2_06()
        {
            // Test that 'var' does not bind to a type
            var source =
@"
using System;
namespace N
{
    class Program
    {
        public static void Main()
        {
            var t = (1, 2);
            { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
            { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
            { Check(true, t is var x); }                           // error 3
        }
        private static void Check<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual)) throw new Exception($""Expected: '{expected}', Actual: '{actual}'"");
        }
    }
    class var { }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,21): error CS0029: Cannot implicitly convert type '(int, int)' to 'N.var'
                //             var t = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 2)").WithArguments("(int, int)", "N.var").WithLocation(9, 21),
                // (10,32): error CS8408: The syntax 'var' for a pattern is not permitted to bind to a type, but it binds to 'N.var' here.
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(10, 32),
                // (10,32): error CS1061: 'var' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'var' could be found (are you missing a using directive or an assembly reference?)
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "var (x, y)").WithArguments("N.var", "Deconstruct").WithLocation(10, 32),
                // (10,32): error CS8129: No suitable Deconstruct instance or extension method was found for type 'var', with 2 out parameters and a void return type.
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "var (x, y)").WithArguments("N.var", "2").WithLocation(10, 32),
                // (11,33): error CS8408: The syntax 'var' for a pattern is not permitted to bind to a type, but it binds to 'N.var' here.
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(11, 33),
                // (11,33): error CS1061: 'var' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'var' could be found (are you missing a using directive or an assembly reference?)
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "var (x, y)").WithArguments("N.var", "Deconstruct").WithLocation(11, 33),
                // (11,33): error CS8129: No suitable Deconstruct instance or extension method was found for type 'var', with 2 out parameters and a void return type.
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "var (x, y)").WithArguments("N.var", "2").WithLocation(11, 33),
                // (12,32): error CS8408: The syntax 'var' for a pattern is not permitted to bind to a type, but it binds to 'N.var' here.
                //             { Check(true, t is var x); }                           // error 3
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(12, 32)
                );
        }

        // PROTOTYPE(patterns2): Need to have tests that exercise:
        // PROTOTYPE(patterns2): Building the decision tree for the var-pattern
        // PROTOTYPE(patterns2): Definite assignment for the var-pattern
        // PROTOTYPE(patterns2): Variable finder for the var-pattern
        // PROTOTYPE(patterns2): Scope binder contains an approprate scope for the var-pattern
        // PROTOTYPE(patterns2): Lazily binding types for variables declared in the var-pattern
        // PROTOTYPE(patterns2): Error when there is a type or constant named var in scope where the var pattern is used
    }
}
