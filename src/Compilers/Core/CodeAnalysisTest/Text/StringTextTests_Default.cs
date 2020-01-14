// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StringTextTest_Default
    {
        private Encoding _currentEncoding;

        protected byte[] GetBytes(Encoding encoding, string source)
        {
            _currentEncoding = encoding;
            return encoding.GetBytesWithPreamble(source);
        }

        protected virtual SourceText Create(string source)
        {
            byte[] buffer = GetBytes(Encoding.Default, source);
            using (var stream = new MemoryStream(buffer, 0, buffer.Length, writable: false, publiclyVisible: true))
            {
                return EncodedStringText.Create(stream);
            }
        }

        /// <summary>
        /// Empty string case
        /// </summary>
        [Fact]
        public void Ctor2()
        {
            var data = Create(string.Empty);
            Assert.Equal(1, data.Lines.Count);
            Assert.Equal(0, data.Lines[0].Span.Length);
        }

        [Fact]
        public void Indexer1()
        {
            var data = Create(String.Empty);
            Assert.Throws<IndexOutOfRangeException>(
                () => { var value = data[-1]; });
        }

        [Fact]
        public void NewLines1()
        {
            string newLine = Environment.NewLine;
            var data = Create("goo" + newLine + " bar");
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal(3, data.Lines[0].Span.Length);
            Assert.Equal(3 + newLine.Length, data.Lines[1].Span.Start);
        }

        [Fact]
        public void NewLines2()
        {
            var text =
@"goo
bar
baz";
            var data = Create(text);
            Assert.Equal(3, data.Lines.Count);
            Assert.Equal("goo", data.ToString(data.Lines[0].Span));
            Assert.Equal("bar", data.ToString(data.Lines[1].Span));
            Assert.Equal("baz", data.ToString(data.Lines[2].Span));
        }

        [Fact]
        public void NewLines3()
        {
            var data = Create("goo\r\nbar");
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal("goo", data.ToString(data.Lines[0].Span));
            Assert.Equal("bar", data.ToString(data.Lines[1].Span));
        }

        [Fact]
        public void NewLines4()
        {
            var data = Create("goo\n\rbar");
            Assert.Equal(3, data.Lines.Count);
        }

        [Fact]
        public void LinesGetText1()
        {
            var data = Create(
@"goo
bar baz");
            Assert.Equal(2, data.Lines.Count);
            Assert.Equal("goo", data.Lines[0].ToString());
            Assert.Equal("bar baz", data.Lines[1].ToString());
        }

        [Fact]
        public void LinesGetText2()
        {
            var data = Create("goo");
            Assert.Equal("goo", data.Lines[0].ToString());
        }

#if false
        [Fact]
        public void TextLine1()
        {
            var text = Create("goo" + Environment.NewLine);
            var span = new TextSpan(0, 3);
            var line = new TextLine(text, 0, 0, text.Length);
            Assert.Equal(span, line.Extent);
            Assert.Equal(5, line.EndIncludingLineBreak);
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void GetText1()
        {
            var text = Create("goo");
            var line = new TextLine(text, 0, 0, 2);
            Assert.Equal("fo", line.ToString());
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void GetText2()
        {
            var text = Create("abcdef");
            var line = new TextLine(text, 0, 1, 2);
            Assert.Equal("bc", line.ToString());
            Assert.Equal(0, line.LineNumber);
        }
#endif

        [Fact]
        public void GetExtendedAsciiText()
        {
            var originalText = Encoding.Default.GetString(new byte[] { 0xAB, 0xCD, 0xEF });
            var encodedText = Create(originalText);
            Assert.Equal(originalText, encodedText.ToString());
        }
    }
}
