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
/// Implementation of an <see cref="IIntervalTree{T}"/> backed by a contiguous array of values.  This is a more memory
/// efficient way to store an interval tree than the traditional binary tree approach.  This should be used when the 
/// values of the interval tree are known up front and will not change after the tree is created.
/// </summary>
/// <typeparam name="T"></typeparam>
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
    /// <remarks>
    /// The binary tree we represent here is a *complete* binary tree (not to be confused with a *perfect* binary tree).
    /// A complete binary tree is a binary tree in which every level, except possibly the last, is completely filled,
    /// and all nodes in the last level are as far left as possible. 
    /// </remarks>
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

        // Place the values into the array in a way that will create a complete binary tree.
        BuildCompleteTreeTop(values, array);

        // Next, do a pass over the entire tree, updating each node to point at the max end node in its subtree.
        ComputeMaxEndNodes(array, 0, in introspector);

        return new FlatArrayIntervalTree<T>(array);

        static void BuildCompleteTreeTop(SegmentedList<T> source, SegmentedArray<Node> destination)
        {
            // The nature of a complete tree is that the last level always only contains the odd remaining numbers.
            // For example, given the initial values a-n:
            // 
            // a, b, c, d, e, f, g, h, i, j, k, l, m, n.  The final tree will look like:
            // h, d, l, b, f, j, n, a, c, e, g, i, k, m.  Which corresponds to:
            //
            //           h
            //        /     \
            //       d       l
            //      / \     / \
            //     b   f   j   n
            //    / \ / \ / \ /
            //    a c e g i k m
            //
            // Note that the first 3 levels are the even elements of the original list) which end up forming a perfect
            // balanced tree, and the odd elements of the original list are the remaining values on the last level.

            // How many levels will be in the perfect binary tree.  For the example above, this would be 3. 
            var level = SegmentedArraySortUtils.Log2((uint)source.Count + 1);

            // How many extra elements will be on the last level of the binary tree (if this is not a perfect tree).
            // For the example above, this is 7.
            var extraElementsCount = source.Count - ((1 << level) - 1);

            if (extraElementsCount > 0)
            {
                // Where at the end to start swapping elements from.  In the above example, this would be 12.
                var lastElementToSwap = extraElementsCount * 2 - 2;

                for (int i = lastElementToSwap, j = 0; i > 1; i -= 2, j++)
                {
                    var destinationIndex = destination.Length - 1 - j;
                    destination[destinationIndex] = new Node(source[i], MaxEndNodeIndex: destinationIndex);
                    source[lastElementToSwap - j] = source[i - 1];

                    // The above loop will do the following over the first few iterations (changes highlighted with *):
                    //
                    // Dst: ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀,   ␀, *m* // m placed at the end of the destination.
                    // Src: a, b, c, d, e, f, g, h, i, j, k, l, *l*,   n // l moved to where m was in the original source.
                    //
                    // Dst: ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀,   ␀, *k*, m // k placed right before m in the destination.
                    // Src: a, b, c, d, e, f, g, h, i, j, k, *j*,   l, n // j moved right before where we placed l in the original source.
                    //
                    // Dst: ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀,   ␀, *i*, k, m // i placed right before k in the destination.
                    // Src: a, b, c, d, e, f, g, h, i, j, *h*,   j, l, n // h moved right before where we placed j in the original source.
                    //
                    // Each iteration takes the next odd element from the end of the source list and places it at the
                    // next available space from the end of the destination array (effectively building the last row of
                    // the complete binary tree).
                    //
                    // It then takes the next even element from the end of the source list and moves it to the next spot
                    // from the end of the source list.  This makes the end of the source-list contain the original even
                    // elements (up the perfect-complete count of elements), now abutted against each other.
                }

                // After this, source will be equal to:
                //
                // a, b, c, d, e, f, g - b, d, f, h, j, l, n.
                //
                // In other words, the last half (after 'g') will be updated to be the even elements from the original
                // list.  This will be what we'll create the perfect tree from below.  We will not look at the elements
                // before this in 'source' as they are already either in the correct place in the 'source' *or*
                // 'destination' arrays.
                //
                // Destination will be equal to:
                // ␀, ␀, ␀, ␀, ␀, ␀, ␀, ␀, c, e, g, i, k, m
                //
                // which is the odd elements from the original list.

                // The above loop will not hit the first element in the list (since we do not want to do a swap for the
                // root element).  So we have to handle this case specially at the end.
                var firstOddIndex = destination.Length - extraElementsCount;
                destination[firstOddIndex] = new Node(source[0], MaxEndNodeIndex: firstOddIndex);
                // Destination will be equal to:
                // ␀, ␀, ␀, ␀, ␀, ␀, ␀, a, c, e, g, i, k, m
            }

            // Recursively build the perfect balanced subtree from the remaining elements, storing them into the start
            // of the array.  In the above example, this is building the perfect balanced tree for the event elements
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

        // Returns the max end *position* of tree rooted at currentNodeIndex.  If there is no tree here (it refers to a
        // null child), then this will return -1;
        static int ComputeMaxEndNodes(SegmentedArray<Node> array, int currentNodeIndex, in TIntrospector introspector)
        {
            if (currentNodeIndex >= array.Length)
                return -1;

            var leftChildIndex = GetLeftChildIndex(currentNodeIndex);
            var rightChildIndex = GetRightChildIndex(currentNodeIndex);

            // ensure the left and right trees have their max end nodes computed first.
            var leftMaxEndValue = ComputeMaxEndNodes(array, leftChildIndex, in introspector);
            var rightMaxEndValue = ComputeMaxEndNodes(array, rightChildIndex, in introspector);

            // Now get the max end of the left and right children and compare to our end.  Whichever is the rightmost
            // endpoint is considered the max end index.
            var currentNode = array[currentNodeIndex];
            var thisEndValue = GetEnd(currentNode.Value, in introspector);

            if (thisEndValue >= leftMaxEndValue && thisEndValue >= rightMaxEndValue)
            {
                // The root's end was further to the right than both the left subtree and the right subtree. No need to
                // change it as that is what we store by default for any node.
                return thisEndValue;
            }

            // One of the left or right subtrees went further to the right.
            Contract.ThrowIfTrue(leftMaxEndValue < 0 && rightMaxEndValue < 0);

            if (leftMaxEndValue >= rightMaxEndValue)
            {
                // Set this node's max end to be the left subtree's max end.
                var maxEndNodeIndex = array[leftChildIndex].MaxEndNodeIndex;
                array[currentNodeIndex] = new Node(currentNode.Value, maxEndNodeIndex);
                return leftMaxEndValue;
            }
            else
            {
                Contract.ThrowIfFalse(rightMaxEndValue > leftMaxEndValue);

                // Set this node's max end to be the right subtree's max end.
                var maxEndNodeIndex = array[rightChildIndex].MaxEndNodeIndex;
                array[currentNodeIndex] = new Node(currentNode.Value, maxEndNodeIndex);
                return rightMaxEndValue;
            }
        }
    }

    private static int GetLeftChildIndex(int nodeIndex)
        => (2 * nodeIndex) + 1;

    private static int GetRightChildIndex(int nodeIndex)
        => (2 * nodeIndex) + 2;

    private static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;

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
