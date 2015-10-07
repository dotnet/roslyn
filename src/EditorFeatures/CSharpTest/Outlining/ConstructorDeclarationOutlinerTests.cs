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
    public class ConstructorDeclarationOutlinerTests :
        AbstractOutlinerTests<ConstructorDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(ConstructorDeclarationSyntax constructorDeclaration)
        {
            var outliner = new ConstructorDeclarationOutliner();
            return outliner.GetOutliningSpans(constructorDeclaration, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(24, 34),
                TextSpan.FromBounds(14, 34),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  {",
                                        "  }                 ",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(24, 51),
                TextSpan.FromBounds(14, 51),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor3()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  {",
                                        "  } // .ctor",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(24, 34),
                TextSpan.FromBounds(14, 34),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor4()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  {",
                                        "  } /* .ctor */",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(24, 34),
                TextSpan.FromBounds(14, 34),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor5()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C() // .ctor",
                                        "  {",
                                        "  } // .ctor",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(33, 43),
                TextSpan.FromBounds(14, 43),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor6()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C() /* .ctor */",
                                        "  {",
                                        "  } // .ctor",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(36, 46),
                TextSpan.FromBounds(14, 46),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor7()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  // .ctor",
                                        "  {",
                                        "  } // .ctor",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(36, 46),
                TextSpan.FromBounds(14, 46),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructor8()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  public C()",
                                        "  /* .ctor */",
                                        "  {",
                                        "  } // .ctor",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegion = GetRegion(consDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(39, 49),
                TextSpan.FromBounds(14, 49),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructorWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  public C()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();

            var actualRegions = GetRegions(consDecl).ToList();

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(44, 54),
                TextSpan.FromBounds(34, 54),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestConstructorMissingCloseParenAndBody()
        {
            // Expected behavior is that the class should be outlined, but the constructor should not.

            var tree = ParseLines("class C",
                                        "{",
                                        "  C(",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var typeDeclOutliner = new TypeDeclarationOutliner();

            var typeDeclRegions = typeDeclOutliner.GetOutliningSpans(typeDecl, CancellationToken.None).ToList();
            Assert.Equal(1, typeDeclRegions.Count);

            var expectedTypeDeclRegion =
                new OutliningSpan(
                    TextSpan.FromBounds(7, 19),
                    TextSpan.FromBounds(0, 19),
                    CSharpOutliningHelpers.Ellipsis,
                    autoCollapse: false);

            AssertRegion(expectedTypeDeclRegion, typeDeclRegions[0]);

            var consDecl = typeDecl.DigToFirstNodeOfType<ConstructorDeclarationSyntax>();
            var consDeclRegions = GetRegions(consDecl).ToList();

            Assert.Equal(0, consDeclRegions.Count);
        }
    }
}
