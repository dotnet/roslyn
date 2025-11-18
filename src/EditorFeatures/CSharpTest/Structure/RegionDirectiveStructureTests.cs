// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class RegionDirectiveStructureTests : AbstractCSharpSyntaxNodeStructureTests<RegionDirectiveTriviaSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new RegionDirectiveStructureProvider();

    [Fact]
    public Task BrokenRegion()
        => VerifyNoBlockSpansAsync("""
                $$#region Goo
                """);

    [Fact]
    public Task SimpleRegion()
        => VerifyBlockSpansAsync("""
                {|span:$$#region Goo
                #endregion|}
                """,
            Region("span", "Goo", autoCollapse: false, isDefaultCollapsed: true));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539361")]
    public Task RegressionFor5284()
        => VerifyBlockSpansAsync("""
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
                }
                """,
            Region("span", "TaoRegion", autoCollapse: false, isDefaultCollapsed: true));

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/953668"), CombinatorialData]
    public async Task RegionsShouldBeCollapsedByDefault(bool collapseRegionsWhenFirstOpened)
    {
        var options = GetDefaultOptions() with
        {
            CollapseRegionsWhenFirstOpened = collapseRegionsWhenFirstOpened
        };

        await VerifyBlockSpansAsync("""
                class C
                {
                    {|span:#region Re$$gion
                    static void Main(string[] args)
                    {
                    }
                    #endregion|}
                }
                """, options,
            Region("span", "Region", autoCollapse: false, isDefaultCollapsed: collapseRegionsWhenFirstOpened));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4105")]
    public Task SpacesBetweenPoundAndRegionShouldNotAffectBanner()
        => VerifyBlockSpansAsync("""
                class C
                {
                {|span:#  region R$$egion
                    static void Main(string[] args)
                    {
                    }
                #  endregion|}
                }
                """,
            Region("span", "Region", autoCollapse: false, isDefaultCollapsed: true));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75583")]
    public Task Trailing()
        => VerifyBlockSpansAsync("""
            {|span:#region R$$1
            /* comment */ #endregion
            /* comment */ #region R2
            #endregion|}
            """,
            Region("span", "R1", autoCollapse: false, isDefaultCollapsed: true));
}
