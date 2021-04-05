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

        private string GetInterpolatedStringBuilderDefinition(bool includeSpanOverloads, bool useDefaultParameters, bool useBoolReturns, string returnExpression = null)
        {
            Debug.Assert(returnExpression == null || useBoolReturns);

            var builder = new StringBuilder();
            builder.AppendLine(@"
namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int holeCount) => new InterpolatedStringBuilder(baseLength);
        private readonly StringBuilder _builder;
        public InterpolatedStringBuilder(int baseLength)
        {
            _builder = new StringBuilder();
        }
        public override string ToString() => _builder.ToString();
        public void Dispose() => Console.WriteLine(""Disposed"");");

            appendSignature("TryFormatBaseString(string s)");
            appendBody(includeValue: false, includeAlignment: false, includeFormat: false);

            if (useDefaultParameters)
            {
                appendSignature("TryFormatInterpolationHole<T>(T value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true);
                appendSignature("TryFormatInterpolationHole(object value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true);
            }
            else
            {
                appendSignature("TryFormatInterpolationHole<T>(T value)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: false);
                appendSignature("TryFormatInterpolationHole<T>(T value, int alignment)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: false);
                appendSignature("TryFormatInterpolationHole<T>(T value, string format)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: true);
                appendSignature("TryFormatInterpolationHole<T>(T value, int alignment, string format)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true);
                appendSignature("TryFormatInterpolationHole(object value)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: false);
                appendSignature("TryFormatInterpolationHole(object value, int alignment)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: false);
                appendSignature("TryFormatInterpolationHole(object value, string format)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: true);
                appendSignature("TryFormatInterpolationHole(object value, int alignment, string format)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true);
            }

            if (includeSpanOverloads)
            {
                if (useDefaultParameters)
                {
                    appendSignature("TryFormatInterpolationHole(ReadOnlySpan<char> value, int alignment = 0, string format = null)");
                    appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: true);
                }
                else
                {
                    appendSignature("TryFormatInterpolationHole(ReadOnlySpan<char> value)");
                    appendBody(includeValue: true, includeAlignment: false, includeFormat: false, isSpan: true);
                    appendSignature("TryFormatInterpolationHole(ReadOnlySpan<char> value, int alignment)");
                    appendBody(includeValue: true, includeAlignment: true, includeFormat: false, isSpan: true);
                    appendSignature("TryFormatInterpolationHole(ReadOnlySpan<char> value, string format)");
                    appendBody(includeValue: true, includeAlignment: false, includeFormat: true, isSpan: true);
                    appendSignature("TryFormatInterpolationHole(ReadOnlySpan<char> value, int alignment, string format)");
                    appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: true);
                }
            }

            builder.Append(@"
    }
}");
            return builder.ToString();

            void appendBody(bool includeValue, bool includeAlignment, bool includeFormat, bool isSpan = false)
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
        }

        [Theory]
        [CombinatorialData]
        public void InterpolatedStringBuilder_OverloadsAndBoolReturns(bool useDefaultParameters, bool useBoolReturns)
        {
            var source =
@"int a = 1;
System.Console.WriteLine($""base{a}{a,1}{a:X}{a,2:Y}"");";

            string interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters, useBoolReturns);

            string expectedOutput = useDefaultParameters ?
@"Disposed
base
value:1,alignment:0:format:
value:1,alignment:1:format:
value:1,alignment:0:format:X
value:1,alignment:2:format:Y" :
@"Disposed
base
value:1
value:1,alignment:1
value:1:format:X
value:1,alignment:2:format:Y";

            string expectedIl = getIl();

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: expectedOutput);
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

            var comp1 = CreateCompilation(interpolatedStringBuilder);

            foreach (var reference in new[] { comp1.EmitToImageReference(), comp1.EmitToPortableExecutableReference() })
            {
                var comp2 = CreateCompilation(source, new[] { reference });
                verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput);
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
            }

            string getIl() => (useDefaultParameters, useBoolReturns) switch
            {
                (useDefaultParameters: false, useBoolReturns: false) => @"
{
  // Code size       97 (0x61)
  .maxstack  4
  .locals init (int V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.4
    IL_0003:  ldc.i4.4
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    IL_000a:  ldloca.s   V_2
    IL_000c:  ldstr      ""base""
    IL_0011:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_0016:  ldloca.s   V_2
    IL_0018:  ldloc.0
    IL_0019:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int)""
    IL_001e:  ldloca.s   V_2
    IL_0020:  ldloc.0
    IL_0021:  ldc.i4.1
    IL_0022:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int)""
    IL_0027:  ldloca.s   V_2
    IL_0029:  ldloc.0
    IL_002a:  ldstr      ""X""
    IL_002f:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, string)""
    IL_0034:  ldloca.s   V_2
    IL_0036:  ldloc.0
    IL_0037:  ldc.i4.2
    IL_0038:  ldstr      ""Y""
    IL_003d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_0042:  ldloca.s   V_2
    IL_0044:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_004a:  callvirt   ""string object.ToString()""
    IL_004f:  stloc.1
    IL_0050:  leave.s    IL_005a
  }
  finally
  {
    IL_0052:  ldloca.s   V_2
    IL_0054:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0059:  endfinally
  }
  IL_005a:  ldloc.1
  IL_005b:  call       ""void System.Console.WriteLine(string)""
  IL_0060:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: false) => @"
{
  // Code size      101 (0x65)
  .maxstack  4
  .locals init (int V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.4
    IL_0003:  ldc.i4.4
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    IL_000a:  ldloca.s   V_2
    IL_000c:  ldstr      ""base""
    IL_0011:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_0016:  ldloca.s   V_2
    IL_0018:  ldloc.0
    IL_0019:  ldc.i4.0
    IL_001a:  ldnull
    IL_001b:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_0020:  ldloca.s   V_2
    IL_0022:  ldloc.0
    IL_0023:  ldc.i4.1
    IL_0024:  ldnull
    IL_0025:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_002a:  ldloca.s   V_2
    IL_002c:  ldloc.0
    IL_002d:  ldc.i4.0
    IL_002e:  ldstr      ""X""
    IL_0033:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_0038:  ldloca.s   V_2
    IL_003a:  ldloc.0
    IL_003b:  ldc.i4.2
    IL_003c:  ldstr      ""Y""
    IL_0041:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_0046:  ldloca.s   V_2
    IL_0048:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_004e:  callvirt   ""string object.ToString()""
    IL_0053:  stloc.1
    IL_0054:  leave.s    IL_005e
  }
  finally
  {
    IL_0056:  ldloca.s   V_2
    IL_0058:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_005d:  endfinally
  }
  IL_005e:  ldloc.1
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ret
}",
                (useDefaultParameters: false, useBoolReturns: true) => @"
{
  // Code size      109 (0x6d)
  .maxstack  4
  .locals init (int V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.4
    IL_0003:  ldc.i4.4
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    IL_000a:  ldloca.s   V_2
    IL_000c:  ldstr      ""base""
    IL_0011:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_0016:  brfalse.s  IL_004c
    IL_0018:  ldloca.s   V_2
    IL_001a:  ldloc.0
    IL_001b:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int)""
    IL_0020:  brfalse.s  IL_004c
    IL_0022:  ldloca.s   V_2
    IL_0024:  ldloc.0
    IL_0025:  ldc.i4.1
    IL_0026:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int)""
    IL_002b:  brfalse.s  IL_004c
    IL_002d:  ldloca.s   V_2
    IL_002f:  ldloc.0
    IL_0030:  ldstr      ""X""
    IL_0035:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, string)""
    IL_003a:  brfalse.s  IL_004c
    IL_003c:  ldloca.s   V_2
    IL_003e:  ldloc.0
    IL_003f:  ldc.i4.2
    IL_0040:  ldstr      ""Y""
    IL_0045:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_004a:  br.s       IL_004d
    IL_004c:  ldc.i4.0
    IL_004d:  pop
    IL_004e:  ldloca.s   V_2
    IL_0050:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0056:  callvirt   ""string object.ToString()""
    IL_005b:  stloc.1
    IL_005c:  leave.s    IL_0066
  }
  finally
  {
    IL_005e:  ldloca.s   V_2
    IL_0060:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0065:  endfinally
  }
  IL_0066:  ldloc.1
  IL_0067:  call       ""void System.Console.WriteLine(string)""
  IL_006c:  ret
}",
                (useDefaultParameters: true, useBoolReturns: true) => @"
{
  // Code size      113 (0x71)
  .maxstack  4
  .locals init (int V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.4
    IL_0003:  ldc.i4.4
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    IL_000a:  ldloca.s   V_2
    IL_000c:  ldstr      ""base""
    IL_0011:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_0016:  brfalse.s  IL_0050
    IL_0018:  ldloca.s   V_2
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.0
    IL_001c:  ldnull
    IL_001d:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_0022:  brfalse.s  IL_0050
    IL_0024:  ldloca.s   V_2
    IL_0026:  ldloc.0
    IL_0027:  ldc.i4.1
    IL_0028:  ldnull
    IL_0029:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_002e:  brfalse.s  IL_0050
    IL_0030:  ldloca.s   V_2
    IL_0032:  ldloc.0
    IL_0033:  ldc.i4.0
    IL_0034:  ldstr      ""X""
    IL_0039:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_003e:  brfalse.s  IL_0050
    IL_0040:  ldloca.s   V_2
    IL_0042:  ldloc.0
    IL_0043:  ldc.i4.2
    IL_0044:  ldstr      ""Y""
    IL_0049:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int, int, string)""
    IL_004e:  br.s       IL_0051
    IL_0050:  ldc.i4.0
    IL_0051:  pop
    IL_0052:  ldloca.s   V_2
    IL_0054:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_005a:  callvirt   ""string object.ToString()""
    IL_005f:  stloc.1
    IL_0060:  leave.s    IL_006a
  }
  finally
  {
    IL_0062:  ldloca.s   V_2
    IL_0064:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0069:  endfinally
  }
  IL_006a:  ldloc.1
  IL_006b:  call       ""void System.Console.WriteLine(string)""
  IL_0070:  ret
}",
            };
        }

        [Fact]
        public void UseOfSpanInInterpolationHole_CSharp9()
        {
            var source = @"
using System;
ReadOnlySpan<char> span = stackalloc char[1];
Console.WriteLine($""{span}"");";

            var comp = CreateCompilation(new[] { source, GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false) }, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,22): error CS8652: The feature 'improved interpolated strings' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // Console.WriteLine($"{span}");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "span").WithArguments("improved interpolated strings").WithLocation(4, 22)
                );
        }

        [Theory]
        [CombinatorialData]
        public void UseOfSpanInInterpolationHole(bool useDefaultParameters, bool useBoolReturns)
        {
            var source =
@"
using System;
ReadOnlySpan<char> a = ""1"";
System.Console.WriteLine($""base{a}{a,1}{a:X}{a,2:Y}"");";

            string interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters, useBoolReturns);

            string expectedOutput = useDefaultParameters ?
