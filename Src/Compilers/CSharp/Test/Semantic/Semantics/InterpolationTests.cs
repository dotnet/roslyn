// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class InterpolationTests : CompilingTestBase
    {
        [Fact]
        public void TestSimpleInterp()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        var number = 8675309;
        Console.WriteLine($""Jenny don\'t change your number { number }."");
        Console.WriteLine($""Jenny don\'t change your number { number , -12 }."");
        Console.WriteLine($""Jenny don\'t change your number { number , 12 }."");
        Console.WriteLine($""Jenny don\'t change your number { number :###-####}."");
        Console.WriteLine($""Jenny don\'t change your number { number , -12 :###-####}."");
        Console.WriteLine($""Jenny don\'t change your number { number , 12 :###-####}."");
        Console.WriteLine($""{number}"");
    }
}";
            string expectedOutput =
@"Jenny don't change your number 8675309.
Jenny don't change your number 8675309     .
Jenny don't change your number      8675309.
Jenny don't change your number 867-5309.
Jenny don't change your number 867-5309    .
Jenny don't change your number     867-5309.
8675309";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestOnlyInterp()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        var number = 8675309;
        Console.WriteLine($""{number}"");
    }
}";
            string expectedOutput =
@"8675309";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestDoubleInterp01()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        var number = 8675309;
        Console.WriteLine($""{number}{number}"");
    }
}";
            string expectedOutput =
@"86753098675309";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestDoubleInterp02()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        var number = 8675309;
        Console.WriteLine($""Jenny don\'t change your number { number :###-####} { number :###-####}."");
    }
}";
            string expectedOutput =
@"Jenny don't change your number 867-5309 867-5309.";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestEmptyInterp()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""Jenny don\'t change your number { /*trash*/ }."");
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,73): error CS1733: Expected expression
                //         Console.WriteLine("Jenny don\'t change your number \{ /*trash*/ }.");
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(5, 73)
                );
        }

        [Fact]
        public void TestHalfOpenInterp01()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""Jenny don\'t change your number { "");
    }
}";
            // too many diagnostics perhaps, but it starts the right way.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,60): error CS8076: Missing close delimiter '}' for interpolated expression started with {.
                //         Console.WriteLine($"Jenny don\'t change your number { ");
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(5, 60),
                // (5,63): error CS1010: Newline in constant
                //         Console.WriteLine($"Jenny don\'t change your number { ");
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(5, 63),
                // (5,66): error CS1026: ) expected
                //         Console.WriteLine($"Jenny don\'t change your number { ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 66),
                // (5,66): error CS1002: ; expected
                //         Console.WriteLine($"Jenny don\'t change your number { ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 66)
                );
        }

        [Fact]
        public void TestHalfOpenInterp02()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""Jenny don\'t change your number { 8675309 // "");
    }
}";
            // too many diagnostics perhaps, but it starts the right way.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,71): error CS8077: A single-line comment may not be used in an interpolated string.
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 // ");
                Diagnostic(ErrorCode.ERR_SingleLineCommentInExpressionHole, "//").WithLocation(5, 71),
                // (5,77): error CS1026: ) expected
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 // ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 77),
                // (5,77): error CS1002: ; expected
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 // ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 77)
                );
        }
        
        [Fact]
        public void TestHalfOpenInterp03()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""Jenny don\'t change your number { 8675309 /* "");
    }
}";
            // too many diagnostics perhaps, but it starts the right way.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,60): error CS8076: Missing close delimiter '}' for interpolated expression started with {.
                //         Console.WriteLine($"Jenny don\'t change your number { 8675309 /* ");
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, " {").WithLocation(5, 60),
                // (5,71): error CS1035: End-of-file found, '*/' expected
                //         Console.WriteLine($"Jenny don\'t change your number { 8675309 /* ");
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(5, 71),
                // (5,77): error CS1026: ) expected
                //         Console.WriteLine($"Jenny don\'t change your number { 8675309 /* ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 77),
                // (5,77): error CS1002: ; expected
                //         Console.WriteLine($"Jenny don\'t change your number { 8675309 /* ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 77)
                );
        }
        
        [Fact]
        public void LambdaInInterp()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        //Console.WriteLine(""jenny {0:(408) ###-####}"", new object[] { ((Func<int>)(() => { return number; })).Invoke() });
        Console.WriteLine($""jenny { ((Func<int>)(() => { return number; })).Invoke() :(408) ###-####}"");
    }

        static int number = 8675309;
    }
";
            string expectedOutput = @"jenny (408) 867-5309";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TwoInserts()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var hello = $""Hello"";
        var world = $""world"" ;
        Console.WriteLine( $""{hello}, { world }."" );
    }
}";
            string expectedOutput = @"Hello, world.";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TwoInserts02()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var hello = $""Hello"";
        var world = $""world"" ;
        Console.WriteLine( $@""{
                                    hello
                            },
{
                            world }."" );
    }
}";
            string expectedOutput = @"Hello,
