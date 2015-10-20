// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class RegionDirectiveOutlinerTests :
        AbstractOutlinerTests<RegionDirectiveTriviaSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(RegionDirectiveTriviaSyntax node)
        {
            var outliner = new MaSOutliners.RegionDirectiveOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void FileHeader()
        {
            var tree = ParseCode(
@"#region Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#endregion");

            var trivia = tree.GetRoot().FindTrivia(position: 0);
            var region = trivia.GetStructure() as RegionDirectiveTriviaSyntax;

            Assert.NotNull(region);

            var actualOutliningSpan = GetRegion(region);
            var expectedOutliningSpan = new OutliningSpan(
                TextSpan.FromBounds(0, 204),
                bannerText: "Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                autoCollapse: true);

            AssertRegion(expectedOutliningSpan, actualOutliningSpan);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void EmptyFileHeader()
        {
            var tree = ParseCode(
@"#region
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll
#endregion");

            var trivia = tree.GetRoot().FindTrivia(position: 0);
            var region = trivia.GetStructure() as RegionDirectiveTriviaSyntax;

            Assert.NotNull(region);

            var actualOutliningSpan = GetRegion(region);
            var expectedOutliningSpan = new OutliningSpan(
                TextSpan.FromBounds(0, 119),
                bannerText: "#region",
                autoCollapse: true);

            AssertRegion(expectedOutliningSpan, actualOutliningSpan);
        }
    }
}
