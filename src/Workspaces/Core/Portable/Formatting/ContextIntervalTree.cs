// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// a tweaked version of our interval tree to meet the formatting engine's need
    /// 
    /// it now has an ability to return a smallest span that contains a position rather than
    /// all Intersecting or overlapping spans
    /// </summary>
    internal class ContextIntervalTree<T> : SimpleIntervalTree<T>
    {
        private readonly Func<T, int, int, bool> _edgeExclusivePredicate;
        private readonly Func<T, int, int, bool> _edgeInclusivePredicate;
        private readonly Func<T, int, int, bool> _containPredicate;

        public ContextIntervalTree(IIntervalIntrospector<T> introspector)
            : base(introspector, values: null)
        {
            _edgeExclusivePredicate = ContainsEdgeExclusive;
            _edgeInclusivePredicate = ContainsEdgeInclusive;
            _containPredicate = (value, start, end) => Contains(value, start, end, Introspector);
        }

        public T GetSmallestEdgeExclusivelyContainingInterval(int start, int length)
        {
            return GetSmallestContainingIntervalWorker(start, length, _edgeExclusivePredicate);
        }

        public T GetSmallestEdgeInclusivelyContainingInterval(int start, int length)
        {
            return GetSmallestContainingIntervalWorker(start, length, _edgeInclusivePredicate);
        }

        public T GetSmallestContainingInterval(int start, int length)
        {
            return GetSmallestContainingIntervalWorker(start, length, _containPredicate);
        }

        private bool ContainsEdgeExclusive(T value, int start, int length)
        {
            var otherStart = start;
            var otherEnd = start + length;

            var thisEnd = GetEnd(value, Introspector);
            var thisStart = Introspector.GetStart(value);

            return thisStart < otherStart && otherEnd < thisEnd;
        }

        private bool ContainsEdgeInclusive(T value, int start, int length)
        {
            var otherStart = start;
            var otherEnd = start + length;

            var thisEnd = GetEnd(value, Introspector);
            var thisStart = Introspector.GetStart(value);

            return thisStart <= otherStart && otherEnd <= thisEnd;
        }

        private T GetSmallestContainingIntervalWorker(int start, int length, Func<T, int, int, bool> predicate)
        {
            var result = default(T);
            if (root == null || MaxEndValue(root) < start)
            {
                return result;
            }

            var end = start + length;

            // * our interval tree is a binary tree that is ordered by a start position.
            //
            // this method works by
            // 1. find a sub tree that has biggest "start" position that is smaller than given "start" by going down right side of a tree
            // 2. once it encounters a right sub tree that it can't go down anymore, move down to left sub tree once and try #1 again
            // 3. once it gets to the position where it can't find any smaller span (both left and
            //    right sub tree doesn't contain given span) start to check whether current node
            //    contains the given "span"
            // 4. move up the spin until it finds one that contains the "span" which should be smallest span that contains the given "span"
            // 5. if it is going up from right side, it make sure to check left side of tree first.
            using var pooledObject = SharedPools.Default<Stack<Node>>().GetPooledObject();

            var spineNodes = pooledObject.Object;

            spineNodes.Push(root);
            while (spineNodes.Count > 0)
            {
                var currentNode = spineNodes.Peek();

                // only goes to right if right tree contains given span
                if (Introspector.GetStart(currentNode.Value) <= start)
                {
                    var right = currentNode.Right;
                    if (right != null && end < MaxEndValue(right))
                    {
                        spineNodes.Push(right);
                        continue;
                    }
                }

                // right side, sub tree doesn't contain the given span, put current node on
                // stack, and move down to left sub tree
                var left = currentNode.Left;
                if (left != null && end <= MaxEndValue(left))
                {
                    spineNodes.Push(left);
                    continue;
                }

                // we reached the point, where we can't go down anymore.
                // now, go back up to find best answer
                while (spineNodes.Count > 0)
                {
                    currentNode = spineNodes.Pop();

                    // check whether current node meets condition
                    if (predicate(currentNode.Value, start, length))
                    {
                        // hold onto best answer
                        if (EqualityComparer<T>.Default.Equals(result, default) ||
                            (Introspector.GetStart(result) <= Introspector.GetStart(currentNode.Value) &&
                             Introspector.GetLength(currentNode.Value) < Introspector.GetLength(result)))
                        {
                            result = currentNode.Value;
                        }
                    }

                    // there is no parent, result we currently have is the best answer
                    if (spineNodes.Count == 0)
                    {
                        return result;
                    }

                    var parentNode = spineNodes.Peek();

                    // if we are under left side of parent node
                    if (parentNode.Left == currentNode)
                    {
                        // go one level up again
                        continue;
                    }

                    // okay, we are under right side of parent node
                    if (parentNode.Right == currentNode)
                    {
                        // try left side of parent node if it can have better answer
                        if (parentNode.Left != null && end <= MaxEndValue(parentNode.Left))
                        {
                            // right side tree doesn't have any answer or if the right side has
                            // an answer but left side can have better answer then try left side
                            if (EqualityComparer<T>.Default.Equals(result, default) ||
                                Introspector.GetStart(parentNode.Value) == Introspector.GetStart(currentNode.Value))
                            {
                                // put left as new root, and break out inner loop
                                spineNodes.Push(parentNode.Left);
                                break;
                            }
                        }

                        // no left side, go one more level up
                        continue;
                    }
                }
            }

            return result;
        }

#if DEBUG
        public override string ToString()
            => $"Interval tree with '{System.Linq.Enumerable.Count(this)}' entries. Use '.ToList()' to visualize contents.";
#endif
    }
}
