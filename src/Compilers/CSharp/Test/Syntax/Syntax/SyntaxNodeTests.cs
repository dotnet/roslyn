﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxNodeTests
    {
        [Fact]
        public void TestQualifiedNameSyntaxWith()
        {
            // this is just a test to prove that at least one generate With method exists and functions correctly. :-)
            var qname = (QualifiedNameSyntax)SyntaxFactory.ParseName("A.B");
            var qname2 = qname.WithRight(SyntaxFactory.IdentifierName("C"));
            var text = qname2.ToString();
            Assert.Equal("A.C", text);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestAddBaseListTypes()
        {
            var cls = SyntaxFactory.ParseCompilationUnit("class C { }").Members[0] as ClassDeclarationSyntax;
            var cls2 = cls.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("B")));
        }

        [Fact]
        public void TestChildNodes()
        {
            var text = "m(a,b,c)";
            var expression = SyntaxFactory.ParseExpression(text);

            var nodes = expression.ChildNodes().ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.IdentifierName, nodes[0].Kind());
            Assert.Equal(SyntaxKind.ArgumentList, nodes[1].Kind());
        }

        [Fact]
        public void TestAncestors()
        {
            var text = "a + (b - (c * (d / e)))";
            var expression = SyntaxFactory.ParseExpression(text);
            var e = expression.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "e");

            var nodes = e.Ancestors().ToList();
            Assert.Equal(7, nodes.Count);
            Assert.Equal(SyntaxKind.DivideExpression, nodes[0].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[1].Kind());
            Assert.Equal(SyntaxKind.MultiplyExpression, nodes[2].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[3].Kind());
            Assert.Equal(SyntaxKind.SubtractExpression, nodes[4].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[5].Kind());
            Assert.Equal(SyntaxKind.AddExpression, nodes[6].Kind());
        }

        [Fact]
        public void TestAncestorsAndSelf()
        {
            var text = "a + (b - (c * (d / e)))";
            var expression = SyntaxFactory.ParseExpression(text);
            var e = expression.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "e");

            var nodes = e.AncestorsAndSelf().ToList();
            Assert.Equal(8, nodes.Count);
            Assert.Equal(SyntaxKind.IdentifierName, nodes[0].Kind());
            Assert.Equal(SyntaxKind.DivideExpression, nodes[1].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[2].Kind());
            Assert.Equal(SyntaxKind.MultiplyExpression, nodes[3].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[4].Kind());
            Assert.Equal(SyntaxKind.SubtractExpression, nodes[5].Kind());
            Assert.Equal(SyntaxKind.ParenthesizedExpression, nodes[6].Kind());
            Assert.Equal(SyntaxKind.AddExpression, nodes[7].Kind());
        }

        [Fact]
        public void TestFirstAncestorOrSelf()
        {
            var text = "a + (b - (c * (d / e)))";
            var expression = SyntaxFactory.ParseExpression(text);
            var e = expression.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "e");

            var firstParens = e.FirstAncestorOrSelf<ExpressionSyntax>(n => n.Kind() == SyntaxKind.ParenthesizedExpression);
            Assert.NotNull(firstParens);
            Assert.Equal("(d / e)", firstParens.ToString());
        }

        [Fact]
        public void TestDescendantNodes()
        {
            var text = "#if true\r\n  return true;";
            var statement = SyntaxFactory.ParseStatement(text);

            var nodes = statement.DescendantNodes().ToList();
            Assert.Equal(1, nodes.Count);
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[0].Kind());

            nodes = statement.DescendantNodes(descendIntoTrivia: true).ToList();
            Assert.Equal(3, nodes.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());

            nodes = statement.DescendantNodes(n => n is StatementSyntax).ToList();
            Assert.Equal(1, nodes.Count);
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[0].Kind());

            nodes = statement.DescendantNodes(n => n is StatementSyntax, descendIntoTrivia: true).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());

            // all over again with spans
            nodes = statement.DescendantNodes(statement.FullSpan).ToList();
            Assert.Equal(1, nodes.Count);
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[0].Kind());

            nodes = statement.DescendantNodes(statement.FullSpan, descendIntoTrivia: true).ToList();
            Assert.Equal(3, nodes.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());

            nodes = statement.DescendantNodes(statement.FullSpan, n => n is StatementSyntax).ToList();
            Assert.Equal(1, nodes.Count);
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[0].Kind());

            nodes = statement.DescendantNodes(statement.FullSpan, n => n is StatementSyntax, descendIntoTrivia: true).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());
        }

        [Fact]
        public void TestDescendantNodesAndSelf()
        {
            var text = "#if true\r\n  return true;";
            var statement = SyntaxFactory.ParseStatement(text);

            var nodes = statement.DescendantNodesAndSelf().ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());

            nodes = statement.DescendantNodesAndSelf(descendIntoTrivia: true).ToList();
            Assert.Equal(4, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[3].Kind());

            nodes = statement.DescendantNodesAndSelf(n => n is StatementSyntax).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());

            nodes = statement.DescendantNodesAndSelf(n => n is StatementSyntax, descendIntoTrivia: true).ToList();
            Assert.Equal(3, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());

            // all over again with spans
            nodes = statement.DescendantNodesAndSelf(statement.FullSpan).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());

            nodes = statement.DescendantNodesAndSelf(statement.FullSpan, descendIntoTrivia: true).ToList();
            Assert.Equal(4, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[3].Kind());

            nodes = statement.DescendantNodesAndSelf(statement.FullSpan, n => n is StatementSyntax).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[1].Kind());

            nodes = statement.DescendantNodesAndSelf(statement.FullSpan, n => n is StatementSyntax, descendIntoTrivia: true).ToList();
            Assert.Equal(3, nodes.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodes[0].Kind());
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodes[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodes[2].Kind());
        }

        [Fact]
        public void TestDescendantNodesAndTokens()
        {
            var text = "#if true\r\n  return true;";
            var statement = SyntaxFactory.ParseStatement(text);

            var nodesAndTokens = statement.DescendantNodesAndTokens().ToList();
            Assert.Equal(4, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[3].Kind());

            nodesAndTokens = statement.DescendantNodesAndTokens(descendIntoTrivia: true).ToList();
            Assert.Equal(10, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.HashToken, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.IfKeyword, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[3].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[4].Kind());
            Assert.Equal(SyntaxKind.EndOfDirectiveToken, nodesAndTokens[5].Kind());
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[6].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[7].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[8].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[9].Kind());

            // with span
            nodesAndTokens = statement.DescendantNodesAndTokens(statement.FullSpan).ToList();
            Assert.Equal(4, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[3].Kind());
        }

        [Fact]
        public void TestDescendantNodesAndTokensAndSelf()
        {
            var text = "#if true\r\n  return true;";
            var statement = SyntaxFactory.ParseStatement(text);

            var nodesAndTokens = statement.DescendantNodesAndTokensAndSelf().ToList();
            Assert.Equal(5, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[3].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[4].Kind());

            nodesAndTokens = statement.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true).ToList();
            Assert.Equal(11, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.HashToken, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.IfKeyword, nodesAndTokens[3].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[4].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[5].Kind());
            Assert.Equal(SyntaxKind.EndOfDirectiveToken, nodesAndTokens[6].Kind());
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[7].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[8].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[9].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[10].Kind());

            // with span
            nodesAndTokens = statement.DescendantNodesAndTokensAndSelf(statement.FullSpan).ToList();
            Assert.Equal(5, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.ReturnStatement, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.ReturnKeyword, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.TrueLiteralExpression, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.TrueKeyword, nodesAndTokens[3].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, nodesAndTokens[4].Kind());
        }

        [Fact]
        public void TestDescendantNodesAndTokensAndSelfForEmptyCompilationUnit()
        {
            var text = "";
            var cu = SyntaxFactory.ParseCompilationUnit(text);
            var nodesAndTokens = cu.DescendantNodesAndTokensAndSelf().ToList();
            Assert.Equal(2, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.CompilationUnit, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.EndOfFileToken, nodesAndTokens[1].Kind());
        }

        [Fact]
        public void TestDescendantNodesAndTokensAndSelfForDocumentationComment()
        {
            var text = "/// Foo\r\n x";
            var expr = SyntaxFactory.ParseExpression(text);

            var nodesAndTokens = expr.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true).ToList();
            Assert.Equal(7, nodesAndTokens.Count);
            Assert.Equal(SyntaxKind.IdentifierName, nodesAndTokens[0].Kind());
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, nodesAndTokens[1].Kind());
            Assert.Equal(SyntaxKind.XmlText, nodesAndTokens[2].Kind());
            Assert.Equal(SyntaxKind.XmlTextLiteralToken, nodesAndTokens[3].Kind());
            Assert.Equal(SyntaxKind.XmlTextLiteralNewLineToken, nodesAndTokens[4].Kind());
            Assert.Equal(SyntaxKind.EndOfDocumentationCommentToken, nodesAndTokens[5].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, nodesAndTokens[6].Kind());
        }

        [Fact]
        public void TestGetAllDirectivesUsingDescendantNodes()
        {
            var text = "#if false\r\n  eat a sandwich\r\n#endif\r\n x";
            var expr = SyntaxFactory.ParseExpression(text);

            var directives = expr.GetDirectives();
            var descendantDirectives = expr.DescendantNodesAndSelf(n => n.ContainsDirectives, descendIntoTrivia: true).OfType<DirectiveTriviaSyntax>().ToList();

            Assert.Equal(directives.Count, descendantDirectives.Count);
            for (int i = 0; i < directives.Count; i++)
            {
                Assert.Equal(directives[i], descendantDirectives[i]);
            }
        }

        [Fact]
        public void TestGetAllAnnotatedNodesUsingDescendantNodes()
        {
            var text = "a + (b - (c * (d / e)))";
            var expr = SyntaxFactory.ParseExpression(text);
            var myAnnotation = new SyntaxAnnotation();

            var identifierNodes = expr.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
            var exprWithAnnotations = expr.ReplaceNodes(identifierNodes, (e, e2) => e2.WithAdditionalAnnotations(myAnnotation));

            var nodesWithMyAnnotations = exprWithAnnotations.DescendantNodesAndSelf(n => n.ContainsAnnotations).Where(n => n.HasAnnotation(myAnnotation)).ToList();

            Assert.Equal(identifierNodes.Count, nodesWithMyAnnotations.Count);

            for (int i = 0; i < identifierNodes.Count; i++)
            {
                // compare text because node identity changed when adding the annotation
                Assert.Equal(identifierNodes[i].ToString(), nodesWithMyAnnotations[i].ToString());
            }
        }

        [Fact]
        public void TestDescendantTokens()
        {
            var s1 = "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens().ToList();
            Assert.Equal(4, tokens.Count);
            Assert.Equal(SyntaxKind.UsingKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[2].Kind());
            Assert.Equal(SyntaxKind.EndOfFileToken, tokens[3].Kind());
        }

        [Fact]
        public void TestDescendantTokensWithExtraWhitespace()
        {
            var s1 = "  using Foo  ;  ";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens().ToList();
            Assert.Equal(4, tokens.Count);
            Assert.Equal(SyntaxKind.UsingKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[2].Kind());
            Assert.Equal(SyntaxKind.EndOfFileToken, tokens[3].Kind());
        }

        [Fact]
        public void TestDescendantTokensEntireRange()
        {
            var s1 = "extern alias Bar;\r\n" + "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens().ToList();
            Assert.Equal(8, tokens.Count);
            Assert.Equal(SyntaxKind.ExternKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.AliasKeyword, tokens[1].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[2].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[3].Kind());
            Assert.Equal(SyntaxKind.UsingKeyword, tokens[4].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[5].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[6].Kind());
            Assert.Equal(SyntaxKind.EndOfFileToken, tokens[7].Kind());
        }

        [Fact]
        public void TestDescendantTokensOverFullSpan()
        {
            var s1 = "extern alias Bar;\r\n" + "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens(new TextSpan(0, 16)).ToList();
            Assert.Equal(3, tokens.Count);
            Assert.Equal(SyntaxKind.ExternKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.AliasKeyword, tokens[1].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[2].Kind());
        }

        [Fact]
        public void TestDescendantTokensOverInsideSpan()
        {
            var s1 = "extern alias Bar;\r\n" + "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens(new TextSpan(1, 14)).ToList();
            Assert.Equal(3, tokens.Count);
            Assert.Equal(SyntaxKind.ExternKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.AliasKeyword, tokens[1].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[2].Kind());
        }

        [Fact]
        public void TestDescendantTokensOverFullSpanOffset()
        {
            var s1 = "extern alias Bar;\r\n" + "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens(new TextSpan(7, 17)).ToList();
            Assert.Equal(4, tokens.Count);
            Assert.Equal(SyntaxKind.AliasKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[2].Kind());
            Assert.Equal(SyntaxKind.UsingKeyword, tokens[3].Kind());
        }

        [Fact]
        public void TestDescendantTokensOverInsideSpanOffset()
        {
            var s1 = "extern alias Bar;\r\n" + "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);
            var tokens = t1.GetCompilationUnitRoot().DescendantTokens(new TextSpan(8, 15)).ToList();
            Assert.Equal(4, tokens.Count);
            Assert.Equal(SyntaxKind.AliasKeyword, tokens[0].Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, tokens[1].Kind());
            Assert.Equal(SyntaxKind.SemicolonToken, tokens[2].Kind());
            Assert.Equal(SyntaxKind.UsingKeyword, tokens[3].Kind());
        }

        [Fact]
        public void TestDescendantTrivia()
        {
            var text = "// foo\r\na + b";
            var expr = SyntaxFactory.ParseExpression(text);

            var list = expr.DescendantTrivia().ToList();
            Assert.Equal(4, list.Count);
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, list[0].Kind());
            Assert.Equal(SyntaxKind.EndOfLineTrivia, list[1].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list[2].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list[3].Kind());
        }

        [Fact]
        public void TestDescendantTriviaIntoStructuredTrivia()
        {
            var text = @"
/// <foo >
/// </foo>
a + b";
            var expr = SyntaxFactory.ParseExpression(text);

            var list = expr.DescendantTrivia(descendIntoTrivia: true).ToList();
            Assert.Equal(7, list.Count);
            Assert.Equal(SyntaxKind.EndOfLineTrivia, list[0].Kind());
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, list[1].Kind());
            Assert.Equal(SyntaxKind.DocumentationCommentExteriorTrivia, list[2].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list[3].Kind());
            Assert.Equal(SyntaxKind.DocumentationCommentExteriorTrivia, list[4].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list[5].Kind());
            Assert.Equal(SyntaxKind.WhitespaceTrivia, list[6].Kind());
        }

        [Fact]
        public void Bug877223()
        {
            var s1 = "using Foo;";
            var t1 = SyntaxFactory.ParseSyntaxTree(s1);

            // var node = t1.GetCompilationUnitRoot().Usings[0].GetTokens(new TextSpan(6, 3)).First();
            var node = t1.GetCompilationUnitRoot().DescendantTokens(new TextSpan(6, 3)).First();
            Assert.Equal(node.ToString(), "Foo");
        }

        [Fact]
        public void TestFindToken()
        {
            var text = "class\n #if XX\n#endif\n foo { }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);

            var token = tree.GetCompilationUnitRoot().FindToken("class\n #i".Length);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal("foo", token.ToString());
            token = tree.GetCompilationUnitRoot().FindToken("class\n #i".Length, findInsideTrivia: true);
            Assert.Equal(SyntaxKind.IfKeyword, token.Kind());
        }

        [Fact]
        public void TestFindTokenInLargeList()
        {
            var identifier = SyntaxFactory.Identifier("x");
            var missingIdentifier = SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
            var name = SyntaxFactory.IdentifierName(identifier);
            var missingName = SyntaxFactory.IdentifierName(missingIdentifier);
            var comma = SyntaxFactory.Token(SyntaxKind.CommaToken);
            var missingComma = SyntaxFactory.MissingToken(SyntaxKind.CommaToken);
            var argument = SyntaxFactory.Argument(name);
            var missingArgument = SyntaxFactory.Argument(missingName);

            // make a large list that has lots of zero-length nodes (that shouldn't be found)
            var nodesAndTokens = SyntaxFactory.NodeOrTokenList(
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                missingArgument, missingComma,
                argument);

            var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(SyntaxFactory.NodeOrTokenList(nodesAndTokens)));
            var invocation = SyntaxFactory.InvocationExpression(name, argumentList);
            CheckFindToken(invocation);
        }

        private void CheckFindToken(SyntaxNode node)
        {
            for (int i = 0; i < node.FullSpan.End; i++)
            {
                var token = node.FindToken(i);
                Assert.Equal(true, token.FullSpan.Contains(i));
            }
        }

        [WorkItem(755236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755236")]
        [Fact]
        public void TestFindNode()
        {
            var text = "class\n #if XX\n#endif\n foo { }\n class bar { }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);

            var root = tree.GetRoot();
            Assert.Equal(root, root.FindNode(root.Span, findInsideTrivia: false));
            Assert.Equal(root, root.FindNode(root.Span, findInsideTrivia: true));

            var classDecl = (TypeDeclarationSyntax)root.ChildNodes().First();

            // IdentifierNameSyntax in trivia.
            var identifier = root.DescendantNodes(descendIntoTrivia: true).Single(n => n is IdentifierNameSyntax);
            var position = identifier.Span.Start + 1;

            Assert.Equal(classDecl, root.FindNode(identifier.Span, findInsideTrivia: false));
            Assert.Equal(identifier, root.FindNode(identifier.Span, findInsideTrivia: true));

            // Token span.
            Assert.Equal(classDecl, root.FindNode(classDecl.Identifier.Span, findInsideTrivia: false));

            // EOF Token span.
            var EOFSpan = new TextSpan(root.FullSpan.End, 0);
            Assert.Equal(root, root.FindNode(EOFSpan, findInsideTrivia: false));
            Assert.Equal(root, root.FindNode(EOFSpan, findInsideTrivia: true));

            // EOF Invalid span for childnode
            var classDecl2 = (TypeDeclarationSyntax)root.ChildNodes().Last();
            Assert.Throws<ArgumentOutOfRangeException>(() => classDecl2.FindNode(EOFSpan));

            // Check end position included in node span
            var nodeEndPositionSpan = new TextSpan(classDecl.FullSpan.End, 0);

            Assert.Equal(classDecl2, root.FindNode(nodeEndPositionSpan, findInsideTrivia: false));
            Assert.Equal(classDecl2, root.FindNode(nodeEndPositionSpan, findInsideTrivia: true));
            Assert.Equal(classDecl2, classDecl2.FindNode(nodeEndPositionSpan, findInsideTrivia: false));
            Assert.Equal(classDecl2, classDecl2.FindNode(nodeEndPositionSpan, findInsideTrivia: true));

            Assert.Throws<ArgumentOutOfRangeException>(() => classDecl.FindNode(nodeEndPositionSpan));

            // Invalid spans.
            var invalidSpan = new TextSpan(100, 100);
            Assert.Throws<ArgumentOutOfRangeException>(() => root.FindNode(invalidSpan));
            invalidSpan = new TextSpan(root.FullSpan.End - 1, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => root.FindNode(invalidSpan));
            invalidSpan = new TextSpan(classDecl2.FullSpan.Start - 1, root.FullSpan.End);
            Assert.Throws<ArgumentOutOfRangeException>(() => classDecl2.FindNode(invalidSpan));
            invalidSpan = new TextSpan(classDecl.FullSpan.End, root.FullSpan.End);
            Assert.Throws<ArgumentOutOfRangeException>(() => classDecl2.FindNode(invalidSpan));
            // Parent node's span.
            Assert.Throws<ArgumentOutOfRangeException>(() => classDecl.FindNode(root.FullSpan));
        }

        [WorkItem(539941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539941")]
        [Fact]
        public void TestFindTriviaNoTriviaExistsAtPosition()
        {
            var code = @"class Foo
{
    void Bar()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var position = tree.GetText().Lines[2].End - 1;
            // position points to the closing parenthesis on the line that has "void Bar()"
            // There should be no trivia at this position
            var trivia = tree.GetCompilationUnitRoot().FindTrivia(position);
            Assert.Equal(SyntaxKind.None, trivia.Kind());
            Assert.Equal(0, trivia.SpanStart);
            Assert.Equal(0, trivia.Span.End);
            Assert.Equal(default(SyntaxTrivia), trivia);
        }

        [Fact]
        public void TestTreeEquivalentToSelf()
        {
            var text = "class foo { }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.True(tree.GetCompilationUnitRoot().IsEquivalentTo(tree.GetCompilationUnitRoot()));
        }

        [Fact]
        public void TestTreeNotEquivalentToNull()
        {
            var text = "class foo { }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.False(tree.GetCompilationUnitRoot().IsEquivalentTo(null));
        }

        [Fact]
        public void TestTreesFromSameSourceEquivalent()
        {
            var text = "class foo { }";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = SyntaxFactory.ParseSyntaxTree(text);
            Assert.NotEqual(tree1.GetCompilationUnitRoot(), tree2.GetCompilationUnitRoot());
            Assert.True(tree1.GetCompilationUnitRoot().IsEquivalentTo(tree2.GetCompilationUnitRoot()));
        }

        [Fact]
        public void TestDifferentTreesNotEquivalent()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class foo { }");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class bar { }");
            Assert.NotEqual(tree1.GetCompilationUnitRoot(), tree2.GetCompilationUnitRoot());
            Assert.False(tree1.GetCompilationUnitRoot().IsEquivalentTo(tree2.GetCompilationUnitRoot()));
        }

        [Fact]
        public void TestVastlyDifferentTreesNotEquivalent()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class foo { }");
            var tree2 = SyntaxFactory.ParseSyntaxTree(string.Empty);
            Assert.NotEqual(tree1.GetCompilationUnitRoot(), tree2.GetCompilationUnitRoot());
            Assert.False(tree1.GetCompilationUnitRoot().IsEquivalentTo(tree2.GetCompilationUnitRoot()));
        }

        [Fact]
        public void TestSimilarSubtreesEquivalent()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class foo { void M() { } }");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class bar { void M() { } }");
            var m1 = ((TypeDeclarationSyntax)tree1.GetCompilationUnitRoot().Members[0]).Members[0];
            var m2 = ((TypeDeclarationSyntax)tree2.GetCompilationUnitRoot().Members[0]).Members[0];
            Assert.Equal(SyntaxKind.MethodDeclaration, m1.Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, m2.Kind());
            Assert.NotEqual(m1, m2);
            Assert.True(m1.IsEquivalentTo(m2));
        }

        [Fact]
        public void TestTreesWithDifferentTriviaAreNotEquivalent()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class foo {void M() { }}");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class foo { void M() { } }");
            Assert.False(tree1.GetCompilationUnitRoot().IsEquivalentTo(tree2.GetCompilationUnitRoot()));
        }

        [Fact, WorkItem(536664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536664")]
        public void TestTriviaNodeCached()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(" class foo {}");

            // get to the trivia node
            var trivia = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia()[0];

            // we get the trivia again
            var triviaAgain = tree.GetCompilationUnitRoot().Members[0].GetLeadingTrivia()[0];

            // should NOT return two distinct objects for trivia and triviaAgain - struct now.
            Assert.True(SyntaxTrivia.Equals(trivia, triviaAgain));
        }

        [Fact]
        public void TestGetFirstToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var first = tree.GetCompilationUnitRoot().GetFirstToken();
            Assert.Equal(SyntaxKind.PublicKeyword, first.Kind());
        }

        [Fact]
        public void TestGetFirstTokenIncludingZeroWidth()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var first = tree.GetCompilationUnitRoot().GetFirstToken(includeZeroWidth: true);
            Assert.Equal(SyntaxKind.PublicKeyword, first.Kind());
        }

        [Fact]
        public void TestGetLastToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var last = tree.GetCompilationUnitRoot().GetLastToken();
            Assert.Equal(SyntaxKind.CloseBraceToken, last.Kind());
        }

        [Fact]
        public void TestGetLastTokenIncludingZeroWidth()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { ");
            var last = tree.GetCompilationUnitRoot().GetLastToken(includeZeroWidth: true);
            Assert.Equal(SyntaxKind.EndOfFileToken, last.Kind());

            last = tree.GetCompilationUnitRoot().Members[0].GetLastToken(includeZeroWidth: true);
            Assert.Equal(SyntaxKind.CloseBraceToken, last.Kind());
            Assert.True(last.IsMissing);
            Assert.Equal(26, last.FullSpan.Start);
        }

        [Fact]
        public void TestReverseChildSyntaxList()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class A {} public class B {} public static class C {}");
            var root1 = tree1.GetCompilationUnitRoot();
            TestReverse(root1.ChildNodesAndTokens());
            TestReverse(root1.Members[0].ChildNodesAndTokens());
            TestReverse(root1.Members[1].ChildNodesAndTokens());
            TestReverse(root1.Members[2].ChildNodesAndTokens());
        }

        private void TestReverse(ChildSyntaxList children)
        {
            var list1 = children.AsEnumerable().Reverse().ToList();
            var list2 = children.Reverse().ToList();
            Assert.Equal(list1.Count, list2.Count);
            for (int i = 0; i < list1.Count; i++)
            {
                Assert.Equal(list1[i], list2[i]);
                Assert.Equal(list1[i].FullSpan.Start, list2[i].FullSpan.Start);
            }
        }

        [Fact]
        public void TestGetNextToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();

            var list = new List<SyntaxToken>();
            var token = tree.GetCompilationUnitRoot().GetFirstToken(includeSkipped: true);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetNextToken(includeSkipped: true);
            }

            // descendant tokens include EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetNextTokenIncludingSkippedTokens()
        {
            var text =
@"garbage
using foo.bar;
";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var tokens = tree.GetCompilationUnitRoot().DescendantTokens(descendIntoTrivia: true).Where(SyntaxToken.NonZeroWidth).ToList();
            Assert.Equal(6, tokens.Count);
            Assert.Equal("garbage", tokens[0].Text);

            var list = new List<SyntaxToken>(tokens.Count);
            var token = tree.GetCompilationUnitRoot().GetFirstToken(includeSkipped: true);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetNextToken(includeSkipped: true);
            }

            Assert.Equal(tokens.Count, list.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetNextTokenExcludingSkippedTokens()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"garbage
using foo.bar;
");
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();
            Assert.Equal(6, tokens.Count);

            var list = new List<SyntaxToken>(tokens.Count);
            var token = tree.GetCompilationUnitRoot().GetFirstToken(includeSkipped: false);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetNextToken(includeSkipped: false);
            }

            // descendant tokens includes EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetNextTokenCommon()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            List<SyntaxToken> tokens = syntaxTree.GetRoot().DescendantTokens().ToList();

            List<SyntaxToken> list = new List<SyntaxToken>();
            SyntaxToken token = syntaxTree.GetRoot().GetFirstToken();
            while (token.RawKind != 0)
            {
                list.Add(token);
                token = token.GetNextToken();
            }

            // descendant tokens includes EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetPreviousToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();

            var list = new List<SyntaxToken>();
            var token = tree.GetCompilationUnitRoot().GetLastToken(); // skip EOF
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetPreviousToken();
            }
            list.Reverse();

            // descendant tokens includes EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetPreviousTokenIncludingSkippedTokens()
        {
            var text =
@"garbage
using foo.bar;
";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var tokens = tree.GetCompilationUnitRoot().DescendantTokens(descendIntoTrivia: true).Where(SyntaxToken.NonZeroWidth).ToList();
            Assert.Equal(6, tokens.Count);
            Assert.Equal("garbage", tokens[0].Text);

            var list = new List<SyntaxToken>(tokens.Count);
            var token = tree.GetCompilationUnitRoot().GetLastToken(includeSkipped: true);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetPreviousToken(includeSkipped: true);
            }
            list.Reverse();

            Assert.Equal(tokens.Count, list.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetPreviousTokenExcludingSkippedTokens()
        {
            var text =
@"garbage
using foo.bar;
";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, tree.GetCompilationUnitRoot().ToFullString());

            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();
            Assert.Equal(6, tokens.Count);

            var list = new List<SyntaxToken>(tokens.Count);
            var token = tree.GetCompilationUnitRoot().GetLastToken(includeSkipped: false);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetPreviousToken(includeSkipped: false);
            }
            list.Reverse();

            // descendant tokens includes EOF
            Assert.Equal(tokens.Count, list.Count + 1);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(tokens[i], list[i]);
            }
        }

        [Fact]
        public void TestGetPreviousTokenCommon()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            List<SyntaxToken> tokens = syntaxTree.GetRoot().DescendantTokens().ToList();

            List<SyntaxToken> list = new List<SyntaxToken>();
            var token = syntaxTree.GetRoot().GetLastToken(includeZeroWidth: false); // skip EOF

            while (token.RawKind != 0)
            {
                list.Add(token);
                token = token.GetPreviousToken();
            }
            list.Reverse();

            // descendant tokens include EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetNextTokenIncludingZeroWidth()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo {");
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();

            var list = new List<SyntaxToken>();
            var token = tree.GetCompilationUnitRoot().GetFirstToken(includeZeroWidth: true);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetNextToken(includeZeroWidth: true);
            }

            Assert.Equal(tokens.Count, list.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetNextTokenIncludingZeroWidthCommon()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("public static class foo {");
            List<SyntaxToken> tokens = syntaxTree.GetRoot().DescendantTokens().ToList();

            List<SyntaxToken> list = new List<SyntaxToken>();
            SyntaxToken token = syntaxTree.GetRoot().GetFirstToken(includeZeroWidth: true);
            while (token.RawKind != 0)
            {
                list.Add(token);
                token = token.GetNextToken(includeZeroWidth: true);
            }

            Assert.Equal(tokens.Count, list.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetPreviousTokenIncludingZeroWidth()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo {");
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens().ToList();

            var list = new List<SyntaxToken>();
            var token = tree.GetCompilationUnitRoot().EndOfFileToken.GetPreviousToken(includeZeroWidth: true);
            while (token.Kind() != SyntaxKind.None)
            {
                list.Add(token);
                token = token.GetPreviousToken(includeZeroWidth: true);
            }

            list.Reverse();

            // descendant tokens include EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(list[i], tokens[i]);
            }
        }

        [Fact]
        public void TestGetPreviousTokenIncludingZeroWidthCommon()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("public static class foo {");
            List<SyntaxToken> tokens = syntaxTree.GetRoot().DescendantTokens().ToList();

            List<SyntaxToken> list = new List<SyntaxToken>();
            SyntaxToken token = ((SyntaxToken)((SyntaxTree)syntaxTree).GetCompilationUnitRoot().EndOfFileToken).GetPreviousToken(includeZeroWidth: true);
            while (token.RawKind != 0)
            {
                list.Add(token);
                token = token.GetPreviousToken(includeZeroWidth: true);
            }
            list.Reverse();

            // descendant tokens includes EOF
            Assert.Equal(tokens.Count - 1, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(tokens[i], list[i]);
            }
        }

        [Fact]
        public void TestGetNextSibling()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var children = tree.GetCompilationUnitRoot().Members[0].ChildNodesAndTokens().ToList();
            var list = new List<SyntaxNodeOrToken>();
            for (var child = children[0]; child.Kind() != SyntaxKind.None; child = child.GetNextSibling())
            {
                list.Add(child);
            }

            Assert.Equal(children.Count, list.Count);
            for (int i = 0; i < children.Count; i++)
            {
                Assert.Equal(list[i], children[i]);
            }
        }

        [Fact]
        public void TestGetPreviousSibling()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var children = tree.GetCompilationUnitRoot().Members[0].ChildNodesAndTokens().ToList();
            var reversed = children.AsEnumerable().Reverse().ToList();
            var list = new List<SyntaxNodeOrToken>();
            for (var child = children[children.Count - 1]; child.Kind() != SyntaxKind.None; child = child.GetPreviousSibling())
            {
                list.Add(child);
            }

            Assert.Equal(children.Count, list.Count);
            for (int i = 0; i < reversed.Count; i++)
            {
                Assert.Equal(list[i], reversed[i]);
            }
        }

        [Fact]
        public void TestSyntaxNodeOrTokenEquality()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("public static class foo { }");
            var child = tree.GetCompilationUnitRoot().ChildNodesAndTokens()[0];
            var member = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            Assert.Equal((SyntaxNodeOrToken)member, child);

            var name = member.Identifier;
            var nameChild = member.ChildNodesAndTokens()[3];
            Assert.Equal((SyntaxNodeOrToken)name, nameChild);

            var closeBraceToken = member.CloseBraceToken;
            var closeBraceChild = member.GetLastToken();
            Assert.Equal((SyntaxNodeOrToken)closeBraceToken, closeBraceChild);
        }

        [Fact]
        public void TestStructuredTriviaHasNoParent()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("#define FOO");
            var trivia = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia()[0];
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, trivia.Kind());
            Assert.True(trivia.HasStructure);
            Assert.NotNull(trivia.GetStructure());
            Assert.Null(trivia.GetStructure().Parent);
        }

        [Fact]
        public void TestStructuredTriviaHasParentTrivia()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("#define FOO");
            var trivia = tree.GetCompilationUnitRoot().EndOfFileToken.GetLeadingTrivia()[0];
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, trivia.Kind());
            Assert.True(trivia.HasStructure);
            Assert.NotNull(trivia.GetStructure());
            var parentTrivia = trivia.GetStructure().ParentTrivia;
            Assert.NotEqual(SyntaxKind.None, parentTrivia.Kind());
            Assert.Equal(trivia, parentTrivia);
        }

        [Fact]
        public void TestStructuredTriviaParentTrivia()
        {
            var def = SyntaxFactory.DefineDirectiveTrivia(SyntaxFactory.Identifier("FOO"), false);

            // unrooted structured trivia should report parent trivia as default 
            Assert.Equal(default(SyntaxTrivia), def.ParentTrivia);

            var trivia = SyntaxFactory.Trivia(def);
            var structure = trivia.GetStructure();
            Assert.NotEqual(def, structure);  // these should not be identity equals
            Assert.True(def.IsEquivalentTo(structure)); // they should be equivalent though
            Assert.Equal(trivia, structure.ParentTrivia); // parent trivia should be equal to original trivia

            // attach trivia to token and walk down to structured trivia and back up again
            var token = SyntaxFactory.Identifier(default(SyntaxTriviaList), "x", SyntaxTriviaList.Create(trivia));
            var tokenTrivia = token.TrailingTrivia[0];
            var tokenStructuredTrivia = tokenTrivia.GetStructure();
            var tokenStructuredParentTrivia = tokenStructuredTrivia.ParentTrivia;
            Assert.Equal(tokenTrivia, tokenStructuredParentTrivia);
            Assert.Equal(token, tokenStructuredParentTrivia.Token);
        }

        [Fact]
        public void TestGetFirstDirective()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("#define FOO");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d.Kind());
        }

        [Fact]
        public void TestGetLastDirective()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#undef FOO
