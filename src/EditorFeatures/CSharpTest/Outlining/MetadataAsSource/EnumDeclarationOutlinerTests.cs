// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class EnumDeclarationOutlinerTests :
        AbstractOutlinerTests<EnumDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EnumDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.EnumDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"enum C
{
    A,
    B
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            Assert.Empty(GetRegions(enumDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"[Bar]
enum C
{
    A,
    B
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegion = GetRegion(enumDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 7),
                TextSpan.FromBounds(0, 13),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"// Summary:
//     This is a summary.
[Bar]
enum C
{
    A,
    B
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegion = GetRegion(enumDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 47),
                TextSpan.FromBounds(0, 53),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAttributesAndModifiers()
        {
            var tree = ParseCode(
@"// Summary:
//     This is a summary.
[Bar]
public enum C
{
    A,
    B
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegion = GetRegion(enumDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 47),
                TextSpan.FromBounds(0, 60),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
