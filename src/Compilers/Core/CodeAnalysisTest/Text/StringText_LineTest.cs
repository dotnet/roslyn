// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class StringText_LineTest
    {
        [Fact]
        public void FromSpanNotIncludingBreaks()
        {
            string newLine = Environment.NewLine;
            var text = SourceText.From("goo" + newLine);
            var span = new TextSpan(0, 3);
            var line = TextLine.FromSpan(text, span);
            Assert.Equal(span, line.Span);
            Assert.Equal(3 + newLine.Length, line.EndIncludingLineBreak);
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void FromSpanIncludingBreaksAtEnd()
        {
            var text = SourceText.From("goo" + Environment.NewLine);
            var span = TextSpan.FromBounds(0, text.Length);
            var line = TextLine.FromSpan(text, span);
            Assert.Equal(span, line.SpanIncludingLineBreak);
            Assert.Equal(3, line.End);
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void FromSpanIncludingBreaks()
        {
            var text = SourceText.From("goo" + Environment.NewLine + "bar");
            var span = TextSpan.FromBounds(0, text.Length);
            var line = TextLine.FromSpan(text, span);
            Assert.Equal(span, line.SpanIncludingLineBreak);
            Assert.Equal(text.Length, line.End);
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void FromSpanNoBreaksBeforeOrAfter()
        {
            var text = SourceText.From("goo");
            var line = TextLine.FromSpan(text, new TextSpan(0, 3));
            Assert.Equal("goo", line.ToString());
            Assert.Equal(0, line.LineNumber);
        }

        [Fact]
        public void FromSpanZeroLengthNotEndOfLineThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(0, 0)));
        }

        [Fact]
        public void FromSpanNotEndOfLineThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(0, 3)));
        }

        [Fact]
        public void FromSpanNotStartOfLineThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(1, 5)));
        }

        [Fact]
        public void FromSpanZeroLengthAtEnd()
        {
            var text = SourceText.From("goo" + Environment.NewLine);
            var start = text.Length;
            var line = TextLine.FromSpan(text, new TextSpan(start, 0));
            Assert.Equal("", line.ToString());
            Assert.Equal(0, line.Span.Length);
            Assert.Equal(0, line.SpanIncludingLineBreak.Length);
            Assert.Equal(start, line.Start);
            Assert.Equal(start, line.Span.Start);
            Assert.Equal(start, line.SpanIncludingLineBreak.Start);
            Assert.Equal(1, line.LineNumber);
        }

        [Fact]
        public void FromSpanZeroLengthWithLineBreak()
        {
            var text = SourceText.From(Environment.NewLine);
            var line = TextLine.FromSpan(text, new TextSpan(0, 0));
            Assert.Equal("", line.ToString());
            Assert.Equal(Environment.NewLine.Length, line.SpanIncludingLineBreak.Length);
        }

        [Fact]
        public void FromSpanLengthGreaterThanTextThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(1, 10)));
        }

        [Fact]
        public void FromSpanStartsBeforeZeroThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(-1, 2)));
        }

        [Fact]
        public void FromSpanZeroLengthBeyondEndThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentOutOfRangeException>(() => TextLine.FromSpan(text, new TextSpan(7, 0)));
        }

        [Fact]
        public void FromSpanTextNullThrows()
        {
            var text = SourceText.From("abcdef");
            Assert.Throws<ArgumentNullException>(() => TextLine.FromSpan(null, new TextSpan(0, 2)));
        }
    }
}
