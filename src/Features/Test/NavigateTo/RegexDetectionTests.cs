// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.NavigateTo;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.NavigateTo;

public sealed class RegexDetectionTests
{
    private static (string? container, string name) SplitOnContainerDot(string pattern)
    {
        var sequence = VirtualCharSequence.Create(0, pattern);
        var tree = RegexParser.TryParse(sequence, RegexOptions.None);
        if (tree is not { Diagnostics: [] })
            return (null, pattern);

        return RegexPatternDetector.SplitOnContainerDot(pattern, tree);
    }

    #region IsRegexPattern — positive (is regex)

    [Theory]
    [InlineData("(Read|Write)")]
    [InlineData("Read|Write")]
    [InlineData("[abc]")]
    [InlineData("Goo.*Bar")]
    [InlineData("Goo.+Bar")]
    [InlineData("x+")]
    [InlineData("x?")]
    [InlineData("x*")]
    [InlineData(@"a\d")]
    [InlineData("^Start")]
    [InlineData("End$")]
    [InlineData("a{2,3}")]
    [InlineData("a{2}")]
    [InlineData(@"Goo\.Bar")]
    [InlineData("(Read|Write)Line")]
    [InlineData("(?:abc)")]
    public void IsRegexPattern_Positive(string pattern)
    {
        Assert.True(RegexPatternDetector.IsRegexPattern(pattern));
    }

    #endregion

    #region IsRegexPattern — negative (not regex)

    [Theory]
    [InlineData("abc")]
    [InlineData("GooBar")]
    [InlineData("x.y")]
    [InlineData("Goo.Bar.Baz")]
    [InlineData("get word")]
    [InlineData("ReadLine")]
    [InlineData("_myField")]
    [InlineData("System.IO.File")]
    [InlineData("")]
    [InlineData("a")]
    public void IsRegexPattern_Negative(string pattern)
    {
        Assert.False(RegexPatternDetector.IsRegexPattern(pattern));
    }

    #endregion

    #region SplitOnContainerDot — positive (split occurs)

    [Fact]
    public void Split_PlainGooDotBar()
    {
        // Goo.Bar -> bare dot splits into container="Goo", name="Bar"
        var (container, name) = SplitOnContainerDot("Goo.Bar");
        Assert.Equal("Goo", container);
        Assert.Equal("Bar", name);
    }

    [Fact]
    public void Split_RegexContainerPlainName()
    {
        // (Goo|Bar).Baz -> bare dot after ')' -> container="(Goo|Bar)", name="Baz"
        var (container, name) = SplitOnContainerDot("(Goo|Bar).Baz");
        Assert.Equal("(Goo|Bar)", container);
        Assert.Equal("Baz", name);
    }

    [Fact]
    public void Split_RegexContainerRegexName()
    {
        // (Goo|Bar).(Baz|Quux) -> bare dot -> container="(Goo|Bar)", name="(Baz|Quux)"
        var (container, name) = SplitOnContainerDot("(Goo|Bar).(Baz|Quux)");
        Assert.Equal("(Goo|Bar)", container);
        Assert.Equal("(Baz|Quux)", name);
    }

    [Fact]
    public void Split_MultipleDots_SplitsOnLast()
    {
        // System.IO.File -> last bare dot is before "File"
        var (container, name) = SplitOnContainerDot("System.IO.File");
        Assert.Equal("System.IO", container);
        Assert.Equal("File", name);
    }

    [Fact]
    public void Split_RegexContainerWithMultipleDots()
    {
        // System.(IO|Net).File -> last bare dot is before "File"
        var (container, name) = SplitOnContainerDot("System.(IO|Net).File");
        Assert.Equal("System.(IO|Net)", container);
        Assert.Equal("File", name);
    }

    [Fact]
    public void Split_ReadDotLine()
    {
        // Read.Line -> bare dot splits
        var (container, name) = SplitOnContainerDot("Read.Line");
        Assert.Equal("Read", container);
        Assert.Equal("Line", name);
    }

    #endregion

