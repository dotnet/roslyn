// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
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

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveExact()
        {
            var match = TryMatchSingleWordPattern("[|Foo|]", "Foo");

            Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_SingleWordPreferCaseSensitiveExactInsensitive()
        {
            var match = TryMatchSingleWordPattern("[|foo|]", "Foo");

            Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitivePrefix()
        {
            var match = TryMatchSingleWordPattern("[|Fo|]o", "Fo");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitivePrefixCaseInsensitive()
        {
            var match = TryMatchSingleWordPattern("[|Fo|]o", "fo");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveCamelCaseMatchSimple()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|B|]ar", "FB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
            Assert.InRange((int)match.Value.CamelCaseWeight, 1, int.MaxValue);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveCamelCaseMatchPartialPattern()
        {
            var match = TryMatchSingleWordPattern("[|Fo|]g[|B|]ar", "FoB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveCamelCaseMatchToLongPattern1()
        {
            var match = TryMatchSingleWordPattern("FogBar", "FBB");

            Assert.Null(match);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveCamelCaseMatchToLongPattern2()
        {
            var match = TryMatchSingleWordPattern("FogBar", "FoooB");

            Assert.Null(match);
        }

        [Fact]
        public void TryMatchSingleWordPattern_CamelCaseMatchPartiallyUnmatched()
        {
            var match = TryMatchSingleWordPattern("FogBarBaz", "FZ");

            Assert.Null(match);
        }

        [Fact]
        public void TryMatchSingleWordPattern_CamelCaseMatchCompletelyUnmatched()
        {
            var match = TryMatchSingleWordPattern("FogBarBaz", "ZZ");

            Assert.Null(match);
        }

        [Fact]
        [WorkItem(544975, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544975")]
        public void TryMatchSingleWordPattern_TwoUppercaseCharacters()
        {
            var match = TryMatchSingleWordPattern("[|Si|]mple[|UI|]Element", "SiUI");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.True(match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveLowercasePattern()
        {
            var match = TryMatchSingleWordPattern("Fog[|B|]ar", "b");

            Assert.Equal(PatternMatchKind.Substring, match.Value.Kind);
            Assert.False(match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveLowercasePattern2()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|B|]ar", "fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveTryUnderscoredName()
        {
            var match = TryMatchSingleWordPattern("[|_f|]og[|B|]ar", "_fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        public void TryMatchSingleWordPattern_PreferCaseSensitiveTryUnderscoredName2()
        {
            var match = TryMatchSingleWordPattern("_[|f|]og[|B|]ar", "fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveTryUnderscoredNameInsensitive()
        {
            var match = TryMatchSingleWordPattern("[|_F|]og[|B|]ar", "_fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveMiddleUnderscore()
        {
            var match = TryMatchSingleWordPattern("[|F|]og_[|B|]ar", "FB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveMiddleUnderscore2()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|_B|]ar", "F_B");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveMiddleUnderscore3()
        {
            var match = TryMatchSingleWordPattern("Fog_Bar", "F__B");

            Assert.Null(match);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveMiddleUnderscore4()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|_B|]ar", "f_B");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveMiddleUnderscore5()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|_B|]ar", "F_b");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
       }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveRelativeWeights1()
        {
            var match1 = TryMatchSingleWordPattern("[|F|]og[|B|]arBaz", "FB");
            var match2 = TryMatchSingleWordPattern("[|F|]ooFlob[|B|]az", "FB");

            // We should prefer something that starts at the beginning if possible
            Assert.InRange((int)match1.Value.CamelCaseWeight, (int)match2.Value.CamelCaseWeight + 1, int.MaxValue);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveRelativeWeights2()
        {
            var match1 = TryMatchSingleWordPattern("BazBar[|F|]oo[|F|]oo[|F|]oo", "FFF");
            var match2 = TryMatchSingleWordPattern("Baz[|F|]ogBar[|F|]oo[|F|]oo", "FFF");

            // Contiguous things should also be preferred
            Assert.InRange((int)match1.Value.CamelCaseWeight, (int)match2.Value.CamelCaseWeight + 1, int.MaxValue);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveRelativeWeights3()
        {
            var match1 = TryMatchSingleWordPattern("[|F|]ogBar[|F|]oo[|F|]oo", "FFF");
            var match2 = TryMatchSingleWordPattern("Bar[|F|]oo[|F|]oo[|F|]oo", "FFF");

            // The weight of being first should be greater than the weight of being contiguous
            Assert.InRange((int)match1.Value.CamelCaseWeight, (int)match2.Value.CamelCaseWeight + 1, int.MaxValue);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveBasicEquals()
        {
            var match = TryMatchSingleWordPattern("[|Foo|]", "foo");

            Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveBasicEquals2()
        {
            var match = TryMatchSingleWordPattern("[|Foo|]", "Foo");

            // Since it's actually case sensitive, we'll report it as such even though we didn't prefer it
            Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveBasicPrefix()
        {
            var match = TryMatchSingleWordPattern("[|Fog|]Bar", "fog");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveBasicPrefix2()
        {
            var match = TryMatchSingleWordPattern("[|Fog|]Bar", "Fog");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveCamelCase1()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|B|]ar", "FB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveCamelCase2()
        {
            var match = TryMatchSingleWordPattern("[|F|]og[|B|]ar", "fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveCamelCase3()
        {
            var match = TryMatchSingleWordPattern("[|f|]og[|B|]ar", "fB");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseSensitiveWhenPrefix()
        {
            var match = TryMatchSingleWordPattern("[|fog|]BarFoo", "Fog");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.False(match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_PreferCaseInsensitiveWhenPrefix()
        {
            var match = TryMatchSingleWordPattern("[|fog|]BarFoo", "Fog");

            Assert.Equal(PatternMatchKind.Prefix, match.Value.Kind);
            Assert.Equal(false, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_CamelCase1()
        {
            var match = TryMatchSingleWordPattern("[|Fo|]oBarry[|Bas|]il", "FoBas");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
        }

        [Fact]
        public void TryMatchSingleWordPattern_CamelCase2()
        {
            Assert.Null(TryMatchSingleWordPattern("FooActBarCatAlp", "FooAlpBarCat"));
        }

        [Fact]
        public void TryMatchSingleWordPattern_CamelCase3()
        {
            var match = TryMatchSingleWordPattern("[|AbCd|]xxx[|Ef|]Cd[|Gh|]", "AbCdEfGh");

            Assert.Equal(PatternMatchKind.CamelCase, match.Value.Kind);
            Assert.Equal(true, match.Value.IsCaseSensitive);
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

        [Fact]
        public void MatchAllLowerPattern1()
        {
            Assert.NotNull(TryMatchSingleWordPattern("FogBar[|ChangedEventArgs|]", "changedeventargs"));
        }

        [Fact]
        public void MatchAllLowerPattern2()
        {
            Assert.Null(TryMatchSingleWordPattern("FogBarChangedEventArgs", "changedeventarrrgh"));
        }

        [Fact]
        public void MatchAllLowerPattern3()
        {
            Assert.NotNull(TryMatchSingleWordPattern("A[|BCD|]EFGH", "bcd"));
        }

        [Fact]
        public void MatchAllLowerPattern4()
        {
            Assert.Null(TryMatchSingleWordPattern("AbcdefghijEfgHij", "efghij"));
        }

        private static IList<string> PartListToSubstrings(string identifier, StringBreaks parts)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < parts.Count; i++)
            {
                var span = parts[i];
                result.Add(identifier.Substring(span.Start, span.Length));
            }

            return result;
        }

        private static IList<string> BreakIntoCharacterParts(string identifier)
        {
            return PartListToSubstrings(identifier, StringBreaker.BreakIntoCharacterParts(identifier));
        }

        private static IList<string> BreakIntoWordParts(string identifier)
        {
            return PartListToSubstrings(identifier, StringBreaker.BreakIntoWordParts(identifier));
        }

        private static PatternMatch? TryMatchSingleWordPattern(string candidate, string pattern)
        {
            IList<TextSpan> spans;
            MarkupTestFile.GetSpans(candidate, out candidate, out spans);

            var match = new PatternMatcher(pattern).MatchSingleWordPattern_ForTestingOnly(candidate);

            if (match == null)
            {
                Assert.True(spans == null || spans.Count == 0);
            }
            else
            {
                Assert.Equal(match.Value.MatchedSpans, spans);
            }

            return match;
        }

        private static IEnumerable<PatternMatch> TryMatchMultiWordPattern(string candidate, string pattern)
        {
            IList<TextSpan> expectedSpans;
            MarkupTestFile.GetSpans(candidate, out candidate, out expectedSpans);

            var matches = new PatternMatcher(pattern).GetMatches(candidate, includeMatchSpans: true);

            if (matches == null)
            {
                Assert.True(expectedSpans == null || expectedSpans.Count == 0);
            }
            else
            {
                var actualSpans = matches.SelectMany(m => m.MatchedSpans).OrderBy(s => s.Start).ToList();
                Assert.Equal(expectedSpans, actualSpans);
            }

            return matches;
        }
    }
}
