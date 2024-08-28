// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Collections;

public abstract class IntervalTreeTests
{
    private protected readonly struct TupleIntrospector<T> : IIntervalIntrospector<Tuple<int, int, T>>
    {
        public TextSpan GetSpan(Tuple<int, int, T> value)
            => new(value.Item1, value.Item2);
    }

    private IEnumerable<IIntervalTree<Tuple<int, int, string>>> CreateTrees(params Tuple<int, int, string>[] values)
        => CreateTrees((IEnumerable<Tuple<int, int, string>>)values);

    private protected abstract IEnumerable<IIntervalTree<Tuple<int, int, string>>> CreateTrees(IEnumerable<Tuple<int, int, string>> values);

    private protected abstract ImmutableArray<Tuple<int, int, string>> GetIntervalsThatIntersectWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length);
    private protected abstract ImmutableArray<Tuple<int, int, string>> GetIntervalsThatOverlapWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length);
    private protected abstract bool HasIntervalThatIntersectsWith(IIntervalTree<Tuple<int, int, string>> tree, int position);

    [Fact]
    public void TestEmpty()
    {
        foreach (var tree in CreateTrees())
        {
            var spans = GetIntervalsThatOverlapWith(tree, 0, 1);

            Assert.Empty(spans);
        }
    }

    [Fact]
    public void TestBeforeSpan()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 0, 1);

            Assert.Empty(spans);
        }
    }

    [Fact]
    public void TestAbuttingBeforeSpan()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 0, 5);

            Assert.Empty(spans);
        }
    }

    [Fact]
    public void TestAfterSpan()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 15, 5);

            Assert.Empty(spans);
        }
    }

    [Fact]
    public void TestAbuttingAfterSpan()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 10, 5);

            Assert.Empty(spans);
        }
    }

    [Fact]
    public void TestMatchingSpan()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 5, 5).Select(t => t.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestContainedAbuttingStart()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 5, 2).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestContainedAbuttingEnd()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 8, 2).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestCompletedContained()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 7, 2).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestOverlappingStart()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 4, 2).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestOverlappingEnd()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 9, 2).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestOverlappingAll()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A")))
        {
            var spans = GetIntervalsThatOverlapWith(tree, 4, 7).Select(i => i.Item3);

            Assert.True(Set("A").SetEquals(spans));
        }
    }

    [Fact]
    public void TestNonOverlappingSpans()
    {
        foreach (var tree in CreateTrees(Tuple.Create(5, 5, "A"), Tuple.Create(15, 5, "B")))
        {
            // Test between the spans
            Assert.Empty(GetIntervalsThatOverlapWith(tree, 2, 2));
            Assert.Empty(GetIntervalsThatOverlapWith(tree, 11, 2));
            Assert.Empty(GetIntervalsThatOverlapWith(tree, 22, 2));

            // Test in the spans
            Assert.True(Set("A").SetEquals(GetIntervalsThatOverlapWith(tree, 6, 2).Select(i => i.Item3)));
            Assert.True(Set("B").SetEquals(GetIntervalsThatOverlapWith(tree, 16, 2).Select(i => i.Item3)));

            // Test covering both spans
            Assert.True(Set("A", "B").SetEquals(GetIntervalsThatOverlapWith(tree, 2, 20).Select(i => i.Item3)));
            Assert.True(Set("A", "B").SetEquals(GetIntervalsThatOverlapWith(tree, 2, 14).Select(i => i.Item3)));
            Assert.True(Set("A", "B").SetEquals(GetIntervalsThatOverlapWith(tree, 6, 10).Select(i => i.Item3)));
            Assert.True(Set("A", "B").SetEquals(GetIntervalsThatOverlapWith(tree, 6, 20).Select(i => i.Item3)));
        }
    }

    [Fact]
    public void TestSubsumedSpans()
    {
        var spans = List(
            Tuple.Create(5, 5, "a"),
            Tuple.Create(6, 3, "b"),
            Tuple.Create(7, 1, "c"));

        TestOverlapsAndIntersects(spans);
    }

    [Fact]
    public void TestOverlappingSpans()
    {
        var spans = List(
            Tuple.Create(5, 5, "a"),
            Tuple.Create(7, 5, "b"),
            Tuple.Create(9, 5, "c"));

        TestOverlapsAndIntersects(spans);
    }

    [Fact]
    public void TestIntersectsWith()
    {
        var spans = List(
            Tuple.Create(0, 2, "a"));

        foreach (var tree in CreateTrees(spans))
        {
            Assert.False(HasIntervalThatIntersectsWith(tree, -1));
            Assert.True(HasIntervalThatIntersectsWith(tree, 0));
            Assert.True(HasIntervalThatIntersectsWith(tree, 1));
            Assert.True(HasIntervalThatIntersectsWith(tree, 2));
            Assert.False(HasIntervalThatIntersectsWith(tree, 3));
        }
    }

    [Fact]
    public void LargeTest()
    {
        var spans = List(
            Tuple.Create(0, 3, "a"),
            Tuple.Create(5, 3, "b"),
            Tuple.Create(6, 4, "c"),
            Tuple.Create(8, 1, "d"),
            Tuple.Create(15, 8, "e"),
            Tuple.Create(16, 5, "f"),
            Tuple.Create(17, 2, "g"),
            Tuple.Create(19, 1, "h"),
            Tuple.Create(25, 5, "i"));

        TestOverlapsAndIntersects(spans);
    }

    [Fact]
    public void TestCrash1()
    {
        foreach (var _ in CreateTrees(Tuple.Create(8, 1, "A"), Tuple.Create(59, 1, "B"), Tuple.Create(52, 1, "C")))
        {
        }
    }

    [Fact]
    public void TestEmptySpanAtStart()
    {
        // Make sure creating empty spans works (there was a bug here)
        var tree = CreateTrees(Tuple.Create(0, 0, "A")).Last();

        Assert.Equal(1, tree.Count());
    }

    private readonly struct Int32Introspector : IIntervalIntrospector<int>
    {
        public TextSpan GetSpan(int value)
            => new(value, 0);
    }

    private static MutableIntervalTree<int> CreateIntTree(params int[] values)
        => MutableIntervalTree<int>.Create(new Int32Introspector(), values);

    [Fact]
    public void TestSortedEnumerable1()
    {
        Assert.Equal(CreateIntTree(0, 0, 0), new[] { 0, 0, 0 });
        Assert.Equal(CreateIntTree(0, 0, 1), new[] { 0, 0, 1 });
        Assert.Equal(CreateIntTree(0, 0, 2), new[] { 0, 0, 2 });
        Assert.Equal(CreateIntTree(0, 1, 0), new[] { 0, 0, 1 });
        Assert.Equal(CreateIntTree(0, 1, 1), new[] { 0, 1, 1 });
        Assert.Equal(CreateIntTree(0, 1, 2), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(0, 2, 0), new[] { 0, 0, 2 });
        Assert.Equal(CreateIntTree(0, 2, 1), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(0, 2, 2), new[] { 0, 2, 2 });

        Assert.Equal(CreateIntTree(1, 0, 0), new[] { 0, 0, 1 });
        Assert.Equal(CreateIntTree(1, 0, 1), new[] { 0, 1, 1 });
        Assert.Equal(CreateIntTree(1, 0, 2), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(1, 1, 0), new[] { 0, 1, 1 });
        Assert.Equal(CreateIntTree(1, 1, 1), new[] { 1, 1, 1 });
        Assert.Equal(CreateIntTree(1, 1, 2), new[] { 1, 1, 2 });
        Assert.Equal(CreateIntTree(1, 2, 0), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(1, 2, 1), new[] { 1, 1, 2 });
        Assert.Equal(CreateIntTree(1, 2, 2), new[] { 1, 2, 2 });

        Assert.Equal(CreateIntTree(2, 0, 0), new[] { 0, 0, 2 });
        Assert.Equal(CreateIntTree(2, 0, 1), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(2, 0, 2), new[] { 0, 2, 2 });
        Assert.Equal(CreateIntTree(2, 1, 0), new[] { 0, 1, 2 });
        Assert.Equal(CreateIntTree(2, 1, 1), new[] { 1, 1, 2 });
        Assert.Equal(CreateIntTree(2, 1, 2), new[] { 1, 2, 2 });
        Assert.Equal(CreateIntTree(2, 2, 0), new[] { 0, 2, 2 });
        Assert.Equal(CreateIntTree(2, 2, 1), new[] { 1, 2, 2 });
        Assert.Equal(CreateIntTree(2, 2, 2), new[] { 2, 2, 2 });
    }

    [Fact]
    public void TestSortedEnumerable2()
    {
        var tree = MutableIntervalTree<int>.Create(new Int32Introspector(), new[] { 1, 0 });

        Assert.Equal(tree, new[] { 0, 1 });
    }

    private void TestOverlapsAndIntersects(IList<Tuple<int, int, string>> spans)
    {
        foreach (var tree in CreateTrees(spans))
        {
            var max = spans.Max(t => t.Item1 + t.Item2);
            for (var start = 0; start <= max; start++)
            {
                for (var length = 1; length <= max; length++)
                {
                    var span = new Span(start, length);

                    var set1 = new HashSet<string>(GetIntervalsThatOverlapWith(tree, start, length).Select(i => i.Item3));
                    var set2 = new HashSet<string>(spans.Where(t =>
                    {
                        return span.OverlapsWith(new Span(t.Item1, t.Item2));
                    }).Select(t => t.Item3));
                    Assert.True(set1.SetEquals(set2));

                    var set3 = new HashSet<string>(GetIntervalsThatIntersectWith(tree, start, length).Select(i => i.Item3));
                    var set4 = new HashSet<string>(spans.Where(t =>
                    {
                        return span.IntersectsWith(new Span(t.Item1, t.Item2));
                    }).Select(t => t.Item3));
                    Assert.True(set3.SetEquals(set4));
                }
            }

            Assert.Equal(spans.Count, tree.Count());
            Assert.True(new HashSet<string>(spans.Select(t => t.Item3)).SetEquals(tree.Select(i => i.Item3)));
        }
    }

    private static ISet<T> Set<T>(params T[] values)
        => new HashSet<T>(values);

    private static IList<T> List<T>(params T[] values)
        => new List<T>(values);
}

