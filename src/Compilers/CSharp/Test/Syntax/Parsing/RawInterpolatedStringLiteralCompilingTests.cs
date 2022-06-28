// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public class RawInterpolatedStringLiteralCompilingTests : CompilingTestBase
{
    private static string Render(string markup, string? normalizedNewLine)
    {
        markup = markup.Replace('␠', ' ').Replace('␉', '\t');

        // If we're normalizing newlines, convert everything to \n, then convert that to the newline form asked for.
        if (normalizedNewLine != null)
        {
            markup = markup.Replace("\r\n", "\n");
            markup = markup.Replace("\r", "\n");
            markup = markup.Replace("\n", normalizedNewLine);
        }

        return markup;
    }

    private void RenderAndVerify(string markup, string expectedOutput)
    {
        RenderAndVerify(markup, expectedOutput, normalize: null);
        RenderAndVerify(markup, expectedOutput, normalize: "\r\n");
        RenderAndVerify(markup, expectedOutput, normalize: "\n");
        RenderAndVerify(markup, expectedOutput, normalize: "\r");
    }

    private void RenderAndVerify(string markup, string expectedOutput, string? normalize)
    {
        var text = Render(markup, normalize);
        ParseAllPrefixes(text);
        CompileAndVerify(text, expectedOutput: Render(expectedOutput, normalize), trimOutput: false);
    }

    private static void RenderAndVerify(string markup, params DiagnosticDescription[] expected)
    {
        RenderAndVerify(markup, expected, normalize: null);
        RenderAndVerify(markup, expected, normalize: "\r\n");
        RenderAndVerify(markup, expected, normalize: "\n");
        RenderAndVerify(markup, expected, normalize: "\r");
    }

    private static void RenderAndVerify(string markup, DiagnosticDescription[] expected, string? normalize)
    {
        var text = Render(markup, normalize);
        ParseAllPrefixes(text);
        CreateCompilation(text).VerifyDiagnostics(expected);
    }

    private static void ParseAllPrefixes(string text)
    {
        // ensure the parser doesn't crash on any test cases.
        for (var i = 0; i < text.Length; i++)
            SyntaxFactory.ParseCompilationUnit(text[0..^i]);
    }

    [Fact]
    public void TestDownlevel()
    {
        CreateCompilation(
@"class C
{
    const string s = $"""""" """""";
}", parseOptions: TestOptions.Regular10).VerifyDiagnostics(
            // (3,22): error CS8652: The feature 'raw string literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     const string s = """ """;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, @"$"""""" """"""").WithArguments("raw string literals").WithLocation(3, 22));
    }

    [Fact]
    public void TestAtLevel()
    {
        CreateCompilation(
@"class C
{
    const string s = $"""""" """""";
}", parseOptions: TestOptions.RegularNext).VerifyDiagnostics();
    }

    [Fact]
    public void TestInFieldInitializer()
    {
        CreateCompilation(
@"class C
{
    string s = $"""""" """""";
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInConstantFieldInitializer1()
    {
        CreateCompilation(
@"class C
{
    const string s = $"""""" """""";
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInConstantFieldInitializer2()
    {
        CreateCompilation(
@"class C
{
    const string s = $"""""" """""" + ""a"";
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInConstantFieldInitializer3()
    {
        CreateCompilation(
@"class C
{
    const string s = ""a"" + $"""""" """""";
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInConstantFieldInitializer4()
    {
        CreateCompilation(
@"class C
{
    const string x = ""bar"";
    const string s = $""""""{x}"""""";
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInAttribute()
    {
        CreateCompilation(
@"
[System.Obsolete($""""""obsolete"""""")]
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
    int s = $"""""" """""".Length;
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
            case $"""""" a """""":
            case $"""""" b """""":
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
        switch ($"""""" a """""")
        {
            case $"""""" a """""":
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
        switch ($"""""" a """""")
        {
            case $"""""""" a """""""":
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
        switch ($"""""" a """""")
        {
            case $"""""""" b """""""":
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
        var v = $""{$""""""a""""""}"";
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
        var v = $@""{$""""""a""""""}"";
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
            $""""""a""""""
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
        var v = $""{$""""""

""""""}"";
    }
}", parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (5,20): error CS8652: The feature 'raw string literals' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var v = $"{$"""
                Diagnostic(ErrorCode.ERR_FeatureInPreview, @"$""""""

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
        var v = $""{$""""""

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
        var v = $@""{$""""""

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
$""""""

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
        var v = $""{$""""""}""""""}"";
    }
}").VerifyDiagnostics(
                // (5,24): error CS9007: Too many closing braces for raw string literal
                //         var v = $"{$"""}"""}";
                Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}").WithLocation(5, 24));
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
        var v = await $"""""" """""";
    }
}").VerifyDiagnostics(
                // (8,17): error CS1061: 'string' does not contain a definition for 'GetAwaiter' and no accessible extension method 'GetAwaiter' accepting a first argument of type 'string' could be found (are you missing a using directive or an assembly reference?)
                //         var v = await $""" """;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, @"await $"""""" """"""").WithArguments("string", "GetAwaiter").WithLocation(8, 17));
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
        if (o is $"""""" """""")
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
        if (o is ($"""""" """""", 1))
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
        if (c is { x: $"""""" """""" })
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
        var x = b ? $"""""" """""" : "" "";
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
        $"""""" """""";
    }
}").VerifyDiagnostics(
            // (6,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
            //         """ """;
            Diagnostic(ErrorCode.ERR_IllegalStatement, @"$"""""" """"""").WithLocation(6, 9));
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
        var v = new { P = $"""""" """""" };
    }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestInParameterDefault()
    {
        CreateCompilation(
@"class C
{
    public void M(string s = $"""""" """""") { }
}").VerifyDiagnostics();
    }

    [Fact]
    public void TestAttemptingMarkdownInspiredLanguageHint()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""xml
    <hi/>
    """""");",
                // (3,11): error CS8997: Unterminated raw string literal
                //     $"""xml
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, "l").WithLocation(3, 11),
                // (4,6): error CS0103: The name 'hi' does not exist in the current context
                //     <hi/>
                Diagnostic(ErrorCode.ERR_NameNotInContext, "hi").WithArguments("hi").WithLocation(4, 6),
                // (4,9): error CS1525: Invalid expression term '>'
                //     <hi/>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">").WithLocation(4, 9),
                // (5,10): error CS8997: Unterminated raw string literal
                //     """);
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, "").WithLocation(5, 10),
                // (5,10): error CS1026: ) expected
                //     """);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 10),
                // (5,10): error CS1002: ; expected
                //     """);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 10));
    }

    [Fact]
    public void TestAttemptingCommentOnStartingQuoteLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $"""""" // lang=xml
    <hi/>
    """""");",
                // (3,20): error CS8997: Unterminated raw string literal
                //     $""" // lang=xml
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, "l").WithLocation(3, 20),
                // (4,6): error CS0103: The name 'hi' does not exist in the current context
                //     <hi/>
                Diagnostic(ErrorCode.ERR_NameNotInContext, "hi").WithArguments("hi").WithLocation(4, 6),
                // (4,9): error CS1525: Invalid expression term '>'
                //     <hi/>
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">").WithLocation(4, 9),
                // (5,10): error CS8997: Unterminated raw string literal
                //     """);
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, "").WithLocation(5, 10),
                // (5,10): error CS1026: ) expected
                //     """);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 10),
                // (5,10): error CS1002: ; expected
                //     """);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 10));
    }

    [Fact]
    public void TestInterpolatingAnonymousObject()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {new { }}
    """""");", expectedOutput: "{ }");
    }

    [Fact]
    public void TestSingleLineWithWhitespaceAndContent()
    {
        RenderAndVerify(@"
System.Console.Write($"""""" abc""def """""");", expectedOutput: @" abc""def ");
    }

    [Fact]
    public void TestSingleLineDiagnosticLocationWithTrivia1()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""{{""""""/**/
#nullable enable
);",
            // (4,9): error CS9006: The interpolated raw string literal does not start with enough '$' characters to allow this many consecutive opening braces as content
            // /**/$"""{{"""/**/
            Diagnostic(ErrorCode.ERR_TooManyOpenBracesForRawString, "{").WithLocation(4, 9),
            // (4,11): error CS1733: Expected expression
            // /**/$"""{{"""/**/
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(4, 11));
    }

    [Fact]
    public void TestSingleLineDiagnosticLocationWithTrivia2()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""}""""""/**/
#nullable enable
);",
            // (4,9): error CS9007: The interpolated raw string literal does not start with enough '$' characters to allow this many consecutive closing braces as content
            // /**/$"""}"""/**/
            Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}").WithLocation(4, 9));
    }

    [Fact]
    public void TestSingleLineDiagnosticLocationWithTrivia3()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""""""""/**/
#nullable enable
);",
            // (4,15): error CS8997: Unterminated raw string literal
            // /**/$""""""/**/
            Diagnostic(ErrorCode.ERR_UnterminatedRawString, "/").WithLocation(4, 15));
    }

    [Fact]
    public void TestMultiLineDiagnosticLocationWithTrivia1()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""
    {{
    """"""/**/
#nullable enable
);",
                // (5,5): error CS9006: The interpolated raw string literal does not start with enough '$' characters to allow this many consecutive opening braces as content
                //     {{
                Diagnostic(ErrorCode.ERR_TooManyOpenBracesForRawString, "{").WithLocation(5, 5),
                // (6,5): error CS1733: Expected expression
                //     """/**/
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(6, 5));
    }

    [Fact]
    public void TestMultiLineDiagnosticLocationWithTrivia2()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""
    }
    """"""/**/
#nullable enable
);",
                // (5,5): error CS9007: The interpolated raw string literal does not start with enough '$' characters to allow this many consecutive closing braces as content.
                //     }
                Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}").WithLocation(5, 5));
    }

    [Fact]
    public void TestMultiLineDiagnosticLocationWithTrivia3()
    {
        RenderAndVerify(@"
System.Console.Write(
#nullable disable
/**/$""""""
    """"""/**/
#nullable enable
);",
                // (5,5): error CS9002: Multi-line raw string literals must contain at least one line of content.
                //     """/**/
                Diagnostic(ErrorCode.ERR_RawStringMustContainContent, @"""""""").WithLocation(5, 5));
    }

    [Fact]
    public void TestPreprocessorConditionInMultilineContent()
    {
        RenderAndVerify(@"
System.Console.Write(
$""""""
#if DEBUG
a
#endif
"""""");", expectedOutput: @"#if DEBUG
a
#endif");
    }

    [Fact]
    public void TestPreprocessorConditionInInterpolation()
    {
        RenderAndVerify(@"
System.Console.Write(
$""""""
{
#if DEBUG
42
#endif
}
"""""");",
                // (4,2): error CS1073: Unexpected token '#'
                // {
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "").WithArguments("#").WithLocation(4, 2),
                // (5,1): error CS1003: Syntax error, '}' expected
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_SyntaxError, "#").WithArguments("}").WithLocation(5, 1),
                // (5,1): error CS1525: Invalid expression term ''
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "#").WithArguments("").WithLocation(5, 1),
                // (5,1): error CS1056: Unexpected character '#'
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(5, 1),
                // (7,1): error CS1056: Unexpected character '#'
                // #endif
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(7, 1));
    }

    [Fact]
    public void TestTrivia()
    {
        RenderAndVerify(@"
System.Console.Write(
$""""""
{
#if DEBUG
42
#endif
}
"""""");",
                // (4,2): error CS1073: Unexpected token '#'
                // {
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "").WithArguments("#").WithLocation(4, 2),
                // (5,1): error CS1003: Syntax error, '}' expected
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_SyntaxError, "#").WithArguments("}").WithLocation(5, 1),
                // (5,1): error CS1525: Invalid expression term ''
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "#").WithArguments("").WithLocation(5, 1),
                // (5,1): error CS1056: Unexpected character '#'
                // #if DEBUG
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(5, 1),
                // (7,1): error CS1056: Unexpected character '#'
                // #endif
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("#").WithLocation(7, 1));
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
        Console.Write($""""""abc""def"""""");
    }
}", expectedOutput: @"abc""def");
    }

    [Fact]
    public void TestSingleLineOutput2()
    {
        CompileAndVerify(
@"
using System;

Console.Write($""""""abc""def"""""");
", expectedOutput: @"abc""def");
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
        Console.Write($""""""
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
        Console.Write(
            $""""""
            abc""
            def
        """""");
    }
}".Replace("\r\n", "\n"), expectedOutput: "    abc\"\n    def");
    }

    [Fact]
    public void MultiLineCase01()
    {
        RenderAndVerify(@"
System.Console.Write(
    $"""""");",
                // (3,10): error CS8997: Unterminated raw string literal
                //     $""");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(3, 10),
                // (3,11): error CS1026: ) expected
                //     $""");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 11),
                // (3,11): error CS1002: ; expected
                //     $""");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 11));
    }

    [Fact]
    public void MultiLineCase02()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    "");",
                // (4,7): error CS8997: Unterminated raw string literal
                //     ");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 7),
                // (4,8): error CS1026: ) expected
                //     ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 8),
                // (4,8): error CS1002: ; expected
                //     ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 8));
    }

    [Fact]
    public void MultiLineCase03()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    """");",
                // (4,8): error CS8997: Unterminated raw string literal
                //     "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 8),
                // (4,9): error CS1026: ) expected
                //     "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
    }

    [Fact]
    public void MultiLineCase04()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    """""");",
                // (4,5): error CS9002: Multi-line raw string literals must contain at least one line of content
                //     """);
                Diagnostic(ErrorCode.ERR_RawStringMustContainContent, @"""""""").WithLocation(4, 5));
    }

    [Fact]
    public void MultiLineCase05()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""

    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase06()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠
    "");",
                // (4,7): error CS8997: Unterminated raw string literal
                //     ");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 7),
                // (4,8): error CS1026: ) expected
                //     ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 8),
                // (4,8): error CS1002: ; expected
                //     ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 8));
    }

    [Fact]
    public void MultiLineCase07()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠
    """");",
                // (4,8): error CS8997: Unterminated raw string literal
                //     "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 8),
                // (4,9): error CS1026: ) expected
                //     "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
    }

    [Fact]
    public void MultiLineCase08()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠
    """""");",
                // (4,5): error CS9002: Multi-line raw string literals must contain at least one line of content
                //     """);
                Diagnostic(ErrorCode.ERR_RawStringMustContainContent, @"""""""").WithLocation(4, 5));
    }

    [Fact]
    public void MultiLineCase09()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠

    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase10()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    "");",
                // (4,7): error CS8997: Unterminated raw string literal
                //     ");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 7),
                // (4,8): error CS1026: ) expected
                //     ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 8),
                // (4,8): error CS1002: ; expected
                //     ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 8));
    }

    [Fact]
    public void MultiLineCase11()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    """");",
                // (4,8): error CS8997: Unterminated raw string literal
                //     "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 8),
                // (4,9): error CS1026: ) expected
                //     "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //     "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
    }

    [Fact]
    public void MultiLineCase12()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    """""");",
                // (4,5): error CS9002: Multi-line raw string literals must contain at least one line of content
                //     """);
                Diagnostic(ErrorCode.ERR_RawStringMustContainContent, @"""""""").WithLocation(4, 5));
    }

    [Fact]
    public void MultiLineCase13()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠

    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase14()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    ␠"");",
                // (4,8): error CS8997: Unterminated raw string literal
                //      ");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 8),
                // (4,9): error CS1026: ) expected
                //      ");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 9),
                // (4,9): error CS1002: ; expected
                //      ");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 9));
    }

    [Fact]
    public void MultiLineCase15()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    ␠"""");",
                // (4,9): error CS8997: Unterminated raw string literal
                //      "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 9),
                // (4,10): error CS1026: ) expected
                //      "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 10),
                // (4,10): error CS1002: ; expected
                //      "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 10));
    }

    [Fact]
    public void MultiLineCase16()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠
    ␠"""");",
                // (4,9): error CS8997: Unterminated raw string literal
                //      "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 9),
                // (4,10): error CS1026: ) expected
                //      "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 10),
                // (4,10): error CS1002: ; expected
                //      "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 10));
    }

    [Fact]
    public void MultiLineCase17()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    ␠␠"""");",
                // (4,10): error CS8997: Unterminated raw string literal
                //       "");
                Diagnostic(ErrorCode.ERR_UnterminatedRawString, ";").WithLocation(4, 10),
                // (4,11): error CS1026: ) expected
                //       "");
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 11),
                // (4,11): error CS1002: ; expected
                //       "");
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 11));
    }

    [Fact]
    public void MultiLineCase18()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    ␠␠"""""");",
                // (4,7): error CS9002: Multi-line raw string literals must contain at least one line of content
                //       """);
                Diagnostic(ErrorCode.ERR_RawStringMustContainContent, @"""""""").WithLocation(4, 7));
    }

    [Fact]
    public void MultiLineCase19()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠

    ␠␠"""""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase20()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a""
    """""");", expectedOutput: "a\"");
    }

    [Fact]
    public void MultiLineCase21()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a""""
    """""");", expectedOutput: "a\"\"");
    }

    [Fact]
    public void MultiLineCase22()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    ""a
    """""");", expectedOutput: "\"a");
    }

    [Fact]
    public void MultiLineCase23()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    """"a
    """""");", expectedOutput: "\"\"a");
    }

    [Fact]
    public void MultiLineCase24()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a"""""");",
                // (4,6): error CS9000: Raw string literal delimiter must be on its own line
                //     a""");
                Diagnostic(ErrorCode.ERR_RawStringDelimiterOnOwnLine, @"""""""").WithLocation(4, 6));
    }

    [Fact]
    public void MultiLineCase25()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a"""""""");",
                // (4,6): error CS9000: Raw string literal delimiter must be on its own line
                //     a"""");
                Diagnostic(ErrorCode.ERR_RawStringDelimiterOnOwnLine, @"""""""""").WithLocation(4, 6));
    }

    [Fact]
    public void MultiLineCase26()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a
    """""");", expectedOutput: "a");
    }

    [Fact]
    public void MultiLineCase27()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    ␠a
    """""");", expectedOutput: " a");
    }

    [Fact]
    public void MultiLineCase28()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a␠
    """""");", expectedOutput: "a ");
    }

    [Fact]
    public void MultiLineCase29()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    ␠a␠
    """""");", expectedOutput: " a ");
    }

    [Fact]
    public void MultiLineCase30()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a
    """""""");",
                // (5,8): error CS8998: Too many closing quotes for raw string literal
                //     """");
                Diagnostic(ErrorCode.ERR_TooManyQuotesForRawString, @"""").WithLocation(5, 8));
    }

    [Fact]
    public void MultiLineCase31()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a
    """""""""");",
                // (5,8): error CS8998: Too many closing quotes for raw string literal
                //     """"");
                Diagnostic(ErrorCode.ERR_TooManyQuotesForRawString, @"""""").WithLocation(5, 8));
    }

    [Fact]
    public void MultiLineCase32()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""␠␠
    a
    """""""""""");",
            // (5,4): error CS8998: Too many closing quotes for raw string literal
            //     """""");
            Diagnostic(ErrorCode.ERR_TooManyQuotesForRawString, @"""""""").WithLocation(5, 8));
    }

    [Fact]
    public void MultiLineCase33()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
  a
    {42}
    b
    {43}
    c
    """""");",
                // (4,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   a
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(4, 1));
    }

    [Fact]
    public void MultiLineCase34()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
  {42}
    b
    {43}
    c
    """""");",
                // (5,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   {42}
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(5, 1));
    }

    [Fact]
    public void MultiLineCase35()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
  b
    {43}
    c
    """""");",
                // (6,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   b
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(6, 1));
    }

    [Fact]
    public void MultiLineCase36()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
    b
  {43}
    c
    """""");",
                // (7,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   {43}
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(7, 1));
    }

    [Fact]
    public void MultiLineCase37()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
    b
    {43}
  c
    """""");",
                // (8,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   c
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(8, 1));
    }

    [Fact]
    public void MultiLineCase38()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
  aa
    {42}
    b
    {43}
    c
    """""");",
                // (5,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   aa
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(5, 1));
    }

    [Fact]
    public void MultiLineCase39()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
  {42}
    b
    {43}
    c
    """""");",
                // (6,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   {42}
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(6, 1));
    }

    [Fact]
    public void MultiLineCase40()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
    b
  bb
    {43}
    c
    """""");",
                // (7,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   bb
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(7, 1));
    }

    [Fact]
    public void MultiLineCase41()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
    b
    {43}
  {43}
    c
    """""");",
                // (8,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   {43}
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(8, 1));
    }

    [Fact]
    public void MultiLineCase42()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    {42}
    b
    {43}
    c
  cc
    """""");",
                // (9,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //   cc
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(9, 1));
    }

    [Fact]
    public void MultiLineCase43()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a{43}
    """""");", expectedOutput: "42a43");
    }

    [Fact]
    public void MultiLineCase44()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a

    a{43}
    """""");", expectedOutput: @"42a

