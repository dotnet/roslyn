// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class NamespaceDeclarationOutlinerTests :
        AbstractOutlinerTests<NamespaceDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(NamespaceDeclarationSyntax namespaceDecl)
        {
            var outliner = new NamespaceDeclarationOutliner();
            return outliner.GetOutliningSpans(namespaceDecl, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespace()
        {
            var tree = ParseLines("namespace N",
                                        "{",
                                        "}");

            var namespaceDecl = tree.DigToFirstNodeOfType<NamespaceDeclarationSyntax>();

            var actualRegion = GetRegion(namespaceDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(11, 17),
                TextSpan.FromBounds(0, 17),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithLeadingComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "namespace C",
                                        "{",
                                        "}");

            var namespaceDecl = tree.DigToFirstNodeOfType<NamespaceDeclarationSyntax>();

            var actualRegions = GetRegions(namespaceDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(27, 33),
                TextSpan.FromBounds(16, 33),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedUsings()
        {
            var tree = ParseLines("namespace C",
                                        "{",
                                        "  using System;",
                                        "  using System.Linq;",
                                        "}");

            var namespaceDecl = tree.DigToFirstNodeOfType<NamespaceDeclarationSyntax>();

            var actualRegions = GetRegions(namespaceDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(11, 56),
                TextSpan.FromBounds(0, 56),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(24, 53),
                hintSpan: TextSpan.FromBounds(18, 53),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedUsingsWithLeadingComments()
        {
            var tree = ParseLines("namespace C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  using System;",
                                        "  using System.Linq;",
                                        "}");

            var namespaceDecl = tree.DigToFirstNodeOfType<NamespaceDeclarationSyntax>();

            var actualRegions = GetRegions(namespaceDecl).ToList();
            Assert.Equal(3, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(11, 76),
                TextSpan.FromBounds(0, 76),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(18, 34),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);

            var expectedRegion3 = new OutliningSpan(
                TextSpan.FromBounds(44, 73),
                hintSpan: TextSpan.FromBounds(38, 73),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion3, actualRegions[2]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestNamespaceWithNestedComments()
        {
            var tree = ParseLines("namespace C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "}");

            var namespaceDecl = tree.DigToFirstNodeOfType<NamespaceDeclarationSyntax>();

            var actualRegions = GetRegions(namespaceDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(11, 37),
                TextSpan.FromBounds(0, 37),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(18, 34),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }
    }
}
