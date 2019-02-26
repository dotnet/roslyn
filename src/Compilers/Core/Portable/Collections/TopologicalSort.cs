// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A helper class that contains a topological sort algorithm.
    /// </summary>
    internal static class TopologicalSort
    {
        /// <summary>
        /// Produce a topological sort of a given directed acyclic graph, given a set of nodes which include all nodes
        /// that have no predecessors. Any nodes not in the given set, but reachable through successors, will be added
        /// to the result. This is an iterative rather than recursive implementation, so it is unlikely to cause a stack
        /// overflow.
        /// </summary>
        /// <typeparam name="TNode">The type of the node</typeparam>
        /// <param name="nodes">Any subset of the nodes that includes all nodes with no predecessors</param>
        /// <param name="successors">A function mapping a node to its set of successors</param>
        /// <returns>A list of all reachable nodes, in which each node always precedes its successors</returns>
        public static ImmutableArray<TNode> IterativeSort<TNode>(IEnumerable<TNode> nodes, Func<TNode, ImmutableArray<TNode>> successors)
        {
            // First, count the predecessors of each node
            PooledDictionary<TNode, int> predecessorCounts = PredecessorCounts(nodes, successors, out ImmutableArray<TNode> allNodes);

            // Initialize the ready set with those nodes that have no predecessors
            var ready = ArrayBuilder<TNode>.GetInstance();
            foreach (TNode node in allNodes)
            {
                if (predecessorCounts[node] == 0)
                {
                    ready.Push(node);
                }
            }

            // Process the ready set. Output a node, and decrement the predecessor count of its successors.
            var resultBuilder = ImmutableArray.CreateBuilder<TNode>();
            while (ready.Count != 0)
            {
                var node = ready.Pop();
                resultBuilder.Add(node);
                foreach (var succ in successors(node))
                {
                    var count = predecessorCounts[succ];
                    Debug.Assert(count != 0);
                    predecessorCounts[succ] = count - 1;
                    if (count == 1)
                    {
                        ready.Push(succ);
                    }
                }
            }

            // At this point all the nodes should have been output, otherwise there was a cycle
            if (predecessorCounts.Count != resultBuilder.Count)
            {
                throw new ArgumentException("Cycle in the input graph");
            }

            predecessorCounts.Free();
            ready.Free();
            return resultBuilder.ToImmutable();
        }

        private static PooledDictionary<TNode, int> PredecessorCounts<TNode>(
            IEnumerable<TNode> nodes,
            Func<TNode, ImmutableArray<TNode>> successors,
            out ImmutableArray<TNode> allNodes)
        {
            var predecessorCounts = PooledDictionary<TNode, int>.GetInstance();
            var counted = PooledHashSet<TNode>.GetInstance();
            var toCount = ArrayBuilder<TNode>.GetInstance();
            var allNodesBuilder = ArrayBuilder<TNode>.GetInstance();
            toCount.AddRange(nodes);
            while (toCount.Count != 0)
            {
                var n = toCount.Pop();
                if (!counted.Add(n))
                {
                    continue;
                }

                allNodesBuilder.Add(n);
                if (!predecessorCounts.ContainsKey(n))
                {
                    predecessorCounts.Add(n, 0);
                }

                foreach (var succ in successors(n))
                {
                    toCount.Push(succ);
                    if (predecessorCounts.TryGetValue(succ, out int succPredecessorCount))
                    {
                        predecessorCounts[succ] = succPredecessorCount + 1;
                    }
                    else
                    {
                        predecessorCounts.Add(succ, 1);
                    }
                }
            }

            counted.Free();
            toCount.Free();
            allNodes = allNodesBuilder.ToImmutableAndFree();
            return predecessorCounts;
        }
    }
}