");
            var d = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.UndefDirectiveTrivia, d.Kind());
        }

        [Fact]
        public void TestGetNextDirective()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#define BAR
class C {
#if FOO
   void M() { }
#endif
}
");
            var d1 = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d1);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d1.Kind());
            var d2 = d1.GetNextDirective();
            Assert.NotNull(d2);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d2.Kind());
            var d3 = d2.GetNextDirective();
            Assert.NotNull(d3);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d3.Kind());
            var d4 = d3.GetNextDirective();
            Assert.NotNull(d4);
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, d4.Kind());
            var d5 = d4.GetNextDirective();
            Assert.Null(d5);
        }

        [Fact]
        public void TestGetPreviousDirective()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#define BAR
class C {
#if FOO
   void M() { }
#endif
}
");
            var d1 = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d1);
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, d1.Kind());
            var d2 = d1.GetPreviousDirective();
            Assert.NotNull(d2);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d2.Kind());
            var d3 = d2.GetPreviousDirective();
            Assert.NotNull(d3);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d3.Kind());
            var d4 = d3.GetPreviousDirective();
            Assert.NotNull(d4);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d4.Kind());
            var d5 = d4.GetPreviousDirective();
            Assert.Null(d5);
        }

        [Fact]
        public void TestGetDirectivesRelatedToIf()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d.Kind());
            d = d.GetNextDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToIfElements()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d.Kind());
            d = d.GetNextDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());

            // get directives related to elif
            var related2 = related[1].GetRelatedDirectives();
            Assert.True(related.SequenceEqual(related2));

            // get directives related to else
            var related3 = related[3].GetRelatedDirectives();
            Assert.True(related.SequenceEqual(related3));
        }

        [Fact]
        public void TestGetDirectivesRelatedToEndIf()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToIfWithNestedIfEndIF()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#if ZED
  class A1 { }
