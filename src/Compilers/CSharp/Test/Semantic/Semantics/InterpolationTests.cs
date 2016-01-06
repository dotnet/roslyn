// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System.Linq;
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

        [Fact, WorkItem(306), WorkItem(308)]
        public void DynamicInterpolation()
        {
            string source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void Main(string[] args)
    {
        dynamic nil = null;
        dynamic a = new string[] {""Hello"", ""world""};
        Console.WriteLine($""<{nil}>"");
        Console.WriteLine($""<{a}>"");
    }
    Expression<Func<string>> M(dynamic d) {
        return () => $""Dynamic: {d}"";
    }
}";
            string expectedOutput = @"<>
<System.String[]>";
            var verifier = CompileAndVerify(source, new[] { SystemCoreRef, CSharpRef }, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact]
        public void UnclosedInterpolation01()
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
        public void UnclosedInterpolation02()
        {
            string source =
@"class Program
{
    static void Main(string[] args)
    {
        var x = $"";";
            // The precise error messages are not important, but this must be an error.
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,19): error CS1010: Newline in constant
                //         var x = $";
                Diagnostic(ErrorCode.ERR_NewlineInConst, ";").WithLocation(5, 19),
                // (5,20): error CS1002: ; expected
                //         var x = $";
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 20),
                // (5,20): error CS1513: } expected
                //         var x = $";
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 20),
                // (5,20): error CS1513: } expected
                //         var x = $";
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 20)
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
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + normal).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + normal + " ").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + normal + "{").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + normal + "{ ").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + verbat).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + verbat + " ").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + verbat + "{").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
            Assert.True(SyntaxFactory.ParseSyntaxTree(source + verbat + "{ ").GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
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
            Assert.True(SyntaxFactory.ParseSyntaxTree(source).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
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
            Assert.True(SyntaxFactory.ParseSyntaxTree(source).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
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
            Assert.True(SyntaxFactory.ParseSyntaxTree(source).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
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
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,21): error CS8087: A '{' character may only be escaped by doubling '{{' in an interpolated string.
                //         var s1 = $" \u007B ";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\u007B").WithArguments("{").WithLocation(5, 21),
                // (6,21): error CS8087: A '}' character may only be escaped by doubling '}}' in an interpolated string.
                //         var s2 = $" \u007D";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\u007D").WithArguments("}").WithLocation(6, 21)
                );
        }

        [Fact, WorkItem(1119878, "DevDiv")]
        public void NoFillIns01()
        {
            string source =
@"class Program
{
    static void Main()
    {
        System.Console.Write($""{{ x }}"");
        System.Console.WriteLine($@""This is a test"");
    }
}";
            string expectedOutput = @"{ x }This is a test";
            CompileAndVerify(source, expectedOutput: expectedOutput);
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

        // Since the platform type System.FormattableString is not yet in our platforms (at the
        // time of writing), we explicitly include the required platform types into the sources under test.
        private const string formattableString = @"
/*============================================================
**
** Class:  FormattableString
**
**
** Purpose: implementation of the FormattableString
** class.
**
===========================================================*/
namespace System
{
    /// <summary>
    /// A composite format string along with the arguments to be formatted. An instance of this
    /// type may result from the use of the C# or VB language primitive ""interpolated string"".
    /// </summary>
    public abstract class FormattableString : IFormattable
    {
        /// <summary>
        /// The composite format string.
        /// </summary>
        public abstract string Format { get; }

        /// <summary>
        /// Returns an object array that contains zero or more objects to format. Clients should not
        /// mutate the contents of the array.
        /// </summary>
        public abstract object[] GetArguments();

        /// <summary>
        /// The number of arguments to be formatted.
        /// </summary>
        public abstract int ArgumentCount { get; }

        /// <summary>
        /// Returns one argument to be formatted from argument position <paramref name=""index""/>.
        /// </summary>
        public abstract object GetArgument(int index);

        /// <summary>
        /// Format to a string using the given culture.
        /// </summary>
        public abstract string ToString(IFormatProvider formatProvider);

        string IFormattable.ToString(string ignored, IFormatProvider formatProvider)
        {
            return ToString(formatProvider);
        }

        /// <summary>
        /// Format the given object in the invariant culture. This static method may be
        /// imported in C# by
        /// <code>
        /// using static System.FormattableString;
        /// </code>.
        /// Within the scope
        /// of that import directive an interpolated string may be formatted in the
        /// invariant culture by writing, for example,
        /// <code>
        /// Invariant($""{{ lat = {latitude}; lon = {longitude} }}"")
        /// </code>
        /// </summary>
        public static string Invariant(FormattableString formattable)
        {
            if (formattable == null)
            {
                throw new ArgumentNullException(""formattable"");
            }

            return formattable.ToString(Globalization.CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return ToString(Globalization.CultureInfo.CurrentCulture);
        }
    }
}


/*============================================================
**
** Class:  FormattableStringFactory
**
**
** Purpose: implementation of the FormattableStringFactory
** class.
**
===========================================================*/
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// A factory type used by compilers to create instances of the type <see cref=""FormattableString""/>.
    /// </summary>
    public static class FormattableStringFactory
    {
        /// <summary>
        /// Create a <see cref=""FormattableString""/> from a composite format string and object
        /// array containing zero or more objects to format.
        /// </summary>
        public static FormattableString Create(string format, params object[] arguments)
        {
            if (format == null)
            {
                throw new ArgumentNullException(""format"");
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(""arguments"");
            }

            return new ConcreteFormattableString(format, arguments);
        }

        private sealed class ConcreteFormattableString : FormattableString
        {
            private readonly string _format;
            private readonly object[] _arguments;

            internal ConcreteFormattableString(string format, object[] arguments)
            {
                _format = format;
                _arguments = arguments;
            }

            public override string Format { get { return _format; } }
            public override object[] GetArguments() { return _arguments; }
            public override int ArgumentCount { get { return _arguments.Length; } }
            public override object GetArgument(int index) { return _arguments[index]; }
            public override string ToString(IFormatProvider formatProvider) { return string.Format(formatProvider, Format, _arguments); }
        }
    }
}
";

        [Fact]
        public void TargetType01()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        IFormattable f = $""test"";
        Console.Write(f is System.FormattableString);
    }
}";
            CompileAndVerify(source + formattableString, expectedOutput: "True");
        }

        [Fact]
        public void TargetType02()
        {
            string source =
@"using System;
interface I1 { void M(String s); }
interface I2 { void M(FormattableString s); }
interface I3 { void M(IFormattable s); }
interface I4 : I1, I2 {}
interface I5 : I1, I3 {}
interface I6 : I2, I3 {}
interface I7 : I1, I2, I3 {}
class C : I1, I2, I3, I4, I5, I6, I7
{
    public void M(String s) { Console.Write(1); }
    public void M(FormattableString s) { Console.Write(2); }
    public void M(IFormattable s) { Console.Write(3); }
}
class Program {
    public static void Main(string[] args)
    {
        C c = new C();
        ((I1)c).M($"""");
        ((I2)c).M($"""");
        ((I3)c).M($"""");
        ((I4)c).M($"""");
        ((I5)c).M($"""");
        ((I6)c).M($"""");
        ((I7)c).M($"""");
        ((C)c).M($"""");
    }
}";
            CompileAndVerify(source + formattableString, expectedOutput: "12311211");
        }

        [Fact]
        public void MissingHelper()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        IFormattable f = $""test"";
    }
}";
            CreateCompilationWithMscorlib(source).VerifyEmitDiagnostics(
                // (5,26): error CS0518: Predefined type 'System.Runtime.CompilerServices.FormattableStringFactory' is not defined or imported
                //         IFormattable f = $"test";
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"$""test""").WithArguments("System.Runtime.CompilerServices.FormattableStringFactory").WithLocation(5, 26)
                );
        }

        [Fact]
        public void AsyncInterp()
        {
            string source =
@"using System;
using System.Threading.Tasks;
class Program {
    public static void Main(string[] args)
    {
        Task<string> hello = Task.FromResult(""Hello"");
        Task<string> world = Task.FromResult(""world"");
        M(hello, world).Wait();
    }
    public static async Task M(Task<string> hello, Task<string> world)
    {
        Console.WriteLine($""{ await hello }, { await world }!"");
    }
}";
            CompileAndVerify(
                source, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 }, expectedOutput: "Hello, world!");
        }

        [Fact]
        public void AlignmentExpression()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""X = { 123 , -(3+4) }."");
    }
}";
            CompileAndVerify(source + formattableString, expectedOutput: "X = 123    .");
        }

        [Fact]
        public void AlignmentMagnitude()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""X = { 123 , (32768) }."");
        Console.WriteLine($""X = { 123 , -(32768) }."");
        Console.WriteLine($""X = { 123 , (32767) }."");
        Console.WriteLine($""X = { 123 , -(32767) }."");
        Console.WriteLine($""X = { 123 , int.MaxValue }."");
        Console.WriteLine($""X = { 123 , int.MinValue }."");
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,42): warning CS8094: Alignment value 32768 has a magnitude greater than 32767 and may result in a large formatted string.
                //         Console.WriteLine($"X = { 123 , (32768) }.");
                Diagnostic(ErrorCode.WRN_AlignmentMagnitude, "32768").WithArguments("32768", "32767").WithLocation(5, 42),
                // (6,41): warning CS8094: Alignment value -32768 has a magnitude greater than 32767 and may result in a large formatted string.
                //         Console.WriteLine($"X = { 123 , -(32768) }.");
                Diagnostic(ErrorCode.WRN_AlignmentMagnitude, "-(32768)").WithArguments("-32768", "32767").WithLocation(6, 41),
                // (9,41): warning CS8094: Alignment value 2147483647 has a magnitude greater than 32767 and may result in a large formatted string.
                //         Console.WriteLine($"X = { 123 , int.MaxValue }.");
                Diagnostic(ErrorCode.WRN_AlignmentMagnitude, "int.MaxValue").WithArguments("2147483647", "32767").WithLocation(9, 41),
                // (10,41): warning CS8094: Alignment value -2147483648 has a magnitude greater than 32767 and may result in a large formatted string.
                //         Console.WriteLine($"X = { 123 , int.MinValue }.");
                Diagnostic(ErrorCode.WRN_AlignmentMagnitude, "int.MinValue").WithArguments("-2147483648", "32767").WithLocation(10, 41)
                );
        }

        [WorkItem(1097388, "DevDiv")]
        [Fact]
        public void InterpolationExpressionMustBeValue01()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""X = { String }."");
        Console.WriteLine($""X = { null }."");
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,35): error CS0119: 'string' is a type, which is not valid in the given context
                //         Console.WriteLine($"X = { String }.");
                Diagnostic(ErrorCode.ERR_BadSKunknown, "String").WithArguments("string", "type").WithLocation(5, 35)
                );
        }

        [Fact]
        public void InterpolationExpressionMustBeValue02()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        Console.WriteLine($""X = { x=>3 }."");
        Console.WriteLine($""X = { Program.Main }."");
        Console.WriteLine($""X = { Program.Main(null) }."");
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,35): error CS1660: Cannot convert lambda expression to type 'object' because it is not a delegate type
                //         Console.WriteLine($"X = { x=>3 }.");
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "x=>3").WithArguments("lambda expression", "object").WithLocation(5, 35),
                // (6,43): error CS0428: Cannot convert method group 'Main' to non-delegate type 'object'. Did you intend to invoke the method?
                //         Console.WriteLine($"X = { Program.Main }.");
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "Main").WithArguments("Main", "object").WithLocation(6, 43),
                // (7,35): error CS0029: Cannot implicitly convert type 'void' to 'object'
                //         Console.WriteLine($"X = { Program.Main(null) }.");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "Program.Main(null)").WithArguments("void", "object").WithLocation(7, 35)
                );
        }

        [WorkItem(1097428, "DevDiv")]
        [Fact]
        public void BadCorelib01()
        {
            var text =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Char { }
    public class String { }

    internal class Program
    {
        public static void Main()
        {
            var s = $""X = { 1 } "";
        }
    }
}";
            CreateCompilation(text, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .VerifyEmitDiagnostics(new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "x.y"),
                // (15,21): error CS0117: 'string' does not contain a definition for 'Format'
                //             var s = $"X = { 1 } ";
                Diagnostic(ErrorCode.ERR_NoSuchMember, @"$""X = { 1 } """).WithArguments("string", "Format").WithLocation(15, 21)
            );
        }

        [WorkItem(1097428, "DevDiv")]
        [Fact]
        public void BadCorelib02()
        {
            var text =
@"namespace System
{
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Char { }
    public class String {
        public static Boolean Format(string format, int arg) { return true; }
    }

    internal class Program
    {
        public static void Main()
        {
            var s = $""X = { 1 } "";
        }
    }
}";
            CreateCompilation(text, options: new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .VerifyEmitDiagnostics(new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "x.y"),
                // (17,21): error CS0029: Cannot implicitly convert type 'bool' to 'string'
                //             var s = $"X = { 1 } ";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""X = { 1 } """).WithArguments("bool", "string").WithLocation(17, 21)
            );
        }

        [Fact]
        public void SillyCoreLib01()
        {
            var text =
@"namespace System
{
    interface IFormattable { }
    namespace Runtime.CompilerServices {
        public static class FormattableStringFactory {
            public static Bozo Create(string format, int arg) { return new Bozo(); }
        }
    }
    public class Object { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { private Boolean m_value; Boolean Use(Boolean b) { m_value = b; return m_value; } }
    public struct Int32 { private Int32 m_value; Int32 Use(Int32 b) { m_value = b; return m_value; } }
    public struct Char { }
    public class String {
        public static Bozo Format(string format, int arg) { return new Bozo(); }
    }
    public class FormattableString {
    }
    public class Bozo {
        public static implicit operator string(Bozo bozo) { return ""zz""; }
        public static implicit operator FormattableString(Bozo bozo) { return new FormattableString(); }
    }

    internal class Program
    {
        public static void Main()
        {
            var s1 = $""X = { 1 } "";
            FormattableString s2 = $""X = { 1 } "";
        }
    }
}";
            var comp = CreateCompilation(text, options: Test.Utilities.TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
            var compilation = CompileAndVerify(comp, verify: false);
            compilation.VerifyIL("System.Program.Main",
@"{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldstr      ""X = {0} ""
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Bozo string.Format(string, int)""
  IL_000b:  call       ""string System.Bozo.op_Implicit(System.Bozo)""
  IL_0010:  pop
  IL_0011:  ldstr      ""X = {0} ""
  IL_0016:  ldc.i4.1
  IL_0017:  call       ""System.Bozo System.Runtime.CompilerServices.FormattableStringFactory.Create(string, int)""
  IL_001c:  call       ""System.FormattableString System.Bozo.op_Implicit(System.Bozo)""
  IL_0021:  pop
  IL_0022:  ret
}");
        }

        [WorkItem(1097386, "DevDiv")]
        [Fact]
        public void Syntax01()
        {
            var text =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = $""{ Math.Abs(value: 1):\}"";
        var y = x;
        }
    } 
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,40): error CS8087: A '}' character may only be escaped by doubling '}}' in an interpolated string.
                //         var x = $"{ Math.Abs(value: 1):\}";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\").WithArguments("}").WithLocation(6, 40),
                // (6,40): error CS1009: Unrecognized escape sequence
                //         var x = $"{ Math.Abs(value: 1):\}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\}").WithLocation(6, 40)
                );
        }

        [WorkItem(1097941, "DevDiv")]
        [Fact]
        public void Syntax02()
        {
            var text =
@"using S = System;
class C
{
    void M()
    {
        var x = $""{ (S:
    }
}";
            // the precise diagnostics do not matter, as long as it is an error and not a crash.
            Assert.True(SyntaxFactory.ParseSyntaxTree(text).GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
        }

        [WorkItem(1097386, "DevDiv")]
        [Fact]
        public void Syntax03()
        {
            var text =
@"using System;
class Program
{
    static void Main(string[] args)
    {
        var x = $""{ Math.Abs(value: 1):}}"";
        var y = x;
        }
    } 
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,18): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //         var x = $"{ Math.Abs(value: 1):}}";
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(6, 18)
                );
        }

        [WorkItem(1099105, "DevDiv")]
        [Fact]
        public void NoUnexpandedForm()
        {
            string source =
@"using System;
class Program {
    public static void Main(string[] args)
    {
        string[] arr1 = new string[] { ""xyzzy"" };
        object[] arr2 = arr1;
        Console.WriteLine($""-{null}-"");
        Console.WriteLine($""-{arr1}-"");
        Console.WriteLine($""-{arr2}-"");
    }
}";
            CompileAndVerify(source + formattableString, expectedOutput:
@"--
-System.String[]-
-System.String[]-");
        }

        [WorkItem(1097386, "DevDiv")]
        [Fact]
        public void Dynamic01()
        {
            var text =
@"class C
{
    const dynamic a = a;
    string s = $""{0,a}"";
}";
            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (3,19): error CS0110: The evaluation of the constant value for 'C.a' involves a circular definition
                //     const dynamic a = a;
                Diagnostic(ErrorCode.ERR_CircConstValue, "a").WithArguments("C.a").WithLocation(3, 19),
                // (3,23): error CS0134: 'C.a' is of type 'dynamic'. A const field of a reference type other than string can only be initialized with null.
                //     const dynamic a = a;
                Diagnostic(ErrorCode.ERR_NotNullConstRefField, "a").WithArguments("C.a", "dynamic").WithLocation(3, 23),
                // (4,21): error CS0150: A constant value is expected
                //     string s = $"{0,a}";
                Diagnostic(ErrorCode.ERR_ConstantExpected, "a").WithLocation(4, 21)
                );
        }

        [WorkItem(1099238, "DevDiv")]
        [Fact]
        public void Syntax04()
        {
            var text =
@"using System;
using System.Linq.Expressions;
 
class Program
{
    static void Main()
    {
        Expression<Func<string>> e = () => $""\u1{0:\u2}"";
        Console.WriteLine(e);
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (8,46): error CS1009: Unrecognized escape sequence
                //         Expression<Func<string>> e = () => $"\u1{0:\u2}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u1").WithLocation(8, 46),
                // (8,52): error CS1009: Unrecognized escape sequence
                //         Expression<Func<string>> e = () => $"\u1{0:\u2}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u2").WithLocation(8, 52)
                );
        }

        [Fact, WorkItem(1098612, "DevDiv")]
        public void MissingConversionFromFormattableStringToIFormattable()
        {
            var text =
@"namespace System.Runtime.CompilerServices
{
    public static class FormattableStringFactory
    {
        public static FormattableString Create(string format, params object[] arguments)
        {
            return null;
        }
    }
}

namespace System
{
    public abstract class FormattableString
    {
    }
}

static class C
{
    static void Main()
    {
        System.IFormattable i = $""{""""}"";
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(text).VerifyEmitDiagnostics(
                // (23,33): error CS0029: Cannot implicitly convert type 'FormattableString' to 'IFormattable'
                //         System.IFormattable i = $"{""}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""{""""}""").WithArguments("System.FormattableString", "System.IFormattable").WithLocation(23, 33)
                );
        }
    }
}
