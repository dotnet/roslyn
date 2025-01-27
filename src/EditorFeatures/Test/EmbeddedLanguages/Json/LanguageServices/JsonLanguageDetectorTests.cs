// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EmbeddedLanguages.Json.LanguageServices;

public class JsonLanguageDetectorTests
{
    private static void Match(string value, JsonOptions? expectedOptions = null)
    {
        MatchWorker($"/*{value}*/", expectedOptions);
        MatchWorker($"/*{value} */", expectedOptions);
        MatchWorker($"//{value}", expectedOptions);
        MatchWorker($"// {value}", expectedOptions);
        MatchWorker($"'{value}", expectedOptions);
        MatchWorker($"' {value}", expectedOptions);

        static void MatchWorker(string value, JsonOptions? expectedOptions)
        {
            Assert.True(JsonLanguageDetector.CommentDetector.TryMatch(value, out _, out var captures));

            if (expectedOptions != null)
            {
                Assert.True(EmbeddedLanguageCommentOptions<JsonOptions>.TryGetOptions(captures!, out var actualOptions));
                Assert.Equal(expectedOptions.Value, actualOptions);
            }
        }
    }

    private static void NoMatch(string value)
    {
        NoMatchWorker($"/*{value}*/");
        NoMatchWorker($"/*{value} */");
        NoMatchWorker($"//{value}");
        NoMatchWorker($"// {value}");
        NoMatchWorker($"'{value}");
        NoMatchWorker($"' {value}");

        static void NoMatchWorker(string value)
        {
            Assert.False(JsonLanguageDetector.CommentDetector.TryMatch(value, out _, out var stringOptions) &&
                EmbeddedLanguageCommentOptions<JsonOptions>.TryGetOptions(stringOptions, out _));
        }
    }

    [Fact]
    public void TestSimpleForm()
        => Match("lang=json");

    [Fact]
    public void TestAllCaps()
        => Match("lang=JSON");

    [Fact]
    public void TestIncompleteForm1()
        => NoMatch("lan=json");

    [Fact]
    public void TestIncompleteForm2()
        => NoMatch("lang=jso");

    [Fact]
    public void TestMissingEquals()
        => NoMatch("lang json");

    [Fact]
    public void TestLanguageForm()
        => Match("language=json");

    [Fact]
    public void TestLanguageNotFullySpelled()
        => NoMatch("languag=json");

    [Fact]
    public void TestSpacesAroundEquals()
        => Match("lang = json");

    [Fact]
    public void TestSpacesAroundPieces()
        => Match(" lang=json ");

    [Fact]
    public void TestSpacesAroundPiecesAndEquals()
        => Match(" lang = json ");

    [Fact]
    public void TestSpaceBetweenJsonAndNextWord()
        => Match("lang=json here");

    [Fact]
    public void TestPeriodAtEnd()
        => Match("lang=json.");

    [Fact]
    public void TestNotWithWordCharAtEnd()
        => NoMatch("lang=jsonc");

    [Fact]
    public void TestWithNoNWordBeforeStart1()
        => NoMatch(":lang=json");

    [Fact]
    public void TestWithNoNWordBeforeStart2()
        => NoMatch(": lang=json");

    [Fact]
    public void TestNotWithWordCharAtStart()
        => NoMatch("clang=json");

    [Fact]
    public void TestOption()
        => Match("lang=json,strict", JsonOptions.Strict);

    [Fact]
    public void TestOptionWithSpaces()
        => Match("lang=json , strict", JsonOptions.Strict);

    [Fact]
    public void TestOptionFollowedByPeriod()
        => Match("lang=json,strict. Explanation", JsonOptions.Strict);

    [Fact]
    public void TestMultiOptionFollowedByPeriod()
        => Match("lang=json,strict,Strict. Explanation", JsonOptions.Strict);

    [Fact]
    public void TestMultiOptionFollowedByPeriod_CaseInsensitive()
        => Match("Language=Json,Strict. Explanation", JsonOptions.Strict);

    [Fact]
    public void TestInvalidOption1()
        => NoMatch("lang=json,ignore");

    [Fact]
    public void TestInvalidOption2()
        => NoMatch("lang=json,strict,ignore");
}