a43");
    }

    [Fact]
    public void MultiLineCase45()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a
␠
    a{43}
    """""");", expectedOutput: @"42a

a43");
    }

    [Fact]
    public void MultiLineCase46()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a
␠␠␠␠␠
    a{43}
    """""");", expectedOutput: @"42a
␠
a43");
    }

    [Fact]
    public void MultiLineCase47()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a
    b

    b
    a{43}
    """""");", expectedOutput: @"42a
b

b
a43");
    }

    [Fact]
    public void MultiLineCase48()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a
    b
␠
    b
    a{43}
    """""");", expectedOutput: @"42a
b

b
a43");
    }

    [Fact]
    public void MultiLineCase49()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    {42}a
    b
␠␠␠␠␠
    b
    a{43}
    """""");", expectedOutput: @"42a
b
␠
b
a43");
    }

    [Fact]
    public void MultiLineCase50()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a

    a
    {42}
    b
    b
    a{43}
    c
    c
    """""");", expectedOutput: @"a

a
42
b
b
a43
c
c");
    }

    [Fact]
    public void MultiLineCase51()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠
    a
    {42}
    b
    b
    a{43}
    c
    c
    """""");", expectedOutput: @"a

a
42
b
b
a43
c
c");
    }

    [Fact]
    public void MultiLineCase52()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠␠␠␠␠
    a
    {42}
    b
    b
    a{43}
    c
    c
    """""");", expectedOutput: @"a
