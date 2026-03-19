// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.LexicalAndXml
{
    public class RawStringLiteralLexingTests : CompilingTestBase
    {
        [Theory]
        #region Single Line Cases
        [InlineData("\"\"\"{|CS8997:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" {|CS8997:|}", SyntaxKind.SingleLineRawStringLiteralToken, " ")]
        [InlineData("\"\"\" \"{|CS8997:|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"")]
        [InlineData("\"\"\" \"\"{|CS8997:|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"")]
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
        [InlineData("\"\"\" \"\"\"{|CS8998:\"|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\"\"")]
        [InlineData("\"\"\" \"\"\"{|CS8998:\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\"\"\"")]
        [InlineData("\"\"\" \"\"\"{|CS8998:\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\"\"\"\"")]
        [InlineData("\"\"\" \"\"\"{|CS8998:\"\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\"\"\"\"\"")]
        [InlineData("\"\"\"a{|CS8997:|}\n", SyntaxKind.SingleLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\" a {|CS8997:|}\n", SyntaxKind.SingleLineRawStringLiteralToken, " a ")]
        [InlineData("\"\"\" \"{|CS8997:|}\n", SyntaxKind.SingleLineRawStringLiteralToken, " \"")]
        [InlineData("\"\"\" \"\"{|CS8997:|}\n", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"")]
        [InlineData("\"\"\"a{|CS8997:|}\r\n", SyntaxKind.SingleLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\" a {|CS8997:|}\r\n", SyntaxKind.SingleLineRawStringLiteralToken, " a ")]
        [InlineData("\"\"\" \"{|CS8997:|}\r\n", SyntaxKind.SingleLineRawStringLiteralToken, " \"")]
        [InlineData("\"\"\" \"\"{|CS8997:|}\r\n", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"")]
        #endregion
        #region Multi Line Cases
        [InlineData("\"\"\"\n{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n")]
        [InlineData("\"\"\"\n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n\"")]
        [InlineData("\"\"\"\n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n\"\"")]
        [InlineData("\"\"\"\n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n\"\"\"")]
        [InlineData("\"\"\"\n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \n\"")]
        [InlineData("\"\"\" \n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \n\"\"")]
        [InlineData("\"\"\" \n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, " \n\"\"\"")]
        [InlineData("\"\"\" \n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \n\"")]
        [InlineData("\"\"\"  \n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \n\"\"")]
        [InlineData("\"\"\"  \n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \n\"\"\"")]
        [InlineData("\"\"\"  \n\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n \"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n \"")]
        [InlineData("\"\"\"\n \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\n \"\"")]
        [InlineData("\"\"\" \n \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \n \"\"")]
        [InlineData("\"\"\"  \n  \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \n  \"\"")]
        [InlineData("\"\"\"  \n  {|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \n  \"\"\"")]
        [InlineData("\"\"\"  \n\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \na\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"")]
        [InlineData("\"\"\"  \na\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"\"")]
        [InlineData("\"\"\"  \n\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"a")]
        [InlineData("\"\"\"  \n\"\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"a")]
        [InlineData("\"\"\"  \na{|CS9000:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \na\"\"\"")]
        [InlineData("\"\"\"  \na{|CS9000:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \na\"\"\"\"")]
        [InlineData("\"\"\"  \na\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a")]
        [InlineData("\"\"\"  \n a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a")]
        [InlineData("\"\"\"  \na \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a ")]
        [InlineData("\"\"\"  \n a \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a ")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS8998:\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \na\n\"\"\"\"")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS8998:\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \na\n\"\"\"\"\"")]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS8998:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \na\n\"\"\"\"\"\"")]
        [InlineData("\"\"\"\r\n{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n")]
        [InlineData("\"\"\"\r\n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n\"")]
        [InlineData("\"\"\"\r\n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n\"\"")]
        [InlineData("\"\"\"\r\n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n\"\"\"")]
        [InlineData("\"\"\"\r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \r\n\"")]
        [InlineData("\"\"\" \r\n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \r\n\"\"")]
        [InlineData("\"\"\" \r\n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, " \r\n\"\"\"")]
        [InlineData("\"\"\" \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\n\"")]
        [InlineData("\"\"\"  \r\n\"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\n\"\"")]
        [InlineData("\"\"\"  \r\n{|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\n\"\"\"")]
        [InlineData("\"\"\"  \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n \"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n \"")]
        [InlineData("\"\"\"\r\n \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n \"\"")]
        [InlineData("\"\"\" \r\n \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, " \r\n \"\"")]
        [InlineData("\"\"\"  \r\n  \"\"{|CS8997:|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\n  \"\"")]
        [InlineData("\"\"\"  \r\n  {|CS9002:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\n  \"\"\"")]
        [InlineData("\"\"\"  \r\n\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\na{|CS9000:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\na\"\"\"")]
        [InlineData("\"\"\"  \r\na{|CS9000:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "  \r\na\"\"\"\"")]
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
        [InlineData("\"\"\"\r\n{|CS8999: |}abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n abc\r\n  \"\"\"")]
        [InlineData("\"\"\"\r\n     abc\r\n     def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   abc\r\n   def")]
        [InlineData("\"\"\"\r\n    \" abc \"\r\n    \"\" def \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  \" abc \"\r\n  \"\" def \"\"")]
        [InlineData("\"\"\"\r\n   \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " ")]
        [InlineData("\"\"\"\r\n   \"  abc  \"\r\n   \"\"  def  \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " \"  abc  \"\r\n \"\"  def  \"\"")]
        [InlineData("\"\"\"\n{|CS9003:\t|}\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\n\t\n \"\"\"")]
        [InlineData("\"\"\"\r\n abc \r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc ")]
        [InlineData("\"\"\"\r\n    abc  \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc  ")]
        [InlineData("\"\"\"\r\n  \"   abc   \"\r\n  \"\"   def   \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"   abc   \"\r\n\"\"   def   \"\"")]
        [InlineData("\"\"\"\r\n  abc \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc ")]
        [InlineData("\"\"\"\r\n abc\r\n{|CS8999:|}def\r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n abc\r\ndef\r\n \"\"\"")]
        [InlineData("\"\"\"\r\n  \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n{|CS9003: |}\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\n \n\t\"\"\"")]
        [InlineData("\"\"\"\r\n  abc\r\n\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef")]
        [InlineData("\"\"\"\r\n  abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc")]
        [InlineData("\"\"\"\r\n  abc\r\n     \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n   \r\ndef")]
        [InlineData("\"\"\"\r\n    abc \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc ")]
        [InlineData("\"\"\"\r\n  abc\r\n   \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n \r\ndef")]
        [InlineData("\"\"\"\n\t\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n{|CS8999:|}abc\r\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\nabc\r\n \"\"\"")]
        [InlineData("\"\"\"\r\n abc \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " abc ")]
        [InlineData("\"\"\"\r\n  abc\r\n    \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n  \r\ndef")]
        [InlineData("\"\"\"\r\n    \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  ")]
        [InlineData("\"\"\"\n{|CS9003: |}abc\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\n abc\n\t\"\"\"")]
        [InlineData("\"\"\"\r\n  abc\r\n  \r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef")]
        [InlineData("\"\"\"\r\n abc\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " abc")]
        [InlineData("\"\"\"\n{|CS9003:\t|}abc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\n\tabc\n \"\"\"")]
        [InlineData("\"\"\"\n{|CS8999: |}abc\n \t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\n abc\n \t\"\"\"")]
        [InlineData("\"\"\"\n\t\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\t")]
        [InlineData("\"\"\"\r\n     abc\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   abc\r\ndef")]
        [InlineData("\"\"\"\n \tabc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\tabc")]
        [InlineData("\"\"\"\r\n    abc\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "  abc")]
        [InlineData("\"\"\"\r\n  abc\r\n  def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\ndef")]
        [InlineData("\"\"\"\r\n  \"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"")]
        [InlineData("\"\"\"\r\n     \"abc\"\r\n     \"\"def\"\"\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "   \"abc\"\r\n   \"\"def\"\"")]
        [InlineData("\"\"\"\r\n{|CS8999: |}abc\r\n\r\n def\r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n abc\r\n\r\n def\r\n  \"\"\"")]
        #endregion
        public void TestSingleToken(string markup, SyntaxKind expectedKind, string expectedValue)
            => TestSingleTokenWorker(markup, expectedKind, expectedValue, testOutput: true);

        private void TestSingleTokenWorker(string markup, SyntaxKind expectedKind, string expectedValue, bool testOutput)
        {
            TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: false, testOutput);
            TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: false, testOutput);

            // If we don't have an unterminated raw string, then also try with some trailing trivia attached.
            if (!markup.Contains("CS" + (int)ErrorCode.ERR_UnterminatedRawString))
            {
                TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: true, testOutput);
                TestSingleTokenWorker(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: true, testOutput);
            }
        }

        private void TestSingleTokenWorker(
            string markup, SyntaxKind expectedKind, string expectedValue, bool leadingTrivia, bool trailingTrivia, bool testOutput)
        {
            if (leadingTrivia)
                markup = " /*leading*/ " + markup;

            if (trailingTrivia)
                markup += " // trailing";

            MarkupTestFile.GetSpans(markup, out var input, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            Assert.True(spans.Count == 0 || spans.Count == 1);

            var literal = (LiteralExpressionSyntax)SyntaxFactory.ParseExpression(input);
            var token = literal.Token;

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

                if (testOutput)
                {

                    var programText = @$"
System.Console.WriteLine(
{input}
);
";

                    this.CompileAndVerify(programText, expectedOutput: expectedValue);
                }
            }
            else
            {
                Assert.Equal(1, spans.Count);

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
                // (2,9): error CS8996: Raw string literals are not allowed in preprocessor directives
                // #line 1 """c:\"""
                Diagnostic(ErrorCode.ERR_RawStringNotInDirectives, "").WithLocation(2, 9));
        }

        [Fact]
        public void AllSingleCharactersInSingleLineLiteral()
        {
            for (var charValue = '\0'; ; charValue++)
            {
                if (charValue == '"' || SyntaxFacts.IsNewLine(charValue))
                    continue;

                TestSingleTokenWorker(
                    "\"\"\"" + charValue + "\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, charValue.ToString(), testOutput: false);

                if (charValue == char.MaxValue)
                    break;
            }
        }

        [Fact]
        public void AllSingleCharactersInMultiLineLiteral()
        {
            for (var charValue = '\0'; ; charValue++)
            {
                TestSingleTokenWorker(
                    "\"\"\"\r\n" + charValue + "\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, charValue.ToString(), testOutput: false);

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
            new object[] { "\\e" },
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
