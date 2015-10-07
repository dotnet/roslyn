// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class EnumDeclarationOutlinerTests :
        AbstractOutlinerTests<EnumDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EnumDeclarationSyntax enumDeclaration)
        {
            var outliner = new EnumDeclarationOutliner();
            return outliner.GetOutliningSpans(enumDeclaration, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnum()
        {
            var tree = ParseLines("enum E",
                                        "{",
                                        "}");

            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegion = GetRegion(enumDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(6, 12),
                TextSpan.FromBounds(0, 12),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnumWithLeadingComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "enum E",
                                        "{",
                                        "}");

            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegions = GetRegions(enumDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(22, 28),
                TextSpan.FromBounds(16, 28),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEnumWithNestedComments()
        {
            var tree = ParseLines("enum E",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "}");

            var enumDecl = tree.DigToFirstNodeOfType<EnumDeclarationSyntax>();

            var actualRegions = GetRegions(enumDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(6, 32),
                TextSpan.FromBounds(0, 32),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(13, 29),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }
    }
}