@"Disposed
base
value:1,alignment:0:format:
value:1,alignment:1:format:
value:1,alignment:0:format:X
value:1,alignment:2:format:Y" :
@"Disposed
base
value:1
value:1,alignment:1
value:1:format:X
value:1,alignment:2:format:Y";

            string expectedIl = getIl();

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: expectedOutput, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
            verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);

            var comp1 = CreateCompilation(interpolatedStringBuilder, targetFramework: TargetFramework.NetCoreApp);

            foreach (var reference in new[] { comp1.EmitToImageReference(), comp1.EmitToPortableExecutableReference() })
            {
                var comp2 = CreateCompilation(source, new[] { reference }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
                verifier = CompileAndVerify(comp2, expectedOutput: expectedOutput);
                verifier.VerifyIL("<top-level-statements-entry-point>", expectedIl);
            }

            string getIl() => (useDefaultParameters, useBoolReturns) switch
            {
                (useDefaultParameters: false, useBoolReturns: false) => @"
{
  // Code size      106 (0x6a)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  ldc.i4.4
    IL_000c:  ldc.i4.4
    IL_000d:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0012:  stloc.2
    IL_0013:  ldloca.s   V_2
    IL_0015:  ldstr      ""base""
    IL_001a:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_001f:  ldloca.s   V_2
    IL_0021:  ldloc.0
    IL_0022:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>)""
    IL_0027:  ldloca.s   V_2
    IL_0029:  ldloc.0
    IL_002a:  ldc.i4.1
    IL_002b:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int)""
    IL_0030:  ldloca.s   V_2
    IL_0032:  ldloc.0
    IL_0033:  ldstr      ""X""
    IL_0038:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, string)""
    IL_003d:  ldloca.s   V_2
    IL_003f:  ldloc.0
    IL_0040:  ldc.i4.2
    IL_0041:  ldstr      ""Y""
    IL_0046:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_004b:  ldloca.s   V_2
    IL_004d:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0053:  callvirt   ""string object.ToString()""
    IL_0058:  stloc.1
    IL_0059:  leave.s    IL_0063
  }
  finally
  {
    IL_005b:  ldloca.s   V_2
    IL_005d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0062:  endfinally
  }
  IL_0063:  ldloc.1
  IL_0064:  call       ""void System.Console.WriteLine(string)""
  IL_0069:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: false) => @"
{
  // Code size      110 (0x6e)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  ldc.i4.4
    IL_000c:  ldc.i4.4
    IL_000d:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0012:  stloc.2
    IL_0013:  ldloca.s   V_2
    IL_0015:  ldstr      ""base""
    IL_001a:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_001f:  ldloca.s   V_2
    IL_0021:  ldloc.0
    IL_0022:  ldc.i4.0
    IL_0023:  ldnull
    IL_0024:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0029:  ldloca.s   V_2
    IL_002b:  ldloc.0
    IL_002c:  ldc.i4.1
    IL_002d:  ldnull
    IL_002e:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0033:  ldloca.s   V_2
    IL_0035:  ldloc.0
    IL_0036:  ldc.i4.0
    IL_0037:  ldstr      ""X""
    IL_003c:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0041:  ldloca.s   V_2
    IL_0043:  ldloc.0
    IL_0044:  ldc.i4.2
    IL_0045:  ldstr      ""Y""
    IL_004a:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_004f:  ldloca.s   V_2
    IL_0051:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0057:  callvirt   ""string object.ToString()""
    IL_005c:  stloc.1
    IL_005d:  leave.s    IL_0067
  }
  finally
  {
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0066:  endfinally
  }
  IL_0067:  ldloc.1
  IL_0068:  call       ""void System.Console.WriteLine(string)""
  IL_006d:  ret
}",
                (useDefaultParameters: false, useBoolReturns: true) => @"
{
  // Code size      118 (0x76)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  ldc.i4.4
    IL_000c:  ldc.i4.4
    IL_000d:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0012:  stloc.2
    IL_0013:  ldloca.s   V_2
    IL_0015:  ldstr      ""base""
    IL_001a:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_001f:  brfalse.s  IL_0055
    IL_0021:  ldloca.s   V_2
    IL_0023:  ldloc.0
    IL_0024:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>)""
    IL_0029:  brfalse.s  IL_0055
    IL_002b:  ldloca.s   V_2
    IL_002d:  ldloc.0
    IL_002e:  ldc.i4.1
    IL_002f:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int)""
    IL_0034:  brfalse.s  IL_0055
    IL_0036:  ldloca.s   V_2
    IL_0038:  ldloc.0
    IL_0039:  ldstr      ""X""
    IL_003e:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, string)""
    IL_0043:  brfalse.s  IL_0055
    IL_0045:  ldloca.s   V_2
    IL_0047:  ldloc.0
    IL_0048:  ldc.i4.2
    IL_0049:  ldstr      ""Y""
    IL_004e:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0053:  br.s       IL_0056
    IL_0055:  ldc.i4.0
    IL_0056:  pop
    IL_0057:  ldloca.s   V_2
    IL_0059:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_005f:  callvirt   ""string object.ToString()""
    IL_0064:  stloc.1
    IL_0065:  leave.s    IL_006f
  }
  finally
  {
    IL_0067:  ldloca.s   V_2
    IL_0069:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_006e:  endfinally
  }
  IL_006f:  ldloc.1
  IL_0070:  call       ""void System.Console.WriteLine(string)""
  IL_0075:  ret
}
",
                (useDefaultParameters: true, useBoolReturns: true) => @"
{
  // Code size      122 (0x7a)
  .maxstack  4
  .locals init (System.ReadOnlySpan<char> V_0, //a
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2)
  IL_0000:  ldstr      ""1""
  IL_0005:  call       ""System.ReadOnlySpan<char> string.op_Implicit(string)""
  IL_000a:  stloc.0
  .try
  {
    IL_000b:  ldc.i4.4
    IL_000c:  ldc.i4.4
    IL_000d:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0012:  stloc.2
    IL_0013:  ldloca.s   V_2
    IL_0015:  ldstr      ""base""
    IL_001a:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
    IL_001f:  brfalse.s  IL_0059
    IL_0021:  ldloca.s   V_2
    IL_0023:  ldloc.0
    IL_0024:  ldc.i4.0
    IL_0025:  ldnull
    IL_0026:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_002b:  brfalse.s  IL_0059
    IL_002d:  ldloca.s   V_2
    IL_002f:  ldloc.0
    IL_0030:  ldc.i4.1
    IL_0031:  ldnull
    IL_0032:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0037:  brfalse.s  IL_0059
    IL_0039:  ldloca.s   V_2
    IL_003b:  ldloc.0
    IL_003c:  ldc.i4.0
    IL_003d:  ldstr      ""X""
    IL_0042:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0047:  brfalse.s  IL_0059
    IL_0049:  ldloca.s   V_2
    IL_004b:  ldloc.0
    IL_004c:  ldc.i4.2
    IL_004d:  ldstr      ""Y""
    IL_0052:  call       ""bool System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>, int, string)""
    IL_0057:  br.s       IL_005a
    IL_0059:  ldc.i4.0
    IL_005a:  pop
    IL_005b:  ldloca.s   V_2
    IL_005d:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0063:  callvirt   ""string object.ToString()""
    IL_0068:  stloc.1
    IL_0069:  leave.s    IL_0073
  }
  finally
  {
    IL_006b:  ldloca.s   V_2
    IL_006d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0072:  endfinally
  }
  IL_0073:  ldloc.1
  IL_0074:  call       ""void System.Console.WriteLine(string)""
  IL_0079:  ret
}",
            };
        }

        [Fact]
        public void BoolReturns_ShortCircuit()
        {
            var source = @"
using System;
int a = 1;
Console.Write($""base{Throw()}{a = 2}"");
Console.WriteLine(a);
string Throw() => throw new Exception();";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: true, returnExpression: "false");

            CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Disposed
base
1");
        }

        [Fact]
        public void AwaitInHoles_UsesFormat()
        {
            // PROTOTYPE(interp-string): We could make this case use the builder as well by evaluating the holes ahead of time. For InterpolatedStringBuilder,
            // we know that the framework is never going to ship a version that short circuits, so it would be a valid optimization for us to make.
            var source = @"
using System;
using System.Threading.Tasks;

Console.WriteLine($""base{await Hole()}"");
Task<int> Hole() => Task.FromResult(1);";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"base1");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (<Program>$.<<Main>$>d__0 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  call       ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Create()""
  IL_0007:  stfld      ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_000c:  ldloca.s   V_0
  IL_000e:  ldc.i4.m1
  IL_000f:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0014:  ldloca.s   V_0
  IL_0016:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<<Program>$.<<Main>$>d__0>(ref <Program>$.<<Main>$>d__0)""
  IL_0022:  ldloca.s   V_0
  IL_0024:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_0029:  call       ""System.Threading.Tasks.Task System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Task.get""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void NoAwaitInHoles_UsesBuilder()
        {
            var source = @"
using System;
using System.Threading.Tasks;

var hole = await Hole();
Console.WriteLine($""base{hole}"");
Task<int> Hole() => Task.FromResult(1);";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Disposed
base
value:1");

            verifier.VerifyIL("<Program>$.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      211 (0xd3)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //hole
                System.Runtime.CompilerServices.TaskAwaiter<int> V_2,
                string V_3,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0041
    IL_000a:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__Hole|0_0()""
    IL_000f:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0014:  stloc.2
    IL_0015:  ldloca.s   V_2
    IL_0017:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_001c:  brtrue.s   IL_005d
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
    IL_003c:  leave      IL_00d2
    IL_0041:  ldarg.0
    IL_0042:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0047:  stloc.2
    IL_0048:  ldarg.0
    IL_0049:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_004e:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0054:  ldarg.0
    IL_0055:  ldc.i4.m1
    IL_0056:  dup
    IL_0057:  stloc.0
    IL_0058:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_005d:  ldloca.s   V_2
    IL_005f:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0064:  stloc.1
    .try
    {
      IL_0065:  ldc.i4.4
      IL_0066:  ldc.i4.1
      IL_0067:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
      IL_006c:  stloc.s    V_4
      IL_006e:  ldloca.s   V_4
      IL_0070:  ldstr      ""base""
      IL_0075:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
      IL_007a:  ldloca.s   V_4
      IL_007c:  ldloc.1
      IL_007d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int)""
      IL_0082:  ldloca.s   V_4
      IL_0084:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
      IL_008a:  callvirt   ""string object.ToString()""
      IL_008f:  stloc.3
      IL_0090:  leave.s    IL_009e
    }
    finally
    {
      IL_0092:  ldloc.0
      IL_0093:  ldc.i4.0
      IL_0094:  bge.s      IL_009d
      IL_0096:  ldloca.s   V_4
      IL_0098:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
      IL_009d:  endfinally
    }
    IL_009e:  ldloc.3
    IL_009f:  call       ""void System.Console.WriteLine(string)""
    IL_00a4:  leave.s    IL_00bf
  }
  catch System.Exception
  {
    IL_00a6:  stloc.s    V_5
    IL_00a8:  ldarg.0
    IL_00a9:  ldc.i4.s   -2
    IL_00ab:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_00b0:  ldarg.0
    IL_00b1:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_00b6:  ldloc.s    V_5
    IL_00b8:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00bd:  leave.s    IL_00d2
  }
  IL_00bf:  ldarg.0
  IL_00c0:  ldc.i4.s   -2
  IL_00c2:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_00c7:  ldarg.0
  IL_00c8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_00cd:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_00d2:  ret
}
");
        }

        [Fact]
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

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
1
Disposed
3
base
value:2");

            verifier.VerifyIL("<Program>$.<<Main>$>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      356 (0x164)
  .maxstack  3
  .locals init (int V_0,
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2,
                int V_3,
                System.Runtime.CompilerServices.TaskAwaiter<int> V_4,
                System.Exception V_5)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0052
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00f7
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4.2
    IL_0013:  stfld      ""int <Program>$.<<Main>$>d__0.<hole>5__2""
    IL_0018:  ldc.i4.1
    IL_0019:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__M|0_1(int)""
    IL_001e:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_0023:  stloc.s    V_4
    IL_0025:  ldloca.s   V_4
    IL_0027:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_002c:  brtrue.s   IL_006f
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.0
    IL_0030:  dup
    IL_0031:  stloc.0
    IL_0032:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0037:  ldarg.0
    IL_0038:  ldloc.s    V_4
    IL_003a:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0045:  ldloca.s   V_4
    IL_0047:  ldarg.0
    IL_0048:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_004d:  leave      IL_0163
    IL_0052:  ldarg.0
    IL_0053:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0058:  stloc.s    V_4
    IL_005a:  ldarg.0
    IL_005b:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0060:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.m1
    IL_0068:  dup
    IL_0069:  stloc.0
    IL_006a:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_006f:  ldarg.0
    IL_0070:  ldloca.s   V_4
    IL_0072:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_0077:  stfld      ""int <Program>$.<<Main>$>d__0.<>7__wrap2""
    .try
    {
      IL_007c:  ldc.i4.4
      IL_007d:  ldc.i4.1
      IL_007e:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
      IL_0083:  stloc.2
      IL_0084:  ldloca.s   V_2
      IL_0086:  ldstr      ""base""
      IL_008b:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)""
      IL_0090:  ldloca.s   V_2
      IL_0092:  ldarg.0
      IL_0093:  ldfld      ""int <Program>$.<<Main>$>d__0.<hole>5__2""
      IL_0098:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int)""
      IL_009d:  ldloca.s   V_2
      IL_009f:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
      IL_00a5:  callvirt   ""string object.ToString()""
      IL_00aa:  stloc.1
      IL_00ab:  leave.s    IL_00b9
    }
    finally
    {
      IL_00ad:  ldloc.0
      IL_00ae:  ldc.i4.0
      IL_00af:  bge.s      IL_00b8
      IL_00b1:  ldloca.s   V_2
      IL_00b3:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
      IL_00b8:  endfinally
    }
    IL_00b9:  ldarg.0
    IL_00ba:  ldloc.1
    IL_00bb:  stfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_00c0:  ldc.i4.3
    IL_00c1:  call       ""System.Threading.Tasks.Task<int> <Program>$.<<Main>$>g__M|0_1(int)""
    IL_00c6:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()""
    IL_00cb:  stloc.s    V_4
    IL_00cd:  ldloca.s   V_4
    IL_00cf:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter<int>.IsCompleted.get""
    IL_00d4:  brtrue.s   IL_0114
    IL_00d6:  ldarg.0
    IL_00d7:  ldc.i4.1
    IL_00d8:  dup
    IL_00d9:  stloc.0
    IL_00da:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_00df:  ldarg.0
    IL_00e0:  ldloc.s    V_4
    IL_00e2:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_00e7:  ldarg.0
    IL_00e8:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_00ed:  ldloca.s   V_4
    IL_00ef:  ldarg.0
    IL_00f0:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter<int>, <Program>$.<<Main>$>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter<int>, ref <Program>$.<<Main>$>d__0)""
    IL_00f5:  leave.s    IL_0163
    IL_00f7:  ldarg.0
    IL_00f8:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_00fd:  stloc.s    V_4
    IL_00ff:  ldarg.0
    IL_0100:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter<int> <Program>$.<<Main>$>d__0.<>u__1""
    IL_0105:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter<int>""
    IL_010b:  ldarg.0
    IL_010c:  ldc.i4.m1
    IL_010d:  dup
    IL_010e:  stloc.0
    IL_010f:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0114:  ldloca.s   V_4
    IL_0116:  call       ""int System.Runtime.CompilerServices.TaskAwaiter<int>.GetResult()""
    IL_011b:  stloc.3
    IL_011c:  ldarg.0
    IL_011d:  ldfld      ""int <Program>$.<<Main>$>d__0.<>7__wrap2""
    IL_0122:  ldarg.0
    IL_0123:  ldfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_0128:  ldloc.3
    IL_0129:  call       ""void <Program>$.<<Main>$>g__Test|0_0(int, string, int)""
    IL_012e:  ldarg.0
    IL_012f:  ldnull
    IL_0130:  stfld      ""string <Program>$.<<Main>$>d__0.<>7__wrap3""
    IL_0135:  leave.s    IL_0150
  }
  catch System.Exception
  {
    IL_0137:  stloc.s    V_5
    IL_0139:  ldarg.0
    IL_013a:  ldc.i4.s   -2
    IL_013c:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
    IL_0141:  ldarg.0
    IL_0142:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
    IL_0147:  ldloc.s    V_5
    IL_0149:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_014e:  leave.s    IL_0163
  }
  IL_0150:  ldarg.0
  IL_0151:  ldc.i4.s   -2
  IL_0153:  stfld      ""int <Program>$.<<Main>$>d__0.<>1__state""
  IL_0158:  ldarg.0
  IL_0159:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder <Program>$.<<Main>$>d__0.<>t__builder""
  IL_015e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0163:  ret
}
");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("public static InterpolatedStringBuilder Create(int baseLength) => throw null;")]
        [InlineData("public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles, object otherParam = null) => throw null;")]
        [InlineData("public static void Create(int baseLength) => throw null;")]
        [InlineData("public InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;")]
        public void MissingWellKnownMethod_Create(string createSignature)
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        " + createSignature + @"
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public override string ToString() => throw null;
        public void Dispose() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.InterpolatedStringBuilder.Create'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder", "Create").WithLocation(1, 5)
            );

        }

        [Theory]
        [InlineData(null)]
        [InlineData("public void Dispose(int baseLength) => throw null;")]
        [InlineData("public bool Dispose() => throw null;")]
        [InlineData("public static void Dispose() => throw null;")]
        public void MissingWellKnownMethod_Dispose(string disposeMethod)
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        " + disposeMethod + @"
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder", "Dispose").WithLocation(1, 5)
            );
        }

        // PROTOTYPE(interp-string): Should we hard error on malformed TryFormat... methods in the well-known InterpolatedStringBuilder as well?

        [Fact]
        public void ObsoleteCreateMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        [System.Obsolete(""Create is obsolete"", error: true)]
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS0619: 'InterpolatedStringBuilder.Create(int, int)' is obsolete: 'Create is obsolete'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)", "Create is obsolete").WithLocation(1, 5)
            );
        }

        [Fact]
        public void ObsoleteDisposeMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        [System.Obsolete(""Dispose is obsolete"", error: true)]
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,5): error CS0619: 'InterpolatedStringBuilder.Dispose()' is obsolete: 'Dispose is obsolete'
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()", "Dispose is obsolete").WithLocation(1, 5)
            );
        }

        [Fact]
        public void ObsoleteTryFormatBaseStringMethod()
        {
            var code = @"_ = $""base{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        [System.Obsolete(""TryFormatBaseString is obsolete"", error: true)]
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,7): error CS0619: 'InterpolatedStringBuilder.TryFormatBaseString(string)' is obsolete: 'TryFormatBaseString is obsolete'
                // _ = $"base{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "base").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatBaseString(string)", "TryFormatBaseString is obsolete").WithLocation(1, 7)
            );
        }

        [Fact]
        public void ObsoleteTryFormatInterpolationHoleMethod()
        {
            var code = @"_ = $""base{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        [System.Obsolete(""TryFormatFormatHole is obsolete"", error: true)]
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,11): error CS0619: 'InterpolatedStringBuilder.TryFormatInterpolationHole<T>(T, int, string)' is obsolete: 'TryFormatFormatHole is obsolete'
                // _ = $"base{(object)1}";
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "{(object)1}").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<T>(T, int, string)", "TryFormatFormatHole is obsolete").WithLocation(1, 11)
            );
        }

        [Fact]
        public void ObsoleteToStringMethod()
        {
            var code = @"_ = $""base{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public void Dispose() => throw null;
        [System.Obsolete(""ToString is obsolete"", error: true)]
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
";

            // Note: We report obsolete on the base method, ie object.ToString(), which is not obsolete. So no diagnostics are reported here.
            var comp = CreateCompilation(new[] { code, interpolatedStringBuilder });
            comp.VerifyEmitDiagnostics(
                // (10,32): warning CS0809: Obsolete member 'InterpolatedStringBuilder.ToString()' overrides non-obsolete member 'object.ToString()'
                //         public override string ToString() => throw null;
                Diagnostic(ErrorCode.WRN_ObsoleteOverridingNonObsolete, "ToString").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.ToString()", "object.ToString()").WithLocation(10, 32)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyCreateMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"
namespace System.Runtime.CompilerServices
{
    public ref struct InterpolatedStringBuilder
    {
        [System.Runtime.InteropServices.UnmanagedCallersOnly]
        public static InterpolatedStringBuilder Create(int baseLength, int numFormatHoles) => throw null;
        public InterpolatedStringBuilder(int baseLength) => throw null;
        public void Dispose() => throw null;
        public override string ToString() => throw null;
        public void TryFormatBaseString(string value) => throw null;
        public void TryFormatInterpolationHole<T>(T hole, int alignment = 0, string format = null) => throw null;
    }
}
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute()
        {
        }

        public Type[] CallConvs;
        public string EntryPoint;
    }
}
";

            var comp1 = CreateCompilation(interpolatedStringBuilder, targetFramework: TargetFramework.NetCoreApp);

            var comp = CreateCompilation(new[] { code }, references: new[] { comp1.EmitToImageReference() });
            comp.VerifyDiagnostics(
                // (1,5): error CS8901: 'InterpolatedStringBuilder.Create(int, int)' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)").WithLocation(1, 5)
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

        [Fact]
        public void UnmanagedCallersOnlyDisposeMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"

.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.InterpolatedStringBuilder
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

    // Methods
    .method public hidebysig static 
        valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder Create (
            int32 baseLength,
            int32 numFormatHoles
        ) cil managed 
    {
        .locals init (
            [0] valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder
        )

        ldnull
        throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 baseLength
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void Dispose () cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
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
        instance void TryFormatBaseString (
            string 'value'
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void TryFormatInterpolationHole<T> (
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
            comp.VerifyDiagnostics(
                // (1,5): error CS0570: 'InterpolatedStringBuilder.Dispose()' is not supported by the language
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BindToBogus, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()").WithLocation(1, 5),
                // (1,5): error CS8901: 'InterpolatedStringBuilder.Dispose()' is attributed with 'UnmanagedCallersOnly' and cannot be called directly. Obtain a function pointer to this method.
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly, @"$""{(object)1}""").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()").WithLocation(1, 5)
            );
        }

        [Fact]
        public void UnmanagedCallersOnlyTryFormatBaseStringMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"

.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.InterpolatedStringBuilder
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

    // Methods
    .method public hidebysig static 
        valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder Create (
            int32 baseLength,
            int32 numFormatHoles
        ) cil managed 
    {
        .locals init (
            [0] valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder
        )

        ldnull
        throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 baseLength
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
        instance void TryFormatBaseString (
            string 'value'
        ) cil managed 
    {
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .method public hidebysig 
        instance void TryFormatInterpolationHole<T> (
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
            var verifier = CompileAndVerify(comp);

            // PROTOTYPE(interp-string): Do we want to hard error in this case, instead of falling back to string.Format?
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldstr      ""{0}""
  IL_0005:  ldc.i4.1
  IL_0006:  box        ""int""
  IL_000b:  call       ""string string.Format(string, object)""
  IL_0010:  pop
  IL_0011:  ret
}
");
        }

        [Fact]
        public void UnmanagedCallersOnlyTryFormatInterpolationHoleMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"

.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.InterpolatedStringBuilder
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

    // Methods
    .method public hidebysig static 
        valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder Create (
            int32 baseLength,
            int32 numFormatHoles
        ) cil managed 
    {
        .locals init (
            [0] valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder
        )

        ldnull
        throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 baseLength
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
        instance void TryFormatBaseString (
            string 'value'
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void TryFormatInterpolationHole<T> (
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
            var verifier = CompileAndVerify(comp);

            // PROTOTYPE(interp-string): Do we want to hard error in this case, instead of falling back to string.Format?
            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldstr      ""{0}""
  IL_0005:  ldc.i4.1
  IL_0006:  box        ""int""
  IL_000b:  call       ""string string.Format(string, object)""
  IL_0010:  pop
  IL_0011:  ret
}
");
        }

        [Fact]
        public void UnmanagedCallersOnlyToStringMethod()
        {
            var code = @"_ = $""{(object)1}"";";

            var interpolatedStringBuilder = @"

.class public sequential ansi sealed beforefieldinit System.Runtime.CompilerServices.InterpolatedStringBuilder
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

    // Methods
    .method public hidebysig static 
        valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder Create (
            int32 baseLength,
            int32 numFormatHoles
        ) cil managed 
    {
        .locals init (
            [0] valuetype System.Runtime.CompilerServices.InterpolatedStringBuilder
        )

        ldnull
        throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 baseLength
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
        .custom instance void System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        ldnull
        throw
    }

    .method public hidebysig 
        instance void TryFormatBaseString (
            string 'value'
        ) cil managed 
    {
        ldnull
        throw
    }

    .method public hidebysig 
        instance void TryFormatInterpolationHole<T> (
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
                // (1,1): error CS0570: 'InterpolatedStringBuilder.ToString()' is not supported by the language
                // _ = $"{(object)1}";
                Diagnostic(ErrorCode.ERR_BindToBogus, @"_ = $""{(object)1}"";").WithArguments("System.Runtime.CompilerServices.InterpolatedStringBuilder.ToString()").WithLocation(1, 1)
            );
        }

        [Fact]
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

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters: true, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeReleaseExe, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (6,12): error CS0029: Cannot implicitly convert type 'int*' to 'object'
                //     _ = $"{i}{s}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "i").WithArguments("int*", "object").WithLocation(6, 12),
                // (6,15): error CS0029: Cannot implicitly convert type 'S' to 'object'
                //     _ = $"{i}{s}";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("S", "object").WithLocation(6, 15)
            );
        }

        [Fact]
        public void TargetTypedInterpolationHoles()
        {
            var source = @"
bool b = true;
System.Console.WriteLine($""{b switch { true => 1, false => null }}{(!b ? null : 2)}{new()}{default}{null}"");";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Disposed
value:1
value:2
value:System.Object
value:
value:");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size      110 (0x6e)
  .maxstack  2
  .locals init (bool V_0, //b
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2,
                object V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.0
    IL_0003:  ldc.i4.5
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    IL_000a:  ldloc.0
    IL_000b:  brfalse.s  IL_0016
    IL_000d:  ldc.i4.1
    IL_000e:  box        ""int""
    IL_0013:  stloc.3
    IL_0014:  br.s       IL_0018
    IL_0016:  ldnull
    IL_0017:  stloc.3
    IL_0018:  ldloca.s   V_2
    IL_001a:  ldloc.3
    IL_001b:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(object)""
    IL_0020:  ldloca.s   V_2
    IL_0022:  ldloc.0
    IL_0023:  brfalse.s  IL_002d
    IL_0025:  ldc.i4.2
    IL_0026:  box        ""int""
    IL_002b:  br.s       IL_002e
    IL_002d:  ldnull
    IL_002e:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(object)""
    IL_0033:  ldloca.s   V_2
    IL_0035:  newobj     ""object..ctor()""
    IL_003a:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(object)""
    IL_003f:  ldloca.s   V_2
    IL_0041:  ldnull
    IL_0042:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(object)""
    IL_0047:  ldloca.s   V_2
    IL_0049:  ldnull
    IL_004a:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(object)""
    IL_004f:  ldloca.s   V_2
    IL_0051:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0057:  callvirt   ""string object.ToString()""
    IL_005c:  stloc.1
    IL_005d:  leave.s    IL_0067
  }
  finally
  {
    IL_005f:  ldloca.s   V_2
    IL_0061:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0066:  endfinally
  }
  IL_0067:  ldloc.1
  IL_0068:  call       ""void System.Console.WriteLine(string)""
  IL_006d:  ret
}
");
        }

        [Fact]
        public void TargetTypedInterpolationHoles_Errors()
        {
            var source = @"System.Console.WriteLine($""{(null, default)}"");";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);
            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder });
            comp.VerifyDiagnostics(
                // (1,29): error CS8135: Tuple with 2 elements cannot be converted to type 'object'.
                // System.Console.WriteLine($"{(null, default)}");
                Diagnostic(ErrorCode.ERR_ConversionNotTupleCompatible, "(null, default)").WithArguments("2", "object").WithLocation(1, 29)
            );
        }

        [Fact]
        public void RefTernary()
        {
            var source = @"
bool b = true;
int i = 1;
System.Console.WriteLine($""{(!b ? ref i : ref i)}"");";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Disposed
value:1");
        }

        [Fact]
        public void NestedInterpolatedStrings()
        {
            var source = @"
int i = 1;
System.Console.WriteLine($""{$""{i}""}"");";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Disposed
Disposed
value:value:1");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (int V_0, //i
                string V_1,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_2,
                string V_3,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_4)
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldc.i4.0
    IL_0003:  ldc.i4.1
    IL_0004:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0009:  stloc.2
    .try
    {
      IL_000a:  ldc.i4.0
      IL_000b:  ldc.i4.1
      IL_000c:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
      IL_0011:  stloc.s    V_4
      IL_0013:  ldloca.s   V_4
      IL_0015:  ldloc.0
      IL_0016:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<int>(int)""
      IL_001b:  ldloca.s   V_4
      IL_001d:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
      IL_0023:  callvirt   ""string object.ToString()""
      IL_0028:  stloc.3
      IL_0029:  leave.s    IL_0033
    }
    finally
    {
      IL_002b:  ldloca.s   V_4
      IL_002d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
      IL_0032:  endfinally
    }
    IL_0033:  ldloca.s   V_2
    IL_0035:  ldloc.3
    IL_0036:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<string>(string)""
    IL_003b:  ldloca.s   V_2
    IL_003d:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0043:  callvirt   ""string object.ToString()""
    IL_0048:  stloc.1
    IL_0049:  leave.s    IL_0053
  }
  finally
  {
    IL_004b:  ldloca.s   V_2
    IL_004d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0052:  endfinally
  }
  IL_0053:  ldloc.1
  IL_0054:  call       ""void System.Console.WriteLine(string)""
  IL_0059:  ret
}
");
        }

        [Fact]
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
catch (MyException e) when (e.ToString() == $""{i}"")
{
    Console.WriteLine(""Caught"");
}

