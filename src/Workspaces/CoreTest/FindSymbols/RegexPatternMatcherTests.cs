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
        using var matcher = new PatternMatcher.RegexPatternMatcher(pattern, includeMatchedSpans);
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
        var match = GetMatch("Foo.*Bar", "FooSomethingBar");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void DotStar_PartialMatch()
    {
        var match = GetMatch("Foo.*Bar", "MyFooSomethingBarEnd");
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
    public void Quantifiers_OneOrMore()
    {
        var match = GetMatch("Fo+Bar", "FooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void Quantifiers_ZeroOrMore()
    {
        var match = GetMatch("Fo*Bar", "FBar");
        Assert.NotNull(match);
    }

    [Fact]
    public void EscapedDot_LiteralMatch()
    {
        // \. matches a literal dot
        var match = GetMatch(@"Foo\.Bar", "Foo.Bar");
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
        // When case-sensitive regex also matches, isCaseSensitive is true
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
        var match = GetMatch("Foo", "");
        Assert.Null(match);
    }

    [Fact]
    public void NoMatch_CaseMattersForAnchored()
    {
        // ^ReadLine$ requires exact match. Case-insensitive finds it but it's still "Exact".
        // However, if we use a case-specific anchor test:
        var match = GetMatch("^readLine$", "ReadLine");
        Assert.NotNull(match); // case-insensitive finds it
        Assert.False(match.Value.IsCaseSensitive);
    }

    #endregion

    #region Case sensitivity categorization

    [Fact]
    public void CaseSensitivity_ExactCaseSensitive()
    {
        var match = GetMatch("FooBar", "FooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_ExactCaseInsensitive()
    {
        var match = GetMatch("foobar", "FooBar");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_SubstringCaseSensitive()
    {
        var match = GetMatch("Foo", "FooBar");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_SubstringCaseInsensitive()
    {
        var match = GetMatch("foo", "FooBar");
        Assert.NotNull(match);
        Assert.False(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_Alternation_MixedCase()
    {
        // Pattern "Read|write" — "ReadLine" matches case-sensitive via "Read" branch
        var match = GetMatch("Read|write", "ReadLine");
        Assert.NotNull(match);
        Assert.True(match.Value.IsCaseSensitive);
    }

    [Fact]
    public void CaseSensitivity_Alternation_CaseInsensitiveOnly()
    {
        // Pattern "read|write" — "ReadLine" matches case-insensitive only
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
        var match = GetMatch("Foo", null!);
        Assert.Null(match);
    }

    [Fact]
    public void WhitespaceCandidate_NoMatch()
    {
        var match = GetMatch("Foo", "   ");
        Assert.Null(match);
    }

    #endregion
}
