// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.Differencing
{
    // Based on general algorithm described in  
    // "Change Detection in Hierarchically Structured Information"
    // by Sudarshan S. Chawathe, Anand Rajaraman, Hector Garcia-Molina, and Jennifer Widom.

    /// <summary>
    /// Implements a tree differencing algorithm.
    /// </summary>
    /// <remarks>
    /// Subclasses define relationships among tree nodes, and parameters to the differencing algorithm.
    /// </remarks>
    /// <typeparam name="TNode">Tree node.</typeparam>
    public abstract class TreeComparer<TNode>
    {
        protected TreeComparer()
        {
        }

        /// <summary>
        /// Returns an edit script that transforms <paramref name="oldRoot"/> to <paramref name="newRoot"/>.
        /// </summary>
        public EditScript<TNode> ComputeEditScript(TNode oldRoot, TNode newRoot)
            => new Match<TNode>(oldRoot, newRoot, this, knownMatches: null).GetTreeEdits();

        /// <summary>
        /// Returns a match map of <paramref name="oldRoot"/> descendants to <paramref name="newRoot"/> descendants.
        /// </summary>
        public Match<TNode> ComputeMatch(TNode oldRoot, TNode newRoot, IEnumerable<KeyValuePair<TNode, TNode>>? knownMatches = null)
            => new(oldRoot, newRoot, this, knownMatches);

        /// <summary>
        /// Calculates the distance [0..1] of two nodes.
        /// </summary>
        /// <remarks>
        /// The more similar the nodes the smaller the distance.
        /// 
        /// Used to determine whether two nodes of the same label match.
        /// Even if 0 is returned the nodes might be slightly different.
        /// </remarks>
        public abstract double GetDistance(TNode oldNode, TNode newNode);

        /// <summary>
        /// Returns true if the specified nodes have equal values.
        /// </summary>
        /// <remarks>
        /// Called with matching nodes (<paramref name="oldNode"/>, <paramref name="newNode"/>).
        /// Return true if the values of the nodes are the same, or their difference is not important.
        /// </remarks>
        public abstract bool ValuesEqual(TNode oldNode, TNode newNode);

        /// <summary>
        /// The number of distinct labels used in the tree.
        /// </summary>
        protected internal abstract int LabelCount { get; }

        /// <summary>
        /// Returns an integer label corresponding to the given node.
        /// </summary>
        /// <remarks>Returned value must be within [0, LabelCount).</remarks>
        protected internal abstract int GetLabel(TNode node);

        /// <summary>
        /// Returns N > 0 if the node with specified label can't change its N-th ancestor node, zero otherwise.
        /// </summary>
        /// <remarks>
        /// 1st ancestor is the node's parent node.
        /// 2nd ancestor is the node's grandparent node.
        /// etc.
        /// </remarks>
        protected internal abstract int TiedToAncestor(int label);

        /// <summary>
        /// May return null if the <paramref name="node"/> is a leaf.
        /// </summary>
        protected internal abstract IEnumerable<TNode>? GetChildren(TNode node);

        /// <summary>
        /// Enumerates all descendant nodes of the given node in depth-first prefix order.
        /// </summary>
        protected internal abstract IEnumerable<TNode> GetDescendants(TNode node);

        /// <summary>
        /// Returns a parent for the specified node.
        /// </summary>
        protected internal abstract bool TryGetParent(TNode node, [MaybeNullWhen(false)] out TNode parent);

        internal TNode GetParent(TNode node)
        {
            var hasParent = TryGetParent(node, out var parent);
            Debug.Assert(hasParent);
            return parent!;
        }

        internal bool TryGetAncestor(TNode node, int level, [MaybeNullWhen(false)] out TNode ancestor)
        {
            while (level > 0)
            {
                if (TryGetParent(node, out var parent))
                {
                    node = parent;
                }
                else
                {
                    ancestor = default;
                    return false;
                }

                level--;
            }

            ancestor = node;
            return true;
        }

        /// <summary>
        /// Return true if specified nodes belong to the same tree.
        /// </summary>
        protected internal abstract bool TreesEqual(TNode oldNode, TNode newNode);

        /// <summary>
        /// Returns the position of the node.
        /// </summary>
        protected internal abstract TextSpan GetSpan(TNode node);
    }
}
