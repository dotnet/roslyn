// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    [Trait(Traits.Feature, Traits.Features.Outlining)]
    public class RegionDirectiveStructureTests : AbstractCSharpSyntaxNodeStructureTests<RegionDirectiveTriviaSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new RegionDirectiveStructureProvider();

        [Fact]
        public async Task BrokenRegion()
        {
            const string code = @"
$$#region Goo";

            await VerifyNoBlockSpansAsync(code);
        }

        [Fact]
        public async Task SimpleRegion()
        {
            const string code = @"
{|span:$$#region Goo
#endregion|}";

            await VerifyBlockSpansAsync(code,
                Region("span", "Goo", autoCollapse: false, isDefaultCollapsed: true));
        }

        [Fact, WorkItem(539361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539361")]
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

        [Theory, WorkItem(953668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/953668"), CombinatorialData]
        public async Task RegionsShouldBeCollapsedByDefault(bool collapseRegionsWhenFirstOpened)
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

            var options = GetDefaultOptions() with
            {
                CollapseRegionsWhenFirstOpened = collapseRegionsWhenFirstOpened
            };

            await VerifyBlockSpansAsync(code, options,
                Region("span", "Region", autoCollapse: false, isDefaultCollapsed: collapseRegionsWhenFirstOpened));
        }

        [Fact, WorkItem(4105, "https://github.com/dotnet/roslyn/issues/4105")]
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
