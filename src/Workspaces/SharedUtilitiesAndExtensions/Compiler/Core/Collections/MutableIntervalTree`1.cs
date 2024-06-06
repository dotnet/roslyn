// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Collections;

/// <summary>
/// An interval tree represents an ordered tree data structure to store intervals of the form [start, end).  It allows
/// you to efficiently find all intervals that intersect or overlap a provided interval.
/// </summary>
/// <remarks>
/// Ths is the root type for all interval trees that store their data in a binary tree format.  This format is good for
/// when mutation of the tree is expected, and a client wants to perform tests before and after such mutation.
/// </remarks>
internal partial class MutableIntervalTree<T> : IIntervalTree<T>
{
    public static readonly MutableIntervalTree<T> Empty = new();

    protected Node? root;

    public static MutableIntervalTree<T> Create<TIntrospector>(in TIntrospector introspector, IEnumerable<T> values)
        where TIntrospector : struct, IIntervalIntrospector<T>
    {
        var result = new MutableIntervalTree<T>();

        foreach (var value in values)
            result.root = Insert(result.root, new Node(value), in introspector);

        return result;
    }

    /// <summary>
    /// Provides access to lots of common algorithms on this interval tree.
    /// </summary>
    public IntervalTreeAlgorithms<T, MutableIntervalTree<T>> Algorithms => new(this);

    bool IIntervalTree<T>.Any<TIntrospector>(int start, int length, TestInterval<T, TIntrospector> testInterval, in TIntrospector introspector)
        => IntervalTreeHelpers<T, MutableIntervalTree<T>, Node, BinaryIntervalTreeWitness>.Any(this, start, length, testInterval, in introspector);

    int IIntervalTree<T>.FillWithIntervalsThatMatch<TIntrospector>(
        int start, int length, TestInterval<T, TIntrospector> testInterval,
        ref TemporaryArray<T> builder, in TIntrospector introspector,
        bool stopAfterFirst)
    {
        return IntervalTreeHelpers<T, MutableIntervalTree<T>, Node, BinaryIntervalTreeWitness>.FillWithIntervalsThatMatch(
            this, start, length, testInterval, ref builder, in introspector, stopAfterFirst);
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

        static Node Balance(Node node, in TIntrospector introspector)
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

        static int BalanceFactor(Node? node)
            => node == null ? 0 : Height(node.Left) - Height(node.Right);
    }

    public IntervalTreeHelpers<T, MutableIntervalTree<T>, Node, BinaryIntervalTreeWitness>.Enumerator GetEnumerator()
        => IntervalTreeHelpers<T, MutableIntervalTree<T>, Node, BinaryIntervalTreeWitness>.GetEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator()
        => this.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => this.GetEnumerator();

    protected static int GetEnd<TIntrospector>(T value, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => introspector.GetSpan(value).End;

    protected static int MaxEndValue<TIntrospector>(Node? node, in TIntrospector introspector)
        where TIntrospector : struct, IIntervalIntrospector<T>
        => node == null ? 0 : GetEnd(node.MaxEndNode.Value, in introspector);

    private static int Height(Node? node)
        => node == null ? 0 : node.Height;

    /// <summary>
    /// Wrapper type to allow the IntervalTreeHelpers type to work with this type.
    /// </summary>
    internal readonly struct BinaryIntervalTreeWitness : IIntervalTreeWitness<T, MutableIntervalTree<T>, Node>
    {
        public T GetValue(MutableIntervalTree<T> tree, Node node)
            => node.Value;

        public Node GetMaxEndNode(MutableIntervalTree<T> tree, Node node)
            => node.MaxEndNode;

        public bool TryGetRoot(MutableIntervalTree<T> tree, [NotNullWhen(true)] out Node? root)
        {
            root = tree.root;
            return root != null;
        }

        public bool TryGetLeftNode(MutableIntervalTree<T> tree, Node node, [NotNullWhen(true)] out Node? leftNode)
        {
            leftNode = node.Left;
            return leftNode != null;
        }

        public bool TryGetRightNode(MutableIntervalTree<T> tree, Node node, [NotNullWhen(true)] out Node? rightNode)
        {
            rightNode = node.Right;
            return rightNode != null;
        }
    }
}
