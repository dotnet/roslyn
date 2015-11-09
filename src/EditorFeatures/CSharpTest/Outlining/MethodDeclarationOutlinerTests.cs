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
    public class MethodDeclarationOutlinerTests :
        AbstractOutlinerTests<MethodDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(MethodDeclarationSyntax methodDecl)
        {
            var outliner = new MethodDeclarationOutliner();
            return outliner.GetOutliningSpans(methodDecl, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethod()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Foo()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var methodDecl = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(methodDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(33, 43),
                TextSpan.FromBounds(14, 43),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithTrailingSpaces()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public string Foo()    ",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var methodDecl = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(methodDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(37, 47),
                TextSpan.FromBounds(14, 47),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithLeadingComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public string Foo()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var methodDecl = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegions = GetRegions(methodDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(53, 63),
                TextSpan.FromBounds(34, 63),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMethodWithWithExpressionBodyAndComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public string Foo() => \"Foo\";",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var methodDecl = typeDecl.DigToFirstNodeOfType<MethodDeclarationSyntax>();

            var actualRegion = GetRegion(methodDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
