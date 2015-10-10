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
    public class ConstructorDeclarationOutlinerTests :
        AbstractOutlinerTests<ConstructorDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(ConstructorDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.ConstructorDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class C
{
    C();
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            Assert.Empty(GetRegions(consDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class C
{
    [Bar]
    C();
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(16, 27),
                TextSpan.FromBounds(16, 31),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"class C
{
    // Summary:
    //     This is a summary.
    [Bar]
    C();
}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(16, 75),
                TextSpan.FromBounds(16, 79),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAttributesAndModifiers()
        {
            var tree = ParseCode(
@"class C
{
    // Summary:
    //     This is a summary.
    [Bar]
    public C();
}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(16, 75),
                TextSpan.FromBounds(16, 86),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
