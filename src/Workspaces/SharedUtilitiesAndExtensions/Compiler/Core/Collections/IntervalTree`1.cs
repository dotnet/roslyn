// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// An interval tree represents an ordered tree data structure to store intervals of the form [start, end).  It allows
/// you to efficiently find all intervals that intersect or overlap a provided interval.
/// </summary>
/// <remarks>
/// Ths is the root type for all interval trees that store their data in a binary tree format.  This format is good for
/// when mutation of the tree is expected, and a client wants to perform tests before and after such mutation.
/// </remarks>
internal partial class BinaryIntervalTree<T> : IIntervalTree<T>
{
    public static readonly BinaryIntervalTree<T> Empty = new();

    private static readonly ObjectPool<Stack<(Node node, bool firstTime)>> s_stackPool = new(() => new(), trimOnFree: false);

    /// <summary>
    /// Keep around a fair number of these as we often use them in parallel algorithms.
    /// </summary>
    private static readonly ObjectPool<Stack<Node>> s_nodePool = new(() => new(), 128);

    protected Node? root;

    public static BinaryIntervalTree<T> Create<TIntrospector>(in TIntrospector introspector, IEnumerable<T>? values = null)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var result = new BinaryIntervalTree<T>();

        if (values != null)
        {
            foreach (var value in values)
                result.root = Insert(result.root, new Node(value), in introspector);
        }

        return result;
    }

    /// <summary>
    /// Provides access to lots of common algorithms on this interval tree.
    /// </summary>
    public IntervalTreeAlgorithms<T, BinaryIntervalTree<T>> Algorithms => new(this);

    bool IIntervalTree<T>.Any<TIntrospector>(int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
    {
        // Inlined version of FillWithIntervalsThatMatch, optimized to do less work and stop once it finds a match.
        if (root is null)
            return false;

        using var _ = s_nodePool.GetPooledObject(out var candidates);

        var end = start + length;

        candidates.Push(root);

        while (candidates.TryPop(out var currentNode))
        {
            // Check the nodes as we go down.  That way we can stop immediately when we find something that matches,
            // instead of having to do an entire in-order walk, which might end up hitting a lot of nodes we don't care
            // about and placing a lot into the stack.
            if (testInterval(currentNode.Value, start, length, in introspector))
                return true;

            if (ShouldExamineRight(start, end, currentNode, in introspector, out var right))
                candidates.Push(right);

            if (ShouldExamineLeft(start, currentNode, in introspector, out var left))
                candidates.Push(left);
        }

        return false;
    }

    int IIntervalTree<T>.FillWithIntervalsThatMatch<TIntrospector>(
        int start, int length, TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder, in TIntrospector introspector,
        bool stopAfterFirst)
    {
        if (root == null)
            return 0;

        using var _ = s_stackPool.GetPooledObject(out var candidates);

        var matches = 0;
        var end = start + length;

        candidates.Push((root, firstTime: true));

        while (candidates.TryPop(out var currentTuple))
        {
            var currentNode = currentTuple.node;

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

                if (ShouldExamineRight(start, end, currentNode, in introspector, out var right))
                    candidates.Push((right, firstTime: true));

                candidates.Push((currentNode, firstTime: false));

                if (ShouldExamineLeft(start, currentNode, in introspector, out var left))
                    candidates.Push((left, firstTime: true));
            }
        }

        return matches;
    }

    private static bool ShouldExamineRight<TIntrospector>(
        int start, int end,
        Node currentNode,
        in TIntrospector introspector,
        [NotNullWhen(true)] out Node? right) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // right children's starts will never be to the left of the parent's start so we should consider right
        // subtree only if root's start overlaps with interval's End, 
        if (introspector.GetSpan(currentNode.Value).Start <= end)
        {
            right = currentNode.Right;
            if (right != null && GetEnd(right.MaxEndNode.Value, in introspector) >= start)
                return true;
        }

        right = null;
        return false;
    }

    private static bool ShouldExamineLeft<TIntrospector>(
        int start,
        Node currentNode,
        in TIntrospector introspector,
        [NotNullWhen(true)] out Node? left) where TIntrospector : struct, IIntervalIntrospector<T>
    {
        // only if left's maxVal overlaps with interval's start, we should consider 
        // left subtree
        left = currentNode.Left;
        if (left != null && GetEnd(left.MaxEndNode.Value, in introspector) >= start)
            return true;

        return false;
    }

    public bool IsEmpty() => this.root == null;

    protected static Node Insert<TIntrospector>(Node? root, Node newNode, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var newNodeStart = introspector.GetSpan(newNode.Value).Start;
        return Insert(root, newNode, newNodeStart, in introspector);
    }

    private static Node Insert<TIntrospector>(Node? root, Node newNode, int newNodeStart, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        if (root == null)
        {
            return newNode;
        }

        Node? newLeft, newRight;

        if (newNodeStart < introspector.GetSpan(root.Value).Start)
        {
            newLeft = Insert(root.Left, newNode, newNodeStart, in introspector);
            newRight = root.Right;
        }
        else
        {
            newLeft = root.Left;
            newRight = Insert(root.Right, newNode, newNodeStart, in introspector);
        }

        root.SetLeftRight(newLeft, newRight, in introspector);
        var newRoot = root;

        return Balance(newRoot, in introspector);
    }

    private static Node Balance<TIntrospector>(Node node, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var balanceFactor = BalanceFactor(node);
        if (balanceFactor == -2)
        {
            var rightBalance = BalanceFactor(node.Right);
            if (rightBalance == -1)
            {
                return node.LeftRotation(in introspector);
            }
            else
            {
                Debug.Assert(rightBalance == 1);
                return node.InnerRightOuterLeftRotation(in introspector);
            }
        }
        else if (balanceFactor == 2)
        {
            var leftBalance = BalanceFactor(node.Left);
            if (leftBalance == 1)
            {
                return node.RightRotation(in introspector);
            }
            else
            {
                Debug.Assert(leftBalance == -1);
                return node.InnerLeftOuterRightRotation(in introspector);
            }
        }

        return node;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return this == Empty || root == null ? SpecializedCollections.EmptyEnumerator<T>() : GetEnumeratorWorker();

        IEnumerator<T> GetEnumeratorWorker()
        {
            Contract.ThrowIfNull(root);
            using var _ = ArrayBuilder<(Node node, bool firstTime)>.GetInstance(out var candidates);
            candidates.Push((root, firstTime: true));
            while (candidates.TryPop(out var tuple))
            {
                var (currentNode, firstTime) = tuple;
                if (firstTime)
                {
                    // First time seeing this node.  Mark that we've been seen and recurse down the left side.  The
                    // next time we see this node we'll yield it out.
                    if (currentNode.Right != null)
                        candidates.Push((currentNode.Right, firstTime: true));

                    candidates.Push((currentNode, firstTime: false));

                    if (currentNode.Left != null)
                        candidates.Push((currentNode.Left, firstTime: true));
                }
                else
                {
                    yield return currentNode.Value;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    protected static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;

    protected static int MaxEndValue<TIntrospector>(Node? node, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => node == null ? 0 : GetEnd(node.MaxEndNode.Value, in introspector);

    private static int Height(Node? node)
        => node == null ? 0 : node.Height;

    private static int BalanceFactor(Node? node)
        => node == null ? 0 : Height(node.Left) - Height(node.Right);
}
