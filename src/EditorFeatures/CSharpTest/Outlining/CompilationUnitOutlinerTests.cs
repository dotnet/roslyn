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
    public class CompilationUnitOutlinerTests : AbstractOutlinerTests<CompilationUnitSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(CompilationUnitSyntax node)
        {
            var outliner = new CompilationUnitOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsings()
        {
            var tree = ParseLines("using System;",
                                        "using System.Core;");

            var compilationUnit = tree.GetRoot() as CompilationUnitSyntax;

            var actualRegion = GetRegion(compilationUnit);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(6, 33),
                hintSpan: TextSpan.FromBounds(0, 33),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsingAliases()
        {
            var tree = ParseLines("using System;",
                                        "using System.Core;",
                                        "using text = System.Text;",
                                        "using linq = System.Linq;");

            var compilationUnit = tree.GetRoot() as CompilationUnitSyntax;

            var actualRegion = GetRegion(compilationUnit);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(6, 87),
                hintSpan: TextSpan.FromBounds(0, 87),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliases()
        {
            var tree = ParseLines("extern alias Foo;",
                                        "extern alias Bar;");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegion = GetRegion(compilationUnit);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(7, 36),
                hintSpan: TextSpan.FromBounds(0, 36),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesAndUsings()
        {
            var tree = ParseLines("extern alias Foo;",
                                        "extern alias Bar;",
                                        "using System;",
                                        "using System.Core;");

            var compilationUnit = tree.GetRoot() as CompilationUnitSyntax;

            var actualRegion = GetRegion(compilationUnit);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(7, 71),
                hintSpan: TextSpan.FromBounds(0, 71),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesAndUsingsWithLeadingTrailingAndNestedComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "extern alias Foo;",
                                        "extern alias Bar;",
                                        "// Foo",
                                        "// Bar",
                                        "using System;",
                                        "using System.Core;",
                                        "// Foo",
                                        "// Bar");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegions = GetRegions(compilationUnit).ToList();
            Assert.Equal(3, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(23, 103),
                hintSpan: TextSpan.FromBounds(16, 103),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);

            var expectedRegion3 = new OutliningSpan(
                TextSpan.FromBounds(105, 119),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion3, actualRegions[2]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestUsingsWithComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "using System;",
                                        "using System.Core;");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegions = GetRegions(compilationUnit).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(22, 49),
                hintSpan: TextSpan.FromBounds(16, 49),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestExternAliasesWithComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "extern alias Foo;",
                                        "extern alias Bar;");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegions = GetRegions(compilationUnit).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(23, 52),
                hintSpan: TextSpan.FromBounds(16, 52),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestWithComments()
        {
            var tree = ParseLines("// Foo",
                                  "// Bar");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegion = GetRegion(compilationUnit);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestWithCommentsAtEnd()
        {
            var tree = ParseLines("using System;",
                                        "// Foo",
                                        "// Bar");

            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();

            var actualRegions = GetRegions(compilationUnit).ToList();

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(6, 13),
                hintSpan: TextSpan.FromBounds(0, 13),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(15, 29),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(539359)]
        public void TestUsingKeywordWithSpace()
        {
            var tree = ParseLines("using ");
            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();
            var actualRegions = GetRegions(compilationUnit).ToList();

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(6, 6),
                TextSpan.FromBounds(0, 5),
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegions[0]);
        }
    }
}
