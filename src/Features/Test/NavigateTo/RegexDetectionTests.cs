// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.NavigateTo;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.NavigateTo;

public class RegexDetectionTests
{
    #region IsRegexPattern — positive (is regex)

    [Theory]
    [InlineData("(Read|Write)")]
    [InlineData("Read|Write")]
    [InlineData("[abc]")]
    [InlineData("Foo.*Bar")]
    [InlineData("Foo.+Bar")]
    [InlineData("x+")]
    [InlineData("x?")]
    [InlineData("x*")]
    [InlineData(@"a\d")]
    [InlineData("^Start")]
    [InlineData("End$")]
    [InlineData("a{2,3}")]
    [InlineData("a{2}")]
    [InlineData(@"Foo\.Bar")]
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
    [InlineData("FooBar")]
    [InlineData("x.y")]
    [InlineData("Foo.Bar.Baz")]
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
    public void Split_PlainFooDotBar()
    {
        // Foo.Bar -> bare dot splits into container="Foo", name="Bar"
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Foo.Bar");
        Assert.Equal("Foo", container);
        Assert.Equal("Bar", name);
    }

    [Fact]
    public void Split_RegexContainerPlainName()
    {
        // (Foo|Bar).Baz -> bare dot after ')' -> container="(Foo|Bar)", name="Baz"
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("(Foo|Bar).Baz");
        Assert.Equal("(Foo|Bar)", container);
        Assert.Equal("Baz", name);
    }

    [Fact]
    public void Split_RegexContainerRegexName()
    {
        // (Foo|Bar).(Baz|Quux) -> bare dot -> container="(Foo|Bar)", name="(Baz|Quux)"
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("(Foo|Bar).(Baz|Quux)");
        Assert.Equal("(Foo|Bar)", container);
        Assert.Equal("(Baz|Quux)", name);
    }

    [Fact]
    public void Split_MultipleDots_SplitsOnLast()
    {
        // System.IO.File -> last bare dot is before "File"
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("System.IO.File");
        Assert.Equal("System.IO", container);
        Assert.Equal("File", name);
    }

    [Fact]
    public void Split_RegexContainerWithMultipleDots()
    {
        // System.(IO|Net).File -> last bare dot is before "File"
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("System.(IO|Net).File");
        Assert.Equal("System.(IO|Net)", container);
        Assert.Equal("File", name);
    }

    [Fact]
    public void Split_ReadDotLine()
    {
        // Read.Line -> bare dot splits
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Read.Line");
        Assert.Equal("Read", container);
        Assert.Equal("Line", name);
    }

    #endregion

    #region SplitOnContainerDot — negative (no split)

    [Fact]
    public void NoSplit_FooDotStarBar()
    {
        // Foo.*Bar -> the dot is quantified with *, not bare -> no split
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Foo.*Bar");
        Assert.Null(container);
        Assert.Equal("Foo.*Bar", name);
    }

    [Fact]
    public void NoSplit_FooDotPlusBar()
    {
        // Foo.+Bar -> the dot is quantified with +, not bare -> no split
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Foo.+Bar");
        Assert.Null(container);
        Assert.Equal("Foo.+Bar", name);
    }

    [Fact]
    public void NoSplit_EscapedDot()
    {
        // Foo\.Bar -> escape node, not a wildcard -> no split
        var (container, name) = RegexPatternDetector.SplitOnContainerDot(@"Foo\.Bar");
        Assert.Null(container);
        Assert.Equal(@"Foo\.Bar", name);
    }

    [Fact]
    public void NoSplit_NoDotAtAll()
    {
        // (Read|Write)Line -> no dot at all
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("(Read|Write)Line");
        Assert.Null(container);
        Assert.Equal("(Read|Write)Line", name);
    }

    [Fact]
    public void NoSplit_PlainTextNoDot()
    {
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("ReadLine");
        Assert.Null(container);
        Assert.Equal("ReadLine", name);
    }

    [Fact]
    public void NoSplit_TopLevelAlternation()
    {
        // Foo.Bar|Baz.Quux -> top-level alternation has two branches; dot is inside a branch, not top-level
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Foo.Bar|Baz.Quux");
        Assert.Null(container);
        Assert.Equal("Foo.Bar|Baz.Quux", name);
    }

    [Fact]
    public void NoSplit_DotQuestionBar()
    {
        // Foo.?Bar -> the dot is quantified with ? -> no split
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("Foo.?Bar");
        Assert.Null(container);
        Assert.Equal("Foo.?Bar", name);
    }

    [Fact]
    public void NoSplit_InvalidRegex()
    {
        // Unbalanced parens -> parser returns diagnostics -> no split, return as-is
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("(Foo.Bar");
        Assert.Null(container);
        Assert.Equal("(Foo.Bar", name);
    }

    [Fact]
    public void NoSplit_EmptyPattern()
    {
        var (container, name) = RegexPatternDetector.SplitOnContainerDot("");
        Assert.Null(container);
        Assert.Equal("", name);
    }

    #endregion
}
