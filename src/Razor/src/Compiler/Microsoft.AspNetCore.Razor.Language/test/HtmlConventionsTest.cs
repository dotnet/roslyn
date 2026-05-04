// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class HtmlConventionsTest
{
    public static TheoryData<string, string> HtmlConversionData
    {
        get
        {
            return new TheoryData<string, string>
                {
                    { "SomeThing", "some-thing" },
                    { "someOtherThing", "some-other-thing" },
                    { "capsONInside", "caps-on-inside" },
                    { "CAPSOnOUTSIDE", "caps-on-outside" },
                    { "ALLCAPS", "allcaps" },
                    { "One1Two2Three3", "one1-two2-three3" },
                    { "ONE1TWO2THREE3", "one1two2three3" },
                    { "First_Second_ThirdHi", "first_second_third-hi" },
                    { "ONE1Two", "one1-two" },
                    { "One123Two234Three345", "one123-two234-three345" },
                    { "ONE123TWO234THREE345", "one123two234three345" },
                    { "1TWO2THREE3", "1two2three3" },
                    { "alllowercase", "alllowercase" },
                };
        }
    }

    private static readonly Regex OldHtmlCaseRegex = new Regex(
        "(?<!^)((?<=[a-zA-Z0-9])[A-Z][a-z])|((?<=[a-z])[A-Z])",
        RegexOptions.None,
        TimeSpan.FromMilliseconds(500));

    [Theory]
    [MemberData(nameof(HtmlConversionData))]
    public void ToHtmlCase_ReturnsExpectedConversions(string input, string expectedOutput)
    {
        // Arrange, Act
        var output = HtmlConventions.ToHtmlCase(input);

        // Assert
        Assert.Equal(expectedOutput, output);

        // Assure backwards compatibility with regex
        var regexResult = OldHtmlCaseRegex.Replace(input, "-$1$2").ToLowerInvariant();
        Assert.Equal(regexResult, output);
    }
}
