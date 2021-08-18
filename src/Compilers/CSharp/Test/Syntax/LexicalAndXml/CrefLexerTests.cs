﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CrefLexerTests : DocumentationCommentLexerTestBase
    {
        [Fact]
        public void TestLexIdentifiers()
        {
            AssertTokens("a", Token(SyntaxKind.IdentifierToken, "a"));
            AssertTokens("\u0061", Token(SyntaxKind.IdentifierToken, "\u0061", "a"));
            AssertTokens("&#x61;", Token(SyntaxKind.IdentifierToken, "&#x61;", "a"));

            AssertTokens("ab", Token(SyntaxKind.IdentifierToken, "ab"));
            AssertTokens("\u0061b", Token(SyntaxKind.IdentifierToken, "\u0061b", "ab"));
            AssertTokens("a\u0062", Token(SyntaxKind.IdentifierToken, "a\u0062", "ab"));
            AssertTokens("&#x61;b", Token(SyntaxKind.IdentifierToken, "&#x61;b", "ab"));
            AssertTokens("a&#x62;", Token(SyntaxKind.IdentifierToken, "a&#x62;", "ab"));
        }

        [Fact]
        public void TestLexKeywords()
        {
            AssertTokens("global", Token(SyntaxKind.IdentifierToken, contextualKind: SyntaxKind.GlobalKeyword));
            AssertTokens("operator", Token(SyntaxKind.OperatorKeyword));
            AssertTokens("explicit", Token(SyntaxKind.ExplicitKeyword));
            AssertTokens("implicit", Token(SyntaxKind.ImplicitKeyword));
            AssertTokens("ref", Token(SyntaxKind.RefKeyword));
            AssertTokens("out", Token(SyntaxKind.OutKeyword));
            AssertTokens("true", Token(SyntaxKind.TrueKeyword));
            AssertTokens("false", Token(SyntaxKind.FalseKeyword));

            AssertTokens("&#103;lobal", Token(SyntaxKind.IdentifierToken, "&#103;lobal", "global", SyntaxKind.GlobalKeyword));
            AssertTokens("&#111;perator", Token(SyntaxKind.OperatorKeyword, "&#111;perator", "operator"));
            AssertTokens("&#101;xplicit", Token(SyntaxKind.ExplicitKeyword, "&#101;xplicit", "explicit"));
            AssertTokens("&#105;mplicit", Token(SyntaxKind.ImplicitKeyword, "&#105;mplicit", "implicit"));
            AssertTokens("&#114;ef", Token(SyntaxKind.RefKeyword, "&#114;ef", "ref"));
            AssertTokens("&#111;ut", Token(SyntaxKind.OutKeyword, "&#111;ut", "out"));
            AssertTokens("&#116;rue", Token(SyntaxKind.TrueKeyword, "&#116;rue", "true"));
            AssertTokens("&#102;alse", Token(SyntaxKind.FalseKeyword, "&#102;alse", "false"));

            AssertTokens("&#103;loba&#108;", Token(SyntaxKind.IdentifierToken, "&#103;loba&#108;", "global", SyntaxKind.GlobalKeyword));
            AssertTokens("&#111;perato&#114;", Token(SyntaxKind.OperatorKeyword, "&#111;perato&#114;", "operator"));
            AssertTokens("&#101;xplici&#116;", Token(SyntaxKind.ExplicitKeyword, "&#101;xplici&#116;", "explicit"));
            AssertTokens("&#105;mplici&#116;", Token(SyntaxKind.ImplicitKeyword, "&#105;mplici&#116;", "implicit"));
            AssertTokens("&#114;e&#102;", Token(SyntaxKind.RefKeyword, "&#114;e&#102;", "ref"));
            AssertTokens("&#111;u&#116;", Token(SyntaxKind.OutKeyword, "&#111;u&#116;", "out"));
            AssertTokens("&#116;ru&#101;", Token(SyntaxKind.TrueKeyword, "&#116;ru&#101;", "true"));
            AssertTokens("&#102;als&#101;", Token(SyntaxKind.FalseKeyword, "&#102;als&#101;", "false"));
        }

        [Fact]
        public void TestLexVerbatimKeywords()
        {
            AssertTokens("@global", Token(SyntaxKind.IdentifierToken, "@global", "global"));
            AssertTokens("@operator", Token(SyntaxKind.IdentifierToken, "@operator", "operator"));
            AssertTokens("@explicit", Token(SyntaxKind.IdentifierToken, "@explicit", "explicit"));
            AssertTokens("@implicit", Token(SyntaxKind.IdentifierToken, "@implicit", "implicit"));
            AssertTokens("@ref", Token(SyntaxKind.IdentifierToken, "@ref", "ref"));
            AssertTokens("@out", Token(SyntaxKind.IdentifierToken, "@out", "out"));
            AssertTokens("@true", Token(SyntaxKind.IdentifierToken, "@true", "true"));
            AssertTokens("@false", Token(SyntaxKind.IdentifierToken, "@false", "false"));

            AssertTokens("&#64;global", Token(SyntaxKind.IdentifierToken, "&#64;global", "global"));
            AssertTokens("&#64;operator", Token(SyntaxKind.IdentifierToken, "&#64;operator", "operator"));
            AssertTokens("&#64;explicit", Token(SyntaxKind.IdentifierToken, "&#64;explicit", "explicit"));
            AssertTokens("&#64;implicit", Token(SyntaxKind.IdentifierToken, "&#64;implicit", "implicit"));
            AssertTokens("&#64;ref", Token(SyntaxKind.IdentifierToken, "&#64;ref", "ref"));
            AssertTokens("&#64;out", Token(SyntaxKind.IdentifierToken, "&#64;out", "out"));
            AssertTokens("&#64;true", Token(SyntaxKind.IdentifierToken, "&#64;true", "true"));
            AssertTokens("&#64;false", Token(SyntaxKind.IdentifierToken, "&#64;false", "false"));
        }

        [Fact]
        public void TestLexUnicodeEscapeKeywords()
        {
            AssertTokens("\\u0067lobal", Token(SyntaxKind.IdentifierToken, "\\u0067lobal", "global"));
            AssertTokens("\\u006Fperator", Token(SyntaxKind.IdentifierToken, "\\u006Fperator", "operator"));
            AssertTokens("\\u0065xplicit", Token(SyntaxKind.IdentifierToken, "\\u0065xplicit", "explicit"));
            AssertTokens("\\u0069mplicit", Token(SyntaxKind.IdentifierToken, "\\u0069mplicit", "implicit"));
            AssertTokens("\\u0072ef", Token(SyntaxKind.IdentifierToken, "\\u0072ef", "ref"));
            AssertTokens("\\u006Fut", Token(SyntaxKind.IdentifierToken, "\\u006Fut", "out"));
            AssertTokens("\\u0074rue", Token(SyntaxKind.IdentifierToken, "\\u0074rue", "true"));
            AssertTokens("\\u0066alse", Token(SyntaxKind.IdentifierToken, "\\u0066alse", "false"));
        }

        [WorkItem(530519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530519")]
        [Fact]
        public void TestLexUnicodeEscapeKeywordsWithEntities()
        {
            // BREAK: Dev11 treats these as verbatim identifiers.
            AssertTokens("&#92;u0067lobal", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0067lobal"));
            AssertTokens("&#92;u006Fperator", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u006Fperator"));
            AssertTokens("&#92;u0065xplicit", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0065xplicit"));
            AssertTokens("&#92;u0069mplicit", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0069mplicit"));
            AssertTokens("&#92;u0072ef", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0072ef"));
            AssertTokens("&#92;u006Fut", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u006Fut"));
            AssertTokens("&#92;u0074rue", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0074rue"));
            AssertTokens("&#92;u0066alse", Token(SyntaxKind.XmlEntityLiteralToken, "&#92;", "\\"), Token(SyntaxKind.IdentifierToken, "u0066alse"));
        }

        [Fact]
        public void TestLexPunctuation()
        {
            AssertTokens(".", Token(SyntaxKind.DotToken));
            AssertTokens(",", Token(SyntaxKind.CommaToken));
            AssertTokens(":", Token(SyntaxKind.ColonToken));
            AssertTokens("::", Token(SyntaxKind.ColonColonToken));
            AssertTokens("(", Token(SyntaxKind.OpenParenToken));
            AssertTokens(")", Token(SyntaxKind.CloseParenToken));
            AssertTokens("[", Token(SyntaxKind.OpenBracketToken));
            AssertTokens("]", Token(SyntaxKind.CloseBracketToken));
            AssertTokens("?", Token(SyntaxKind.QuestionToken));
            AssertTokens("??", Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken));
            AssertTokens("*", Token(SyntaxKind.AsteriskToken));
            AssertTokens("<", Token(SyntaxKind.BadToken, "<")); //illegal in attribute
            AssertTokens(">", Token(SyntaxKind.GreaterThanToken));

            // Special case: curly brackets become angle brackets.
            AssertTokens("{", Token(SyntaxKind.LessThanToken, "{", "<"));
            AssertTokens("}", Token(SyntaxKind.GreaterThanToken, "}", ">"));

            AssertTokens("&#46;", Token(SyntaxKind.DotToken, "&#46;", "."));
            AssertTokens("&#44;", Token(SyntaxKind.CommaToken, "&#44;", ","));
            //AssertTokens("&#58;", Token(SyntaxKind.ColonToken, "&#46;", ":")); //not needed
            AssertTokens("&#58;&#58;", Token(SyntaxKind.ColonColonToken, "&#58;&#58;", "::"));
            AssertTokens("&#58;:", Token(SyntaxKind.ColonColonToken, "&#58;:", "::"));
            AssertTokens(":&#58;", Token(SyntaxKind.ColonColonToken, ":&#58;", "::"));
            AssertTokens("&#40;", Token(SyntaxKind.OpenParenToken, "&#40;", "("));
            AssertTokens("&#41;", Token(SyntaxKind.CloseParenToken, "&#41;", ")"));
            AssertTokens("&#91;", Token(SyntaxKind.OpenBracketToken, "&#91;", "["));
            AssertTokens("&#93;", Token(SyntaxKind.CloseBracketToken, "&#93;", "]"));
            AssertTokens("&#63;", Token(SyntaxKind.QuestionToken, "&#63;", "?"));
            AssertTokens("&#63;&#63;", Token(SyntaxKind.QuestionToken, "&#63;", "?"), Token(SyntaxKind.QuestionToken, "&#63;", "?"));
            AssertTokens("&#42;", Token(SyntaxKind.AsteriskToken, "&#42;", "*"));
            AssertTokens("&#60;", Token(SyntaxKind.LessThanToken, "&#60;", "<"));
            AssertTokens("&#62;", Token(SyntaxKind.GreaterThanToken, "&#62;", ">"));

            // Special case: curly brackets become angle brackets.
            AssertTokens("&#123;", Token(SyntaxKind.LessThanToken, "&#123;", "<"));
            AssertTokens("&#125;", Token(SyntaxKind.GreaterThanToken, "&#125;", ">"));
        }

        [Fact]
        public void TestLexPunctuationSequences()
        {
            AssertTokens(":::", Token(SyntaxKind.ColonColonToken), Token(SyntaxKind.ColonToken));
            AssertTokens("::::", Token(SyntaxKind.ColonColonToken), Token(SyntaxKind.ColonColonToken));
            AssertTokens(":::::", Token(SyntaxKind.ColonColonToken), Token(SyntaxKind.ColonColonToken), Token(SyntaxKind.ColonToken));

            // No null-coalescing in crefs
            AssertTokens("???", Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken));
            AssertTokens("????", Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken), Token(SyntaxKind.QuestionToken));
        }

        [Fact]
        public void TestLexPunctuationSpecial()
        {
            AssertTokens("&amp;", Token(SyntaxKind.AmpersandToken, "&amp;", "&"));
            AssertTokens("&#38;", Token(SyntaxKind.AmpersandToken, "&#38;", "&"));
            AssertTokens("&#038;", Token(SyntaxKind.AmpersandToken, "&#038;", "&"));
            AssertTokens("&#0038;", Token(SyntaxKind.AmpersandToken, "&#0038;", "&"));
            AssertTokens("&#x26;", Token(SyntaxKind.AmpersandToken, "&#x26;", "&"));
            AssertTokens("&#x026;", Token(SyntaxKind.AmpersandToken, "&#x026;", "&"));
            AssertTokens("&#x0026;", Token(SyntaxKind.AmpersandToken, "&#x0026;", "&"));

            AssertTokens("&lt;", Token(SyntaxKind.LessThanToken, "&lt;", "<"));
            AssertTokens("&#60;", Token(SyntaxKind.LessThanToken, "&#60;", "<"));
            AssertTokens("&#060;", Token(SyntaxKind.LessThanToken, "&#060;", "<"));
            AssertTokens("&#0060;", Token(SyntaxKind.LessThanToken, "&#0060;", "<"));
            AssertTokens("&#x3C;", Token(SyntaxKind.LessThanToken, "&#x3C;", "<"));
            AssertTokens("&#x03C;", Token(SyntaxKind.LessThanToken, "&#x03C;", "<"));
            AssertTokens("&#x003C;", Token(SyntaxKind.LessThanToken, "&#x003C;", "<"));
            AssertTokens("{", Token(SyntaxKind.LessThanToken, "{", "<"));
            AssertTokens("&#123;", Token(SyntaxKind.LessThanToken, "&#123;", "<"));
            AssertTokens("&#0123;", Token(SyntaxKind.LessThanToken, "&#0123;", "<"));
            AssertTokens("&#x7B;", Token(SyntaxKind.LessThanToken, "&#x7B;", "<"));
            AssertTokens("&#x07B;", Token(SyntaxKind.LessThanToken, "&#x07B;", "<"));
            AssertTokens("&#x007B;", Token(SyntaxKind.LessThanToken, "&#x007B;", "<"));

            AssertTokens("&gt;", Token(SyntaxKind.GreaterThanToken, "&gt;", ">"));
            AssertTokens("&#62;", Token(SyntaxKind.GreaterThanToken, "&#62;", ">"));
            AssertTokens("&#062;", Token(SyntaxKind.GreaterThanToken, "&#062;", ">"));
            AssertTokens("&#0062;", Token(SyntaxKind.GreaterThanToken, "&#0062;", ">"));
            AssertTokens("&#x3E;", Token(SyntaxKind.GreaterThanToken, "&#x3E;", ">"));
            AssertTokens("&#x03E;", Token(SyntaxKind.GreaterThanToken, "&#x03E;", ">"));
            AssertTokens("&#x003E;", Token(SyntaxKind.GreaterThanToken, "&#x003E;", ">"));
            AssertTokens("}", Token(SyntaxKind.GreaterThanToken, "}", ">"));
            AssertTokens("&#125;", Token(SyntaxKind.GreaterThanToken, "&#125;", ">"));
            AssertTokens("&#0125;", Token(SyntaxKind.GreaterThanToken, "&#0125;", ">"));
            AssertTokens("&#x7D;", Token(SyntaxKind.GreaterThanToken, "&#x7D;", ">"));
            AssertTokens("&#x07D;", Token(SyntaxKind.GreaterThanToken, "&#x07D;", ">"));
            AssertTokens("&#x007D;", Token(SyntaxKind.GreaterThanToken, "&#x007D;", ">"));
        }

        [Fact]
        public void TestLexOperators()
        {
            // Single-character
            AssertTokens("&", Token(SyntaxKind.XmlEntityLiteralToken, "&")); // Not valid XML
            AssertTokens("~", Token(SyntaxKind.TildeToken));
            AssertTokens("*", Token(SyntaxKind.AsteriskToken));
            AssertTokens("/", Token(SyntaxKind.SlashToken));
            AssertTokens("%", Token(SyntaxKind.PercentToken));
            AssertTokens("|", Token(SyntaxKind.BarToken));
            AssertTokens("^", Token(SyntaxKind.CaretToken));

            // Multi-character
            AssertTokens("+", Token(SyntaxKind.PlusToken));
            AssertTokens("++", Token(SyntaxKind.PlusPlusToken));
            AssertTokens("-", Token(SyntaxKind.MinusToken));
            AssertTokens("--", Token(SyntaxKind.MinusMinusToken));
            AssertTokens("<", Token(SyntaxKind.BadToken, "<"));
            AssertTokens("<<", Token(SyntaxKind.BadToken, "<"), Token(SyntaxKind.BadToken, "<"));
            AssertTokens("<=", Token(SyntaxKind.BadToken, "<"), Token(SyntaxKind.EqualsToken));
            AssertTokens(">", Token(SyntaxKind.GreaterThanToken));
            AssertTokens(">>", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken));
            AssertTokens(">=", Token(SyntaxKind.GreaterThanEqualsToken));
            AssertTokens("=", Token(SyntaxKind.EqualsToken));
            AssertTokens("==", Token(SyntaxKind.EqualsEqualsToken));
            AssertTokens("!", Token(SyntaxKind.ExclamationToken));
            AssertTokens("!=", Token(SyntaxKind.ExclamationEqualsToken));

            // Single-character
            AssertTokens("&#38;", Token(SyntaxKind.AmpersandToken, "&#38;", "&")); // Fine
            AssertTokens("&#126;", Token(SyntaxKind.TildeToken, "&#126;", "~"));
            AssertTokens("&#42;", Token(SyntaxKind.AsteriskToken, "&#42;", "*"));
            AssertTokens("&#47;", Token(SyntaxKind.SlashToken, "&#47;", "/"));
            AssertTokens("&#37;", Token(SyntaxKind.PercentToken, "&#37;", "%"));
            AssertTokens("&#124;", Token(SyntaxKind.BarToken, "&#124;", "|"));
            AssertTokens("&#94;", Token(SyntaxKind.CaretToken, "&#94;", "^"));

            // Multi-character
            AssertTokens("&#43;", Token(SyntaxKind.PlusToken, "&#43;", "+"));
            AssertTokens("+&#43;", Token(SyntaxKind.PlusPlusToken, "+&#43;", "++"));
            AssertTokens("&#43;+", Token(SyntaxKind.PlusPlusToken, "&#43;+", "++"));
            AssertTokens("&#43;&#43;", Token(SyntaxKind.PlusPlusToken, "&#43;&#43;", "++"));
            AssertTokens("&#45;", Token(SyntaxKind.MinusToken, "&#45;", "-"));
            AssertTokens("-&#45;", Token(SyntaxKind.MinusMinusToken, "-&#45;", "--"));
            AssertTokens("&#45;-", Token(SyntaxKind.MinusMinusToken, "&#45;-", "--"));
            AssertTokens("&#45;&#45;", Token(SyntaxKind.MinusMinusToken, "&#45;&#45;", "--"));
            AssertTokens("&#60;", Token(SyntaxKind.LessThanToken, "&#60;", "<"));
            AssertTokens("&#60;&#60;", Token(SyntaxKind.LessThanLessThanToken, "&#60;&#60;", "<<"));
            AssertTokens("&#60;=", Token(SyntaxKind.LessThanEqualsToken, "&#60;=", "<="));
            AssertTokens("&#60;&#61;", Token(SyntaxKind.LessThanEqualsToken, "&#60;&#61;", "<="));
            AssertTokens("&#62;", Token(SyntaxKind.GreaterThanToken, "&#62;", ">"));
            AssertTokens(">&#62;", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken, "&#62;", ">"));
            AssertTokens("&#62;>", Token(SyntaxKind.GreaterThanToken, "&#62;", ">"), Token(SyntaxKind.GreaterThanToken));
            AssertTokens("&#62;&#62;", Token(SyntaxKind.GreaterThanToken, "&#62;", ">"), Token(SyntaxKind.GreaterThanToken, "&#62;", ">"));
            AssertTokens("&#62;=", Token(SyntaxKind.GreaterThanEqualsToken, "&#62;=", ">="));
            AssertTokens(">&#61;", Token(SyntaxKind.GreaterThanEqualsToken, ">&#61;", ">="));
            AssertTokens("&#62;&#61;", Token(SyntaxKind.GreaterThanEqualsToken, "&#62;&#61;", ">="));
            AssertTokens("&#61;", Token(SyntaxKind.EqualsToken, "&#61;", "="));
            AssertTokens("=&#61;", Token(SyntaxKind.EqualsEqualsToken, "=&#61;", "=="));
            AssertTokens("&#61;=", Token(SyntaxKind.EqualsEqualsToken, "&#61;=", "=="));
            AssertTokens("&#61;&#61;", Token(SyntaxKind.EqualsEqualsToken, "&#61;&#61;", "=="));
            AssertTokens("&#33;", Token(SyntaxKind.ExclamationToken, "&#33;", "!"));
            AssertTokens("!&#61;", Token(SyntaxKind.ExclamationEqualsToken, "!&#61;", "!="));
            AssertTokens("&#33;=", Token(SyntaxKind.ExclamationEqualsToken, "&#33;=", "!="));
            AssertTokens("&#33;&#61;", Token(SyntaxKind.ExclamationEqualsToken, "&#33;&#61;", "!="));
        }

        [Fact]
        public void TestLexOperatorSequence()
        {
            AssertTokens("+++", Token(SyntaxKind.PlusPlusToken), Token(SyntaxKind.PlusToken));
            AssertTokens("++++", Token(SyntaxKind.PlusPlusToken), Token(SyntaxKind.PlusPlusToken));
            AssertTokens("+++++", Token(SyntaxKind.PlusPlusToken), Token(SyntaxKind.PlusPlusToken), Token(SyntaxKind.PlusToken));

            AssertTokens("---", Token(SyntaxKind.MinusMinusToken), Token(SyntaxKind.MinusToken));
            AssertTokens("----", Token(SyntaxKind.MinusMinusToken), Token(SyntaxKind.MinusMinusToken));
            AssertTokens("-----", Token(SyntaxKind.MinusMinusToken), Token(SyntaxKind.MinusMinusToken), Token(SyntaxKind.MinusToken));

            AssertTokens("===", Token(SyntaxKind.EqualsEqualsToken), Token(SyntaxKind.EqualsToken));
            AssertTokens("====", Token(SyntaxKind.EqualsEqualsToken), Token(SyntaxKind.EqualsEqualsToken));
            AssertTokens("=====", Token(SyntaxKind.EqualsEqualsToken), Token(SyntaxKind.EqualsEqualsToken), Token(SyntaxKind.EqualsToken));

            AssertTokens("&lt;&lt;&lt;", Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"), Token(SyntaxKind.LessThanToken, "&lt;", "<"));
            AssertTokens("&lt;&lt;&lt;&lt;", Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"), Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"));
            AssertTokens("&lt;&lt;&lt;&lt;&lt;", Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"), Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"), Token(SyntaxKind.LessThanToken, "&lt;", "<"));

            AssertTokens(">>>", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken));
            AssertTokens(">>>>", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken));
            AssertTokens(">>>>>", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanToken));

            AssertTokens("!!=", Token(SyntaxKind.ExclamationToken), Token(SyntaxKind.ExclamationEqualsToken));
            AssertTokens("&lt;&lt;=", Token(SyntaxKind.LessThanLessThanToken, "&lt;&lt;", "<<"), Token(SyntaxKind.EqualsToken));
            AssertTokens(">>=", Token(SyntaxKind.GreaterThanToken), Token(SyntaxKind.GreaterThanEqualsToken)); //fixed up by parser

            AssertTokens("!==", Token(SyntaxKind.ExclamationEqualsToken), Token(SyntaxKind.EqualsToken));
            AssertTokens("&lt;==", Token(SyntaxKind.LessThanEqualsToken, "&lt;=", "<="), Token(SyntaxKind.EqualsToken));
            AssertTokens(">==", Token(SyntaxKind.GreaterThanEqualsToken), Token(SyntaxKind.EqualsToken));

            AssertTokens("{", Token(SyntaxKind.LessThanToken, "{", "<"));
            AssertTokens("{{", Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"));
            AssertTokens("{{{", Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"), Token(SyntaxKind.LessThanToken, "{", "<"));
            AssertTokens("{{{{", Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"), Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"));
            AssertTokens("{{{{{", Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"), Token(SyntaxKind.LessThanLessThanToken, "{{", "<<"), Token(SyntaxKind.LessThanToken, "{", "<"));
        }

        [Fact]
        public void TestLexBadEntity()
        {
            // Bad xml entities
            AssertTokens("&", Token(SyntaxKind.XmlEntityLiteralToken, "&"));
            AssertTokens("&&", Token(SyntaxKind.XmlEntityLiteralToken, "&"), Token(SyntaxKind.XmlEntityLiteralToken, "&"));
            AssertTokens("&;", Token(SyntaxKind.XmlEntityLiteralToken, "&;"));
            AssertTokens("&a;", Token(SyntaxKind.XmlEntityLiteralToken, "&a;"));
            AssertTokens("&#;", Token(SyntaxKind.XmlEntityLiteralToken, "&#;"));
            AssertTokens("&#x;", Token(SyntaxKind.XmlEntityLiteralToken, "&#x;"));
            AssertTokens("&#a;", Token(SyntaxKind.XmlEntityLiteralToken, "&#"), Token(SyntaxKind.IdentifierToken, "a"), Token(SyntaxKind.BadToken, ";"));
            AssertTokens("&#xg;", Token(SyntaxKind.XmlEntityLiteralToken, "&#x"), Token(SyntaxKind.IdentifierToken, "g"), Token(SyntaxKind.BadToken, ";"));

            // Overflowing entities
            AssertTokens("&#99999999999999999999;", Token(SyntaxKind.XmlEntityLiteralToken, "&#99999999999999999999;"));
            AssertTokens("&#x99999999999999999999;", Token(SyntaxKind.XmlEntityLiteralToken, "&#x99999999999999999999;"));

            // Long but not overflowing entities
            AssertTokens("&#00000000000000000097;", Token(SyntaxKind.IdentifierToken, "&#00000000000000000097;", "a"));
            AssertTokens("&#x00000000000000000061;", Token(SyntaxKind.IdentifierToken, "&#x00000000000000000061;", "a"));
        }

        [Fact]
        public void TestLexBadXml()
        {
            AssertTokens("<", Token(SyntaxKind.BadToken, "<"));
            AssertTokens(">", Token(SyntaxKind.GreaterThanToken));
        }

        [Fact]
        public void TestLexEmpty()
        {
            AssertTokens("");
        }

        [Fact]
        public void TestLexExamples()
        {
            AssertTokens("A.B.C",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "C"));

            AssertTokens("A::B.this[ref int]",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.ColonColonToken),
                Token(SyntaxKind.IdentifierToken, "B"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.ThisKeyword),
                Token(SyntaxKind.OpenBracketToken),
                Token(SyntaxKind.RefKeyword),
                Token(SyntaxKind.IntKeyword),
                Token(SyntaxKind.CloseBracketToken));

            AssertTokens("A.operator +",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.OperatorKeyword),
                Token(SyntaxKind.PlusToken));

            AssertTokens("A.explicit operator B::C(out C?*, int)",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.ExplicitKeyword),
                Token(SyntaxKind.OperatorKeyword),
                Token(SyntaxKind.IdentifierToken, "B"),
                Token(SyntaxKind.ColonColonToken),
                Token(SyntaxKind.IdentifierToken, "C"),
                Token(SyntaxKind.OpenParenToken),
                Token(SyntaxKind.OutKeyword),
                Token(SyntaxKind.IdentifierToken, "C"),
                Token(SyntaxKind.QuestionToken),
                Token(SyntaxKind.AsteriskToken),
                Token(SyntaxKind.CommaToken),
                Token(SyntaxKind.IntKeyword),
                Token(SyntaxKind.CloseParenToken));
        }

        [WorkItem(530523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530523")]
        [Fact(Skip = "530523")]
        public void TestLexNewline()
        {
            AssertTokens(@"A
.B",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"));

            AssertTokens(@"A&#10;.B",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"));
        }

        [WorkItem(530523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530523")]
        [Fact(Skip = "530523")]
        public void TestLexEntityInTrivia()
        {
            AssertTokens(@"A&#32;.B",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"));
        }

        [WorkItem(530523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530523")]
        [Fact(Skip = "530523")]
        public void TestLexCSharpTrivia()
        {
            AssertTokens(@"A //comment
.B",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"));

            AssertTokens(@"A /*comment*/.B",
                Token(SyntaxKind.IdentifierToken, "A"),
                Token(SyntaxKind.DotToken),
                Token(SyntaxKind.IdentifierToken, "B"));
        }

        internal override IEnumerable<InternalSyntax.SyntaxToken> GetTokens(string text)
        {
            Assert.DoesNotContain("'", text, StringComparison.Ordinal);
            using (var lexer = new InternalSyntax.Lexer(SourceText.From(text + "'"), TestOptions.RegularWithDocumentationComments))
            {
                while (true)
                {
                    var token = lexer.Lex(InternalSyntax.LexerMode.XmlCrefQuote | InternalSyntax.LexerMode.XmlDocCommentStyleSingleLine | InternalSyntax.LexerMode.XmlDocCommentLocationInterior);

                    if (token.Kind == SyntaxKind.SingleQuoteToken)
                    {
                        break;
                    }

                    yield return token;
                }
            }
        }
    }
}
