// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class CommentTests : AbstractOutlinerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment1()
        {
            var tree = ParseLines("// Hello",
                                        "// C#!",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(4, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 16),
                "// Hello ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment2()
        {
            var tree = ParseLines("// Hello",
                                        "//",
                                        "// C#!",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(6, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 20),
                "// Hello ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSimpleComment3()
        {
            var tree = ParseLines("// Hello",
                                        string.Empty,
                                        "// C#!",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(5, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 18),
                "// Hello ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestSingleLineCommentGroupFollowedByDocumentationComment()
        {
            var tree = ParseLines("// Hello",
                                        string.Empty,
                                        "// C#!",
                                        "/// <summary></summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(6, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 18),
                "// Hello ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineComment1()
        {
            var tree = ParseLines("/* Hello",
                                        "C#! */",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(2, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 16),
                "/* Hello ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineCommentOnOneLine()
        {
            var tree = ParseLines("/* Hello C#! */",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia();
            Assert.Equal(2, trivia.Count);

            var regions = CSharpOutliningHelpers.CreateCommentRegions(trivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 15),
                "/* Hello C#! ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIncompleteMultilineCommentZeroSpace()
        {
            var tree = ParseLines("/*");

            var multiLineCommentTrivia = tree.GetRoot().FindToken(0).LeadingTrivia;
            var regions = CSharpOutliningHelpers.CreateCommentRegions(multiLineCommentTrivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 2),
                "/*  ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WorkItem(791)]
        [WorkItem(1108049)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIncompleteMultilineCommentSingleSpace()
        {
            var tree = ParseLines("/* ");

            var multiLineCommentTrivia = tree.GetRoot().FindToken(0).LeadingTrivia;
            var regions = CSharpOutliningHelpers.CreateCommentRegions(multiLineCommentTrivia).ToList();
            Assert.Equal(1, regions.Count);

            var actualRegion = regions[0];
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 3),
                "/*  ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
