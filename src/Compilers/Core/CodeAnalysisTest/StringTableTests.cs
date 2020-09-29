// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class StringTableTests
    {
        [Fact]
        public void TestAddSameWithStringBuilderProducesSameStringInstance()
        {
            var st = new StringTable();
            var sb1 = new StringBuilder("goo");
            var sb2 = new StringBuilder("goo");
            var s1 = st.Add(sb1);
            var s2 = st.Add(sb2);
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestAddDifferentWithStringBuilderProducesDifferentStringInstance()
        {
            var st = new StringTable();
            var sb1 = new StringBuilder("goo");
            var sb2 = new StringBuilder("bar");
            var s1 = st.Add(sb1);
            var s2 = st.Add(sb2);
            Assert.NotEqual((object)s1, (object)s2);
        }

        [Fact]
        public void TestAddSameWithVariousInputsProducesSameStringInstance()
        {
            var st = new StringTable();

            var s1 = st.Add(new StringBuilder(" "));
            var s2 = st.Add(' ');
            Assert.Same(s1, s2);

            var s3 = st.Add(" ");
            Assert.Same(s2, s3);

            var s4 = st.Add(new char[1] { ' ' }, 0, 1);
            Assert.Same(s3, s4);

            var s5 = st.Add("ABC DEF", 3, 1);
            Assert.Same(s4, s5);
        }

        [Fact]
        public void TestAddSameWithCharProducesSameStringInstance()
        {
            var st = new StringTable();
            var s1 = st.Add(' ');
            var s2 = st.Add(' ');
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestSharedAddSameWithStringBuilderProducesSameStringInstance()
        {
            var sb1 = new StringBuilder("goo");
            var sb2 = new StringBuilder("goo");
            var s1 = new StringTable().Add(sb1);
            var s2 = new StringTable().Add(sb2);
            Assert.Same(s1, s2);
        }

        [Fact]
        public void TestSharedAddSameWithCharProducesSameStringInstance()
        {
            // Regression test for a bug where single-char strings were not being
            // found in the shared table.
            var s1 = new StringTable().Add(' ');
            var s2 = new StringTable().Add(' ');
            Assert.Same(s1, s2);
        }

        private static unsafe bool TestTextEqualsASCII(string str, string ascii)
        {
            fixed (byte* ptr = Encoding.ASCII.GetBytes(ascii))
            {
                var ptrResult = StringTable.TextEqualsASCII(str, new ReadOnlySpan<byte>(ptr, ascii.Length));
                var sbResult = StringTable.TextEquals(str, new StringBuilder(ascii));
                var substrResult = StringTable.TextEquals(str, "xxx" + ascii + "yyy", 3, ascii.Length);
                Assert.Equal(substrResult, sbResult);
                Assert.Equal(ptrResult, sbResult);
                return ptrResult;
            }
        }

        [Fact]
        public void TextEquals1()
        {
            Assert.True(TestTextEqualsASCII("", ""));
            Assert.False(TestTextEqualsASCII("a", ""));
            Assert.False(TestTextEqualsASCII("", "a"));
            Assert.True(TestTextEqualsASCII("a", "a"));
            Assert.False(TestTextEqualsASCII("a", "ab"));
            Assert.False(TestTextEqualsASCII("ab", "a"));
            Assert.False(TestTextEqualsASCII("abc", "a"));
            Assert.False(TestTextEqualsASCII("abcd", "a"));
            Assert.False(TestTextEqualsASCII("abcde", "a"));
            Assert.False(TestTextEqualsASCII("abcdef", "a"));
            Assert.False(TestTextEqualsASCII("abcdefg", "a"));
            Assert.False(TestTextEqualsASCII("abcdefgh", "a"));
            Assert.False(TestTextEqualsASCII("a", "ab"));
            Assert.False(TestTextEqualsASCII("a", "abc"));
            Assert.False(TestTextEqualsASCII("a", "abcd"));
            Assert.False(TestTextEqualsASCII("a", "abcde"));
            Assert.False(TestTextEqualsASCII("a", "abcdef"));
            Assert.False(TestTextEqualsASCII("a", "abcdefg"));
            Assert.False(TestTextEqualsASCII("a", "abcdefgh"));
            Assert.False(TestTextEqualsASCII("\u1234", "a"));
            Assert.False(TestTextEqualsASCII("\ud800", "xx"));
            Assert.False(TestTextEqualsASCII("\uffff", ""));
        }

        [Fact]
        public void TestAddEqualSubstringsFromDifferentStringsWorks()
        {
            // Make neither of the strings equal to the result of the substring call
            // to test an issue that was surfaced by pooling the wrong string.
            var str1 = "abcd1";
            var str2 = "abcd2";
            var st = new StringTable();

            var s1 = st.Add(str1, 0, 4);
            var s2 = st.Add(str2, 0, 4);

            Assert.Same(s1, s2);
            Assert.Equal("abcd", s1);
        }
    }
}
