// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

public sealed class PatternMatcherTests
{
    [Fact]
    public void BreakIntoCharacterParts_EmptyIdentifier()
        => VerifyBreakIntoCharacterParts(string.Empty, []);

    [Fact]
    public void BreakIntoCharacterParts_SimpleIdentifier()
        => VerifyBreakIntoCharacterParts("goo", "goo");

    [Fact]
    public void BreakIntoCharacterParts_PrefixUnderscoredIdentifier()
        => VerifyBreakIntoCharacterParts("_goo", "_", "goo");

    [Fact]
    public void BreakIntoCharacterParts_UnderscoredIdentifier()
        => VerifyBreakIntoCharacterParts("g_oo", "g", "_", "oo");

    [Fact]
    public void BreakIntoCharacterParts_PostfixUnderscoredIdentifier()
        => VerifyBreakIntoCharacterParts("goo_", "goo", "_");

    [Fact]
    public void BreakIntoCharacterParts_PrefixUnderscoredIdentifierWithCapital()
        => VerifyBreakIntoCharacterParts("_Goo", "_", "Goo");

    [Fact]
    public void BreakIntoCharacterParts_MUnderscorePrefixed()
        => VerifyBreakIntoCharacterParts("m_goo", "m", "_", "goo");

    [Fact]
    public void BreakIntoCharacterParts_CamelCaseIdentifier()
        => VerifyBreakIntoCharacterParts("FogBar", "Fog", "Bar");

    [Fact]
    public void BreakIntoCharacterParts_MixedCaseIdentifier()
        => VerifyBreakIntoCharacterParts("fogBar", "fog", "Bar");

    [Fact]
    public void BreakIntoCharacterParts_TwoCharacterCapitalIdentifier()
        => VerifyBreakIntoCharacterParts("UIElement", "U", "I", "Element");

    [Fact]
    public void BreakIntoCharacterParts_NumberSuffixedIdentifier()
        => VerifyBreakIntoCharacterParts("Goo42", "Goo", "42");

    [Fact]
    public void BreakIntoCharacterParts_NumberContainingIdentifier()
        => VerifyBreakIntoCharacterParts("Fog42Bar", "Fog", "42", "Bar");

