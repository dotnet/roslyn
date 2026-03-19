// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public class RawInterpolatedStringLiteralParsingTests : CSharpTestBase
{
    #region Single Line

    [Fact]
    public void SingleLine1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineTooManyCloseQuotes1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" """""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                // (6,25): error CS8998: Too many closing quotes for raw string literal
                //         var v = $""" """";
                Diagnostic(ErrorCode.ERR_TooManyQuotesForRawString, @"""").WithLocation(6, 25));
    }

    [Fact]
    public void SingleLineTooManyCloseQuotes2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" """""""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,25): error CS8998: Too many closing quotes for raw string literal
            //         var v = $""" """"";
            Diagnostic(ErrorCode.ERR_TooManyQuotesForRawString, @"""""").WithLocation(6, 25));
    }

    [Fact]
    public void SingleLineSingleQuoteInside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" "" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineDoubleQuoteInside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" """" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationInside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{0}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationInsideSpacesOutside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" {0} """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationInsideSpacesInside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{ 0 }"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationInsideSpacesInsideAndOutside()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $"""""" { 0 } """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{{0}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,21): error CS9006: Too many open braces for raw string literal
            //         var v = $"""{{0}}""";
            Diagnostic(ErrorCode.ERR_TooManyOpenBracesForRawString, "{").WithLocation(6, 21));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{{0}}}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,22): error CS9006: Too many open braces for raw string literal
            //         var v = $$"""{{{{0}}}}""";
            Diagnostic(ErrorCode.ERR_TooManyOpenBracesForRawString, "{{").WithLocation(6, 22));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{0}}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,24): error CS9007: Too many closing braces for raw string literal
            //         var v = $"""{0}}}""";
            Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}}").WithLocation(6, 24));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{0}}}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,28): error CS9007: Too many closing braces for raw string literal
            //         var v = $$"""{{{0}}}}""";
            Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}}").WithLocation(6, 28));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{0}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,24): error CS9007: Too many closing braces for raw string literal
            //         var v = $$"""{0}}""";
            Diagnostic(ErrorCode.ERR_TooManyCloseBracesForRawString, "}}").WithLocation(6, 24));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesNotAllowed6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{0}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,22): error CS9005: Not enough closing braces for raw string literal
            //         var v = $$"""{{{0}""";
            Diagnostic(ErrorCode.ERR_NotEnoughCloseBracesForRawString, "{").WithLocation(6, 22));
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesAllowed1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{0}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesAllowed2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{0}}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationMultipleCurliesAllowed4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{0}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingNormalString()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""a""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimString1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@""a""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimString2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@""
a""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingInterpolatedString1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""a""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingInterpolatedString2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""{0}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimInterpolatedString1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$@""{0}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimInterpolatedString2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@$""{0}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimInterpolatedString3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$@""{
0}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingVerbatimInterpolatedString4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{
$@""{
0}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawStringLiteral1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""""""a""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawStringLiteral2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""""""
  a
  """"""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawStringLiteral3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""""""
  a
    """"""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (7,1): error CS8999: Line does not start with the same whitespace as the closing line of the raw string literal
            //   a
            Diagnostic(ErrorCode.ERR_LineDoesNotStartWithSameWhitespace, "  ").WithLocation(7, 1));
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$"""""" """"""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$"""""""" """"""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""""""{0}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""""""{
0}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{
$""""""{
0}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$$""""""{{0}}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral7()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$$""""""{{{0}}}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingRawInterpolatedStringLiteral8()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""""{{{$""""""{0}""""""}}}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingClosingBraceAsCharacterLiteral()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{'}'}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingClosingBraceAsRegularStringLiteral()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingClosingBraceAsVerbatimStringLiteral()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void SingleLineInterpolationContainingClosingBraceAsRawStringLiteral()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{""""""}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleNormalInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""{$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleNormalInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""{$@""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleNormalInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""{$""""""{0}""""""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleVerbatimInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{@$""{$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleVerbatimInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{@$""{@$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleVerbatimInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{@$""{$""""""{0}""""""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleRawInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""""""{$""{0}""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleRawInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""""""{@$""{0}""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterNormalMiddleRawInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""{$""""""{$""""""{0}""""""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleNormalInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""{$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleNormalInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""{$@""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleNormalInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""{$""""""{0}""""""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleVerbatimInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{@$""{$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleVerbatimInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{@$""{@$""{0}""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleVerbatimInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{@$""{$""""""{0}""""""}""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleRawInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""""""{$""{0}""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleRawInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""""""{@$""{0}""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterVerbatimMiddleRawInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@""{$""""""{$""""""{0}""""""}""""""}"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleNormalInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""{$""{0}""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleNormalInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""{$@""{0}""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleNormalInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""{$""""""{0}""""""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleVerbatimInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@$""{$""{0}""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleVerbatimInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@$""{@$""{0}""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleVerbatimInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{@$""{$""""""{0}""""""}""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleRawInnerNormal()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""""""{$""{0}""}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleRawInnerVerbatim()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""""""{@$""{0}""}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void OuterRawMiddleRawInnerRaw()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $""""""{$""""""{$""""""{0}""""""}""""""}"""""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics();
    }

    [Fact]
    public void MultipleAtSigns1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS1525: Invalid expression term ''
                    //         var v = @@;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "@@").WithArguments("").WithLocation(6, 17),
                    // (6,17): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                    //         var v = @@;
                    Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, "").WithLocation(6, 17));
    }

    [Fact]
    public void MultipleAtSigns2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@";
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@").WithLocation(6, 17),
                    // (6,17): error CS1039: Unterminated string literal
                    //         var v = @@";
                    Diagnostic(ErrorCode.ERR_UnterminatedStringLit, "").WithLocation(6, 17),
                    // (8,2): error CS1002: ; expected
                    // }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 2),
                    // (8,2): error CS1513: } expected
                    // }
                    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2),
                    // (8,2): error CS1513: } expected
                    // }
                    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2));
    }

    [Fact]
    public void MultipleAtSigns3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@" ";
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@").WithLocation(6, 17));
    }

    [Fact]
    public void MultipleAtSigns4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@""" """;
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@").WithLocation(6, 17));
    }

    [Fact]
    public void MultipleAtSigns5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS1525: Invalid expression term ''
                    //         var v = @@@;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "@@@").WithArguments("").WithLocation(6, 17),
                    // (6,17): error CS1646: Keyword, identifier, or string expected after verbatim specifier: @
                    //         var v = @@@;
                    Diagnostic(ErrorCode.ERR_ExpectedVerbatimLiteral, "").WithLocation(6, 17));
    }

    [Fact]
    public void MultipleAtSigns6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@@";
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@@").WithLocation(6, 17),
                    // (6,17): error CS1039: Unterminated string literal
                    //         var v = @@@";
                    Diagnostic(ErrorCode.ERR_UnterminatedStringLit, "").WithLocation(6, 17),
                    // (8,2): error CS1002: ; expected
                    // }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 2),
                    // (8,2): error CS1513: } expected
                    // }
                    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2),
                    // (8,2): error CS1513: } expected
                    // }
                    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 2));
    }

    [Fact]
    public void MultipleAtSigns7()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@@" ";
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@@").WithLocation(6, 17));
    }

    [Fact]
    public void MultipleAtSigns8()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9008: Sequence of '@' characters is not allowed. A verbatim string or identifier can only have one '@' character and a raw string cannot have any.
                    //         var v = @@@""" """;
                    Diagnostic(ErrorCode.ERR_IllegalAtSequence, "@@@").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = $@@;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "$@@").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@""").WithLocation(6, 17),
            // (6,22): error CS1002: ; expected
            //         var v = $@@";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 22));
    }

    [Fact]
    public void DollarThenAt3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@""").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@""""""").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = $@@@;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "$@@@").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@@";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@@""").WithLocation(6, 17),
            // (6,23): error CS1002: ; expected
            //         var v = $@@@";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 23));
    }

    [Fact]
    public void DollarThenAt7()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@@" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@@""").WithLocation(6, 17));
    }

    [Fact]
    public void DollarThenAt8()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $@@@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = $@@@""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"$@@@""""""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = @@$;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "@@$").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$""").WithLocation(6, 17),
            // (6,22): error CS1002: ; expected
            //         var v = @@$";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 22));
    }

    [Fact]
    public void AtThenDollar3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$""""""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = @@$$;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "@@$$").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$""").WithLocation(6, 17),
            // (6,23): error CS1002: ; expected
            //         var v = @@$$";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 23));
    }

    [Fact]
    public void AtThenDollar7()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollar8()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$""""""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = @@$@;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "@@$@").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$@";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$@""").WithLocation(6, 17),
            // (6,23): error CS1002: ; expected
            //         var v = @@$@";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 23));
    }

    [Fact]
    public void AtThenDollarThenAt3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$@" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$@""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt4()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$@""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$@""""""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt5()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$@;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = @@$$@;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "@@$$@").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt6()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$@"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$@";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$@""").WithLocation(6, 17),
            // (6,24): error CS1002: ; expected
            //         var v = @@$$@";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 24));
    }

    [Fact]
    public void AtThenDollarThenAt7()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$@"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$@" ";
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$@""").WithLocation(6, 17));
    }

    [Fact]
    public void AtThenDollarThenAt8()
    {
        var text = @"
class C
{
    void M()
    {
        var v = @@$$@"""""" """""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,17): error CS9009: Cannot mix verbatim and raw strings
            //         var v = @@$$@""" """;
            Diagnostic(ErrorCode.ERR_IllegalAtSequence, @"@@$$@""""""").WithLocation(6, 17));
    }

    [Fact]
    public void DollarsWithoutQuotes0()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS1525: Invalid expression term ''
                    //         var v = $;
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "$").WithArguments("").WithLocation(6, 17),
                    // (6,17): error CS1056: Unexpected character '$'
                    //         var v = $;
                    Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments("$").WithLocation(6, 17));
    }

    [Fact]
    public void DollarsWithoutQuotes1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                // (6,17): error CS9009: String must start with quote character: "
                //         var v = $$;
                Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "$$").WithLocation(6, 17));
    }

    [Fact]
    public void DollarsWithoutQuotes2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$$;
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
                    // (6,17): error CS9009: String must start with quote character: "
                    //         var v = $$$;
                    Diagnostic(ErrorCode.ERR_StringMustStartWithQuoteCharacter, "$$$").WithLocation(6, 17));
    }

    [Fact]
    public void DollarsWithQuotes1()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,19): error CS9004: Not enough quotes for raw string literal
            //         var v = $$";
            Diagnostic(ErrorCode.ERR_NotEnoughQuotesForRawString, @"""").WithLocation(6, 19),
            // (6,21): error CS1002: ; expected
            //         var v = $$";
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 21));
    }

    [Fact]
    public void DollarsWithQuotes2()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$"" "";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,19): error CS9004: Not enough quotes for raw string literal
            //         var v = $$" ";
            Diagnostic(ErrorCode.ERR_NotEnoughQuotesForRawString, @"""").WithLocation(6, 19));
    }

    [Fact]
    public void DollarsWithQuotes3()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$"""" """";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,19): error CS9004: Not enough quotes for raw string literal
            //         var v = $$"" "";
            Diagnostic(ErrorCode.ERR_NotEnoughQuotesForRawString, @"""""").WithLocation(6, 19));
    }

    #endregion

    #region Multi Line

    [Fact]
    public void DollarsWithQuotes2_MultiLine()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""

"";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,19): error CS9004: Not enough quotes for raw string literal
            //         var v = $$"
            Diagnostic(ErrorCode.ERR_NotEnoughQuotesForRawString, @"""").WithLocation(6, 19));
    }

    [Fact]
    public void DollarsWithQuotes3_MultiLine()
    {
        var text = @"
class C
{
    void M()
    {
        var v = $$""""

"""";
    }
}";

        CreateCompilation(text).VerifyDiagnostics(
            // (6,19): error CS9004: Not enough quotes for raw string literal
            //         var v = $$""
            Diagnostic(ErrorCode.ERR_NotEnoughQuotesForRawString, @"""""").WithLocation(6, 19));
    }

    #endregion
}
