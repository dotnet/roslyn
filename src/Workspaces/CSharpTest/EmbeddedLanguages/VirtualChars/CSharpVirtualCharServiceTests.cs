// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.VirtualChars
{
    public class CSharpVirtualCharServiceTests
    {
        private const string _statementPrefix = "var v = ";

        private SyntaxToken GetStringToken(string text, bool allowFailure)
        {
            var statement = _statementPrefix + text;
            var parsedStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(statement);
            var expression = parsedStatement.Declaration.Variables[0].Initializer.Value;

            if (expression is LiteralExpressionSyntax literal)
            {
                return literal.Token;
            }
            else if (expression is InterpolatedStringExpressionSyntax interpolation)
            {
                return ((InterpolatedStringTextSyntax)interpolation.Contents[0]).TextToken;
            }
            else if (allowFailure)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void Test(string stringText, string expected)
        {
            var token = GetStringToken(stringText, allowFailure: false);
            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            var actual = ConvertToString(virtualChars);
            Assert.Equal(expected, actual);
        }

        private void TestFailure(string stringText)
        {
            var token = GetStringToken(stringText, allowFailure: true);
            if (token == default)
            {
                return;
            }

            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            Assert.True(virtualChars.IsDefault);
        }

        [Fact]
        public void TestEmptyString()
        {
            Test("\"\"", "");
        }

        [Fact]
        public void TestEmptyVerbatimString()
        {
            Test("@\"\"", "");
        }

        [Fact]
        public void TestSimpleString()
        {
            Test("\"a\"", "['a',[1,2]]");
        }

        [Fact]
        public void TestSimpleMultiCharString()
        {
            Test("\"abc\"", "['a',[1,2]]['b',[2,3]]['c',[3,4]]");
        }

        [Fact]
        public void TestBracesInSimpleString()
        {
            Test("\"{{\"", "['{',[1,2]]['{',[2,3]]");
        }

        [Fact]
        public void TestBracesInInterpolatedSimpleString()
        {
            Test("$\"{{\"", "['{',[2,4]]");
        }

        [Fact]
        public void TestBracesInInterpolatedVerbatimSimpleString()
        {
            Test("$@\"{{\"", "['{',[3,5]]");
        }

        [Fact]
        public void TestBracesInReverseInterpolatedVerbatimSimpleString()
        {
            Test("@$\"{{\"", "['{',[3,5]]");
        }

        [Fact]
        public void TestEscapeInInterpolatedSimpleString()
        {
            Test("$\"\\n\"", @"['\u000A',[2,4]]");
        }

        [Fact]
        public void TestEscapeInInterpolatedVerbatimSimpleString()
        {
            Test("$@\"\\n\"", @"['\u005C',[3,4]]['n',[4,5]]");
        }

        [Fact]
        public void TestSimpleVerbatimString()
        {
            Test("@\"a\"", "['a',[2,3]]");
        }

        [Fact]
        public void TestUnterminatedString()
        {
            TestFailure("\"");
        }

        [Fact]
        public void TestUnterminatedVerbatimString()
        {
            TestFailure("@\"");
        }

        [Fact]
        public void TestSimpleEscape()
        {
            Test(@"""a\ta""", "['a',[1,2]]['\\u0009',[2,4]]['a',[4,5]]");
        }

        [Fact]
        public void TestMultipleSimpleEscape()
        {
            Test(@"""a\t\ta""", "['a',[1,2]]['\\u0009',[2,4]]['\\u0009',[4,6]]['a',[6,7]]");
        }

        [Fact]
        public void TestNonEscapeInVerbatim()
        {
            Test(@"@""a\ta""", "['a',[2,3]]['\\u005C',[3,4]]['t',[4,5]]['a',[5,6]]");
        }

        [Fact]
        public void TestInvalidHexEscape()
        {
            TestFailure(@"""\xZ""");
        }

        [Fact]
        public void TestValidHex1Escape()
        {
            Test(@"""\xa""", @"['\u000A',[1,4]]");
        }

        [Fact]
        public void TestValidHex1EscapeInInterpolatedString()
        {
            Test(@"$""\xa""", @"['\u000A',[2,5]]");
        }

        [Fact]
        public void TestValidHex2Escape()
        {
            Test(@"""\xaa""", @"['\u00AA',[1,5]]");
        }

        [Fact]
        public void TestValidHex3Escape()
        {
            Test(@"""\xaaa""", @"['\u0AAA',[1,6]]");
        }

        [Fact]
        public void TestValidHex4Escape()
        {
            Test(@"""\xaaaa""", @"['\uAAAA',[1,7]]");
        }

        [Fact]
        public void TestValidHex5Escape()
        {
            Test(@"""\xaaaaa""", @"['\uAAAA',[1,7]]['a',[7,8]]");
        }

        [Fact]
        public void TestValidHex6Escape()
        {
            Test(@"""a\xaaaaa""", @"['a',[1,2]]['\uAAAA',[2,8]]['a',[8,9]]");
        }

        [Fact]
        public void TestInvalidUnicodeEscape()
        {
            TestFailure(@"""\u000""");
        }

        [Fact]
        public void TestValidUnicodeEscape1()
        {
            Test(@"""\u0000""", @"['\u0000',[1,7]]");
        }

        [Fact]
        public void TestValidUnicodeEscape2()
        {
            Test(@"""a\u0000a""", @"['a',[1,2]]['\u0000',[2,8]]['a',[8,9]]");
        }

        [Fact]
        public void TestInvalidLongUnicodeEscape1()
        {
            TestFailure(@"""\U0000""");
        }

        [Fact]
        public void TestInvalidLongUnicodeEscape2()
        {
            TestFailure(@"""\U10000000""");
        }

        [Fact]
        public void TestValidLongEscape1()
        {
            Test(@"""\U00000000""", @"['\u0000',[1,11]]");
        }

        [Fact]
        public void TestValidLongEscape2()
        {
            Test(@"""\U0000ffff""", @"['\uFFFF',[1,11]]");
        }

        [Fact]
        public void TestValidLongEscape3()
        {
            Test(@"""a\U00000000a""", @"['a',[1,2]]['\u0000',[2,12]]['a',[12,13]]");
        }

        [Fact]
        public void TestValidButUnsupportedLongEscape1()
        {
            var token = GetStringToken(@"""\U00010000""", allowFailure: false);
            Assert.False(token.ContainsDiagnostics);
            TestFailure(@"""\U00010000""");
        }

        [Fact]
        public void TestEscapedQuoteInVerbatimString()
        {
            Test("@\"a\"\"a\"", @"['a',[2,3]]['\u0022',[3,5]]['a',[5,6]]");
        }

        private string ConvertToString(VirtualCharSequence virtualChars)
        {
            var strings = ArrayBuilder<string>.GetInstance();
            foreach (var ch in virtualChars)
            {
                strings.Add(ConvertToString(ch));
            }

            return string.Join("", strings.ToImmutableAndFree());
        }

        private string ConvertToString(VirtualChar vc)
            => $"[{ConvertToString(vc.Char)},[{vc.Span.Start - _statementPrefix.Length},{vc.Span.End - _statementPrefix.Length}]]";

        private string ConvertToString(char c)
            => PrintAsUnicodeEscape(c) ? $"'\\u{((int)c).ToString("X4")}'" : $"'{c}'";

        private static bool PrintAsUnicodeEscape(char c)
        {
            if (char.IsLetterOrDigit(c) && c < 127)
            {
                return false;
            }

            if (c == '{' || c == '}')
            {
                return false;
            }

            return true;
        }
    }
}
