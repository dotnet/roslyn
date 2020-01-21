// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EmbeddedLanguages.RegularExpressions.LanguageServices
{
    public class RegexPatternDetectorTests
    {
        private void Match(string value, RegexOptions? expectedOptions = null)
        {
            var (matched, options) = RegexPatternDetector.TestAccessor.TryMatch(value);
            Assert.True(matched);

            if (expectedOptions != null)
            {
                Assert.Equal(expectedOptions.Value, options);
            }
        }

        private void NoMatch(string value)
        {
            var (matched, options) = RegexPatternDetector.TestAccessor.TryMatch(value);
            Assert.False(matched);
        }

        [Fact]
        public void TestSimpleForm()
        {
            Match("lang=regex");
        }

        [Fact]
        public void TestEndingInP()
        {
            Match("lang=regexp");
        }

        [Fact]
        public void TestLanguageForm()
        {
            Match("language=regex");
        }

        [Fact]
        public void TestLanguageFormWithP()
        {
            Match("language=regexp");
        }

        [Fact]
        public void TestLanguageFullySpelled()
        {
            NoMatch("languag=regexp");
        }

        [Fact]
        public void TestSpacesAroundEquals()
        {
            Match("lang = regex");
        }

        [Fact]
        public void TestSpacesAroundPieces()
        {
            Match(" lang=regex ");
        }

        [Fact]
        public void TestSpacesAroundPiecesAndEquals()
        {
            Match(" lang = regex ");
        }

        [Fact]
        public void TestSpaceBetweenRegexAndP()
        {
            Match("lang=regex p");
        }

        [Fact]
        public void TestPeriodAtEnd()
        {
            Match("lang=regex.");
        }

        [Fact]
        public void TestNotWithWordCharAtEnd()
        {
            NoMatch("lang=regexc");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart1()
        {
            Match(":lang=regex");
        }

        [Fact]
        public void TestWithNoNWordBeforeStart2()
        {
            Match(": lang=regex");
        }

        [Fact]
        public void TestNotWithWordCharAtStart()
        {
            NoMatch("clang=regex");
        }

        [Fact]
        public void TestOption()
        {
            Match("lang=regex,ecmascript", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOptionWithSpaces()
        {
            Match("lang=regex , ecmascript", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestOptionFollowedByPeriod()
        {
            Match("lang=regex,ecmascript. Explanation", RegexOptions.ECMAScript);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod()
        {
            Match("lang=regex,ecmascript,ignorecase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestMultiOptionFollowedByPeriod_CaseInsensitive()
        {
            Match("Language=Regexp,ECMAScript,IgnoreCase. Explanation", RegexOptions.ECMAScript | RegexOptions.IgnoreCase);
        }

        [Fact]
        public void TestInvalidOption1()
        {
            NoMatch("lang=regex,ignore");
        }

        [Fact]
        public void TestInvalidOption2()
        {
            NoMatch("lang=regex,ecmascript,ignore");
        }
    }
}
