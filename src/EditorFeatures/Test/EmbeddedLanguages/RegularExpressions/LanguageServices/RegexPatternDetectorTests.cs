// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    public class RegexPatternDetectorTests
    {
        private static void Match(string value, RegexOptions? expectedOptions = null)
        {
            MatchWorker($"/*{value}*/", expectedOptions);
            MatchWorker($"/*{value} */", expectedOptions);
            MatchWorker($"//{value}", expectedOptions);
            MatchWorker($"// {value}", expectedOptions);
            MatchWorker($"'{value}", expectedOptions);
            MatchWorker($"' {value}", expectedOptions);

            static void MatchWorker(string value, RegexOptions? expectedOptions)
            {
                Assert.True(RegexLanguageDetector.TestAccessor.TryMatch(value, out var actualOptions));

                if (expectedOptions != null)
                    Assert.Equal(expectedOptions.Value, actualOptions);
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
                Assert.False(RegexLanguageDetector.TestAccessor.TryMatch(value, out _));
            }
        }

        [Fact]
        public void TestSimpleForm()
            => Match("lang=regex");

        [Fact]
        public void TestIncompleteForm1()
            => NoMatch("lan=regex");

        [Fact]
        public void TestIncompleteForm2()
            => NoMatch("lang=rege");

        [Fact]
        public void TestMissingEquals()
            => NoMatch("lang regex");

        [Fact]
        public void TestEndingInP()
            => Match("lang=regexp");

        [Fact]
        public void TestLanguageForm()
            => Match("language=regex");

        [Fact]
        public void TestLanguageFormWithP()
            => Match("language=regexp");

        [Fact]
        public void TestLanguageFullySpelled()
            => NoMatch("languag=regexp");

        [Fact]
        public void TestSpacesAroundEquals()
            => Match("lang = regex");

        [Fact]
        public void TestSpacesAroundPieces()
            => Match(" lang=regex ");

        [Fact]
        public void TestSpacesAroundPiecesAndEquals()
            => Match(" lang = regex ");

        [Fact]
        public void TestSpaceBetweenRegexAndP()
            => Match("lang=regex p");

        [Fact]
        public void TestPeriodAtEnd()
            => Match("lang=regex.");

        [Fact]
        public void TestNotWithWordCharAtEnd()
            => NoMatch("lang=regexc");

        [Fact]
        public void TestWithNoNWordBeforeStart1()
            => NoMatch(":lang=regex");

        [Fact]
        public void TestWithNoNWordBeforeStart2()
            => NoMatch(": lang=regex");

        [Fact]
        public void TestNotWithWordCharAtStart()
            => NoMatch("clang=regex");

        [Fact]
        public void TestOption()
            => Match("lang=regex,ecmascript", RegexOptions.ECMAScript);

        [Fact]
        public void TestOptionWithSpaces()
            => Match("lang=regex , ecmascript", RegexOptions.ECMAScript);

        [Fact]
        public void TestOptionFollowedByPeriod()
            => Match("lang=regex,ecmascript. Explanation", RegexOptions.ECMAScript);

        [Fact]
        public void TestMultiOptionFollowedByPeriod()
            => Match("lang=regex,ecmascript,ignorecase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        [Fact]
        public void TestMultiOptionFollowedByPeriod_CaseInsensitive()
            => Match("Language=Regexp,ECMAScript,IgnoreCase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);

        [Fact]
        public void TestInvalidOption1()
            => NoMatch("lang=regex,ignore");

        [Fact]
        public void TestInvalidOption2()
            => NoMatch("lang=regex,ecmascript,ignore");
    }
}
