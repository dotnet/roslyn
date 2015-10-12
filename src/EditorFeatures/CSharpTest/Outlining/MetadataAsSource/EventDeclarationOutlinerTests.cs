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
    public class EventDeclarationOutlinerTests :
        AbstractOutlinerTests<EventDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EventDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.EventDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    public event EventArgs foo { add; remove; }
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            Assert.Empty(GetRegions(eventDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    [Foo]
    public event EventArgs foo { add; remove; }
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 29),
                TextSpan.FromBounds(18, 72),
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
    event EventArgs foo { add; remove; }
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 113),
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
    public event EventArgs foo { add; remove; }
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 120),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
