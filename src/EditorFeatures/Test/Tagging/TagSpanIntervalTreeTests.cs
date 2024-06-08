// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Tagging;

[UseExportProvider]
public class TagSpanIntervalTreeTests
{
    private static (TagSpanIntervalTree<ITextMarkerTag>, ITextBuffer) CreateTree(string text, params Span[] spans)
    {
        var exportProvider = EditorTestCompositions.Editor.ExportProviderFactory.CreateExportProvider();
        var buffer = EditorFactory.CreateBuffer(exportProvider, text);
        var tags = new SegmentedList<TagSpan<ITextMarkerTag>>(
            spans.Select(s => new TagSpan<ITextMarkerTag>(new SnapshotSpan(buffer.CurrentSnapshot, s), new TextMarkerTag(string.Empty))));
        return (new TagSpanIntervalTree<ITextMarkerTag>(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive, tags), buffer);
    }

    private static IReadOnlyList<TagSpan<TTag>> GetIntersectingSpans<TTag>(
        TagSpanIntervalTree<TTag> tree, SnapshotSpan snapshotSpan)
        where TTag : ITag
    {
        var result = new SegmentedList<TagSpan<TTag>>();
        tree.AddIntersectingTagSpans(snapshotSpan, result);
        return result;
    }

    [Fact]
    public void TestEmptyTree()
    {
        var (tree, buffer) = CreateTree(string.Empty);

        Assert.Empty(GetIntersectingSpans(tree, buffer.CurrentSnapshot.GetFullSpan()));
    }

    [Fact]
    public void TestSingleSpan()
    {
        var (tree, buffer) = CreateTree("Hello, World", new Span(0, 5));

        Assert.Equal(new Span(0, 5), GetIntersectingSpans(tree, buffer.CurrentSnapshot.GetFullSpan()).Single().Span);
    }

    [Fact]
    public void TestSingleIntersectingSpanAtStartWithEdit()
    {
        var (tree, buffer) = CreateTree("Hello, World", new Span(7, 5));
        buffer.Insert(0, new string('c', 100));

        // The span should start at 107
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 107, 0));
        Assert.Equal(new Span(107, 5), spans.Single().Span);
    }

    [Fact]
    public void TestSingleIntersectingSpanAtEndWithEdit()
    {
        var (tree, buffer) = CreateTree("Hello, World", new Span(7, 5));
        buffer.Insert(0, new string('c', 100));

        // The span should end at 112
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 112, 0));
        Assert.Equal(new Span(107, 5), spans.Single().Span);
    }

    [Fact]
    public void TestManySpansWithEdit()
    {
        // Create a buffer with the second half of the buffer covered with spans
        var (tree, buffer) = CreateTree(new string('c', 100), Enumerable.Range(50, count: 50).Select(s => new Span(s, 1)).ToArray());
        buffer.Insert(0, new string('c', 100));

        // We should have 50 spans if we start looking at just the end
        Assert.Equal(50, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 150, 50)).Count());

        // And we should have 26 here. We directly cover 25 spans, and we touch one more
        Assert.Equal(26, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 175, 25)).Count());
    }

    [Fact]
    public void TestManySpansWithEdit2()
    {
        // Cover the full buffer with spans
        var (tree, buffer) = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());
        buffer.Insert(0, new string('c', 100));

        // We should see one span anywhere in the beginning of the buffer, since this is edge inclusive
        Assert.Equal(1, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 0, 1)).Count());
        Assert.Equal(1, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 50, 1)).Count());

        // We should see two at position 100 (the first span that is now expanded, and the second of width 1)
        Assert.Equal(2, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 100, 1)).Count());
    }

    [Fact]
    public void TestManySpansWithDeleteAndEditAtStart()
    {
        // Cover the full buffer with spans
        var (tree, buffer) = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

        buffer.Delete(new Span(0, 50));
        buffer.Insert(0, new string('c', 50));

        // We should see 51 spans intersecting the start. When we did the delete, we contracted 50 spans to size
        // zero, and then the insert will have expanded all of those, plus the span right next to it.
        Assert.Equal(51, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 0, 1)).Count());
    }

    [Fact]
    public void TestManySpansWithDeleteAndEditAtEnd()
    {
        // Cover the full buffer with spans
        var (tree, buffer) = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

        buffer.Delete(new Span(50, 50));
        buffer.Insert(50, new string('c', 50));

        // We should see 51 spans intersecting the end. When we did the delete, we contracted 50 spans to size zero,
        // and then the insert will have expanded all of those, plus the span right next to it.
        Assert.Equal(51, GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 99, 1)).Count());
    }

    [Fact]
    public void TestTagSpanOrdering()
    {
        // Cover the full buffer with spans
        var (tree, buffer) = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

        var lastStart = -1;
        foreach (var tag in GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)))
        {
            Assert.True(lastStart < tag.Span.Start.Position);
            lastStart = tag.Span.Start.Position;
        }
    }

    [Fact]
    public void TestEmptySpanIntersects1()
    {
        var (tree, buffer) = CreateTree("goo", new Span(0, 0));
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, new Span(0, 0)));
        Assert.Single(spans);
    }

    [Fact]
    public void TestEmptySpanIntersects2()
    {
        var (tree, buffer) = CreateTree("goo", new Span(0, 0));
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, new Span(0, "goo".Length)));
        Assert.Single(spans);
    }

    [Fact]
    public void TestEmptySpanIntersects3()
    {
        var (tree, buffer) = CreateTree("goo", new Span(1, 0));
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, new Span(0, 1)));
        Assert.Single(spans);
    }

    [Fact]
    public void TestEmptySpanIntersects4()
    {
        var (tree, buffer) = CreateTree("goo", new Span(1, 0));
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, new Span(1, 0)));
        Assert.Single(spans);
    }

    [Fact]
    public void TestEmptySpanIntersects5()
    {
        var (tree, buffer) = CreateTree("goo", new Span(1, 0));
        var spans = GetIntersectingSpans(tree, new SnapshotSpan(buffer.CurrentSnapshot, new Span(1, 1)));
        Assert.Single(spans);
    }
}
