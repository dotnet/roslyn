// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Tagging
{
    [UseExportProvider]
    public class TagSpanIntervalTreeTests
    {
        private TagSpanIntervalTree<ITextMarkerTag> CreateTree(string text, params Span[] spans)
        {
            var buffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, text);
            var tags = spans.Select(s => new TagSpan<ITextMarkerTag>(new SnapshotSpan(buffer.CurrentSnapshot, s), new TextMarkerTag(string.Empty)));
            return new TagSpanIntervalTree<ITextMarkerTag>(buffer, SpanTrackingMode.EdgeInclusive, tags);
        }

        [Fact]
        public void TestEmptyTree()
        {
            var tree = CreateTree(string.Empty);

            Assert.Empty(tree.GetSpans(tree.Buffer.CurrentSnapshot));
        }

        [Fact]
        public void TestSingleSpan()
        {
            var tree = CreateTree("Hello, World", new Span(0, 5));

            Assert.Equal(new Span(0, 5), tree.GetSpans(tree.Buffer.CurrentSnapshot).Single().Span);
        }

        [Fact]
        public void TestSingleIntersectingSpanAtStartWithEdit()
        {
            var tree = CreateTree("Hello, World", new Span(7, 5));
            tree.Buffer.Insert(0, new string('c', 100));

            // The span should start at 107
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 107, 0));
            Assert.Equal(new Span(107, 5), spans.Single().Span);
        }

        [Fact]
        public void TestSingleIntersectingSpanAtEndWithEdit()
        {
            var tree = CreateTree("Hello, World", new Span(7, 5));
            tree.Buffer.Insert(0, new string('c', 100));

            // The span should end at 112
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 112, 0));
            Assert.Equal(new Span(107, 5), spans.Single().Span);
        }

        [Fact]
        public void TestManySpansWithEdit()
        {
            // Create a buffer with the second half of the buffer covered with spans
            var tree = CreateTree(new string('c', 100), Enumerable.Range(50, count: 50).Select(s => new Span(s, 1)).ToArray());
            tree.Buffer.Insert(0, new string('c', 100));

            // We should have 50 spans if we start looking at just the end
            Assert.Equal(50, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 150, 50)).Count());

            // And we should have 26 here. We directly cover 25 spans, and we touch one more
            Assert.Equal(26, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 175, 25)).Count());
        }

        [Fact]
        public void TestManySpansWithEdit2()
        {
            // Cover the full buffer with spans
            var tree = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());
            tree.Buffer.Insert(0, new string('c', 100));

            // We should see one span anywhere in the beginning of the buffer, since this is edge inclusive
            Assert.Equal(1, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 0, 1)).Count());
            Assert.Equal(1, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 50, 1)).Count());

            // We should see two at position 100 (the first span that is now expanded, and the second of width 1)
            Assert.Equal(2, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 100, 1)).Count());
        }

        [Fact]
        public void TestManySpansWithDeleteAndEditAtStart()
        {
            // Cover the full buffer with spans
            var tree = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

            tree.Buffer.Delete(new Span(0, 50));
            tree.Buffer.Insert(0, new string('c', 50));

            // We should see 51 spans intersecting the start. When we did the delete, we contracted 50 spans to size
            // zero, and then the insert will have expanded all of those, plus the span right next to it.
            Assert.Equal(51, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 0, 1)).Count());
        }

        [Fact]
        public void TestManySpansWithDeleteAndEditAtEnd()
        {
            // Cover the full buffer with spans
            var tree = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

            tree.Buffer.Delete(new Span(50, 50));
            tree.Buffer.Insert(50, new string('c', 50));

            // We should see 51 spans intersecting the end. When we did the delete, we contracted 50 spans to size zero,
            // and then the insert will have expanded all of those, plus the span right next to it.
            Assert.Equal(51, tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 99, 1)).Count());
        }

        [Fact]
        public void TestTagSpanOrdering()
        {
            // Cover the full buffer with spans
            var tree = CreateTree(new string('c', 100), Enumerable.Range(0, count: 100).Select(s => new Span(s, 1)).ToArray());

            var lastStart = -1;
            foreach (var tag in tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, 0, tree.Buffer.CurrentSnapshot.Length)))
            {
                Assert.True(lastStart < tag.Span.Start.Position);
                lastStart = tag.Span.Start.Position;
            }
        }

        [Fact]
        public void TestEmptySpanIntersects1()
        {
            var tree = CreateTree("goo", new Span(0, 0));
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, new Span(0, 0)));
            Assert.True(spans.Count == 1);
        }

        [Fact]
        public void TestEmptySpanIntersects2()
        {
            var tree = CreateTree("goo", new Span(0, 0));
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, new Span(0, "goo".Length)));
            Assert.True(spans.Count == 1);
        }

        [Fact]
        public void TestEmptySpanIntersects3()
        {
            var tree = CreateTree("goo", new Span(1, 0));
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, new Span(0, 1)));
            Assert.True(spans.Count == 1);
        }

        [Fact]
        public void TestEmptySpanIntersects4()
        {
            var tree = CreateTree("goo", new Span(1, 0));
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, new Span(1, 0)));
            Assert.True(spans.Count == 1);
        }

        [Fact]
        public void TestEmptySpanIntersects5()
        {
            var tree = CreateTree("goo", new Span(1, 0));
            var spans = tree.GetIntersectingSpans(new SnapshotSpan(tree.Buffer.CurrentSnapshot, new Span(1, 1)));
            Assert.True(spans.Count == 1);
        }
    }
}
