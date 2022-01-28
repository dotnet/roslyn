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
            var (success, actualOptions) = RegexPatternDetector.TestAccessor.TryMatch(value);
            Assert.True(success);

            if (expectedOptions != null)
            {
                Assert.Equal(expectedOptions.Value, actualOptions);
            }
        }

        private static void NoMatch(string value)
        {
            var (success, _) = RegexPatternDetector.TestAccessor.TryMatch(value);
            Assert.False(success);
        }

        [Fact]
        public void TestSimpleForm()
        {
            RegexPatternDetectorTests.Match("lang=regex");
        }

        [Fact]
        public void TestIncompleteForm1()
        {
            RegexPatternDetectorTests.NoMatch("lan=regex");
        }

        [Fact]
        public void TestIncompleteForm2()
        {
            RegexPatternDetectorTests.NoMatch("lang=rege");
        }

        [Fact]
        public void TestMissingEquals()
        {
            RegexPatternDetectorTests.NoMatch("lang regex");
        }

        [Fact]
        public void TestEndingInP()
        {
            RegexPatternDetectorTests.Match("lang=regexp");
        }

        [Fact]
        public void TestLanguageForm()
        {
            RegexPatternDetectorTests.Match("language=regex");
        }

        [Fact]
        public void TestLanguageFormWithP()
        {
            RegexPatternDetectorTests.Match("language=regexp");
        }

        [Fact]
        public void TestLanguageFullySpelled()
        {
            RegexPatternDetectorTests.NoMatch("languag=regexp");
        }

        [Fact]
        public void TestSpacesAroundEquals()
        {
            RegexPatternDetectorTests.Match("lang = regex");
        }

        [Fact]
        public void TestSpacesAroundPieces()
        {
            RegexPatternDetectorTests.Match(" lang=regex ");
        }

        [Fact]
        public void TestSpacesAroundPiecesAndEquals()
        {
            RegexPatternDetectorTests.Match(" lang = regex ");
        }

        [Fact]
        public void TestSpaceBetweenRegexAndP()
        {
            RegexPatternDetectorTests.Match("lang=regex p");
        }

        [Fact]
        public void TestPeriodAtEnd()
        {
            RegexPatternDetectorTests.Match("lang=regex.");
        }

        [Fact]
        public void TestNotWithWordCharAtEnd()
        {
            RegexPatternDetectorTests.NoMatch("lang=regexc");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart1()
        {
            RegexPatternDetectorTests.Match(":lang=regex");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart2()
        {
            RegexPatternDetectorTests.Match(": lang=regex");
        }

        [Fact]
        public void TestNotWithWordCharAtStart()
        {
            RegexPatternDetectorTests.NoMatch("clang=regex");
        }

        [Fact]
        public void TestOption()
        {
            RegexPatternDetectorTests.Match("lang=regex,ecmascript", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOptionWithSpaces()
        {
            RegexPatternDetectorTests.Match("lang=regex , ecmascript", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOptionFollowedByPeriod()
        {
            RegexPatternDetectorTests.Match("lang=regex,ecmascript. Explanation", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod()
        {
            RegexPatternDetectorTests.Match("lang=regex,ecmascript,ignorecase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod_CaseInsensitive()
        {
            RegexPatternDetectorTests.Match("Language=Regexp,ECMAScript,IgnoreCase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestInvalidOption1()
        {
            RegexPatternDetectorTests.NoMatch("lang=regex,ignore");
        }

        [Fact]
        public void TestInvalidOption2()
        {
            RegexPatternDetectorTests.NoMatch("lang=regex,ecmascript,ignore");
        }
    }
}
