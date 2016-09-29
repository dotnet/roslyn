// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace APISampleUnitTestsCS
{
    public class Parsing
    {
        [Fact]
        public void TextParseTreeRoundtrip()
        {
            string text = "class C { void M() { } } // exact text round trip, including comments and whitespace";
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, tree.ToString());
        }

        [Fact]
        public void DetermineValidIdentifierName()
        {
            ValidIdentifier("@class", true);
            ValidIdentifier("class", false);
        }

        private void ValidIdentifier(string identifier, bool expectedValid)
        {
            SyntaxToken token = SyntaxFactory.ParseToken(identifier);
            Assert.Equal(expectedValid,
                token.Kind() == SyntaxKind.IdentifierToken && token.Span.Length == identifier.Length);
        }

        [Fact]
        public void SyntaxFactsMethods()
        {
            Assert.Equal("protected internal", SyntaxFacts.GetText(Accessibility.ProtectedOrInternal));
            Assert.Equal("??", SyntaxFacts.GetText(SyntaxKind.QuestionQuestionToken));
            Assert.Equal("this", SyntaxFacts.GetText(SyntaxKind.ThisKeyword));

            Assert.Equal(SyntaxKind.CharacterLiteralExpression, SyntaxFacts.GetLiteralExpression(SyntaxKind.CharacterLiteralToken));
            Assert.Equal(SyntaxKind.CoalesceExpression, SyntaxFacts.GetBinaryExpression(SyntaxKind.QuestionQuestionToken));
            Assert.Equal(SyntaxKind.None, SyntaxFacts.GetBinaryExpression(SyntaxKind.UndefDirectiveTrivia));
            Assert.Equal(false, SyntaxFacts.IsPunctuation(SyntaxKind.StringLiteralToken));
        }

        [Fact]
        public void ParseTokens()
        {
            IEnumerable<SyntaxToken> tokens = SyntaxFactory.ParseTokens("class C { // trivia");
            IEnumerable<string> fullTexts = tokens.Select(token => token.ToFullString());

            Assert.True(fullTexts.SequenceEqual(new[]
            {
                "class ",
                "C ",
                "{ // trivia",
                "" // EOF
            }));
        }

        [Fact]
        public void ParseExpression()
        {
            ExpressionSyntax expression = SyntaxFactory.ParseExpression("1 + 2");
            if (expression.Kind() == SyntaxKind.AddExpression)
            {
                BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax)expression;
                SyntaxToken operatorToken = binaryExpression.OperatorToken;
                Assert.Equal("+", operatorToken.ToString());

                ExpressionSyntax left = binaryExpression.Left;
                Assert.Equal(SyntaxKind.NumericLiteralExpression, left.Kind());
            }
        }

        [Fact]
        public void IncrementalParse()
        {
            var oldText = SourceText.From("class C { }");
            var newText = oldText.WithChanges(new TextChange(new TextSpan(9, 0), "void M() { } "));
            
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newTree = tree.WithChangedText(newText);

            Assert.Equal(newText.ToString(), newTree.ToString());
        }

        [Fact]
        public void PreprocessorDirectives()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(@"#if true
class A { }
#else
class B { }
#endif");
            SyntaxToken eof = tree.GetRoot().FindToken(tree.GetText().Length, false);
            Assert.Equal(true, eof.HasLeadingTrivia);
            Assert.Equal(false, eof.HasTrailingTrivia);
            Assert.Equal(true, eof.ContainsDirectives);

            SyntaxTriviaList trivia = eof.LeadingTrivia;
            Assert.Equal(3, trivia.Count);
            Assert.Equal("#else", trivia.ElementAt(0).ToString());
            Assert.Equal(SyntaxKind.DisabledTextTrivia, trivia.ElementAt(1).Kind());
            Assert.Equal("#endif", trivia.ElementAt(2).ToString());

            DirectiveTriviaSyntax directive = tree.GetRoot().GetLastDirective();
            Assert.Equal("endif", directive.DirectiveNameToken.Value);

            directive = directive.GetPreviousDirective();
            Assert.Equal("else", directive.DirectiveNameToken.Value);

            // List<DirectiveSyntax> relatedDirectives = directive.GetRelatedDirectives();
            // Assert.Equal(3, relatedDirectives.Count);
        }
    }
}