    [Fact]
    public void BreakIntoCharacterParts_NumberPrefixedIdentifier()
    {
        // 42Bar is not a valid identifier in either C# or VB, but it is entirely conceivable the user might be
        // typing it trying to do a substring match
        VerifyBreakIntoCharacterParts("42Bar", "42", "Bar");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544296")]
    public void BreakIntoWordParts_VerbatimIdentifier()
        => VerifyBreakIntoWordParts("@int:", "int");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537875")]
    public void BreakIntoWordParts_AllCapsConstant()
        => VerifyBreakIntoWordParts("C_STYLE_CONSTANT", "C", "_", "STYLE", "_", "CONSTANT");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
    public void BreakIntoWordParts_SingleLetterPrefix1()
        => VerifyBreakIntoWordParts("UInteger", "U", "Integer");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
    public void BreakIntoWordParts_SingleLetterPrefix2()
        => VerifyBreakIntoWordParts("IDisposable", "I", "Disposable");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
    public void BreakIntoWordParts_TwoCharacterCapitalIdentifier()
        => VerifyBreakIntoWordParts("UIElement", "UI", "Element");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
    public void BreakIntoWordParts_XDocument()
        => VerifyBreakIntoWordParts("XDocument", "X", "Document");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540087")]
    public void BreakIntoWordParts_XMLDocument1()
        => VerifyBreakIntoWordParts("XMLDocument", "XML", "Document");

    [Fact]
    public void BreakIntoWordParts_XMLDocument2()
        => VerifyBreakIntoWordParts("XmlDocument", "Xml", "Document");

    [Fact]
    public void BreakIntoWordParts_TwoUppercaseCharacters()
        => VerifyBreakIntoWordParts("SimpleUIElement", "Simple", "UI", "Element");

    private static void VerifyBreakIntoWordParts(string original, params string[] parts)
        => Roslyn.Test.Utilities.AssertEx.Equal(parts, BreakIntoWordParts(original));

    private static void VerifyBreakIntoCharacterParts(string original, params string[] parts)
        => Roslyn.Test.Utilities.AssertEx.Equal(parts, BreakIntoCharacterParts(original));

    private const bool CaseSensitive = true;
    private const bool CaseInsensitive = !CaseSensitive;

    [Theory]
    [InlineData("[|Goo|]", "Goo", PatternMatchKind.Exact, CaseSensitive)]
    [InlineData("[|goo|]", "Goo", PatternMatchKind.Exact, CaseInsensitive)]
    [InlineData("[|Goo|]", "goo", PatternMatchKind.Exact, CaseInsensitive)]

    [InlineData("[|Fo|]o", "Fo", PatternMatchKind.Prefix, CaseSensitive)]
    [InlineData("[|Fog|]Bar", "Fog", PatternMatchKind.Prefix, CaseSensitive)]

    [InlineData("[|Fo|]o", "fo", PatternMatchKind.Prefix, CaseInsensitive)]
    [InlineData("[|Fog|]Bar", "fog", PatternMatchKind.Prefix, CaseInsensitive)]
    [InlineData("[|fog|]BarGoo", "Fog", PatternMatchKind.Prefix, CaseInsensitive)]

    [InlineData("[|system.ref|]lection", "system.ref", PatternMatchKind.Prefix, CaseSensitive)]

    [InlineData("Fog[|B|]ar", "b", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]

    [InlineData("_[|my|]Button", "my", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("my[|_b|]utton", "_b", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("_[|my|]button", "my", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("_my[|_b|]utton", "_b", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("_[|myb|]utton", "myb", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("_[|myB|]utton", "myB", PatternMatchKind.NonLowercaseSubstring, CaseSensitive)]

    [InlineData("my[|_B|]utton", "_b", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]
    [InlineData("_my[|_B|]utton", "_b", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]
    [InlineData("_[|myB|]utton", "myb", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]

    [InlineData("[|AbCd|]xxx[|Ef|]Cd[|Gh|]", "AbCdEfGh", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseSensitive)]

    [InlineData("A[|BCD|]EFGH", "bcd", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]
    [InlineData("FogBar[|ChangedEventArgs|]", "changedeventargs", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]
    [InlineData("Abcdefghij[|EfgHij|]", "efghij", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]

    [InlineData("[|F|]og[|B|]ar", "FB", PatternMatchKind.CamelCaseExact, CaseSensitive)]
    [InlineData("[|Fo|]g[|B|]ar", "FoB", PatternMatchKind.CamelCaseExact, CaseSensitive)]
    [InlineData("[|_f|]og[|B|]ar", "_fB", PatternMatchKind.CamelCaseExact, CaseSensitive)]
    [InlineData("[|F|]og[|_B|]ar", "F_B", PatternMatchKind.CamelCaseExact, CaseSensitive)]
    [InlineData("[|F|]og[|B|]ar", "fB", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("Baz[|F|]ogBar[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCaseNonContiguousSubstring, CaseSensitive)]
    [InlineData("[|F|]og[|B|]arBaz", "FB", PatternMatchKind.CamelCasePrefix, CaseSensitive)]
    [InlineData("[|F|]og_[|B|]ar", "FB", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseSensitive)]
    [InlineData("[|F|]ooFlob[|B|]az", "FB", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseSensitive)]
    [InlineData("Bar[|F|]oo[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCaseSubstring, CaseSensitive)]
    [InlineData("BazBar[|F|]oo[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCaseSubstring, CaseSensitive)]
    [InlineData("[|Fo|]oBarry[|Bas|]il", "FoBas", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseSensitive)]
    [InlineData("[|F|]ogBar[|F|]oo[|F|]oo", "FFF", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseSensitive)]

    [InlineData("[|F|]og[|_B|]ar", "F_b", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("[|_F|]og[|B|]ar", "_fB", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("[|F|]og[|_B|]ar", "f_B", PatternMatchKind.CamelCaseExact, CaseInsensitive)]

    [InlineData("[|Si|]mple[|UI|]Element", "SiUI", PatternMatchKind.CamelCaseExact, CaseSensitive)]

    [InlineData("_[|co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCaseNonContiguousSubstring, CaseInsensitive)]
    [InlineData("Code[|Fi|]xObject[|Pro|]vider", "fipro", PatternMatchKind.CamelCaseNonContiguousSubstring, CaseInsensitive)]
    [InlineData("[|Co|]de[|Fi|]x[|Pro|]vider", "cofipro", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("Code[|Fi|]x[|Pro|]vider", "fipro", PatternMatchKind.CamelCaseSubstring, CaseInsensitive)]
    [InlineData("[|Co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseInsensitive)]
    [InlineData("[|co|]deFix[|Pro|]vider", "copro", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseInsensitive)]
    [InlineData("[|Co|]deFix_[|Pro|]vider", "copro", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseInsensitive)]
    [InlineData("[|C|]ore[|Ofi|]lac[|Pro|]fessional", "cofipro", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("[|C|]lear[|Ofi|]lac[|Pro|]fessional", "cofipro", PatternMatchKind.CamelCaseExact, CaseInsensitive)]
    [InlineData("[|CO|]DE_FIX_[|PRO|]VIDER", "copro", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseInsensitive)]

    [InlineData("my[|_b|]utton", "_B", PatternMatchKind.CamelCaseSubstring, CaseInsensitive)]
    [InlineData("[|_|]my_[|b|]utton", "_B", PatternMatchKind.CamelCaseNonContiguousPrefix, CaseInsensitive)]
    [InlineData("Com[|bin|]e", "bin", PatternMatchKind.LowercaseSubstring, CaseSensitive)]
    [InlineData("Combine[|Bin|]ary", "bin", PatternMatchKind.StartOfWordSubstring, CaseInsensitive)]

    [InlineData("_ABC_[|Abc|]_", "Abc", PatternMatchKind.StartOfWordSubstring, CaseSensitive)]
    [InlineData("[|C|]reate[|R|]ange", "CR", PatternMatchKind.CamelCaseExact, CaseSensitive)]

    [WorkItem("https://github.com/dotnet/roslyn/issues/51029")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17275")]
    internal void TestNonFuzzyMatch(
        string candidate, string pattern, PatternMatchKind matchKind, bool isCaseSensitive)
    {
        var match = TestNonFuzzyMatchCore(candidate, pattern);
        Assert.NotNull(match);

        Assert.Equal(matchKind, match.Value.Kind);
        Assert.Equal(isCaseSensitive, match.Value.IsCaseSensitive);
    }

    [Theory]
    [InlineData("CodeFixObjectProvider", "ficopro")]
    [InlineData("FogBar", "FBB")]
    [InlineData("FogBarBaz", "ZZ")]
    [InlineData("FogBar", "GoooB")]
    [InlineData("GooActBarCatAlp", "GooAlpBarCat")]
    // We don't want a lowercase pattern to match *across* a word boundary.
    [InlineData("AbcdefGhijklmnop", "efghij")]
    [InlineData("Fog_Bar", "F__B")]
    [InlineData("FogBarBaz", "FZ")]
    [InlineData("_mybutton", "myB")]
    [InlineData("FogBarChangedEventArgs", "changedeventarrrgh")]
    [InlineData("runtime.native.system", "system.reflection")]
    public void TestNonFuzzyMatch_NoMatch(string candidate, string pattern)
    {
        var match = TestNonFuzzyMatchCore(candidate, pattern);
        Assert.Null(match);
    }

    private static void AssertContainsType(PatternMatchKind type, IEnumerable<PatternMatch> results)
        => Assert.True(results.Any(r => r.Kind == type));

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

        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
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

        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
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

        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_TwoLowercaseWords()
    {
        var match = TryMatchMultiWordPattern("[|Add|][|Metadata|]Reference", "add metadata");

        AssertContainsType(PatternMatchKind.Prefix, match);
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_TwoUppercaseLettersSeparateWords()
    {
        var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadataReference", "A M");

        AssertContainsType(PatternMatchKind.Prefix, match);
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_TwoUppercaseLettersOneWord()
    {
        var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadataReference", "AM");

        AssertContainsType(PatternMatchKind.CamelCasePrefix, match);
    }

    [Fact]
    public void MatchMultiWordPattern_Mixed1()
    {
        var match = TryMatchMultiWordPattern("Add[|Metadata|][|Ref|]erence", "ref Metadata");

        Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.StartOfWordSubstring, PatternMatchKind.StartOfWordSubstring }));
    }

    [Fact]
    public void MatchMultiWordPattern_Mixed2()
    {
        var match = TryMatchMultiWordPattern("Add[|M|]etadata[|Ref|]erence", "ref M");

        Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.StartOfWordSubstring, PatternMatchKind.StartOfWordSubstring }));
    }

    [Fact]
    public void MatchMultiWordPattern_MixedCamelCase()
    {
        var match = TryMatchMultiWordPattern("[|A|]dd[|M|]etadata[|Re|]ference", "AMRe");

        AssertContainsType(PatternMatchKind.CamelCaseExact, match);
    }

    [Fact]
    public void MatchMultiWordPattern_BlankPattern()
        => Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", string.Empty));

    [Fact]
    public void MatchMultiWordPattern_WhitespaceOnlyPattern()
        => Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", " "));

    [Fact]
    public void MatchMultiWordPattern_EachWordSeparately1()
    {
        var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "add Meta");

        AssertContainsType(PatternMatchKind.Prefix, match);
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_EachWordSeparately2()
    {
        var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "Add meta");

        AssertContainsType(PatternMatchKind.Prefix, match);
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_EachWordSeparately3()
    {
        var match = TryMatchMultiWordPattern("[|Add|][|Meta|]dataReference", "Add Meta");

        AssertContainsType(PatternMatchKind.Prefix, match);
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
    }

    [Fact]
    public void MatchMultiWordPattern_MixedCasing1()
        => Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", "mEta"));

    [Fact]
    public void MatchMultiWordPattern_MixedCasing2()
        => Assert.Null(TryMatchMultiWordPattern("AddMetadataReference", "Data"));

    [Fact]
    public void MatchMultiWordPattern_AsteriskSplit()
    {
        var match = TryMatchMultiWordPattern("Get[|K|]ey[|W|]ord", "K*W");

        Assert.True(match.Select(m => m.Kind).SequenceEqual(new[] { PatternMatchKind.StartOfWordSubstring, PatternMatchKind.StartOfWordSubstring }));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544628")]
    public void MatchMultiWordPattern_LowercaseSubstring1()
        => Assert.Null(TryMatchMultiWordPattern("Operator", "a"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544628")]
    public void MatchMultiWordPattern_LowercaseSubstring2()
    {
        var match = TryMatchMultiWordPattern("Goo[|A|]ttribute", "a");
        AssertContainsType(PatternMatchKind.StartOfWordSubstring, match);
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
            var match = TestNonFuzzyMatchCore("[|ioo|]", "\u0130oo"); // u0130 = Capital I with dot

            Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
            Assert.False(match.Value.IsCaseSensitive);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void TestCachingOfPriorResult()
    {
        using var matcher = PatternMatcher.CreatePatternMatcher("Goo", includeMatchedSpans: true, allowFuzzyMatching: true);
        matcher.Matches("Go");

        // Ensure that the above call ended up caching the result.
        Assert.True(((PatternMatcher.SimplePatternMatcher)matcher).GetTestAccessor().LastCacheResultIs(areSimilar: true, candidateText: "Go"));

        matcher.Matches("DefNotAMatch");
        Assert.True(((PatternMatcher.SimplePatternMatcher)matcher).GetTestAccessor().LastCacheResultIs(areSimilar: false, candidateText: "DefNotAMatch"));
    }

    private static ImmutableArray<string> PartListToSubstrings(string identifier, in TemporaryArray<TextSpan> parts)
    {
        using var result = TemporaryArray<string>.Empty;
        foreach (var span in parts)
            result.Add(identifier.Substring(span.Start, span.Length));

        return result.ToImmutableAndClear();
    }

    private static ImmutableArray<string> BreakIntoCharacterParts(string identifier)
    {
        using var parts = TemporaryArray<TextSpan>.Empty;
        StringBreaker.AddCharacterParts(identifier, ref parts.AsRef());
        return PartListToSubstrings(identifier, parts);
    }

    private static ImmutableArray<string> BreakIntoWordParts(string identifier)
    {
        using var parts = TemporaryArray<TextSpan>.Empty;
        StringBreaker.AddWordParts(identifier, ref parts.AsRef());
        return PartListToSubstrings(identifier, parts);
    }

    private static PatternMatch? TestNonFuzzyMatchCore(string candidate, string pattern)
    {
        MarkupTestFile.GetSpans(candidate, out candidate, out var spans);

        var match = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: true, allowFuzzyMatching: false)
            .GetFirstMatch(candidate);

        if (match == null)
        {
            Assert.Empty(spans);
        }
        else
        {
            Assert.Equal<TextSpan>(match.Value.MatchedSpans, spans);
        }

        return match;
    }

    private static IEnumerable<PatternMatch> TryMatchMultiWordPattern(string candidate, string pattern)
    {
        MarkupTestFile.GetSpans(candidate, out candidate, out var expectedSpans);

        using var matches = TemporaryArray<PatternMatch>.Empty;
        PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: true).AddMatches(candidate, ref matches.AsRef());

        if (matches.Count == 0)
        {
            Assert.Empty(expectedSpans);
            return null;
        }
        else
        {
            var flattened = new List<TextSpan>();
            foreach (var match in matches)
                flattened.AddRange(match.MatchedSpans);

            var actualSpans = flattened.OrderBy(s => s.Start).ToList();
            Assert.Equal(expectedSpans, actualSpans);
            return matches.ToImmutableAndClear();
        }
    }
}
