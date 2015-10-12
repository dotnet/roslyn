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
    public class PropertyDeclarationOutlinerTests :
        AbstractOutlinerTests<PropertyDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(PropertyDeclarationSyntax propDecl)
        {
            var outliner = new PropertyDeclarationOutliner();
            return outliner.GetOutliningSpans(propDecl, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestProperty()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public int Foo",
                                        "  {",
                                        "    get { }",
                                        "    set { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();

            var actualRegion = GetRegion(propDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(28, 64),
                TextSpan.FromBounds(14, 64),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyWithLeadingComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public int Foo",
                                        "  {",
                                        "    get { }",
                                        "    set { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();

            var actualRegions = GetRegions(propDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(48, 84),
                TextSpan.FromBounds(34, 84),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyWithWithExpressionBodyAndComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public int Foo => 0;",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();

            var actualRegion = GetRegion(propDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestPropertyWithSpaceAfterIdentifier()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public int Foo    ",
                                        "  {",
                                        "    get { }",
                                        "    set { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var propDecl = typeDecl.DigToFirstNodeOfType<PropertyDeclarationSyntax>();

            var actualRegion = GetRegion(propDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(32, 68),
                TextSpan.FromBounds(14, 68),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