class MyException : Exception
{
    public int Prop { get; set; }
    public override string ToString() => Prop.ToString();
}";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: false, useDefaultParameters: false, useBoolReturns: false);

            // Note the lack of "Disposed" in the output
            var verifier = CompileAndVerify(new[] { source, interpolatedStringBuilder }, expectedOutput: @"
Starting try
Caught");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       82 (0x52)
  .maxstack  3
  .locals init (int V_0) //i
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
    IL_0023:  br.s       IL_0042
    IL_0025:  callvirt   ""string object.ToString()""
    IL_002a:  ldstr      ""{0}""
    IL_002f:  ldloc.0
    IL_0030:  box        ""int""
    IL_0035:  call       ""string string.Format(string, object)""
    IL_003a:  call       ""bool string.op_Equality(string, string)""
    IL_003f:  ldc.i4.0
    IL_0040:  cgt.un
    IL_0042:  endfilter
  }  // end filter
  {  // handler
    IL_0044:  pop
    IL_0045:  ldstr      ""Caught""
    IL_004a:  call       ""void System.Console.WriteLine(string)""
    IL_004f:  leave.s    IL_0051
  }
  IL_0051:  ret
}
");
        }

        [Fact]
        public void ExceptionFilter_02()
        {
            var source = @"
using System;

try
{
}
catch (Exception e) when (e.ToString() == $""{(ReadOnlySpan<char>)""""}"")
{
}
";

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            // PROTOTYPE(interp-string): Can we get a better error message for this?
            comp.VerifyDiagnostics(
                // (7,46): error CS0029: Cannot implicitly convert type 'System.ReadOnlySpan<char>' to 'object'
                // catch (Exception e) when (e.ToString() == $"{(ReadOnlySpan<char>)""}")
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(ReadOnlySpan<char>)""""").WithArguments("System.ReadOnlySpan<char>", "object").WithLocation(7, 46)
            );
        }

        [Fact]
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

            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
