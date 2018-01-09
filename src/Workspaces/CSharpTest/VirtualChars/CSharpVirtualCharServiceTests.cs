// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.VirtualChars;
using Microsoft.CodeAnalysis.VirtualChars;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.VirtualChars
{
    public class CSharpVirtualCharServiceTests
    {
        private const string _statmentPrefix = "var v = ";
        
        private SyntaxToken GetStringToken(string text)
        {
            var statement = _statmentPrefix + text;
            var parsedStatement = SyntaxFactory.ParseStatement(statement);
            var token = parsedStatement.DescendantTokens().ToArray()[3];
            Assert.True(token.Kind() == SyntaxKind.StringLiteralToken);

            return token;
        }

        private void Test(string stringText, string expected)
        {
            var token = GetStringToken(stringText);
            var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
            var actual = ConvertToString(virtualChars);
            Assert.Equal(expected, actual);
        }

        private void TestFailure(string stringText)
        {
            var token = GetStringToken(stringText);
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
            var token = GetStringToken(@"""\U00010000""");
            Assert.False(token.ContainsDiagnostics);
            TestFailure(@"""\U00010000""");
        }

        [Fact]
        public void TestEscapedQuoteInVerbatimString()
        {
            Test("@\"a\"\"a\"", @"['a',[2,3]]['\u0022',[3,5]]['a',[5,6]]");
        }

        private string ConvertToString(ImmutableArray<VirtualChar> virtualChars)
            => string.Join("", virtualChars.Select(ConvertToString));

        private string ConvertToString(VirtualChar vc)
            => $"[{ConvertToString(vc.Char)},[{vc.Span.Start - _statmentPrefix.Length},{vc.Span.End - _statmentPrefix.Length}]]";

        private string ConvertToString(char c)
            => char.IsLetterOrDigit(c) && c < 127 ? $"'{c}'" : $"'\\u{((int)c).ToString("X4")}'";
    }
}
