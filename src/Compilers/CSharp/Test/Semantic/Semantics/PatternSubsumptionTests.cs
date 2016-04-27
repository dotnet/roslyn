// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternSubsumptionTests : CSharpTestBase
    {
        private static CSharpParseOptions patternParseOptions =
            TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)
                    .WithFeature(MessageID.IDS_FeaturePatternMatching.RequiredFeature(), "true");

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            case var i when (i&2 == 0):
                break;
            case 1: // error: subsumed by previous case
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            // TODO: assert the specific diagnostics
            compilation.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            compilation.VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break; // warning: unreachable code
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            case ""foo"": // error: subsumed by previous case
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
        public void SimpleSubsumption02()
        {
            var source =
@"public class X
{
    public static void Main()
    {
        bool b = false;
        switch (s)
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
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            case var x: // error: subsumed by previous cases
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8823")]
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
            case true:
            case false:
            case null:
                break;
            case var x: // error: subsumed by previous cases
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: patternParseOptions);
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            // TODO: assert the specific errors produced.
            compilation.VerifyDiagnostics();
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10601")]
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10632")]
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
            Assert.True(compilation.GetDiagnostics().HasAnyErrors());
            // TODO: assert the specific errors
            compilation.VerifyDiagnostics(
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10601")]
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