Disposed
value:S converted
value:C");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (S V_0, //s
                C V_1, //c
                string V_2,
                System.Runtime.CompilerServices.InterpolatedStringBuilder V_3)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S""
  IL_0008:  newobj     ""C..ctor()""
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldc.i4.0
    IL_000f:  ldc.i4.2
    IL_0010:  call       ""System.Runtime.CompilerServices.InterpolatedStringBuilder System.Runtime.CompilerServices.InterpolatedStringBuilder.Create(int, int)""
    IL_0015:  stloc.3
    IL_0016:  ldloca.s   V_3
    IL_0018:  ldloc.0
    IL_0019:  call       ""System.ReadOnlySpan<char> S.op_Implicit(S)""
    IL_001e:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole(System.ReadOnlySpan<char>)""
    IL_0023:  ldloca.s   V_3
    IL_0025:  ldloc.1
    IL_0026:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.TryFormatInterpolationHole<C>(C)""
    IL_002b:  ldloca.s   V_3
    IL_002d:  constrained. ""System.Runtime.CompilerServices.InterpolatedStringBuilder""
    IL_0033:  callvirt   ""string object.ToString()""
    IL_0038:  stloc.2
    IL_0039:  leave.s    IL_0043
  }
  finally
  {
    IL_003b:  ldloca.s   V_3
    IL_003d:  call       ""void System.Runtime.CompilerServices.InterpolatedStringBuilder.Dispose()""
    IL_0042:  endfinally
  }
  IL_0043:  ldloc.2
  IL_0044:  call       ""void System.Console.WriteLine(string)""
  IL_0049:  ret
}
");
        }


        [Fact]
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
            var interpolatedStringBuilder = GetInterpolatedStringBuilderDefinition(includeSpanOverloads: true, useDefaultParameters: false, useBoolReturns: false);

            var comp = CreateCompilation(new[] { source, interpolatedStringBuilder }, targetFramework: TargetFramework.NetCoreApp, parseOptions: TestOptions.RegularPreview);
            // PROTOTYPE(interp-string): Can we make a better error here?
            comp.VerifyDiagnostics(
                // (5,22): error CS0029: Cannot implicitly convert type 'S' to 'object'
                // Console.WriteLine($"{s}");
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "s").WithArguments("S", "object").WithLocation(5, 22)
            );
        }
    }
}
