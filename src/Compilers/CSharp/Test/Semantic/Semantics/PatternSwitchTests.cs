// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternSwitchTests : PatternMatchingTestBase
    {
        [Fact]
        public void EqualConstant()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        switch (1)
        {
            case 1:
                break;
            case var i when ((i&1) == 0):
                break; // warning: unreachable (1)
            case 1: // error: duplicate case label
                break; // warning: unreachable (2)
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
                //             case 1: // error: duplicate case label
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable (1)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable (2)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void EqualConstant02()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 1 when true:
                break;
            case 1 when true: // error: subsumed
                break; // warning: unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (9,18): error CS8120: The switch case has already been handled by a previous case.
                //             case 1 when true: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "1").WithLocation(9, 18),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
        }

        [Fact]
        public void UnEqualConstant()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        switch (1)
        {
            case 2 when true:
                break; // warning: unreachable code (impossible given the value)
            case 1 when true:
                break;
            case 1: // error: handled previously
                break; // warning
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 1: // error: handled previously
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 1:").WithLocation(11, 13),
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable code (impossible given the value)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // warning
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void SimpleSubsumption01()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        string s = nameof(Main);
        switch (s)
        {
            case var n:
                break;
            case ""goo"": ; // error: subsumed by previous case
            case null: ; // error: subsumed by previous case
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (10,13): error CS8120: The switch case has already been handled by a previous case.
                //             case "goo": ; // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, @"case ""goo"":").WithLocation(10, 13),
                // (11,13): error CS8120: The switch case has already been handled by a previous case.
                //             case null: ; // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case null:").WithLocation(11, 13)
                );
        }

        [Fact]
        public void SimpleSubsumption02()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = false;
        switch (b)
        {
            case bool n:
                break;
            case ""goo"": // wrong type
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "goo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""goo""").WithArguments("string", "bool").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact]
        public void SimpleSubsumption03()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        object o = null;
        switch (o)
        {
            case IComparable i1:
                break;
            case string s: // error: subsumed by previous case
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case string s: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "string s").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void SimpleSubsumption04()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
public class X
{
    public static void Main()
    {
        object o = null;
        switch (o)
        {
            case IEnumerable i:
                break;
            case IEnumerable<string> i: // error: subsumed by previous case
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (12,18): error CS8120: The switch case has already been handled by a previous case.
                //             case IEnumerable<string> i: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "IEnumerable<string> i").WithLocation(12, 18),
                // (13,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(13, 17)
                );
        }

        [Fact]
        public void SimpleSubsumption05()
        {
            var source =
@"using System.Collections.Generic;
public class X : List<string>
{
    public static void Main()
    {
        object o = null;
        switch (o)
        {
            case List<string> list:
                break;
            case X list: // error: subsumed by previous case
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case X list: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "X list").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void JointSubsumption01()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = true;
        switch (b)
        {
            case true:
            case false:
                break;
            case var x: // error: subsumed
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var x: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var x").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void JointSubsumption02()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool? b = true;
        switch (b)
        {
            case bool b2:
            case null:
                break;
            case var x: // error: subsumed by previous cases
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var x: // error: subsumed by previous cases
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var x").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
        }

        [Fact]
        public void JointSubsumption02b()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool? b = true;
        switch (b)
        {
            case bool b2 when b==true:
            case null:
                break;
            case var x:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void TypeMismatch()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        string s = null;
        switch (s)
        {
            case int i: // error: type mismatch.
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,18): error CS8121: An expression of type 'string' cannot be handled by a pattern of type 'int'.
                //             case int i: // error: type mismatch.
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("string", "int").WithLocation(8, 18),
                // (9,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(9, 17)
                );
        }

        [Fact]
        public void JointNonSubsumption02()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        M(true);
        M(false);
        M(null);
    }
    public static void M(bool? b)
    {
        switch (b)
        {
            case false:
            case null:
                break;
            case var x: // catches `true`
                System.Console.WriteLine(true.ToString());
                break;
        }
        switch (b)
        {
            case true:
            case null:
                break;
            case var x: // catches `false`
                System.Console.WriteLine(false.ToString());
                break;
        }
        switch (b)
        {
            case true:
            case false:
                break;
            case var x: // catches `null`
                System.Console.WriteLine(""null"");
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
False
null";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(10601, "https://github.com/dotnet/roslyn/issues/10601")]
        public void NullMismatch01()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = true;
        switch (b)
        {
            case true when true:
                break;
            case null: // error: impossible given the type
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,18): error CS0037: Cannot convert null to 'bool' because it is a non-nullable value type
                //             case null: // error: impossible given the type
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("bool").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact, WorkItem(10632, "https://github.com/dotnet/roslyn/issues/10632")]
        public void TypeMismatch01()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = true;
        switch (b)
        {
            case true when true:
                break;
            case 3: // error: impossible given the type
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'int' to 'bool'
                //             case 3: // error: impossible given the type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "3").WithArguments("int", "bool").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact, WorkItem(10601, "https://github.com/dotnet/roslyn/issues/10601")]
        public void ValueMismatch01()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        byte b = 1;
        switch (b)
        {
            case 1 when true:
                break;
            case 1000: // error: impossible given the type
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,18): error CS0031: Constant value '1000' cannot be converted to a 'byte'
                //             case 1000: // error: impossible given the type
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000").WithArguments("1000", "byte").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
                );
        }

        [Fact]
        public void Subsumption01()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (args.Length)
        {
            case int i:
                break;
            case 11: // error: subsumed
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (9,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 11: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 11:").WithLocation(9, 13),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
        }

        [Fact]
        public void Subsumption02()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (args.Length == 1)
        {
            case true:
            case false:
                break;
            case bool b: // error: subsumed
                break; // unreachable
            default: //ok
                break; // unreachable because a single case handles all input
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (10,18): error CS8120: The switch case has already been handled by a previous case.
                //             case bool b: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "bool b").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable because a single case handles all input
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(13, 17)
                );
        }

        [Fact]
        public void Subsumption03()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (1)
        {
            case 2 when true:
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17)
                );
        }

