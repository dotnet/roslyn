// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using IndentCache = Microsoft.AspNetCore.Razor.Language.CodeGeneration.CodeWriter.IndentCache;

namespace Microsoft.AspNetCore.Razor.Language.Test.CodeGeneration;

public class IndentCacheTest
{
    [Theory]
    [InlineData(0, false, 4, "")]
    [InlineData(4, false, 4, "    ")]
    [InlineData(8, false, 4, "        ")]
    [InlineData(0, true, 4, "")]
    [InlineData(4, true, 4, "\t")]
    [InlineData(8, true, 4, "\t\t")]
    [InlineData(6, true, 4, "\t  ")]
    [InlineData(5, true, 4, "\t ")]
    [InlineData(3, true, 4, "   ")]
    public void GetIndentString_ReturnsExpectedString(int size, bool useTabs, int tabSize, string expected)
    {
        var result = IndentCache.GetIndentString(size, useTabs, tabSize);
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public void GetIndentString_TabSizeOne_UsesOnlyTabs()
    {
        var result = IndentCache.GetIndentString(size: 5, useTabs: true, tabSize: 1);
        Assert.Equal(new string('\t', 5), result.ToString());
    }

    [Fact]
    public void GetIndentString_TabSizeGreaterThanSize_UsesSpaces()
    {
        var result = IndentCache.GetIndentString(size: 3, useTabs: true, tabSize: 10);
        Assert.Equal("   ", result.ToString());
    }

    [Fact]
    public void GetIndentString_TabsAndSpacesInResultExceedCachedSizes()
    {
        var spaceCount = IndentCache.MaxSpaceCount + 1;
        var tabCount = IndentCache.MaxTabCount + 1;
        var tabSize = spaceCount + 1;

        var size = tabSize * tabCount + spaceCount;
        var result = IndentCache.GetIndentString(size, useTabs: true, tabSize);

        var expected = new string('\t', tabCount) + new string(' ', spaceCount);
        Assert.Equal(expected, result.ToString());
    }
}
