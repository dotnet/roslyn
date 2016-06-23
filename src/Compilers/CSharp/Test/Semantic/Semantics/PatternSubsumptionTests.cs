// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternSubsumptionTests : CSharpTestBase
    {
        private static CSharpParseOptions patternParseOptions =
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)
                    .WithFeature(MessageID.IDS_FeaturePatternMatching.RequiredFeature(), "true");

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
            case var i when ((i&1) == 0): // error: subsumed
                break; // warning: unreachable (1)
            case 1: // error: duplicate case label
                break; // warning: unreachable (2)
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,13): error CS0152: The switch statement contains multiple cases with the label value '1'
                //             case 1: // error: duplicate case label
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 1:").WithArguments("1").WithLocation(11, 13),
                // (9,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var i when ((i&1) == 0): // error: subsumed
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var i").WithLocation(9, 18),
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            case 2:
                break; // warning: unreachable code (impossible given the value)
            case 1 when true:
                break;
            case 1:
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (11,13): error CS8120: The switch case has already been handled by a previous case.
                //             case 1:
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "case 1:").WithLocation(11, 13),
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable code (impossible given the value)
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            case ""foo"": // error: subsumed by previous case
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (10,18): error CS0029: Cannot implicitly convert type 'string' to 'bool'
                //             case "foo": // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""foo""").WithArguments("string", "bool").WithLocation(10, 18),
                // (11,17): warning CS0162: Unreachable code detected
                //                 break;
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
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case string s: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "string s").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
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
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (12,18): error CS8120: The switch case has already been handled by a previous case.
                //             case IEnumerable<string> i: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "IEnumerable<string> i").WithLocation(12, 18),
                // (13,17): warning CS0162: Unreachable code detected
                //                 break;
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
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case X list: // error: subsumed by previous case
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "X list").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
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
            case var x: // OK; there are 256 values of bool!, and in any case we do not do value-based exhaustiveness checking
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics();
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
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (11,18): error CS8120: The switch case has already been handled by a previous case.
                //             case var x: // error: subsumed by previous cases
                Diagnostic(ErrorCode.ERR_PatternIsSubsumed, "var x").WithLocation(11, 18),
                // (12,17): warning CS0162: Unreachable code detected
                //                 break;
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (10,18): error CS0031: Constant value '1000' cannot be converted to a 'byte'
                //             case 1000: // error: impossible given the type
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "1000").WithArguments("1000", "byte").WithLocation(10, 18)
                );
        }
    }
}
