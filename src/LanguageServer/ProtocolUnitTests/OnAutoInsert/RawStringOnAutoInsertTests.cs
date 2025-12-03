// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.OnAutoInsert;

public sealed class RawStringOnAutoInsertTests(ITestOutputHelper testOutputHelper) : AbstractOnAutoInsertTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateInitialEmpty(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"
            var v = """{|type:|}
            """",
            """"
            var v = """$0"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateInitialEmpty_Interpolated(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"
            var v = $"""{|type:|}
            """",
            """"
            var v = $"""$0"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowInitialEmpty(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = """"{|type:|}"""
            """"",
            """""
            var v = """"$0""""
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowWithText(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = """"{|type:|} test """
            """"",
            """""
            var v = """" test """"
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_NoGrowForTwoStart(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """"
            var v = """{|type:|}"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_NoGrowForVerbatim(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """
            var v = @""${|type:|}
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateAtEndOfFile(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"var v = """{|type:|}"""",
            """"
            var v = """$0"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateWithSemicolonAfter(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"var v = """{|type:|};"""",
            """"var v = """$0""";"""", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateWithInterpolatedString_TwoDollarSigns(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"var v = $$"""{|type:|}"""",
            """"
            var v = $$"""$0"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GenerateWithInterpolatedString_ThreeDollarSigns(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """"var v = $$$"""{|type:|}"""",
            """"
            var v = $$$"""$0"""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_NoGenerateWithVerbatimString(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """var v = @""{|type:|}""", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_NoGenerateWithVerbatimInterpolatedString1(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """var v = @$""{|type:|}""", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_NoGenerateWithVerbatimInterpolatedString2(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """var v = $@""{|type:|}""", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_DoNotGrowEmptyInsideSimpleString(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """
            var v = ""{|type:|}"
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_DoNotGrowEmptyInsideFourQuotes(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """"
            var v = """{|type:|}""
            """", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowEmptyInsideSixQuotesInInterpolatedRaw(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = $""""{|type:|}"""
            """"",
            """""
            var v = $""""$0""""
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_DoNotGrowEmptyInsideSixQuotesWhenNotInMiddle(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """""
            var v = $"""{|type:|}""""
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowDelimitersWhenEndExists_SingleLine(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = """"{|type:|} """
            """"",
            """""
            var v = """" """"
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowDelimitersWhenEndExists_MultiLine(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = """"{|type:|}

                """
            """"",
            """""
            var v = """"

                """"
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_GrowDelimitersWhenEndExists_Interpolated(bool mutatingLspWorkspace)
        => VerifyCSharpMarkupAndExpectedRawString("\"",
            """""
            var v = $""""{|type:|}

                """
            """"",
            """""
            var v = $""""

                """"
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_DoNotGrowDelimitersWhenEndNotThere(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """""
            var v = """"{|type:|}
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_DoNotGrowDelimitersWhenEndTooShort(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """""
            var v = """"{|type:|}

                ""
            """"", mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task OnAutoInsert_RawString_TypeQuoteEmptyFile(bool mutatingLspWorkspace)
        => VerifyNoResult("\"",
            """
            "{|type:|}
            """, mutatingLspWorkspace);

    private async Task VerifyCSharpMarkupAndExpectedRawString(
        string characterTyped,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var locationTyped = testLspServer.GetLocations("type").Single();

        var document = await testLspServer.GetDocumentAsync(locationTyped.DocumentUri);
        var documentText = await document.GetTextAsync();

        var result = await RunOnAutoInsertAsync(testLspServer, characterTyped, locationTyped, insertSpaces: true, tabSize: 4);

        AssertEx.NotNull(result);
        var actualText = ApplyTextEdits([result.TextEdit], documentText);

        var expectedCaret = expected.IndexOf("$0");
        if (expectedCaret >= 0)
        {
            Assert.Equal(InsertTextFormat.Snippet, result.TextEditFormat);
        }
        else
        {
            Assert.Equal(InsertTextFormat.Plaintext, result.TextEditFormat);
        }

        MarkupTestFile.GetPositionAndSpans(expected, out var massaged, out int? caretPosition, out var spans);

        Assert.Equal(massaged, actualText);
    }
}
