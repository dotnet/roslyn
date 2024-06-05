// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
internal interface IIntervalTree<T> : IEnumerable<T>
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

    /// <summary>
    /// The nodes of this interval tree flatted into a single array.  The root is as index 0.  The left child of any
    /// node at index <c>i</c> is at <c>2*i + 1</c> and the right child is at <c>2*i + 2</c>. If a left/right child
    /// index is beyond the length of this array, that is equivalent to that node not having such a child.
    /// </summary>
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
    /// <list type="bullet">The values must be sorted such that given any two elements 'a' and 'b' in the list, if 'a'
    /// comes before 'b' in the list, then it's "start position" (as determined by the introspector) must be less than
    /// or equal to 'b's start position.  This is a requirement for the algorithm to work correctly.
    /// </list>
    /// <list type="bullet">The <paramref name="values"/> list will be mutated as part of this operation.
    /// </list>
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

        // Create the array to sort the binary search tree nodes in.
        var array = new SegmentedArray<Node>(values.Count);

        // Place the values into the array in a way that will create a complete binary tree.  A complete binary tree is
        // a binary tree in which every level, except possibly the last, is completely filled, and all nodes in the last
        // level are as far left as possible. 
        BuildCompleteTreeTop(values, array);

        // Next, do a pass over the entire tree, updating each node to point at the max end node in its subtree.
        ComputeMaxEndNodes(array, 0, in introspector);

        return new FlatArrayIntervalTree<T>(array);

        static void BuildCompleteTreeTop(SegmentedList<T> source, SegmentedArray<Node> destination)
        {
            // The nature of a complete tree is that the last level always only contains the odd remaining numbers.
            // For example, given the initial values 1-14 the final tree will look like:
            //
            // 8, 4, 12, 2, 6, 10, 14, 1, 3, 5, 7, 9, 11, 13.  Which corresponds to;
            //
            //               8
            //        /            \
            //       4              12
            //      / \            /  \
            //     2   6        10      14
            //    / \ / \       / \    /
            //   1  3 5  7     9  11  13
            //
            // Note that 8-14 (even values) are a perfect balanced tree, and the 1-13 (odd values) are the remaining
            // values on the last level.

            // How many levels will be in the perfect binary tree.  For the example above, this would be 3. 
            var level = SegmentedArraySortUtils.Log2((uint)source.Count + 1);

            // How many extra elements will be on the last level of the binary tree (if this is not a perfect tree).
            // For the example above, this is 7.  
            var extraElementsCount = source.Count - (int)(Math.Pow(2, level) - 1);

            if (extraElementsCount > 0)
            {
                var lastElementToSwap = extraElementsCount * 2 - 2;

                for (int i = lastElementToSwap, j = 0; i > 1; i -= 2, j++)
                {
                    var destinationIndex = destination.Length - 1 - j;
                    destination[destination.Length - 1 - j] = new Node(source[i], MaxEndNodeIndex: destinationIndex);
                    source[lastElementToSwap - j] = source[i - 1];
                }

                // After this, source will be equal to:
                // 1, 2, 3, 4, 5, 6, 7, 2, 4, 6, 8, 10, 12, 14.
                //
                // In other words, the last half (after '2') will be updated to be the even elements.  This will be what
                // we'll create the perfect tree from below.
                //
                // Destination will be equal to:
                // ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, 3, 5, 7, 9, 11, 13

                var firstOddIndex = destination.Length - extraElementsCount;
                destination[firstOddIndex] = new Node(source[0], MaxEndNodeIndex: firstOddIndex);
                // Destination will be equal to:
                // ␀, ␀, ␀, ␀, ␀, ␀, ␀, 1, 3, 5, 7, 9, 11, 13
            }

            // Recursively build the perfect balanced subtree from the remaining elements, storing them into the start
            // of the array.  In the above example, this is bulding the perfect balanced tree for the event elements
            // 8-14.
            BuildCompleteTreeRecursive(
                source, destination, startInclusive: extraElementsCount, endExclusive: source.Count, destinationIndex: 0);
        }

        static void BuildCompleteTreeRecursive(
            SegmentedList<T> source,
            SegmentedArray<Node> destination,
            int startInclusive,
            int endExclusive,
            int destinationIndex)
        {
            if (startInclusive >= endExclusive)
                return;

            var midPoint = (startInclusive + endExclusive) / 2;
            destination[destinationIndex] = new Node(source[midPoint], MaxEndNodeIndex: destinationIndex);

            BuildCompleteTreeRecursive(source, destination, startInclusive, midPoint, GetLeftChildIndex(destinationIndex));
            BuildCompleteTreeRecursive(source, destination, midPoint + 1, endExclusive, GetRightChildIndex(destinationIndex));
        }

        static void ComputeMaxEndNodes(SegmentedArray<Node> array, int currentElementIndex, in TIntrospector introspector)
        {
            if (currentElementIndex >= array.Length)
                return;

            var leftChildIndex = GetLeftChildIndex(currentElementIndex);
            var rightChildIndex = GetRightChildIndex(currentElementIndex);

            var currentNode = array[currentElementIndex];
            var thisEndValue = GetEnd(currentNode.Value, in introspector);
            var leftEndValue = MaxEndValue(array, leftChildIndex, in introspector);
            var rightEndValue = MaxEndValue(array, rightChildIndex, in introspector);

            int maxEndNodeIndex;
            if (thisEndValue >= leftEndValue && thisEndValue >= rightEndValue)
            {
                maxEndNodeIndex = currentElementIndex;
            }
            else if ((leftEndValue >= rightEndValue) && leftEndValue < array.Length)
            {
                maxEndNodeIndex = array[leftEndValue].MaxEndNodeIndex;
            }
            else if (rightChildIndex < array.Length)
            {
                maxEndNodeIndex = array[rightChildIndex].MaxEndNodeIndex;
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }

            array[currentElementIndex] = new Node(currentNode.Value, maxEndNodeIndex);
        }
    }

