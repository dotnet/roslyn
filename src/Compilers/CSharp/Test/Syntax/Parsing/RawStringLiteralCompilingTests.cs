// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public class RawStringLiteralCompilingTests : CompilingTestBase
{
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
    public void TestMultiLineRawLiteralInSingleLineInterpolatedString_CSharp9()
    {
        CreateCompilation(
@"class C
{
    void M()
    {
        var v = $""{""""""

""""""}"";
    }
}", parseOptions: TestOptions.Regular9).VerifyDiagnostics(
            // (5,20): error CS8652: The feature 'raw string literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         var v = $"{"""
            Diagnostic(ErrorCode.ERR_FeatureInPreview, @"""""""

""""""").WithArguments("raw string literals").WithLocation(5, 20),
            // (7,4): error CS8967: Newlines inside a non-verbatim interpolated string are not supported in C# 9.0. Please use language version preview or greater.
            // """}";
            Diagnostic(ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString, "}").WithArguments("9.0", "preview").WithLocation(7, 4));
    }

    [Fact]
    public void TestMultiLineRawLiteralInSingleLineInterpolatedString_CSharp10()
    {
        CreateCompilation(
@"class C
{
    void M()
    {
        var v = $""{""""""

""""""}"";
    }
}").VerifyDiagnostics();
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

    [Fact]
    public void TestInIsConstant()
    {
        CreateCompilation(
@"
class C
{
    void M(object o)
    {
        if (o is """""" """""")
        {
        }
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInIsTuple()
    {
        CreateCompilation(
@"
class C
{
    void M((string s, int i) o)
    {
        if (o is ("""""" """""", 1))
        {
        }
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInSubpattern()
    {
        CreateCompilation(
@"
class C
{
    string x = """";
    void M(C c)
    {
        if (c is { x: """""" """""" })
        {
        }
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInConditionalExpression()
    {
        CreateCompilation(
@"
class C
{
    void M(bool b)
    {
        var x = b ? """""" """""" : "" "";
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInExpressionStatement()
    {
        CreateCompilation(
@"
class C
{
    void M(bool b)
    {
        """""" """""";
    }
}").VerifyDiagnostics(
            // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //         """ """;
            Diagnostic(ErrorCode.ERR_IllegalStatement, @""""""" """"""").WithLocation(6, 9));
    }

    [Fact]
    public void TestInAnonymousObject()
    {
        CreateCompilation(
@"
class C
{
    void M()
    {
        var v = new { P = """""" """""" };
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestSingleLineOutput1()
    {
        CompileAndVerify(
@"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(""""""abc""def"""""");
    }
}", expectedOutput: @"abc""def");
    }

    [Fact]
    public void TestSingleLineOutput2()
    {
        CompileAndVerify(
@"
using System;

Console.WriteLine(""""""abc""def"""""");
", expectedOutput: @"abc""def");
    }

    [Fact]
    public void TestSingleLineOutput3()
    {
        CompileAndVerify(
@"
using System;

Console.WriteLine("""""" abc""def """""");
", expectedOutput: @" abc""def ");
    }

    [Fact]
    public void TestMultiLineOutput1()
    {
        CompileAndVerify(
@"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(""""""
                          abc""
                          def
                          """""");
    }
}".Replace("\r\n", "\n"), expectedOutput: "abc\"\ndef");
    }

    [Fact]
    public void TestMultiLineOutput2()
    {
        CompileAndVerify(
@"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine(
            """"""
            abc""
            def
        """""");
    }
}".Replace("\r\n", "\n"), expectedOutput: "    abc\"\n    def");
    }

    [Fact]
    public void TestInParameterDefault()
    {
        CreateCompilation(
@"class C
{
    public void M(string s = """""" """""") { }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestWhitespaceMismatch1()
    {
        CreateCompilation(
"class C\r\n{\r\nconst string s = \"\"\"\r\n\t\r\n \"\"\";\r\n}").VerifyDiagnostics(
                // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, "	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch2()
    {
        CreateCompilation(
"class C\r\n{\r\nconst string s = \"\"\"\r\n \r\n\t\"\"\";\r\n}").VerifyDiagnostics(
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\u0020' versus '\t'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " ").WithArguments(@"\u0020", @"\t").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch3()
    {
        CreateCompilation(
"class C\r\n{\r\nconst string s = \"\"\"\r\n \t\r\n  \"\"\";\r\n}").VerifyDiagnostics(
                // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " 	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch4()
    {
        CreateCompilation(
"class C\r\n{\r\nconst string s = \"\"\"\r\n \t\r\n   \"\"\";\r\n}").VerifyDiagnostics(
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " 	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch5()
    {
        CreateCompilation(
"class C\r\n{\r\nconst string s = \"\"\"\r\n\f\r\n\v\"\"\";\r\n}").VerifyDiagnostics(
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\f' versus '\v'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, "").WithArguments(@"\f", @"\v").WithLocation(4, 1));
    }
}
