// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
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
    internal abstract class TreeComparer
    {
        protected TreeComparer()
        {
        }

        /// <summary>
        /// Returns an edit script that transforms <paramref name="oldRoot"/> to <paramref name="newRoot"/>.
        /// </summary>
        public SyntaxEditScript ComputeEditScript(SyntaxNode oldRoot, SyntaxNode newRoot)
        {
            return new SyntaxMatch(oldRoot, newRoot, this, knownMatches: null).GetTreeEdits();
        }

        /// <summary>
        /// Returns a match map of <paramref name="oldRoot"/> descendants to <paramref name="newRoot"/> descendants.
        /// </summary>
        public SyntaxMatch ComputeMatch(SyntaxNode oldRoot, SyntaxNode newRoot, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>> knownMatches = null)
        {
            return new SyntaxMatch(oldRoot, newRoot, this, knownMatches);
        }

        /// <summary>
        /// Calculates the distance [0..1] of two nodes.
        /// </summary>
        /// <remarks>
        /// The more similar the nodes the smaller the distance.
        /// 
        /// Used to determine whether two nodes of the same label match.
        /// Even if 0 is returned the nodes might be slightly different.
        /// </remarks>
        public abstract double GetDistance(SyntaxNode oldNode, SyntaxNode newNode);

        /// <summary>
        /// Returns true if the specified nodes have equal values.
        /// </summary>
        /// <remarks>
        /// Called with matching nodes (<paramref name="oldNode"/>, <paramref name="newNode"/>).
        /// Return true if the values of the nodes are the same, or their difference is not important.
        /// </remarks>
        public abstract bool ValuesEqual(SyntaxNode oldNode, SyntaxNode newNode);

        /// <summary>
        /// The number of distinct labels used in the tree.
        /// </summary>
        protected internal abstract int LabelCount { get; }

        /// <summary>
        /// Returns an integer label corresponding to the given node.
        /// </summary>
        /// <remarks>Returned value must be within [0, LabelCount).</remarks>
        protected internal abstract int GetLabel(SyntaxNode node);

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
        protected internal abstract IEnumerable<SyntaxNode> GetChildren(SyntaxNode node);

        /// <summary>
        /// Enumerates all descendant nodes of the given node in depth-first prefix order.
        /// </summary>
        protected internal abstract IEnumerable<SyntaxNode> GetDescendants(SyntaxNode node);

        /// <summary>
        /// Returns a parent for the specified node.
        /// </summary>
        protected internal abstract bool TryGetParent(SyntaxNode node, out SyntaxNode parent);

        internal SyntaxNode GetParent(SyntaxNode node)
        {
            SyntaxNode parent;
            bool hasParent = TryGetParent(node, out parent);
            Debug.Assert(hasParent);
            return parent;
        }

        internal SyntaxNode GetAncestor(SyntaxNode node, int level)
        {
            while (level > 0)
            {
                node = GetParent(node);
                level--;
            }

            return node;
        }

        /// <summary>
        /// Return true if specified nodes belong to the same tree.
        /// </summary>
        protected internal abstract bool TreesEqual(SyntaxNode oldNode, SyntaxNode newNode);

        /// <summary>
        /// Returns the position of the node.
        /// </summary>
        protected internal abstract TextSpan GetSpan(SyntaxNode node);
    }
}
