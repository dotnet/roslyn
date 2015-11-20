// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class RegionDirectiveOutlinerTests : AbstractOutlinerTests<RegionDirectiveTriviaSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<RegionDirectiveTriviaSyntax> CreateOutliner()
        {
            return new RegionDirectiveOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void BrokenRegion()
        {
            const string code = @"
$$#region Foo";

            NoRegions(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void SimpleRegion()
        {
            const string code = @"
{|span:$$#region Foo
#endregion|}";

            Regions(code,
                Region("span", "Foo", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(539361)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void RegressionFor5284()
        {
            const string code = @"
namespace BasicGenerateFromUsage
{
    class BasicGenerateFromUsage
    {
        {|span:#reg$$ion TaoRegion

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
        #endregion TaoRegion|}
    }

    class Classic
    {
    }
}";

            Regions(code,
                Region("span", "TaoRegion", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(953668)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void RegionsShouldBeCollapsedByDefault()
        {
            const string code = @"
class C
{
    {|span:#region Re$$gion
    static void Main(string[] args)
    {
    }
    #endregion|}
}";

            Regions(code,
                Region("span", "Region", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(4105, "https://github.com/dotnet/roslyn/issues/4105")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void SpacesBetweenPoundAndRegionShouldNotAffectBanner()
        {
            const string code = @"
class C
{
{|span:#  region R$$egion
    static void Main(string[] args)
    {
    }
#  endregion|}
}";

            Regions(code,
                Region("span", "Region", autoCollapse: true, isDefaultCollapsed: true));
        }
    }
}
