// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Generic function representing the type of interval testing operation that can be performed on an interval tree. For
/// example checking if an interval 'contains', 'intersects', or 'overlaps' with a requested span.
/// </summary>
internal delegate bool TestInterval<T, TIntrospector>(T value, int start, int length, in TIntrospector introspector)
    where TIntrospector : struct, IIntervalIntrospector<T>;

/// <summary>
/// Base interface all interval trees need to implement to get full functionality.  Callers are not expected to use
/// these methods directly.  Instead, they are the low level building blocks that the higher level extension methods are
/// built upon. Consumers of an interval tree should use <c>.Algorithms</c> on the instance to get access to a wealth of
/// fast operations through the <see cref="IntervalTreeAlgorithms{T, TIntervalTree}"/> type.
/// </summary>
internal interface IIntervalTree<T>
{
    /// <summary>
    /// Adds all intervals within the tree within the given start/length pair that match the given <paramref
    /// name="testInterval"/> predicate.  Results are added to the <paramref name="builder"/> array.  The <paramref
    /// name="stopAfterFirst"/> indicates if the search should stop after the first interval is found.  Results will be
    /// returned in a sorted order based on the start point of the interval.
    /// </summary>
    /// <returns>The number of matching intervals found by the method.</returns>
    int FillWithIntervalsThatMatch<TIntrospector>(
        int start, int length, TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder, in TIntrospector introspector,
        bool stopAfterFirst)
        where TIntrospector : struct, IIntervalIntrospector<T>;

    /// <summary>
    /// Practically equivalent to <see cref="FillWithIntervalsThatMatch{TIntrospector}"/> with a check that at least one
    /// item was found.  However, separated out as a separate method as implementations can often be more efficient just
    /// answering this question, versus the more complex "fill with intervals" question above.
    /// </summary>
    bool Any<TIntrospector>(int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>;
}

internal readonly struct FlatArrayIntervalTree<T> : IIntervalTree<T>
{
    private readonly record struct Node(T Value, int MaxEndNodeIndex);

    public static readonly FlatArrayIntervalTree<T> Empty = new(new SegmentedArray<Node>(0));

    private static readonly ObjectPool<Stack<(int nodeIndex, bool firstTime)>> s_stackPool = new(() => new(), trimOnFree: false);

    /// <summary>
    /// Keep around a fair number of these as we often use them in parallel algorithms.
    /// </summary>
    private static readonly ObjectPool<Stack<int>> s_nodeIndexPool = new(() => new(), 128, trimOnFree: false);

    private readonly SegmentedArray<Node> _array;

    private FlatArrayIntervalTree(SegmentedArray<Node> array)
    {
        _array = array;
    }

    /// <summary>
    /// Provides access to lots of common algorithms on this interval tree.
    /// </summary>
    public IntervalTreeAlgorithms<T, FlatArrayIntervalTree<T>> Algorithms => new(this);

    /// <summary>
    /// Creates a <see cref="FlatArrayIntervalTree{T}"/> from an unsorted list of <paramref name="values"/>.  This will
    /// incur a delegate allocation to sort the values.  If callers can avoid that allocation by pre-sorting the values,
    /// they should do so and call <see cref="CreateFromSorted"/> instead.
    /// </summary>
    /// <remarks>
    /// <paramref name="values"/> will be sorted in place.
    /// </remarks>
    public static FlatArrayIntervalTree<T> CreateFromUnsorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var localIntrospector = introspector;
        values.Sort((t1, t2) => localIntrospector.GetSpan(t1).Start - localIntrospector.GetSpan(t2).Start);
        return CreateFromSorted(introspector, values);
    }

    /// <summary>
    /// Creates an interval tree from a sorted list of values.  This is more efficient than creating from an unsorted
    /// list as building doesn't need to figure out where the nodes need to go n-log(n) and doesn't have to rebalance
    /// anything (again, another n-log(n) operation).  Rebalancing is particularly expensive as it involves tons of
    /// pointer chasing operations, which is both slow, and which impacts the GC which has to track all those writes.
    /// </summary>
    /// <remarks>
    /// The values must be sorted such that given any two elements 'a' and 'b' in the list, if 'a' comes before 'b' in
    /// the list, then it's "start position" (as determined by the introspector) must be less than or equal to 'b's
    /// start position.  This is a requirement for the algorithm to work correctly.
    /// </remarks>
    public static FlatArrayIntervalTree<T> CreateFromSorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
#if DEBUG
        var localIntrospector = introspector;
        Debug.Assert(values.IsSorted(Comparer<T>.Create((t1, t2) => localIntrospector.GetSpan(t1).Start - localIntrospector.GetSpan(t2).Start)));
#endif

        if (values.Count == 0)
            return Empty;

