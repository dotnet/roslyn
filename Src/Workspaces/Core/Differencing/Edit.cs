// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Differencing
{
    /// <summary>
    /// Represents an edit operation on a tree or a sequence of nodes.
    /// </summary>
    /// <typeparam name="TNode">Tree node.</typeparam>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public struct Edit<TNode> : IEquatable<Edit<TNode>>
    {
        private readonly TreeComparer<TNode> comparer;
        private readonly EditKind kind;
        private readonly TNode node1;
        private readonly TNode node2;

        internal Edit(EditKind kind, TreeComparer<TNode> comparer, TNode node1, TNode node2)
        {
            Debug.Assert((node1 == null || node1.Equals(default(TNode))) == (kind == EditKind.Insert));
            Debug.Assert((node2 == null || node2.Equals(default(TNode))) == (kind == EditKind.Delete));

            Debug.Assert((node1 == null || node1.Equals(default(TNode))) ||
                         (node2 == null || node2.Equals(default(TNode))) ||
                         !comparer.TreesEqual(node1, node2));

            this.comparer = comparer;
            this.kind = kind;
            this.node1 = node1;
            this.node2 = node2;
        }

        public EditKind Kind { get { return kind; } }

        /// <summary>
        /// Insert: 
        /// default(TNode).
        /// 
        /// Delete: 
        /// Deleted node.
        /// 
        /// Move, Update: 
        /// Node in the tree/sequence #1.
        /// </summary>
        public TNode OldNode { get { return node1; } }

        /// <summary>
        /// Insert: 
        /// Inserted node.
        /// 
        /// Delete: 
        /// default(TNode)
        /// 
        /// Move, Update:
        /// Node in the tree/sequence #2.
        /// </summary>
        public TNode NewNode { get { return node2; } }

        public override bool Equals(object obj)
        {
            return obj is Edit<TNode> && Equals((Edit<TNode>)obj);
        }

        public bool Equals(Edit<TNode> other)
        {
            return this.kind == other.kind
                && (this.node1 == null) ? other.node1 == null : this.node1.Equals(other.node1)
                && (this.node2 == null) ? other.node2 == null : this.node2.Equals(other.node2);
        }

        public override int GetHashCode()
        {
            int hash = (int)this.kind;
            if (node1 != null)
            {
                hash = Hash.Combine(node1.GetHashCode(), hash);
            }

            if (node2 != null)
            {
                hash = Hash.Combine(node2.GetHashCode(), hash);
            }

            return hash;
        }

        // Has to be 'internal' for now as it's used by EnC test tool
        internal string GetDebuggerDisplay()
        {
            string result = Kind.ToString();
            switch (Kind)
            {
                case EditKind.Delete:
                    return result + " [" + node1.ToString() + "]" + DisplayPosition(node1);

                case EditKind.Insert:
                    return result + " [" + node2.ToString() + "]" + DisplayPosition(node2);

                case EditKind.Update:
                    return result + " [" + node1.ToString() + "]" + DisplayPosition(node1) + " -> [" + node2.ToString() + "]" + DisplayPosition(node2);

                case EditKind.Move:
                case EditKind.Reorder:
                    return result + " [" + node1.ToString() + "]" + DisplayPosition(node1) + " -> " + DisplayPosition(node2);
            }

            return result;
        }

        private string DisplayPosition(TNode node)
        {
            return "@" + comparer.GetSpan(node).Start;
        }
    }
}
