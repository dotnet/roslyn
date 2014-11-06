// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace APISampleUnitTestsCS
{
    [TestClass]
    public class Parsing
    {
        [TestMethod]
        public void TextParseTreeRoundtrip()
        {
            string text = "class C { void M() { } } // exact text round trip, including comments and whitespace";
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.AreEqual(text, tree.ToString());
        }

        [TestMethod]
        public void DetermineValidIdentifierName()
        {
            ValidIdentifier("@class", true);
            ValidIdentifier("class", false);
        }

        private void ValidIdentifier(string identifier, bool expectedValid)
        {
            SyntaxToken token = SyntaxFactory.ParseToken(identifier);
            Assert.AreEqual(expectedValid,
                token.CSharpKind() == SyntaxKind.IdentifierToken && token.Span.Length == identifier.Length);
        }

        [TestMethod]
        public void SyntaxFactsMethods()
        {
            Assert.AreEqual("protected internal", SyntaxFacts.GetText(Accessibility.ProtectedOrInternal));
            Assert.AreEqual("??", SyntaxFacts.GetText(SyntaxKind.QuestionQuestionToken));
            Assert.AreEqual("this", SyntaxFacts.GetText(SyntaxKind.ThisKeyword));

            Assert.AreEqual(SyntaxKind.CharacterLiteralExpression, SyntaxFacts.GetLiteralExpression(SyntaxKind.CharacterLiteralToken));
            Assert.AreEqual(SyntaxKind.CoalesceExpression, SyntaxFacts.GetBinaryExpression(SyntaxKind.QuestionQuestionToken));
            Assert.AreEqual(SyntaxKind.None, SyntaxFacts.GetBinaryExpression(SyntaxKind.UndefDirectiveTrivia));
            Assert.AreEqual(false, SyntaxFacts.IsPunctuation(SyntaxKind.StringLiteralToken));
        }

        [TestMethod]
        public void ParseTokens()
        {
            IEnumerable<SyntaxToken> tokens = SyntaxFactory.ParseTokens("class C { // trivia");
            IEnumerable<string> fullTexts = tokens.Select(token => token.ToFullString());

            Assert.IsTrue(fullTexts.SequenceEqual(new[]
            {
                "class ",
                "C ",
                "{ // trivia",
                "" // EOF
            }));
        }

        [TestMethod]
        public void ParseExpression()
        {
            ExpressionSyntax expression = SyntaxFactory.ParseExpression("1 + 2");
            if (expression.CSharpKind() == SyntaxKind.AddExpression)
            {
                BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax)expression;
                SyntaxToken operatorToken = binaryExpression.OperatorToken;
                Assert.AreEqual("+", operatorToken.ToString());

                ExpressionSyntax left = binaryExpression.Left;
                Assert.AreEqual(SyntaxKind.NumericLiteralExpression, left.CSharpKind());
            }
        }

        [TestMethod]
        public void IncrementalParse()
        {
            var oldText = SourceText.From("class C { }");
            var newText = oldText.WithChanges(new TextChange(new TextSpan(9, 0), "void M() { } "));
            
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newTree = tree.WithChangedText(newText);

            Assert.AreEqual(newText.ToString(), newTree.ToString());
        }

        [TestMethod]
        public void PreprocessorDirectives()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(@"#if true
class A { }
#else
class B { }
#endif");
            SyntaxToken eof = tree.GetRoot().FindToken(tree.GetText().Length, false);
            Assert.AreEqual(true, eof.HasLeadingTrivia);
            Assert.AreEqual(false, eof.HasTrailingTrivia);
            Assert.AreEqual(true, eof.ContainsDirectives);

            SyntaxTriviaList trivia = eof.LeadingTrivia;
            Assert.AreEqual(3, trivia.Count);
            Assert.AreEqual("#else", trivia.ElementAt(0).ToString());
            Assert.AreEqual(SyntaxKind.DisabledTextTrivia, trivia.ElementAt(1).CSharpKind());
            Assert.AreEqual("#endif", trivia.ElementAt(2).ToString());

            DirectiveTriviaSyntax directive = tree.GetRoot().GetLastDirective();
            Assert.AreEqual("endif", directive.DirectiveNameToken.Value);

            directive = directive.GetPreviousDirective();
            Assert.AreEqual("else", directive.DirectiveNameToken.Value);

            // List<DirectiveSyntax> relatedDirectives = directive.GetRelatedDirectives();
            // Assert.AreEqual(3, relatedDirectives.Count);
        }
    }
}
