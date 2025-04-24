// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure;

public sealed class BlockSpanTests
{
    [Fact]
    public void TestProperties()
    {
        var span = TextSpan.FromBounds(0, 1);
        var hintSpan = TextSpan.FromBounds(2, 3);
        var bannerText = "Goo";
        var autoCollapse = true;

        var outliningRegion = new BlockSpan(
            isCollapsible: true, textSpan: span, hintSpan: hintSpan,
            type: BlockTypes.Nonstructural, bannerText: bannerText, autoCollapse: autoCollapse);

        Assert.Equal(span, outliningRegion.TextSpan);
        Assert.Equal(hintSpan, outliningRegion.HintSpan);
        Assert.Equal(bannerText, outliningRegion.BannerText);
        Assert.Equal(autoCollapse, outliningRegion.AutoCollapse);
    }

    [Fact]
    public void TestToStringWithHintSpan()
    {
        var span = TextSpan.FromBounds(0, 1);
        var hintSpan = TextSpan.FromBounds(2, 3);
        var bannerText = "Goo";
        var autoCollapse = true;

        var outliningRegion = new BlockSpan(
            isCollapsible: true, textSpan: span, hintSpan: hintSpan,
            type: BlockTypes.Nonstructural, bannerText: bannerText, autoCollapse: autoCollapse);

        Assert.Equal("{Span=[0..1), HintSpan=[2..3), BannerText=\"Goo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
    }

    [Fact]
    public void TestToStringWithoutHintSpan()
    {
        var span = TextSpan.FromBounds(0, 1);
        var bannerText = "Goo";
        var autoCollapse = true;

        var outliningRegion = new BlockSpan(
            isCollapsible: true, textSpan: span,
            type: BlockTypes.Nonstructural, bannerText: bannerText, autoCollapse: autoCollapse);

        Assert.Equal("{Span=[0..1), BannerText=\"Goo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
    }
}
