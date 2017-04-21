// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    public class PatternMatcherTests
    {
        [Fact]
        public void BreakIntoCharacterParts_EmptyIdentifier()
        {
            VerifyBreakIntoCharacterParts(string.Empty, Array.Empty<string>());
        }

        [Fact]
        public void BreakIntoCharacterParts_SimpleIdentifier()
        {
            VerifyBreakIntoCharacterParts("foo", "foo");
        }

        [Fact]
        public void BreakIntoCharacterParts_PrefixUnderscoredIdentifier()
        {
            VerifyBreakIntoCharacterParts("_foo", "_", "foo");
        }

        [Fact]
        public void BreakIntoCharacterParts_UnderscoredIdentifier()
        {
            VerifyBreakIntoCharacterParts("f_oo", "f", "_", "oo");
        }

        [Fact]
        public void BreakIntoCharacterParts_PostfixUnderscoredIdentifier()
        {
            VerifyBreakIntoCharacterParts("foo_", "foo", "_");
        }

        [Fact]
        public void BreakIntoCharacterParts_PrefixUnderscoredIdentifierWithCapital()
        {
            VerifyBreakIntoCharacterParts("_Foo", "_", "Foo");
        }

        [Fact]
        public void BreakIntoCharacterParts_MUnderscorePrefixed()
        {
            VerifyBreakIntoCharacterParts("m_foo", "m", "_", "foo");
        }

        [Fact]
        public void BreakIntoCharacterParts_CamelCaseIdentifier()
        {
            VerifyBreakIntoCharacterParts("FogBar", "Fog", "Bar");
        }

        [Fact]
        public void BreakIntoCharacterParts_MixedCaseIdentifier()
        {
            VerifyBreakIntoCharacterParts("fogBar", "fog", "Bar");
        }

        [Fact]
        public void BreakIntoCharacterParts_TwoCharacterCapitalIdentifier()
        {
            VerifyBreakIntoCharacterParts("UIElement", "U", "I", "Element");
        }

        [Fact]
        public void BreakIntoCharacterParts_NumberSuffixedIdentifier()
        {
            VerifyBreakIntoCharacterParts("Foo42", "Foo", "42");
        }

        [Fact]
        public void BreakIntoCharacterParts_NumberContainingIdentifier()
        {
            VerifyBreakIntoCharacterParts("Fog42Bar", "Fog", "42", "Bar");
        }

        [Fact]
        public void BreakIntoCharacterParts_NumberPrefixedIdentifier()
        {
            // 42Bar is not a valid identifier in either C# or VB, but it is entirely conceivable the user might be
            // typing it trying to do a substring match
            VerifyBreakIntoCharacterParts("42Bar", "42", "Bar");
        }

        [Fact]
        [WorkItem(544296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544296")]
        public void BreakIntoWordParts_VerbatimIdentifier()
        {
            VerifyBreakIntoWordParts("@int:", "int");
        }

        [Fact]
        [WorkItem(537875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537875")]
        public void BreakIntoWordParts_AllCapsConstant()
        {
            VerifyBreakIntoWordParts("C_STYLE_CONSTANT", "C", "_", "STYLE", "_", "CONSTANT");
        }

        [Fact]
        [WorkItem(540087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
        public void BreakIntoWordParts_SingleLetterPrefix1()
        {
            VerifyBreakIntoWordParts("UInteger", "U", "Integer");
        }

        [Fact]
        [WorkItem(540087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
        public void BreakIntoWordParts_SingleLetterPrefix2()
        {
            VerifyBreakIntoWordParts("IDisposable", "I", "Disposable");
        }

        [Fact]
        [WorkItem(540087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
        public void BreakIntoWordParts_TwoCharacterCapitalIdentifier()
        {
            VerifyBreakIntoWordParts("UIElement", "UI", "Element");
        }

        [Fact]
        [WorkItem(540087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
        public void BreakIntoWordParts_XDocument()
        {
            VerifyBreakIntoWordParts("XDocument", "X", "Document");
        }

        [Fact]
        [WorkItem(540087, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
        public void BreakIntoWordParts_XMLDocument1()
        {
            VerifyBreakIntoWordParts("XMLDocument", "XML", "Document");
        }

        [Fact]
        public void BreakIntoWordParts_XMLDocument2()
        {
            VerifyBreakIntoWordParts("XmlDocument", "Xml", "Document");
        }

        [Fact]
        public void BreakIntoWordParts_TwoUppercaseCharacters()
        {
            VerifyBreakIntoWordParts("SimpleUIElement", "Simple", "UI", "Element");
        }

        private void VerifyBreakIntoWordParts(string original, params string[] parts)
        {
            AssertEx.Equal(parts, BreakIntoWordParts(original));
        }

        private void VerifyBreakIntoCharacterParts(string original, params string[] parts)
        {
            AssertEx.Equal(parts, BreakIntoCharacterParts(original));
        }

        private const bool CaseSensitive = true;
        private const bool CaseInsensitive = !CaseSensitive;

        [Theory]
        [InlineData("[|Foo|]", "Foo", PatternMatchKind.Exact, CaseSensitive)]
        [InlineData("[|foo|]", "Foo", PatternMatchKind.Exact, CaseInsensitive)]
        [InlineData("[|Foo|]", "foo", PatternMatchKind.Exact, CaseInsensitive)]

        [InlineData("[|Fo|]o", "Fo", PatternMatchKind.Prefix, CaseSensitive)]
        [InlineData("[|Fog|]Bar", "Fog", PatternMatchKind.Prefix, CaseSensitive)]

        [InlineData("[|Fo|]o", "fo", PatternMatchKind.Prefix, CaseInsensitive)]
        [InlineData("[|Fog|]Bar", "fog", PatternMatchKind.Prefix, CaseInsensitive)]
        [InlineData("[|fog|]BarFoo", "Fog", PatternMatchKind.Prefix, CaseInsensitive)]

        [InlineData("Fog[|B|]ar", "b", PatternMatchKind.Substring, CaseInsensitive)]

        [InlineData("_[|my|]Button", "my", PatternMatchKind.Substring, CaseSensitive)]
        [InlineData("my[|_b|]utton", "_b", PatternMatchKind.Substring, CaseSensitive)]
        [InlineData("_[|my|]button", "my", PatternMatchKind.Substring, CaseSensitive)]
        [InlineData("_my[|_b|]utton", "_b", PatternMatchKind.Substring, CaseSensitive)]
        [InlineData("_[|myb|]utton", "myb", PatternMatchKind.Substring, CaseSensitive)]
        [InlineData("_[|myB|]utton", "myB", PatternMatchKind.Substring, CaseSensitive)]

        [InlineData("my[|_B|]utton", "_b", PatternMatchKind.Substring, CaseInsensitive)]
        [InlineData("_my[|_B|]utton", "_b", PatternMatchKind.Substring, CaseInsensitive)]
        [InlineData("_[|myB|]utton", "myb", PatternMatchKind.Substring, CaseInsensitive)]

        [InlineData("[|AbCd|]xxx[|Ef|]Cd[|Gh|]", "AbCdEfGh", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]

        [InlineData("A[|BCD|]EFGH", "bcd", PatternMatchKind.Substring, CaseInsensitive)]
        [InlineData("Abcdefghij[|EfgHij|]", "efghij", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseContiguousBonus)]

        [InlineData("[|F|]og[|B|]ar", "FB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|Fo|]g[|B|]ar", "FoB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|_f|]og[|B|]ar", "_fB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|F|]og[|_B|]ar", "F_B", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|F|]og[|B|]ar", "fB", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("Baz[|F|]ogBar[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.NoBonus)]
        [InlineData("[|F|]og[|B|]arBaz", "FB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|F|]og_[|B|]ar", "FB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("[|F|]ooFlob[|B|]az", "FB", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("Bar[|F|]oo[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseContiguousBonus)]
        [InlineData("BazBar[|F|]oo[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseContiguousBonus)]
        [InlineData("[|Fo|]oBarry[|Bas|]il", "FoBas", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("[|F|]ogBar[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]

        [InlineData("[|F|]og[|_B|]ar", "F_b", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|_F|]og[|B|]ar", "_fB", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|F|]og[|_B|]ar", "f_B", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("FogBar[|ChangedEventArgs|]", "changedeventargs", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseContiguousBonus)]

        [InlineData("[|Si|]mple[|UI|]Element", "SiUI", PatternMatchKind.CamelCase, CaseSensitive, PatternMatcher.CamelCaseMaxWeight)]

        [InlineData("_[|co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.NoBonus)]
        [InlineData("Code[|Fi|]xObject[|Pro|]vider", "fipro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.NoBonus)]
        [InlineData("[|Co|]de[|Fi|]x[|Pro|]vider", "cofipro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("Code[|Fi|]x[|Pro|]vider", "fipro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseContiguousBonus)]
        [InlineData("[|Co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("[|co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("[|Co|]deFix_[|Pro|]vider", "copro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        [InlineData("[|C|]ore[|Ofi|]lac[|Pro|]fessional", "cofipro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|C|]lear[|Ofi|]lac[|Pro|]fessional", "cofipro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMaxWeight)]
        [InlineData("[|CO|]DE_FIX_[|PRO|]VIDER", "copro", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]

        [InlineData("my[|_b|]utton", "_B", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseContiguousBonus)]
        [InlineData("[|_|]my_[|b|]utton", "_B", PatternMatchKind.CamelCase, CaseInsensitive, PatternMatcher.CamelCaseMatchesFromStartBonus)]
        public void TryMatchSingleWordPattern(
            string candidate, string pattern, int matchKindInt, bool isCaseSensitive, int? camelCaseWeight = null)
        {
            var match = TryMatchSingleWordPattern(candidate, pattern);
            Assert.NotNull(match);

            var matchKind = (PatternMatchKind)matchKindInt;
            Assert.Equal(match.Value.Kind, matchKind);
            Assert.Equal(match.Value.IsCaseSensitive, isCaseSensitive);

            if (matchKind == PatternMatchKind.CamelCase)
            {
                Assert.NotNull(match.Value.CamelCaseWeight);
                Assert.NotNull(camelCaseWeight);

                Assert.Equal(match.Value.CamelCaseWeight, camelCaseWeight);
            }
            else
            {
                Assert.Null(match.Value.CamelCaseWeight);
                Assert.Null(camelCaseWeight);
            }
        }

        [Theory]
        [InlineData("CodeFixObjectProvider", "ficopro")]
        [InlineData("FogBar", "FBB")]
        [InlineData("FogBarBaz", "ZZ")]
        [InlineData("FogBar", "FoooB")]
        [InlineData("FooActBarCatAlp", "FooAlpBarCat")]
        [InlineData("Abcdefghijefghij", "efghij")]
        [InlineData("Fog_Bar", "F__B")]
        [InlineData("FogBarBaz", "FZ")]
        [InlineData("_mybutton", "myB")]
        [InlineData("FogBarChangedEventArgs", "changedeventarrrgh")]
        public void TryMatchSingleWordPattern_NoMatch(string candidate, string pattern)
        {
            var match = TryMatchSingleWordPattern(candidate, pattern);
            Assert.Null(match);
        }

        private void AssertContainsType(PatternMatchKind type, IEnumerable<PatternMatch> results)
        {
            Assert.True(results.Any(r => r.Kind == type));
        }

        [Fact]
        public void MatchMultiWordPattern_ExactWithLowercase()
        {
            var match = TryMatchMultiWordPattern("[|AddMetadataReference|]", "addmetadatareference");

            AssertContainsType(PatternMatchKind.Exact, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleLowercasedSearchWord1()
        {
            var match = TryMatchMultiWordPattern("[|Add|]MetadataReference", "add");

            AssertContainsType(PatternMatchKind.Prefix, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleLowercasedSearchWord2()
        {
            var match = TryMatchMultiWordPattern("Add[|Metadata|]Reference", "metadata");

            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleUppercaseSearchWord1()
        {
            var match = TryMatchMultiWordPattern("[|Add|]MetadataReference", "Add");

            AssertContainsType(PatternMatchKind.Prefix, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleUppercaseSearchWord2()
        {
            var match = TryMatchMultiWordPattern("Add[|Metadata|]Reference", "Metadata");

            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleUppercaseSearchLetter1()
        {
            var match = TryMatchMultiWordPattern("[|A|]ddMetadataReference", "A");

            AssertContainsType(PatternMatchKind.Prefix, match);
        }

        [Fact]
        public void MatchMultiWordPattern_SingleUppercaseSearchLetter2()
        {
            var match = TryMatchMultiWordPattern("Add[|M|]etadataReference", "M");

            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_TwoLowercaseWords()
        {
            var match = TryMatchMultiWordPattern("[|Add|][|Metadata|]Reference", "add metadata");

            AssertContainsType(PatternMatchKind.Prefix, match);
            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_TwoUppercaseLettersSeparateWords()
        {
            var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadataReference", "A M");

            AssertContainsType(PatternMatchKind.Prefix, match);
            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_TwoUppercaseLettersOneWord()
        {
            var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadataReference", "AM");

            AssertContainsType(PatternMatchKind.CamelCase, match);
        }

        [Fact]
        public void MatchMultiWordPattern_Mixed1()
        {
            var match = TryMatchMultiWordPattern("Add[|Metadata|][|Ref|]erence", "ref Metadata");

            Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.Substring, PatternMatchKind.Substring }));
        }

        [Fact]
        public void MatchMultiWordPattern_Mixed2()
        {
            var match = TryMatchMultiWordPattern("Add[|M|]etadata[|Ref|]erence", "ref M");

            Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.Substring, PatternMatchKind.Substring }));
        }

        [Fact]
        public void MatchMultiWordPattern_MixedCamelCase()
        {
            var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadata[|Re|]ference", "AMRe");

            AssertContainsType(PatternMatchKind.CamelCase, match);
        }

        [Fact]
        public void MatchMultiWordPattern_BlankPattern()
        {
            Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", string.Empty));
        }

        [Fact]
        public void MatchMultiWordPattern_WhitespaceOnlyPattern()
        {
            Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", " "));
        }

        [Fact]
        public void MatchMultiWordPattern_EachWordSeparately1()
        {
            var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "add Meta");

            AssertContainsType(PatternMatchKind.Prefix, match);
            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_EachWordSeparately2()
        {
            var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "Add meta");

            AssertContainsType(PatternMatchKind.Prefix, match);
            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_EachWordSeparately3()
        {
            var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "Add Meta");

            AssertContainsType(PatternMatchKind.Prefix, match);
            AssertContainsType(PatternMatchKind.Substring, match);
        }

        [Fact]
        public void MatchMultiWordPattern_MixedCasing1()
        {
            Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", "mEta"));
        }

        [Fact]
        public void MatchMultiWordPattern_MixedCasing2()
        {
            Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", "Data"));
        }

        [Fact]
        public void MatchMultiWordPattern_AsteriskSplit()
        {
            var match = TryMatchMultiWordPattern("Get[|K|]ey[|W|]ord", "K*W");

            Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.Substring, PatternMatchKind.Substring }));
        }

        [WorkItem(544628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544628")]
        [Fact]
        public void MatchMultiWordPattern_LowercaseSubstring1()
        {
            Assert.Null(TryMatchMultiWordPattern("Operator", "a"));
        }

        [WorkItem(544628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544628")]
        [Fact]
        public void MatchMultiWordPattern_LowercaseSubstring2()
        {
            var match = TryMatchMultiWordPattern("Foo[|A|]ttribute", "a");
            AssertContainsType(PatternMatchKind.Substring, match);
            Assert.False(match.First().IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_CultureAwareSingleWordPreferCaseSensitiveExactInsensitive()
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            var turkish = CultureInfo.GetCultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = turkish;

            try
            {
                var match = TryMatchSingleWordPattern("[|ioo|]", "\u0130oo"); // u0130 = Capital I with dot

                Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
                Assert.False(match.Value.IsCaseSensitive);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        private static IList<string> PartListToSubstrings(string identifier, StringBreaks parts)
        {
            var result = new List<string>();
            for (int i = 0, n = parts.GetCount(); i < n; i++)
            {
                var span = parts[i];
                result.Add(identifier.Substring(span.Start, span.Length));
            }

            return result;
        }

        private static IList<string> BreakIntoCharacterParts(string identifier)
            => PartListToSubstrings(identifier, StringBreaker.BreakIntoCharacterParts(identifier));

        private static IList<string> BreakIntoWordParts(string identifier)
            => PartListToSubstrings(identifier, StringBreaker.BreakIntoWordParts(identifier));

        private static PatternMatch? TryMatchSingleWordPattern(string candidate, string pattern)
        {
            MarkupTestFile.GetSpans(candidate, out candidate, out ImmutableArray<TextSpan> spans);

            var match = new PatternMatcher(pattern).MatchSingleWordPattern_ForTestingOnly(candidate);

            if (match == null)
            {
                Assert.True(spans.Length == 0);
            }
            else
            {
                Assert.Equal<TextSpan>(match.Value.MatchedSpans, spans);
            }

            return match;
        }

        private static IEnumerable<PatternMatch> TryMatchMultiWordPattern(string candidate, string pattern)
        {
            MarkupTestFile.GetSpans(candidate, out candidate, out ImmutableArray<TextSpan> expectedSpans);

            var matches = new PatternMatcher(pattern).GetMatches(candidate, includeMatchSpans: true);

            if (matches.IsDefaultOrEmpty)
            {
                Assert.True(expectedSpans.Length == 0);
                return null;
            }
            else
            {
                var actualSpans = matches.SelectMany(m => m.MatchedSpans).OrderBy(s => s.Start).ToList();
                Assert.Equal(expectedSpans, actualSpans);
                return matches;
            }
        }
    }
}
