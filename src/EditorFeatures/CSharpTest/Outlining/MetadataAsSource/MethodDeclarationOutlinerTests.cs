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
    public class MethodDeclarationOutlinerTests :
        AbstractOutlinerTests<MethodDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(MethodDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.MethodDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    public string Bar(int x);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var method = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            Assert.Empty(GetRegions(method));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    [Foo]
    public string Bar(int x);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var method = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(method);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 29),
                TextSpan.FromBounds(18, 54),
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
    string Bar(int x);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var method = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(method);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 95),
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
    public string Bar(int x);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var method = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(method);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 77),
                TextSpan.FromBounds(18, 102),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
