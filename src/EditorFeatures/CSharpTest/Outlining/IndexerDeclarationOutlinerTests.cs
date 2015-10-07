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
    public class IndexerDeclarationOutlinerTests :
        AbstractOutlinerTests<IndexerDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(IndexerDeclarationSyntax indexerDecl)
        {
            var outliner = new IndexerDeclarationOutliner();
            return outliner.GetOutliningSpans(indexerDecl, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndexer()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string this[int index]",
                                        "  {",
                                        "    get { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var indexerDecl = typeDecl.DigToFirstNodeOfType<IndexerDeclarationSyntax>();

            var actualRegion = GetRegion(indexerDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(43, 66),
                TextSpan.FromBounds(14, 66),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndexerWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public string this[int index]",
                                        "  {",
                                        "    get { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var indexerDecl = typeDecl.DigToFirstNodeOfType<IndexerDeclarationSyntax>();

            var actualRegions = GetRegions(indexerDecl).ToList();

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(63, 86),
                TextSpan.FromBounds(34, 86),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndexerWithWithExpressionBodyAndComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public string this[int index] => 0;",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var indexerDecl = typeDecl.DigToFirstNodeOfType<IndexerDeclarationSyntax>();

            var actualRegion = GetRegion(indexerDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
