// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Roslyn.Utilities.UnitTests.InternalUtilities
{
    public class JsonWriterTests
    {
        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void Boolean(string expected, bool value)
        {
            Assert.StrictEqual(expected, WriteToString(value));
        }

        [Theory]
        [InlineData("-2147483648", int.MinValue)]
        [InlineData("-42", -42)]
        [InlineData("-1", -1)]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("42", 42)]
        [InlineData("2147483647", int.MaxValue)]
        public void Integer(string expected, int value)
        {
            Assert.StrictEqual(expected, WriteToString(value));
        }

        [Theory]
        // escaped
        [InlineData(@"\\", '\\')]
        [InlineData(@"\""", '"')]
        // unescaped
        [InlineData(@"'", '\'')]
        [InlineData(@"/", '/')]
        //
        // There are 5 ranges of characters that have output in the form of "\u<code>".
        // The following tests the start and end character and at least one character in each range
        // and also in between the ranges.
        //
        // #1. 0x0000 - 0x001F
        [InlineData(@"\u0000", (char)0x00)]
        [InlineData(@"\u0010", (char)0x10)]
        [InlineData(@"\u001f", (char)0x1f)]
        // Between #1 and #2
        [InlineData(@"a", (char)0x61)]
        // #2. 0x0085 NEXT LINE
        [InlineData(@"\u0085", (char)0x85)]
        // Between #2 and #3
        [InlineData(@"ñ", (char)0xF1)]
        // #3. 0x2028 LINE SEPARATOR - 0x2029 PARAGRAPH SEPARATOR
        [InlineData(@"\u2028", (char)0x2028)]
        [InlineData(@"\u2029", (char)0x2029)]
        // Between #3 and #4
        [InlineData(@"漢", (char)0x6F22)]
        // #4. 0xD800 - 0xDFFF
        [InlineData(@"\ud800", (char)0xd800)]
        [InlineData(@"\udabc", (char)0xdabc)]
        [InlineData(@"\udfff", (char)0xdfff)]
        // Between #4 and #5
        [InlineData("\ueabc", (char)0xeabc)]
        // #5. 0xFFFE - 0xFFFF
        [InlineData(@"\ufffe", (char)0xfffe)]
        [InlineData(@"\uffff", (char)0xffff)]
        public void Character(string expected, char value)
        {
            WriteInMultiplePositionsAndCheck(expected, value.ToString());
        }

        [Theory]
        [InlineData(@"\ud83d\udc4d", "👍")]
        public void String(string expected, string value)
        {
            WriteInMultiplePositionsAndCheck(expected, value);
        }

        private static void WriteInMultiplePositionsAndCheck(string expected, string value)
        {
            Assert.StrictEqual($"\"{expected}\"", WriteToString(value));
            Assert.StrictEqual($"\"{expected}_after\"", WriteToString($"{value}_after"));
            Assert.StrictEqual($"\"before_{expected}\"", WriteToString($"before_{value}"));
            Assert.StrictEqual($"\"before_{expected}_after\"", WriteToString($"before_{value}_after"));
        }

        private static string WriteToString(Action<JsonWriter> action)
        {
            var stringWriter = new StringWriter();

            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                action(jsonWriter);
            }

            return stringWriter.ToString();
        }

        private static string WriteToString(bool value)
        {
            return WriteToString(j => j.Write(value));
        }

        private static string WriteToString(int value)
        {
            return WriteToString(j => j.Write(value));
        }

        private static string WriteToString(string value)
        {
            return WriteToString(j => j.Write(value));
        }
    }
}