        [Fact]
        public void Subsumption04()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch ((object)null)
        {
            case object o:
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17)
                );
        }

        [Fact]
        public void Subsumption04b()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch ((string)null)
        {
            case object o:
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17)
                );
        }

        [Fact]
        public void Subsumption05()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (""silly"")
        {
            case null when true:
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17)
                );
        }

        [Fact]
        public void Subsumption06()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (new object())
        {
            case null when true:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Subsumption07()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch ((bool?)null)
        {
            case null when true:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void Subsumption08()
        {
            var source =
@"public class X
{
    public static void Main(string[] args)
    {
        switch (""goo"")
        {
            case null when true:
                break;
            case null:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (9,13): error CS8120: The switch case has already been handled by a previous case.
                //             case null:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case null:").WithLocation(9, 13),
                // (8,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17),
                // (10,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 17)
                );
        }

        [Fact]
        public void EqualConstant03()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool _false = false;
        switch (1)
        {
            case 1 when _false:
                break;
            case var i:
                break; // reachable because previous case does not handle all inputs
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                );
        }

        [Fact]
        public void CascadedUnreachableDiagnostic()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = false;
        switch (b)
        {
            case true:
            case false:
                break;
            case ""goo"": // wrong type
                break;
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "goo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""goo""").WithArguments("string", "bool").WithLocation(11, 18)
                );
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6WithV7SwitchBinder).VerifyDiagnostics(
                // (11,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "goo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""goo""").WithArguments("string", "bool").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "goo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""goo""").WithArguments("string", "bool").WithLocation(11, 18)
                );
        }

        [Fact]
        public void EnumConversions01()
        {
            var source =
@"enum Color { Red=0, Blue=1, Green=2, Mauve=3 }

class Program
{
    public static void Main()
    {
        Color color = Color.Red;
        switch (color)
        {
            case Color.Blue:
                goto case 1; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
            case 0:
                break;
            case 2:          // error CS0266: Cannot implicitly convert type 'int' to 'Color'. An explicit conversion exists (are you missing a cast?)
                goto case 3; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                             // error CS0159: No such label 'case 3:' within the scope of the goto statement
                break;       // (optional) warning CS0162: Unreachable code detected
            case Color x when false:
                {}
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (18,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case Color x when false:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case Color x when false:").WithArguments("pattern matching", "7.0").WithLocation(18, 13),
                // (11,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                //                 goto case 1; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 1;").WithArguments("Color").WithLocation(11, 17),
                // (14,18): error CS0266: Cannot implicitly convert type 'int' to 'Color'. An explicit conversion exists (are you missing a cast?)
                //             case 2:          // error CS0266: Cannot implicitly convert type 'int' to 'Color'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "2").WithArguments("int", "Color").WithLocation(14, 18),
                // (15,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                //                 goto case 3; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 3;").WithArguments("Color").WithLocation(15, 17),
                // (15,17): error CS0159: No such label 'case 3:' within the scope of the goto statement
                //                 goto case 3; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 3;").WithArguments("case 3:").WithLocation(15, 17)
                );
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                //                 goto case 1; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 1;").WithArguments("Color").WithLocation(11, 17),
                // (14,18): error CS0266: Cannot implicitly convert type 'int' to 'Color'. An explicit conversion exists (are you missing a cast?)
                //             case 2:          // error CS0266: Cannot implicitly convert type 'int' to 'Color'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "2").WithArguments("int", "Color").WithLocation(14, 18),
                // (15,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                //                 goto case 3; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 3;").WithArguments("Color").WithLocation(15, 17),
                // (15,17): error CS0159: No such label 'case 3:' within the scope of the goto statement
                //                 goto case 3; // warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 3;").WithArguments("case 3:").WithLocation(15, 17)
                );
        }

        [Fact]
        public void EnumConversions02()
        {
            var source =
@"using System;

enum Color { Red=0, Blue=1, Green=2, Mauve=3 }

class Program
{
    public static void Main()
    {
        Color color = Color.Green;
        switch (color)
        {
            case Color.Red:
                goto default;
            case Color.Blue:
                goto case 0;
            case Color.Green:
                goto case 1;
            case Color.Mauve when true:
                break;
            default:
                Console.WriteLine(""done"");
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (17,17): warning CS0469: The 'goto case' value is not implicitly convertible to type 'Color'
                //                 goto case 1;
                Diagnostic(ErrorCode.WRN_GotoCaseShouldConvert, "goto case 1;").WithArguments("Color").WithLocation(17, 17)
                );
            var expectedOutput = @"done";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void WhenClause01()
        {
            // This test exercises a tricky aspect of lowering: the variables of a switch section
            // (such as i and j below) need to be in scope, from the point of view of the IL, both
            // in the when clause and in the body of the switch block. But those two bodies of
            // code are separate from each other: the when clause is part of the decision tree,
            // while the switch blocks are emitted after the entire decision tree has been emitted.
            // To get the scoping right (from the point-of-view of emit and the debugger), the compiler
            // organizes the code so that the when clauses and the section bodies are co-located
            // within the block that defines the variables of the switch section. We branch to a
            // label within the when clause, where the pattern variables are assigned followed by
            // evaluating the when condition. If it fails we branch back into the decision tree. If
            // it succeeds we branch to the user-written body of the switch block.
            var source =
@"using System;

class Program
{
    public static void Main()
    {
        M(1);
        M(""sasquatch"");
    }
    public static void M(object o)
    {
        switch (o)
        {
            case int i when i is int j:
                Console.WriteLine(j);
                break;
            case string s when s is string t:
                Console.WriteLine(t);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"1
sasquatch";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void WhenClause02()
        {
            var source =
@"using System;

class Program
{
    public static void Main()
    {
        M(0.0);
        M(-0.0);
        M(2.1);
        M(1.0);
        M(double.NaN);
        M(-double.NaN);
        M(0.0f);
        M(-0.0f);
        M(2.1f);
        M(1.0f);
        M(float.NaN);
        M(-float.NaN);
        M(0.0m);
        M(0m);
        M(2.1m);
        M(1.0m);
        M(null);
    }
    public static void M(object o)
    {
        switch (o)
        {
            case 0.0f:
                Console.WriteLine(""0.0f !"");
                break;
            case 0.0d:
                Console.WriteLine(""0.0d !"");
                break;
            case 0.0m:
                Console.WriteLine(""0.0m !"");
                break;
            case 1.0f:
                Console.WriteLine(""1.0f !"");
                break;
            case 1.0d:
                Console.WriteLine(""1.0d !"");
                break;
            case 1.0m:
                Console.WriteLine(""1.0m !"");
                break;
            case 2.0f:
                Console.WriteLine(""2.0f !"");
                break;
            case 2.0d:
                Console.WriteLine(""2.0d !"");
                break;
            case 2.0m:
                Console.WriteLine(""2.0m !"");
                break;
            case float.NaN:
                Console.WriteLine(""float.NaN !"");
                break;
            case double.NaN:
                Console.WriteLine(""double.NaN !"");
                break;
            case float f when f is float g:
                Console.WriteLine(""float "" + g);
                break;
            case double d when d is double e:
                Console.WriteLine(""double "" + e);
                break;
            case decimal d when d is decimal e:
                Console.WriteLine(""decimal "" + e);
                break;
            case null:
                Console.WriteLine(""null"");
                break;
            case object k:
                Console.WriteLine(k.GetType() + "" + "" + k);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
$@"0.0d !
0.0d !
double {2.1}
1.0d !
double.NaN !
double.NaN !
0.0f !
0.0f !
float {2.1f}
1.0f !
float.NaN !
float.NaN !
0.0m !
0.0m !
decimal {2.1m}
1.0m !
null";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem(25846, "https://github.com/dotnet/roslyn/issues/25846")]
        public void DuplicateDouble()
        {
            var source =
@"class Program
{
    public static void Main()
    {
    }
    public static void M(double d)
    {
        switch (d)
        {
            case 0.0:
            case double.NaN:
            case 1.01:
            case 1.01: // duplicate
            case 2.0:
            case 3.0:
            case 1.1:
            case 1.2:
            case 2.1:
            case 3.1:
            case 11.01:
            case 12.0:
            case 13.0:
            case 11.1:
            case 11.2:
            case 12.1:
            case 13.1:
            case 21.01:
            case 22.0:
            case 23.0:
            case 21.1:
            case 21.2:
            case 22.1:
            case 23.1:
            case -0.0: // duplicate
            case -double.NaN: // duplicate
                break;
        }
    }
    public static void M(float d)
    {
        switch (d)
        {
            case 0.0f:
            case float.NaN:
            case 1.01f:
            case 1.01f: // duplicate
            case 2.0f:
            case 3.0f:
            case 1.1f:
            case 1.2f:
            case 2.1f:
            case 3.1f:
            case 11.01f:
            case 12.0f:
            case 13.0f:
            case 11.1f:
            case 11.2f:
            case 12.1f:
            case 13.1f:
            case 21.01f:
            case 22.0f:
            case 23.0f:
            case 21.1f:
            case 21.2f:
            case 22.1f:
            case 23.1f:
            case -0.0f: // duplicate
            case -float.NaN: // duplicate
                break;
        }
    }
    public static void M(decimal d)
    {
        switch (d)
        {
            case 0.0m:
            case 1.01m:
            case 1.01m: // duplicate
            case 2.0m:
            case 3.0m:
            case 1.1m:
            case 1.2m:
            case 2.1m:
            case 3.1m:
            case 11.01m:
            case 12.0m:
            case 13.0m:
            case 11.1m:
            case 11.2m:
            case 12.1m:
            case 13.1m:
            case 21.01m:
            case 22.0m:
            case 23.0m:
            case 21.1m:
            case 21.2m:
            case 22.1m:
            case 23.1m:
            case -0.0m: // duplicate
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (13,13): error CS0152: The switch statement contains multiple cases with the label value '1.01'
                //             case 1.01: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1.01:").WithArguments(1.01.ToString()).WithLocation(13, 13),
                // (34,13): error CS0152: The switch statement contains multiple cases with the label value '0'
                //             case -0.0: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case -0.0:").WithArguments((-0.0).ToString()).WithLocation(34, 13),
                // (35,13): error CS0152: The switch statement contains multiple cases with the label value 'NaN'
                //             case -double.NaN: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case -double.NaN:").WithArguments((-double.NaN).ToString()).WithLocation(35, 13),
                // (46,13): error CS0152: The switch statement contains multiple cases with the label value '1.01'
                //             case 1.01f: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1.01f:").WithArguments(1.01f.ToString()).WithLocation(46, 13),
                // (67,13): error CS0152: The switch statement contains multiple cases with the label value '0'
                //             case -0.0f: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case -0.0f:").WithArguments((-0.0f).ToString()).WithLocation(67, 13),
                // (68,13): error CS0152: The switch statement contains multiple cases with the label value 'NaN'
                //             case -float.NaN: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case -float.NaN:").WithArguments((-float.NaN).ToString()).WithLocation(68, 13),
                // (78,13): error CS0152: The switch statement contains multiple cases with the label value '1.01'
                //             case 1.01m: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1.01m:").WithArguments(1.01m.ToString()).WithLocation(78, 13),
                // (99,13): error CS0152: The switch statement contains multiple cases with the label value '0.0'
                //             case -0.0m: // duplicate
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case -0.0m:").WithArguments((-0.0m).ToString()).WithLocation(99, 13)
                );
        }

        [Fact]
        public void NanValuesAreEqual()
        {
            var source =
@"using System;

class Program
{
    public static void Main()
    {
        M(0.0);
        M(-0.0);
        M(MakeNaN(0));
        M(MakeNaN(1));
    }
    public static void M(double d)
    {
        switch (d)
        {
            case 0:
                Console.WriteLine(""zero"");
                break;
            case double.NaN:
                Console.WriteLine(""NaN"");
                break;
            case 1: case 2: case 3: case 4: case 5:
            case 6: case 7: case 8: case 9: case 10:
                Console.WriteLine(""unexpected"");
                break;
            default:
                Console.WriteLine(""other"");
                break;
        }
    }
    public static double MakeNaN(int x)
    {
        return BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(double.NaN) ^ x);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"zero
zero
NaN
NaN";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(12573, "https://github.com/dotnet/roslyn/issues/12573")]
        public void EnumAndUnderlyingType()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        M(0);
        M(0L);
        M((byte)0);
        M(EnumA.ValueA);
        M(2);
    }

    public static void M(object value)
    {
        switch (value)
        {
            case 0:
                Console.WriteLine(""0"");
                break;
            case 0L:
                Console.WriteLine(""0L"");
                break;
            case (byte)0:
                Console.WriteLine(""(byte)0"");
                break;
            case EnumA.ValueA:
                Console.WriteLine(""EnumA.ValueA"");
                break;
            default:
                Console.WriteLine(""Default"");
                break;
        }
    }
}

public enum EnumA
{
    ValueA,
    ValueB,
    ValueC
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"0
0L
(byte)0
EnumA.ValueA
Default";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(10446, "https://github.com/dotnet/roslyn/issues/10446")]
        public void InferenceInSwitch()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        object o = 1;
        switch (o)
        {
            case var i when i.ToString() is var s:
                Console.WriteLine(s);
                break;
            case var i2:
                var s2 =  i2.ToString();
                Console.WriteLine(s2);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var sRef = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.ToString() == "s").Single();
            Assert.Equal("System.String", model.GetTypeInfo(sRef).Type.ToTestDisplayString());
            var iRef = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.ToString() == "i").Single();
            Assert.Equal("System.Object", model.GetTypeInfo(iRef).Type.ToTestDisplayString());
            var s2Ref = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.ToString() == "s2").Single();
            Assert.Equal("System.String", model.GetTypeInfo(s2Ref).Type.ToTestDisplayString());
            var i2Ref = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.ToString() == "i2").Single();
            Assert.Equal("System.Object", model.GetTypeInfo(i2Ref).Type.ToTestDisplayString());
        }

        [Fact, WorkItem(13395, "https://github.com/dotnet/roslyn/issues/13395")]
        public void CodeGenSwitchInLoop()
        {
            var source =
@"using System;

class Program
{
    public static void Main(string[] args)
    {
        bool hasB = false;
        foreach (var c in ""ab"")
        {
           switch (c)
           {
              case char b when IsB(b):
                 hasB = true;
                 break;

              default:
                 hasB = false;
                 break;
           }
        }
        Console.WriteLine(hasB);
    }

    public static bool IsB(char value)
    {
        return value == 'b';
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(13520, "https://github.com/dotnet/roslyn/issues/13520")]
        public void ConditionalPatternsCannotSubsume()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        object value = false;
        switch (value)
        {
            case true: break;
            case object o when args.Length == -1: break;
            case false: break;
            case bool b: throw null; // error: bool already handled by previous cases.
        }
    }
    public static bool IsB(char value)
    {
        return value == 'b';
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case bool b: ; // error: bool already handled by previous cases.
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "bool b").WithLocation(11, 18)
                );
        }

        [Fact, WorkItem(273713, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=273713")]
        public void ExplicitTupleConversion_Crash()
        {
            var source =
@"namespace System
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
}
class Program
{
    public static void Main(string[] args)
    {
        object[] oa = new object[] {
            (1, 2),
            (3L, 4L)
        };
        foreach (var o in oa)
        {
            switch (o)
            {
                case System.ValueTuple<int, int> z1:
                    System.Console.Write(z1.Item1);
                    break;
                case System.ValueTuple<long, long> z3:
                    System.Console.Write(z3.Item1);
                    break;
            }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "13");
        }

        [Fact, WorkItem(273713, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=273713")]
        public void TupleInPattern()
        {
            var source =
@"namespace System
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
}
class Program
{
    static void M(object o)
    {
        switch (o)
        {
            case (int, int):
            case (int x, int y):
            case (int, int) z:
            case (int a, int b) c:
            case (long, long) d:
                break;
        }
        switch (o)
        {
            case (int, int) z:
                break;
            case (long, long) d:
                break;
        }
        switch (o)
        {
            case (System.Int32, System.Int32) z:
                break;
            case (System.Int64, System.Int64) d:
                break;
        }
        {
            if (o is (int, int)) {}
            if (o is (int x, int y)) {}
            if (o is (int, int) z)) {}
            if (o is (int a, int b) c) {}
        }
        {
            if (o is (System.Int32, System.Int32)) {}
            if (o is (System.Int32 x, System.Int32 y)) {}
            if (o is (System.Int32, System.Int32) z)) {}
            if (o is (System.Int32 a, System.Int32 b) c) {}
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
                // (21,19): error CS1525: Invalid expression term 'int'
                //             case (int, int):
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(21, 19),
                // (21,24): error CS1525: Invalid expression term 'int'
                //             case (int, int):
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(21, 24),
                // (23,19): error CS1525: Invalid expression term 'int'
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(23, 19),
                // (23,24): error CS1525: Invalid expression term 'int'
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(23, 24),
                // (23,29): error CS1003: Syntax error, ':' expected
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_SyntaxError, "z").WithArguments(":", "").WithLocation(23, 29),
                // (23,31): error CS1525: Invalid expression term 'case'
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("case").WithLocation(23, 31),
                // (23,31): error CS1002: ; expected
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(23, 31),
                // (24,33): error CS1003: Syntax error, ':' expected
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(":", "").WithLocation(24, 33),
                // (24,35): error CS1525: Invalid expression term 'case'
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("case").WithLocation(24, 35),
                // (24,35): error CS1002: ; expected
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(24, 35),
                // (25,19): error CS1525: Invalid expression term 'long'
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "long").WithArguments("long").WithLocation(25, 19),
                // (25,25): error CS1525: Invalid expression term 'long'
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "long").WithArguments("long").WithLocation(25, 25),
                // (25,31): error CS1003: Syntax error, ':' expected
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(":", "").WithLocation(25, 31),
                // (30,19): error CS1525: Invalid expression term 'int'
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(30, 19),
                // (30,24): error CS1525: Invalid expression term 'int'
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(30, 24),
                // (30,29): error CS1003: Syntax error, ':' expected
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_SyntaxError, "z").WithArguments(":", "").WithLocation(30, 29),
                // (32,19): error CS1525: Invalid expression term 'long'
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "long").WithArguments("long").WithLocation(32, 19),
                // (32,25): error CS1525: Invalid expression term 'long'
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "long").WithArguments("long").WithLocation(32, 25),
                // (32,31): error CS1003: Syntax error, ':' expected
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(":", "").WithLocation(32, 31),
                // (37,47): error CS1003: Syntax error, ':' expected
                //             case (System.Int32, System.Int32) z:
                Diagnostic(ErrorCode.ERR_SyntaxError, "z").WithArguments(":", "").WithLocation(37, 47),
                // (39,47): error CS1003: Syntax error, ':' expected
                //             case (System.Int64, System.Int64) d:
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(":", "").WithLocation(39, 47),
                // (43,23): error CS1525: Invalid expression term 'int'
                //             if (o is (int, int)) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(43, 23),
                // (43,28): error CS1525: Invalid expression term 'int'
                //             if (o is (int, int)) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(43, 28),
                // (45,23): error CS1525: Invalid expression term 'int'
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(45, 23),
                // (45,28): error CS1525: Invalid expression term 'int'
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(45, 28),
                // (45,33): error CS1026: ) expected
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "z").WithLocation(45, 33),
                // (45,34): error CS1002: ; expected
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(45, 34),
                // (45,34): error CS1513: } expected
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(45, 34),
                // (46,37): error CS1026: ) expected
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "c").WithLocation(46, 37),
                // (46,38): error CS1002: ; expected
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(46, 38),
                // (46,38): error CS1513: } expected
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(46, 38),
                // (51,51): error CS1026: ) expected
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "z").WithLocation(51, 51),
                // (51,52): error CS1002: ; expected
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(51, 52),
                // (51,52): error CS1513: } expected
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(51, 52),
                // (52,55): error CS1026: ) expected
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "c").WithLocation(52, 55),
                // (52,56): error CS1002: ; expected
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(52, 56),
                // (52,56): error CS1513: } expected
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(52, 56),
                // (21,18): error CS0150: A constant value is expected
                //             case (int, int):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(21, 18),
                // (22,19): error CS8185: A declaration is not allowed in this context.
                //             case (int x, int y):
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(22, 19),
                // (22,26): error CS8185: A declaration is not allowed in this context.
                //             case (int x, int y):
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(22, 26),
                // (22,18): error CS0150: A constant value is expected
                //             case (int x, int y):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int x, int y)").WithLocation(22, 18),
                // (23,18): error CS0150: A constant value is expected
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(23, 18),
                // (24,19): error CS8185: A declaration is not allowed in this context.
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int a").WithLocation(24, 19),
                // (24,26): error CS8185: A declaration is not allowed in this context.
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int b").WithLocation(24, 26),
                // (24,18): error CS0150: A constant value is expected
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int a, int b)").WithLocation(24, 18),
                // (25,18): error CS0150: A constant value is expected
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(long, long)").WithLocation(25, 18),
                // (30,18): error CS0150: A constant value is expected
                //             case (int, int) z:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(30, 18),
                // (32,18): error CS0150: A constant value is expected
                //             case (long, long) d:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(long, long)").WithLocation(32, 18),
                // (37,19): error CS0119: 'int' is a type, which is not valid in the given context
                //             case (System.Int32, System.Int32) z:
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(37, 19),
                // (37,33): error CS0119: 'int' is a type, which is not valid in the given context
                //             case (System.Int32, System.Int32) z:
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(37, 33),
                // (39,19): error CS0119: 'long' is a type, which is not valid in the given context
                //             case (System.Int64, System.Int64) d:
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int64").WithArguments("long", "type").WithLocation(39, 19),
                // (39,33): error CS0119: 'long' is a type, which is not valid in the given context
                //             case (System.Int64, System.Int64) d:
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int64").WithArguments("long", "type").WithLocation(39, 33),
                // (43,22): error CS0150: A constant value is expected
                //             if (o is (int, int)) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(43, 22),
                // (44,23): error CS8185: A declaration is not allowed in this context.
                //             if (o is (int x, int y)) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(44, 23),
                // (44,30): error CS8185: A declaration is not allowed in this context.
                //             if (o is (int x, int y)) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(44, 30),
                // (44,22): error CS0150: A constant value is expected
                //             if (o is (int x, int y)) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int x, int y)").WithLocation(44, 22),
                // (45,22): error CS0150: A constant value is expected
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int, int)").WithLocation(45, 22),
                // (45,33): error CS0103: The name 'z' does not exist in the current context
                //             if (o is (int, int) z)) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(45, 33),
                // (46,23): error CS8185: A declaration is not allowed in this context.
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int a").WithLocation(46, 23),
                // (46,30): error CS8185: A declaration is not allowed in this context.
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int b").WithLocation(46, 30),
                // (46,22): error CS0150: A constant value is expected
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(int a, int b)").WithLocation(46, 22),
                // (46,37): error CS0103: The name 'c' does not exist in the current context
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(46, 37),
                // (49,23): error CS0119: 'int' is a type, which is not valid in the given context
                //             if (o is (System.Int32, System.Int32)) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(49, 23),
                // (49,37): error CS0119: 'int' is a type, which is not valid in the given context
                //             if (o is (System.Int32, System.Int32)) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(49, 37),
                // (50,23): error CS8185: A declaration is not allowed in this context.
                //             if (o is (System.Int32 x, System.Int32 y)) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "System.Int32 x").WithLocation(50, 23),
                // (50,39): error CS8185: A declaration is not allowed in this context.
                //             if (o is (System.Int32 x, System.Int32 y)) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "System.Int32 y").WithLocation(50, 39),
                // (50,22): error CS0150: A constant value is expected
                //             if (o is (System.Int32 x, System.Int32 y)) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(System.Int32 x, System.Int32 y)").WithLocation(50, 22),
                // (51,23): error CS0119: 'int' is a type, which is not valid in the given context
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(51, 23),
                // (51,37): error CS0119: 'int' is a type, which is not valid in the given context
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.Int32").WithArguments("int", "type").WithLocation(51, 37),
                // (51,51): error CS0103: The name 'z' does not exist in the current context
                //             if (o is (System.Int32, System.Int32) z)) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(51, 51),
                // (52,23): error CS8185: A declaration is not allowed in this context.
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "System.Int32 a").WithLocation(52, 23),
                // (52,39): error CS8185: A declaration is not allowed in this context.
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "System.Int32 b").WithLocation(52, 39),
                // (52,22): error CS0150: A constant value is expected
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_ConstantExpected, "(System.Int32 a, System.Int32 b)").WithLocation(52, 22),
                // (52,55): error CS0103: The name 'c' does not exist in the current context
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(52, 55),
                // (23,29): warning CS0162: Unreachable code detected
                //             case (int, int) z:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "z").WithLocation(23, 29),
                // (24,33): warning CS0162: Unreachable code detected
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "c").WithLocation(24, 33),
                // (25,31): warning CS0162: Unreachable code detected
                //             case (long, long) d:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "d").WithLocation(25, 31),
                // (30,29): warning CS0162: Unreachable code detected
                //             case (int, int) z:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "z").WithLocation(30, 29),
                // (32,31): warning CS0162: Unreachable code detected
                //             case (long, long) d:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "d").WithLocation(32, 31),
                // (37,47): warning CS0162: Unreachable code detected
                //             case (System.Int32, System.Int32) z:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "z").WithLocation(37, 47),
                // (39,47): warning CS0162: Unreachable code detected
                //             case (System.Int64, System.Int64) d:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "d").WithLocation(39, 47),
                // (23,29): warning CS0164: This label has not been referenced
                //             case (int, int) z:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "z").WithLocation(23, 29),
                // (24,33): warning CS0164: This label has not been referenced
                //             case (int a, int b) c:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(24, 33),
                // (25,31): warning CS0164: This label has not been referenced
                //             case (long, long) d:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(25, 31),
                // (30,29): warning CS0164: This label has not been referenced
                //             case (int, int) z:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "z").WithLocation(30, 29),
                // (32,31): warning CS0164: This label has not been referenced
                //             case (long, long) d:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(32, 31),
                // (37,47): warning CS0164: This label has not been referenced
                //             case (System.Int32, System.Int32) z:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "z").WithLocation(37, 47),
                // (39,47): warning CS0164: This label has not been referenced
                //             case (System.Int64, System.Int64) d:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "d").WithLocation(39, 47),
                // (44,23): error CS0165: Use of unassigned local variable 'x'
                //             if (o is (int x, int y)) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(44, 23),
                // (44,30): error CS0165: Use of unassigned local variable 'y'
                //             if (o is (int x, int y)) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(44, 30),
                // (46,23): error CS0165: Use of unassigned local variable 'a'
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int a").WithArguments("a").WithLocation(46, 23),
                // (46,30): error CS0165: Use of unassigned local variable 'b'
                //             if (o is (int a, int b) c) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int b").WithArguments("b").WithLocation(46, 30),
                // (50,23): error CS0165: Use of unassigned local variable 'x'
                //             if (o is (System.Int32 x, System.Int32 y)) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "System.Int32 x").WithArguments("x").WithLocation(50, 23),
                // (50,39): error CS0165: Use of unassigned local variable 'y'
                //             if (o is (System.Int32 x, System.Int32 y)) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "System.Int32 y").WithArguments("y").WithLocation(50, 39),
                // (52,23): error CS0165: Use of unassigned local variable 'a'
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "System.Int32 a").WithArguments("a").WithLocation(52, 23),
                // (52,39): error CS0165: Use of unassigned local variable 'b'
                //             if (o is (System.Int32 a, System.Int32 b) c) {}
                Diagnostic(ErrorCode.ERR_UseDefViolation, "System.Int32 b").WithArguments("b").WithLocation(52, 39)
                );
        }

        [Fact, WorkItem(273713, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=273713")]
        public void PointerConversion_Crash()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        System.IntPtr ip = (System.IntPtr)1;
        int i = 2;
        object[] oa = new object[] { ip, i };
        foreach (object o in oa)
        {
            switch (o)
            {
                case System.IntPtr a:
                    System.Console.Write((int)a);
                    break;
                case int b:
                    System.Console.Write(b);
                    break;
            }
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "12");
        }

        [Fact, WorkItem(14717, "https://github.com/dotnet/roslyn/issues/14717")]
        public void ExpressionVariableInCase_1()
        {
            string source =
@"
class Program
{
    static void Main(string[] args)
    {
        switch (true)
        {
            case new object() is int x1:
                System.Console.WriteLine(x1);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            // The point of this test is that it should not crash.
            compilation.VerifyDiagnostics(
                // (8,18): error CS0150: A constant value is expected
                //             case new object() is int x1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "new object() is int x1").WithLocation(8, 18),
                // (9,17): warning CS0162: Unreachable code detected
                //                 System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(9, 17)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
        }

        [Fact, WorkItem(14717, "https://github.com/dotnet/roslyn/issues/14717")]
        public void ExpressionVariableInCase_2()
        {
            string source =
@"class Program
{
    static void Main(string[] args)
    {
        switch (args)
        {
            case is EnvDTE.Project x1:
                System.Console.WriteLine(x1);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            // The point of this test is that it should not crash.
            compilation.VerifyDiagnostics(
                // (7,18): error CS1525: Invalid expression term 'is'
                //             case is EnvDTE.Project x1:
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "is").WithArguments("is").WithLocation(7, 18),
                // (7,21): error CS0246: The type or namespace name 'EnvDTE' could not be found (are you missing a using directive or an assembly reference?)
                //             case is EnvDTE.Project x1:
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "EnvDTE").WithArguments("EnvDTE").WithLocation(7, 21),
                // (8,17): warning CS0162: Unreachable code detected
                //                 System.Console.WriteLine(x1);
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(8, 17)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
        }

        [Fact, WorkItem(14296, "https://github.com/dotnet/roslyn/issues/14296")]
        public void PatternSwitchInLocalFunctionInGenericMethod()
        {
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(Is<string>(string.Empty));
        Console.WriteLine(Is<int>(string.Empty));
        Console.WriteLine(Is<int>(1));
        Console.WriteLine(Is<string>(1));
    }
    public static bool Is<T>(object o1)
    {
        bool Local(object o2)
        {
            switch (o2)
            {
                case T t: return true;
                default: return false;
            }
        };
        return Local(o1);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"True
False
True
False";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void CopySwitchExpression()
        {
            // This test ensures that we switch on a *copy* of the switch expression,
            // so that it is not affected by subsequent assignment to a variable appearing
            // in the switch expression.
            var source =
@"using System;
class Program
{
    public static void Main(string[] args)
    {
        int i = 1;
        switch (i)
        {
            case 1 when BP(false, i = 2): break;
            case int j when BP(false, i = 3): break;
            case 1 when BP(true, i = 4):
                Console.WriteLine(""Correct"");
                Console.WriteLine(i);
                break;
        }
    }
    static bool BP(bool b, int print)
    {
        Console.WriteLine(print);
        return b;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput =
@"2
3
4
Correct
4";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(14707, "https://github.com/dotnet/roslyn/issues/14707")]
        public void ParenthesizedGuardClause()
        {
            var source =
@"class Program
{
    public static void Main(string[] args)
    {
        object[] oa = { null, new Rectangle(1, 1), new Rectangle(1, 2) };
        foreach (object o in oa)
        {
            switch (o)
            {
                case Rectangle s when (s.Length == s.Height):
                    System.Console.WriteLine($""S {s.Length}"");
                    break;
                case Rectangle r when ((true && (r.Length != r.Height))):
                    System.Console.WriteLine($""R {r.Height} {r.Length}"");
                    break;
                default:
                    System.Console.WriteLine($""other"");
                    break;
            }
        }
    }
}
class Rectangle
{
    public int Height, Length;
    public Rectangle(int x, int y) { Height = x; Length = y; }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: @"other
S 1
R 1 2");
        }

        [Fact, WorkItem(14721, "https://github.com/dotnet/roslyn/issues/14721")]
        public void NullTest_Crash()
        {
            var source =
@"using System;

static class Program {
    static void Test(object o) {
        switch (o) {
            case var value when value != null:
                Console.WriteLine(""not null"");
                break;
            case null:
                Console.WriteLine(""null"");
            break;
        }
    }

    static void Main()
    {
        Test(1);
        Test(null);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput:
@"not null
null");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithLambda_01()
        {
            var source =
@"
class Program
{
    static void Main(string[] args)
    {
        switch((object)new A())
        {
            case A a:
                System.Action print = () => System.Console.WriteLine(a);
                print();
                break;
        }
    }
}

class A{}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = "A";
            var comp = CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithLambda_02()
        {
            var source =
@"
class Program
{
    static void Main(string[] args)
    {
        int val = 3;
        var l = new System.Collections.Generic.List<System.Action>();
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                System.Console.WriteLine(""case 1: {0}"", i);
                a = i++;
                l.Add(() => System.Console.WriteLine(a));
                goto case 2;
            case int a when a == 4:
            case 2:
                System.Console.WriteLine(""case 2: {0}"", i);
                a = i++;
                l.Add(() => System.Console.WriteLine(a));

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        foreach (var a in l)
        {
            a();
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
case 2: 2
case 1: 3
case 2: 4
3
4
3
4");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithYield_01()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        foreach (var a in Test())
        {
            System.Console.WriteLine(a);
        }
    }

    static System.Collections.Generic.IEnumerable<string> Test()
    {
        int val = 3;
        var l = new System.Collections.Generic.List<System.Action>();
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                yield return string.Format(""case 1: {0}"", i);
                a = i++;
                l.Add(() => System.Console.WriteLine(a));
                goto case 2;
            case int a when a == 4:
            case 2:
                yield return string.Format(""case 2: {0}"", i);
                a = i++;
                l.Add(() => System.Console.WriteLine(a));

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        foreach (var a in l)
        {
            a();
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
case 2: 2
case 1: 3
case 2: 4
3
4
3
4");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithYield_02()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        foreach (var a in Test())
        {
            System.Console.WriteLine(a);
        }
    }

    static System.Collections.Generic.IEnumerable<string> Test()
    {
        int val = 3;
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                yield return string.Format(""case 1: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);
                goto case 2;
            case int a when a == 4:
            case 2:
                yield return string.Format(""case 2: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
1
case 2: 2
2
case 1: 3
3
case 2: 4
4");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithYield_03()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        foreach (var a in Test())
        {
            System.Console.WriteLine(a);
        }
    }

    static System.Collections.Generic.IEnumerable<int> Test()
    {
        int val = 3;
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                System.Console.WriteLine(""case 1: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);
                goto case 2;
            case int a when a == 4:
            case 2:
                System.Console.WriteLine(""case 2: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        yield return i;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
1
case 2: 2
2
case 1: 3
3
case 2: 4
4
5");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithYield_04()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        foreach (var a in Test())
        {
            System.Console.WriteLine(a);
        }
    }

    static System.Collections.Generic.IEnumerable<string> Test()
    {
        int val = 3;
        switch(val)
        {
            case int a when TakeOutVar(out var b) && a == b:
                yield return string.Format(""case: {0}"", val);
                System.Console.WriteLine(a);
                break;
        }
    }

    static bool TakeOutVar(out int x)
    {
        x = 3;
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case: 3
3");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithAwait_01()
        {
            var source =
@"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test().Result);
    }

    static async Task<int> Test()
    {
        int val = 3;
        var l = new System.Collections.Generic.List<System.Action>();
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                System.Console.WriteLine(await GetTask(string.Format(""case 1: {0}"", i)));
                a = i++;
                l.Add(() => System.Console.WriteLine(a));
                goto case 2;
            case int a when a == 4:
            case 2:
                System.Console.WriteLine(await GetTask(string.Format(""case 2: {0}"", i)));
                a = i++;
                l.Add(() => System.Console.WriteLine(a));

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        foreach (var a in l)
        {
            a();
        }

        return i;
    }

    static async Task<string> GetTask(string val)
    {
        await Task.Yield();
        return val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
case 2: 2
case 1: 3
case 2: 4
3
4
3
4
5");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithAwait_02()
        {
            var source =
@"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test().Result);
    }

    static async Task<int> Test()
    {
        int val = 3;
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                System.Console.WriteLine(await GetTask(string.Format(""case 1: {0}"", i)));
                a = i++;
                System.Console.WriteLine(a);
                goto case 2;
            case int a when a == 4:
            case 2:
                System.Console.WriteLine(await GetTask(string.Format(""case 2: {0}"", i)));
                a = i++;
                System.Console.WriteLine(a);

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        return i;
    }

    static async Task<string> GetTask(string val)
    {
        await Task.Yield();
        return val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
1
case 2: 2
2
case 1: 3
3
case 2: 4
4
5");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithAwait_03()
        {
            var source =
@"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test().Result);
    }

    static async Task<int> Test()
    {
        int val = 3;
        int i = 1;
        switch(val)
        {
            case int a when a == 3:
            case 1:
                System.Console.WriteLine(""case 1: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);
                goto case 2;
            case int a when a == 4:
            case 2:
                System.Console.WriteLine(""case 2: {0}"", i);
                a = i++;
                System.Console.WriteLine(a);

                if (i < 4)
                {
                    goto case 1;
                }
                break;
        }

        await Task.Yield();
        return i;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case 1: 1
1
case 2: 2
2
case 1: 3
3
case 2: 4
4
5");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        public void SwitchSectionWithAwait_04()
        {
            var source =
@"
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(Test().Result);
    }

    static async Task<int> Test()
    {
        int val = 3;
        switch(val)
        {
            case int a when TakeOutVar(out var b) && a == b:
                System.Console.WriteLine(await GetTask(string.Format(""case: {0}"", val)));
                return a;
        }

        return 0;
    }

    static bool TakeOutVar(out int x)
    {
        x = 3;
        return true;
    }

    static async Task<string> GetTask(string val)
    {
        await Task.Yield();
        return val;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"case: 3
3");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        [WorkItem(401335, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=401335")]
        public void SwitchAwaitGenericsAndOptimization()
        {
            var source =
@"
using System.Threading.Tasks;

class Test
{
    static void Main()
    {
        System.Console.WriteLine(SendMessageAsync<string>(""a"").Result);
        System.Console.WriteLine(SendMessageAsync<string>('a').Result);
    }

    public static async Task<string> SendMessageAsync<T>(object response)
    {
        switch (response)
        {
            case T expected:
                return ""T"";
            default:
                return ""default"";
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (12,38): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     public static async Task<string> SendMessageAsync<T>(object response)
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "SendMessageAsync").WithLocation(12, 38)
                );
            var comp = CompileAndVerify(compilation, expectedOutput:
@"T
default");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        [WorkItem(401335, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=401335")]
        public void SwitchLambdaGenericsAndOptimization()
        {
            var source =
@"
class Test
{
    static void Main()
    {
        System.Console.WriteLine(SendMessage<string>(""a""));
        System.Console.WriteLine(SendMessage<string>('a'));
    }

    public static string SendMessage<T>(object input)
    {
        System.Func<object, string> f = (response) =>
        {
            switch (response)
            {
                case T expected:
                    return ""T"";
                default:
                    return ""default"";
            }
        };

        return f(input);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"T
default");
        }

        [Fact]
        [WorkItem(16066, "https://github.com/dotnet/roslyn/issues/16066")]
        [WorkItem(401335, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=401335")]
        public void SwitchIteratorGenericsAndOptimization()
        {
            var source =
@"
class Test
{
    static void Main()
    {
        foreach (var s in SendMessage<string>(""a""))
        {
            System.Console.WriteLine(s);
        }
        foreach (var s in SendMessage<string>('a'))
        {
            System.Console.WriteLine(s);
        }
    }

    public static System.Collections.Generic.IEnumerable<string> SendMessage<T>(object response)
    {
        switch (response)
        {
            case T expected:
                yield return ""T"";
                yield break;
            default:
                yield return ""default"";
                yield break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput:
@"T
default");
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void TupleNameDifferences_01()
        {
            var source =
@"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var list = new List<(int a, int b)>();
        switch (list)
        {
            case List<(int x, int y)> list1:
                break;
            case List<(int z, int w)> list2: // subsumed
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (13,18): error CS8120: The switch case has already been handled by a previous case.
                //             case List<(int z, int w)> list2: // subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "List<(int z, int w)> list2").WithLocation(13, 18),
                // (14,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(14, 17)
                );
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void TupleNameDifferences_02()
        {
            var source =
@"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var list = new List<(int a, int b)>();
        switch (list)
        {
            case List<(int x, int y)> list1:
                Console.WriteLine(""pass"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"pass");
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void TupleNameDifferences_03()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        var t = (a: 1, b: 2);
        switch (t)
        {
            case ValueTuple<int, int> x:
                Console.WriteLine(""pass"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"pass");
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void TupleNameDifferences_04()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        var t = (a: 1, b: 2);
        switch (t)
        {
            case var x:
                Console.WriteLine(x.a);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"1");
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void TupleNameDifferences_05()
        {
            var source =
@"
using System;

class Program
{
    static void Main()
    {
        var t = (a: 1, b: 2);
        switch (t)
        {
            case ValueTuple<int, int> x:
                break;
            case var x:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (13,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var x:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var x").WithLocation(13, 18),
                // (14,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(14, 17)
                );
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void DynamicDifferences_01()
        {
            var source =
@"
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var list = new List<dynamic>();
        switch (list)
        {
            case List<object> list1:
                break;
            case List<dynamic> list2: // subsumed
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (13,18): error CS8120: The switch case has already been handled by a previous case.
                //             case List<dynamic> list2: // subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "List<dynamic> list2").WithLocation(13, 18),
                // (14,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break")
                );
        }

        [Fact]
        [WorkItem(17088, "https://github.com/dotnet/roslyn/issues/17088")]
        public void DynamicDifferences_02()
        {
            var source =
@"
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var list = new List<dynamic>();
        switch (list)
        {
            case List<object> list1:
                Console.WriteLine(""pass"");
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: s_valueTupleRefs, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"pass");
        }

        [Fact]
        [WorkItem(17089, "https://github.com/dotnet/roslyn/issues/17089")]
        public void Dynamic_01()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        dynamic d = 1;
        switch (d)
        {
            case dynamic x: // error 1
                break;
        }
        if (d is dynamic y) {} // error 2
        if (d is var z) // ok
        {
            long l = z;
            string s = z;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (9,18): error CS8330: It is not legal to use the type 'dynamic' in a pattern.
                //             case dynamic x: // error 1
                Diagnostic(ErrorCode.ERR_PatternDynamicType, "dynamic"),
                // (12,18): error CS8330: It is not legal to use the type 'dynamic' in a pattern.
                //         if (d is dynamic y) {} // error 2
                Diagnostic(ErrorCode.ERR_PatternDynamicType, "dynamic").WithLocation(12, 18)
                );
        }

        [Fact]
        [WorkItem(17089, "https://github.com/dotnet/roslyn/issues/17089")]
        public void Dynamic_02()
        {
            var source =
@"
class Program
{
    static void Main()
    {
        dynamic d = 1;
        switch (d)
        {
            case object x:
            case var y: // OK, catches null
            case var z: // error: subsumed
                break;
        }
        switch (d)
        {
            case object x:
            case null:
            case var y: // error: subsumed
                break;
        }
        switch (d)
        {
            case object x:
            case 1: // error: subsumed
                break;
        }
        switch (d)
        {
            case object x:
            case int y: // error: subsumed
                break;
        }
        switch (d)
        {
            case object x:
            case (dynamic)null:
            case (string)null: // error: subsumed
                break;
        }
        switch (d)
        {
            case int i:
            case long l:
            case object o:
            case var v:
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var z: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var z").WithLocation(11, 18),
                // (18,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var y: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var y").WithLocation(18, 18),
                // (24,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 1: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 1:"),
                // (30,18): error CS8120: The switch case has already been handled by a previous case.
                //             case int y: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "int y").WithLocation(30, 18),
                // (37,13): error CS0152: The switch statement contains multiple cases with the label value 'null'
                //             case (string)null: // error: subsumed
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case (string)null:").WithArguments("null").WithLocation(37, 13)
                );
        }

        [Fact]
        public void SubsumedCasesAreUnreachable_01()
        {
            var source =
@"
class Program
{
    static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 1:
                break;
            case System.IComparable c:
                break;
            case 2: // error: subsumed
                break; // unreachable (1)
            case int n: // error: subsumed
                break; // unreachable (2)
            case var i: // error: subsumed
                break; // unreachable (3)
            default:
                break; // unreachable, because `var i` would catch all
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (12,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 2: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 2:").WithLocation(12, 13),
                // (14,18): error CS8120: The switch case has already been handled by a previous case.
                //             case int n: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "int n").WithLocation(14, 18),
                // (16,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var i: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var i").WithLocation(16, 18),
                // (13,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable (1)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(13, 17),
                // (15,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable (2)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(15, 17),
                // (17,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable (3)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(17, 17),
                // (19,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable, because `var i` would catch all
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(19, 17)
                );
        }

        [Fact]
        public void SwitchTuple()
        {
            var source =
@"
class Program
{
    static void Main(string[] args)
    {
        switch ((x: 1, y: 2))
        {
            case System.IComparable c:
                break;
            case System.ValueTuple<int, int> x: // error: subsumed
                break; // unreachable
            default:
                break; // unreachable because a single case handles all input
        }
    }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (10,18): error CS8120: The switch case has already been handled by a previous case.
                //             case System.ValueTuple<int, int> x: // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "System.ValueTuple<int, int> x").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17),
                // (13,17): warning CS0162: Unreachable code detected
                //                 break; // unreachable because a single case handles all input
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(13, 17)
               );
        }

        [Fact]
        public void ByValueThenByTypeTwice()
        {
            var source =
@"
class Program
{
    static bool b = false;
    static int i = 2;
    static void Main(string[] args)
    {
        switch (i)
        {
            case 1:
                break;
            case System.IComparable c when b:
                break;
            case System.IFormattable f when b:
                break;
            default:
                System.Console.WriteLine(nameof(Main));
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib40(
                source, options: TestOptions.ReleaseExe, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation, expectedOutput: "Main");
        }

        [Fact, WorkItem(18948, "https://github.com/dotnet/roslyn/issues/18948")]
        public void AsyncGenericPatternCrash()
        {
            var source =
@"
using System.Threading.Tasks;

static class Ex
{
    public static async Task<T> SwitchWithAwaitInPatternFails<T>(Task self, T defaultValue)
    {
        switch (self)
        {
            case Task<T> resultTask:
                return await resultTask.ConfigureAwait(false);

            default:
                await self.ConfigureAwait(false);
                return default(T);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(
                source, options: TestOptions.ReleaseDll.WithOptimizationLevel(OptimizationLevel.Release), references: new[] { SystemCoreRef, CSharpRef });
            compilation.VerifyDiagnostics();
            var comp = CompileAndVerify(compilation);
        }

        [Fact, WorkItem(388743, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=388743")]
        public void SemanticModelForBrokenSwitch_01()
        {
            // a syntax error that happens to look like a pattern switch if you squint
            var source =
@"class Sample
{
    void M()
    {
        bool x = true;

        switch (x) {
            case

        var q = 3;
        var y = q/*BIND*/;
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular6);
            compilation.VerifyDiagnostics(
                // (8,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, @"case

        var q ").WithArguments("pattern matching", "7.0").WithLocation(8, 13),
                // (10,15): error CS1003: Syntax error, ':' expected
                //         var q = 3;
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments(":", "=").WithLocation(10, 15),
                // (10,15): error CS1525: Invalid expression term '='
                //         var q = 3;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(10, 15),
                // (13,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(13, 2),
                // (10,9): error CS8070: Control cannot fall out of switch from final case label ('var q')
                //         var q = 3;
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "var q").WithArguments("var q").WithLocation(10, 9)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(n => n.Identifier.ValueText == "q" && n.ToFullString().Contains("/*BIND*/"))
                .Single();
            var type = model.GetTypeInfo(node);
        }

        [Fact, WorkItem(388743, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=388743")]
        public void SemanticModelForBrokenSwitch_02()
        {
            // a simple legal pattern switch but run in language version 6
            var source =
@"class Sample
{
    void M()
    {
        bool b = true;
        switch (b) {
            case var q:
                System.Console.WriteLine(q/*BIND*/);
                break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular6);
            compilation.VerifyDiagnostics(
                // (7,13): error CS8059: Feature 'pattern matching' is not available in C# 6. Please use language version 7.0 or greater.
                //             case var q:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case var q:").WithArguments("pattern matching", "7.0").WithLocation(7, 13)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(n => n.Identifier.ValueText == "q" && n.ToFullString().Contains("/*BIND*/"))
                .Single();
            var type = model.GetTypeInfo(node);
            Assert.Equal(SpecialType.System_Boolean, type.Type.SpecialType);
            Assert.Equal(SpecialType.System_Boolean, type.ConvertedType.SpecialType);
        }

        [Fact, WorkItem(20210, "https://github.com/dotnet/roslyn/issues/20210")]
        public void SwitchOnNull_20210()
        {
            var source =
@"class Sample
{
    void M()
    {
        switch (default(object))
        {
          case bool _:
          case true:     // error: subsumed (1 of 12)
          case false:    // error: subsumed (2 of 12)
            break; // unreachable (1)
        }

        switch (new object())
        {
          case bool _:
          case true:     // error: subsumed (3 of 12)
          case false:    // error: subsumed (4 of 12)
            break;
        }

        switch ((object)null)
        {
          case bool _:
          case true:     // error: subsumed (5 of 12)
          case false:    // error: subsumed (6 of 12)
            break; // unreachable (2)
        }

        switch ((bool?)null)
        {
          case bool _:
          case true:     // error: subsumed (7 of 12)
          case false:    // error: subsumed (8 of 12)
            break; // unreachable (3)
        }

        switch (default(bool?))
        {
          case bool _:
          case true:     // error: subsumed (9 of 12)
          case false:    // error: subsumed (10 of 12)
            break; // unreachable (4)
            // warning on the previous line missing due to https://github.com/dotnet/roslyn/issues/22125
        }

        switch (default(bool))
        {
          case bool _:
          case true:     // error: subsumed (11 of 12)
          case false:    // error: subsumed (12 of 12)
            break;
        }
    }
}";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
                // (8,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (1 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(8, 11),
                // (9,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (2 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(9, 11),
                // (16,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (3 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(16, 11),
                // (17,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (4 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(17, 11),
                // (24,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (5 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(24, 11),
                // (25,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (6 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(25, 11),
                // (32,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (7 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(32, 11),
                // (33,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (8 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(33, 11),
                // (40,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (9 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(40, 11),
                // (41,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (10 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(41, 11),
                // (49,11): error CS8120: The switch case has already been handled by a previous case.
                //           case true:     // error: subsumed (11 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case true:").WithLocation(49, 11),
                // (50,11): error CS8120: The switch case has already been handled by a previous case.
                //           case false:    // error: subsumed (12 of 12)
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case false:").WithLocation(50, 11),
                // (10,13): warning CS0162: Unreachable code detected
                //             break; // unreachable (1)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(10, 13),
                // (26,13): warning CS0162: Unreachable code detected
                //             break; // unreachable (2)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(26, 13),
                // (34,13): warning CS0162: Unreachable code detected
                //             break; // unreachable (3)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(34, 13)
                );
        }
    }
}
