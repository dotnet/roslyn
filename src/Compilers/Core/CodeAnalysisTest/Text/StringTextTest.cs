// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Text;
using System.IO;
using Roslyn.Test.Utilities;
using System.Security.Cryptography;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    /// <summary>
    /// This is a test class for StringTextTest and is intended
    /// to contain all StringTextTest Unit Tests
    /// </summary>
    public class StringTextTest
    {
        internal static string ChecksumToHexQuads(ImmutableArray<byte> checksum)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < checksum.Length; i++)
            {
                if (i > 0 && ((i % 4) == 0))
                {
                    builder.Append(' ');
                }

                byte b = checksum[i];
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }

        [Fact]
        public void FromString()
        {
            var data = SourceText.From("goo", Encoding.UTF8);
            Assert.Equal(1, data.Lines.Count);
            Assert.Equal(3, data.Lines[0].Span.Length);
        }

        [Fact]
        public void FromString_DefaultEncoding()
        {
            var data = SourceText.From("goo");
            Assert.Null(data.Encoding);
        }

        [Fact]
        public void FromStringEmpty()
        {
            var data = SourceText.From(string.Empty);
            Assert.Equal(1, data.Lines.Count);
            Assert.Equal(0, data.Lines[0].Span.Length);
        }

        [Fact]
        public void FromString_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => SourceText.From((string)null, Encoding.UTF8));
        }

        [Fact]
        public void FromStream_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => SourceText.From((Stream)null, Encoding.UTF8));
            Assert.Throws<ArgumentException>(() => SourceText.From(new TestStream(canRead: false, canSeek: true), Encoding.UTF8));
            Assert.Throws<ArgumentException>(() => SourceText.From(new TestStream(canRead: true, canSeek: false), Encoding.UTF8));
        }

        [Fact]
        public void Indexer1()
        {
            var data = SourceText.From(string.Empty, Encoding.UTF8);
            Assert.Throws(
                typeof(IndexOutOfRangeException),
                () => { var value = data[-1]; });
        }

        private void CheckEqualLine(TextLine first, TextLine second)
        {
            Assert.Equal(first, second);
#if false
            // We do not guarantee either identity or Equals!
            Assert.Equal(first.Extent, second.Extent);
            Assert.Equal(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        private void CheckNotEqualLine(TextLine first, TextLine second)
        {
            Assert.NotEqual(first, second);
#if false
            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Extent, second.Extent);
            Assert.NotEqual(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        private void CheckLine(SourceText text, int lineNumber, int start, int length, int newlineLength, string lineText)
        {
            var textLine = text.Lines[lineNumber];

            Assert.Equal(start, textLine.Start);
            Assert.Equal(start + length, textLine.End);
            Assert.Equal(start + length + newlineLength, textLine.EndIncludingLineBreak);
            Assert.Equal(start, textLine.Span.Start);
            Assert.Equal(length, textLine.Span.Length);
            Assert.Equal(start, textLine.SpanIncludingLineBreak.Start);
            Assert.Equal(length + newlineLength, textLine.SpanIncludingLineBreak.Length);
            Assert.Equal(lineNumber, textLine.LineNumber);
            Assert.Equal(lineText, textLine.ToString());
            Assert.Equal(text.ToString().Substring(start, length), textLine.ToString());

            CheckEqualLine(textLine, text.Lines[lineNumber]);
            for (int p = textLine.Start; p < textLine.EndIncludingLineBreak; ++p)
            {
                CheckEqualLine(textLine, text.Lines.GetLineFromPosition(p));
                Assert.Equal(lineNumber, text.Lines.IndexOf(p));
                Assert.Equal(lineNumber, text.Lines.GetLinePosition(p).Line);
                Assert.Equal(p - start, text.Lines.GetLinePosition(p).Character);
            }

            if (start != 0)
            {
                CheckNotEqualLine(textLine, text.Lines.GetLineFromPosition(start - 1));
                Assert.Equal(lineNumber - 1, text.Lines.IndexOf(start - 1));
                Assert.Equal(lineNumber - 1, text.Lines.GetLinePosition(start - 1).Line);
            }

            int nextPosition = start + length + newlineLength;
            if (nextPosition < text.Length)
            {
                CheckNotEqualLine(textLine, text.Lines.GetLineFromPosition(nextPosition));
                Assert.Equal(lineNumber + 1, text.Lines.IndexOf(nextPosition));
                Assert.Equal(lineNumber + 1, text.Lines.GetLinePosition(nextPosition).Line);
            }
        }

        [Fact]
        public void NewLines1()
        {
            string newLine = Environment.NewLine;
            var data = SourceText.From("goo" + newLine + " bar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: newLine.Length, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 3 + newLine.Length, length: 4, newlineLength: 0, lineText: " bar");
        }

        [Fact]
        public void NewLines2()
        {
            var text =
@"goo
bar
baz";
            var data = SourceText.From(text);
            Assert.Equal(3, data.Lines.Count);
            var newlineLength = Environment.NewLine.Length;
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: newlineLength, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 3 + newlineLength, length: 3, newlineLength: newlineLength, lineText: "bar");
            CheckLine(data, lineNumber: 2, start: 2 * (3 + newlineLength), length: 3, newlineLength: 0, lineText: "baz");
        }

        [Fact]
        public void NewLines3()
        {
            var data = SourceText.From("goo\r\nbar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 2, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 5, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void NewLines4()
        {
            var data = SourceText.From("goo\n\rbar\u2028");
            Assert.Equal(4, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 1, lineText: "");
            CheckLine(data, lineNumber: 2, start: 5, length: 3, newlineLength: 1, lineText: "bar");
            CheckLine(data, lineNumber: 3, start: 9, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void Empty()
        {
            var data = SourceText.From("");
            Assert.Equal(1, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void LinesGetText1()
        {
            var text =
@"goo
bar baz";
            var data = SourceText.From(text);
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal("goo", data.Lines[0].ToString());
            Assert.Equal("bar baz", data.Lines[1].ToString());
        }

        [Fact]
        public void LinesGetText2()
        {
            var text = "goo";
            var data = SourceText.From(text);
            Assert.Equal("goo", data.Lines[0].ToString());
        }

        [Fact]
        public void CheckSum_Utf8_BOM()
        {
            var data = SourceText.From("The quick brown fox jumps over the lazy dog", Encoding.UTF8);
            var checksum = data.GetChecksum();
            Assert.Equal("88d3ed78 9b0bae8b ced8e348 91133516 b79ba9fb", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void FromStream_CheckSum_BOM()
        {
            var bytes = new byte[] { 0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63 };

            var source = SourceText.From(new MemoryStream(bytes), Encoding.ASCII);
            Assert.Equal("abc", source.ToString());

            var checksum = source.GetChecksum();
            AssertEx.Equal(CryptographicHashProvider.ComputeSha1(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_NoBOM()
        {
            // Note: The 0x95 is outside the ASCII range, so a question mark will
            // be substituted in decoded text. Note, however, that the checksum
            // should be derived from the original input.
            var bytes = new byte[] { 0x61, 0x62, 0x95 };

            var source = SourceText.From(new MemoryStream(bytes), Encoding.ASCII);
            Assert.Equal("ab?", source.ToString());

            var checksum = source.GetChecksum();
            AssertEx.Equal(CryptographicHashProvider.ComputeSha1(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_DefaultEncoding()
        {
            var bytes = Encoding.UTF8.GetBytes("\u1234");

            var source = SourceText.From(new MemoryStream(bytes));
            Assert.Equal("\u1234", source.ToString());

            var checksum = source.GetChecksum();
            AssertEx.Equal(CryptographicHashProvider.ComputeSha1(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_SeekToBeginning()
        {
            var bytes = new byte[] { 0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63 };

            var stream = new MemoryStream(bytes);
            stream.Seek(3, SeekOrigin.Begin);

            var source = SourceText.From(stream, Encoding.ASCII);
            Assert.Equal("abc", source.ToString());

            var checksum = source.GetChecksum();
            AssertEx.Equal(CryptographicHashProvider.ComputeSha1(bytes), checksum);
        }
    }
}
