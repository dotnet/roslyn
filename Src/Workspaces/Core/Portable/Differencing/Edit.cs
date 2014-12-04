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
        private readonly TNode oldNode;
        private readonly TNode newNode;

        internal Edit(EditKind kind, TreeComparer<TNode> comparer, TNode oldNode, TNode newNode)
        {
            Debug.Assert((oldNode == null || oldNode.Equals(default(TNode))) == (kind == EditKind.Insert));
            Debug.Assert((newNode == null || newNode.Equals(default(TNode))) == (kind == EditKind.Delete));

            Debug.Assert((oldNode == null || oldNode.Equals(default(TNode))) ||
                         (newNode == null || newNode.Equals(default(TNode))) ||
                         !comparer.TreesEqual(oldNode, newNode));

            this.comparer = comparer;
            this.kind = kind;
            this.oldNode = oldNode;
            this.newNode = newNode;
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
        /// Node in the old tree/sequence.
        /// </summary>
        public TNode OldNode { get { return oldNode; } }

        /// <summary>
        /// Insert: 
        /// Inserted node.
        /// 
        /// Delete: 
        /// default(TNode)
        /// 
        /// Move, Update:
        /// Node in the new tree/sequence.
        /// </summary>
        public TNode NewNode { get { return newNode; } }

        public override bool Equals(object obj)
        {
            return obj is Edit<TNode> && Equals((Edit<TNode>)obj);
        }

        public bool Equals(Edit<TNode> other)
        {
            return this.kind == other.kind
                && (this.oldNode == null) ? other.oldNode == null : this.oldNode.Equals(other.oldNode)
                && (this.newNode == null) ? other.newNode == null : this.newNode.Equals(other.newNode);
        }

        public override int GetHashCode()
        {
            int hash = (int)this.kind;
            if (oldNode != null)
            {
                hash = Hash.Combine(oldNode.GetHashCode(), hash);
            }

            if (newNode != null)
            {
                hash = Hash.Combine(newNode.GetHashCode(), hash);
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
                    return result + " [" + oldNode.ToString() + "]" + DisplayPosition(oldNode);

                case EditKind.Insert:
                    return result + " [" + newNode.ToString() + "]" + DisplayPosition(newNode);

                case EditKind.Update:
                    return result + " [" + oldNode.ToString() + "]" + DisplayPosition(oldNode) + " -> [" + newNode.ToString() + "]" + DisplayPosition(newNode);

                case EditKind.Move:
                case EditKind.Reorder:
                    return result + " [" + oldNode.ToString() + "]" + DisplayPosition(oldNode) + " -> " + DisplayPosition(newNode);
            }

            return result;
        }

        private string DisplayPosition(TNode node)
        {
            return "@" + comparer.GetSpan(node).Start;
        }
    }
}
