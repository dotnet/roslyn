// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System.Collections.Immutable;
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
    public ref partial struct DefaultInterpolatedStringHandler
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

            CompileAndVerify(source, expectedOutput: "value:1");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MixedBuilderReturnTypes_04()
        {
            var source = @"
using System;
using System.Text;
using System.Runtime.CompilerServices;

Console.WriteLine((CustomHandler)$""l"");

[InterpolatedStringHandler]
public class CustomHandler
{
    private readonly StringBuilder _builder;
    public CustomHandler(int literalLength, int formattedCount)
    {
        _builder = new StringBuilder();
    }
    public override string ToString() => _builder.ToString();
    public bool AppendFormatted(object o) => true;
    public void AppendLiteral(string s)
    {
        _builder.AppendLine(""literal:"" + s.ToString());
    }
}
";

            CompileAndVerify(new[] { source, InterpolatedStringHandlerAttribute }, parseOptions: TestOptions.RegularPreview, expectedOutput: "literal:l");
        }

        private const string InterpolatedStringHandlerAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerAttribute : Attribute
    {
        public InterpolatedStringHandlerAttribute()
        {
        }
    }
}
";

        private string GetCustomHandlerType(string name, string type, bool useBoolReturns, bool includeOneTimeHelpers = true)
        {
            var returnType = useBoolReturns ? "bool" : "void";
            var returnStatement = useBoolReturns ? "return true;" : "return;";

            var cultureInfoHandler = @"
public class CultureInfoNormalizer
{
    public static void Normalize()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
";

            return (includeOneTimeHelpers ? "using System.Globalization;\n" : "") + @"
using System.Text;
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public " + type + " " + name + @"
{
    private readonly StringBuilder _builder;
    public " + name + @"(int literalLength, int formattedCount)
    {
        _builder = new();
    }
    public " + returnType + @" AppendLiteral(string literal)
    {
        _builder.AppendLine(""literal:"" + literal);
        " + returnStatement + @"
    }
    public " + returnType + @" AppendFormatted(object o, int alignment = 0, string format = null)
    {
        _builder.AppendLine(""value:"" + o?.ToString());
        _builder.AppendLine(""alignment:"" + alignment.ToString());
        _builder.AppendLine(""format:"" + format);
        " + returnStatement + @"
    }
    public override string ToString() => _builder.ToString();
}
" + (includeOneTimeHelpers ? InterpolatedStringHandlerAttribute + cultureInfoHandler : "");
        }

        private void VerifyInterpolatedStringExpression(CSharpCompilation comp, string handlerType = "CustomHandler")
        {
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var interpolatedString = tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().Single();
            var semanticInfo = model.GetSemanticInfoSummary(interpolatedString);

            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);
            Assert.Equal(handlerType, semanticInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(ConversionKind.InterpolatedStringHandler, semanticInfo.ImplicitConversion.Kind);
            Assert.True(semanticInfo.ImplicitConversion.Exists);
            Assert.True(semanticInfo.ImplicitConversion.IsValid);
            Assert.True(semanticInfo.ImplicitConversion.IsInterpolatedStringHandler);
            Assert.Null(semanticInfo.ImplicitConversion.Method);

            // PROTOTYPE(interp-string): Assert IConversionOperation.IsImplicit when IOperation is implemented for interpolated strings.
        }

        private CompilationVerifier CompileAndVerifyOnCorrectPlatforms(CSharpCompilation compilation, string expectedOutput)
         => CompileAndVerify(
             compilation,
             expectedOutput: ExecutionConditionUtil.IsMonoOrCoreClr ? expectedOutput : null,
             verify: ExecutionConditionUtil.IsMonoOrCoreClr ? Verification.Passes : Verification.Skipped);

        [ConditionalTheory(typeof(NoIOperationValidation))]
        [CombinatorialData]
        public void CustomHandlerLocal([CombinatorialValues("class", "struct")] string type, bool useBoolReturns)
        {
            var code = @"
CustomHandler builder = $""Literal{1,2:f}"";
System.Console.WriteLine(builder.ToString());";

            var builder = GetCustomHandlerType("CustomHandler", type, useBoolReturns);
            var comp = CreateCompilation(new[] { code, builder }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);

            var verifier = CompileAndVerify(comp, expectedOutput: @"
literal:Literal
value:1
alignment:2
format:f");

            verifier.VerifyIL("<top-level-statements-entry-point>", getIl());

            string getIl() => (type, useBoolReturns) switch
            {
                (type: "struct", useBoolReturns: true) => @"
{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (CustomHandler V_0, //builder
                CustomHandler V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldstr      ""Literal""
  IL_0010:  call       ""bool CustomHandler.AppendLiteral(string)""
  IL_0015:  brfalse.s  IL_002c
  IL_0017:  ldloca.s   V_1
  IL_0019:  ldc.i4.1
  IL_001a:  box        ""int""
  IL_001f:  ldc.i4.2
  IL_0020:  ldstr      ""f""
  IL_0025:  call       ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_002a:  br.s       IL_002d
  IL_002c:  ldc.i4.0
  IL_002d:  pop
  IL_002e:  ldloc.1
  IL_002f:  stloc.0
  IL_0030:  ldloca.s   V_0
  IL_0032:  constrained. ""CustomHandler""
  IL_0038:  callvirt   ""string object.ToString()""
  IL_003d:  call       ""void System.Console.WriteLine(string)""
  IL_0042:  ret
}
",
                (type: "struct", useBoolReturns: false) => @"
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (CustomHandler V_0, //builder
                CustomHandler V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_1
  IL_000b:  ldstr      ""Literal""
  IL_0010:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0015:  ldloca.s   V_1
  IL_0017:  ldc.i4.1
  IL_0018:  box        ""int""
  IL_001d:  ldc.i4.2
  IL_001e:  ldstr      ""f""
  IL_0023:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0028:  ldloc.1
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  constrained. ""CustomHandler""
  IL_0032:  callvirt   ""string object.ToString()""
  IL_0037:  call       ""void System.Console.WriteLine(string)""
  IL_003c:  ret
}
",
                (type: "class", useBoolReturns: true) => @"
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldstr      ""Literal""
  IL_000e:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0013:  brfalse.s  IL_0029
  IL_0015:  ldloc.0
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""int""
  IL_001c:  ldc.i4.2
  IL_001d:  ldstr      ""f""
  IL_0022:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  callvirt   ""string object.ToString()""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ret
}
",
                (type: "class", useBoolReturns: false) => @"
{
  // Code size       47 (0x2f)
  .maxstack  5
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  ldstr      ""Literal""
  IL_000d:  callvirt   ""void CustomHandler.AppendLiteral(string)""
  IL_0012:  dup
  IL_0013:  ldc.i4.1
  IL_0014:  box        ""int""
  IL_0019:  ldc.i4.2
  IL_001a:  ldstr      ""f""
  IL_001f:  callvirt   ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0024:  callvirt   ""string object.ToString()""
  IL_0029:  call       ""void System.Console.WriteLine(string)""
  IL_002e:  ret
}
",
                _ => throw ExceptionUtilities.Unreachable
            };
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void CustomHandlerMethodArgument()
        {
            var code = @"
M($""{1,2:f}Literal"");
void M(CustomHandler b)
{
    System.Console.WriteLine(b.ToString());
}";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL(@"<top-level-statements-entry-point>", @"
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  call       ""void <Program>$.<<Main>$>g__M|0_0(CustomHandler)""
  IL_0031:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void ExplicitHandlerCast_InCode()
        {
            var code = @"System.Console.WriteLine((CustomHandler)$""{1,2:f}Literal"");";
            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            SyntaxNode syntax = tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>().Single();
            var semanticInfo = model.GetSemanticInfoSummary(syntax);
            Assert.Equal("CustomHandler", semanticInfo.Type.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Object, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.ImplicitReference, semanticInfo.ImplicitConversion.Kind);

            syntax = ((CastExpressionSyntax)syntax).Expression;
            Assert.IsType<InterpolatedStringExpressionSyntax>(syntax);
            semanticInfo = model.GetSemanticInfoSummary(syntax);
            Assert.Equal(SpecialType.System_String, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_String, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            // PROTOTYPE(interp-string): Assert cast is explicit after IOperation is implemented

            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       42 (0x2a)
  .maxstack  5
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  ldc.i4.2
  IL_000f:  ldstr      ""f""
  IL_0014:  callvirt   ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0019:  dup
  IL_001a:  ldstr      ""Literal""
  IL_001f:  callvirt   ""void CustomHandler.AppendLiteral(string)""
  IL_0024:  call       ""void System.Console.WriteLine(object)""
  IL_0029:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void HandlerConversionPreferredOverStringForNonConstant()
        {
            var code = @"
C.M($""{1,2:f}Literal"");
class C
{
    public static void M(CustomHandler b)
    {
        System.Console.WriteLine(b.ToString());
    }
    public static void M(string s)
    {
        throw null;
    }
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL(@"<top-level-statements-entry-point>", @"
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  call       ""void C.M(CustomHandler)""
  IL_0031:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void StringPreferredOverHandlerConversionForConstant()
        {
            var code = @"
C.M($""{""Literal""}"");
class C
{
    public static void M(CustomHandler b)
    {
        throw null;
    }
    public static void M(string s)
    {
        System.Console.WriteLine(s);
    }
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: @"Literal");

            verifier.VerifyIL(@"<top-level-statements-entry-point>", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""Literal""
  IL_0005:  call       ""void C.M(string)""
  IL_000a:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void HandlerConversionPreferredOverStringForNonConstant_AttributeConstructor()
        {
            var code = @"
using System;

[Attr($""{1}"")]
class Attr : Attribute
{
    public Attr(string s) {}
    public Attr(CustomHandler c) {}
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (4,2): error CS0181: Attribute constructor parameter 'c' has type 'CustomHandler', which is not a valid attribute parameter type
                // [Attr($"{1}")]
                Diagnostic(ErrorCode.ERR_BadAttributeParamType, "Attr").WithArguments("c", "CustomHandler").WithLocation(4, 2)
            );
            VerifyInterpolatedStringExpression(comp);

            var attr = comp.SourceAssembly.SourceModule.GlobalNamespace.GetTypeMember("Attr");
            Assert.Equal("Attr..ctor(CustomHandler c)", attr.GetAttributes().Single().AttributeConstructor.ToTestDisplayString());
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void StringPreferredOverHandlerConversionForConstant_AttributeConstructor()
        {
            var code = @"
using System;

[Attr($""{""Literal""}"")]
class Attr : Attribute
{
    public Attr(string s) {}
    public Attr(CustomHandler c) {}
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate);

            void validate(ModuleSymbol m)
            {
                var attr = m.GlobalNamespace.GetTypeMember("Attr");
                Assert.Equal("Attr..ctor(System.String s)", attr.GetAttributes().Single().AttributeConstructor.ToTestDisplayString());
            }
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void MultipleBuilderTypes()
        {
            var code = @"
C.M($"""");

class C
{
    public static void M(CustomHandler1 c) => throw null;
    public static void M(CustomHandler2 c) => throw null;
}";

            var comp = CreateCompilation(new[]
            {
                code,
                GetCustomHandlerType("CustomHandler1", "struct", useBoolReturns: false),
                GetCustomHandlerType("CustomHandler2", "struct", useBoolReturns: false, includeOneTimeHelpers: false)
            }, parseOptions: TestOptions.RegularPreview);

            comp.VerifyDiagnostics(
                // (2,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(CustomHandler1)' and 'C.M(CustomHandler2)'
                // C.M($"");
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(CustomHandler1)", "C.M(CustomHandler2)").WithLocation(2, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericOverloadResolution_01()
        {
            var code = @"
using System;

C.M($""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t) => throw null;
    public static void M(CustomHandler c) => Console.WriteLine(c);
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  call       ""void C.M(CustomHandler)""
  IL_0031:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericOverloadResolution_02()
        {
            var code = @"
using System;

C.M($""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t) where T : CustomHandler => throw null;
    public static void M(CustomHandler c) => Console.WriteLine(c);
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       50 (0x32)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  call       ""void C.M(CustomHandler)""
  IL_0031:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericOverloadResolution_03()
        {
            var code = @"
C.M($""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t) where T : CustomHandler => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,3): error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'C.M<T>(T)'. There is no implicit reference conversion from 'string' to 'CustomHandler'.
                // C.M($"{1,2:f}Literal");
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "M").WithArguments("C.M<T>(T)", "CustomHandler", "T", "string").WithLocation(2, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericInference_01()
        {
            var code = @"
C.M($""{1,2:f}Literal"", default(CustomHandler));
C.M(default(CustomHandler), $""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t1, T t2) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,3): error CS0411: The type arguments for method 'C.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // C.M($"{1,2:f}Literal", default(CustomHandler));
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, T)").WithLocation(2, 3),
                // (3,3): error CS0411: The type arguments for method 'C.M<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // C.M(default(CustomHandler), $"{1,2:f}Literal");
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, T)").WithLocation(3, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericInference_02()
        {
            var code = @"
using System;
C.M(default(CustomHandler), () => $""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t1, Func<T> t2) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,3): error CS0411: The type arguments for method 'C.M<T>(T, Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                // C.M(default(CustomHandler), () => $"{1,2:f}Literal");
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M").WithArguments("C.M<T>(T, System.Func<T>)").WithLocation(3, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericInference_03()
        {
            var code = @"
using System;
C.M($""{1,2:f}Literal"", default(CustomHandler));

class C
{
    public static void M<T>(T t1, T t2) => Console.WriteLine(t1);
}

partial class CustomHandler
{
    public static implicit operator CustomHandler(string s) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       51 (0x33)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  ldnull
  IL_002d:  call       ""void C.M<CustomHandler>(CustomHandler, CustomHandler)""
  IL_0032:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void GenericInference_04()
        {
            var code = @"
using System;
C.M(default(CustomHandler), () => $""{1,2:f}Literal"");

class C
{
    public static void M<T>(T t1, Func<T> t2) => Console.WriteLine(t2());
}

partial class CustomHandler
{
    public static implicit operator CustomHandler(string s) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<Program>$.<>c.<<Main>$>b__0_0()", @"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_01()
        {
            var code = @"
using System;
Func<CustomHandler> f = () => $""{1,2:f}Literal"";
Console.WriteLine(f());
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerify(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0()", @"
{
  // Code size       45 (0x2d)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldc.i4.1
  IL_000a:  box        ""int""
  IL_000f:  ldc.i4.2
  IL_0010:  ldstr      ""f""
  IL_0015:  callvirt   ""bool CustomHandler.AppendFormatted(object, int, string)""
  IL_001a:  brfalse.s  IL_0029
  IL_001c:  ldloc.0
  IL_001d:  ldstr      ""Literal""
  IL_0022:  callvirt   ""bool CustomHandler.AppendLiteral(string)""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldc.i4.0
  IL_002a:  pop
  IL_002b:  ldloc.0
  IL_002c:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_02()
        {
            var code = @"
using System;
CultureInfoNormalizer.Normalize();
C.M(() => $""{1,2:f}Literal"");

class C
{
    public static void M(Func<string> f) => Console.WriteLine(f());
    public static void M(Func<CustomHandler> f) => throw null;
}
";

            // Interpolated string handler conversions are not considered when determining the natural type of an expression: the natural return type of this lambda is string,
            // so we don't even consider that there is a conversion from interpolated string expression to CustomHandler here (Sections 12.6.3.13 and 12.6.3.15 of the spec).
            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: true) }, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: @"1.00Literal");

            // No DefaultInterpolatedStringHandler was included in the compilation, so it falls back to string.Format
            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0()", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldstr      ""{0,2:f}Literal""
  IL_0005:  ldc.i4.1
  IL_0006:  box        ""int""
  IL_000b:  call       ""string string.Format(string, object)""
  IL_0010:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_03()
        {
            // Same as 2, but using a type that isn't allowed in an interpolated string. There is an implicit conversion error on the ref struct
            // when converting to a string, because S cannot be a component of an interpolated string. This conversion error causes the lambda to
            // fail to bind as Func<string>, even though the natural return type is string, and the only successful bind is Func<CustomHandler>.

            var code = @"
using System;
C.M(() => $""{new S { Field = ""Field"" }}"");

static class C
{
    public static void M(Func<string> f) => throw null;
    public static void M(Func<CustomHandler> f) => Console.WriteLine(f());
}

public partial class CustomHandler
{
    public void AppendFormatted(S value) => _builder.AppendLine(""value:"" + value.Field);
}
public ref struct S
{
    public string Field { get; set; }
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial class", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"value:Field");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0()", @"
{
  // Code size       35 (0x23)
  .maxstack  4
  .locals init (S V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  ldloca.s   V_0
  IL_000a:  initobj    ""S""
  IL_0010:  ldloca.s   V_0
  IL_0012:  ldstr      ""Field""
  IL_0017:  call       ""void S.Field.set""
  IL_001c:  ldloc.0
  IL_001d:  callvirt   ""void CustomHandler.AppendFormatted(S)""
  IL_0022:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_04()
        {
            // Same as 3, but with S added to DefaultInterpolatedStringHandler (which then allows the lambda to be bound as Func<string>, matching the natural return type)

            var code = @"
using System;
C.M(() => $""{new S { Field = ""Field"" }}"");

static class C
{
    public static void M(Func<string> f) => Console.WriteLine(f());
    public static void M(Func<CustomHandler> f) => throw null;
}

public partial class CustomHandler
{
    public void AppendFormatted(S value) => throw null;
}
public ref struct S
{
    public string Field { get; set; }
}
namespace System.Runtime.CompilerServices
{
    public ref partial struct DefaultInterpolatedStringHandler
    {
        public void AppendFormatted(S value) => _builder.AppendLine(""value:"" + value.Field);
    }
}
";

            string[] source = new[] {
                code,
                GetCustomHandlerType("CustomHandler", "partial class", useBoolReturns: false),
                GetInterpolatedStringHandlerDefinition(includeSpanOverloads: false, useDefaultParameters: true, useBoolReturns: false)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (3,11): error CS8652: The feature 'interpolated string handlers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // C.M(() => $"{new S { Field = "Field" }}");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"$""{new S { Field = ""Field"" }}""").WithArguments("interpolated string handlers").WithLocation(3, 11),
                // (3,14): error CS8652: The feature 'interpolated string handlers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // C.M(() => $"{new S { Field = "Field" }}");
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"new S { Field = ""Field"" }").WithArguments("interpolated string handlers").WithLocation(3, 14)
            );

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"value:Field");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0()", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Runtime.CompilerServices.DefaultInterpolatedStringHandler V_0,
                S V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""System.Runtime.CompilerServices.DefaultInterpolatedStringHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloca.s   V_1
  IL_000d:  initobj    ""S""
  IL_0013:  ldloca.s   V_1
  IL_0015:  ldstr      ""Field""
  IL_001a:  call       ""void S.Field.set""
  IL_001f:  ldloc.1
  IL_0020:  call       ""void System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.AppendFormatted(S)""
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""string System.Runtime.CompilerServices.DefaultInterpolatedStringHandler.ToStringAndClear()""
  IL_002c:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_05()
        {
            var code = @"
using System;
C.M(b => 
    {
        if (b) return default(CustomHandler);
        else return $""{1,2:f}Literal"";
    });

static class C
{
    public static void M(Func<bool, string> f) => throw null;
    public static void M(Func<bool, CustomHandler> f) => Console.WriteLine(f(false));
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0(bool)", @"
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""CustomHandler""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.7
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""CustomHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  ldc.i4.2
  IL_001f:  ldstr      ""f""
  IL_0024:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  ldstr      ""Literal""
  IL_0030:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0035:  ldloc.0
  IL_0036:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_06()
        {
            // Same as 5, but with an implicit conversion from the builder type to string. This implicit conversion
            // means that a best common type can be inferred for all branches of the lambda expression (Section 12.6.3.15 of the spec)
            // and because there is a best common type, the inferred return type of the lambda is string. Since the inferred return type
            // has an identity conversion to the return type of Func<bool, string>, that is preferred.
            var code = @"
using System;
CultureInfoNormalizer.Normalize();
C.M(b => 
    {
        if (b) return default(CustomHandler);
        else return $""{1,2:f}Literal"";
    });

static class C
{
    public static void M(Func<bool, string> f) => Console.WriteLine(f(false));
    public static void M(Func<bool, CustomHandler> f) => throw null;
}
public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"1.00Literal");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0(bool)", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (CustomHandler V_0)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""CustomHandler""
  IL_000b:  ldloc.0
  IL_000c:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0011:  ret
  IL_0012:  ldstr      ""{0,2:f}Literal""
  IL_0017:  ldc.i4.1
  IL_0018:  box        ""int""
  IL_001d:  call       ""string string.Format(string, object)""
  IL_0022:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_07()
        {
            // Same as 5, but with an implicit conversion from string to the builder type.
            var code = @"
using System;
C.M(b => 
    {
        if (b) return default(CustomHandler);
        else return $""{1,2:f}Literal"";
    });

static class C
{
    public static void M(Func<bool, string> f) => Console.WriteLine(f(false));
    public static void M(Func<bool, CustomHandler> f) => Console.WriteLine(f(false));
}
public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string s) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL(@"<Program>$.<>c.<<Main>$>b__0_0(bool)", @"
{
  // Code size       55 (0x37)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""CustomHandler""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.7
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""CustomHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  ldc.i4.2
  IL_001f:  ldstr      ""f""
  IL_0024:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  ldstr      ""Literal""
  IL_0030:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0035:  ldloc.0
  IL_0036:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaReturnInference_08()
        {
            // Same as 5, but with an implicit conversion from the builder type to string and from string to the builder type.
            var code = @"
using System;
C.M(b => 
    {
        if (b) return default(CustomHandler);
        else return $""{1,2:f}Literal"";
    });

static class C
{
    public static void M(Func<bool, string> f) => Console.WriteLine(f(false));
    public static void M(Func<bool, CustomHandler> f) => throw null;
}
public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => throw null;
    public static implicit operator CustomHandler(string c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (3,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Func<bool, string>)' and 'C.M(Func<bool, CustomHandler>)'
                // C.M(b => 
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Func<bool, string>)", "C.M(System.Func<bool, CustomHandler>)").WithLocation(3, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void LambdaInference_AmbiguousInOlderLangVersions()
        {
            var code = @"
using System;
C.M(param => 
    {
        param = $""{1}"";
    });

static class C
{
    public static void M(Action<string> f) => throw null;
    public static void M(Action<CustomHandler> f) => throw null;
}
";

            var source = new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) };
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);

            // This successful emit is being caused by https://github.com/dotnet/roslyn/issues/53761, along with the duplicate diagnostics in LambdaReturnInference_04
            // We should not be changing binding behavior based on LangVersion.
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (3,3): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(Action<string>)' and 'C.M(Action<CustomHandler>)'
                // C.M(param => 
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(System.Action<string>)", "C.M(System.Action<CustomHandler>)").WithLocation(3, 3)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_01()
        {
            var code = @"
using System;

var x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brtrue.s   IL_0038
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.7
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""CustomHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  ldc.i4.2
  IL_001f:  ldstr      ""f""
  IL_0024:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  ldstr      ""Literal""
  IL_0030:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0035:  ldloc.0
  IL_0036:  br.s       IL_0041
  IL_0038:  ldloca.s   V_0
  IL_003a:  initobj    ""CustomHandler""
  IL_0040:  ldloc.0
  IL_0041:  box        ""CustomHandler""
  IL_0046:  call       ""void System.Console.WriteLine(object)""
  IL_004b:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_02()
        {
            // Same as 01, but with a conversion from CustomHandler to string. The rules here are similar to LambdaReturnInference_06
            var code = @"
using System;

CultureInfoNormalizer.Normalize();
var x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"1.00Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (CustomHandler V_0)
  IL_0000:  call       ""void CultureInfoNormalizer.Normalize()""
  IL_0005:  ldc.i4.0
  IL_0006:  box        ""bool""
  IL_000b:  unbox.any  ""bool""
  IL_0010:  brtrue.s   IL_0024
  IL_0012:  ldstr      ""{0,2:f}Literal""
  IL_0017:  ldc.i4.1
  IL_0018:  box        ""int""
  IL_001d:  call       ""string string.Format(string, object)""
  IL_0022:  br.s       IL_0032
  IL_0024:  ldloca.s   V_0
  IL_0026:  initobj    ""CustomHandler""
  IL_002c:  ldloc.0
  IL_002d:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0032:  call       ""void System.Console.WriteLine(string)""
  IL_0037:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_03()
        {
            // Same as 02, but with a target-type
            var code = @"
using System;

CustomHandler x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,19): error CS0029: Cannot implicitly convert type 'string' to 'CustomHandler'
                // CustomHandler x = (bool)(object)false ? default(CustomHandler) : $"{1,2:f}Literal";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"(bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal""").WithArguments("string", "CustomHandler").WithLocation(4, 19)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_04()
        {
            // Same 01, but with a conversion from string to CustomHandler. The rules here are similar to LambdaReturnInference_07
            var code = @"
using System;

var x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brtrue.s   IL_0038
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.7
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""CustomHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  ldc.i4.2
  IL_001f:  ldstr      ""f""
  IL_0024:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  ldstr      ""Literal""
  IL_0030:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0035:  ldloc.0
  IL_0036:  br.s       IL_0041
  IL_0038:  ldloca.s   V_0
  IL_003a:  initobj    ""CustomHandler""
  IL_0040:  ldloc.0
  IL_0041:  box        ""CustomHandler""
  IL_0046:  call       ""void System.Console.WriteLine(object)""
  IL_004b:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_05()
        {
            // Same 01, but with a conversion from string to CustomHandler and CustomHandler to string.
            var code = @"
using System;

var x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,9): error CS0172: Type of conditional expression cannot be determined because 'CustomHandler' and 'string' implicitly convert to one another
                // var x = (bool)(object)false ? default(CustomHandler) : $"{1,2:f}Literal";
                Diagnostic(ErrorCode.ERR_AmbigQM, @"(bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal""").WithArguments("CustomHandler", "string").WithLocation(4, 9)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void TernaryTypes_06()
        {
            // Same 05, but with a target type
            var code = @"
using System;

CustomHandler x = (bool)(object)false ? default(CustomHandler) : $""{1,2:f}Literal"";
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
    public static implicit operator string(CustomHandler c) => c.ToString();
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brtrue.s   IL_0038
  IL_000d:  ldloca.s   V_0
  IL_000f:  ldc.i4.7
  IL_0010:  ldc.i4.1
  IL_0011:  call       ""CustomHandler..ctor(int, int)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldc.i4.1
  IL_0019:  box        ""int""
  IL_001e:  ldc.i4.2
  IL_001f:  ldstr      ""f""
  IL_0024:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0029:  ldloca.s   V_0
  IL_002b:  ldstr      ""Literal""
  IL_0030:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0035:  ldloc.0
  IL_0036:  br.s       IL_0041
  IL_0038:  ldloca.s   V_0
  IL_003a:  initobj    ""CustomHandler""
  IL_0040:  ldloc.0
  IL_0041:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0046:  call       ""void System.Console.WriteLine(string)""
  IL_004b:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_01()
        {
            // Switch expressions infer a best type based on _types_, not based on expressions (section 12.6.3.15 of the spec). Because this is based on types
            // and not on expression conversions, no best type can be found for this switch expression.

            var code = @"
using System;

var x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,29): error CS8506: No best type was found for the switch expression.
                // var x = (bool)(object)false switch { true => default(CustomHandler), false => $"{1,2:f}Literal" };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(4, 29)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_02()
        {
            // Same as 01, but with a conversion from CustomHandler. This allows the switch expression to infer a best-common type, which is string.
            var code = @"
using System;

CultureInfoNormalizer.Normalize();
var x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"1.00Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (string V_0,
                CustomHandler V_1)
  IL_0000:  call       ""void CultureInfoNormalizer.Normalize()""
  IL_0005:  ldc.i4.0
  IL_0006:  box        ""bool""
  IL_000b:  unbox.any  ""bool""
  IL_0010:  brfalse.s  IL_0023
  IL_0012:  ldloca.s   V_1
  IL_0014:  initobj    ""CustomHandler""
  IL_001a:  ldloc.1
  IL_001b:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0020:  stloc.0
  IL_0021:  br.s       IL_0034
  IL_0023:  ldstr      ""{0,2:f}Literal""
  IL_0028:  ldc.i4.1
  IL_0029:  box        ""int""
  IL_002e:  call       ""string string.Format(string, object)""
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  call       ""void System.Console.WriteLine(string)""
  IL_003a:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_03()
        {
            // Same 02, but with a target-type. The natural type will fail to compile, so the switch will use a target type (unlike TernaryTypes_03, which fails to compile).
            var code = @"
using System;

CustomHandler x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator string(CustomHandler c) => c.ToString();
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (CustomHandler V_0,
                CustomHandler V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brfalse.s  IL_0019
  IL_000d:  ldloca.s   V_1
  IL_000f:  initobj    ""CustomHandler""
  IL_0015:  ldloc.1
  IL_0016:  stloc.0
  IL_0017:  br.s       IL_0043
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.7
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""CustomHandler..ctor(int, int)""
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldc.i4.1
  IL_0025:  box        ""int""
  IL_002a:  ldc.i4.2
  IL_002b:  ldstr      ""f""
  IL_0030:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldstr      ""Literal""
  IL_003c:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0041:  ldloc.1
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0049:  call       ""void System.Console.WriteLine(string)""
  IL_004e:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_04()
        {
            // Same as 01, but with a conversion to CustomHandler. This allows the switch expression to infer a best-common type, which is CustomHandler.
            var code = @"
using System;

var x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (CustomHandler V_0,
                CustomHandler V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brfalse.s  IL_0019
  IL_000d:  ldloca.s   V_1
  IL_000f:  initobj    ""CustomHandler""
  IL_0015:  ldloc.1
  IL_0016:  stloc.0
  IL_0017:  br.s       IL_0043
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.7
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""CustomHandler..ctor(int, int)""
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldc.i4.1
  IL_0025:  box        ""int""
  IL_002a:  ldc.i4.2
  IL_002b:  ldstr      ""f""
  IL_0030:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldstr      ""Literal""
  IL_003c:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0041:  ldloc.1
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  box        ""CustomHandler""
  IL_0049:  call       ""void System.Console.WriteLine(object)""
  IL_004e:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_05()
        {
            // Same as 01, but with conversions in both directions. No best common type can be found.
            var code = @"
using System;

var x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
    public static implicit operator string(CustomHandler c) => throw null;
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,29): error CS8506: No best type was found for the switch expression.
                // var x = (bool)(object)false switch { true => default(CustomHandler), false => $"{1,2:f}Literal" };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(4, 29)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void SwitchTypes_06()
        {
            // Same as 05, but with a target type.
            var code = @"
using System;

CustomHandler x = (bool)(object)false switch { true => default(CustomHandler), false => $""{1,2:f}Literal"" };
Console.WriteLine(x);

public partial struct CustomHandler
{
    public static implicit operator CustomHandler(string c) => throw null;
    public static implicit operator string(CustomHandler c) => c.ToString();
}
";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       79 (0x4f)
  .maxstack  4
  .locals init (CustomHandler V_0,
                CustomHandler V_1)
  IL_0000:  ldc.i4.0
  IL_0001:  box        ""bool""
  IL_0006:  unbox.any  ""bool""
  IL_000b:  brfalse.s  IL_0019
  IL_000d:  ldloca.s   V_1
  IL_000f:  initobj    ""CustomHandler""
  IL_0015:  ldloc.1
  IL_0016:  stloc.0
  IL_0017:  br.s       IL_0043
  IL_0019:  ldloca.s   V_1
  IL_001b:  ldc.i4.7
  IL_001c:  ldc.i4.1
  IL_001d:  call       ""CustomHandler..ctor(int, int)""
  IL_0022:  ldloca.s   V_1
  IL_0024:  ldc.i4.1
  IL_0025:  box        ""int""
  IL_002a:  ldc.i4.2
  IL_002b:  ldstr      ""f""
  IL_0030:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0035:  ldloca.s   V_1
  IL_0037:  ldstr      ""Literal""
  IL_003c:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0041:  ldloc.1
  IL_0042:  stloc.0
  IL_0043:  ldloc.0
  IL_0044:  call       ""string CustomHandler.op_Implicit(CustomHandler)""
  IL_0049:  call       ""void System.Console.WriteLine(string)""
  IL_004e:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void PassAsRefWithoutKeyword_01()
        {
            var code = @"
M($""{1,2:f}Literal"");

void M(ref CustomHandler c) => System.Console.WriteLine(c);";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""f""
  IL_0017:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldstr      ""Literal""
  IL_0023:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""void <Program>$.<<Main>$>g__M|0_0(ref CustomHandler)""
  IL_002f:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void PassAsRefWithoutKeyword_02()
        {
            var code = @"
M($""{1,2:f}Literal"");
M(ref $""{1,2:f}Literal"");

void M(ref CustomHandler c) => System.Console.WriteLine(c);";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (2,3): error CS1620: Argument 1 must be passed with the 'ref' keyword
                // M($"{1,2:f}Literal");
                Diagnostic(ErrorCode.ERR_BadArgRef, @"$""{1,2:f}Literal""").WithArguments("1", "ref").WithLocation(2, 3),
                // (3,7): error CS1510: A ref or out value must be an assignable variable
                // M(ref $"{1,2:f}Literal");
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, @"$""{1,2:f}Literal""").WithLocation(3, 7)
            );
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void PassAsRefWithoutKeyword_03()
        {
            var code = @"
M($""{1,2:f}Literal"");

void M(in CustomHandler c) => System.Console.WriteLine(c);";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "class", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       45 (0x2d)
  .maxstack  5
  .locals init (CustomHandler V_0)
  IL_0000:  ldc.i4.7
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""CustomHandler..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  ldc.i4.1
  IL_0009:  box        ""int""
  IL_000e:  ldc.i4.2
  IL_000f:  ldstr      ""f""
  IL_0014:  callvirt   ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_0019:  dup
  IL_001a:  ldstr      ""Literal""
  IL_001f:  callvirt   ""void CustomHandler.AppendLiteral(string)""
  IL_0024:  stloc.0
  IL_0025:  ldloca.s   V_0
  IL_0027:  call       ""void <Program>$.<<Main>$>g__M|0_0(in CustomHandler)""
  IL_002c:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void PassAsRefWithoutKeyword_04()
        {
            var code = @"
M($""{1,2:f}Literal"");

void M(in CustomHandler c) => System.Console.WriteLine(c);";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""f""
  IL_0017:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldstr      ""Literal""
  IL_0023:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0028:  ldloca.s   V_0
  IL_002a:  call       ""void <Program>$.<<Main>$>g__M|0_0(in CustomHandler)""
  IL_002f:  ret
}
");
        }

        [ConditionalTheory(typeof(NoIOperationValidation))]
        [CombinatorialData]
        public void RefOverloadResolution_Struct([CombinatorialValues("in", "ref")] string refKind)
        {
            var code = @"
C.M($""{1,2:f}Literal"");

class C
{
    public static void M(CustomHandler c) => System.Console.WriteLine(c);
    public static void M(" + refKind + @" CustomHandler c) => System.Console.WriteLine(c);
}";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""f""
  IL_0017:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldstr      ""Literal""
  IL_0023:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0028:  ldloc.0
  IL_0029:  call       ""void C.M(CustomHandler)""
  IL_002e:  ret
}
");
        }

        [ConditionalTheory(typeof(NoIOperationValidation))]
        [CombinatorialData]
        public void RefOverloadResolution_Class([CombinatorialValues("in", "ref")] string refKind)
        {
            var code = @"
C.M($""{1,2:f}Literal"");

class C
{
    public static void M(CustomHandler c) => System.Console.WriteLine(c);
    public static void M(" + refKind + @" CustomHandler c) => System.Console.WriteLine(c);
}";

            var comp = CreateCompilation(new[] { code, GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: false) }, parseOptions: TestOptions.RegularPreview, targetFramework: TargetFramework.NetCoreApp);
            VerifyInterpolatedStringExpression(comp);
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (CustomHandler V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""f""
  IL_0017:  call       ""void CustomHandler.AppendFormatted(object, int, string)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldstr      ""Literal""
  IL_0023:  call       ""void CustomHandler.AppendLiteral(string)""
  IL_0028:  ldloc.0
  IL_0029:  call       ""void C.M(CustomHandler)""
  IL_002e:  ret
}
");
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void RefOverloadResolution_MultipleBuilderTypes()
        {
            var code = @"
C.M($""{1,2:f}Literal"");

class C
{
    public static void M(CustomHandler1 c) => System.Console.WriteLine(c);
    public static void M(ref CustomHandler2 c) => throw null;
}";

            var comp = CreateCompilation(new[]
            {
                code,
                GetCustomHandlerType("CustomHandler1", "struct", useBoolReturns: false),
                GetCustomHandlerType("CustomHandler2", "struct", useBoolReturns: false, includeOneTimeHelpers: false)
            }, parseOptions: TestOptions.RegularPreview);
            VerifyInterpolatedStringExpression(comp, "CustomHandler1");
            var verifier = CompileAndVerifyOnCorrectPlatforms(comp, expectedOutput: @"
value:1
alignment:2
format:f
literal:Literal");

            verifier.VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       47 (0x2f)
  .maxstack  4
  .locals init (CustomHandler1 V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.7
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""CustomHandler1..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""int""
  IL_0011:  ldc.i4.2
  IL_0012:  ldstr      ""f""
  IL_0017:  call       ""void CustomHandler1.AppendFormatted(object, int, string)""
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldstr      ""Literal""
  IL_0023:  call       ""void CustomHandler1.AppendLiteral(string)""
  IL_0028:  ldloc.0
  IL_0029:  call       ""void C.M(CustomHandler1)""
  IL_002e:  ret
}
");
        }

        private const string InterpolatedStringHandlerArgumentAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
    {
        public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = new string[] { argument };
        public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;
        public string[] Arguments { get; }
    }
}
";

        private const string InterpolatedStringHandlerAttributesVB = @"
Namespace System.Runtime.CompilerServices
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class InterpolatedStringHandlerAttribute
        Inherits Attribute
    End Class
    <AttributeUsage(AttributeTargets.Parameter, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class InterpolatedStringHandlerArgumentAttribute
        Inherits Attribute

        Public Sub New(argument As String)
            Arguments = { argument }
        End Sub

        Public Sub New(ParamArray arguments() as String)
            Me.Arguments = arguments
        End Sub

        Public ReadOnly Property Arguments As String()
    End Class
End Namespace
";

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NonHandlerType()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M([InterpolatedStringHandlerArgumentAttribute] string s) {}
}
";

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS9007: 'string' is not an interpolated string handler type.
                //     void M([InterpolatedStringHandlerArgumentAttribute] string s) {}
                Diagnostic(ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType, "InterpolatedStringHandlerArgumentAttribute").WithArguments("string").WithLocation(5, 13)
            );

            var sParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           sParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(sParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(sParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NonHandlerType_Metadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i as Integer, <InterpolatedStringHandlerArgument(""i"")> c As String)
    End Sub
End Class
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var sParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           sParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(sParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(sParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_InvalidArgument()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M([InterpolatedStringHandlerArgumentAttribute(1)] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,56): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //     void M([InterpolatedStringHandlerArgumentAttribute(1)] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(5, 56)
            );

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.False(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_01()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M([InterpolatedStringHandlerArgumentAttribute(""NonExistant"")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,13): error CS9006: 'NonExistant' is not a valid parameter name from 'C.M(CustomHandler)'.
                //     void M([InterpolatedStringHandlerArgumentAttribute("NonExistant")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgumentAttribute(""NonExistant"")").WithArguments("NonExistant", "C.M(CustomHandler)").WithLocation(5, 13)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_01_FromMetadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(<InterpolatedStringHandlerArgument(""NonExistant"")> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_02()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M(int i, [InterpolatedStringHandlerArgumentAttribute(""i"", ""NonExistant"")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9006: 'NonExistant' is not a valid parameter name from 'C.M(int, CustomHandler)'.
                //     void M(int i, [InterpolatedStringHandlerArgumentAttribute("i", "NonExistant")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgumentAttribute(""i"", ""NonExistant"")").WithArguments("NonExistant", "C.M(int, CustomHandler)").WithLocation(5, 20)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_02_FromMetadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i As Integer, <InterpolatedStringHandlerArgument(""i"", ""NonExistant"")> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_03()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M(int i, [InterpolatedStringHandlerArgumentAttribute(""NonExistant1"", ""NonExistant2"")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9006: 'NonExistant1' is not a valid parameter name from 'C.M(int, CustomHandler)'.
                //     void M(int i, [InterpolatedStringHandlerArgumentAttribute("NonExistant1", "NonExistant2")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgumentAttribute(""NonExistant1"", ""NonExistant2"")").WithArguments("NonExistant1", "C.M(int, CustomHandler)").WithLocation(5, 20),
                // (5,20): error CS9006: 'NonExistant2' is not a valid parameter name from 'C.M(int, CustomHandler)'.
                //     void M(int i, [InterpolatedStringHandlerArgumentAttribute("NonExistant1", "NonExistant2")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgumentAttribute(""NonExistant1"", ""NonExistant2"")").WithArguments("NonExistant2", "C.M(int, CustomHandler)").WithLocation(5, 20)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_UnknownName_03_FromMetadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i As Integer, <InterpolatedStringHandlerArgument(""NonExistant1"", ""NonExistant2"")> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_ReferenceSelf()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M(int i, [InterpolatedStringHandlerArgumentAttribute(""c"")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9010: InterpolatedStringHandlerArgumentAttribute arguments cannot refer to the parameter the attribute is used on.
                //     void M(int i, [InterpolatedStringHandlerArgumentAttribute("c")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_CannotUseSelfAsInterpolatedStringHandlerArgument, @"InterpolatedStringHandlerArgumentAttribute(""c"")").WithLocation(5, 20)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_ReferencesSelf_FromMetadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(<InterpolatedStringHandlerArgument(""c"")> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NullConstant()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    void M(int i, [InterpolatedStringHandlerArgumentAttribute(new string[] { null })] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9004: null is not a valid parameter name. To get access to the receiver of an instance method, use the empty string as the parameter name.
                //     void M(int i, [InterpolatedStringHandlerArgumentAttribute(new string[] { null })] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_NullInvalidInterpolatedStringHandlerArgumentName, "InterpolatedStringHandlerArgumentAttribute(new string[] { null })").WithLocation(5, 20)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NullConstant_FromMetadata_01()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i As Integer, <InterpolatedStringHandlerArgument({ Nothing })> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NullConstant_FromMetadata_02()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i As Integer, <InterpolatedStringHandlerArgument({ Nothing, ""i"" })> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_NullConstant_FromMetadata_03()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Sub M(i As Integer, <InterpolatedStringHandlerArgument(CStr(Nothing))> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_ThisOnStaticMethod()
        {
            var code = @"
using System.Runtime.CompilerServices;
class C
{
    static void M([InterpolatedStringHandlerArgumentAttribute("""")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9005: 'C.M(CustomHandler)' is not an instance method, the receiver cannot be an interpolated string handler argument.
                //     static void M([InterpolatedStringHandlerArgumentAttribute("")] CustomHandler c) {}
                Diagnostic(ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName, @"InterpolatedStringHandlerArgumentAttribute("""")").WithArguments("C.M(CustomHandler)").WithLocation(5, 20)
            );

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_ThisOnStaticMethod_FromMetadata_01()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Shared Sub M(<InterpolatedStringHandlerArgument({ """" })> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeError_ThisOnStaticMethod_FromMetadata_02()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Shared Sub M(<InterpolatedStringHandlerArgument("""")> c As CustomHandler)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerAttributeArgumentError_SubstitutedTypeSymbol()
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C<T>
{
    public void M([InterpolatedStringHandlerArgumentAttribute("""")] T t) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, C<CustomHandler> c) { _builder = null; }
}
";

            var customHander = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: false);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, customHander }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,20): error CS9007: 'T' is not an interpolated string handler type.
                //     public void M([InterpolatedStringHandlerArgumentAttribute("")] T t) { }
                Diagnostic(ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType, @"InterpolatedStringHandlerArgumentAttribute("""")").WithArguments("T").WithLocation(5, 20)
            );

            var c = comp.SourceModule.GlobalNamespace.GetTypeMember("C");
            var customHandler = comp.SourceModule.GlobalNamespace.GetTypeMember("CustomHandler");

            var substitutedC = c.WithTypeArguments(ImmutableArray.Create(TypeWithAnnotations.Create(customHandler)));

            var cParam = substitutedC.GetMethod("M").Parameters.Single();
            Assert.IsType<SubstitutedParameterSymbol>(cParam);
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            Assert.True(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeWarn_ParameterAfterHandler()
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M([InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c, int i) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i) : this()
    {
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics(
                // (5,27): warning CS9009: Parameter int occurs after CustomHandler in the parameter list, but is used as an argument for interpolated string handler conversions. This will require the caller to reorder parameters with named arguments at the call site. Consider putting the interpolated string handler parameter after all arguments involved.
                //     public static void M([InterpolatedStringHandlerArgumentAttribute("i")] CustomHandler c, int i) {}
                Diagnostic(ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter, @"InterpolatedStringHandlerArgumentAttribute(""i"")").WithArguments("int", "CustomHandler").WithLocation(5, 27)
            );

            static void validate(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.First();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(1, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
                Assert.False(cParam.HasInterpolatedStringHandlerArgumentError);
            }
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttributeWarn_ParameterAfterHandler_FromMetadata()
        {
            var vbCode = @"
Imports System.Runtime.CompilerServices
Public Class C
    Public Shared Sub M(<InterpolatedStringHandlerArgument(""i"")> c As CustomHandler, i As Integer)
    End Sub
End Class
<InterpolatedStringHandler>
Public Structure CustomHandler
End Structure
";

            var vbComp = CreateVisualBasicCompilation(new[] { vbCode, InterpolatedStringHandlerAttributesVB });
            vbComp.VerifyDiagnostics();

            var comp = CreateCompilation("", references: new[] { vbComp.EmitToImageReference() }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();

            var customHandler = comp.GetTypeByMetadataName("CustomHandler");
            Assert.True(customHandler.IsInterpolatedStringHandlerType);

            var cParam = comp.GetTypeByMetadataName("C").GetMethod("M").Parameters.First();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                           cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Equal(1, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
            Assert.False(cParam.HasInterpolatedStringHandlerArgumentError);
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttribute_MissingConstructor()
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            // https://github.com/dotnet/roslyn/issues/53981 tracks warning here in the future, with user feedback.
            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: validate, symbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(0, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
                Assert.False(cParam.HasInterpolatedStringHandlerArgumentError);
            }
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttribute_InaccessibleConstructor()
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) {}
}

public partial struct CustomHandler
{
    private CustomHandler(int literalLength, int formattedCount, int i) : this() {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            // https://github.com/dotnet/roslyn/issues/53981 tracks warning here in the future, with user feedback.
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(0, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
            }
        }

        [Theory]
        [InlineData("ref", "")]
        [InlineData("ref", "out")]
        [InlineData("ref", "in")]
        [InlineData("in", "")]
        [InlineData("in", "out")]
        [InlineData("in", "ref")]
        [InlineData("out", "")]
        [InlineData("out", "ref")]
        [InlineData("", "ref")]
        [InlineData("", "out")]
        public void InterpolatedStringHandlerArgumentAttribute_MismatchedRefTypes(string mRef, string customHandlerRef)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(" + mRef + @" int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, " + customHandlerRef + @" int i) : this() { }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            // https://github.com/dotnet/roslyn/issues/53981 tracks warning here in the future, with user feedback.
            var expected = DiagnosticDescription.None;

            if (mRef == "out")
            {
                expected = expected.Append(
                    // (5,24): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                    //     public static void M(out int i, [InterpolatedStringHandlerArgumentAttribute("i")] CustomHandler c) { }
                    Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("i").WithLocation(5, 24)
                );
            }

            if (customHandlerRef == "out")
            {
                expected = expected.Append(
                    // (10,12): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                    //     public CustomHandler(int literalLength, int formattedCount, out int i) : this() { }
                    Diagnostic(ErrorCode.ERR_ParamUnassigned, "CustomHandler").WithArguments("i").WithLocation(10, 12)
                );
            }

            comp.VerifyDiagnostics(expected);

            var cParam = comp.SourceModule.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
            AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
            Assert.Equal(0, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
        }

        [Fact]
        public void InterpolatedStringHandlerArgumentAttribute_MismatchedType()
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, string s) : this() {}
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            // https://github.com/dotnet/roslyn/issues/53981 tracks warning here in the future, with user feedback.
            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(0, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_SingleArg(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, [InterpolatedStringHandlerArgumentAttribute(""i"")] CustomHandler c) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(1).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(0, cParam.InterpolatedStringHandlerArgumentIndexes.Single());
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_MultipleArgs(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute(""i"", ""s"")] CustomHandler c) {}
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i, string s" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol verifier)
            {
                var cParam = verifier.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 0, 1 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_RefKindsMatch(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(in int i, ref string s, out object o, [InterpolatedStringHandlerArgumentAttribute(""i"", ""s"", ""o"")] CustomHandler c) { o = null; }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, in int i, ref string s, out object o" + extraConstructorArg + @") : this()
    {
        o = null;
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(3).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 0, 1, 2 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_Reordered(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute(""s"", ""i"")] CustomHandler c) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, string s, int i" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 1, 0 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_Duplicated(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute(""i"", ""i"")] CustomHandler c) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i1, int i2" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 0, 0 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_EmptyWithMatchingConstructor(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute()] CustomHandler c) { }
}
[InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount" + extraConstructorArg + @")
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, InterpolatedStringHandlerAttribute }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_EmptyWithoutMatchingConstructor(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public static void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute()] CustomHandler c) { }
}
[InterpolatedStringHandler]
public struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i" + extraConstructorArg + @")
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, InterpolatedStringHandlerAttribute }, parseOptions: TestOptions.RegularPreview);
            // https://github.com/dotnet/roslyn/issues/53981 tracks warning here in the future, with user feedback.
            CompileAndVerify(comp, symbolValidator: validate, sourceSymbolValidator: validate).VerifyDiagnostics();

            static void validate(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Empty(cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_OnIndexer(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public int this[int i, string s, [InterpolatedStringHandlerArgumentAttribute(""i"", ""s"")] CustomHandler c] { get => throw null; }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i1, string s" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetIndexer<PropertySymbol>("Item").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 0, 1 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(", out bool success")]
        public void InterpolatedStringHandlerArgumentAttribute_ThisParameter(string extraConstructorArg)
        {
            var code = @"
using System.Runtime.CompilerServices;
public class C
{
    public void M(int i, string s, [InterpolatedStringHandlerArgumentAttribute(""i"", """", ""s"")] CustomHandler c) { }
}

public partial struct CustomHandler
{
    public CustomHandler(int literalLength, int formattedCount, int i1, C c, string s" + extraConstructorArg + @") : this()
    {
" + (extraConstructorArg != "" ? "success = true;" : "") + @"
    }
}
";

            var handler = GetCustomHandlerType("CustomHandler", "partial struct", useBoolReturns: true);

            var comp = CreateCompilation(new[] { code, InterpolatedStringHandlerArgumentAttribute, handler }, parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(comp, sourceSymbolValidator: verifier, symbolValidator: verifier).VerifyDiagnostics();

            static void verifier(ModuleSymbol module)
            {
                var cParam = module.GlobalNamespace.GetTypeMember("C").GetMethod("M").Parameters.Skip(2).Single();
                AssertEx.Equal("System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
                               cParam.GetAttributes().Single().AttributeClass.ToTestDisplayString());
                Assert.Equal(new[] { 0, -1, 1 }, cParam.InterpolatedStringHandlerArgumentIndexes);
            }
        }
    }
}
