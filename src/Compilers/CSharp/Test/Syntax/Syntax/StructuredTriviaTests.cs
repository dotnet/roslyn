﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StructuredTriviaTests
    {
        [Fact]
        public void GetParentTrivia()
        {
            const string conditionName = "condition";

            var trivia1 = SyntaxFactory.Trivia(SyntaxFactory.IfDirectiveTrivia(SyntaxFactory.IdentifierName(conditionName), false, false, false));
            var structuredTrivia = trivia1.GetStructure() as IfDirectiveTriviaSyntax;
            Assert.NotNull(structuredTrivia);
            Assert.Equal(conditionName, ((IdentifierNameSyntax)structuredTrivia.Condition).Identifier.ValueText);
            var trivia2 = structuredTrivia.ParentTrivia;
            Assert.Equal(trivia1, trivia2);
        }

        [Fact]
        public void TestStructuredTrivia()
        {
            var spaceTrivia = SyntaxTriviaListBuilder.Create().Add(SyntaxFactory.Whitespace(" ")).ToList();
            var emptyTrivia = SyntaxTriviaListBuilder.Create().ToList();

            var name = "goo";
            var xmlStartElement = SyntaxFactory.XmlElementStartTag(
                SyntaxFactory.Token(spaceTrivia, SyntaxKind.LessThanToken, default(SyntaxTriviaList)),
                SyntaxFactory.XmlName(null,
                    SyntaxFactory.Identifier(name)),
                default(SyntaxList<XmlAttributeSyntax>),
                SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.GreaterThanToken, spaceTrivia));

            var xmlEndElement = SyntaxFactory.XmlElementEndTag(
                SyntaxFactory.Token(SyntaxKind.LessThanSlashToken),
                SyntaxFactory.XmlName(null,
                    SyntaxFactory.Identifier(name)),
                SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.GreaterThanToken, spaceTrivia));

            var xmlElement = SyntaxFactory.XmlElement(xmlStartElement, default(SyntaxList<XmlNodeSyntax>), xmlEndElement);
            Assert.Equal(" <goo> </goo> ", xmlElement.ToFullString());
            Assert.Equal("<goo> </goo>", xmlElement.ToString());

            var docComment = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia).WithContent(new SyntaxList<XmlNodeSyntax>(xmlElement));
            Assert.Equal(" <goo> </goo> ", docComment.ToFullString());
            // Assert.Equal("<goo> </goo>", docComment.GetText());
            var child = (XmlElementSyntax)docComment.ChildNodesAndTokens()[0];
            Assert.Equal(" <goo> </goo> ", child.ToFullString());
            Assert.Equal("<goo> </goo>", child.ToString());
            Assert.Equal(" <goo> ", child.StartTag.ToFullString());
            Assert.Equal("<goo>", child.StartTag.ToString());

            var sTrivia = SyntaxFactory.Trivia(docComment);
            Assert.NotEqual(default(SyntaxTrivia), sTrivia);
            var ident = SyntaxFactory.Identifier(SyntaxTriviaList.Create(sTrivia), "banana", spaceTrivia);

            Assert.Equal(" <goo> </goo> banana ", ident.ToFullString());
            Assert.Equal("banana", ident.ToString());
            Assert.Equal(" <goo> </goo> ", ident.LeadingTrivia[0].ToFullString());
            // Assert.Equal("<goo> </goo>", ident.LeadingTrivia[0].GetText());

            var identExpr = SyntaxFactory.IdentifierName(ident);

            // make sure FindLeaf digs into the structured trivia.
            var result = identExpr.FindToken(3, true);
            Assert.Equal(SyntaxKind.IdentifierToken, result.Kind());
            Assert.Equal("goo", result.ToString());

            var trResult = identExpr.FindTrivia(6, SyntaxTrivia.Any);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, trResult.Kind());
            Assert.Equal(" ", trResult.ToString());

            var foundDocComment = result.Parent.Parent.Parent.Parent;
            Assert.Null(foundDocComment.Parent);

            var identTrivia = identExpr.GetLeadingTrivia()[0];
            var foundTrivia = ((DocumentationCommentTriviaSyntax)foundDocComment).ParentTrivia;
            Assert.Equal(identTrivia, foundTrivia);

            // make sure FindLeafNodesOverlappingWithSpan does not dig into the structured trivia.
            var resultList = identExpr.DescendantTokens(t => t.FullSpan.OverlapsWith(new TextSpan(3, 18)));
            Assert.Equal(1, resultList.Count());
        }

        [Fact]
        public void ReferenceDirectives1()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
#r ""ref0""
#define Goo
#r ""ref1""
#r ""ref2""
using Blah;
#r ""ref3""
");
            var compilationUnit = tree.GetCompilationUnitRoot();
            var directives = compilationUnit.GetReferenceDirectives();
            Assert.Equal(3, directives.Count);
            Assert.Equal("ref0", directives[0].File.Value);
            Assert.Equal("ref1", directives[1].File.Value);
            Assert.Equal("ref2", directives[2].File.Value);
        }

        [Fact]
        public void ReferenceDirectives2()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
#r ""ref0""
");
            var compilationUnit = tree.GetCompilationUnitRoot();
            var directives = compilationUnit.GetReferenceDirectives();
            Assert.Equal(1, directives.Count);
            Assert.Equal("ref0", directives[0].File.Value);
        }

        [Fact]
        public void ReferenceDirectives3()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
");
            var compilationUnit = tree.GetCompilationUnitRoot();
            var directives = compilationUnit.GetReferenceDirectives();
            Assert.Equal(0, directives.Count);
        }

        [Fact]
        public void ReferenceDirectives4()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
#r 
#r ""
#r ""a"" blah
");
            var compilationUnit = tree.GetCompilationUnitRoot();
            var directives = compilationUnit.GetReferenceDirectives();
            Assert.Equal(3, directives.Count);
            Assert.True(directives[0].File.IsMissing);
            Assert.False(directives[1].File.IsMissing);
            Assert.Equal("", directives[1].File.Value);
            Assert.Equal("a", directives[2].File.Value);
        }

        [WorkItem(546207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546207")]
        [Fact]
        public void DocumentationCommentsLocation_SingleLine()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{ /// <summary/>

    static void Main() { }
}
");

            var trivia = tree.GetCompilationUnitRoot().DescendantTrivia().Single(t => t.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia);
            Assert.Equal(SyntaxKind.StaticKeyword, trivia.Token.Kind());
        }

        [WorkItem(546207, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546207")]
        [Fact]
        public void DocumentationCommentsLocation_MultiLine()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{ /** <summary/> */

    static void Main() { }
}
");

            var trivia = tree.GetCompilationUnitRoot().DescendantTrivia().Single(t => t.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia);
            Assert.Equal(SyntaxKind.StaticKeyword, trivia.Token.Kind());
        }

        [Fact]
        public void TestTriviaList_getItemFailures()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(" class goo {}");

            var trivia = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            var t1 = trivia[0];
            Assert.Equal(1, trivia.Count);

            // Bounds checking exceptions
            Assert.Throws<System.ArgumentOutOfRangeException>(delegate
            {
                var t2 = trivia[1];
            });

            Assert.Throws<System.ArgumentOutOfRangeException>(delegate
            {
                var t3 = trivia[-1];
            });

            // Invalid Use create SyntaxTriviaList
            Assert.Throws<System.ArgumentOutOfRangeException>(delegate
            {
                var trl = new SyntaxTriviaList();
                var t2 = trl[0];
            });
        }
    }
}
