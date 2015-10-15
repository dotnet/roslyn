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
    public class FieldDeclarationOutlinerTests :
        AbstractOutlinerTests<FieldDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(FieldDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.FieldDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    public int foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var fieldDecl = typeDecl.DigToFirstNodeOfType<FieldDeclarationSyntax>();

            Assert.Empty(GetRegions(fieldDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    [Foo]
    public int foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var fieldDecl = typeDecl.DigToFirstNodeOfType<FieldDeclarationSyntax>();

            var actualRegion = GetRegion(fieldDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 29),
                TextSpan.FromBounds(18, 44),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    // Summary:
    //     This is a summary.
    [Foo]
    int foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var fieldDecl = typeDecl.DigToFirstNodeOfType<FieldDeclarationSyntax>();

            var actualRegion = GetRegion(fieldDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 85),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAttributesAndModifiers()
        {
            var tree = ParseCode(
@"class Foo
{
    // Summary:
    //     This is a summary.
    [Foo]
    public int foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var fieldDecl = typeDecl.DigToFirstNodeOfType<FieldDeclarationSyntax>();

            var actualRegion = GetRegion(fieldDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 92),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