        var array = new SegmentedArray<Node>(values.Count);
        CreateFromSortedWorker(values, startInclusive: 0, endExclusive: values.Count, destination: array, middleElementIndex: 0, in introspector);
        return new FlatArrayIntervalTree<T>(array);
    }

    private static void CreateFromSortedWorker<TIntrospector>(
        SegmentedList<T> values,
        int startInclusive,
        int endExclusive,
        SegmentedArray<Node> destination,
        int middleElementIndex,
        in TIntrospector introspector) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var length = endExclusive - startInclusive;
        if (length <= 0)
            return;

        // Start in the middle of the range of values we were asked to look at.
        var mid = startInclusive + (length >> 1);
        var midValue = values[mid];

        // Process the left side.  Everything from the start up to the mid.
        var leftChildDestinationIndex = GetLeftChildIndex(middleElementIndex);
        CreateFromSortedWorker(values, startInclusive, mid, destination, middleElementIndex: leftChildDestinationIndex, in introspector);

        // Process the right side.  Everything after the mid up to the end.
        var rightChildDestinationIndex = GetRightChildIndex(middleElementIndex);
        CreateFromSortedWorker(values, mid + 1, endExclusive, destination, middleElementIndex: rightChildDestinationIndex, in introspector);

        var thisEndValue = GetEnd(midValue, in introspector);
        var leftEndValue = MaxEndValue(destination, leftChildDestinationIndex, in introspector);
        var rightEndValue = MaxEndValue(destination, rightChildDestinationIndex, in introspector);

        int maxEndNodeIndex;
        if (thisEndValue >= leftEndValue && thisEndValue >= rightEndValue)
        {
            maxEndNodeIndex = middleElementIndex;
        }
        else if ((leftEndValue >= rightEndValue) && leftChildDestinationIndex < destination.Length)
        {
            maxEndNodeIndex = destination[leftChildDestinationIndex].MaxEndNodeIndex;
        }
        else if (rightChildDestinationIndex < destination.Length)
        {
            maxEndNodeIndex = destination[rightChildDestinationIndex].MaxEndNodeIndex;
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }

        destination[middleElementIndex] = new Node(midValue, maxEndNodeIndex);
    }

    private static int GetLeftChildIndex(int nodeIndex)
        => (2 * nodeIndex) + 1;

    private static int GetRightChildIndex(int nodeIndex)
        => (2 * nodeIndex) + 2;

    private static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;

    private static int MaxEndValue<TIntrospector>(SegmentedArray<Node> nodes, int index, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => index >= nodes.Length ? 0 : GetEnd(nodes[nodes[index].MaxEndNodeIndex].Value, in introspector);

    bool IIntervalTree<T>.Any<TIntrospector>(int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
    {
        // Inlined version of FillWithIntervalsThatMatch, optimized to do less work and stop once it finds a match.
        var array = _array;
        if (array.Length == 0)
            return false;

        using var _ = s_nodeIndexPool.GetPooledObject(out var candidates);

        var end = start + length;

        candidates.Push(0);

        while (candidates.TryPop(out var currentNodeIndex))
        {
            // Check the nodes as we go down.  That way we can stop immediately when we find something that matches,
            // instead of having to do an entire in-order walk, which might end up hitting a lot of nodes we don't care
            // about and placing a lot into the stack.
            var node = array[currentNodeIndex];
            if (testInterval(node.Value, start, length, in introspector))
                return true;

            if (ShouldExamineRight(start, end, currentNodeIndex, in introspector, out var rightIndex))
                candidates.Push(rightIndex);

            if (ShouldExamineLeft(start, currentNodeIndex, in introspector, out var leftIndex))
                candidates.Push(leftIndex);
        }

        return false;
    }


    int IIntervalTree<T>.FillWithIntervalsThatMatch<TIntrospector>(
        int start, int length, TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder, in TIntrospector introspector,
        bool stopAfterFirst)
    {
        var array = _array;
        if (array.Length == 0)
            return 0;

        using var _ = s_stackPool.GetPooledObject(out var candidates);

        var matches = 0;
        var end = start + length;

        candidates.Push((nodeIndex: 0, firstTime: true));

        while (candidates.TryPop(out var currentTuple))
        {
            var currentNodeIndex = currentTuple.nodeIndex;
            var currentNode = array[currentNodeIndex];

            if (!currentTuple.firstTime)
            {
                // We're seeing this node for the second time (as we walk back up the left
                // side of it).  Now see if it matches our test, and if so return it out.
                if (testInterval(currentNode.Value, start, length, in introspector))
                {
                    matches++;
                    builder.Add(currentNode.Value);

                    if (stopAfterFirst)
                        return 1;
                }
            }
            else
            {
                // First time we're seeing this node.  In order to see the node 'in-order', we push the right side, then
                // the node again, then the left side.  This time we mark the current node with 'false' to indicate that
                // it's the second time we're seeing it the next time it comes around.

                if (ShouldExamineRight(start, end, currentNodeIndex, in introspector, out var right))
                    candidates.Push((right, firstTime: true));

                candidates.Push((currentNodeIndex, firstTime: false));

                if (ShouldExamineLeft(start, currentNodeIndex, in introspector, out var left))
                    candidates.Push((left, firstTime: true));
            }
        }

        return matches;
    }

    private bool ShouldExamineRight<TIntrospector>(
        int start, int end,
        int currentNodeIndex,
        in TIntrospector introspector,
        out int rightIndex) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // right children's starts will never be to the left of the parent's start so we should consider right
        // subtree only if root's start overlaps with interval's End, 
        var array = _array;
        if (introspector.GetSpan(array[currentNodeIndex].Value).Start <= end)
        {
            rightIndex = GetRightChildIndex(currentNodeIndex);
            if (rightIndex < array.Length && GetEnd(array[array[rightIndex].MaxEndNodeIndex].Value, in introspector) >= start)
                return true;
        }

        rightIndex = 0;
        return false;
    }

    private bool ShouldExamineLeft<TIntrospector>(
        int start,
        int currentNodeIndex,
        in TIntrospector introspector,
        out int leftIndex) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // only if left's maxVal overlaps with interval's start, we should consider 
        // left subtree
        var array = _array;
        leftIndex = GetLeftChildIndex(currentNodeIndex);
        if (leftIndex < array.Length && GetEnd(array[array[leftIndex].MaxEndNodeIndex].Value, in introspector) >= start)
            return true;

        return false;
    }
}
