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
    public class TypeDeclarationOutlinerTests :
        AbstractOutlinerTests<TypeDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(TypeDeclarationSyntax typeDecl)
        {
            var outliner = new TypeDeclarationOutliner();
            return outliner.GetOutliningSpans(typeDecl, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClass()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegion = GetRegion(typeDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(7, 13),
                TextSpan.FromBounds(0, 13),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClassWithLeadingComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(23, 29),
                TextSpan.FromBounds(16, 29),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestClassWithNestedComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(7, 33),
                TextSpan.FromBounds(0, 33),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestInterface()
        {
            var tree = ParseLines("interface I",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegion = GetRegion(typeDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(11, 17),
                TextSpan.FromBounds(0, 17),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestInterfaceWithLeadingComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "interface I",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
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
        public void TestInterfaceWithNestedComments()
        {
            var tree = ParseLines("interface I",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStruct()
        {
            var tree = ParseLines("struct S",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegion = GetRegion(typeDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(8, 14),
                TextSpan.FromBounds(0, 14),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStructWithLeadingComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "struct S",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(24, 30),
                TextSpan.FromBounds(16, 30),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestStructWithNestedComments()
        {
            var tree = ParseLines("struct S",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegions = GetRegions(typeDecl).ToList();
            Assert.Equal(2, actualRegions.Count);

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(8, 34),
                TextSpan.FromBounds(0, 34),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(15, 31),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }
    }
}
