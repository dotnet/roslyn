// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        [InlineData("\"\"\" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\t\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "\t", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"a\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"abc\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "abc", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" abc \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " abc ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  abc  \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "  abc  ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \" ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \"\" \"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \"\" ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\" \"\"\" \"\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, " \"\"\" ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"'\"\"\"", SyntaxKind.SingleLineRawStringLiteralToken, "'", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \"\"\"{|CS9102:\"\"\"\"|}", SyntaxKind.SingleLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
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
        [InlineData("\"\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n \"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n  \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a\"\"", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n\"\"a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na{|CS9104:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na{|CS9104:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n a\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n a \n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \na\n\"\"\"{|CS9102:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\r\n{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\" \r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\r\n \"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"\r\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" \r\n \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n  \"\"{|CS9101:|}", SyntaxKind.MultiLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"  \r\n  \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\na{|CS9104:\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\na{|CS9104:\"\"\"\"|}", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\na\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n a\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\na \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "a ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n a \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n\r\n a \r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n a ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n a \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, " a \r\n", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \r\n\r\n a \r\n\r\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\r\n a \r\n", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"  \n\"\"\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\"  \n\"\"\"\n\"\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"", /*trailingTrivia*/ true)]
        #endregion
        #region Multi Line Indentation Cases
        [InlineData(
@"""""""
 abc
""""""", SyntaxKind.MultiLineRawStringLiteralToken, " abc", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
 abc
 """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
    abc
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "  abc", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
{|CS9103:|}abc
 """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
 abc
{|CS9103:|}def
 """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
     def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n   def", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc

  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
  
  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n\r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
   
  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n \r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
    
  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n  \r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  abc
     
  def
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "abc\r\n   \r\ndef", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
{|CS9103: |}abc
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  ""
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "\"", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  """"
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""""
  """"""
  """"""""", SyntaxKind.MultiLineRawStringLiteralToken, "\"\"\"", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""

  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
 
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
  
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
   
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, " ", /*trailingTrivia*/ true)]
        [InlineData(
@"""""""
    
  """"""", SyntaxKind.MultiLineRawStringLiteralToken, "  ", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n{|CS9103: |}abc\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n{|CS9103: |}abc\n \t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n{|CS9103:\t|}abc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n \tabc\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\tabc", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n\t\n\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "\t", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n\t\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n{|CS9103:\t|}\n \"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        [InlineData("\"\"\"\n{|CS9103: |}\n\t\"\"\"", SyntaxKind.MultiLineRawStringLiteralToken, "", /*trailingTrivia*/ true)]
        #endregion
        public void TestSingleToken(string markup, SyntaxKind expectedKind, string expectedValue, bool trailingTrivia = false)
        {
            TestSingleToken(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: false);
            TestSingleToken(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: false);

            if (trailingTrivia == true)
            {
                TestSingleToken(markup, expectedKind, expectedValue, leadingTrivia: false, trailingTrivia: true);
                TestSingleToken(markup, expectedKind, expectedValue, leadingTrivia: true, trailingTrivia: true);
            }
        }

        private void TestSingleToken(
            string markup, SyntaxKind expectedKind, string expectedValue,
            bool leadingTrivia, bool trailingTrivia)
        {
            if (leadingTrivia)
                markup = " /*leading*/ " + markup;

            if (trailingTrivia)
                markup += " // trailing";

            MarkupTestFile.GetSpans(markup, out var input, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            Assert.True(spans.Count == 0 || spans.Count == 1);
            if (spans.Count == 1)
                Assert.True(spans.Single().Value.Length == 1);

            var token = SyntaxFactory.ParseToken(input);
            var literal = SyntaxFactory.LiteralExpression(SyntaxKind.MultiLineRawStringLiteralExpression, token);
            token = literal.Token;

            Assert.Equal(expectedKind, token.Kind());
            Assert.Equal(input.Length, token.FullWidth);
            Assert.Equal(input, token.ToFullString());
            Assert.NotNull(token.Value);
            Assert.NotNull(token.ValueText);
            Assert.Equal(expectedValue, token.ValueText);

            if (spans.Count == 0)
            {
                Assert.Empty(token.GetDiagnostics());
            }
            else
            {
                // If we get any diagnostics, then the token's value text should always be empty.
                Assert.Equal("", token.ValueText);

                var diagnostics = token.GetDiagnostics();
                Assert.True(diagnostics.Count() == 1);

                var actualDiagnostic = diagnostics.Single();
                var expectedDiagnostic = spans.Single();

                Assert.Equal(expectedDiagnostic.Key, actualDiagnostic.Id);
                Assert.Equal(expectedDiagnostic.Value.Single(), actualDiagnostic.Location.SourceSpan);
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
    }
}
