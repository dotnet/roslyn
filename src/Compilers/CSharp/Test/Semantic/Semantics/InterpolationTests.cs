// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                //         Console.WriteLine($"Jenny don\'t change your number { 8675309 // ");
                Diagnostic(ErrorCode.ERR_SingleLineCommentInExpressionHole, "//").WithLocation(5, 71)
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

        [Fact, WorkItem(306, "https://github.com/dotnet/roslyn/issues/306"), WorkItem(308, "https://github.com/dotnet/roslyn/issues/308")]
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
            var verifier = CompileAndVerify(source, new[] { CSharpRef }, expectedOutput: expectedOutput).VerifyDiagnostics();
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

        [Fact, WorkItem(1119878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119878")]
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
        var t = $""{1,(int)1E10}"";
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (5,22): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
                //         var s = $"{1,1E10}";
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1E10").WithArguments("double", "int").WithLocation(5, 22),
                // (5,22): error CS0150: A constant value is expected
                //         var s = $"{1,1E10}";
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1E10").WithLocation(5, 22),
                // (6,22): error CS0221: Constant value '10000000000' cannot be converted to a 'int' (use 'unchecked' syntax to override)
                //         var t = $"{1,(int)1E10}";
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(int)1E10").WithArguments("10000000000", "int").WithLocation(6, 22),
                // (6,22): error CS0150: A constant value is expected
                //         var t = $"{1,(int)1E10}";
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int)1E10").WithLocation(6, 22)
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
            CreateCompilationWithMscorlib40(source).VerifyEmitDiagnostics(
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
                source, references: new[] { MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, SystemCoreRef_v4_0_30319_17929 }, expectedOutput: "Hello, world!", targetFramework: TargetFramework.Empty);
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [WorkItem(1097388, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097388")]
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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

        [WorkItem(1097428, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097428")]
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
            CreateEmptyCompilation(text, options: TestOptions.DebugExe)
            .VerifyEmitDiagnostics(new CodeAnalysis.Emit.EmitOptions(runtimeMetadataVersion: "x.y"),
                // (15,21): error CS0117: 'string' does not contain a definition for 'Format'
                //             var s = $"X = { 1 } ";
                Diagnostic(ErrorCode.ERR_NoSuchMember, @"$""X = { 1 } """).WithArguments("string", "Format").WithLocation(15, 21)
            );
        }

        [WorkItem(1097428, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097428")]
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
            CreateEmptyCompilation(text, options: TestOptions.DebugExe)
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
            var comp = CreateEmptyCompilation(text, options: Test.Utilities.TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
            var compilation = CompileAndVerify(comp, verify: Verification.Fails);
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

        [WorkItem(1097386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097386")]
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
            CreateCompilation(text).VerifyDiagnostics(
                // (6,40): error CS8087: A '}' character may only be escaped by doubling '}}' in an interpolated string.
                //         var x = $"{ Math.Abs(value: 1):\}";
                Diagnostic(ErrorCode.ERR_EscapedCurly, @"\").WithArguments("}").WithLocation(6, 40),
                // (6,40): error CS1009: Unrecognized escape sequence
                //         var x = $"{ Math.Abs(value: 1):\}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\}").WithLocation(6, 40)
                );
        }

        [WorkItem(1097941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097941")]
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

        [WorkItem(1097386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097386")]
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
            CreateCompilation(text).VerifyDiagnostics(
                // (6,18): error CS8076: Missing close delimiter '}' for interpolated expression started with '{'.
                //         var x = $"{ Math.Abs(value: 1):}}";
                Diagnostic(ErrorCode.ERR_UnclosedExpressionHole, @"""{").WithLocation(6, 18)
                );
        }

        [WorkItem(1099105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099105")]
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

        [WorkItem(1097386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097386")]
        [Fact]
        public void Dynamic01()
        {
            var text =
@"class C
{
    const dynamic a = a;
    string s = $""{0,a}"";
}";
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
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

        [WorkItem(1099238, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1099238")]
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
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,46): error CS1009: Unrecognized escape sequence
                //         Expression<Func<string>> e = () => $"\u1{0:\u2}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u1").WithLocation(8, 46),
                // (8,52): error CS1009: Unrecognized escape sequence
                //         Expression<Func<string>> e = () => $"\u1{0:\u2}";
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u2").WithLocation(8, 52)
                );
        }

        [Fact, WorkItem(1098612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1098612")]
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
            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyEmitDiagnostics(
                // (23,33): error CS0029: Cannot implicitly convert type 'FormattableString' to 'IFormattable'
                //         System.IFormattable i = $"{""}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"$""{""""}""").WithArguments("System.FormattableString", "System.IFormattable").WithLocation(23, 33)
                );
        }

        // PROTOTYPE(interp-string): Define how these are represented in IOperation
        private string GetInterpolatedStringHandlerDefinition(bool includeSpanOverloads, bool useDefaultParameters, bool useBoolReturns, string returnExpression = null)
        {
            Debug.Assert(returnExpression == null || useBoolReturns);

            var builder = new StringBuilder();
            builder.AppendLine(@"
namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();");

            appendSignature("AppendLiteral(string s)");
            appendBody(includeValue: false, includeAlignment: false, includeFormat: false, isSpan: false);

            if (useDefaultParameters)
            {
                appendSignature("AppendFormatted<T>(T value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
                appendSignature("AppendFormatted(object value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
                appendSignature("AppendFormatted(string value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
            }
            else
            {
                appendNonDefaultVariantsWithGenericAndType("T", "<T>");
                appendNonDefaultVariantsWithGenericAndType("object", generic: null);
                appendNonDefaultVariantsWithGenericAndType("string", generic: null);
            }

            if (includeSpanOverloads)
            {
                if (useDefaultParameters)
                {
                    appendSignature("AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string format = null)");
                    appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: true);
                }
                else
                {
                    appendNonDefaultVariantsWithGenericAndType("ReadOnlySpan<char>", generic: null, isSpan: true);
                }
            }

            builder.Append(@"
    }
}");
            return builder.ToString();

            void appendBody(bool includeValue, bool includeAlignment, bool includeFormat, bool isSpan)
            {
                if (includeValue)
                {
                    builder.Append($@"
        {{
            _builder.Append(""value:"");
            _builder.Append(value{(isSpan ? "" : "?")}.ToString());");
                }
                else
                {
                    builder.Append(@"
        {
            _builder.Append(s);");
                }

                if (includeAlignment)
                {
                    builder.Append(@"
            _builder.Append("",alignment:"");
            _builder.Append(alignment);");
                }

                if (includeFormat)
                {
                    builder.Append(@"
            _builder.Append("":format:"");
            _builder.Append(format);");
                }

                builder.Append(@"
            _builder.AppendLine();");

                if (useBoolReturns)
                {
                    builder.Append($@"
            return {returnExpression ?? "true"};");
                }

                builder.AppendLine(@"
        }");
            }

            void appendSignature(string nameAndParams)
            {
                builder.Append(@$"
        public {(useBoolReturns ? "bool" : "void")} {nameAndParams}");
            }

            void appendNonDefaultVariantsWithGenericAndType(string type, string generic, bool isSpan = false)
            {
                appendSignature($"AppendFormatted{generic}({type} value)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: false, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, int alignment)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: false, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, string format)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: true, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, int alignment, string format)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan);
            }
        }

        [ConditionalTheory(typeof(NoIOperationValidation))]
        [CombinatorialData]
        public void InterpolatedStringHandler_OverloadsAndBoolReturns(bool useDefaultParameters, bool useBoolReturns)
        {
            var source =
@"int a = 1;
System.Console.WriteLine($""base{a}{a,1}{a:X}{a,2:Y}"");";

            string interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters, useBoolReturns);

            string expectedOutput = useDefaultParameters ?
@"base
value:1,alignment:0:format:
value:1,alignment:1:format:
value:1,alignment:0:format:X
value:1,alignment:2:format:Y" :
@"base
value:1
value:1,alignment:1
value:1:format:X
value:1,alignment:2:format:Y";

            string expectedIl = getIl();

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: expectedOutput);
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

            var comp1 = CreateCompilation(interpolatedStringBuilder);

            foreach (var reference in new[] { comp1.EmitToImageReference(), comp1.ToMetadataReference() })
            {
                var comp2 = CreateCompilation(source, new[] { reference });
                verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput);
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
            }

            string getIl() => (useDefaultParameters, useBoolReturns) switch
            {
                (useDefaultParameters: false, useBoolReturns: false) => @"
{
  // Code size       80 (0x50)
  .maxstack  4
  .locals init (int V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.4
  IL_0005:  ldc.i4.4
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldstr      ""base""
  IL_0012:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldloc.0
  IL_001a:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
  IL_001f:  ldloca.s   V_1
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.1
  IL_0023:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int)""
  IL_0028:  ldloca.s   V_1
  IL_002a:  ldloc.0
  IL_002b:  ldstr      ""X""
  IL_0030:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, string)""
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldloc.0
  IL_0038:  ldc.i4.2
  IL_0039:  ldstr      ""Y""
  IL_003e:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_0043:  ldloca.s   V_1
  IL_0045:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_004a:  call       ""void System.Console.WriteLine(string)""
  IL_004f:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: false) => @"
{
  // Code size       84 (0x54)
  .maxstack  4
  .locals init (int V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.4
  IL_0005:  ldc.i4.4
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldstr      ""base""
  IL_0012:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.0
  IL_001b:  ldnull
  IL_001c:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_0021:  ldloca.s   V_1
  IL_0023:  ldloc.0
  IL_0024:  ldc.i4.1
  IL_0025:  ldnull
  IL_0026:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_002b:  ldloca.s   V_1
  IL_002d:  ldloc.0
  IL_002e:  ldc.i4.0
  IL_002f:  ldstr      ""X""
  IL_0034:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_0039:  ldloca.s   V_1
  IL_003b:  ldloc.0
  IL_003c:  ldc.i4.2
  IL_003d:  ldstr      ""Y""
  IL_0042:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_0047:  ldloca.s   V_1
  IL_0049:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_004e:  call       ""void System.Console.WriteLine(string)""
  IL_0053:  ret
}
",
                (useDefaultParameters: false, useBoolReturns: true) => @"
{
  // Code size       92 (0x5c)
  .maxstack  4
  .locals init (int V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.4
  IL_0005:  ldc.i4.4
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldstr      ""base""
  IL_0012:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0017:  brfalse.s  IL_004d
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldloc.0
  IL_001c:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
  IL_0021:  brfalse.s  IL_004d
  IL_0023:  ldloca.s   V_1
  IL_0025:  ldloc.0
  IL_0026:  ldc.i4.1
  IL_0027:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int)""
  IL_002c:  brfalse.s  IL_004d
  IL_002e:  ldloca.s   V_1
  IL_0030:  ldloc.0
  IL_0031:  ldstr      ""X""
  IL_0036:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, string)""
  IL_003b:  brfalse.s  IL_004d
  IL_003d:  ldloca.s   V_1
  IL_003f:  ldloc.0
  IL_0040:  ldc.i4.2
  IL_0041:  ldstr      ""Y""
  IL_0046:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_004b:  br.s       IL_004e
  IL_004d:  ldc.i4.0
  IL_004e:  pop
  IL_004f:  ldloca.s   V_1
  IL_0051:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0056:  call       ""void System.Console.WriteLine(string)""
  IL_005b:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: true) => @"
{
  // Code size       96 (0x60)
  .maxstack  4
  .locals init (int V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.4
  IL_0005:  ldc.i4.4
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldstr      ""base""
  IL_0012:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0017:  brfalse.s  IL_0051
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldloc.0
  IL_001c:  ldc.i4.0
  IL_001d:  ldnull
  IL_001e:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_0023:  brfalse.s  IL_0051
  IL_0025:  ldloca.s   V_1
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.1
  IL_0029:  ldnull
  IL_002a:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_002f:  brfalse.s  IL_0051
  IL_0031:  ldloca.s   V_1
  IL_0033:  ldloc.0
  IL_0034:  ldc.i4.0
  IL_0035:  ldstr      ""X""
  IL_003a:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_003f:  brfalse.s  IL_0051
  IL_0041:  ldloca.s   V_1
  IL_0043:  ldloc.0
  IL_0044:  ldc.i4.2
  IL_0045:  ldstr      ""Y""
  IL_004a:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int, int, string)""
  IL_004f:  br.s       IL_0052
  IL_0051:  ldc.i4.0
  IL_0052:  pop
  IL_0053:  ldloca.s   V_1
  IL_0055:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_005a:  call       ""void System.Console.WriteLine(string)""
  IL_005f:  ret
}
",
            };
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void UseOfSpanInInterpolationHole_CSharp9()
        {
            var source = @"
using System;
ReadOnlySpan<char> span = stackalloc char[1];
Console.WriteLine($""{span}"");";

            var comp = CreateCompilation(new[] { source, GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false) }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,22): error CS8652: The feature 'interpolated string handlers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // Console.WriteLine($"{span}");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "span").WithArguments("interpolated string handlers").WithLocation(4, 22)
                );
        }

        [ConditionalTheory(typeof(MonoOrCoreClrOnly), typeof(NoIOperationValidation))]
        [CombinatorialData]
        public void UseOfSpanInInterpolationHole(bool useDefaultParameters, bool useBoolReturns)
        {
            var source =
@"
using System;
ReadOnlySpan<char> a = ""1"";
System.Console.WriteLine($""base{a}{a,1}{a:X}{a,2:Y}"");";

            string interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters, useBoolReturns);

            string expectedOutput = useDefaultParameters ?
@"base
value:1,alignment:0:format:
value:1,alignment:1:format:
value:1,alignment:0:format:X
value:1,alignment:2:format:Y" :
@"base
value:1
value:1,alignment:1
value:1:format:X
value:1,alignment:2:format:Y";

            string expectedIl = getIl();

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: expectedOutput, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

            var comp1 = CreateCompilation(interpolatedStringBuilder, targetFramework: TargetFramework.NetCoreApp);

            foreach (var reference in new[] { comp1.EmitToImageReference(), comp1.ToMetadataReference() })
            {
                var comp2 = CreateCompilation(source, new[] { reference }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
                verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput);
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
            }

            string getIl() => (useDefaultParameters, useBoolReturns) switch
            {
                (useDefaultParameters: false, useBoolReturns: false) => @"
{
  // Code size       89 (0x59)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.4
  IL_000e:  ldc.i4.4
  IL_000f:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldstr      ""base""
  IL_001b:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.0
  IL_0023:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>)""
  IL_0028:  ldloca.s   V_1
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.1
  IL_002c:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int)""
  IL_0031:  ldloca.s   V_1
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""X""
  IL_0039:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, string)""
  IL_003e:  ldloca.s   V_1
  IL_0040:  ldloc.0
  IL_0041:  ldc.i4.2
  IL_0042:  ldstr      ""Y""
  IL_0047:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_004c:  ldloca.s   V_1
  IL_004e:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0053:  call       ""void System.Console.WriteLine(string)""
  IL_0058:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: false) => @"
{
  // Code size       93 (0x5d)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.4
  IL_000e:  ldc.i4.4
  IL_000f:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldstr      ""base""
  IL_001b:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0020:  ldloca.s   V_1
  IL_0022:  ldloc.0
  IL_0023:  ldc.i4.0
  IL_0024:  ldnull
  IL_0025:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_002a:  ldloca.s   V_1
  IL_002c:  ldloc.0
  IL_002d:  ldc.i4.1
  IL_002e:  ldnull
  IL_002f:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0034:  ldloca.s   V_1
  IL_0036:  ldloc.0
  IL_0037:  ldc.i4.0
  IL_0038:  ldstr      ""X""
  IL_003d:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0042:  ldloca.s   V_1
  IL_0044:  ldloc.0
  IL_0045:  ldc.i4.2
  IL_0046:  ldstr      ""Y""
  IL_004b:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0050:  ldloca.s   V_1
  IL_0052:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0057:  call       ""void System.Console.WriteLine(string)""
  IL_005c:  ret
}
",
                (useDefaultParameters: false, useBoolReturns: true) => @"
{
  // Code size      101 (0x65)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.4
  IL_000e:  ldc.i4.4
  IL_000f:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldstr      ""base""
  IL_001b:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0020:  brfalse.s  IL_0056
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldloc.0
  IL_0025:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>)""
  IL_002a:  brfalse.s  IL_0056
  IL_002c:  ldloca.s   V_1
  IL_002e:  ldloc.0
  IL_002f:  ldc.i4.1
  IL_0030:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int)""
  IL_0035:  brfalse.s  IL_0056
  IL_0037:  ldloca.s   V_1
  IL_0039:  ldloc.0
  IL_003a:  ldstr      ""X""
  IL_003f:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, string)""
  IL_0044:  brfalse.s  IL_0056
  IL_0046:  ldloca.s   V_1
  IL_0048:  ldloc.0
  IL_0049:  ldc.i4.2
  IL_004a:  ldstr      ""Y""
  IL_004f:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0054:  br.s       IL_0057
  IL_0056:  ldc.i4.0
  IL_0057:  pop
  IL_0058:  ldloca.s   V_1
  IL_005a:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: true) => @"
{
  // Code size      105 (0x69)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldc.i4.4
  IL_000e:  ldc.i4.4
  IL_000f:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0014:  ldloca.s   V_1
  IL_0016:  ldstr      ""base""
  IL_001b:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
  IL_0020:  brfalse.s  IL_005a
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.0
  IL_0026:  ldnull
  IL_0027:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_002c:  brfalse.s  IL_005a
  IL_002e:  ldloca.s   V_1
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.1
  IL_0032:  ldnull
  IL_0033:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0038:  brfalse.s  IL_005a
  IL_003a:  ldloca.s   V_1
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.0
  IL_003e:  ldstr      ""X""
  IL_0043:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0048:  brfalse.s  IL_005a
  IL_004a:  ldloca.s   V_1
  IL_004c:  ldloc.0
  IL_004d:  ldc.i4.2
  IL_004e:  ldstr      ""Y""
  IL_0053:  call       ""bool System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>, int, string)""
  IL_0058:  br.s       IL_005b
  IL_005a:  ldc.i4.0
  IL_005b:  pop
  IL_005c:  ldloca.s   V_1
  IL_005e:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0063:  call       ""void System.Console.WriteLine(string)""
  IL_0068:  ret
}
",
            };
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void BoolReturns_ShortCircuit()
        {
            var source = @"
using System;
int a = 1;
Console.Write($""base{Throw()}{a = 2}"");
Console.WriteLine(a);
string Throw() => throw new Exception();";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: true, returnExpression: "false");

            CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
base
1");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void AwaitInHoles_UsesFormat()
        {
            // PROTOTYPE(interp-string): We could make this case use the builder as well by evaluating the holes ahead of time. For DefaultInterpolatedStringHandler,
            // we know that the framework is never going to ship a version that short circuits, so it would be a valid optimization for us to make.
            var source = @"
using System;
using System.Threading.Tasks;

Console.WriteLine($""base{await Hole()}"");
Task<int> Hole() => Task.FromResult(1);";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"base1");

            verifier.VerifyIL("<Program>$.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext", @"
{
  // Code size      164 (0xa4)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003e
    IL_000a:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__Hole|0_0()""
    IL_000f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0014:  stloc.2
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_001c:  brtrue.s   IL_005a
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  dup
    IL_0021:  stloc.0
    IL_0022:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0027:  ldarg.0
    IL_0028:  ldloc.2
    IL_0029:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_002e:  ldarg.0
    IL_002f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0034:  ldloca.s   V_2
    IL_0036:  ldarg.0
    IL_0037:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_003c:  leave.s    IL_00a3
    IL_003e:  ldarg.0
    IL_003f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0044:  stloc.2
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_004b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0051:  ldarg.0
    IL_0052:  ldc.i4.m1
    IL_0053:  dup
    IL_0054:  stloc.0
    IL_0055:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_005a:  ldloca.s   V_2
    IL_005c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0061:  stloc.1
    IL_0062:  ldstr      ""base{0}""
    IL_0067:  ldloc.1
    IL_0068:  box        ""int""
    IL_006d:  call       ""string string.Format(string, object)""
    IL_0072:  call       ""void System.Console.WriteLine(string)""
    IL_0077:  leave.s    IL_0090
  }
  catch System.Exception
  {
    IL_0079:  stloc.3
    IL_007a:  ldarg.0
    IL_007b:  ldc.i4.s   -2
    IL_007d:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0082:  ldarg.0
    IL_0083:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0088:  ldloc.3
    IL_0089:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_008e:  leave.s    IL_00a3
  }
  IL_0090:  ldarg.0
  IL_0091:  ldc.i4.s   -2
  IL_0093:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0098:  ldarg.0
  IL_0099:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_009e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00a3:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void NoAwaitInHoles_UsesBuilder()
        {
            var source = @"
using System;
using System.Threading.Tasks;

var hole = await Hole();
Console.WriteLine($""base{hole}"");
Task<int> Hole() => Task.FromResult(1);";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
base
value:1");

            verifier.VerifyIL("<Program>$.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      185 (0xb9)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //hole
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_003e
    IL_000a:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__Hole|0_0()""
    IL_000f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0014:  stloc.2
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_001c:  brtrue.s   IL_005a
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  dup
    IL_0021:  stloc.0
    IL_0022:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0027:  ldarg.0
    IL_0028:  ldloc.2
    IL_0029:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_002e:  ldarg.0
    IL_002f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0034:  ldloca.s   V_2
    IL_0036:  ldarg.0
    IL_0037:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_003c:  leave.s    IL_00b8
    IL_003e:  ldarg.0
    IL_003f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0044:  stloc.2
    IL_0045:  ldarg.0
    IL_0046:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_004b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0051:  ldarg.0
    IL_0052:  ldc.i4.m1
    IL_0053:  dup
    IL_0054:  stloc.0
    IL_0055:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_005a:  ldloca.s   V_2
    IL_005c:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0061:  stloc.1
    IL_0062:  ldc.i4.4
    IL_0063:  ldc.i4.1
    IL_0064:  newobj     ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
    IL_0069:  stloc.3
    IL_006a:  ldloca.s   V_3
    IL_006c:  ldstr      ""base""
    IL_0071:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
    IL_0076:  ldloca.s   V_3
    IL_0078:  ldloc.1
    IL_0079:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
    IL_007e:  ldloca.s   V_3
    IL_0080:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
    IL_0085:  call       ""void System.Console.WriteLine(string)""
    IL_008a:  leave.s    IL_00a5
  }
  catch System.Exception
  {
    IL_008c:  stloc.s    V_4
    IL_008e:  ldarg.0
    IL_008f:  ldc.i4.s   -2
    IL_0091:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0096:  ldarg.0
    IL_0097:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_009c:  ldloc.s    V_4
    IL_009e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00a3:  leave.s    IL_00b8
  }
  IL_00a5:  ldarg.0
  IL_00a6:  ldc.i4.s   -2
  IL_00a8:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_00ad:  ldarg.0
  IL_00ae:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_00b3:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00b8:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void NoAwaitInHoles_AwaitInExpression_UsesBuilder()
        {
            var source = @"
using System;
using System.Threading.Tasks;

var hole = 2;
Test(await M(1), $""base{hole}"", await M(3));
void Test(int i1, string s, int i2) => Console.WriteLine(s);
Task<int> M(int i) 
{
    Console.WriteLine(i);
    return Task.FromResult(1);
}";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
1
3
base
value:2");

            verifier.VerifyIL("<Program>$.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      328 (0x148)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0050
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00dc
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4.2
    IL_0013:  stfld      ""int <Program>$.<<Main>$>d__0.<hole>5__2""
    IL_0018:  ldc.i4.1
    IL_0019:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__M|0_1(int)""
    IL_001e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0023:  stloc.2
    IL_0024:  ldloca.s   V_2
    IL_0026:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002b:  brtrue.s   IL_006c
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4.0
    IL_002f:  dup
    IL_0030:  stloc.0
    IL_0031:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0036:  ldarg.0
    IL_0037:  ldloc.2
    IL_0038:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_003d:  ldarg.0
    IL_003e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0043:  ldloca.s   V_2
    IL_0045:  ldarg.0
    IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_004b:  leave      IL_0147
    IL_0050:  ldarg.0
    IL_0051:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0056:  stloc.2
    IL_0057:  ldarg.0
    IL_0058:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_005d:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0063:  ldarg.0
    IL_0064:  ldc.i4.m1
    IL_0065:  dup
    IL_0066:  stloc.0
    IL_0067:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_006c:  ldarg.0
    IL_006d:  ldloca.s   V_2
    IL_006f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0074:  stfld      ""int <Program>$.<<Main>$>d__0.<>7__wrap2""
    IL_0079:  ldarg.0
    IL_007a:  ldc.i4.4
    IL_007b:  ldc.i4.1
    IL_007c:  newobj     ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
    IL_0081:  stloc.3
    IL_0082:  ldloca.s   V_3
    IL_0084:  ldstr      ""base""
    IL_0089:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)""
    IL_008e:  ldloca.s   V_3
    IL_0090:  ldarg.0
    IL_0091:  ldfld      ""int <Program>$.<<Main>$>d__0.<hole>5__2""
    IL_0096:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
    IL_009b:  ldloca.s   V_3
    IL_009d:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
    IL_00a2:  stfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_00a7:  ldc.i4.3
    IL_00a8:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__M|0_1(int)""
    IL_00ad:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00b2:  stloc.2
    IL_00b3:  ldloca.s   V_2
    IL_00b5:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00ba:  brtrue.s   IL_00f8
    IL_00bc:  ldarg.0
    IL_00bd:  ldc.i4.1
    IL_00be:  dup
    IL_00bf:  stloc.0
    IL_00c0:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_00c5:  ldarg.0
    IL_00c6:  ldloc.2
    IL_00c7:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_00cc:  ldarg.0
    IL_00cd:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_00d2:  ldloca.s   V_2
    IL_00d4:  ldarg.0
    IL_00d5:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_00da:  leave.s    IL_0147
    IL_00dc:  ldarg.0
    IL_00dd:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_00e2:  stloc.2
    IL_00e3:  ldarg.0
    IL_00e4:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_00e9:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_00ef:  ldarg.0
    IL_00f0:  ldc.i4.m1
    IL_00f1:  dup
    IL_00f2:  stloc.0
    IL_00f3:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_00f8:  ldloca.s   V_2
    IL_00fa:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_00ff:  stloc.1
    IL_0100:  ldarg.0
    IL_0101:  ldfld      ""int <Program>$.<<Main>$>d__0.<>7__wrap2""
    IL_0106:  ldarg.0
    IL_0107:  ldfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_010c:  ldloc.1
    IL_010d:  call       ""void <Program>$.<<Main>$>g__Test|0_0(int, string, int)""
    IL_0112:  ldarg.0
    IL_0113:  ldnull
    IL_0114:  stfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_0119:  leave.s    IL_0134
  }
  catch System.Exception
  {
    IL_011b:  stloc.s    V_4
    IL_011d:  ldarg.0
    IL_011e:  ldc.i4.s   -2
    IL_0120:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0125:  ldarg.0
    IL_0126:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_012b:  ldloc.s    V_4
    IL_012d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0132:  leave.s    IL_0147
  }
  IL_0134:  ldarg.0
  IL_0135:  ldc.i4.s   -2
  IL_0137:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_013c:  ldarg.0
  IL_013d:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_0142:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0147:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MissingCreate_01()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public override string ToString() => throw null;
        public void Dispose() => throw null;
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS1729: 'DefaultInterpolatedStringHandler' does not contain a constructor that takes 2 arguments
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "2").WithLocation(1, 5)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MissingCreate_02()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public DefaultInterpolatedStringHandler(int literalLength) => throw null;
        public override string ToString() => throw null;
        public void Dispose() => throw null;
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS1729: 'DefaultInterpolatedStringHandler' does not contain a constructor that takes 2 arguments
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "2").WithLocation(1, 5)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MissingCreate_03()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public DefaultInterpolatedStringHandler(ref int literalLength, int formattedCount) => throw null;
        public override string ToString() => throw null;
        public void Dispose() => throw null;
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            // PROTOTYPE(interp-string): Better error here?
            comp.VerifyDiagnostics(
                // (1,5): error CS1620: Argument 1 must be passed with the 'ref' keyword
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BadArgRef, @"$""{(object)1}""").WithArguments("1", "ref").WithLocation(1, 5)
            );
        }

        [ConditionalTheory(typeof(NoIOperationValidation))]
        [InlineData(null)]
        [InlineData("public string ToStringAndClear(int literalLength) => throw null;")]
        [InlineData("public void ToStringAndClear() => throw null;")]
        [InlineData("public static string ToStringAndClear() => throw null;")]
        public void MissingWellKnownMethod_ToStringAndClear(string toStringAndClearMethod)
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount) => throw null;
        " + toStringAndClearMethod + @"
        public override string ToString() => throw null;
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (1,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "ToStringAndClear").WithLocation(1, 5)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ObsoleteCreateMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        [System.Obsolete(""Constructor is obsolete"", error: true)]
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS0619: 'DefaultInterpolatedStringHandler.DefaultInterpolatedStringHandler(int, int)' is obsolete: 'Constructor is obsolete'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.DefaultInterpolatedStringHandler(int, int)", "Constructor is obsolete").WithLocation(1, 5)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ObsoleteAppendLiteralMethod()
        {
            var code = @"_ = $""base{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        [System.Obsolete(""AppendLiteral is obsolete"", error: true)]
        public void AppendLiteral(string value) => throw null;
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,7): error CS0619: 'DefaultInterpolatedStringHandler.AppendLiteral(string)' is obsolete: 'AppendLiteral is obsolete'
                // _ = $"base{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "base").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)", "AppendLiteral is obsolete").WithLocation(1, 7)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ObsoleteAppendFormattedMethod()
        {
            var code = @"_ = $""base{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct DefaultInterpolatedStringHandler
    {
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void AppendLiteral(string value) => throw null;
        [System.Obsolete(""AppendFormatted is obsolete"", error: true)]
        public void AppendFormatted<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,11): error CS0619: 'DefaultInterpolatedStringHandler.AppendFormatted<T>(T, int, string)' is obsolete: 'AppendFormatted is obsolete'
                // _ = $"base{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "{(object)1}").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<T>(T, int, string)", "AppendFormatted is obsolete").WithLocation(1, 11)
            );
        }

        private const string UnmanagedCallersOnlyIl = @"
.class public auto ansi sealed beforefieldinit System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 40 00 00 00 01 00 54 02 09 49 6e 68 65 72
        69 74 65 64 00
    )
    .field public class [mscorlib]System.Type[] CallConvs
    .field public string EntryPoint
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    }
}";

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void UnmanagedCallersOnlyAppendFormattedMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.DefaultInterpolatedStringHandler
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 52 54 79 70 65 73 20 77 69 74 68 20 65 6d
        62 65 64 64 65 64 20 72 65 66 65 72 65 6e 63 65
        73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f 72
        74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72 73
        69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d 70
        69 6c 65 72 2e 01 00 00
    )
    .pack 0
    .size 1

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 literalLength,
            int32 formattedCount
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void Dispose () cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig virtual 
        instance string ToString () cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void AppendLiteral (
            string 'value'
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void AppendFormatted<T> (
            !!T hole,
            [opt] int32 'alignment',
            [opt] string format
        ) cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .param [2] = int32(0)
        .param [3] = nullref
        ldnull
        throw
    }
}
";

            var comp = CreateCompilationWithIL(code, ilSource: interpolatedStringBuilder + UnmanagedCallersOnlyIl);
            comp.VerifyDiagnostics(
                // (1,7): error CS0570: 'DefaultInterpolatedStringHandler.AppendFormatted<T>(T, int, string)' is not supported by the language
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BindToBogus, "{(object)1}").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<T>(T, int, string)").WithLocation(1, 7)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void UnmanagedCallersOnlyToStringMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"

.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.DefaultInterpolatedStringHandler
    extends [mscorlib]System.ValueType
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string, bool) = (
        01 00 52 54 79 70 65 73 20 77 69 74 68 20 65 6d
        62 65 64 64 65 64 20 72 65 66 65 72 65 6e 63 65
        73 20 61 72 65 20 6e 6f 74 20 73 75 70 70 6f 72
        74 65 64 20 69 6e 20 74 68 69 73 20 76 65 72 73
        69 6f 6e 20 6f 66 20 79 6f 75 72 20 63 6f 6d 70
        69 6c 65 72 2e 01 00 00
    )
    .pack 0
    .size 1

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 literalLength,
            int32 formattedCount
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig instance string ToStringAndClear () cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .method public hidebysig 
        instance void AppendLiteral (
            string 'value'
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void AppendFormatted<T> (
            !!T hole,
            [opt] int32 'alignment',
            [opt] string format
        ) cil managed 
    {
        .param [2] = int32(0)
        .param [3] = nullref
        ldnull
        throw
    }
}
";

            var comp = CreateCompilationWithIL(code, ilSource: interpolatedStringBuilder + UnmanagedCallersOnlyIl);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (1,5): error CS0570: 'DefaultInterpolatedStringHandler.ToStringAndClear()' is not supported by the language
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BindToBogus, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()").WithLocation(1, 5)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void UnsupportedArgumentType()
        {
            var source = @"
unsafe
{
    int* i = null;
    var s = new S();
    _ = $""{i}{s}"";
}
ref struct S
{
}";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters: true, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,11): error CS0306: The type 'int*' may not be used as a type argument
                //     _ = $"{i}{s}";
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "{i}").WithArguments("int*").WithLocation(6, 11),
                // (6,14): error CS0306: The type 'S' may not be used as a type argument
                //     _ = $"{i}{s}";
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "{s}").WithArguments("S").WithLocation(6, 14)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TargetTypedInterpolationHoles()
        {
            var source = @"
bool b = true;
System.Console.WriteLine($""{b switch { true => 1, false => null }}{(!b ? null : 2)}{default}{null}"");";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
value:1
value:2
value:
value:");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (bool V_0, //b
                object V_1,
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_2
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.4
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloc.0
  IL_000c:  brfalse.s  IL_0017
  IL_000e:  ldc.i4.1
  IL_000f:  box        ""int""
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0019
  IL_0017:  ldnull
  IL_0018:  stloc.1
  IL_0019:  ldloca.s   V_2
  IL_001b:  ldloc.1
  IL_001c:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)""
  IL_0021:  ldloca.s   V_2
  IL_0023:  ldloc.0
  IL_0024:  brfalse.s  IL_002e
  IL_0026:  ldc.i4.2
  IL_0027:  box        ""int""
  IL_002c:  br.s       IL_002f
  IL_002e:  ldnull
  IL_002f:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)""
  IL_0034:  ldloca.s   V_2
  IL_0036:  ldnull
  IL_0037:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(string)""
  IL_003c:  ldloca.s   V_2
  IL_003e:  ldnull
  IL_003f:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(string)""
  IL_0044:  ldloca.s   V_2
  IL_0046:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TargetTypedInterpolationHoles_Errors()
        {
            var source = @"System.Console.WriteLine($""{(null, default)}{new()}"");";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);
            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,29): error CS1503: Argument 1: cannot convert from '(<null>, default)' to 'object'
                // System.Console.WriteLine($"{(null, default)}");
                Diagnostic(ErrorCode.ERR_BadArgType, "(null, default)").WithArguments("1", "(<null>, default)", "object").WithLocation(1, 29),
                // (1,29): error CS8652: The feature 'interpolated string handlers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // System.Console.WriteLine($"{(null, default)}");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(null, default)").WithArguments("interpolated string handlers").WithLocation(1, 29),
                // (1,46): error CS1729: 'string' does not contain a constructor that takes 0 arguments
                // System.Console.WriteLine($"{(null, default)}{new()}");
                // PROTOTYPE(interp-string): This is technically a break. Should we special case this?
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "new()").WithArguments("string", "0").WithLocation(1, 46)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void RefTernary()
        {
            var source = @"
bool b = true;
int i = 1;
System.Console.WriteLine($""{(!b ? ref i : ref i)}"");";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"value:1");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void NestedInterpolatedStrings()
        {
            // PROTOTYPE(interp-string): Should we notice the nested string and just treat it as being concated?
            var source = @"
int i = 1;
System.Console.WriteLine($""{$""{i}""}"");";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"value:value:1");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (int V_0, //i
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1,
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  ldc.i4.0
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_000b:  ldloca.s   V_1
  IL_000d:  ldloca.s   V_2
  IL_000f:  ldc.i4.0
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_2
  IL_0018:  ldloc.0
  IL_0019:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
  IL_001e:  ldloca.s   V_2
  IL_0020:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0025:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(string)""
  IL_002a:  ldloca.s   V_1
  IL_002c:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ExceptionFilter_01()
        {
            var source = @"
using System;

int i = 1;
try
{
    Console.WriteLine(""Starting try"");
    throw new MyException { Prop = i };
}
// Test DefaultInterpolatedStringHandler renders specially, so we're actually comparing to ""value:Prop"" plus some whitespace
catch (MyException e) when (e.ToString() == $""{i}"".Trim())
{
    Console.WriteLine(""Caught"");
}

class MyException : Exception
{
    public int Prop { get; set; }
    public override string ToString() => ""value:"" + Prop.ToString();
}";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Starting try
Caught");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       95 (0x5f)
  .maxstack  4
  .locals init (int V_0, //i
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldstr      ""Starting try""
    IL_0007:  call       ""void System.Console.WriteLine(string)""
    IL_000c:  newobj     ""MyException..ctor()""
    IL_0011:  dup
    IL_0012:  ldloc.0
    IL_0013:  callvirt   ""void MyException.Prop.set""
    IL_0018:  throw
  }
  filter
  {
    IL_0019:  isinst     ""MyException""
    IL_001e:  dup
    IL_001f:  brtrue.s   IL_0025
    IL_0021:  pop
    IL_0022:  ldc.i4.0
    IL_0023:  br.s       IL_004f
    IL_0025:  callvirt   ""string object.ToString()""
    IL_002a:  ldloca.s   V_1
    IL_002c:  ldc.i4.0
    IL_002d:  ldc.i4.1
    IL_002e:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
    IL_0033:  ldloca.s   V_1
    IL_0035:  ldloc.0
    IL_0036:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<int>(int)""
    IL_003b:  ldloca.s   V_1
    IL_003d:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
    IL_0042:  callvirt   ""string string.Trim()""
    IL_0047:  call       ""bool string.op_Equality(string, string)""
    IL_004c:  ldc.i4.0
    IL_004d:  cgt.un
    IL_004f:  endfilter
  }  // end filter
  {  // handler
    IL_0051:  pop
    IL_0052:  ldstr      ""Caught""
    IL_0057:  call       ""void System.Console.WriteLine(string)""
    IL_005c:  leave.s    IL_005e
  }
  IL_005e:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly), typeof(NoIOperationValidation))]
        public void ExceptionFilter_02()
        {
            var source = @"
using System;

ReadOnlySpan<char> s = new char[] { 'i' };
try
{
    Console.WriteLine(""Starting try"");
    throw new MyException { Prop = s.ToString() };
}
// Test DefaultInterpolatedStringHandler renders specially, so we're actually comparing to ""value:Prop"" plus some whitespace
catch (MyException e) when (e.ToString() == $""{s}"".Trim())
{
    Console.WriteLine(""Caught"");
}

class MyException : Exception
{
    public string Prop { get; set; }
    public override string ToString() => ""value:"" + Prop.ToString();
}";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp, expectedOutput: @"
Starting try
Caught");


            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size      122 (0x7a)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //s
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""char""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.s   105
  IL_000a:  stelem.i2
  IL_000b:  call       ""System.ReadOnlySpan<char> System.ReadOnlySpan<char>.op_Implicit(char[])""
  IL_0010:  stloc.0
  .try
  {
    IL_0011:  ldstr      ""Starting try""
    IL_0016:  call       ""void System.Console.WriteLine(string)""
    IL_001b:  newobj     ""MyException..ctor()""
    IL_0020:  dup
    IL_0021:  ldloca.s   V_0
    IL_0023:  constrained. ""System.ReadOnlySpan<char>""
    IL_0029:  callvirt   ""string object.ToString()""
    IL_002e:  callvirt   ""void MyException.Prop.set""
    IL_0033:  throw
  }
  filter
  {
    IL_0034:  isinst     ""MyException""
    IL_0039:  dup
    IL_003a:  brtrue.s   IL_0040
    IL_003c:  pop
    IL_003d:  ldc.i4.0
    IL_003e:  br.s       IL_006a
    IL_0040:  callvirt   ""string object.ToString()""
    IL_0045:  ldloca.s   V_1
    IL_0047:  ldc.i4.0
    IL_0048:  ldc.i4.1
    IL_0049:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
    IL_004e:  ldloca.s   V_1
    IL_0050:  ldloc.0
    IL_0051:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>)""
    IL_0056:  ldloca.s   V_1
    IL_0058:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
    IL_005d:  callvirt   ""string string.Trim()""
    IL_0062:  call       ""bool string.op_Equality(string, string)""
    IL_0067:  ldc.i4.0
    IL_0068:  cgt.un
    IL_006a:  endfilter
  }  // end filter
  {  // handler
    IL_006c:  pop
    IL_006d:  ldstr      ""Caught""
    IL_0072:  call       ""void System.Console.WriteLine(string)""
    IL_0077:  leave.s    IL_0079
  }
  IL_0079:  ret
}
");
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly), typeof(NoIOperationValidation))]
        public void ImplicitUserDefinedConversionInHole()
        {
            var source = @"
using System;

S s = default;
C c = new C();
Console.WriteLine($""{s}{c}"");

ref struct S
{
    public static implicit operator ReadOnlySpan<char>(S s) => ""S converted"";
}
class C
{
    public static implicit operator ReadOnlySpan<char>(C s) => ""C converted"";
}";

            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder },
                targetFramework: TargetFramework.NetCoreApp,
                parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:S converted