world.";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void UnclosedInterpolation()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $""{"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,29): error CS8076: Missing close delimiter '}' for interpolated expression started with {.
                //         Console.WriteLine( $"{" );
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(6, 29),
                // (6,31): error CS1010: Newline in constant
                //         Console.WriteLine( $"{" );
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(6, 31),
                // (6,35): error CS1026: ) expected
                //         Console.WriteLine( $"{" );
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(6, 35),
                // (6,35): error CS1002: ; expected
                //         Console.WriteLine( $"{" );
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 35)
                );
        }

        [Fact]
        public void EmptyFormatSpecifier()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $""{3:}"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,32): error CS8089: Empty format specifier.
                //         Console.WriteLine( $"{3:}" );
                Diagnostic(ErrorCode.ERR_EmptyFormatSpecifier, ":").WithLocation(6, 32)
                );
        }

        [Fact]
        public void TrailingSpaceInFormatSpecifier()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $""{3:d }"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,32): error CS8088: A format specifier may not contain trailing whitespace.
                //         Console.WriteLine( $"{3:d }" );
                Diagnostic(ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier, ":d ").WithLocation(6, 32)
                );
        }

        [Fact]
        public void TrailingSpaceInFormatSpecifier02()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $@""{3:d
}"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    // (6,33): error CS8088: A format specifier may not contain trailing whitespace.
    //         Console.WriteLine( $@"{3:d
    Diagnostic(ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier, @":d
").WithLocation(6, 33)
                );
        }

        [Fact]
        public void MissingInterpolationExpression01()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $""{ }"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,32): error CS1733: Expected expression
                //         Console.WriteLine( $"{ }" );
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 32)
                );
        }

        [Fact]
        public void MissingInterpolationExpression02()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( $@""{ }"" );
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,33): error CS1733: Expected expression
                //         Console.WriteLine( $@"{ }" );
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 33)
                );
        }

        [Fact]
        public void MissingInterpolationExpression03()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine( ";
            var normal = "$\"";
            var verbat = "$@\"";
            // ensure reparsing of interpolated string token is precise in error scenarios (assertions do not fail)
            CreateCompilationWithMscorlib45(source + normal).GetDiagnostics();
            CreateCompilationWithMscorlib45(source + normal + " ").GetDiagnostics();
            CreateCompilationWithMscorlib45(source + normal + "{").GetDiagnostics();
            CreateCompilationWithMscorlib45(source + normal + "{ ").GetDiagnostics();
            CreateCompilationWithMscorlib45(source + verbat).GetDiagnostics();
            CreateCompilationWithMscorlib45(source + verbat + " ").GetDiagnostics();
            CreateCompilationWithMscorlib45(source + verbat + "{").GetDiagnostics();
            CreateCompilationWithMscorlib45(source + verbat + "{ ").GetDiagnostics();
        }

        [Fact]
        public void MisplacedNewline01()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var s = $""{ @""
"" }
            "";
    }
}";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,18): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(6, 18),
                // (6,21): error CS1039: Unterminated string literal
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_UnterminatedStringLit, "").WithLocation(6, 21),
                // (6,23): error CS1002: ; expected
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 23),
                // (7,1): error CS1010: Newline in constant
                // " }
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(7, 1),
                // (7,4): error CS1002: ; expected
                // " }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 4),
                // (8,13): error CS1010: Newline in constant
                //             ";
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(8, 13),
                // (8,15): error CS1002: ; expected
                //             ";
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 15),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1)
                );
        }

        [Fact]
        public void MisplacedNewline02()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var s = $""{ @""
""}
            "";
    }
}";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,18): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(6, 18),
                // (6,21): error CS1039: Unterminated string literal
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_UnterminatedStringLit, "").WithLocation(6, 21),
                // (6,23): error CS1002: ; expected
                //         var s = $"{ @"
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 23),
                // (7,1): error CS1010: Newline in constant
                // "}
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(7, 1),
                // (7,3): error CS1002: ; expected
                // "}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 3),
                // (8,13): error CS1010: Newline in constant
                //             ";
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(8, 13),
                // (8,15): error CS1002: ; expected
                //             ";
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 15),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1)
                );
        }

        [Fact(Skip = "1093602")]
        public void PreprocessorInsideInterpolation()
        {
            string source =
@"class Program
{
    static void Main()
    {
        var s = $@""{
#region :
#endregion
0
}"";
    }
}";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
    // (6,1): error CS1003: Syntax error, '}' expected
    // #region :
    Diagnostic(ErrorCode.ERR_SyntaxError, "#").WithArguments("}").WithLocation(6, 1),
    // (6,1): error CS1525: Invalid expression term ''
    // #region :
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "#").WithArguments("").WithLocation(6, 1),
    // (5,21): error CS1056: Unexpected character '#'
    //         var s = $@"{
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(5, 21),
    // (6,1): error CS1056: Unexpected character '#'
    // #region :
    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(6, 1),
    // (6,9): error CS8088: A format specifier may not contain trailing whitespace.
    // #region :
    Diagnostic(ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier, @":
#endregion
0
").WithLocation(6, 9)
                );
        }

        [Fact]
        public void EscapedCurly()
        {
            string source =
@"class Program
{
    static void Main()
    {
        var s1 = $"" \u007B "";
        var s2 = $"" \u007D"";
    }
}";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,21): error CS8087: A '{' character may only be escaped by doubling '{{' in an interpolated string.
                //         var s1 = $" \u007B ";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\u007B").WithArguments("{").WithLocation(5, 21),
                // (6,21): error CS8087: A '}' character may only be escaped by doubling '}}' in an interpolated string.
                //         var s2 = $" \u007D";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\u007D").WithArguments("}").WithLocation(6, 21)
                );
        }

        [Fact]
        public void BadAlignment()
        {
            string source =
@"class Program
{
    static void Main()
    {
        var s = $""{1,1E10}"";
    }
}";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,22): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var s = $"{1,1E10}";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1E10").WithArguments("double", "int").WithLocation(5, 22)
                );
        }

        [Fact]
        public void NestedInterpolatedVerbatim()
        {
            string source =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var s = $@""{$@""{1}""}"";
        Console.WriteLine(s);
        }
    }";
            string expectedOutput = @"1";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }
    }
}
