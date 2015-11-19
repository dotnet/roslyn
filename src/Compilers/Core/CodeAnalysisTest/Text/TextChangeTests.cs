﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TextChangeTests
    {
        [Fact]
        public void TestSubTextStart()
        {
            var text = SourceText.From("Hello World");
            var subText = text.GetSubText(6);
            Assert.Equal("World", subText.ToString());
        }

        [Fact]
        public void TestSubTextSpanFirst()
        {
            var text = SourceText.From("Hello World");
            var subText = text.GetSubText(new TextSpan(0, 5));
            Assert.Equal("Hello", subText.ToString());
        }

        [Fact]
        public void TestSubTextSpanLast()
        {
            var text = SourceText.From("Hello World");
            var subText = text.GetSubText(new TextSpan(6, 5));
            Assert.Equal("World", subText.ToString());
        }

        [Fact]
        public void TestSubTextSpanMid()
        {
            var text = SourceText.From("Hello World");
            var subText = text.GetSubText(new TextSpan(4, 3));
            Assert.Equal("o W", subText.ToString());
        }

        [Fact]
        public void TestChangedText()
        {
            var text = SourceText.From("Hello World");
            var newText = text.Replace(6, 0, "Beautiful ");
            Assert.Equal("Hello Beautiful World", newText.ToString());
        }

        [Fact]
        public void TestChangedTextChanges()
        {
            var text = SourceText.From("Hello World");
            var newText = text.Replace(6, 0, "Beautiful ");

            var changes = newText.GetChangeRanges(text);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(6, changes[0].Span.Start);
            Assert.Equal(0, changes[0].Span.Length);
            Assert.Equal(10, changes[0].NewLength);
        }

        [Fact]
        public void TestChangedTextWithMultipleChanges()
        {
            var text = SourceText.From("Hello World");
            var newText = text.WithChanges(
                new TextChange(new TextSpan(0, 5), "Halo"),
                new TextChange(new TextSpan(6, 5), "Universe"));

            Assert.Equal("Halo Universe", newText.ToString());
        }

        [Fact]
        public void TestChangedTextWithMultipleOverlappingChanges()
        {
            var text = SourceText.From("Hello World");
            var changes = new[]
            {
                new TextChange(new TextSpan(0, 5), "Halo"),
                new TextChange(new TextSpan(3, 5), "Universe")
            };

            Assert.Throws<ArgumentException>(() => text.WithChanges(changes));
        }

        [Fact]
        public void TestChangedTextWithMultipleUnorderedChanges()
        {
            var text = SourceText.From("Hello World");
            var changes = new[]
            {
                new TextChange(new TextSpan(6, 7), "Universe"),
                new TextChange(new TextSpan(0, 5), "Halo")
            };

            Assert.Throws<ArgumentException>(() => text.WithChanges(changes));
        }

        [Fact]
        public void TestChangedTextWithMultipleConsecutiveInsertsSamePosition()
        {
            var text = SourceText.From("Hello World");

            var newText = text.WithChanges(
                new TextChange(new TextSpan(6, 0), "Super "),
                new TextChange(new TextSpan(6, 0), "Spectacular "));

            Assert.Equal("Hello Super Spectacular World", newText.ToString());
        }

        [Fact]
        public void TestChangedTextWithReplaceAfterInsertSamePosition()
        {
            var text = SourceText.From("Hello World");

            var newText = text.WithChanges(
                new TextChange(new TextSpan(6, 0), "Super "),
                new TextChange(new TextSpan(6, 2), "Vu"));

            Assert.Equal("Hello Super Vurld", newText.ToString());
        }

        [Fact]
        public void TestChangedTextWithReplaceBeforeInsertSamePosition()
        {
            var text = SourceText.From("Hello World");
            var changes = new[]
            {
                new TextChange(new TextSpan(6, 2), "Vu"),
                new TextChange(new TextSpan(6, 0), "Super ")
            };

            // this causes overlap
            Assert.Throws<ArgumentException>(() => text.WithChanges(changes));
        }

        [Fact]
        public void TestChangedTextWithDeleteAfterDeleteAdjacent()
        {
            var text = SourceText.From("Hello World");

            var newText = text.WithChanges(
                new TextChange(new TextSpan(4, 1), string.Empty),
                new TextChange(new TextSpan(5, 1), string.Empty));

            Assert.Equal("HellWorld", newText.ToString());
        }

        [Fact]
        public void TestSubTextAfterMultipleChanges()
        {
            var text = SourceText.From("Hello World", Encoding.UTF7, SourceHashAlgorithm.Sha256);
            var newText = text.WithChanges(
                new TextChange(new TextSpan(4, 1), string.Empty),
                new TextChange(new TextSpan(6, 5), "Universe"));

            var subText = newText.GetSubText(new TextSpan(3, 4));
            Assert.Equal("l Un", subText.ToString());

            Assert.Equal(SourceHashAlgorithm.Sha256, subText.ChecksumAlgorithm);
            Assert.Same(Encoding.UTF7, subText.Encoding);
        }

        [Fact]
        public void TestLinesInChangedText()
        {
            var text = SourceText.From("Hello World");
            var newText = text.WithChanges(
                new TextChange(new TextSpan(4, 1), string.Empty));

            Assert.Equal(1, newText.Lines.Count);
        }

        [Fact]
        public void TestCopyTo()
        {
            var text = SourceText.From("Hello World");
            var newText = text.WithChanges(
                new TextChange(new TextSpan(6, 5), "Universe"));

            var destination = new char[32];
            newText.CopyTo(0, destination, 0, 0);   //should copy nothing and not throw.
            Assert.Throws<ArgumentOutOfRangeException>(() => newText.CopyTo(-1, destination, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => newText.CopyTo(0, destination, -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => newText.CopyTo(0, destination, 0, -1));
            Assert.Throws<ArgumentNullException>(() => newText.CopyTo(0, null, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => newText.CopyTo(newText.Length - 1, destination, 0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => newText.CopyTo(0, destination, destination.Length - 1, 2));
        }

        [Fact]
        public void TestGetTextChangesToChangedText()
        {
            var text = SourceText.From(new string('.', 2048), Encoding.UTF7, SourceHashAlgorithm.Sha256); // start bigger than GetText() copy buffer
            var changes = new TextChange[] {
                new TextChange(new TextSpan(0, 1), "[1]"),
                new TextChange(new TextSpan(1, 1), "[2]"),
                new TextChange(new TextSpan(5, 0), "[3]"),
                new TextChange(new TextSpan(25, 2), "[4]")
            };

            var newText = text.WithChanges(changes);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
            Assert.Same(Encoding.UTF7, newText.Encoding);

            var result = newText.GetTextChanges(text).ToList();

            Assert.Equal(changes.Length, result.Count);
            for (int i = 0; i < changes.Length; i++)
            {
                var expected = changes[i];
                var actual = result[i];
                Assert.Equal(expected.Span, actual.Span);
                Assert.Equal(expected.NewText, actual.NewText);
            }
        }

        private sealed class TextLineEqualityComparer : IEqualityComparer<TextLine>
        {
            public bool Equals(TextLine x, TextLine y)
            {
                return x.Span == y.Span;
            }

            public int GetHashCode(TextLine obj)
            {
                return obj.Span.GetHashCode();
            }
        }

        private static void AssertChangedTextLinesHelper(string originalText, params TextChange[] changes)
        {
            var changedText = SourceText.From(originalText).WithChanges(changes);
            Assert.Equal(SourceText.From(changedText.ToString()).Lines, changedText.Lines, new TextLineEqualityComparer());
        }

        [Fact]
        public void TestOptimizedSourceTextLinesSimpleSubstitution()
        {
            AssertChangedTextLinesHelper("Line1\r\nLine2\r\nLine3",
                new TextChange(new TextSpan(8, 2), "IN"),
                new TextChange(new TextSpan(15, 2), "IN"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesSubstitutionWithLongerText()
        {
            AssertChangedTextLinesHelper("Line1\r\nLine2\r\nLine3",
                new TextChange(new TextSpan(8, 2), new string('a', 10)),
                new TextChange(new TextSpan(15, 2), new string('a', 10)));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesInsertCrLf()
        {
            AssertChangedTextLinesHelper("Line1\r\nLine2\r\nLine3",
                new TextChange(new TextSpan(8, 2), "\r\n"),
                new TextChange(new TextSpan(15, 2), "\r\n"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesSimpleCr()
        {
            AssertChangedTextLinesHelper("Line1\rLine2\rLine3",
                new TextChange(new TextSpan(6, 0), "aa\r"),
                new TextChange(new TextSpan(11, 0), "aa\r"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesSimpleLf()
        {
            AssertChangedTextLinesHelper("Line1\nLine2\nLine3",
                new TextChange(new TextSpan(6, 0), "aa\n"),
                new TextChange(new TextSpan(11, 0), "aa\n"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesRemoveCrLf()
        {
            AssertChangedTextLinesHelper("Line1\r\nLine2\r\nLine3",
                new TextChange(new TextSpan(4, 4), "aaaaaa"),
                new TextChange(new TextSpan(15, 4), "aaaaaa"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesBrakeCrLf()
        {
            AssertChangedTextLinesHelper("Test\r\nMessage",
                new TextChange(new TextSpan(5, 0), "aaaaaa"));
        }

        [Fact]
        public void TestOptimizedSourceTextLinesBrakeCrLfWithLfPrefixedAndCrSuffixed()
        {
            AssertChangedTextLinesHelper("Test\r\nMessage",
                new TextChange(new TextSpan(5, 0), "\naaaaaa\r"));
        }

        [Fact]
        public void TestOptimizedSourceTextLineInsertAtEnd()
        {
            AssertChangedTextLinesHelper("Line1\r\nLine2\r\nLine3\r\n",
                new TextChange(new TextSpan(21, 0), "Line4\r\n"),
                new TextChange(new TextSpan(21, 0), "Line5\r\n"));
        }
    }
}
