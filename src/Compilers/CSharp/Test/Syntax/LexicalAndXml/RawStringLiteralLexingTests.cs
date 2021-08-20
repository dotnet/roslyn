// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.LexicalAndXml
{
    public class RawStringLiteralLexingTests : CSharpTestBase
    {
        [Theory]
        #region Single Line Cases
        [InlineData("\"\"\"{|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" {|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"{|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"{|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " ")]
        [InlineData("\"\"\"\t\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "\t")]
        [InlineData("\"\"\"a\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\"abc\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "abc")]
        [InlineData("\"\"\" abc \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " abc ")]
        [InlineData("\"\"\"  abc  \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "  abc  ")]
        [InlineData("\"\"\" \" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \" ")]
        [InlineData("\"\"\" \"\" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \"\" ")]
        [InlineData("\"\"\"\" \"\"\" \"\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\" ")]
        [InlineData("\"\"\"'\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "'")]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"a{|CS9101:\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" a {|CS9101:\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"{|CS9101:\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"{|CS9101:\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"a{|CS9101:\r\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" a {|CS9101:\r\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"{|CS9101:\r\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \"\"{|CS9101:\r\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        #endregion
        #region Multi Line Cases
        [InlineData("\"\"\"\n{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n \"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n  \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n  {|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"")]
        [InlineData("\"\"\"  \na\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"\"")]
        [InlineData("\"\"\"  \n\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"a")]
        [InlineData("\"\"\"  \n\"\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"a")]
        [InlineData("\"\"\"  \na{|CS9104:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na{|CS9104:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\"  \n a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a")]
        [InlineData("\"\"\"  \na \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a ")]
        [InlineData("\"\"\"  \n a \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a ")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n{|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n \"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n  \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n  {|CS9106:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\na{|CS9104:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\na{|CS9104:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\na\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\"  \r\n a\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a")]
        [InlineData("\"\"\"  \r\na \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a ")]
        [InlineData("\"\"\"  \r\n a \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a ")]
        [InlineData("\"\"\"  \r\n\r\n a \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n a ")]
        [InlineData("\"\"\"  \r\n a \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a \r\n")]
        [InlineData("\"\"\"  \r\n\r\n a \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n a \r\n")]
        [InlineData("\"\"\"  \n\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"")]
        [InlineData("\"\"\"  \n\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"")]
        [InlineData("\"\"\"\"  \n\"\"\"\n\"\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"")]
        #endregion
        #region Multi Line Indentation Cases
        [InlineData("\"\"\"\r\n  abc\r\n     def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n   def")]
        [InlineData("\"\"\"\"\r\n  \"\"\"\r\n  \"\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"")]
        [InlineData("\"\"\"\r\n \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n  \"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"")]
        [InlineData("\"\"\"\r\n\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n abc\r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc")]
        [InlineData("\"\"\"\n\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n{|CS9103: |}abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n     abc\r\n     def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   abc\r\n   def")]
        [InlineData("\"\"\"\r\n    \" abc \"\r\n    \"\" def \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  \" abc \"\r\n  \"\" def \"\"")]
        [InlineData("\"\"\"\r\n   \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " ")]
        [InlineData("\"\"\"\r\n   \"  abc  \"\r\n   \"\"  def  \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " \"  abc  \"\r\n \"\"  def  \"\"")]
        [InlineData("\"\"\"\n{|CS9103:\t|}\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n abc \r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc ")]
        [InlineData("\"\"\"\r\n    abc  \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc  ")]
        [InlineData("\"\"\"\r\n  \"   abc   \"\r\n  \"\"   def   \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"   abc   \"\r\n\"\"   def   \"\"")]
        [InlineData("\"\"\"\r\n  abc \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc ")]
        [InlineData("\"\"\"\r\n abc\r\n{|CS9103:|}def\r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n  \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n{|CS9103: |}\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n  abc\r\n\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef")]
        [InlineData("\"\"\"\r\n  abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc")]
        [InlineData("\"\"\"\r\n  abc\r\n     \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n   \r\ndef")]
        [InlineData("\"\"\"\r\n    abc \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc ")]
        [InlineData("\"\"\"\r\n  abc\r\n   \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n \r\ndef")]
        [InlineData("\"\"\"\n\t\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n{|CS9103:|}abc\r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n abc \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " abc ")]
        [InlineData("\"\"\"\r\n  abc\r\n    \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n  \r\ndef")]
        [InlineData("\"\"\"\r\n    \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  ")]
        [InlineData("\"\"\"\n{|CS9103: |}abc\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n  abc\r\n  \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef")]
        [InlineData("\"\"\"\r\n abc\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " abc")]
        [InlineData("\"\"\"\n{|CS9103:\t|}abc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n{|CS9103: |}abc\n \t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n\t\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\t")]
        [InlineData("\"\"\"\r\n     abc\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   abc\r\ndef")]
        [InlineData("\"\"\"\n \tabc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\tabc")]
        [InlineData("\"\"\"\r\n    abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc")]
        [InlineData("\"\"\"\r\n  abc\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\ndef")]
        [InlineData("\"\"\"\r\n  \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"")]
        [InlineData("\"\"\"\r\n     \"abc\"\r\n     \"\"def\"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   \"abc\"\r\n   \"\"def\"\"")]
        [InlineData("\"\"\"\r\n{|CS9103: |}abc\r\n\r\n def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        #endregion
        public void TestSingleToken(string markup, SyntaxKind expectedKind, string expectedValue)
        {
            TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: false);
            TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: false);

            // If we don't have an unterminated raw string, then also try with some trailing trivia attached.
            if (!markup.Contains("CS" + (int)ErrorCode.ERR_Unterminated_raw_string_literal))
            {
                TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: true);
                TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: true);
            }
        }

        private static void TestSingleTokenWorker(
            string markup, SyntaxKind expectedKind, string expectedValue, bool leadingTrivia, bool trailingTrivia)
        {
            if (leadingTrivia)
                markup = " /*leading*/ " + markup;

            if (trailingTrivia)
                markup += " // trailing";

            MarkupTestFile.GetSpans(markup, out var input, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            Assert.True(spans.Count == 0 || spans.Count == 1);

            var token = SyntaxFactory.ParseToken(input);
            var literal = SyntaxFactory.LiteralExpression(SyntaxKind.MultiLineRawStringLiteralExpression, token);
            token = literal.Token;

            Assert.Equal(expectedKind, token.Kind());
            Assert.Equal(input.Length, token.FullWidth);
            Assert.Equal(input, token.ToFullString());
            Assert.NotNull(token.Value);
            Assert.IsType<string>(token.Value);
            Assert.NotNull(token.ValueText);
            Assert.Equal(expectedValue, token.ValueText);

            if (spans.Count == 0)
            {
                Assert.Empty(token.GetDiagnostics());
            }
            else
            {
                Assert.True(spans.Count == 1);

                // If we get any diagnostics, then the token's value text should always be empty.
                Assert.Equal("", token.ValueText);

                var diagnostics = token.GetDiagnostics();

                Assert.All(diagnostics, d => Assert.Equal(spans.Single().Key, d.Id));

                var expectedDiagnosticSpans = spans.Single().Value.OrderBy(d => d.Start);
                var actualDiagnosticsSpans = diagnostics.Select(d => d.Location.SourceSpan).OrderBy(d => d.Start);

                Assert.Equal(expectedDiagnosticSpans, actualDiagnosticsSpans);
            }
        }

        [Fact]
        public void TestDirectiveWithRawString()
        {
            CreateCompilation(
@"
#line 1 """"""c:\""""""").VerifyDiagnostics(
                // (2,9): error CS9100: Raw string literals are not allowed in preprocessor directives
                // #line 1 """c:\"""
                Diagnostic(ErrorCode.ERR_Raw_string_literals_are_not_allowed_in_preprocessor_directives, "").WithLocation(2, 9));
        }

        [Fact]
        public void AllSingleCharactersInSingleLineLiteral()
        {
            for (var charValue = '\0'; ; charValue++)
            {
                if (charValue == '"' || SyntaxFacts.IsNewLine(charValue))
                    continue;

                TestSingleToken("\"\"\"" + charValue + "\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, charValue.ToString());

                if (charValue == char.MaxValue)
                    break;
            }
        }

        [Fact]
        public void AllSingleCharactersInMultiLineLiteral()
        {
            for (var charValue = '\0'; ; charValue++)
            {
                TestSingleToken("\"\"\"\r\n" + charValue + "\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, charValue.ToString());

                if (charValue == char.MaxValue)
                    break;
            }
        }

        public static IEnumerable<object[]> EscapeSequences => new[]
        {
            new object[] { "\\'" },
            new object[] { "\\\"" },
            new object[] { "\\\\" },
            new object[] { "\\0" },
            new object[] { "\\a" },
            new object[] { "\\b" },
            new object[] { "\\f" },
            new object[] { "\\n" },
            new object[] { "\\r" },
            new object[] { "\\t" },
            new object[] { "\\v" },
            new object[] { "\\u1234" },
            new object[] { "\\U12345678" },
            new object[] { "\\x1234" },
        };

        [Theory, MemberData(nameof(EscapeSequences))]
        public void AllEscapeSequencesInSingleLineLiteral(string escapeSequence)
        {
            TestSingleToken("\"\"\" " + escapeSequence + " \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, $" {escapeSequence} ");
        }

        [Theory, MemberData(nameof(EscapeSequences))]
        public void AllEscapeSequencesInMultiLineLiteral(string escapeSequence)
        {
            TestSingleToken("\"\"\"\r\n" + escapeSequence + "\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, escapeSequence);
        }
    }
}
