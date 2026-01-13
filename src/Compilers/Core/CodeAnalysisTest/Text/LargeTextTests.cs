// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class LargeTextTests : TestBase
    {
        private static SourceText CreateSourceText(string s, Encoding encoding = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream, encoding ?? Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    sw.Write(s);
                }

                return CreateSourceText(stream, encoding);
            }
        }

        private static SourceText CreateSourceText(Stream stream, Encoding encoding = null)
        {
            return LargeText.Decode(stream, encoding ?? Encoding.UTF8, SourceHashAlgorithm.Sha1, throwIfBinaryDetected: true, canBeEmbedded: false);
        }

        private static SourceText CreateSourceText(TextReader reader, int length, Encoding encoding = null)
        {
            return LargeText.Decode(reader, length, encoding ?? Encoding.UTF8, SourceHashAlgorithm.Sha1);
        }

        private const string HelloWorld = "Hello, world!";

        [Fact]
        public void BasicTest()
        {
            var text = CreateSourceText(HelloWorld);
            Assert.Equal(HelloWorld, text.ToString());
            Assert.Equal(HelloWorld.Length, text.Length);
        }

        [Fact]
        public void EmptyTest()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var text = CreateSourceText(stream);
                Assert.Equal(0, text.Length);
            }
        }

        [Fact]
        public void IndexerTest()
        {
            var text = CreateSourceText(HelloWorld);
            Assert.Throws<IndexOutOfRangeException>(() => text[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => text[HelloWorld.Length]);
            for (int i = HelloWorld.Length - 1; i >= 0; i--)
            {
                Assert.Equal(HelloWorld[i], text[i]);
            }
        }

        [Fact]
        public void CopyToTest()
        {
            var text = CreateSourceText(HelloWorld);

            const int destOffset = 10;
            char[] buffer = new char[HelloWorld.Length + destOffset];

            // Copy the entire text to a non-zero offset in the destination
            text.CopyTo(0, buffer.AsSpan(destOffset), text.Length);

            for (int i = 0; i < destOffset; i++)
            {
                Assert.Equal('\0', buffer[i]);
            }

            for (int i = destOffset; i < buffer.Length; i++)
            {
                Assert.Equal(HelloWorld[i - destOffset], buffer[i]);
            }

            Array.Clear(buffer, 0, buffer.Length);

            // Copy a sub-string
            text.CopyTo(3, buffer, 3);
            Assert.Equal(HelloWorld[3], buffer[0]);
            Assert.Equal(HelloWorld[4], buffer[1]);
            Assert.Equal(HelloWorld[5], buffer[2]);
            for (int i = 3; i < buffer.Length; i++)
            {
                Assert.Equal('\0', buffer[i]);
            }
        }

        [Fact]
        public void CopyToLargeTest()
        {
            // Tests CopyTo at the chunk boundaries
            int targetLength = LargeText.ChunkSize * 2;

            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true))
                {
                    while (stream.Length < targetLength)
                    {
                        sw.Write(HelloWorld);
                    }
                }

                var text = SourceText.From(stream);
                Assert.IsType<LargeText>(text);

                char[] buffer = new char[HelloWorld.Length];
                for (int start = 0; start < text.Length; start += HelloWorld.Length)
                {
                    text.CopyTo(start, buffer, HelloWorld.Length);
                    Assert.Equal(HelloWorld, new string(buffer));
                }
            }
        }

        private static void CheckEqualLine(TextLine first, TextLine second)
        {
            Assert.Equal(first, second);
#if false
            // We do not guarantee either identity or Equals!
            Assert.Equal(first.Extent, second.Extent);
            Assert.Equal(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        private static void CheckNotEqualLine(TextLine first, TextLine second)
        {
            Assert.NotEqual(first, second);
#if false
            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Extent, second.Extent);
            Assert.NotEqual(first.ExtentIncludingLineBreak, second.ExtentIncludingLineBreak);
#endif
        }

        private static void CheckLine(SourceText text, int lineNumber, int start, int length, int newlineLength, string lineText)
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
            var newline = Environment.NewLine;
            var data = CreateSourceText("goo" + newline + " bar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: newline.Length, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 3 + newline.Length, length: 4, newlineLength: 0, lineText: " bar");
        }

        [Fact]
        public void NewLines2()
        {
            var text =
@"goo
bar
baz";
            var data = CreateSourceText(text);
            Assert.Equal(3, data.Lines.Count);
            var newlineLength = Environment.NewLine.Length;
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 3 + newlineLength, length: 3, newlineLength, lineText: "bar");
            CheckLine(data, lineNumber: 2, start: 2 * (3 + newlineLength), length: 3, newlineLength: 0, lineText: "baz");
        }

        [Fact]
        public void NewLines3()
        {
            var data = CreateSourceText("goo\r\nbar");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 2, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 5, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void NewLines4()
        {
            var data = CreateSourceText("goo\n\rbar\u2028");
            Assert.Equal(4, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 1, lineText: "");
            CheckLine(data, lineNumber: 2, start: 5, length: 3, newlineLength: 1, lineText: "bar");
            CheckLine(data, lineNumber: 3, start: 9, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void NewLines5()
        {
            // Trailing CR
            var data = CreateSourceText("goo\r");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void NewLines6()
        {
            // Trailing CR+LF
            var data = CreateSourceText("goo\r\n");
            Assert.Equal(2, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 2, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 5, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void NewLines7()
        {
            // Consecutive CR
            var data = CreateSourceText("goo\r\rbar");
            Assert.Equal(3, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 1, lineText: "");
            CheckLine(data, lineNumber: 2, start: 5, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void NewLines8()
        {
            // Mix CR with CR+LF
            const string cr = "\r";
            const string crLf = "\r\n";
            var data = CreateSourceText("goo" + cr + crLf + cr + "bar");
            Assert.Equal(4, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 2, lineText: "");
            CheckLine(data, lineNumber: 2, start: 6, length: 0, newlineLength: 1, lineText: "");
            CheckLine(data, lineNumber: 3, start: 7, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void NewLines9()
        {
            // Mix CR with CR+LF
            const string cr = "\r";
            const string crLf = "\r\n";
            const string lf = "\n";
            var data = CreateSourceText("goo" + cr + crLf + lf + "bar");
            Assert.Equal(4, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 3, newlineLength: 1, lineText: "goo");
            CheckLine(data, lineNumber: 1, start: 4, length: 0, newlineLength: 2, lineText: "");
            CheckLine(data, lineNumber: 2, start: 6, length: 0, newlineLength: 1, lineText: "");
            CheckLine(data, lineNumber: 3, start: 7, length: 3, newlineLength: 0, lineText: "bar");
        }

        [Fact]
        public void Empty()
        {
            var data = CreateSourceText("");
            Assert.Equal(1, data.Lines.Count);
            CheckLine(data, lineNumber: 0, start: 0, length: 0, newlineLength: 0, lineText: "");
        }

        [Fact]
        public void LinesGetText1()
        {
            var text =
@"goo
bar baz";
            var data = CreateSourceText(text);
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal("goo", data.Lines[0].ToString());
            Assert.Equal("bar baz", data.Lines[1].ToString());
        }

        [Fact]
        public void LinesGetText2()
        {
            var text = "goo";
            var data = CreateSourceText(text);
            Assert.Equal("goo", data.Lines[0].ToString());
        }

        [Fact]
        public void FromTextReader()
        {
            var expected = "goo";
            var expectedSourceText = CreateSourceText(expected);

            var actual = new StringReader(expected);
            var actualSourceText = CreateSourceText(actual, expected.Length);

            Assert.Equal("goo", actualSourceText.Lines[0].ToString());
            Assert.Equal<byte>(expectedSourceText.GetChecksum(), actualSourceText.GetChecksum());
        }
    }
}