#endif
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d.Kind());
            d = d.GetNextDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToIfWithNestedRegionEndRegion()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#region some region
  class A1 { }
#endregion
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.DefineDirectiveTrivia, d.Kind());
            d = d.GetNextDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToEndIfWithNestedIfEndIf()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
class A { }
#if ZED
  class A1 { }
#endif
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToEndIfWithNestedRegionEndRegion()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#define FOO
#if FOO
#region some region
class A { }
#endregion
#elif BAR
class B { }
#elif BAZ
class B { }
#else 
class C { }
#endif
");
            var d = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(5, related.Count);
            Assert.Equal(SyntaxKind.IfDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[1].Kind());
            Assert.Equal(SyntaxKind.ElifDirectiveTrivia, related[2].Kind());
            Assert.Equal(SyntaxKind.ElseDirectiveTrivia, related[3].Kind());
            Assert.Equal(SyntaxKind.EndIfDirectiveTrivia, related[4].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToRegion()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"#region Some Region
class A { }
#endregion
#if FOO
#endif
");
            var d = tree.GetCompilationUnitRoot().GetFirstDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.RegionDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(2, related.Count);
            Assert.Equal(SyntaxKind.RegionDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.EndRegionDirectiveTrivia, related[1].Kind());
        }

        [Fact]
        public void TestGetDirectivesRelatedToEndRegion()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"
