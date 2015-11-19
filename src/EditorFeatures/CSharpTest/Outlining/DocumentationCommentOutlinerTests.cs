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
    public class DocumentationCommentOutlinerTests :
        AbstractOutlinerTests<DocumentationCommentTriviaSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(DocumentationCommentTriviaSyntax documentationComment)
        {
            var outliner = new DocumentationCommentOutliner();
            return outliner.GetOutliningSpans(documentationComment, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationCommentWithoutSummaryTag1()
        {
            var tree = ParseLines("/// XML doc comment",
                                        "/// some description",
                                        "/// of",
                                        "/// the comment",
                                        "class Class3",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 66),
                "/// XML doc comment ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationCommentWithoutSummaryTag2()
        {
            var tree = ParseLines("/** Block comment",
                                        "* some description",
                                        "* of",
                                        "* the comment",
                                        "*/",
                                        "class Class3",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(2, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 62),
                "/** Block comment ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationCommentWithoutSummaryTag3()
        {
            var tree = ParseLines("/// <param name=\"tree\"></param>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 31),
                "/// <param name=\"tree\"></param> ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationComment()
        {
            var tree = ParseLines("/// <summary>",
                                        "/// Hello C#!",
                                        "/// </summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 44),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationCommentWithLongBannerText()
        {
            var tree = ParseLines("/// <summary>",
                                        "/// " + new string('x', 240),
                                        "/// </summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedBannerText = "/// <summary> " + new string('x', 106) + " ...";
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 275),
                expectedBannerText,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineDocumentationComment()
        {
            var tree = ParseLines("/** <summary>",
                                        "Hello C#!",
                                        "</summary> */",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(2, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 39),
                "/** <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndentedDocumentationComment()
        {
            var tree = ParseLines("    /// <summary>",
                                        "    /// Hello C#!",
                                        "    /// </summary>",
                                        "    class C",
                                        "    {",
                                        "    }");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(3, trivia.Count);

            var documentationComment = trivia[1].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(4, 56),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndentedMultilineDocumentationComment()
        {
            var tree = ParseLines("    /** <summary>",
                                        "    Hello C#!",
                                        "    </summary> */",
                                        "    class C",
                                        "    {",
                                        "    }");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(4, trivia.Count);

            var documentationComment = trivia[1].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(4, 51),
                "/** <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDocumentationCommentOnASingleLine()
        {
            var tree = ParseLines("/// <summary>Hello C#!</summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 32),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineDocumentationCommentOnASingleLine()
        {
            var tree = ParseLines("/** <summary>Hello C#!</summary> */",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(2, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 35),
                "/** <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndentedDocumentationCommentOnASingleLine()
        {
            var tree = ParseLines("    /// <summary>Hello C#!</summary>",
                                        "    class C",
                                        "    {",
                                        "    }");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(3, trivia.Count);

            var documentationComment = trivia[1].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(4, 36),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestIndentedMultilineDocumentationCommentOnASingleLine()
        {
            var tree = ParseLines("    /** <summary>Hello C#!</summary> */",
                                        "    class C",
                                        "    {",
                                        "    }");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(4, trivia.Count);

            var documentationComment = trivia[1].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(4, 39),
                "/** <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineSummaryInDocumentationComment1()
        {
            var tree = ParseLines("/// <summary>",
                                        "/// Hello",
                                        "/// C#!",
                                        "/// </summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 49),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestMultilineSummaryInDocumentationComment2()
        {
            var tree = ParseLines("/// <summary>",
                                        "/// Hello",
                                        "/// ",
                                        "/// C#!",
                                        "/// </summary>",
                                        "class C",
                                        "{",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var trivia = typeDecl.GetLeadingTrivia().ToList();
            Assert.Equal(1, trivia.Count);

            var documentationComment = trivia[0].GetStructure() as DocumentationCommentTriviaSyntax;
            Assert.NotNull(documentationComment);

            var actualRegion = GetRegion(documentationComment);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 55),
                "/// <summary> Hello C#!",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(2129, "https://github.com/dotnet/roslyn/issues/2129")]
        public void CrefInSummary()
        {
            var tree = ParseLines("class C",
                                  "{",
                                  "    /// <summary>",
                                  "    /// Summary with <see cref=\"SeeClass\" />, <seealso cref=\"SeeAlsoClass\" />, ",
                                  "    /// <see langword=\"null\" />, <typeparamref name=\"T\" />, <paramref name=\"t\" />, and <see unsupported-attribute=\"not-supported\" />.",
                                  "    /// </summary>",
                                  "    public void M<T>(T t) { }",
                                  "}");

            var method = tree.GetRoot().FindFirstNodeOfType<MethodDeclarationSyntax>();
            var trivia = method.GetLeadingTrivia();

            var docComment = (DocumentationCommentTriviaSyntax)trivia.Single(t => t.HasStructure).GetStructure();
            var actualRegion = GetRegion(docComment);
            var expectedRegion = new OutliningSpan(
                         TextSpan.FromBounds(16, 265),
                         "/// <summary> Summary with SeeClass , SeeAlsoClass , null , T , t , and not-supported .",
                         autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
