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
    public class PatternSwitchTests : CSharpTestBase
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
            case ""foo"": ; // error: subsumed by previous case
            case null: ; // error: subsumed by previous case
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (10,13): error CS8120: The switch case has already been handled by a previous case.
                //             case "foo": ; // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, @"case ""foo"":").WithLocation(10, 13),
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
            case ""foo"": // wrong type
                break; // unreachable
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "foo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""foo""").WithArguments("string", "bool").WithLocation(10, 18),
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
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);
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
                // (8,18): error CS8121: An expression of type string cannot be handled by a pattern of type int.
                //             case int i: // error: type mismatch.
                Diagnostic(ErrorCode.ERR_PatternWrongType, "int").WithArguments("string", "int").WithLocation(8, 18)
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
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("bool").WithLocation(10, 18)
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
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "3").WithArguments("int", "bool").WithLocation(10, 18)
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
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000").WithArguments("1000", "byte").WithLocation(10, 18)
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
                break; // always considered reachable
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
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(11, 17)
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
            case ""foo"": // wrong type
                break;
        }
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "foo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""foo""").WithArguments("string", "bool").WithLocation(11, 18)
                );
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular6WithV7SwitchBinder).VerifyDiagnostics(
                // (11,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "foo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""foo""").WithArguments("string", "bool").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(12, 17)
                );
            CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe).VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "foo": // wrong type
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""foo""").WithArguments("string", "bool").WithLocation(11, 18)
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
                // (18,13): error CS8059: Feature 'pattern matching' is not available in C# 6.  Please use language version 7 or greater.
                //             case Color x when false:
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "case Color x when false:").WithArguments("pattern matching", "7").WithLocation(18, 13),
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
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 3;").WithArguments("case 3:").WithLocation(15, 17),
                // (18,13): error CS8070: Control cannot fall out of switch from final case label ('case Color x when false:')
                //             case Color x when false:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case Color x when false:").WithArguments("case Color x when false:").WithLocation(18, 13)
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

        [Fact]
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
    }
}
