// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PatternMatching;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FindSymbols;

public class RegexPatternMatcherTests
{
    private static PatternMatch? GetMatch(string pattern, string candidate, bool includeMatchedSpans = false)
    {
        using var matcher = PatternMatcher.CreateNameMatcher(pattern, isRegex: true, includeMatchedSpans);
        if (matcher is null)
            return null;

        using var matches = TemporaryArray<PatternMatch>.Empty;
        if (matcher.AddMatches(candidate, ref matches.AsRef()))
            return matches[0];

        return null;
    }

    #region Positive — matching

    [Fact]
    public void SimpleLiteral_ExactMatch()
    {
        var match = GetMatch("ReadLine", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void SimpleLiteral_CaseInsensitiveMatch()
    {
        var match = GetMatch("readline", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void Alternation_FirstBranch()
    {
        var match = GetMatch("(Read|Write)Line", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void Alternation_SecondBranch()
    {
        var match = GetMatch("(Read|Write)Line", "WriteLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void SubstringMatch()
    {
        var match = GetMatch("Read", "StreamReader");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.NonLowercaseSubstring, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void DotStar_SubstringMatch()
    {
        var match = GetMatch("Goo.*Bar", "GooSomethingBar");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void DotStar_PartialMatch()
    {
        var match = GetMatch("Goo.*Bar", "MyGooSomethingBarEnd");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.NonLowercaseSubstring, match.Value.Kind);
    }

    [Fact]
    public void AnchoredExact()
    {
        var match = GetMatch("^ReadLine$", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
    }

    [Fact]
    public void CharacterClass()
    {
        var match = GetMatch("[A-Z]ead", "Read");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void MatchedSpans_Reported()
    {
        var match = GetMatch("Line", "ReadLine", includeMatchedSpans: true);
        Assert.NotNull(match);
        Assert.Single(match.Value.MatchedSpans);
        Assert.Equal(4, match.Value.MatchedSpans[0].Start);
        Assert.Equal(4, match.Value.MatchedSpans[0].Length);
    }

    [Fact]
    public void MatchedSpans_RegexSubstring()
    {
        var match = GetMatch("Goo.*Bar", "MyGooSomethingBarEnd", includeMatchedSpans: true);
        Assert.NotNull(match);
        Assert.Single(match.Value.MatchedSpans);
        // "GooSomethingBar" starts at index 2, length 15
        Assert.Equal(2, match.Value.MatchedSpans[0].Start);
        Assert.Equal(15, match.Value.MatchedSpans[0].Length);
    }

    [Fact]
    public void MatchedSpans_ExactMatch_CoversFullString()
    {
        var match = GetMatch("GooBar", "GooBar", includeMatchedSpans: true);
        Assert.NotNull(match);
        Assert.Single(match.Value.MatchedSpans);
        Assert.Equal(0, match.Value.MatchedSpans[0].Start);
        Assert.Equal(6, match.Value.MatchedSpans[0].Length);
    }

    [Fact]
    public void MatchedSpans_NotReported_WhenNotRequested()
    {
        var match = GetMatch("Line", "ReadLine", includeMatchedSpans: false);
        Assert.NotNull(match);
        Assert.True(match.Value.MatchedSpans.IsDefaultOrEmpty);
    }

    [Fact]
    public void Quantifiers_OneOrMore()
    {
        var match = GetMatch("Go+Bar", "GooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void Quantifiers_ZeroOrMore()
    {
        var match = GetMatch("Go*Bar", "GBar");
        Assert.NotNull(match);
    }

    [Fact]
    public void EscapedDot_LiteralMatch()
    {
        var match = GetMatch(@"Goo\.Bar", "Goo.Bar");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
    }

    [Fact]
    public void CaseInsensitive_SubstringMatch()
    {
        var match = GetMatch("read", "StreamReader");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.NonLowercaseSubstring, match.Value.Kind);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitive_MatchHigherPriority()
    {
        var match = GetMatch("Read", "ReadLine");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    #endregion

    #region Negative — no match

    [Fact]
    public void NoMatch_DifferentText()
    {
        var match = GetMatch("ReadLine", "WriteBuffer");
        Assert.Null(match);
    }

    [Fact]
    public void NoMatch_AnchoredPrefix_NotFull()
    {
        var match = GetMatch("^ReadLine$", "MyReadLine");
        Assert.Null(match);
    }

    [Fact]
    public void NoMatch_AlternationMiss()
    {
        var match = GetMatch("(Read|Write)Line", "StreamBuffer");
        Assert.Null(match);
    }

    [Fact]
    public void NoMatch_EmptyCandidate()
    {
        var match = GetMatch("Goo", "");
        Assert.Null(match);
    }

    [Fact]
    public void NoMatch_CaseMattersForAnchored()
    {
        var match = GetMatch("^readLine$", "ReadLine");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    #endregion

    #region Case sensitivity categorization

    [Fact]
    public void CaseSensitivity_ExactCaseSensitive()
    {
        var match = GetMatch("GooBar", "GooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_ExactCaseInsensitive()
    {
        var match = GetMatch("goobar", "GooBar");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_SubstringCaseSensitive()
    {
        var match = GetMatch("Goo", "GooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_SubstringCaseInsensitive()
    {
        var match = GetMatch("goo", "GooBar");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_Alternation_MixedCase()
    {
        var match = GetMatch("Read|write", "ReadLine");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_Alternation_CaseInsensitiveOnly()
    {
        var match = GetMatch("read|write", "ReadLine");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void SingleCharPattern()
    {
        var match = GetMatch("R", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.NonLowercaseSubstring, match.Value.Kind);
    }

    [Fact]
    public void WildcardOnly_MatchesAnything()
    {
        var match = GetMatch(".*", "Anything");
        Assert.NotNull(match);
    }

    [Fact]
    public void NullCandidate_NoMatch()
    {
        var match = GetMatch("Goo", null!);
        Assert.Null(match);
    }

    [Fact]
    public void WhitespaceCandidate_NoMatch()
    {
        var match = GetMatch("Goo", "   ");
        Assert.Null(match);
    }

    [Fact]
    public void InvalidRegex_ReturnsNull()
    {
        var match = GetMatch("(unclosed", "Anything");
        Assert.Null(match);
    }

    [Fact]
    public void InvalidRegex_BadEscape_ReturnsNull()
    {
        var match = GetMatch(@"\", "Anything");
        Assert.Null(match);
    }

    #endregion

    #region Whitespace handling

    [Fact]
    public void WhitespaceInPattern_IsStripped()
    {
        var match = GetMatch("( Read | Write ) Line", "ReadLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void WhitespaceInPattern_IsStripped_SecondBranch()
    {
        var match = GetMatch("( Read | Write ) Line", "WriteLine");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void WhitespaceInPattern_SimpleLiteral()
    {
        var match = GetMatch("Goo Bar", "GooBar");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
    }

    [Fact]
    public void WhitespaceInPattern_DoesNotMatchSpacedCandidate()
    {
        var match = GetMatch("Goo Bar", "Goo Bar");
        Assert.Null(match);
    }

    #endregion
}
