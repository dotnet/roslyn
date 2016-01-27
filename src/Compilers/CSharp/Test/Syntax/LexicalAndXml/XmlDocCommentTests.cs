// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class XmlDocCommentTests : CSharpTestBase
    {
        private CSharpParseOptions GetOptions(string[] defines)
        {
            return new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp3,
                documentationMode: DocumentationMode.Diagnose,
                preprocessorSymbols: defines);
        }

        private SyntaxTree Parse(string text, params string[] defines)
        {
            var options = this.GetOptions(defines);
            var itext = SourceText.From(text);
            return SyntaxFactory.ParseSyntaxTree(itext, options);
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributes()
        {
            var text = "/// <foo />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia;
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
        }

        [WorkItem(537500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537500")]
        [Fact]
        public void TestFourOrMoreSlashesIsNotXmlComment()
        {
            var text = "//// <foo />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia;
            Assert.Equal(1, leading.Count);
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, leading[0].Kind());
            Assert.Equal(text, leading[0].ToFullString());
        }

        [WorkItem(537500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537500")]
        [Fact]
        public void TestFourOrMoreSlashesInsideXmlCommentIsNotXmlComment()
        {
            var text = @"/// <foo>
//// </foo>
";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia;
            Assert.Equal(3, leading.Count);
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, leading[0].Kind());
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, leading[1].Kind());
        }

        [WorkItem(537500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537500")]
        [Fact]
        public void TestThreeOrMoreAsterisksIsNotXmlComment()
        {
            var text = "/*** <foo /> */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia;
            Assert.Equal(1, leading.Count);
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, leading[0].Kind());
            Assert.Equal(text, leading[0].ToFullString());
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributesPrecedingClass()
        {
            var text =
@"/// <foo />
class C { }";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            Assert.Equal(SyntaxKind.ClassDeclaration, tree.GetCompilationUnitRoot().Members[0].Kind());
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <foo />\r\n", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementNoAttributesDelimited()
        {
            var text = "/** <foo /> */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.LeadingTrivia;
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementNoAttributesDelimitedPrecedingClass()
        {
            var text =
@"/** <foo /> */
class C { }";

            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            Assert.Equal(SyntaxKind.ClassDeclaration, tree.GetCompilationUnitRoot().Members[0].Kind());
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(2, leading.Count); // a new line follows the comment
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/** <foo /> */", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementWithAttributes()
        {
            var text =
@"/// <foo a=""xyz""/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributesSingleQuoted()
        {
            var text =
@"/// <foo a='xyz'/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributesNestedQuote()
        {
            var text =
@"/// <foo a=""x'y'z""/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlTextAttribute, element.Attributes[0].Kind());
            var attr = (XmlTextAttributeSyntax)element.Attributes[0];
            Assert.Equal(1, attr.TextTokens.Count);
            Assert.Equal("x'y'z", attr.TextTokens[0].ToString());
        }

        [Fact]
        public void TestEmptyElementWithAttributesNestedQuoteSingleQuoted()
        {
            var text =
@"/// <foo a='x""y""z'/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlTextAttribute, element.Attributes[0].Kind());
            var attr = (XmlTextAttributeSyntax)element.Attributes[0];
            Assert.Equal(1, attr.TextTokens.Count);
            Assert.Equal("x\"y\"z", attr.TextTokens[0].ToString());
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributesMultipleLines()
        {
            var text =
@"/// <foo 
/// />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal("<foo \r\n/// />", doc.Content[1].ToFullString());
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributesMultipleLinesPrecedingClass()
        {
            var text =
@"/// <foo 
/// />
class C { }";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <foo \r\n/// />\r\n", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal("<foo \r\n/// />", doc.Content[1].ToFullString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributesMultipleLinesDelimited()
        {
            var text =
@"/** <foo 
  * />
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal("<foo \r\n  * />", doc.Content[1].ToFullString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [ClrOnlyFact]
        public void TestEmptyElementNoAttributesMultipleLinesDelimitedPrecedingClass()
        {
            var text =
@"/** <foo 
  * />
  */
class C { }";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(2, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/** <foo \r\n  * />\r\n  */", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            Assert.Equal("<foo \r\n  * />", doc.Content[1].ToFullString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementWithAttributesDoubleQuoteMultipleLines()
        {
            var text =
@"/// <foo 
/// a
/// =
/// ""xyz""
/// />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributesQuoteMultipleLines()
        {
            var text =
@"/// <foo 
/// a
/// =
/// 'xyz'
/// />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributesQuoteMultipleLinesDelimited()
        {
            var text =
@"/** <foo 
  * a
  * =
  * 'xyz'
  * />
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementWithAttributesDoubleQuoteMultipleLinesDelimited()
        {
            var text =
@"/** <foo 
  * a
  * =
  * ""xyz""
  * />
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementWithAttributeQuoteAndAttributeTextOnMultipleLines()
        {
            var text =
@"/// <foo 
/// a
/// =
/// '
/// xyz
/// '
/// />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributeDoubleQuoteAndAttributeTextOnMultipleLines()
        {
            var text =
@"/// <foo 
/// a
/// =
/// ""
/// xyz
/// ""
/// />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
        }

        [Fact]
        public void TestEmptyElementWithAttributeDoubleQuoteAndAttributeTextOnMultipleLinesDelimited()
        {
            var text =
@"/** <foo 
  * a
  * =
  * ""
  * xyz
  * ""
  * />
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestEmptyElementWithAttributeQuoteAndAttributeTextOnMultipleLinesDelimited()
        {
            var text =
@"/** <foo 
  * a
  * =
  * '
  * xyz
  * '
  * />
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestElementDotInName()
        {
            var text = "/// <foo.bar />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal("foo.bar", element.Name.ToString());
        }

        [Fact]
        public void TestElementColonInName()
        {
            var text = "/// <foo:bar />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal("foo:bar", element.Name.ToString());
        }

        [Fact]
        public void TestElementDashInName()
        {
            var text = "/// <abc-def />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal("abc-def", element.Name.ToString());
        }

        [Fact]
        public void TestElementNumberInName()
        {
            var text = "/// <foo123 />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal("foo123", element.Name.ToString());
        }

        [Fact]
        public void TestElementNumberIsNameError()
        {
            var text = "/// <123 />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.NotEqual(0, doc.ErrorsAndWarnings().Length);
        }

        [ClrOnlyFact]
        public void TestNonEmptyElementNoAttributes()
        {
            var text =
@"/// <foo>
/// bar
/// </foo>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlElement, doc.Content[1].Kind());
            var element = (XmlElementSyntax)doc.Content[1];
            Assert.Equal("foo", element.StartTag.Name.ToString());
            Assert.Equal("foo", element.EndTag.Name.ToString());
            Assert.Equal(1, element.Content.Count);
            var textsyntax = (XmlTextSyntax)element.Content[0];
            Assert.Equal(4, textsyntax.ChildNodesAndTokens().Count);
            Assert.Equal("\r\n", textsyntax.ChildNodesAndTokens()[0].ToString());
            Assert.Equal(" bar", textsyntax.ChildNodesAndTokens()[1].ToString());
            Assert.Equal("\r\n", textsyntax.ChildNodesAndTokens()[2].ToString());
            Assert.Equal(" ", textsyntax.ChildNodesAndTokens()[3].ToString());
        }

        [ClrOnlyFact]
        public void TestNonEmptyElementNoAttributesDelimited()
        {
            var text =
@"/** <foo>
  * bar
  * </foo>
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlElement, doc.Content[1].Kind());
            var element = (XmlElementSyntax)doc.Content[1];
            Assert.Equal("foo", element.StartTag.Name.ToString());
            Assert.Equal("foo", element.EndTag.Name.ToString());
            Assert.Equal(1, element.Content.Count);
            var textsyntax = (XmlTextSyntax)element.Content[0];
            Assert.Equal(4, textsyntax.ChildNodesAndTokens().Count);
            Assert.Equal("\r\n", textsyntax.ChildNodesAndTokens()[0].ToString());
            Assert.Equal(" bar", textsyntax.ChildNodesAndTokens()[1].ToString());
            Assert.Equal("\r\n", textsyntax.ChildNodesAndTokens()[2].ToString());
            Assert.Equal(" ", textsyntax.ChildNodesAndTokens()[3].ToString());
        }

        [ClrOnlyFact]
        public void TestCDataSection()
        {
            var text =
@"/// <![CDATA[ this is a test
/// of &some; cdata /// */ /**
/// ""']]<>/></text]]>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(5, cdata.TextTokens.Count);
            Assert.Equal(" this is a test", cdata.TextTokens[0].ToString());
            Assert.Equal("\r\n", cdata.TextTokens[1].ToString());
            Assert.Equal(" of &some; cdata /// */ /**", cdata.TextTokens[2].ToString());
            Assert.Equal("\r\n", cdata.TextTokens[3].ToString());
            Assert.Equal(" \"']]<>/></text", cdata.TextTokens[4].ToString());
        }

        [ClrOnlyFact]
        public void TestCDataSectionDelimited()
        {
            var text =
@"/** <![CDATA[ this is a test
  * of &some; cdata
  * ""']]<>/></text]]>
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(5, cdata.TextTokens.Count);
            Assert.Equal(" this is a test", cdata.TextTokens[0].ToString());
            Assert.Equal("\r\n", cdata.TextTokens[1].ToString());
            Assert.Equal(" of &some; cdata", cdata.TextTokens[2].ToString());
            Assert.Equal("\r\n", cdata.TextTokens[3].ToString());
            Assert.Equal(" \"']]<>/></text", cdata.TextTokens[4].ToString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestIncompleteEOFCDataSection()
        {
            var text = "/// <![CDATA[ incomplete"; // end of file
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(1, cdata.ErrorsAndWarnings().Length);
            Assert.Equal(1, cdata.TextTokens.Count);
            Assert.Equal(" incomplete", cdata.TextTokens[0].ToString());
        }

        [ClrOnlyFact]
        public void TestIncompleteEOLCDataSection()
        {
            var text = @"/// <![CDATA[ incomplete
class C { }"; // end of line/comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <![CDATA[ incomplete\r\n", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(1, cdata.ErrorsAndWarnings().Length);
            Assert.Equal(2, cdata.TextTokens.Count);
            Assert.Equal(" incomplete", cdata.TextTokens[0].ToString());
            Assert.Equal("\r\n", cdata.TextTokens[1].ToString());
        }

        [Fact]
        public void TestIncompleteEOLCDataSection_OtherNewline()
        {
            Assert.True(SyntaxFacts.IsNewLine('\u0085'));
            var text = "/// <![CDATA[ incomplete\u0085class C { }"; // end of line/comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <![CDATA[ incomplete\u0085", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(1, cdata.ErrorsAndWarnings().Length);
            Assert.Equal(2, cdata.TextTokens.Count);
            Assert.Equal(" incomplete", cdata.TextTokens[0].ToString());
            Assert.Equal("\u0085", cdata.TextTokens[1].ToString());
        }

        [Fact]
        public void TestIncompleteDelimitedCDataSection()
        {
            var text = "/** <![CDATA[ incomplete*/"; // end of comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlCDataSection, doc.Content[1].Kind());
            var cdata = (XmlCDataSectionSyntax)doc.Content[1];
            Assert.Equal(1, cdata.ErrorsAndWarnings().Length);
            Assert.Equal(1, cdata.TextTokens.Count);
            Assert.Equal(" incomplete", cdata.TextTokens[0].ToString());
        }

        [ClrOnlyFact]
        public void TestComment()
        {
            var text =
@"/// <!-- this is a test
/// of &some; comment
/// ""']]<>/></text-->";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlComment, doc.Content[1].Kind());
            var comment = (XmlCommentSyntax)doc.Content[1];
            Assert.Equal(5, comment.TextTokens.Count);
            Assert.Equal(" this is a test", comment.TextTokens[0].ToString());
            Assert.Equal("\r\n", comment.TextTokens[1].ToString());
            Assert.Equal(" of &some; comment", comment.TextTokens[2].ToString());
            Assert.Equal("\r\n", comment.TextTokens[3].ToString());
            Assert.Equal(" \"']]<>/></text", comment.TextTokens[4].ToString());
        }

        [ClrOnlyFact]
        public void TestCommentDelimited()
        {
            var text =
@"/** <!-- this is a test
  * of &some; comment
  * ""']]<>/></text-->
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlComment, doc.Content[1].Kind());
            var comment = (XmlCommentSyntax)doc.Content[1];
            Assert.Equal(5, comment.TextTokens.Count);
            Assert.Equal(" this is a test", comment.TextTokens[0].ToString());
            Assert.Equal("\r\n", comment.TextTokens[1].ToString());
            Assert.Equal(" of &some; comment", comment.TextTokens[2].ToString());
            Assert.Equal("\r\n", comment.TextTokens[3].ToString());
            Assert.Equal(" \"']]<>/></text", comment.TextTokens[4].ToString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestIncompleteEOFComment()
        {
            var text = "/// <!-- incomplete"; // end of file
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlComment, doc.Content[1].Kind());
            var comment = (XmlCommentSyntax)doc.Content[1];
            Assert.Equal(1, comment.ErrorsAndWarnings().Length);
            Assert.Equal(1, comment.TextTokens.Count);
            Assert.Equal(" incomplete", comment.TextTokens[0].ToString());
        }

        [ClrOnlyFact]
        public void TestIncompleteEOLComment()
        {
            var text = @"/// <!-- incomplete
class C { }"; // end of line/comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <!-- incomplete\r\n", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlComment, doc.Content[1].Kind());
            var comment = (XmlCommentSyntax)doc.Content[1];
            Assert.Equal(1, comment.ErrorsAndWarnings().Length);
            Assert.Equal(2, comment.TextTokens.Count);
            Assert.Equal(" incomplete", comment.TextTokens[0].ToString());
            Assert.Equal("\r\n", comment.TextTokens[1].ToString());
        }

        [Fact]
        public void TestIncompleteDelimitedComment()
        {
            var text = "/** <!-- incomplete*/"; // end of comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlComment, doc.Content[1].Kind());
            var comment = (XmlCommentSyntax)doc.Content[1];
            Assert.Equal(1, comment.ErrorsAndWarnings().Length);
            Assert.Equal(1, comment.TextTokens.Count);
            Assert.Equal(" incomplete", comment.TextTokens[0].ToString());
        }

        [ClrOnlyFact]
        public void TestProcessingInstruction()
        {
            var text =
@"/// <?ProcessingInstruction this is a test
/// of &a; ProcessingInstruction /// */ /**
/// ""']]>/>?</text?>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlProcessingInstruction, doc.Content[1].Kind());
            var ProcessingInstruction = (XmlProcessingInstructionSyntax)doc.Content[1];
            Assert.Null(ProcessingInstruction.Name.Prefix);
            Assert.Equal("ProcessingInstruction", ProcessingInstruction.Name.LocalName.Text);
            Assert.Equal(5, ProcessingInstruction.TextTokens.Count);
            Assert.Equal(" this is a test", ProcessingInstruction.TextTokens[0].ToString());
            Assert.Equal("\r\n", ProcessingInstruction.TextTokens[1].ToString());
            Assert.Equal(" of &a; ProcessingInstruction /// */ /**", ProcessingInstruction.TextTokens[2].ToString());
            Assert.Equal("\r\n", ProcessingInstruction.TextTokens[3].ToString());
            Assert.Equal(" \"']]>/>?</text", ProcessingInstruction.TextTokens[4].ToString());
        }

        [ClrOnlyFact]
        public void TestProcessingInstructionDelimited()
        {
            var text =
@"/** <?prefix:localname this is a test <!--
  * of &a; ProcessingInstruction
  * ""']]>/></text>]]>?>
  */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlProcessingInstruction, doc.Content[1].Kind());
            var ProcessingInstruction = (XmlProcessingInstructionSyntax)doc.Content[1];
            Assert.Equal("prefix", ProcessingInstruction.Name.Prefix.Prefix.Text);
            Assert.Equal(":", ProcessingInstruction.Name.Prefix.ColonToken.Text);
            Assert.Equal("localname", ProcessingInstruction.Name.LocalName.Text);
            Assert.Equal(5, ProcessingInstruction.TextTokens.Count);
            Assert.Equal(" this is a test <!--", ProcessingInstruction.TextTokens[0].ToString());
            Assert.Equal("\r\n", ProcessingInstruction.TextTokens[1].ToString());
            Assert.Equal(" of &a; ProcessingInstruction", ProcessingInstruction.TextTokens[2].ToString());
            Assert.Equal("\r\n", ProcessingInstruction.TextTokens[3].ToString());
            Assert.Equal(" \"']]>/></text>]]>", ProcessingInstruction.TextTokens[4].ToString());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [Fact]
        public void TestIncompleteEOFProcessingInstruction()
        {
            var text = "/// <?incomplete"; // end of file
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlProcessingInstruction, doc.Content[1].Kind());
            var ProcessingInstruction = (XmlProcessingInstructionSyntax)doc.Content[1];
            Assert.Null(ProcessingInstruction.Name.Prefix);
            Assert.Equal("incomplete", ProcessingInstruction.Name.LocalName.Text);
            Assert.Equal(1, ProcessingInstruction.ErrorsAndWarnings().Length);
            Assert.Equal(0, ProcessingInstruction.TextTokens.Count);
        }

        [Fact]
        public void TestIncompleteEOLProcessingInstruction_OtherNewline()
        {
            Assert.True(SyntaxFacts.IsNewLine('\u0085'));
            var text = "/// <?name incomplete\u0085class C { }"; // end of line/comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal("/// <?name incomplete\u0085", node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlProcessingInstruction, doc.Content[1].Kind());
            var ProcessingInstruction = (XmlProcessingInstructionSyntax)doc.Content[1];
            Assert.Null(ProcessingInstruction.Name.Prefix);
            Assert.Equal("name", ProcessingInstruction.Name.LocalName.Text);
            Assert.Equal(1, ProcessingInstruction.ErrorsAndWarnings().Length);
            Assert.Equal(2, ProcessingInstruction.TextTokens.Count);
            Assert.Equal(" incomplete", ProcessingInstruction.TextTokens[0].ToString());
            Assert.Equal("\u0085", ProcessingInstruction.TextTokens[1].ToString());
        }

        [Fact]
        public void TestIncompleteDelimitedProcessingInstruction()
        {
            var text = "/** <?name incomplete*/"; // end of comment
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlProcessingInstruction, doc.Content[1].Kind());
            var ProcessingInstruction = (XmlProcessingInstructionSyntax)doc.Content[1];
            Assert.Null(ProcessingInstruction.Name.Prefix);
            Assert.Equal("name", ProcessingInstruction.Name.LocalName.Text);
            Assert.Equal(1, ProcessingInstruction.ErrorsAndWarnings().Length);
            Assert.Equal(1, ProcessingInstruction.TextTokens.Count);
            Assert.Equal(" incomplete", ProcessingInstruction.TextTokens[0].ToString());
        }

        [WorkItem(899122, "DevDiv/Personal")]
        [Fact]
        public void TestIncompleteXMLComment()
        {
            var text = "/**\n"; // end of comment
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
        }

        [Fact]
        public void TestEarlyTerminationOfXmlParse()
        {
            var text =
@"/// <foo>
/// bar
/// </foo>
/// </uhoh>
///
class C { }";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(2, tree.GetCompilationUnitRoot().ChildNodesAndTokens().Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, tree.GetCompilationUnitRoot().ChildNodesAndTokens()[0].Kind());
            var classdecl = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().ChildNodesAndTokens()[0].AsNode();
            Assert.Equal("class C { }", classdecl.ToString());
            Assert.True(classdecl.HasLeadingTrivia);
            var leading = classdecl.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.NotEqual(0, node.ErrorsAndWarnings().Length);
            Assert.Equal(SyntaxKind.EndOfFileToken, tree.GetCompilationUnitRoot().ChildNodesAndTokens()[1].Kind());
        }

        [Fact]
        public void TestPredefinedXmlEntity()
        {
            var text =
@"/// &lt;";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(2, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("<", xmltext.TextTokens[1].Value);
        }

        [Fact]
        public void TestPredefinedXmlEntityDelimited()
        {
            var text =
@"/** &lt; */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(3, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("<", xmltext.TextTokens[1].Value);
            Assert.Equal(" ", xmltext.TextTokens[2].Value);
        }

        [Fact]
        public void TestHexCharacterXmlEntity()
        {
            var text =
@"/// &#x41;";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(2, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("A", xmltext.TextTokens[1].Value);
        }

        [Fact]
        public void TestHexCharacterXmlEntityDelimited()
        {
            var text =
@"/** &#x41; */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(3, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("A", xmltext.TextTokens[1].Value);
            Assert.Equal(" ", xmltext.TextTokens[2].Value);
        }

        [Fact]
        public void TestDecCharacterXmlEntity()
        {
            var text =
@"/// &#65;";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(2, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("A", xmltext.TextTokens[1].Value);
        }

        [Fact]
        public void TestDecCharacterXmlEntityDelimited()
        {
            var text =
@"/** &#65; */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(3, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("A", xmltext.TextTokens[1].Value);
            Assert.Equal(" ", xmltext.TextTokens[2].Value);
        }

        [Fact]
        public void TestLargeHexCharacterXmlEntity()
        {
            var text =
@"/// &#x1d11e;";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(2, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("\U0001D11E", xmltext.TextTokens[1].Value);
        }

        [Fact]
        public void TestLargeHexCharacterXmlEntityDelimited()
        {
            var text =
@"/** &#x1D11E; */";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(3, xmltext.TextTokens.Count);
            Assert.Equal(" ", xmltext.TextTokens[0].Value);
            Assert.Equal("\U0001D11E", xmltext.TextTokens[1].Value);
            Assert.Equal(" ", xmltext.TextTokens[2].Value);
        }

        [Fact]
        public void TestXmlEntityUndefined()
        {
            var text =
@"///&#abcdef;";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
        }

        [Fact]
        public void TestXmlAttributeLessThan()
        {
            var text =
@"///<foo attr=""less<than"" />";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
        }

        [Fact]
        public void TestXmlCommentDashDash()
        {
            var text =
@"///<!-- A Comment with -- -->";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
        }

        [Fact]
        public void TestXmlElementMismatch()
        {
            var text =
@"///< foo > </ bar >";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
        }

        [Fact]
        public void TestXmlElementDuplicateAttributes()
        {
            var text =
@"///< foo x = ""bar"" x = ""baz"" ";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
        }

        [Fact]
        public void TestPredefinedXmlEntityInAttribute()
        {
            var text =
@"/// <foo a="" &lt; ""/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            var attribute = (XmlTextAttributeSyntax)element.Attributes[0];
            Assert.Equal(3, attribute.TextTokens.Count);
            Assert.Equal(" ", attribute.TextTokens[0].Value);
            Assert.Equal("<", attribute.TextTokens[1].Value);
            Assert.Equal(" ", attribute.TextTokens[2].Value);
        }

        [Fact]
        public void TestPredefinedXmlEntityInAttributeDelimited()
        {
            var text =
@"/** <foo a="" &lt; ""/>*/";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.True(doc.Content[0].HasLeadingTrivia);
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            var attribute = (XmlTextAttributeSyntax)element.Attributes[0];
            Assert.Equal(3, attribute.TextTokens.Count);
            Assert.Equal(" ", attribute.TextTokens[0].Value);
            Assert.Equal("<", attribute.TextTokens[1].Value);
            Assert.Equal(" ", attribute.TextTokens[2].Value);
        }

        [WorkItem(899590, "DevDiv/Personal")]
        [Fact]
        public void TestLessThanInAttributeTextIsError()
        {
            var text = @"/// <foo a = '<>'/>";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(2, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.Equal(SyntaxKind.XmlEmptyElement, doc.Content[1].Kind());
            var element = (XmlEmptyElementSyntax)doc.Content[1];
            Assert.Equal(1, element.Attributes.Count);
            Assert.NotEqual(0, element.Attributes[0].ErrorsAndWarnings().Length);
        }

        [WorkItem(899559, "DevDiv/Personal")]
        [ClrOnlyFact]
        public void TestNoZeroWidthTrivia()
        {
            var text =
@"/**
x
*/";
            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
            var leading = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            var node = leading[0];
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, node.Kind());
            Assert.Equal(text, node.ToFullString());
            var doc = (DocumentationCommentTriviaSyntax)node.GetStructure();
            Assert.Equal(1, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            var xmltext = (XmlTextSyntax)doc.Content[0];
            Assert.Equal(3, xmltext.ChildNodesAndTokens().Count);
            Assert.Equal(SyntaxKind.XmlTextLiteralNewLineToken, xmltext.ChildNodesAndTokens()[0].Kind());
            Assert.True(xmltext.ChildNodesAndTokens()[0].HasLeadingTrivia);
            Assert.Equal("\r\n", xmltext.ChildNodesAndTokens()[0].ToString());
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, xmltext.ChildNodesAndTokens()[1].Kind());
            Assert.False(xmltext.ChildNodesAndTokens()[1].HasLeadingTrivia);
            Assert.Equal("x", xmltext.ChildNodesAndTokens()[1].ToString());
            Assert.Equal(SyntaxKind.XmlTextLiteralNewLineToken, xmltext.ChildNodesAndTokens()[2].Kind());
            Assert.False(xmltext.ChildNodesAndTokens()[2].HasLeadingTrivia);
            Assert.Equal("\r\n", xmltext.ChildNodesAndTokens()[2].ToString());
        }

        [WorkItem(906364, "DevDiv/Personal")]
        [Fact]
        public void TestXmlAttributeWithoutEqualSign()
        {
            var text = @"/// <foo a""as""> </foo>";

            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get a warning about the = token missing
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);

            // we expect one warning
            VerifyDiagnostics(tree.GetCompilationUnitRoot(), new List<TestError>() { new TestError(1570, true) });
        }

        [WorkItem(906367, "DevDiv/Personal")]
        [Fact]
        public void TestXmlAttributeWithoutWhitespaceSeparators()
        {
            var text = @"/// <foo a=""as""b=""as""> </foo>";

            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get a warning about the = token missing
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);

            // we expect one warning
            VerifyDiagnostics(tree.GetCompilationUnitRoot(), new List<TestError>() { new TestError(1570, true) });
        }

        [Fact]
        public void TestSingleLineXmlCommentBetweenRegularComments()
        {
            var text = @"//Comment
/// <foo a=""as""> </foo>
//Comment
";

            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // grab the trivia off the EOF token
            var trivias = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();

            // we should have 5 trivias
            Assert.Equal(5, trivias.Count);

            // we verify that the regular comments are also there
            Assert.False(trivias[0].HasStructure);
            Assert.False(trivias[4].HasStructure);

            // the 3rd one should be XmlDocComment
            Assert.True(trivias[2].HasStructure);
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[2].GetStructure().GetType());
            var doc = trivias[2].GetStructure() as DocumentationCommentTriviaSyntax;

            // we validate the xml comment
            var xmlElement = doc.Content[1] as XmlElementSyntax;

            // we verify the content of the tag
            VerifyXmlElement(xmlElement, "foo", " ");
            VerifyXmlAttributes(xmlElement.StartTag.Attributes, new Dictionary<string, string>() { { "a", "as" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentAfterMultilineXmlComment()
        {
            var text = @"/** <bar a='val'> 
* text
* </bar>
*/

/// <foo a=""as""> </foo>
";

            var tree = Parse(text);
            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // grab the trivia off the EOF token
            var trivias = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();

            // we should have 4 trivias
            Assert.Equal(4, trivias.Count);

            // we should also have two xml comments
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[3].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;
            var secondComment = trivias[3].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[1] as XmlElementSyntax, "bar", @" 
* text
* ");
            VerifyXmlAttributes((firstComment.Content[1] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "val" } });

            VerifyXmlElement(secondComment.Content[1] as XmlElementSyntax, "foo", " ");
            VerifyXmlAttributes((secondComment.Content[1] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "as" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentAfterInvalidMultilineXmlComment()
        {
            var text = @"/** <bar a='val'> 
* text
*/

/// <foo a=""as""> </foo>
";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we expect 1 warning
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);

            // grab the trivia off the EOF token
            var trivias = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia();

            // we should have 4 trivias
            Assert.Equal(4, trivias.Count);

            // we should also have two xml comments
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[3].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;
            var secondComment = trivias[3].GetStructure() as DocumentationCommentTriviaSyntax;

            // we validate that the error is on the firstComment node
            firstComment.GetDiagnostics().Verify(
                // (3,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'bar'.'
                // */
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("bar"));

            // verify that the xml elements contain the right info
            VerifyXmlElement(secondComment.Content[1] as XmlElementSyntax, "foo", " ");
            VerifyXmlAttributes((secondComment.Content[1] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "as" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforeMethodDecl()
        {
            var text = @"class C{
///<foo a=""val""/>
  void Foo(){}
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the void keyword
            Assert.Equal(typeof(MethodDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            var keyword = ((tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as MethodDeclarationSyntax).ReturnType;

            var trivias = keyword.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(2, trivias.Count);

            // we should also have one comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlEmptyElementSyntax, "foo");
            VerifyXmlAttributes((firstComment.Content[0] as XmlEmptyElementSyntax).Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforeGenericMethodDecl()
        {
            var text = @"class C{
///<foo a=""val""> </foo>
  void Foo<T>(){}
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the void keyword
            Assert.Equal(typeof(MethodDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            var keyword = ((tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as MethodDeclarationSyntax).ReturnType;

            var trivias = keyword.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(2, trivias.Count);

            // we should also have one comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", " ");
            VerifyXmlAttributes((firstComment.Content[0] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforePropertyDecl()
        {
            var text = @"class C{
///<foo a=""val""/>
  int Foo{get;set;}
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the void keyword
            Assert.Equal(typeof(PropertyDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            var keyword = ((tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as PropertyDeclarationSyntax).Type;

            var trivias = keyword.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(2, trivias.Count);

            // we should also have one comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlEmptyElementSyntax, "foo");
            VerifyXmlAttributes((firstComment.Content[0] as XmlEmptyElementSyntax).Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforeIndexerDecl()
        {
            var text = @"class C{
///<foo a=""val""/>
  int this[int x] { get { return 1; } set { } }
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the void keyword
            Assert.Equal(typeof(IndexerDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            var keyword = ((tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as IndexerDeclarationSyntax).Type;

            var trivias = keyword.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(2, trivias.Count);

            // we should also have one comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlEmptyElementSyntax, "foo");
            VerifyXmlAttributes((firstComment.Content[0] as XmlEmptyElementSyntax).Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [WorkItem(906381, "DevDiv/Personal")]
        [Fact]
        public void TestMultiLineXmlCommentBeforeGenericTypeParameterOnMethodDecl()
        {
            var text = @"class C {
    void Foo</**<foo>test</foo>*/T>() { }
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // do we parsed a method?
            Assert.Equal(typeof(MethodDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            // we grab the open bracket for the Foo method decl
            var method = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as MethodDeclarationSyntax;
            var typeParameter = method.TypeParameterList.Parameters.Single();

            var trivias = typeParameter.GetLeadingTrivia();

            // we should have 1 trivia
            Assert.Equal(1, trivias.Count);

            // we should also have one XML comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", "test");

            // we don't have any attributes
            Assert.Equal(0, (firstComment.Content[0] as XmlElementSyntax).StartTag.Attributes.Count);
        }

        [WorkItem(906381, "DevDiv/Personal")]
        [Fact]
        public void TestMultiLineXmlCommentBeforeGenericTypeParameterOnClassDecl()
        {
            var text = @"class C</**<foo>test</foo>*/T>{}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // do we parsed a method?
            Assert.Equal(typeof(ClassDeclarationSyntax), tree.GetCompilationUnitRoot().Members[0].GetType());

            // we grab the open bracket for the Foo method decl
            var typeParameter = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).TypeParameterList.Parameters.Single();

            var trivias = typeParameter.GetLeadingTrivia();

            // we should have 1 trivia
            Assert.Equal(1, trivias.Count);

            // we should also have one XML comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", "test");

            // we don't have any attributes
            Assert.Equal(0, (firstComment.Content[0] as XmlElementSyntax).StartTag.Attributes.Count);
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforeIncompleteGenericMethodDecl()
        {
            var text = @"class C{
///<foo a=""val""> </foo>
  void Foo<T(){}
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().Errors().Length); // 4 errors because of the incomplete class decl

            // we grab the void keyword
            Assert.Equal(typeof(MethodDeclarationSyntax), (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0].GetType());

            var keyword = ((tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Members[0] as MethodDeclarationSyntax).ReturnType;

            var trivias = keyword.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(2, trivias.Count);

            // we should also have one comment
            Assert.Equal(0, trivias[0].Errors().Length);
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", " ");
            VerifyXmlAttributes((firstComment.Content[0] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentAfterMethodDecl()
        {
            var text = @"class C{
  void Foo(){}
///<foo a=""val""/>
}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            var bracket = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).CloseBraceToken;

            var trivias = bracket.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(1, trivias.Count);

            // we should also have one comment
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlEmptyElementSyntax, "foo");
            VerifyXmlAttributes((firstComment.Content[0] as XmlEmptyElementSyntax).Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentAfterIncompleteMethodDecl()
        {
            var text = @"class C{
  void Foo({}
///<foo a=""val""> </foo>
}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().Errors().Length); // error because of the incomplete class decl

            // we grab the close bracket for the class
            var classDecl = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var bracket = classDecl.CloseBraceToken;

            var trivias = bracket.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(1, trivias.Count);

            // we should also have one comment
            Assert.Equal(0, trivias[0].Errors().Length);
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", " ");
            VerifyXmlAttributes((firstComment.Content[0] as XmlElementSyntax).StartTag.Attributes, new Dictionary<string, string>() { { "a", "val" } });
        }

        [Fact]
        public void TestSingleLineXmlCommentBeforePreprocessorDirective()
        {
            var text = @"///<foo></foo>
# if DOODAD
# endif";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length); // 4 errors because of the incomplete class decl

            // we grab the close bracket from the OEF token
            var bracket = tree.GetCompilationUnitRoot().EndOfFileToken;

            var trivias = bracket.GetLeadingTrivia();

            // we should have 3 trivias
            Assert.Equal(3, trivias.Count);

            Assert.Equal(0, trivias[0].Errors().Length);
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", string.Empty);
        }

        [Fact]
        public void TestSingleLineXmlCommentAfterPreprocessorDirective()
        {
            var text = @"# if DOODAD
# endif
///<foo></foo>";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length); // 4 errors because of the incomplete class decl

            // we grab the close bracket from the OEF token
            var bracket = tree.GetCompilationUnitRoot().EndOfFileToken;

            var trivias = bracket.GetLeadingTrivia();

            // we should have 3 trivias
            Assert.Equal(3, trivias.Count);

            Assert.Equal(0, trivias[0].Errors().Length);
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[2].GetStructure().GetType());

            // we grab the xml comments
            var firstComment = trivias[2].GetStructure() as DocumentationCommentTriviaSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(firstComment.Content[0] as XmlElementSyntax, "foo", string.Empty);
        }

        [Fact]
        public void TestSingleLineXmlCommentInsideMultiLineXmlComment()
        {
            var text = @"/** <foo> 
* /// <bar> </bar>
* </foo>
*/";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the close bracket from the OEF token
            var bracket = tree.GetCompilationUnitRoot().EndOfFileToken;

            var trivias = bracket.GetLeadingTrivia();

            // we should have 2 trivias
            Assert.Equal(1, trivias.Count);
            Assert.Equal(0, trivias[0].Errors().Length);

            // make sure that the external node exists
            Assert.Equal(typeof(DocumentationCommentTriviaSyntax), trivias[0].GetStructure().GetType());

            // we grab the xml comments
            var outerComment = (trivias[0].GetStructure() as DocumentationCommentTriviaSyntax).Content[1] as XmlElementSyntax;
            var innerComment = outerComment.Content[1] as XmlElementSyntax;

            // verify that the xml elements contain the right info
            VerifyXmlElement(outerComment, "foo", @" 
* /// <bar> </bar>
* ");

            VerifyXmlElement(innerComment, "bar", " ");
        }

        [WorkItem(906500, "DevDiv/Personal")]
        [Fact]
        public void TestIncompleteMultiLineXmlComment()
        {
            var text = @"/** <foo/>";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we expect one warning
            Assert.Equal(1, tree.GetCompilationUnitRoot().Errors().Length);
            VerifyDiagnostics(tree.GetCompilationUnitRoot(), new List<TestError>() { new TestError(1035, false) });
        }

        [Fact]
        public void TestSingleLineXmlCommentWithMultipleAttributes()
        {
            var text = @"///<foo attr1=""a"" attr2=""b"" attr3=""test""> </foo>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            Assert.Equal(1, trivias.Count);

            // we verify that we parsed a correct XML element
            VerifyXmlElement((trivias[0].GetStructure() as DocumentationCommentTriviaSyntax).Content[0] as XmlElementSyntax, "foo", " ");

            VerifyXmlAttributes(((trivias[0].GetStructure() as DocumentationCommentTriviaSyntax).Content[0] as XmlElementSyntax).StartTag.Attributes,
                new Dictionary<string, string>() { { "attr1", "a" }, { "attr2", "b" }, { "attr3", "test" } });
        }

        [Fact]
        public void TestNestedXmlTagsInsideSingleLineXmlDocComment()
        {
            var text = @"///<foo>
/// <bar>
///  <baz attr=""a"">
///  </baz>
/// </bar>
///</foo>";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            // we grab the top trivia.
            var eofToken = tree.GetCompilationUnitRoot().EndOfFileToken;

            var topTrivias = eofToken.GetLeadingTrivia();
            Assert.Equal(1, topTrivias.Count);

            var doc = topTrivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            var topTriviaElement = doc.Content[0] as XmlElementSyntax;
            VerifyXmlElement(topTriviaElement, "foo", @"
/// <bar>
///  <baz attr=""a"">
///  </baz>
/// </bar>
");
            var secondLevelTrivia = topTriviaElement.Content[1] as XmlElementSyntax;
            VerifyXmlElement(secondLevelTrivia, "bar", @"
///  <baz attr=""a"">
///  </baz>
/// ");

            var thirdLevelTrivia = secondLevelTrivia.Content[1] as XmlElementSyntax;
            VerifyXmlElement(thirdLevelTrivia, "baz", @"
///  ");
            VerifyXmlAttributes(thirdLevelTrivia.StartTag.Attributes, new Dictionary<string, string>() { { "attr", "a" } });
        }

        [Fact]
        public void TestMultiLineXmlCommentWithNestedTagThatContainsCDATA()
        {
            var text = @"/**
<foo>
  <bar> <![CDATA[ Some text
 ]]> </bar>
</foo>
*/";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);

            var eofToken = tree.GetCompilationUnitRoot().EndOfFileToken;

            var trivias = eofToken.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            var topNode = doc.Content[1] as XmlElementSyntax;
            VerifyXmlElement(topNode, "foo", @"
  <bar> <![CDATA[ Some text
 ]]> </bar>
");
            var secondLevel = topNode.Content[1] as XmlElementSyntax;
            VerifyXmlElement(secondLevel, "bar", @" <![CDATA[ Some text
 ]]> ");

            // verify the CDATA content
            var cdata = secondLevel.Content[1];
            var actual = (cdata as XmlCDataSectionSyntax).TextTokens.ToFullString();
            Assert.Equal(@" Some text
", actual);
        }

        [Fact]
        public void TestSingleLineXmlCommentWithMismatchedUpperLowerCaseTagName()
        {
            var text = @"///<foo> </Foo>";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get a warning
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);

            // we get to the xml trivia
            var eofToken = tree.GetCompilationUnitRoot().EndOfFileToken;
            var trivias = eofToken.GetLeadingTrivia();

            // we got a trivia
            Assert.Equal(1, trivias.Count);

            VerifyDiagnostics(trivias[0], new List<TestError>() { new TestError(1570, true) });
        }

        [WorkItem(906704, "DevDiv/Personal")]
        [ClrOnlyFact]
        public void TestSingleLineXmlCommentWithMissingStartTag()
        {
            var text = @"///</Foo>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);

            // we get to the xml trivia
            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            // we get the xmldoc comment
            var doc = classKeyword.GetLeadingTrivia()[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // we get the xmlText
            var xmlText = doc.Content[0] as XmlTextSyntax;

            // we have an error on that node
            VerifyDiagnostics(xmlText, new List<TestError>() { new TestError(1570, true) });

            // we should get just 2 nodes
            Assert.Equal(2, xmlText.TextTokens.Count);

            Assert.Equal("///</Foo>\r\n", xmlText.TextTokens.ToFullString());
        }

        [WorkItem(906719, "DevDiv/Personal")]
        [Fact]
        public void TestMultiLineXmlCommentWithMissingStartTag()
        {
            var text = @"/**</Foo>*/
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(1, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);

            // we get to the xml trivia
            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            // we get the xmldoc comment
            var doc = classKeyword.GetLeadingTrivia()[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // we get the xmlText
            var xmlText = doc.Content[0] as XmlTextSyntax;

            // we have an error on that node
            VerifyDiagnostics(xmlText, new List<TestError>() { new TestError(1570, true) });

            // we should get just 2 nodes
            Assert.Equal(1, xmlText.TextTokens.Count);

            Assert.Equal("/**</Foo>", xmlText.TextTokens.ToFullString());
        }

        [Fact]
        public void TestSingleLineXmlCommentWithMissingEndTag()
        {
            var text = @"///<Foo>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            doc.GetDiagnostics().Verify(
                // (2,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'Foo'.'
                // class C{}
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("Foo"));
        }

        [WorkItem(906752, "DevDiv/Personal")]
        [Fact]
        public void TestMultiLineXmlCommentWithMissingEndTag()
        {
            var text = @"/**<Foo>*/
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 1 warning
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            doc.GetDiagnostics().Verify(
                // (1,9): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'Foo'.'
                // /**<Foo>*/
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("Foo"));
        }

        [Fact]
        public void TestMultiLineXmlCommentWithInterleavedTags()
        {
            var text = @"/**<foo>
<bar></foo>
</bar>*/
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 2 warnings
            Assert.Equal(2, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // we have an error on that node
            VerifyDiagnostics(doc, new List<TestError>() { new TestError(1570, true), new TestError(1570, true) });
        }

        [Fact]
        public void TestSingleLineXmlCommentWithInterleavedTags()
        {
            var text = @"///<foo>
///<bar></foo>
///</bar>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 2 warnings
            Assert.Equal(2, tree.GetCompilationUnitRoot().Warnings().Length);

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // we have an error on that node
            VerifyDiagnostics(doc, new List<TestError>() { new TestError(1570, true), new TestError(1570, true) });
        }

        [Fact]
        public void TestMultiLineXmlCommentWithIncompleteInterleavedTags()
        {
            var text = @"/**<foo>
<bar></foo>
*/
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.LeadingTrivia;

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            doc.GetDiagnostics().Verify(
                // (2,8): warning CS1570: XML comment has badly formed XML -- 'End tag 'foo' does not match the start tag 'bar'.'
                // <bar></foo>
                Diagnostic(ErrorCode.WRN_XMLParseError, "foo").WithArguments("foo", "bar"),
                // (3,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'foo'.'
                // */
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("foo"));
        }

        [Fact]
        public void TestSingleLineXmlCommentWithIncompleteInterleavedTags()
        {
            var text = @"///<foo>
///<bar></foo>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var trivias = classKeyword.GetLeadingTrivia();

            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            doc.GetDiagnostics().Verify(
                // (2,11): warning CS1570: XML comment has badly formed XML -- 'End tag 'foo' does not match the start tag 'bar'.'
                // ///<bar></foo>
                Diagnostic(ErrorCode.WRN_XMLParseError, "foo").WithArguments("foo", "bar"),
                // (3,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'foo'.'
                // class C{}
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("foo"));
        }

        [Fact]
        public void TestMultiLineXmlCommentWithMultipleStartTokens()
        {
            var text = @"/** <a 
<b 
*/";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 2 warnings
            Assert.Equal(2, tree.GetCompilationUnitRoot().Warnings().Length);
        }

        [Fact]
        public void TestMultiLineXmlCommentWithMultipleEndTags()
        {
            var text = @"/** <a> </a> </a> 

*/";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 2 warnings
            Assert.Equal(1, tree.GetCompilationUnitRoot().Warnings().Length);
        }

        [Fact]
        public void TestMultiLineXmlCommentWithMultipleEndTags2()
        {
            var text = @"/** <a> </b> </a> 
*/";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            // we should get 2 warnings
            Assert.Equal(2, tree.GetCompilationUnitRoot().Warnings().Length);
        }

        [WorkItem(906814, "DevDiv/Personal")]
        [Fact]
        public void TestSingleLineXmlCommentWithInvalidStringAttributeValue()
        {
            var text = @"///<foo a=""</>""> </foo> 
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var doc = classKeyword.GetLeadingTrivia()[0].GetStructure() as DocumentationCommentTriviaSyntax;

            Assert.Equal(typeof(XmlElementSyntax), doc.Content[0].GetType());
        }

        [WorkItem(537113, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537113")]
        [Fact]
        public void TestSingleLineXmlCommentWithAttributeWithoutQuotes()
        {
            var text = @"///<foo a=4></foo>
class C{}";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;

            var doc = classKeyword.GetLeadingTrivia()[0].GetStructure() as DocumentationCommentTriviaSyntax;

            // we should still get an XmlElement
            Assert.IsType(typeof(XmlElementSyntax), doc.Content[0]);
        }

        [WorkItem(926873, "DevDiv/Personal")]
        [Fact]
        public void TestSomeXmlEntities()
        {
            var text = @"/// <doc>
/// <line>&#1631;</line>
/// <line>&#x65f;</line>
/// </doc>
class A {}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(0, tree.GetCompilationUnitRoot().Errors().Length);
        }

        [WorkItem(926683, "DevDiv/Personal")]
        [Fact]
        public void TestSomeBadXmlEntities()
        {
            var text = @"/// &#1;<doc1>&#2;</doc1>
/// <doc2><![CDATA[&#5;&#31;]]></doc2>
/// <doc3 x = ""&#14;""></doc3>&#xffff;
/// <!-- &#xfffe; -->
class A {}
";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(4, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            VerifyDiagnostics(tree.GetCompilationUnitRoot(), new List<TestError>
            {
                    new TestError(1570, true),
                    new TestError(1570, true),
                    new TestError(1570, true),
                    new TestError(1570, true)
            });
        }

        [WorkItem(926804, "DevDiv/Personal")]
        [Fact]
        public void TestSomeBadWhitespaceInTags()
        {
            var text = @"/// < doc></doc>
/// <abc> </ abc>
/// < a/>
/// <
///  b></b>
class A {}
";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());
            Assert.Equal(4, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            VerifyDiagnostics(tree.GetCompilationUnitRoot(), new List<TestError>
            {
                    new TestError(1570, true),
                    new TestError(1570, true),
                    new TestError(1570, true),
                    new TestError(1570, true)
            });
        }

        [WorkItem(926807, "DevDiv/Personal")]
        [Fact]
        public void TestCDataEndTagInXmlText()
        {
            var text = @"/// <doc> ]]> </doc>
/// <a>abc]]]>def</a>
/// <a attr=""]]>""></a>
class A {}";

            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;
            var trivias = classKeyword.GetLeadingTrivia();
            var doc = trivias[0].GetStructure() as DocumentationCommentTriviaSyntax;

            Assert.Equal(2, doc.ErrorsAndWarnings().Length);
            VerifyDiagnostics(doc, new List<TestError>() { new TestError(1570, true), new TestError(1570, true) });
        }

        [WorkItem(536748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536748")]
        [Fact]
        public void AttributesInEndTag()
        {
            var text = @"
/// <summary attr=""A"">
/// </summary attr=""A"">
class A
{ 
}
";
            var tree = Parse(text);

            Assert.NotNull(tree);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var classKeyword = (tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax).Keyword;
            var trivias = classKeyword.GetLeadingTrivia();
            var doc = trivias[1].GetStructure() as DocumentationCommentTriviaSyntax;

            Assert.Equal(3, doc.Content.Count);
            Assert.Equal(SyntaxKind.XmlText, doc.Content[0].Kind());
            Assert.Equal(SyntaxKind.XmlElement, doc.Content[1].Kind());
            Assert.Equal(SyntaxKind.XmlText, doc.Content[2].Kind());
        }

        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void NonAsciiQuotationMarks()
        {
            var text = @"
class A
{
    /// <see cref=”A()”/>
    /// <param name=”x”/>
    /// <other attr=”value”/>
    void M(int x) { }
}";

            var tree = Parse(text);
            tree.GetDiagnostics().Verify(
                // (4,19): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <see cref=”A()”/>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (4,23): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <see cref=”A()”/>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),

                // (5,21): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”x”/>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (5,23): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”x”/>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),

                // What's happening with the text attribute is that "”/>" is correctly (if unintuitively) being consumed as part of the
                // attribute value.  It then complains about the missing closing quotation mark and '/>'.

                // (6,21): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <other attr=”value”/>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (7,1): warning CS1570: XML comment has badly formed XML -- 'Missing closing quotation mark for string literal.'
                //     void M(int x) { }
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (7,1): warning CS1570: XML comment has badly formed XML -- 'Expected '>' or '/>' to close tag 'other'.'
                //     void M(int x) { }
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("other"));
        }

        [WorkItem(546989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546989")]
        [Fact]
        public void Microsoft_TeamFoundation_Client_Dll()
        {
            var text = @"
/// <summary></summary>
public class Program
{
     static void Main() { }
    /// <summary>
    /// GetEntityConnectionString from the selected path
    /// path is of the format <project name>\<nodename>\<nodename>
    /// </summary>
    /// <param name=”metadata”></param>
    /// <param name=”provider”></param>
    protected void GetEntityConnectionString(
        string metadata,
        string provider)
    {
    }
}";

            var tree = Parse(text);
            tree.GetDiagnostics().Verify(
                // (8,44): warning CS1570: XML comment has badly formed XML -- 'Missing equals sign between attribute and attribute value.'
                //     /// path is of the format <project name>\<nodename>\<nodename>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (9,11): warning CS1570: XML comment has badly formed XML -- 'End tag 'summary' does not match the start tag 'nodename'.'
                //     /// </summary>
                Diagnostic(ErrorCode.WRN_XMLParseError, "summary").WithArguments("summary", "nodename"),
                // (10,21): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”metadata”></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (10,30): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”metadata”></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (11,21): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”provider”></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (11,30): warning CS1570: XML comment has badly formed XML -- 'Non-ASCII quotations marks may not be used around string literals.'
                //     /// <param name=”provider”></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, ""),
                // (12,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'nodename'.'
                //     protected void GetEntityConnectionString(
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("nodename"),
                // (12,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'project'.'
                //     protected void GetEntityConnectionString(
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("project"),
                // (12,1): warning CS1570: XML comment has badly formed XML -- 'Expected an end tag for element 'summary'.'
                //     protected void GetEntityConnectionString(
                Diagnostic(ErrorCode.WRN_XMLParseError, "").WithArguments("summary"));
        }

        [WorkItem(547188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547188")]
        [Fact]
        public void WhitespaceInXmlName()
        {
            var text = @"
/// <A:B/>
/// <A: B/>
/// <A :B/>
/// <A : B/>
public class Program
{
}";

            var tree = Parse(text);
            tree.GetDiagnostics().Verify(
                // (3,8): warning CS1570: XML comment has badly formed XML -- 'Whitespace is not allowed at this location.'
                // /// <A: B/>
                Diagnostic(ErrorCode.WRN_XMLParseError, " "),
                // (4,7): warning CS1570: XML comment has badly formed XML -- 'Whitespace is not allowed at this location.'
                // /// <A :B/>
                Diagnostic(ErrorCode.WRN_XMLParseError, " "),
                // (5,7): warning CS1570: XML comment has badly formed XML -- 'Whitespace is not allowed at this location.'
                // /// <A : B/>
                Diagnostic(ErrorCode.WRN_XMLParseError, " "),
                // (5,9): warning CS1570: XML comment has badly formed XML -- 'Whitespace is not allowed at this location.'
                // /// <A : B/>
                Diagnostic(ErrorCode.WRN_XMLParseError, " "));
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestDocumentationComment()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// This class provides extension methods for the <see cref=\"TypeName\"/> class.\r\n" +
                "/// </summary>\r\n" +
                "/// <threadsafety static=\"true\" instance=\"false\"/>\r\n" +
                "/// <preliminary/>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods for the "),
                    SyntaxFactory.XmlSeeElement(
                        SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText(" class."),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlThreadSafetyElement(),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlPreliminaryElement());

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlSummaryElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// This class provides extension methods.\r\n" +
                "/// </summary>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods."),
                    SyntaxFactory.XmlNewLine("\r\n")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlSeeElementAndXmlSeeAlsoElement()
        {
            var expected = 
                "/// <summary>\r\n" + 
                "/// This class provides extension methods for the <see cref=\"TypeName\"/> class and the <seealso cref=\"TypeName2\"/> class.\r\n" +
                "/// </summary>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This class provides extension methods for the "),
                    SyntaxFactory.XmlSeeElement(
                        SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText(" class and the "),
                    SyntaxFactory.XmlSeeAlsoElement(
                        SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("TypeName2"))),
                    SyntaxFactory.XmlText(" class."),
                    SyntaxFactory.XmlNewLine("\r\n")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlNewLineElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// This is a summary.\r\n" +
                "/// </summary>\r\n" +
                "/// \r\n" +
                "/// \r\n" +
                "/// <remarks>\r\n" +
                "/// \r\n" +
                "/// </remarks>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("This is a summary."),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlRemarksElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlParamAndParamRefElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// <paramref name=\"b\"/>\r\n" +
                "/// </summary>\r\n" +
                "/// <param name=\"a\"></param>\r\n" +
                "/// <param name=\"b\"></param>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlParamRefElement("b"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlParamElement("a"),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlParamElement("b"));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlReturnsElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// \r\n" +
                "/// </summary>\r\n" +
                "/// <returns>\r\n" +
                "/// Returns a value.\r\n" +
                "/// </returns>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlReturnsElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("Returns a value."),
                    SyntaxFactory.XmlNewLine("\r\n")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlRemarksElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// \r\n" +
                "/// </summary>\r\n" +
                "/// <remarks>\r\n" +
                "/// Same as in class <see cref=\"TypeName\"/>.\r\n" +
                "/// </remarks>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlRemarksElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlText("Same as in class "),
                    SyntaxFactory.XmlSeeElement(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("TypeName"))),
                    SyntaxFactory.XmlText("."),
                    SyntaxFactory.XmlNewLine("\r\n")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlExceptionElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// \r\n" +
                "/// </summary>\r\n" +
                "/// <exception cref=\"InvalidOperationException\">This exception will be thrown if the object is in an invalid state when calling this method.</exception>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"), 
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlExceptionElement(
                    SyntaxFactory.TypeCref(
                        SyntaxFactory.ParseTypeName("InvalidOperationException")),
                        SyntaxFactory.XmlText("This exception will be thrown if the object is in an invalid state when calling this method.")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        [Fact]
        [Trait("Feature", "Xml Documentation Comments")]
        public void TestXmlPermissionElement()
        {
            var expected = 
                "/// <summary>\r\n" +
                "/// \r\n" +
                "/// </summary>\r\n" +
                "/// <permission cref=\"MyPermission\">Needs MyPermission to execute.</permission>";

            DocumentationCommentTriviaSyntax documentationComment = SyntaxFactory.DocumentationComment(
                SyntaxFactory.XmlSummaryElement(
                    SyntaxFactory.XmlNewLine("\r\n"),
                    SyntaxFactory.XmlNewLine("\r\n")),
                SyntaxFactory.XmlNewLine("\r\n"),
                SyntaxFactory.XmlPermissionElement(
                    SyntaxFactory.TypeCref(
                        SyntaxFactory.ParseTypeName("MyPermission")),
                    SyntaxFactory.XmlText("Needs MyPermission to execute.")));

            var actual = documentationComment.ToFullString();

            Assert.Equal<string>(expected, actual);
        }

        #region Xml Test helpers

        /// <summary>
        /// Verifies that the errors on the given CSharpSyntaxNode match the expected error codes and types
        /// </summary>
        /// <param name="node">The node that has errors</param>
        /// <param name="errors">The list of expected errors</param>
        private void VerifyDiagnostics(CSharpSyntaxNode node, List<TestError> errors)
        {
            VerifyDiagnostics(node.ErrorsAndWarnings(), errors);
        }

        private void VerifyDiagnostics(SyntaxToken token, List<TestError> errors)
        {
            VerifyDiagnostics(token.ErrorsAndWarnings(), errors);
        }

        private void VerifyDiagnostics(SyntaxTrivia trivia, List<TestError> errors)
        {
            VerifyDiagnostics(trivia.ErrorsAndWarnings(), errors);
        }

        private void VerifyDiagnostics(IEnumerable<DiagnosticInfo> actual, List<TestError> expected)
        {
            Assert.Equal(actual.Count(), expected.Count);

            var actualErrors = (from e in actual
                                orderby e.Code
                                select new TestError(e.Code, e.Severity == DiagnosticSeverity.Warning)).ToList();
            var expectedErrors = (from e in expected
                                  orderby e.ErrorCode
                                  select new TestError(e.ErrorCode, e.IsWarning)).ToList();

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expectedErrors[i].ErrorCode, actualErrors[i].ErrorCode);
                Assert.Equal(expectedErrors[i].IsWarning, actualErrors[i].IsWarning);
            }
        }

        /// <summary>
        /// Verify if a given XmlElement is correct
        /// </summary>
        /// <param name="xmlElement">The XmlElement object to validate</param>
        /// <param name="tagName">The name of the tag the XML element should have</param>
        /// <param name="innerText">The text inside the XmlElement</param>
        /// 
        private void VerifyXmlElement(XmlElementSyntax xmlElement, string tagName, string innerText)
        {
            // if the innerText is empty, then the content has no nodes.
            if (innerText == string.Empty)
            {
                Assert.Equal(0, xmlElement.Content.Count);
            }
            else
            {
                var elementInnerText = GetXmlElementText(xmlElement);
                Assert.Equal(innerText, elementInnerText);
            }

            Assert.Equal(tagName, xmlElement.StartTag.Name.LocalName.Value);
            Assert.Equal(tagName, xmlElement.EndTag.Name.LocalName.Value);
        }

        /// <summary>
        /// Gets the string representation for a XmlElementText
        /// </summary>
        /// <param name="xmlElement"></param>
        /// <returns></returns>
        private string GetXmlElementText(XmlElementSyntax xmlElement)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var element in xmlElement.Content)
            {
                if (element.GetType() == typeof(XmlElementSyntax))
                {
                    sb.Append(element.ToFullString());
                }
                else if (element.GetType() == typeof(XmlTextSyntax))
                {
                    sb.Append((element as XmlTextSyntax).TextTokens.ToFullString());
                }
                else if (element.GetType() == typeof(XmlCDataSectionSyntax))
                {
                    sb.Append(element.ToFullString());
                }
            }

            return sb.ToString();

            // return getTextFromTextTokens((xmlElement.Content[0] as XmlTextSyntax).TextTokens);
        }

        /// <summary>
        /// Verifies an empty XmlElement
        /// </summary>
        /// <param name="xmlElement">The XmlElement object to validate</param>
        /// <param name="tagName">The name of the tag the XML element should have</param>
        private void VerifyXmlElement(XmlEmptyElementSyntax xmlElement, string tagName)
        {
            Assert.Equal(tagName, xmlElement.Name.LocalName.Value);
        }

        /// <summary>
        /// Verify if the attributes for a given XML node match the expected ones
        /// </summary>
        /// <param name="xmlAttributes">The list of attributes to verify</param>
        /// <param name="attributes">The dictionary contains the key-value pair for the expected attribute values</param>
        private void VerifyXmlAttributes(SyntaxList<XmlAttributeSyntax> xmlAttributes, Dictionary<string, string> attributes)
        {
            // we have the same number of attributes
            Assert.Equal(attributes.Keys.Count, xmlAttributes.Count);
            foreach (XmlTextAttributeSyntax attribute in xmlAttributes)
            {
                // we make sure we have that attribute
                Assert.True(attributes.ContainsKey(attribute.Name.LocalName.Value as string));

                // we make sure that the value for the attribute is the right one.
                Assert.Equal(attributes[attribute.Name.LocalName.Value as string], attribute.TextTokens.ToString());
            }
        }

        /// <summary>
        /// This class is used to represent the expected errors
        /// </summary>
        private class TestError
        {
            public bool IsWarning { get; }
            public int ErrorCode { get; }

            public TestError(int code, bool warning)
            {
                this.IsWarning = warning;
                this.ErrorCode = code;
            }
        }
        #endregion
    }
}
