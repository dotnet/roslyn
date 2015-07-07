// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// An interval tree represents an ordered tree data structure to store intervals of the form [start, end).  It
    /// allows you to efficiently find all intervals that intersect or overlap a provided interval.
    /// </summary>
    internal partial class IntervalTree<T> : IEnumerable<T>
    {
        public static readonly IntervalTree<T> Empty = new IntervalTree<T>();

        protected Node root;

        private delegate bool TestInterval(T value, int start, int length, IIntervalIntrospector<T> introspector);
        private static readonly TestInterval s_intersectsWithTest = IntersectsWith;
        private static readonly TestInterval s_containsTest = Contains;
        private static readonly TestInterval s_overlapsWithTest = OverlapsWith;

        protected IntervalTree(Node root)
        {
            this.root = root;
        }

        public IntervalTree()
            : this(root: null)
        {
        }

        public IntervalTree(IIntervalIntrospector<T> introspector, IEnumerable<T> values)
            : this(root: null)
        {
            foreach (var value in values)
            {
                root = Insert(root, new Node(introspector, value), introspector, inPlace: true);
            }
        }

        protected static bool Contains(T value, int start, int length, IIntervalIntrospector<T> introspector)
        {
            var otherStart = start;
            var otherEnd = start + length;

            var thisEnd = GetEnd(value, introspector);
            var thisStart = introspector.GetStart(value);

            // make sure "Contains" test to be same as what TextSpan does
            if (length == 0)
            {
                return thisStart <= otherStart && otherEnd < thisEnd;
            }

            return thisStart <= otherStart && otherEnd <= thisEnd;
        }

        private static bool IntersectsWith(T value, int start, int length, IIntervalIntrospector<T> introspector)
        {
            var otherStart = start;
            var otherEnd = start + length;

            var thisEnd = GetEnd(value, introspector);
            var thisStart = introspector.GetStart(value);

            return otherStart <= thisEnd && otherEnd >= thisStart;
        }

        private static bool OverlapsWith(T value, int start, int length, IIntervalIntrospector<T> introspector)
        {
            var otherStart = start;
            var otherEnd = start + length;

            var thisEnd = GetEnd(value, introspector);
            var thisStart = introspector.GetStart(value);

            if (length == 0)
            {
                return thisStart < otherStart && otherStart < thisEnd;
            }

            int overlapStart = Math.Max(thisStart, otherStart);
            int overlapEnd = Math.Min(thisEnd, otherEnd);

            return overlapStart < overlapEnd;
        }

        public IEnumerable<T> GetOverlappingIntervals(int start, int length, IIntervalIntrospector<T> introspector)
        {
            return this.GetInOrderIntervals(start, length, s_overlapsWithTest, introspector);
        }

        public IEnumerable<T> GetIntersectingIntervals(int start, int length, IIntervalIntrospector<T> introspector)
        {
            return this.GetInOrderIntervals(start, length, s_intersectsWithTest, introspector);
        }

        public IEnumerable<T> GetContainingIntervals(int start, int length, IIntervalIntrospector<T> introspector)
        {
            return this.GetInOrderIntervals(start, length, s_containsTest, introspector);
        }

        public bool IntersectsWith(int position, IIntervalIntrospector<T> introspector)
        {
            return GetIntersectingIntervals(position, 0, introspector).Any();
        }

        private IEnumerable<T> GetInOrderIntervals(int start, int length, TestInterval testInterval, IIntervalIntrospector<T> introspector)
        {
            if (root == null)
            {
                yield break;
            }

            var end = start + length;

            // The bool indicates if this is the first time we are seeing the node.
            var candidates = new Stack<ValueTuple<Node, bool>>();
            candidates.Push(ValueTuple.Create(root, true));

            while (candidates.Count > 0)
            {
                var currentTuple = candidates.Pop();
                var currentNode = currentTuple.Item1;
                Debug.Assert(currentNode != null);

                var firstTime = currentTuple.Item2;

                if (!firstTime)
                {
                    // We're seeing this node for the second time (as we walk back up the left
                    // side of it).  Now see if it matches our test, and if so return it out.
                    if (testInterval(currentNode.Value, start, length, introspector))
                    {
                        yield return currentNode.Value;
                    }
                }
                else
                {
                    // First time we're seeing this node.  In order to see the node 'in-order',
                    // we push the right side, then the node again, then the left side.  This 
                    // time we mark the current node with 'false' to indicate that it's the
                    // second time we're seeing it the next time it comes around.

                    // right children's starts will never be to the left of the parent's start
                    // so we should consider right subtree only if root's start overlaps with
                    // interval's End, 
                    if (introspector.GetStart(currentNode.Value) <= end)
                    {
                        var right = currentNode.Right;
                        if (right != null && GetEnd(right.MaxEndNode.Value, introspector) >= start)
                        {
                            candidates.Push(ValueTuple.Create(right, true));
                        }
                    }

                    candidates.Push(ValueTuple.Create(currentNode, false));

                    // only if left's maxVal overlaps with interval's start, we should consider 
                    // left subtree
                    var left = currentNode.Left;
                    if (left != null && GetEnd(left.MaxEndNode.Value, introspector) >= start)
                    {
                        candidates.Push(ValueTuple.Create(left, true));
                    }
                }
            }
        }

        public IntervalTree<T> AddInterval(T value, IIntervalIntrospector<T> introspector)
        {
            var newNode = new Node(introspector, value);
            return new IntervalTree<T>(Insert(root, newNode, introspector, inPlace: false));
        }

        public bool IsEmpty()
        {
            return this.root == null;
        }

        protected static Node Insert(Node root, Node newNode, IIntervalIntrospector<T> introspector, bool inPlace)
        {
            var newNodeStart = introspector.GetStart(newNode.Value);
            return Insert(root, newNode, newNodeStart, introspector, inPlace);
        }

        private static Node Insert(Node root, Node newNode, int newNodeStart, IIntervalIntrospector<T> introspector, bool inPlace)
        {
            if (root == null)
            {
                return newNode;
            }

            Node newLeft, newRight;

            if (newNodeStart < introspector.GetStart(root.Value))
            {
                newLeft = Insert(root.Left, newNode, newNodeStart, introspector, inPlace);
                newRight = root.Right;
            }
            else
            {
                newLeft = root.Left;
                newRight = Insert(root.Right, newNode, newNodeStart, introspector, inPlace);
            }

            Node newRoot;
            if (inPlace)
            {
                root.SetLeftRight(newLeft, newRight, introspector);
                newRoot = root;
            }
            else
            {
                newRoot = new Node(introspector, root.Value, newLeft, newRight);
            }

            return Balance(newRoot, inPlace, introspector);
        }

        private static Node Balance(Node node, bool inPlace, IIntervalIntrospector<T> introspector)
        {
            int balanceFactor = BalanceFactor(node);
            if (balanceFactor == -2)
            {
                int rightBalance = BalanceFactor(node.Right);
                if (rightBalance == -1)
                {
                    return node.LeftRotation(inPlace, introspector);
                }
                else
                {
                    Contract.Requires(rightBalance == 1);
                    return node.InnerRightOuterLeftRotation(inPlace, introspector);
                }
            }
            else if (balanceFactor == 2)
            {
                int leftBalance = BalanceFactor(node.Left);
                if (leftBalance == 1)
                {
                    return node.RightRotation(inPlace, introspector);
                }
                else
                {
                    Contract.Requires(leftBalance == -1);
                    return node.InnerLeftOuterRightRotation(inPlace, introspector);
                }
            }

            return node;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (root == null)
            {
                yield break;
            }

            // The bool indicates if this is the first time we are seeing the node.
            var candidates = new Stack<ValueTuple<Node, bool>>();
            candidates.Push(ValueTuple.Create(root, true));
            while (candidates.Count != 0)
            {
                var currentTuple = candidates.Pop();
                var currentNode = currentTuple.Item1;
                if (currentNode != null)
                {
                    if (currentTuple.Item2)
                    {
                        // First time seeing this node.  Mark that we've been seen and recurse
                        // down the left side.  The next time we see this node we'll yield it
                        // out.
                        candidates.Push(ValueTuple.Create(currentNode.Right, true));
                        candidates.Push(ValueTuple.Create(currentNode, false));
                        candidates.Push(ValueTuple.Create(currentNode.Left, true));
                    }
                    else
                    {
                        yield return currentNode.Value;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        protected static int GetEnd(T value, IIntervalIntrospector<T> introspector)
        {
            return introspector.GetStart(value) + introspector.GetLength(value);
        }

        protected static int MaxEndValue(Node node, IIntervalIntrospector<T> arg)
        {
            return node == null ? 0 : GetEnd(node.MaxEndNode.Value, arg);
        }

        private static int Height(Node node)
        {
            return node == null ? 0 : node.Height;
        }

        private static int BalanceFactor(Node node)
        {
            if (node == null)
            {
                return 0;
            }

            return Height(node.Left) - Height(node.Right);
        }
    }
}
