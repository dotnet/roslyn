// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
{
    public class OutliningSpanTests
    {
        [WpfFact]
        public void TestProperties()
        {
            var span = TextSpan.FromBounds(0, 1);
            var hintSpan = TextSpan.FromBounds(2, 3);
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new OutliningSpan(span, hintSpan, bannerText, autoCollapse);

            Assert.Equal(span, outliningRegion.TextSpan);
            Assert.Equal(hintSpan, outliningRegion.HintSpan);
            Assert.Equal(bannerText, outliningRegion.BannerText);
            Assert.Equal(autoCollapse, outliningRegion.AutoCollapse);
        }

        [WpfFact]
        public void TestToStringWithHintSpan()
        {
            var span = TextSpan.FromBounds(0, 1);
            var hintSpan = TextSpan.FromBounds(2, 3);
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new OutliningSpan(span, hintSpan, bannerText, autoCollapse);

            Assert.Equal("{Span=[0..1), HintSpan=[2..3), BannerText=\"Foo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
        }

        [WpfFact]
        public void TestToStringWithoutHintSpan()
        {
            var span = TextSpan.FromBounds(0, 1);
            var bannerText = "Foo";
            var autoCollapse = true;

            var outliningRegion = new OutliningSpan(span, bannerText, autoCollapse);

            Assert.Equal("{Span=[0..1), BannerText=\"Foo\", AutoCollapse=True, IsDefaultCollapsed=False}", outliningRegion.ToString());
        }
    }
}