#if false
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
#endif

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

            if (ShouldExamineRight(array, start, end, currentNodeIndex, in introspector, out var rightIndex))
                candidates.Push(rightIndex);

            if (ShouldExamineLeft(array, start, currentNodeIndex, in introspector, out var leftIndex))
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

                if (ShouldExamineRight(array, start, end, currentNodeIndex, in introspector, out var right))
                    candidates.Push((right, firstTime: true));

                candidates.Push((currentNodeIndex, firstTime: false));

                if (ShouldExamineLeft(array, start, currentNodeIndex, in introspector, out var left))
                    candidates.Push((left, firstTime: true));
            }
        }

        return matches;
    }

    private static bool ShouldExamineRight<TIntrospector>(
        SegmentedArray<Node> array,
        int start,
        int end,
        int currentNodeIndex,
        in TIntrospector introspector,
        out int rightIndex) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // right children's starts will never be to the left of the parent's start so we should consider right
        // subtree only if root's start overlaps with interval's End, 
        if (introspector.GetSpan(array[currentNodeIndex].Value).Start <= end)
        {
            rightIndex = GetRightChildIndex(currentNodeIndex);
            if (rightIndex < array.Length && GetEnd(array[array[rightIndex].MaxEndNodeIndex].Value, in introspector) >= start)
                return true;
        }

        rightIndex = 0;
        return false;
    }

    private static bool ShouldExamineLeft<TIntrospector>(
        SegmentedArray<Node> array,
        int start,
        int currentNodeIndex,
        in TIntrospector introspector,
        out int leftIndex) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // only if left's maxVal overlaps with interval's start, we should consider 
        // left subtree
        leftIndex = GetLeftChildIndex(currentNodeIndex);
        if (leftIndex < array.Length && GetEnd(array[array[leftIndex].MaxEndNodeIndex].Value, in introspector) >= start)
            return true;

        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public IEnumerator<T> GetEnumerator()
    {
        var array = _array;
        return array.Length == 0 ? SpecializedCollections.EmptyEnumerator<T>() : GetEnumeratorWorker(array);

        static IEnumerator<T> GetEnumeratorWorker(SegmentedArray<Node> array)
        {
            using var _ = s_stackPool.GetPooledObject(out var candidates);
            candidates.Push((0, firstTime: true));
            while (candidates.TryPop(out var tuple))
            {
                var (currentNodeIndex, firstTime) = tuple;
                if (firstTime)
                {
                    // First time seeing this node.  Mark that we've been seen and recurse down the left side.  The
                    // next time we see this node we'll yield it out.
                    var rightIndex = GetRightChildIndex(currentNodeIndex);
                    var leftIndex = GetLeftChildIndex(currentNodeIndex);

                    if (rightIndex < array.Length)
                        candidates.Push((rightIndex, firstTime: true));

                    candidates.Push((currentNodeIndex, firstTime: false));

                    if (leftIndex < array.Length)
                        candidates.Push((leftIndex, firstTime: true));
                }
                else
                {
                    yield return array[currentNodeIndex].Value;
                }
            }
        }
    }
}
