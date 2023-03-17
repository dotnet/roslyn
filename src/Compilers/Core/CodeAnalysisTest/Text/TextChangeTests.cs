// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TextChangeTests
    {
        private readonly ITestOutputHelper _output;

        public TextChangeTests(ITestOutputHelper output)
        {
            _output = output;
        }

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
                new TextChange(new TextSpan(6, 5), "Universe"),
                new TextChange(new TextSpan(0, 5), "Halo")
            };

            var newText = text.WithChanges(changes);
            Assert.Equal("Halo Universe", newText.ToString());
        }

        [Fact]
        public void TestChangedTextWithMultipleUnorderedChangesAndOneIsOutOfBounds()
        {
            var text = SourceText.From("Hello World");
            var changes = new[]
            {
                new TextChange(new TextSpan(6, 7), "Universe"),
                new TextChange(new TextSpan(0, 5), "Halo")
            };
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                var newText = text.WithChanges(changes);
            });
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
            var text = SourceText.From("Hello World", Encoding.Unicode, SourceHashAlgorithms.Default);
            var newText = text.WithChanges(
                new TextChange(new TextSpan(4, 1), string.Empty),
                new TextChange(new TextSpan(6, 5), "Universe"));

            var subText = newText.GetSubText(new TextSpan(3, 4));
            Assert.Equal("l Un", subText.ToString());

            Assert.Equal(SourceHashAlgorithms.Default, subText.ChecksumAlgorithm);
            Assert.Same(Encoding.Unicode, subText.Encoding);
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
            var text = SourceText.From(new string('.', 2048), Encoding.Unicode, SourceHashAlgorithms.Default); // start bigger than GetText() copy buffer
            var changes = new TextChange[] {
                new TextChange(new TextSpan(0, 1), "[1]"),
                new TextChange(new TextSpan(1, 1), "[2]"),
                new TextChange(new TextSpan(5, 0), "[3]"),
                new TextChange(new TextSpan(25, 2), "[4]")
            };

            var newText = text.WithChanges(changes);
            Assert.Equal(SourceHashAlgorithms.Default, newText.ChecksumAlgorithm);
            Assert.Same(Encoding.Unicode, newText.Encoding);

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
            return new LargeText(ImmutableArray.Create(chunks), Encoding.UTF8, default(ImmutableArray<byte>), SourceHashAlgorithms.Default, default(ImmutableArray<byte>));
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
        [WorkItem(22289, "https://github.com/dotnet/roslyn/issues/22289")]
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
        [WorkItem(22289, "https://github.com/dotnet/roslyn/issues/22289")]
        public void TestMergeChanges_Overlapping_NewInsideOld_AndOldHasLeadingDeletion_SmallerThanLeadingInsertion()
        {
            var original = SourceText.From("012");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 1), "aaa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(3, 0), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aaa2", change1.ToString());
            Assert.Equal("0aabba2", change2.ToString());
            Assert.Equal(new[] { new TextChange(new TextSpan(1, 1), "aabba") }, changes);
        }

        [Fact]
        [WorkItem(22289, "https://github.com/dotnet/roslyn/issues/22289")]
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
        [WorkItem(22289, "https://github.com/dotnet/roslyn/issues/22289")]
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
        [WorkItem(39405, "https://github.com/dotnet/roslyn/issues/39405")]
        public void TestMergeChanges_NewDeletionLargerThanOld()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(1, 3), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(1, 3), "bb"));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("0aa4", change1.ToString());
            Assert.Equal("0bb", change2.ToString());
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

        [Fact]
        public void TestMergeChanges_IntegrationTestCase1()
        {
            var oldChanges = ImmutableArray.Create(
                new TextChangeRange(new TextSpan(919, 10), 466),
                new TextChangeRange(new TextSpan(936, 33), 29),
                new TextChangeRange(new TextSpan(1098, 0), 70),
                new TextChangeRange(new TextSpan(1125, 4), 34),
                new TextChangeRange(new TextSpan(1138, 0), 47));
            var newChanges = ImmutableArray.Create(
                new TextChangeRange(new TextSpan(997, 0), 2),
                new TextChangeRange(new TextSpan(1414, 0), 2),
                new TextChangeRange(new TextSpan(1419, 0), 2),
                new TextChangeRange(new TextSpan(1671, 5), 5),
                new TextChangeRange(new TextSpan(1681, 0), 4));

            var merged = ChangedText.TestAccessor.Merge(oldChanges, newChanges);

            var expected = ImmutableArray.Create(
                new TextChangeRange(new TextSpan(919, 10), 468),
                new TextChangeRange(new TextSpan(936, 33), 33),
                new TextChangeRange(new TextSpan(1098, 0), 70),
                new TextChangeRange(new TextSpan(1125, 4), 38),
                new TextChangeRange(new TextSpan(1138, 0), 47));
            Assert.Equal<TextChangeRange>(expected, merged);
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void DebuggerDisplay()
        {
            Assert.Equal("new TextChange(new TextSpan(0, 0), null)", default(TextChange).GetDebuggerDisplay());
            Assert.Equal("new TextChange(new TextSpan(0, 1), \"abc\")", new TextChange(new TextSpan(0, 1), "abc").GetDebuggerDisplay());
            Assert.Equal("new TextChange(new TextSpan(0, 1), (NewLength = 10))", new TextChange(new TextSpan(0, 1), "0123456789").GetDebuggerDisplay());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz()
        {
            var random = new Random();

            // Adjust upper bound as needed to generate a simpler reproducer for an error scenario
            var originalText = SourceText.From(string.Join("", Enumerable.Range(0, random.Next(10))));

            for (var iteration = 0; iteration < 100000; iteration++)
            {
                var editedLength = originalText.Length;
                ArrayBuilder<TextChange> oldChangesBuilder = ArrayBuilder<TextChange>.GetInstance();

                // Adjust as needed to get a simpler error reproducer.
                var oldMaxInsertLength = originalText.Length * 2;
                const int maxSkipLength = 2;
                // generate sequence of "old edits" which meet invariants
                for (int i = 0; i < originalText.Length; i += random.Next(maxSkipLength))
                {
                    var newText = string.Join("", Enumerable.Repeat('a', random.Next(oldMaxInsertLength)));
                    var newChange = new TextChange(new TextSpan(i, length: random.Next(originalText.Length - i)), newText);
                    i = newChange.Span.End;

                    editedLength = editedLength - newChange.Span.Length + newChange.NewText.Length;
                    oldChangesBuilder.Add(newChange);

                    // Adjust as needed to generate a simpler reproducer for an error scenario
                    if (oldChangesBuilder.Count == 5) break;
                }

                var change1 = originalText.WithChanges(oldChangesBuilder);

                ArrayBuilder<TextChange> newChangesBuilder = ArrayBuilder<TextChange>.GetInstance();

                // Adjust as needed to get a simpler error reproducer.
                var newMaxInsertLength = editedLength * 2;
                // generate sequence of "new edits" which meet invariants
                for (int i = 0; i < editedLength; i += random.Next(maxSkipLength))
                {
                    var newText = string.Join("", Enumerable.Repeat('b', random.Next(newMaxInsertLength)));
                    var newChange = new TextChange(new TextSpan(i, length: random.Next(editedLength - i)), newText);
                    i = newChange.Span.End;

                    newChangesBuilder.Add(newChange);

                    // Adjust as needed to generate a simpler reproducer for an error scenario
                    if (newChangesBuilder.Count == 5) break;
                }

                var change2 = change1.WithChanges(newChangesBuilder);
                try
                {
                    var textChanges = change2.GetTextChanges(originalText);
                    Assert.Equal(originalText.WithChanges(textChanges).ToString(), change2.ToString());
                }
                catch
                {
                    _output.WriteLine($@"
    [Fact]
    public void Fuzz_{iteration}()
    {{
        var originalText = SourceText.From(""{originalText}"");
        var change1 = originalText.WithChanges({string.Join(", ", oldChangesBuilder.Select(c => c.GetDebuggerDisplay()))});
        var change2 = change1.WithChanges({string.Join(", ", newChangesBuilder.Select(c => c.GetDebuggerDisplay()))});
        Assert.Equal(""{change1}"", change1.ToString()); // double-check for correctness
        Assert.Equal(""{change2}"", change2.ToString()); // double-check for correctness

        var changes = change2.GetTextChanges(originalText);
        Assert.Equal(""{change2}"", originalText.WithChanges(changes).ToString());
    }}
");
                    throw;
                }
                finally
                {
                    // we delay freeing so that if we need to debug the fuzzer
                    // it's easier to see what changes were introduced at each stage.
                    oldChangesBuilder.Free();
                    newChangesBuilder.Free();
                }
            }
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_0()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 2), "a"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 2), "bb"));
            Assert.Equal("a234", change1.ToString());
            Assert.Equal("bb34", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("bb34", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_1()
        {
            var original = SourceText.From("01234");
            var change1 = original.WithChanges(new TextChange(new TextSpan(0, 0), "aa"), new TextChange(new TextSpan(1, 1), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 1), "b"), new TextChange(new TextSpan(2, 2), ""));

            var changes = change2.GetTextChanges(original);
            Assert.Equal("aa0aa234", change1.ToString());
            Assert.Equal("baa234", change2.ToString());
            Assert.Equal(change2.ToString(), original.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_2()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 0), "a"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 2), ""), new TextChange(new TextSpan(2, 0), "bb"));
            Assert.Equal("a01234", change1.ToString());
            Assert.Equal("bb1234", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("bb1234", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_3()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 1), "aa"), new TextChange(new TextSpan(3, 1), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 0), "bbb"));
            Assert.Equal("aa12aa4", change1.ToString());
            Assert.Equal("bbbaa12aa4", change2.ToString());
            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("bbbaa12aa4", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_4()
        {
            var originalText = SourceText.From("012345");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 3), "a"), new TextChange(new TextSpan(5, 0), "aaa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 2), ""), new TextChange(new TextSpan(3, 1), "bb"));
            Assert.Equal("a34aaa5", change1.ToString());
            Assert.Equal("4bbaa5", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("4bbaa5", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_7()
        {
            var originalText = SourceText.From("01234567");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 1), "aaaaa"), new TextChange(new TextSpan(3, 1), "aaaa"), new TextChange(new TextSpan(6, 1), "aaaaa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 0), "b"), new TextChange(new TextSpan(2, 0), "b"), new TextChange(new TextSpan(3, 4), "bbbbb"), new TextChange(new TextSpan(9, 5), "bbbbb"), new TextChange(new TextSpan(15, 3), ""));
            Assert.Equal("aaaaa12aaaa45aaaaa7", change1.ToString());
            Assert.Equal("baababbbbbaabbbbba7", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("baababbbbbaabbbbba7", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_10()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 1), "a"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 1), "b"), new TextChange(new TextSpan(2, 2), "b"));
            Assert.Equal("a1234", change1.ToString());
            Assert.Equal("b1b4", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("b1b4", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_23()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 1), "aa"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 0), "b"), new TextChange(new TextSpan(1, 2), "b"));
            Assert.Equal("aa1234", change1.ToString());
            Assert.Equal("bab234", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("bab234", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_32()
        {
            var originalText = SourceText.From("012345");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 2), "a"), new TextChange(new TextSpan(3, 2), "a"));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 3), "bbb"));
            Assert.Equal("a2a5", change1.ToString());
            Assert.Equal("bbb5", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("bbb5", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_39()
        {
            var originalText = SourceText.From("0123456");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 4), ""), new TextChange(new TextSpan(5, 1), ""));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 1), ""), new TextChange(new TextSpan(1, 0), ""));
            Assert.Equal("46", change1.ToString());
            Assert.Equal("6", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("6", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_55()
        {
            var originalText = SourceText.From("012345");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 2), ""), new TextChange(new TextSpan(3, 1), ""), new TextChange(new TextSpan(4, 0), ""), new TextChange(new TextSpan(4, 0), ""), new TextChange(new TextSpan(4, 0), ""));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 1), ""), new TextChange(new TextSpan(1, 1), ""), new TextChange(new TextSpan(2, 0), ""));
            Assert.Equal("245", change1.ToString());
            Assert.Equal("5", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("5", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(47234, "https://github.com/dotnet/roslyn/issues/47234")]
        public void Fuzz_110()
        {
            var originalText = SourceText.From("01234");
            var change1 = originalText.WithChanges(new TextChange(new TextSpan(0, 1), ""), new TextChange(new TextSpan(2, 1), ""));
            var change2 = change1.WithChanges(new TextChange(new TextSpan(0, 0), ""), new TextChange(new TextSpan(1, 1), ""));
            Assert.Equal("134", change1.ToString());
            Assert.Equal("14", change2.ToString());

            var changes = change2.GetTextChanges(originalText);
            Assert.Equal("14", originalText.WithChanges(changes).ToString());
        }

        [Fact]
        [WorkItem(41413, "https://github.com/dotnet/roslyn/issues/41413")]
        public void GetTextChanges_NonOverlappingSpans()
        {
            var content = @"@functions{
    public class Foo
    {
void Method()
{
    
}
    }
}";

            var text = SourceText.From(content);
            var edits1 = new TextChange[]
            {
                new TextChange(new TextSpan(39, 0), "    "),
                new TextChange(new TextSpan(42, 0), "            "),
                new TextChange(new TextSpan(57, 0), "            "),
                new TextChange(new TextSpan(58, 0), "\r\n"),
                new TextChange(new TextSpan(64, 2), "        "),
                new TextChange(new TextSpan(69, 0), "    "),
            };
            var changedText = text.WithChanges(edits1);

            var edits2 = new TextChange[]
            {
                new TextChange(new TextSpan(35, 4), string.Empty),
                new TextChange(new TextSpan(46, 4), string.Empty),
                new TextChange(new TextSpan(73, 4), string.Empty),
                new TextChange(new TextSpan(88, 0), "    "),
                new TextChange(new TextSpan(90, 4), string.Empty),
                new TextChange(new TextSpan(105, 4), string.Empty),
            };
            var changedText2 = changedText.WithChanges(edits2);

            var changes = changedText2.GetTextChanges(text);

            var position = 0;
            foreach (var change in changes)
            {
                Assert.True(position <= change.Span.Start);
                position = change.Span.End;
            }
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
