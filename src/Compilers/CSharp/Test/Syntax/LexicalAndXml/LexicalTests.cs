// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LexicalTests
    {
        private readonly CSharpParseOptions _options;
        private readonly CSharpParseOptions _binaryOptions;
        private readonly CSharpParseOptions _underscoreOptions;
        private readonly CSharpParseOptions _binaryUnderscoreOptions;

        public LexicalTests()
        {
            _options = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp3);
            var binaryLiterals = new[] { new KeyValuePair<string, string>("binaryLiterals", "true") };
            var digitSeparators = new[] { new KeyValuePair<string, string>("digitSeparators", "true") };
            _binaryOptions = _options.WithFeatures(binaryLiterals);
            _underscoreOptions = _options.WithFeatures(digitSeparators);
            _binaryUnderscoreOptions = _options.WithFeatures(binaryLiterals.Concat(digitSeparators));
        }

        private IEnumerable<SyntaxToken> Lex(string text, CSharpParseOptions options = null)
        {
            return SyntaxFactory.ParseTokens(text, options: options);
        }

        private SyntaxToken LexToken(string text, CSharpParseOptions options = null)
        {
            SyntaxToken result = default(SyntaxToken);
            foreach (var token in Lex(text, options))
            {
                if (result.Kind() == SyntaxKind.None)
                {
                    result = token;
                }
                else if (token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    continue;
                }
                else
                {
                    Assert.True(false, "More than one token was lexed: " + token);
                }
            }
            if (result.Kind() == SyntaxKind.None)
            {
                Assert.True(false, "No tokens were lexed");
            }
            return result;
        }

        private SyntaxToken DebuggerLex(string text)
        {
            using (var lexer = new InternalSyntax.Lexer(SourceText.From(text), _options))
            {
                return new SyntaxToken(lexer.Lex(InternalSyntax.LexerMode.DebuggerSyntax));
            }
        }

        private SyntaxToken DebuggerLexToken(string text)
        {
            return DebuggerLex(text);
        }

        private IEnumerable<InternalSyntax.BlendedNode> Blend(string text)
        {
            using (var lexer = new InternalSyntax.Lexer(SourceText.From(text), _options))
            {
                var blender = new InternalSyntax.Blender(lexer, null, null);
                InternalSyntax.BlendedNode result;
                do
                {
                    result = blender.ReadToken(InternalSyntax.LexerMode.Syntax);
                    blender = result.Blender;
                    yield return result;
                }
                while (result.Token.Kind != SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestSingleLineComment()
        {
            var text = "// comment";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            Assert.Equal(text.Length, token.FullWidth);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.LeadingTrivia.ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestSingleLineCommentWithUnicode()
        {
            var text = "// ҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉ ҉҉̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌… ̒̓̔̕̚ ̍̎̏̐̑̒̓̔̕̚̕̚ ̡̢̛̗̘̙̜̝̞̟̠̊̋̌̍̎̏̚ ̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎… ̕̚̕̚ ̔̕̚̕̚҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉ ";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            Assert.Equal(text.Length, token.FullWidth);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.LeadingTrivia.ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestSingleLineXmlComment()
        {
            string text = "/// xml comment";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            Assert.Equal(text.Length, token.FullWidth);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.LeadingTrivia.ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.SingleLineDocumentationCommentTrivia, trivia[0].Kind());
        }

        [WorkItem(537500, "DevDiv")]
        [Fact]
        [Trait("Feature", "Comments")]
        public void TestSingleLineDocCommentFollowedBySlash()
        {
            string text = "////test";
            var token = LexToken(text);

            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            Assert.False(token.ContainsDiagnostics);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);

            var trivia = token.LeadingTrivia.Single();
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, trivia.Kind());
            Assert.Equal(text, trivia.ToFullString());
            Assert.Equal(text, trivia.ToString());
            Assert.False(trivia.ContainsDiagnostics);
            Assert.Equal(0, errors.Length);
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestMultiLineCommentOnOneLine()
        {
            var text = "/* comment */";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.GetLeadingTrivia().ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestMultiLineCommentOnMultipleLines()
        {
            var text =
@"/* 
 comment 
 on many lines
*/";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.GetLeadingTrivia().ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestMultiLineXmlCommentOnMultipleLines()
        {
            var text =
@"/** 
 xml comment 
 on many lines
**/";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.GetLeadingTrivia().ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.MultiLineDocumentationCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestUnterminatedMultiLineComment()
        {
            var text = "/* comment";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            var trivia = token.GetLeadingTrivia().ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.MultiLineCommentTrivia, trivia[0].Kind());
            errors = trivia[0].Errors();
            Assert.Equal(1, errors.Length);
        }

        [Fact]
        [Trait("Feature", "Comments")]
        public void TestCommentWithTextWindowSentinel()
        {
            Assert.Equal('\uFFFF', SlidingTextWindow.InvalidCharacter);
            var text = "// com\uFFFFment";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.EndOfFileToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            Assert.Equal(text.Length, token.FullWidth);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            var trivia = token.LeadingTrivia.ToArray();
            Assert.Equal(1, trivia.Length);
            Assert.NotNull(trivia[0]);
            Assert.Equal(SyntaxKind.SingleLineCommentTrivia, trivia[0].Kind());
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestSingleLetterIdentifier()
        {
            var text = "a";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestMultiLetterIdentifier()
        {
            var text = "abc";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestMixedAlphaNumericIdentifier()
        {
            var text = "a0b1c2";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestIdentifierWithUnicode()
        {
            var text = "Fō̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄̄o";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestIdentifierWithSpaceLookingCharacters()
        {
            var text = "my͏very͏long͏identifier"; // These are COMBINING GRAPHEME JOINERs, not actual spaces
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestVerbatimSingleLetterIdentifier()
        {
            var text = "@x";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("x", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestVerbatimKeywordIdentifier()
        {
            var text = "@if";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("if", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestUnicodeEscapeIdentifier()
        {
            var text = "\\u1234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(1, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestVerbatimUnicodeEscapeIdentifier()
        {
            var text = "@\\u1234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(1, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestLongUnicodeEscapeIdentifier()
        {
            var text = "\\U00001234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(1, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestVerbatimLongUnicodeEscapeIdentifier()
        {
            var text = "@\\U00001234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(1, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestMixedUnicodeEscapeIdentifier()
        {
            var text = "a\\u1234b";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(3, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestMultiUnicodeEscapeIdentifier()
        {
            var text = "a\\u1234\\u5678b";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(4, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestBadUnicodeEscapeIdentifier()
        {
            var text = "\\u123";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.ToFullString());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(1, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestAllVerbatimKeywordsAsIdentifiers()
        {
            foreach (var keyword in SyntaxFacts.GetKeywordKinds())
            {
                if (SyntaxFacts.IsReservedKeyword(keyword))
                {
                    var value = SyntaxFacts.GetText(keyword);
                    var text = "@" + value;
                    var token = LexToken(text);

                    Assert.NotNull(token);
                    Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
                    Assert.Equal(text, token.Text);
                    var errors = token.Errors();
                    Assert.Equal(0, errors.Length);
                    Assert.Equal(value, token.ValueText);
                }
            }
        }

        [Fact]
        [Trait("Feature", "Identifiers")]
        public void TestNonLatinIdentifier()
        {
            var text = "\u00C6sh";
            var token = LexToken(text);

            Assert.NotEqual('\\', text[0]);
            Assert.Equal(System.Globalization.UnicodeCategory.UppercaseLetter, Char.GetUnicodeCategory(text[0]));

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.NotNull(token.ValueText);
            Assert.IsType(typeof(string), token.ValueText);
            Assert.Equal(3, ((string)token.ValueText).Length);
        }

        [Fact]
        [Trait("Feature", "Keywords")]
        public void TestAllLanguageKeywords()
        {
            foreach (var keyword in SyntaxFacts.GetKeywordKinds())
            {
                if (SyntaxFacts.IsReservedKeyword(keyword))
                {
                    var text = SyntaxFacts.GetText(keyword);
                    var token = LexToken(text);

                    Assert.NotNull(token);
                    Assert.True(SyntaxFacts.IsReservedKeyword(token.Kind()));
                    Assert.Equal(text, token.Text);
                    var errors = token.Errors();
                    Assert.Equal(0, errors.Length);
                    Assert.Equal(text, token.ValueText);
                }
            }
        }

        [Fact]
        [Trait("Feature", "Punctuation")]
        public void TestAllLanguagePunctuation()
        {
            TestPunctuation(SyntaxKind.TildeToken);
            TestPunctuation(SyntaxKind.ExclamationToken);

            // TestPunctuation(ParseKind.Dollar);    debugger only
            TestPunctuation(SyntaxKind.PercentToken);
            TestPunctuation(SyntaxKind.CaretToken);
            TestPunctuation(SyntaxKind.AmpersandToken);
            TestPunctuation(SyntaxKind.AsteriskToken);
            TestPunctuation(SyntaxKind.OpenParenToken);
            TestPunctuation(SyntaxKind.CloseParenToken);
            TestPunctuation(SyntaxKind.MinusToken);
            TestPunctuation(SyntaxKind.PlusToken);
            TestPunctuation(SyntaxKind.EqualsToken);
            TestPunctuation(SyntaxKind.OpenBraceToken);
            TestPunctuation(SyntaxKind.CloseBraceToken);
            TestPunctuation(SyntaxKind.OpenBracketToken);
            TestPunctuation(SyntaxKind.CloseBracketToken);
            TestPunctuation(SyntaxKind.BarToken);

            // TestPunctuation(ParseKind.BackSlash);   escape
            TestPunctuation(SyntaxKind.ColonToken);
            TestPunctuation(SyntaxKind.SemicolonToken);

            // TestPunctuation(ParseKind.DoubleQuote);   string literal
            // TestPunctuation(ParseKind.Quote);         character literal
            TestPunctuation(SyntaxKind.LessThanToken);
            TestPunctuation(SyntaxKind.CommaToken);
            TestPunctuation(SyntaxKind.GreaterThanToken);
            TestPunctuation(SyntaxKind.DotToken);
            TestPunctuation(SyntaxKind.QuestionToken);

            // TestPunctuation(ParseKind.Hash);  preprocessor only
            TestPunctuation(SyntaxKind.SlashToken);
            TestPunctuation(SyntaxKind.BarBarToken);
            TestPunctuation(SyntaxKind.AmpersandAmpersandToken);
            TestPunctuation(SyntaxKind.MinusMinusToken);
            TestPunctuation(SyntaxKind.PlusPlusToken);
            TestPunctuation(SyntaxKind.ColonColonToken);
            TestPunctuation(SyntaxKind.QuestionQuestionToken);
            TestPunctuation(SyntaxKind.MinusGreaterThanToken);
            TestPunctuation(SyntaxKind.ExclamationEqualsToken);
            TestPunctuation(SyntaxKind.EqualsEqualsToken);
            TestPunctuation(SyntaxKind.EqualsGreaterThanToken);
            TestPunctuation(SyntaxKind.LessThanEqualsToken);
            TestPunctuation(SyntaxKind.LessThanLessThanToken);
            TestPunctuation(SyntaxKind.LessThanLessThanEqualsToken);
            TestPunctuation(SyntaxKind.GreaterThanEqualsToken);

            // TestPunctuation(ParseKind.GreaterThanGreaterThan);  not directly lexed  (generics)
            // TestPunctuation(ParseKind.GreaterThanGreaterThanEquals);  not directly lexed (generics)
            TestPunctuation(SyntaxKind.SlashEqualsToken);
            TestPunctuation(SyntaxKind.AsteriskEqualsToken);
            TestPunctuation(SyntaxKind.BarEqualsToken);
            TestPunctuation(SyntaxKind.AmpersandEqualsToken);
            TestPunctuation(SyntaxKind.PlusEqualsToken);
            TestPunctuation(SyntaxKind.MinusEqualsToken);
            TestPunctuation(SyntaxKind.CaretEqualsToken);
            TestPunctuation(SyntaxKind.PercentEqualsToken);
        }

        private void TestPunctuation(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            var token = LexToken(text);
            Assert.NotNull(token);
            Assert.Equal(kind, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteral()
        {
            var text = "\"literal\"";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("literal", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithUnicode()
        {
            var text = "\"҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉ ҉҉̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌… ̒̓̔̕̚ ̍̎̏̐̑̒̓̔̕̚̕̚ ̡̢̛̗̘̙̜̝̞̟̠̊̋̌̍̎̏̚ ̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎… ̕̚̕̚ ̔̕̚̕̚҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉\"";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉ ҉҉̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌… ̒̓̔̕̚ ̍̎̏̐̑̒̓̔̕̚̕̚ ̡̢̛̗̘̙̜̝̞̟̠̊̋̌̍̎̏̚ ̡̢̡̢̛̛̖̗̘̙̜̝̞̟̠̖̗̘̙̜̝̞̟̠̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎… ̕̚̕̚ ̔̕̚̕̚҉ ҉̵̞̟̠̖̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̐̑̒̓̔̊̋̌̍̎̏̐̑̒̓̔̿̿̿… ͡҉҉ ̵̡̢̛̗̘̙̜̝̞̟̠͇̊̋̌̍̎̏̿̿̿̚ ҉", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestVerbatimStringLiteral()
        {
            var text = "@\"literal\"";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("literal", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestMultiLineVerbatimStringLiteral()
        {
            var text = "@\"multi line\r\nliteral\"";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("multi line\r\nliteral", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithNewLine()
        {
            var text = "\"literal\r\nwith new line\"";
            var token = Lex(text).First();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.NotEqual(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NewlineInConst, errors[0].Code);
            Assert.Equal("literal", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestUnterminatedStringLiteral()
        {
            var text = "\"literal";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NewlineInConst, errors[0].Code);
            Assert.Equal("literal", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestUnterminatedVerbatimStringLiteral()
        {
            var text = "@\"literal";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_UnterminatedStringLit, errors[0].Code);
            Assert.Equal("literal", token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithUnicodeEscape()
        {
            var text = "\"\\u1234\"";
            var value = "\u1234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithLongUnicodeEscape()
        {
            var text = "\"\\U00001234\"";
            var value = "\U00001234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithMultipleUnicodeEscapes()
        {
            var text = "\"\\u1234\\u1234\"";
            var value = "\u1234\u1234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestStringLiteralWithBadUnicodeEscape()
        {
            var value = "\u0012";
            var text = "\"\\u12\"";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.StringLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_IllegalEscape, errors[0].Code);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteral()
        {
            var value = "x";
            var text = "'" + value + "'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_R()
        {
            var value = "\r";
            var text = "'\\r'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_N()
        {
            var value = "\n";
            var text = "'\\n'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_0()
        {
            var value = "\0";
            var text = "'\\0'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_T()
        {
            var value = "\t";
            var text = "'\\t'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_A()
        {
            var value = "\a";
            var text = "'\\a'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_B()
        {
            var value = "\b";
            var text = "'\\b'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_V()
        {
            var value = "\v";
            var text = "'\\v'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_F()
        {
            var value = "\f";
            var text = "'\\f'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralBadEscape()
        {
            var value = "q";
            var text = "'\\q'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_BackSlash()
        {
            var value = "\\";
            var text = "'\\\\'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_SingleQuote()
        {
            var value = "'";
            var text = "'\\''";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralEscape_DoubleQuote()
        {
            var value = "\"";
            var text = "'\\\"'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralUnicodeEscape()
        {
            var value = "\u1234";
            var text = "'\\u1234'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralLongUnicodeEscape()
        {
            var value = "\U00001234";
            var text = "'\\U00001234'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralWithBadUnicodeEscape()
        {
            var value = "\u0012";
            var text = "'\\u12'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_IllegalEscape, errors[0].Code);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralThatsTooSmall()
        {
            var text = "''";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_EmptyCharConst, errors[0].Code);
            Assert.Equal(SlidingTextWindow.InvalidCharacter, Char.Parse(token.ValueText));
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralThatsTooBig()
        {
            var value = "a";
            var text = "'ab'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal(text, token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_TooManyCharsInConst, errors[0].Code);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralWithNewline()
        {
            var value = "a";
            var text = "'a\r'";
            var token = Lex(text).First();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal("'a", token.Text);
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NewlineInConst, errors[0].Code);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralThatsTooSmallWithNewline()
        {
            var text = "'\r'";
            var token = Lex(text).First();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal("'", token.Text);
            var errors = token.Errors();
            Assert.Equal(2, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NewlineInConst, errors[0].Code);
            Assert.Equal((int)ErrorCode.ERR_EmptyCharConst, errors[1].Code);
            Assert.Equal(SlidingTextWindow.InvalidCharacter, Char.Parse(token.ValueText));
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralThatsTooBigWithNewline()
        {
            var value = "a";
            var text = "'ab\r'";
            var token = Lex(text).First();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal("'ab", token.Text);
            var errors = token.Errors();
            Assert.Equal(2, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NewlineInConst, errors[0].Code);
            Assert.Equal((int)ErrorCode.ERR_TooManyCharsInConst, errors[1].Code);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestCharacterLiteralContainingTextWindowSentinel()
        {
            Assert.Equal('\uFFFF', SlidingTextWindow.InvalidCharacter);

            var value = "\uFFFF";
            var text = "'\uFFFF'";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, token.Kind());
            Assert.Equal("'\uFFFF'", token.Text);
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(value, token.ValueText);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestUnicodeEscapeWithNoDigits()
        {
            var text = "\\";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestVerbatimIdentifierWithNoCharacters()
        {
            var text = "@";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_ExpectedVerbatimLiteral, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestVerbatimIdentifierWithNoCharactersAndTrivia()
        {
            var text = "@  ";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_ExpectedVerbatimLiteral, errors[0].Code);
            Assert.Equal(text, token.ToFullString());
            var trivia = token.GetTrailingTrivia().ToList();
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteral()
        {
            var value = 123;
            var text = "123";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimal()
        {
            var value = 123.456;
            var text = "123.456";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithHugeDecimal()
        {
            var value = 123.45632434234234234234234234324234234234;
            var text = "123.45632434234234234234234234324234234234";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithHugeNumberAndDecimal()
        {
            var value = 12332434234234234234234234324234234234.456;
            var text = "12332434234234234234234234324234234234.456";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        public void TestNumericLiteralWithHugeNumberAndHugeDecimal()
        {
            var value = 12332434234234234234234234324234234234.45623423423423423423423423423423423423;
            var text = "12332434234234234234234234324234234234.45623423423423423423423423423423423423";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndExponent()
        {
            var text = "123.456e10";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndHugeExponent()
        {
            var text = "123.456e999";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndHugeExponentAndFloatSpecifier()
        {
            var text = "123.456e999f";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndHugeExponentAndDoubleSpecifier()
        {
            var text = "123.456e999d";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndHugeExponentAndDecimalSpecifier()
        {
            var text = "123.456e999m";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithHugeDecimalAndDecimalSpecifier()
        {
            var text = "10234230492340923423423423423423423434234.23402349234098230498234m";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithExponentAndDecimalSpecifier01()
        {
            var text = "0.0E1000M";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            decimal d;
            if (decimal.TryParse("0E1", System.Globalization.NumberStyles.AllowExponent, null, out d))
            {
                Assert.Equal(0, errors.Length);
            }
            else
            {
                Assert.Equal(1, errors.Length);
                Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
                Assert.Equal(text, token.Text);
            }
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithExponentAndDecimalSpecifier02()
        {
            var text = "1.0E29M";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithExponentAndDecimalSpecifier03()
        {
            var text = "0.1E29M";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
        }

        [WorkItem(547238, "DevDiv")]
        [Fact, WorkItem(547238, "DevDiv")]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithExponentAndDecimalSpecifier04()
        {
            var text = "0.1EM";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidReal, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndUpperExponent()
        {
            var text = "123.456E10";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralStartsWithDecimal()
        {
            var value = .456;
            var text = ".456";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralStartsWithDecimalAndExponent()
        {
            var value = .456e10;
            var text = ".456e10";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithFloatSpecifier()
        {
            var value = 123f;
            var text = "123f";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperFloatSpecifier()
        {
            var value = 123F;
            var text = "123F";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndFloatSpecifier()
        {
            var value = 123.456f;
            var text = "123.456f";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndExponentAndFloatSpecifier()
        {
            var value = 123.456e10f;
            var text = "123.456e10f";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDoubleSpecifier()
        {
            var value = 123d;
            var text = "123d";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperDoubleSpecifier()
        {
            var value = 123D;
            var text = "123D";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndDoubleSpecifier()
        {
            var value = 123.456d;
            var text = "123.456d";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalAndExponentAndDoubleSpecifier()
        {
            var value = 123.456e10d;
            var text = "123.456e10D";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalSpecifier()
        {
            var value = 123m;
            var text = "123m";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalPointAndDecimalSpecifier()
        {
            var value = 123.456m;
            var text = "123.456m";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithDecimalPointAndExponentAndDecimalSpecifier()
        {
            var value = 123.456e2m;
            var text = "123.456e2m";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUnsignedSpecifier()
        {
            var value = 123u;
            var text = "123u";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperUnsignedSpecifier()
        {
            var value = 123U;
            var text = "123U";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithLongSpecifier()
        {
            var value = 123L;
            var text = "123l";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.ErrorsAndWarnings();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.WRN_LowercaseEllSuffix, errors[0].Code);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperLongSpecifier()
        {
            var value = 123L;
            var text = "123L";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUnsignedAndLongSpecifier()
        {
            var value = 123ul;
            var text = "123ul";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "123lu";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperUnsignedAndLongSpecifier()
        {
            var value = 123Ul;
            var text = "123Ul";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "123lU";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUnsignedAndUpperLongSpecifier()
        {
            var value = 123uL;
            var text = "123uL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "123Lu";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralWithUpperUnsignedAndUpperLongSpecifier()
        {
            var value = 123UL;
            var text = "123UL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "123LU";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithUnsignedAndLongSpecifier()
        {
            var value = 0x123ul;
            var text = "0x123ul";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0x123lu";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithUpperUnsignedAndLongSpecifier()
        {
            var value = 0x123Ul;
            var text = "0x123Ul";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0x123lU";
            token = LexToken(text);
            errors = token.Errors();
            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithUnsignedAndUpperLongSpecifier()
        {
            var value = 0x123uL;
            var text = "0x123uL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0x123Lu";
            token = LexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithUpperUnsignedAndUpperLongSpecifier()
        {
            var value = 0x123UL;
            var text = "0x123UL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0x123LU";
            token = LexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithoutFeatureFlag()
        {
            var text = "0b1";
            var token = LexToken(text);

            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FeatureIsExperimental, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithUnsignedAndLongSpecifier()
        {
            var value = 0x123ul;
            var text = "0b100100011ul";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0b100100011lu";
            token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithUpperUnsignedAndLongSpecifier()
        {
            var value = 0x123Ul;
            var text = "0b100100011Ul";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0b100100011lU";
            token = LexToken(text, _binaryOptions);
            errors = token.Errors();
            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithUnsignedAndUpperLongSpecifier()
        {
            var value = 0x123uL;
            var text = "0b100100011uL";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0b100100011Lu";
            token = LexToken(text, _binaryOptions);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithUpperUnsignedAndUpperLongSpecifier()
        {
            var value = 0x123UL;
            var text = "0b100100011UL";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);

            text = "0b100100011LU";
            token = LexToken(text, _binaryOptions);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralMaxInt32()
        {
            var text = Int32.MaxValue.ToString();
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(Int32.MaxValue, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralMaxUInt32()
        {
            var text = UInt32.MaxValue.ToString() + "U";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(UInt32.MaxValue, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForInt32BecomesUInt32()
        {
            var value = ((uint)Int32.MaxValue) + 1;
            var text = value.ToString();
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForUInt32BecomesInt64()
        {
            var value = ((long)UInt32.MaxValue) + 1;
            var text = value.ToString();
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForInt64BecomesUInt64()
        {
            var value = ((ulong)Int64.MaxValue) + 1;
            var text = value.ToString();
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForUInt32OrUInt64()
        {
            var text = UInt64.MaxValue.ToString() + "0U";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_IntOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralMaxInt64()
        {
            var text = Int64.MaxValue.ToString() + "L";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(Int64.MaxValue, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForInt64()
        {
            var text = (((ulong)Int64.MaxValue) + 1).ToString() + "L";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal((ulong)Int64.MaxValue + 1, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralMaxUInt64()
        {
            var text = UInt64.MaxValue.ToString() + "UL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(UInt64.MaxValue, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericLiteralTooBigForUInt64()
        {
            var text = UInt64.MaxValue.ToString() + "0UL";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_IntOverflow, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteral()
        {
            var value = 0x123;
            var text = "0x123";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithUnsignedSpecifier()
        {
            var value = 0x123u;
            var text = "0x123u";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericHexLiteralWithLongSpecifier()
        {
            var value = 0x123L;
            var text = "0x123L";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitHexLiteral()
        {
            var value = 0x12345678;
            var text = "0x12345678";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxHexLiteral()
        {
            var value = 0x7FFFFFFF;
            var text = "0x7FFFFFFF";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxUnsignedHexLiteral()
        {
            var value = 0xFFFFFFFF;
            var text = "0xFFFFFFFF";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxUnsignedHexLiteralWithUnsignedSpecifier()
        {
            var value = 0xFFFFFFFFu;
            var text = "0xFFFFFFFFu";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric16DigitMaxHexLiteral()
        {
            var value = 0x7FFFFFFFFFFFFFFF;
            var text = "0x7FFFFFFFFFFFFFFF";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric16DigitMaxUnsignedHexLiteral()
        {
            var value = 0xFFFFFFFFFFFFFFFF;
            var text = "0xFFFFFFFFFFFFFFFF";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteral()
        {
            var value = 0x123;
            var text = "0b100100011";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithUnsignedSpecifier()
        {
            var value = 0x123u;
            var text = "0b100100011u";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericBinaryLiteralWithLongSpecifier()
        {
            var value = 0x123L;
            var text = "0b100100011L";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitBinaryLiteral()
        {
            var value = 0xAA;
            var text = "0b10101010";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxBinaryLiteral()
        {
            var value = 0x7FFFFFFF;
            var text = "0b1111111111111111111111111111111";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxUnsignedBinaryLiteral()
        {
            var value = 0xFFFFFFFF;
            var text = "0b11111111111111111111111111111111";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric8DigitMaxUnsignedBinaryLiteralWithUnsignedSpecifier()
        {
            var value = 0xFFFFFFFFu;
            var text = "0b11111111111111111111111111111111u";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric16DigitMaxBinaryLiteral()
        {
            var value = 0x7FFFFFFFFFFFFFFF;
            var text = "0b111111111111111111111111111111111111111111111111111111111111111";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumeric16DigitMaxUnsignedBinaryLiteral()
        {
            var value = 0xFFFFFFFFFFFFFFFF;
            var text = "0b1111111111111111111111111111111111111111111111111111111111111111";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericOverflowBinaryLiteral()
        {
            var text = "0b10000000000000000000000000000000000000000000000000000000000000000";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_IntOverflow, errors[0].Code);
            Assert.Equal("error CS1021: Integral constant is too large", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericEmptyBinaryLiteral()
        {
            var text = "0b";
            var token = LexToken(text, _binaryOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericWithUnderscores()
        {
            var text = "1_000";
            var token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(1000, token.Value);
            Assert.Equal(text, token.Text);

            text = "1___0_0___0";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(1000, token.Value);
            Assert.Equal(text, token.Text);

            text = "1_000.000_1";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(1000.0001, token.Value);
            Assert.Equal(text, token.Text);

            text = "1_01__0.0__10_1f";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(1010.0101f, token.Value);
            Assert.Equal(text, token.Text);

            text = "1_01__0.0__10_1e0_1";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(1010.0101e01, token.Value);
            Assert.Equal(text, token.Text);

            text = "0xA_A";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(0xAA, token.Value);
            Assert.Equal(text, token.Text);

            text = "0b1_1";
            token = LexToken(text, _binaryUnderscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(3, token.Value);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericWithUnderscoresWithoutFeatureFlag()
        {
            var text = "1_000";
            var token = LexToken(text);

            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_FeatureIsExperimental, errors[0].Code);
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericWithBadUnderscores()
        {
            var text = "_1000";
            var token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);

            text = "1000_";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "1000_.0";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            // parses as Int32.Member, where Member is _0
            // TODO: Check for that it does parse as a field access
            // text = "1000._0";

            text = "1000.0_";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "1000.0_e1";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "1000.0e_1";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "1000.0e1_";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "0x_A";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "0xA_";
            token = LexToken(text, _underscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "0b_1";
            token = LexToken(text, _binaryUnderscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);

            text = "0b1_";
            token = LexToken(text, _binaryUnderscoreOptions);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidNumber, errors[0].Code);
            Assert.Equal("error CS1013: Invalid number", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericWithTrailingDot()
        {
            var value = 3;
            var text = "3.";
            var token = Lex(text).First();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal("3", token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericFloatWithLeadingDecimalPoint()
        {
            var value = .3;
            var text = ".3";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericExponentWithoutDecimalPoint()
        {
            var value = 3e1;
            var text = "3e1";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        [Trait("Feature", "Literals")]
        public void TestNumericExponentWithNegative()
        {
            var value = 3e-1;
            var text = "3e-1";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(value, token.Value);
        }

        [Fact]
        public void TestDottedNameSequence()
        {
            var results = this.Blend("T.X.Y").ToList();
            Assert.Equal(6, results.Count);
        }

        [Fact]
        public void TestDebuggerDollarIdentifiers()
        {
            string text = "$";
            var token = DebuggerLexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(text, token.Value);

            text = "$x";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(text, token.Value);

            text = "x$";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text.Substring(0, text.Length - 1), token.Text);
            Assert.Equal(text.Substring(0, text.Length - 1), token.Value);
        }

        [Fact]
        public void TestDebuggerObjectAddressIdentifiers()
        {
            var token = Lex("@0x0").First();
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_ExpectedVerbatimLiteral);
            Assert.Equal("@", token.Text);
            Assert.Equal("@", token.Value);

            token = DebuggerLexToken("@0x0");
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            VerifyNoErrors(token);
            Assert.Equal("@0x0", token.Text);
            Assert.Equal("0x0", token.Value);

            token = DebuggerLexToken("@0X012345678");
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            VerifyNoErrors(token);
            Assert.Equal("@0X012345678", token.Text);
            Assert.Equal("0X012345678", token.Value);

            token = DebuggerLexToken("@0x9abcdefA");
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            VerifyNoErrors(token);
            Assert.Equal("@0x9abcdefA", token.Text);
            Assert.Equal("0x9abcdefA", token.Value);

            token = DebuggerLexToken("@0xBCDEF");
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            VerifyNoErrors(token);
            Assert.Equal("@0xBCDEF", token.Text);
            Assert.Equal("0xBCDEF", token.Value);

            token = DebuggerLexToken("@0x");
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_ExpectedVerbatimLiteral);
            Assert.Equal("@", token.Text);
            Assert.Equal("@", token.Value);

            token = Lex("@0b1c2d3e4f").First();
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_ExpectedVerbatimLiteral);
            Assert.Equal("@", token.Text);
            Assert.Equal("@", token.Value);

            token = DebuggerLexToken("@0b1c2d3e4f");
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_ExpectedVerbatimLiteral);
            Assert.Equal("@", token.Text);
            Assert.Equal("@", token.Value);

            token = DebuggerLexToken("@0x12u");
            Assert.Equal(SyntaxKind.BadToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_ExpectedVerbatimLiteral);
            Assert.Equal("@", token.Text);
            Assert.Equal("@", token.Value);

            token = DebuggerLexToken("@0xffff0000ffff0000ffff0000");
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            VerifyError(token, ErrorCode.ERR_IntOverflow);
            Assert.Equal("@0xffff0000ffff0000ffff0000", token.Text);
            Assert.Equal("0xffff0000ffff0000ffff0000", token.Value);
        }

        /// <summary>
        /// Earlier identifier syntax "[0-9]+#" not supported.
        /// </summary>
        [WorkItem(1071347)]
        [Fact]
        public void TestDebuggerAliasIdentifiers()
        {
            string text = "123#";
            var token = DebuggerLexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)ErrorCode.ERR_LegacyObjectIdSyntax, errors[0].Code);
            Assert.Equal("error CS2043: 'id#' syntax is no longer supported. Use '$id' instead.", errors[0].ToString(EnsureEnglishUICulture.PreferredOrNull));
            Assert.Equal(text, token.Text);
            Assert.Equal(text, token.Value);

            text = "0123#";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(1, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal(text, token.Value);

            text = "0x123#";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(1, errors.Length);
            Assert.Equal(text.Substring(0, text.Length - 1), token.Text);
            Assert.Equal(0x123, token.Value);

            text = "123L#";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(1, errors.Length);
            Assert.Equal(text.Substring(0, text.Length - 1), token.Text);
            Assert.Equal(123L, token.Value);

            // Current syntax.
            text = "$123";
            token = DebuggerLexToken(text);
            errors = token.Errors();

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal(0, errors.Length);
            Assert.Equal(text, token.Text);
            Assert.Equal("$123", token.Value);
        }

        [Fact]
        public void TestWhitespace()
        {
            string text = " \t\v\f\u00A0token\u00A0\f\v\t ";
            var token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal("token", token.Text);
            Assert.True(token.HasLeadingTrivia);
            var leading = token.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, leading[0].Kind());
            Assert.Equal(" \t\v\f\u00A0", leading[0].ToString());

            Assert.True(token.HasTrailingTrivia);
            var trailing = token.GetTrailingTrivia();
            Assert.Equal(1, trailing.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, trailing[0].Kind());
            Assert.Equal("\u00A0\f\v\t ", trailing[0].ToString());

            text = "\u001Atoken\u001A";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal("token", token.Text);
            Assert.True(token.HasLeadingTrivia);
            leading = token.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, leading[0].Kind());
            Assert.Equal("\u001A", leading[0].ToString());

            Assert.True(token.HasTrailingTrivia);
            trailing = token.GetTrailingTrivia();
            Assert.Equal(1, trailing.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, trailing[0].Kind());
            Assert.Equal("\u001A", trailing[0].ToString());

            // BOM is special. It is both whitespace and an identifier character.

            text = "\uFEFFto\uFEFFken\uFEFF \uFEFF";
            token = LexToken(text);

            Assert.NotNull(token);
            Assert.Equal(SyntaxKind.IdentifierToken, token.Kind());
            Assert.Equal("to\uFEFFken\uFEFF", token.Text);
            Assert.Equal("token", token.Value);
            Assert.True(token.HasLeadingTrivia);
            leading = token.GetLeadingTrivia();
            Assert.Equal(1, leading.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, leading[0].Kind());
            Assert.Equal("\uFEFF", leading[0].ToString());

            Assert.True(token.HasTrailingTrivia);
            trailing = token.GetTrailingTrivia();
            Assert.Equal(1, trailing.Count);
            Assert.Equal(SyntaxKind.WhitespaceTrivia, trailing[0].Kind());
            Assert.Equal(" \uFEFF", trailing[0].ToString());
        }

        [Fact]
        public void DecimalLiterals()
        {
            // Max value is 79228162514264337593543950335

            // Push boundary at various precisions.
            AssertGoodDecimalLiteral("7e28m", "70000000b30310a7e22ea49300000000");
            AssertBadDecimalLiteral("8e28m"); //too large

            AssertGoodDecimalLiteral("792E26m", "600000000ae7ac71ffe8b45b00000000");
            AssertBadDecimalLiteral("793E26m"); //too large

            AssertGoodDecimalLiteral("7922816251426433759354395033E1m", "fffffffaffffffffffffffff00000000");
            AssertBadDecimalLiteral("7922816251426433759354395034E1m"); //too large

            // Exact boundary with various scales
            AssertGoodDecimalLiteral("79228162514264337593543950335E0m", "ffffffffffffffffffffffff00000000");
            AssertBadDecimalLiteral("79228162514264337593543950346E0m"); //too large

            AssertGoodDecimalLiteral("7922816251426433759354395033.5E1m", "ffffffffffffffffffffffff00000000");
            AssertBadDecimalLiteral("7922816251426433759354395034.6E1m"); //too large

            AssertGoodDecimalLiteral("7.9228162514264337593543950335E28m", "ffffffffffffffffffffffff00000000");
            AssertBadDecimalLiteral("7.9228162514264337593543950346E28m"); //too large

            // Exponent has too many digits.
            AssertBadDecimalLiteral("1e9999M");
            AssertGoodDecimalLiteral("1e-9999M", "000000000000000000000000001c0000"); // Native compiler reports CS0594
            AssertBadDecimalLiteral("1.0e9999M");
            AssertGoodDecimalLiteral("1.0e-9999M", "000000000000000000000000001c0000"); // Native compiler reports CS0594

            // Exponent has way too many digits.
            AssertBadDecimalLiteral("1e99999999999999999999999999M");
            AssertGoodDecimalLiteral("1e-99999999999999999999999999M", "000000000000000000000000001c0000"); // Native compiler reports CS0594
            AssertBadDecimalLiteral("1.0e99999999999999999999999999M");
            AssertGoodDecimalLiteral("1.0e-99999999999999999999999999M", "000000000000000000000000001c0000"); // Native compiler reports CS0594

            // Zeroes with precision.
            AssertGoodDecimalLiteral("0e-27M", "000000000000000000000000001b0000");
            AssertGoodDecimalLiteral("0e-28M", "000000000000000000000000001c0000");
            AssertGoodDecimalLiteral("0e-29M", "000000000000000000000000001c0000"); //CONSIDER: dev10 has 00000000000000000000000000000000, which makes no sense

            // Silent underflow.
            AssertGoodDecimalLiteral("1e-27M", "000000010000000000000000001b0000");
            AssertGoodDecimalLiteral("1e-28M", "000000010000000000000000001c0000");
            AssertGoodDecimalLiteral("1e-29M", "000000000000000000000000001c0000"); //Becomes zero.
        }

        [Fact]
        public void DecimalLiteralsManyDigits()
        {
            AssertGoodDecimalLiteral("1.23456789012345678901234567890123456789012345678901234567890e28m", "6e39811546bec9b127e41b3200000000");
            AssertBadDecimalLiteral("1.23456789012345678901234567890123456789012345678901234567890e29m");
            AssertGoodDecimalLiteral("123456789012345678901234567890123456789012345678901234567890e-31m", "6e39811546bec9b127e41b3200000000");
            AssertBadDecimalLiteral("123456789012345678901234567890123456789012345678901234567890e-30m");
            AssertGoodDecimalLiteral("123456789012345678901234567890.123456789012345678901234567890e-1m", "6e39811546bec9b127e41b3200000000");
            AssertBadDecimalLiteral("123456789012345678901234567890.123456789012345678901234567890e-0m");
        }

        [Fact]
        public void MoreDecimalLiterals()
        {
            // Max value is 79228162514264337593543950335

            AssertGoodDecimalLiteral("792281625142643375935439503350E-1M", "ffffffffffffffffffffffff00000000");
            AssertGoodDecimalLiteral("7922816251426433759354395033500000E-5M", "ffffffffffffffffffffffff00000000");

            AssertGoodDecimalLiteral("792281625142643375935439503354E-1M", "ffffffffffffffffffffffff00000000");
            AssertGoodDecimalLiteral("7922816251426433759354395033549999E-5M", "ffffffffffffffffffffffff00000000");
        }

        [Fact]
        public void TestMaxKeywordLength()
        {
            int max = SyntaxFacts
                .GetKeywordKinds()
                .Concat(SyntaxFacts.GetContextualKeywordKinds())
                .Select(SyntaxFacts.GetText)
                .Max(x => x.Length);
            Assert.Equal(LexerCache.MaxKeywordLength, max);
        }

        [WorkItem(545781, "DevDiv")]
        [Fact]
        public void DecimalLiteralsOtherCulture()
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = (CultureInfo)oldCulture.Clone();
                Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~";
                Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ",";

                // If the exponent ("-1") is parsed using the current culture, then
                // parsing will raise a FormatException because the current culture
                // uses '~' as a negative sign.  Not seeing an exception ensures that
                // we are parsing with the invariant culture.
                AssertGoodDecimalLiteral("1.1E-1M", "0000000b000000000000000000020000");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

        private void AssertBadDecimalLiteral(string text)
        {
            var token = LexToken(text);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            var error = token.Errors().Single();
            Assert.Equal((int)ErrorCode.ERR_FloatOverflow, error.Code);
        }

        private void AssertGoodDecimalLiteral(string text, string expectedBits)
        {
            var token = LexToken(text);
            Assert.Equal(SyntaxKind.NumericLiteralToken, token.Kind());
            Assert.Equal(0, token.Errors().Length);
            Assert.Equal(expectedBits, ToHexString((decimal)token.Value));
        }

        private static void VerifyNoErrors(SyntaxToken token)
        {
            var errors = token.Errors();
            Assert.Equal(0, errors.Length);
        }

        private static void VerifyError(SyntaxToken token, ErrorCode expected)
        {
            var errors = token.Errors();
            Assert.Equal(1, errors.Length);
            Assert.Equal((int)expected, errors[0].Code);
        }

        private static string ToHexString(decimal d)
        {
            return string.Join("", decimal.GetBits(d).Select(word => string.Format("{0:x8}", word)));
        }
    }
}
