// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class RegionDirectiveOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<RegionDirectiveTriviaSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new RegionDirectiveOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task BrokenRegion()
        {
            const string code = @"
$$#region Foo";

            await VerifyNoRegionsAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task SimpleRegion()
        {
            const string code = @"
{|span:$$#region Foo
#endregion|}";

            await VerifyRegionsAsync(code,
                Region("span", "Foo", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(539361)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task RegressionFor5284()
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

            await VerifyRegionsAsync(code,
                Region("span", "TaoRegion", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(953668)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task RegionsShouldBeCollapsedByDefault()
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

            await VerifyRegionsAsync(code,
                Region("span", "Region", autoCollapse: true, isDefaultCollapsed: true));
        }

        [WorkItem(4105, "https://github.com/dotnet/roslyn/issues/4105")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task SpacesBetweenPoundAndRegionShouldNotAffectBanner()
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

            await VerifyRegionsAsync(code,
                Region("span", "Region", autoCollapse: true, isDefaultCollapsed: true));
        }
    }
}
