// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class RegionDirectiveOutlinerTests :
        AbstractOutlinerTests<RegionDirectiveTriviaSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(RegionDirectiveTriviaSyntax regionDirective)
        {
            var outliner = new RegionDirectiveOutliner();
            return outliner.GetOutliningSpans(regionDirective, CancellationToken.None);
        }

        private void TestRegion(string expectedRegionName, string code)
        {
            TestTrivia(expectedRegionName, code, SyntaxKind.RegionDirectiveTrivia, autoCollapse: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void BrokenRegion()
        {
            TestRegion(null, "$$#region Foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void SimpleRegion()
        {
            TestRegion("Foo", @"$$[|#region Foo
#endregion|]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(539361)]
        public void RegressionFor5284()
        {
            TestRegion("TaoRegion", @"namespace BasicGenerateFromUsage
{
 
    class BasicGenerateFromUsage
    {
        [|#reg$$ion TaoRegion
 
        static void Main(string[] args)
        {
            /*Marker1*/
            CustomStack s = new CustomStack(); //Generate new class
 
            //Generate constructor
            Classic cc = new Classic(5, 6, 7);/*Marker2*/
 
            Classic cc = new Classic();
            //generate property
            cc.NewProperty = 5; /*Marker3*/
 
        }
        #endregion TaoRegion|]
    }
 
    class Classic
    {
    }
}

");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(953668)]
        public void RegionsShouldBeCollapsedByDefault()
        {
            TestRegion("Region", @"
class C
{
    [|#region R$$egion
    static void Main(string[] args)
    {
    }
    #endregion|]
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(4105, "https://github.com/dotnet/roslyn/issues/4105")]
        public void SpacesBetweenPoundAndRegionShouldNotAffectBanner()
        {
            TestRegion("Region", @"
class C
{
[|#  region R$$egion
    static void Main(string[] args)
    {
    }
#  endregion|]
}
");
        }
    }
}
