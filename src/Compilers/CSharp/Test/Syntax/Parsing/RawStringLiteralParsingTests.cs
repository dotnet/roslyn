// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class RawStringLiteralParsingTests : ParsingTests
    {
        public RawStringLiteralParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestDownlevel()
        {
            CreateCompilation(
@"class C
{
    const string s = """""" """"""; 
}", parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (3,22): error CS8652: The feature 'raw string literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     const string s = """ """; 
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @""""""" """"""").WithArguments("raw string literals").WithLocation(3, 22));
        }

        [Fact]
        public void TestInFieldInitializer()
        {
            CreateCompilation(
@"class C
{
    string s = """""" """"""; 
}").VerifyDiagnostics(
                // (3,12): warning CS0414: The field 'C.s' is assigned but its value is never used
                //     string s = """ """; 
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s").WithArguments("C.s").WithLocation(3, 12));
        }

        [Fact]
        public void TestInConstantFieldInitializer1()
        {
            CreateCompilation(
@"class C
{
    const string s = """""" """"""; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInConstantFieldInitializer2()
        {
            CreateCompilation(
@"class C
{
    const string s = """""" """""" + ""a""; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInConstantFieldInitializer3()
        {
            CreateCompilation(
@"class C
{
    const string s = ""a"" + """""" """"""; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInAttribute()
        {
            CreateCompilation(
@"
[System.Obsolete(""""""obsolete"""""")]
class C
{
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestMemberAccess()
        {
            CreateCompilation(
@"class C
{
    int s = """""" """""".Length; 
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestInSwitch()
        {
            CreateCompilation(
@"class C
{
    void M(string s)
    {
        switch (s)
        {
            case """""" a """""":
            case """""" b """""":
                break;
        }
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestReachableSwitchCase1()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        switch ("""""" a """""")
        {
            case """""" a """""":
                break;
        }
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestReachableSwitchCase2()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        switch ("""""" a """""")
        {
            case """""""" a """""""":
                break;
        }
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestUnreachableSwitchCase1()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        switch ("""""" a """""")
        {
            case """""""" b """""""":
                break;
        }
    }
}").VerifyDiagnostics(
                // (8,17): warning CS0162: Unreachable code detected
                //                 break;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "break").WithLocation(8, 17));
        }

        [Fact]
        public void TestSingleLineRawLiteralInSingleLineInterpolatedString()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $""{""""""a""""""}"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestSingleLineRawLiteralInMultiLineInterpolatedString1()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $@""{""""""a""""""}"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestSingleLineRawLiteralInMultiLineInterpolatedString2()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $@""{
            """"""a""""""
        }"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestMultiLineRawLiteralInSingleLineInterpolatedString()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $""{""""""

""""""}"";
    }
}").VerifyDiagnostics(
                // (5,20): error CS9105: Multi-line raw string literals are only allowed in verbatim interpolated strings
                //         var v = $"{"""
                Diagnostic(ErrorCode.ERR_Multi_line_raw_string_literals_are_only_allowed_in_verbatim_interpolated_strings, @"""""""

""""""").WithLocation(5, 20));
        }

        [Fact]
        public void TestMultiLineRawLiteralInMultiLineInterpolatedString1()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $@""{""""""

""""""}"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestMultiLineRawLiteralInMultiLineInterpolatedString2()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $@""{
""""""

""""""
}"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestSingleLineRawLiteralContainingClosingBraceInSingleLineInterpolatedString()
        {
            CreateCompilation(
@"class C
{
    void M()
    {
        var v = $""{""""""}""""""}"";
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void TestAwaitRawStringLiteral()
        {
            CreateCompilation(
@"
using System.Threading.Tasks;

class C
{
    async Task M()
    {
        var v = await """""" """""";
    }
}").VerifyDiagnostics(
                // (8,17): error CS1061: 'string' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         var v = await """ """;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"await """""" """"""").WithArguments("string", "GetAwaiter").WithLocation(8, 17));
        }
    }
}