public sealed class BinaryIntervalTreeTests : IntervalTreeTests
{
    private protected override IEnumerable<IIntervalTree<Tuple<int, int, string>>> CreateTrees(IEnumerable<Tuple<int, int, string>> values)
    {
        yield return SimpleMutableIntervalTree.Create(new TupleIntrospector<string>(), values);
    }

    private protected override bool HasIntervalThatIntersectsWith(IIntervalTree<Tuple<int, int, string>> tree, int position)
    {
        return ((MutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.HasIntervalThatIntersectsWith(position, new TupleIntrospector<string>());
    }

    private protected override ImmutableArray<Tuple<int, int, string>> GetIntervalsThatIntersectWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length)
    {
        return ((MutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.GetIntervalsThatIntersectWith(start, length, new TupleIntrospector<string>());
    }

    private protected override ImmutableArray<Tuple<int, int, string>> GetIntervalsThatOverlapWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length)
    {
        return ((MutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.GetIntervalsThatOverlapWith(start, length, new TupleIntrospector<string>());
    }
}

public sealed class FlatArrayIntervalTreeTests : IntervalTreeTests
{
    private protected override IEnumerable<IIntervalTree<Tuple<int, int, string>>> CreateTrees(IEnumerable<Tuple<int, int, string>> values)
    {
        yield return ImmutableIntervalTree<Tuple<int, int, string>>.CreateFromUnsorted(new TupleIntrospector<string>(), new SegmentedList<Tuple<int, int, string>>(values));
    }

    private protected override bool HasIntervalThatIntersectsWith(IIntervalTree<Tuple<int, int, string>> tree, int position)
    {
        return ((ImmutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.HasIntervalThatIntersectsWith(position, new TupleIntrospector<string>());
    }

    private protected override ImmutableArray<Tuple<int, int, string>> GetIntervalsThatIntersectWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length)
    {
        return ((ImmutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.GetIntervalsThatIntersectWith(start, length, new TupleIntrospector<string>());
    }

    private protected override ImmutableArray<Tuple<int, int, string>> GetIntervalsThatOverlapWith(IIntervalTree<Tuple<int, int, string>> tree, int start, int length)
    {
        return ((ImmutableIntervalTree<Tuple<int, int, string>>)tree).Algorithms.GetIntervalsThatOverlapWith(start, length, new TupleIntrospector<string>());
    }

    private readonly struct Int32IntervalIntrospector : IIntervalIntrospector<int>
    {
        public TextSpan GetSpan(int value)
            => new(value, 0);
    }

    private readonly struct CharIntervalIntrospector : IIntervalIntrospector<char>
    {
        public TextSpan GetSpan(char value)
            => new(value, 0);
    }

    [Fact]
    public void TestProperBalancing()
    {
        for (var i = 0; i < 3000; i++)
        {
            var tree = ImmutableIntervalTree<int>.CreateFromUnsorted(new Int32IntervalIntrospector(), new(Enumerable.Range(1, i)));

            // Ensure that the tree produces the same elements in sorted order.
            AssertEx.Equal(tree, Enumerable.Range(1, i));
        }
    }

    [Fact]
    public void TestVeryLargeBalancing()
    {
        for (var i = 10; i < 20; i++)
        {
            var totalCount = 1 << i;

            // Test the values where we have almost filled the tree, to having slightly more than a filled tree.
            Iterate(totalCount);

            // Also test the values where the last row is almost 50% full to more than 50% full.
            Iterate(totalCount - (totalCount >> 2));
        }

        static void Iterate(int totalCount)
        {
            for (var j = -3; j <= 2; j++)
            {
                var allInts = Enumerable.Range(1, totalCount + j);
                var tree = ImmutableIntervalTree<int>.CreateFromSorted(new Int32IntervalIntrospector(), new(allInts));

                // Ensure that the tree produces the same elements in sorted order.
                Assert.True(tree.SequenceEqual(allInts));
            }
        }
    }
}
