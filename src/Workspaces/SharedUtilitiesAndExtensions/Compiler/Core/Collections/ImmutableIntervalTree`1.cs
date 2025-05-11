// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// Implementation of an <see cref="IIntervalTree{T}"/> backed by a contiguous array of values.  This is a more memory
/// efficient way to store an interval tree than the traditional binary tree approach.  This should be used when the 
/// values of the interval tree are known up front and will not change after the tree is created.
/// </summary>
/// <typeparam name="T"></typeparam>
internal readonly struct ImmutableIntervalTree<T> : IIntervalTree<T>
{
    private readonly record struct Node(T Value, int MaxEndNodeIndex);

    public static readonly ImmutableIntervalTree<T> Empty = new(new SegmentedArray<Node>(0));

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

    private ImmutableIntervalTree(SegmentedArray<Node> array)
        => _array = array;

    /// <summary>
    /// Provides access to lots of common algorithms on this interval tree.
    /// </summary>
    public IntervalTreeAlgorithms<T, ImmutableIntervalTree<T>> Algorithms => new(this);

    /// <summary>
    /// Creates a <see cref="ImmutableIntervalTree{T}"/> from an unsorted list of <paramref name="values"/>.  This will
    /// incur a delegate allocation to sort the values.  If callers can avoid that allocation by pre-sorting the values,
    /// they should do so and call <see cref="CreateFromSorted"/> instead.
    /// </summary>
    /// <remarks>
    /// <paramref name="values"/> will be sorted in place.
    /// </remarks>
    public static ImmutableIntervalTree<T> CreateFromUnsorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
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
    public static ImmutableIntervalTree<T> CreateFromSorted<TIntrospector>(in TIntrospector introspector, SegmentedList<T> values)
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
        BuildCompleteTree(values, sourceStartInclusive: 0, sourceEndExclusive: values.Count, array, destinationIndex: 0);

        // Next, do a pass over the entire tree, updating each node to point at the max end node in its subtree.
        ComputeMaxEndNodes(array, 0, in introspector);

        return new ImmutableIntervalTree<T>(array);

        static void BuildCompleteTree(
            SegmentedList<T> source, int sourceStartInclusive, int sourceEndExclusive, SegmentedArray<Node> destination, int destinationIndex)
        {
            var length = sourceEndExclusive - sourceStartInclusive;
            if (length == 0)
                return;

            // Find the element we want to make the root of this subtree.
            //
            // Note: rootIndex is computed entirely based on the length of the subtree.  So it comes back in the range
            // [0, length).  
            //
            // To then index into source, we need to offset it by sourceStartInclusive as that is the start of the
            // source corresponding to the subtree we've walked into.
            var rootIndex = GetRootSourceIndex(length);
            var rootIndexInSource = sourceStartInclusive + rootIndex;

            // Place that element in the appropriate location in the destination.
            destination[destinationIndex] = new(source[rootIndexInSource], destinationIndex);

            // Now recurse into the left and right subtrees to the left/right of the root index in this subtree.
            BuildCompleteTree(source, sourceStartInclusive, rootIndexInSource, destination, destinationIndex * 2 + 1);
            BuildCompleteTree(source, rootIndexInSource + 1, sourceEndExclusive, destination, destinationIndex * 2 + 2);
        }

        // <param name="subtreeNodeCount">Number of nodes in this particular subtree</param>
        static int GetRootSourceIndex(int subtreeNodeCount)
        {
            // Trivial case.  The tree has one element.  That element will be the root.
            if (subtreeNodeCount == 1)
                return 0;

            // We are building a complete binary tree.  By definition, this means that we either have a perfect tree
            // (where every level is full).  Or we have a tree where every level is full except the last level which is
            // filled from left to right.
            //
            // The perfect case is trivial.  We simply take the middle element of the array and make it the root, and
            // then recurse into the left and right halves of the array.

            // The height of the perfect portion of the tree (the rows that are completely full from left to right).
            // This is '3' in both of the examples below.  It will be the height of the whole tree if the whole tree is
            // perfect itself.
            var perfectPortionHeight = SegmentedArraySortUtils.Log2((uint)subtreeNodeCount + 1);

            // Then number of nodes in the perfect portion.  For the example trees below this is 7.
            var perfectPortionNodeCount = PerfectTreeNodeCount(perfectPortionHeight);

            // If the entire subtree we're looking at is perfect or not.  It's perfect if every layer is full.
            // In the above example, both trees are not perfect.
            var wholeSubtreeIsPerfect = perfectPortionNodeCount == subtreeNodeCount;

            // If we do have a perfect tree, the root item trivially the s the middle element of that tree.
            var perfectPortionMidwayPoint = perfectPortionNodeCount / 2;
            if (wholeSubtreeIsPerfect)
                return perfectPortionMidwayPoint;

            // The interesting, imperfect, cases case be demonstrated with the following examples:
            //
            //             10 elements:
            // g, d, i, b, f, h, j, a, c, e
            // 
            //                g
            //          _____/ \_____
            //          d           i
            //       __/ \__       / \
            //       b     f      h   j
            //      / \   /
            //      a c   e
            // 
            // 13 elements:
            // h, d, l, b, f, j, m, a, c, e, g, i, k
            // 
            //                h
            //          _____/ \_____
            //          d           l
            //       __/ \__       / \
            //       b     f      j   m
            //      / \   / \    / \
            //      a c   e g    i k
            //
            // The difference in these cases is the 'd' subtree.  We either have:
            //
            // 1. not enough elements to start filling the right sibling ('i').  This is the case in the 10 element
            //    tree.
            //
            // 2. enough elements to fill it and start filling its right sibling ('l').  This is the case in the 13
            //    element tree.
            //
            // In both cases, one of the two children of the root will be a perfect tree (the right child in the 10
            // element case, and the left element in the 13 element case).  So, when we recurse into either, we either
            // recurse into a perfect tree (which we know is trivial to handle).  Or we will recurse into another tree
            // that we can handle using the balancing logic below.

            // The total tree height.  Since we know we're not perfect, we computed based on one greater than than the
            // perfect-portion height.
            var nodeCountIfTreeWerePerfect = PerfectTreeNodeCount(height: perfectPortionHeight + 1);

            var elementsInLastIncompleteRow = subtreeNodeCount - perfectPortionNodeCount;
            var elementsInLastRowIfTreeWerePerfect = nodeCountIfTreeWerePerfect - perfectPortionNodeCount;

            // The min point in the last row.  If we have it filled less than half full, it's the number of elements.
            // If it is more than half full, it's the midway point.
            var elementsInLastRowCappedAtMidwayPoint = Math.Min(elementsInLastIncompleteRow, elementsInLastRowIfTreeWerePerfect / 2);

            // The pivot point in the array. While filling up the first half of the final row, we're continually
            // incrementing the pivot point (so we include more elements in the left tree).  Once we hit the halfway
            // point in the last row, then we want to stop incrementing the pivot point (so that we include more
            // elements in the right tree).
            return perfectPortionMidwayPoint + elementsInLastRowCappedAtMidwayPoint;
        }

        static int PerfectTreeNodeCount(int height)
            => (1 << height) - 1;

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
            var thisEndValue = introspector.GetSpan(currentNode.Value).End;

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

    bool IIntervalTree<T>.Any<TIntrospector, TIntervalTester>(int start, int length, in TIntrospector introspector, in TIntervalTester intervalTester)
        => IntervalTreeHelpers<T, ImmutableIntervalTree<T>, /*TNode*/ int, FlatArrayIntervalTreeWitness>.Any(this, start, length, introspector, intervalTester);

    int IIntervalTree<T>.FillWithIntervalsThatMatch<TIntrospector, TIntervalTester>(
        int start, int length, ref TemporaryArray<T> builder,
        in TIntrospector introspector, in TIntervalTester intervalTester,
        bool stopAfterFirst)
    {
        return IntervalTreeHelpers<T, ImmutableIntervalTree<T>, /*TNode*/ int, FlatArrayIntervalTreeWitness>.FillWithIntervalsThatMatch(
            this, start, length, ref builder, in introspector, in intervalTester, stopAfterFirst);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => GetEnumerator();

    public IntervalTreeHelpers<T, ImmutableIntervalTree<T>, /*TNode*/ int, FlatArrayIntervalTreeWitness>.Enumerator GetEnumerator()
        => IntervalTreeHelpers<T, ImmutableIntervalTree<T>, /*TNode*/ int, FlatArrayIntervalTreeWitness>.GetEnumerator(this);

    /// <summary>
    /// Wrapper type to allow the IntervalTreeHelpers type to work with this type.
    /// </summary>
    internal readonly struct FlatArrayIntervalTreeWitness : IIntervalTreeWitness<T, ImmutableIntervalTree<T>, int>
    {
        public T GetValue(ImmutableIntervalTree<T> tree, int node)
            => tree._array[node].Value;

        public int GetMaxEndNode(ImmutableIntervalTree<T> tree, int node)
            => tree._array[node].MaxEndNodeIndex;

        public bool TryGetRoot(ImmutableIntervalTree<T> tree, out int root)
        {
            root = 0;
            return tree._array.Length > 0;
        }

        public bool TryGetLeftNode(ImmutableIntervalTree<T> tree, int node, out int leftNode)
        {
            leftNode = GetLeftChildIndex(node);
            return leftNode < tree._array.Length;
        }

        public bool TryGetRightNode(ImmutableIntervalTree<T> tree, int node, out int rightNode)
        {
            rightNode = GetRightChildIndex(node);
            return rightNode < tree._array.Length;
        }
    }
}
