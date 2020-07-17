// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests2 : PatternMatchingTestBase
    {
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
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,22): error CS8415: An expression of type '(int x, int y)' can never match the provided pattern.
                //         Check(false, p is (1, 4) { x: 3 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "p is (1, 4) { x: 3 }").WithArguments("(int x, int y)").WithLocation(9, 22),
                // (10,22): error CS8415: An expression of type '(int x, int y)' can never match the provided pattern.
                //         Check(false, p is (3, 1) { y: 4 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "p is (3, 1) { y: 4 }").WithArguments("(int x, int y)").WithLocation(10, 22),
                // (11,22): error CS8415: An expression of type '(int x, int y)' can never match the provided pattern.
                //         Check(false, p is (3, 4) { x: 1 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "p is (3, 4) { x: 1 }").WithArguments("(int x, int y)").WithLocation(11, 22),
                // (13,22): error CS8415: An expression of type '(int x, int y)' can never match the provided pattern.
                //         Check(false, p is (1, 4) { x: 3 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "p is (1, 4) { x: 3 }").WithArguments("(int x, int y)").WithLocation(13, 22),
                // (15,22): error CS8415: An expression of type '(int x, int y)' can never match the provided pattern.
                //         Check(false, p is (3, 4) { x: 1 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "p is (3, 4) { x: 1 }").WithArguments("(int x, int y)").WithLocation(15, 22)
                );
        }

        [Fact]
        public void Patterns2_04b()
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
        Check(true, p is (3, 4) { x: 3 } q2 && Check(p, q2));
        Check(false, p is (3, 1) { x: 3 });
    }
    private static bool Check<T>(T expected, T actual)
    {
        if (!object.Equals(expected, actual)) throw new Exception($""expected: {expected}; actual: {actual}"");
        return true;
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
}}";
            void testErrorCase(string s1, string s2, string s3)
            {
                var source = string.Format(sourceTemplate, s1, s2, s3);
                var compilation = CreatePatternCompilation(source);
                compilation.VerifyDiagnostics(
                    // (12,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    //             case (_, _): // error - subsumed
                    Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "(_, _)").WithLocation(12, 18)
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

        [Fact]
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
        switch (i) { case default: break; } // error 3
        switch (i) { case default when true: break; } // error 4
        switch ((1, 2)) { case (1, default): break; } // error 5

        if (i is < default) {} // error 6
        switch (i) { case < default: break; } // error 7
        if (i is < ((default))) {} // error 8
        switch (i) { case < ((default)): break; } // error 9

        if (i is default!) {} // error 10
        if (i is (default!)) {} // error 11
        if (i is < ((default)!)) {} // error 12
        if (i is default!!) {} // error 13
        if (i is (default!!)) {} // error 14
        if (i is < ((default)!!)) {} // error 15

        // These are not accepted by the parser. See https://github.com/dotnet/roslyn/issues/45387
        if (i is (default)!) {} // error 16
        if (i is ((default)!)) {} // error 17
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is default) {} // error 1
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(6, 18),
                // (7,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default)) {} // error 2
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(7, 19),
                // (8,27): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case default: break; } // error 3
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(8, 27),
                // (9,27): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case default when true: break; } // error 4
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(9, 27),
                // (10,36): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch ((1, 2)) { case (1, default): break; } // error 5
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(10, 36),
                // (12,20): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is < default) {} // error 6
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(12, 20),
                // (13,29): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case < default: break; } // error 7
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(13, 29),
                // (14,22): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is < ((default))) {} // error 8
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(14, 22),
                // (15,31): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         switch (i) { case < ((default)): break; } // error 9
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(15, 31),
                // (17,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is default!) {} // error 10
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(17, 18),
                // (18,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default!)) {} // error 11
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(18, 19),
                // (19,22): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is < ((default)!)) {} // error 12
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(19, 22),
                // (20,18): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is default!!) {} // error 13
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(20, 18),
                // (21,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default!!)) {} // error 14
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(21, 19),
                // (22,22): error CS8715: Duplicate null suppression operator ('!')
                //         if (i is < ((default)!!)) {} // error 15
                Diagnostic(ErrorCode.ERR_DuplicateNullSuppression, "default").WithLocation(22, 22),
                // (22,22): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is < ((default)!!)) {} // error 15
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(22, 22),
                // (25,19): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is (default)!) {} // error 16
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(25, 19),
                // (25,27): error CS1026: ) expected
                //         if (i is (default)!) {} // error 16
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "!").WithLocation(25, 27),
                // (25,28): error CS1525: Invalid expression term ')'
                //         if (i is (default)!) {} // error 16
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(25, 28),
                // (25,28): error CS1002: ; expected
                //         if (i is (default)!) {} // error 16
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(25, 28),
                // (25,28): error CS1513: } expected
                //         if (i is (default)!) {} // error 16
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(25, 28),
                // (26,18): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'int', with 2 out parameters and a void return type.
                //         if (i is ((default)!)) {} // error 17
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "((default)!)").WithArguments("int", "2").WithLocation(26, 18),
                // (26,20): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
                //         if (i is ((default)!)) {} // error 17
                Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(26, 20),
                // (26,28): error CS1003: Syntax error, ',' expected
                //         if (i is ((default)!)) {} // error 17
                Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(26, 28),
                // (26,29): error CS1525: Invalid expression term ')'
                //         if (i is ((default)!)) {} // error 17
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(26, 29)
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
        var r = 1 switch { _ => 0, };
    }
}";
            CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithoutRecursivePatterns).VerifyDiagnostics(
                // (5,17): error CS8652: The feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var r = 1 switch { _ => 0, };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1 switch { _ => 0, }").WithArguments("recursive patterns", "8.0").WithLocation(5, 17)
                );

            CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
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
            // This is admittedly poor syntax error recovery (for the line declaring r2),
            // but this test demonstrates that it is a syntax error.
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (6,34): error CS1003: Syntax error, '=>' expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments("=>", "?").WithLocation(6, 34),
                // (6,34): error CS1525: Invalid expression term '?'
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(6, 34),
                // (6,48): error CS1003: Syntax error, ',' expected
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(6, 48),
                // (6,48): error CS8504: Pattern missing
                //         var r1 = b switch { true ? true : true => true, false => false };
                Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(6, 48)
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
        var r1 = b switch { (true ? true : true, _) => true, _ => false, };
        var r2 = b is (true ? true : true, _);
        switch (b.Item1) { case true ? true : true: break; }
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
        public void EmptySwitchExpression()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var r = 1 switch { };
    }
}";
            CreatePatternCompilation(source).VerifyDiagnostics(
                // (5,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         var r = 1 switch { };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(5, 19),
                // (5,19): error CS8506: No best type was found for the switch expression.
                //         var r = 1 switch { };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(5, 19));
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
                // (5,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '3' is not covered.
                //         var x = 1 switch { 0 => M, 1 => new D(M), 2 => M };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("3").WithLocation(5, 19)
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
        var x = q switch { 0 => u=0, 1 => u=M(u), _ => u=2, };
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
        var b = a switch { var x1 => x1, };
        var c = a switch { var x2 when x2 is var x3 => x3 };
        var d = a switch { var x4 => x4 is var x5 ? x5 : 1, };
    }
    static int M(int i) => i;
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //         var c = a switch { var x2 when x2 is var x3 => x3 };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(7, 19)
                );
            var names = new[] { "x1", "x2", "x3", "x4", "x5" };
            var tree = compilation.SyntaxTrees[0];
            foreach (var designation in tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>())
            {
                var model = compilation.GetSemanticModel(tree);
                var symbol = model.GetDeclaredSymbol(designation);
                Assert.Equal(SymbolKind.Local, symbol.Kind);
                Assert.Equal("int", ((ILocalSymbol)symbol).Type.ToDisplayString());
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
        if (t is (int z1) _) { }                        // ok
        if (t is (Item1: int z2)) { }                   // ok
        if (t is (int z3) { }) { }                      // ok
        if (t is ValueTuple<int>(int z4)) { }           // ok
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(
                // (8,18): error CS8652: The feature 'parenthesized pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         if (t is (int x)) { }                           // error 1
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x)").WithArguments("parenthesized pattern").WithLocation(8, 18),
                // (8,19): error CS8121: An expression of type 'ValueTuple<int>' cannot be handled by a pattern of type 'int'.
                //         if (t is (int x)) { }                           // error 1
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("System.ValueTuple<int>", "int").WithLocation(8, 19),
                // (9,27): error CS8652: The feature 'parenthesized pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         switch (t) { case (_): break; }                 // error 2
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(_)").WithArguments("parenthesized pattern").WithLocation(9, 27),
                // (10,28): error CS8652: The feature 'parenthesized pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var u = t switch { (int y) => y, _ => 2 };      // error 3
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int y)").WithArguments("parenthesized pattern").WithLocation(10, 28),
                // (10,29): error CS8121: An expression of type 'ValueTuple<int>' cannot be handled by a pattern of type 'int'.
                //         var u = t switch { (int y) => y, _ => 2 };      // error 3
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("System.ValueTuple<int>", "int").WithLocation(10, 29)
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
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (9,21): error CS0029: Cannot implicitly convert type '(int, int)' to 'N.var'
                //             var t = (1, 2);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(1, 2)").WithArguments("(int, int)", "N.var").WithLocation(9, 21),
                // (10,32): error CS8508: The syntax 'var' for a pattern is not permitted to refer to a type, but 'N.var' is in scope here.
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(10, 32),
                // (10,36): error CS1061: 'var' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'var' could be found (are you missing a using directive or an assembly reference?)
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(x, y)").WithArguments("N.var", "Deconstruct").WithLocation(10, 36),
                // (10,36): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'var', with 2 out parameters and a void return type.
                //             { Check(true, t is var (x, y) && x == 1 && y == 2); }  // error 1
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(x, y)").WithArguments("N.var", "2").WithLocation(10, 36),
                // (11,33): error CS8508: The syntax 'var' for a pattern is not permitted to refer to a type, but 'N.var' is in scope here.
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(11, 33),
                // (11,37): error CS1061: 'var' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'var' could be found (are you missing a using directive or an assembly reference?)
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(x, y)").WithArguments("N.var", "Deconstruct").WithLocation(11, 37),
                // (11,37): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'var', with 2 out parameters and a void return type.
                //             { Check(false, t is var (x, y) && x == 1 && y == 3); } // error 2
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(x, y)").WithArguments("N.var", "2").WithLocation(11, 37),
                // (12,32): error CS8508: The syntax 'var' for a pattern is not permitted to refer to a type, but 'N.var' is in scope here.
                //             { Check(true, t is var x); }                           // error 3
                Diagnostic(ErrorCode.ERR_VarMayNotBindToType, "var").WithArguments("N.var").WithLocation(12, 32)
                );
        }

        [Fact]
        public void Patterns2_10()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M((false, false)));
        Console.Write(M((false, true)));
        Console.Write(M((true, false)));
        Console.Write(M((true, true)));
    }
    private static int M((bool, bool) t)
    {
        switch (t)
        {
            case (false, false): return 0;
            case (false, _): return 1;
            case (_, false): return 2;
            case var _: return 3;
        }
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
            var comp = CompileAndVerify(compilation, expectedOutput: @"0123");
        }

        [Fact]
        public void Patterns2_11()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M((false, false)));
        Console.Write(M((false, true)));
        Console.Write(M((true, false)));
        Console.Write(M((true, true)));
    }
    private static int M((bool, bool) t)
    {
        switch (t)
        {
            case (false, false): return 0;
            case (false, _): return 1;
            case (_, false): return 2;
            case (true, true): return 3;
            case var _: return 4;
        }
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
                // (20,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                //             case var _: return 4;
                Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "var _").WithLocation(20, 18)
                );
        }

        [Fact]
        public void Patterns2_12()
        {
            var source =
@"
using System;
class Program
{
    public static void Main()
    {
        Console.Write(M((false, false)));
        Console.Write(M((false, true)));
        Console.Write(M((true, false)));
        Console.Write(M((true, true)));
    }
    private static int M((bool, bool) t)
    {
        return t switch {
            (false, false) => 0,
            (false, _) => 1,
            (_, false) => 2,
            _ => 3
        };
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
            var comp = CompileAndVerify(compilation, expectedOutput: @"0123");
        }

        [Fact]
        public void SwitchArmSubsumed()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        string s = string.Empty;
        string s2 = s switch { null => null, string t => t, ""foo"" => null };
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,61): error CS8410: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //         string s2 = s switch { null => null, string t => t, "foo" => null };
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, @"""foo""").WithLocation(6, 61)
                );
        }

        [Fact]
        public void LongTuples()
        {
            var source =
@"using System;

public class X
{
    public static void Main()
    {
        var t = (1, 2, 3, 4, 5, 6, 7, 8, 9);
        {
            Console.WriteLine(t is (_, _, _, _, _, _, _, _, var t9) ? t9 : 100);
        }
        switch (t)
        {
            case (_, _, _, _, _, _, _, _, var t9):
                Console.WriteLine(t9);
                break;
        }
        Console.WriteLine(t switch { (_, _, _, _, _, _, _, _, var t9) => t9 });
    }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"9
9
9");
        }

        [Fact]
        public void TypeCheckInPropertyPattern()
        {
            var source =
@"using System;

class Program2
{
    public static void Main()
    {
        object o = new Frog(1, 2);
        if (o is Frog(1, 2))
        {
            Console.Write(1);
        }
        if (o is Frog { A: 1, B: 2 })
        {
            Console.Write(2);
        }
        if (o is Frog(1, 2) { A: 1, B: 2, C: 3 })
        {
            Console.Write(3);
        }

        if (o is Frog(9, 2) { A: 1, B: 2, C: 3 }) {} else
        {
            Console.Write(4);
        }
        if (o is Frog(1, 9) { A: 1, B: 2, C: 3 }) {} else
        {
            Console.Write(5);
        }
        if (o is Frog(1, 2) { A: 9, B: 2, C: 3 }) {} else
        {
            Console.Write(6);
        }
        if (o is Frog(1, 2) { A: 1, B: 9, C: 3 }) {} else
        {
            Console.Write(7);
        }
        if (o is Frog(1, 2) { A: 1, B: 2, C: 9 }) {} else
        {
            Console.Write(8);
        }
    }
}

class Frog
{
    public object A, B;
    public object C => (int)A + (int)B;
    public Frog(object A, object B) => (this.A, this.B) = (A, B);
    public void Deconstruct(out object A, out object B) => (A, B) = (this.A, this.B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"12345678");
        }

        [Fact]
        public void OvereagerSubsumption()
        {
            var source =
@"class Program2
{
    public static int Main() => 0;
    public static void M(object o)
    {
        switch (o)
        {
            case (1, 2):
                break;
            case string s:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source); // doesn't have ITuple
            // Two errors below instead of one due to https://github.com/dotnet/roslyn/issues/25533
            compilation.VerifyDiagnostics(
                // (8,18): error CS1061: 'object' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                //             case (1, 2):
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(1, 2)").WithArguments("object", "Deconstruct").WithLocation(8, 18),
                // (8,18): error CS8129: No suitable Deconstruct instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //             case (1, 2):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(1, 2)").WithArguments("object", "2").WithLocation(8, 18)
                );
        }

        [Fact]
        public void UnderscoreDeclaredAndDiscardPattern_01()
        {
            var source =
@"class Program0
{
    static int Main() => 0;
    private const int _ = 1;
    bool M1(object o) => o is _;
    bool M2(object o) => o switch { 1 => true, _ => false };
}
class Program1
{
    class _ {}
    bool M3(object o) => o is _;
    bool M4(object o) => o switch { 1 => true, _ => false };
}
";
            var expected = new[]
            {
                // (5,31): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //     bool M1(object o) => o is _;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(5, 31),
                // (11,31): warning CS8513: The name '_' refers to the type 'Program1._', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
                //     bool M3(object o) => o is _;
                Diagnostic(ErrorCode.WRN_IsTypeNamedUnderscore, "_").WithArguments("Program1._").WithLocation(11, 31)
            };

            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(expected);

            compilation = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            compilation.VerifyDiagnostics(expected);

            expected = new[]
            {
                // (5,31): error CS0246: The type or namespace name '_' could not be found (are you missing a using directive or an assembly reference?)
                //     bool M1(object o) => o is _;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "_").WithArguments("_").WithLocation(5, 31),
                // (6,26): error CS8652: The feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     bool M2(object o) => o switch { 1 => true, _ => false };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "o switch { 1 => true, _ => false }").WithArguments("recursive patterns", "8.0").WithLocation(6, 26),
                // (12,26): error CS8652: The feature 'recursive patterns' is not available in C# 7.3. Please use language version 8.0 or greater.
                //     bool M4(object o) => o switch { 1 => true, _ => false };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "o switch { 1 => true, _ => false }").WithArguments("recursive patterns", "8.0").WithLocation(12, 26)
            };

            compilation = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            compilation.VerifyDiagnostics(expected);
        }

        [Fact]
        public void UnderscoreDeclaredAndDiscardPattern_02()
        {
            var source =
@"class Program0
{
    static int Main() => 0;
    private const int _ = 1;
}
class Program1 : Program0
{
    bool M2(object o) => o switch { 1 => true, _ => false }; // ok, private member not inherited
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void UnderscoreDeclaredAndDiscardPattern_03()
        {
            var source =
@"class Program0
{
    static int Main() => 0;
    protected const int _ = 1;
}
class Program1 : Program0
{
    bool M2(object o) => o switch { 1 => true, _ => false };
}
class Program2
{
    bool _(object q) => true;
    bool M2(object o) => o switch { 1 => true, _ => false };
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void UnderscoreDeclaredAndDiscardPattern_04()
        {
            var source =
@"using _ = System.Int32;
class Program
{
    static int Main() => 0;
    bool M2(object o) => o switch { 1 => true, _ => false };
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using _ = System.Int32;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using _ = System.Int32;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void EscapingUnderscoreDeclaredAndDiscardPattern_04()
        {
            var source =
@"class Program0
{
    static int Main() => 0;
    private const int _ = 2;
    bool M1(object o) => o is @_;
    int M2(object o) => o switch { 1 => 1, @_ => 2, var _ => 3 };
}
class Program1
{
    class _ {}
    bool M1(object o) => o is @_;
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void ErroneousSwitchArmDefiniteAssignment()
        {
            // When a switch expression arm is erroneous, ensure that the expression is treated as unreachable (e.g. for definite assignment purposes).
            var source =
@"class Program2
{
    public static int Main() => 0;
    public static void M(string s)
    {
        int i;
        int j = s switch { ""frog"" => 1, 0 => i, _ => 2 };
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,41): error CS0029: Cannot implicitly convert type 'int' to 'string'
                //         int j = s switch { "frog" => 1, 0 => i, _ => 2 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "0").WithArguments("int", "string").WithLocation(7, 41)
                );
        }

        [Fact, WorkItem(9154, "https://github.com/dotnet/roslyn/issues/9154")]
        public void ErroneousIsPatternDefiniteAssignment()
        {
            var source =
@"class Program2
{
    public static int Main() => 0;
    void Dummy(object o) {}
    void Test5()
    {
        Dummy((System.Func<object, object, bool>) ((o1, o2) => o1 is int x5 && 
                                                               o2 is int x5 && 
                                                               x5 > 0));
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,74): error CS0128: A local variable or function named 'x5' is already defined in this scope
                //                                                                o2 is int x5 && 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(8, 74)
                );
        }

        [Fact]
        public void ERR_IsPatternImpossible()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        System.Console.WriteLine(""frog"" is string { Length: 4, Length: 5 });
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,34): error CS8415: An expression of type 'string' can never match the provided pattern.
                //         System.Console.WriteLine("frog" is string { Length: 4, Length: 5 });
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"""frog"" is string { Length: 4, Length: 5 }").WithArguments("string").WithLocation(5, 34)
                );
        }

        [Fact]
        public void WRN_GivenExpressionNeverMatchesPattern01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        System.Console.WriteLine(3 is 4);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (5,34): warning CS8416: The given expression never matches the provided pattern.
                //         System.Console.WriteLine(3 is 4);
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "3 is 4").WithLocation(5, 34)
                );
        }

        [Fact]
        public void WRN_GivenExpressionNeverMatchesPattern02()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        const string s = null;
        System.Console.WriteLine(s is string { Length: 3 });
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,34): warning CS8416: The given expression never matches the provided pattern.
                //         System.Console.WriteLine(s is string { Length: 3 });
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "s is string { Length: 3 }").WithLocation(6, 34)
                );
        }

        [Fact]
        public void DefiniteAssignmentForIsPattern01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        string s = 300.ToString();
        System.Console.WriteLine(s is string { Length: int j });
        System.Console.WriteLine(j);
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,34): error CS0165: Use of unassigned local variable 'j'
                //         System.Console.WriteLine(j);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j").WithLocation(7, 34)
                );
        }

        [Fact]
        public void DefiniteAssignmentForIsPattern02()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        const string s = ""300"";
        System.Console.WriteLine(s is string { Length: int j });
        System.Console.WriteLine(j);
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, parseOptions: TestOptions.RegularWithRecursivePatterns);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"True
3";
            var compVerifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void DefiniteAssignmentForIsPattern03()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        int j;
        const string s = null;
        if (s is string { Length: 3 })
        {
            System.Console.WriteLine(j);
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,13): warning CS8416: The given expression never matches the provided pattern.
                //         if (s is string { Length: 3 })
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "s is string { Length: 3 }").WithLocation(7, 13)
                );
        }

        [Fact]
        public void RefutableConstantPattern01()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        int j;
        const int N = 3;
        const int M = 3;
        if (N is M)
        {
        }
        else
        {
            System.Console.WriteLine(j);
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,13): warning CS8417: The given expression always matches the provided constant.
                //         if (N is M)
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant, "N is M").WithLocation(8, 13)
                );
        }

        [Fact, WorkItem(25591, "https://github.com/dotnet/roslyn/issues/25591")]
        public void TupleSubsumptionError()
        {
            var source =
@"class Program2
{
    public static void Main()
    {
        M(new Fox());
        M(new Cat());
        M(new Program2());
    }
    static void M(object o)
    {
        switch ((o, 0))
        {
            case (Fox fox, _):
                System.Console.Write(""Fox "");
                break;
            case (Cat cat, _):
                System.Console.Write(""Cat"");
                break;
        }
    }
}
class Fox {}
class Cat {}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"Fox Cat");
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns01()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (a: 1, b: 2)
        {
            case (c: 2, d: 3): // error: c and d not defined
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): error CS8416: The name 'c' does not identify tuple element 'Item1'.
                //             case (c: 2, d: 3): // error: c and d not defined
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "c").WithArguments("c", "Item1").WithLocation(7, 19),
                // (7,25): error CS8416: The name 'd' does not identify tuple element 'Item2'.
                //             case (c: 2, d: 3): // error: c and d not defined
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "d").WithArguments("d", "Item2").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns02()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (a: 1, b: 2)
        {
            case (a: 2, a: 3):
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,25): error CS8416: The name 'a' does not identify tuple element 'Item2'.
                //             case (a: 2, a: 3):
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "a").WithArguments("a", "Item2").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns03()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (a: 1, b: 2)
        {
            case (a: 2, Item2: 3):
                System.Console.WriteLine(666);
                break;
            case (a: 1, Item2: 2):
                System.Console.WriteLine(111);
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"111");
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns04()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (c: 2, d: 3):
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
    public void Deconstruct(out int a, out int b) => (a, b) = (A, B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): error CS8417: The name 'c' does not match the corresponding 'Deconstruct' parameter 'a'.
                //             case (c: 2, d: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "c").WithArguments("c", "a").WithLocation(7, 19),
                // (7,25): error CS8417: The name 'd' does not match the corresponding 'Deconstruct' parameter 'b'.
                //             case (c: 2, d: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "d").WithArguments("d", "b").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns05()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (c: 2, d: 3):
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
}
static class Extensions
{
    public static void Deconstruct(this T t, out int a, out int b) => (a, b) = (t.A, t.B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): error CS8417: The name 'c' does not match the corresponding 'Deconstruct' parameter 'a'.
                //             case (c: 2, d: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "c").WithArguments("c", "a").WithLocation(7, 19),
                // (7,25): error CS8417: The name 'd' does not match the corresponding 'Deconstruct' parameter 'b'.
                //             case (c: 2, d: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "d").WithArguments("d", "b").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns06()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (a: 2, a: 3):
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
    public void Deconstruct(out int a, out int b) => (a, b) = (A, B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,25): error CS8417: The name 'a' does not match the corresponding 'Deconstruct' parameter 'b'.
                //             case (a: 2, a: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "a").WithArguments("a", "b").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns07()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (a: 2, a: 3):
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
}
static class Extensions
{
    public static void Deconstruct(this T t, out int a, out int b) => (a, b) = (t.A, t.B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,25): error CS8417: The name 'a' does not match the corresponding 'Deconstruct' parameter 'b'.
                //             case (a: 2, a: 3):
                Diagnostic(ErrorCode.ERR_DeconstructParameterNameMismatch, "a").WithArguments("a", "b").WithLocation(7, 25)
                );
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns08()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (a: 2, b: 3):
                System.Console.WriteLine(666);
                break;
            case (a: 1, b: 2):
                System.Console.WriteLine(111);
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
    public void Deconstruct(out int a, out int b) => (a, b) = (A, B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"111");
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns09()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (new T(a: 1, b: 2))
        {
            case (a: 2, b: 3):
                System.Console.WriteLine(666);
                break;
            case (a: 1, b: 2):
                System.Console.WriteLine(111);
                break;
        }
    }
}
class T
{
    public int A;
    public int B;
    public T(int a, int b) => (A, B) = (a, b);
}
static class Extensions
{
    public static void Deconstruct(this T t, out int a, out int b) => (a, b) = (t.A, t.B);
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"111");
        }

        [Fact, WorkItem(25934, "https://github.com/dotnet/roslyn/issues/25934")]
        public void NamesInPositionalPatterns10()
        {
            var source =
@"class Program
{
    static void Main()
    {
        switch (a: 1, b: 2)
        {
            case (Item2: 1, 2):
                break;
        }
    }
}
";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (7,19): error CS8416: The name 'Item2' does not identify tuple element 'Item1'.
                //             case (Item2: 1, 2):
                Diagnostic(ErrorCode.ERR_TupleElementNameMismatch, "Item2").WithArguments("Item2", "Item1").WithLocation(7, 19)
                );
        }

        [Fact]
        public void PropertyPatternMemberMissing01()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        Blah b = null;
        if (b is Blah { X: int i })
        {
        }
    }
}

class Blah
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,25): error CS0117: 'Blah' does not contain a definition for 'X'
                //         if (b is Blah { X: int i })
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("Blah", "X").WithLocation(6, 25)
                );
        }

        [Fact]
        public void PropertyPatternMemberMissing02()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        Blah b = null;
        if (b is Blah { X: int i })
        {
        }
    }
}

class Blah
{
    public int X { set {} }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,25): error CS0154: The property or indexer 'Blah.X' cannot be used in this context because it lacks the get accessor
                //         if (b is Blah { X: int i })
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "X:").WithArguments("Blah.X").WithLocation(6, 25)
                );
        }

        [Fact]
        public void PropertyPatternMemberMissing03()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        Blah b = null;
        switch (b)
        {
            case Blah { X: int i }:
                break;
        }
    }
}

class Blah
{
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,25): error CS0117: 'Blah' does not contain a definition for 'X'
                //             case Blah { X: int i }:
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("Blah", "X").WithLocation(8, 25)
                );
        }

        [Fact]
        public void PropertyPatternMemberMissing04()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        Blah b = null;
        switch (b)
        {
            case Blah { X: int i }:
                break;
        }
    }
}

class Blah
{
    public int X { set {} }
}";
            var compilation = CreatePatternCompilation(source);
            compilation.VerifyDiagnostics(
                // (8,25): error CS0154: The property or indexer 'Blah.X' cannot be used in this context because it lacks the get accessor
                //             case Blah { X: int i }:
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "X:").WithArguments("Blah.X").WithLocation(8, 25)
                );
        }

        [Fact]
        [WorkItem(24550, "https://github.com/dotnet/roslyn/issues/24550")]
        [WorkItem(1284, "https://github.com/dotnet/csharplang/issues/1284")]
        public void ConstantPatternVsUnconstrainedTypeParameter03()
        {
            var source =
@"class C<T>
{
    internal struct S { }
    static bool Test(S s)
    {
        return s is null;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (6,21): error CS0037: Cannot convert null to 'C<T>.S' because it is a non-nullable value type
                //         return s is null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("C<T>.S").WithLocation(6, 21)
                );
        }

        [Fact]
        [WorkItem(24550, "https://github.com/dotnet/roslyn/issues/24550")]
        [WorkItem(1284, "https://github.com/dotnet/csharplang/issues/1284")]
        public void ConstantPatternVsUnconstrainedTypeParameter04()
        {
            var source =
@"class C<T>
{
    static bool Test(C<T> x)
    {
        return x is 1;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (5,21): error CS8121: An expression of type 'C<T>' cannot be handled by a pattern of type 'int'.
                //         return x is 1;
                Diagnostic(ErrorCode.ERR_PatternWrongType, "1").WithArguments("C<T>", "int").WithLocation(5, 21)
                );
        }

        [Fact]
        [WorkItem(20724, "https://github.com/dotnet/roslyn/issues/20724")]
        public void SpeculateWithNameConflict01()
        {
            var source =
@"public class Class1
    {
        int i = 1;

        public override int GetHashCode() => 1;
        public override bool Equals(object obj)
        {
            return obj is global::Class1 @class && this.i == @class.i;
        }
    }
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            var returnStatement = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            Assert.Equal("return obj is global::Class1 @class && this.i == @class.i;", returnStatement.ToString());
            var modifiedReturnStatement = (ReturnStatementSyntax)new RemoveAliasQualifiers().Visit(returnStatement);
            Assert.Equal("return obj is Class1 @class && this.i == @class.i;", modifiedReturnStatement.ToString());
            var gotModel = model.TryGetSpeculativeSemanticModel(returnStatement.Location.SourceSpan.Start, modifiedReturnStatement, out var speculativeModel);
            Assert.True(gotModel);
            Assert.NotNull(speculativeModel);
            var typeInfo = speculativeModel.GetTypeInfo(modifiedReturnStatement.Expression);
            Assert.Equal(SpecialType.System_Boolean, typeInfo.Type.SpecialType);
        }

        /// <summary>
        /// Helper class to remove alias qualifications.
        /// </summary>
        class RemoveAliasQualifiers : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
            {
                return node.Name;
            }
        }

        [Fact]
        [WorkItem(20724, "https://github.com/dotnet/roslyn/issues/20724")]
        public void SpeculateWithNameConflict02()
        {
            var source =
@"public class Class1
    {
        public override int GetHashCode() => 1;
        public override bool Equals(object obj)
        {
            return obj is global::Class1 @class;
        }
    }
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                );
            var tree = compilation.SyntaxTrees[0];
            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            var returnStatement = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            Assert.Equal("return obj is global::Class1 @class;", returnStatement.ToString());
            var modifiedReturnStatement = (ReturnStatementSyntax)new RemoveAliasQualifiers().Visit(returnStatement);
            Assert.Equal("return obj is Class1 @class;", modifiedReturnStatement.ToString());
            var gotModel = model.TryGetSpeculativeSemanticModel(returnStatement.Location.SourceSpan.Start, modifiedReturnStatement, out var speculativeModel);
            Assert.True(gotModel);
            Assert.NotNull(speculativeModel);
            var typeInfo = speculativeModel.GetTypeInfo(modifiedReturnStatement.Expression);
            Assert.Equal(SpecialType.System_Boolean, typeInfo.Type.SpecialType);
        }

        [Fact]
        public void WrongArity()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        Point p = new Point() { X = 3, Y = 4 };
        if (p is Point())
        {
        }
    }
}

class Point
{
    public int X, Y;
    public void Deconstruct(out int X, out int Y) => (X, Y) = (this.X, this.Y);
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (6,23): error CS7036: There is no argument given that corresponds to the required formal parameter 'X' of 'Point.Deconstruct(out int, out int)'
                //         if (p is Point())
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "()").WithArguments("X", "Point.Deconstruct(out int, out int)").WithLocation(6, 23),
                // (6,23): error CS8129: No suitable Deconstruct instance or extension method was found for type 'Point', with 0 out parameters and a void return type.
                //         if (p is Point())
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("Point", "0").WithLocation(6, 23)
                );
        }

        [Fact]
        public void GetTypeInfo_01()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        object o = null;
        Point p = null;
        if (o is Point(3, string { Length: 2 })) { }
        if (p is (_, { })) { }
        if (p is Point({ }, { }, { })) { }
        if (p is Point(, { })) { }
    }
}

class Point
{
    public object X, Y;
    public void Deconstruct(out object X, out object Y) => (X, Y) = (this.X, this.Y);
    public Point(object X, object Y) => (this.X, this.Y) = (X, Y);
}
";
            var expected = new[]
            {
                new { Source = "Point(3, string { Length: 2 })", Type = "System.Object", ConvertedType = "Point" },
                new { Source = "3", Type = "System.Object", ConvertedType = "System.Int32" },
                new { Source = "string { Length: 2 }", Type = "System.Object", ConvertedType = "System.String" },
                new { Source = "2", Type = "System.Int32", ConvertedType = "System.Int32" },
                new { Source = "(_, { })", Type = "Point", ConvertedType = "Point" },
                new { Source = "_", Type = "System.Object", ConvertedType = "System.Object" },
                new { Source = "{ }", Type = "System.Object", ConvertedType = "System.Object" },
                new { Source = "Point({ }, { }, { })", Type = "Point", ConvertedType = "Point" },
                new { Source = "{ }", Type = "?", ConvertedType = "?" },
                new { Source = "{ }", Type = "?", ConvertedType = "?" },
                new { Source = "{ }", Type = "?", ConvertedType = "?" },
                new { Source = "Point(, { })", Type = "Point", ConvertedType = "Point" },
                new { Source = "", Type = "System.Object", ConvertedType = "System.Object" },
                new { Source = "{ }", Type = "System.Object", ConvertedType = "System.Object" },
            };
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (10,24): error CS8504: Pattern missing
                //         if (p is Point(, { })) { }
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(10, 24),
                // (9,23): error CS1501: No overload for method 'Deconstruct' takes 3 arguments
                //         if (p is Point({ }, { }, { })) { }
                Diagnostic(ErrorCode.ERR_BadArgCount, "({ }, { }, { })").WithArguments("Deconstruct", "3").WithLocation(9, 23),
                // (9,23): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'Point', with 3 out parameters and a void return type.
                //         if (p is Point({ }, { }, { })) { }
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "({ }, { }, { })").WithArguments("Point", "3").WithLocation(9, 23)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            int i = 0;
            foreach (var pat in tree.GetRoot().DescendantNodesAndSelf().OfType<PatternSyntax>())
            {
                var typeInfo = model.GetTypeInfo(pat);
                var ex = expected[i++];
                Assert.Equal(ex.Source, pat.ToString());
                Assert.Equal(ex.Type, typeInfo.Type.ToTestDisplayString());
                Assert.Equal(ex.ConvertedType, typeInfo.ConvertedType.ToTestDisplayString());
            }
            Assert.Equal(expected.Length, i);
        }

        [Fact, WorkItem(26613, "https://github.com/dotnet/roslyn/issues/26613")]
        public void MissingDeconstruct_01()
        {
            var source =
@"using System;
public class C {
    public void M() {
        _ = this is (a: 1);
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (4,21): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         _ = this is (a: 1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(a: 1)").WithArguments("C", "Deconstruct").WithLocation(4, 21),
                // (4,21): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 1 out parameters and a void return type.
                //         _ = this is (a: 1);
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(a: 1)").WithArguments("C", "1").WithLocation(4, 21)
                );
        }

        [Fact, WorkItem(26613, "https://github.com/dotnet/roslyn/issues/26613")]
        public void MissingDeconstruct_02()
        {
            var source =
@"using System;
public class C {
    public void M() {
        _ = this is C(a: 1);
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (4,22): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         _ = this is C(a: 1);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(a: 1)").WithArguments("C", "Deconstruct").WithLocation(4, 22),
                // (4,22): error CS8129: No suitable Deconstruct instance or extension method was found for type 'C', with 1 out parameters and a void return type.
                //         _ = this is C(a: 1);
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(a: 1)").WithArguments("C", "1").WithLocation(4, 22)
                );
        }

        [Fact]
        public void PatternTypeInfo_01()
        {
            var source = @"
public class C
{
    void M(T1 t1)
    {
        if (t1 is T2 (var t3, t4: T4 t4) { V5 : T6 t5 }) {}
    }
}
class T1
{
}
class T2 : T1
{
    public T5 V5 = null;
    public void Deconstruct(out T3 t3, out T4 t4) => throw null;
}
class T3
{
}
class T4
{
}
class T5
{
}
class T6 : T5
{
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var patterns = tree.GetRoot().DescendantNodesAndSelf().OfType<PatternSyntax>().ToArray();
            Assert.Equal(4, patterns.Length);

            Assert.Equal("T2 (var t3, t4: T4 t4) { V5 : T6 t5 }", patterns[0].ToString());
            var ti = model.GetTypeInfo(patterns[0]);
            Assert.Equal("T1", ti.Type.ToTestDisplayString());
            Assert.Equal("T2", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("var t3", patterns[1].ToString());
            ti = model.GetTypeInfo(patterns[1]);
            Assert.Equal("T3", ti.Type.ToTestDisplayString());
            Assert.Equal("T3", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("T4 t4", patterns[2].ToString());
            ti = model.GetTypeInfo(patterns[2]);
            Assert.Equal("T4", ti.Type.ToTestDisplayString());
            Assert.Equal("T4", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("T6 t5", patterns[3].ToString());
            ti = model.GetTypeInfo(patterns[3]);
            Assert.Equal("T5", ti.Type.ToTestDisplayString());
            Assert.Equal("T6", ti.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void PatternTypeInfo_02()
        {
            var source = @"
public class C
{
    void M(object o)
    {
        if (o is Point(3, 4.0)) {}
    }
}
class Point
{
    public void Deconstruct(out object o1, out System.IComparable o2) => throw null;
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var patterns = tree.GetRoot().DescendantNodesAndSelf().OfType<PatternSyntax>().ToArray();
            Assert.Equal(3, patterns.Length);

            Assert.Equal("Point(3, 4.0)", patterns[0].ToString());
            var ti = model.GetTypeInfo(patterns[0]);
            Assert.Equal("System.Object", ti.Type.ToTestDisplayString());
            Assert.Equal("Point", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("3", patterns[1].ToString());
            ti = model.GetTypeInfo(patterns[1]);
            Assert.Equal("System.Object", ti.Type.ToTestDisplayString());
            Assert.Equal("System.Int32", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("4.0", patterns[2].ToString());
            ti = model.GetTypeInfo(patterns[2]);
            Assert.Equal("System.IComparable", ti.Type.ToTestDisplayString());
            Assert.Equal("System.Double", ti.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void PatternTypeInfo_03()
        {
            var source = @"
public class C
{
    void M(object o)
    {
        if (o is Point(3, 4.0) { Missing: Xyzzy }) {}
        if (o is Q7 t) {}
    }
}
";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,18): error CS0246: The type or namespace name 'Point' could not be found (are you missing a using directive or an assembly reference?)
                //         if (o is Point(3, 4.0) { Missing: Xyzzy }) {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Point").WithArguments("Point").WithLocation(6, 18),
                // (6,43): error CS0103: The name 'Xyzzy' does not exist in the current context
                //         if (o is Point(3, 4.0) { Missing: Xyzzy }) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Xyzzy").WithArguments("Xyzzy").WithLocation(6, 43),
                // (7,18): error CS0246: The type or namespace name 'Q7' could not be found (are you missing a using directive or an assembly reference?)
                //         if (o is Q7 t) {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Q7").WithArguments("Q7").WithLocation(7, 18)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var patterns = tree.GetRoot().DescendantNodesAndSelf().OfType<PatternSyntax>().ToArray();
            Assert.Equal(5, patterns.Length);

            Assert.Equal("Point(3, 4.0) { Missing: Xyzzy }", patterns[0].ToString());
            var ti = model.GetTypeInfo(patterns[0]);
            Assert.Equal("System.Object", ti.Type.ToTestDisplayString());
            Assert.Equal("Point", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("3", patterns[1].ToString());
            ti = model.GetTypeInfo(patterns[1]);
            Assert.Equal("?", ti.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, ti.Type.TypeKind);
            Assert.Equal("System.Int32", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("4.0", patterns[2].ToString());
            ti = model.GetTypeInfo(patterns[2]);
            Assert.Equal("?", ti.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, ti.Type.TypeKind);
            Assert.Equal("System.Double", ti.ConvertedType.ToTestDisplayString());

            Assert.Equal("Xyzzy", patterns[3].ToString());
            ti = model.GetTypeInfo(patterns[3]);
            Assert.Equal("?", ti.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, ti.Type.TypeKind);
            Assert.Equal("?", ti.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, ti.ConvertedType.TypeKind);

            Assert.Equal("Q7 t", patterns[4].ToString());
            ti = model.GetTypeInfo(patterns[4]);
            Assert.Equal("System.Object", ti.Type.ToTestDisplayString());
            Assert.Equal("Q7", ti.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, ti.ConvertedType.TypeKind);
        }

        [Fact]
        [WorkItem(34678, "https://github.com/dotnet/roslyn/issues/34678")]
        public void ConstantPatternVsUnconstrainedTypeParameter05()
        {
            var source =
@"class C<T>
{
    static bool Test1(T t)
    {
        return t is null; // 1
    }
    static bool Test2(C<T> t)
    {
        return t is null; // ok
    }
    static bool Test3(T t)
    {
        return t is 1; // 2
    }
    static bool Test4(T t)
    {
        return t is ""frog""; // 3
    }
}";
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (5,21): error CS8511: An expression of type 'T' cannot be handled by a pattern of type '<null>'. Please use language version '8.0' or greater to match an open type with a constant pattern.
                //         return t is null; // 1
                Diagnostic(ErrorCode.ERR_ConstantPatternVsOpenType, "null").WithArguments("T", "<null>", "8.0").WithLocation(5, 21),
                // (13,21): error CS8511: An expression of type 'T' cannot be handled by a pattern of type 'int'. Please use language version '8.0' or greater to match an open type with a constant pattern.
                //         return t is 1; // 2
                Diagnostic(ErrorCode.ERR_ConstantPatternVsOpenType, "1").WithArguments("T", "int", "8.0").WithLocation(13, 21),
                // (17,21): error CS8511: An expression of type 'T' cannot be handled by a pattern of type 'string'. Please use language version '8.0' or greater to match an open type with a constant pattern.
                //         return t is "frog"; // 3
                Diagnostic(ErrorCode.ERR_ConstantPatternVsOpenType, @"""frog""").WithArguments("T", "string", "8.0").WithLocation(17, 21));
        }

        [Fact]
        [WorkItem(34905, "https://github.com/dotnet/roslyn/issues/34905")]
        public void ConstantPatternVsUnconstrainedTypeParameter06()
        {
            var source =
@"public class C<T>
{
    public enum E
    {
        V1, V2
    }

    public void M()
    {
        switch (default(E))
        {
            case E.V1:
                break;
        }
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll).VerifyDiagnostics();
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics();
        }

        [Fact]
        public void WarnUnmatchedIsRelationalPattern()
        {
            var source =
@"public class C
{
    public void M()
    {
        _ = 1 is < 0; // 1
        _ = 1 is < 1; // 2
        _ = 1 is < 2; // 3
        _ = 1 is <= 0; // 4
        _ = 1 is <= 1; // 5
        _ = 1 is <= 2; // 6
        _ = 1 is > 0; // 7
        _ = 1 is > 1; // 8
        _ = 1 is > 2; // 9
        _ = 1 is >= 0; // 10
        _ = 1 is >= 1; // 11
        _ = 1 is >= 2; // 12
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (5,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is < 0; // 1
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is < 0").WithLocation(5, 13),
                // (6,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is < 1; // 2
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is < 1").WithLocation(6, 13),
                // (7,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is < 2; // 3
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is < 2").WithLocation(7, 13),
                // (8,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is <= 0; // 4
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is <= 0").WithLocation(8, 13),
                // (9,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is <= 1; // 5
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is <= 1").WithLocation(9, 13),
                // (10,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is <= 2; // 6
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is <= 2").WithLocation(10, 13),
                // (11,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is > 0; // 7
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is > 0").WithLocation(11, 13),
                // (12,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is > 1; // 8
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is > 1").WithLocation(12, 13),
                // (13,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is > 2; // 9
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is > 2").WithLocation(13, 13),
                // (14,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is >= 0; // 10
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is >= 0").WithLocation(14, 13),
                // (15,13): error CS8793: The given expression always matches the provided pattern.
                //         _ = 1 is >= 1; // 11
                Diagnostic(ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern, "1 is >= 1").WithLocation(15, 13),
                // (16,13): warning CS8519: The given expression never matches the provided pattern.
                //         _ = 1 is >= 2; // 12
                Diagnostic(ErrorCode.WRN_GivenExpressionNeverMatchesPattern, "1 is >= 2").WithLocation(16, 13)
                );
        }

        [Fact]
        public void RelationalPatternInSwitchWithConstantControllingExpression()
        {
            var source =
@"public class C
{
    public void M()
    {
        switch (1)
        {
            case < 0: break; // 1
            case < 1: break; // 2
            case < 2: break;
            case < 3: break; // 3
        }
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (7,23): warning CS0162: Unreachable code detected
                //             case < 0: break; // 1
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(7, 23),
                // (8,23): warning CS0162: Unreachable code detected
                //             case < 1: break; // 2
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 23),
                // (10,23): warning CS0162: Unreachable code detected
                //             case < 3: break; // 3
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 23)
                );
        }

        [Fact]
        public void RelationalPatternInSwitchWithOutOfRangeComparand()
        {
            var source =
@"public class C
{
    public void M(int i)
    {
        switch (i)
        {
            case < int.MinValue: break; // 1
            case <= int.MinValue: break;
            case > int.MaxValue: break; // 2
            case >= int.MaxValue: break;
        }
    }
    public void M(uint i)
    {
        switch (i)
        {
            case < 0: break; // 3
            case <= 0: break;
            case > uint.MaxValue: break; // 4
            case >= uint.MaxValue: break;
        }
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                    // (7,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    //             case < int.MinValue: break; // 1
                    Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "< int.MinValue").WithLocation(7, 18),
                    // (9,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    //             case > int.MaxValue: break; // 2
                    Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "> int.MaxValue").WithLocation(9, 18),
                    // (17,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    //             case < 0: break; // 3
                    Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "< 0").WithLocation(17, 18),
                    // (19,18): error CS8120: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
                    //             case > uint.MaxValue: break; // 4
                    Diagnostic(ErrorCode.ERR_SwitchCaseSubsumed, "> uint.MaxValue").WithLocation(19, 18)
                );
        }

        [Fact]
        public void IsRelationalPatternWithOutOfRangeComparand()
        {
            var source =
@"public class C
{
    public void M(int i)
    {
        _ = i is < int.MinValue; // 1
        _ = i is <= int.MinValue;
        _ = i is > int.MaxValue; // 2
        _ = i is >= int.MaxValue;
    }
    public void M(uint i)
    {
        _ = i is < 0; // 3
        _ = i is <= 0;
        _ = i is > uint.MaxValue; // 4
        _ = i is >= uint.MaxValue;
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                    // (5,13): error CS8518: An expression of type 'int' can never match the provided pattern.
                    //         _ = i is < int.MinValue; // 1
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, "i is < int.MinValue").WithArguments("int").WithLocation(5, 13),
                    // (7,13): error CS8518: An expression of type 'int' can never match the provided pattern.
                    //         _ = i is > int.MaxValue; // 2
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, "i is > int.MaxValue").WithArguments("int").WithLocation(7, 13),
                    // (12,13): error CS8518: An expression of type 'uint' can never match the provided pattern.
                    //         _ = i is < 0; // 3
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, "i is < 0").WithArguments("uint").WithLocation(12, 13),
                    // (14,13): error CS8518: An expression of type 'uint' can never match the provided pattern.
                    //         _ = i is > uint.MaxValue; // 4
                    Diagnostic(ErrorCode.ERR_IsPatternImpossible, "i is > uint.MaxValue").WithArguments("uint").WithLocation(14, 13)
                );
        }

        [Fact]
        public void IsRelationalPatternWithAlwaysMatchingRange()
        {
            var source =
@"public class C
{
    public void M(int i)
    {
        _ = i is > int.MinValue;
        _ = i is >= int.MinValue; // 1
        _ = i is < int.MaxValue;
        _ = i is <= int.MaxValue; // 2
    }
    public void M(uint i)
    {
        _ = i is > 0;
        _ = i is >= 0; // 3
        _ = i is < uint.MaxValue;
        _ = i is <= uint.MaxValue; // 4
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (6,13): warning CS8794: An expression of type 'int' always matches the provided pattern.
                //         _ = i is >= int.MinValue; // 1
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is >= int.MinValue").WithArguments("int").WithLocation(6, 13),
                // (8,13): warning CS8794: An expression of type 'int' always matches the provided pattern.
                //         _ = i is <= int.MaxValue; // 2
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is <= int.MaxValue").WithArguments("int").WithLocation(8, 13),
                // (13,13): warning CS8794: An expression of type 'uint' always matches the provided pattern.
                //         _ = i is >= 0; // 3
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is >= 0").WithArguments("uint").WithLocation(13, 13),
                // (15,13): warning CS8794: An expression of type 'uint' always matches the provided pattern.
                //         _ = i is <= uint.MaxValue; // 4
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "i is <= uint.MaxValue").WithArguments("uint").WithLocation(15, 13)
                );
        }

        [Fact]
        public void IsImpossiblePatternKinds()
        {
            var source =
@"public class C
{
    public void M(string s)
    {
        _ = s is (System.Delegate); // impossible parenthesized type pattern
        _ = s is not _;             // impossible negated pattern
        _ = s is ""a"" and ""b"";   // impossible conjunctive pattern
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (5,19): error CS8121: An expression of type 'string' cannot be handled by a pattern of type 'Delegate'.
                //         _ = s is (System.Delegate); // impossible parenthesized type pattern
                Diagnostic(ErrorCode.ERR_PatternWrongType, "System.Delegate").WithArguments("string", "System.Delegate").WithLocation(5, 19),
                // (6,13): error CS8518: An expression of type 'string' can never match the provided pattern.
                //         _ = s is not _;             // impossible negated pattern
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, "s is not _").WithArguments("string").WithLocation(6, 13),
                // (7,13): error CS8518: An expression of type 'string' can never match the provided pattern.
                //         _ = s is "a" and "b";   // impossible conjunctive pattern
                Diagnostic(ErrorCode.ERR_IsPatternImpossible, @"s is ""a"" and ""b""").WithArguments("string").WithLocation(7, 13)
                );
        }

        [Fact]
        public void IsNullableReferenceType_01()
        {
            var source =
@"#nullable enable
public class C {
    public void M1(object o) {
        var t = o is string? { };
    }
    public void M2(object o) {
        var t = o is (string? { });
    }
    public void M3(object o) {
        var t = o is string?;
    }
    public void M4(object o) {
        var t = o is string? _;
    }
    public void M5(object o) {
        var t = o is (string? _);
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (4,22): error CS8116: It is not legal to use nullable type 'string?' in a pattern; use the underlying type 'string' instead.
                //         var t = o is string? { };
                Diagnostic(ErrorCode.ERR_PatternNullableType, "string?").WithArguments("string").WithLocation(4, 22),
                // (7,22): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //         var t = o is (string? { });
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(string? { })").WithArguments("object", "2").WithLocation(7, 22),
                // (7,29): error CS1003: Syntax error, ',' expected
                //         var t = o is (string? { });
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(",", "?").WithLocation(7, 29),
                // (7,31): error CS1003: Syntax error, ',' expected
                //         var t = o is (string? { });
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(7, 31),
                // (10,22): error CS8650: It is not legal to use nullable reference type 'string?' in an is-type expression; use the underlying type 'string' instead.
                //         var t = o is string?;
                Diagnostic(ErrorCode.ERR_IsNullableType, "string?").WithArguments("string").WithLocation(10, 22),
                // (13,30): error CS0103: The name '_' does not exist in the current context
                //         var t = o is string? _;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(13, 30),
                // (13,31): error CS1003: Syntax error, ':' expected
                //         var t = o is string? _;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":", ";").WithLocation(13, 31),
                // (13,31): error CS1525: Invalid expression term ';'
                //         var t = o is string? _;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(13, 31),
                // (16,22): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'object', with 2 out parameters and a void return type.
                //         var t = o is (string? _);
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(string? _)").WithArguments("object", "2").WithLocation(16, 22),
                // (16,29): error CS1003: Syntax error, ',' expected
                //         var t = o is (string? _);
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(",", "?").WithLocation(16, 29),
                // (16,31): error CS1003: Syntax error, ',' expected
                //         var t = o is (string? _);
                Diagnostic(ErrorCode.ERR_SyntaxError, "_").WithArguments(",", "").WithLocation(16, 31)
                );
        }

        [Fact]
        public void IsAlwaysPatternKinds()
        {
            var source =
@"public class C
{
    public void M(string s)
    {
        _ = s is (_);                   // always parenthesized discard pattern
        _ = s is not System.Delegate;   // always negated type pattern
        _ = s is string or null;        // always disjunctive pattern
    }
}
";
            CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (6,22): error CS8121: An expression of type 'string' cannot be handled by a pattern of type 'Delegate'.
                //         _ = s is not System.Delegate;   // always negated type pattern
                Diagnostic(ErrorCode.ERR_PatternWrongType, "System.Delegate").WithArguments("string", "System.Delegate").WithLocation(6, 22),
                // (7,13): warning CS8794: An expression of type 'string' always matches the provided pattern.
                //         _ = s is string or null;        // always disjunctive pattern
                Diagnostic(ErrorCode.WRN_IsPatternAlways, "s is string or null").WithArguments("string").WithLocation(7, 13)
                );
        }

        [Fact]
        public void SemanticModelForSwitchExpression()
        {
            var source =
@"public class C
{
    void M(int i)
    {
        C x0 = i switch // 0
        {
            0 => new A(),
            1 => new B(),
            _ => throw null,
        };
        _ = i switch // 1
        {
            0 => new A(),
            1 => new B(),
            _ => throw null,
        };
        D x2 = i switch // 2
        {
            0 => new A(),
            1 => new B(),
            _ => throw null,
        };
        D x3 = i switch // 3
        {
            0 => new E(), // 3.1
            1 => new F(), // 3.2
            _ => throw null,
        };
        C x4 = i switch // 4
        {
            0 => new A(),
            1 => new B(),
            2 => new C(),
            _ => throw null,
        };
        D x5 = i switch // 5
        {
            0 => new A(),
            1 => new B(),
            2 => new C(),
            _ => throw null,
        };
        D x6 = i switch // 6
        {
            0 => 1,
            1 => 2,
            _ => throw null,
        };
        _ = (C)(i switch // 7
        {
            0 => new A(),
            1 => new B(),
            _ => throw null,
        });
        _ = (D)(i switch // 8
        {
            0 => new A(),
            1 => new B(),
            _ => throw null,
        });
        _ = (D)(i switch // 9
        {
            0 => new E(), // 9.1
            1 => new F(), // 9.2
            _ => throw null,
        });
        _ = (C)(i switch // 10
        {
            0 => new A(),
            1 => new B(),
            2 => new C(),
            _ => throw null,
        });
        _ = (D)(i switch // 11
        {
            0 => new A(),
            1 => new B(),
            2 => new C(),
            _ => throw null,
        });
        _ = (D)(i switch // 12
        {
            0 => 1,
            1 => 2,
            _ => throw null,
        });
    }
}

class A : C { }
class B : C { }
class D
{
    public static implicit operator D(C c) => throw null;
    public static implicit operator D(short s) => throw null;
}
class E
{
    public static implicit operator C(E c) => throw null;
}
class F
{
    public static implicit operator C(F c) => throw null;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithPatternCombinators).VerifyDiagnostics(
                // (11,15): error CS8506: No best type was found for the switch expression.
                //         _ = i switch // 1
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(11, 15),
                // (25,18): error CS0029: Cannot implicitly convert type 'E' to 'D'
                //             0 => new E(), // 3.1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new E()").WithArguments("E", "D").WithLocation(25, 18),
                // (26,18): error CS0029: Cannot implicitly convert type 'F' to 'D'
                //             1 => new F(), // 3.2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new F()").WithArguments("F", "D").WithLocation(26, 18),
                // (63,18): error CS0029: Cannot implicitly convert type 'E' to 'D'
                //             0 => new E(), // 9.1
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new E()").WithArguments("E", "D").WithLocation(63, 18),
                // (64,18): error CS0029: Cannot implicitly convert type 'F' to 'D'
                //             1 => new F(), // 9.2
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new F()").WithArguments("F", "D").WithLocation(64, 18)
                );
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            void checkType(ExpressionSyntax expr, string expectedNaturalType, string expectedConvertedType, ConversionKind expectedConversionKind)
            {
                var typeInfo = model.GetTypeInfo(expr);
                var conversion = model.GetConversion(expr);
                Assert.Equal(expectedNaturalType, typeInfo.Type?.ToTestDisplayString());
                Assert.Equal(expectedConvertedType, typeInfo.ConvertedType?.ToTestDisplayString());
                Assert.Equal(expectedConversionKind, conversion.Kind);
            }

            var switches = tree.GetRoot().DescendantNodes().OfType<SwitchExpressionSyntax>().ToArray();
            for (int i = 0; i < switches.Length; i++)
            {
                var expr = switches[i];
                switch (i)
                {
                    case 0:
                    case 7:
                        checkType(expr, null, "C", ConversionKind.SwitchExpression);
                        checkType(expr.Arms[0].Expression, "A", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[1].Expression, "B", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[2].Expression, null, "C", ConversionKind.ImplicitThrow);
                        break;
                    case 1:
                        checkType(expr, null, "?", ConversionKind.Identity);
                        checkType(expr.Arms[0].Expression, "A", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[1].Expression, "B", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[2].Expression, null, "?", ConversionKind.ImplicitThrow);
                        break;
                    case 2:
                    case 8:
                        checkType(expr, null, "D", ConversionKind.SwitchExpression);
                        checkType(expr.Arms[0].Expression, "A", "D", ConversionKind.ImplicitUserDefined);
                        checkType(expr.Arms[1].Expression, "B", "D", ConversionKind.ImplicitUserDefined);
                        checkType(expr.Arms[2].Expression, null, "D", ConversionKind.ImplicitThrow);
                        break;
                    case 3:
                        checkType(expr, "?", "D", ConversionKind.NoConversion);
                        checkType(expr.Arms[0].Expression, "E", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[1].Expression, "F", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[2].Expression, null, "?", ConversionKind.ImplicitThrow);
                        break;
                    case 9:
                        checkType(expr, null, "?", ConversionKind.Identity);
                        checkType(expr.Arms[0].Expression, "E", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[1].Expression, "F", "?", ConversionKind.NoConversion);
                        checkType(expr.Arms[2].Expression, null, "?", ConversionKind.ImplicitThrow);
                        break;
                    case 4:
                    case 10:
                        checkType(expr, "C", "C", ConversionKind.Identity);
                        checkType(expr.Arms[0].Expression, "A", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[1].Expression, "B", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[2].Expression, "C", "C", ConversionKind.Identity);
                        checkType(expr.Arms[3].Expression, null, "C", ConversionKind.ImplicitThrow);
                        break;
                    case 5:
                        checkType(expr, "C", "D", ConversionKind.ImplicitUserDefined);
                        checkType(expr.Arms[0].Expression, "A", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[1].Expression, "B", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[2].Expression, "C", "C", ConversionKind.Identity);
                        checkType(expr.Arms[3].Expression, null, "C", ConversionKind.ImplicitThrow);
                        break;
                    case 11:
                        checkType(expr, "C", "C", ConversionKind.Identity);
                        checkType(expr.Arms[0].Expression, "A", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[1].Expression, "B", "C", ConversionKind.ImplicitReference);
                        checkType(expr.Arms[2].Expression, "C", "C", ConversionKind.Identity);
                        checkType(expr.Arms[3].Expression, null, "C", ConversionKind.ImplicitThrow);
                        break;
                    case 6:
                    case 12:
                        checkType(expr, "System.Int32", "D", ConversionKind.SwitchExpression);
                        checkType(expr.Arms[0].Expression, "System.Int32", "D", ConversionKind.ImplicitUserDefined);
                        checkType(expr.Arms[1].Expression, "System.Int32", "D", ConversionKind.ImplicitUserDefined);
                        checkType(expr.Arms[2].Expression, null, "D", ConversionKind.ImplicitThrow);
                        break;
                    default:
                        Assert.False(true);
                        break;
                }
            }
        }

        [Fact, WorkItem(45946, "https://github.com/dotnet/roslyn/issues/45946")]
        public void VoidPattern_01()
        {
            var source = @"
class C
{
    void F(object o)
    {
        _ = is this.F(1);
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS1525: Invalid expression term 'is'
                //         _ = is this.F(1);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "is").WithArguments("is").WithLocation(6, 13)
                );
        }

        [Fact, WorkItem(45946, "https://github.com/dotnet/roslyn/issues/45946")]
        public void VoidPattern_02()
        {
            var source = @"
class C
{
    void F(object o)
    {
        _ = switch { this.F(1) => 1 };
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,13): error CS1525: Invalid expression term 'switch'
                //         _ = switch { this.F(1) => 1 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "switch").WithArguments("switch").WithLocation(6, 13)
                );
        }
    }
}
