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
    public class EventFieldDeclarationOutlinerTests :
        AbstractOutlinerTests<EventFieldDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EventFieldDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.EventFieldDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    public event EventArgs foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventFieldDeclarationSyntax>();

            Assert.Empty(GetRegions(eventDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    [Foo]
    public event EventArgs foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventFieldDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 29),
                TextSpan.FromBounds(18, 56),
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
    event EventArgs foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventFieldDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 97),
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
    public event EventArgs foo;
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventFieldDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 104),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