value:C");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       57 (0x39)
  .maxstack  3
  .locals init (S V_0, //s
                C V_1, //c
                System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_2)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  newobj     ""C..ctor()""
  IL_000d:  stloc.1
  IL_000e:  ldloca.s   V_2
  IL_0010:  ldc.i4.0
  IL_0011:  ldc.i4.2
  IL_0012:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0017:  ldloca.s   V_2
  IL_0019:  ldloc.0
  IL_001a:  call       ""System.ReadOnlySpan<char> S.op_Implicit(S)""
  IL_001f:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(System.ReadOnlySpan<char>)""
  IL_0024:  ldloca.s   V_2
  IL_0026:  ldloc.1
  IL_0027:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted<C>(C)""
  IL_002c:  ldloca.s   V_2
  IL_002e:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_0033:  call       ""void System.Console.WriteLine(string)""
  IL_0038:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ExplicitUserDefinedConversionInHole()
        {
            var source = @"
using System;

S s = default;
Console.WriteLine($""{s}"");

ref struct S
{
    public static explicit operator ReadOnlySpan<char>(S s) => ""S converted"";
}
";
            var interpolatedStringBuilder = GetInterpolatedStringHandlerDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,21): error CS0306: The type 'S' may not be used as a type argument
                // Console.WriteLine($"{s}");
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "{s}").WithArguments("S").WithLocation(5, 21)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ImplicitUserDefinedConversionInLiteral()
        {
            var source = @"
using System;

Console.WriteLine($""Text{1}"");

public struct CustomStruct
{
    public static implicit operator CustomStruct(string s) => new CustomStruct { S = s };
    public string S { get; set; }
    public override string ToString() => ""literal:"" + S;
}

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public void AppendLiteral(CustomStruct s) => _builder.AppendLine(s.ToString());
        public void AppendFormatted(object o) => _builder.AppendLine(""value:"" + o.ToString());
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: @"
literal:Text
value:1");
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.4
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldstr      ""Text""
  IL_0010:  call       ""CustomStruct CustomStruct.op_Implicit(string)""
  IL_0015:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(CustomStruct)""
  IL_001a:  ldloca.s   V_0
  IL_001c:  ldc.i4.1
  IL_001d:  box        ""int""
  IL_0022:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)""
  IL_0027:  ldloca.s   V_0
  IL_0029:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ExplicitUserDefinedConversionInLiteral()
        {
            var source = @"
using System;

Console.WriteLine($""Text{1}"");

public struct CustomStruct
{
    public static explicit operator CustomStruct(string s) => new CustomStruct { S = s };
    public string S { get; set; }
    public override string ToString() => ""literal:"" + S;
}

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public void AppendLiteral(CustomStruct s) => _builder.AppendLine(s.ToString());
        public void AppendFormatted(object o) => _builder.AppendLine(""value:"" + o.ToString());
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS1503: Argument 1: cannot convert from 'string' to 'CustomStruct'
                // Console.WriteLine($"Text{1}");
                Diagnostic(ErrorCode.ERR_BadArgType, "Text").WithArguments("1", "string", "CustomStruct").WithLocation(4, 21)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void InvalidBuilderReturnType()
        {
            var source = @"
using System;

Console.WriteLine($""Text{1}"");

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public int AppendLiteral(string s) => 0;
        public int AppendFormatted(object o) => 0;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS9001: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendLiteral(string)' is malformed. It does not return 'void' or 'bool'.
                // Console.WriteLine($"Text{1}");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "Text").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)").WithLocation(4, 21),
                // (4,25): error CS9001: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendFormatted(object)' is malformed. It does not return 'void' or 'bool'.
                // Console.WriteLine($"Text{1}");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed, "{1}").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)").WithLocation(4, 25)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MixedBuilderReturnTypes_01()
        {
            var source = @"
using System;

Console.WriteLine($""Text{1}"");
Console.WriteLine($""{1}Text"");

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public bool AppendLiteral(string s) => true;
        public void AppendFormatted(object o) { }
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,25): error CS9002: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendFormatted(object)' has inconsistent return type. Expected to return 'bool'.
                // Console.WriteLine($"Text{1}");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent, "{1}").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)", "bool").WithLocation(4, 25),
                // (5,24): error CS9002: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendLiteral(string)' has inconsistent return type. Expected to return 'void'.
                // Console.WriteLine($"{1}Text");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent, "Text").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)", "void").WithLocation(5, 24)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MixedBuilderReturnTypes_02()
        {
            var source = @"
using System;

Console.WriteLine($""Text{1}"");
Console.WriteLine($""{1}Text"");

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public void AppendLiteral(string s) { }
        public bool AppendFormatted(object o) => true;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,25): error CS9002: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendFormatted(object)' has inconsistent return type. Expected to return 'void'.
                // Console.WriteLine($"Text{1}");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent, "{1}").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(object)", "void").WithLocation(4, 25),
                // (5,24): error CS9002: Interpolated string handler method 'DefaultInterpolatedStringHandler.AppendLiteral(string)' has inconsistent return type. Expected to return 'bool'.
                // Console.WriteLine($"{1}Text");
                Diagnostic(ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent, "Text").WithArguments("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendLiteral(string)", "bool").WithLocation(5, 24)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MixedBuilderReturnTypes_03()
        {
            var source = @"
using System;

Console.WriteLine($""{1}"");

namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new StringBuilder();
        }
        public string ToStringAndClear() => _builder.ToString();
        public bool AppendLiteral(string s) => true;
        public void AppendFormatted(object o)
        {
            _builder.AppendLine(""value:"" + o.ToString());
        }
    }
}";

            // PROTOTYPE(interp-string): Have a symmetric test for just using AppendLiteral when we have general builder
            // type support (that the compiler won't optimize away to just the literal value)
            CompileAndVerify(source, expectedOutput: "value:1");
        }
    }
}
