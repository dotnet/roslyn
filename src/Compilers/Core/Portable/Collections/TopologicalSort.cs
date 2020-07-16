// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        /// <param name="result">A list of all reachable nodes, in which each node always precedes its successors</param>
        /// <returns>true if successful; false if not successful due to cycles in the graph</returns>
        public static bool TryIterativeSort<TNode>(IEnumerable<TNode> nodes, Func<TNode, ImmutableArray<TNode>> successors, out ImmutableArray<TNode> result)
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
            var resultBuilder = ArrayBuilder<TNode>.GetInstance();
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
            bool hadCycle = predecessorCounts.Count != resultBuilder.Count;
            result = hadCycle ? ImmutableArray<TNode>.Empty : resultBuilder.ToImmutable();
            predecessorCounts.Free();
            ready.Free();
            resultBuilder.Free();
            return !hadCycle;
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
