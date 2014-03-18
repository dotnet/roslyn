// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static SourceText CreateEncoded(string source, Encoding encoding)
        {
            var stream = new MemoryStream();

            using (var writer = new StreamWriter(stream, encoding, 512, leaveOpen: false))
            {
                writer.Write(source);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return new EncodedStringText(stream, encodingOpt: null);
            }          
        }

        [Fact]
        public void FromString()
        {
            var data = SourceText.From("foo");
            Assert.Equal(1, data.Lines.Count);
            Assert.Equal(3, data.Lines[0].Span.Length);
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
            Assert.Throws<ArgumentNullException>(() => SourceText.From((string)null));

            // invalid SHA1 hash
            Assert.Throws<ArgumentException>(() => SourceText.From("abc", ImmutableArray.Create<byte>()));
            Assert.Throws<ArgumentException>(() => SourceText.From("abc", ImmutableArray.Create<byte>(1, 3)));
        }

        [Fact]
        public void FromStream_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => SourceText.From((Stream)null));
            Assert.Throws<ArgumentException>(() => SourceText.From(new TestStream(canRead: false, canSeek: true)));
            Assert.Throws<ArgumentException>(() => SourceText.From(new TestStream(canRead: true, canSeek: false)));
        }

        [Fact]
        public void Indexer1()
        {
            var data = SourceText.From(String.Empty);
            Assert.Throws(
                typeof(IndexOutOfRangeException),
                () => { var value = data[-1]; });
        }

        void CheckEqualLine(TextLine first, TextLine second)
        {
            Assert.Equal(first, second);
#if false
            // We do not guarantee either identity or Equals!
            Assert.Equal(first.Extent, second.Extent);
            Assert.Equal(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        void CheckNotEqualLine(TextLine first, TextLine second)
        {
            Assert.NotEqual(first, second);
#if false
            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Extent, second.Extent);
            Assert.NotEqual(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        void CheckLine(SourceText text, int lineNumber, int start, int length, int newlineLength, string lineText)
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
            var data = SourceText.From("foo" + Environment.NewLine + " bar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber:0, start:0, length:3, newlineLength:2, lineText:"foo");
            CheckLine(data, lineNumber:1, start:5, length:4, newlineLength:0, lineText:" bar");
        }

        [Fact]
        public void NewLines2()
        {
            var text =
@"foo
bar
baz";
            var data = SourceText.From(text);
            Assert.Equal(3, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 2, lineText: "foo");
            CheckLine(data, lineNumber: 1, start: 5, length: 3, newlineLength: 2, lineText: "bar");
            CheckLine(data, lineNumber: 2, start: 10, length: 3, newlineLength: 0, lineText: "baz");
        }

        [Fact]
        public void NewLines3()
        {
            var data = SourceText.From("foo\r\nbar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 2, lineText: "foo");
            CheckLine(data, lineNumber: 1, start: 5, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void NewLines4()
        {
            var data = SourceText.From("foo\n\rbar\u2028");
            Assert.Equal(4, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "foo");
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
@"foo
bar baz";
            var data = SourceText.From(text);
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal("foo", data.Lines[0].ToString());
            Assert.Equal("bar baz", data.Lines[1].ToString());
        }

        [Fact]
        public void LinesGetText2()
        {
            var text = "foo";
            var data = SourceText.From(text);
            Assert.Equal("foo", data.Lines[0].ToString());
        }

        private static string ChecksumToHexQuads(ImmutableArray<byte> checksum)
        {
            Assert.Equal(Hash.Sha1HashSize, checksum.Length);

            var builder = new StringBuilder();

            for (int i = 0; i < Hash.Sha1HashSize; i++)
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
        public void CheckSum_Unspecified1()
        {
            var data = SourceText.From("The quick brown fox jumps over the lazy dog");
            var checksum = data.GetSha1Checksum();
            Assert.True(checksum.IsEmpty);
        }


        [Fact]
        public void CheckSum002()
        {
            var data = CreateEncoded("The quick brown fox jumps over the lazy dog", Encoding.ASCII);

            // this is known to be "2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetSha1Checksum();
            Assert.Equal("2fd4e1c6 7a2d28fc ed849ee1 bb76e739 1b93eb12", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum003()
        {
            var data = CreateEncoded("The quick brown fox jumps over the lazy dog", Encoding.Unicode);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("9d0047c0 8c84a7ef a55a955e aa3b4aae f62c9c39", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum004()
        {
            var data = CreateEncoded("The quick brown fox jumps over the lazy dog", Encoding.BigEndianUnicode);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("72b2beae c76188ac 5b38c16c 4f9d518a 2be0a34c", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum006()
        {
            var data = CreateEncoded("", Encoding.ASCII);

            // this is known to be "da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", see http://en.wikipedia.org/wiki/SHA-1
            var checksum = data.GetSha1Checksum();
            Assert.Equal("da39a3ee 5e6b4b0d 3255bfef 95601890 afd80709", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum007()
        {
            var data = CreateEncoded("", Encoding.Unicode);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("d62636d8 caec13f0 4e28442a 0a6fa1af eb024bbb", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum008()
        {
            var data = CreateEncoded("", Encoding.BigEndianUnicode);

            var checksum = data.GetSha1Checksum();
            Assert.Equal("26237800 2c95ae7e 29535cb9 f438db21 9adf98f5", ChecksumToHexQuads(checksum));
        }

        [Fact]
        public void CheckSum_Explicit()
        {
            var hash = ImmutableArray.Create<byte>(
                0x01, 0x02, 0x03, 0x04, 0x05,
                0x01, 0x02, 0x03, 0x04, 0x05,
                0x01, 0x02, 0x03, 0x04, 0x05,
                0x01, 0x02, 0x03, 0x04, 0x05);

            var source = SourceText.From("foo", hash);
            AssertEx.Equal(hash, source.GetSha1Checksum());
        }

        [Fact]
        public void FromStream_CheckSum_BOM()
        {
            var bytes = new byte[] { 0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63 };

            var source = SourceText.From(new MemoryStream(bytes), Encoding.ASCII);
            Assert.Equal("abc", source.ToString());

            var checksum = source.GetSha1Checksum();
            Assert.Equal(new SHA1CryptoServiceProvider().ComputeHash(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_NoBOM()
        {
            var bytes = new byte[] { 0x61, 0x62, 0x95 };

            var source = SourceText.From(new MemoryStream(bytes), Encoding.ASCII);
            Assert.Equal("ab?", source.ToString());

            var checksum = source.GetSha1Checksum();
            Assert.Equal(new SHA1CryptoServiceProvider().ComputeHash(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_DefaultEncoding()
        {
            var bytes = Encoding.UTF8.GetBytes("\u1234");

            var source = SourceText.From(new MemoryStream(bytes));
            Assert.Equal("\u1234", source.ToString());

            var checksum = source.GetSha1Checksum();
            Assert.Equal(new SHA1CryptoServiceProvider().ComputeHash(bytes), checksum);
        }

        [Fact]
        public void FromStream_CheckSum_SeekToBeginning()
        {
            var bytes = new byte[] { 0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63 };

            var stream = new MemoryStream(bytes);
            stream.Seek(3, SeekOrigin.Begin);

            var source = SourceText.From(stream, Encoding.ASCII);
            Assert.Equal("abc", source.ToString());

            var checksum = source.GetSha1Checksum();
            Assert.Equal(new SHA1CryptoServiceProvider().ComputeHash(bytes), checksum);
        }
    }
}
