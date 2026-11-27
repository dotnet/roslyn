// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text;

/// <summary>
/// Exhaustive tests for <see cref="TextLine"/> properties across all <see cref="SourceText"/>
/// implementations and all supported line break sequences.
/// </summary>
public sealed class TextLineNewLineTests : TestBase
{
    /// <summary>
    /// The four SourceText implementation strategies under test.
    /// </summary>
    public enum TextKind { String, Large, Sub, Composite }

    private static readonly TextKind[] s_allTextKinds =
        [TextKind.String, TextKind.Large, TextKind.Sub, TextKind.Composite];

    private static readonly (string NewLine, int NewLineLength)[] s_newlines =
    [
        ("\n", 1), ("\r", 1), ("\r\n", 2),
        ("\u0085", 1), ("\u2028", 1), ("\u2029", 1),
    ];

    #region Theory data generators

    public static IEnumerable<object[]> AllNewlines
    {
        get
        {
            foreach (var (newLine, newLineLength) in s_newlines)
                yield return [newLine, newLineLength];
        }
    }

    public static IEnumerable<object[]> AllTextKinds
    {
        get
        {
            foreach (var kind in s_allTextKinds)
                yield return [kind];
        }
    }

    /// <summary>Cross-product of all newlines × all text kinds.</summary>
    public static IEnumerable<object[]> AllNewlinesAndKinds
    {
        get
        {
            foreach (var (newLine, newLineLength) in s_newlines)
            {
                foreach (var kind in s_allTextKinds)
                    yield return [newLine, newLineLength, kind];
            }
        }
    }

    /// <summary>Cross-product of all newline pairs × all text kinds.</summary>
    public static IEnumerable<object[]> AllNewlinePairsAndKinds
    {
        get
        {
            foreach (var (newLine1, newLineLength1) in s_newlines)
            {
                foreach (var (newLine2, newLineLength2) in s_newlines)
                {
                    foreach (var kind in s_allTextKinds)
                        yield return [newLine1, newLineLength1, newLine2, newLineLength2, kind];
                }
            }
        }
    }

    /// <summary>
    /// Content patterns that exercise all interesting newline scenarios, used for cross-type
    /// consistency testing.
    /// </summary>
    public static IEnumerable<object[]> InterestingContents =>
    [
        [""],
        ["hello"],
        ["abc\ndef"],
        ["abc\rdef"],
        ["abc\r\ndef"],
        ["abc\u0085def"],
        ["abc\u2028def"],
        ["abc\u2029def"],
        ["\n"],
        ["\r"],
        ["\r\n"],
        ["\u0085"],
        ["\u2028"],
        ["\u2029"],
        ["\n\n\n"],
        ["\r\r\r"],
        ["\r\n\r\n\r\n"],
        ["abc\n\rdef"],
        ["a\r\r\nb"],
        ["a\r\n\rb"],
        ["a\nb\rc\r\nd\u0085e\u2028f\u2029g"],
        ["abc\r\n"],
        ["\r\nabc"],
        ["a\r\n\r\nb"],
        ["\n\r\n\r\n\r"],
    ];

    /// <summary>Cross-product of interesting contents × all TextKind pairs.</summary>
    public static IEnumerable<object[]> InterestingContentsAndKindPairs
    {
        get
        {
            foreach (var contentData in InterestingContents)
            {
                foreach (var k1 in s_allTextKinds)
                {
                    foreach (var k2 in s_allTextKinds)
                        yield return [(string)contentData[0], k1, k2];
                }
            }
        }
    }

    #endregion

    #region Helpers — SourceText creation