#if FOO
#endif
#region Some Region
class A { }
#endregion
");
            var d = tree.GetCompilationUnitRoot().GetLastDirective();
            Assert.NotNull(d);
            Assert.Equal(SyntaxKind.EndRegionDirectiveTrivia, d.Kind());
            var related = d.GetRelatedDirectives();
            Assert.NotNull(related);
            Assert.Equal(2, related.Count);
            Assert.Equal(SyntaxKind.RegionDirectiveTrivia, related[0].Kind());
            Assert.Equal(SyntaxKind.EndRegionDirectiveTrivia, related[1].Kind());
        }

        [WorkItem(536995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536995")]
        [Fact]
        public void TestTextAndSpanWithTrivia1()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"/*START*/namespace Microsoft.CSharp.Test
{
}/*END*/");
            var rootNode = tree.GetCompilationUnitRoot();

            Assert.Equal(rootNode.FullSpan.Length, rootNode.ToFullString().Length);
            Assert.Equal(rootNode.Span.Length, rootNode.ToString().Length);
            Assert.Equal(true, rootNode.ToString().Contains("/*END*/"));
            Assert.Equal(false, rootNode.ToString().Contains("/*START*/"));
        }

        [WorkItem(536996, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536996")]
        [Fact]
        public void TestTextAndSpanWithTrivia2()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"/*START*/
namespace Microsoft.CSharp.Test
{
}
/*END*/");
            var rootNode = tree.GetCompilationUnitRoot();

            Assert.Equal(rootNode.FullSpan.Length, rootNode.ToFullString().Length);
            Assert.Equal(rootNode.Span.Length, rootNode.ToString().Length);
            Assert.Equal(true, rootNode.ToString().Contains("/*END*/"));
            Assert.Equal(false, rootNode.ToString().Contains("/*START*/"));
        }

        [Fact]
        public void TestCreateCommonSyntaxNode()
        {
            var rootNode = SyntaxFactory.ParseSyntaxTree("using X; namespace Y { }").GetCompilationUnitRoot();
            var namespaceNode = rootNode.ChildNodesAndTokens()[1].AsNode();
            var nodeOrToken = (SyntaxNodeOrToken)namespaceNode;
            Assert.True(nodeOrToken.IsNode);
            Assert.Equal(namespaceNode, nodeOrToken.AsNode());
            Assert.Equal(rootNode, nodeOrToken.Parent);
            Assert.Equal(namespaceNode.FullSpan, nodeOrToken.FullSpan);
            Assert.Equal(namespaceNode.Span, nodeOrToken.Span);
        }

        [Fact, WorkItem(537070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537070")]
        public void TestTraversalUsingCommonSyntaxNodeOrToken()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(@"class c1
{
}");
            var nodeOrToken = (SyntaxNodeOrToken)syntaxTree.GetRoot();
            Assert.Equal(0, syntaxTree.GetDiagnostics().Count());
            Action<SyntaxNodeOrToken> walk = null;
            walk = (SyntaxNodeOrToken nOrT) =>
            {
                Assert.Equal(0, syntaxTree.GetDiagnostics(nOrT).Count());
                foreach (var child in nOrT.ChildNodesAndTokens())
                {
                    walk(child);
                }
            };
            walk(nodeOrToken);
        }

        [WorkItem(537747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537747")]
        [Fact]
        public void SyntaxTriviaDefaultIsDirective()
        {
            SyntaxTrivia trivia = new SyntaxTrivia();
            Assert.False(trivia.IsDirective);
        }

        [Fact]
        public void SyntaxNames()
        {
            var cc = SyntaxFactory.Token(SyntaxKind.ColonColonToken);
            var lt = SyntaxFactory.Token(SyntaxKind.LessThanToken);
            var gt = SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
            var dot = SyntaxFactory.Token(SyntaxKind.DotToken);
            var gp = SyntaxFactory.SingletonSeparatedList<TypeSyntax>(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));

            var externAlias = SyntaxFactory.IdentifierName("alias");
            var foo = SyntaxFactory.IdentifierName("Foo");
            var bar = SyntaxFactory.IdentifierName("Bar");

            // Foo.Bar
            var qualified = SyntaxFactory.QualifiedName(foo, dot, bar);
            Assert.Equal("Foo.Bar", qualified.ToString());
            Assert.Equal("Bar", qualified.GetUnqualifiedName().Identifier.ValueText);

            // Bar<int>
            var generic = SyntaxFactory.GenericName(bar.Identifier, SyntaxFactory.TypeArgumentList(lt, gp, gt));
            Assert.Equal("Bar<int>", generic.ToString());
            Assert.Equal("Bar", generic.GetUnqualifiedName().Identifier.ValueText);

            // Foo.Bar<int>
            var qualifiedGeneric = SyntaxFactory.QualifiedName(foo, dot, generic);
            Assert.Equal("Foo.Bar<int>", qualifiedGeneric.ToString());
            Assert.Equal("Bar", qualifiedGeneric.GetUnqualifiedName().Identifier.ValueText);

            // alias::Foo
            var alias = SyntaxFactory.AliasQualifiedName(externAlias, cc, foo);
            Assert.Equal("alias::Foo", alias.ToString());
            Assert.Equal("Foo", alias.GetUnqualifiedName().Identifier.ValueText);

            // alias::Bar<int>
            var aliasGeneric = SyntaxFactory.AliasQualifiedName(externAlias, cc, generic);
            Assert.Equal("alias::Bar<int>", aliasGeneric.ToString());
            Assert.Equal("Bar", aliasGeneric.GetUnqualifiedName().Identifier.ValueText);

            // alias::Foo.Bar
            var aliasQualified = SyntaxFactory.QualifiedName(alias, dot, bar);
            Assert.Equal("alias::Foo.Bar", aliasQualified.ToString());
            Assert.Equal("Bar", aliasQualified.GetUnqualifiedName().Identifier.ValueText);

            // alias::Foo.Bar<int>
            var aliasQualifiedGeneric = SyntaxFactory.QualifiedName(alias, dot, generic);
            Assert.Equal("alias::Foo.Bar<int>", aliasQualifiedGeneric.ToString());
            Assert.Equal("Bar", aliasQualifiedGeneric.GetUnqualifiedName().Identifier.ValueText);
        }

        [Fact]
        public void ZeroWidthTokensInListAreUnique()
        {
            var someToken = SyntaxFactory.MissingToken(SyntaxKind.IntKeyword);
            var list = SyntaxFactory.TokenList(someToken, someToken);
            Assert.Equal(someToken, someToken);
            Assert.NotEqual(list[0], list[1]);
        }

        [Fact]
        public void ZeroWidthTokensInParentAreUnique()
        {
            var missingComma = SyntaxFactory.MissingToken(SyntaxKind.CommaToken);
            var omittedArraySize = SyntaxFactory.OmittedArraySizeExpression(SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken));
            var spec = SyntaxFactory.ArrayRankSpecifier(
                SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                SyntaxFactory.SeparatedList<ExpressionSyntax>(new SyntaxNodeOrToken[] { omittedArraySize, missingComma, omittedArraySize, missingComma, omittedArraySize, missingComma, omittedArraySize }),
                SyntaxFactory.Token(SyntaxKind.CloseBracketToken)
                );

            var sizes = spec.Sizes;
            Assert.Equal(4, sizes.Count);
            Assert.Equal(3, sizes.SeparatorCount);

            Assert.NotEqual(sizes[0], sizes[1]);
            Assert.NotEqual(sizes[0], sizes[2]);
            Assert.NotEqual(sizes[0], sizes[3]);
            Assert.NotEqual(sizes[1], sizes[2]);
            Assert.NotEqual(sizes[1], sizes[3]);
            Assert.NotEqual(sizes[2], sizes[3]);

            Assert.NotEqual(sizes.GetSeparator(0), sizes.GetSeparator(1));
            Assert.NotEqual(sizes.GetSeparator(0), sizes.GetSeparator(2));
            Assert.NotEqual(sizes.GetSeparator(1), sizes.GetSeparator(2));
        }

        [Fact]
        public void ZeroWidthStructuredTrivia()
        {
            // create zero width structured trivia (not sure how these come about but its not impossible)
            var zeroWidth = SyntaxFactory.ElseDirectiveTrivia(SyntaxFactory.MissingToken(SyntaxKind.HashToken), SyntaxFactory.MissingToken(SyntaxKind.ElseKeyword), SyntaxFactory.MissingToken(SyntaxKind.EndOfDirectiveToken), false, false);
            Assert.Equal(0, zeroWidth.Width);

            // create token with more than one instance of same zero width structured trivia!
            var someToken = SyntaxFactory.Identifier(default(SyntaxTriviaList), "foo", SyntaxFactory.TriviaList(SyntaxFactory.Trivia(zeroWidth), SyntaxFactory.Trivia(zeroWidth)));

            // create node with this token
            var someNode = SyntaxFactory.IdentifierName(someToken);

            Assert.Equal(2, someNode.Identifier.TrailingTrivia.Count);
            Assert.True(someNode.Identifier.TrailingTrivia[0].HasStructure);
            Assert.True(someNode.Identifier.TrailingTrivia[1].HasStructure);

            // prove that trivia have different identity
            Assert.False(someNode.Identifier.TrailingTrivia[0].Equals(someNode.Identifier.TrailingTrivia[1]));

            var tt0 = someNode.Identifier.TrailingTrivia[0];
            var tt1 = someNode.Identifier.TrailingTrivia[1];

            var str0 = tt0.GetStructure();
            var str1 = tt1.GetStructure();

            // prove that structures have different identity
            Assert.NotEqual(str0, str1);

            // prove that structured trivia can get back to original trivia with correct identity
            var tr0 = str0.ParentTrivia;
            Assert.Equal(tt0, tr0);

            var tr1 = str1.ParentTrivia;
            Assert.Equal(tt1, tr1);
        }

        [Fact]
        public void ZeroWidthStructuredTriviaOnZeroWidthToken()
        {
            // create zero width structured trivia (not sure how these come about but its not impossible)
            var zeroWidth = SyntaxFactory.ElseDirectiveTrivia(SyntaxFactory.MissingToken(SyntaxKind.HashToken), SyntaxFactory.MissingToken(SyntaxKind.ElseKeyword), SyntaxFactory.MissingToken(SyntaxKind.EndOfDirectiveToken), false, false);
            Assert.Equal(0, zeroWidth.Width);

            // create token with more than one instance of same zero width structured trivia!
            var someToken = SyntaxFactory.Identifier(default(SyntaxTriviaList), "", SyntaxFactory.TriviaList(SyntaxFactory.Trivia(zeroWidth), SyntaxFactory.Trivia(zeroWidth)));

            // create node with this token
            var someNode = SyntaxFactory.IdentifierName(someToken);

            Assert.Equal(2, someNode.Identifier.TrailingTrivia.Count);
            Assert.True(someNode.Identifier.TrailingTrivia[0].HasStructure);
            Assert.True(someNode.Identifier.TrailingTrivia[1].HasStructure);

            // prove that trivia have different identity
            Assert.False(someNode.Identifier.TrailingTrivia[0].Equals(someNode.Identifier.TrailingTrivia[1]));

            var tt0 = someNode.Identifier.TrailingTrivia[0];
            var tt1 = someNode.Identifier.TrailingTrivia[1];

            var str0 = tt0.GetStructure();
            var str1 = tt1.GetStructure();

            // prove that structures have different identity
            Assert.NotEqual(str0, str1);

            // prove that structured trivia can get back to original trivia with correct identity
            var tr0 = str0.ParentTrivia;
            Assert.Equal(tt0, tr0);

            var tr1 = str1.ParentTrivia;
            Assert.Equal(tt1, tr1);
        }

        [WorkItem(537059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537059")]
        [Fact]
        public void TestIncompleteDeclWithDotToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
@"
class Test
{
  int IX.FOO
");

            // Verify the kind of the CSharpSyntaxNode "int IX.FOO" is MethodDeclaration and NOT FieldDeclaration
            Assert.Equal(SyntaxKind.MethodDeclaration, tree.GetCompilationUnitRoot().ChildNodesAndTokens()[0].ChildNodesAndTokens()[3].Kind());
        }

        [WorkItem(538360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538360")]
        [Fact]
        public void TestGetTokensLanguageAny()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("class C {}");

            var actualTokens = syntaxTree.GetCompilationUnitRoot().DescendantTokens();

            var expectedTokenKinds = new SyntaxKind[]
            {
                SyntaxKind.ClassKeyword,
                SyntaxKind.IdentifierToken,
                SyntaxKind.OpenBraceToken,
                SyntaxKind.CloseBraceToken,
                SyntaxKind.EndOfFileToken,
            };

            Assert.Equal(expectedTokenKinds.Count(), actualTokens.Count()); //redundant but helps debug
            Assert.True(expectedTokenKinds.SequenceEqual(actualTokens.Select(t => t.Kind())));
        }

        [WorkItem(538360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538360")]
        [Fact]
        public void TestGetTokensCommonAny()
        {
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("class C {}");

            var actualTokens = syntaxTree.GetRoot().DescendantTokens(syntaxTree.GetRoot().FullSpan);

            var expectedTokenKinds = new SyntaxKind[]
            {
                SyntaxKind.ClassKeyword,
                SyntaxKind.IdentifierToken,
                SyntaxKind.OpenBraceToken,
                SyntaxKind.CloseBraceToken,
                SyntaxKind.EndOfFileToken,
            };

            Assert.Equal(expectedTokenKinds.Count(), actualTokens.Count()); //redundant but helps debug
            Assert.True(expectedTokenKinds.SequenceEqual(actualTokens.Select(t => (SyntaxKind)t.RawKind)));
        }

        [Fact]
        public void TestGetLocation()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class C { void F() { } }");
            dynamic root = tree.GetCompilationUnitRoot();
            MethodDeclarationSyntax method = root.Members[0].Members[0];

            var nodeLocation = method.GetLocation();
            Assert.True(nodeLocation.IsInSource);
            Assert.Equal(tree, nodeLocation.SourceTree);
            Assert.Equal(method.Span, nodeLocation.SourceSpan);

            var tokenLocation = method.Identifier.GetLocation();
            Assert.True(tokenLocation.IsInSource);
            Assert.Equal(tree, tokenLocation.SourceTree);
            Assert.Equal(method.Identifier.Span, tokenLocation.SourceSpan);

            var triviaLocation = method.ReturnType.GetLastToken().TrailingTrivia[0].GetLocation();
            Assert.True(triviaLocation.IsInSource);
            Assert.Equal(tree, triviaLocation.SourceTree);
            Assert.Equal(method.ReturnType.GetLastToken().TrailingTrivia[0].Span, triviaLocation.SourceSpan);

            var textSpan = new TextSpan(5, 10);
            var spanLocation = tree.GetLocation(textSpan);
            Assert.True(spanLocation.IsInSource);
            Assert.Equal(tree, spanLocation.SourceTree);
            Assert.Equal(textSpan, spanLocation.SourceSpan);
        }

        [Fact]
        public void TestReplaceNode()
        {
            var expr = SyntaxFactory.ParseExpression("a + b");
            var bex = (BinaryExpressionSyntax)expr;
            var expr2 = bex.ReplaceNode(bex.Right, SyntaxFactory.ParseExpression("c"));
            Assert.Equal("a + c", expr2.ToFullString());
        }

        [Fact]
        public void TestReplaceNodes()
        {
            var expr = SyntaxFactory.ParseExpression("a + b + c + d");

            // replace each expression with a parenthesized expression
            var replaced = expr.ReplaceNodes(
                expr.DescendantNodes().OfType<ExpressionSyntax>(),
                (node, rewritten) => SyntaxFactory.ParenthesizedExpression(rewritten));

            var replacedText = replaced.ToFullString();
            Assert.Equal("(((a )+ (b ))+ (c ))+ (d)", replacedText);
        }

        [Fact]
        public void TestReplaceNodeInListWithMultiple()
        {
            var invocation = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("m(a, b)");
            var argC = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("c"));
            var argD = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("d"));

            // replace first with multiple
            var newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments[0], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(c,d, b)", newNode.ToFullString());

            // replace last with multiple
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments[1], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(a, c,d)", newNode.ToFullString());

            // replace first with empty list
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments[0], new SyntaxNode[] { });
            Assert.Equal("m(b)", newNode.ToFullString());

            // replace last with empty list
            newNode = invocation.ReplaceNode(invocation.ArgumentList.Arguments[1], new SyntaxNode[] { });
            Assert.Equal("m(a)", newNode.ToFullString());
        }

        [Fact]
        public void TestReplaceNonListNodeWithMultiple()
        {
            var ifstatement = (IfStatementSyntax)SyntaxFactory.ParseStatement("if (a < b) m(c)");
            var then = ifstatement.Statement;

            var stat1 = SyntaxFactory.ParseStatement("m1(x)");
            var stat2 = SyntaxFactory.ParseStatement("m2(y)");

            // you cannot replace a node that is a single node member with multiple nodes
            Assert.Throws<InvalidOperationException>(() => ifstatement.ReplaceNode(then, new[] { stat1, stat2 }));

            // you cannot replace a node that is a single node member with an empty list
            Assert.Throws<InvalidOperationException>(() => ifstatement.ReplaceNode(then, new StatementSyntax[] { }));
        }

        [Fact]
        public void TestInsertNodesInList()
        {
            var invocation = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("m(a, b)");
            var argC = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("c"));
            var argD = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("d"));

            // insert before first
            var newNode = invocation.InsertNodesBefore(invocation.ArgumentList.Arguments[0], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(c,d,a, b)", newNode.ToFullString());

            // insert after first
            newNode = invocation.InsertNodesAfter(invocation.ArgumentList.Arguments[0], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(a,c,d, b)", newNode.ToFullString());

            // insert before last
            newNode = invocation.InsertNodesBefore(invocation.ArgumentList.Arguments[1], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(a,c,d, b)", newNode.ToFullString());

            // insert after last
            newNode = invocation.InsertNodesAfter(invocation.ArgumentList.Arguments[1], new SyntaxNode[] { argC, argD });
            Assert.Equal("m(a, b,c,d)", newNode.ToFullString());
        }

        [Fact]
        public void TestInsertNodesRelativeToNonListNode()
        {
            var ifstatement = (IfStatementSyntax)SyntaxFactory.ParseStatement("if (a < b) m(c)");
            var then = ifstatement.Statement;

            var stat1 = SyntaxFactory.ParseStatement("m1(x)");
            var stat2 = SyntaxFactory.ParseStatement("m2(y)");

            // you cannot insert nodes before/after a node that is not part of a list
            Assert.Throws<InvalidOperationException>(() => ifstatement.InsertNodesBefore(then, new[] { stat1, stat2 }));

            // you cannot insert nodes before/after a node that is not part of a list
            Assert.Throws<InvalidOperationException>(() => ifstatement.InsertNodesAfter(then, new StatementSyntax[] { }));
        }

        [Fact]
        public void TestReplaceStatementInListWithMultiple()
        {
            var block = (BlockSyntax)SyntaxFactory.ParseStatement("{ var x = 10; var y = 20; }");
            var stmt1 = SyntaxFactory.ParseStatement("var z = 30; ");
            var stmt2 = SyntaxFactory.ParseStatement("var q = 40; ");

            // replace first with multiple
            var newBlock = block.ReplaceNode(block.Statements[0], new[] { stmt1, stmt2 });
            Assert.Equal("{ var z = 30; var q = 40; var y = 20; }", newBlock.ToFullString());

            // replace second with multiple
            newBlock = block.ReplaceNode(block.Statements[1], new[] { stmt1, stmt2 });
            Assert.Equal("{ var x = 10; var z = 30; var q = 40; }", newBlock.ToFullString());

            // replace first with empty list
            newBlock = block.ReplaceNode(block.Statements[0], new SyntaxNode[] { });
            Assert.Equal("{ var y = 20; }", newBlock.ToFullString());

            // replace second with empty list
            newBlock = block.ReplaceNode(block.Statements[1], new SyntaxNode[] { });
            Assert.Equal("{ var x = 10; }", newBlock.ToFullString());
        }

        [Fact]
        public void TestInsertStatementsInList()
        {
            var block = (BlockSyntax)SyntaxFactory.ParseStatement("{ var x = 10; var y = 20; }");
            var stmt1 = SyntaxFactory.ParseStatement("var z = 30; ");
            var stmt2 = SyntaxFactory.ParseStatement("var q = 40; ");

            // insert before first
            var newBlock = block.InsertNodesBefore(block.Statements[0], new[] { stmt1, stmt2 });
            Assert.Equal("{ var z = 30; var q = 40; var x = 10; var y = 20; }", newBlock.ToFullString());

            // insert after first
            newBlock = block.InsertNodesAfter(block.Statements[0], new[] { stmt1, stmt2 });
            Assert.Equal("{ var x = 10; var z = 30; var q = 40; var y = 20; }", newBlock.ToFullString());

            // insert before last
            newBlock = block.InsertNodesBefore(block.Statements[1], new[] { stmt1, stmt2 });
            Assert.Equal("{ var x = 10; var z = 30; var q = 40; var y = 20; }", newBlock.ToFullString());

            // insert after last
            newBlock = block.InsertNodesAfter(block.Statements[1], new[] { stmt1, stmt2 });
            Assert.Equal("{ var x = 10; var y = 20; var z = 30; var q = 40; }", newBlock.ToFullString());
        }

        [Fact]
        public void TestReplaceSingleToken()
        {
            var expr = SyntaxFactory.ParseExpression("a + b");
            var bToken = expr.DescendantTokens().First(t => t.Text == "b");
            var expr2 = expr.ReplaceToken(bToken, SyntaxFactory.ParseToken("c"));
            Assert.Equal("a + c", expr2.ToString());
        }

        [Fact]
        public void TestReplaceMultipleTokens()
        {
            var expr = SyntaxFactory.ParseExpression("a + b + c");
            var d = SyntaxFactory.ParseToken("d ");
            var tokens = expr.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).ToList();
            var replaced = expr.ReplaceTokens(tokens, (tok, tok2) => d);
            Assert.Equal("d + d + d ", replaced.ToFullString());
        }

        [Fact]
        public void TestReplaceSingleTokenWithMultipleTokens()
        {
            var cu = SyntaxFactory.ParseCompilationUnit("private class C { }");
            var privateToken = ((ClassDeclarationSyntax)cu.Members[0]).Modifiers[0];
            var publicToken = SyntaxFactory.ParseToken("public ");
            var partialToken = SyntaxFactory.ParseToken("partial ");

            var cu1 = cu.ReplaceToken(privateToken, publicToken);
            Assert.Equal("public class C { }", cu1.ToFullString());

            var cu2 = cu.ReplaceToken(privateToken, new[] { publicToken, partialToken });
            Assert.Equal("public partial class C { }", cu2.ToFullString());

            var cu3 = cu.ReplaceToken(privateToken, new SyntaxToken[] { });
            Assert.Equal("class C { }", cu3.ToFullString());
        }

        [Fact]
        public void TestReplaceNonListTokenWithMultipleTokensFails()
        {
            var cu = SyntaxFactory.ParseCompilationUnit("private class C { }");
            var identifierC = cu.DescendantTokens().First(t => t.Text == "C");

            var identifierA = SyntaxFactory.ParseToken("A");
            var identifierB = SyntaxFactory.ParseToken("B");

            // you cannot replace a token that is a single token member with multiple tokens
            Assert.Throws<InvalidOperationException>(() => cu.ReplaceToken(identifierC, new[] { identifierA, identifierB }));

            // you cannot replace a token that is a single token member with an empty list of tokens
            Assert.Throws<InvalidOperationException>(() => cu.ReplaceToken(identifierC, new SyntaxToken[] { }));
        }

        [Fact]
        public void TestInsertTokens()
        {
            var cu = SyntaxFactory.ParseCompilationUnit("public class C { }");
            var publicToken = ((ClassDeclarationSyntax)cu.Members[0]).Modifiers[0];
            var partialToken = SyntaxFactory.ParseToken("partial ");
            var staticToken = SyntaxFactory.ParseToken("static ");

            var cu1 = cu.InsertTokensBefore(publicToken, new[] { staticToken });
            Assert.Equal("static public class C { }", cu1.ToFullString());

            var cu2 = cu.InsertTokensAfter(publicToken, new[] { staticToken });
            Assert.Equal("public static class C { }", cu2.ToFullString());
        }

        [Fact]
        public void TestInsertTokensRelativeToNonListToken()
        {
            var cu = SyntaxFactory.ParseCompilationUnit("public class C { }");
            var identifierC = cu.DescendantTokens().First(t => t.Text == "C");

            var identifierA = SyntaxFactory.ParseToken("A");
            var identifierB = SyntaxFactory.ParseToken("B");

            // you cannot insert a token before/after a token that is not part of a list of tokens
            Assert.Throws<InvalidOperationException>(() => cu.InsertTokensBefore(identifierC, new[] { identifierA, identifierB }));

            // you cannot insert a token before/after a token that is not part of a list of tokens
            Assert.Throws<InvalidOperationException>(() => cu.InsertTokensAfter(identifierC, new[] { identifierA, identifierB }));
        }

        [Fact]
        public void ReplaceMissingToken()
        {
            var text = "return x";
            var expr = SyntaxFactory.ParseStatement(text);

            var token = expr.DescendantTokens().First(t => t.IsMissing);

            var expr2 = expr.ReplaceToken(token, SyntaxFactory.Token(token.Kind()));
            var text2 = expr2.ToFullString();

            Assert.Equal("return x;", text2);
        }

        [Fact]
        public void ReplaceEndOfCommentToken()
        {
            var text = "/// Foo\r\n return x;";
            var expr = SyntaxFactory.ParseStatement(text);

            var tokens = expr.DescendantTokens(descendIntoTrivia: true).ToList();
            var token = tokens.First(t => t.Kind() == SyntaxKind.EndOfDocumentationCommentToken);

            var expr2 = expr.ReplaceToken(token, SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Whitespace("garbage")), token.Kind(), default(SyntaxTriviaList)));
            var text2 = expr2.ToFullString();

            Assert.Equal("/// Foo\r\ngarbage return x;", text2);
        }

        [Fact]
        public void ReplaceEndOfFileToken()
        {
            var text = "";
            var cu = SyntaxFactory.ParseCompilationUnit(text);
            var token = cu.DescendantTokens().Single(t => t.Kind() == SyntaxKind.EndOfFileToken);

            var cu2 = cu.ReplaceToken(token, SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Whitespace("  ")), token.Kind(), default(SyntaxTriviaList)));
            var text2 = cu2.ToFullString();

            Assert.Equal("  ", text2);
        }

        [Fact]
        public void TestReplaceTriviaDeep()
        {
            var expr = SyntaxFactory.ParseExpression("#if true\r\na + \r\n#endif\r\n + b");

            // get whitespace trivia inside structured directive trivia
            var deepTrivia = expr.GetDirectives().SelectMany(d => d.DescendantTrivia().Where(tr => tr.Kind() == SyntaxKind.WhitespaceTrivia)).ToList();

            // replace deep trivia with double-whitespace trivia
            var twoSpace = SyntaxFactory.Whitespace("  ");
            var expr2 = expr.ReplaceTrivia(deepTrivia, (tr, tr2) => twoSpace);

            Assert.Equal("#if  true\r\na + \r\n#endif\r\n + b", expr2.ToFullString());
        }

        [Fact]
        public void TestReplaceSingleTriviaInNode()
        {
            var expr = SyntaxFactory.ParseExpression("a + b");
            var trivia = expr.DescendantTokens().First(t => t.Text == "a").TrailingTrivia[0];
            var twoSpaces = SyntaxFactory.Whitespace("  ");
            var expr2 = expr.ReplaceTrivia(trivia, twoSpaces);
            Assert.Equal("a  + b", expr2.ToFullString());
        }

        [Fact]
        public void TestReplaceMultipleTriviaInNode()
        {
            var expr = SyntaxFactory.ParseExpression("a + b");
            var twoSpaces = SyntaxFactory.Whitespace("  ");
            var trivia = expr.DescendantTrivia().Where(tr => tr.IsKind(SyntaxKind.WhitespaceTrivia)).ToList();
            var replaced = expr.ReplaceTrivia(trivia, (tr, tr2) => twoSpaces);
            Assert.Equal("a  +  b", replaced.ToFullString());
        }

        [Fact]
        public void TestReplaceSingleTriviaWithMultipleTriviaInNode()
        {
            var ex = SyntaxFactory.ParseExpression("/* c */ identifier");
            var leadingTrivia = ex.GetLeadingTrivia();
            Assert.Equal(2, leadingTrivia.Count);
            var comment1 = leadingTrivia[0];
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, comment1.Kind());

            var newComment1 = SyntaxFactory.ParseLeadingTrivia("/* a */")[0];
            var newComment2 = SyntaxFactory.ParseLeadingTrivia("/* b */")[0];

            var ex1 = ex.ReplaceTrivia(comment1, newComment1);
            Assert.Equal("/* a */ identifier", ex1.ToFullString());

            var ex2 = ex.ReplaceTrivia(comment1, new[] { newComment1, newComment2 });
            Assert.Equal("/* a *//* b */ identifier", ex2.ToFullString());

            var ex3 = ex.ReplaceTrivia(comment1, new SyntaxTrivia[] { });
            Assert.Equal(" identifier", ex3.ToFullString());
        }

        [Fact]
        public void TestInsertTriviaInNode()
        {
            var ex = SyntaxFactory.ParseExpression("/* c */ identifier");
            var leadingTrivia = ex.GetLeadingTrivia();
            Assert.Equal(2, leadingTrivia.Count);
            var comment1 = leadingTrivia[0];
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, comment1.Kind());

            var newComment1 = SyntaxFactory.ParseLeadingTrivia("/* a */")[0];
            var newComment2 = SyntaxFactory.ParseLeadingTrivia("/* b */")[0];

            var ex1 = ex.InsertTriviaBefore(comment1, new[] { newComment1, newComment2 });
            Assert.Equal("/* a *//* b *//* c */ identifier", ex1.ToFullString());

            var ex2 = ex.InsertTriviaAfter(comment1, new[] { newComment1, newComment2 });
            Assert.Equal("/* c *//* a *//* b */ identifier", ex2.ToFullString());
        }

        [Fact]
        public void TestReplaceSingleTriviaInToken()
        {
            var id = SyntaxFactory.ParseToken("a ");
            var trivia = id.TrailingTrivia[0];
            var twoSpace = SyntaxFactory.Whitespace("  ");
            var id2 = id.ReplaceTrivia(trivia, twoSpace);
            Assert.Equal("a  ", id2.ToFullString());
        }

        [Fact]
        public void TestReplaceMultipleTriviaInToken()
        {
            var id = SyntaxFactory.ParseToken("a // foo\r\n");

            // replace each trivia with a single space
            var id2 = id.ReplaceTrivia(id.GetAllTrivia(), (tr, tr2) => SyntaxFactory.Space);

            // should be 3 spaces (one for original space, comment and end-of-line)
            Assert.Equal("a   ", id2.ToFullString());
        }

        [Fact]
        public void TestRemoveNodeInSeparatedList_KeepExteriorTrivia()
        {
            var expr = SyntaxFactory.ParseExpression("m(a, b, /* trivia */ c)");

            var b = expr.DescendantTokens().Where(t => t.Text == "b").Select(t => t.Parent.FirstAncestorOrSelf<ArgumentSyntax>()).FirstOrDefault();
            Assert.NotNull(b);

            var expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("m(a , /* trivia */ c)", text);
        }

        [Fact]
        public void TestRemoveNodeInSeparatedList_KeepNoTrivia()
        {
            var expr = SyntaxFactory.ParseExpression("m(a, b, /* trivia */ c)");

            var b = expr.DescendantTokens().Where(t => t.Text == "b").Select(t => t.Parent.FirstAncestorOrSelf<ArgumentSyntax>()).FirstOrDefault();
            Assert.NotNull(b);

            var expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepNoTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("m(a, /* trivia */ c)", text);
        }

        [Fact]
        public void TestRemoveOnlyNodeInSeparatedList_KeepExteriorTrivia()
        {
            var expr = SyntaxFactory.ParseExpression("m(/* before */ a /* after */)");

            var n = expr.DescendantTokens().Where(t => t.Text == "a").Select(t => t.Parent.FirstAncestorOrSelf<ArgumentSyntax>()).FirstOrDefault();
            Assert.NotNull(n);

            var expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("m(/* before */  /* after */)", text);
        }

        [Fact]
        public void TestRemoveFirstNodeInSeparatedList_KeepExteriorTrivia()
        {
            var expr = SyntaxFactory.ParseExpression("m(/* before */ a /* after */, b, c)");

            var n = expr.DescendantTokens().Where(t => t.Text == "a").Select(t => t.Parent.FirstAncestorOrSelf<ArgumentSyntax>()).FirstOrDefault();
            Assert.NotNull(n);

            var expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("m(/* before */  /* after */ b, c)", text);
        }

        [Fact]
        public void TestRemoveLastNodeInSeparatedList_KeepExteriorTrivia()
        {
            var expr = SyntaxFactory.ParseExpression("m(a, b, /* before */ c /* after */)");

            var n = expr.DescendantTokens().Where(t => t.Text == "c").Select(t => t.Parent.FirstAncestorOrSelf<ArgumentSyntax>()).FirstOrDefault();
            Assert.NotNull(n);

            var expr2 = expr.RemoveNode(n, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("m(a, b /* before */  /* after */)", text);
        }

        [Fact]
        public void TestRemoveNode_KeepNoTrivia()
        {
            var expr = SyntaxFactory.ParseStatement("{ a; b; /* trivia */ c }");

            var b = expr.DescendantTokens().Where(t => t.Text == "b").Select(t => t.Parent.FirstAncestorOrSelf<StatementSyntax>()).FirstOrDefault();
            Assert.NotNull(b);

            var expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepNoTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("{ a; c }", text);
        }

        [Fact]
        public void TestRemoveNode_KeepExteriorTrivia()
        {
            var expr = SyntaxFactory.ParseStatement("{ a; b; /* trivia */ c }");

            var b = expr.DescendantTokens().Where(t => t.Text == "b").Select(t => t.Parent.FirstAncestorOrSelf<StatementSyntax>()).FirstOrDefault();
            Assert.NotNull(b);

            var expr2 = expr.RemoveNode(b, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = expr2.ToFullString();
            Assert.Equal("{ a;  /* trivia */ c }", text);
        }

        [Fact]
        public void TestRemoveLastNode_KeepExteriorTrivia()
        {
            // this tests removing the last node in a non-terminal such that there is no token to the right of the removed
            // node to attach the kept trivia too.  The trivia must be attached to the previous token.

            var cu = SyntaxFactory.ParseCompilationUnit("class C { void M() { } /* trivia */ }");

            var m = cu.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(m);

            // remove the body block from the method syntax (since it can be set to null)
            var m2 = m.RemoveNode(m.Body, SyntaxRemoveOptions.KeepExteriorTrivia);

            var text = m2.ToFullString();

            Assert.Equal("void M()  /* trivia */ ", text);
        }

        [Fact]
        public void TestRemove_KeepExteriorTrivia_KeepUnbalancedDirectives()
        {
            var cu = SyntaxFactory.ParseCompilationUnit(@"
class C
{
// before
void M()
{
#region Fred
} // after
#endregion
}");

            var expectedText = @"
class C
{
// before
#region Fred
 // after
#endregion
}";


            var m = cu.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(m);

            var cu2 = cu.RemoveNode(m, SyntaxRemoveOptions.KeepExteriorTrivia | SyntaxRemoveOptions.KeepUnbalancedDirectives);

            var text = cu2.ToFullString();

            Assert.Equal(expectedText, text);
        }

        [Fact]
        public void TestRemove_KeepUnbalancedDirectives()
        {
            var cu = SyntaxFactory.ParseCompilationUnit(@"
class C
{
// before
#region Fred
// more before
void M()
{
} // after
#endregion
}".NormalizeLineEndings());

            var expectedText = @"
class C
{

#region Fred
#endregion
}".NormalizeLineEndings();

            var m = cu.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(m);

            var cu2 = cu.RemoveNode(m, SyntaxRemoveOptions.KeepUnbalancedDirectives);

            var text = cu2.ToFullString();

            Assert.Equal(expectedText, text);
        }

        [Fact]
        public void TestRemove_KeepDirectives()
        {
            var cu = SyntaxFactory.ParseCompilationUnit(@"
class C
{
// before
#region Fred
// more before
void M()
{
#if true
#endif
} // after
#endregion
}".NormalizeLineEndings());

            var expectedText = @"
class C
{

#region Fred
#if true
#endif
#endregion
}".NormalizeLineEndings();

            var m = cu.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.NotNull(m);

            var cu2 = cu.RemoveNode(m, SyntaxRemoveOptions.KeepDirectives);

            var text = cu2.ToFullString();

            Assert.Equal(expectedText, text);
        }

        [Fact]
        public void SeparatorsOfSeparatedSyntaxLists()
        {
            var s1 = "int foo(int a, int b, int c) {}";
            var tree = SyntaxFactory.ParseSyntaxTree(s1);

            dynamic root = tree.GetCompilationUnitRoot();
            MethodDeclarationSyntax method = root.Members[0];

            var list = (SeparatedSyntaxList<ParameterSyntax>)method.ParameterList.Parameters;

            Assert.Equal(((SyntaxToken)list.GetSeparator(0)).Kind(), SyntaxKind.CommaToken);
            Assert.Equal(((SyntaxToken)list.GetSeparator(1)).Kind(), SyntaxKind.CommaToken);

            foreach (var index in new int[] { -1, 2 })
            {
                bool exceptionThrown = false;
                try
                {
                    var unused = list.GetSeparator(2);
                }
                catch (ArgumentOutOfRangeException)
                {
                    exceptionThrown = true;
                }
                Assert.True(exceptionThrown);
            }

            var internalParameterList = (InternalSyntax.ParameterListSyntax)method.ParameterList.Green;
            var internalParameters = internalParameterList.Parameters;

            Assert.Equal(internalParameters.SeparatorCount, 2);
            Assert.Equal((new SyntaxToken(internalParameters.GetSeparator(0))).Kind(), SyntaxKind.CommaToken);
            Assert.Equal((new SyntaxToken(internalParameters.GetSeparator(1))).Kind(), SyntaxKind.CommaToken);

            Assert.Equal(internalParameters.Count, 3);
            Assert.Equal(internalParameters[0].Identifier.ValueText, "a");
            Assert.Equal(internalParameters[1].Identifier.ValueText, "b");
            Assert.Equal(internalParameters[2].Identifier.ValueText, "c");
        }

        [Fact]
        public void ThrowIfUnderlyingNodeIsNullForList()
        {
            var list = new SyntaxNodeOrTokenList();
            Assert.Equal(list.Count, 0);

            foreach (var index in new int[] { -1, 0, 23 })
            {
                bool exceptionThrown = false;
                try
                {
                    var unused = list[0];
                }
                catch (ArgumentOutOfRangeException)
                {
                    exceptionThrown = true;
                }
                Assert.True(exceptionThrown);
            }
        }

        [WorkItem(541188, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541188")]
        [Fact]
        public void GetDiagnosticsOnMissingToken()
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(@"c1<t");
            var token = syntaxTree.FindNodeOrTokenByKind(SyntaxKind.GreaterThanToken);
            var diag = syntaxTree.GetDiagnostics(token).ToList();

            Assert.True(token.IsMissing);
            Assert.Equal(1, diag.Count);
        }

        [WorkItem(541325, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541325")]
        [Fact]
        public void GetDiagnosticsOnMissingToken2()
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(@"
class Base<T>
{
    public virtual int Property
    {
        get { return 0; }
        // Note: Repro for bug 7990 requires a missing close brace token i.e. missing } below
        set { 
    }
    public virtual void Method()
    {
    }
}");
            foreach (var t in syntaxTree.GetCompilationUnitRoot().DescendantTokens())
            {
                // Bug 7990: Below for loop is an infinite loop.
                foreach (var e in syntaxTree.GetDiagnostics(t))
                {
                }
            }

            // TODO: Please add meaningful checks once the above deadlock issue is fixed.
        }

        [WorkItem(541587, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541587")]
        [Fact]
        public void GetDiagnosticsOnMissingToken3()
        {
            const string code = @"class c2 4";
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code);
            var token = syntaxTree.GetCompilationUnitRoot().FindToken(code.IndexOf('4'));
            var diag = syntaxTree.GetDiagnostics(token).ToList();

            Assert.True(token.IsMissing);
            Assert.Equal(2, diag.Count);
        }

        [WorkItem(541648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541648")]
        [Fact]
        public void GetDiagnosticsOnMissingToken4()
        {
            string code = @"
public class MyClass
{	
using Lib;
using Lib2;

public class Test1
{
}
}";
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code);
            var token = syntaxTree.GetCompilationUnitRoot().FindToken(code.IndexOf("using Lib;", StringComparison.Ordinal));
            var diag = syntaxTree.GetDiagnostics(token).ToList();

            Assert.True(token.IsMissing);
            Assert.Equal(3, diag.Count);
        }

        [WorkItem(541630, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541630")]
        [Fact]
        public void GetDiagnosticsOnBadReferenceDirective()
        {
            string code = @"class c1
{
    #r
    void m1()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var trivia = tree.GetCompilationUnitRoot().FindTrivia(code.IndexOf("#r", StringComparison.Ordinal)); // ReferenceDirective.

            foreach (var diag in tree.GetDiagnostics(trivia))
            {
                Assert.NotNull(diag);
                // TODO: Please add any additional validations if necessary.
            }
        }

        [WorkItem(528626, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528626")]
        [Fact]
        public void SpanOfNodeWithMissingChildren()
        {
            string code = @"delegate = 1;";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            var compilationUnit = tree.GetCompilationUnitRoot();
            var delegateDecl = (DelegateDeclarationSyntax)compilationUnit.Members[0];
            var paramList = delegateDecl.ParameterList;

            // For (non-EOF) tokens, IsMissing is true if and only if Width is 0.
            Assert.True(compilationUnit.DescendantTokens(node => true).
                Where(token => token.Kind() != SyntaxKind.EndOfFileToken).
                All(token => token.IsMissing == (token.Width == 0)));

            // For non-terminals, Is true if Width is 0, but the converse may not hold.
            Assert.True(paramList.IsMissing);
            Assert.NotEqual(0, paramList.Width);
            Assert.NotEqual(0, paramList.FullWidth);
        }

        [WorkItem(542457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542457")]
        [Fact]
        public void AddMethodModifier()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class Program
{
    static void Main(string[] args)
    {
    }
}");
            var compilationUnit = tree.GetCompilationUnitRoot();
            var @class = (ClassDeclarationSyntax)compilationUnit.Members.Single();
            var method = (MethodDeclarationSyntax)@class.Members.Single();
            var newModifiers = method.Modifiers.Add(SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.UnsafeKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));
            Assert.Equal("    static unsafe ", newModifiers.ToFullString());
            Assert.Equal(2, newModifiers.Count);
            Assert.Equal(SyntaxKind.StaticKeyword, newModifiers[0].Kind());
            Assert.Equal(SyntaxKind.UnsafeKeyword, newModifiers[1].Kind());
        }

        [Fact]
        public void SeparatedSyntaxListValidation()
        {
            var intType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
            var commaToken = SyntaxFactory.Token(SyntaxKind.CommaToken);

            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(intType);
            SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { intType, commaToken });
            SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { intType, commaToken, intType });
            SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { intType, commaToken, intType, commaToken });

            Assert.Throws<ArgumentException>(() => SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { commaToken }));
            Assert.Throws<ArgumentException>(() => SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { intType, commaToken, commaToken }));
            Assert.Throws<ArgumentException>(() => SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] { intType, intType }));
        }

        [WorkItem(543310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543310")]
        [Fact]
        public void SyntaxDotParseCompilationUnitContainingOnlyWhitespace()
        {
            var node = SyntaxFactory.ParseCompilationUnit("  ");
            Assert.True(node.HasLeadingTrivia);
            Assert.Equal(1, node.GetLeadingTrivia().Count);
            Assert.Equal(1, node.DescendantTrivia().Count());
            Assert.Equal("  ", node.GetLeadingTrivia().First().ToString());
        }

        [WorkItem(543310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543310")]
        [Fact]
        public void SyntaxTreeDotParseCompilationUnitContainingOnlyWhitespace()
        {
            var node = SyntaxFactory.ParseSyntaxTree("  ").GetCompilationUnitRoot();
            Assert.True(node.HasLeadingTrivia);
            Assert.Equal(1, node.GetLeadingTrivia().Count);
            Assert.Equal(1, node.DescendantTrivia().Count());
            Assert.Equal("  ", node.GetLeadingTrivia().First().ToString());
        }

        [Fact]
        public void SyntaxNodeAndTokenToString()
        {
            var text = @"class A { }";
            var root = SyntaxFactory.ParseCompilationUnit(text);
            var children = root.DescendantNodesAndTokens();

            var nodeOrToken = children.First();
            Assert.Equal("class A { }", nodeOrToken.ToString());
            Assert.Equal(text, nodeOrToken.ToString());

            var node = (SyntaxNode)children.First(n => n.IsNode);
            Assert.Equal("class A { }", node.ToString());
            Assert.Equal(text, node.ToFullString());

            var token = (SyntaxToken)children.First(n => n.IsToken);
            Assert.Equal("class", token.ToString());
            Assert.Equal("class ", token.ToFullString());

            var trivia = root.DescendantTrivia().First();
            Assert.Equal(" ", trivia.ToString());
            Assert.Equal(" ", trivia.ToFullString());
        }

        [WorkItem(545116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545116")]
        [Fact]
        public void FindTriviaOutsideNode()
        {
            var text = @"// This is trivia
class C
{
    static void Main()
    {
    }
}
";
            var root = SyntaxFactory.ParseCompilationUnit(text);
            Assert.InRange(0, root.FullSpan.Start, root.FullSpan.End);
            var rootTrivia = root.FindTrivia(0);
            Assert.Equal("// This is trivia", rootTrivia.ToString().Trim());

            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            Assert.NotInRange(0, method.FullSpan.Start, method.FullSpan.End);
            var methodTrivia = method.FindTrivia(0);
            Assert.Equal(default(SyntaxTrivia), methodTrivia);
        }

        [Fact]
        public void TestSyntaxTriviaListEquals()
        {
            var emptyWhitespace = SyntaxFactory.Whitespace("");
            var emptyToken = SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken).WithTrailingTrivia(emptyWhitespace, emptyWhitespace);
            var emptyTokenList = SyntaxFactory.TokenList(emptyToken, emptyToken);

            // elements should be not equal
            Assert.NotEqual(emptyTokenList[0].TrailingTrivia[0], emptyTokenList[1].TrailingTrivia[0]);

            // lists should be not equal
            Assert.NotEqual(emptyTokenList[0].TrailingTrivia, emptyTokenList[1].TrailingTrivia);

            // Two lists with the same parent node, but different indexes should NOT be the same.
            var emptyTriviaList = SyntaxFactory.TriviaList(emptyWhitespace, emptyWhitespace);
            emptyToken = emptyToken.WithLeadingTrivia(emptyTriviaList).WithTrailingTrivia(emptyTriviaList);

            // elements should be not equal
            Assert.NotEqual(emptyToken.LeadingTrivia[0], emptyToken.TrailingTrivia[0]);

            // lists should be not equal
            Assert.NotEqual(emptyToken.LeadingTrivia, emptyToken.TrailingTrivia);
        }

        [Fact]
        public void Test_SyntaxTree_ParseTextInvalidArguments()
        {
            // Invalid arguments - Validate Exceptions     
            Assert.Throws<System.ArgumentNullException>(delegate
            {
                var treeFromSource_invalid2 = SyntaxFactory.ParseSyntaxTree("", path: null);
            });

            Assert.Throws<System.ArgumentNullException>(delegate
            {
                SourceText st = null;
                var treeFromSource_invalid2 = SyntaxFactory.ParseSyntaxTree(st);
            });
        }

        [Fact]
        public void TestSyntaxTree_Changes()
        {
            string SourceText = @"using System;
using System.Linq;
using System.Collections;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }";

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(SourceText);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            // Get the Imports Clauses
            var FirstUsingClause = root.Usings[0];
            var SecondUsingClause = root.Usings[1];
            var ThirdUsingClause = root.Usings[2];

            var ChangesForDifferentTrees = FirstUsingClause.SyntaxTree.GetChanges(SecondUsingClause.SyntaxTree);
            Assert.Equal(0, ChangesForDifferentTrees.Count);

            // Do a transform to Replace and Existing Tree
            NameSyntax name = SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Collections.Generic"));

            UsingDirectiveSyntax newUsingClause = ThirdUsingClause.WithName(name);

            // Replace Node with a different Imports Clause
            root = root.ReplaceNode(ThirdUsingClause, newUsingClause);

            var ChangesFromTransform = ThirdUsingClause.SyntaxTree.GetChanges(newUsingClause.SyntaxTree);
            Assert.Equal(2, ChangesFromTransform.Count);

            // Using the Common Syntax Changes Method
            SyntaxTree x = ThirdUsingClause.SyntaxTree;
            SyntaxTree y = newUsingClause.SyntaxTree;

            var changes2UsingCommonSyntax = x.GetChanges(y);
            Assert.Equal(2, changes2UsingCommonSyntax.Count);

            // Verify Changes from CS Specific SyntaxTree and Common SyntaxTree are the same
            Assert.Equal(ChangesFromTransform, changes2UsingCommonSyntax);
        }

        [Fact, WorkItem(658329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658329")]
        public void TestSyntaxTree_GetChangesInvalid()
        {
            string SourceText = @"using System;
using System.Linq;
using System.Collections;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }";

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(SourceText);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            // Get the Imports Clauses
            var FirstUsingClause = root.Usings[0];
            var SecondUsingClause = root.Usings[1];
            var ThirdUsingClause = root.Usings[2];

            var ChangesForDifferentTrees = FirstUsingClause.SyntaxTree.GetChanges(SecondUsingClause.SyntaxTree);
            Assert.Equal(0, ChangesForDifferentTrees.Count);

            // With null tree
            SyntaxTree BlankTree = null;
            Assert.Throws<ArgumentNullException>(() => FirstUsingClause.SyntaxTree.GetChanges(BlankTree));
        }

        [Fact, WorkItem(658329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/658329")]
        public void TestSyntaxTree_GetChangedSpansInvalid()
        {
            string SourceText = @"using System;
using System.Linq;
using System.Collections;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }";

            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(SourceText);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            // Get the Imports Clauses
            var FirstUsingClause = root.Usings[0];
            var SecondUsingClause = root.Usings[1];
            var ThirdUsingClause = root.Usings[2];

            var ChangesForDifferentTrees = FirstUsingClause.SyntaxTree.GetChangedSpans(SecondUsingClause.SyntaxTree);
            Assert.Equal(0, ChangesForDifferentTrees.Count);

            // With null tree
            SyntaxTree BlankTree = null;
            Assert.Throws<ArgumentNullException>(() => FirstUsingClause.SyntaxTree.GetChangedSpans(BlankTree));
        }

        [Fact]
        public void TestTriviaExists()
        {
            // token constructed using factory w/o specifying trivia (should have zero-width elastic trivia)
            var idToken = SyntaxFactory.Identifier("foo");
            Assert.Equal(true, idToken.HasLeadingTrivia);
            Assert.Equal(1, idToken.LeadingTrivia.Count);
            Assert.Equal(0, idToken.LeadingTrivia.Span.Length); // zero-width elastic trivia
            Assert.Equal(true, idToken.HasTrailingTrivia);
            Assert.Equal(1, idToken.TrailingTrivia.Count);
            Assert.Equal(0, idToken.TrailingTrivia.Span.Length); // zero-width elastic trivia

            // token constructed by parser w/o trivia
            idToken = SyntaxFactory.ParseToken("x");
            Assert.Equal(false, idToken.HasLeadingTrivia);
            Assert.Equal(0, idToken.LeadingTrivia.Count);
            Assert.Equal(false, idToken.HasTrailingTrivia);
            Assert.Equal(0, idToken.TrailingTrivia.Count);

            // token constructed by parser with trivia
            idToken = SyntaxFactory.ParseToken(" x  ");
            Assert.Equal(true, idToken.HasLeadingTrivia);
            Assert.Equal(1, idToken.LeadingTrivia.Count);
            Assert.Equal(1, idToken.LeadingTrivia.Span.Length);
            Assert.Equal(true, idToken.HasTrailingTrivia);
            Assert.Equal(1, idToken.TrailingTrivia.Count);
            Assert.Equal(2, idToken.TrailingTrivia.Span.Length);

            // node constructed using factory w/o specifying trivia
            SyntaxNode namedNode = SyntaxFactory.IdentifierName("foo");
            Assert.Equal(true, namedNode.HasLeadingTrivia);
            Assert.Equal(1, namedNode.GetLeadingTrivia().Count);
            Assert.Equal(0, namedNode.GetLeadingTrivia().Span.Length);  // zero-width elastic trivia
            Assert.Equal(true, namedNode.HasTrailingTrivia);
            Assert.Equal(1, namedNode.GetTrailingTrivia().Count);
            Assert.Equal(0, namedNode.GetTrailingTrivia().Span.Length);  // zero-width elastic trivia

            // node constructed by parse w/o trivia
            namedNode = SyntaxFactory.ParseExpression("foo");
            Assert.Equal(false, namedNode.HasLeadingTrivia);
            Assert.Equal(0, namedNode.GetLeadingTrivia().Count);
            Assert.Equal(false, namedNode.HasTrailingTrivia);
            Assert.Equal(0, namedNode.GetTrailingTrivia().Count);

            // node constructed by parse with trivia
            namedNode = SyntaxFactory.ParseExpression(" foo  ");
            Assert.Equal(true, namedNode.HasLeadingTrivia);
            Assert.Equal(1, namedNode.GetLeadingTrivia().Count);
            Assert.Equal(1, namedNode.GetLeadingTrivia().Span.Length);
            Assert.Equal(true, namedNode.HasTrailingTrivia);
            Assert.Equal(1, namedNode.GetTrailingTrivia().Count);
            Assert.Equal(2, namedNode.GetTrailingTrivia().Span.Length);

            // nodeOrToken with token constructed from factory w/o specifying trivia
            SyntaxNodeOrToken nodeOrToken = SyntaxFactory.Identifier("foo");
            Assert.Equal(true, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(0, nodeOrToken.GetLeadingTrivia().Span.Length); // zero-width elastic trivia
            Assert.Equal(true, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(1, nodeOrToken.GetTrailingTrivia().Count);
            Assert.Equal(0, nodeOrToken.GetTrailingTrivia().Span.Length); // zero-width elastic trivia

            // nodeOrToken with node constructed from factory w/o specifying trivia
            nodeOrToken = SyntaxFactory.IdentifierName("foo");
            Assert.Equal(true, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(0, nodeOrToken.GetLeadingTrivia().Span.Length); // zero-width elastic trivia
            Assert.Equal(true, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(1, nodeOrToken.GetTrailingTrivia().Count);
            Assert.Equal(0, nodeOrToken.GetTrailingTrivia().Span.Length); // zero-width elastic trivia

            // nodeOrToken with token parsed from factory w/o trivia
            nodeOrToken = SyntaxFactory.ParseToken("foo");
            Assert.Equal(false, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(0, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(false, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(0, nodeOrToken.GetTrailingTrivia().Count);

            // nodeOrToken with node parsed from factory w/o trivia
            nodeOrToken = SyntaxFactory.ParseExpression("foo");
            Assert.Equal(false, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(0, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(false, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(0, nodeOrToken.GetTrailingTrivia().Count);

            // nodeOrToken with token parsed from factory with trivia
            nodeOrToken = SyntaxFactory.ParseToken(" foo  ");
            Assert.Equal(true, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Span.Length); // zero-width elastic trivia
            Assert.Equal(true, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(1, nodeOrToken.GetTrailingTrivia().Count);
            Assert.Equal(2, nodeOrToken.GetTrailingTrivia().Span.Length); // zero-width elastic trivia

            // nodeOrToken with node parsed from factory with trivia
            nodeOrToken = SyntaxFactory.ParseExpression(" foo  ");
            Assert.Equal(true, nodeOrToken.HasLeadingTrivia);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Count);
            Assert.Equal(1, nodeOrToken.GetLeadingTrivia().Span.Length); // zero-width elastic trivia
            Assert.Equal(true, nodeOrToken.HasTrailingTrivia);
            Assert.Equal(1, nodeOrToken.GetTrailingTrivia().Count);
            Assert.Equal(2, nodeOrToken.GetTrailingTrivia().Span.Length); // zero-width elastic trivia
        }
    }
}