␠
a
42
b
b
a43
c
c");
    }

    [Fact]
    public void MultiLineCase53()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠a
    a
    {42}
    b
    b
    a{43}
    c
    c
    """""");",
                // (5,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //  a
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, " ").WithLocation(5, 1));
    }

    [Fact]
    public void MultiLineCase54()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    a
    {42}
    b
    b
    a{43}
    c
␠
    c
    """""");", expectedOutput: @"a
a
42
b
b
a43
c

c");
    }

    [Fact]
    public void MultiLineCase55()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    a
    {42}
    b
    b
    a{43}
    c
␠␠␠␠␠
    c
    """""");", expectedOutput: @"a
a
42
b
b
a43
c
␠
c");
    }

    [Fact]
    public void MultiLineCase56()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
    a
    {42}
    b
    b
    a{43}
    c
␠c
    c
    """""");",
                // (11,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
                //  c
                Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, " ").WithLocation(11, 1));
    }

    [Fact]
    public void MultiLineCase57()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""

    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase58()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
␠
    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase59()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
␠␠
    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase60()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
␠␠␠␠
    """""");", expectedOutput: "");
    }

    [Fact]
    public void MultiLineCase61()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
␠␠␠␠␠
    """""");", expectedOutput: "␠");
    }

    [Fact]
    public void MultiLineCase62()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a

    """""");", expectedOutput: @"a
