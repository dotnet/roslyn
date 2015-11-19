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
    public class EnumMemberDeclarationOutlinerTests :
        AbstractOutlinerTests<EnumMemberDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EnumMemberDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.EnumMemberDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"enum E
{
    Foo,
    Bar
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();
            var enumMember = enumDecl.DigToFirstNodeOfType<EnumMemberDeclarationSyntax>();

            Assert.Empty(GetRegions(enumMember));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"enum E
{
    [Blah]
    Foo,
    Bar
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();
            var enumMember = enumDecl.DigToFirstNodeOfType<EnumMemberDeclarationSyntax>();

            var actualRegion = GetRegion(enumMember);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(15, 27),
                TextSpan.FromBounds(15, 30),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"enum E
{
    // Summary:
    //     This is a summary.
    [Blah]
    Foo,
    Bar
}");
            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();
            var enumMember = enumDecl.DigToFirstNodeOfType<EnumMemberDeclarationSyntax>();

            var actualRegion = GetRegion(enumMember);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(15, 75),
                TextSpan.FromBounds(15, 78),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
