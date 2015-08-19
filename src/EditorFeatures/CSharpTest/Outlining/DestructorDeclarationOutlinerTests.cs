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
    public class DestructorDeclarationOutlinerTests :
        AbstractOutlinerTests<DestructorDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(DestructorDeclarationSyntax destructorDeclaration)
        {
            var outliner = new DestructorDeclarationOutliner();
            return outliner.GetOutliningSpans(destructorDeclaration, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDestructor()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  ~C()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var destructor = typeDecl.DigToFirstNodeOfType<DestructorDeclarationSyntax>();

            var actualRegion = GetRegion(destructor);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 28),
                TextSpan.FromBounds(14, 28),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDestructorWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  ~C()",
                                        "  {",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var destructor = typeDecl.DigToFirstNodeOfType<DestructorDeclarationSyntax>();

            var actualRegions = GetRegions(destructor).ToList();

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(38, 48),
                TextSpan.FromBounds(34, 48),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDestructorMissingCloseParenAndBody()
        {
            // Expected behavior is that the class should be outlined, but the destructor should not.

            var tree = ParseLines("class C",
                                        "{",
                                        "  ~C(",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var typeDeclOutliner = new TypeDeclarationOutliner();

            var typeDeclRegions = typeDeclOutliner.GetOutliningSpans(typeDecl, CancellationToken.None).ToList();
            Assert.Equal(1, typeDeclRegions.Count);

            var expectedTypeDeclRegion =
                new OutliningSpan(
                    TextSpan.FromBounds(7, 20),
                    TextSpan.FromBounds(0, 20),
                    CSharpOutliningHelpers.Ellipsis,
                    autoCollapse: false);

            AssertRegion(expectedTypeDeclRegion, typeDeclRegions[0]);

            var destructor = typeDecl.DigToFirstNodeOfType<DestructorDeclarationSyntax>();
            var destructorRegions = GetRegions(destructor).ToList();

            Assert.Equal(0, destructorRegions.Count);
        }
    }
}