    #region SplitOnContainerDot — negative (no split)

    [Fact]
    public void NoSplit_GooDotStarBar()
    {
        // Goo.*Bar -> the dot is quantified with *, not bare -> no split
        var (container, name) = SplitOnContainerDot("Goo.*Bar");
        Assert.Null(container);
        Assert.Equal("Goo.*Bar", name);
    }

    [Fact]
    public void NoSplit_GooDotPlusBar()
    {
        // Goo.+Bar -> the dot is quantified with +, not bare -> no split
        var (container, name) = SplitOnContainerDot("Goo.+Bar");
        Assert.Null(container);
        Assert.Equal("Goo.+Bar", name);
    }

    [Fact]
    public void NoSplit_EscapedDot()
    {
        // Goo\.Bar -> escape node, not a wildcard -> no split
        var (container, name) = SplitOnContainerDot(@"Goo\.Bar");
        Assert.Null(container);
        Assert.Equal(@"Goo\.Bar", name);
    }

    [Fact]
    public void NoSplit_NoDotAtAll()
    {
        // (Read|Write)Line -> no dot at all
        var (container, name) = SplitOnContainerDot("(Read|Write)Line");
        Assert.Null(container);
        Assert.Equal("(Read|Write)Line", name);
    }

    [Fact]
    public void NoSplit_PlainTextNoDot()
    {
        var (container, name) = SplitOnContainerDot("ReadLine");
        Assert.Null(container);
        Assert.Equal("ReadLine", name);
    }

    [Fact]
    public void NoSplit_TopLevelAlternation()
    {
        // Goo.Bar|Baz.Quux -> top-level alternation has two branches; dot is inside a branch, not top-level
        var (container, name) = SplitOnContainerDot("Goo.Bar|Baz.Quux");
        Assert.Null(container);
        Assert.Equal("Goo.Bar|Baz.Quux", name);
    }

    [Fact]
    public void NoSplit_DotQuestionBar()
    {
        // Goo.?Bar -> the dot is quantified with ? -> no split
        var (container, name) = SplitOnContainerDot("Goo.?Bar");
        Assert.Null(container);
        Assert.Equal("Goo.?Bar", name);
    }

    [Fact]
    public void NoSplit_InvalidRegex()
    {
        // Unbalanced parens -> parser returns diagnostics -> no split, return as-is
        var (container, name) = SplitOnContainerDot("(Goo.Bar");
        Assert.Null(container);
        Assert.Equal("(Goo.Bar", name);
    }

    [Fact]
    public void NoSplit_EmptyPattern()
    {
        var (container, name) = SplitOnContainerDot("");
        Assert.Null(container);
        Assert.Equal("", name);
    }

    [Fact]
    public void NoSplit_DotAtStart()
    {
        // .Goo -> the bare dot is at position 0, so containerEnd == 0 -> skip it
        var (container, name) = SplitOnContainerDot(".Goo");
        Assert.Null(container);
        Assert.Equal(".Goo", name);
    }

    [Fact]
    public void NoSplit_DotAtEnd()
    {
        // Goo. -> the bare dot is at the last position, so nameStart >= pattern.Length -> skip it
        var (container, name) = SplitOnContainerDot("Goo.");
        Assert.Null(container);
        Assert.Equal("Goo.", name);
    }

    [Fact]
    public void Split_DotAtStartAndMiddle_SplitsOnMiddle()
    {
        // .Goo.Bar -> leading dot is skipped (containerEnd == 0), middle dot splits
        var (container, name) = SplitOnContainerDot(".Goo.Bar");
        Assert.Equal(".Goo", container);
        Assert.Equal("Bar", name);
    }

    [Fact]
    public void Split_DotAtMiddleAndEnd_SplitsOnMiddle()
    {
        // Goo.Bar. -> trailing dot is skipped (nameStart >= Length), middle dot splits
        var (container, name) = SplitOnContainerDot("Goo.Bar.");
        Assert.Equal("Goo", container);
        Assert.Equal("Bar.", name);
    }

    #endregion
}