");
    }

    [Fact]
    public void MultiLineCase63()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠
    """""");", expectedOutput: @"a
");
    }

    [Fact]
    public void MultiLineCase64()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠␠
    """""");", expectedOutput: @"a
");
    }

    [Fact]
    public void MultiLineCase65()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠␠␠␠
    """""");", expectedOutput: @"a
");
    }

    [Fact]
    public void MultiLineCase66()
    {
        RenderAndVerify(@"
System.Console.Write(
    $""""""
    a
␠␠␠␠␠
    """""");", expectedOutput: @"a
␠");
    }

    [Fact]
    public void TestOutVarOrderOfEvaluation1()
    {
        CompileAndVerify(
@"
using System;

Console.Write($""""""{M(out var x)} {x}"""""");

int M(out int val)
{
    val = 2;
    return 1;
}
", expectedOutput: @"1 2");
    }

    [Fact]
    public void TestOutVarOrderOfEvaluation2()
    {
        RenderAndVerify(
@"
using System;

Console.Write($""""""{x} {M(out var x)}"""""");

int M(out int val)
{
    val = 2;
    return 1;
}
",
                // (4,20): error CS0841: Cannot use local variable 'x' before it is declared
                // Console.Write($"""{x} {M(out var x)}""");
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(4, 20));
    }


    [Fact]
    public void TestWhitespaceMismatch1()
    {
        RenderAndVerify(
"class C\r\n{\r\nconst string s = $\"\"\"\r\n\t\r\n \"\"\";\r\n}",
                // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, "	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch2()
    {
        RenderAndVerify(
"class C\r\n{\r\nconst string s = $\"\"\"\r\n \r\n\t\"\"\";\r\n}",
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\u0020' versus '\t'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " ").WithArguments(@"\u0020", @"\t").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch3()
    {
        RenderAndVerify(
"class C\r\n{\r\nconst string s = $\"\"\"\r\n \t\r\n  \"\"\";\r\n}",
                // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " 	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact]
    public void TestWhitespaceMismatch4()
    {
        RenderAndVerify(
"class C\r\n{\r\nconst string s = $\"\"\"\r\n \t\r\n   \"\"\";\r\n}",
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\t' versus '\u0020'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, " 	").WithArguments(@"\t", @"\u0020").WithLocation(4, 1));
    }

    [Fact, WorkItem(59603, "https://github.com/dotnet/roslyn/issues/59603")]
    public void TestWhitespaceMismatch5()
    {
        RenderAndVerify(
"class C\r\n{\r\nconst string s = $\"\"\"\r\n\f\r\n\v\"\"\";\r\n}",
                    // (4,1): error CS9003: Line contains different whitespace than the closing line of the raw string literal: '\f' versus '\v'
                    Diagnostic(ErrorCode.ERR_LineContainsDifferentWhitespace, "").WithArguments(@"\f", @"\v").WithLocation(4, 1));
    }

    [Fact, WorkItem(59603, "https://github.com/dotnet/roslyn/issues/59603")]
    public void TestThreeDollarTwoCurly_SingleLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$""""""{{1 + 2}}"""""");", expectedOutput: "{{1 + 2}}");
    }

    [Fact]
    public void TestThreeDollarTwoCurly_MultiLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$""""""
    {{1 + 2}}
    """""");", expectedOutput: "{{1 + 2}}");
    }

    [Fact, WorkItem(59603, "https://github.com/dotnet/roslyn/issues/59603")]
    public void TestFourDollarTwoCurly_SingleLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$$""""""{{1 + 2}}"""""");", expectedOutput: "{{1 + 2}}");
    }

    [Fact]
    public void TestFourDollarTwoCurly_MultiLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$""""""
    {{1 + 2}}
    """""");", expectedOutput: "{{1 + 2}}");
    }

    [Fact, WorkItem(59603, "https://github.com/dotnet/roslyn/issues/59603")]
    public void TestThreeDollarThreeCurly_SingleLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$""""""{{{1 + 2}}}"""""");", expectedOutput: "3");
    }

    [Fact]
    public void TestThreeDollarThreeCurly_MultiLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$""""""
    {{{1 + 2}}}
    """""");", expectedOutput: "3");
    }

    [Fact, WorkItem(59603, "https://github.com/dotnet/roslyn/issues/59603")]
    public void TestFourDollarThreeCurly_SingleLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$$""""""{{{1 + 2}}}"""""");", expectedOutput: "{{{1 + 2}}}");
    }

    [Fact]
    public void TestFourDollarThreeCurly_MultiLine()
    {
        RenderAndVerify(@"
System.Console.Write(
    $$$$""""""
    {{{1 + 2}}}
    """""");", expectedOutput: "{{{1 + 2}}}");
    }
}