    /// <summary>
    /// Creates a <see cref="SourceText"/> with the given <paramref name="content"/> using the
    /// implementation strategy indicated by <paramref name="kind"/>.
    /// </summary>
    private static SourceText CreateText(string content, TextKind kind) => kind switch
    {
        TextKind.String => SourceText.From(content),
        TextKind.Large => CreateLargeText(content),
        TextKind.Sub => CreateSubText(content),
        TextKind.Composite => CreateCompositeFromContent(content),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static SourceText CreateLargeText(string content)
    {
        if (content.Length == 0)
            return SourceText.From(content);
        return LargeText.Decode(new StringReader(content), content.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1);
    }

    /// <summary>
    /// Creates a SubText by embedding <paramref name="content"/> inside a larger text with
    /// non-newline padding, then extracting the inner portion.  This proves that line properties
    /// are not influenced by the surrounding text.
    /// </summary>
    private static SourceText CreateSubText(string content)
    {
        const string prefix = "PADDING_PREFIX_";
        const string suffix = "_SUFFIX_PADDING";
        var full = SourceText.From(prefix + content + suffix);
        return new SubText(full, new TextSpan(prefix.Length, content.Length));
    }

    /// <summary>
    /// Creates a CompositeText by splitting <paramref name="content"/> at the midpoint.
    /// For very short content where a meaningful split isn't possible, falls back to StringText.
    /// </summary>
    private static SourceText CreateCompositeFromContent(string content)
    {
        if (content.Length < 2)
            return SourceText.From(content);

        var mid = content.Length / 2;
        return CreateCompositeText(content[..mid], content[mid..]);
    }

    private static SourceText CreateLargeTextWithChunks(params string[] chunks)
    {
        var builder = ImmutableArray.CreateBuilder<char[]>(chunks.Length);
        foreach (var chunk in chunks)
            builder.Add(chunk.ToCharArray());
        return new LargeText(builder.MoveToImmutable(), Encoding.UTF8, SourceHashAlgorithm.Sha1);
    }

    private static SourceText CreateCompositeText(params string[] segments)
    {
        var builder = ArrayBuilder<SourceText>.GetInstance();
        foreach (var segment in segments)
            builder.Add(SourceText.From(segment));

        var reference = SourceText.From(string.Concat(segments));
        var result = CompositeText.ToSourceText(builder, reference, adjustSegments: false);
        builder.Free();
        return result;
    }

    #endregion

    #region Helpers — assertion

    private static void CheckLine(
        SourceText text, int lineNumber,
        int start, int length, int newLineLength,
        string lineText)
    {
        var line = text.Lines[lineNumber];
        Assert.Equal(start, line.Start);
        Assert.Equal(start + length, line.End);
        Assert.Equal(start + length + newLineLength, line.EndIncludingLineBreak);
        Assert.Equal(length, line.Span.Length);
        Assert.Equal(length + newLineLength, line.SpanIncludingLineBreak.Length);
        Assert.Equal(lineText, line.ToString());
    }

    /// <summary>
    /// Asserts that every line property of <paramref name="actual"/> matches the corresponding
    /// line in <paramref name="reference"/>, and that <c>Lines.IndexOf</c> agrees for every
    /// position in the text.
    /// </summary>
    private static void AssertLinesMatch(SourceText reference, SourceText actual)
    {
        Assert.Equal(reference.Length, actual.Length);
        Assert.Equal(reference.Lines.Count, actual.Lines.Count);

        for (var i = 0; i < reference.Lines.Count; i++)
        {
            var referenceLine = reference.Lines[i];
            var actualLine = actual.Lines[i];
            Assert.Equal(referenceLine.Start, actualLine.Start);
            Assert.Equal(referenceLine.End, actualLine.End);
            Assert.Equal(referenceLine.EndIncludingLineBreak, actualLine.EndIncludingLineBreak);
            Assert.Equal(referenceLine.Span, actualLine.Span);
            Assert.Equal(referenceLine.SpanIncludingLineBreak, actualLine.SpanIncludingLineBreak);
            Assert.Equal(referenceLine.ToString(), actualLine.ToString());
        }

        for (var pos = 0; pos < reference.Length; pos++)
            Assert.Equal(reference.Lines.IndexOf(pos), actual.Lines.IndexOf(pos));
    }

    #endregion

    // =========================================================================
    // Part 1: Ground truth — verify exact Start/End/EndIncludingLineBreak
    //         values.  Every test is parameterized over all TextKind variants
    //         so that StringText, LargeText, SubText, and CompositeText are
    //         all validated against the same expected values.
    // =========================================================================

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void NewlineInMiddle(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText("abc" + newLine + "def", kind);
        Assert.Equal(2, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 3, newLineLength: newLineLength, lineText: "abc");
        CheckLine(text, 1, start: 3 + newLineLength, length: 3, newLineLength: 0, lineText: "def");
    }

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void NewlineAtEnd(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText("abc" + newLine, kind);
        Assert.Equal(2, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 3, newLineLength: newLineLength, lineText: "abc");
        CheckLine(text, 1, start: 3 + newLineLength, length: 0, newLineLength: 0, lineText: "");
    }

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void NewlineAtStart(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText(newLine + "abc", kind);
        Assert.Equal(2, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 0, newLineLength: newLineLength, lineText: "");
        CheckLine(text, 1, start: newLineLength, length: 3, newLineLength: 0, lineText: "abc");
    }

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void OnlyNewline(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText(newLine, kind);
        Assert.Equal(2, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 0, newLineLength: newLineLength, lineText: "");
        CheckLine(text, 1, start: newLineLength, length: 0, newLineLength: 0, lineText: "");
    }

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void ConsecutiveSameNewlines(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText(newLine + newLine, kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 0, newLineLength: newLineLength, lineText: "");
        CheckLine(text, 1, start: newLineLength, length: 0, newLineLength: newLineLength, lineText: "");
        CheckLine(text, 2, start: 2 * newLineLength, length: 0, newLineLength: 0, lineText: "");
    }

    [Theory, MemberData(nameof(AllNewlinesAndKinds))]
    public void ThreeConsecutiveSameNewlines(string newLine, int newLineLength, TextKind kind)
    {
        var text = CreateText(newLine + newLine + newLine, kind);
        Assert.Equal(4, text.Lines.Count);
        for (var i = 0; i < 3; i++)
            CheckLine(text, i, start: i * newLineLength, length: 0, newLineLength: newLineLength, lineText: "");
        CheckLine(text, 3, start: 3 * newLineLength, length: 0, newLineLength: 0, lineText: "");
    }

    [Theory, MemberData(nameof(AllNewlinePairsAndKinds))]
    public void TwoDifferentNewlines(string newLine1, int newLine1Length, string newLine2, int newLine2Length, TextKind kind)
    {
        var text = CreateText("a" + newLine1 + "b" + newLine2 + "c", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 1, newLineLength: newLine1Length, lineText: "a");
        CheckLine(text, 1, start: 1 + newLine1Length, length: 1, newLineLength: newLine2Length, lineText: "b");
        CheckLine(text, 2, start: 1 + newLine1Length + 1 + newLine2Length, length: 1, newLineLength: 0, lineText: "c");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void Empty(TextKind kind)
    {
        var text = CreateText("", kind);
        Assert.Equal(1, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 0, newLineLength: 0, lineText: "");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void NoNewlines(TextKind kind)
    {
        var text = CreateText("hello world", kind);
        Assert.Equal(1, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 11, newLineLength: 0, lineText: "hello world");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void CRNotFollowedByLF(TextKind kind)
    {
        var text = CreateText("abc\rdef\nghi", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 3, newLineLength: 1, lineText: "abc");
        CheckLine(text, 1, start: 4, length: 3, newLineLength: 1, lineText: "def");
        CheckLine(text, 2, start: 8, length: 3, newLineLength: 0, lineText: "ghi");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void LFThenCR_AreSeparateBreaks(TextKind kind)
    {
        var text = CreateText("abc\n\rdef", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 3, newLineLength: 1, lineText: "abc");
        CheckLine(text, 1, start: 4, length: 0, newLineLength: 1, lineText: "");
        CheckLine(text, 2, start: 5, length: 3, newLineLength: 0, lineText: "def");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void AllSixNewlineTypes(TextKind kind)
    {
        var text = CreateText("a\nb\rc\r\nd\u0085e\u2028f\u2029g", kind);
        Assert.Equal(7, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 1, newLineLength: 1, lineText: "a");
        CheckLine(text, 1, start: 2, length: 1, newLineLength: 1, lineText: "b");
        CheckLine(text, 2, start: 4, length: 1, newLineLength: 2, lineText: "c");
        CheckLine(text, 3, start: 7, length: 1, newLineLength: 1, lineText: "d");
        CheckLine(text, 4, start: 9, length: 1, newLineLength: 1, lineText: "e");
        CheckLine(text, 5, start: 11, length: 1, newLineLength: 1, lineText: "f");
        CheckLine(text, 6, start: 13, length: 1, newLineLength: 0, lineText: "g");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void ConsecutiveCRLF(TextKind kind)
    {
        var text = CreateText("a\r\n\r\nb", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 1, newLineLength: 2, lineText: "a");
        CheckLine(text, 1, start: 3, length: 0, newLineLength: 2, lineText: "");
        CheckLine(text, 2, start: 5, length: 1, newLineLength: 0, lineText: "b");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void CRThenCRLF(TextKind kind)
    {
        var text = CreateText("a\r\r\nb", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 1, newLineLength: 1, lineText: "a");
        CheckLine(text, 1, start: 2, length: 0, newLineLength: 2, lineText: "");
        CheckLine(text, 2, start: 4, length: 1, newLineLength: 0, lineText: "b");
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void CRLFThenCR(TextKind kind)
    {
        var text = CreateText("a\r\n\rb", kind);
        Assert.Equal(3, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 1, newLineLength: 2, lineText: "a");
        CheckLine(text, 1, start: 3, length: 0, newLineLength: 1, lineText: "");
        CheckLine(text, 2, start: 4, length: 1, newLineLength: 0, lineText: "b");
    }

    // =========================================================================
    // Part 2: Cross-type consistency — verify that every pair of SourceText
    //         implementations produces identical line information for the same
    //         content.  This catches any disagreement between implementations
    //         even if the ground truth expectations above were wrong.
    // =========================================================================

    [Theory, MemberData(nameof(InterestingContentsAndKindPairs))]
    public void CrossType_SameContent_SameLines(string content, TextKind kind1, TextKind kind2)
    {
        var text1 = CreateText(content, kind1);
        var text2 = CreateText(content, kind2);
        AssertLinesMatch(text1, text2);
    }

    // =========================================================================
    // Part 3: Type-specific edge cases that exercise implementation details
    //         unique to each SourceText subclass.
    // =========================================================================

    // --- LargeText: chunk boundary tests ---

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void LargeText_ChunkBoundaryBeforeNewline(string newLine)
    {
        AssertLinesMatch(
            SourceText.From("abc" + newLine + "def"),
            CreateLargeTextWithChunks("abc", newLine + "def"));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\r\n")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void LargeText_ChunkBoundaryAfterNewline(string newLine)
    {
        AssertLinesMatch(
            SourceText.From("abc" + newLine + "def"),
            CreateLargeTextWithChunks("abc" + newLine, "def"));
    }

    [Fact]
    public void LargeText_ChunkBoundarySplitsCRLF()
    {
        var text = CreateLargeTextWithChunks("abc\r", "\ndef");
        AssertLinesMatch(SourceText.From("abc\r\ndef"), text);
        Assert.Equal(2, text.Lines.Count);
        CheckLine(text, 0, start: 0, length: 3, newLineLength: 2, lineText: "abc");
        CheckLine(text, 1, start: 5, length: 3, newLineLength: 0, lineText: "def");
    }

    [Fact]
    public void LargeText_ChunkBoundarySplitsCRLF_OnlyNewlines()
    {
        AssertLinesMatch(SourceText.From("\r\n"), CreateLargeTextWithChunks("\r", "\n"));
    }

    [Fact]
    public void LargeText_ChunkBoundaryCRNotFollowedByLF()
    {
        AssertLinesMatch(
            SourceText.From("abc\rdef"),
            CreateLargeTextWithChunks("abc\r", "def"));
    }

    [Fact]
    public void LargeText_ChunkBoundary_MultipleCRLFSplits()
    {
        AssertLinesMatch(
            SourceText.From("ab\r\ncd\r\nef"),
            CreateLargeTextWithChunks("ab\r", "\ncd\r", "\nef"));
    }

    [Fact]
    public void LargeText_ChunkBoundary_CRAtEndThenCRLFSplit()
    {
        AssertLinesMatch(
            SourceText.From("a\rb\r\nc"),
            CreateLargeTextWithChunks("a\rb\r", "\nc"));
    }

    [Theory]
    [InlineData("abc\ndef\rghi\r\njkl")]
    [InlineData("\r\n\r\n\r\n")]
    [InlineData("\r\r\r")]
    [InlineData("\n\n\n")]
    [InlineData("a\u0085b\u2028c\u2029d")]
    [InlineData("a\r\nb\r\nc\r\n")]
    [InlineData("\ra\n\rb\n\rc\n")]
    public void LargeText_EachCharIsOwnChunk(string content)
    {
        var chunks = new string[content.Length];
        for (var i = 0; i < content.Length; i++)
            chunks[i] = content[i].ToString();

        AssertLinesMatch(SourceText.From(content), CreateLargeTextWithChunks(chunks));
    }

    // --- SubText: CRLF splitting at SubText boundaries ---

    [Fact]
    public void SubText_SplitsCRLF_EndAtCR()
    {
        var fullText = SourceText.From("abc\r\ndef");
        var subText = new SubText(fullText, new TextSpan(0, 4));
        AssertLinesMatch(SourceText.From("abc\r"), subText);
        Assert.Equal(2, subText.Lines.Count);
        CheckLine(subText, 0, start: 0, length: 3, newLineLength: 1, lineText: "abc");
    }

    [Fact]
    public void SubText_SplitsCRLF_StartAtLF()
    {
        var fullText = SourceText.From("abc\r\ndef");
        AssertLinesMatch(
            SourceText.From("\ndef"),
            new SubText(fullText, new TextSpan(4, 4)));
    }

    [Fact]
    public void SubText_SplitsCRLF_JustCR()
    {
        var fullText = SourceText.From("abc\r\ndef");
        AssertLinesMatch(
            SourceText.From("\r"),
            new SubText(fullText, new TextSpan(3, 1)));
    }

    [Fact]
    public void SubText_SplitsCRLF_JustLF()
    {
        var fullText = SourceText.From("abc\r\ndef");
        AssertLinesMatch(
            SourceText.From("\n"),
            new SubText(fullText, new TextSpan(4, 1)));
    }

    [Fact]
    public void SubText_SplitsCRLF_AtStart()
    {
        var fullText = SourceText.From("\r\nabc");
        AssertLinesMatch(SourceText.From("\r"), new SubText(fullText, new TextSpan(0, 1)));
        AssertLinesMatch(SourceText.From("\nabc"), new SubText(fullText, new TextSpan(1, 4)));
    }

    [Fact]
    public void SubText_SplitsCRLF_AtEnd()
    {
        var fullText = SourceText.From("abc\r\n");
        AssertLinesMatch(SourceText.From("abc\r"), new SubText(fullText, new TextSpan(0, 4)));
        AssertLinesMatch(SourceText.From("\n"), new SubText(fullText, new TextSpan(4, 1)));
    }

    [Fact]
    public void SubText_SplitsCRLF_MultiplePairs()
    {
        var fullText = SourceText.From("a\r\nb\r\nc");
        AssertLinesMatch(SourceText.From("a\r"), new SubText(fullText, new TextSpan(0, 2)));
        AssertLinesMatch(SourceText.From("\nb\r\nc"), new SubText(fullText, new TextSpan(2, 5)));
    }

    [Theory]
    [InlineData("abc\ndef\rghi\r\njkl")]
    [InlineData("\r\n\r\n\r\n")]
    [InlineData("\r\r\r")]
    [InlineData("\n\n\n")]
    [InlineData("a\u0085b\u2028c\u2029d")]
    [InlineData("\r\n")]
    [InlineData("x")]
    [InlineData("ab\r\ncd")]
    [InlineData("\n\r")]
    [InlineData("\r\n\r")]
    [InlineData("\n\r\n")]
    public void SubText_AllSubstrings_MatchStringText(string content)
    {
        var fullText = SourceText.From(content);
        for (var start = 0; start < content.Length; start++)
        {
            for (var end = start + 1; end <= content.Length; end++)
            {
                var reference = SourceText.From(content[start..end]);
                var subText = new SubText(fullText, new TextSpan(start, end - start));
                AssertLinesMatch(reference, subText);
            }
        }
    }

    // --- CompositeText: segment boundary tests ---

    [Fact]
    public void CompositeText_CRLFSpansBoundary()
    {
        var composite = CreateCompositeText("abc\r", "\ndef");
        AssertLinesMatch(SourceText.From("abc\r\ndef"), composite);
    }

    [Fact]
    public void CompositeText_CRLFSpansBoundary_MultipleTimes()
    {
        AssertLinesMatch(
            SourceText.From("ab\r\ncd\r\nef"),
            CreateCompositeText("ab\r", "\ncd\r", "\nef"));
    }

    [Fact]
    public void CompositeText_CRAtEndOfSegment_NotFollowedByLF()
    {
        AssertLinesMatch(
            SourceText.From("abc\rdef"),
            CreateCompositeText("abc\r", "def"));
    }

    [Theory, MemberData(nameof(InterestingContents))]
    public void CompositeText_AllSplitPoints_MatchStringText(string content)
    {
        var reference = SourceText.From(content);
        for (var split = 1; split < content.Length; split++)
        {
            var composite = CreateCompositeText(content[..split], content[split..]);
            AssertLinesMatch(reference, composite);
        }
    }

    [Theory, MemberData(nameof(InterestingContents))]
    public void CompositeText_ThreeWaySplit_AllCombinations(string content)
    {
        var reference = SourceText.From(content);
        for (var split1 = 1; split1 < content.Length - 1; split1++)
        {
            for (var split2 = split1 + 1; split2 < content.Length; split2++)
            {
                var composite = CreateCompositeText(
                    content[..split1], content[split1..split2], content[split2..]);
                AssertLinesMatch(reference, composite);
            }
        }
    }

    // =========================================================================
    // Part 4: TextLine Equals / GetHashCode — verify that the packed _data
    //         representation correctly distinguishes lines and that equal lines
    //         produce identical hash codes.
    // =========================================================================

    [Fact]
    public void TextLine_SameLineIsEqual()
    {
        var text = SourceText.From("abc\ndef");
        var line0a = text.Lines[0];
        var line0b = text.Lines[0];
        Assert.Equal(line0a, line0b);
        Assert.True(line0a == line0b);
        Assert.False(line0a != line0b);
        Assert.Equal(line0a.GetHashCode(), line0b.GetHashCode());
    }

    [Fact]
    public void TextLine_DifferentLinesAreNotEqual()
    {
        var text = SourceText.From("abc\ndef");
        var line0 = text.Lines[0];
        var line1 = text.Lines[1];
        Assert.NotEqual(line0, line1);
        Assert.False(line0 == line1);
        Assert.True(line0 != line1);
    }

    [Fact]
    public void TextLine_SamePositionDifferentBreakLengths_AreNotEqual()
    {
        // "abc\n" has line 0 with break length 1
        // "abc\r\n" has line 0 with break length 2
        // Both lines start at 0 and have text "abc", but the break differs.
        var textLF = SourceText.From("abc\ndef");
        var textCRLF = SourceText.From("abc\r\ndef");
        var lineLF = textLF.Lines[0];
        var lineCRLF = textCRLF.Lines[0];

        Assert.Equal(lineLF.Start, lineCRLF.Start);
        Assert.Equal(lineLF.End, lineCRLF.End);
        Assert.NotEqual(lineLF.EndIncludingLineBreak, lineCRLF.EndIncludingLineBreak);
        Assert.NotEqual(lineLF, lineCRLF);
    }

    [Fact]
    public void TextLine_DifferentTextInstances_AreNotEqual()
    {
        var text1 = SourceText.From("abc");
        var text2 = SourceText.From("abc");
        var line1 = text1.Lines[0];
        var line2 = text2.Lines[0];

        Assert.Equal(line1.Start, line2.Start);
        Assert.Equal(line1.End, line2.End);
        Assert.NotEqual(line1, line2);
    }

    [Fact]
    public void TextLine_EqualsObject()
    {
        var text = SourceText.From("abc\ndef");
        var line = text.Lines[0];
        Assert.True(line.Equals((object)text.Lines[0]));
        Assert.False(line.Equals((object)text.Lines[1]));
        Assert.False(line.Equals("not a TextLine"));
        Assert.False(line.Equals(null));
    }

    [Theory, MemberData(nameof(AllTextKinds))]
    public void TextLine_EqualLines_HaveEqualHashCodes(TextKind kind)
    {
        var text = CreateText("abc\ndef\r\nghi", kind);
        for (var i = 0; i < text.Lines.Count; i++)
        {
            var lineA = text.Lines[i];
            var lineB = text.Lines[i];
            Assert.Equal(lineA, lineB);
            Assert.Equal(lineA.GetHashCode(), lineB.GetHashCode());
        }
    }
}
