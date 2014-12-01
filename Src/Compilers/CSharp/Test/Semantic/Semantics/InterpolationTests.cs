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
        Console.WriteLine(""Jenny don\'t change your number \{ number }."");
        Console.WriteLine(""Jenny don\'t change your number \{ number , -12 }."");
        Console.WriteLine(""Jenny don\'t change your number \{ number , 12 }."");
        Console.WriteLine(""Jenny don\'t change your number \{ number : ""###-####"" }."");
        Console.WriteLine(""Jenny don\'t change your number \{ number , -12 : ""###-####"" }."");
        Console.WriteLine(""Jenny don\'t change your number \{ number , 12 : ""###-####"" }."");
        Console.WriteLine(""\{number}"");
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
        Console.WriteLine(""\{number}"");
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
        Console.WriteLine(""\{number}\{number}"");
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
        Console.WriteLine(""Jenny don\'t change your number \{ number : ""###-####"" } \{ number : ""###-####"" }."");
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
        Console.WriteLine(""Jenny don\'t change your number \{ /*trash*/ }."");
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
        Console.WriteLine(""Jenny don\'t change your number \{ "");
    }
}";
            // too many diagnostics perhaps, but it starts the right way.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,60): error CS8076: Missing close delimiter '}' for interpolated expression started with \{.
                //         Console.WriteLine("Jenny don\'t change your number \{ ");
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"\{").WithLocation(5, 60),
                // (5,63): error CS1010: Newline in constant
                //         Console.WriteLine("Jenny don\'t change your number \{ ");
                Diagnostic(ErrorCode.ERR_NewlineInConst, "").WithLocation(5, 63),
                // (5,66): error CS1026: ) expected
                //         Console.WriteLine("Jenny don\'t change your number \{ ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 66),
                // (5,66): error CS1002: ; expected
                //         Console.WriteLine("Jenny don\'t change your number \{ ");
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
        Console.WriteLine(""Jenny don\'t change your number \{ 8675309 // "");
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
        Console.WriteLine(""Jenny don\'t change your number \{ 8675309 /* "");
    }
}";
            // too many diagnostics perhaps, but it starts the right way.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,60): error CS8076: Missing close delimiter '}' for interpolated expression started with \{.
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 /* ");
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"\{").WithLocation(5, 60),
                // (5,71): error CS1035: End-of-file found, '*/' expected
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 /* ");
                Diagnostic(ErrorCode.ERR_OpenEndedComment, "").WithLocation(5, 71),
                // (5,77): error CS1026: ) expected
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 /* ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 77),
                // (5,77): error CS1002: ; expected
                //         Console.WriteLine("Jenny don\'t change your number \{ 8675309 /* ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 77)
                );
        }

        [Fact]
        public void TestComplexInterp()
        {
            string source =
@"#define X
using System;
class Program {
    
    /// <summary>
    /// This is a doc comment.
    /// </summary>
    public static void Main(string[] args)
    {
        var number = 8675309;
        Console.WriteLine(""Jenny don't change your number \{number:###-####}"");
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,68): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                //         Console.WriteLine("Jenny don't change your number \{number:###-####}");
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(11, 68),
                // (11,76): error CS1003: Syntax error, '' expected
                //         Console.WriteLine("Jenny don't change your number \{number:###-####}");
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("", "").WithLocation(11, 76)
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
        Console.WriteLine(""jenny \{ ((Func<int>)(() => { return number; })).Invoke() : ""(408) ###-####"" }"");
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
        var hello = ""Hello"";
        var world = ""world"";
        Console.WriteLine(""\{ hello }, \{ world }."");
    }
}";
            string expectedOutput = @"Hello, world.";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }
    }
}
