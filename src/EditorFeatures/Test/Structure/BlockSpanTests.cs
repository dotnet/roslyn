// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    public class BlockSpanTests
    {
        [Fact]
        public void TestProperties()
        {
            var span = TextSpan.FromBounds(0, 1);
            var hintSpan = TextSpan.FromBounds(2, 3);
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new BlockSpan(true, span, hintSpan, 
                bannerText: bannerText, autoCollapse: autoCollapse);

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
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new BlockSpan(true, span, hintSpan, 
                bannerText: bannerText, autoCollapse: autoCollapse);

            Assert.Equal("{Span=[0..1), HintSpan=[2..3), BannerText=\"Foo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
        }

        [Fact]
        public void TestToStringWithoutHintSpan()
        {
            var span = TextSpan.FromBounds(0, 1);
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new BlockSpan(true, span, 
                bannerText: bannerText, autoCollapse: autoCollapse);

            Assert.Equal("{Span=[0..1), BannerText=\"Foo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
        }
    }
}