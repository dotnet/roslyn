// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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

            var newText = text.WithChanges(changes);
            Assert.Equal("Halo Universe", newText.ToString());
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

            var newText = text.WithChanges(changes);
            Assert.Equal("Hello Super Vurld", newText.ToString());
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

        [Fact]
        public void TestManySingleCharacterAdds()
        {
            var str = new String('.', 1024);
            var text = SourceText.From(str);

            var lines = text.Lines;
            int n = 20000;
            var expected = str;
            for (int i = 0; i < n; i++)
            {
                char c = (char)(((ushort)'a') + (i % 26));
                text = text.Replace(50 + i, 0, c.ToString());
                expected = expected.Substring(0, 50 + i) + c + expected.Substring(50 + i);
            }

            Assert.Equal(str.Length + n, text.Length);
            Assert.Equal(expected, text.ToString());
        }

        [Fact]
        public void TestManySingleCharacterReplacements()
        {
            var str = new String('.', 1024);
            var text = SourceText.From(str);

            var lines = text.Lines;
            var expected = str;
            for (int i = 0; i < str.Length; i++)
            {
                char c = (char)(((ushort)'a') + (i % 26));

                text = text.Replace(i, 1, c.ToString());
                expected = expected.Substring(0, i) + c + str.Substring(i + 1);
            }

            Assert.Equal(str.Length, text.Length);
            Assert.Equal(expected, text.ToString());
        }

        [Fact]
        public void TestSubTextCausesSizeLengthDifference()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            Assert.Equal(26, text.Length);
            Assert.Equal(26, text.StorageSize);

            var subtext = text.GetSubText(new TextSpan(5, 10));
            Assert.Equal(10, subtext.Length);
            Assert.Equal("fghijklmno", subtext.ToString());
            Assert.Equal(26, subtext.StorageSize);
        }

        [Fact]
        public void TestRemovingMajorityOfTextCompressesStorage()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            var newText = text.Replace(new TextSpan(0, 20), "");

            Assert.Equal(6, newText.Length);
            Assert.Equal(6, newText.StorageSize);
        }

        [Fact]
        public void TestRemovingMinorityOfTextDoesNotCompressesStorage()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            var newText = text.Replace(new TextSpan(10, 6), "");

            Assert.Equal(20, newText.Length);
            Assert.Equal(26, newText.StorageSize);
        }

        [Fact]
        public void TestRemovingTextCreatesSegments()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            Assert.Equal(0, text.Segments.Length);
            var newText = text.Replace(new TextSpan(10, 1), "");

            Assert.Equal(25, newText.Length);
            Assert.Equal(26, newText.StorageSize);

            Assert.Equal(2, newText.Segments.Length);
            Assert.Equal("abcdefghij", newText.Segments[0].ToString());
            Assert.Equal("lmnopqrstuvwxyz", newText.Segments[1].ToString());
        }

        [Fact]
        public void TestAddingTextCreatesSegments()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            Assert.Equal(0, text.Segments.Length);
            var textWithSegments = text.Replace(new TextSpan(10, 0), "*");

            Assert.Equal(27, textWithSegments.Length);
            Assert.Equal("abcdefghij*klmnopqrstuvwxyz", textWithSegments.ToString());

            Assert.Equal(3, textWithSegments.Segments.Length);
            Assert.Equal("abcdefghij", textWithSegments.Segments[0].ToString());
            Assert.Equal("*", textWithSegments.Segments[1].ToString());
            Assert.Equal("klmnopqrstuvwxyz", textWithSegments.Segments[2].ToString());
        }

        [Fact]
        public void TestRemovingAcrossExistingSegmentsRemovesSegments()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            Assert.Equal(0, text.Segments.Length);
            var textWithSegments = text.Replace(new TextSpan(10, 0), "*");
            Assert.Equal(27, textWithSegments.Length);
            Assert.Equal(27, textWithSegments.StorageSize);

            var textWithFewerSegments = textWithSegments.Replace(new TextSpan(9, 3), "");
            Assert.Equal("abcdefghilmnopqrstuvwxyz", textWithFewerSegments.ToString());
            Assert.Equal(24, textWithFewerSegments.Length);
            Assert.Equal(26, textWithFewerSegments.StorageSize);

            Assert.Equal(2, textWithFewerSegments.Segments.Length);
            Assert.Equal("abcdefghi", textWithFewerSegments.Segments[0].ToString());
            Assert.Equal("lmnopqrstuvwxyz", textWithFewerSegments.Segments[1].ToString());
        }

        [Fact]
        public void TestRemovingEverythingSucceeds()
        {
            var text = SourceText.From("abcdefghijklmnopqrstuvwxyz");

            Assert.Equal(0, text.Segments.Length);
            var textWithSegments = text.Replace(new TextSpan(0, text.Length), "");
            Assert.Equal(0, textWithSegments.Length);
            Assert.Equal(0, textWithSegments.StorageSize);
        }

        [Fact]
        public void TestCompressingSegmentsCompressesSmallerSegmentsFirst()
        {
            var a = new string('a', 64);
            var b = new string('b', 64);

            var t = SourceText.From(a);
            t = t.Replace(t.Length, 0, b); // add b's

            var segs = t.Segments.Length;
            Assert.Equal(2, segs);
            Assert.Equal(a, t.Segments[0].ToString());
            Assert.Equal(b, t.Segments[1].ToString());

            // keep appending little segments until we trigger compression
            do
            {
                segs = t.Segments.Length;
                t = t.Replace(t.Length, 0, "c");
            }
            while (t.Segments.Length > segs);

            // this should compact all the 'c' segments into one
            Assert.Equal(3, t.Segments.Length);
            Assert.Equal(a, t.Segments[0].ToString());
            Assert.Equal(b, t.Segments[1].ToString());
            Assert.Equal(new string('c', t.Segments[2].Length), t.Segments[2].ToString());
        }

        [Fact]
        public void TestCompressingSegmentsCompressesLargerSegmentsIfNecessary()
        {
            var a = new string('a', 64);
            var b = new string('b', 64);
            var c = new string('c', 64);

            var t = SourceText.From(a);
            t = t.Replace(t.Length, 0, b); // add b's

            var segs = t.Segments.Length;
            Assert.Equal(2, segs);
            Assert.Equal(a, t.Segments[0].ToString());
            Assert.Equal(b, t.Segments[1].ToString());

            // keep appending larger segments (larger than initial size)
            do
            {
                segs = t.Segments.Length;
                t = t.Replace(t.Length, 0, c);  // add c's that are the same segment size as the a's and b's
            }
            while (t.Segments.Length > segs);

            // this should compact all the segments since they all were the same size and 
            // compress at the same time
            Assert.Equal(0, t.Segments.Length);
        }

        [Fact]
        public void TestOldEditsCanBeCollected()
        {
            // this test proves that intermediate edits are not being kept alive by successive edits.
            WeakReference weakFirstEdit;
            SourceText secondEdit;
            CreateEdits(out weakFirstEdit, out secondEdit);

            int tries = 0;
            while (weakFirstEdit.IsAlive)
            {
                tries++;
                if (tries > 10)
                {
                    throw new InvalidOperationException("Failed to GC old edit");
                }

                GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            }
        }

        private void CreateEdits(out WeakReference weakFirstEdit, out SourceText secondEdit)
        {
            var text = SourceText.From("This is the old text");
            var firstEdit = text.Replace(11, 3, "new");
            secondEdit = firstEdit.Replace(11, 3, "newer");

            weakFirstEdit = new WeakReference(firstEdit);
        }

        [Fact]
        public void TestLargeTextWriterReusesLargeChunks()
        {
            var chunk1 = "this is the large text".ToArray();
            var largeText = CreateLargeText(chunk1);

            // chunks are considered large because they are bigger than the expected size
            var writer = new LargeTextWriter(largeText.Encoding, largeText.ChecksumAlgorithm, 10);
            largeText.Write(writer);

            var newText = (LargeText)writer.ToSourceText();
            Assert.NotSame(largeText, newText);

            Assert.Equal(1, GetChunks(newText).Length);
            Assert.Same(chunk1, GetChunks(newText)[0]);
        }

        private SourceText CreateLargeText(params char[][] chunks)
        {
            return new LargeText(ImmutableArray.Create(chunks), Encoding.UTF8, default(ImmutableArray<byte>), SourceHashAlgorithm.Sha256, default(ImmutableArray<byte>));
        }

        private ImmutableArray<char[]> GetChunks(SourceText text)
        {
            var largeText = text as LargeText;
            if (largeText != null)
            {
                var chunkField = text.GetType().GetField("_chunks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (ImmutableArray<char[]>)chunkField.GetValue(text);
            }
            else
            {
                return ImmutableArray<char[]>.Empty;
            }
        }

        [Fact]
        public void TestLargeTextWriterDoesNotReuseSmallChunks()
        {
            var text = SourceText.From("small preamble");
            var chunk1 = "this is the large text".ToArray();
            var largeText = CreateLargeText(chunk1);

            // chunks are considered small because they fit within the buffer (which is the expected length for this test)
            var writer = new LargeTextWriter(largeText.Encoding, largeText.ChecksumAlgorithm, chunk1.Length * 4);

            // write preamble so buffer is allocated and has contents.
            text.Write(writer);

            // large text fits within the remaining buffer
            largeText.Write(writer);

            var newText = (LargeText)writer.ToSourceText();
            Assert.NotSame(largeText, newText);
            Assert.Equal(text.Length + largeText.Length, newText.Length);

            Assert.Equal(1, GetChunks(newText).Length);
            Assert.NotSame(chunk1, GetChunks(newText)[0]);
        }

        [Fact]
        [WorkItem(10452, "https://github.com/dotnet/roslyn/issues/10452")]
        public void TestEmptyChangeAfterChange()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(5, 6), string.Empty)); // prepare a ChangedText instance
            var change2 = change1.WithChanges(); // this should not cause exception

            Assert.Same(change1, change2); // this was a no-op and returned the same instance
        }

        [Fact]
        [WorkItem(10452, "https://github.com/dotnet/roslyn/issues/10452")]
        public void TestEmptyChangeAfterChange2()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(5, 6), string.Empty)); // prepare a ChangedText instance
            var change2 = change1.WithChanges(new TextChange(new TextSpan(2, 0), string.Empty)); // this should not cause exception

            Assert.Same(change1, change2); // this was a no-op and returned the same instance
        }

        [Fact]
        public void TestMergeChanges_Overlapping_NewInsideOld()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(6, 0), "Cruel "));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(7, 3), "oo"));
            Assert.Equal("Hello Cool World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(6, 0), changes[0].Span);
            Assert.Equal("Cool ", changes[0].NewText);
        }

        [Fact]
        public void TestMergeChanges_Overlapping_NewInsideOld_AndOldHasDeletion()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 3), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(2, 0), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa4", change1.ToString());
            Assert.Equal("0abba4", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 3), "abba") }, changes);
        }

        [Fact]
        public void TestMergeChanges_Overlapping_NewInsideOld_AndBothHaveDeletion_NewDeletionSmallerThanOld()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 3), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(2, 1), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa4", change1.ToString());
            Assert.Equal("0abb4", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 3), "abb") }, changes);
        }

        [Fact]
        public void TestMergeChanges_Overlapping_OldInsideNew()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(6, 0), "Cruel "));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(2, 14), "ar"));
            Assert.Equal("Heard", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(2, 8), changes[0].Span);
            Assert.Equal("ar", changes[0].NewText);
        }

        [Fact]
        public void TestMergeChanges_Overlapping_NewBeforeOld()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(6, 0), "Cruel "));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(4, 6), " Bel"));
            Assert.Equal("Hell Bell World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(4, 2), changes[0].Span);
            Assert.Equal(" Bell ", changes[0].NewText);
        }

        [Fact]
        public void TestMergeChanges_Overlapping_OldBeforeNew()
        {
            var original = SourceText.From("Hello World");
            var change1 = original.WithChanges(new TextChange(new TextSpan(6, 0), "Cruel "));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(7, 6), "wazy V"));
            Assert.Equal("Hello Cwazy Vorld", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(6, 1), changes[0].Span);
            Assert.Equal("Cwazy V", changes[0].NewText);
        }

        [Fact]
        public void TestMergeChanges_SameStart()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 0), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 0), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa1234", change1.ToString());
            Assert.Equal("0bbaa1234", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 0), "bbaa") }, changes);
        }

        [Fact]
        public void TestMergeChanges_SameStart_AndOldHasDeletion()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 3), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 0), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa4", change1.ToString());
            Assert.Equal("0bbaa4", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 3), "bbaa") }, changes);
        }

        [Fact]
        public void TestMergeChanges_SameStart_AndNewHasDeletion_SmallerThanOldInsertion()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 0), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 1), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa1234", change1.ToString());
            Assert.Equal("0bba1234", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 0), "bba") }, changes);
        }

        [Fact]
        public void TestMergeChanges_SameStart_AndNewHasDeletion_EqualToOldInsertion()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 0), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 2), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa1234", change1.ToString());
            Assert.Equal("0bb1234", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 0), "bb") }, changes);
        }

        [Fact]
        public void TestMergeChanges_SameStart_AndNewHasDeletion_LargerThanOldInsertion()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 0), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 3), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa1234", change1.ToString());
            Assert.Equal("0bb234", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 1), "bb") }, changes);
        }

        [Fact]
        public void TestMergeChanges_SameStart_AndBothHaveDeletion_NewDeletionSmallerThanOld()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 3), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 1), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa4", change1.ToString());
            Assert.Equal("0bba4", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 3), "bba") }, changes);
        }

        [Fact]
        public void TestMergeChanges_AfterAdjacent()
        {
            var original = SourceText.From("Hell");
            var change1 = original.WithChanges(new TextChange(new TextSpan(4, 0), "o "));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(6, 0), "World"));
            Assert.Equal("Hello World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(4, 0), changes[0].Span);
            Assert.Equal("o World", changes[0].NewText);
        }

        [Fact]
        public void TestMergeChanges_AfterSeparated()
        {
            var original = SourceText.From("Hell ");
            var change1 = original.WithChanges(new TextChange(new TextSpan(4, 0), "o"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(6, 0), "World"));
            Assert.Equal("Hello World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(2, changes.Count);
            Assert.Equal(new TextSpan(4, 0), changes[0].Span);
            Assert.Equal("o", changes[0].NewText);
            Assert.Equal(new TextSpan(5, 0), changes[1].Span);
            Assert.Equal("World", changes[1].NewText);
        }

        [Fact]
        public void TestMergeChanges_BeforeSeparated()
        {
            var original = SourceText.From("Hell Word");
            var change1 = original.WithChanges(new TextChange(new TextSpan(8, 0), "l"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(4, 0), "o"));
            Assert.Equal("Hello World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(2, changes.Count);
            Assert.Equal(new TextSpan(4, 0), changes[0].Span);
            Assert.Equal("o", changes[0].NewText);
            Assert.Equal(new TextSpan(8, 0), changes[1].Span);
            Assert.Equal("l", changes[1].NewText);
        }

        [Fact]
        public void TestMergeChanges_BeforeAdjacent()
        {
            var original = SourceText.From("Hell");
            var change1 = original.WithChanges(new TextChange(new TextSpan(4, 0), " World"));
            Assert.Equal("Hell World", change1.ToString());
            var change2 = change1.WithChanges(new TextChange(new TextSpan(4, 0), "o"));
            Assert.Equal("Hello World", change2.ToString());

            var changes = change2.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(4, 0), changes[0].Span);
            Assert.Equal("o World", changes[0].NewText);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10961")]
        public void TestMergeChanges_NoMiddleMan()
        {
            var original = SourceText.From("Hell");

            var final = GetChangesWithoutMiddle(
                original,
                c => c.WithChanges(new TextChange(new TextSpan(4, 0), "o ")),
                c => c.WithChanges(new TextChange(new TextSpan(6, 0), "World")));

            Assert.Equal("Hello World", final.ToString());

            var changes = final.GetTextChanges(original);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(4, 0), changes[0].Span);
            Assert.Equal("o World", changes[0].NewText);
        }

        private SourceText GetChangesWithoutMiddle(
            SourceText original,
            Func<SourceText, SourceText> fnChange1,
            Func<SourceText, SourceText> fnChange2)
        {
            WeakReference change1;
            SourceText change2;
            GetChangesWithoutMiddle_Helper(original, fnChange1, fnChange2, out change1, out change2);

            while (change1.IsAlive)
            {
                GC.Collect(2);
                GC.WaitForFullGCComplete();
            }

            return change2;
        }

        private void GetChangesWithoutMiddle_Helper(
            SourceText original,
            Func<SourceText, SourceText> fnChange1,
            Func<SourceText, SourceText> fnChange2,
            out WeakReference change1,
            out SourceText change2)
        {
            var c1 = fnChange1(original);
            change1 = new WeakReference(c1);
            change2 = fnChange2(c1);
        }
    }
}
