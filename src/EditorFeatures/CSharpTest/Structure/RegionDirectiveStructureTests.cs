// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class RegionDirectiveStructureTests : AbstractCSharpSyntaxNodeStructureTests<RegionDirectiveTriviaSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new RegionDirectiveStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task BrokenRegion()
        {
            const string code = @"
$$#region Goo";

            await VerifyNoBlockSpansAsync(code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task SimpleRegion()
        {
            const string code = @"
{|span:$$#region Goo
#endregion|}";

            await VerifyBlockSpansAsync(code,
                Region("span", "Goo", autoCollapse: false, isDefaultCollapsed: true));
        }

        [WorkItem(539361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539361")]
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

            await VerifyBlockSpansAsync(code,
                Region("span", "TaoRegion", autoCollapse: false, isDefaultCollapsed: true));
        }

        [WorkItem(953668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/953668")]
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

            await VerifyBlockSpansAsync(code,
                Region("span", "Region", autoCollapse: false, isDefaultCollapsed: true));
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

            await VerifyBlockSpansAsync(code,
                Region("span", "Region", autoCollapse: false, isDefaultCollapsed: true));
        }
    }
}
