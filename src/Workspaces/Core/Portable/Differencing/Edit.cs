// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly TreeComparer<TNode> _comparer;
        private readonly EditKind _kind;
        private readonly TNode _oldNode;
        private readonly TNode _newNode;

        internal Edit(EditKind kind, TreeComparer<TNode> comparer, TNode oldNode, TNode newNode)
        {
            Debug.Assert((oldNode == null || oldNode.Equals(default)) == (kind == EditKind.Insert));
            Debug.Assert((newNode == null || newNode.Equals(default)) == (kind == EditKind.Delete));

            Debug.Assert((oldNode == null || oldNode.Equals(default)) ||
                         (newNode == null || newNode.Equals(default)) ||
                         !comparer.TreesEqual(oldNode, newNode));

            _comparer = comparer;
            _kind = kind;
            _oldNode = oldNode;
            _newNode = newNode;
        }

        public EditKind Kind => _kind;

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
        public TNode OldNode => _oldNode;

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
        public TNode NewNode => _newNode;

        public override bool Equals(object obj)
        {
            return obj is Edit<TNode> && Equals((Edit<TNode>)obj);
        }

        public bool Equals(Edit<TNode> other)
        {
            return _kind == other._kind
                && (_oldNode == null) ? other._oldNode == null : _oldNode.Equals(other._oldNode)
                && (_newNode == null) ? other._newNode == null : _newNode.Equals(other._newNode);
        }

        public override int GetHashCode()
        {
            var hash = (int)_kind;
            if (_oldNode != null)
            {
                hash = Hash.Combine(_oldNode.GetHashCode(), hash);
            }

            if (_newNode != null)
            {
                hash = Hash.Combine(_newNode.GetHashCode(), hash);
            }

            return hash;
        }

        // Has to be 'internal' for now as it's used by EnC test tool
        internal string GetDebuggerDisplay()
        {
            var result = Kind.ToString();
            switch (Kind)
            {
                case EditKind.Delete:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode);

                case EditKind.Insert:
                    return result + " [" + _newNode.ToString() + "]" + DisplayPosition(_newNode);

                case EditKind.Update:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode) + " -> [" + _newNode.ToString() + "]" + DisplayPosition(_newNode);

                case EditKind.Move:
                case EditKind.Reorder:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode) + " -> " + DisplayPosition(_newNode);
            }

            return result;
        }

        private string DisplayPosition(TNode node)
        {
            return "@" + _comparer.GetSpan(node).Start;
        }
    }
}
